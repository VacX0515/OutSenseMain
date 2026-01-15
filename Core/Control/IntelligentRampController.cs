using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Extensions; // 확장 메서드 추가
using static VacX_OutSense.Core.Control.ExpertParameterSystem;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 실시간 피드백 기반 지능형 Ramp 제어 엔진 (수정판)
    /// </summary>
    public class IntelligentRampController : IDisposable
    {
        #region 필드 및 속성

        private readonly TempController _tempController;
        private ExperimentProfile _currentProfile;
        private readonly System.Timers.Timer _controlTimer;
        private readonly object _lockObject = new object();

        // 제어 상태
        private bool _isRunning;
        private DateTime _rampStartTime;
        private double _targetSampleTemp;
        private double _targetRampRate;
        private double _initialSampleTemp;

        // PID 제어
        private double _integral;
        private double _previousError;
        private DateTime _lastControlTime;

        // 데이터 버퍼
        private readonly Queue<ThermalDataPoint> _sampleHistory;
        private readonly Queue<ThermalDataPoint> _heaterHistory;

        // 통계
        public RampStatistics Statistics { get; private set; }

        /// <summary>
        /// 현재 실행 중 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Ramp 진행률 (0-100%)
        /// </summary>
        public double Progress { get; private set; }

        #endregion

        #region 이벤트

        public event EventHandler<RampStatusEventArgs> StatusChanged;
        public event EventHandler<ThermalDataEventArgs> DataUpdated;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<RampCompletedEventArgs> RampCompleted;

        #endregion

        #region 내부 클래스

        public class ThermalDataPoint
        {
            public DateTime Timestamp { get; set; }
            public double Temperature { get; set; }
            public double SetPoint { get; set; }
            public double Output { get; set; }
        }

        public class RampStatistics
        {
            public double AverageError { get; set; }
            public double MaxError { get; set; }
            public double RampTime { get; set; }
            public double MaxOvershoot { get; set; }
            public double SettlingTime { get; set; }
            public double AverageHeaterSampleDelta { get; set; }
        }

        public class RampStatusEventArgs : EventArgs
        {
            public string Status { get; set; }
            public double CurrentTemp { get; set; }
            public double TargetTemp { get; set; }
            public double HeaterTemp { get; set; }
            public double Progress { get; set; }
        }

        public class ThermalDataEventArgs : EventArgs
        {
            public double[] SampleTemps { get; set; }
            public double HeaterTemp { get; set; }
            public double HeaterOutput { get; set; }
            public double Error { get; set; }
        }

        public class RampCompletedEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public RampStatistics Statistics { get; set; }
            public string Message { get; set; }
        }

        #endregion

        #region 생성자

        public IntelligentRampController(TempController tempController)
        {
            _tempController = tempController ?? throw new ArgumentNullException(nameof(tempController));

            _sampleHistory = new Queue<ThermalDataPoint>(1000);
            _heaterHistory = new Queue<ThermalDataPoint>(1000);
            Statistics = new RampStatistics();

            _controlTimer = new System.Timers.Timer(100); // 100ms 제어 주기
            _controlTimer.Elapsed += ControlTimer_Elapsed;
        }

        #endregion

        #region 메인 제어 메서드

        /// <summary>
        /// 프로파일 기반 Ramp 실행
        /// </summary>
        public async Task<bool> ExecuteRampWithProfile(
            ExperimentProfile profile,
            double targetSampleTemp,
            double rampRate)
        {
            if (_isRunning)
            {
                OnError("이미 Ramp가 실행 중입니다.");
                return false;
            }

            try
            {
                // 프로파일 설정
                _currentProfile = profile;
                _targetSampleTemp = targetSampleTemp;
                _targetRampRate = rampRate;

                // 안전 검증
                if (!ValidateSafety(targetSampleTemp, rampRate))
                    return false;

                // 초기화
                await Initialize();

                // Ramp 실행
                _isRunning = true;
                _controlTimer.Start();

                OnStatusChanged($"Ramp 시작: {_initialSampleTemp:F1}°C → {targetSampleTemp:F1}°C @ {rampRate:F1}°C/min");

                // 완료 대기
                await WaitForCompletion();

                // 통계 계산
                CalculateStatistics();

                OnRampCompleted(true, "Ramp 완료");
                return true;
            }
            catch (Exception ex)
            {
                OnError($"Ramp 실행 오류: {ex.Message}");
                return false;
            }
            finally
            {
                _isRunning = false;
                _controlTimer.Stop();
            }
        }

        /// <summary>
        /// 초기화
        /// </summary>
        private async Task Initialize()
        {
            // 현재 온도 읽기 - 확장 메서드 사용
            _tempController.UpdateAllChannelStatus();
            _initialSampleTemp = GetAverageSampleTemp();

            // PID 초기화
            _integral = 0;
            _previousError = 0;
            _lastControlTime = DateTime.Now;
            _rampStartTime = DateTime.Now;

            // 버퍼 초기화
            _sampleHistory.Clear();
            _heaterHistory.Clear();

            // 초기 히터 온도 설정
            double initialHeaterTemp = CalculateInitialHeaterTemp();
            SetHeaterTemperature(1, initialHeaterTemp);

            // CH1 시작 - 확장 메서드 사용
            _tempController.SetRunStop(1, true);

            await Task.Delay(100);
        }

        /// <summary>
        /// 초기 히터 온도 계산 (프로파일 기반)
        /// </summary>
        private double CalculateInitialHeaterTemp()
        {
            var sample = _currentProfile.SampleSettings;
            var comp = _currentProfile.CompensationSettings;

            // 기본 오프셋
            double baseOffset = sample.SteadyStateOffset;

            // 온도별 보정
            if (sample.TemperatureOffsets != null && sample.TemperatureOffsets.Any())
            {
                var closest = sample.TemperatureOffsets
                    .OrderBy(kvp => Math.Abs(kvp.Key - _targetSampleTemp))
                    .First();
                baseOffset = closest.Value;
            }

            // 초기 부스트
            double boost = sample.InitialOvershoot * comp.InitialBoostFactor;

            // 열용량 보정
            double thermalCorrection = (1.0 - sample.ThermalMass) * 10;

            return _targetSampleTemp + baseOffset + boost + thermalCorrection;
        }

        #endregion

        #region 제어 루프

        /// <summary>
        /// 제어 타이머 이벤트 (100ms마다)
        /// </summary>
        private void ControlTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!_isRunning) return;

            try
            {
                lock (_lockObject)
                {
                    // 상태 업데이트
                    UpdateStatus();

                    // 제어 실행
                    ExecuteControl();

                    // 데이터 기록
                    RecordData();

                    // 이벤트 발생
                    PublishData();
                }
            }
            catch (Exception ex)
            {
                OnError($"제어 루프 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 상태 업데이트
        /// </summary>
        private void UpdateStatus()
        {
            // 모든 채널 상태 읽기 - 확장 메서드 사용
            _tempController.UpdateAllChannelStatus();

            // 진행률 계산
            double elapsedMinutes = (DateTime.Now - _rampStartTime).TotalMinutes;
            double expectedTemp = _initialSampleTemp + (_targetRampRate * elapsedMinutes);
            expectedTemp = Math.Min(expectedTemp, _targetSampleTemp);

            double currentSampleTemp = GetAverageSampleTemp();
            Progress = ((currentSampleTemp - _initialSampleTemp) /
                       (_targetSampleTemp - _initialSampleTemp)) * 100;
            Progress = Math.Max(0, Math.Min(100, Progress));
        }

        /// <summary>
        /// 제어 실행
        /// </summary>
        private void ExecuteControl()
        {
            double currentSampleTemp = GetAverageSampleTemp();
            double currentHeaterTemp = GetHeaterTemp();
            double elapsedMinutes = (DateTime.Now - _rampStartTime).TotalMinutes;

            // 목표 궤적 계산
            double expectedSampleTemp = CalculateExpectedTemp(elapsedMinutes);

            // 오차 계산
            double error = expectedSampleTemp - currentSampleTemp;

            // 히터 조정량 계산
            double adjustment = CalculateHeaterAdjustment(
                error,
                currentSampleTemp,
                currentHeaterTemp,
                elapsedMinutes);

            // 새 히터 온도 설정
            double newHeaterTemp = currentHeaterTemp + adjustment;

            // 안전 제한 적용
            newHeaterTemp = ApplySafetyLimits(newHeaterTemp, currentSampleTemp);

            // 히터 온도 설정
            SetHeaterTemperature(1, newHeaterTemp);
        }

        /// <summary>
        /// 히터 조정량 계산 (핵심 알고리즘)
        /// </summary>
        private double CalculateHeaterAdjustment(
            double error,
            double currentSampleTemp,
            double currentHeaterTemp,
            double elapsedTime)
        {
            var pid = _currentProfile.PIDSettings;
            var sample = _currentProfile.SampleSettings;
            var comp = _currentProfile.CompensationSettings;

            DateTime now = DateTime.Now;
            double dt = (now - _lastControlTime).TotalSeconds;
            if (dt <= 0) return 0;

            // 1. 적응형 PID 게인 계산
            double adaptiveKp = CalculateAdaptiveGain(currentSampleTemp, sample, pid.ProportionalGain);
            double adaptiveKi = pid.IntegralGain * sample.ContactQuality;
            double adaptiveKd = pid.DerivativeGain * (1.0 + sample.ResponseDelay / 30.0);

            // 2. PID 계산
            // P항
            double proportional = adaptiveKp * error;

            // I항 (안티 와인드업)
            _integral += error * dt;
            _integral = Math.Max(-pid.IntegralLimit, Math.Min(pid.IntegralLimit, _integral));
            double integral = adaptiveKi * _integral;

            // D항
            double derivative = 0;
            if (dt > 0)
            {
                derivative = adaptiveKd * (error - _previousError) / dt;
            }

            double pidOutput = proportional + integral + derivative;

            // 3. 피드포워드 보상
            double feedforward = 0;
            if (comp.EnableDynamicCompensation)
            {
                double currentDelta = currentHeaterTemp - currentSampleTemp;

                if (error > 1.0) // 샘플이 뒤처짐
                {
                    feedforward = error * (1 + currentDelta / 50.0) * comp.PredictiveWeight;
                }
                else if (error < -1.0) // 샘플이 너무 빠름  
                {
                    feedforward = error * 0.5 * comp.PredictiveWeight;
                }
            }

            // 4. 오버슈트 방지
            double overshootPrevention = 0;
            if (comp.EnableOvershootPrevention)
            {
                double remaining = _targetSampleTemp - currentSampleTemp;
                if (remaining < comp.SlowdownThreshold && remaining > 0)
                {
                    // 목표 근처에서 감속
                    double slowdownFactor = remaining / comp.SlowdownThreshold;
                    overshootPrevention = -pidOutput * (1 - slowdownFactor) * 0.5;
                }
            }

            // 5. 총 조정량
            double totalAdjustment = pidOutput + feedforward + overshootPrevention;

            // 6. 변화율 제한
            double maxAdjust = _currentProfile.SafetyLimits.MaxHeaterAdjustment;
            totalAdjustment = Math.Max(-maxAdjust, Math.Min(maxAdjust, totalAdjustment));

            _previousError = error;
            _lastControlTime = now;

            return totalAdjustment;
        }

        /// <summary>
        /// 적응형 P 게인 계산
        /// </summary>
        private double CalculateAdaptiveGain(double currentTemp, SamplePreset sample, double baseGain)
        {
            // 온도가 높을수록 게인 증가 (열복사 손실 보상)
            double tempFactor = 1.0 + (currentTemp / 200.0);

            // 열용량이 클수록 게인 증가
            double massFactor = 1.0 + (sample.ThermalMass * 0.5);

            // 접촉이 나쁠수록 게인 증가
            double contactFactor = 1.0 + ((1.0 - sample.ContactQuality) * 0.5);

            return baseGain * tempFactor * massFactor * contactFactor;
        }

        /// <summary>
        /// 안전 제한 적용
        /// </summary>
        private double ApplySafetyLimits(double heaterTemp, double sampleTemp)
        {
            var safety = _currentProfile.SafetyLimits;
            var sample = _currentProfile.SampleSettings;

            // 최대 온도 제한
            heaterTemp = Math.Min(heaterTemp, sample.MaxHeaterTemp);

            // 최대 온도차 제한
            double maxDelta = safety.MaxTempDifference;
            if (heaterTemp - sampleTemp > maxDelta)
            {
                heaterTemp = sampleTemp + maxDelta;
            }

            // 비상 정지 체크
            if (heaterTemp - sampleTemp > safety.EmergencyStopDelta)
            {
                OnError($"비상 정지: 온도차 {heaterTemp - sampleTemp:F1}°C 초과");
                EmergencyStop();
            }

            return Math.Max(0, heaterTemp);
        }

        #endregion

        #region 헬퍼 메서드

        /// <summary>
        /// 평균 샘플 온도 계산 (CH2-5)
        /// </summary>
        private double GetAverageSampleTemp()
        {
            var temps = new List<double>();
            for (int ch = 2; ch <= 5; ch++)
            {
                if (ch <= _tempController.Status.ChannelStatus.Length)
                {
                    var status = _tempController.Status.ChannelStatus[ch - 1];
                    double temp = status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
                    temps.Add(temp);
                }
            }
            return temps.Any() ? temps.Average() : 25.0;
        }

        /// <summary>
        /// 히터 온도 읽기 (CH1)
        /// </summary>
        private double GetHeaterTemp()
        {
            var status = _tempController.Status.ChannelStatus[0];
            return status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        /// <summary>
        /// 히터 온도 설정
        /// </summary>
        private void SetHeaterTemperature(int channel, double temperature)
        {
            short setValue = (short)(temperature *
                (_tempController.Status.ChannelStatus[channel - 1].Dot == 0 ? 1 : 10));
            _tempController.SetTemperature(channel, setValue);
        }

        /// <summary>
        /// 예상 온도 계산
        /// </summary>
        private double CalculateExpectedTemp(double elapsedMinutes)
        {
            double tempRise = _targetRampRate * elapsedMinutes;
            double expectedTemp = _initialSampleTemp + tempRise;
            return Math.Min(expectedTemp, _targetSampleTemp);
        }

        /// <summary>
        /// 안전 검증
        /// </summary>
        private bool ValidateSafety(double targetTemp, double rampRate)
        {
            var sample = _currentProfile.SampleSettings;
            var safety = _currentProfile.SafetyLimits;

            if (targetTemp > sample.MaxHeaterTemp - 20)
            {
                OnError($"목표 온도가 너무 높습니다. 최대: {sample.MaxHeaterTemp - 20}°C");
                return false;
            }

            if (rampRate > sample.MaxRampRate)
            {
                OnError($"Ramp Rate가 너무 높습니다. 최대: {sample.MaxRampRate}°C/min");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 완료 대기
        /// </summary>
        private async Task WaitForCompletion()
        {
            double tolerance = 0.5;
            int stableCount = 0;
            int requiredStableCount = 10;

            DateTime timeoutTime = DateTime.Now.AddMinutes(_currentProfile.SafetyLimits.TargetReachTimeout);

            while (_isRunning && DateTime.Now < timeoutTime)
            {
                double currentTemp = GetAverageSampleTemp();
                double error = Math.Abs(currentTemp - _targetSampleTemp);

                if (error < tolerance)
                {
                    stableCount++;
                    if (stableCount >= requiredStableCount)
                    {
                        OnStatusChanged($"목표 온도 {_targetSampleTemp:F1}°C 도달 및 안정화");
                        break;
                    }
                }
                else
                {
                    stableCount = 0;
                }

                await Task.Delay(1000);
            }

            if (DateTime.Now >= timeoutTime)
            {
                OnError("목표 도달 타임아웃");
            }
        }

        /// <summary>
        /// 데이터 기록
        /// </summary>
        private void RecordData()
        {
            var samplePoint = new ThermalDataPoint
            {
                Timestamp = DateTime.Now,
                Temperature = GetAverageSampleTemp(),
                SetPoint = _targetSampleTemp,
                Output = _tempController.Status.ChannelStatus[0].HeatingMV
            };

            var heaterPoint = new ThermalDataPoint
            {
                Timestamp = DateTime.Now,
                Temperature = GetHeaterTemp(),
                SetPoint = _targetSampleTemp,
                Output = _tempController.Status.ChannelStatus[0].HeatingMV
            };

            _sampleHistory.Enqueue(samplePoint);
            _heaterHistory.Enqueue(heaterPoint);

            // 버퍼 크기 제한
            while (_sampleHistory.Count > 1000)
                _sampleHistory.Dequeue();
            while (_heaterHistory.Count > 1000)
                _heaterHistory.Dequeue();
        }

        /// <summary>
        /// 통계 계산
        /// </summary>
        private void CalculateStatistics()
        {
            if (!_sampleHistory.Any()) return;

            var samples = _sampleHistory.ToArray();
            var heaters = _heaterHistory.ToArray();

            Statistics = new RampStatistics
            {
                RampTime = (DateTime.Now - _rampStartTime).TotalMinutes,
                AverageError = samples.Average(s => Math.Abs(s.SetPoint - s.Temperature)),
                MaxError = samples.Max(s => Math.Abs(s.SetPoint - s.Temperature)),
                MaxOvershoot = samples.Max(s => Math.Max(0, s.Temperature - s.SetPoint)),
                AverageHeaterSampleDelta = heaters.Zip(samples, (h, s) => h.Temperature - s.Temperature).Average()
            };
        }

        /// <summary>
        /// 비상 정지
        /// </summary>
        public void EmergencyStop()
        {
            _isRunning = false;
            _controlTimer.Stop();
            _tempController.SetRunStop(1, false); // 확장 메서드 사용
            OnStatusChanged("비상 정지 실행됨");
        }

        #endregion

        #region 이벤트 발생

        private void OnStatusChanged(string status)
        {
            StatusChanged?.Invoke(this, new RampStatusEventArgs
            {
                Status = status,
                CurrentTemp = GetAverageSampleTemp(),
                TargetTemp = _targetSampleTemp,
                HeaterTemp = GetHeaterTemp(),
                Progress = Progress
            });
        }

        private void OnError(string error)
        {
            ErrorOccurred?.Invoke(this, error);
        }

        private void OnRampCompleted(bool success, string message)
        {
            RampCompleted?.Invoke(this, new RampCompletedEventArgs
            {
                Success = success,
                Statistics = Statistics,
                Message = message
            });
        }

        private void PublishData()
        {
            var sampleTemps = new double[4];
            for (int i = 0; i < 4; i++)
            {
                var status = _tempController.Status.ChannelStatus[i + 1];
                sampleTemps[i] = status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
            }

            DataUpdated?.Invoke(this, new ThermalDataEventArgs
            {
                SampleTemps = sampleTemps,
                HeaterTemp = GetHeaterTemp(),
                HeaterOutput = _tempController.Status.ChannelStatus[0].HeatingMV,
                Error = _previousError
            });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _controlTimer?.Stop();
            _controlTimer?.Dispose();
        }

        #endregion
    }
}