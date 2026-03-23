using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// AutoRun 시퀀스 실행 서비스
    /// </summary>
    public class AutoRunService : IDisposable
    {
        #region 필드 및 속성

        private readonly MainForm _mainForm;
        private readonly AutoRunConfiguration _config;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _runningTask;
        private readonly object _lockObject = new object();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private DateTime _startTime;
        private DateTime _experimentStartTime;
        private bool _isExperimentTimerRunning = false;

        // 상태 저장용
        private string _experimentName = "";

        private AutoRunState _currentState = AutoRunState.Idle;
        private bool _isRunning = false;
        private bool _isPaused = false;
        private int _currentStepNumber = 0;
        private const int TOTAL_STEPS = 9;

        // 스텝별 가중치 (실제 소요 시간 비율 반영)
        // 1:초기화  2:진공준비  3:드라이펌프  4:터보펌프  5:이온게이지  6:고진공대기  7:히터시작  8:실험진행  9:종료
        private static readonly double[] StepWeights = { 2, 2, 3, 5, 3, 10, 2, 63, 10 };
        private static readonly double StepWeightTotal = 100; // Sum of StepWeights

        /// <summary>
        /// 현재 상태
        /// </summary>
        public AutoRunState CurrentState
        {
            get { lock (_lockObject) { return _currentState; } }
            private set
            {
                lock (_lockObject)
                {
                    if (_currentState != value)
                    {
                        var previousState = _currentState;
                        _currentState = value;
                        OnStateChanged(new AutoRunStateChangedEventArgs(previousState, value));

                        // 진행 중인 상태 변경 시 스냅샷 저장
                        if (_isRunning && value != AutoRunState.Completed
                            && value != AutoRunState.Aborted && value != AutoRunState.Error)
                        {
                            Task.Run(() => SaveStateSnapshot());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 실행 중 여부
        /// </summary>
        public bool IsRunning
        {
            get { lock (_lockObject) { return _isRunning; } }
        }

        /// <summary>
        /// 일시정지 여부
        /// </summary>
        public bool IsPaused
        {
            get { lock (_lockObject) { return _isPaused; } }
        }

        /// <summary>
        /// 현재 설정
        /// </summary>
        public AutoRunConfiguration Configuration => _config;

        /// <summary>
        /// 베이크아웃 열 특성 실시간 계수
        /// </summary>
        public ThermalCharacteristics ThermalParams { get; } = new ThermalCharacteristics();

        /// <summary>
        /// 실험 시작 시간 (온도 도달 후 카운트 시작 시점)
        /// </summary>
        public DateTime ExperimentStartTime => _experimentStartTime;

        /// <summary>
        /// 실험 타이머가 실제 카운트 중인지 (온도 도달 후)
        /// </summary>
        public bool IsExperimentTimerRunning => _isExperimentTimerRunning;

        /// <summary>
        /// 현재 실행 중인 단계 번호 (1~9)
        /// </summary>
        public int CurrentStepNumber => _currentStepNumber;

        /// <summary>
        /// 현재 실험이 베이크아웃 모드인지
        /// </summary>
        private bool IsBakeoutMode => _config.ExperimentType == ExperimentType.Bakeout;

        /// <summary>
        /// 실험 이름 (상태 저장용)
        /// </summary>
        public string ExperimentName
        {
            get => _experimentName;
            set => _experimentName = value ?? "";
        }

        /// <summary>
        /// 현재 오토런 진행 상태를 파일에 저장 (재시작 시 이어하기용)
        /// </summary>
        public void SaveStateSnapshot()
        {
            if (!_isRunning) return;

            try
            {
                var snapshot = new AutoRunStateSnapshot
                {
                    CurrentStepNumber = _currentStepNumber,
                    StartTime = _startTime,
                    ExperimentStartTime = _experimentStartTime,
                    IsExperimentTimerRunning = _isExperimentTimerRunning,
                    ExperimentType = _config.ExperimentType,
                    ExperimentName = _experimentName,
                    AutoRunElapsedSeconds = (int)_stopwatch.Elapsed.TotalSeconds,
                    ExperimentElapsedSeconds = _isExperimentTimerRunning
                        ? (int)(DateTime.Now - _experimentStartTime).TotalSeconds : 0
                };
                snapshot.Save();
            }
            catch { }
        }

        /// <summary>
        /// 저장된 상태 파일 삭제
        /// </summary>
        public static void ClearStateSnapshot() => AutoRunStateSnapshot.Clear();

        #endregion

        #region 이벤트

        public event EventHandler<AutoRunStateChangedEventArgs> StateChanged;
        public event EventHandler<AutoRunProgressEventArgs> ProgressUpdated;
        public event EventHandler<AutoRunErrorEventArgs> ErrorOccurred;
        public event EventHandler<AutoRunCompletedEventArgs> Completed;

        #endregion

        #region 생성자 및 소멸자

        public AutoRunService(MainForm mainForm, AutoRunConfiguration config = null)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _config = config ?? new AutoRunConfiguration();
        }

        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            _stopwatch?.Stop();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 지정된 단계부터 AutoRun을 시작합니다.
        /// </summary>
        /// <param name="startFromStep">시작 단계 (1~8, 기본 1)</param>
        public async Task<bool> StartAsync(int startFromStep = 1)
        {
            lock (_lockObject)
            {
                if (_isRunning)
                {
                    LogWarning("AutoRun이 이미 실행 중입니다.");
                    return false;
                }
                _isRunning = true;
            }

            // 이전 CTS 정리 후 새로 생성
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();

            startFromStep = Math.Max(1, Math.Min(8, startFromStep));

            if (startFromStep > 1)
                LogInfo($"=== AutoRun 시퀀스 시작 (단계 {startFromStep}: {SystemStateAssessment.GetStepName(startFromStep)}부터) ===");
            else
                LogInfo("=== AutoRun 시퀀스 시작 ===");

            // 설정값 요약 로그
            if (IsBakeoutMode)
            {
                LogInfo($"[설정] 베이크아웃 — 목표:{_config.BakeoutTargetTemperature:F1}°C, " +
                    $"램프:{_config.BakeoutRampRate:F0}°C/h, " +
                    $"모니터:{_config.GetBakeoutMonitorLabel()}, " +
                    $"CH1상한:{_config.BakeoutHeaterMaxTemperature:F1}°C, " +
                    $"ΔT제한:{(_config.BakeoutMaxDeltaT > 0 ? $"{_config.BakeoutMaxDeltaT:F0}°C" : "없음")}, " +
                    $"홀드:{_config.BakeoutHoldTimeMinutes}분, " +
                    $"종료:{_config.BakeoutEndAction}" +
                    (_config.PressureInterlockDurationSeconds > 0
                        ? $", 압력인터락:{_config.PressureInterlockDurationSeconds}초"
                        : ""));
            }
            else
            {
                LogInfo($"[설정] 탈가스율 — 목표:{_config.HeaterCh1SetTemperature:F1}°C, " +
                    $"실험:{_config.ExperimentDurationMinutes}분, " +
                    $"최대압력:{_config.MaxPressureDuringExperiment:E1} Torr" +
                    (_config.PressureInterlockDurationSeconds > 0
                        ? $", 압력인터락:{_config.PressureInterlockDurationSeconds}초"
                        : ""));
            }

            LogInfo($"[설정] 안전: 실패시종료={(_config.EnableSafeShutdownOnFailure ? "ON" : "OFF")}, " +
                $"알람={(_config.EnableAlarmOnError ? "ON" : "OFF")}, " +
                $"재시도={_config.MaxRetryCount}회 (실험단계: 재시도없음)");

            _startTime = DateTime.Now;
            _stopwatch.Restart();
            _currentStepNumber = 0;

            try
            {
                _runningTask = RunSequenceAsync(_cancellationTokenSource.Token, startFromStep);
                await _runningTask;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"AutoRun 실행 중 오류 발생: {ex.Message}", ex);
                return false;
            }
        }

        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                LogWarning("AutoRun 중지 요청됨");
                _cancellationTokenSource.Cancel();
                _isRunning = false;
                _isPaused = false;
                _isExperimentTimerRunning = false;
            }

            CurrentState = AutoRunState.Aborted;
            AutoRunStateSnapshot.Clear();
            _stopwatch.Stop();
            EnableManualControls(true);
        }

        /// <summary>
        /// Stop 후 백그라운드 태스크 완료 대기 (UI에서 호출)
        /// </summary>
        public async Task WaitForStopAsync(int timeoutMs = 5000)
        {
            if (_runningTask == null) return;
            try
            {
                await Task.WhenAny(_runningTask, Task.Delay(timeoutMs));
                if (!_runningTask.IsCompleted)
                    LogWarning("AutoRun 태스크가 아직 종료되지 않았습니다 (타임아웃)");
            }
            catch { }
        }

        /// <summary>
        /// 종료 시퀀스만 단독 실행 (사용자 중지 후 안전 종료용)
        /// </summary>
        public async Task RunShutdownSequenceAsync()
        {
            try
            {
                CurrentState = AutoRunState.ShuttingDown;
                LogInfo("── 종료 시퀀스 시작 (수동) ──");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_config.ShutdownTimeout));
                bool success = await FullShutdownAsync(cts.Token);

                if (success)
                    LogInfo("── 종료 시퀀스 완료 ──");
                else
                    LogWarning("── 종료 시퀀스 일부 실패 ──");
            }
            catch (OperationCanceledException)
            {
                LogError($"종료 시퀀스 타임아웃 ({_config.ShutdownTimeout}초 초과) — 시퀀스 강제 중단");
            }
            catch (Exception ex)
            {
                LogError($"종료 시퀀스 오류: {ex.Message}");
            }
            finally
            {
                CurrentState = AutoRunState.Idle;
            }
        }

        public void Pause()
        {
            lock (_lockObject)
            {
                if (!_isRunning || _isPaused) return;
                _isPaused = true;
            }

            LogInfo("AutoRun 일시정지됨");
            CurrentState = AutoRunState.Paused;
        }

        public void Resume()
        {
            lock (_lockObject)
            {
                if (!_isRunning || !_isPaused) return;
                _isPaused = false;
            }

            LogInfo("AutoRun 재개됨");
        }

        /// <summary>
        /// 현재 시스템 상태를 감지하여 어느 단계부터 시작 가능한지 판단합니다.
        /// </summary>
        public SystemStateAssessment DetectCurrentSystemState()
        {
            var assessment = new SystemStateAssessment();

            try
            {
                // 1. 장치 연결 상태
                assessment.AllDevicesConnected =
                    (_mainForm._ioModule?.IsConnected == true) &&
                    (_mainForm._dryPump?.IsConnected == true) &&
                    (_mainForm._turboPump?.IsConnected == true) &&
                    (_mainForm._bathCirculator?.IsConnected == true) &&
                    (_mainForm._tempController?.IsConnected == true);

                // 2. 밸브 상태
                // DI/DO 기반으로 밸브 상태 확인
                var doData = _mainForm._ioModule?.LastValidDOValues;

                // 게이트 밸브: DI 기반 리드 스위치로 확인
                assessment.GateValveStatus = _mainForm._ioModule?.GateValvePosition ?? "Unknown";

                if (doData != null)
                {
                    assessment.VentValveStatus = doData.IsVentValveOn ? "Opened" : "Closed";
                    assessment.ExhaustValveStatus = doData.IsExhaustValveOn ? "Opened" : "Closed";
                }

                assessment.ValvesReady =
                    assessment.GateValveStatus == "Opened" &&
                    assessment.VentValveStatus == "Closed" &&
                    assessment.ExhaustValveStatus == "Closed";

                // 3. 드라이펌프
                assessment.DryPumpRunning = _mainForm._dryPump?.Status?.IsRunning == true;

                // 4. 터보펌프 (정격의 95% 이상)
                assessment.TurboPumpSpeed = _mainForm._turboPump?.Status?.CurrentSpeed ?? 0;
                assessment.TurboPumpAtSpeed = _mainForm._turboPump?.Status?.IsRunning == true
                    && assessment.TurboPumpSpeed >= 590;

                // 5. 이온게이지 — DO 기반
                assessment.IonGaugeActive = doData?.IsIonGaugeHVOn == true;

                // 6. 압력
                var aiData = _mainForm._ioModule?.LastValidAIValues;
                if (aiData != null)
                {
                    assessment.CurrentPressure =
                        _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(
                            aiData.ExpansionVoltageValues[1]) ?? 0;

                    if (assessment.IonGaugeActive && assessment.CurrentPressure < 1E-3)
                    {
                        var ionPressure = _mainForm._ionGauge?.ConvertVoltageToPressureInTorr(
                            aiData.ExpansionVoltageValues[2]) ?? 0;
                        if (ionPressure > 0 && ionPressure < assessment.CurrentPressure)
                            assessment.CurrentPressure = ionPressure;
                    }
                }

                assessment.HighVacuumReached = assessment.CurrentPressure > 0
                    && assessment.CurrentPressure <= _config.TargetPressureForHeater;

                // 7. 히터
                assessment.HeaterRunning = _mainForm._tempController?.Status?.ChannelStatus != null
                    && _mainForm._tempController.Status.ChannelStatus.Length > 0
                    && _mainForm._tempController.Status.ChannelStatus[0].IsRunning;

                // ── 권장 시작 단계 결정 (뒤에서부터 체크) ──
                if (assessment.HeaterRunning && assessment.HighVacuumReached)
                    assessment.RecommendedStartStep = 8;
                else if (assessment.HighVacuumReached && assessment.IonGaugeActive)
                    assessment.RecommendedStartStep = 7;
                else if (assessment.IonGaugeActive && assessment.TurboPumpAtSpeed)
                    assessment.RecommendedStartStep = 6;
                else if (assessment.TurboPumpAtSpeed && assessment.DryPumpRunning)
                    assessment.RecommendedStartStep = 5;
                else if (assessment.DryPumpRunning && assessment.ValvesReady)
                    assessment.RecommendedStartStep = 4;
                else if (assessment.ValvesReady)
                    assessment.RecommendedStartStep = 3;
                else if (assessment.AllDevicesConnected)
                    assessment.RecommendedStartStep = 2;
                else
                    assessment.RecommendedStartStep = 1;
            }
            catch (Exception ex)
            {
                LogWarning($"시스템 상태 감지 중 오류: {ex.Message}");
                assessment.RecommendedStartStep = 1;
            }

            return assessment;
        }

        #endregion

        #region 시퀀스 실행

        /// <summary>
        /// 메인 시퀀스 실행 (지정 단계부터 시작 가능)
        /// </summary>
        private async Task RunSequenceAsync(CancellationToken cancellationToken, int startFromStep = 1)
        {
            StepResult result = StepResult.Success;

            try
            {
                EnableManualControls(false);

                // 건너뛴 단계 로깅
                if (startFromStep > 1)
                {
                    for (int i = 1; i < startFromStep; i++)
                    {
                        LogInfo($"단계 {i}/{TOTAL_STEPS}: {GetStateDescription(GetAutoRunStateForStep(i))} — 건너뜀 (이미 완료)");
                    }
                }

                // 단계 1: 초기화
                if (startFromStep <= 1)
                {
                    _currentStepNumber = 1;
                    result = await ExecuteStepAsync(AutoRunState.Initializing, InitializeAsync,
                        _config.InitializationTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 2: 진공 준비
                if (startFromStep <= 2)
                {
                    _currentStepNumber = 2;
                    result = await ExecuteStepAsync(AutoRunState.PreparingVacuum, PrepareVacuumAsync,
                        _config.ValveOperationTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 3: 드라이펌프 시작
                if (startFromStep <= 3)
                {
                    _currentStepNumber = 3;
                    result = await ExecuteStepAsync(AutoRunState.StartingDryPump, StartDryPumpAsync,
                        _config.DryPumpStartTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 4: 터보펌프 시작
                if (startFromStep <= 4)
                {
                    _currentStepNumber = 4;
                    result = await ExecuteStepAsync(AutoRunState.StartingTurboPump, StartTurboPumpAsync,
                        _config.TurboPumpStartTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 5: 이온게이지 활성화 (PTR225 사용 시에만)
                if (startFromStep <= 5)
                {
                    if (_mainForm._ionGauge?.Model == IonGaugeModel.PTR225)
                    {
                        _currentStepNumber = 5;
                        result = await ExecuteStepAsync(AutoRunState.ActivatingIonGauge, ActivateIonGaugeAsync,
                            _config.IonGaugeActivationTimeout, cancellationToken);
                        if (result != StepResult.Success) goto Cleanup;
                    }
                    else
                    {
                        LogInfo("이온게이지 모델이 PTR225가 아님 — 이온게이지 활성화 단계 스킵");
                    }
                }

                // 단계 6: 고진공 대기
                if (startFromStep <= 6)
                {
                    _currentStepNumber = 6;
                    result = await ExecuteStepAsync(AutoRunState.WaitingHighVacuum, WaitForHighVacuumAsync,
                        _config.HighVacuumTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 7: 히터 시작
                if (startFromStep <= 7)
                {
                    _currentStepNumber = 7;
                    result = await ExecuteStepAsync(AutoRunState.StartingHeater, StartHeaterAsync,
                        _config.HeaterStartTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
                }

                // 단계 7을 건너뛴 경우 (히터가 이미 작동 중) → 온도 재설정 + 칠러 PID 시작
                if (startFromStep > 7)
                {
                    double effectiveTargetTemp = IsBakeoutMode
                        ? _config.BakeoutTargetTemperature
                        : _config.HeaterCh1SetTemperature;

                    // 설정 변경 후 재시작일 수 있으므로 새 목표 온도를 컨트롤러에 전송
                    if (_mainForm._tempController?.IsConnected == true)
                    {
                        await _mainForm._tempController.UpdateStatusAsync();
                        var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];

                        short ch1SetValue = ch1Status.Dot == 1
                            ? (short)(effectiveTargetTemp * 10)
                            : (short)effectiveTargetTemp;

                        double currentSV = ch1Status.Dot == 1
                            ? ch1Status.SetValue / 10.0 : ch1Status.SetValue;

                        if (Math.Abs(currentSV - effectiveTargetTemp) > 0.5)
                        {
                            _mainForm._tempController.SetTemperature(1, ch1SetValue);
                            LogInfo($"히터 CH1 온도 재설정: {currentSV:F1}°C → {effectiveTargetTemp:F1}°C");
                        }
                    }

                    if (!IsBakeoutMode)
                        StartChillerPID();
                }

                // 단계 8: 실험 진행 (항상 실행)
                _currentStepNumber = 8;
                int experimentDurationMinutes = IsBakeoutMode
                    ? _config.BakeoutHoldTimeMinutes
                    : _config.ExperimentDurationMinutes;
                // ★ [Fix#11] 승온 타임아웃을 램프 속도 기반으로 동적 계산
                int riseTimeoutSec = 7200; // 기본 2시간 (탈가스율)
                if (IsBakeoutMode && _config.BakeoutRampRate > 0)
                {
                    double maxTempGap = _config.BakeoutTargetTemperature; // 최악: 0°C부터 승온
                    double estimatedRiseHours = maxTempGap / _config.BakeoutRampRate;
                    riseTimeoutSec = Math.Max(7200, (int)(estimatedRiseHours * 3600 * 3) + 1800);
                }
                // ★ 실험 단계는 재시도 불가 — 승온/센서/과압 실패는 재시도로 해결되지 않음
                result = await ExecuteStepAsync(AutoRunState.RunningExperiment, RunExperimentAsync,
                    experimentDurationMinutes * 60 + riseTimeoutSec, cancellationToken, maxRetryOverride: 0);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 9: 종료 시퀀스 (재시도 불가)
                _currentStepNumber = 9;
                result = await ExecuteStepAsync(AutoRunState.ShuttingDown, ShutdownSequenceAsync,
                    _config.ShutdownTimeout, cancellationToken, maxRetryOverride: 0);

                CurrentState = AutoRunState.Completed;
                AutoRunStateSnapshot.Clear();
                var totalTime = DateTime.Now - _startTime;
                LogInfo($"=== AutoRun 시퀀스 정상 완료 (총 소요시간: {totalTime:hh\\:mm\\:ss}) ===");

            Cleanup:
                if (result != StepResult.Success)
                {
                    string resultDesc = result == StepResult.Aborted ? "사용자 중단"
                        : result == StepResult.Timeout ? "시간 초과"
                        : "실패";
                    var totalElapsed = DateTime.Now - _startTime;
                    LogError($"=== AutoRun 종료 (결과: {resultDesc}, 단계: {_currentStepNumber}/{TOTAL_STEPS}, 총 경과: {totalElapsed:hh\\:mm\\:ss}) ===");

                    CurrentState = result == StepResult.Aborted
                        ? AutoRunState.Aborted
                        : AutoRunState.Error;

                    if (result == StepResult.Aborted)
                    {
                        LogWarning("[조치] 사용자 중단 — 안전 종료 생략 (수동 제어 전환)");
                    }
                    else if (_config.EnableSafeShutdownOnFailure)
                    {
                        LogWarning($"[조치] 안전 종료 시퀀스 실행 — 히터 정지, 밸브 닫기, 펌프 정지 (실패 단계: {_currentStepNumber})");
                        await EmergencyShutdownAsync(_currentStepNumber);
                    }
                    else
                    {
                        LogWarning("[조치] 안전 종료 비활성화 — 장비 현재 상태 유지 (수동 조치 필요!)");
                    }
                }

                // OnCompleted 이벤트 발생 전에 _isRunning을 false로 설정
                // → UI 핸들러(UpdateAutoRunBanner 등)가 IsRunning == false를 확인하여 배너를 숨길 수 있도록
                lock (_lockObject)
                {
                    _isRunning = false;
                    _isPaused = false;
                    _isExperimentTimerRunning = false;
                }

                var completedArgs = new AutoRunCompletedEventArgs(
                    result == StepResult.Success, _startTime, DateTime.Now)
                {
                    Summary = GenerateSummary(result)
                };
                OnCompleted(completedArgs);

                EnableManualControls(true);
            }
            catch (Exception ex)
            {
                LogError($"시퀀스 실행 중 예외 발생: {ex.Message}", ex);
                CurrentState = AutoRunState.Error;
                EnableManualControls(true);
                throw;
            }
            finally
            {
                // 예외 경로에서도 확실히 정리
                lock (_lockObject)
                {
                    _isRunning = false;
                    _isPaused = false;
                    _isExperimentTimerRunning = false;
                }
            }
        }

        /// <summary>
        /// 단계 실행 래퍼
        /// </summary>
        private async Task<StepResult> ExecuteStepAsync(AutoRunState state, Func<CancellationToken, Task<bool>> stepAction,
            int timeoutSeconds, CancellationToken cancellationToken, int maxRetryOverride = -1)
        {
            CurrentState = state;
            int maxRetry = maxRetryOverride >= 0 ? maxRetryOverride : _config.MaxRetryCount;
            var stepStartTime = DateTime.Now;
            string stepName = GetStateDescription(state);
            LogInfo($"── 단계 {_currentStepNumber}/{TOTAL_STEPS}: {stepName} 시작 ──" +
                (maxRetry == 0 ? " (재시도 없음)" : $" (최대 재시도: {maxRetry}회, 타임아웃: {timeoutSeconds}초)"));

            var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            stepCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                int retryCount = 0;
                bool success = false;

                while (retryCount <= maxRetry && !success)
                {
                    if (retryCount > 0)
                    {
                        LogWarning($"재시도 {retryCount}/{maxRetry}...");
                        await Task.Delay(_config.RetryDelaySeconds * 1000, cancellationToken);
                    }

                    try
                    {
                        while (_isPaused && !cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(100, cancellationToken);
                        }

                        success = await stepAction(stepCts.Token);
                        if (success)
                        {
                            var stepElapsed = DateTime.Now - stepStartTime;
                            string stepSummary = GetStepCompletionSummary(state);
                            LogInfo($"── 단계 {_currentStepNumber} 완료: {stepName} [{stepElapsed:mm\\:ss}] {stepSummary} ──");
                            return StepResult.Success;
                        }
                    }
                    catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        var stepElapsed = DateTime.Now - stepStartTime;
                        LogError($"[중단] 단계 {_currentStepNumber} {stepName} 시간 초과 ({timeoutSeconds}초, 경과: {stepElapsed:mm\\:ss})");
                        return StepResult.Timeout;
                    }

                    retryCount++;
                }

                var failElapsed = DateTime.Now - stepStartTime;
                LogError($"[중단] 단계 {_currentStepNumber} {stepName} 실패 [{failElapsed:mm\\:ss}]" +
                    (maxRetry > 0 ? $" (재시도 {maxRetry}회 초과)" : " — 재시도 불가 단계"));
                return StepResult.Failed;
            }
            catch (OperationCanceledException)
            {
                LogWarning($"단계 {_currentStepNumber} 중단됨");
                return StepResult.Aborted;
            }
            finally
            {
                stepCts?.Dispose();
            }
        }

        #endregion

        #region 각 단계 구현

        /// <summary>
        /// 단계 1: 초기화
        /// </summary>
        private async Task<bool> InitializeAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("시스템 상태 점검 중...", 0);

            if (!_mainForm._ioModule?.IsConnected ?? true)
            {
                LogError("IO Module이 연결되지 않았습니다.");
                return false;
            }

            UpdateProgress("펌프 상태 확인 중...", 20);

            if (_mainForm._dryPump?.IsConnected == true)
            {
                if (_mainForm._dryPump.Status?.IsRunning == true)
                {
                    LogInfo("드라이펌프가 이미 작동 중입니다. 시작 단계를 건너뜁니다.");
                }
            }

            if (_mainForm._turboPump?.IsConnected == true)
            {
                if (_mainForm._turboPump.Status?.IsRunning == true)
                {
                    LogInfo("터보펌프가 이미 작동 중입니다. 시작 단계를 건너뜁니다.");
                }
            }

            UpdateProgress("칠러 상태 확인 중...", 40);

            if (_mainForm._bathCirculator?.IsConnected == true)
            {
                // 폴링과 완전히 독립된 우선순위 통신으로 상태 확인 및 시작
                bool checkOk = await Task.Run(() => _mainForm._bathCirculator.CheckStatusPriority());
                bool isRunning = checkOk && _mainForm._bathCirculator.Status.IsRunning;
                LogInfo($"칠러 상태: checkOk={checkOk}, IsRunning={isRunning}, OperationReg={_mainForm._bathCirculator.Status.OperationRegister}");

                if (!isRunning)
                {
                    LogInfo("칠러를 시작합니다...");
                    bool startResult = await Task.Run(() => _mainForm._bathCirculator.StartPriority());
                    LogInfo($"칠러 Start 결과: {startResult}");
                }

                // 칠러 온도는 PID가 제어 — PID 베이스 온도를 초기값으로 설정
                var pidService = _mainForm.ChillerPIDService;
                if (pidService != null)
                {
                    double baseTemp = pidService.ChillerBaseTemperature;
                    UpdateProgress($"칠러 초기 온도 설정 중 (PID 베이스: {baseTemp}°C)...", 50);
                    _mainForm._bathCirculator.SetTemperature(baseTemp);
                    await Task.Delay(500, cancellationToken);
                    LogInfo($"칠러 초기 온도 = PID 베이스 {baseTemp}°C (이후 PID가 자동 제어)");
                }
                else
                {
                    LogWarning("칠러 PID 서비스 미초기화 — 칠러 온도를 현재 설정값으로 유지");
                }
            }
            else
            {
                LogWarning("칠러가 연결되지 않았습니다. 계속 진행합니다.");
            }

            UpdateProgress("히터 상태 확인 중...", 60);

            if (_mainForm._tempController?.IsConnected == true)
            {
                await _mainForm._tempController.UpdateStatusAsync();
                var status = _mainForm._tempController.Status;
                if (status.ChannelStatus[0].IsRunning)
                {
                    LogWarning("히터 CH1이 이미 작동 중입니다. 안전을 위해 정지합니다.");
                    _mainForm._tempController.Stop(1);
                    await Task.Delay(1000, cancellationToken);
                    LogInfo("히터 CH1 정지 완료");
                }
            }

            UpdateProgress("압력 확인 중...", 80);

            var measurements = await GetCurrentMeasurementsAsync();
            LogInfo($"현재 압력: {measurements.CurrentPressure:E2} Torr");

            UpdateProgress("초기화 완료", 100);
            await Task.Delay(500, cancellationToken);

            return true;
        }

        /// <summary>
        /// 단계 2: 진공 준비
        /// </summary>
        private async Task<bool> PrepareVacuumAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("게이트 밸브 열기...", 0);

            if (!await _mainForm._ioModule.ControlGateValveAsync(true))
            {
                LogError("게이트 밸브 열기 실패");
                return false;
            }

            await Task.Delay(2000, cancellationToken);
            UpdateProgress("게이트 밸브 상태 확인...", 33);

            UpdateProgress("벤트 밸브 닫기...", 33);
            if (!await _mainForm._ioModule.ControlVentValveAsync(false))
            {
                LogError("벤트 밸브 닫기 실패");
                return false;
            }

            await Task.Delay(1000, cancellationToken);

            UpdateProgress("배기 밸브 닫기...", 66);
            if (!await _mainForm._ioModule.ControlExhaustValveAsync(false))
            {
                LogError("배기 밸브 닫기 실패");
                return false;
            }

            await Task.Delay(1000, cancellationToken);

            // DO 기반으로 밸브 상태 확인
            UpdateProgress("밸브 상태 최종 확인...", 90);
            var doData = await _mainForm._ioModule.ReadDigitalOutputsAsync();
            if (doData == null)
            {
                LogError("밸브 상태 읽기 실패");
                return false;
            }

            var measurements = await GetCurrentMeasurementsAsync();

            if (measurements.GateValveStatus != "Opened")
            {
                LogError($"게이트 밸브가 열리지 않았습니다. 상태: {measurements.GateValveStatus}");
                return false;
            }

            if (measurements.VentValveStatus != "Closed")
            {
                LogError($"벤트 밸브가 닫히지 않았습니다. 상태: {measurements.VentValveStatus}");
                return false;
            }

            if (measurements.ExhaustValveStatus != "Closed")
            {
                LogError($"배기 밸브가 닫히지 않았습니다. 상태: {measurements.ExhaustValveStatus}");
                return false;
            }

            UpdateProgress("진공 준비 완료", 100);
            return true;
        }

        /// <summary>
        /// 단계 3: 드라이펌프 시작
        /// </summary>
        private async Task<bool> StartDryPumpAsync(CancellationToken cancellationToken)
        {
            if (!_mainForm._dryPump?.IsConnected ?? true)
            {
                LogWarning("드라이펌프가 연결되지 않았습니다. 단계를 건너뜁니다.");
                return true;
            }

            UpdateProgress("드라이펌프 시작 중...", 0);

            // 이미 작동 중이면 건너뛰기
            if (_mainForm._dryPump.Status?.IsRunning == true)
            {
                LogInfo("드라이펌프가 이미 작동 중입니다. 시작 단계를 건너뜁니다.");
                UpdateProgress("드라이펌프 시작 완료 (이미 작동 중)", 100);
                return true;
            }

            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogError("밸브 상태가 올바르지 않습니다.");
                return false;
            }

            // 시작 명령 전송 (최대 3회 재시도)
            // ★ 폴링 서비스와 동시 통신 충돌 방지를 위해 UpdateStatusAsync 직접 호출 금지
            bool startSent = false;
            for (int retry = 0; retry < 3; retry++)
            {
                if (_mainForm._dryPump.Start())
                {
                    startSent = true;
                    break;
                }
                LogWarning($"드라이펌프 시작 명령 실패 (시도 {retry + 1}/3)");
                await Task.Delay(1000, cancellationToken);
            }

            if (!startSent)
            {
                // 명령 실패해도 폴링 서비스가 갱신할 때까지 대기
                await Task.Delay(3000, cancellationToken);
                if (_mainForm._dryPump.Status?.IsRunning == true)
                {
                    LogWarning("시작 명령 응답 실패했으나 드라이펌프가 작동 중입니다.");
                    return true;
                }
                LogError("드라이펌프 시작 명령 실패");
                return false;
            }

            // 폴링 서비스가 상태를 갱신할 때까지 대기 (최대 30초)
            int waitCount = 0;
            while (waitCount < 30)
            {
                await Task.Delay(1000, cancellationToken);

                UpdateProgress($"드라이펌프 상태 확인 중... ({waitCount + 1}/30)", (waitCount + 1) * 100 / 30);

                if (_mainForm._dryPump.Status?.IsRunning == true)
                {
                    LogInfo("드라이펌프 정상 작동 확인");
                    UpdateProgress("드라이펌프 시작 완료", 100);
                    return true;
                }

                waitCount++;
            }

            LogError("드라이펌프 시작 확인 시간 초과");
            return false;
        }

        /// <summary>
        /// 단계 4: 터보펌프 시작
        /// </summary>
        private async Task<bool> StartTurboPumpAsync(CancellationToken cancellationToken)
        {
            if (!_mainForm._turboPump?.IsConnected ?? true)
            {
                LogWarning("터보펌프가 연결되지 않았습니다. 단계를 건너뜁니다.");
                return true;
            }

            UpdateProgress("터보펌프 시작 조건 확인 중...", 0);

            // 이미 작동 중이면 건너뛰기
            if (_mainForm._turboPump.Status?.IsRunning == true)
            {
                LogInfo("터보펌프가 이미 작동 중입니다. 시작 단계를 건너뜁니다.");
                UpdateProgress("터보펌프 시작 완료 (이미 작동 중)", 100);
                return true;
            }

            var measurements = await GetCurrentMeasurementsAsync();

            // 밸브 상태 확인 — 올바르지 않으면 자동 설정 (초기화 단계 건너뛴 경우 대비)
            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogInfo("밸브 상태가 올바르지 않습니다. 자동 설정합니다...");

                if (measurements.GateValveStatus != "Opened")
                {
                    await _mainForm._ioModule.ControlGateValveAsync(true);
                    await Task.Delay(2000, cancellationToken);
                }
                if (measurements.VentValveStatus != "Closed")
                {
                    await _mainForm._ioModule.ControlVentValveAsync(false);
                    await Task.Delay(1000, cancellationToken);
                }
                if (measurements.ExhaustValveStatus != "Closed")
                {
                    await _mainForm._ioModule.ControlExhaustValveAsync(false);
                    await Task.Delay(1000, cancellationToken);
                }

                // 재확인
                measurements = await GetCurrentMeasurementsAsync();
                if (measurements.GateValveStatus != "Opened" ||
                    measurements.VentValveStatus != "Closed" ||
                    measurements.ExhaustValveStatus != "Closed")
                {
                    LogError("밸브 자동 설정 실패");
                    return false;
                }
                LogInfo("밸브 자동 설정 완료");
            }

            // 드라이펌프 상태 확인 — 꺼져있으면 시작 (초기화 단계 건너뛴 경우 대비)
            if (_mainForm._dryPump?.IsConnected == true && _mainForm._dryPump.Status?.IsRunning != true)
            {
                LogInfo("드라이펌프가 꺼져있습니다. 시작합니다...");
                for (int retry = 0; retry < 3; retry++)
                {
                    if (_mainForm._dryPump.Start()) break;
                    await Task.Delay(1000, cancellationToken);
                }

                bool dpStarted = false;
                for (int i = 0; i < 30; i++)
                {
                    await Task.Delay(1000, cancellationToken);
                    if (_mainForm._dryPump.Status?.IsRunning == true) { dpStarted = true; break; }
                    UpdateProgress($"드라이펌프 작동 대기 중... ({i + 1}/30)", 5);
                }
                if (!dpStarted)
                {
                    LogError("드라이펌프 시작 실패");
                    return false;
                }
                LogInfo("드라이펌프 정상 작동 확인");
            }
            else if (_mainForm._dryPump?.Status?.IsRunning != true)
            {
                LogError("드라이펌프가 작동하지 않습니다.");
                return false;
            }

            // 칠러 상태 확인 — 꺼져있으면 시작 (초기화 단계를 건너뛴 경우 대비)
            if (_mainForm._bathCirculator?.IsConnected == true)
            {
                bool chkOk = await Task.Run(() => _mainForm._bathCirculator.CheckStatusPriority());
                bool chillerRunning = chkOk && _mainForm._bathCirculator.Status?.IsRunning == true;

                if (!chillerRunning)
                {
                    LogInfo("칠러가 꺼져있습니다. 시작합니다...");
                    bool startResult = await Task.Run(() => _mainForm._bathCirculator.StartPriority());
                    LogInfo($"칠러 Start 결과: {startResult}");

                    // 시작 후 확인
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(1000, cancellationToken);
                        chkOk = await Task.Run(() => _mainForm._bathCirculator.CheckStatusPriority());
                        if (chkOk && _mainForm._bathCirculator.Status?.IsRunning == true)
                        {
                            chillerRunning = true;
                            break;
                        }
                        UpdateProgress($"칠러 작동 대기 중... ({i + 1}/10)", 10);
                    }

                    if (!chillerRunning)
                    {
                        LogError($"칠러 시작 실패. (checkOk={chkOk}, OperationReg={_mainForm._bathCirculator.Status?.OperationRegister})");
                        return false;
                    }
                    LogInfo("칠러 정상 작동 확인");
                }
            }

            UpdateProgress($"압력 확인 중... (현재: {measurements.CurrentPressure:E2} Torr)", 20);
            if (measurements.CurrentPressure > _config.TargetPressureForTurboPump)
            {
                LogInfo($"압력이 {_config.TargetPressureForTurboPump} Torr 이하가 될 때까지 대기 중...");

                int waitCount = 0;
                while (measurements.CurrentPressure > _config.TargetPressureForTurboPump && waitCount < 300)
                {
                    await Task.Delay(1000, cancellationToken);
                    measurements = await GetCurrentMeasurementsAsync();
                    UpdateProgress($"압력 대기 중... (현재: {measurements.CurrentPressure:E2} Torr)", 20 + (waitCount * 30 / 300));
                    waitCount++;
                }

                if (measurements.CurrentPressure > _config.TargetPressureForTurboPump)
                {
                    LogError($"압력이 목표값에 도달하지 못했습니다. (현재: {measurements.CurrentPressure:E2} Torr)");
                    return false;
                }
            }

            UpdateProgress("터보펌프 시작 중...", 50);

            // 시작 명령 전송 (최대 3회 재시도)
            bool tpStartSent = false;
            for (int retry = 0; retry < 3; retry++)
            {
                if (_mainForm._turboPump.Start())
                {
                    tpStartSent = true;
                    break;
                }
                LogWarning($"터보펌프 시작 명령 실패 (시도 {retry + 1}/3)");
                await Task.Delay(1000, cancellationToken);
            }

            if (!tpStartSent)
            {
                // 폴링 서비스가 갱신할 때까지 대기
                await Task.Delay(3000, cancellationToken);
                if (_mainForm._turboPump.Status?.IsRunning == true || _mainForm._turboPump.Status?.IsAccelerating == true)
                {
                    LogWarning("시작 명령 응답 실패했으나 터보펌프가 작동 중입니다.");
                }
                else
                {
                    LogError("터보펌프 시작 명령 실패");
                    return false;
                }
            }

            int speedWaitCount = 0;
            int targetSpeed = 620;
            bool igActivatedDuringAccel = false;

            // 폴링 서비스가 상태를 갱신하므로 UpdateStatusAsync 직접 호출 불필요
            while (speedWaitCount < 600)
            {
                await Task.Delay(1000, cancellationToken);

                var currentSpeed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                UpdateProgress($"터보펌프 가속 중... ({currentSpeed} Hz)", 50 + (currentSpeed * 50 / targetSpeed));

                // ── 가속 중 압력 모니터링 (IG 조기 활성화 + 히터 조기 진행) ──
                var currentMeasurements = await GetCurrentMeasurementsAsync();

                // DO 기반으로 IG HV 상태 확인 (PTR225 사용 시에만)
                if (!igActivatedDuringAccel && _mainForm._ionGauge?.Model == IonGaugeModel.PTR225
                    && _mainForm._ioModule?.IsConnected == true)
                {
                    var doData = _mainForm._ioModule.LastValidDOValues;
                    if (doData?.IsIonGaugeHVOn != true)
                    {
                        if (currentMeasurements.CurrentPressure > 0 &&
                            currentMeasurements.CurrentPressure <= _config.TargetPressureForIonGauge)
                        {
                            LogInfo($"터보펌프 가속 중 IG 활성화 압력 도달 ({currentMeasurements.CurrentPressure:E2} Torr) — 이온게이지 조기 활성화");
                            if (await _mainForm._ioModule.ControlIonGaugeHVAsync(true))
                            {
                                igActivatedDuringAccel = true;
                                LogInfo("이온게이지 HV ON 성공 (터보 가속 중)");
                            }
                        }
                    }
                    else
                    {
                        igActivatedDuringAccel = true; // 이미 켜져 있음
                    }
                }

                // ★ 히터 시작 목표 압력 도달 시 가속 완료를 기다리지 않고 조기 진행
                if (currentMeasurements.CurrentPressure > 0 &&
                    currentMeasurements.CurrentPressure <= _config.TargetPressureForHeater)
                {
                    LogInfo($"터보펌프 가속 중 히터 시작 압력 도달 ({currentMeasurements.CurrentPressure:E2} Torr ≤ {_config.TargetPressureForHeater:E2}) — 정격 속도 대기 생략, 다음 단계 진행");
                    UpdateProgress($"터보펌프 가속 중 ({currentSpeed} Hz) — 압력 조건 충족, 조기 진행", 100);
                    return true;
                }

                if (currentSpeed >= targetSpeed * 0.95)
                {
                    LogInfo($"터보펌프 정격 속도 도달: {currentSpeed} Hz");
                    UpdateProgress("터보펌프 시작 완료", 100);
                    return true;
                }

                speedWaitCount++;
            }

            LogError("터보펌프 정격 속도 도달 시간 초과");
            return false;
        }

        /// <summary>
        /// 단계 5: 이온게이지 활성화
        /// - 압력 미도달 시 대기 후 활성화
        /// - DO 상태로 HV ON 확인
        /// </summary>
        private async Task<bool> ActivateIonGaugeAsync(CancellationToken cancellationToken)
        {
            // DO 기반으로 IG HV 상태 확인
            // 이미 활성화되어 있으면 스킵 (터보펌프 가속 중 조기 활성화된 경우)
            var doCheck = _mainForm._ioModule?.LastValidDOValues;
            if (doCheck?.IsIonGaugeHVOn == true)
            {
                LogInfo("이온게이지 이미 활성화 상태 — 단계 스킵");
                UpdateProgress("이온게이지 활성화 완료 (이미 ON)", 100);
                return true;
            }

            UpdateProgress("이온게이지 활성화 조건 확인 중...", 0);

            var measurements = await GetCurrentMeasurementsAsync();
            double targetPressure = _config.TargetPressureForIonGauge;

            if (measurements.CurrentPressure > targetPressure)
            {
                LogInfo($"이온게이지 활성화 압력 대기 중... " +
                    $"(현재: {measurements.CurrentPressure:E2} Torr, 목표: {targetPressure:E2} Torr)");

                int waitCount = 0;
                int maxWait = _config.IonGaugeActivationTimeout;

                while (measurements.CurrentPressure > targetPressure && waitCount < maxWait)
                {
                    await Task.Delay(5000, cancellationToken);
                    measurements = await GetCurrentMeasurementsAsync();
                    waitCount += 5;

                    double progress = Math.Min((double)waitCount / maxWait * 40, 40);
                    UpdateProgress(
                        $"압력 대기 중... ({measurements.CurrentPressure:E2} / {targetPressure:E2} Torr)",
                        progress);
                }

                if (measurements.CurrentPressure > targetPressure)
                {
                    LogError($"이온게이지 활성화 압력 미도달 (타임아웃). " +
                        $"현재: {measurements.CurrentPressure:E2} Torr");
                    return false;
                }
            }

            LogInfo($"이온게이지 활성화 압력 도달: {measurements.CurrentPressure:E2} Torr");
            UpdateProgress("압력 조건 충족", 50);

            UpdateProgress("이온게이지 HV 켜는 중...", 60);
            if (!await _mainForm._ioModule.ControlIonGaugeHVAsync(true))
            {
                LogError("이온게이지 HV ON 실패");
                return false;
            }

            LogInfo("이온게이지 HV ON 명령 성공 — 안정화 대기 중...");
            await Task.Delay(3000, cancellationToken);

            // DO 기반으로 HV ON 확인
            UpdateProgress("이온게이지 상태 확인 중...", 85);

            for (int i = 0; i < 5; i++)
            {
                var doData = _mainForm._ioModule?.LastValidDOValues;
                if (doData?.IsIonGaugeHVOn == true)
                {
                    LogInfo("이온게이지 HV ON 확인 완료");
                    UpdateProgress("이온게이지 활성화 완료", 100);
                    return true;
                }
                await Task.Delay(1000, cancellationToken);
            }

            LogWarning("이온게이지 HV ON 명령은 성공했으나 DO 상태 확인 실패 — 계속 진행합니다.");
            UpdateProgress("이온게이지 활성화 완료 (상태 미확인)", 100);
            return true;
        }

        /// <summary>
        /// 단계 6: 고진공 대기
        /// </summary>
        private async Task<bool> WaitForHighVacuumAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("고진공 도달 대기 중...", 0);

            var startTime = DateTime.Now;
            var measurements = await GetCurrentMeasurementsAsync();

            while (measurements.CurrentPressure > _config.TargetPressureForHeater)
            {
                var elapsed = DateTime.Now - startTime;
                var progress = Math.Min(elapsed.TotalSeconds / _config.HighVacuumTimeout * 100, 99);

                UpdateProgress($"현재 압력: {measurements.CurrentPressure:E2} Torr (목표: {_config.TargetPressureForHeater:E2} Torr)", progress);

                await Task.Delay(5000, cancellationToken);
                measurements = await GetCurrentMeasurementsAsync();
            }

            LogInfo($"고진공 도달: {measurements.CurrentPressure:E2} Torr");
            UpdateProgress("고진공 도달 완료", 100);
            return true;
        }

        /// <summary>
        /// 단계 7: 히터 시작
        /// </summary>
        private async Task<bool> StartHeaterAsync(CancellationToken cancellationToken)
        {
            if (!_mainForm._tempController?.IsConnected ?? true)
            {
                LogWarning("온도 컨트롤러가 연결되지 않았습니다. 단계를 건너뜁니다.");
                return true;
            }

            // 실험 유형에 따른 목표 온도 결정
            double targetTemperature = IsBakeoutMode
                ? _config.BakeoutTargetTemperature
                : _config.HeaterCh1SetTemperature;

            UpdateProgress("히터 시작 조건 확인 중...", 0);

            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.CurrentPressure > _config.TargetPressureForHeater)
            {
                LogError($"압력이 너무 높습니다. (현재: {measurements.CurrentPressure:E2} Torr)");
                return false;
            }

            // CH1 온도 설정
            UpdateProgress("히터 CH1 온도 설정 중...", 10);

            await _mainForm._tempController.UpdateStatusAsync();

            var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];

            // ★ CH1 모니터링 시 TM4 하드웨어 램프 설정
            //   PI 비활성화 → TM4 자체 PID + 하드웨어 램프로 승온 제어
            //   PI 활성화 → 소프트웨어 램프(rampedTarget)가 처리 → 하드웨어 램프 불필요
            var monitorChannels = IsBakeoutMode ? _config.GetBakeoutMonitorChannels() : new List<int> { 1 };
            bool usePIFeedbackForRamp = IsBakeoutMode && monitorChannels.Any(ch => ch != 1);
            if (IsBakeoutMode && !usePIFeedbackForRamp && _config.BakeoutRampRate > 0)
            {
                // BakeoutRampRate(°C/h) → TM4 raw값 (Dot=1이면 ×10)
                ushort rampRaw = ch1Status.Dot == 1
                    ? (ushort)(_config.BakeoutRampRate * 10)
                    : (ushort)_config.BakeoutRampRate;

                if (_mainForm._tempController.SetRampConfiguration(1, rampRaw, 0, TempController.RampTimeUnit.Hour))
                {
                    double displayRate = ch1Status.Dot == 1 ? rampRaw / 10.0 : rampRaw;
                    LogInfo($"TM4 하드웨어 램프 설정: {displayRate}°C/h (raw: {rampRaw}, CH1 모니터링 모드)");
                }
                else
                {
                    LogWarning("TM4 하드웨어 램프 설정 실패 — TM4 기존 램프 설정으로 진행");
                }
            }
            else if (IsBakeoutMode && usePIFeedbackForRamp)
            {
                // PI 피드백 모드에서는 하드웨어 램프 비활성화 (PI가 SV를 덮어쓰므로)
                _mainForm._tempController.SetRampConfiguration(1, 0, 0, TempController.RampTimeUnit.Hour);
                LogInfo("TM4 하드웨어 램프 OFF (소프트웨어 램프 사용)");
            }
            else if (!IsBakeoutMode)
            {
                // ★ Outgassing: HeaterRampUpRate(°C/min) → TM4 하드웨어 램프 설정
                if (_config.HeaterRampUpRate > 0)
                {
                    double rampRatePerHour = _config.HeaterRampUpRate * 60;
                    ushort rampRaw = ch1Status.Dot == 1
                        ? (ushort)(rampRatePerHour * 10)
                        : (ushort)rampRatePerHour;
                    if (_mainForm._tempController.SetRampConfiguration(1, rampRaw, 0, TempController.RampTimeUnit.Hour))
                    {
                        LogInfo($"TM4 하드웨어 램프 설정: {rampRatePerHour:F0}°C/h ({_config.HeaterRampUpRate:F1}°C/min)");
                    }
                    else
                    {
                        LogWarning("TM4 하드웨어 램프 설정 실패 — TM4 기존 설정으로 진행");
                    }
                }
                else
                {
                    // HeaterRampUpRate=0 → 램프 비활성화 (즉시 가열)
                    _mainForm._tempController.SetRampConfiguration(1, 0, 0, TempController.RampTimeUnit.Hour);
                    LogInfo("TM4 하드웨어 램프 OFF (즉시 가열 모드)");
                }
            }

            short ch1SetValue = ch1Status.Dot == 1
                ? (short)(targetTemperature * 10)
                : (short)targetTemperature;

            // SetTemperature 호출 (Modbus 응답 검증 실패 가능하므로 반환값 무시)
            _mainForm._tempController.SetTemperature(1, ch1SetValue);
            LogInfo($"히터 CH1 온도 설정 명령 전송: {targetTemperature}°C (Dot:{ch1Status.Dot}, raw:{ch1SetValue})");

            // 500ms 대기 후 실제 SV 확인
            await Task.Delay(500, cancellationToken);
            await _mainForm._tempController.UpdateStatusAsync();

            var updatedCh1 = _mainForm._tempController.Status.ChannelStatus[0];
            double actualSV = updatedCh1.Dot == 1 ? updatedCh1.SetValue / 10.0 : updatedCh1.SetValue;

            if (Math.Abs(actualSV - targetTemperature) > 0.5)
            {
                LogWarning($"CH1 SV 불일치 감지 (설정:{targetTemperature}°C, 실제:{actualSV}°C), 재시도...");
                _mainForm._tempController.SetTemperature(1, ch1SetValue);
                await Task.Delay(500, cancellationToken);
                await _mainForm._tempController.UpdateStatusAsync();

                updatedCh1 = _mainForm._tempController.Status.ChannelStatus[0];
                actualSV = updatedCh1.Dot == 1 ? updatedCh1.SetValue / 10.0 : updatedCh1.SetValue;

                if (Math.Abs(actualSV - targetTemperature) > 0.5)
                {
                    LogError($"CH1 온도 설정 실패 (설정:{targetTemperature}°C, 실제:{actualSV}°C)");
                    return false;
                }
            }

            LogInfo($"히터 CH1 온도 설정 확인: SV={actualSV}°C (목표: {targetTemperature}°C)");

            await Task.Delay(1000, cancellationToken);

            // CH1 히터 시작
            UpdateProgress("히터 CH1 시작 중...", 50);
            if (!_mainForm._tempController.Start(1))
            {
                LogError("히터 CH1 시작 실패");
                return false;
            }

            await Task.Delay(3000, cancellationToken);
            UpdateProgress("히터 작동 상태 확인 중...", 80);

            await _mainForm._tempController.UpdateStatusAsync();
            var status = _mainForm._tempController.Status;

            if (status.ChannelStatus[0].IsRunning)
            {
                LogInfo("히터 CH1 정상 작동 확인");

                // 칠러 PID 제어 시작 (베이크아웃에서는 사용하지 않음)
                if (!IsBakeoutMode)
                {
                    StartChillerPID();
                }
                else
                {
                    LogInfo($"베이크아웃 모드 — 칠러 PID 건너뜀, 모니터 채널: {_config.GetBakeoutMonitorLabel()}");
                }

                UpdateProgress("히터 시작 완료", 100);
                return true;
            }

            LogError("히터 CH1 작동 확인 실패");
            return false;
        }

        /// <summary>
        /// 단계 8: 실험 진행
        /// </summary>
        private async Task<bool> RunExperimentAsync(CancellationToken cancellationToken)
        {
            // 실험 유형에 따른 파라미터 결정
            double targetTemperature = IsBakeoutMode
                ? _config.BakeoutTargetTemperature
                : _config.HeaterCh1SetTemperature;
            int holdMinutes = IsBakeoutMode
                ? _config.BakeoutHoldTimeMinutes
                : _config.ExperimentDurationMinutes;
            string experimentLabel = IsBakeoutMode ? "베이크아웃" : "실험";
            var monitorChannels = IsBakeoutMode ? _config.GetBakeoutMonitorChannels() : new List<int> { 1 };
            bool usePIFeedback = IsBakeoutMode && monitorChannels.Any(ch => ch != 1);
            string monitorLabel = IsBakeoutMode ? _config.GetBakeoutMonitorLabel() : "CH1";

            // ══════════════════════════════════════════════════════════
            //  1단계: 목표 온도 도달 (베이크아웃: PI 피드백 제어)
            // ══════════════════════════════════════════════════════════
            UpdateProgress("온도 도달 대기 중...", 0);
            LogInfo($"{monitorLabel} 목표 온도({targetTemperature:F1}°C) 도달 대기 중...");

            // PID 제어 상태 (베이크아웃 전용)
            // ★ 적응형 게인: 기본값을 관측된 열지연으로 스케일링
            //   열지연이 클수록(응답 느림) → Kp 감소, Kd 증가 (진동 방지, 선행 제동)
            //   열지연이 작을수록(응답 빠름) → Kp 증가, Kd 감소 (빠른 추종)
            const double Kp_base = 1.5;
            const double Ki_norm = 0.01;       // 정규화 적분 게인 (°C/회)
            const double Kd_base = 3.0;
            double Kp = Kp_base;               // 아래에서 열지연 기반으로 조정
            double Kd = Kd_base;
            double observedThermalLag = 0;     // 관측된 열 지연 (CH1 PV − 샘플) — 적응형 감속 구간용 (아래에서 실측 초기화)
            double smoothedRate = 0;           // 변화율 이동평균 (노이즈 필터)
            double integralTerm = 0;           // 적분 기여값 (°C)
            double maxIntegral = IsBakeoutMode
                ? _config.BakeoutHeaterMaxTemperature - targetTemperature
                : 0;
            double lastCh1Setpoint = targetTemperature;
            double feedbackIntervalSec = IsBakeoutMode && _config.BakeoutFeedbackIntervalSec > 0
                ? _config.BakeoutFeedbackIntervalSec : 5.0;

            // ★ [#4 해결] 센서 이상값 감지용 이전 온도
            double prevMonitorTemp = double.NaN;
            const double maxTempJump = 30.0;     // 1회 루프에서 허용 최대 변화량 (°C)
            const double minValidTemp = -10.0;   // 유효 최저 온도
            int sensorAnomalyCount = 0;
            const int maxSensorAnomalies = 3;    // 연속 이상 허용 횟수

            // ★ [#5 해결] CH1 PV 응답 추적
            int ch1NoResponseCount = 0;
            const int maxCh1NoResponse = 12;     // 60초 (5초×12) 동안 응답 없으면 경고

            // ★ [#1 해결] CH1 상한 포화 조기 감지 (stall detection)
            int stallCount = 0;
            const int maxStallCount = 60;        // 300초 (5초×60) 동안 상한 포화 + 샘플 정체 시 실패
            double stallBaselineTemp = 0;

            // ★ [Fix#6] TempController 통신 끊김 감지
            int disconnectCount = 0;
            const int maxDisconnectCount = 6;    // 30초 (5초×6) 연속 끊김 시 실패

            // ★ SetTemperature 연속 실패 카운터 (통신 오류 vs 실제 거부 구분)
            int setTempFailCount = 0;
            const int maxSetTempFail = 5;        // 5회 연속 실패 시 경고

            // ★ 진공 과압 대응 카운터 — PressureInterlockDurationSeconds 기반 동적 임계값
            int overpressureCount = 0;
            int maxOverpressureWarning;  // N회 연속 → CH1 SV 감소
            int maxOverpressureAbort;    // N회 연속 → 중단
            if (_config.PressureInterlockDurationSeconds > 0)
            {
                // 사용자 지정 duration: SV감소 = duration, 중단 = duration × 2
                maxOverpressureWarning = Math.Max(1, (int)(_config.PressureInterlockDurationSeconds / feedbackIntervalSec));
                maxOverpressureAbort = maxOverpressureWarning * 2;
            }
            else
            {
                // 기본값: SV감소 15초(3회), 중단 30초(6회)
                maxOverpressureWarning = 3;
                maxOverpressureAbort = 6;
            }
            bool svReducedForOverpressure = false;  // 과압으로 SV 감소 상태 추적 (비PI 모드 복원용)

            // 동적 타임아웃
            await _mainForm._tempController.UpdateStatusAsync();
            double initialMonitorTemp = !usePIFeedback
                ? _mainForm._tempController.Status.ChannelStatus[0].PresentValue
                    / (_mainForm._tempController.Status.ChannelStatus[0].Dot == 1 ? 10.0 : 1.0)
                : GetMonitorChannelTemperature();

            // ★ observedThermalLag 초기화: 현재 CH1 PV와 샘플 온도 차이로 실측
            if (usePIFeedback)
            {
                var ch1Init = _mainForm._tempController.Status.ChannelStatus[0];
                double ch1PvInit = ch1Init.Dot == 1 ? ch1Init.PresentValue / 10.0 : ch1Init.PresentValue;
                observedThermalLag = Math.Max(0, ch1PvInit - initialMonitorTemp);
                if (observedThermalLag > 0)
                    LogInfo($"초기 열지연 실측: {observedThermalLag:F1}°C (CH1={ch1PvInit:F1}, 샘플={initialMonitorTemp:F1})");
            }

            double tempGap = Math.Max(targetTemperature - initialMonitorTemp, 1);
            int riseTimeoutMinutes;
            if (IsBakeoutMode && _config.BakeoutRiseTimeoutMinutes > 0)
            {
                // 사용자 지정 타임아웃
                riseTimeoutMinutes = _config.BakeoutRiseTimeoutMinutes;
            }
            else if (IsBakeoutMode && _config.BakeoutRampRate > 0)
            {
                // 자동 계산: 램프 속도 기반 × 3 + 30분 (최소 60분)
                double estimatedHours = tempGap / _config.BakeoutRampRate;
                riseTimeoutMinutes = Math.Max(60, (int)(estimatedHours * 60 * 3) + 30);
            }
            else
            {
                // Outgassing: HeaterRampUpRate 기반 동적 계산 (기본 최소 60분)
                if (_config.HeaterRampUpRate > 0)
                {
                    double estimatedMinutes = tempGap / _config.HeaterRampUpRate;
                    riseTimeoutMinutes = Math.Max(60, (int)(estimatedMinutes * 3) + 30);
                }
                else
                {
                    riseTimeoutMinutes = 60;
                }
            }
            LogInfo($"승온 타임아웃: {riseTimeoutMinutes}분 (온도 차이: {tempGap:F1}°C, 초기: {initialMonitorTemp:F1}°C{(_config.BakeoutRiseTimeoutMinutes > 0 ? ", 수동설정" : ", 자동계산")})");

            // ★ [Fix#1] 하드웨어 온도 상한 확인 — 모든 모드에서 목표 온도 vs 하드웨어 상한 검증
            double effectiveMaxTemp = _config.BakeoutHeaterMaxTemperature;
            if (_mainForm._tempController?.IsConnected == true)
            {
                var ch1StatusForMax = _mainForm._tempController.Status.ChannelStatus[0];
                double hardwareMaxTemp = ch1StatusForMax.Dot == 1
                    ? _mainForm._tempController.MaxTemperatureRaw / 10.0
                    : _mainForm._tempController.MaxTemperatureRaw;

                if (IsBakeoutMode)
                {
                    if (effectiveMaxTemp > hardwareMaxTemp)
                    {
                        LogWarning($"설정 상한({effectiveMaxTemp:F1}°C) > 하드웨어 상한({hardwareMaxTemp:F1}°C) — {hardwareMaxTemp:F1}°C로 제한");
                        effectiveMaxTemp = hardwareMaxTemp;
                    }
                    // maxIntegral도 실제 상한 기준으로 재계산
                    maxIntegral = effectiveMaxTemp - targetTemperature;
                    if (maxIntegral < 0) maxIntegral = 0;
                    LogInfo($"유효 CH1 상한: {effectiveMaxTemp:F1}°C, 최대 적분: {maxIntegral:F1}°C");
                }

                // ★ [Fix#12] 목표 온도가 하드웨어 상한 이하인지 조기 검증 (모든 모드)
                if (targetTemperature > hardwareMaxTemp)
                {
                    LogError($"목표 온도({targetTemperature:F1}°C)가 하드웨어 상한({hardwareMaxTemp:F1}°C)을 초과 — 도달 불가능.");
                    return false;
                }
            }

            // ★ 소프트웨어 램프: 샘플 온도 상승률을 BakeoutRampRate 이내로 제어
            //   PI 출력을 직접 제한하는 대신, 램프 기준선(rampedTarget)을 시간에 따라 올림
            //   rampedTarget = 초기 샘플온도 + (경과시간 × 램프속도)
            //   PI의 목표를 rampedTarget으로 치환 → 샘플이 램프 속도 이상 올라가지 않음
            double rampRatePerSec = IsBakeoutMode && _config.BakeoutRampRate > 0
                ? _config.BakeoutRampRate / 3600.0
                : double.MaxValue;
            if (IsBakeoutMode && _config.BakeoutRampRate > 0)
                LogInfo($"소프트웨어 램프: {_config.BakeoutRampRate:F0}°C/h ({rampRatePerSec:F4}°C/s), 초기 샘플: {initialMonitorTemp:F1}°C");

            // ★ CH1 자기참조 감지: 모니터 채널이 CH1만이면 PI 비활성화 (TM4 자체 PID 사용)
            if (IsBakeoutMode && !usePIFeedback)
                LogInfo("모니터 채널 = CH1 → PI 피드백 비활성화 (TM4 내장 PID로 직접 제어)");
            else if (IsBakeoutMode)
                LogInfo($"모니터 채널: {monitorLabel} → PI 피드백 활성화 (MAX 기반 제어)");

            // ★ [Fix#4] CH1이 이미 가열된 상태에서 재시작 시 초기 적분 추정
            //   조건 완화: 샘플이 타겟 근처가 아니어도, CH1이 샘플보다 높으면 적분 추정
            //   → 중지 후 재시작 시 CH1을 갑자기 낮추지 않고 현재 상태 유지
            if (usePIFeedback)
            {
                var ch1StatusInit = _mainForm._tempController.Status.ChannelStatus[0];
                double ch1PVInit = ch1StatusInit.Dot == 1
                    ? ch1StatusInit.PresentValue / 10.0
                    : ch1StatusInit.PresentValue;
                double observedOffset = ch1PVInit - initialMonitorTemp;
                if (observedOffset > 2)
                {
                    // CH1이 이미 뜨거움 → 현재 CH1 위치를 유지하도록 적분 초기화
                    integralTerm = Math.Min(observedOffset, maxIntegral);
                    lastCh1Setpoint = Math.Min(ch1PVInit, effectiveMaxTemp);

                    // ★ [Fix#7] 추정 SV를 실제 컨트롤러에도 전송 (현재 CH1 PV 유지)
                    short rawApply = ch1StatusInit.Dot == 1
                        ? (short)(lastCh1Setpoint * 10)
                        : (short)lastCh1Setpoint;
                    bool applyOk = _mainForm._tempController.SetTemperaturePriority(1, rawApply);
                    if (!applyOk)
                    {
                        await Task.Delay(200, cancellationToken);
                        applyOk = _mainForm._tempController.SetTemperaturePriority(1, rawApply);
                    }
                    if (applyOk)
                    {
                        LogInfo($"초기 적분 추정 (CH1 유지): I={integralTerm:F1}°C, CH1 SV={lastCh1Setpoint:F1}°C (CH1 PV:{ch1PVInit:F1}°C, 샘플:{initialMonitorTemp:F1}°C, ΔT:{observedOffset:F1}°C)");
                    }
                    else
                    {
                        LogWarning($"초기 적분 SV 적용 실패 (raw:{rawApply}) — PI가 자동 보정 예정");
                        lastCh1Setpoint = targetTemperature;
                        integralTerm = 0;
                    }
                }
            }

            bool temperatureReached = false;
            var waitStartTime = DateTime.Now;
            double reachTolerance = _config.BakeoutTolerance > 0 ? _config.BakeoutTolerance : 1.0;
            // ★ 안정화 유지 시간: 목표±허용오차 범위를 이 시간 동안 연속 유지해야 도달로 판정
            int stabilizationRequired = IsBakeoutMode ? _config.BakeoutStabilizationSeconds : 0;
            int stableCount = 0; // 안정 범위 내 연속 체크 횟수
            int stableCountRequired = stabilizationRequired > 0
                ? Math.Max(1, (int)Math.Ceiling(stabilizationRequired / feedbackIntervalSec))
                : 0;
            double _prevStableSV = lastCh1Setpoint;
            if (stabilizationRequired > 0)
                LogInfo($"안정화 유지 조건: 목표±{reachTolerance:F1}°C 범위에서 {stabilizationRequired}초 연속 유지 후 홀드 시작 (SV 변동 <0.5°C/사이클)");
            // 도달 가능성 예측: 초기 5분간 승온율 관측
            double predictBaseline = double.NaN;
            double predictBaselineTime = 0;
            bool predictChecked = false;

            while (!temperatureReached && (DateTime.Now - waitStartTime).TotalMinutes < riseTimeoutMinutes)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var loopStart = DateTime.Now;

                // 일시정지 처리
                if (_isPaused)
                {
                    LogInfo("승온 중 일시정지됨");
                    var pauseStart = DateTime.Now;
                    while (_isPaused && !cancellationToken.IsCancellationRequested)
                        await Task.Delay(500, cancellationToken);
                    var pauseDuration = DateTime.Now - pauseStart;
                    waitStartTime += pauseDuration;
                    LogInfo($"승온 재개 (정지 시간: {pauseDuration:mm\\:ss})");
                }

                var measurements = await GetCurrentMeasurementsAsync();
                // ★ [Fix#13] UpdateStatusAsync 제거 — 폴링 서비스가 ~1초 간격으로 Status 갱신 중
                //   UpdateStatusAsync는 5채널×(레지스터+코일+Ramp) = 11 Modbus요청 + 폴링 경합 → 10~20초 소요
                //   GetCurrentMeasurementsAsync가 이미 폴링 캐시의 Status를 읽으므로 중복 불필요

                // ★ [Fix#6] TempController 통신 끊김 감지
                if (_mainForm._tempController?.IsConnected != true)
                {
                    disconnectCount++;
                    LogWarning($"[경고] 온도 컨트롤러 통신 끊김 [{disconnectCount}/{maxDisconnectCount}] — 재연결 대기 중");
                    if (disconnectCount >= maxDisconnectCount)
                    {
                        LogError($"[중단] 온도 컨트롤러 연결 끊김 {maxDisconnectCount}회 연속 → 안전 종료 실행");
                        return false;
                    }
                    // ★ [Fix#9] 재연결 후 jump 오판 방지 — prevMonitorTemp 리셋
                    prevMonitorTemp = double.NaN;
                    sensorAnomalyCount = 0;
                    await Task.Delay((int)(feedbackIntervalSec * 1000), cancellationToken);
                    continue;
                }
                disconnectCount = 0;

                double monitorTemp = !usePIFeedback
                    ? measurements.HeaterCh1Temperature
                    : GetMonitorChannelTemperature();

                // ★ [#4] 센서 에러 플래그 체크 (모든 모드 — 모니터 채널 전체 에러 시 중단)
                {
                    var chStatus = _mainForm._tempController.Status.ChannelStatus;
                    int sensorErrorCount = 0;
                    foreach (int ch in monitorChannels)
                    {
                        int idx = ch - 1;
                        if (idx >= 0 && idx < chStatus.Length && !string.IsNullOrEmpty(chStatus[idx].SensorError))
                        {
                            sensorErrorCount++;
                            LogWarning($"CH{ch} 센서 에러: {chStatus[idx].SensorError}");
                        }
                    }
                    if (sensorErrorCount >= monitorChannels.Count)
                    {
                        LogError($"[중단] {monitorLabel} 전체 센서 에러 → 안전 종료 실행");
                        return false;
                    }
                }

                // ★ 센서 읽기 실패 (0 반환) 또는 NaN 체크
                if (double.IsNaN(monitorTemp) || (usePIFeedback && monitorTemp <= 0))
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] {monitorLabel} 무효 온도: {monitorTemp:F1}°C [{sensorAnomalyCount}/{maxSensorAnomalies}] — 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] {monitorLabel} 무효 온도 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    await Task.Delay((int)(feedbackIntervalSec * 1000), cancellationToken);
                    continue;
                }

                // ★ [#4] 센서 이상값 체크 (모든 모드 — 에러 플래그 없이 비정상 값)
                if (monitorTemp < minValidTemp)
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] {monitorLabel} 이상 온도: {monitorTemp:F1}°C (하한: {minValidTemp}°C) [{sensorAnomalyCount}/{maxSensorAnomalies}] — 이번 사이클 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] {monitorLabel} 센서 이상값 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    await Task.Delay((int)(feedbackIntervalSec * 1000), cancellationToken);
                    continue;
                }
                if (!double.IsNaN(prevMonitorTemp) && Math.Abs(monitorTemp - prevMonitorTemp) > maxTempJump)
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] {monitorLabel} 급격한 변화: {prevMonitorTemp:F1} → {monitorTemp:F1}°C [{sensorAnomalyCount}/{maxSensorAnomalies}] — 이번 사이클 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] {monitorLabel} 센서 이상 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    await Task.Delay((int)(feedbackIntervalSec * 1000), cancellationToken);
                    continue;
                }
                sensorAnomalyCount = 0;

                // ★ 승온 중 압력 체크 — 과압 시 실제 대응
                if (measurements.CurrentPressure > _config.MaxPressureDuringExperiment)
                {
                    overpressureCount++;
                    if (overpressureCount >= maxOverpressureAbort)
                    {
                        LogError($"[중단] 압력 초과 {overpressureCount}회 연속 ({measurements.CurrentPressure:E2} Torr) → 안전 종료 실행");
                        return false;
                    }
                    if (overpressureCount >= maxOverpressureWarning
                        && _mainForm._tempController?.IsConnected == true)
                    {
                        // CH1 SV를 10°C 감소시켜 가스 방출 억제 (모든 모드)
                        double reducedSV = Math.Max(targetTemperature * 0.8, lastCh1Setpoint - 10);
                        var ch1St = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawReduced = ch1St.Dot == 1 ? (short)(reducedSV * 10) : (short)reducedSV;
                        bool svOk = _mainForm._tempController.SetTemperaturePriority(1, rawReduced);
                        if (svOk)
                        {
                            LogWarning($"[조치] 압력 초과 {overpressureCount}회 — CH1 SV 감소: {lastCh1Setpoint:F1} → {reducedSV:F1}°C ({maxOverpressureAbort}회 시 중단)");
                            lastCh1Setpoint = reducedSV;
                            if (usePIFeedback) integralTerm = Math.Max(0, integralTerm - 10);
                            svReducedForOverpressure = true;
                        }
                    }
                    else
                    {
                        LogWarning($"[경고] 승온 중 압력 초과: {measurements.CurrentPressure:E2} Torr [{overpressureCount}/{maxOverpressureAbort}] — 이번 사이클 피드백 건너뜀");
                    }
                    await Task.Delay((int)(feedbackIntervalSec * 1000), cancellationToken);
                    continue;
                }
                else
                {
                    if (overpressureCount > 0)
                    {
                        LogInfo($"압력 정상 복귀: {measurements.CurrentPressure:E2} Torr (초과 {overpressureCount}회 후 해제)");
                        overpressureCount = 0;
                    }
                    // ★ 비PI 모드: 과압으로 감소시킨 SV 복원
                    if (svReducedForOverpressure && !usePIFeedback
                        && _mainForm._tempController?.IsConnected == true)
                    {
                        var ch1St = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawTarget = ch1St.Dot == 1 ? (short)(targetTemperature * 10) : (short)targetTemperature;
                        if (_mainForm._tempController.SetTemperaturePriority(1, rawTarget))
                        {
                            LogInfo($"[조치] 압력 정상 — CH1 SV 복원: {lastCh1Setpoint:F1} → {targetTemperature:F1}°C");
                            lastCh1Setpoint = targetTemperature;
                        }
                        svReducedForOverpressure = false;
                    }
                }

                // ── 베이크아웃 PI 피드백 제어 (monitorCh ≠ 1일 때만) ──
                if (usePIFeedback && _mainForm._tempController?.IsConnected == true)
                {
                    // ★ 램프 기준선: 시간에 따라 목표를 점진적으로 올림
                    //   rampedTarget = 초기 샘플온도 + (경과시간 × 램프속도)
                    //   PI는 rampedTarget을 추종 → 샘플 상승률이 자연스럽게 제한됨
                    //   PI 출력(CH1 SV)은 자유롭게 움직일 수 있어 열 전달 병목에도 빠르게 대응
                    double elapsedSec = (DateTime.Now - waitStartTime).TotalSeconds;
                    double rampedTarget = rampRatePerSec < double.MaxValue
                        ? Math.Min(initialMonitorTemp + rampRatePerSec * elapsedSec, targetTemperature)
                        : targetTemperature;

                    double error = rampedTarget - monitorTemp;

                    // ★ 변화율: 이동평균으로 노이즈 필터링 (α=0.3)
                    double rawRate = double.IsNaN(prevMonitorTemp) ? 0
                        : (monitorTemp - prevMonitorTemp);
                    smoothedRate = smoothedRate * 0.7 + rawRate * 0.3;
                    double rateOfChange = smoothedRate;

                    double integralGain = Ki_norm;
                    if (rateOfChange > 0.5 && error > 0)
                        integralGain *= 0.3;

                    // ★ 적응형 감속: 현재 열 지연 추적 (빠른 감쇠)
                    double distanceToTarget = targetTemperature - monitorTemp;
                    double ch1PVNow = measurements.HeaterCh1Temperature;
                    double currentLag = Math.Max(0, ch1PVNow - monitorTemp);
                    double decelerationZone = Math.Max(5.0, currentLag * 1.5);
                    // 근접도: 목표에 가까울수록 1, 멀수록 0
                    double proximity = decelerationZone > 0
                        ? Math.Max(0, 1.0 - distanceToTarget / decelerationZone) : 0;

                    // 빠른 추적: 증가 시 즉시, 감소 시 EMA
                    //   목표 근처(proximity>0.5)에서는 감쇠를 늦춰 과도기 SV 하한 보호
                    if (currentLag > observedThermalLag && rateOfChange > 0.05)
                        observedThermalLag = currentLag;
                    else
                    {
                        double emaAlpha = proximity > 0.5 ? 0.005 : 0.02;
                        observedThermalLag = observedThermalLag * (1 - emaAlpha) + currentLag * emaAlpha;
                    }

                    // ★ 적응형 게인 스케일링: 열지연 기반
                    //   기준: 열지연 20°C → 게인 1.0배 (기본)
                    //   열지연 40°C → Kp 0.7배, Kd 1.4배 (느린 시스템 → 진동 방지)
                    //   열지연 10°C → Kp 1.3배, Kd 0.7배 (빠른 시스템 → 빠른 추종)
                    double lagScale = observedThermalLag > 1 ? 20.0 / observedThermalLag : 1.0;
                    lagScale = Math.Max(0.5, Math.Min(lagScale, 2.0)); // 0.5~2.0배 제한
                    Kp = Kp_base * Math.Sqrt(lagScale);   // 제곱근으로 완만하게
                    Kd = Kd_base / Math.Sqrt(lagScale);

                    // ★ 축적 에너지 분석: 현재 열 지연 기반 (과도 상태 인플레이션 방지)
                    double storedEnergy = ch1PVNow - (targetTemperature + currentLag);

                    if (error > 0)
                    {
                        double growthRate = integralGain;
                        // 감속구간 억제: 초과 에너지가 있을 때만 적용
                        if (storedEnergy > 0 && decelerationZone > 0 && distanceToTarget > 0 && distanceToTarget < decelerationZone)
                            growthRate *= distanceToTarget / decelerationZone;
                        if (storedEnergy > 0 && proximity > 0.5)
                            growthRate *= (1.0 - proximity);
                        integralTerm += error * growthRate;
                    }
                    else
                    {
                        // ★ 언더슛 방지: 음의 적분 속도를 완화 (3x → 1.5x)
                        integralTerm += error * Ki_norm * 1.5;
                    }

                    // ★ 축적 에너지 기반 적분 감쇠: 유의미한 초과 에너지(>2°C)가 있을 때만
                    //   평형 근처(storedEnergy≈0)에서는 감쇠하지 않아 적분 유지
                    if (storedEnergy > 2.0 && proximity > 0.3 && integralTerm > 0)
                    {
                        double decayRate = ((storedEnergy - 2.0) / Math.Max(1, observedThermalLag)) * proximity;
                        decayRate = Math.Min(decayRate, 0.05); // 1사이클당 최대 5% 감쇠
                        integralTerm *= (1.0 - decayRate);
                    }
                    // ★ 정상상태에서 열지연 보상을 위한 적분 최소값 유지
                    double minIntegralForLag = proximity > 0.3 ? observedThermalLag * 0.85 : 0;
                    integralTerm = Math.Max(minIntegralForLag, Math.Min(integralTerm, maxIntegral));

                    // ★ 모델 기반 적분 수렴 가속: 적분이 추정값보다 낮을 때만 빠르게 올림
                    //   추정값보다 높을 때는 PI 자체 조절에 맡김 (잘못된 추정으로 하향 방지)
                    if (proximity > 0.5 && observedThermalLag > 1)
                    {
                        double estimatedSteadyIntegral = observedThermalLag * 0.9;
                        if (integralTerm < estimatedSteadyIntegral - 1.0)
                        {
                            double boostGap = estimatedSteadyIntegral - integralTerm;
                            integralTerm += boostGap * 0.2;
                            integralTerm = Math.Max(minIntegralForLag, Math.Min(integralTerm, maxIntegral));
                        }
                    }

                    // ★ D항: 양방향 — 상승 시 억제, 하강 시 복구 가속
                    double dTerm = -Kd * rateOfChange;

                    // ★ 축적 에너지 보상: 근접도에 비례해 점진적 적용
                    //   과도한 보상은 언더슛을 유발하므로 계수를 완화 (0.5 → 0.15)
                    double energyCompensation = 0;
                    if (storedEnergy > 0 && proximity > 0.3)
                        energyCompensation = -storedEnergy * 0.15 * proximity;

                    double newCh1Setpoint = rampedTarget + error * Kp + integralTerm + dTerm + energyCompensation;

                    // ★ NaN 방어: PI 계산 결과가 비정상이면 이전 SV 유지
                    if (double.IsNaN(newCh1Setpoint) || double.IsInfinity(newCh1Setpoint))
                    {
                        LogWarning($"[경고] PI 출력 NaN/Inf — 이전 SV {lastCh1Setpoint:F1}°C 유지 (e={error:F1} I={integralTerm:F1} D={dTerm:F1} E={energyCompensation:F1})");
                        newCh1Setpoint = lastCh1Setpoint;
                    }

                    // 상한: 히터 최대 온도 (ΔT 제한은 더 이상 사용하지 않음)
                    double upperLimit = effectiveMaxTemp;
                    // ★ SV 하한: 정상상태에서 CH1은 목표+열지연 이상이어야 함
                    //   승온 중 관측된 열지연을 기반으로 정상상태 SV를 추정
                    double estimatedSteadyStateSV = rampedTarget + observedThermalLag * 0.85;
                    double svLowerBound = Math.Max(estimatedSteadyStateSV,
                        monitorTemp - currentLag * 0.3);
                    newCh1Setpoint = Math.Max(svLowerBound,
                        Math.Min(newCh1Setpoint, upperLimit));

                    // ★ SV 감소 속도 제한: 3단계
                    if (newCh1Setpoint < lastCh1Setpoint)
                    {
                        if (monitorTemp < targetTemperature)
                        {
                            // 목표 미만 → 완전 차단
                            newCh1Setpoint = lastCh1Setpoint;
                        }
                        else if (monitorTemp <= targetTemperature + reachTolerance)
                        {
                            // 목표~목표+허용오차 → 미세 감소 (0.1°C/사이클)
                            newCh1Setpoint = Math.Max(newCh1Setpoint, lastCh1Setpoint - 0.1);
                        }
                        else
                        {
                            // 오버슈트 → 서서히 감소 (0.3°C/사이클)
                            newCh1Setpoint = Math.Max(newCh1Setpoint, lastCh1Setpoint - 0.3);
                        }
                    }

                    // ★ 열 특성 계수 갱신 (UI 표시용)
                    double heatingMV = _mainForm._tempController.Status.ChannelStatus[0].HeatingMV;
                    ThermalParams.ThermalLag = observedThermalLag;
                    ThermalParams.CurrentLag = currentLag;
                    ThermalParams.SampleRate = smoothedRate;
                    ThermalParams.Kp = Kp;
                    ThermalParams.Kd = Kd;
                    ThermalParams.IntegralTerm = integralTerm;
                    ThermalParams.StoredEnergy = storedEnergy;
                    ThermalParams.Proximity = proximity;
                    ThermalParams.EstimatedSteadyStateSV = estimatedSteadyStateSV;
                    // 시정수 추정: 열지연 / 변화율 (변화율 > 0일 때만 유의미)
                    ThermalParams.EstimatedThermalTimeConstant = smoothedRate > 0.01
                        ? observedThermalLag / smoothedRate * feedbackIntervalSec : 0;
                    // 열저항 추정: 열지연 / 히터출력% (상대값)
                    ThermalParams.EstimatedThermalResistance = heatingMV > 1
                        ? observedThermalLag / (heatingMV / 10.0) : 0;

                    // ★ [#1] CH1 상한 포화 + 샘플 정체 감지
                    //   목표까지 거리가 멀 때만 정체로 판정 (error > 10°C)
                    //   목표 근처에서는 열평형에 의해 승온 속도가 자연스럽게 느려지므로 정체가 아님
                    bool isAtMaxLimit = newCh1Setpoint >= effectiveMaxTemp - 1;
                    if (isAtMaxLimit && error > 10)
                    {
                        if (stallCount == 0) stallBaselineTemp = monitorTemp;
                        stallCount++;
                        if (stallCount >= maxStallCount)
                        {
                            double stallProgress = monitorTemp - stallBaselineTemp;
                            // 정체 기준: 목표까지 거리의 10% 미만 상승 (최소 1°C)
                            double minProgress = Math.Max(1.0, error * 0.1);
                            if (stallProgress < minProgress)
                            {
                                LogError($"[중단] CH1 상한({effectiveMaxTemp:F0}°C) 포화 + 샘플 정체 ({stallProgress:F1}°C/5분, 필요:{minProgress:F1}°C) — 열 전달 부족 → 안전 종료 실행");
                                return false;
                            }
                            else
                            {
                                LogWarning($"CH1 상한 근접, 샘플 느린 상승 중 ({stallProgress:F1}°C/5분)");
                            }
                            stallCount = 0;
                        }
                    }
                    else
                    {
                        stallCount = 0;
                    }

                    // ★ 도달 가능성 조기 예측 (초기 10분 후 판정)
                    double elapsedMin = (DateTime.Now - waitStartTime).TotalMinutes;
                    if (double.IsNaN(predictBaseline) && elapsedMin >= 2)
                    {
                        predictBaseline = monitorTemp;
                        predictBaselineTime = elapsedMin;
                    }
                    if (!predictChecked && !double.IsNaN(predictBaseline) && elapsedMin >= predictBaselineTime + 10)
                    {
                        predictChecked = true;
                        double tempRise = monitorTemp - predictBaseline;
                        double minutesElapsed = elapsedMin - predictBaselineTime;
                        double ratePerMin = tempRise / minutesElapsed; // °C/min

                        if (ratePerMin > 0.01 && distanceToTarget > 0)
                        {
                            // 1차 지연 시스템: 승온율은 목표에 가까울수록 감소
                            // 현재 승온율이 계속 유지된다 가정해도 도달 불가 시 → 실제론 더 느림
                            double estimatedMinutes = distanceToTarget / ratePerMin;
                            // 히터가 이미 최대 근처인데 승온율이 낮으면 → 평형 예측
                            if (ch1PVNow >= effectiveMaxTemp - 5)
                            {
                                // 지수 감쇠 모델: T(t) = T_eq - (T_eq - T_now) × e^(-t/τ_eff)
                                // τ_eff 추정: 현재 rate = (T_eq - T_now) / τ_eff → τ_eff = (T_eq - T_now) / rate
                                // rate가 감소 중이면 T_eq ≈ T_now + rate × τ_eff
                                // 간단 추정: 현재 rate와 남은 거리로 평형 추정
                                // τ_eff 추정: 현재 열 지연과 승온율로 간접 추정
                                double tauEstMin = (currentLag > 1 && ratePerMin > 0) ? currentLag / ratePerMin : 120;
                                double estimatedEquilibrium = monitorTemp + ratePerMin * tauEstMin;
                                if (estimatedEquilibrium < targetTemperature - reachTolerance)
                                {
                                    LogWarning($"[예측 경고] 히터 최대({effectiveMaxTemp:F0}°C) 근접 상태에서 승온율 {ratePerMin:F3}°C/min");
                                    LogWarning($"  예상 평형 온도: ~{estimatedEquilibrium:F1}°C (목표: {targetTemperature:F1}°C) — 도달 불가 가능성");
                                    LogWarning($"  히터 최대 온도 상향 또는 방열 조건 개선 필요");
                                }
                            }
                        }
                        else if (ratePerMin <= 0.01 && distanceToTarget > 5)
                        {
                            LogWarning($"[예측 경고] 10분간 승온율 {ratePerMin:F4}°C/min — 사실상 정체. 목표 도달 어려움 예상");
                        }
                    }

                    // ★ [#5] CH1 PV 응답 검증
                    double ch1PV = measurements.HeaterCh1Temperature;
                    if (lastCh1Setpoint > targetTemperature + 10 && ch1PV < targetTemperature * 0.5)
                    {
                        ch1NoResponseCount++;
                        if (ch1NoResponseCount >= maxCh1NoResponse)
                        {
                            LogError($"[중단] CH1 무응답 {maxCh1NoResponse}회: SV={lastCh1Setpoint:F1}°C, PV={ch1PV:F1}°C — 히터 이상 → 안전 종료 실행");
                            return false;
                        }
                    }
                    else
                    {
                        ch1NoResponseCount = 0;
                    }

                    // ★ [Fix#2] 유의미한 변경(>0.5°C)만 전송 + 반환값 검증
                    if (Math.Abs(newCh1Setpoint - lastCh1Setpoint) > 0.2)
                    {
                        var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawValue = ch1Status.Dot == 1
                            ? (short)(newCh1Setpoint * 10)
                            : (short)newCh1Setpoint;
                        bool setOk = _mainForm._tempController.SetTemperaturePriority(1, rawValue);
                        if (!setOk)
                        {
                            await Task.Delay(200, cancellationToken);
                            setOk = _mainForm._tempController.SetTemperaturePriority(1, rawValue);
                        }
                        if (setOk)
                        {
                            setTempFailCount = 0;
                            LogInfo($"피드백: CH1 {lastCh1Setpoint:F1} → {newCh1Setpoint:F1}°C (램프목표:{rampedTarget:F1} P:{error * Kp:F1} I:{integralTerm:F1} D:{dTerm:F1} 샘플:{monitorTemp:F1}°C)");
                            lastCh1Setpoint = newCh1Setpoint;
                        }
                        else
                        {
                            setTempFailCount++;
                            LogWarning($"CH1 온도 설정 통신 실패 [{setTempFailCount}회] (목표:{newCh1Setpoint:F1}°C, raw:{rawValue}) — 이전 SV {lastCh1Setpoint:F1}°C 유지");
                            // 통신 오류는 effectiveMaxTemp를 낮추지 않음 — 다음 사이클에서 재시도
                            if (setTempFailCount >= maxSetTempFail)
                                LogWarning($"CH1 온도 설정 연속 {maxSetTempFail}회 실패 — 통신 상태 확인 필요");
                        }
                    }
                }

                // ★ [Fix#10] PI 피드백이 rateOfChange를 사용한 후에 갱신 (모든 모드에서 센서 이상 감지용)
                prevMonitorTemp = monitorTemp;

                var waitElapsed = DateTime.Now - waitStartTime;
                double progressRatio = targetTemperature > 0
                    ? Math.Min(monitorTemp / targetTemperature * 10, 10)
                    : 0;

                string ch1Info = IsBakeoutMode
                    ? $"CH1: {measurements.HeaterCh1Temperature:F1}°C (SV:{lastCh1Setpoint:F1})"
                    : $"CH1: {measurements.HeaterCh1Temperature:F1}°C";
                string tempDirection = monitorTemp < targetTemperature - reachTolerance ? "승온 중"
                    : monitorTemp > targetTemperature + reachTolerance ? "냉각 대기"
                    : "안정화 대기";
                UpdateProgress($"{tempDirection}  {monitorLabel}: {monitorTemp:F1}°C → {targetTemperature:F1}°C  |  " +
                    $"{ch1Info}  |  압력: {measurements.CurrentPressure:E2} Torr  |  대기: {waitElapsed:mm\\:ss}",
                    progressRatio);

                // ★ 안정화 판정: 목표±허용오차 범위 내 연속 유지
                // ★ 안정화 판정: 온도 범위 + 변화율 + SV 안정성
                //   범위에 진입만 하고 아직 변하는 중이면 안정화가 아님
                //   SV가 크게 변하는 중이면 곧 샘플 온도도 변할 것이므로 진짜 안정이 아님
                double absRate = usePIFeedback ? Math.Abs(smoothedRate) : 0;
                double svChange = Math.Abs(lastCh1Setpoint - _prevStableSV);
                bool svStable = !usePIFeedback || svChange < 0.5;
                _prevStableSV = lastCh1Setpoint;
                bool inStableRange = monitorTemp >= targetTemperature - reachTolerance
                    && monitorTemp <= targetTemperature + reachTolerance
                    && absRate < 0.3
                    && svStable; // SV도 안정되어야 함

                if (stableCountRequired <= 0)
                {
                    // 안정화 시간 미설정 (0초): 기존 동작 — 한 번 도달하면 즉시 홀드
                    if (monitorTemp >= targetTemperature - reachTolerance)
                    {
                        temperatureReached = true;
                        LogInfo($"{monitorLabel} 목표 도달: {monitorTemp:F1}°C ≈ {targetTemperature:F1}°C (허용오차 {reachTolerance:F1}°C, 대기: {waitElapsed:mm\\:ss}, CH1 SV: {lastCh1Setpoint:F1}°C, I:{integralTerm:F1})");
                    }
                }
                else
                {
                    // 안정화 시간 설정됨: 범위 내 연속 유지 확인
                    if (inStableRange)
                    {
                        stableCount++;
                        if (stableCount == 1)
                            LogInfo($"{monitorLabel} 목표 범위 진입: {monitorTemp:F1}°C (안정화 대기 시작 0/{stabilizationRequired}초)");
                        if (stableCount % (int)(30 / feedbackIntervalSec) == 0 && stableCount < stableCountRequired)
                            LogInfo($"안정화 유지 중: {monitorTemp:F1}°C ({(int)(stableCount * feedbackIntervalSec)}/{stabilizationRequired}초)");
                        if (stableCount >= stableCountRequired)
                        {
                            temperatureReached = true;
                            LogInfo($"{monitorLabel} 안정화 완료: {monitorTemp:F1}°C ≈ {targetTemperature:F1}°C (±{reachTolerance:F1}°C 범위에서 {stabilizationRequired}초 유지, 대기: {waitElapsed:mm\\:ss}, CH1 SV: {lastCh1Setpoint:F1}°C, I:{integralTerm:F1})");
                        }
                    }
                    else
                    {
                        if (stableCount > 0)
                        {
                            bool tempInRange = monitorTemp >= targetTemperature - reachTolerance
                                && monitorTemp <= targetTemperature + reachTolerance;
                            string reason = !tempInRange
                                ? $"범위 이탈 ({monitorTemp:F1}°C)"
                                : !svStable
                                ? $"SV 변동 과대 ({svChange:F1}°C)"
                                : $"변화율 과대 ({absRate:F2}°C/cyc)";
                            LogInfo($"안정화 리셋: {reason}, {(int)(stableCount * feedbackIntervalSec)}초 유지 후 리셋");
                        }
                        stableCount = 0;
                    }
                }

                // ★ 정확한 주기 유지: 루프 처리시간을 빼고 잔여 시간만 대기
                int elapsedMs = (int)(DateTime.Now - loopStart).TotalMilliseconds;
                int remainMs = (int)(feedbackIntervalSec * 1000) - elapsedMs;
                if (remainMs > 50)
                    await Task.Delay(remainMs, cancellationToken);
            }

            if (!temperatureReached)
            {
                double lastMonitorTemp = !usePIFeedback
                    ? (_mainForm._tempController?.Status?.ChannelStatus[0]?.PresentValue ?? 0)
                        / (_mainForm._tempController?.Status?.ChannelStatus[0]?.Dot == 1 ? 10.0 : 1.0)
                    : GetMonitorChannelTemperature();
                LogError($"[중단] {monitorLabel} 목표 온도 도달 실패 ({riseTimeoutMinutes}분 타임아웃, 현재: {lastMonitorTemp:F1}°C, 목표: {targetTemperature:F1}°C) → 안전 종료 실행");
                return false;
            }

            // ══════════════════════════════════════════════════════════
            //  2단계: 홀드 (베이크아웃: PI 피드백으로 온도 유지)
            // ══════════════════════════════════════════════════════════
            _experimentStartTime = DateTime.Now;
            _isExperimentTimerRunning = true;
            var experimentDuration = TimeSpan.FromMinutes(holdMinutes);
            // 적분 항 유지 (승온에서 찾은 평형점 그대로 이어감)
            // ★ [#4] 카운터 리셋 (승온→홀드 전환)
            sensorAnomalyCount = 0;
            ch1NoResponseCount = 0;
            disconnectCount = 0;
            svReducedForOverpressure = false;
            // ★ 모든 모드에서 동일 간격(5초)으로 안전 체크, 로그만 DataLoggingInterval 간격
            int holdIterationCount = 0;
            double holdLoopIntervalSec = feedbackIntervalSec;
            int dataLogEveryN = Math.Max(1, _config.DataLoggingIntervalSeconds / (int)feedbackIntervalSec);

            LogInfo($"★ {experimentLabel} 홀드 시작 — 목표: {holdMinutes}분 ({holdMinutes / 60}시간 {holdMinutes % 60}분), CH1 SV: {lastCh1Setpoint:F1}°C, I:{integralTerm:F1}");

            while ((DateTime.Now - _experimentStartTime) < experimentDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var holdLoopStart = DateTime.Now;

                // 일시정지 처리
                if (_isPaused)
                {
                    LogInfo($"{experimentLabel} 일시정지됨");
                    var pauseStart = DateTime.Now;
                    while (_isPaused && !cancellationToken.IsCancellationRequested)
                        await Task.Delay(500, cancellationToken);
                    var pauseDuration = DateTime.Now - pauseStart;
                    _experimentStartTime += pauseDuration;
                    LogInfo($"{experimentLabel} 재개 (정지 시간: {pauseDuration:mm\\:ss})");
                }

                var elapsed = DateTime.Now - _experimentStartTime;
                var remaining = experimentDuration - elapsed;
                var progress = elapsed.TotalSeconds / experimentDuration.TotalSeconds * 100;

                var measurements = await GetCurrentMeasurementsAsync();
                // ★ [Fix#13] 홀드 루프도 동일 — 폴링 캐시 사용, UpdateStatusAsync 제거

                // ★ [Fix#6] TempController 통신 끊김 감지
                if (_mainForm._tempController?.IsConnected != true)
                {
                    disconnectCount++;
                    LogWarning($"[경고] 홀드 중 온도 컨트롤러 통신 끊김 [{disconnectCount}/{maxDisconnectCount}] — 재연결 대기 중");
                    if (disconnectCount >= maxDisconnectCount)
                    {
                        LogError($"[중단] 홀드 중 온도 컨트롤러 연결 끊김 {maxDisconnectCount}회 연속 → 안전 종료 실행");
                        return false;
                    }
                    // ★ [Fix#9] 재연결 후 jump 오판 방지 — prevMonitorTemp 리셋
                    prevMonitorTemp = double.NaN;
                    sensorAnomalyCount = 0;
                    holdIterationCount++;
                    await Task.Delay((int)(holdLoopIntervalSec * 1000), cancellationToken);
                    continue;
                }
                disconnectCount = 0;

                double monitorTempNow = !usePIFeedback
                    ? measurements.HeaterCh1Temperature
                    : GetMonitorChannelTemperature();

                // ★ 센서 읽기 실패 (0 반환) 또는 NaN 체크 (홀드)
                if (double.IsNaN(monitorTempNow) || (usePIFeedback && monitorTempNow <= 0))
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] 홀드 중 {monitorLabel} 무효 온도: {monitorTempNow:F1}°C [{sensorAnomalyCount}/{maxSensorAnomalies}] — 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] 홀드 중 {monitorLabel} 무효 온도 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    holdIterationCount++;
                    await Task.Delay((int)(holdLoopIntervalSec * 1000), cancellationToken);
                    continue;
                }

                // ★ [#4] 센서 에러 플래그 체크 (모든 모드 — 모니터 채널 전체 에러 시 중단)
                {
                    var chStatus = _mainForm._tempController.Status.ChannelStatus;
                    int sensorErrorCount = 0;
                    foreach (int ch in monitorChannels)
                    {
                        int idx = ch - 1;
                        if (idx >= 0 && idx < chStatus.Length && !string.IsNullOrEmpty(chStatus[idx].SensorError))
                        {
                            sensorErrorCount++;
                            LogWarning($"[경고] 홀드 중 CH{ch} 센서 에러: {chStatus[idx].SensorError}");
                        }
                    }
                    if (sensorErrorCount >= monitorChannels.Count)
                    {
                        LogError($"[중단] {monitorLabel} 전체 센서 에러 → 안전 종료 실행");
                        return false;
                    }
                }

                // ★ [#4] 센서 이상값 체크 (모든 모드)
                if (monitorTempNow < minValidTemp)
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] 홀드 중 {monitorLabel} 이상 온도: {monitorTempNow:F1}°C [{sensorAnomalyCount}/{maxSensorAnomalies}] — 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] {monitorLabel} 센서 이상값 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    holdIterationCount++;
                    await Task.Delay((int)(holdLoopIntervalSec * 1000), cancellationToken);
                    continue;
                }
                if (!double.IsNaN(prevMonitorTemp) && Math.Abs(monitorTempNow - prevMonitorTemp) > maxTempJump)
                {
                    sensorAnomalyCount++;
                    LogWarning($"[경고] 홀드 중 {monitorLabel} 급변: {prevMonitorTemp:F1} → {monitorTempNow:F1}°C [{sensorAnomalyCount}/{maxSensorAnomalies}] — 피드백 건너뜀");
                    if (sensorAnomalyCount >= maxSensorAnomalies)
                    {
                        LogError($"[중단] {monitorLabel} 센서 이상 연속 {maxSensorAnomalies}회 → 안전 종료 실행");
                        return false;
                    }
                    holdIterationCount++;
                    await Task.Delay((int)(holdLoopIntervalSec * 1000), cancellationToken);
                    continue;
                }
                sensorAnomalyCount = 0;
                prevMonitorTemp = monitorTempNow;

                // ★ 홀드 중 압력 체크 — 과압 시 실제 대응
                if (measurements.CurrentPressure > _config.MaxPressureDuringExperiment)
                {
                    overpressureCount++;
                    if (overpressureCount >= maxOverpressureAbort)
                    {
                        LogError($"[중단] 홀드 중 압력 초과 {overpressureCount}회 연속 ({measurements.CurrentPressure:E2} Torr) → 안전 종료 실행");
                        return false;
                    }
                    if (overpressureCount >= maxOverpressureWarning
                        && _mainForm._tempController?.IsConnected == true)
                    {
                        // CH1 SV를 10°C 감소시켜 가스 방출 억제 (모든 모드)
                        double reducedSV = Math.Max(targetTemperature * 0.8, lastCh1Setpoint - 10);
                        var ch1St = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawReduced = ch1St.Dot == 1 ? (short)(reducedSV * 10) : (short)reducedSV;
                        bool svOk = _mainForm._tempController.SetTemperaturePriority(1, rawReduced);
                        if (svOk)
                        {
                            LogWarning($"[조치] 홀드 압력 초과 {overpressureCount}회 — CH1 SV 감소: {lastCh1Setpoint:F1} → {reducedSV:F1}°C ({maxOverpressureAbort}회 시 중단)");
                            lastCh1Setpoint = reducedSV;
                            if (usePIFeedback) integralTerm = Math.Max(0, integralTerm - 10);
                            svReducedForOverpressure = true;
                        }
                    }
                    else
                    {
                        LogWarning($"[경고] 홀드 중 압력 초과: {measurements.CurrentPressure:E2} Torr [{overpressureCount}/{maxOverpressureAbort}] — 피드백 건너뜀");
                    }
                    holdIterationCount++;
                    await Task.Delay((int)(holdLoopIntervalSec * 1000), cancellationToken);
                    continue;
                }
                else
                {
                    if (overpressureCount > 0)
                    {
                        LogInfo($"홀드 압력 정상 복귀: {measurements.CurrentPressure:E2} Torr (초과 {overpressureCount}회 후 해제)");
                        overpressureCount = 0;
                    }
                    // ★ 비PI 모드: 과압으로 감소시킨 SV 복원
                    if (svReducedForOverpressure && !usePIFeedback
                        && _mainForm._tempController?.IsConnected == true)
                    {
                        var ch1St = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawTarget = ch1St.Dot == 1 ? (short)(targetTemperature * 10) : (short)targetTemperature;
                        if (_mainForm._tempController.SetTemperaturePriority(1, rawTarget))
                        {
                            LogInfo($"[조치] 홀드 압력 정상 — CH1 SV 복원: {lastCh1Setpoint:F1} → {targetTemperature:F1}°C");
                            lastCh1Setpoint = targetTemperature;
                        }
                        svReducedForOverpressure = false;
                    }
                }

                // ── 베이크아웃 홀드 중 PI 피드백: 단순 PI로 온도 유지 ──
                //   홀드는 이미 평형 상태 → 감쇠/에너지보상 불필요
                //   적분은 승온에서 찾은 정상상태 값을 유지하며 미세 조정만 수행
                if (usePIFeedback && _mainForm._tempController?.IsConnected == true)
                {
                    double holdError = targetTemperature - monitorTempNow;

                    // 변화율 (D항용)
                    double holdRateOfChange = !double.IsNaN(prevMonitorTemp)
                        ? (monitorTempNow - prevMonitorTemp) : 0;
                    smoothedRate = smoothedRate * 0.7 + holdRateOfChange * 0.3;

                    // 적분: 대칭 속도로 미세 조정 (홀드에서는 공격적 감소 불필요)
                    integralTerm += holdError * Ki_norm;
                    integralTerm = Math.Max(0, Math.Min(integralTerm, maxIntegral));

                    // D항: 양방향
                    double holdDTerm = -Kd * smoothedRate;

                    double holdCh1PV = measurements.HeaterCh1Temperature;

                    double newCh1 = targetTemperature + holdError * Kp + integralTerm + holdDTerm;

                    // NaN 방어
                    if (double.IsNaN(newCh1) || double.IsInfinity(newCh1))
                    {
                        LogWarning($"[경고] 홀드 PI 출력 NaN/Inf — 이전 SV {lastCh1Setpoint:F1}°C 유지");
                        newCh1 = lastCh1Setpoint;
                    }

                    // SV 제한
                    newCh1 = Math.Max(targetTemperature, Math.Min(newCh1, effectiveMaxTemp));

                    // ★ SV 감소 속도 제한: 3단계 (홀드)
                    if (newCh1 < lastCh1Setpoint)
                    {
                        if (monitorTempNow < targetTemperature)
                            newCh1 = lastCh1Setpoint;
                        else if (monitorTempNow <= targetTemperature + reachTolerance)
                            newCh1 = Math.Max(newCh1, lastCh1Setpoint - 0.1);
                        else
                            newCh1 = Math.Max(newCh1, lastCh1Setpoint - 0.3);
                    }

                    // ★ [Fix#2] 유의미한 변경만 전송 + 반환값 검증
                    if (Math.Abs(newCh1 - lastCh1Setpoint) > 0.2)
                    {
                        var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];
                        short rawValue = ch1Status.Dot == 1
                            ? (short)(newCh1 * 10) : (short)newCh1;
                        bool setOk = _mainForm._tempController.SetTemperaturePriority(1, rawValue);
                        if (!setOk)
                        {
                            await Task.Delay(200, cancellationToken);
                            setOk = _mainForm._tempController.SetTemperaturePriority(1, rawValue);
                        }
                        if (setOk)
                        {
                            setTempFailCount = 0;
                            LogInfo($"홀드 피드백: CH1 {lastCh1Setpoint:F1} → {newCh1:F1}°C (샘플:{monitorTempNow:F1} 오차:{holdError:F1} I:{integralTerm:F1} D:{holdDTerm:F1})");
                            lastCh1Setpoint = newCh1;
                        }
                        else
                        {
                            setTempFailCount++;
                            LogWarning($"홀드 CH1 설정 통신 실패 [{setTempFailCount}회] (목표:{newCh1:F1}°C, raw:{rawValue}) — SV {lastCh1Setpoint:F1}°C 유지");
                            if (setTempFailCount >= maxSetTempFail)
                                LogWarning($"홀드 CH1 온도 설정 연속 {maxSetTempFail}회 실패 — 통신 상태 확인 필요");
                        }
                    }

                    // ★ [Fix#3] CH1 PV 응답 검증 (홀드에서도)
                    double ch1PVHold = measurements.HeaterCh1Temperature;
                    if (lastCh1Setpoint > targetTemperature + 10 && ch1PVHold < targetTemperature * 0.5)
                    {
                        ch1NoResponseCount++;
                        if (ch1NoResponseCount >= maxCh1NoResponse)
                        {
                            LogError($"홀드 중 CH1 무응답: SV={lastCh1Setpoint:F1}°C이나 PV={ch1PVHold:F1}°C — 히터 이상 의심");
                            return false;
                        }
                    }
                    else
                    {
                        ch1NoResponseCount = 0;
                    }

                    // 편차 경고 (로그 간격으로만)
                    if (holdIterationCount % dataLogEveryN == 0 &&
                        Math.Abs(holdError) > _config.TemperatureStabilityTolerance)
                        LogWarning($"{monitorLabel} 편차: {monitorTempNow:F1}°C (목표: {targetTemperature:F1}°C)");
                }
                else if (!IsBakeoutMode)
                {
                    // ★ 탈가스율 모드: CH1 PV 응답 검증 + 편차 체크
                    double ch1PVOutgas = measurements.HeaterCh1Temperature;
                    double ch1DiffOutgas = Math.Abs(ch1PVOutgas - targetTemperature);

                    // CH1 PV가 목표 대비 50% 미만이면 히터 이상 (단선, 컨트롤러 리셋 등)
                    if (ch1PVOutgas < targetTemperature * 0.5 && targetTemperature > 50)
                    {
                        ch1NoResponseCount++;
                        if (ch1NoResponseCount >= maxCh1NoResponse)
                        {
                            LogError($"[중단] 홀드 중 CH1 무응답: PV={ch1PVOutgas:F1}°C, 목표={targetTemperature:F1}°C — 히터 이상 → 안전 종료 실행");
                            return false;
                        }
                    }
                    else
                    {
                        ch1NoResponseCount = 0;
                    }

                    // 편차 경고 (로그 간격으로만)
                    if (holdIterationCount % dataLogEveryN == 0 &&
                        ch1DiffOutgas > _config.TemperatureStabilityTolerance)
                        LogWarning($"CH1 온도 편차: {ch1PVOutgas:F1}°C (목표: {targetTemperature:F1}°C)");
                }

                // ★ [Fix#5] 상세 진행 표시는 DataLoggingInterval 간격으로
                if (holdIterationCount % dataLogEveryN == 0)
                {
                    string tempInfo = IsBakeoutMode
                        ? $"{monitorLabel}: {monitorTempNow:F1}°C  |  CH1: {measurements.HeaterCh1Temperature:F1}°C (SV:{lastCh1Setpoint:F1})"
                        : $"CH1: {measurements.HeaterCh1Temperature:F1}°C  |  칠러: {measurements.ChillerTemperature:F1}°C";
                    UpdateProgress(
                        $"{experimentLabel} 진행 [{elapsed:hh\\:mm\\:ss} / {experimentDuration:hh\\:mm\\:ss}]  남은: {remaining:hh\\:mm\\:ss}  |  " +
                        $"{tempInfo}  |  압력: {measurements.CurrentPressure:E2} Torr",
                        progress);
                }

                holdIterationCount++;
                // ★ 정확한 주기 유지
                int holdElapsedMs = (int)(DateTime.Now - holdLoopStart).TotalMilliseconds;
                int holdRemainMs = (int)(holdLoopIntervalSec * 1000) - holdElapsedMs;
                if (holdRemainMs > 50)
                    await Task.Delay(holdRemainMs, cancellationToken);
            }

            LogInfo($"{experimentLabel} 완료");
            UpdateProgress($"{experimentLabel} 완료", 100);
            return true;
        }

        /// <summary>
        /// 단계 9: 종료 시퀀스
        /// </summary>
        private async Task<bool> ShutdownSequenceAsync(CancellationToken cancellationToken)
        {
            if (IsBakeoutMode)
            {
                return await BakeoutShutdownAsync(cancellationToken);
            }

            return await FullShutdownAsync(cancellationToken);
        }

        /// <summary>
        /// 베이크아웃 종료 시퀀스 — BakeoutEndAction에 따라 동작
        /// </summary>
        private async Task<bool> BakeoutShutdownAsync(CancellationToken cancellationToken)
        {
            var endAction = _config.BakeoutEndAction;
            LogInfo($"베이크아웃 종료 동작: {endAction}");

            switch (endAction)
            {
                case BakeoutEndAction.HeaterOff:
                    return await FullShutdownAsync(cancellationToken);

                case BakeoutEndAction.MaintainTemperature:
                    UpdateProgress("베이크아웃 완료 — 현재 온도 유지", 50);
                    LogInfo("히터, 펌프, 칠러 PID를 유지하며 AutoRun을 완료합니다.");

                    var measurements = await GetCurrentMeasurementsAsync();
                    LogInfo($"현재 상태: CH1={measurements.HeaterCh1Temperature:F1}°C, 압력={measurements.CurrentPressure:E2} Torr");

                    UpdateProgress("베이크아웃 종료 — 온도 유지 모드", 100);
                    return true;

                case BakeoutEndAction.NotifyOnly:
                    UpdateProgress("베이크아웃 완료 — 알림", 50);
                    LogInfo("베이크아웃 시간이 완료되었습니다. 수동 조작을 기다립니다.");

                    _mainForm.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(
                            "베이크아웃이 완료되었습니다.\n\n현재 상태가 유지됩니다. 수동으로 종료해 주세요.",
                            "베이크아웃 완료 알림",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }));

                    UpdateProgress("베이크아웃 종료 — 알림 완료", 100);
                    return true;

                default:
                    LogWarning($"알 수 없는 종료 동작: {endAction}. 전체 종료를 수행합니다.");
                    return await FullShutdownAsync(cancellationToken);
            }
        }

        /// <summary>
        /// 전체 종료 시퀀스 (기존 셧다운)
        /// </summary>
        private async Task<bool> FullShutdownAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("종료 시퀀스 시작...", 0);

            // 1. 히터 CH1 끄기 (펌프/칠러는 유지 — 진공 보호)
            if (_mainForm._tempController?.IsConnected == true)
            {
                UpdateProgress("히터 CH1 종료 중...", 3);
                _mainForm._tempController.Stop(1);
                await SafeDelay(1000, cancellationToken);
                LogInfo("히터 CH1 정지 (펌프 유지 중)");
            }
            cancellationToken.ThrowIfCancellationRequested();

            // 2. 벤팅 시작 온도 대기 (펌프 가동 상태에서 냉각 — 진공 유지로 산화 방지)
            if (_mainForm._tempController?.IsConnected == true)
            {
                int ventTempWait = 0;
                int ventTempMaxWait = 5400; // 최대 90분
                bool ventTempReady = false;

                LogInfo($"벤팅 온도 대기 시작 (펌프 유지 중, 목표: ≤{_config.VentingStartTemperature}°C)");

                while (!ventTempReady && ventTempWait < ventTempMaxWait)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var status = _mainForm._tempController.Status;
                    double ch1Temp = status.ChannelStatus[0].Dot == 0
                        ? status.ChannelStatus[0].PresentValue
                        : status.ChannelStatus[0].PresentValue / 10.0;

                    if (ch1Temp <= _config.VentingStartTemperature)
                    {
                        ventTempReady = true;
                        LogInfo($"벤팅 시작 온도 도달: CH1={ch1Temp:F1}°C ≤ {_config.VentingStartTemperature}°C");
                        break;
                    }

                    if (ventTempWait % 60 == 0)
                        LogInfo($"냉각 대기 중: CH1={ch1Temp:F1}°C (목표: ≤{_config.VentingStartTemperature}°C, 펌프 유지)");

                    UpdateProgress(
                        $"냉각 대기 중 (펌프 유지)  CH1: {ch1Temp:F1}°C → ≤{_config.VentingStartTemperature}°C",
                        3 + (int)(20.0 * ventTempWait / ventTempMaxWait));

                    await SafeDelay(5000, cancellationToken);
                    ventTempWait += 5;
                }

                if (!ventTempReady)
                    LogWarning($"벤팅 시작 온도 대기 타임아웃 — 현재 온도로 진행합니다.");
            }
            cancellationToken.ThrowIfCancellationRequested();

            // 3. 이온게이지 HV OFF
            UpdateProgress("이온게이지 HV 끄는 중...", 24);
            await _mainForm._ioModule.ControlIonGaugeHVAsync(false);
            await SafeDelay(1000, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 4. 터보펌프 정지 + 완전 감속 대기
            if (_mainForm._turboPump?.IsConnected == true && _mainForm._turboPump.Status?.IsRunning == true)
            {
                UpdateProgress("터보펌프 정지 중...", 26);
                _mainForm._turboPump.Stop();

                int waitCount = 0;
                while (_mainForm._turboPump.Status?.CurrentSpeed > 0 && waitCount < 600)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await SafeDelay(1000, cancellationToken);
                    var speed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;

                    if (waitCount % 10 == 0)
                        LogInfo($"터보펌프 감속 중: {speed} Hz");

                    UpdateProgress($"터보펌프 감속 중... ({speed} Hz)", 26 + (int)(10.0 * (1.0 - speed / 630.0)));
                    waitCount++;
                }

                LogInfo("터보펌프 완전 정지 확인");
            }
            cancellationToken.ThrowIfCancellationRequested();

            // 5. 게이트 밸브 닫기 (터보펌프 완전 정지 후)
            UpdateProgress("게이트 밸브 닫는 중...", 37);
            if (!await _mainForm._ioModule.ControlGateValveAsync(false))
                LogWarning("게이트 밸브 닫기 실패");
            await SafeDelay(2000, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            // 6. 드라이펌프 정지 (벤트 전에 반드시 정지 — 대기압 역류 방지)
            if (_mainForm._dryPump?.IsConnected == true && _mainForm._dryPump.Status?.IsRunning == true)
            {
                UpdateProgress("드라이펌프 정지 중...", 39);
                _mainForm._dryPump.Stop();
                await SafeDelay(3000, cancellationToken);
                LogInfo("드라이펌프 정지 완료");
            }
            cancellationToken.ThrowIfCancellationRequested();

            // 6. 벤트 밸브 열기 → ATM 스위치 목표 압력 도달 대기
            UpdateProgress("벤트 밸브 여는 중...", 27);
            if (!await _mainForm._ioModule.ControlVentValveAsync(true))
            {
                LogWarning("벤트 밸브 열기 실패");
            }
            LogInfo($"벤트 밸브 열림 → ATM 스위치 목표: {_config.VentTargetPressure_kPa} kPa 대기");

            bool atmReached = false;
            int ventWaitCount = 0;
            int ventMaxWait = 600; // 최대 10분 대기

            while (!atmReached && ventWaitCount < ventMaxWait)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var measurements = await GetCurrentMeasurementsAsync();
                double atmPressure = measurements.AtmPressure;

                if (ventWaitCount % 30 == 0) // 30초마다 로그
                {
                    LogInfo($"벤트 대기 중: ATM={atmPressure:F1} kPa (목표: {_config.VentTargetPressure_kPa} kPa)");
                }

                UpdateProgress(
                    $"벤트 대기 중... ATM: {atmPressure:F1} kPa → {_config.VentTargetPressure_kPa} kPa",
                    25 + (int)(10.0 * ventWaitCount / ventMaxWait));

                if (atmPressure >= _config.VentTargetPressure_kPa)
                {
                    atmReached = true;
                    LogInfo($"ATM 목표 압력 도달: {atmPressure:F1} kPa");
                }

                await Task.Delay(1000, cancellationToken);
                ventWaitCount++;
            }

            if (!atmReached)
            {
                LogWarning("ATM 목표 압력 도달 타임아웃 — 배기 밸브를 강제로 엽니다.");
            }

            // 6. 배기 밸브 열기 → CH1 쿨링 온도 도달 대기
            UpdateProgress("배기 밸브 여는 중...", 35);
            if (!await _mainForm._ioModule.ControlExhaustValveAsync(true))
            {
                LogWarning("배기 밸브 열기 실패");
            }
            LogInfo($"배기 밸브 열림 → CH1 쿨링 목표: {_config.CoolingTargetTemperature}°C 대기");

            bool coolingComplete = false;
            int coolWaitCount = 0;
            int coolMaxWait = 5400; // 최대 90분 대기

            while (!coolingComplete && coolWaitCount < coolMaxWait)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double ch1Temp = 0;

                if (_mainForm._tempController?.IsConnected == true)
                {
                    var status = _mainForm._tempController.Status;
                    ch1Temp = status.ChannelStatus[0].Dot == 0
                        ? status.ChannelStatus[0].PresentValue
                        : status.ChannelStatus[0].PresentValue / 10.0;
                }

                if (coolWaitCount % 60 == 0) // 1분마다 로그
                {
                    LogInfo($"쿨링 대기 중: CH1={ch1Temp:F1}°C (목표: ≤{_config.CoolingTargetTemperature}°C)");
                }

                var coolElapsed = TimeSpan.FromSeconds(coolWaitCount);
                UpdateProgress(
                    $"쿨링 대기 중  CH1: {ch1Temp:F1}°C → {_config.CoolingTargetTemperature}°C  |  경과: {coolElapsed:mm\\:ss}",
                    35 + (int)(45.0 * Math.Min(1.0, coolWaitCount / (double)coolMaxWait)));

                if (ch1Temp <= _config.CoolingTargetTemperature)
                {
                    coolingComplete = true;
                    LogInfo($"CH1 쿨링 완료: {ch1Temp:F1}°C (목표: ≤{_config.CoolingTargetTemperature}°C, 소요: {coolElapsed:mm\\:ss})");
                }

                await Task.Delay(5000, cancellationToken);
                coolWaitCount += 5;
            }

            if (!coolingComplete)
            {
                LogWarning("CH1 쿨링 타임아웃 — 밸브를 닫고 계속 진행합니다.");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // 7. 벤트 밸브 닫기
            UpdateProgress("벤트 밸브 닫는 중...", 82);
            if (!await _mainForm._ioModule.ControlVentValveAsync(false))
            {
                LogWarning("벤트 밸브 닫기 실패");
            }
            await Task.Delay(1000, cancellationToken);
            LogInfo("벤트 밸브 닫힘");

            // 8. 배기 밸브 닫기
            UpdateProgress("배기 밸브 닫는 중...", 85);
            if (!await _mainForm._ioModule.ControlExhaustValveAsync(false))
            {
                LogWarning("배기 밸브 닫기 실패");
            }
            await Task.Delay(1000, cancellationToken);
            LogInfo("배기 밸브 닫힘");

            // 9. 칠러 PID 정지
            StopChillerPID();

            // 10. 최종 상태 확인
            UpdateProgress("최종 상태 확인 중...", 95);
            await Task.Delay(2000, cancellationToken);

            var finalCheck = true;

            if (_mainForm._turboPump?.Status?.IsRunning == true)
            {
                LogError("터보펌프가 여전히 작동 중입니다.");
                finalCheck = false;
            }

            if (_mainForm._tempController?.IsConnected == true)
            {
                await _mainForm._tempController.UpdateStatusAsync();
                if (_mainForm._tempController.Status.ChannelStatus[0].IsRunning)
                {
                    LogError("히터 CH1이 여전히 작동 중입니다.");
                    finalCheck = false;
                }
            }

            UpdateProgress("종료 시퀀스 완료", 100);
            return finalCheck;
        }

        /// <summary>
        /// 비상 종료 시퀀스
        /// </summary>
        private async Task EmergencyShutdownAsync(int failedStep = 9)
        {
            try
            {
                LogWarning($"비상 종료 시퀀스 실행 (실패 단계: {failedStep})");

                // 1. 칠러 PID 정지 (단계 7 이후에만 — 히터 시작 후 PID가 활성화됨)
                if (failedStep >= 7)
                {
                    StopChillerPID();
                }

                // 2. 히터 CH1 즉시 종료 (단계 7 이후에만 — AutoRun이 히터를 시작한 경우)
                if (failedStep >= 7 && _mainForm._tempController?.IsConnected == true)
                {
                    _mainForm._tempController.Stop(1);
                    LogWarning("히터 CH1 비상 정지");
                }

                // 3. 이온게이지 HV OFF (단계 5 이후에만)
                if (failedStep >= 5 && _mainForm._ioModule?.IsConnected == true)
                {
                    await _mainForm._ioModule.ControlIonGaugeHVAsync(false);
                    LogWarning("이온게이지 HV OFF");
                }

                // ★ 정지 순서: 터보펌프 감속 완료 → 게이트밸브 닫기 → 드라이펌프 정지
                //   (터보 고속 회전 중 드라이펌프 정지 시 역류 위험)

                // 4. 터보펌프 정지 + 감속 대기 (단계 4 이후에만)
                if (failedStep >= 4)
                {
                    if (_mainForm._turboPump?.IsConnected == true && _mainForm._turboPump.Status?.IsRunning == true)
                    {
                        _mainForm._turboPump.Stop();
                        LogWarning("터보펌프 비상 정지 — 감속 대기 시작");

                        int waitCount = 0;
                        while (_mainForm._turboPump.Status?.CurrentSpeed > 0 && waitCount < 600)
                        {
                            await Task.Delay(1000);
                            var speed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;

                            if (waitCount % 10 == 0)
                            {
                                LogWarning($"터보펌프 감속 중: {speed} Hz");
                            }
                            waitCount++;
                        }

                        if (_mainForm._turboPump.Status?.CurrentSpeed > 0)
                        {
                            LogError("터보펌프 감속 타임아웃 (600초) — 강제 진행");
                        }
                        else
                        {
                            LogWarning("터보펌프 완전 정지 확인");
                        }
                    }
                    else
                    {
                        _mainForm._turboPump?.Stop();
                        LogWarning("터보펌프 비상 정지");
                    }
                }

                // 5. 게이트밸브 닫기 (터보펌프 완전 정지 후, 단계 2 이후에만)
                if (failedStep >= 2 && _mainForm._ioModule?.IsConnected == true)
                {
                    await _mainForm._ioModule.ControlGateValveAsync(false);
                    LogWarning("게이트밸브 비상 닫기");
                    await Task.Delay(2000);
                }

                // 6. 드라이펌프 정지 (게이트밸브 닫힌 후, 단계 3 이후에만)
                if (failedStep >= 3)
                {
                    _mainForm._dryPump?.Stop();
                    LogWarning("드라이펌프 비상 정지");
                }

                LogWarning("비상 종료 완료");
            }
            catch (Exception ex)
            {
                LogError($"비상 종료 중 오류: {ex.Message}", ex);
            }
        }

        #endregion

        #region 도우미 메서드

        /// <summary>
        /// 현재 측정값 가져오기 (DO/DI 기반)
        /// </summary>
        private async Task<AutoRunMeasurements> GetCurrentMeasurementsAsync()
        {
            var measurements = new AutoRunMeasurements();

            try
            {
                if (_mainForm._ioModule?.IsConnected == true)
                {
                    var aiData = _mainForm._ioModule.LastValidAIValues;
                    // DO 기반 밸브/IG 상태
                    var doData = _mainForm._ioModule.LastValidDOValues;

                    if (aiData != null)
                    {
                        measurements.AtmPressure = _mainForm._atmSwitch?.ConvertVoltageToPressureInkPa(aiData.ExpansionVoltageValues[0]) ?? 0;
                        measurements.CurrentPressure = _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;

                        // ★ 이온게이지 압력: PTR90은 HV 불필요, PTR225는 HV ON 필요
                        if (_mainForm._ionGauge != null && measurements.CurrentPressure < 1E-2)
                        {
                            bool useIG = _mainForm._ionGauge.Model == IonGaugeModel.PTR90
                                ? measurements.CurrentPressure > 0
                                : doData?.IsIonGaugeHVOn == true;

                            if (useIG)
                            {
                                double igVoltage = aiData.ExpansionVoltageValues[2];
                                var igCal = _mainForm._tempCalibrationConfig?.IonGauge;
                                if (igCal != null) igVoltage = igCal.ApplyVoltageOffset(igVoltage);
                                double ionPressure = _mainForm._ionGauge.ConvertVoltageToPressureInTorr(igVoltage);
                                if (igCal != null) ionPressure = igCal.Apply(ionPressure);
                                if (ionPressure > 0 && ionPressure < measurements.CurrentPressure)
                                {
                                    measurements.CurrentPressure = ionPressure;
                                }
                            }
                        }
                    }

                    // 게이트 밸브: DI 기반 리드 스위치
                    measurements.GateValveStatus = _mainForm._ioModule.GateValvePosition ?? "Unknown";

                    // VV/EV 상태: DO 기반
                    if (doData != null)
                    {
                        measurements.VentValveStatus = doData.IsVentValveOn ? "Opened" : "Closed";
                        measurements.ExhaustValveStatus = doData.IsExhaustValveOn ? "Opened" : "Closed";
                    }
                }

                if (_mainForm._dryPump?.IsConnected == true)
                {
                    measurements.DryPumpStatus = _mainForm._dryPump.GetStatusText();
                }

                if (_mainForm._turboPump?.IsConnected == true)
                {
                    measurements.TurboPumpStatus = _mainForm._turboPump.GetStatusText();
                    measurements.TurboPumpSpeed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                }

                if (_mainForm._bathCirculator?.IsConnected == true)
                {
                    measurements.ChillerTemperature = _mainForm._bathCirculator.Status.CurrentTemperature;
                }

                if (_mainForm._tempController?.IsConnected == true)
                {
                    var status = _mainForm._tempController.Status;

                    if (status.ChannelStatus[0].Dot == 0)
                        measurements.HeaterCh1Temperature = status.ChannelStatus[0].PresentValue;
                    else
                        measurements.HeaterCh1Temperature = status.ChannelStatus[0].PresentValue / 10.0;

                    if (status.ChannelStatus[1].Dot == 0)
                        measurements.HeaterCh2Temperature = status.ChannelStatus[1].PresentValue;
                    else
                        measurements.HeaterCh2Temperature = status.ChannelStatus[1].PresentValue / 10.0;
                }
            }
            catch (Exception ex)
            {
                LogError($"측정값 읽기 오류: {ex.Message}", ex);
            }

            return measurements;
        }

        /// <summary>
        /// 진행 상황 업데이트
        /// </summary>
        private void UpdateProgress(string message, double stepProgress, AutoRunMeasurements currentValues = null)
        {
            // 가중치 기반 전체 진행률 계산
            double completedWeight = 0;
            for (int i = 0; i < _currentStepNumber - 1 && i < StepWeights.Length; i++)
                completedWeight += StepWeights[i];

            double currentWeight = (_currentStepNumber >= 1 && _currentStepNumber <= StepWeights.Length)
                ? StepWeights[_currentStepNumber - 1] : 0;

            var overallProgress = (completedWeight + currentWeight * stepProgress / 100.0) / StepWeightTotal * 100.0;

            var args = new AutoRunProgressEventArgs(
                CurrentState,
                message,
                stepProgress,
                overallProgress);

            args.CurrentValues = currentValues;

            OnProgressUpdated(args);

            if (_config.EnableDetailedLogging)
            {
                LogDebug($"[{CurrentState}] {message} ({stepProgress:F1}%)");
            }
        }

        /// <summary>
        /// 선택된 모니터 채널들 중 최대 온도 반환.
        /// 센서 에러 채널은 제외. 모두 에러면 0 반환.
        /// 목적: 샘플 어느 부분도 타겟 온도를 초과하지 않도록 가장 뜨거운 지점 추적.
        /// </summary>
        private double GetMonitorChannelTemperature()
        {
            if (_mainForm._tempController?.IsConnected != true)
                return 0;

            var channels = _config.GetBakeoutMonitorChannels();
            var chStatus = _mainForm._tempController.Status.ChannelStatus;
            double maxTemp = double.MinValue;
            int validCount = 0;

            foreach (int ch in channels)
            {
                int idx = ch - 1;
                if (idx < 0 || idx >= chStatus.Length) continue;
                if (!string.IsNullOrEmpty(chStatus[idx].SensorError)) continue;

                double temp = chStatus[idx].Dot == 1
                    ? chStatus[idx].PresentValue / 10.0
                    : chStatus[idx].PresentValue;
                if (temp > maxTemp) maxTemp = temp;
                validCount++;
            }

            return validCount > 0 ? maxTemp : 0;
        }

        /// <summary>
        /// 수동 컨트롤 활성화/비활성화
        /// </summary>
        private void EnableManualControls(bool enable)
        {
            // 현재 주석 처리 — 추후 구현
        }

        /// <summary>
        /// 칠러 PID 제어 시작 (AutoRun용)
        /// </summary>
        private void StartChillerPID()
        {
            try
            {
                var pidService = _mainForm.ChillerPIDService;
                if (pidService == null)
                {
                    LogWarning("칠러 PID 서비스가 초기화되지 않았습니다.");
                    return;
                }

                if (_mainForm._bathCirculator?.IsConnected != true
                    || _mainForm._tempController?.IsConnected != true)
                {
                    LogWarning("칠러 또는 온도 컨트롤러 미연결 — PID 건너뜀");
                    return;
                }

                // 이미 실행 중이면 스킵
                if (pidService.IsEnabled)
                {
                    LogInfo("칠러 PID 이미 실행 중 — 유지");
                    return;
                }

                // PID 활성화 + 시작
                pidService.IsEnabled = true;
                LogInfo($"칠러 PID 제어 시작 (Ch2 목표: {pidService.Ch2TargetTemperature}°C, " +
                    $"기준: {pidService.ChillerBaseTemperature}°C)");

                // UI 체크박스 동기화
                _mainForm.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var chk = _mainForm.Controls.Find("chkChillerPIDEnabled", true);
                        if (chk.Length > 0 && chk[0] is CheckBox cb)
                            cb.Checked = true;
                    }
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                LogWarning($"칠러 PID 시작 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 칠러 PID 제어 정지 (AutoRun용)
        /// </summary>
        private void StopChillerPID()
        {
            try
            {
                var pidService = _mainForm.ChillerPIDService;
                if (pidService == null || !pidService.IsEnabled)
                    return;

                pidService.IsEnabled = false;
                LogInfo("칠러 PID 제어 정지");

                // UI 체크박스 동기화
                _mainForm.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var chk = _mainForm.Controls.Find("chkChillerPIDEnabled", true);
                        if (chk.Length > 0 && chk[0] is CheckBox cb)
                            cb.Checked = false;
                    }
                    catch { }
                }));
            }
            catch (Exception ex)
            {
                LogWarning($"칠러 PID 정지 중 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 단계 번호로 AutoRunState 가져오기
        /// </summary>
        private AutoRunState GetAutoRunStateForStep(int step)
        {
            switch (step)
            {
                case 1: return AutoRunState.Initializing;
                case 2: return AutoRunState.PreparingVacuum;
                case 3: return AutoRunState.StartingDryPump;
                case 4: return AutoRunState.StartingTurboPump;
                case 5: return AutoRunState.ActivatingIonGauge;
                case 6: return AutoRunState.WaitingHighVacuum;
                case 7: return AutoRunState.StartingHeater;
                case 8: return AutoRunState.RunningExperiment;
                case 9: return AutoRunState.ShuttingDown;
                default: return AutoRunState.Idle;
            }
        }

        /// <summary>
        /// 상태 설명 가져오기
        /// </summary>
        private string GetStateDescription(AutoRunState state)
        {
            switch (state)
            {
                case AutoRunState.Initializing: return "초기화";
                case AutoRunState.PreparingVacuum: return "진공 준비";
                case AutoRunState.StartingDryPump: return "드라이펌프 시작";
                case AutoRunState.StartingTurboPump: return "터보펌프 시작";
                case AutoRunState.ActivatingIonGauge: return "이온게이지 활성화";
                case AutoRunState.WaitingHighVacuum: return "고진공 대기";
                case AutoRunState.StartingHeater: return "히터 시작";
                case AutoRunState.RunningExperiment: return "실험 진행";
                case AutoRunState.ShuttingDown: return "종료 시퀀스";
                default: return state.ToString();
            }
        }

        /// <summary>
        /// 단계 완료 시 요약 정보 생성
        /// </summary>
        /// <summary>현재 압력 동기 조회 (UI 표시용)</summary>
        private double GetCurrentPressureSync()
        {
            try
            {
                if (_mainForm._ioModule?.IsConnected != true) return 0;
                var aiData = _mainForm._ioModule.LastValidAIValues;
                if (aiData == null) return 0;

                var doData = _mainForm._ioModule.LastValidDOValues;
                bool useIG = false;
                if (_mainForm._ionGauge != null)
                {
                    if (_mainForm._ionGauge.Model == IonGaugeModel.PTR90)
                    {
                        double piraniP = _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;
                        useIG = piraniP > 0 && piraniP < 1E-2;
                    }
                    else
                    {
                        useIG = doData?.IsIonGaugeHVOn == true;
                    }
                }

                if (useIG)
                {
                    double igVoltage = aiData.ExpansionVoltageValues[2];
                    var igCal = _mainForm._tempCalibrationConfig?.IonGauge;
                    if (igCal != null) igVoltage = igCal.ApplyVoltageOffset(igVoltage);
                    double ionPressure = _mainForm._ionGauge.ConvertVoltageToPressureInTorr(igVoltage);
                    if (igCal != null) ionPressure = igCal.Apply(ionPressure);
                    if (ionPressure > 0) return ionPressure;
                }

                return _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;
            }
            catch { return 0; }
        }

        private string GetStepCompletionSummary(AutoRunState state)
        {
            try
            {
                switch (state)
                {
                    case AutoRunState.StartingDryPump:
                    {
                        var p = GetCurrentPressureSync();
                        return p > 0 ? $"압력: {p:E1} Torr" : "";
                    }
                    case AutoRunState.StartingTurboPump:
                    {
                        int speed = _mainForm._turboPump?.Status?.CurrentSpeed ?? 0;
                        var p = GetCurrentPressureSync();
                        return $"속도: {speed} RPM, 압력: {(p > 0 ? $"{p:E1} Torr" : "N/A")}";
                    }
                    case AutoRunState.ActivatingIonGauge:
                    {
                        var p = GetCurrentPressureSync();
                        return p > 0 ? $"압력: {p:E2} Torr" : "";
                    }
                    case AutoRunState.WaitingHighVacuum:
                    {
                        var p = GetCurrentPressureSync();
                        return p > 0 ? $"도달 압력: {p:E2} Torr" : "";
                    }
                    case AutoRunState.StartingHeater:
                    {
                        if (_mainForm._tempController?.IsConnected == true)
                        {
                            var ch1 = _mainForm._tempController.Status.ChannelStatus[0];
                            double pv = ch1.Dot == 1 ? ch1.PresentValue / 10.0 : ch1.PresentValue;
                            double sv = ch1.Dot == 1 ? ch1.SetValue / 10.0 : ch1.SetValue;
                            return $"CH1: {pv:F1}°C (SV:{sv:F1})";
                        }
                        return "";
                    }
                    case AutoRunState.RunningExperiment:
                    {
                        var tp = ThermalParams;
                        if (_mainForm._tempController?.IsConnected == true)
                        {
                            double sampleTemp = IsBakeoutMode ? GetMonitorChannelTemperature()
                                : _mainForm._tempController.Status.ChannelStatus[0].PresentValue
                                  / (_mainForm._tempController.Status.ChannelStatus[0].Dot == 1 ? 10.0 : 1.0);
                            return $"샘플: {sampleTemp:F1}°C, 열지연: {tp.ThermalLag:F1}°C, 정상SV: {tp.EstimatedSteadyStateSV:F1}°C";
                        }
                        return "";
                    }
                    case AutoRunState.ShuttingDown:
                    {
                        if (_mainForm._tempController?.IsConnected == true)
                        {
                            var ch1 = _mainForm._tempController.Status.ChannelStatus[0];
                            double pv = ch1.Dot == 1 ? ch1.PresentValue / 10.0 : ch1.PresentValue;
                            return $"CH1: {pv:F1}°C";
                        }
                        return "";
                    }
                    default:
                        return "";
                }
            }
            catch { return ""; }
        }

        /// <summary>
        /// 실행 요약 생성
        /// </summary>
        private string GenerateSummary(StepResult result)
        {
            var sb = new System.Text.StringBuilder();
            string experimentTypeText = IsBakeoutMode ? "베이크아웃" : "탈가스율 측정";
            sb.AppendLine("=== AutoRun 실행 요약 ===");
            sb.AppendLine($"실험 유형: {experimentTypeText}");
            sb.AppendLine($"시작 시간: {_startTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"종료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"총 소요 시간: {_stopwatch.Elapsed:hh\\:mm\\:ss}");
            sb.AppendLine($"최종 상태: {result}");
            sb.AppendLine($"완료 단계: {_currentStepNumber}/{TOTAL_STEPS}");

            if (result == StepResult.Success && _experimentStartTime != default)
            {
                int durationMinutes = IsBakeoutMode
                    ? _config.BakeoutHoldTimeMinutes
                    : _config.ExperimentDurationMinutes;
                sb.AppendLine($"실험 시작: {_experimentStartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"실험 시간: {durationMinutes}분");

                if (IsBakeoutMode)
                {
                    sb.AppendLine($"모니터 채널: {_config.GetBakeoutMonitorLabel()}");
                    sb.AppendLine($"램프 속도: {_config.BakeoutRampRate:F0}°C/h");
                    sb.AppendLine($"종료 동작: {_config.BakeoutEndAction}");
                }
                else
                {
                    sb.AppendLine($"목표 온도: {_config.HeaterCh1SetTemperature:F1}°C");
                    sb.AppendLine($"램프 속도: {_config.HeaterRampUpRate:F1}°C/min");
                    sb.AppendLine($"최대 허용 압력: {_config.MaxPressureDuringExperiment:E2} Torr");
                }
            }

            return sb.ToString();
        }

        #endregion

        #region 로깅 메서드

        private string LogPrefix => IsBakeoutMode ? "[Bakeout AutoRun]" : "[AutoRun]";

        private void LogInfo(string message)
        {
            AsyncLoggingService.Instance.LogInfo($"{LogPrefix} {message}");
        }

        /// <summary>CancellationToken 취소 시 예외 대신 조기 리턴</summary>
        private async Task SafeDelay(int ms, CancellationToken ct)
        {
            try { await Task.Delay(ms, ct); }
            catch (OperationCanceledException) { }
        }

        private void LogWarning(string message)
        {
            AsyncLoggingService.Instance.LogWarning($"{LogPrefix} {message}");
        }

        private void LogError(string message, Exception ex = null)
        {
            AsyncLoggingService.Instance.LogError($"{LogPrefix} {message}", ex);
            OnErrorOccurred(new AutoRunErrorEventArgs(CurrentState, message, ex));
        }

        private void LogDebug(string message)
        {
            AsyncLoggingService.Instance.LogDebug($"[AutoRun] {message}");
        }

        #endregion

        #region 이벤트 발생 메서드

        protected virtual void OnStateChanged(AutoRunStateChangedEventArgs e)
        {
            e.ElapsedTime = _stopwatch.Elapsed;
            StateChanged?.Invoke(this, e);
        }

        protected virtual void OnProgressUpdated(AutoRunProgressEventArgs e)
        {
            ProgressUpdated?.Invoke(this, e);
        }

        protected virtual void OnErrorOccurred(AutoRunErrorEventArgs e)
        {
            ErrorOccurred?.Invoke(this, e);
        }

        protected virtual void OnCompleted(AutoRunCompletedEventArgs e)
        {
            Completed?.Invoke(this, e);
        }

        #endregion
    }

    /// <summary>
    /// 베이크아웃 열 특성 실시간 계수
    /// </summary>
    public class ThermalCharacteristics
    {
        /// <summary>관측된 열지연 CH1-샘플 (°C)</summary>
        public double ThermalLag { get; set; }
        /// <summary>현재 열지연 CH1 PV - 샘플 (°C)</summary>
        public double CurrentLag { get; set; }
        /// <summary>샘플 온도 변화율 (°C/사이클)</summary>
        public double SampleRate { get; set; }
        /// <summary>적응형 Kp</summary>
        public double Kp { get; set; }
        /// <summary>적응형 Kd</summary>
        public double Kd { get; set; }
        /// <summary>적분항 (°C)</summary>
        public double IntegralTerm { get; set; }
        /// <summary>축적 에너지 (°C)</summary>
        public double StoredEnergy { get; set; }
        /// <summary>근접도 (0~1)</summary>
        public double Proximity { get; set; }
        /// <summary>추정 정상상태 SV (°C)</summary>
        public double EstimatedSteadyStateSV { get; set; }
        /// <summary>열 시정수 추정 (초) — 열지연/변화율 기반</summary>
        public double EstimatedThermalTimeConstant { get; set; }
        /// <summary>열저항 추정 (°C/W 상대값) — 열지연/히터출력 기반</summary>
        public double EstimatedThermalResistance { get; set; }
    }
}