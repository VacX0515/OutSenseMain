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
        private readonly CancellationTokenSource _cancellationTokenSource;
        private Task _runningTask;
        private readonly object _lockObject = new object();
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private DateTime _startTime;
        private DateTime _experimentStartTime;

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

        #endregion

        #region 이벤트

        /// <summary>
        /// 상태 변경 이벤트
        /// </summary>
        public event EventHandler<AutoRunStateChangedEventArgs> StateChanged;

        /// <summary>
        /// 진행 상황 업데이트 이벤트
        /// </summary>
        public event EventHandler<AutoRunProgressEventArgs> ProgressUpdated;

        /// <summary>
        /// 오류 발생 이벤트
        /// </summary>
        public event EventHandler<AutoRunErrorEventArgs> ErrorOccurred;

        /// <summary>
        /// 완료 이벤트
        /// </summary>
        public event EventHandler<AutoRunCompletedEventArgs> Completed;

        #endregion

        #region 생성자 및 소멸자

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunService(MainForm mainForm, AutoRunConfiguration config = null)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            _config = config ?? new AutoRunConfiguration();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// 소멸자
        /// </summary>
        public void Dispose()
        {
            Stop();
            _cancellationTokenSource?.Dispose();
            _stopwatch?.Stop();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// AutoRun 시작
        /// </summary>
        public async Task<bool> StartAsync()
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

            LogInfo("=== AutoRun 시퀀스 시작 ===");
            _startTime = DateTime.Now;
            _stopwatch.Restart();
            _currentStepNumber = 0;

            try
            {
                _runningTask = RunSequenceAsync(_cancellationTokenSource.Token);
                await _runningTask;
                return true;
            }
            catch (Exception ex)
            {
                LogError($"AutoRun 실행 중 오류 발생: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// AutoRun 중지
        /// </summary>
        public void Stop()
        {
            lock (_lockObject)
            {
                if (!_isRunning) return;

                LogWarning("AutoRun 중지 요청됨");
                _cancellationTokenSource.Cancel();
                _isRunning = false;
                _isPaused = false;
            }

            try
            {
                _runningTask?.Wait(5000);
            }
            catch { }

            CurrentState = AutoRunState.Aborted;
            _stopwatch.Stop();

            // UI 컨트롤 활성화
            EnableManualControls(true);
        }

        /// <summary>
        /// AutoRun 일시정지
        /// </summary>
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

        /// <summary>
        /// AutoRun 재개
        /// </summary>
        public void Resume()
        {
            lock (_lockObject)
            {
                if (!_isRunning || !_isPaused) return;
                _isPaused = false;
            }

            LogInfo("AutoRun 재개됨");
        }

        #endregion

        #region 시퀀스 실행

        /// <summary>
        /// 메인 시퀀스 실행
        /// </summary>
        private async Task RunSequenceAsync(CancellationToken cancellationToken)
        {
            StepResult result = StepResult.Success;

            try
            {
                // UI 컨트롤 비활성화
                EnableManualControls(false);

                // 단계 1: 초기화
                _currentStepNumber = 1;
                result = await ExecuteStepAsync(AutoRunState.Initializing, InitializeAsync, _config.InitializationTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 2: 진공 준비
                _currentStepNumber = 2;
                result = await ExecuteStepAsync(AutoRunState.PreparingVacuum, PrepareVacuumAsync, _config.ValveOperationTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 3: 드라이펌프 시작
                _currentStepNumber = 3;
                result = await ExecuteStepAsync(AutoRunState.StartingDryPump, StartDryPumpAsync, _config.DryPumpStartTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 4: 터보펌프 시작
                _currentStepNumber = 4;
                result = await ExecuteStepAsync(AutoRunState.StartingTurboPump, StartTurboPumpAsync, _config.TurboPumpStartTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 5: 이온게이지 활성화
                _currentStepNumber = 5;
                result = await ExecuteStepAsync(AutoRunState.ActivatingIonGauge, ActivateIonGaugeAsync, _config.IonGaugeActivationTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 6: 고진공 대기
                _currentStepNumber = 6;
                result = await ExecuteStepAsync(AutoRunState.WaitingHighVacuum, WaitForHighVacuumAsync, _config.HighVacuumTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 7: 히터 시작
                _currentStepNumber = 7;
                result = await ExecuteStepAsync(AutoRunState.StartingHeater, StartHeaterAsync, _config.HeaterStartTimeout, cancellationToken);
                if (result != StepResult.Success) goto Cleanup;

                // 단계 8: 실험 진행
                _currentStepNumber = 8;
                result = await ExecuteStepAsync(AutoRunState.RunningExperiment, RunExperimentAsync,
                    _config.ExperimentDurationHours * 3600 + 3600, cancellationToken); // 실험시간 + 1시간 여유
                if (result != StepResult.Success) goto Cleanup;

                // 단계 9: 종료 시퀀스
                _currentStepNumber = 9;
                result = await ExecuteStepAsync(AutoRunState.ShuttingDown, ShutdownSequenceAsync, _config.ShutdownTimeout, cancellationToken);

                // 완료
                CurrentState = AutoRunState.Completed;
                LogInfo("=== AutoRun 시퀀스 정상 완료 ===");

            Cleanup:
                if (result != StepResult.Success)
                {
                    CurrentState = result == StepResult.Aborted ? AutoRunState.Aborted : AutoRunState.Error;

                    if (_config.EnableSafeShutdownOnFailure && result != StepResult.Aborted)
                    {
                        LogWarning("안전 종료 시퀀스 실행 중...");
                        await EmergencyShutdownAsync();
                    }
                }

                // 완료 이벤트 발생
                var completedArgs = new AutoRunCompletedEventArgs(
                    result == StepResult.Success,
                    _startTime,
                    DateTime.Now)
                {
                    Summary = GenerateSummary(result)
                };
                OnCompleted(completedArgs);

                // UI 컨트롤 재활성화
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
                lock (_lockObject)
                {
                    _isRunning = false;
                    _isPaused = false;
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
                // 재시도 로직
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
                        // 일시정지 체크
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

            // 모든 장치 연결 확인
            if (!_mainForm._ioModule?.IsConnected ?? true)
            {
                LogError("IO Module이 연결되지 않았습니다.");
                return false;
            }

            UpdateProgress("펌프 상태 확인 중...", 20);

            // 펌프 상태 확인
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

                // 칠러 온도 설정
                if (!_mainForm._bathCirculator.SetTemperature(_config.ChillerSetTemperature))
                {
                    LogError("칠러 온도 설정 실패");
                    return false;
                }
            }
            else
            {
                LogWarning("칠러가 연결되지 않았습니다. 계속 진행합니다.");
            }

            UpdateProgress("히터 상태 확인 중...", 60);

            // 히터 상태 확인
            if (_mainForm._tempController?.IsConnected == true)
            {
                var status = _mainForm._tempController.Status;
                if (status.ChannelStatus[0].IsRunning || status.ChannelStatus[1].IsRunning)
                {
                    LogError("히터가 이미 작동 중입니다. 먼저 정지해주세요.");
                    return false;
                }
            }

            UpdateProgress("압력 확인 중...", 80);

            // 초기 압력 확인
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

            // 게이트 밸브 열기
            if (!await _mainForm._ioModule.ControlGateValveAsync(true))
            {
                LogError("게이트 밸브 열기 실패");
                return false;
            }

            await Task.Delay(2000, cancellationToken);
            UpdateProgress("게이트 밸브 상태 확인...", 33);

            // 벤트 밸브 닫기
            UpdateProgress("벤트 밸브 닫기...", 33);
            if (!await _mainForm._ioModule.ControlVentValveAsync(false))
            {
                LogError("벤트 밸브 닫기 실패");
                return false;
            }

            await Task.Delay(1000, cancellationToken);

            // 배기 밸브 닫기
            UpdateProgress("배기 밸브 닫기...", 66);
            if (!await _mainForm._ioModule.ControlExhaustValveAsync(false))
            {
                LogError("배기 밸브 닫기 실패");
                return false;
            }

            await Task.Delay(1000, cancellationToken);

            // 밸브 상태 확인
            UpdateProgress("밸브 상태 최종 확인...", 90);
            var aoData = await _mainForm._ioModule.ReadAnalogOutputsAsync();
            if (aoData == null)
            {
                LogError("밸브 상태 읽기 실패");
                return false;
            }

            // 실제 상태 확인은 MainForm의 로직에 따라 수행
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

            // Precondition 확인
            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogError("밸브 상태가 올바르지 않습니다.");
                return false;
            }

            // 드라이펌프 시작
            if (!_mainForm._dryPump.Start())
            {
                LogError("드라이펌프 시작 명령 실패");
                return false;
            }

            // 펌프가 정상 작동할 때까지 대기
            int waitCount = 0;
            while (waitCount < 30) // 최대 30초 대기
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

            // Precondition 확인
            var measurements = await GetCurrentMeasurementsAsync();

            // 밸브 상태 확인
            if (measurements.GateValveStatus != "Opened" ||
                measurements.VentValveStatus != "Closed" ||
                measurements.ExhaustValveStatus != "Closed")
            {
                LogError("밸브 상태가 올바르지 않습니다.");
                return false;
            }

            // 드라이펌프 작동 확인
            if (_mainForm._dryPump?.Status?.IsRunning != true)
            {
                LogError("드라이펌프가 작동하지 않습니다.");
                return false;
            }

            // 칠러 작동 확인
            if (_mainForm._bathCirculator?.Status?.IsRunning != true)
            {
                LogError("칠러가 작동하지 않습니다.");
                return false;
            }

            // 압력 조건 확인
            UpdateProgress($"압력 확인 중... (현재: {measurements.CurrentPressure:E2} Torr)", 20);
            if (measurements.CurrentPressure > _config.TargetPressureForTurboPump)
            {
                LogInfo($"압력이 {_config.TargetPressureForTurboPump} Torr 이하가 될 때까지 대기 중...");

                int waitCount = 0;
                while (measurements.CurrentPressure > _config.TargetPressureForTurboPump && waitCount < 300) // 최대 5분
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

            // 터보펌프 시작
            UpdateProgress("터보펌프 시작 중...", 50);
            if (!_mainForm._turboPump.Start())
            {
                LogError("터보펌프 시작 명령 실패");
                return false;
            }

            // 터보펌프가 정격 속도에 도달할 때까지 대기
            int speedWaitCount = 0;
            int targetSpeed = 620; // RPM

            while (speedWaitCount < 600) // 최대 10분
            {
                await Task.Delay(1000, cancellationToken);
                await _mainForm._turboPump.UpdateStatusAsync();

                var currentSpeed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                UpdateProgress($"터보펌프 가속 중... ({currentSpeed} RPM)", 50 + (currentSpeed * 50 / targetSpeed));

                if (currentSpeed >= targetSpeed * 0.95) // 95% 이상 도달 시 성공
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
        /// </summary>
        private async Task<bool> ActivateIonGaugeAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("이온게이지 활성화 조건 확인 중...", 0);

            // 압력 확인
            var measurements = await GetCurrentMeasurementsAsync();
            UpdateProgress($"현재 압력: {measurements.CurrentPressure:E2} Torr", 20);

            if (measurements.CurrentPressure > _config.TargetPressureForIonGauge)
            {
                LogError($"압력이 너무 높습니다. (현재: {measurements.CurrentPressure:E2} Torr, 목표: {_config.TargetPressureForIonGauge:E2} Torr)");
                return false;
            }

            // 이온게이지 HV ON
            UpdateProgress("이온게이지 HV 켜는 중...", 50);
            if (!await _mainForm._ioModule.ControlIonGaugeHVAsync(true))
            {
                LogError("이온게이지 HV ON 실패");
                return false;
            }

            await Task.Delay(2000, cancellationToken);

            // 상태 확인
            UpdateProgress("이온게이지 상태 확인 중...", 80);
            measurements = await GetCurrentMeasurementsAsync();

            //if (measurements.IonGaugeStatus == "HV on" || measurements.IonGaugeStatus == "Normal")
            //{
            //    LogInfo("이온게이지 정상 작동 확인");
            //    UpdateProgress("이온게이지 활성화 완료", 100);
            //    return true;
            //}

            //LogError($"이온게이지 상태 이상: {measurements.IonGaugeStatus}");
            return false;
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

                await Task.Delay(5000, cancellationToken); // 5초마다 확인
                measurements = await GetCurrentMeasurementsAsync();

                // 시간 초과는 상위 ExecuteStepAsync에서 처리
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

            // 압력 확인
            var measurements = await GetCurrentMeasurementsAsync();
            if (measurements.CurrentPressure > _config.TargetPressureForHeater)
            {
                LogError($"압력이 너무 높습니다. (현재: {measurements.CurrentPressure:E2} Torr)");
                return false;
            }
            // 온도 설정
            UpdateProgress("히터 온도 설정 중...", 20);

            // Dot 값 확보를 위해 최신 상태 읽기
            // (Dot 기본값 0 → 실제 센서가 Dot=1이면 변환 누락 방지)
            await _mainForm._tempController.UpdateStatusAsync();

            // Dot 값에 따라 raw 레지스터 값 변환
            // Config의 온도는 실제 온도(°C) 기준
            // Dot=0: raw = 실제온도 (예: 100°C → 100)
            // Dot=1: raw = 실제온도 × 10 (예: 100.0°C → 1000)
            var ch1Status = _mainForm._tempController.Status.ChannelStatus[0];
            var ch2Status = _mainForm._tempController.Status.ChannelStatus[1];

            short ch1SetValue = ch1Status.Dot == 1
                ? (short)(_config.HeaterCh1SetTemperature * 10)
                : (short)_config.HeaterCh1SetTemperature;

            if (!_mainForm._tempController.SetTemperature(1, ch1SetValue))
            {
                LogError("CH1 온도 설정 실패");
                return false;
            }

            short ch2SetValue = ch2Status.Dot == 1
                ? (short)(_config.HeaterCh2SetTemperature * 10)
                : (short)_config.HeaterCh2SetTemperature;

            if (!_mainForm._tempController.SetTemperature(2, ch2SetValue))
            {
                LogError("CH2 온도 설정 실패");
                return false;
            }

            LogInfo($"히터 온도 설정 완료 - CH1: {_config.HeaterCh1SetTemperature}°C (Dot:{ch1Status.Dot}, raw:{ch1SetValue}), " +
                    $"CH2: {_config.HeaterCh2SetTemperature}°C (Dot:{ch2Status.Dot}, raw:{ch2SetValue})");

            await Task.Delay(1000, cancellationToken);

            // 히터 시작
            UpdateProgress("히터 CH1 시작 중...", 40);
            if (!_mainForm._tempController.Start(1))
            {
                LogError("히터 CH1 시작 실패");
                return false;
            }

            UpdateProgress("히터 CH2 시작 중...", 60);
            if (!_mainForm._tempController.Start(2))
            {
                LogError("히터 CH2 시작 실패");
                return false;
            }

            // 히터 작동 확인
            await Task.Delay(3000, cancellationToken);
            UpdateProgress("히터 작동 상태 확인 중...", 80);

            await _mainForm._tempController.UpdateStatusAsync();
            var status = _mainForm._tempController.Status;

            if (status.ChannelStatus[0].IsRunning && status.ChannelStatus[1].IsRunning)
            {
                LogInfo("히터 정상 작동 확인");
                UpdateProgress("히터 시작 완료", 100);
                return true;
            }

            LogError("히터 작동 확인 실패");
            return false;
        }

        /// <summary>
        /// 단계 8: 실험 진행
        /// </summary>
        private async Task<bool> RunExperimentAsync(CancellationToken cancellationToken)
        {
            UpdateProgress("실험 시작...", 0);
            _experimentStartTime = DateTime.Now;

            // 설정 온도 도달 대기
            LogInfo("설정 온도 도달 대기 중...");
            bool temperatureReached = false;
            var waitStartTime = DateTime.Now;

            while (!temperatureReached && (DateTime.Now - waitStartTime).TotalMinutes < 60) // 최대 60분 대기
            {
                var measurements = await GetCurrentMeasurementsAsync();

                var ch1Diff = Math.Abs(measurements.HeaterCh1Temperature - _config.HeaterCh1SetTemperature);
                var ch2Diff = Math.Abs(measurements.HeaterCh2Temperature - _config.HeaterCh2SetTemperature);

                UpdateProgress($"온도 상승 중... CH1: {measurements.HeaterCh1Temperature:F1}°C, CH2: {measurements.HeaterCh2Temperature:F1}°C",
                    (measurements.HeaterCh1Temperature + measurements.HeaterCh2Temperature) /
                    (_config.HeaterCh1SetTemperature + _config.HeaterCh2SetTemperature) * 10);

                if (ch1Diff <= _config.TemperatureStabilityTolerance &&
                    ch2Diff <= _config.TemperatureStabilityTolerance)
                {
                    temperatureReached = true;
                    LogInfo("설정 온도 도달 확인");
                }

                await Task.Delay(5000, cancellationToken);
            }

            if (!temperatureReached)
            {
                LogError("설정 온도 도달 실패");
                return false;
            }

            // 실험 시간 카운트 시작
            _experimentStartTime = DateTime.Now;
            var experimentDuration = TimeSpan.FromHours(_config.ExperimentDurationHours);

            LogInfo($"실험 진행 시작 (지속 시간: {_config.ExperimentDurationHours}시간)");

            // 실험 진행 중 모니터링
            while ((DateTime.Now - _experimentStartTime) < experimentDuration)
            {
                var elapsed = DateTime.Now - _experimentStartTime;
                var progress = elapsed.TotalSeconds / experimentDuration.TotalSeconds * 100;

                var measurements = await GetCurrentMeasurementsAsync();

                // 압력 체크
                if (measurements.CurrentPressure > _config.MaxPressureDuringExperiment)
                {
                    LogWarning($"압력 상승 감지: {measurements.CurrentPressure:E2} Torr");
                }

                // 온도 체크
                var ch1Diff = Math.Abs(measurements.HeaterCh1Temperature - _config.HeaterCh1SetTemperature);
                var ch2Diff = Math.Abs(measurements.HeaterCh2Temperature - _config.HeaterCh2SetTemperature);

                if (ch1Diff > _config.TemperatureStabilityTolerance ||
                    ch2Diff > _config.TemperatureStabilityTolerance)
                {
                    LogWarning($"온도 편차 감지 - CH1: {measurements.HeaterCh1Temperature:F1}°C, CH2: {measurements.HeaterCh2Temperature:F1}°C");
                }

                var remainingTime = experimentDuration - elapsed;
                UpdateProgress($"실험 진행 중... (남은 시간: {remainingTime:hh\\:mm\\:ss})", progress);

                // 데이터 로깅 간격으로 대기
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

            // 히터 끄기
            if (_mainForm._tempController?.IsConnected == true)
            {
                UpdateProgress("히터 종료 중...", 10);
                _mainForm._tempController.Stop(1);
                _mainForm._tempController.Stop(2);
                await Task.Delay(1000, cancellationToken);
            }

            // 이온게이지 HV OFF
            UpdateProgress("이온게이지 HV 끄는 중...", 30);
            await _mainForm._ioModule.ControlIonGaugeHVAsync(false);
            await Task.Delay(1000, cancellationToken);

            // 터보펌프 정지
            if (_mainForm._turboPump?.IsConnected == true && _mainForm._turboPump.Status?.IsRunning == true)
            {
                UpdateProgress("터보펌프 정지 중...", 40);
                _mainForm._turboPump.Stop();

                // 터보펌프 완전 정지 대기
                int waitCount = 0;
                while (_mainForm._turboPump.Status?.CurrentSpeed > 100 && waitCount < 300) // 최대 5분
                {
                    await Task.Delay(1000, cancellationToken);
                    await _mainForm._turboPump.UpdateStatusAsync();
                    var speed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                    UpdateProgress($"터보펌프 감속 중... ({speed} RPM)", 40 + (30 - speed * 30 / 24000));
                    waitCount++;
                }
            }

            // Success Condition 확인
            UpdateProgress("최종 상태 확인 중...", 90);
            await Task.Delay(2000, cancellationToken);

            var finalCheck = true;

            // 터보펌프 OFF 확인
            if (_mainForm._turboPump?.Status?.IsRunning == true)
            {
                LogError("터보펌프가 여전히 작동 중입니다.");
                finalCheck = false;
            }

            // 히터 OFF 확인
            if (_mainForm._tempController?.IsConnected == true)
            {
                await _mainForm._tempController.UpdateStatusAsync();
                if (_mainForm._tempController.Status.ChannelStatus[0].IsRunning ||
                    _mainForm._tempController.Status.ChannelStatus[1].IsRunning)
                {
                    LogError("히터가 여전히 작동 중입니다.");
                    finalCheck = false;
                }
            }

            // 드라이펌프 ON 확인
            if (_mainForm._dryPump?.Status?.IsRunning != true)
            {
                LogWarning("드라이펌프가 정지되어 있습니다.");
                // 이것은 오류가 아닌 경고로 처리
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

                // 히터 즉시 종료
                if (_mainForm._tempController?.IsConnected == true)
                {
                    _mainForm._tempController.Stop(1);
                    _mainForm._tempController.Stop(2);
                }

                // 이온게이지 HV OFF
                await _mainForm._ioModule?.ControlIonGaugeHVAsync(false);

                // 터보펌프 정지
                _mainForm._turboPump?.Stop();

                // 게이트 밸브는 열어둠 (진공 유지)

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
                // IO Module 데이터 읽기
                if (_mainForm._ioModule?.IsConnected == true)
                {
                    var aiData = _mainForm._ioModule.LastValidAIValues;
                    var aoData =  _mainForm._ioModule.LastValidAOValues;

                    if (aiData != null)
                    {
                        measurements.AtmPressure = _mainForm._atmSwitch?.ConvertVoltageToPressureInkPa(aiData.ExpansionVoltageValues[0]) ?? 0;
                        measurements.CurrentPressure = _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;

                        // 이온게이지 활성화된 경우 이온게이지 값 사용
                        if (aoData?.IsIonGaugeHVOn == true && measurements.CurrentPressure < 1E-3)
                        {
                            var ionPressure = _mainForm._ionGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[2]) ?? 0;
                            if (ionPressure > 0 && ionPressure < measurements.CurrentPressure)
                            {
                                measurements.CurrentPressure = ionPressure;
                            }
                        }

                        // 게이트 밸브 상태
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

                // 펌프 상태
                if (_mainForm._dryPump?.IsConnected == true)
                {
                    measurements.DryPumpStatus = _mainForm._dryPump.GetStatusText();
                }

                if (_mainForm._turboPump?.IsConnected == true)
                {
                    measurements.TurboPumpStatus = _mainForm._turboPump.GetStatusText();
                    measurements.TurboPumpSpeed = _mainForm._turboPump.Status?.CurrentSpeed ?? 0;
                }

                // 온도 데이터
                if (_mainForm._bathCirculator?.IsConnected == true)
                {
                    measurements.ChillerTemperature = _mainForm._bathCirculator.Status.CurrentTemperature;
                }

                if (_mainForm._tempController?.IsConnected == true)
                {
                    var status = _mainForm._tempController.Status;

                    // 온도값 변환 (소수점 위치에 따라)
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
        private void UpdateProgress(string message, double stepProgress)
        {
            var overallProgress = ((_currentStepNumber - 1) * 100.0 + stepProgress) / TOTAL_STEPS;

            var args = new AutoRunProgressEventArgs(
                CurrentState,
                message,
                stepProgress,
                overallProgress);

            if (_stopwatch.IsRunning)
            {
                args.CurrentValues = GetCurrentMeasurementsAsync().GetAwaiter().GetResult();
            }

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
            //_mainForm.BeginInvoke(new Action(() =>
            //{
            //    // 밸브 버튼들
            //    _mainForm.btn_GV.Enabled = enable;
            //    _mainForm.btn_VV.Enabled = enable;
            //    _mainForm.btn_EV.Enabled = enable;
            //    _mainForm.btn_iongauge.Enabled = enable;

            //    // 펌프 버튼들
            //    _mainForm.btnDryPumpStart.Enabled = enable;
            //    _mainForm.btnDryPumpStop.Enabled = enable;
            //    _mainForm.btnDryPumpStandby.Enabled = enable;
            //    _mainForm.btnDryPumpNormal.Enabled = enable;

            //    _mainForm.btnTurboPumpStart.Enabled = enable;
            //    _mainForm.btnTurboPumpStop.Enabled = enable;
            //    _mainForm.btnTurboPumpVent.Enabled = enable;
            //    _mainForm.btnTurboPumpReset.Enabled = enable;

            //    // 온도 관련 버튼들
            //    _mainForm.btnBathCirculatorStart.Enabled = enable;
            //    _mainForm.btnBathCirculatorStop.Enabled = enable;
            //    _mainForm.btnBathCirculatorSetTemp.Enabled = enable;
            //    _mainForm.btnBathCirculatorSetTime.Enabled = enable;

            //    _mainForm.btnCh1Start.Enabled = enable;
            //    _mainForm.btnCh1Stop.Enabled = enable;
            //    _mainForm.btnCh1SetTemp.Enabled = enable;
            //    _mainForm.btnCh1AutoTuning.Enabled = enable;

            //    _mainForm.btnCh2Start.Enabled = enable;
            //    _mainForm.btnCh2Stop.Enabled = enable;
            //    _mainForm.btnCh2SetTemp.Enabled = enable;
            //    _mainForm.btnCh2AutoTuning.Enabled = enable;
            //}));
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
                sb.AppendLine($"실험 시간: {_config.ExperimentDurationHours}시간");
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