using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.DryPump;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.IO_Module;
using VacX_OutSense.Core.Devices.IO_Module.Enum;
using VacX_OutSense.Core.Devices.IO_Module.Models;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Devices.TurboPump;
using VacX_OutSense.Models;
using VacX_OutSense.Utils;

namespace VacX_OutSense
{
    public partial class MainForm : Form
    {
        #region 필드 및 속성

        #region 타이머 관련 필드 (클래스 필드 영역에 추가)

        // CH1 타이머 관련
        private System.Windows.Forms.Timer _ch1AutoStopTimer;
        private DateTime _ch1StartTime;
        private TimeSpan _ch1Duration;
        private bool _ch1TimerActive = false;
        private bool _ch1WaitingForTargetTemp = false;  // SV 도달 대기 상태
        private double _ch1TargetTolerance = 0.1;       // 목표 온도 허용 오차 (0.1)

        #endregion


        private System.Windows.Forms.Timer _connectionCheckTimer;
        private Dictionary<string, bool> _previousConnectionStates = new Dictionary<string, bool>();

        // 최적화된 서비스들
        private OptimizedDataCollectionService _dataCollectionService;
        private SimplifiedUIUpdateService _uiUpdateService;

        #region 장치 인스턴스
        public IO_Module _ioModule;
        public DryPump _dryPump;
        public TurboPump _turboPump;
        public BathCirculator _bathCirculator;
        public TempController _tempController;
        #endregion

        #region 게이지 인스턴스
        public ATMswitch _atmSwitch;
        public PiraniGauge _piraniGauge;
        public IonGauge _ionGauge;
        #endregion

        #region 통신 관리
        private MultiPortSerialManager _multiPortManager;
        private Dictionary<string, DevicePortAdapter> _deviceAdapters = new Dictionary<string, DevicePortAdapter>();
        private List<IDevice> _deviceList = new List<IDevice>();
        #endregion

        #region 통신 설정
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
        #endregion

        #region 로깅 관련
        private bool _isLoggingEnabled = true;
        private ToolStripMenuItem _menuStartLogging;
        private ToolStripMenuItem _menuStopLogging;
        #endregion

        #endregion

        #region 생성자 및 초기화

        public MainForm()
        {
            InitializeComponent();
            this.FormClosing += MainForm_FormClosing;

            InitializeTimers();
        }

        #region 타이머 초기화 (MainForm_Load 또는 생성자에 추가)

        private void InitializeTimers()
        {
            // CH1 자동 정지 타이머 초기화
            _ch1AutoStopTimer = new System.Windows.Forms.Timer();
            _ch1AutoStopTimer.Interval = 1000; // 1초마다 업데이트
            _ch1AutoStopTimer.Tick += Ch1AutoStopTimer_Tick;

            // 초기 상태 설정
            UpdateCh1TimerControls();
        }

        #endregion

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                // 메인폼 비활성화
                this.Enabled = false;

                // 로딩 화면 표시
                using (var loadingForm = new LoadingForm())
                {
                    loadingForm.Show();
                    Application.DoEvents();

                    // 단계별 초기화
                    loadingForm.UpdateStatus("로깅 시스템 초기화 중...");
                    await InitializeLoggingAsync();

                    loadingForm.UpdateStatus("통신 관리자 초기화 중...");
                    await InitializeCommunicationAsync();

                    loadingForm.UpdateStatus("장치 생성 중...");
                    await CreateDevicesAsync();

                    loadingForm.UpdateStatus("장치 연결 중...");
                    await ConnectDevicesAsync();

                    loadingForm.UpdateStatus("UI 서비스 시작 중...");
                    StartServices();

                    loadingForm.UpdateStatus("초기화 완료!");
                    await Task.Delay(200);
                }

                LogInfo("시스템 초기화 완료");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError("초기화 실패", ex);
            }
            finally
            {
                // 메인폼 활성화
                this.Enabled = true;
                this.Focus();
            }
        }

        private void InitializeConnectionChecker()
        {
            _connectionCheckTimer = new System.Windows.Forms.Timer();
            _connectionCheckTimer.Interval = 5000; // 5초마다 확인
            _connectionCheckTimer.Tick += ConnectionCheckTimer_Tick;
            _connectionCheckTimer.Start();
        }

        private void ConnectionCheckTimer_Tick(object sender, EventArgs e)
        {
            CheckDeviceConnections();
        }

        private void CheckDeviceConnections()
        {
            foreach (var device in _deviceList)
            {
                string deviceName = device.DeviceName;
                bool currentState = device.IsConnected;

                if (_previousConnectionStates.ContainsKey(deviceName))
                {
                    bool previousState = _previousConnectionStates[deviceName];

                    if (previousState && !currentState)
                    {
                        // 연결이 끊어짐
                        LogWarning($"{deviceName} 연결 끊김 감지");
                        StopDeviceDataLogging(deviceName);
                    }
                    else if (!previousState && currentState)
                    {
                        // 연결이 복구됨
                        LogInfo($"{deviceName} 연결 복구됨");
                    }
                }

                _previousConnectionStates[deviceName] = currentState;
            }
        }


        private async Task InitializeLoggingAsync()
        {
            await Task.Run(() =>
            {
                // AsyncLoggingService 싱글톤 시작
                AsyncLoggingService.Instance.Start();

                // 로그 이벤트 연결
                AsyncLoggingService.Instance.LogAdded += OnLogAdded;

                // LoggerService를 AsyncLoggingService로 대체
                LoggerService.Instance = new LoggerServiceWrapper(AsyncLoggingService.Instance);

                // 데이터 로거 헤더 설정
                InitializeDataLogging();

                // 메뉴 추가
                AddLoggingMenu();
            });
        }

        private async Task InitializeCommunicationAsync()
        {
            await Task.Run(() =>
            {
                _multiPortManager = MultiPortSerialManager.Instance;

                // 포트 어댑터 생성
                RegisterDevicePort("COM1", _turboPumpDefaultSettings);
                RegisterDevicePort("COM3", _dryPumpDefaultSettings);
                RegisterDevicePort("COM4", _defaultSettings);
                RegisterDevicePort("COM5", _bathCirculatorDefaultSettings);
                RegisterDevicePort("COM6", _tempControllerDefaultSettings);
            });
        }

        private void RegisterDevicePort(string portName, CommunicationSettings settings)
        {
            var adapter = new DevicePortAdapter(portName, _multiPortManager);
            _deviceAdapters[portName] = adapter;
        }

        private async Task CreateDevicesAsync()
        {
            await Task.Run(() =>
            {
                // IO 모듈 (개선된 버전)
                _ioModule = new IO_Module(_deviceAdapters["COM4"], 1);

                // 펌프들
                _dryPump = new DryPump(_deviceAdapters["COM3"], "ECODRY 25 plus", 1);

                _turboPump = new TurboPump(_deviceAdapters["COM1"], "MAG W 1300", 1);

                // 온도 장치들
                _bathCirculator = new BathCirculator(_deviceAdapters["COM5"], "LK-1000", 1);

                _tempController = new TempController(_deviceAdapters["COM6"], 1);

                // 게이지들
                _atmSwitch = new ATMswitch();
                _piraniGauge = new PiraniGauge();
                _ionGauge = new IonGauge();

                // 장치 목록
                _deviceList.AddRange(new IDevice[] { _ioModule, _dryPump, _turboPump, _bathCirculator, _tempController });
            });

            // UI 스레드에서 데이터바인딩
            SetupDataBindings();
        }
        // 통신 오류 처리 메서드
        public void HandleDeviceCommunicationError(string deviceName)
        {
            try
            {
                // 장치별 로그 타입 매핑
                string logType = deviceName switch
                {
                    "IOModule" => "Pressure",
                    "DryPump" => "DryPump",
                    "TurboPump" => "TurboPump",
                    "BathCirculator" => "BathCirculator",
                    "TempController" => "TempController",
                    _ => null
                };

                if (!string.IsNullOrEmpty(logType))
                {
                    DataLoggerService.Instance.StopLogging(logType);
                    LogWarning($"{deviceName} 통신 오류로 인한 데이터 로깅 중단");
                }
            }
            catch (Exception ex)
            {
                LogError($"{deviceName} 통신 오류 처리 실패", ex);
            }
        }


        /// <summary>
        /// 장치 연결 상태 변경 이벤트 핸들러
        /// </summary>
        private void OnDeviceConnectionStateChanged(object sender, bool isConnected)
        {
            if (sender is IDevice device)
            {
                string deviceName = device.DeviceName;

                if (isConnected)
                {
                    LogInfo($"{deviceName} 연결됨");
                }
                else
                {
                    LogWarning($"{deviceName} 연결 끊김");

                    // 연결이 끊긴 장치의 데이터 로그 중단
                    StopDeviceDataLogging(deviceName);
                }
            }
        }

        /// <summary>
        /// 특정 장치의 데이터 로깅 중단
        /// </summary>
        private void StopDeviceDataLogging(string deviceName)
        {
            try
            {
                // 장치별 로그 타입 매핑
                string logType = deviceName switch
                {
                    "ECODRY 25 plus" => "DryPump",
                    "MAG W 1300" => "TurboPump",
                    "LK-1000" => "BathCirculator",
                    "TempController" => "TempController",
                    "IO Module" => "Pressure",
                    _ => null
                };

                if (!string.IsNullOrEmpty(logType))
                {
                    // 해당 장치의 데이터 로깅 중단
                    DataLoggerService.Instance.StopLogging(logType);
                    LogInfo($"{deviceName} 데이터 로깅 중단됨");
                }
            }
            catch (Exception ex)
            {
                LogError($"{deviceName} 데이터 로깅 중단 오류", ex);
            }
        }

        private void SetupDataBindings()
        {
            try
            {
                connectionIndicator_iomodule.DataSource = _ioModule;
                connectionIndicator_iomodule.DataMember = "IsConnected";

                connectionIndicator_drypump.DataSource = _dryPump;
                connectionIndicator_drypump.DataMember = "IsConnected";

                connectionIndicator_turbopump.DataSource = _turboPump;
                connectionIndicator_turbopump.DataMember = "IsConnected";

                connectionIndicator_bathcirculator.DataSource = _bathCirculator;
                connectionIndicator_bathcirculator.DataMember = "IsConnected";

                connectionIndicator_tempcontroller.DataSource = _tempController;
                connectionIndicator_tempcontroller.DataMember = "IsConnected";

                // PropertyChanged 이벤트 추가
                _ioModule.PropertyChanged += Device_PropertyChanged;
                _dryPump.PropertyChanged += Device_PropertyChanged;
                _turboPump.PropertyChanged += Device_PropertyChanged;
                _bathCirculator.PropertyChanged += Device_PropertyChanged;
                _tempController.PropertyChanged += Device_PropertyChanged;

            }
            catch (Exception ex)
            {
                LogError("데이터바인딩 오류", ex);
            }
        }

        // 새로운 이벤트 핸들러 추가
        private void Device_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsConnected" && sender is IDevice device)
            {
                if (!device.IsConnected)
                {
                    LogWarning($"{device.DeviceName} 연결 끊김");
                    StopDeviceDataLogging(device.DeviceName);
                }
                else
                {
                    LogInfo($"{device.DeviceName} 연결됨");
                }
            }
        }

        private async Task ConnectDevicesAsync()
        {
            var tasks = new List<Task<bool>>();

            tasks.Add(Task.Run(() => _ioModule.Connect("COM4", _defaultSettings)));
            tasks.Add(Task.Run(() => _dryPump.Connect("COM3", _dryPumpDefaultSettings)));
            tasks.Add(Task.Run(() => _turboPump.Connect("COM1", _turboPumpDefaultSettings)));
            tasks.Add(Task.Run(() => _bathCirculator.Connect("COM5", _bathCirculatorDefaultSettings)));
            tasks.Add(Task.Run(() => _tempController.Connect("COM6", _tempControllerDefaultSettings)));

            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < _deviceList.Count; i++)
            {
                var device = _deviceList[i];
                var connected = results[i];
                LogInfo($"{device.DeviceName} 연결 {(connected ? "성공" : "실패")}");
                if (!connected) MessageBox.Show($"{device.DeviceName} 연결 실패.");
            }
        }

        private void StartServices()
        {
            // UI 업데이트 서비스
            _uiUpdateService = new SimplifiedUIUpdateService(this);
            _uiUpdateService.Start();

            // 데이터 수집 서비스
            _dataCollectionService = new OptimizedDataCollectionService(this);
            _dataCollectionService.DataUpdated += OnDataUpdated;
            _dataCollectionService.Start();

            // 연결 상태 체크 타이머 시작
            InitializeConnectionChecker();

            LogInfo("서비스 시작 완료");
        }

        #endregion

        #region 이벤트 핸들러

        private void OnDataUpdated(object sender, UIDataSnapshot snapshot)
        {
            // UI 업데이트 요청
            _uiUpdateService.RequestUpdate(snapshot);

            // 비동기 로깅 - DataLoggerService만 사용 (AsyncLoggingService 사용 안함)
            if (_isLoggingEnabled)
            {
                Task.Run(() =>
                {
                    try
                    {
                        // IOModule이 연결되어 있을 때만 압력 및 밸브 데이터 로깅
                        if (snapshot.Connections.IOModule)
                        {
                            var pressureValues = new List<string>
                    {
                        snapshot.AtmPressure.ToString("F2"),
                        snapshot.PiraniPressure.ToString("E2"),
                        snapshot.IonPressure.ToString("E2"),
                        snapshot.IonGaugeStatus,
                        snapshot.GateValveStatus,
                        snapshot.VentValveStatus,
                        snapshot.ExhaustValveStatus,
                        snapshot.IonGaugeHVStatus
                    };
                            DataLoggerService.Instance.LogDataAsync("Pressure", pressureValues);
                        }

                        // 드라이펌프 로깅 (연결되어 있을 때만)
                        if (snapshot.Connections.DryPump && _dryPump?.Status != null)
                        {
                            var dryPumpValues = new List<string>
                    {
                        snapshot.DryPump.Status,
                        _dryPump.Status.MotorFrequency.ToString("F1"),
                        _dryPump.Status.MotorCurrent.ToString("F2"),
                        _dryPump.Status.MotorTemperature.ToString("F1"),
                        snapshot.DryPump.HasWarning.ToString(),
                        snapshot.DryPump.HasError.ToString()
                    };
                            DataLoggerService.Instance.LogDataAsync("DryPump", dryPumpValues);
                        }

                        // 터보펌프 로깅 (연결되어 있을 때만)
                        if (snapshot.Connections.TurboPump && _turboPump?.Status != null)
                        {
                            var turboPumpValues = new List<string>
                    {
                        snapshot.TurboPump.Status,
                        _turboPump.Status.CurrentSpeed.ToString(),
                        _turboPump.Status.MotorCurrent.ToString("F2"),
                        _turboPump.Status.MotorTemperature.ToString(),
                        snapshot.TurboPump.HasWarning.ToString(),
                        snapshot.TurboPump.HasError.ToString()
                    };
                            DataLoggerService.Instance.LogDataAsync("TurboPump", turboPumpValues);
                        }

                        // 칠러 로깅 (연결되어 있을 때만)
                        if (snapshot.Connections.BathCirculator && _bathCirculator?.Status != null)
                        {
                            var bathValues = new List<string>
                    {
                        snapshot.BathCirculator.Status,
                        _bathCirculator.Status.CurrentTemperature.ToString("F1"),
                        _bathCirculator.Status.TargetTemperature.ToString("F1"),
                        snapshot.BathCirculator.Mode,
                        snapshot.BathCirculator.Time,
                        snapshot.BathCirculator.HasError.ToString(),
                        snapshot.BathCirculator.HasWarning.ToString()
                    };
                            DataLoggerService.Instance.LogDataAsync("BathCirculator", bathValues);
                        }

                        // 온도컨트롤러 로깅 (연결되어 있을 때만)
                        if (snapshot.Connections.TempController && _tempController?.Status != null)
                        {
                            var tempValues = new List<string>
                    {
                        snapshot.TempController.Channels[0].PresentValue,
                        snapshot.TempController.Channels[0].SetValue,
                        snapshot.TempController.Channels[0].HeatingMV.Replace(" %", ""),
                        snapshot.TempController.Channels[0].Status,
                        snapshot.TempController.Channels[1].PresentValue,
                        snapshot.TempController.Channels[1].SetValue,
                        snapshot.TempController.Channels[1].HeatingMV.Replace(" %", ""),
                        snapshot.TempController.Channels[1].Status
                    };
                            DataLoggerService.Instance.LogDataAsync("TempController", tempValues);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError($"데이터 로깅 오류: {ex.Message}", ex);
                    }
                });
            }
        }

        #endregion

        #region UI 업데이트 메서드 (SimplifiedUIUpdateService에서 호출)

        public void SetAtmPressureText(string value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetAtmPressureText), value);
                return;
            }
            try
            {
                if (txtATM != null) txtATM.TextValue = value;
            }
            catch (Exception ex)
            {
                LogDebug($"AtmPressure 업데이트 오류: {ex.Message}");
            }
        }

        public void SetPiraniPressureText(string value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetPiraniPressureText), value);
                return;
            }
            try
            {
                if (txtPG != null)
                {
                    txtPG.TextValue = value;
                    // 압력이 높으면 빨간색 경고
                    if (double.TryParse(value.Replace("E", "e"), out double pressure) && pressure > 1E-3)
                    {
                        txtPG.ForeColor = Color.Red;
                    }
                    else
                    {
                        txtPG.ForeColor = SystemColors.WindowText;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"PiraniPressure 업데이트 오류: {ex.Message}");
            }
        }

        public void SetIonPressureText(string value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetIonPressureText), value);
                return;
            }
            try
            {
                if (txtIG != null) txtIG.TextValue = value;
            }
            catch (Exception ex)
            {
                LogDebug($"IonPressure 업데이트 오류: {ex.Message}");
            }
        }

        public void SetIonGaugeStatusText(string value)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetIonGaugeStatusText), value);
                return;
            }
            try
            {
                if (txtIGStatus != null) txtIGStatus.TextValue = value;
            }
            catch (Exception ex)
            {
                LogDebug($"IonGaugeStatus 업데이트 오류: {ex.Message}");
            }
        }

        public void SetGateValveStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetGateValveStatus), status);
                return;
            }
            try
            {
                if (btn_GV != null && !btn_GV.Focused)
                {
                    btn_GV.Text = status;
                    if (status == "Moving")
                        btn_GV.BackColor = Color.Yellow;
                    else if (status == "Opened")
                        btn_GV.BackColor = Color.LightGreen;
                    else
                        btn_GV.BackColor = SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"GateValve 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetVentValveStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetVentValveStatus), status);
                return;
            }
            try
            {
                if (btn_VV != null && !btn_VV.Focused)
                {
                    btn_VV.Text = status;
                    btn_VV.BackColor = status == "Opened" ? Color.LightBlue : SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"VentValve 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetExhaustValveStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetExhaustValveStatus), status);
                return;
            }
            try
            {
                if (btn_EV != null && !btn_EV.Focused)
                {
                    btn_EV.Text = status;
                    btn_EV.BackColor = status == "Opened" ? Color.LightCoral : SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ExhaustValve 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetIonGaugeHVStatus(string status)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(SetIonGaugeHVStatus), status);
                return;
            }
            try
            {
                if (btn_iongauge != null && !btn_iongauge.Focused)
                {
                    btn_iongauge.Text = status;
                    btn_iongauge.BackColor = status == "HV on" ? Color.Orange : SystemColors.Control;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"IonGaugeHV 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetDryPumpStatus(string status, string speed, string current, string temperature,
            bool hasWarning, bool hasError, string warningMessage = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, bool, bool, string>(SetDryPumpStatus),
                           status, speed, current, temperature, hasWarning, hasError, warningMessage);
                return;
            }
            try
            {
                if (txtDryPumpStatus != null) txtDryPumpStatus.Text = status;
                if (txtDryPumpFrequency != null) txtDryPumpFrequency.Text = speed;
                if (txtDryPumpCurrent != null) txtDryPumpCurrent.Text = current;
                if (txtDryPumpMotorTemp != null) txtDryPumpMotorTemp.Text = temperature;

                if (lblDryPumpWarning != null)
                {
                    if (hasError || hasWarning)
                    {
                        lblDryPumpWarning.Visible = true;
                        lblDryPumpWarning.Text = warningMessage;
                        lblDryPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange;
                    }
                    else
                    {
                        lblDryPumpWarning.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"DryPump 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetTurboPumpStatus(string status, string speed, string current, string temperature,
            bool hasWarning, bool hasError, string warningMessage = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, bool, bool, string>(SetTurboPumpStatus),
                           status, speed, current, temperature, hasWarning, hasError, warningMessage);
                return;
            }
            try
            {
                if (txtTurboPumpStatus != null) txtTurboPumpStatus.Text = status;
                if (txtTurboPumpSpeed != null) txtTurboPumpSpeed.Text = speed;
                if (txtTurboPumpCurrent != null) txtTurboPumpCurrent.Text = current;
                if (txtTurboPumpMotorTemp != null) txtTurboPumpMotorTemp.Text = temperature;

                if (lblTurboPumpWarning != null)
                {
                    if (hasError || hasWarning)
                    {
                        lblTurboPumpWarning.Visible = true;
                        lblTurboPumpWarning.Text = warningMessage;
                        lblTurboPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange;
                    }
                    else
                    {
                        lblTurboPumpWarning.Visible = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"TurboPump 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetBathCirculatorStatus(string status, string currentTemp, string targetTemp,
            string time, string mode, bool hasError, bool hasWarning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, string, bool, bool>(SetBathCirculatorStatus),
                           status, currentTemp, targetTemp, time, mode, hasError, hasWarning);
                return;
            }
            try
            {
                if (txtBathCirculatorStatus != null) txtBathCirculatorStatus.Text = status;
                if (txtBathCirculatorCurrentTemp != null) txtBathCirculatorCurrentTemp.Text = currentTemp;
                if (txtBathCirculatorTargetTemp != null) txtBathCirculatorTargetTemp.Text = targetTemp;
                if (txtBathCirculatorTime != null) txtBathCirculatorTime.Text = time;
                if (txtBathCirculatorMode != null) txtBathCirculatorMode.Text = mode;
            }
            catch (Exception ex)
            {
                LogDebug($"BathCirculator 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetTempControllerChannelStatus(int channel, string presentValue, string setValue,
            string status, string heatingMV, bool isAutoTuning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, string, string, string, string, bool>(SetTempControllerChannelStatus),
                           channel, presentValue, setValue, status, heatingMV, isAutoTuning);
                return;
            }
            try
            {
                switch (channel)
                {
                    case 1:
                        if (txtCh1PresentValue != null) txtCh1PresentValue.Text = presentValue;
                        if (txtCh1SetValue != null) txtCh1SetValue.Text = setValue;
                        if (txtCh1Status != null) txtCh1Status.Text = status;
                        if (txtCh1HeatingMV != null) txtCh1HeatingMV.Text = heatingMV;
                        if (txtCh1IsAutotune != null) txtCh1IsAutotune.Text = isAutoTuning ? "On" : "Off";
                        break;

                    case 2:
                        if (txtCh2PresentValue != null) txtCh2PresentValue.Text = presentValue;
                        if (txtCh2SetValue != null) txtCh2SetValue.Text = setValue;
                        if (txtCh2Status != null) txtCh2Status.Text = status;
                        if (txtCh2HeatingMV != null) txtCh2HeatingMV.Text = heatingMV;
                        if (txtCh2IsAutotune != null) txtCh2IsAutotune.Text = isAutoTuning ? "On" : "Off";
                        break;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"TempController CH{channel} 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetButtonEnabled(string buttonName, bool enabled)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, bool>(SetButtonEnabled), buttonName, enabled);
                return;
            }
            try
            {
                Button button = GetButtonByName(buttonName);
                if (button != null && button.Enabled != enabled)
                {
                    button.Enabled = enabled;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"버튼 [{buttonName}] 활성화 상태 업데이트 오류: {ex.Message}");
            }
        }

        public void SetConnectionStatus(string deviceName, bool isConnected)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, bool>(SetConnectionStatus), deviceName, isConnected);
                return;
            }
            try
            {
                Control indicator = GetConnectionIndicatorByName(deviceName);
                if (indicator != null)
                {
                    indicator.BackColor = isConnected ? Color.Green : Color.Red;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"연결 상태 표시기 [{deviceName}] 업데이트 오류: {ex.Message}");
            }
        }

        private Button GetButtonByName(string buttonName)
        {
            switch (buttonName.ToLower())
            {
                case "iongauge": case "btn_iongauge": return btn_iongauge;
                case "ventvalve": case "btn_vv": return btn_VV;
                case "exhaustvalve": case "btn_ev": return btn_EV;
                case "drypumpstart": case "btndrypumpstart": return btnDryPumpStart;
                case "drypumpstop": case "btndrypumpstop": return btnDryPumpStop;
                case "drypumpstandby": case "btndrypumpstandby": return btnDryPumpStandby;
                case "drypumpnormal": case "btndrypumpnormal": return btnDryPumpNormal;
                case "turbopumpstart": case "btnturbopumpstart": return btnTurboPumpStart;
                case "turbopumpstop": case "btnturbopumpstop": return btnTurboPumpStop;
                case "turbopumpvent": case "btnturbopumpvent": return btnTurboPumpVent;
                case "turbopumpreset": case "btnturbopumpreset": return btnTurboPumpReset;
                case "bathcirculatorstart": case "btnbathcirculatorstart": return btnBathCirculatorStart;
                case "bathcirculatorstop": case "btnbathcirculatorstop": return btnBathCirculatorStop;
                case "ch1start": case "btnch1start": return btnCh1Start;
                case "ch1stop": case "btnch1stop": return btnCh1Stop;
                case "ch2start": case "btnch2start": return btnCh2Start;
                case "ch2stop": case "btnch2stop": return btnCh2Stop;
                default: return null;
            }
        }

        private Control GetConnectionIndicatorByName(string deviceName)
        {
            switch (deviceName.ToLower())
            {
                case "iomodule": return connectionIndicator_iomodule;
                case "drypump": return connectionIndicator_drypump;
                case "turbopump": return connectionIndicator_turbopump;
                case "bathcirculator": return connectionIndicator_bathcirculator;
                case "tempcontroller": return connectionIndicator_tempcontroller;
                default: return null;
            }
        }

        #endregion

        #region 밸브 제어 메서드

        private async void btn_GV_Click(object sender, EventArgs e)
        {
            btn_GV.Enabled = false;
            try
            {
                bool isOpen = btn_GV.Text == "Opened";
                bool result = await _ioModule.ControlGateValveAsync(!isOpen);

                if (result) 
                {
                    LogInfo($"게이트 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
                }
            }
            finally
            {
                btn_GV.Enabled = true;
            }
        }

        private async void btn_VV_Click(object sender, EventArgs e)
        {
            // 인터락 체크
            if (_turboPump?.Status?.CurrentSpeed > 100)
            {
                MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btn_VV.Enabled = false;
            try
            {
                bool isOpen = btn_VV.Text == "Opened";
                bool result = await _ioModule.ControlVentValveAsync(!isOpen);

                if (result)
                {
                    LogInfo($"벤트 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
                }
            }
            finally
            {
                btn_VV.Enabled = true;
            }
        }

        private async void btn_EV_Click(object sender, EventArgs e)
        {
            // 인터락 체크
            if (_turboPump?.Status?.CurrentSpeed > 100)
            {
                MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btn_EV.Enabled = false;
            try
            {
                bool isOpen = btn_EV.Text == "Opened";
                bool result = await _ioModule.ControlExhaustValveAsync(!isOpen);

                if (result)
                {
                    LogInfo($"배기 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
                }
            }
            finally
            {
                btn_EV.Enabled = true;
            }
        }

        private async void btn_iongauge_Click(object sender, EventArgs e)
        {
            //// 압력 체크
            //if (_dataCollectionService?.GetLatestPressure() > 1E-2 && btn_iongauge.Text != "HV on")
            //{
            //    MessageBox.Show("압력이 너무 높습니다 (> 1E-2 Torr)", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            //    return;
            //}

            btn_iongauge.Enabled = false;
            try
            {
                bool isOn = btn_iongauge.Text == "HV on";
                bool result = await _ioModule.ControlIonGaugeHVAsync(!isOn);

                if (result)
                {
                    LogInfo($"이온 게이지 HV {(!isOn ? "ON" : "OFF")} 성공");
                }
            }
            finally
            {
                btn_iongauge.Enabled = true;
            }
        }

        #endregion

        #region 펌프 제어 메서드

        private async void btnDryPumpStart_Click(object sender, EventArgs e)
        {
            // 인터락 체크
            if (btn_GV.Text != "Opened")
            {
                MessageBox.Show("게이트 밸브가 열려있지 않습니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (btn_VV.Text == "Opened" || btn_EV.Text == "Opened")
            {
                MessageBox.Show("벤트 또는 배기 밸브가 열려있습니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnDryPumpStart.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.Start());
                LogInfo("드라이펌프 시작");
            }
            finally
            {
                btnDryPumpStart.Enabled = true;
            }
        }

        private async void btnDryPumpStop_Click(object sender, EventArgs e)
        {
            // 인터락 체크
            if (_turboPump?.Status?.IsRunning == true)
            {
                MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnDryPumpStop.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.Stop());
                LogInfo("드라이펌프 정지");
            }
            finally
            {
                btnDryPumpStop.Enabled = true;
            }
        }

        private async void btnDryPumpStandby_Click(object sender, EventArgs e)
        {
            btnDryPumpStandby.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.SetStandby());
                LogInfo("드라이펌프 대기모드");
            }
            finally
            {
                btnDryPumpStandby.Enabled = true;
            }
        }

        private async void btnDryPumpNormal_Click(object sender, EventArgs e)
        {
            btnDryPumpNormal.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.SetNormalMode());
                LogInfo("드라이펌프 정상모드");
            }
            finally
            {
                btnDryPumpNormal.Enabled = true;
            }
        }

        private async void btnTurboPumpStart_Click(object sender, EventArgs e)
        {
            // 인터락 체크
            if (!_dryPump?.Status?.IsRunning ?? true)
            {
                MessageBox.Show("드라이펌프가 작동중이 아닙니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_dataCollectionService?.GetLatestPressure() > 1)
            {
                MessageBox.Show("챔버 압력이 너무 높습니다 (> 1 Torr)", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_bathCirculator?.Status?.IsRunning ?? true)
            {
                MessageBox.Show("칠러가 작동중이 아닙니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTurboPumpStart.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.Start());
                LogInfo("터보펌프 시작");



                

                btn_iongauge_Click(null, null);
                LogInfo("이온게이지 HV ON - 터보 펌프 시작 후 10초 후");
            }
            finally
            {
                btnTurboPumpStart.Enabled = true;
            }
        }

        private async void btnTurboPumpStop_Click(object sender, EventArgs e)
        {
            btnTurboPumpStop.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.Stop());
                LogInfo("터보펌프 정지");
            }
            finally
            {
                btnTurboPumpStop.Enabled = true;
            }
        }

        private async void btnTurboPumpVent_Click(object sender, EventArgs e)
        {
            btnTurboPumpVent.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.Vent());
                LogInfo("터보펌프 벤트");
            }
            finally
            {
                btnTurboPumpVent.Enabled = true;
            }
        }

        private async void btnTurboPumpReset_Click(object sender, EventArgs e)
        {
            btnTurboPumpReset.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.ResetError());
                LogInfo("터보펌프 리셋");
            }
            finally
            {
                btnTurboPumpReset.Enabled = true;
            }
        }

        #endregion

        #region 온도 제어 메서드

        private async void btnBathCirculatorStart_Click(object sender, EventArgs e)
        {
            btnBathCirculatorStart.Enabled = false;
            try
            {
                await Task.Run(() => _bathCirculator.Start());
                LogInfo("칠러 시작");
            }
            finally
            {
                btnBathCirculatorStart.Enabled = true;
            }
        }

        private async void btnBathCirculatorStop_Click(object sender, EventArgs e)
        {
            // 압력 체크
            if (_turboPump.IsRunning)
            {
                MessageBox.Show("터보 펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            btnBathCirculatorStop.Enabled = false;
            try
            {
                await Task.Run(() => _bathCirculator.Stop());
                LogInfo("칠러 정지");
            }
            finally
            {
                btnBathCirculatorStop.Enabled = true;
            }
        }

        private void btnBathCirculatorSetTemp_Click(object sender, EventArgs e)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                "목표 온도를 입력하세요 (℃):",
                "온도 설정",
                _bathCirculator.Status.SetTemperature.ToString("F1"));

            if (!string.IsNullOrEmpty(input) && double.TryParse(input, out double temperature))
            {
                if (_bathCirculator.SetTemperature(temperature))
                {
                    LogInfo($"칠러 온도 설정: {temperature}℃");
                }
            }
        }

        private void btnBathCirculatorSetTime_Click(object sender, EventArgs e)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            try
            {
                // 시간 설정 다이얼로그
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
                    Location = new System.Drawing.Point(20, 20),
                    Width = 150,
                    Checked = _bathCirculator.Status.SetTimeMinutes == -1
                };

                RadioButton rbEndTime = new RadioButton
                {
                    Text = "종료 시간",
                    Location = new System.Drawing.Point(20, 50),
                    Width = 150,
                    Checked = _bathCirculator.Status.SetTimeMinutes == 0
                };

                RadioButton rbSetTime = new RadioButton
                {
                    Text = "시간 설정 (분):",
                    Location = new System.Drawing.Point(20, 80),
                    Width = 120
                };

                TextBox tbMinutes = new TextBox
                {
                    Location = new System.Drawing.Point(150, 80),
                    Width = 100,
                    Text = _bathCirculator.Status.SetTimeMinutes > 0 ? _bathCirculator.Status.SetTimeMinutes.ToString() : "60",
                    Enabled = _bathCirculator.Status.SetTimeMinutes > 0
                };

                rbSetTime.CheckedChanged += (s, args) => tbMinutes.Enabled = rbSetTime.Checked;

                Button btnOk = new Button
                {
                    Text = "확인",
                    DialogResult = DialogResult.OK,
                    Location = new System.Drawing.Point(60, 120),
                    Width = 75
                };

                Button btnCancel = new Button
                {
                    Text = "취소",
                    DialogResult = DialogResult.Cancel,
                    Location = new System.Drawing.Point(150, 120),
                    Width = 75
                };

                timeSettingForm.Controls.AddRange(new Control[]
                {
                    rbNoLimit, rbEndTime, rbSetTime, tbMinutes, btnOk, btnCancel
                });

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
                            MessageBox.Show("유효한 시간(분)을 입력하세요.", "입력 오류",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }

                    bool result = _bathCirculator.SetOperationTime(minutes);
                    if (result)
                    {
                        string timeDesc = minutes == -1 ? "제한 없음" :
                                        (minutes == 0 ? "종료 시간" : $"{minutes}분");
                        LogInfo($"칠러 작동 시간 설정: {timeDesc}");
                    }
                    else
                    {
                        LogError("칠러 작동 시간 설정 실패");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"칠러 시간 설정 오류: {ex.Message}", ex);
            }
        }

        private async void btnCh1Start_Click(object sender, EventArgs e)
        {
            btnCh1Start.Enabled = false;
            try
            {
                await Task.Run(() => _tempController.Start(1));
                LogInfo("온도컨트롤러 CH1 시작");

                // 타이머가 활성화되어 있으면 타이머 시작
                if (chkCh1TimerEnabled.Checked)
                {
                    StartCh1Timer();
                }
            }
            finally
            {
                btnCh1Start.Enabled = true;
            }
        }

        private async void btnCh1Stop_Click(object sender, EventArgs e)
        {
            btnCh1Stop.Enabled = false;
            try
            {
                await Task.Run(() => _tempController.Stop(1));
                LogInfo("온도컨트롤러 CH1 정지");

                // 타이머가 실행 중이면 정지
                if (_ch1TimerActive)
                {
                    StopCh1Timer();
                }
            }
            finally
            {
                btnCh1Stop.Enabled = true;
            }
        }

        private void btnCh1SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(1);
        }

        private async void btnCh1AutoTuning_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            var result = MessageBox.Show(
                "채널 1의 오토튜닝을 시작하시겠습니까?\n" +
                "오토튜닝 중에는 온도가 불안정할 수 있습니다.",
                "오토튜닝 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                btnCh1AutoTuning.Enabled = false;
                try
                {
                    bool success = await Task.Run(() => _tempController.StartAutoTuning(1));
                    if (success)
                    {
                        LogInfo("온도컨트롤러 CH1 오토튜닝 시작");
                        MessageBox.Show("채널 1 오토튜닝이 시작되었습니다.", "알림",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        LogError("온도컨트롤러 CH1 오토튜닝 시작 실패");
                        MessageBox.Show("오토튜닝 시작에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    btnCh1AutoTuning.Enabled = true;
                }
            }
        }

        private async void btnCh2Start_Click(object sender, EventArgs e)
        {
            btnCh2Start.Enabled = false;
            try
            {
                await Task.Run(() => _tempController.Start(2));
                LogInfo("온도컨트롤러 CH2 시작");
            }
            finally
            {
                btnCh2Start.Enabled = true;
            }
        }

        private async void btnCh2Stop_Click(object sender, EventArgs e)
        {
            btnCh2Stop.Enabled = false;
            try
            {
                await Task.Run(() => _tempController.Stop(2));
                LogInfo("온도컨트롤러 CH2 정지");
            }
            finally
            {
                btnCh2Stop.Enabled = true;
            }
        }

        private void btnCh2SetTemp_Click(object sender, EventArgs e)
        {
            ShowTemperatureSetDialog(2);
        }

        private async void btnCh2AutoTuning_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            var result = MessageBox.Show(
                "채널 2의 오토튜닝을 시작하시겠습니까?\n" +
                "오토튜닝 중에는 온도가 불안정할 수 있습니다.",
                "오토튜닝 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                btnCh2AutoTuning.Enabled = false;
                try
                {
                    bool success = await Task.Run(() => _tempController.StartAutoTuning(2));
                    if (success)
                    {
                        LogInfo("온도컨트롤러 CH2 오토튜닝 시작");
                        MessageBox.Show("채널 2 오토튜닝이 시작되었습니다.", "알림",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        LogError("온도컨트롤러 CH2 오토튜닝 시작 실패");
                        MessageBox.Show("오토튜닝 시작에 실패했습니다.", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                finally
                {
                    btnCh2AutoTuning.Enabled = true;
                }
            }
        }

        private void ShowTemperatureSetDialog(int channel)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            var channelStatus = _tempController.Status.ChannelStatus[channel - 1];
            string currentValue = channelStatus.Dot == 0 ?
                channelStatus.SetValue.ToString() :
                (channelStatus.SetValue / 10.0).ToString("F1");

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"채널 {channel} 목표 온도를 입력하세요 ({channelStatus.TemperatureUnit}):",
                "온도 설정",
                currentValue);

            if (!string.IsNullOrEmpty(input))
            {
                short setValue;
                if (channelStatus.Dot == 0)
                {
                    if (short.TryParse(input, out setValue))
                    {
                        _tempController.SetTemperature(channel, setValue);
                        LogInfo($"CH{channel} 온도 설정: {setValue}{channelStatus.TemperatureUnit}");
                    }
                }
                else
                {
                    if (double.TryParse(input, out double doubleValue))
                    {
                        setValue = (short)(doubleValue * 10);
                        _tempController.SetTemperature(channel, setValue);
                        LogInfo($"CH{channel} 온도 설정: {doubleValue:F1}{channelStatus.TemperatureUnit}");
                    }
                }
            }
        }

        #endregion

        #region 로깅 관련

        private void LogInfo(string message)
        {
            AsyncLoggingService.Instance.LogInfo(message);
        }

        private void LogError(string message, Exception ex = null)
        {
            AsyncLoggingService.Instance.LogError(message, ex);
        }

        private void LogWarning(string message)
        {
            AsyncLoggingService.Instance.LogWarning(message);
        }

        private void LogDebug(string message)
        {
            AsyncLoggingService.Instance.LogDebug(message);
        }

        private void OnLogAdded(object sender, string logMessage)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, string>(OnLogAdded), sender, logMessage);
                return;
            }

            try
            {
                if (txtLog != null && !txtLog.IsDisposed)
                {
                    // 로그 창 크기 제한
                    if (txtLog.Lines.Length > 1000)
                    {
                        var lines = txtLog.Lines.Skip(500).ToArray();
                        txtLog.Lines = lines;
                    }

                    txtLog.AppendText(logMessage + Environment.NewLine);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
            catch { }
        }

        private void InitializeDataLogging()
        {
            // 압력 및 밸브 상태 데이터 - 이름 통일 및 밸브 상태 추가
            DataLoggerService.Instance.StartLogging("Pressure", new List<string>
    {
        "ATM(kPa)",
        "Pirani(Torr)",
        "Ion(Torr)",
        "IonStatus",
        "GateValve",
        "VentValve",
        "ExhaustValve",
        "IonGaugeHV"
    });

            // 드라이펌프
            DataLoggerService.Instance.StartLogging("DryPump", new List<string>
    {
        "Status", "Frequency(Hz)", "Current(A)", "Temperature(°C)", "HasWarning", "HasFault"
    });

            // 터보펌프
            DataLoggerService.Instance.StartLogging("TurboPump", new List<string>
    {
        "Status", "Speed(RPM)", "Current(A)", "Temperature(°C)", "HasWarning", "HasError"
    });

            // 칠러
            DataLoggerService.Instance.StartLogging("BathCirculator", new List<string>
    {
        "Status", "CurrentTemp(°C)", "TargetTemp(°C)", "Mode", "Time", "HasError", "HasWarning"
    });

            // 온도컨트롤러
            DataLoggerService.Instance.StartLogging("TempController", new List<string>
    {
        "Ch1_PV(°C)", "Ch1_SV(°C)", "Ch1_HeatingMV(%)", "Ch1_Status",
        "Ch2_PV(°C)", "Ch2_SV(°C)", "Ch2_HeatingMV(%)", "Ch2_Status"
    });
        }

        private void AddLoggingMenu()
        {
            if (menuStrip == null) return;

            var menuLogging = new ToolStripMenuItem("로깅(&L)");

            _menuStartLogging = new ToolStripMenuItem("로깅 시작");
            _menuStartLogging.Click += (s, e) => ToggleLogging(true);

            _menuStopLogging = new ToolStripMenuItem("로깅 중지");
            _menuStopLogging.Click += (s, e) => ToggleLogging(false);

            var menuOpenLogFolder = new ToolStripMenuItem("로그 폴더 열기");
            menuOpenLogFolder.Click += (s, e) => OpenFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));

            var menuOpenDataFolder = new ToolStripMenuItem("데이터 폴더 열기");
            menuOpenDataFolder.Click += (s, e) => OpenFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));

            menuLogging.DropDownItems.AddRange(new ToolStripItem[]
            {
                _menuStartLogging, _menuStopLogging, new ToolStripSeparator(),
                menuOpenLogFolder, menuOpenDataFolder
            });

            menuStrip.Items.Add(menuLogging);
            UpdateLoggingMenuState();
        }

        private void ToggleLogging(bool enable)
        {
            _isLoggingEnabled = enable;
            LogInfo($"데이터 로깅이 {(enable ? "활성화" : "비활성화")}되었습니다.");
            UpdateLoggingMenuState();
        }

        private void UpdateLoggingMenuState()
        {
            if (_menuStartLogging != null && _menuStopLogging != null)
            {
                _menuStartLogging.Enabled = !_isLoggingEnabled;
                _menuStopLogging.Enabled = _isLoggingEnabled;
            }
        }

        private void OpenFolder(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                System.Diagnostics.Process.Start("explorer.exe", path);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"폴더를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 종료 처리

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogInfo("시스템 종료 시작");

            // 로딩 화면 표시
            using (var loadingForm = new LoadingForm())
            {
                loadingForm.Show();
                Application.DoEvents();

                // 단계별 초기화
                loadingForm.UpdateStatus("서비스 종료 중...");
                _dataCollectionService?.Stop();
                _uiUpdateService?.Stop();

                loadingForm.UpdateStatus("연결 종료 중...");
                // 장치 연결 해제
                foreach (var device in _deviceList)
                {
                    try
                    {
                        if (device.IsConnected)
                            device.Disconnect();
                    }
                    catch { }
                };

                loadingForm.UpdateStatus("로깅 종료 중...");
                DataLoggerService.Instance.StopAllLogging();
                AsyncLoggingService.Instance.Stop(); ;

                loadingForm.UpdateStatus("리소스 정리 중...");
                // 리소스 정리
                _dataCollectionService?.Dispose();
                _uiUpdateService?.Dispose(); ;

                loadingForm.UpdateStatus("시스템 종료");
                Task.Delay(200);
            }
                // 타이머 정지 및 해제
                if (_ch1AutoStopTimer != null)
                {
                    _ch1AutoStopTimer.Stop();
                    _ch1AutoStopTimer.Dispose();
                }



            LogInfo("시스템 종료 완료");

        }

        #endregion

        #region 기타 이벤트 핸들러

        // 메뉴 이벤트
        private void menuFileExit_Click(object sender, EventArgs e) => Close();

        private void MenuCommSettings_Click(object sender, EventArgs e)
        {
            MessageBox.Show("통신 설정은 현재 메인 화면에서 직접 변경 가능합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MenuHelpAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show("VacX OutSense System Controller\n버전 1.0.0\n\n© 2024 VacX Inc.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion

        #region 타이머 이벤트 핸들러

        /// <summary>
        /// CH1 타이머 활성화 체크박스 변경 이벤트
        /// </summary>
        private void chkCh1TimerEnabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCh1TimerControls();
        }

        /// <summary>
        /// CH1 타이머 컨트롤 상태 업데이트
        /// </summary>
        private void UpdateCh1TimerControls()
        {
            bool enabled = chkCh1TimerEnabled.Checked;
            bool timerRunning = _ch1TimerActive || _ch1WaitingForTargetTemp;

            numCh1Hours.Enabled = enabled && !timerRunning;
            numCh1Minutes.Enabled = enabled && !timerRunning;
            numCh1Seconds.Enabled = enabled && !timerRunning;

            if (!enabled && timerRunning)
            {
                StopCh1Timer();
            }
        }

        /// <summary>
        /// CH1 자동 정지 타이머 틱 이벤트
        /// </summary>
        private void Ch1AutoStopTimer_Tick(object sender, EventArgs e)
        {
            if (!_ch1TimerActive && !_ch1WaitingForTargetTemp)
            {
                _ch1AutoStopTimer.Stop();
                return;
            }

            // 목표 온도 도달 대기 중
            if (_ch1WaitingForTargetTemp)
            {
                // PV와 SV 값 확인
                if (_tempController?.Status?.ChannelStatus != null && _tempController.Status.ChannelStatus.Length > 0)
                {
                    var ch1Status = _tempController.Status.ChannelStatus[0];
                    double pv = ch1Status.PresentValue;
                    double sv = ch1Status.SetValue;

                    // 소수점 처리
                    if (ch1Status.Dot > 0)
                    {
                        pv = pv / Math.Pow(10, ch1Status.Dot);
                        sv = sv / Math.Pow(10, ch1Status.Dot);
                    }

                    // 목표 온도 도달 확인 (허용 오차 내)
                    if (Math.Abs(pv - sv) <= _ch1TargetTolerance)
                    {
                        // 목표 온도 도달 - 실제 타이머 시작
                        _ch1WaitingForTargetTemp = false;
                        _ch1TimerActive = true;
                        _ch1StartTime = DateTime.Now;

                        lblCh1TimeRemainingValue.ForeColor = Color.Blue;
                        LogInfo($"CH1 목표 온도 도달 (PV: {pv:F1}°C, SV: {sv:F1}°C) - 타이머 시작");
                    }
                    else
                    {
                        // 대기 중 표시
                        lblCh1TimeRemainingValue.Text = $"대기중 ({pv:F1}/{sv:F1}°C)";
                        lblCh1TimeRemainingValue.ForeColor = Color.Orange;
                    }
                }
                return;
            }

            // 타이머 실행 중
            if (_ch1TimerActive)
            {
                // 남은 시간 계산
                TimeSpan elapsed = DateTime.Now - _ch1StartTime;
                TimeSpan remaining = _ch1Duration - elapsed;

                if (remaining.TotalSeconds <= 0)
                {
                    // 시간이 만료되면 CH1 정지 및 종료 시퀀스 실행
                    StopCh1WithTimer();
                    lblCh1TimeRemainingValue.Text = "00:00:00";
                    lblCh1TimeRemainingValue.ForeColor = Color.Red;
                }
                else
                {
                    // 남은 시간 표시 업데이트
                    lblCh1TimeRemainingValue.Text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

                    // 남은 시간에 따라 색상 변경
                    if (remaining.TotalSeconds <= 60)
                    {
                        lblCh1TimeRemainingValue.ForeColor = Color.Red;
                    }
                    else if (remaining.TotalMinutes <= 5)
                    {
                        lblCh1TimeRemainingValue.ForeColor = Color.Orange;
                    }
                    else
                    {
                        lblCh1TimeRemainingValue.ForeColor = Color.Blue;
                    }
                }
            }
        }

        /// <summary>
        /// CH1 타이머 시작 (목표 온도 도달 대기)
        /// </summary>
        private void StartCh1Timer()
        {
            if (!chkCh1TimerEnabled.Checked)
                return;

            // 설정된 시간 가져오기
            int hours = (int)numCh1Hours.Value;
            int minutes = (int)numCh1Minutes.Value;
            int seconds = (int)numCh1Seconds.Value;

            // 시간이 0이면 시작하지 않음
            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show("타이머 시간을 설정해주세요.", "알림",
                              MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _ch1Duration = new TimeSpan(hours, minutes, seconds);
            _ch1WaitingForTargetTemp = true;  // 목표 온도 도달 대기 상태로 설정
            _ch1TimerActive = false;

            // 타이머 시작 (온도 체크용)
            _ch1AutoStopTimer.Start();

            // UI 업데이트
            UpdateCh1TimerControls();
            lblCh1TimeRemainingValue.Text = "대기중...";
            lblCh1TimeRemainingValue.ForeColor = Color.Orange;

            LogInfo($"CH1 타이머 설정: {hours}시간 {minutes}분 {seconds}초 (목표 온도 도달 후 시작)");
        }


        /// <summary>
        /// CH1 타이머 정지
        /// </summary>
        private void StopCh1Timer()
        {
            _ch1TimerActive = false;
            _ch1WaitingForTargetTemp = false;
            _ch1AutoStopTimer.Stop();
            lblCh1TimeRemainingValue.Text = "00:00:00";
            lblCh1TimeRemainingValue.ForeColor = Color.Blue;
            UpdateCh1TimerControls();

            LogInfo("CH1 타이머 정지");
        }

        /// <summary>
        /// 타이머에 의한 CH1 정지 및 종료 시퀀스
        /// </summary>
        private async void StopCh1WithTimer()
        {
            try
            {
                // 타이머 정지
                StopCh1Timer();

                LogInfo("CH1 타이머 만료 - 종료 시퀀스 시작");

                // 1. CH1 정지
                if (_tempController?.IsConnected == true)
                {
                    btnCh1Stop.Enabled = false;
                    await Task.Run(() => _tempController.Stop(1));
                    LogInfo("CH1 자동 정지됨");
                    btnCh1Stop.Enabled = true;
                }

                // 2. 이온게이지 HV OFF
                if (_ioModule?.IsConnected == true && btn_iongauge.Text == "HV on")
                {
                    LogInfo("이온게이지 HV OFF 시작");
                    btn_iongauge.Enabled = false;
                    try
                    {
                        bool result = await _ioModule.ControlIonGaugeHVAsync(false);
                        if (result)
                        {
                            LogInfo("이온게이지 HV OFF 완료");
                        }
                        else
                        {
                            LogWarning("이온게이지 HV OFF 실패");
                        }
                    }
                    finally
                    {
                        btn_iongauge.Enabled = true;
                    }
                }

                // 3. 터보펌프 정지
                if (_turboPump?.IsConnected == true && _turboPump.Status?.IsRunning == true)
                {
                    LogInfo("터보펌프 정지 시작");
                    btnTurboPumpStop.Enabled = false;
                    try
                    {
                        await Task.Run(() => _turboPump.Stop());
                        LogInfo("터보펌프 정지 명령 전송 완료");
                    }
                    finally
                    {
                        btnTurboPumpStop.Enabled = true;
                    }
                }

                LogInfo("CH1 타이머 종료 시퀀스 완료");

                // 사용자에게 알림
                MessageBox.Show(
                    "CH1 타이머가 만료되어 다음 작업이 수행되었습니다:\n" +
                    "1. CH1 온도 제어 정지\n" +
                    "2. 이온게이지 HV OFF\n" +
                    "3. 터보펌프 정지",
                    "타이머 종료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"CH1 타이머 종료 시퀀스 오류: {ex.Message}", ex);
                MessageBox.Show(
                    $"종료 시퀀스 중 오류가 발생했습니다:\n{ex.Message}",
                    "오류",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }


        #endregion



        #region UI 성능 최적화

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED - 더블 버퍼링
                return cp;
            }
        }

        #endregion
    }
}