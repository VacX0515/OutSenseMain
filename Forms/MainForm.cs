using System.IO.Ports;
using System.Text;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.DryPump;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.IO_Module;
using VacX_OutSense.Core.Devices.IO_Module.Enum;
using VacX_OutSense.Core.Devices.IO_Module.Models;
using VacX_OutSense.Core.Devices.Relay_Module;
using VacX_OutSense.Core.Devices.Relay_Module.Enum;
using VacX_OutSense.Core.Devices.Relay_Module.Models;
using VacX_OutSense.Core.Devices.TurboPump;
using VacX_OutSense.Core.Devices.TempController;
// 필요한 using 문 추가
using System.IO;
using System.Text;
using System.Threading.Tasks;
using VacX_OutSense.Utils; // LoggerService와 DataLoggerService가 있는 네임스페이스

namespace VacX_OutSense
{
    public partial class MainForm : Form
    {
        #region 필드 및 속성

        // 로깅 관련 필드 추가
        private bool _isLoggingEnabled = true; // 기본적으로 로깅 활성화

        // 통신 관리자
        private MultiPortSerialManager _multiPortManager;
        private Dictionary<string, DevicePortAdapter> _deviceAdapters = new Dictionary<string, DevicePortAdapter>();

        // 장치 목록
        private List<IDevice> _deviceList = new List<IDevice>();

        // 통신 장치 인스턴스들
        private IO_Module _ioModule;
        private RelayModule _relayModule;
        private DryPump _dryPump;
        private TurboPump _turboPump;
        private BathCirculator _bathCirculator;
        private TempController _tempController;

        // 장치 인스턴트
        private ATMswitch _atmSwitch;
        private PiraniGauge _piraniGauge;
        private IonGauge _ionGauge;

        // 타이머 (주기적 데이터 업데이트용)
        private System.Windows.Forms.Timer _ioModuleUpdateTimer;
        private System.Windows.Forms.Timer _relayModuleUpdateTimer;
        private System.Windows.Forms.Timer _dryPumpUpdateTimer;
        private System.Windows.Forms.Timer _turboPumpUpdateTimer;
        private System.Windows.Forms.Timer _bathCirculatorUpdateTimer;
        private System.Windows.Forms.Timer _tempControllerUpdateTimer;
        private System.Windows.Forms.Timer _updateTimer;

        // 타이머 간격 (밀리초)
        private const int DEFAULT_IO_UPDATE_INTERVAL = 200;        // 0.5초
        private const int DEFAULT_RELAY_UPDATE_INTERVAL = 200;     // 0.5초
        private const int DEFAULT_DRYPUMP_UPDATE_INTERVAL = 500;  // 1초
        private const int DEFAULT_TURBOPUMP_UPDATE_INTERVAL = 500; // 1초
        private const int DEFAULT_BATHCIRCULATOR_UPDATE_INTERVAL = 1000; // 1초
        private const int DEFAULT_TEMP_CONTROLLER_UPDATE_INTERVAL = 200; // 0.5초

        // 비동기 통신 상태 추적
        private bool _isUpdatingIOData = false;
        private bool _isUpdatingRelayData = false;
        private bool _isUpdatingTurboPumpData = false;
        private bool _isUpdatingBathCirculatorData = false;
        private bool _isUpdatingTempControllerData = false;

        // 장치별 통신 설정
        private Dictionary<string, CommunicationSettings> _deviceSettings = new Dictionary<string, CommunicationSettings>();

        // 장치별 기본 통신 설정
        private readonly CommunicationSettings _defaultSettings = new CommunicationSettings
        {
            BaudRate = 9600,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.None,
            StopBits = System.IO.Ports.StopBits.One,
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };

        private readonly CommunicationSettings _dryPumpDefaultSettings = new CommunicationSettings
        {
            BaudRate = 38400,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.Even,
            StopBits = System.IO.Ports.StopBits.One,
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };

        private readonly CommunicationSettings _turboPumpDefaultSettings = new CommunicationSettings
        {
            BaudRate = 19200,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.Even,
            StopBits = System.IO.Ports.StopBits.One,
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };

        private readonly CommunicationSettings _bathCirculatorDefaultSettings = new CommunicationSettings
        {
            BaudRate = 9600,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.Even,
            StopBits = System.IO.Ports.StopBits.One,
            ReadTimeout = 2000,
            WriteTimeout = 2000

        };

        private readonly CommunicationSettings _tempControllerDefaultSettings = new CommunicationSettings
        {
            BaudRate = 9600,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.None,
            StopBits = System.IO.Ports.StopBits.One,
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };

        // 로깅
        private StringBuilder _logBuffer = new StringBuilder();
        private const int MAX_LOG_ENTRIES = 100;
        private readonly object _logLock = new object(); // 로그 동기화를 위한 락 객체

        // 캐시 데이터
        private SemaphoreSlim _uiUpdateSemaphore = new SemaphoreSlim(1, 1);

        #endregion

        #region 생성자 및 초기화

        public MainForm()
        {
            InitializeComponent();
            SetupEventHandlers();

            InitializeWithLoadingScreen();
        }

        private async void InitializeWithLoadingScreen()
        {
            // 로딩창 생성
            LoadingForm loadingForm = new LoadingForm();

            // 메인 폼은 초기에 숨김 처리
            this.Opacity = 0;

            // 백그라운드 작업 설정
            CancellationTokenSource cts = new CancellationTokenSource();
            Task initTask = Task.Run(() =>
            {
                try
                {
                    // 로딩창 메시지 업데이트
                    loadingForm.UpdateStatus("타이머 초기화 중...");
                    InitializeTimer();
                    Thread.Sleep(100);

                    // 로딩창 메시지 업데이트
                    loadingForm.UpdateStatus("통신 관리자 초기화 중...");
                    InitializeSerialManager();
                    Thread.Sleep(100); // UI 업데이트를 위한 짧은 지연

                    //// 로딩창 메시지 업데이트
                    //loadingForm.UpdateStatus("장치 초기화 중...");
                    //InitializeDevices();
                    //Thread.Sleep(100);

                    // 로딩창 메시지 업데이트
                    loadingForm.UpdateStatus("로깅 시스템 초기화 중...");
                    InitializeLogging();
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"초기화 중 오류가 발생했습니다: {ex.Message}", "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }, cts.Token);

            // 로딩창 표시
            loadingForm.Show();

            // 초기화 작업이 완료될 때까지 대기
            await initTask;

            // 로딩창 닫기
            loadingForm.Close();
            loadingForm.Dispose();

            // 메인 폼 표시
            this.Opacity = 1;

            // 중요: 타이머 시작을 UI 스레드에서 명시적으로 다시 호출
            SafeInvoke(() =>
            {
                InitializeDevices();
                LoggerService.Instance.LogInfo("장치 타이머를 시작했습니다.");
            });


            // 로그 메시지
            LoggerService.Instance.LogInfo("VacX_OutSense 시스템 초기화 완료");
        }

        private void InitializeTimer()
        {
            // IO 모듈 타이머 초기화
            _ioModuleUpdateTimer = new System.Windows.Forms.Timer();
            _ioModuleUpdateTimer.Interval = DEFAULT_IO_UPDATE_INTERVAL;
            _ioModuleUpdateTimer.Tick += IOModuleUpdateTimer_Tick;

            // 릴레이 모듈 타이머 초기화
            _relayModuleUpdateTimer = new System.Windows.Forms.Timer();
            _relayModuleUpdateTimer.Interval = DEFAULT_RELAY_UPDATE_INTERVAL;
            _relayModuleUpdateTimer.Tick += RelayModuleUpdateTimer_Tick;

            // 드라이펌프 타이머 초기화
            _dryPumpUpdateTimer = new System.Windows.Forms.Timer();
            _dryPumpUpdateTimer.Interval = DEFAULT_DRYPUMP_UPDATE_INTERVAL;
            _dryPumpUpdateTimer.Tick += DryPumpUpdateTimer_Tick;

            // 터보펌프 타이머 초기화
            _turboPumpUpdateTimer = new System.Windows.Forms.Timer();
            _turboPumpUpdateTimer.Interval = DEFAULT_TURBOPUMP_UPDATE_INTERVAL;
            _turboPumpUpdateTimer.Tick += TurboPumpUpdateTimer_Tick;

            // Bath Circulator(칠러) 타이머 초기화
            _bathCirculatorUpdateTimer = new System.Windows.Forms.Timer();
            _bathCirculatorUpdateTimer.Interval = DEFAULT_BATHCIRCULATOR_UPDATE_INTERVAL;
            _bathCirculatorUpdateTimer.Tick += BathCirculatorUpdateTimer_Tick;

            // 타이머 초기화
            _tempControllerUpdateTimer = new System.Windows.Forms.Timer();
            _tempControllerUpdateTimer.Interval = DEFAULT_TEMP_CONTROLLER_UPDATE_INTERVAL;
            _tempControllerUpdateTimer.Tick += TempControllerUpdateTimer_Tick;
        }

        private void InitializeSerialManager()
        {
            _multiPortManager = MultiPortSerialManager.Instance;
            _multiPortManager.StatusChanged += MultiPortManager_StatusChanged;
        }

        private void MultiPortManager_StatusChanged(object sender, CommunicationStatusEventArgs e)
        {
            SafeInvoke(() =>
            {
                AppendLog($"통신 상태 변경: {e.StatusMessage}");
            });
        }

        private void InitializeDevices()
        {
            try
            {
                // 각 장치별 어댑터 생성 및 통신 설정 등록
                RegisterDevicePort("COM1", _turboPumpDefaultSettings); // 터보 펌프
                RegisterDevicePort("COM2", _defaultSettings);         // 릴레이 모듈
                RegisterDevicePort("COM3", _dryPumpDefaultSettings);  // 드라이 펌프
                RegisterDevicePort("COM4", _defaultSettings);         // IO 모듈
                RegisterDevicePort("COM5", _bathCirculatorDefaultSettings); // 칠러
                RegisterDevicePort("COM6", _tempControllerDefaultSettings); //PID

                // 터보 펌프 초기화
                _turboPump = new TurboPump(_deviceAdapters["COM1"], "MAG W 1300", 1);
                connectionIndicator_turbopump.DataSource = _turboPump;
                connectionIndicator_turbopump.DataMember = "IsConnected";

                // 릴레이 모듈 초기화
                _relayModule = new RelayModule(_deviceAdapters["COM2"]);
                connectionIndicator_relaymodule.DataSource = _relayModule;
                connectionIndicator_relaymodule.DataMember = "IsConnected";

                // 드라이 펌프 초기화
                _dryPump = new DryPump(_deviceAdapters["COM3"], "ECODRY 25 plus", 1);
                connectionIndicator_drypump.DataSource = _dryPump;
                connectionIndicator_drypump.DataMember = "IsConnected";

                // IO 모듈 초기화
                _ioModule = new IO_Module(_deviceAdapters["COM4"], "M31-XAXA0404G-L", 1);
                connectionIndicator_iomodule.DataSource = _ioModule;
                connectionIndicator_iomodule.DataMember = "IsConnected";

                // Bath Circulator(칠러) 초기화
                _bathCirculator = new BathCirculator(_deviceAdapters["COM5"], "LK-1000", 1);
                connectionIndicator_bathcirculator.DataSource = _bathCirculator;
                connectionIndicator_bathcirculator.DataMember = "IsConnected";

                // 온도 컨트롤러 인스턴스 생성 (국번: 1)
                _tempController = new TempController(_deviceAdapters["COM6"], 1);
                connectionIndicator_tempcontroller.DataSource = _tempController;
                connectionIndicator_tempcontroller.DataMember = "IsConnected";

                // 그 외 장치들
                _atmSwitch = new ATMswitch();
                _piraniGauge = new PiraniGauge();
                _ionGauge = new IonGauge();

                // 장치 목록에 추가
                _deviceList.Add(_ioModule);
                _deviceList.Add(_relayModule);
                _deviceList.Add(_dryPump);
                _deviceList.Add(_turboPump);
                _deviceList.Add(_bathCirculator);
                _deviceList.Add(_tempController);

                // 연결
                ConnectToDevicesWithCustomSettings();
            }
            catch (Exception ex)
            {
                AppendLog($"장치 초기화 중 오류 발생: {ex.Message}", true);
                MessageBox.Show($"장치 초기화 중 오류 발생: {ex.Message}", "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegisterDevicePort(string portName, CommunicationSettings settings)
        {
            DevicePortAdapter adapter = new DevicePortAdapter(portName, _multiPortManager);
            _deviceAdapters[portName] = adapter;
            _deviceSettings[portName] = settings;
        }

        // 장치별 서로 다른 통신 설정을 지원하는 연결 메서드
        private void ConnectToDevicesWithCustomSettings()
        {
            // 모든 장치 중 연결 시도할 장치 필터링
            foreach (IDevice device in _deviceList)
            {
                try
                {
                    string portName = GetDevicePortName(device);
                    if (string.IsNullOrEmpty(portName))
                    {
                        AppendLog($"{device.DeviceName}에 할당된 포트를 찾을 수 없습니다.", true);
                        continue;
                    }

                    var settings = _deviceSettings.ContainsKey(portName) ?
                        _deviceSettings[portName] : _defaultSettings;

                    // 로그에 연결 설정 기록
                    AppendLog($"{device.DeviceName} 연결 시도 - 포트: {portName}, 보드레이트: {settings.BaudRate}, 패리티: {settings.Parity}");

                    // 장치별 맞춤 설정으로 연결
                    bool result = device.Connect(portName, settings);

                    if (result)
                        AppendLog($"{device.DeviceName} 연결 성공");
                    else
                        AppendLog($"{device.DeviceName} 연결 실패", true);
                }
                catch (Exception ex)
                {
                    AppendLog($"{device.DeviceName} 연결 중 오류: {ex.Message}", true);
                }
            }

            // 타이머 시작
            StartDeviceTimers();
        }

        private string GetDevicePortName(IDevice device)
        {
            foreach (var adapterEntry in _deviceAdapters)
            {
                if (device.CommunicationManager == adapterEntry.Value)
                {
                    return adapterEntry.Key;
                }
            }
            return null;
        }

        // 타이머 시작을 위한 별도의 메서드
        private void StartDeviceTimers()
        {
            StartTimerIfInitialized(_ioModuleUpdateTimer, "IO Module");
            StartTimerIfInitialized(_relayModuleUpdateTimer, "Relay Module");
            StartTimerIfInitialized(_dryPumpUpdateTimer, "Dry Pump");
            StartTimerIfInitialized(_turboPumpUpdateTimer, "Turbo Pump");
            StartTimerIfInitialized(_bathCirculatorUpdateTimer, "Bath Circulator");
            StartTimerIfInitialized(_tempControllerUpdateTimer, "Temp Controller");
        }

        private void StartTimerIfInitialized(System.Windows.Forms.Timer timer, string deviceName)
        {
            if (timer != null)
                timer.Start();
            else
                AppendLog($"{deviceName} 타이머가 초기화되지 않았습니다.", true);
        }

        private void SetupEventHandlers()
        {
            this.FormClosing += MainForm_FormClosing;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LoggerService.Instance.LogInfo("애플리케이션 종료 중...");

            // 연결 해제 및 리소스 정리
            DisconnectAllDevices();

            // 로깅 세션 종료
            DataLoggerService.Instance.StopAllLogging();

            // 리소스 정리
            if (_updateTimer != null)
            {
                _updateTimer.Dispose();
            }

            // 이벤트 핸들러 정리
            LoggerService.Instance.LogAdded -= LoggerService_LogAdded;

            // 종료 로그
            LoggerService.Instance.LogInfo("애플리케이션이 종료되었습니다.");
        }

        #endregion

        #region 타이머 이벤트 핸들러

        // IO 모듈 타이머 이벤트 핸들러
        private void IOModuleUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingIOData || _ioModule == null || !_ioModule.IsConnected)
                return;

            _ioModuleUpdateTimer.Stop();
            UpdateIOModuleDataAsync();
        }

        // 릴레이 모듈 타이머 이벤트 핸들러
        private void RelayModuleUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingRelayData || _relayModule == null || !_relayModule.IsConnected)
                return;

            _relayModuleUpdateTimer.Stop();
            UpdateRelayModuleDataAsync();
        }

        // 드라이 펌프 타이머 이벤트 핸들러
        private void DryPumpUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_dryPump == null || !_dryPump.IsConnected)
                return;

            _dryPumpUpdateTimer.Stop();
            UpdateDryPumpDataAsync();
        }

        // 터보 펌프 타이머 이벤트 핸들러
        private void TurboPumpUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingTurboPumpData || _turboPump == null || !_turboPump.IsConnected)
                return;

            _turboPumpUpdateTimer.Stop();
            UpdateTurboPumpDataAsync();
        }

        // 칠러 타이머 이벤트 핸들러
        private void BathCirculatorUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingBathCirculatorData || _bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            _bathCirculatorUpdateTimer.Stop();
            UpdateBathCirculatorDataAsync();
        }

        // 온도 컨트롤러 타이머 이벤트 핸들러
        private void TempControllerUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdatingTempControllerData || _tempController == null || !_tempController.IsConnected)
                return;

            _tempControllerUpdateTimer.Stop();
            UpdateTempControllerDataAsync();
        }

        #endregion

        #region 온도 컨트롤러 데이터 업데이트

        // 온도 컨트롤러 데이터 비동기 업데이트
        private async void UpdateTempControllerDataAsync()
        {
            _isUpdatingTempControllerData = true;

            try
            {
                bool result = await _tempController.UpdateStatusAsync().ConfigureAwait(false);

                if (result)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateTempControllerUI());
                        // 활성화된 경우에만 데이터 로깅 수행
                        if (_isLoggingEnabled)
                        {
                            try
                            {
                                List<string> tempControllerValues = new List<string>();

                                // 채널 1 데이터
                                //250520 YH 요청 로그에 온도 단위 제거(그래프)
                                if (_tempController.Status.ChannelStatus.Length > 0)
                                {
                                    var ch1 = _tempController.Status.ChannelStatus[0];
                                    tempControllerValues.Add(ch1.FormattedPresentValue.Trim('C').Trim('°'));
                                    tempControllerValues.Add(ch1.FormattedSetValue.Trim('C').Trim('°'));
                                    tempControllerValues.Add(ch1.HeatingMV.ToString("F1"));
                                    tempControllerValues.Add(_tempController.GetChannelStatusText(1));
                                }
                                else
                                {
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                }

                                // 채널 2 데이터
                                if (_tempController.Status.ChannelStatus.Length > 1)
                                {
                                    var ch2 = _tempController.Status.ChannelStatus[1];
                                    tempControllerValues.Add(ch2.FormattedPresentValue.Trim('C').Trim('°'));
                                    tempControllerValues.Add(ch2.FormattedSetValue.Trim('C').Trim('°'));
                                    tempControllerValues.Add(ch2.HeatingMV.ToString("F1"));
                                    tempControllerValues.Add(_tempController.GetChannelStatusText(2));
                                }
                                else
                                {
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                    tempControllerValues.Add("N/A");
                                }

                                await DataLoggerService.Instance.LogDataAsync("TempController", tempControllerValues).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Instance.LogError($"온도 컨트롤러 데이터 로깅 중 오류: {ex.Message}", ex);
                            }


                        }
                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"온도 컨트롤러 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                _isUpdatingTempControllerData = false;
                SafeInvoke(() => _tempControllerUpdateTimer.Start());
            }
        }

        // 온도 컨트롤러 UI 업데이트
        private void UpdateTempControllerUI()
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            // 각 채널별 정보 업데이트
            for (int ch = 0; ch < _tempController.ChannelCount; ch++)
            {
                var channelStatus = _tempController.Status.ChannelStatus[ch];

                // 채널별 UI 요소 업데이트
                switch (ch)
                {
                    case 0: // CH1
                        txtCh1PresentValue.Text = channelStatus.FormattedPresentValue;
                        txtCh1SetValue.Text = channelStatus.FormattedSetValue;
                        txtCh1Status.Text = _tempController.GetChannelStatusText(1);
                        txtCh1HeatingMV.Text = $"{channelStatus.HeatingMV:F1} %";
                        btnCh1Start.Enabled = !channelStatus.IsRunning;
                        btnCh1Stop.Enabled = channelStatus.IsRunning;
                        txtCh1IsAutotune.Text = channelStatus.IsAutoTuning ? "On" : "Off";
                        break;

                    case 1: // CH2
                        txtCh2PresentValue.Text = channelStatus.FormattedPresentValue;
                        txtCh2SetValue.Text = channelStatus.FormattedSetValue;
                        txtCh2Status.Text = _tempController.GetChannelStatusText(2);
                        txtCh2HeatingMV.Text = $"{channelStatus.HeatingMV:F1} %";
                        btnCh2Start.Enabled = !channelStatus.IsRunning;
                        btnCh2Stop.Enabled = channelStatus.IsRunning;
                        txtCh2IsAutotune.Text = channelStatus.IsAutoTuning ? "On" : "Off";
                        break;

                        //case 2: // CH3
                        //    txtCh3PresentValue.Text = channelStatus.FormattedPresentValue;
                        //    txtCh3SetValue.Text = channelStatus.FormattedSetValue;
                        //    txtCh3Status.Text = _tempController.GetChannelStatusText(3);
                        //    txtCh3HeatingMV.Text = $"{channelStatus.HeatingMV:F1} %";
                        //    btnCh3Start.Enabled = !channelStatus.IsRunning;
                        //    btnCh3Stop.Enabled = channelStatus.IsRunning;
                        //    break;

                        //case 3: // CH4
                        //    txtCh4PresentValue.Text = channelStatus.FormattedPresentValue;
                        //    txtCh4SetValue.Text = channelStatus.FormattedSetValue;
                        //    txtCh4Status.Text = _tempController.GetChannelStatusText(4);
                        //    txtCh4HeatingMV.Text = $"{channelStatus.HeatingMV:F1} %";
                        //    btnCh4Start.Enabled = !channelStatus.IsRunning;
                        //    btnCh4Stop.Enabled = channelStatus.IsRunning;
                        //    break;
                }
            }

            //// 장치 정보 표시
            //txtTempControllerModel.Text = _tempController.Status.ModelName;
            //txtTempControllerComm.Text = $"{_tempController.Status.BaudRateString}, {_tempController.Status.ParityBitString}, {_tempController.Status.StopBitString}";
        }

        #endregion

        #region 버튼 이벤트 핸들러

        // 채널1 시작 버튼
        private void btnCh1Start_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Start(1), "CH1 시작");

            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(1), "CH1 오토튜닝");

        }

        // 채널1 정지 버튼
        private void btnCh1Stop_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Stop(1), "CH1 정지");
        }

        // 채널1 온도 설정 버튼
        private void btnCh1SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(1);
        }

        // 채널1 오토튜닝 버튼
        private void btnCh1AutoTuning_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(1), "CH1 오토튜닝");
        }

        // 채널2 시작 버튼
        private void btnCh2Start_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Start(2), "CH2 시작");

            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(2), "CH2 오토튜닝");
        }

        // 채널2 정지 버튼
        private void btnCh2Stop_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Stop(2), "CH2 정지");
        }

        // 채널2 온도 설정 버튼
        private void btnCh2SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(2);
        }

        // 채널2 오토튜닝 버튼
        private void btnCh2AutoTuning_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(2), "CH2 오토튜닝");
        }

        // 채널3 시작 버튼
        private void btnCh3Start_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Start(3), "CH3 시작");
        }

        // 채널3 정지 버튼
        private void btnCh3Stop_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Stop(3), "CH3 정지");
        }

        // 채널3 온도 설정 버튼
        private void btnCh3SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(3);
        }

        // 채널3 오토튜닝 버튼
        private void btnCh3AutoTuning_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(3), "CH3 오토튜닝");
        }

        // 채널4 시작 버튼
        private void btnCh4Start_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Start(4), "CH4 시작");
        }

        // 채널4 정지 버튼
        private void btnCh4Stop_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.Stop(4), "CH4 정지");
        }

        // 채널4 온도 설정 버튼
        private void btnCh4SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(4);
        }

        // 채널4 오토튜닝 버튼
        private void btnCh4AutoTuning_Click(object sender, EventArgs e)
        {
            ExecuteTempControllerCommand(() => _tempController.StartAutoTuning(4), "CH4 오토튜닝");
        }

        #endregion

        #region 유틸리티 메서드

        // 온도 설정 다이얼로그 표시
        private void ShowTemperatureSetDialog(int channelNumber)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            try
            {
                var channelStatus = _tempController.Status.ChannelStatus[channelNumber - 1];
                short currentSetValue = channelStatus.SetValue;

                // 온도 입력 대화상자 표시
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"채널 {channelNumber} 목표 온도를 입력하세요 ({channelStatus.TemperatureUnit}):",
                    "온도 설정",
                    channelStatus.Dot == 0 ? currentSetValue.ToString() : (currentSetValue / 10.0).ToString("F1"));

                if (!string.IsNullOrEmpty(input))
                {
                    short setValue;

                    // 소수점 여부에 따른 입력값 변환
                    if (channelStatus.Dot == 0)
                    {
                        // 정수 입력
                        if (short.TryParse(input, out setValue))
                        {
                            ExecuteTempControllerCommand(() => _tempController.SetTemperature(channelNumber, setValue),
                                $"CH{channelNumber} 온도 설정 ({setValue}{channelStatus.TemperatureUnit})");
                        }
                        else
                        {
                            MessageBox.Show("유효한 온도 값을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        // 소수점 입력
                        if (double.TryParse(input, out double doubleValue))
                        {
                            setValue = (short)(doubleValue * 10); // 소수점 변환 (0.1 단위)
                            ExecuteTempControllerCommand(() => _tempController.SetTemperature(channelNumber, setValue),
                                $"CH{channelNumber} 온도 설정 ({doubleValue:F1}{channelStatus.TemperatureUnit})");
                        }
                        else
                        {
                            MessageBox.Show("유효한 온도 값을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"온도 설정 오류: {ex.Message}", true);
            }
        }

        // 온도 컨트롤러 명령 실행
        private void ExecuteTempControllerCommand(Func<bool> command, string commandName)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            try
            {
                bool result = command();
                if (result)
                    AppendLog($"온도 컨트롤러 {commandName} 명령 성공");
                else
                    AppendLog($"온도 컨트롤러 {commandName} 명령 실패", true);

            }
            catch (Exception ex)
            {
                AppendLog($"온도 컨트롤러 {commandName} 오류: {ex.Message}", true);
            }
        }

        #endregion



        #region IO 모듈 관련 기능

        private async void UpdateIOModuleDataAsync()
        {
            _isUpdatingIOData = true;

            try
            {
                // ConfigureAwait(false)를 사용하여 UI 스레드로 돌아오지 않도록 함
                var analogInputs = await _ioModule.ReadAnalogInputsAsync().ConfigureAwait(false);

                // UI 업데이트는 한 번에 묶어서 처리
                if (analogInputs != null)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateIOModuleUI(analogInputs));

                        // 활성화된 경우에만 데이터 로깅 수행
                        if (_isLoggingEnabled)
                        {
                            try
                            {
                                List<string> pressureValues = new List<string>
                                    {
                                        _atmSwitch.PressureInkPa.ToString("E2"),
                                        _piraniGauge.PressureInTorr.ToString("E2"),
                                        _ionGauge.PressureInTorr.ToString("E2"),
                                        _ionGauge.Status
                                    };

                                // 아날로그 입력 값 로깅
                                List<string> analogValues = new List<string>();

                                // 마스터 모듈 전류 값
                                foreach (double value in analogInputs.MasterCurrentValues)
                                {
                                    analogValues.Add(value.ToString("F3"));
                                }

                                // 확장 모듈 전압 값
                                foreach (double value in analogInputs.ExpansionVoltageValues)
                                {
                                    analogValues.Add(value.ToString("F3"));
                                }

                                // 비동기 로깅 - 메인 스레드를 차단하지 않도록 ConfigureAwait(false) 사용
                                await Task.WhenAll(
                                    DataLoggerService.Instance.LogDataAsync("PressureInTorr", pressureValues),
                                    DataLoggerService.Instance.LogDataAsync("AnalogInput", analogValues)
                                ).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Instance.LogError($"IO 모듈 데이터 로깅 중 오류: {ex.Message}", ex);
                            }
                        }
                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"IO 모듈 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                _isUpdatingIOData = false;
                SafeInvoke(() => _ioModuleUpdateTimer.Start());
            }
        }

        private void UpdateIOModuleUI(AnalogInputValues values)
        {
            bindableTextBox1.TextValue = _atmSwitch.ConvertVoltageToPressureInkPa(values.ExpansionVoltageValues[0]).ToString();
            txtPG.TextValue = _piraniGauge.ConvertVoltageToPressureInTorr(values.ExpansionVoltageValues[1]).ToString("E2");
            txtIG.TextValue = _ionGauge.ConvertVoltageToPressureInTorr(values.ExpansionVoltageValues[2]).ToString("E2");
            bindableTextBox4.TextValue = _ionGauge.CheckGaugeStatus(values.ExpansionVoltageValues[2], values.ExpansionVoltageValues[3]).ToString();

            //Interlock : 이온 게이지는 Enable 1E-3 Torr 이하일때만 버튼 활성화 및 자동으로 끄기(PG 기준 2V)
            if (_piraniGauge.PressureInTorr > 1E-3)
            {
                IongaugeOff();
                if (btn_iongauge.Enabled) btn_iongauge.Enabled = false;
            }
            else
            {
                if (!btn_iongauge.Enabled) btn_iongauge.Enabled = true;
            }

            //Interlock : EV open condition, > 1atm && Pump is not running
            if (_atmSwitch.PressureInkPa > 97.5 && !_turboPump.IsRunning && !_dryPump.IsRunning)
            {
                ExhaustValveOpen();
            }
            else
            {
                ExhaustValveClose();
            }
            //게이트 밸브 상태
            if (values.MasterCurrentValues[3] > 10)
            {
                btn_GV.Text = "Opened";
            }
            else if (values.MasterCurrentValues[0] > 10)
            {
                btn_GV.Text = "Closed";
            }
            else
            {
                btn_GV.Text = "Moving";
            }

            //Interlock : VV, EV Enable condition 
            if (_turboPump.Status.CurrentSpeed < 2)
            {
                if (!btn_VV.Enabled)
                {
                    btn_VV.Enabled = true;
                }

                if (!btn_EV.Enabled)
                {
                    btn_EV.Enabled = true;
                }
            }
        }

        #endregion

        #region 릴레이 모듈 기능

        private async void UpdateRelayModuleDataAsync()
        {
            _isUpdatingRelayData = true;

            try
            {
                var relayStates = await _relayModule.ReadAllRelayStatesAsync().ConfigureAwait(false);

                if (relayStates != null)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateRelayModuleUI(relayStates));

                        // 활성화된 경우에만 데이터 로깅 수행
                        if (_isLoggingEnabled)
                        {
                            try
                            {
                                // 릴레이 상태 데이터 로깅
                                List<string> relayValues = new List<string>();

                                for (int i = 0; i < 8; i++)
                                {
                                    if (i < relayStates.RelayStates.Length)
                                        relayValues.Add(relayStates.RelayStates[i] ? "ON" : "OFF");
                                    else
                                        relayValues.Add("N/A");
                                }

                                await DataLoggerService.Instance.LogDataAsync("Relay", relayValues).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Instance.LogError($"릴레이 모듈 데이터 로깅 중 오류: {ex.Message}", ex);
                            }
                        }
                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"릴레이 모듈 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                _isUpdatingRelayData = false;
                SafeInvoke(() => _relayModuleUpdateTimer.Start());
            }
        }

        private void UpdateRelayModuleUI(RelayModuleValues values)
        {
            //0 게이트 1 벤트 2 배기 3 이온게이지
            // 게이트 밸브는 센서 값으로 설정
            //btn_GV.Text = values.RelayStates[0] ? "ON" : "OFF";
            btn_VV.Text = values.RelayStates[1] ? "Opened" : "Closed";
            btn_EV.Text = values.RelayStates[2] ? "Opened" : "Closed";
            btn_iongauge.Text = values.RelayStates[3] ? "HV on" : "HV off";
        }

        #endregion

        #region 드라이 펌프 관련 기능

        private async void UpdateDryPumpDataAsync()
        {
            try
            {
                bool result = await _dryPump.UpdateStatusAsync().ConfigureAwait(false);

                if (result)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateDryPumpUI());
                        // 활성화된 경우에만 데이터 로깅 수행
                        if (_isLoggingEnabled)
                        {
                            try
                            {
                                List<string> dryPumpValues = new List<string>
                        {
                            _dryPump.GetStatusText(),
                            _dryPump.Status.MotorFrequency.ToString("F1"),
                            _dryPump.Status.MotorCurrent.ToString("F2"),
                            _dryPump.Status.MotorPower.ToString("F1"),
                            _dryPump.Status.MotorTemperature.ToString("F1"),
                            _dryPump.Status.RunTimeHours.ToString("F1"),
                            _dryPump.Status.IsServiceDue.ToString(),
                            _dryPump.Status.HasWarning.ToString(),
                            _dryPump.Status.HasFault.ToString()
                        };

                                await DataLoggerService.Instance.LogDataAsync("DryPump", dryPumpValues).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Instance.LogError($"드라이 펌프 데이터 로깅 중 오류: {ex.Message}", ex);
                            }
                        }
                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"드라이 펌프 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                SafeInvoke(() => _dryPumpUpdateTimer.Start());
            }
        }

        private void UpdateDryPumpUI()
        {
            // 상태 텍스트 업데이트
            txtDryPumpStatus.Text = _dryPump.GetStatusText();

            // 값들 업데이트
            txtDryPumpFrequency.Text = $"{_dryPump.Status.MotorFrequency:F1} Hz";
            txtDryPumpCurrent.Text = $"{_dryPump.Status.MotorCurrent:F2} A";
            //txtDryPumpPower.Text = $"{_dryPump.Status.MotorPower:F1} W";
            txtDryPumpMotorTemp.Text = $"{_dryPump.Status.MotorTemperature:F1} °C";

            // 실행 시간 표시
            //txtDryPumpRunTime.Text = $"{_dryPump.Status.RunTimeHours:F1} 시간";

            // 서비스 필요 표시
            lblDryPumpService.Visible = _dryPump.Status.IsServiceDue;
            if (_dryPump.Status.IsServiceDue)
            {
                lblDryPumpService.Text = "서비스 필요";
            }

            // 경고/오류 표시
            if (_dryPump.Status.HasFault)
            {
                lblDryPumpWarning.Visible = true;
                lblDryPumpWarning.Text = "오류: " + _dryPump.GetAlarmDescription();
                lblDryPumpWarning.ForeColor = Color.Red;
            }
            else if (_dryPump.Status.HasWarning)
            {
                lblDryPumpWarning.Visible = true;
                lblDryPumpWarning.Text = "경고: " + _dryPump.GetWarningDescription();
                lblDryPumpWarning.ForeColor = Color.Orange;
            }
            else
            {
                lblDryPumpWarning.Visible = false;
            }

            // 버튼 상태 업데이트
            UpdateDryPumpButtonStates();
        }

        private void UpdateDryPumpButtonStates()
        {
            bool IsbtnDryPumpStartEnable = _dryPump.IsConnected && !_dryPump.Status.IsRunning && !_dryPump.Status.IsStopping;
            bool IsbtnDryPumpStopEnable = _dryPump.IsConnected && _dryPump.Status.IsRunning && !_dryPump.Status.IsStopping;
            bool IsbtnDryPumpStandbyEnable = _dryPump.IsConnected && _dryPump.Status.IsRunning && !_dryPump.Status.IsStandby;
            bool IsbtnDryPumpNormalEnable = _dryPump.IsConnected && _dryPump.Status.IsRunning && _dryPump.Status.IsStandby;

            // 시작 버튼 (펌프가 정지 상태일 때만 활성화)
            if (btnDryPumpStart.Enabled != IsbtnDryPumpStartEnable)
            {
                btnDryPumpStart.Enabled = IsbtnDryPumpStartEnable;
            }
            if (btnDryPumpStop.Enabled != IsbtnDryPumpStopEnable)
            {
                btnDryPumpStop.Enabled = IsbtnDryPumpStopEnable;
            }
            if (btnDryPumpStandby.Enabled != IsbtnDryPumpStandbyEnable)
            {
                btnDryPumpStandby.Enabled = IsbtnDryPumpStandbyEnable;
            }
            if (btnDryPumpNormal.Enabled != IsbtnDryPumpNormalEnable)
            {
                btnDryPumpNormal.Enabled = IsbtnDryPumpNormalEnable;
            }
            //// 정지 버튼 (펌프가 실행 중일 때만 활성화)
            //btnDryPumpStop.Enabled = _dryPump.IsConnected && _dryPump.Status.IsRunning && !_dryPump.Status.IsStopping;

            //// 대기 버튼 (실행 중이고 대기 모드가 아닐 때 활성화)
            //btnDryPumpStandby.Enabled = _dryPump.IsConnected && _dryPump.Status.IsRunning && !_dryPump.Status.IsStandby;

            //// 정상 모드 버튼 (실행 중이고 대기 모드일 때 활성화)
            //btnDryPumpNormal.Enabled = _dryPump.IsConnected && _dryPump.Status.IsRunning && _dryPump.Status.IsStandby;
        }

        // 버튼 이벤트 핸들러들
        private void btnDryPumpStart_Click(object sender, EventArgs e)
        {
            if (btn_GV.Text != "Opened")
            {
                MessageBox.Show("GateValve is not opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (btn_VV.Text == "Opened")
            {
                MessageBox.Show("VentValve is opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (btn_EV.Text == "Opened")
            {
                MessageBox.Show("ExhaustValve is opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ExecuteDryPumpCommand(() => _dryPump.Start(), "시작");
            }
        }

        private void btnDryPumpStop_Click(object sender, EventArgs e)
        {
            if (_turboPump.IsRunning)
            {
                MessageBox.Show("TurboPump is in running status.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ExecuteDryPumpCommand(() => _dryPump.Stop(), "정지");
            }
        }

        private void btnDryPumpStandby_Click(object sender, EventArgs e)
        {
            ExecuteDryPumpCommand(() => _dryPump.SetStandby(), "대기 모드");
        }

        private void btnDryPumpNormal_Click(object sender, EventArgs e)
        {
            ExecuteDryPumpCommand(() => _dryPump.SetNormalMode(), "정상 모드");
        }

        private void ExecuteDryPumpCommand(Func<bool> command, string commandName)
        {
            if (_dryPump == null || !_dryPump.IsConnected)
                return;

            try
            {
                bool result = command();
                if (result)
                    AppendLog($"드라이 펌프 {commandName} 명령 성공");
                else
                    AppendLog($"드라이 펌프 {commandName} 명령 실패", true);
            }
            catch (Exception ex)
            {
                AppendLog($"드라이 펌프 {commandName} 오류: {ex.Message}", true);
            }
        }

        #endregion

        #region 터보 펌프 관련 기능

        private async void UpdateTurboPumpDataAsync()
        {
            _isUpdatingTurboPumpData = true;

            try
            {
                bool result = await _turboPump.UpdateStatusAsync().ConfigureAwait(false);

                if (result)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateTurboPumpUI());
                        try
                        {
                            List<string> turboPumpValues = new List<string>
                        {
                            _turboPump.GetStatusText(),
                            _turboPump.Status.CurrentSpeed.ToString(),
                            _turboPump.Status.MotorCurrent.ToString("F2"),
                            _turboPump.Status.MotorTemperature.ToString(),
                            _turboPump.Status.ElectronicsTemperature.ToString(),
                            _turboPump.Status.BearingTemperature.ToString(),
                            _turboPump.Status.IsRemoteActive.ToString(),
                            _turboPump.Status.IsReady.ToString(),
                            _turboPump.Status.IsInNormalOperation.ToString(),
                            _turboPump.Status.RunningTimeHours.ToString(),
                            _turboPump.Status.HasWarning.ToString(),
                            _turboPump.Status.HasError.ToString()
                        };

                            await DataLoggerService.Instance.LogDataAsync("TurboPump", turboPumpValues).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            LoggerService.Instance.LogError($"터보 펌프 데이터 로깅 중 오류: {ex.Message}", ex);
                        }

                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"터보 펌프 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                _isUpdatingTurboPumpData = false;
                SafeInvoke(() => _turboPumpUpdateTimer.Start());
            }
        }

        private void UpdateTurboPumpUI()
        {
            // 상태 텍스트 업데이트
            txtTurboPumpStatus.Text = _turboPump.GetStatusText();

            // 값들 업데이트
            txtTurboPumpSpeed.Text = $"{_turboPump.Status.CurrentSpeed} RPM";
            txtTurboPumpCurrent.Text = $"{_turboPump.Status.MotorCurrent:F2} A";
            txtTurboPumpMotorTemp.Text = $"{_turboPump.Status.MotorTemperature} °C";
            txtTurboPumpElectronicsTemp.Text = $"{_turboPump.Status.ElectronicsTemperature} °C";
            txtTurboPumpBearingTemp.Text = $"{_turboPump.Status.BearingTemperature} °C";
            txtTurboPumpRemote.Text = $"{_turboPump.Status.IsRemoteActive}";
            txtTurboPumpReady.Text = $"{_turboPump.Status.IsReady}";
            txtTurboPumpNormal.Text = $"{_turboPump.Status.IsInNormalOperation}";

            // 실행 시간 표시
            txtTurboPumpRunTime.Text = $"{_turboPump.Status.RunningTimeHours} 시간";

            // 속도 게이지 업데이트
            txtTurboPumpingRate.Text = $"{_turboPump.GetSpeedPercentage().ToString()}%";

            // 경고/오류 표시
            if (_turboPump.Status.HasError)
            {
                lblTurboPumpWarning.Visible = true;
                lblTurboPumpWarning.Text = "오류: " + _turboPump.GetErrorDescription();
                lblTurboPumpWarning.ForeColor = Color.Red;
            }
            else if (_turboPump.Status.HasWarning)
            {
                lblTurboPumpWarning.Visible = true;
                lblTurboPumpWarning.Text = "경고: " + _turboPump.GetWarningDescription();
                lblTurboPumpWarning.ForeColor = Color.Orange;
            }
            else
            {
                lblTurboPumpWarning.Visible = false;
            }

            // 버튼 상태 업데이트
            UpdateTurboPumpButtonStates();
        }

        private void UpdateTurboPumpButtonStates()
        {
            bool IsbtnTurboPumpStartEnable = _turboPump.IsConnected && !_turboPump.Status.IsRunning &&
                                        !_turboPump.Status.IsAccelerating && !_turboPump.Status.IsDecelerating;
            bool IsbtnTurboPumpStopEnable = _turboPump.IsConnected && _turboPump.Status.IsRunning &&
                                       !_turboPump.Status.IsDecelerating;
            bool IsbtnTurboPumpVentEnable = _turboPump.IsConnected && !_turboPump.Status.IsRunning &&
                                       !_turboPump.Status.IsVented;
            bool IsbtnTurboPumpResetEnable = _turboPump.IsConnected && _turboPump.Status.HasError;

            // 시작 버튼 (펌프가 정지 상태일 때만 활성화)
            if (btnTurboPumpStart.Enabled != IsbtnTurboPumpStartEnable)
            {
                btnTurboPumpStart.Enabled = IsbtnTurboPumpStartEnable;
            }
            if (btnTurboPumpStop.Enabled != IsbtnTurboPumpStopEnable)
            {
                btnTurboPumpStop.Enabled = IsbtnTurboPumpStopEnable;
            }
            if (btnTurboPumpVent.Enabled != IsbtnTurboPumpVentEnable)
            {
                btnTurboPumpVent.Enabled = IsbtnTurboPumpVentEnable;
            }
            if (btnTurboPumpReset.Enabled != IsbtnTurboPumpResetEnable)
            {
                btnTurboPumpReset.Enabled = IsbtnTurboPumpResetEnable;
            }


            //// 시작 버튼 (펌프가 정지 상태일 때만 활성화)
            //btnTurboPumpStart.Enabled = _turboPump.IsConnected && !_turboPump.Status.IsRunning &&
            //                            !_turboPump.Status.IsAccelerating && !_turboPump.Status.IsDecelerating;

            //// 정지 버튼 (펌프가 실행 중일 때만, 감속 중이 아닐 때 활성화)
            //btnTurboPumpStop.Enabled = _turboPump.IsConnected && _turboPump.Status.IsRunning &&
            //                           !_turboPump.Status.IsDecelerating;

            //// 벤트 버튼 (펌프가 정지 중이고 벤트되지 않았을 때만 활성화)
            //btnTurboPumpVent.Enabled = _turboPump.IsConnected && !_turboPump.Status.IsRunning &&
            //                           !_turboPump.Status.IsVented;

            //// 오류 리셋 버튼 (오류가 있을 때만 활성화)
            //btnTurboPumpReset.Enabled = _turboPump.IsConnected && _turboPump.Status.HasError;
        }

        // 터보 펌프 버튼 이벤트 핸들러들
        private void btnTurboPumpStart_Click(object sender, EventArgs e)
        {
            //터보 펌프 인터락
            if (!_dryPump.Status.IsRunning)
            {
                MessageBox.Show("Drypump is not in running status.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (_piraniGauge.PressureInTorr > 1)
            {
                MessageBox.Show("Chamber pressure is over 1 Torr.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (btn_GV.Text != "Opened")
            {
                MessageBox.Show("GateValve is not opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (btn_VV.Text == "Opened")
            {
                MessageBox.Show("VentValve is opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (btn_EV.Text == "Opened")
            {
                MessageBox.Show("ExhaustValve is opened.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (!_bathCirculator.IsRunning)
            {
                MessageBox.Show("Chiller is not in running status.", "Interlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                ExecuteTurboPumpCommand(() => _turboPump.Start(), "시작");
                btn_VV.Enabled = false;
                btn_EV.Enabled = false;
            }
        }

        private void btnTurboPumpStop_Click(object sender, EventArgs e)
        {
            ExecuteTurboPumpCommand(() => _turboPump.Stop(), "정지");
        }

        private void btnTurboPumpVent_Click(object sender, EventArgs e)
        {
            ExecuteTurboPumpCommand(() => _turboPump.Vent(), "벤트");
        }

        private void btnTurboPumpCloseVent_Click(object sender, EventArgs e)
        {
            ExecuteTurboPumpCommand(() => _turboPump.CloseVent(), "벤트 닫기");
        }

        private void btnTurboPumpReset_Click(object sender, EventArgs e)
        {
            ExecuteTurboPumpCommand(() => _turboPump.ResetError(), "오류 리셋");
        }

        private void ExecuteTurboPumpCommand(Func<bool> command, string commandName)
        {
            if (_turboPump == null || !_turboPump.IsConnected)
                return;

            try
            {
                bool result = command();
                if (result)
                    AppendLog($"터보 펌프 {commandName} 명령 성공");
                else
                    AppendLog($"터보 펌프 {commandName} 명령 실패", true);
            }
            catch (Exception ex)
            {
                AppendLog($"터보 펌프 {commandName} 오류: {ex.Message}", true);
            }
        }

        #endregion

        #region 이온 게이지

        #endregion
        private void IongaugeOn()
        {
            _relayModule.TurnOnRelay(4);

        }

        private void IongaugeOff()
        {
            _relayModule.TurnOffRelay(4);

        }
        #region Bath Circulator 관련 기능

        private async void UpdateBathCirculatorDataAsync()
        {
            _isUpdatingBathCirculatorData = true;

            try
            {
                bool result = await _bathCirculator.UpdateStatusAsync().ConfigureAwait(false);

                if (result)
                {
                    await _uiUpdateSemaphore.WaitAsync();
                    try
                    {
                        SafeInvoke(() => UpdateBathCirculatorUI());
                    }
                    finally
                    {
                        _uiUpdateSemaphore.Release();
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Bath Circulator 데이터 업데이트 오류: {ex.Message}", true);
            }
            finally
            {
                _isUpdatingBathCirculatorData = false;
                SafeInvoke(() => _bathCirculatorUpdateTimer.Start());
            }
        }

        private void UpdateBathCirculatorUI()
        {
            // 상태 텍스트 업데이트
            txtBathCirculatorStatus.Text = _bathCirculator.GetStatusText();

            // 값들 업데이트
            txtBathCirculatorCurrentTemp.Text = $"{_bathCirculator.Status.CurrentTemperature:F1} °C";
            txtBathCirculatorTargetTemp.Text = $"{_bathCirculator.Status.TargetTemperature:F1} °C";

            // 작동 시간 표시
            if (_bathCirculator.Status.SetTimeMinutes == -1)
            {
                txtBathCirculatorTime.Text = "제한 없음";
            }
            else if (_bathCirculator.Status.SetTimeMinutes == 0)
            {
                txtBathCirculatorTime.Text = "종료 시간";
            }
            else
            {
                txtBathCirculatorTime.Text = $"{_bathCirculator.Status.OperationTimeMinutes} / {_bathCirculator.Status.SetTimeMinutes} 분";
            }

            // 모드 표시
            txtBathCirculatorMode.Text = _bathCirculator.Status.IsFixMode ? "FIX 모드" : "PROG 모드";

            // 경고/오류 표시
            if (_bathCirculator.Status.HasError)
            {
                lblBathCirculatorWarning.Visible = true;
                lblBathCirculatorWarning.Text = "오류: " + _bathCirculator.GetErrorDescription();
                lblBathCirculatorWarning.ForeColor = Color.Red;
            }
            else if (_bathCirculator.Status.HasWarning)
            {
                lblBathCirculatorWarning.Visible = true;
                lblBathCirculatorWarning.Text = "경고 있음";
                lblBathCirculatorWarning.ForeColor = Color.Orange;
            }
            else
            {
                lblBathCirculatorWarning.Visible = false;
            }

            // 버튼 상태 업데이트
            UpdateBathCirculatorButtonStates();
        }

        private void UpdateBathCirculatorButtonStates()
        {
            if(!_bathCirculator.IsConnected)
            {
                btnBathCirculatorStart.Enabled = btnBathCirculatorStop.Enabled = btnBathCirculatorSetTemp.Enabled = btnBathCirculatorSetTime.Enabled = false;
            }
            else
            {
                if(_bathCirculator.Status.IsRunning)
                {
                    if(!btnBathCirculatorStop.Enabled)
                    {
                        btnBathCirculatorStop.Enabled = true;
                    }
                    if (btnBathCirculatorStart.Enabled)
                    {
                        btnBathCirculatorStart.Enabled = false;
                    }
                }
                else
                {
                    if(!btnBathCirculatorStart.Enabled)
                    {
                        btnBathCirculatorStart.Enabled = true;
                    }
                    if (btnBathCirculatorStop.Enabled)
                    {
                        btnBathCirculatorStop.Enabled = false;
                    }
                }
            }

            //// 시작 버튼 (장치가 정지 상태일 때만 활성화)
            //btnBathCirculatorStart.Enabled = _bathCirculator.IsConnected && !_bathCirculator.Status.IsRunning;

            //// 정지 버튼 (장치가 실행 중일 때만 활성화)
            //btnBathCirculatorStop.Enabled = _bathCirculator.IsConnected && _bathCirculator.Status.IsRunning;

            //// 온도 설정 버튼은 항상 활성화 (장치가 연결되어 있으면)
            //btnBathCirculatorSetTemp.Enabled = _bathCirculator.IsConnected;

            //// 시간 설정 버튼은 항상 활성화 (장치가 연결되어 있으면)
            //btnBathCirculatorSetTime.Enabled = _bathCirculator.IsConnected;
        }

        // 버튼 이벤트 핸들러들
        private void btnBathCirculatorStart_Click(object sender, EventArgs e)
        {
            ExecuteBathCirculatorCommand(() => _bathCirculator.Start(), "시작");
        }

        private void btnBathCirculatorStop_Click(object sender, EventArgs e)
        {
            ExecuteBathCirculatorCommand(() => _bathCirculator.Stop(), "정지");
        }

        private void btnBathCirculatorSetTemp_Click(object sender, EventArgs e)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            try
            {
                // 온도 입력 대화상자 표시
                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    "목표 온도를 입력하세요 (℃):",
                    "온도 설정",
                    _bathCirculator.Status.SetTemperature.ToString("F1"));

                if (!string.IsNullOrEmpty(input) && double.TryParse(input, out double temperature))
                {
                    bool result = _bathCirculator.SetTemperature(temperature);
                    if (result)
                    {
                        AppendLog($"Bath Circulator 온도 설정 성공: {temperature}℃");
                    }
                    else
                    {
                        AppendLog("Bath Circulator 온도 설정 실패", true);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Bath Circulator 온도 설정 오류: {ex.Message}", true);
            }
        }

        private void btnBathCirculatorSetTime_Click(object sender, EventArgs e)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            try
            {
                // 시간 설정 라디오 버튼 대화상자 표시
                Form timeSettingForm = new Form
                {
                    Text = "작동 시간 설정",
                    Width = 300,
                    Height = 200,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    StartPosition = FormStartPosition.CenterParent,
                    MaximizeBox = false,
                    MinimizeBox = false
                };

                RadioButton rbNoLimit = new RadioButton
                {
                    Text = "제한 없음",
                    Location = new Point(20, 20),
                    Checked = _bathCirculator.Status.SetTimeMinutes == -1
                };

                RadioButton rbEndTime = new RadioButton
                {
                    Text = "종료 시간",
                    Location = new Point(20, 50),
                    Checked = _bathCirculator.Status.SetTimeMinutes == 0
                };

                RadioButton rbSetTime = new RadioButton
                {
                    Text = "시간 설정 (분):",
                    Location = new Point(20, 80),
                    Checked = _bathCirculator.Status.SetTimeMinutes > 0
                };

                TextBox tbMinutes = new TextBox
                {
                    Location = new Point(150, 80),
                    Width = 100,
                    Text = _bathCirculator.Status.SetTimeMinutes > 0 ? _bathCirculator.Status.SetTimeMinutes.ToString() : "60",
                    Enabled = _bathCirculator.Status.SetTimeMinutes > 0
                };

                rbSetTime.CheckedChanged += (s, args) => tbMinutes.Enabled = rbSetTime.Checked;

                Button btnOk = new Button
                {
                    Text = "확인",
                    DialogResult = DialogResult.OK,
                    Location = new Point(60, 120)
                };

                Button btnCancel = new Button
                {
                    Text = "취소",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(150, 120)
                };

                timeSettingForm.Controls.Add(rbNoLimit);
                timeSettingForm.Controls.Add(rbEndTime);
                timeSettingForm.Controls.Add(rbSetTime);
                timeSettingForm.Controls.Add(tbMinutes);
                timeSettingForm.Controls.Add(btnOk);
                timeSettingForm.Controls.Add(btnCancel);
                timeSettingForm.AcceptButton = btnOk;
                timeSettingForm.CancelButton = btnCancel;

                if (timeSettingForm.ShowDialog() == DialogResult.OK)
                {
                    int minutes;
                    if (rbNoLimit.Checked)
                    {
                        minutes = -1;
                    }
                    else if (rbEndTime.Checked)
                    {
                        minutes = 0;
                    }
                    else
                    {
                        if (!int.TryParse(tbMinutes.Text, out minutes) || minutes <= 0)
                        {
                            MessageBox.Show("유효한 시간(분)을 입력하세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    bool result = _bathCirculator.SetOperationTime(minutes);
                    if (result)
                    {
                        string timeDesc = minutes == -1 ? "제한 없음" : (minutes == 0 ? "종료 시간" : $"{minutes}분");
                        AppendLog($"Bath Circulator 작동 시간 설정 성공: {timeDesc}");
                    }
                    else
                    {
                        AppendLog("Bath Circulator 작동 시간 설정 실패", true);
                    }
                }
            }
            catch (Exception ex)
            {
                AppendLog($"Bath Circulator 시간 설정 오류: {ex.Message}", true);
            }
        }

        private void ExecuteBathCirculatorCommand(Func<bool> command, string commandName)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            try
            {
                bool result = command();
                if (result)
                    AppendLog($"Bath Circulator {commandName} 명령 성공");
                else
                    AppendLog($"Bath Circulator {commandName} 명령 실패", true);
            }
            catch (Exception ex)
            {
                AppendLog($"Bath Circulator {commandName} 오류: {ex.Message}", true);
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void btn_iongauge_Click(object sender, EventArgs e)
        {
            if (_relayModule.CurrentValues.RelayStates[3] == true)
            {
                IongaugeOff();
            }
            else
            {
                IongaugeOn();
            }
        }

        private void VentValveOpen()
        {
            if (_relayModule.CurrentValues.RelayStates[1] == false)
                _relayModule.TurnOnRelay(2);
        }

        private void VentValveClose()
        {
            if (_relayModule.CurrentValues.RelayStates[1] == true)
                _relayModule.TurnOffRelay(2);
        }

        private void btn_VV_Click(object sender, EventArgs e)
        {
            if (_relayModule.CurrentValues.RelayStates[1] == true)
            {
                _relayModule.TurnOffRelay(2);
            }
            else
            {
                _relayModule.TurnOnRelay(2);
            }
        }

        private void ExhaustValveOpen()
        {
            if (_relayModule.CurrentValues.RelayStates[2] == false)
                _relayModule.TurnOnRelay(3);
        }

        private void ExhaustValveClose()
        {
            if (_relayModule.CurrentValues.RelayStates[2] == true)
                _relayModule.TurnOffRelay(3);
        }


        private void btn_EV_Click(object sender, EventArgs e)
        {
            if (_relayModule.CurrentValues.RelayStates[2] == true)
            {
                _relayModule.TurnOffRelay(3);
            }
            else
            {
                _relayModule.TurnOnRelay(3);
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (_turboPump != null && _turboPump.IsConnected)
            {
                try
                {
                    bool result = _turboPump.DisableWatchdog();
                    AppendLog(result ? "터보 펌프 와치독 비활성화 성공" : "터보 펌프 와치독 비활성화 실패", !result);
                }
                catch (Exception ex)
                {
                    AppendLog($"터보 펌프 와치독 비활성화 오류: {ex.Message}", true);
                }
            }
        }

        #endregion

        #region 로깅 기능

        private void AppendLog(string message, bool isError = false)
        {
            if (isError)
            {
                LoggerService.Instance.LogError(message);
            }
            else
            {
                LoggerService.Instance.LogInfo(message);
            }
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// UI 스레드에서 안전하게 코드를 실행합니다.
        /// </summary>
        private void SafeInvoke(Action action)
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.BeginInvoke(action);
                }
                else
                {
                    action();
                }
            }
            catch (Exception ex)
            {
                // 폼이 닫히는 과정에서 발생하는 ObjectDisposedException 무시
                if (!(ex is ObjectDisposedException))
                {
                    Console.WriteLine($"UI 업데이트 오류: {ex.Message}");
                }
            }
        }

        private void UpdateDeviceInfo()
        {
            StringBuilder info = new StringBuilder("장치 정보: ");

            foreach (IDevice device in _deviceList)
            {
                if (device.IsConnected)
                {
                    info.Append($"{device.DeviceName} ({device.Model}), ");
                }
            }

            // 마지막 쉼표 제거
            if (info.Length > 11)
            {
                info.Length -= 2;
            }

            // 상태 표시줄 업데이트
            SafeInvoke(() =>
            {
                // 여기에 장치 정보 업데이트 코드 추가
                // 예: toolStripStatusDevices.Text = info.ToString();
            });
        }

        /// <summary>
        /// 모든 장치의 연결을 해제하고 타이머를 정지합니다.
        /// </summary>
        private void DisconnectAllDevices()
        {
            // 먼저 모든 타이머 중지
            StopAllTimers();

            // 그 다음 장치 연결 해제
            foreach (IDevice device in _deviceList)
            {
                try
                {
                    if (device.IsConnected)
                    {
                        device.Disconnect();
                        AppendLog($"{device.DeviceName} 연결 해제");
                    }
                }
                catch (Exception ex)
                {
                    AppendLog($"{device.DeviceName} 연결 해제 중 오류: {ex.Message}", true);
                }
            }
        }

        /// <summary>
        /// 모든 타이머를 정지합니다.
        /// </summary>
        private void StopAllTimers()
        {
            _ioModuleUpdateTimer?.Stop();
            _relayModuleUpdateTimer?.Stop();
            _dryPumpUpdateTimer?.Stop();
            _turboPumpUpdateTimer?.Stop();
            _bathCirculatorUpdateTimer?.Stop();
            _tempControllerUpdateTimer?.Stop();
        }

        #endregion

        #region 메뉴 및 기타 이벤트 핸들러

        private void menuFileExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuCommSettings_Click(object sender, EventArgs e)
        {
            // 통신 설정 다이얼로그 표시
            MessageBox.Show("통신 설정은 현재 메인 화면에서 직접 변경 가능합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MenuHelpAbout_Click(object sender, EventArgs e)
        {
            // 정보 다이얼로그 표시
            MessageBox.Show("VacX OutSense System Controller\n버전 1.0.0\n\n© 2024 VacX Inc.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // 컨트롤 로드 이벤트 핸들러
        private void bindableTextBox1_Load(object sender, EventArgs e)
        {
            bindableTextBox1.IsReadOnly = true;
        }

        private void bindableTextBox2_Load(object sender, EventArgs e)
        {
            txtPG.LabelText = "Pirani(Torr)";
            txtPG.IsReadOnly = true;
        }

        private void bindableTextBox3_Load(object sender, EventArgs e)
        {
            txtIG.LabelText = "Ion(Torr)";
            txtIG.IsReadOnly = true;
        }

        private void bindableTextBox4_Load_1(object sender, EventArgs e)
        {
            bindableTextBox4.LabelText = "상태";
            bindableTextBox4.IsReadOnly = true;
        }

        private void connectionIndicator2_Load(object sender, EventArgs e)
        {
            // 초기화 코드가 필요한 경우 여기에 추가
        }

        private void connectionIndicator3_Load(object sender, EventArgs e)
        {
            // 초기화 코드가 필요한 경우 여기에 추가
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            // 패널 그리기 코드가 필요한 경우 여기에 추가
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_relayModule.CurrentValues.RelayStates[0] == true)
            {
                _relayModule.TurnOffRelay(1);
            }
            else
            {
                _relayModule.TurnOnRelay(1);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            // 이벤트 처리 코드가 필요한 경우 여기에 추가
        }

        private void label3_Click(object sender, EventArgs e)
        {
            // 이벤트 처리 코드가 필요한 경우 여기에 추가
        }

        private void txtBathCirculatorTime_TextChanged(object sender, EventArgs e)
        {
            // 이벤트 처리 코드가 필요한 경우 여기에 추가
        }

        private void label14_Click(object sender, EventArgs e)
        {
            // 이벤트 처리 코드가 필요한 경우 여기에 추가
        }

        #endregion

        #region 로깅 메뉴 기능 (선택사항)

        // 로깅 메뉴 추가 (선택사항)
        private void AddLoggingMenu()
        {
            // 기존 메뉴에 로깅 관련 메뉴 추가
            ToolStripMenuItem menuLogging = new ToolStripMenuItem("로깅(&L)");

            ToolStripMenuItem menuStartLogging = new ToolStripMenuItem("로깅 시작");
            menuStartLogging.Click += (s, e) => ToggleLogging(true);

            ToolStripMenuItem menuStopLogging = new ToolStripMenuItem("로깅 중지");
            menuStopLogging.Click += (s, e) => ToggleLogging(false);

            ToolStripMenuItem menuOpenLogFolder = new ToolStripMenuItem("로그 폴더 열기");
            menuOpenLogFolder.Click += (s, e) => OpenLogFolder();

            ToolStripMenuItem menuOpenDataFolder = new ToolStripMenuItem("데이터 폴더 열기");
            menuOpenDataFolder.Click += (s, e) => OpenDataFolder();

            // 메뉴 구성
            menuLogging.DropDownItems.Add(menuStartLogging);
            menuLogging.DropDownItems.Add(menuStopLogging);
            menuLogging.DropDownItems.Add(new ToolStripSeparator());
            menuLogging.DropDownItems.Add(menuOpenLogFolder);
            menuLogging.DropDownItems.Add(menuOpenDataFolder);

            // 메인 메뉴에 추가 (menuStrip이 있는 경우)
            if (menuStrip != null && menuStrip.Items.Count > 0)
            {
                menuStrip.Items.Add(menuLogging);
            }
        }

        // 로그 폴더 열기
        private void OpenLogFolder()
        {
            try
            {
                string logFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                System.Diagnostics.Process.Start("explorer.exe", logFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로그 폴더를 열지 못했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // 데이터 폴더 열기
        private void OpenDataFolder()
        {
            try
            {
                string dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                }

                System.Diagnostics.Process.Start("explorer.exe", dataFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"데이터 폴더를 열지 못했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 데이터 로깅 활성화/비활성화 전환
        /// </summary>
        public void ToggleLogging(bool enable)
        {
            _isLoggingEnabled = enable;

            if (enable)
            {
                LoggerService.Instance.LogInfo("데이터 로깅이 활성화되었습니다.");
            }
            else
            {
                LoggerService.Instance.LogInfo("데이터 로깅이 비활성화되었습니다.");
            }

            // 메뉴 UI 업데이트 (메뉴가 있는 경우)
            UpdateLoggingMenuState();
        }

        /// <summary>
        /// 로깅 메뉴 상태 업데이트
        /// </summary>
        private void UpdateLoggingMenuState()
        {
            //// 로깅 메뉴가 있는 경우 UI 상태 업데이트
            //if (menuDataStartLogging != null && menuDataStopLogging != null)
            //{
            //    menuDataStartLogging.Enabled = !_isLoggingEnabled;
            //    menuDataStopLogging.Enabled = _isLoggingEnabled;
            //}
        }

        /// <summary>
        /// 데이터 로깅 초기화
        /// </summary>
        private void InitializeDataLogging()
        {
            // 압력 데이터 로깅 설정
            List<string> pressureHeaders = new List<string>
    {
        "ATM_Pressure(kPa)",
        "Pirani_Pressure(Torr)",
        "Ion_Pressure(Torr)",
        "Gauge_Status"
    };

            // 릴레이 상태 데이터 로깅 설정
            List<string> relayHeaders = new List<string>
    {
        "Relay1_State",
        "Relay2_State",
        "Relay3_State",
        "Relay4_State",
        "Relay5_State",
        "Relay6_State",
        "Relay7_State",
        "Relay8_State"
    };

            // 아날로그 입력 값 로깅 설정
            List<string> analogHeaders = new List<string>
    {
        "Master_Ch1(mA)",
        "Master_Ch2(mA)",
        "Master_Ch3(mA)",
        "Master_Ch4(mA)",
        "Expansion_Ch1(V)",
        "Expansion_Ch2(V)",
        "Expansion_Ch3(V)",
        "Expansion_Ch4(V)",
        "Expansion_Ch5(V)",
        "Expansion_Ch6(V)",
        "Expansion_Ch7(V)",
        "Expansion_Ch8(V)"
    };

            // 드라이 펌프 데이터 로깅 설정
            List<string> dryPumpHeaders = new List<string>
    {
        "Status",
        "Frequency(Hz)",
        "Current(A)",
        "Power(W)",
        "Temperature(°C)",
        "RunTime(h)",
        "IsServiceDue",
        "HasWarning",
        "HasFault"
    };

            // 터보 펌프 데이터 로깅 설정
            List<string> turboPumpHeaders = new List<string>
    {
        "Status",
        "Speed(RPM)",
        "Current(A)",
        "MotorTemp(°C)",
        "ElectronicsTemp(°C)",
        "BearingTemp(°C)",
        "IsRemote",
        "IsReady",
        "IsNormal",
        "RunTime(h)",
        "HasWarning",
        "HasError"
    };

            // 온도 컨트롤러 데이터 로깅 설정
            List<string> tempControllerHeaders = new List<string>
    {
        "Ch1_PV(°C)",
        "Ch1_SV(°C)",
        "Ch1_HeatingMV(%)",
        "Ch1_Status",
        "Ch2_PV(°C)",
        "Ch2_SV(°C)",
        "Ch2_HeatingMV(%)",
        "Ch2_Status"
    };

            // 로그 세션 시작
            DataLoggerService.Instance.StartLogging("PressureInTorr", pressureHeaders);
            DataLoggerService.Instance.StartLogging("Relay", relayHeaders);
            DataLoggerService.Instance.StartLogging("AnalogInput", analogHeaders);
            DataLoggerService.Instance.StartLogging("DryPump", dryPumpHeaders);
            DataLoggerService.Instance.StartLogging("TurboPump", turboPumpHeaders);
            DataLoggerService.Instance.StartLogging("TempController", tempControllerHeaders);
        }

        /// <summary>
        /// 로그 이벤트 핸들러
        /// </summary>
        private void LoggerService_LogAdded(object sender, string logMessage)
        {
            // UI 스레드에서 실행 확인
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => LoggerService_LogAdded(sender, logMessage)));
                return;
            }

            // 로그 창에 로그 추가
            if (txtLog != null)
            {
                // 최대 로그 표시 개수 제한 (성능 최적화)
                const int MAX_VISIBLE_LOGS = 1000;
                string[] lines = txtLog.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length > MAX_VISIBLE_LOGS)
                {
                    StringBuilder sb = new StringBuilder();
                    for (int i = lines.Length - MAX_VISIBLE_LOGS; i < lines.Length; i++)
                    {
                        sb.AppendLine(lines[i]);
                    }
                    txtLog.Text = sb.ToString();
                }

                // 새 로그 추가
                txtLog.AppendText(logMessage + Environment.NewLine);
                txtLog.SelectionStart = txtLog.Text.Length;
                txtLog.ScrollToCaret();
            }
        }

        /// <summary>
        /// 로깅 시스템 초기화
        /// </summary>
        private void InitializeLogging()
        {
            try
            {
                // 로그 폴더가 없으면 생성
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
                if (!Directory.Exists(dataDirectory))
                {
                    Directory.CreateDirectory(dataDirectory);
                }

                // 로그 서비스 초기화
                LoggerService.Instance.LogAdded += LoggerService_LogAdded;
                LoggerService.Instance.LogInfo("애플리케이션이 시작되었습니다.");

                // 데이터 로거 초기화
                InitializeDataLogging();

                // 로깅 시작 시 알림
                LoggerService.Instance.LogInfo("데이터 로깅이 활성화되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"로깅 시스템 초기화 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        #endregion

        private void MainForm_Load(object sender, EventArgs e)
        {
            //DisableButtonKeyboardBehavior();
        }

        #region 버튼 클릭 방지(스페이스, 엔터 키)
        // 2. 폼에서 모든 버튼의 기본 포커스 동작 변경 (사용 시 MainForm 클래스에 추가)
        private void DisableButtonKeyboardBehavior()
        {
            // 폼의 모든 버튼에 대해 처리
            foreach (Control control in this.Controls)
            {
                ProcessControlsRecursively(control);
            }
        }

        private void ProcessControlsRecursively(Control parent)
        {
            // 현재 컨트롤이 버튼인 경우 처리
            if (parent is Button button)
            {
                // 키 입력으로 클릭되지 않도록 설정
                button.PreviewKeyDown += Button_PreviewKeyDown;
            }

            // 컨테이너 컨트롤이면 자식 컨트롤들도 처리
            foreach (Control child in parent.Controls)
            {
                ProcessControlsRecursively(child);
            }
        }

        private void Button_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            // 스페이스바와 엔터키 처리 방지
            if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
            {
                e.IsInputKey = false;
            }
        }

        // 방법 3: 폼 레벨에서 처리하는 방법 (폼 클래스에 추가)
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // 폼에 있는 어떤 버튼이든 포커스가 있을 때 엔터나 스페이스를 처리
            if ((keyData == Keys.Enter || keyData == Keys.Space) &&
                this.ActiveControl is Button)
            {
                return true; // 키 입력 처리됨으로 표시하여 기본 동작 방지
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }


        #endregion

    }
}