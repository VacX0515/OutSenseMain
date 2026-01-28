using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 단순화된 열 램프 컨트롤러
    /// 사용자 친화적인 ThermalRampProfile을 기반으로 동작
    /// </summary>
    public class SimpleThermalRampController : IDisposable
    {
        #region 열거형 및 이벤트 인자

        public enum RampState
        {
            Idle,           // 대기
            Ramping,        // 램프 진행 중
            Stabilizing,    // 목표 도달, 안정화 중
            Completed,      // 완료 (타이머 시작 가능)
            Holding,        // 온도 유지 중
            Stopped,        // 정지됨
            Error           // 오류
        }

        public class RampProgressEventArgs : EventArgs
        {
            public RampState State { get; set; }
            public double HeaterTemp { get; set; }
            public double SampleTemp { get; set; }
            public double TargetTemp { get; set; }
            public double HeaterSetpoint { get; set; }
            public double ProgressPercent { get; set; }
            public string StatusMessage { get; set; }
            public TimeSpan ElapsedTime { get; set; }
        }

        public class RampCompletedEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public TimeSpan TotalTime { get; set; }
            public double FinalSampleTemp { get; set; }
        }

        #endregion

        #region 이벤트

        /// <summary>
        /// 진행 상태 업데이트 (주기적 발생)
        /// </summary>
        public event EventHandler<RampProgressEventArgs> ProgressUpdated;

        /// <summary>
        /// 램프 완료 (성공 또는 실패)
        /// </summary>
        public event EventHandler<RampCompletedEventArgs> RampCompleted;

        /// <summary>
        /// 목표 온도 도달 (타이머 시작 트리거용)
        /// </summary>
        public event EventHandler TargetReached;

        /// <summary>
        /// 오류 발생
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// 로그 메시지
        /// </summary>
        public event EventHandler<string> LogMessage;

        #endregion

        #region 필드

        private readonly TempController _tempController;
        private ThermalRampProfile _currentProfile;
        private System.Timers.Timer _controlTimer;
        private readonly object _lockObject = new object();

        // 제어 상태
        private RampState _state = RampState.Idle;
        private double _targetSampleTemp;
        private double _rampRate;
        private double _initialSampleTemp;
        private DateTime _rampStartTime;
        private DateTime _stabilizationStartTime;
        private bool _targetReachedFired = false;
        
        // 완료 후 Hold 모드 유지 옵션
        private bool _holdAfterComplete = true;

        // PID 상태
        private double _integral;
        private double _previousError;
        private DateTime _lastControlTime;
        
        // 통신 충돌 방지용 - 설정 요청 큐
        private double? _pendingSetpoint = null;
        private readonly object _setpointLock = new object();
        private DateTime _lastSetpointTime = DateTime.MinValue;
        private const int MinSetpointIntervalMs = 300; // 최소 300ms 간격으로 설정

        // 데이터 버퍼 (이동 평균용)
        private readonly Queue<double> _sampleTempBuffer = new Queue<double>();
        private readonly Queue<double> _heaterTempBuffer = new Queue<double>();
        private const int BufferSize = 10;

        #endregion

        #region 속성

        /// <summary>
        /// 현재 상태
        /// </summary>
        public RampState State => _state;

        /// <summary>
        /// 실행 중 여부
        /// </summary>
        public bool IsRunning => _state == RampState.Ramping || 
                                  _state == RampState.Stabilizing || 
                                  _state == RampState.Holding;

        /// <summary>
        /// 현재 사용 중인 프로파일
        /// </summary>
        public ThermalRampProfile CurrentProfile => _currentProfile;

        /// <summary>
        /// 목표 샘플 온도
        /// </summary>
        public double TargetTemperature => _targetSampleTemp;

        /// <summary>
        /// 설정된 램프 속도
        /// </summary>
        public double RampRate => _rampRate;

        /// <summary>
        /// 샘플 온도 채널 번호 (기본: 2)
        /// </summary>
        public int SampleChannel { get; set; } = 2;

        /// <summary>
        /// 히터 채널 번호 (기본: 1)
        /// </summary>
        public int HeaterChannel { get; set; } = 1;

        /// <summary>
        /// 추가 모니터링 채널 목록 (평균 계산에 포함)
        /// </summary>
        public List<int> AdditionalSampleChannels { get; set; } = new List<int>();

        /// <summary>
        /// 램프 완료 후 Hold 모드 유지 여부
        /// true: 목표 도달 후에도 온도 유지 제어 계속
        /// false: 목표 도달 후 제어 정지
        /// </summary>
        public bool HoldAfterComplete
        {
            get => _holdAfterComplete;
            set => _holdAfterComplete = value;
        }

        #endregion

        #region 생성자

        public SimpleThermalRampController(TempController tempController)
        {
            _tempController = tempController ?? throw new ArgumentNullException(nameof(tempController));
            
            _controlTimer = new System.Timers.Timer(1000); // 1초 주기 (통신 충돌 방지)
            _controlTimer.Elapsed += ControlTimer_Elapsed;
            _controlTimer.AutoReset = true;
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 램프 시작
        /// </summary>
        /// <param name="profile">사용할 프로파일</param>
        /// <param name="targetTemp">목표 샘플 온도 (°C)</param>
        /// <param name="rampRate">승온 속도 (°C/min)</param>
        public async Task<bool> StartRampAsync(ThermalRampProfile profile, double targetTemp, double rampRate)
        {
            if (IsRunning)
            {
                OnError("이미 실행 중입니다. 먼저 정지하세요.");
                return false;
            }

            // 파라미터 검증
            if (profile == null)
            {
                OnError("프로파일이 선택되지 않았습니다.");
                return false;
            }

            if (rampRate > profile.MaxRampRate)
            {
                OnError($"램프 속도가 최대값({profile.MaxRampRate}°C/min)을 초과합니다.");
                return false;
            }

            if (targetTemp > profile.MaxHeaterTemperature)
            {
                OnError($"목표 온도가 히터 최대 온도({profile.MaxHeaterTemperature}°C)를 초과합니다.");
                return false;
            }

            try
            {
                _currentProfile = profile;
                _targetSampleTemp = targetTemp;
                _rampRate = rampRate;

                // 현재 온도 읽기
                _initialSampleTemp = GetSampleTemperature();
                
                OnLog($"램프 시작: {_initialSampleTemp:F1}°C → {targetTemp:F1}°C @ {rampRate:F1}°C/min");
                OnLog($"프로파일: {profile.Name}");

                // 초기화
                ResetControlState();
                _rampStartTime = DateTime.Now;
                _targetReachedFired = false;

                // 초기 히터 온도 설정
                double initialHeaterSetpoint = CalculateInitialHeaterSetpoint();
                SetHeaterTemperature(initialHeaterSetpoint);

                // 히터 시작
                _tempController.Start(HeaterChannel);
                await Task.Delay(100);

                // 제어 시작
                _state = RampState.Ramping;
                _controlTimer.Start();

                return true;
            }
            catch (Exception ex)
            {
                OnError($"램프 시작 실패: {ex.Message}");
                _state = RampState.Error;
                return false;
            }
        }

        /// <summary>
        /// 정지 (히터 유지)
        /// </summary>
        public void Stop()
        {
            _controlTimer.Stop();
            _state = RampState.Stopped;
            OnLog("램프 정지됨 (히터 상태 유지)");
        }

        /// <summary>
        /// 비상 정지 (히터 OFF)
        /// </summary>
        public void EmergencyStop()
        {
            _controlTimer.Stop();
            _state = RampState.Stopped;
            
            try
            {
                _tempController.Stop(HeaterChannel);
            }
            catch { }

            OnLog("!!! 비상 정지 - 히터 OFF !!!");
            OnError("비상 정지가 실행되었습니다.");
        }

        /// <summary>
        /// 현재 온도 유지 모드로 전환
        /// </summary>
        public void HoldCurrentTemperature()
        {
            if (_state == RampState.Ramping || _state == RampState.Stabilizing)
            {
                _state = RampState.Holding;
                OnLog($"현재 온도 유지 모드: {GetSampleTemperature():F1}°C");
            }
        }

        /// <summary>
        /// 종료 동작에 따라 정지
        /// </summary>
        /// <param name="endAction">종료 동작 타입</param>
        public void StopWithAction(BakeoutEndAction endAction)
        {
            switch (endAction)
            {
                case BakeoutEndAction.HeaterOff:
                    // 히터 OFF
                    EmergencyStop();
                    OnLog("타이머 종료 - 히터 OFF");
                    break;

                case BakeoutEndAction.MaintainTemperature:
                    // Hold 모드 유지 (타이머는 종료되지만 온도 제어는 계속)
                    // 이미 Holding 상태면 유지, 아니면 Holding으로 전환
                    if (_state != RampState.Holding)
                    {
                        _state = RampState.Holding;
                        if (!_controlTimer.Enabled)
                        {
                            _controlTimer.Start();
                        }
                    }
                    OnLog("타이머 종료 - 목표 온도 유지 중");
                    break;

                case BakeoutEndAction.NotifyOnly:
                    // 제어 정지 (알림만)
                    Stop();
                    OnLog("타이머 종료 - 수동 조작 필요");
                    break;
            }
        }

        #endregion

        #region 제어 루프

        private void ControlTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                try
                {
                    ExecuteControlCycle();
                }
                catch (Exception ex)
                {
                    OnError($"제어 오류: {ex.Message}");
                }
            }
        }

        private void ExecuteControlCycle()
        {
            // 현재 온도 읽기
            double sampleTemp = GetSampleTemperature();
            double heaterTemp = GetHeaterTemperature();
            double heaterSetpoint = GetHeaterSetpoint();

            // 버퍼에 추가 (이동 평균용)
            UpdateBuffers(sampleTemp, heaterTemp);

            // 안전 체크
            if (!CheckSafety(heaterTemp, sampleTemp))
            {
                return;
            }

            // 상태에 따른 제어
            switch (_state)
            {
                case RampState.Ramping:
                    ExecuteRampControl(sampleTemp, heaterTemp);
                    break;

                case RampState.Stabilizing:
                    ExecuteStabilizationCheck(sampleTemp);
                    break;

                case RampState.Holding:
                    ExecuteHoldControl(sampleTemp, heaterTemp);
                    break;
            }

            // 진행 상태 업데이트
            UpdateProgress(sampleTemp, heaterTemp, heaterSetpoint);
        }

        private void ExecuteRampControl(double sampleTemp, double heaterTemp)
        {
            double elapsedMinutes = (DateTime.Now - _rampStartTime).TotalMinutes;
            
            // 예상 샘플 온도 (목표 궤적)
            double expectedSampleTemp = _initialSampleTemp + (_rampRate * elapsedMinutes);
            expectedSampleTemp = Math.Min(expectedSampleTemp, _targetSampleTemp);

            // 오차 계산
            double error = expectedSampleTemp - sampleTemp;

            // 히터 조정량 계산
            double adjustment = CalculateHeaterAdjustment(error, sampleTemp, heaterTemp);

            // 새 히터 설정값
            double currentSetpoint = GetHeaterSetpoint();
            double newSetpoint = currentSetpoint + adjustment;

            // 안전 제한 적용
            newSetpoint = ApplySafetyLimits(newSetpoint, sampleTemp);

            // 히터 온도 설정
            SetHeaterTemperature(newSetpoint);

            // 목표 근처 도달 체크
            if (Math.Abs(sampleTemp - _targetSampleTemp) <= _currentProfile.TemperatureStabilityRange)
            {
                _state = RampState.Stabilizing;
                _stabilizationStartTime = DateTime.Now;
                OnLog($"목표 온도 근처 도달, 안정화 시작: {sampleTemp:F1}°C");
            }
        }

        private void ExecuteStabilizationCheck(double sampleTemp)
        {
            bool inRange = Math.Abs(sampleTemp - _targetSampleTemp) <= _currentProfile.TemperatureStabilityRange;

            if (!inRange)
            {
                // 범위를 벗어나면 다시 램프 모드로
                _state = RampState.Ramping;
                OnLog($"온도 범위 이탈, 램프 재개: {sampleTemp:F1}°C");
                return;
            }

            double stabilizingSeconds = (DateTime.Now - _stabilizationStartTime).TotalSeconds;
            
            if (stabilizingSeconds >= _currentProfile.StabilizationTime)
            {
                OnLog($"안정화 완료: {sampleTemp:F1}°C (목표: {_targetSampleTemp:F1}°C)");

                // 목표 도달 이벤트 (타이머 시작용)
                if (!_targetReachedFired)
                {
                    _targetReachedFired = true;
                    TargetReached?.Invoke(this, EventArgs.Empty);
                }

                // Hold 모드 유지 여부에 따라 처리
                if (_holdAfterComplete)
                {
                    // Hold 모드로 전환 (온도 유지 제어 계속)
                    _state = RampState.Holding;
                    OnLog("Hold 모드 전환 - 목표 온도 유지 제어 중");
                    // _controlTimer는 계속 실행
                }
                else
                {
                    // 제어 정지
                    _state = RampState.Completed;
                    _controlTimer.Stop();
                    OnLog("램프 완료 - 제어 정지");
                }

                // 완료 이벤트
                RampCompleted?.Invoke(this, new RampCompletedEventArgs
                {
                    Success = true,
                    Message = _holdAfterComplete ? "목표 온도 도달 - Hold 모드 유지 중" : "목표 온도 도달 및 안정화 완료",
                    TotalTime = DateTime.Now - _rampStartTime,
                    FinalSampleTemp = sampleTemp
                });
            }
        }

        private void ExecuteHoldControl(double sampleTemp, double heaterTemp)
        {
            double currentSetpoint = GetHeaterSetpoint();
            double newSetpoint;

            // 샘플 온도가 목표를 초과한 경우
            if (sampleTemp >= _targetSampleTemp)
            {
                // 히터를 목표보다 낮게 설정
                newSetpoint = _targetSampleTemp - 2;
            }
            else
            {
                // 현재 샘플 온도를 목표로 유지
                double error = _targetSampleTemp - sampleTemp;
                double adjustment = CalculateHeaterAdjustment(error, sampleTemp, heaterTemp) * 0.3; // 매우 약하게

                newSetpoint = currentSetpoint + adjustment;
            }

            newSetpoint = ApplySafetyLimits(newSetpoint, sampleTemp);
            SetHeaterTemperature(newSetpoint);
        }

        #endregion

        #region 히터 조정 계산

        private double CalculateInitialHeaterSetpoint()
        {
            double sampleTemp = GetSampleTemperature();
            double distanceToTarget = _targetSampleTemp - sampleTemp;
            
            // 기본 설정: 현재 샘플 온도 + 초기 오프셋
            double offset = _currentProfile.MaxHeaterSampleGap * 0.5;
            
            if (_currentProfile.UseInitialBoost && distanceToTarget > 20)
            {
                // 초기 부스트: 목표까지 충분히 떨어져 있을 때만 적용
                offset += distanceToTarget * 0.1 * _currentProfile.ThermalCompensationFactor;
            }

            double setpoint = sampleTemp + offset;

            // === 오버슈트 방지: 초기 설정도 목표 온도 기준 제한 ===
            if (distanceToTarget <= 20)
            {
                // 목표까지 20°C 이내면 목표 + 5°C 이하로 제한
                setpoint = Math.Min(setpoint, _targetSampleTemp + 5);
            }
            if (distanceToTarget <= 10)
            {
                // 목표까지 10°C 이내면 목표 + 3°C 이하로 제한
                setpoint = Math.Min(setpoint, _targetSampleTemp + 3);
            }
            if (distanceToTarget <= 5)
            {
                // 목표까지 5°C 이내면 목표 이하로 제한
                setpoint = Math.Min(setpoint, _targetSampleTemp);
            }

            // 최대 히터 온도 제한
            setpoint = Math.Min(setpoint, _currentProfile.MaxHeaterTemperature);

            return setpoint;
        }

        private double CalculateHeaterAdjustment(double error, double sampleTemp, double heaterTemp)
        {
            DateTime now = DateTime.Now;
            double dt = (now - _lastControlTime).TotalSeconds;
            if (dt <= 0) dt = 0.5;

            // 적응형 PID 게인
            double kp = _currentProfile.InternalKp;
            double ki = _currentProfile.InternalKi;
            double kd = _currentProfile.InternalKd;

            // 열 지연 보상
            kp *= _currentProfile.ThermalCompensationFactor;

            // P항
            double proportional = kp * error;

            // I항 (안티 와인드업)
            _integral += error * dt;
            _integral = Math.Max(-50, Math.Min(50, _integral));
            double integral = ki * _integral;

            // D항
            double derivative = 0;
            if (dt > 0 && _previousError != 0)
            {
                derivative = kd * (error - _previousError) / dt;
            }

            double pidOutput = proportional + integral + derivative;

            // === 오버슈트 방지를 위한 보수적 접근 ===
            double distanceToTarget = _targetSampleTemp - sampleTemp;
            
            if (distanceToTarget <= 0)
            {
                // 이미 목표 도달 또는 초과 → 히터 올리지 않음
                pidOutput = Math.Min(pidOutput, 0);
                _integral = Math.Min(_integral, 0); // 적분항도 리셋
            }
            else if (distanceToTarget <= 2)
            {
                // 2°C 이내 → 매우 보수적 (5% 출력)
                pidOutput *= 0.05;
                _integral *= 0.5;
            }
            else if (distanceToTarget <= 5)
            {
                // 5°C 이내 → 보수적 (15% 출력)
                pidOutput *= 0.15;
            }
            else if (distanceToTarget <= 10)
            {
                // 10°C 이내 → 감속 (30% 출력)
                pidOutput *= 0.3;
            }
            else if (distanceToTarget <= 20)
            {
                // 20°C 이내 → 약한 감속 (60% 출력)
                pidOutput *= 0.6;
            }
            // 20°C 이상은 정상 출력

            // 변화율 제한
            double maxAdjust = 2.0; // 사이클당 최대 2°C (3에서 감소)
            pidOutput = Math.Max(-maxAdjust, Math.Min(maxAdjust, pidOutput));

            _previousError = error;
            _lastControlTime = now;

            return pidOutput;
        }

        private double ApplySafetyLimits(double heaterSetpoint, double sampleTemp)
        {
            // === 1. 오버슈트 방지 (최우선) ===
            // 진공 환경에서는 냉각이 어려우므로 절대 목표 온도를 넘지 않도록 함
            double distanceToTarget = _targetSampleTemp - sampleTemp;
            
            if (distanceToTarget <= 0)
            {
                // 샘플이 이미 목표 온도 이상 → 히터를 목표보다 낮게 설정
                heaterSetpoint = Math.Min(heaterSetpoint, _targetSampleTemp - 2);
            }
            else if (distanceToTarget <= 2)
            {
                // 목표까지 2°C 이내 → 히터를 목표 - 2°C 이하로 제한
                heaterSetpoint = Math.Min(heaterSetpoint, _targetSampleTemp - 2);
            }
            else if (distanceToTarget <= 5)
            {
                // 목표까지 5°C 이내 → 히터를 목표 이하로 제한
                heaterSetpoint = Math.Min(heaterSetpoint, _targetSampleTemp);
            }
            else if (distanceToTarget <= 10)
            {
                // 목표까지 10°C 이내 → 히터를 목표 + 3°C 이하로 제한
                heaterSetpoint = Math.Min(heaterSetpoint, _targetSampleTemp + 3);
            }
            else if (distanceToTarget <= 20)
            {
                // 목표까지 20°C 이내 → 히터를 목표 + 5°C 이하로 제한
                heaterSetpoint = Math.Min(heaterSetpoint, _targetSampleTemp + 5);
            }
            // 20°C 이상 떨어져 있으면 MaxHeaterSampleGap 제한만 적용

            // === 2. 최대 히터 온도 제한 ===
            heaterSetpoint = Math.Min(heaterSetpoint, _currentProfile.MaxHeaterTemperature);

            // === 3. 최대 온도차 제한 ===
            if (heaterSetpoint - sampleTemp > _currentProfile.MaxHeaterSampleGap)
            {
                heaterSetpoint = sampleTemp + _currentProfile.MaxHeaterSampleGap;
            }

            // === 4. 최소값 제한 ===
            heaterSetpoint = Math.Max(heaterSetpoint, sampleTemp);

            return heaterSetpoint;
        }

        #endregion

        #region 안전 체크

        private bool CheckSafety(double heaterTemp, double sampleTemp)
        {
            double gap = heaterTemp - sampleTemp;

            // === 샘플 온도 오버슈트 체크 (최우선) ===
            double sampleOvershoot = sampleTemp - _targetSampleTemp;
            
            if (sampleOvershoot > 5)
            {
                // 5°C 이상 초과 시 비상정지
                EmergencyStop();
                OnError($"비상정지: 샘플 온도 {sampleTemp:F1}°C가 목표({_targetSampleTemp:F1}°C)를 5°C 이상 초과");
                return false;
            }
            else if (sampleOvershoot > 2)
            {
                // 2°C 초과 시 경고
                OnLog($"경고: 샘플 온도 {sampleTemp:F1}°C가 목표({_targetSampleTemp:F1}°C)를 초과");
            }

            // 비상정지 온도차 초과
            if (gap > _currentProfile.EmergencyStopGap)
            {
                EmergencyStop();
                OnError($"비상정지: 온도차 {gap:F1}°C가 제한({_currentProfile.EmergencyStopGap}°C)을 초과");
                return false;
            }

            // 히터 최대 온도 초과
            if (heaterTemp > _currentProfile.MaxHeaterTemperature + 10)
            {
                EmergencyStop();
                OnError($"비상정지: 히터 온도 {heaterTemp:F1}°C가 최대({_currentProfile.MaxHeaterTemperature}°C)를 초과");
                return false;
            }

            return true;
        }

        #endregion

        #region 온도 읽기/쓰기

        private double GetSampleTemperature()
        {
            var temps = new List<double>();

            // 메인 샘플 채널
            temps.Add(GetChannelTemperature(SampleChannel));

            // 추가 채널들
            foreach (int ch in AdditionalSampleChannels)
            {
                temps.Add(GetChannelTemperature(ch));
            }

            return temps.Average();
        }

        private double GetHeaterTemperature()
        {
            return GetChannelTemperature(HeaterChannel);
        }

        private double GetChannelTemperature(int channel)
        {
            if (channel < 1 || channel > _tempController.Status.ChannelStatus.Length)
                return 25.0;

            var status = _tempController.Status.ChannelStatus[channel - 1];
            return status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        private double GetHeaterSetpoint()
        {
            var status = _tempController.Status.ChannelStatus[HeaterChannel - 1];
            return status.SetValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        private void SetHeaterTemperature(double temperature)
        {
            // 최소 간격 체크 (통신 충돌 방지)
            lock (_setpointLock)
            {
                _pendingSetpoint = temperature;
            }
        }
        
        /// <summary>
        /// 대기 중인 온도 설정값을 실제로 적용 (MainForm에서 호출)
        /// </summary>
        /// <returns>설정값이 적용되었으면 true</returns>
        public bool ApplyPendingSetpoint()
        {
            double? setpoint;
            lock (_setpointLock)
            {
                setpoint = _pendingSetpoint;
                if (setpoint == null) return false;
                
                // 최소 간격 체크
                if ((DateTime.Now - _lastSetpointTime).TotalMilliseconds < MinSetpointIntervalMs)
                    return false;
                    
                _pendingSetpoint = null;
                _lastSetpointTime = DateTime.Now;
            }
            
            try
            {
                var status = _tempController.Status.ChannelStatus[HeaterChannel - 1];
                short setValue = (short)(setpoint.Value * (status.Dot == 0 ? 1 : 10));
                _tempController.SetTemperature(HeaterChannel, setValue);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 대기 중인 설정값이 있는지 확인
        /// </summary>
        public bool HasPendingSetpoint
        {
            get
            {
                lock (_setpointLock)
                {
                    return _pendingSetpoint.HasValue;
                }
            }
        }

        #endregion

        #region 헬퍼 메서드

        private void ResetControlState()
        {
            _integral = 0;
            _previousError = 0;
            _lastControlTime = DateTime.Now;
            _sampleTempBuffer.Clear();
            _heaterTempBuffer.Clear();
        }

        private void UpdateBuffers(double sampleTemp, double heaterTemp)
        {
            _sampleTempBuffer.Enqueue(sampleTemp);
            _heaterTempBuffer.Enqueue(heaterTemp);

            while (_sampleTempBuffer.Count > BufferSize)
                _sampleTempBuffer.Dequeue();
            while (_heaterTempBuffer.Count > BufferSize)
                _heaterTempBuffer.Dequeue();
        }

        private void UpdateProgress(double sampleTemp, double heaterTemp, double heaterSetpoint)
        {
            double progress = 0;
            if (_targetSampleTemp > _initialSampleTemp)
            {
                progress = (sampleTemp - _initialSampleTemp) / (_targetSampleTemp - _initialSampleTemp) * 100;
                progress = Math.Max(0, Math.Min(100, progress));
            }

            string statusMsg = _state switch
            {
                RampState.Ramping => $"램프 진행 중 ({sampleTemp:F1}°C → {_targetSampleTemp:F1}°C)",
                RampState.Stabilizing => $"안정화 중 ({sampleTemp:F1}°C)",
                RampState.Holding => $"온도 유지 중 ({sampleTemp:F1}°C)",
                RampState.Completed => "완료",
                _ => _state.ToString()
            };

            ProgressUpdated?.Invoke(this, new RampProgressEventArgs
            {
                State = _state,
                HeaterTemp = heaterTemp,
                SampleTemp = sampleTemp,
                TargetTemp = _targetSampleTemp,
                HeaterSetpoint = heaterSetpoint,
                ProgressPercent = progress,
                StatusMessage = statusMsg,
                ElapsedTime = DateTime.Now - _rampStartTime
            });
        }

        private void OnLog(string message)
        {
            LogMessage?.Invoke(this, $"[ThermalRamp] {message}");
        }

        private void OnError(string message)
        {
            ErrorOccurred?.Invoke(this, message);
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
