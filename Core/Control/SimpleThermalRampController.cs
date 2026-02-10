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

        private DateTime _holdCheckStartTime;
        private double _holdCheckStartTemp;


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

        private HoldModeSettings _holdSettings;

        public HoldModeSettings HoldSettings
        {
            get => _holdSettings;
            set => _holdSettings = value ?? new HoldModeSettings();
        }


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
            _holdSettings = HoldModeSettings.Load();  // ★ 설정 로드

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

                    SetHeaterTemperature(safeInitialHeater);
                    _tempController.Start(HeaterChannel);
                    await Task.Delay(100);
                    _state = RampState.Learning;
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
        /// 학습 단계: 열 전달 계수 측정
        /// </summary>
        private void ExecuteLearningPhase(double sampleTemp, double heaterTemp)
        {
            double elapsedSeconds = (DateTime.Now - _learningStartTime).TotalSeconds;
            double tempRise = sampleTemp - _learningStartSampleTemp;

            // 학습 완료 조건: 시간 경과 또는 충분한 온도 상승
            if (elapsedSeconds >= LearningDurationSeconds || tempRise >= MinLearningTempRise * 2)
            {
                if (tempRise >= MinLearningTempRise)
                {
                    // 열 전달 계수 계산
                    // K = 샘플 온도 변화율 / (평균 히터온도 - 평균 샘플온도)
                    double avgHeaterTemp = (_learningStartHeaterTemp + heaterTemp) / 2;
                    double avgSampleTemp = (_learningStartSampleTemp + sampleTemp) / 2;
                    double tempDiff = avgHeaterTemp - avgSampleTemp;

                    if (tempDiff > 1)
                    {
                        double ratePerMinute = tempRise / (elapsedSeconds / 60.0);
                        _thermalTransferCoeff = ratePerMinute / tempDiff;
                        _coefficientLearned = true;

                        OnLog($"열 특성 학습 완료: K={_thermalTransferCoeff:F4} °C/min/°C");
                        OnLog($"  (샘플 {tempRise:F2}°C 상승, 온도차 {tempDiff:F1}°C, {elapsedSeconds:F0}초)");
                    }
                }

                if (!_coefficientLearned)
                {
                    // 학습 실패 → 기본값 사용
                    _thermalTransferCoeff = 0.1;  // 보수적 기본값
                    OnLog($"학습 데이터 부족, 기본 K={_thermalTransferCoeff} 사용");
                }

                // Ramping 단계로 전환
                _state = RampState.Ramping;
                OnLog("램프 제어 시작");
            }
        }

        /// <summary>
        /// 적응형 램프 제어 (학습된 K 사용)
        /// </summary>
        private void ExecuteAdaptiveRampControl(double sampleTemp, double heaterTemp)
        {
            double distanceToTarget = _targetSampleTemp - sampleTemp;

            // 목표 근접 시 Approaching 모드로 전환
            if (distanceToTarget <= 3)
            {
                _state = RampState.Approaching;
                _approachAdjustCount = 0;
                OnLog($"목표 접근, 평형점 탐색 시작: {sampleTemp:F1}°C");
                return;
            }

            // === 안전 우선: 승온 속도 체크 ===
            if (_currentRampRate > _rampRate * 1.2)  // 20% 초과
            {
                // 속도 초과 → 히터 낮춤
                double currentSetpoint = GetHeaterSetpoint();
                double newSetpoint = currentSetpoint - 2;
                newSetpoint = Math.Max(newSetpoint, sampleTemp);
                SetHeaterTemperature(newSetpoint);
                OnLog($"승온 속도 초과 ({_currentRampRate:F2}°C/min), 히터 감소 → {newSetpoint:F1}°C");
                return;
            }

            // === 예측 기반 히터 설정 ===
            double targetRampRate = _rampRate;

            // 목표 승온을 위해 필요한 온도차 계산
            // K = 승온속도 / 온도차 → 온도차 = 승온속도 / K
            double requiredTempDiff = _thermalTransferCoeff > 0
                ? targetRampRate / _thermalTransferCoeff
                : 20;  // 기본값

            // 안전 마진 적용 (80%)
            double targetHeaterTemp = sampleTemp + (requiredTempDiff * 0.8);

            // === 거리에 따른 추가 제한 ===
            if (distanceToTarget <= 10)
            {
                // 10°C 이내: 더 보수적
                targetHeaterTemp = Math.Min(targetHeaterTemp, _targetSampleTemp + 3);
            }
            else if (distanceToTarget <= 20)
            {
                // 20°C 이내: 약간 보수적
                targetHeaterTemp = Math.Min(targetHeaterTemp, _targetSampleTemp + 10);
            }

            // 프로파일 제한 적용
            targetHeaterTemp = Math.Min(targetHeaterTemp, _currentProfile.MaxHeaterTemperature);
            targetHeaterTemp = Math.Min(targetHeaterTemp, sampleTemp + _currentProfile.MaxHeaterSampleGap);
            targetHeaterTemp = Math.Max(targetHeaterTemp, sampleTemp);

            // 현재 설정값과 비교해서 점진적 변경 (급격한 변화 방지)
            double currentSetpoint2 = GetHeaterSetpoint();
            double maxChange = 3.0;  // 사이클당 최대 3°C 변화
            double newSetpoint2 = currentSetpoint2;

            if (targetHeaterTemp > currentSetpoint2)
            {
                newSetpoint2 = Math.Min(currentSetpoint2 + maxChange, targetHeaterTemp);
            }
            else if (targetHeaterTemp < currentSetpoint2)
            {
                newSetpoint2 = Math.Max(currentSetpoint2 - maxChange, targetHeaterTemp);
            }

            SetHeaterTemperature(newSetpoint2);
        }

        /// <summary>
        /// 접근 제어: 평형점 탐색
        /// </summary>
        private void ExecuteApproachControl(double sampleTemp, double heaterTemp)
        {
            double error = _targetSampleTemp - sampleTemp;
            double currentSetpoint = GetHeaterSetpoint();
            double newSetpoint;

            // 샘플이 목표를 초과한 경우 → 히터를 샘플 아래로
            if (error < -0.5)
            {
                newSetpoint = sampleTemp - 2;
                OnLog($"샘플 초과 ({sampleTemp:F1}°C > 목표), 히터 감소");
            }
            // 샘플이 목표 범위 내 → 안정화로 전환
            else if (Math.Abs(error) <= _currentProfile.TemperatureStabilityRange)
            {
                _equilibriumHeaterTemp = currentSetpoint;
                _state = RampState.Stabilizing;
                _stabilizationStartTime = DateTime.Now;
                OnLog($"목표 도달, 평형 히터 온도: {_equilibriumHeaterTemp:F1}°C");
                return;
            }
            // 샘플이 목표 미만 → 히터 미세 증가
            else
            {
                // 남은 거리에 비례해서 조정
                double adjustment = error * 0.3;  // 30%만
                adjustment = Math.Max(0.2, Math.Min(2.0, adjustment));  // 0.2~2°C
                newSetpoint = currentSetpoint + adjustment;

                // 안전 제한
                newSetpoint = Math.Min(newSetpoint, _targetSampleTemp + 5);
                newSetpoint = Math.Min(newSetpoint, _currentProfile.MaxHeaterTemperature);
            }

            newSetpoint = Math.Max(newSetpoint, sampleTemp - 5);
            SetHeaterTemperature(newSetpoint);
            _approachAdjustCount++;
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

        /// <summary>
        /// Hold 제어 (10분 주기)
        /// </summary>
        private void ExecuteHoldControl(double sampleTemp, double heaterTemp)
        {
            double controlTemp = GetSampleTemperatureBySource();
            double checkIntervalSeconds = _holdSettings.CheckIntervalMinutes * 60;

            double elapsedSeconds = (DateTime.Now - _holdCheckStartTime).TotalSeconds;
            if (elapsedSeconds < checkIntervalSeconds)
            {
                return;
            }

            double error = _targetSampleTemp - controlTemp;
            double tempChange = controlTemp - _holdCheckStartTemp;
            double changeRate = tempChange / (elapsedSeconds / 60.0);  // °C/min
            double currentSetpoint = GetHeaterSetpoint();

            string sourceText = _holdSettings.GetSourceText();
            OnLog($"Hold 체크: {sourceText}={controlTemp:F1}°C, 변화율={changeRate:+0.00;-0.00}°C/min, 오차={error:+0.0;-0.0}°C");

            // ★ 자동 판단 로직
            double newSV = currentSetpoint;
            bool shouldAdjust = false;
            string reason = "";

            // 1. 목표 범위 내 → 유지
            if (Math.Abs(error) <= _holdSettings.ErrorTolerance)
            {
                _equilibriumHeaterTemp = currentSetpoint;
                reason = "목표 범위 내";
            }
            // 2. 초과 상태 (error < 0)
            else if (error < 0)
            {
                if (changeRate < -0.02)
                {
                    // 이미 하강 중 → 대기
                    reason = "초과지만 하강 중 → 대기";
                }
                else
                {
                    // 하강 안 함 → SV 감소 필요
                    shouldAdjust = true;
                    newSV = currentSetpoint + error;  // error가 음수이므로 감소
                    reason = "초과 + 정체 → SV 감소";
                }
            }
            // 3. 부족 상태 (error > 0)
            else
            {
                if (changeRate > 0.02)
                {
                    // 이미 상승 중 → 대기
                    reason = "부족하지만 상승 중 → 대기";
                }
                else
                {
                    // 상승 안 함 → SV 증가 필요
                    shouldAdjust = true;
                    newSV = currentSetpoint + error;  // error가 양수이므로 증가
                    reason = "부족 + 정체 → SV 증가";
                }
            }

            OnLog($"Hold 판단: {reason}");

            if (shouldAdjust)
            {
                // 최대 조정량 제한
                double maxAdj = _holdSettings.MaxAdjustment;
                double adjustment = newSV - currentSetpoint;
                adjustment = Math.Max(-maxAdj, Math.Min(maxAdj, adjustment));
                newSV = currentSetpoint + adjustment;

                // 범위 제한
                newSV = Math.Max(newSV, _holdSettings.MinHeaterTemp);
                newSV = Math.Min(newSV, _holdSettings.MaxHeaterTemp);

                if (Math.Abs(newSV - currentSetpoint) > 0.1)
                {
                    SetHeaterTemperature(newSV);
                    OnLog($"Hold: CH1 SV {currentSetpoint:F1} → {newSV:F1}°C");
                }
            }

            _holdCheckStartTime = DateTime.Now;
            _holdCheckStartTemp = controlTemp;
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

        /// <summary>
        /// 지정 채널 온도 읽기
        /// </summary>
        private double GetChannelTemperature(int channel)
        {
            try
            {
                if (_tempController?.Status?.ChannelStatus == null)
                    return double.NaN;

                int index = channel - 1;
                if (index < 0 || index >= _tempController.Status.ChannelStatus.Length)
                    return double.NaN;

                var ch = _tempController.Status.ChannelStatus[index];
                double temp = ch.PresentValue;
                if (ch.Dot > 0)
                    temp /= Math.Pow(10, ch.Dot);

                return temp;
            }
            catch
            {
                return double.NaN;
            }
        }

        /// <summary>
        /// 설정에 따라 샘플 온도 읽기
        /// </summary>
        private double GetSampleTemperatureBySource()
        {
            var channels = _holdSettings.GetSelectedChannels();

            if (channels.Count == 0)
                return GetSampleTemperature();

            double sum = 0;
            int count = 0;

            foreach (int ch in channels)
            {
                double temp = GetChannelTemperature(ch);
                if (!double.IsNaN(temp))
                {
                    sum += temp;
                    count++;
                }
            }

            return count > 0 ? sum / count : GetSampleTemperature();
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