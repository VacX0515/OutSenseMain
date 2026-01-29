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
            Learning,       // 열 특성 학습 중
            Ramping,        // 램프 진행 중
            Approaching,    // 목표 접근 중 (평형점 탐색)
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

        // === 학습 및 예측 기반 제어 ===
        // 열 전달 계수: 샘플 온도 변화율 / (히터온도 - 샘플온도)
        private double _thermalTransferCoeff = 0;  // K 값 (학습됨)
        private bool _coefficientLearned = false;

        // 학습 데이터
        private DateTime _learningStartTime;
        private double _learningStartSampleTemp;
        private double _learningStartHeaterTemp;
        private const double LearningDurationSeconds = 30;  // 학습 시간
        private const double MinLearningTempRise = 0.5;     // 최소 학습 온도 상승

        // 승온 속도 모니터링
        private double _lastSampleTemp;
        private DateTime _lastRateCheckTime;
        private double _currentRampRate;  // 현재 실제 승온 속도 (°C/min)

        // 평형점 탐색
        private double _equilibriumHeaterTemp = 0;  // 발견된 평형 히터 온도
        private int _approachAdjustCount = 0;       // 접근 중 조정 횟수

        #endregion

        #region 속성

        /// <summary>
        /// 현재 상태
        /// </summary>
        public RampState State => _state;

        /// <summary>
        /// 실행 중 여부
        /// </summary>
        public bool IsRunning => _state == RampState.Learning ||
                                  _state == RampState.Ramping ||
                                  _state == RampState.Approaching ||
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

        /// <summary>
        /// 학습된 열 전달 계수 (K)
        /// </summary>
        public double ThermalTransferCoefficient => _thermalTransferCoeff;

        /// <summary>
        /// 열 전달 계수 학습 완료 여부
        /// </summary>
        public bool IsCoefficientLearned => _coefficientLearned;

        /// <summary>
        /// 발견된 평형 히터 온도
        /// </summary>
        public double EquilibriumHeaterTemp => _equilibriumHeaterTemp;

        /// <summary>
        /// 현재 실제 승온 속도 (°C/min)
        /// </summary>
        public double CurrentRampRate => _currentRampRate;

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

                // 학습 상태 초기화
                _coefficientLearned = false;
                _thermalTransferCoeff = 0;
                _equilibriumHeaterTemp = 0;
                _approachAdjustCount = 0;
                _lastSampleTemp = _initialSampleTemp;
                _lastRateCheckTime = DateTime.Now;
                _currentRampRate = 0;

                // 목표까지 거리 확인
                double distanceToTarget = targetTemp - _initialSampleTemp;

                if (distanceToTarget <= 3)
                {
                    // 이미 목표 근처 → 바로 Approaching으로
                    OnLog("이미 목표 근처, 평형점 탐색 시작");
                    double initialSetpoint = _initialSampleTemp + 2;
                    SetHeaterTemperature(initialSetpoint);
                    _tempController.Start(HeaterChannel);
                    await Task.Delay(100);
                    _state = RampState.Approaching;
                }
                else
                {
                    // Learning 단계 시작
                    OnLog("열 특성 학습 시작...");
                    _learningStartTime = DateTime.Now;
                    _learningStartSampleTemp = _initialSampleTemp;

                    // 안전한 초기 히터 온도: 현재 + 10°C (단, 목표 초과 금지)
                    double safeInitialHeater = Math.Min(_initialSampleTemp + 10, targetTemp + 5);
                    safeInitialHeater = Math.Min(safeInitialHeater, profile.MaxHeaterTemperature);
                    _learningStartHeaterTemp = safeInitialHeater;

                    // 수정:
                    // ★ Start 전에 직접 SV 설정
                    ApplySetpointDirect(safeInitialHeater);

                    _tempController.Start(HeaterChannel);
                    await Task.Delay(200);
                    _state = RampState.Learning;

                    // ★ Start 후에도 다시 설정 (TM4가 내부값으로 복원했을 수 있음)
                    ApplySetpointDirect(safeInitialHeater);

                }

                // 제어 시작
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
        /// 히터 설정 즉시 적용 (긴급 시)
        /// </summary>
        private void SetHeaterTemperatureImmediate(double temperature)
        {
            try
            {
                var status = _tempController.Status.ChannelStatus[HeaterChannel - 1];
                short setValue = (short)(temperature * (status.Dot == 0 ? 1 : 10));
                _tempController.SetTemperature(HeaterChannel, setValue);
            }
            catch { }
        }


        /// <summary>
        /// 설정값 직접 적용 (pendingSetpoint 무시)
        /// </summary>
        private void ApplySetpointDirect(double temperature)
        {
            try
            {
                var status = _tempController.Status.ChannelStatus[HeaterChannel - 1];
                short setValue = (short)(temperature * (status.Dot == 0 ? 1 : 10));
                _tempController.SetTemperature(HeaterChannel, setValue);
                OnLog($"히터 SV 직접 설정: {temperature:F1}°C");
            }
            catch (Exception ex)
            {
                OnLog($"SV 설정 실패: {ex.Message}");
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

            // 승온 속도 계산
            UpdateRampRate(sampleTemp);

            // 안전 체크 (최우선)
            if (!CheckSafety(heaterTemp, sampleTemp))
            {
                return;
            }

            // 상태에 따른 제어
            switch (_state)
            {
                case RampState.Learning:
                    ExecuteLearningPhase(sampleTemp, heaterTemp);
                    break;

                case RampState.Ramping:
                    ExecuteAdaptiveRampControl(sampleTemp, heaterTemp);
                    break;

                case RampState.Approaching:
                    ExecuteApproachControl(sampleTemp, heaterTemp);
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

        /// <summary>
        /// 승온 속도 계산 (이동 평균)
        /// </summary>
        private void UpdateRampRate(double sampleTemp)
        {
            double elapsed = (DateTime.Now - _lastRateCheckTime).TotalMinutes;
            if (elapsed >= 0.1)  // 6초마다 계산
            {
                _currentRampRate = (sampleTemp - _lastSampleTemp) / elapsed;
                _lastSampleTemp = sampleTemp;
                _lastRateCheckTime = DateTime.Now;
            }
        }

        /// <summary>
        /// 히터 설정값에 모든 제한 적용
        /// </summary>
        private double ApplyHeaterLimits(double setpoint, double sampleTemp)
        {
            // 1. MaxHeaterSampleGap 제한
            setpoint = Math.Min(setpoint, sampleTemp + _currentProfile.MaxHeaterSampleGap);

            // 2. MaxHeaterTemperature 제한
            setpoint = Math.Min(setpoint, _currentProfile.MaxHeaterTemperature);

            // 3. UseTargetSlowdown 적용 - 목표 근접 시 추가 제한
            if (_currentProfile.UseTargetSlowdown)
            {
                double distanceToTarget = _targetSampleTemp - sampleTemp;

                // HeatTransferDelay 기반 선제 감속 거리 계산
                // 지연이 클수록 더 일찍 감속 시작
                double slowdownDistance = Math.Max(3, _currentProfile.HeatTransferDelay / 10.0);

                if (distanceToTarget <= 0)
                    setpoint = Math.Min(setpoint, _targetSampleTemp - 3);
                else if (distanceToTarget <= 1)
                    setpoint = Math.Min(setpoint, _targetSampleTemp - 2);
                else if (distanceToTarget <= slowdownDistance)
                    setpoint = Math.Min(setpoint, _targetSampleTemp);
                else if (distanceToTarget <= slowdownDistance * 2)
                    setpoint = Math.Min(setpoint, _targetSampleTemp + 3);
            }

            // 4. 최소값 (샘플보다 너무 낮지 않게)
            setpoint = Math.Max(setpoint, sampleTemp - 5);

            return setpoint;
        }

        /// <summary>
        /// 학습 단계: 열 전달 계수 측정
        /// </summary>
        private void ExecuteLearningPhase(double sampleTemp, double heaterTemp)
        {
            double elapsedSeconds = (DateTime.Now - _learningStartTime).TotalSeconds;
            double tempRise = sampleTemp - _learningStartSampleTemp;

            // ★ 추가: 학습 중에도 MaxHeaterSampleGap 적용
            double currentSetpoint = GetHeaterSetpoint();
            double limitedSetpoint = ApplyHeaterLimits(currentSetpoint, sampleTemp);
            if (Math.Abs(currentSetpoint - limitedSetpoint) > 0.5)
            {
                SetHeaterTemperature(limitedSetpoint);
            }

            // 학습 완료 조건 (기존 코드 유지)
            if (elapsedSeconds >= LearningDurationSeconds || tempRise >= MinLearningTempRise * 2)
            {
                // ... 기존 코드 ...
            }
        }
        /// <summary>
        /// 적응형 램프 제어 (학습된 K 사용)
        /// </summary>
        private void ExecuteAdaptiveRampControl(double sampleTemp, double heaterTemp)
        {
            double distanceToTarget = _targetSampleTemp - sampleTemp;

            // 목표 근접 시 Approaching 모드로 전환
            // ★ 수정: HeatTransferDelay 기반 전환 거리
            double approachDistance = Math.Max(3, _currentProfile.HeatTransferDelay / 20.0);
            if (distanceToTarget <= approachDistance)
            {
                _state = RampState.Approaching;
                _approachAdjustCount = 0;
                OnLog($"목표 접근, 평형점 탐색 시작: {sampleTemp:F1}°C");
                return;
            }

            // ★ 추가: MaxRampRate 실시간 제한
            if (_currentRampRate > _currentProfile.MaxRampRate * 1.1)
            {
                double currentSetpoint = GetHeaterSetpoint();
                double newSetpoint = currentSetpoint - 3;
                newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);
                SetHeaterTemperature(newSetpoint);
                OnLog($"승온 속도 초과 ({_currentRampRate:F2} > {_currentProfile.MaxRampRate}), 히터 감소");
                return;
            }

            // 승온 속도 체크 (기존)
            if (_currentRampRate > _rampRate * 1.2)
            {
                double currentSetpoint = GetHeaterSetpoint();
                double newSetpoint = currentSetpoint - 2;
                newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);
                SetHeaterTemperature(newSetpoint);
                OnLog($"목표 속도 초과 ({_currentRampRate:F2}°C/min), 히터 감소");
                return;
            }

            // 예측 기반 히터 설정 (기존)
            double targetRampRate = _rampRate;
            double requiredTempDiff = _thermalTransferCoeff > 0
                ? targetRampRate / _thermalTransferCoeff
                : 20;

            double targetHeaterTemp = sampleTemp + (requiredTempDiff * 0.8);

            // ★ 수정: 공통 제한 함수 사용
            targetHeaterTemp = ApplyHeaterLimits(targetHeaterTemp, sampleTemp);

            // 점진적 변경 (기존)
            double currentSetpoint2 = GetHeaterSetpoint();
            double maxChange = 3.0;
            double newSetpoint2 = currentSetpoint2;

            if (targetHeaterTemp > currentSetpoint2)
                newSetpoint2 = Math.Min(currentSetpoint2 + maxChange, targetHeaterTemp);
            else if (targetHeaterTemp < currentSetpoint2)
                newSetpoint2 = Math.Max(currentSetpoint2 - maxChange, targetHeaterTemp);

            SetHeaterTemperature(newSetpoint2);
        }
        private void ExecuteApproachControl(double sampleTemp, double heaterTemp)
        {
            double error = _targetSampleTemp - sampleTemp;
            double currentSetpoint = GetHeaterSetpoint();
            double newSetpoint;

            // ★ 1. 이미 초과 → 즉시 히터 낮춤
            if (error < -0.3)
            {
                newSetpoint = _targetSampleTemp - 3;
                newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);
                SetHeaterTemperatureImmediate(newSetpoint);
                OnLog($"⚠️ 샘플 초과 ({sampleTemp:F1}°C), 히터 즉시 → {newSetpoint:F1}°C");
                return;
            }

            // ★ 2. UseTargetSlowdown 적용 - 선제 감속
            if (_currentProfile.UseTargetSlowdown)
            {
                if (error < 0.5)
                {
                    newSetpoint = _targetSampleTemp - 2;
                    newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);
                    SetHeaterTemperatureImmediate(newSetpoint);
                    return;
                }
                if (error < 1.0)
                {
                    newSetpoint = _targetSampleTemp - 1;
                    newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);
                    SetHeaterTemperature(newSetpoint);
                    return;
                }
            }

            // 3. 안정 범위 도달 → Stabilizing 전환
            if (Math.Abs(error) <= _currentProfile.TemperatureStabilityRange)
            {
                _equilibriumHeaterTemp = currentSetpoint;
                _state = RampState.Stabilizing;
                _stabilizationStartTime = DateTime.Now;
                OnLog($"목표 도달, 평형 히터: {_equilibriumHeaterTemp:F1}°C");
                return;
            }

            // 4. 그 외 → 미세 증가
            double adjustment = error * 0.2;
            adjustment = Math.Max(0.2, Math.Min(1.0, adjustment));
            newSetpoint = currentSetpoint + adjustment;

            // ★ 공통 제한 적용
            newSetpoint = ApplyHeaterLimits(newSetpoint, sampleTemp);

            SetHeaterTemperature(newSetpoint);
            _approachAdjustCount++;
        }

        /// <summary>
        /// Hold 모드만 시작 (램프 건너뛰고 바로 유지)
        /// </summary>
        public async Task<bool> StartHoldOnlyAsync(ThermalRampProfile profile, double targetTemp)
        {
            if (IsRunning)
            {
                OnError("이미 실행 중입니다.");
                return false;
            }

            try
            {
                _currentProfile = profile;
                _targetSampleTemp = targetTemp;
                _rampStartTime = DateTime.Now;

                // 현재 온도 읽기
                double currentSampleTemp = GetSampleTemperature();
                _initialSampleTemp = currentSampleTemp;

                // 현재 히터 설정값을 평형점으로 가정
                _equilibriumHeaterTemp = GetHeaterSetpoint();

                OnLog($"Hold 모드 시작: 목표 {targetTemp:F1}°C, 현재 {currentSampleTemp:F1}°C");

                // 5분 관찰 초기화
                _holdCheckStartTime = DateTime.Now;
                _holdCheckStartTemp = currentSampleTemp;

                // 바로 Holding 상태로
                _state = RampState.Holding;

                // 제어 타이머 시작
                _controlTimer.Start();

                return true;
            }
            catch (Exception ex)
            {
                OnError($"Hold 모드 시작 실패: {ex.Message}");
                _state = RampState.Error;
                return false;
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
                    // ★ 5분 관찰 초기화
                    _holdCheckStartTime = DateTime.Now;
                    _holdCheckStartTemp = sampleTemp;

                    OnLog("Hold 모드 전환 - 5분 주기 관찰 시작");
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

        // Hold 모드 - 5분 주기 관찰용
        private DateTime _holdCheckStartTime;
        private double _holdCheckStartTemp;
        private const double HoldCheckIntervalSeconds = 300;  // 5분
        private void ExecuteHoldControl(double sampleTemp, double heaterTemp)
        {
            double error = _targetSampleTemp - sampleTemp;
            double currentSetpoint = GetHeaterSetpoint();

            // ★ 1. 즉시 대응: CH2가 목표 초과 (0.3°C 이상) → 바로 감소
            if (error < -0.3)
            {
                double decrease = Math.Abs(error) * 2;
                decrease = Math.Max(0.5, Math.Min(5, decrease));

                double newSetpoint = currentSetpoint - decrease;
                newSetpoint = Math.Max(newSetpoint, _targetSampleTemp - 5);

                SetHeaterTemperatureImmediate(newSetpoint);
                OnLog($"Hold: CH2 초과({sampleTemp:F1}°C), CH1 SV 즉시 감소 → {newSetpoint:F1}°C");

                // 관찰 리셋
                _holdCheckStartTime = DateTime.Now;
                _holdCheckStartTemp = sampleTemp;
                return;
            }

            // ★ 2. 5분 주기 체크
            double elapsedSeconds = (DateTime.Now - _holdCheckStartTime).TotalSeconds;

            if (elapsedSeconds < HoldCheckIntervalSeconds)
            {
                // 아직 5분 안됨 → 대기
                return;
            }

            // ★ 3. 5분 경과 → 변화량 계산
            double tempChange = sampleTemp - _holdCheckStartTemp;
            double changePerMinute = tempChange / (elapsedSeconds / 60.0);

            OnLog($"Hold 체크: {_holdCheckStartTemp:F1}→{sampleTemp:F1}°C ({tempChange:+0.0;-0.0}°C / {elapsedSeconds / 60:F1}분)");

            double newSV = currentSetpoint;

            // ★ 4. 상태별 SV 조정
            if (error > 0.1)
            {
                // CH2가 목표보다 낮음
                if (tempChange <= 0.1)
                {
                    // 온도 상승 없음 → SV 증가
                    newSV = currentSetpoint + 1;
                    OnLog($"Hold: 온도 상승 없음, CH1 SV +1 → {newSV:F1}°C");
                }
                else if (tempChange < error * 0.5)
                {
                    // 상승 중이지만 느림 → SV 소폭 증가
                    newSV = currentSetpoint + 0.5;
                    OnLog($"Hold: 상승 느림, CH1 SV +0.5 → {newSV:F1}°C");
                }
                // else: 적절히 상승 중 → 유지
            }
            else if (error < -0.1)
            {
                // CH2가 목표보다 높음 (0.1~0.3 사이)
                if (tempChange >= 0)
                {
                    // 계속 상승 중 → SV 감소
                    newSV = currentSetpoint - 0.5;
                    OnLog($"Hold: 초과 상태에서 상승 중, CH1 SV -0.5 → {newSV:F1}°C");
                }
                // else: 하강 중 → 유지
            }
            else
            {
                // 목표 범위 내 (±0.1°C)
                _equilibriumHeaterTemp = currentSetpoint;

                if (Math.Abs(tempChange) > 0.2)
                {
                    // 범위 내지만 변화 큼 → 미세 조정
                    if (tempChange > 0.2)
                    {
                        newSV = currentSetpoint - 0.3;
                        OnLog($"Hold: 목표 도달, 상승 추세 → CH1 SV -0.3");
                    }
                    else if (tempChange < -0.2)
                    {
                        newSV = currentSetpoint + 0.3;
                        OnLog($"Hold: 목표 도달, 하강 추세 → CH1 SV +0.3");
                    }
                }
            }

            // 제한 적용
            newSV = Math.Max(newSV, _targetSampleTemp - 5);
            newSV = Math.Min(newSV, _targetSampleTemp + _currentProfile.MaxHeaterSampleGap);
            newSV = Math.Min(newSV, _currentProfile.MaxHeaterTemperature);

            if (Math.Abs(newSV - currentSetpoint) > 0.1)
            {
                SetHeaterTemperature(newSV);
            }

            // ★ 5. 다음 5분 관찰 시작
            _holdCheckStartTime = DateTime.Now;
            _holdCheckStartTemp = sampleTemp;
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

            // 학습/예측 상태 초기화
            _thermalTransferCoeff = 0;
            _coefficientLearned = false;
            _equilibriumHeaterTemp = 0;
            _approachAdjustCount = 0;
            _currentRampRate = 0;
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
                RampState.Learning => $"열 특성 학습 중 ({sampleTemp:F1}°C)",
                RampState.Ramping => $"램프 진행 중 ({sampleTemp:F1}°C → {_targetSampleTemp:F1}°C, {_currentRampRate:F2}°C/min)",
                RampState.Approaching => $"평형점 탐색 중 ({sampleTemp:F1}°C → {_targetSampleTemp:F1}°C)",
                RampState.Stabilizing => $"안정화 중 ({sampleTemp:F1}°C)",
                RampState.Holding => $"온도 유지 중 ({sampleTemp:F1}°C, 평형 히터: {_equilibriumHeaterTemp:F1}°C)",
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