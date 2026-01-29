using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Devices.IO_Module.Models;
using VacX_OutSense.Models;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 개선된 데이터 수집 서비스 - 우선순위 기반으로 효율적으로 동작
    /// ★ 추가 AI (±10V) 지원 포함
    /// </summary>
    public class OptimizedDataCollectionService : IDisposable
    {
        // 추가 AI 값 추적용
        private double _lastAdditionalAIValue = double.NaN;

        private readonly MainForm _mainForm;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        // 우선순위별 수집 태스크
        private Task _criticalDataTask;
        private Task _normalDataTask;
        private Task _lowPriorityDataTask;

        private volatile bool _isRunning = false;
        private volatile bool _isPaused = false;

        // 스레드 안전 데이터 저장소
        private readonly ConcurrentDictionary<string, object> _latestData = new ConcurrentDictionary<string, object>();

        // 통신 동기화 (장치별 락)
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

        // 오류 카운터 (연속 오류 시 수집 간격 늘리기)
        private readonly ConcurrentDictionary<string, int> _errorCounts = new ConcurrentDictionary<string, int>();
        private const int MAX_ERROR_COUNT = 5;

        // 이벤트
        public event EventHandler<UIDataSnapshot> DataUpdated;

        public OptimizedDataCollectionService(MainForm mainForm)
        {
            _mainForm = mainForm;

            // 장치별 통신 락 초기화
            _deviceLocks["IOModule"] = new SemaphoreSlim(1, 1);
            _deviceLocks["DryPump"] = new SemaphoreSlim(1, 1);
            _deviceLocks["TurboPump"] = new SemaphoreSlim(1, 1);
            _deviceLocks["BathCirculator"] = new SemaphoreSlim(1, 1);
            _deviceLocks["TempController"] = new SemaphoreSlim(1, 1);
        }

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;

            // 우선순위별 수집 태스크 시작
            _criticalDataTask = Task.Run(CriticalDataCollectionLoop, _cancellationTokenSource.Token);
            _normalDataTask = Task.Run(NormalDataCollectionLoop, _cancellationTokenSource.Token);
            _lowPriorityDataTask = Task.Run(LowPriorityDataCollectionLoop, _cancellationTokenSource.Token);

            LoggerService.Instance.LogInfo("최적화된 데이터 수집 서비스 시작됨");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();

            try
            {
                Task.WaitAll(new[] { _criticalDataTask, _normalDataTask, _lowPriorityDataTask }, 3000);
            }
            catch (OperationCanceledException) { }

            LoggerService.Instance.LogInfo("데이터 수집 서비스 중지됨");
        }

        /// <summary>
        /// 데이터 수집 일시 정지
        /// </summary>
        public void PauseCollection()
        {
            _isPaused = true;
            LoggerService.Instance.LogDebug("데이터 수집 일시 정지");
        }

        /// <summary>
        /// 데이터 수집 재개
        /// </summary>
        public void ResumeCollection()
        {
            _isPaused = false;
            LoggerService.Instance.LogDebug("데이터 수집 재개");
        }

        /// <summary>
        /// 최신 피라니 압력값 조회 (인터락용)
        /// </summary>
        public double GetLatestPressure()
        {
            if (_latestData.TryGetValue("AI_Data", out var aiObj) && aiObj is AnalogInputValues aiData)
            {
                return _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;
            }
            return 0;
        }

        /// <summary>
        /// 중요 데이터 수집 루프 (100ms) - 압력값만
        /// </summary>
        private async Task CriticalDataCollectionLoop()
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_isPaused)
                    {
                        // IOModule에서 압력 데이터만 빠르게 읽기
                        await CollectPressureData();

                        // UI 업데이트 트리거
                        TriggerUIUpdate();
                    }

                    await Task.Delay(100, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"압력 데이터 수집 오류: {ex.Message}", ex);
                    await Task.Delay(200, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// 일반 데이터 수집 루프 (1000ms) - 펌프 상태 등
        /// </summary>
        private async Task NormalDataCollectionLoop()
        {
            await Task.Delay(200); // 시작 시간 분산

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_isPaused)
                    {
                        var tasks = new List<Task>();

                        // 드라이펌프와 터보펌프 상태
                        if (ShouldCollect("DryPump"))
                            tasks.Add(CollectDeviceDataAsync("DryPump", CollectDryPumpData));

                        if (ShouldCollect("TurboPump"))
                            tasks.Add(CollectDeviceDataAsync("TurboPump", CollectTurboPumpData));

                        await Task.WhenAll(tasks);

                        // UI 업데이트 트리거
                        TriggerUIUpdate();
                    }

                    await Task.Delay(500, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"일반 데이터 수집 오류: {ex.Message}", ex);
                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// 저우선순위 데이터 수집 루프 (5000ms) - 온도 데이터 등
        /// </summary>
        private async Task LowPriorityDataCollectionLoop()
        {
            await Task.Delay(400); // 시작 시간 분산

            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!_isPaused)
                    {
                        var tasks = new List<Task>();

                        // 칠러와 온도 컨트롤러
                        if (ShouldCollect("BathCirculator"))
                            tasks.Add(CollectDeviceDataAsync("BathCirculator", CollectBathCirculatorData));

                        if (ShouldCollect("TempController"))
                            tasks.Add(CollectDeviceDataAsync("TempController", CollectTempControllerData));

                        await Task.WhenAll(tasks);

                        // UI 업데이트 트리거
                        TriggerUIUpdate();
                    }

                    await Task.Delay(1000, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"저우선순위 데이터 수집 오류: {ex.Message}", ex);
                    await Task.Delay(2000, _cancellationTokenSource.Token);
                }
            }
        }

        /// <summary>
        /// 압력 데이터 수집 (IO 충돌 방지) - ★ 추가 AI 포함
        /// </summary>
        private async Task CollectPressureData()
        {
            if (_mainForm._ioModule?.IsConnected != true) return;

            var semaphore = _deviceLocks["IOModule"];
            if (!await semaphore.WaitAsync(50)) return;

            try
            {
                _mainForm._ioModule.CommunicationManager.DiscardInBuffer();
                await Task.Delay(10);

                // AI 값 읽기 (압력 센서 + 추가 AI)
                var aiData = await _mainForm._ioModule.ReadAnalogInputsAsync();
                if (aiData != null && IsValidAIData(aiData))
                {
                    _latestData["AI_Data"] = aiData;
                    _errorCounts["IOModule"] = 0;

                    // ★ 추가 AI 고정밀 읽기 (Floating-point)
                    double additionalAIValue = aiData.AdditionalAIValue;  // 기본값 (Integer)

                    var floatValue = await _mainForm._ioModule.ReadAIChannelFloatAsync(
                        _mainForm._ioModule.AdditionalAIChannel);

                    if (floatValue.HasValue)
                    {
                        additionalAIValue = floatValue.Value;
                        aiData.AdditionalAIValueFloat = floatValue.Value;
                    }

                    // 값 저장
                    _latestData["AdditionalAI_Value"] = additionalAIValue;
                    _latestData["AdditionalAI_Timestamp"] = aiData.Timestamp;

                    // 값 변경 시 로깅
                    if (Math.Abs(_lastAdditionalAIValue - additionalAIValue) > 0.0001 ||
                        double.IsNaN(_lastAdditionalAIValue))
                    {
                        LoggerService.Instance.LogDebug(
                            $"추가 AI (CH{aiData.AdditionalAIChannelIndex + 1}): {additionalAIValue:F6}V");
                        _lastAdditionalAIValue = additionalAIValue;
                    }
                }
                else
                {
                    IncrementErrorCount("IOModule");
                }

                // AO 값 읽기
                var aoData = await _mainForm._ioModule.ReadAnalogOutputsAsync();
                if (aoData != null)
                {
                    _latestData["AO_Data"] = aoData;
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <summary>
        /// AI 데이터 유효성 검사 (0값 체크)
        /// </summary>
        private bool IsValidAIData(AnalogInputValues data)
        {
            if (data == null) return false;

            // 최소한 하나의 채널은 0이 아닌 값을 가져야 함
            for (int i = 0; i < 8; i++)
            {
                if (Math.Abs(data.ExpansionVoltageValues[i]) > 0.001)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 장치 데이터 수집 (락 사용)
        /// </summary>
        private async Task CollectDeviceDataAsync(string deviceName, Func<Task> collectFunc)
        {
            var semaphore = _deviceLocks[deviceName];
            if (!await semaphore.WaitAsync(100)) return;

            try
            {
                await collectFunc();
                _errorCounts[deviceName] = 0;
            }
            catch (Exception ex)
            {
                IncrementErrorCount(deviceName);
                LoggerService.Instance.LogDebug($"{deviceName} 수집 오류: {ex.Message}");

                // 추가: 연속 오류 시 연결 끊김으로 판단
                if (_errorCounts.TryGetValue(deviceName, out int errorCount) && errorCount >= MAX_ERROR_COUNT)
                {
                    LoggerService.Instance.LogWarning($"{deviceName} 통신 오류 다수 발생 - 연결 상태 확인 필요");

                    // 메인폼에 통신 오류 알림
                    _mainForm.BeginInvoke(new Action(() =>
                    {
                        _mainForm.HandleDeviceCommunicationError(deviceName);
                    }));
                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task CollectDryPumpData()
        {
            if (_mainForm._dryPump?.IsConnected == true)
            {
                _mainForm._dryPump.CommunicationManager.DiscardInBuffer();
                await Task.Delay(10);

                await _mainForm._dryPump.UpdateStatusAsync();
                _latestData["DryPump"] = _mainForm._dryPump.Status;
            }
        }

        private async Task CollectTurboPumpData()
        {
            if (_mainForm._turboPump?.IsConnected == true)
            {
                _mainForm._turboPump.CommunicationManager.DiscardInBuffer();
                await Task.Delay(10);

                await _mainForm._turboPump.UpdateStatusAsync();
                _latestData["TurboPump"] = _mainForm._turboPump.Status;
            }
        }

        private async Task CollectBathCirculatorData()
        {
            if (_mainForm._bathCirculator?.IsConnected == true)
            {
                _mainForm._bathCirculator.CommunicationManager.DiscardInBuffer();
                await Task.Delay(10);

                await _mainForm._bathCirculator.UpdateStatusAsync();
                _latestData["BathCirculator"] = _mainForm._bathCirculator.Status;
            }
        }

        /// <summary>
        /// 온도 컨트롤러 데이터 수집 (메인 + 확장 모듈 통합)
        /// UpdateStatusAsync() 하나로 메인 + 확장 모듈 모두 처리됨
        /// </summary>
        private async Task CollectTempControllerData()
        {
            if (_mainForm._tempController?.IsConnected == true)
            {
                _mainForm._tempController.CommunicationManager.DiscardInBuffer();
                await Task.Delay(10);

                await _mainForm._tempController.UpdateStatusAsync();
                _latestData["TempController"] = _mainForm._tempController.Status;
            }
        }

        /// <summary>
        /// 오류 카운트 증가
        /// </summary>
        private void IncrementErrorCount(string deviceName)
        {
            _errorCounts.AddOrUpdate(deviceName, 1, (key, oldValue) => oldValue + 1);
        }

        /// <summary>
        /// 수집 여부 결정 (오류가 많으면 수집 빈도 줄이기)
        /// </summary>
        private bool ShouldCollect(string deviceName)
        {
            if (!_errorCounts.TryGetValue(deviceName, out int errorCount))
                return true;

            // 오류가 많으면 수집 빈도 줄이기
            if (errorCount >= MAX_ERROR_COUNT)
            {
                // 10번에 1번만 수집 시도
                return DateTime.Now.Second % 10 == 0;
            }

            return true;
        }

        /// <summary>
        /// UI 업데이트 트리거
        /// </summary>
        private void TriggerUIUpdate()
        {
            try
            {
                var snapshot = BuildUISnapshot();
                if (snapshot != null)
                {
                    DataUpdated?.Invoke(this, snapshot);
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"UI 스냅샷 생성 오류: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 현재 데이터로 UI 스냅샷 생성
        /// </summary>
        private UIDataSnapshot BuildUISnapshot()
        {
            var snapshot = new UIDataSnapshot();

            // 기본값 설정 (연결 안됨 상태)
            snapshot.AtmPressure = 0;
            snapshot.PiraniPressure = 0;
            snapshot.IonPressure = 0;
            snapshot.IonGaugeStatus = "N/A";
            snapshot.GateValveStatus = "Unknown";
            snapshot.VentValveStatus = "Unknown";
            snapshot.ExhaustValveStatus = "Unknown";
            snapshot.IonGaugeHVStatus = "Unknown";

            // AI 데이터 처리
            if (_latestData.TryGetValue("AI_Data", out var aiObj) && aiObj is AnalogInputValues aiData)
            {
                ProcessIOModuleData(snapshot, aiData, null);
            }

            // AO 데이터 처리
            if (_latestData.TryGetValue("AO_Data", out var aoObj) && aoObj is AnalogOutputValues aoData)
            {
                ProcessIOModuleData(snapshot, null, aoData);
            }

            // 드라이펌프 데이터
            if (_latestData.TryGetValue("DryPump", out var dpObj))
            {
                ProcessDryPumpData(snapshot, dpObj);
            }

            // 터보펌프 데이터
            if (_latestData.TryGetValue("TurboPump", out var tpObj))
            {
                ProcessTurboPumpData(snapshot, tpObj);
            }

            // 칠러 데이터
            if (_latestData.TryGetValue("BathCirculator", out var bathObj))
            {
                ProcessBathCirculatorData(snapshot, bathObj);
            }

            // 온도 컨트롤러 데이터
            if (_latestData.TryGetValue("TempController", out var tempObj))
            {
                ProcessTempControllerData(snapshot, tempObj);
            }

            // 연결 상태 업데이트
            UpdateConnectionStates(snapshot);

            // 버튼 상태 계산
            CalculateButtonStates(snapshot);

            return snapshot;
        }

        /// <summary>
        /// IO 모듈 데이터 처리 - ★ 추가 AI 포함
        /// </summary>
        private void ProcessIOModuleData(UIDataSnapshot snapshot, AnalogInputValues aiData, AnalogOutputValues aoData)
        {
            try
            {
                if (aiData != null)
                {
                    // 압력 데이터 계산
                    snapshot.AtmPressure = _mainForm._atmSwitch?.ConvertVoltageToPressureInkPa(aiData.ExpansionVoltageValues[0]) ?? 0;
                    snapshot.PiraniPressure = _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]) ?? 0;
                    snapshot.IonPressure = _mainForm._ionGauge?.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[2]) ?? 0;
                    snapshot.IonGaugeStatus = _mainForm._ionGauge?.CheckGaugeStatus(aiData.ExpansionVoltageValues[2], aiData.ExpansionVoltageValues[3]).ToString() ?? "N/A";

                    // ★ 수정: Float 값 우선 사용
                    if (_latestData.TryGetValue("AdditionalAI_Value", out var floatVal) && floatVal is double dVal)
                    {
                        snapshot.AdditionalAIValue = dVal;
                    }
                    else
                    {
                        snapshot.AdditionalAIValue = aiData.AdditionalAIValue;
                    }
                    snapshot.AdditionalAITimestamp = aiData.Timestamp;

                    // 밸브 상태 계산
                    string gateValvePhysical = "";
                    if (aiData.MasterCurrentValues[3] > 1) gateValvePhysical = "Opened";
                    else if (aiData.MasterCurrentValues[0] > 1) gateValvePhysical = "Closed";
                    else gateValvePhysical = "Moving";

                    snapshot.GateValveStatus = gateValvePhysical;
                }

                if (aoData != null)
                {
                    snapshot.VentValveStatus = aoData.IsVentValveOpen ? "Opened" : "Closed";
                    snapshot.ExhaustValveStatus = aoData.IsExhaustValveOpen ? "Opened" : "Closed";
                    snapshot.IonGaugeHVStatus = aoData.IsIonGaugeHVOn ? "HV on" : "HV off";
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"IOModule 데이터 처리 오류: {ex.Message}", ex);
            }
        }

        private void ProcessDryPumpData(UIDataSnapshot snapshot, object dryPumpStatus)
        {
            try
            {
                if (_mainForm._dryPump?.Status != null)
                {
                    var status = _mainForm._dryPump.Status;
                    snapshot.DryPump.Status = _mainForm._dryPump.GetStatusText();
                    snapshot.DryPump.Speed = $"{status.MotorFrequency:F1} Hz";
                    snapshot.DryPump.Current = $"{status.MotorCurrent:F2} A";
                    snapshot.DryPump.Temperature = $"{status.MotorTemperature:F1} °C";
                    snapshot.DryPump.HasWarning = status.HasWarning;
                    snapshot.DryPump.HasError = status.HasFault;

                    if (status.HasFault)
                        snapshot.DryPump.Warning = "오류: " + _mainForm._dryPump.GetAlarmDescription();
                    else if (status.HasWarning)
                        snapshot.DryPump.Warning = "경고: " + _mainForm._dryPump.GetWarningDescription();
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"DryPump 데이터 처리 오류: {ex.Message}", ex);
            }
        }

        private void ProcessTurboPumpData(UIDataSnapshot snapshot, object turboPumpStatus)
        {
            try
            {
                if (_mainForm._turboPump?.Status != null)
                {
                    var status = _mainForm._turboPump.Status;
                    snapshot.TurboPump.Status = _mainForm._turboPump.GetStatusText();
                    snapshot.TurboPump.Speed = $"{status.CurrentSpeed} RPM";
                    snapshot.TurboPump.Current = $"{status.MotorCurrent:F2} A";
                    snapshot.TurboPump.Temperature = $"{status.MotorTemperature} °C";
                    snapshot.TurboPump.HasWarning = status.HasWarning;
                    snapshot.TurboPump.HasError = status.HasError;

                    if (status.HasError)
                        snapshot.TurboPump.Warning = "오류: " + _mainForm._turboPump.GetErrorDescription();
                    else if (status.HasWarning)
                        snapshot.TurboPump.Warning = "경고: " + _mainForm._turboPump.GetErrorDescription();
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"TurboPump 데이터 처리 오류: {ex.Message}", ex);
            }
        }

        private void ProcessBathCirculatorData(UIDataSnapshot snapshot, object bathStatus)
        {
            try
            {
                if (_mainForm._bathCirculator?.Status != null)
                {
                    var status = _mainForm._bathCirculator.Status;
                    snapshot.BathCirculator.Status = _mainForm._bathCirculator.GetStatusText();
                    snapshot.BathCirculator.CurrentTemp = $"{status.CurrentTemperature:F1} °C";
                    snapshot.BathCirculator.TargetTemp = $"{status.TargetTemperature:F1} °C";
                    snapshot.BathCirculator.Mode = status.IsFixMode ? "FIX 모드" : "PROG 모드";
                    snapshot.BathCirculator.HasError = status.HasError;
                    snapshot.BathCirculator.HasWarning = status.HasWarning;

                    if (status.SetTimeMinutes == -1)
                        snapshot.BathCirculator.Time = "제한 없음";
                    else if (status.SetTimeMinutes == 0)
                        snapshot.BathCirculator.Time = "종료 시간";
                    else
                        snapshot.BathCirculator.Time = $"{status.OperationTimeMinutes} / {status.SetTimeMinutes} 분";
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"BathCirculator 데이터 처리 오류: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 온도 컨트롤러 데이터 처리 (5채널: 메인 2 + 확장 3)
        /// </summary>
        private void ProcessTempControllerData(UIDataSnapshot snapshot, object tempStatus)
        {
            try
            {
                if (_mainForm._tempController?.Status != null)
                {
                    var status = _mainForm._tempController.Status;

                    // 전체 채널 처리 (최대 5채널)
                    int totalChannels = Math.Min(5, status.ChannelStatus.Length);

                    for (int i = 0; i < totalChannels; i++)
                    {
                        var ch = status.ChannelStatus[i];
                        snapshot.TempController.Channels[i].PresentValue = ch.FormattedPresentValue;
                        snapshot.TempController.Channels[i].SetValue = ch.FormattedSetValue;
                        snapshot.TempController.Channels[i].HeatingMV = $"{ch.HeatingMV:F1} %";
                        snapshot.TempController.Channels[i].Status = _mainForm._tempController.GetChannelStatusText(i + 1);
                        snapshot.TempController.Channels[i].IsRunning = ch.IsRunning;
                        snapshot.TempController.Channels[i].IsAutoTuning = ch.IsAutoTuning;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"TempController 데이터 처리 오류: {ex.Message}", ex);
            }
        }

        private void UpdateConnectionStates(UIDataSnapshot snapshot)
        {
            try
            {
                snapshot.Connections.IOModule = _mainForm._ioModule?.IsConnected == true;
                snapshot.Connections.DryPump = _mainForm._dryPump?.IsConnected == true;
                snapshot.Connections.TurboPump = _mainForm._turboPump?.IsConnected == true;
                snapshot.Connections.BathCirculator = _mainForm._bathCirculator?.IsConnected == true;
                snapshot.Connections.TempController = _mainForm._tempController?.IsConnected == true;
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"연결 상태 업데이트 오류: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 버튼 상태 계산 (5채널 지원)
        /// </summary>
        private void CalculateButtonStates(UIDataSnapshot snapshot)
        {
            try
            {
                // 이온게이지 버튼 상태
                snapshot.ButtonStates.IonGaugeEnabled = snapshot.PiraniPressure <= 1E-3;

                // 드라이펌프 버튼 상태
                if (_mainForm._dryPump?.Status != null)
                {
                    var status = _mainForm._dryPump.Status;
                    snapshot.ButtonStates.DryPumpStartEnabled = snapshot.Connections.DryPump && !status.IsRunning && !status.IsStopping;
                    snapshot.ButtonStates.DryPumpStopEnabled = snapshot.Connections.DryPump && status.IsRunning && !status.IsStopping;
                    snapshot.ButtonStates.DryPumpStandbyEnabled = snapshot.Connections.DryPump && status.IsRunning && !status.IsStandby;
                    snapshot.ButtonStates.DryPumpNormalEnabled = snapshot.Connections.DryPump && status.IsRunning && status.IsStandby;
                }

                // 터보펌프 버튼 상태
                if (_mainForm._turboPump?.Status != null)
                {
                    var status = _mainForm._turboPump.Status;
                    snapshot.ButtonStates.TurboPumpStartEnabled = snapshot.Connections.TurboPump && !status.IsRunning && !status.IsAccelerating && !status.IsDecelerating;
                    snapshot.ButtonStates.TurboPumpStopEnabled = snapshot.Connections.TurboPump && status.IsRunning && !status.IsDecelerating;
                    snapshot.ButtonStates.TurboPumpVentEnabled = snapshot.Connections.TurboPump && !status.IsRunning && !status.IsVented;
                    snapshot.ButtonStates.TurboPumpResetEnabled = snapshot.Connections.TurboPump && status.HasError;

                    // 터보펌프 속도에 따른 밸브 버튼 상태
                    bool turboPumpStopped = !status.IsRunning;
                    snapshot.ButtonStates.VentValveEnabled = turboPumpStopped;
                    snapshot.ButtonStates.ExhaustValveEnabled = turboPumpStopped;
                }

                // 칠러 버튼 상태
                if (_mainForm._bathCirculator?.Status != null)
                {
                    var status = _mainForm._bathCirculator.Status;
                    snapshot.ButtonStates.BathCirculatorStartEnabled = snapshot.Connections.BathCirculator && !status.IsRunning;
                    snapshot.ButtonStates.BathCirculatorStopEnabled = snapshot.Connections.BathCirculator && status.IsRunning;
                }

                // 온도컨트롤러 버튼 상태 (5채널)
                if (_mainForm._tempController?.Status != null)
                {
                    var status = _mainForm._tempController.Status;
                    int totalChannels = Math.Min(5, status.ChannelStatus.Length);

                    for (int i = 0; i < totalChannels; i++)
                    {
                        var ch = status.ChannelStatus[i];

                        // 확장 채널(CH3~CH5)은 입력 전용이므로 버튼 비활성화
                        if (ch.IsExpansionChannel)
                        {
                            snapshot.ButtonStates.TempControllerStartEnabled[i] = false;
                            snapshot.ButtonStates.TempControllerStopEnabled[i] = false;
                        }
                        else
                        {
                            snapshot.ButtonStates.TempControllerStartEnabled[i] = snapshot.Connections.TempController && !ch.IsRunning;
                            snapshot.ButtonStates.TempControllerStopEnabled[i] = snapshot.Connections.TempController && ch.IsRunning;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"버튼 상태 계산 오류: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 최신 AI 데이터 가져오기
        /// </summary>
        public AnalogInputValues GetLatestAIData()
        {
            if (_latestData.TryGetValue("AI_Data", out var aiObj) && aiObj is AnalogInputValues aiData)
            {
                return aiData;
            }
            return null;
        }

        /// <summary>
        /// 최신 AO 데이터 가져오기
        /// </summary>
        public AnalogOutputValues GetLatestAOData()
        {
            if (_latestData.TryGetValue("AO_Data", out var aoObj) && aoObj is AnalogOutputValues aoData)
            {
                return aoData;
            }
            return null;
        }

        /// <summary>
        /// ★ 최신 추가 AI 값 조회 (±10V)
        /// </summary>
        public double GetLatestAdditionalAIValue()
        {
            // 먼저 별도 저장된 값 확인
            if (_latestData.TryGetValue("AdditionalAI_Value", out var value) && value is double dValue)
            {
                return dValue;
            }

            // AI_Data에서 직접 가져오기
            if (_latestData.TryGetValue("AI_Data", out var aiObj) && aiObj is AnalogInputValues aiData)
            {
                return aiData.AdditionalAIValue;
            }

            return double.NaN;
        }

        /// <summary>
        /// ★ 최신 추가 AI 타임스탬프 조회
        /// </summary>
        public DateTime? GetLatestAdditionalAITimestamp()
        {
            if (_latestData.TryGetValue("AdditionalAI_Timestamp", out var value) && value is DateTime timestamp)
            {
                return timestamp;
            }

            if (_latestData.TryGetValue("AI_Data", out var aiObj) && aiObj is AnalogInputValues aiData)
            {
                return aiData.Timestamp;
            }

            return null;
        }

        /// <summary>
        /// 게이트 밸브 상태 가져오기
        /// </summary>
        public string GetGateValveStatus()
        {
            var aiData = GetLatestAIData();
            if (aiData != null)
            {
                if (aiData.MasterCurrentValues[3] > 1) return "Opened";
                else if (aiData.MasterCurrentValues[0] > 1) return "Closed";
                else return "Moving";
            }
            return "Unknown";
        }

        /// <summary>
        /// 모든 밸브 상태 가져오기
        /// </summary>
        public (bool ventOpen, bool exhaustOpen, bool ionGaugeHV) GetValveStates()
        {
            var aoData = GetLatestAOData();
            if (aoData != null)
            {
                return (aoData.IsVentValveOpen, aoData.IsExhaustValveOpen, aoData.IsIonGaugeHVOn);
            }
            return (false, false, false);
        }

        public void Dispose()
        {
            Stop();

            foreach (var semaphore in _deviceLocks.Values)
            {
                semaphore?.Dispose();
            }

            _cancellationTokenSource?.Dispose();
        }
    }
}