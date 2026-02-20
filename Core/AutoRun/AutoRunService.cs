using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
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

        private AutoRunState _currentState = AutoRunState.Idle;
        private bool _isRunning = false;
        private bool _isPaused = false;
        private int _currentStepNumber = 0;
        private const int TOTAL_STEPS = 9;

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

            // NOTE: _runningTask?.Wait()를 사용하지 않음
            // UI 스레드에서 Wait()하면 백그라운드 태스크의 Invoke()와 데드락 발생
            // _isRunning = false + Cancel()이면 태스크가 자체 종료됨

            CurrentState = AutoRunState.Aborted;
            _stopwatch.Stop();
            EnableManualControls(true);
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
                var aiData = _mainForm._ioModule?.LastValidAIValues;
                var aoData = _mainForm._ioModule?.LastValidAOValues;

                if (aiData != null)
                {
                    if (aiData.MasterCurrentValues[3] > 1)
                        assessment.GateValveStatus = "Opened";
                    else if (aiData.MasterCurrentValues[0] > 1)
                        assessment.GateValveStatus = "Closed";
                    else
                        assessment.GateValveStatus = "Moving";
                }

                if (aoData != null)
                {
                    assessment.VentValveStatus = aoData.IsVentValveOpen ? "Opened" : "Closed";
                    assessment.ExhaustValveStatus = aoData.IsExhaustValveOpen ? "Opened" : "Closed";
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

                // 5. 이온게이지
                assessment.IonGaugeActive = aoData?.IsIonGaugeHVOn == true;

                // 6. 압력
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

                // 단계 5: 이온게이지 활성화
                if (startFromStep <= 5)
                {
                    _currentStepNumber = 5;
                    result = await ExecuteStepAsync(AutoRunState.ActivatingIonGauge, ActivateIonGaugeAsync,
                        _config.IonGaugeActivationTimeout, cancellationToken);
                    if (result != StepResult.Success) goto Cleanup;
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
                    // 설정 변경 후 재시작일 수 있으므로 새 목표 온도를 컨트롤러에 전송
                    if (_mainForm._tempController?.IsConnected == true)
                    {
                        await _mainForm._tempController.UpdateStatusAsync();
                        var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];

                        short ch1SetValue = ch1Status.Dot == 1
                            ? (short)(_config.HeaterCh1SetTemperature * 10)
                            : (short)_config.HeaterCh1SetTemperature;

                        double currentSV = ch1Status.Dot == 1
                            ? ch1Status.SetValue / 10.0 : ch1Status.SetValue;

                        if (Math.Abs(currentSV - _config.HeaterCh1SetTemperature) > 0.5)
                        {
                            _mainForm._tempController.SetTemperature(1, ch1SetValue);
                            LogInfo($"히터 CH1 온도 재설정: {currentSV:F1}°C → {_config.HeaterCh1SetTemperature:F1}°C");
                        }
                    }

                    StartChillerPID();
                }

                // 단계 8: 실험 진행 (항상 실행)
                _currentStepNumber = 8;
                result = await ExecuteStepAsync(AutoRunState.RunningExperiment, RunExperimentAsync,
                    _config.ExperimentDurationMinutes * 60 + 7200, cancellationToken); // 실험시간 + 온도도달 대기 2시간
                if (result != StepResult.Success) goto Cleanup;

                // 단계 9: 종료 시퀀스 (항상 실행)
                _currentStepNumber = 9;
                result = await ExecuteStepAsync(AutoRunState.ShuttingDown, ShutdownSequenceAsync,
                    _config.ShutdownTimeout, cancellationToken);

                CurrentState = AutoRunState.Completed;
                LogInfo("=== AutoRun 시퀀스 정상 완료 ===");

            Cleanup:
                if (result != StepResult.Success)
                {
                    CurrentState = result == StepResult.Aborted
                        ? AutoRunState.Aborted
                        : AutoRunState.Error;

                    if (_config.EnableSafeShutdownOnFailure && result != StepResult.Aborted)
                    {
                        LogWarning("안전 종료 시퀀스 실행 중...");
                        await EmergencyShutdownAsync();
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
            int timeoutSeconds, CancellationToken cancellationToken)
        {
            CurrentState = state;
            LogInfo($"단계 {_currentStepNumber}/{TOTAL_STEPS}: {GetStateDescription(state)} 시작");

            var stepCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            stepCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            try
            {
                int retryCount = 0;
                bool success = false;

                while (retryCount <= _config.MaxRetryCount && !success)
                {
                    if (retryCount > 0)
                    {
                        LogWarning($"재시도 {retryCount}/{_config.MaxRetryCount}...");
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
                            LogInfo($"단계 {_currentStepNumber} 완료");
                            return StepResult.Success;
                        }
                    }
                    catch (OperationCanceledException) when (stepCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        LogError($"단계 {_currentStepNumber} 시간 초과 ({timeoutSeconds}초)");
                        return StepResult.Timeout;
                    }

                    retryCount++;
                }

                LogError($"단계 {_currentStepNumber} 실패 (재시도 {_config.MaxRetryCount}회 초과)");
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
                    LogError("드라이펌프가 이미 작동 중입니다. 먼저 정지해주세요.");
                    return false;
                }
            }

            if (_mainForm._turboPump?.IsConnected == true)
            {
                if (_mainForm._turboPump.Status?.IsRunning == true)
                {
                    LogError("터보펌프가 이미 작동 중입니다. 먼저 정지해주세요.");
                    return false;
                }
            }

            UpdateProgress("칠러 상태 확인 중...", 40);

            // 칠러 상태 확인 및 시작
            if (_mainForm._bathCirculator?.IsConnected == true)
            {
                if (!_mainForm._bathCirculator.Status.IsRunning)
                {
                    LogInfo("칠러를 시작합니다...");
                    if (!_mainForm._bathCirculator.Start())
                    {
                        LogError("칠러 시작 실패");
                        return false;
                    }
                    await Task.Delay(2000, cancellationToken);
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
                var status = _mainForm._tempController.Status;
                if (status.ChannelStatus[0].IsRunning)
                {
                    LogError("히터 CH1이 이미 작동 중입니다. 먼저 정지해주세요.");
                    return false;
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

            UpdateProgress("밸브 상태 최종 확인...", 90);
            var aoData = await _mainForm._ioModule.ReadAnalogOutputsAsync();
            if (aoData == null)
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

            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogError("밸브 상태가 올바르지 않습니다.");
                return false;
            }

            if (!_mainForm._dryPump.Start())
            {
                LogError("드라이펌프 시작 명령 실패");
                return false;
            }

            int waitCount = 0;
            while (waitCount < 30)
            {
                await Task.Delay(1000, cancellationToken);
                await _mainForm._dryPump.UpdateStatusAsync();

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

            var measurements = await GetCurrentMeasurementsAsync();

            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogError("밸브 상태가 올바르지 않습니다.");
                return false;
            }

            if (_mainForm._dryPump?.Status?.IsRunning != true)
            {
                LogError("드라이펌프가 작동하지 않습니다.");
                return false;
            }

            if (_mainForm._bathCirculator?.Status?.IsRunning != true)
            {
                LogError("칠러가 작동하지 않습니다.");
                return false;
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
            if (!_mainForm._turboPump.Start())
            {
                LogError("터보펌프 시작 명령 실패");
                return false;
            }

            int speedWaitCount = 0;
            int targetSpeed = 620;
            bool igActivatedDuringAccel = false;

            while (speedWaitCount < 600)
            {
                await Task.Delay(1000, cancellationToken);
                await _mainForm._turboPump.UpdateStatusAsync();

                var currentSpeed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                UpdateProgress($"터보펌프 가속 중... ({currentSpeed} RPM)", 50 + (currentSpeed * 50 / targetSpeed));

                // ── 가속 중 이온게이지 조기 활성화 ──
                // 터보펌프 가속 도중 압력이 IG 활성화 임계값 이하로 내려가면 미리 켬
                if (!igActivatedDuringAccel && _mainForm._ioModule?.IsConnected == true)
                {
                    var aoData = _mainForm._ioModule.LastValidAOValues;
                    if (aoData?.IsIonGaugeHVOn != true)
                    {
                        var currentMeasurements = await GetCurrentMeasurementsAsync();
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

                if (currentSpeed >= targetSpeed * 0.95)
                {
                    LogInfo($"터보펌프 정격 속도 도달: {currentSpeed} RPM");
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
        /// - AO 상태로 HV ON 확인
        /// </summary>
        private async Task<bool> ActivateIonGaugeAsync(CancellationToken cancellationToken)
        {
            // 이미 활성화되어 있으면 스킵 (터보펌프 가속 중 조기 활성화된 경우)
            var aoCheck = _mainForm._ioModule?.LastValidAOValues;
            if (aoCheck?.IsIonGaugeHVOn == true)
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

            UpdateProgress("이온게이지 상태 확인 중...", 85);

            for (int i = 0; i < 5; i++)
            {
                var aoData = _mainForm._ioModule?.LastValidAOValues;
                if (aoData?.IsIonGaugeHVOn == true)
                {
                    LogInfo("이온게이지 HV ON 확인 완료");
                    UpdateProgress("이온게이지 활성화 완료", 100);
                    return true;
                }
                await Task.Delay(1000, cancellationToken);
            }

            LogWarning("이온게이지 HV ON 명령은 성공했으나 AO 상태 확인 실패 — 계속 진행합니다.");
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

            UpdateProgress("히터 시작 조건 확인 중...", 0);

            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.CurrentPressure > _config.TargetPressureForHeater)
            {
                LogError($"압력이 너무 높습니다. (현재: {measurements.CurrentPressure:E2} Torr)");
                return false;
            }

            // CH1 온도 설정 (CH2는 칠러 PID가 제어)
            UpdateProgress("히터 CH1 온도 설정 중...", 20);

            await _mainForm._tempController.UpdateStatusAsync();

            var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];

            short ch1SetValue = ch1Status.Dot == 1
                ? (short)(_config.HeaterCh1SetTemperature * 10)
                : (short)_config.HeaterCh1SetTemperature;

            // SetTemperature 호출 (Modbus 응답 검증 실패 가능하므로 반환값 무시)
            _mainForm._tempController.SetTemperature(1, ch1SetValue);
            LogInfo($"히터 CH1 온도 설정 명령 전송: {_config.HeaterCh1SetTemperature}°C (Dot:{ch1Status.Dot}, raw:{ch1SetValue})");

            // 500ms 대기 후 실제 SV 확인
            await Task.Delay(500, cancellationToken);
            await _mainForm._tempController.UpdateStatusAsync();

            var updatedCh1 = _mainForm._tempController.Status.ChannelStatus[0];
            double actualSV = updatedCh1.Dot == 1 ? updatedCh1.SetValue / 10.0 : updatedCh1.SetValue;

            if (Math.Abs(actualSV - _config.HeaterCh1SetTemperature) > 0.5)
            {
                LogWarning($"CH1 SV 불일치 감지 (설정:{_config.HeaterCh1SetTemperature}°C, 실제:{actualSV}°C), 재시도...");
                _mainForm._tempController.SetTemperature(1, ch1SetValue);
                await Task.Delay(500, cancellationToken);
                await _mainForm._tempController.UpdateStatusAsync();

                updatedCh1 = _mainForm._tempController.Status.ChannelStatus[0];
                actualSV = updatedCh1.Dot == 1 ? updatedCh1.SetValue / 10.0 : updatedCh1.SetValue;

                if (Math.Abs(actualSV - _config.HeaterCh1SetTemperature) > 0.5)
                {
                    LogError($"CH1 온도 설정 실패 (설정:{_config.HeaterCh1SetTemperature}°C, 실제:{actualSV}°C)");
                    return false;
                }
            }

            LogInfo($"히터 CH1 온도 설정 확인: SV={actualSV}°C (목표: {_config.HeaterCh1SetTemperature}°C)");

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

                // 칠러 PID 제어 시작 (CH2 온도 제어)
                StartChillerPID();

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
            UpdateProgress("온도 도달 대기 중...", 0);

            // ── 1단계: CH1 설정 온도 이상 도달 대기 ──
            LogInfo($"CH1 설정 온도({_config.HeaterCh1SetTemperature:F1}°C) 이상 도달 대기 중...");
            bool temperatureReached = false;
            var waitStartTime = DateTime.Now;

            while (!temperatureReached && (DateTime.Now - waitStartTime).TotalMinutes < 60)
            {
                var measurements = await GetCurrentMeasurementsAsync();

                var waitElapsed = DateTime.Now - waitStartTime;
                double progressRatio = _config.HeaterCh1SetTemperature > 0
                    ? Math.Min(measurements.HeaterCh1Temperature / _config.HeaterCh1SetTemperature * 10, 10)
                    : 0;

                UpdateProgress($"온도 상승 중  CH1: {measurements.HeaterCh1Temperature:F1}°C → {_config.HeaterCh1SetTemperature:F1}°C  |  " +
                    $"압력: {measurements.CurrentPressure:E2} Torr  |  대기: {waitElapsed:mm\\:ss}",
                    progressRatio);

                if (measurements.HeaterCh1Temperature >= _config.HeaterCh1SetTemperature)
                {
                    temperatureReached = true;
                    LogInfo($"CH1 설정 온도 도달: {measurements.HeaterCh1Temperature:F1}°C ≥ {_config.HeaterCh1SetTemperature:F1}°C (대기: {waitElapsed:mm\\:ss})");
                }

                await Task.Delay(5000, cancellationToken);
            }

            if (!temperatureReached)
            {
                LogError("CH1 설정 온도 도달 실패 (60분 타임아웃)");
                return false;
            }

            // ── 2단계: 실험 시간 카운트 시작 (온도 도달 후부터) ──
            _experimentStartTime = DateTime.Now;
            _isExperimentTimerRunning = true;
            var experimentDuration = TimeSpan.FromMinutes(_config.ExperimentDurationMinutes);

            LogInfo($"★ 실험 진행 시작 — 목표: {_config.ExperimentDurationMinutes}분 ({_config.ExperimentDurationMinutes / 60}시간 {_config.ExperimentDurationMinutes % 60}분)");

            while ((DateTime.Now - _experimentStartTime) < experimentDuration)
            {
                // 일시정지 처리 (정지 시간만큼 시작시간 보정)
                if (_isPaused)
                {
                    LogInfo("실험 일시정지됨");
                    var pauseStart = DateTime.Now;
                    while (_isPaused && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(500, cancellationToken);
                    }
                    var pauseDuration = DateTime.Now - pauseStart;
                    _experimentStartTime += pauseDuration;
                    LogInfo($"실험 재개 (정지 시간: {pauseDuration:mm\\:ss})");
                }

                var elapsed = DateTime.Now - _experimentStartTime;
                var remaining = experimentDuration - elapsed;
                var progress = elapsed.TotalSeconds / experimentDuration.TotalSeconds * 100;

                var measurements = await GetCurrentMeasurementsAsync();

                // 압력 체크
                if (measurements.CurrentPressure > _config.MaxPressureDuringExperiment)
                {
                    LogWarning($"압력 상승 감지: {measurements.CurrentPressure:E2} Torr");
                }

                // CH1 온도 체크
                var ch1Diff = Math.Abs(measurements.HeaterCh1Temperature - _config.HeaterCh1SetTemperature);
                if (ch1Diff > _config.TemperatureStabilityTolerance)
                {
                    LogWarning($"CH1 온도 편차 감지: {measurements.HeaterCh1Temperature:F1}°C (목표: {_config.HeaterCh1SetTemperature:F1}°C)");
                }

                // 상세 진행 상태 표시
                UpdateProgress(
                    $"실험 진행 [{elapsed:hh\\:mm\\:ss} / {experimentDuration:hh\\:mm\\:ss}]  남은: {remaining:hh\\:mm\\:ss}  |  " +
                    $"CH1: {measurements.HeaterCh1Temperature:F1}°C  |  " +
                    $"압력: {measurements.CurrentPressure:E2} Torr  |  " +
                    $"칠러: {measurements.ChillerTemperature:F1}°C",
                    progress);

                await Task.Delay(_config.DataLoggingIntervalSeconds * 1000, cancellationToken);
            }

            LogInfo("실험 완료");
            UpdateProgress("실험 완료", 100);
            return true;
        }

        /// <summary>
        /// 단계 9: 종료 시퀀스
        /// </summary>
        private async Task<bool> ShutdownSequenceAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("종료 시퀀스 시작...", 0);

            // 1. 히터 CH1 끄기 (칠러 PID는 쿨링을 위해 유지)
            if (_mainForm._tempController?.IsConnected == true)
            {
                UpdateProgress("히터 CH1 종료 중...", 3);
                _mainForm._tempController.Stop(1);
                await Task.Delay(1000, cancellationToken);
                LogInfo("히터 CH1 정지");
            }

            // 2. 이온게이지 HV OFF
            UpdateProgress("이온게이지 HV 끄는 중...", 6);
            await _mainForm._ioModule.ControlIonGaugeHVAsync(false);
            await Task.Delay(1000, cancellationToken);

            // 3. 터보펌프 정지 + 완전 감속 대기
            if (_mainForm._turboPump?.IsConnected == true && _mainForm._turboPump.Status?.IsRunning == true)
            {
                UpdateProgress("터보펌프 정지 중...", 9);
                _mainForm._turboPump.Stop();

                int waitCount = 0;
                while (_mainForm._turboPump.Status?.CurrentSpeed > 0 && waitCount < 600)
                {
                    await Task.Delay(1000, cancellationToken);
                    await _mainForm._turboPump.UpdateStatusAsync();
                    var speed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;

                    if (waitCount % 10 == 0)
                    {
                        LogInfo($"터보펌프 감속 중: {speed} RPM");
                    }

                    UpdateProgress($"터보펌프 감속 중... ({speed} RPM)", 9 + (int)(13.0 * (1.0 - speed / 24000.0)));
                    waitCount++;
                }

                LogInfo("터보펌프 완전 정지 확인");
            }

            // 4. 게이트 밸브 닫기 (터보펌프 완전 정지 후)
            UpdateProgress("게이트 밸브 닫는 중...", 22);
            if (!await _mainForm._ioModule.ControlGateValveAsync(false))
            {
                LogWarning("게이트 밸브 닫기 실패");
            }
            await Task.Delay(2000, cancellationToken);

            // 5. 드라이펌프 정지 (벤트 전에 반드시 정지 — 대기압 역류 방지)
            if (_mainForm._dryPump?.IsConnected == true && _mainForm._dryPump.Status?.IsRunning == true)
            {
                UpdateProgress("드라이펌프 정지 중...", 24);
                _mainForm._dryPump.Stop();
                await Task.Delay(3000, cancellationToken);
                LogInfo("드라이펌프 정지 완료");
            }

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
        private async Task EmergencyShutdownAsync()
        {
            try
            {
                LogWarning("비상 종료 시퀀스 실행");

                // 칠러 PID 정지
                StopChillerPID();

                // 히터 CH1 즉시 종료
                if (_mainForm._tempController?.IsConnected == true)
                {
                    _mainForm._tempController.Stop(1);
                }

                // 이온게이지 HV OFF
                if (_mainForm._ioModule?.IsConnected == true)
                {
                    await _mainForm._ioModule.ControlIonGaugeHVAsync(false);

                    // 게이트밸브 닫기 (챔버 보호)
                    await _mainForm._ioModule.ControlGateValveAsync(false);
                }

                // 터보펌프 정지
                _mainForm._turboPump?.Stop();

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
        /// 현재 측정값 가져오기
        /// </summary>
        private async Task<AutoRunMeasurements> GetCurrentMeasurementsAsync()
        {
            var measurements = new AutoRunMeasurements();

            try
            {
                if (_mainForm._ioModule?.IsConnected == true)
                {
                    var aiData = _mainForm._ioModule.LastValidAIValues;
                    var aoData = _mainForm._ioModule.LastValidAOValues;

                    if (aiData != null)
                    {
                        measurements.AtmPressure = _mainForm._atmSwitch?.ConvertVoltageToPressureInkPa(aiData.ExpansionVoltageValues[0]) ?? 0;
                        measurements.CurrentPressure = _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;

                        if (aoData?.IsIonGaugeHVOn == true && measurements.CurrentPressure < 1E-3)
                        {
                            var ionPressure = _mainForm._ionGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[2]) ?? 0;
                            if (ionPressure > 0 && ionPressure < measurements.CurrentPressure)
                            {
                                measurements.CurrentPressure = ionPressure;
                            }
                        }

                        if (aiData.MasterCurrentValues[3] > 1)
                            measurements.GateValveStatus = "Opened";
                        else if (aiData.MasterCurrentValues[0] > 1)
                            measurements.GateValveStatus = "Closed";
                        else
                            measurements.GateValveStatus = "Moving";
                    }

                    if (aoData != null)
                    {
                        measurements.VentValveStatus = aoData.IsVentValveOpen ? "Opened" : "Closed";
                        measurements.ExhaustValveStatus = aoData.IsExhaustValveOpen ? "Opened" : "Closed";
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
            var overallProgress = ((_currentStepNumber - 1) * 100.0 + stepProgress) / TOTAL_STEPS;

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
        /// 실행 요약 생성
        /// </summary>
        private string GenerateSummary(StepResult result)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== AutoRun 실행 요약 ===");
            sb.AppendLine($"시작 시간: {_startTime:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"종료 시간: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"총 소요 시간: {_stopwatch.Elapsed:hh\\:mm\\:ss}");
            sb.AppendLine($"최종 상태: {result}");
            sb.AppendLine($"완료 단계: {_currentStepNumber}/{TOTAL_STEPS}");

            if (result == StepResult.Success && _experimentStartTime != default)
            {
                sb.AppendLine($"실험 시작: {_experimentStartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"실험 시간: {_config.ExperimentDurationMinutes}분");
            }

            return sb.ToString();
        }

        #endregion

        #region 로깅 메서드

        private void LogInfo(string message)
        {
            AsyncLoggingService.Instance.LogInfo($"[AutoRun] {message}");
        }

        private void LogWarning(string message)
        {
            AsyncLoggingService.Instance.LogWarning($"[AutoRun] {message}");
        }

        private void LogError(string message, Exception ex = null)
        {
            AsyncLoggingService.Instance.LogError($"[AutoRun] {message}", ex);
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
}