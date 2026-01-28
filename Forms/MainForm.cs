using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using VacX_OutSense.Core.AutoRun;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.DryPump;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.IO_Module;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Devices.TurboPump;
using VacX_OutSense.Forms;
using VacX_OutSense.Models;
using VacX_OutSense.Utils;

namespace VacX_OutSense
{
    public partial class MainForm : Form
    {
        #region 필드 및 속성

        #region AutoRun 관련 필드
        private AutoRunService _autoRunService;
        private AutoRunConfiguration _autoRunConfig;
        private System.Windows.Forms.Timer _autoRunTimer;
        private int _autoRunElapsedSeconds = 0;

        private GroupBox groupBoxAutoRun;
        private Button btnAutoRunStart;
        private Button btnAutoRunStop;
        private Button btnAutoRunPause;
        private Button btnAutoRunResume;
        private Button btnAutoRunConfig;
        private Label lblAutoRunStatus;
        private Label lblAutoRunStep;
        private Label lblAutoRunProgress;
        private ProgressBar progressBarAutoRun;
        private ListView listViewAutoRunLog;
        private Label lblAutoRunElapsedTime;
        private Label lblAutoRunRemainingTime;
        #endregion

        #region 베이크 아웃 관련 필드
        private ThermalRampProfileManager _profileManager;
        private BakeoutSettings _bakeoutSettings;
        #endregion

        #region 타이머 관련 필드
        private System.Windows.Forms.Timer _ch1AutoStopTimer;
        private bool _ch1TimerActive = false;
        private bool _ch1WaitingForTargetTemp = false;
        private DateTime _ch1StartTime;
        private TimeSpan _ch1Duration;
        private double _ch1TargetTolerance = 1.0;
        private double _ch1VentTargetTemp = 50.0;

        // 자동 시작 관련 필드
        private bool _ch1AutoStartEnabled = false;
        private bool _ch1WaitingForVacuum = false;
        private double _ch1TargetPressure = 1E-5;  // 기본값: 1E-5 Torr
        private int _ch1PressureReachCount = 0;     // 현재 도달 횟수
        private int _ch1RequiredReachCount = 3;     // 필요 도달 횟수 (기본값: 3회)
        #endregion

        #region 서비스 및 타이머
        private System.Windows.Forms.Timer _connectionCheckTimer;
        private Dictionary<string, bool> _previousConnectionStates = new Dictionary<string, bool>();
        private OptimizedDataCollectionService _dataCollectionService;
        private SimplifiedUIUpdateService _uiUpdateService;
        private ChillerPIDControlService _chillerPIDService;
        #endregion

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

        private void InitializeTimers()
        {
            _ch1AutoStopTimer = new System.Windows.Forms.Timer();
            _ch1AutoStopTimer.Interval = 1000;
            _ch1AutoStopTimer.Tick += Ch1AutoStopTimer_Tick;
            UpdateCh1TimerControls();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                this.Enabled = false;

                using (var loadingForm = new LoadingForm())
                {
                    loadingForm.Show();
                    Application.DoEvents();

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

                    loadingForm.UpdateStatus("AutoRun 초기화 중...");
                    InitializeAutoRun();

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
                this.Enabled = true;
                this.Focus();
            }
        }

        private async Task InitializeLoggingAsync()
        {
            await Task.Run(() =>
            {
                AsyncLoggingService.Instance.Start();
                AsyncLoggingService.Instance.LogAdded += OnLogAdded;
                LoggerService.Instance = new LoggerServiceWrapper(AsyncLoggingService.Instance);
                InitializeDataLogging();
                AddLoggingMenu();
            });
        }

        private async Task InitializeCommunicationAsync()
        {
            await Task.Run(() =>
            {
                _multiPortManager = MultiPortSerialManager.Instance;
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
                _ioModule = new IO_Module(_deviceAdapters["COM4"], 1);
                _dryPump = new DryPump(_deviceAdapters["COM3"], "ECODRY 25 plus", 1);
                _turboPump = new TurboPump(_deviceAdapters["COM1"], "MAG W 1300", 1);
                _bathCirculator = new BathCirculator(_deviceAdapters["COM5"], "LK-1000", 1);

                _tempController = new TempController(
                    _deviceAdapters["COM6"],
                    deviceAddress: 1,
                    numChannels: 2,
                    expansionSlaveAddress: 2,
                    expansionChannels: 3
                );

                _atmSwitch = new ATMswitch();
                _piraniGauge = new PiraniGauge();
                _ionGauge = new IonGauge();

                _deviceList.AddRange(new IDevice[] { _ioModule, _dryPump, _turboPump, _bathCirculator, _tempController });
            });

            SetupDataBindings();
        }

        private async Task ConnectDevicesAsync()
        {
            var tasks = new List<Task<bool>>
            {
                Task.Run(() => _ioModule.Connect("COM4", _defaultSettings)),
                Task.Run(() => _dryPump.Connect("COM3", _dryPumpDefaultSettings)),
                Task.Run(() => _turboPump.Connect("COM1", _turboPumpDefaultSettings)),
                Task.Run(() => _bathCirculator.Connect("COM5", _bathCirculatorDefaultSettings)),
                Task.Run(() => _tempController.Connect("COM6", _tempControllerDefaultSettings))
            };

            var results = await Task.WhenAll(tasks);

            for (int i = 0; i < _deviceList.Count; i++)
            {
                var device = _deviceList[i];
                var connected = results[i];
                LogInfo($"{device.DeviceName} 연결 {(connected ? "성공" : "실패")}");
                if (!connected)
                    MessageBox.Show($"{device.DeviceName} 연결 실패.");
            }

            if (_tempController.HasExpansion)
                LogInfo($"TM4-N2SE 확장 모듈 통합됨 (총 {_tempController.TotalChannelCount}채널)");
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
                    LogInfo($"{device.DeviceName} 연결됨");
            }
        }

        private void StartServices()
        {
            _uiUpdateService = new SimplifiedUIUpdateService(this);
            _uiUpdateService.Start();

            _dataCollectionService = new OptimizedDataCollectionService(this);
            _dataCollectionService.DataUpdated += OnDataUpdated;
            _dataCollectionService.Start();

            _chillerPIDService = new ChillerPIDControlService(this);

            InitializeConnectionChecker();

            // ★ 추가: 베이크 아웃 설정 초기화
            InitializeBakeoutSettings();

            // ★ 추가: SimpleRampControl 초기화
            InitializeSimpleRampControl();


            LogInfo("서비스 시작 완료");
        }

        /// <summary>
        /// 베이크 아웃 설정 초기화
        /// </summary>
        private void InitializeBakeoutSettings()
        {
            _profileManager = new ThermalRampProfileManager();
            _bakeoutSettings = BakeoutSettings.Load();
            LogInfo("베이크 아웃 설정 로드 완료");
        }

        /// <summary>
        /// SimpleRampControl 초기화 및 이벤트 연결
        /// </summary>
        private void InitializeSimpleRampControl()
        {
            if (_tempController != null && simpleRampControl1 != null)
            {
                // 컨트롤 초기화
                simpleRampControl1.Initialize(_tempController);

                // 목표 온도 도달 시 타이머 시작 연동
                simpleRampControl1.TargetTemperatureReached += SimpleRampControl_TargetReached;

                // 로그 연동
                simpleRampControl1.LogMessage += (s, msg) => LogInfo(msg);

                // ★ 추가: 램프 완료/정지 시 컨트롤 상태 업데이트
                if (simpleRampControl1.RampController != null)
                {
                    simpleRampControl1.RampController.RampCompleted += (s, e) =>
                    {
                        RunOnUIThread(() => UpdateCh1TimerControls());
                    };
                }

                LogInfo("SimpleRampControl 초기화 완료");
            }
        }
        /// <summary>
        /// 온도 램프 목표 도달 시 CH1 타이머 시작
        /// </summary>
        private void SimpleRampControl_TargetReached(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SimpleRampControl_TargetReached(sender, e)));
                return;
            }

            // 자동 타이머 시작 옵션 확인
            if (!simpleRampControl1.AutoStartTimerOnTargetReached) return;

            // 타이머 활성화되어 있으면 시작
            if (chkCh1TimerEnabled.Checked)
            {
                // ★ 타이머 직접 시작 (StartCh1Timer 호출하면 _ch1WaitingForTargetTemp = true가 됨)
                _ch1TimerActive = true;
                _ch1StartTime = DateTime.Now;

                int hours = (int)numCh1Hours.Value;
                int minutes = (int)numCh1Minutes.Value;
                int seconds = (int)numCh1Seconds.Value;
                _ch1Duration = new TimeSpan(hours, minutes, seconds);

                if (!_ch1AutoStopTimer.Enabled)
                    _ch1AutoStopTimer.Start();

                UpdateCh1TimerControls();
                lblCh1TimeRemainingValue.ForeColor = Color.Blue;
                LogInfo("[온도 램프] CH2 목표 온도 도달 - 타이머 시작");
            }
        }

        private void InitializeConnectionChecker()
        {
            _connectionCheckTimer = new System.Windows.Forms.Timer();
            _connectionCheckTimer.Interval = 5000;
            _connectionCheckTimer.Tick += ConnectionCheckTimer_Tick;
            _connectionCheckTimer.Start();
        }

        private void ConnectionCheckTimer_Tick(object sender, EventArgs e) => CheckDeviceConnections();

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
                        LogWarning($"{deviceName} 연결 끊김 감지");
                        StopDeviceDataLogging(deviceName);
                    }
                    else if (!previousState && currentState)
                        LogInfo($"{deviceName} 연결 복구됨");
                }
                _previousConnectionStates[deviceName] = currentState;
            }
        }

        #endregion

        #region AutoRun (축약)

        private void InitializeAutoRun()
        {
            try
            {
                _autoRunConfig = LoadAutoRunConfiguration() ?? new AutoRunConfiguration();
                _autoRunService = new AutoRunService(this, _autoRunConfig);
                _autoRunService.StateChanged += OnAutoRunStateChanged;
                _autoRunService.ProgressUpdated += OnAutoRunProgressUpdated;
                _autoRunService.ErrorOccurred += OnAutoRunErrorOccurred;
                _autoRunService.Completed += OnAutoRunCompleted;

                _autoRunTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _autoRunTimer.Tick += AutoRunTimer_Tick;

                InitializeAutoRunUI();
                UpdateAutoRunUI();
                LogInfo("AutoRun 기능 초기화 완료");
            }
            catch (Exception ex)
            {
                LogError($"AutoRun 초기화 실패: {ex.Message}", ex);
            }
        }

        private void InitializeAutoRunUI()
        {
            Panel panelAutoRun = new Panel { Dock = DockStyle.Fill };
            tabPageAutoRun.Controls.Add(panelAutoRun);

            GroupBox groupBoxStatus = new GroupBox { Text = "AutoRun 상태", Location = new Point(10, 10), Size = new Size(760, 200) };
            panelAutoRun.Controls.Add(groupBoxStatus);

            lblAutoRunStatus = new Label { Location = new Point(10, 25), Size = new Size(200, 20), Text = "상태: 대기 중", Font = new Font("맑은 고딕", 10F, FontStyle.Bold) };
            lblAutoRunStep = new Label { Location = new Point(10, 50), Size = new Size(400, 20), Text = "단계: -" };
            lblAutoRunProgress = new Label { Location = new Point(10, 75), Size = new Size(200, 20), Text = "진행률: 0%" };
            progressBarAutoRun = new ProgressBar { Location = new Point(10, 100), Size = new Size(500, 25), Style = ProgressBarStyle.Continuous };
            lblAutoRunElapsedTime = new Label { Location = new Point(10, 135), Size = new Size(200, 20), Text = "경과 시간: 00:00:00" };
            lblAutoRunRemainingTime = new Label { Location = new Point(220, 135), Size = new Size(200, 20), Text = "남은 시간: --:--:--" };
            groupBoxStatus.Controls.AddRange(new Control[] { lblAutoRunStatus, lblAutoRunStep, lblAutoRunProgress, progressBarAutoRun, lblAutoRunElapsedTime, lblAutoRunRemainingTime });

            GroupBox groupBoxControl = new GroupBox { Text = "제어", Location = new Point(520, 25), Size = new Size(230, 160) };
            btnAutoRunStart = new Button { Location = new Point(10, 25), Size = new Size(100, 30), Text = "시작" };
            btnAutoRunStart.Click += BtnAutoRunStart_Click;
            btnAutoRunStop = new Button { Location = new Point(120, 25), Size = new Size(100, 30), Text = "중지", Enabled = false };
            btnAutoRunStop.Click += BtnAutoRunStop_Click;
            btnAutoRunPause = new Button { Location = new Point(10, 65), Size = new Size(100, 30), Text = "일시정지", Enabled = false };
            btnAutoRunPause.Click += BtnAutoRunPause_Click;
            btnAutoRunResume = new Button { Location = new Point(120, 65), Size = new Size(100, 30), Text = "재개", Enabled = false };
            btnAutoRunResume.Click += BtnAutoRunResume_Click;
            btnAutoRunConfig = new Button { Location = new Point(65, 105), Size = new Size(100, 30), Text = "설정" };
            btnAutoRunConfig.Click += BtnAutoRunConfig_Click;
            groupBoxControl.Controls.AddRange(new Control[] { btnAutoRunStart, btnAutoRunStop, btnAutoRunPause, btnAutoRunResume, btnAutoRunConfig });
            groupBoxStatus.Controls.Add(groupBoxControl);

            GroupBox groupBoxLog = new GroupBox { Text = "실행 로그", Location = new Point(10, 220), Size = new Size(760, 320) };
            listViewAutoRunLog = new ListView { Location = new Point(10, 25), Size = new Size(740, 285), View = View.Details, FullRowSelect = true, GridLines = true };
            listViewAutoRunLog.Columns.Add("시간", 120);
            listViewAutoRunLog.Columns.Add("상태", 100);
            listViewAutoRunLog.Columns.Add("메시지", 500);
            groupBoxLog.Controls.Add(listViewAutoRunLog);
            panelAutoRun.Controls.Add(groupBoxLog);
        }

        private async void BtnAutoRunStart_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("AutoRun을 시작하시겠습니까?", "AutoRun 시작 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            btnAutoRunStart.Enabled = false; btnAutoRunStop.Enabled = true; btnAutoRunPause.Enabled = true; btnAutoRunConfig.Enabled = false;
            listViewAutoRunLog.Items.Clear(); _autoRunElapsedSeconds = 0;
            _autoRunTimer.Start();
            if (!await _autoRunService.StartAsync()) { MessageBox.Show("AutoRun 시작 실패", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); UpdateAutoRunUI(); }
        }

        private void BtnAutoRunStop_Click(object sender, EventArgs e) { if (MessageBox.Show("AutoRun을 중지하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) { _autoRunService.Stop(); _autoRunTimer.Stop(); UpdateAutoRunUI(); } }
        private void BtnAutoRunPause_Click(object sender, EventArgs e) { _autoRunService.Pause(); UpdateAutoRunUI(); }
        private void BtnAutoRunResume_Click(object sender, EventArgs e) { _autoRunService.Resume(); UpdateAutoRunUI(); }

        private void BtnAutoRunConfig_Click(object sender, EventArgs e)
        {
            using (var configForm = new AutoRunConfigForm(_autoRunConfig))
            {
                if (configForm.ShowDialog() == DialogResult.OK)
                {
                    _autoRunConfig = configForm.Configuration;
                    SaveAutoRunConfiguration(_autoRunConfig);
                    _autoRunService.Dispose();
                    _autoRunService = new AutoRunService(this, _autoRunConfig);
                    _autoRunService.StateChanged += OnAutoRunStateChanged;
                    _autoRunService.ProgressUpdated += OnAutoRunProgressUpdated;
                    _autoRunService.ErrorOccurred += OnAutoRunErrorOccurred;
                    _autoRunService.Completed += OnAutoRunCompleted;
                }
            }
        }

        private void AutoRunTimer_Tick(object sender, EventArgs e)
        {
            if (_autoRunService.IsRunning)
            {
                _autoRunElapsedSeconds++;
                lblAutoRunElapsedTime.Text = $"경과 시간: {TimeSpan.FromSeconds(_autoRunElapsedSeconds):hh\\:mm\\:ss}";
                if (_autoRunService.CurrentState == AutoRunState.RunningExperiment)
                {
                    var remaining = TimeSpan.FromSeconds(Math.Max(0, _autoRunConfig.ExperimentDurationHours * 3600 - _autoRunElapsedSeconds));
                    lblAutoRunRemainingTime.Text = $"남은 시간: {remaining:hh\\:mm\\:ss}";
                }
            }
        }

        private void OnAutoRunStateChanged(object sender, AutoRunStateChangedEventArgs e) { if (InvokeRequired) { Invoke(new EventHandler<AutoRunStateChangedEventArgs>(OnAutoRunStateChanged), sender, e); return; } lblAutoRunStatus.Text = $"상태: {GetAutoRunStateText(e.CurrentState)}"; AddAutoRunLog(e.CurrentState.ToString(), e.Message ?? GetAutoRunStateText(e.CurrentState)); UpdateAutoRunUI(); }
        private void OnAutoRunProgressUpdated(object sender, AutoRunProgressEventArgs e) { if (InvokeRequired) { Invoke(new EventHandler<AutoRunProgressEventArgs>(OnAutoRunProgressUpdated), sender, e); return; } progressBarAutoRun.Value = (int)Math.Min(100, e.OverallProgress); lblAutoRunProgress.Text = $"진행률: {e.OverallProgress:F1}%"; lblAutoRunStep.Text = $"단계: {e.Message}"; }
        private void OnAutoRunErrorOccurred(object sender, AutoRunErrorEventArgs e) { if (InvokeRequired) { Invoke(new EventHandler<AutoRunErrorEventArgs>(OnAutoRunErrorOccurred), sender, e); return; } AddAutoRunLog("ERROR", e.ErrorMessage, Color.Red); MessageBox.Show($"AutoRun 오류:\n{e.ErrorMessage}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        private void OnAutoRunCompleted(object sender, AutoRunCompletedEventArgs e) { if (InvokeRequired) { Invoke(new EventHandler<AutoRunCompletedEventArgs>(OnAutoRunCompleted), sender, e); return; } _autoRunTimer.Stop(); AddAutoRunLog("COMPLETE", e.IsSuccess ? "정상 완료" : "중단됨", e.IsSuccess ? Color.Green : Color.Orange); if (!string.IsNullOrEmpty(e.Summary)) MessageBox.Show(e.Summary, "완료", MessageBoxButtons.OK, e.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Warning); UpdateAutoRunUI(); }

        private void UpdateAutoRunUI()
        {
            bool isRunning = _autoRunService?.IsRunning ?? false;
            bool isPaused = _autoRunService?.IsPaused ?? false;
            btnAutoRunStart.Enabled = !isRunning; btnAutoRunStop.Enabled = isRunning; btnAutoRunPause.Enabled = isRunning && !isPaused; btnAutoRunResume.Enabled = isRunning && isPaused; btnAutoRunConfig.Enabled = !isRunning;
            if (!isRunning) { progressBarAutoRun.Value = 0; lblAutoRunProgress.Text = "진행률: 0%"; lblAutoRunStep.Text = "단계: -"; lblAutoRunRemainingTime.Text = "남은 시간: --:--:--"; }
        }

        private void AddAutoRunLog(string state, string message, Color? color = null) { var item = new ListViewItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")); item.SubItems.Add(state); item.SubItems.Add(message); if (color.HasValue) item.ForeColor = color.Value; listViewAutoRunLog.Items.Insert(0, item); while (listViewAutoRunLog.Items.Count > 100) listViewAutoRunLog.Items.RemoveAt(listViewAutoRunLog.Items.Count - 1); }
        private string GetAutoRunStateText(AutoRunState state) => state switch { AutoRunState.Idle => "대기 중", AutoRunState.Initializing => "초기화", AutoRunState.PreparingVacuum => "진공 준비", AutoRunState.StartingDryPump => "드라이펌프 시작", AutoRunState.StartingTurboPump => "터보펌프 시작", AutoRunState.ActivatingIonGauge => "이온게이지 활성화", AutoRunState.WaitingHighVacuum => "고진공 대기", AutoRunState.StartingHeater => "히터 시작", AutoRunState.RunningExperiment => "실험 진행", AutoRunState.ShuttingDown => "종료 중", AutoRunState.Completed => "완료", AutoRunState.Aborted => "중단됨", AutoRunState.Error => "오류", AutoRunState.Paused => "일시정지", _ => state.ToString() };

        private AutoRunConfiguration LoadAutoRunConfiguration() { try { string configPath = Path.Combine(Application.StartupPath, "Config", "AutoRunConfig.xml"); if (File.Exists(configPath)) { using (var reader = new StreamReader(configPath)) { return (AutoRunConfiguration)new XmlSerializer(typeof(AutoRunConfiguration)).Deserialize(reader); } } } catch (Exception ex) { LogError($"AutoRun 설정 로드 실패: {ex.Message}", ex); } return null; }
        private void SaveAutoRunConfiguration(AutoRunConfiguration config) { try { string configDir = Path.Combine(Application.StartupPath, "Config"); if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir); using (var writer = new StreamWriter(Path.Combine(configDir, "AutoRunConfig.xml"))) { new XmlSerializer(typeof(AutoRunConfiguration)).Serialize(writer, config); } } catch (Exception ex) { LogError($"AutoRun 설정 저장 실패: {ex.Message}", ex); } }

        #endregion

        #region 데이터 수집 이벤트

        private void OnDataUpdated(object sender, UIDataSnapshot snapshot)
        {
            _uiUpdateService.RequestUpdate(snapshot);

            // ★ 추가: Thermal Ramp 설정값 적용 (통신 충돌 방지)
            ApplyThermalRampPendingSetpoint();

            if (_isLoggingEnabled)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (snapshot.Connections.IOModule)
                            DataLoggerService.Instance.LogDataAsync("Pressure", new List<string> { snapshot.AtmPressure.ToString("F2"), snapshot.PiraniPressure.ToString("E2"), snapshot.IonPressure.ToString("E2"), snapshot.IonGaugeStatus, snapshot.GateValveStatus, snapshot.VentValveStatus, snapshot.ExhaustValveStatus, snapshot.IonGaugeHVStatus });

                        if (snapshot.Connections.DryPump && _dryPump?.Status != null)
                            DataLoggerService.Instance.LogDataAsync("DryPump", new List<string> { snapshot.DryPump.Status, _dryPump.Status.MotorFrequency.ToString("F1"), _dryPump.Status.MotorCurrent.ToString("F2"), _dryPump.Status.MotorTemperature.ToString("F1"), snapshot.DryPump.HasWarning.ToString(), snapshot.DryPump.HasError.ToString() });

                        if (snapshot.Connections.TurboPump && _turboPump?.Status != null)
                            DataLoggerService.Instance.LogDataAsync("TurboPump", new List<string> { snapshot.TurboPump.Status, _turboPump.Status.CurrentSpeed.ToString(), _turboPump.Status.MotorCurrent.ToString("F2"), _turboPump.Status.MotorTemperature.ToString(), snapshot.TurboPump.HasWarning.ToString(), snapshot.TurboPump.HasError.ToString() });

                        if (snapshot.Connections.BathCirculator && _bathCirculator?.Status != null)
                            DataLoggerService.Instance.LogDataAsync("BathCirculator", new List<string> { snapshot.BathCirculator.Status, _bathCirculator.Status.CurrentTemperature.ToString("F1"), _bathCirculator.Status.TargetTemperature.ToString("F1"), snapshot.BathCirculator.Mode, snapshot.BathCirculator.Time, snapshot.BathCirculator.HasError.ToString(), snapshot.BathCirculator.HasWarning.ToString() });

                        if (snapshot.Connections.TempController && _tempController?.Status != null)
                            DataLoggerService.Instance.LogDataAsync("TempController", new List<string> { snapshot.TempController.Channels[0].PresentValue, snapshot.TempController.Channels[0].SetValue, snapshot.TempController.Channels[0].HeatingMV.Replace(" %", ""), snapshot.TempController.Channels[0].Status, snapshot.TempController.Channels[1].PresentValue, snapshot.TempController.Channels[1].SetValue, snapshot.TempController.Channels[1].HeatingMV.Replace(" %", ""), snapshot.TempController.Channels[1].Status, snapshot.TempController.Channels[2].PresentValue, snapshot.TempController.Channels[3].PresentValue, snapshot.TempController.Channels[4].PresentValue });
                    }
                    catch (Exception ex) { LogError($"데이터 로깅 오류: {ex.Message}", ex); }
                });
            }
        }

        /// <summary>
        /// Thermal Ramp 대기 중인 설정값 적용 (데이터 수집 주기에 맞춰 호출)
        /// </summary>
        private void ApplyThermalRampPendingSetpoint()
        {
            // SimpleRampControl이 실행 중이고 대기 중인 설정값이 있으면 적용
            if (simpleRampControl1 != null && simpleRampControl1.IsRunning)
            {
                try
                {
                    simpleRampControl1.RampController?.ApplyPendingSetpoint();
                }
                catch { }
            }
        }

        #endregion

        #region UI 업데이트 메서드

        public void SetAtmPressureText(string value) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetAtmPressureText), value); return; } try { if (txtATM != null) txtATM.TextValue = value; } catch { } }
        public void SetPiraniPressureText(string value) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetPiraniPressureText), value); return; } try { if (txtPG != null) { txtPG.TextValue = value; txtPG.ForeColor = (double.TryParse(value.Replace("E", "e"), out double p) && p > 1E-3) ? Color.Red : SystemColors.WindowText; } } catch { } }
        public void SetIonPressureText(string value) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonPressureText), value); return; } try { if (txtIG != null) txtIG.TextValue = value; } catch { } }
        public void SetIonGaugeStatusText(string value) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonGaugeStatusText), value); return; } try { if (txtIGStatus != null) txtIGStatus.TextValue = value; } catch { } }
        public void SetGateValveStatus(string status) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetGateValveStatus), status); return; } try { if (btn_GV != null && !btn_GV.Focused) { btn_GV.Text = status; btn_GV.BackColor = status == "Moving" ? Color.Yellow : status == "Opened" ? Color.LightGreen : SystemColors.Control; } } catch { } }
        public void SetVentValveStatus(string status) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetVentValveStatus), status); return; } try { if (btn_VV != null && !btn_VV.Focused) { btn_VV.Text = status; btn_VV.BackColor = status == "Opened" ? Color.LightBlue : SystemColors.Control; } } catch { } }
        public void SetExhaustValveStatus(string status) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetExhaustValveStatus), status); return; } try { if (btn_EV != null && !btn_EV.Focused) { btn_EV.Text = status; btn_EV.BackColor = status == "Opened" ? Color.LightCoral : SystemColors.Control; } } catch { } }
        public void SetIonGaugeHVStatus(string status) { if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonGaugeHVStatus), status); return; } try { if (btn_iongauge != null && !btn_iongauge.Focused) { btn_iongauge.Text = status; btn_iongauge.BackColor = status == "HV on" ? Color.Orange : SystemColors.Control; } } catch { } }
        public void SetDryPumpStatus(string status, string speed, string current, string temperature, bool hasWarning, bool hasError, string warningMessage = "") { if (InvokeRequired) { BeginInvoke(new Action<string, string, string, string, bool, bool, string>(SetDryPumpStatus), status, speed, current, temperature, hasWarning, hasError, warningMessage); return; } try { if (txtDryPumpStatus != null) txtDryPumpStatus.Text = status; if (txtDryPumpFrequency != null) txtDryPumpFrequency.Text = speed; if (txtDryPumpCurrent != null) txtDryPumpCurrent.Text = current; if (txtDryPumpMotorTemp != null) txtDryPumpMotorTemp.Text = temperature; if (lblDryPumpWarning != null) { lblDryPumpWarning.Visible = hasError || hasWarning; if (hasError || hasWarning) { lblDryPumpWarning.Text = warningMessage; lblDryPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange; } } } catch { } }
        public void SetTurboPumpStatus(string status, string speed, string current, string temperature, bool hasWarning, bool hasError, string warningMessage = "") { if (InvokeRequired) { BeginInvoke(new Action<string, string, string, string, bool, bool, string>(SetTurboPumpStatus), status, speed, current, temperature, hasWarning, hasError, warningMessage); return; } try { if (txtTurboPumpStatus != null) txtTurboPumpStatus.Text = status; if (txtTurboPumpSpeed != null) txtTurboPumpSpeed.Text = speed; if (txtTurboPumpCurrent != null) txtTurboPumpCurrent.Text = current; if (txtTurboPumpMotorTemp != null) txtTurboPumpMotorTemp.Text = temperature; if (lblTurboPumpWarning != null) { lblTurboPumpWarning.Visible = hasError || hasWarning; if (hasError || hasWarning) { lblTurboPumpWarning.Text = warningMessage; lblTurboPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange; } } } catch { } }
        public void SetBathCirculatorStatus(string status, string currentTemp, string targetTemp, string time, string mode, bool hasError, bool hasWarning) { if (InvokeRequired) { BeginInvoke(new Action<string, string, string, string, string, bool, bool>(SetBathCirculatorStatus), status, currentTemp, targetTemp, time, mode, hasError, hasWarning); return; } try { if (txtBathCirculatorStatus != null) txtBathCirculatorStatus.Text = status; if (txtBathCirculatorCurrentTemp != null) txtBathCirculatorCurrentTemp.Text = currentTemp; if (txtBathCirculatorTargetTemp != null) txtBathCirculatorTargetTemp.Text = targetTemp; if (txtBathCirculatorTime != null) txtBathCirculatorTime.Text = time; if (txtBathCirculatorMode != null) txtBathCirculatorMode.Text = mode; } catch { } }

        public void SetTempControllerChannelStatus(int channel, string presentValue, string setValue, string status, string heatingMV, bool isAutoTuning)
        {
            if (InvokeRequired) { BeginInvoke(new Action<int, string, string, string, string, bool>(SetTempControllerChannelStatus), channel, presentValue, setValue, status, heatingMV, isAutoTuning); return; }
            try
            {
                switch (channel)
                {
                    case 1:
                        if (txtCh1PresentValue != null) txtCh1PresentValue.Text = $"{presentValue}℃";
                        if (txtCh1SetValue != null) txtCh1SetValue.Text = $"{setValue}℃";
                        if (txtCh1Status != null) txtCh1Status.Text = status;
                        if (txtCh1HeatingMV != null) txtCh1HeatingMV.Text = heatingMV;
                        if (txtCh1IsAutotune != null) txtCh1IsAutotune.Text = isAutoTuning ? "On" : "Off";
                        break;
                    case 2: if (txtCh2PresentValue != null) txtCh2PresentValue.Text = $"{presentValue}℃"; break;
                    case 3: if (txtCh3PresentValue != null) txtCh3PresentValue.Text = $"{presentValue}℃"; break;
                    case 4: if (txtCh4PresentValue != null) txtCh4PresentValue.Text = $"{presentValue}℃"; break;
                    case 5: if (txtCh5PresentValue != null) txtCh5PresentValue.Text = $"{presentValue}℃"; break;
                }
            }
            catch { }
        }

        public void SetButtonEnabled(string buttonName, bool enabled) { if (InvokeRequired) { BeginInvoke(new Action<string, bool>(SetButtonEnabled), buttonName, enabled); return; } try { var btn = GetButtonByName(buttonName); if (btn != null && btn.Enabled != enabled) btn.Enabled = enabled; } catch { } }
        public void SetConnectionStatus(string deviceName, bool isConnected) { if (InvokeRequired) { BeginInvoke(new Action<string, bool>(SetConnectionStatus), deviceName, isConnected); return; } try { var indicator = GetConnectionIndicatorByName(deviceName); if (indicator != null) indicator.BackColor = isConnected ? Color.Green : Color.Red; } catch { } }
        private Button GetButtonByName(string buttonName) => buttonName.ToLower() switch { "iongauge" or "btn_iongauge" => btn_iongauge, "ventvalve" or "btn_vv" => btn_VV, "exhaustvalve" or "btn_ev" => btn_EV, "drypumpstart" => btnDryPumpStart, "drypumpstop" => btnDryPumpStop, "drypumpstandby" => btnDryPumpStandby, "drypumpnormal" => btnDryPumpNormal, "turbopumpstart" => btnTurboPumpStart, "turbopumpstop" => btnTurboPumpStop, "turbopumpvent" => btnTurboPumpVent, "turbopumpreset" => btnTurboPumpReset, "bathcirculatorstart" => btnBathCirculatorStart, "bathcirculatorstop" => btnBathCirculatorStop, "ch1start" => btnCh1Start, "ch1stop" => btnCh1Stop, _ => null };
        private Control GetConnectionIndicatorByName(string deviceName) => deviceName.ToLower() switch { "iomodule" => connectionIndicator_iomodule, "drypump" => connectionIndicator_drypump, "turbopump" => connectionIndicator_turbopump, "bathcirculator" => connectionIndicator_bathcirculator, "tempcontroller" => connectionIndicator_tempcontroller, _ => null };
        public void UpdatePIDStatus() { if (_chillerPIDService != null && _chillerPIDService.IsEnabled) lblLastOutputValue.Text = $"{_chillerPIDService.LastOutput:F2}°C (Ch2: {_chillerPIDService.LastCh2Temperature:F1}°C, 칠러: {_chillerPIDService.LastChillerSetpoint:F1}°C)"; }

        #endregion

        #region 밸브 제어

        private async void btn_GV_Click(object sender, EventArgs e) { btn_GV.Enabled = false; try { bool isOpen = btn_GV.Text == "Opened"; if (await _ioModule.ControlGateValveAsync(!isOpen)) LogInfo($"게이트 밸브 {(!isOpen ? "열기" : "닫기")} 성공"); } finally { btn_GV.Enabled = true; } }
        private async void btn_VV_Click(object sender, EventArgs e) { if (_turboPump?.Status?.CurrentSpeed > 100) { MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btn_VV.Enabled = false; try { bool isOpen = btn_VV.Text == "Opened"; if (await _ioModule.ControlVentValveAsync(!isOpen)) LogInfo($"벤트 밸브 {(!isOpen ? "열기" : "닫기")} 성공"); } finally { btn_VV.Enabled = true; } }
        private async void btn_EV_Click(object sender, EventArgs e) { if (_turboPump?.Status?.CurrentSpeed > 100) { MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btn_EV.Enabled = false; try { bool isOpen = btn_EV.Text == "Opened"; if (await _ioModule.ControlExhaustValveAsync(!isOpen)) LogInfo($"배기 밸브 {(!isOpen ? "열기" : "닫기")} 성공"); } finally { btn_EV.Enabled = true; } }
        private async void btn_iongauge_Click(object sender, EventArgs e) { btn_iongauge.Enabled = false; try { bool isOn = btn_iongauge.Text == "HV on"; if (await _ioModule.ControlIonGaugeHVAsync(!isOn)) LogInfo($"이온 게이지 HV {(!isOn ? "ON" : "OFF")} 성공"); } finally { btn_iongauge.Enabled = true; } }

        #endregion

        #region 펌프 제어

        private async void btnDryPumpStart_Click(object sender, EventArgs e) { if (btn_GV.Text != "Opened") { MessageBox.Show("게이트 밸브가 열려있지 않습니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } if (btn_VV.Text == "Opened" || btn_EV.Text == "Opened") { MessageBox.Show("벤트 또는 배기 밸브가 열려있습니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btnDryPumpStart.Enabled = false; try { await Task.Run(() => _dryPump.Start()); LogInfo("드라이펌프 시작"); } finally { btnDryPumpStart.Enabled = true; } }
        private async void btnDryPumpStop_Click(object sender, EventArgs e) { if (_turboPump?.Status?.IsRunning == true) { MessageBox.Show("터보펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btnDryPumpStop.Enabled = false; try { await Task.Run(() => _dryPump.Stop()); LogInfo("드라이펌프 정지"); } finally { btnDryPumpStop.Enabled = true; } }
        private async void btnDryPumpStandby_Click(object sender, EventArgs e) { btnDryPumpStandby.Enabled = false; try { await Task.Run(() => _dryPump.SetStandby()); LogInfo("드라이펌프 대기모드"); } finally { btnDryPumpStandby.Enabled = true; } }
        private async void btnDryPumpNormal_Click(object sender, EventArgs e) { btnDryPumpNormal.Enabled = false; try { await Task.Run(() => _dryPump.SetNormalMode()); LogInfo("드라이펌프 정상모드"); } finally { btnDryPumpNormal.Enabled = true; } }
        private async void btnTurboPumpStart_Click(object sender, EventArgs e) { if (!_dryPump?.Status?.IsRunning ?? true) { MessageBox.Show("드라이펌프가 작동중이 아닙니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } if (_dataCollectionService?.GetLatestPressure() > 1) { MessageBox.Show("챔버 압력이 너무 높습니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } if (!_bathCirculator?.Status?.IsRunning ?? true) { MessageBox.Show("칠러가 작동중이 아닙니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btnTurboPumpStart.Enabled = false; try { await Task.Run(() => _turboPump.Start()); LogInfo("터보펌프 시작"); } finally { btnTurboPumpStart.Enabled = true; } }
        private async void btnTurboPumpStop_Click(object sender, EventArgs e) { btnTurboPumpStop.Enabled = false; try { await Task.Run(() => _turboPump.Stop()); LogInfo("터보펌프 정지"); } finally { btnTurboPumpStop.Enabled = true; } }
        private async void btnTurboPumpVent_Click(object sender, EventArgs e) { btnTurboPumpVent.Enabled = false; try { await Task.Run(() => _turboPump.Vent()); LogInfo("터보펌프 벤트"); } finally { btnTurboPumpVent.Enabled = true; } }
        private async void btnTurboPumpReset_Click(object sender, EventArgs e) { btnTurboPumpReset.Enabled = false; try { await Task.Run(() => _turboPump.ResetError()); LogInfo("터보펌프 리셋"); } finally { btnTurboPumpReset.Enabled = true; } }

        #endregion

        #region 온도 제어

        private async void btnBathCirculatorStart_Click(object sender, EventArgs e) { btnBathCirculatorStart.Enabled = false; try { await Task.Run(() => _bathCirculator.Start()); LogInfo("칠러 시작"); } finally { btnBathCirculatorStart.Enabled = true; } }
        private async void btnBathCirculatorStop_Click(object sender, EventArgs e) { if (_turboPump.IsRunning) { MessageBox.Show("터보 펌프가 작동 중입니다.", "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; } btnBathCirculatorStop.Enabled = false; try { await Task.Run(() => _bathCirculator.Stop()); LogInfo("칠러 정지"); } finally { btnBathCirculatorStop.Enabled = true; } }
        private void btnBathCirculatorSetTemp_Click(object sender, EventArgs e) { if (_bathCirculator == null || !_bathCirculator.IsConnected) return; string input = Microsoft.VisualBasic.Interaction.InputBox("목표 온도 (℃):", "온도 설정", _bathCirculator.Status.SetTemperature.ToString("F1")); if (!string.IsNullOrEmpty(input) && double.TryParse(input, out double temp)) { if (_bathCirculator.SetTemperature(temp)) LogInfo($"칠러 온도 설정: {temp}℃"); } }
        private void btnBathCirculatorSetTime_Click(object sender, EventArgs e) { /* 기존 코드 유지 */ }

        private async void btnCh1Start_Click(object sender, EventArgs e) { btnCh1Start.Enabled = false; try { await Task.Run(() => _tempController.Start(1)); LogInfo("온도컨트롤러 CH1 시작"); if (chkCh1TimerEnabled.Checked) StartCh1Timer(); } finally { btnCh1Start.Enabled = true; } }
        private async void btnCh1Stop_Click(object sender, EventArgs e) { btnCh1Stop.Enabled = false; try { await Task.Run(() => _tempController.Stop(1)); LogInfo("온도컨트롤러 CH1 정지"); if (_ch1TimerActive || _ch1WaitingForTargetTemp || _ch1WaitingForVacuum) StopCh1Timer(); } finally { btnCh1Stop.Enabled = true; } }
        private void btnCh1SetTemp_Click(object sender, EventArgs e) { ShowTemperatureSetDialog(1); }
        private async void btnCh1AutoTuning_Click(object sender, EventArgs e) { if (_tempController == null || !_tempController.IsConnected) return; if (MessageBox.Show("CH1 오토튜닝을 시작하시겠습니까?", "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { btnCh1AutoTuning.Enabled = false; try { if (await Task.Run(() => _tempController.StartAutoTuning(1))) { LogInfo("CH1 오토튜닝 시작"); MessageBox.Show("CH1 오토튜닝 시작됨", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information); } else { LogError("CH1 오토튜닝 시작 실패"); MessageBox.Show("오토튜닝 실패", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); } } finally { btnCh1AutoTuning.Enabled = true; } } }

        private void ShowTemperatureSetDialog(int channel)
        {
            if (_tempController == null || !_tempController.IsConnected) return;
            if (channel > _tempController.ChannelCount) { MessageBox.Show($"CH{channel}은 확장 모듈 채널로 입력 전용입니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            var ch = _tempController.Status.ChannelStatus[channel - 1];
            string currentValue = ch.Dot == 0 ? ch.SetValue.ToString() : (ch.SetValue / 10.0).ToString("F1");
            string input = Microsoft.VisualBasic.Interaction.InputBox($"CH{channel} 목표 온도 ({ch.TemperatureUnit}):", "온도 설정", currentValue);
            if (!string.IsNullOrEmpty(input))
            {
                if (ch.Dot == 0) { if (short.TryParse(input, out short sv)) { _tempController.SetTemperature(channel, sv); LogInfo($"CH{channel} 온도 설정: {sv}{ch.TemperatureUnit}"); } }
                else { if (double.TryParse(input, out double dv)) { _tempController.SetTemperature(channel, (short)(dv * 10)); LogInfo($"CH{channel} 온도 설정: {dv:F1}{ch.TemperatureUnit}"); } }
            }
        }

        #endregion

        #region 칠러 PID 제어

        private void chkChillerPIDEnabled_CheckedChanged(object sender, EventArgs e) { if (_chillerPIDService == null) return; bool enabled = chkChillerPIDEnabled.Checked; _chillerPIDService.IsEnabled = enabled; UpdateChillerPIDControls(enabled); lblPIDStatusValue.Text = enabled ? "실행 중" : "정지됨"; lblPIDStatusValue.ForeColor = enabled ? Color.Green : Color.Red; LogInfo($"칠러 PID 제어 {(enabled ? "활성화" : "비활성화")}"); }
        private void numCh2Target_ValueChanged(object sender, EventArgs e) { if (_chillerPIDService != null) _chillerPIDService.Ch2TargetTemperature = (double)numCh2Target.Value; }
        private void numChillerBase_ValueChanged(object sender, EventArgs e) { if (_chillerPIDService != null) _chillerPIDService.ChillerBaseTemperature = (double)numChillerBase.Value; }
        private void PIDParams_ValueChanged(object sender, EventArgs e) { if (_chillerPIDService != null) _chillerPIDService.SetPIDParameters((double)numKp.Value, (double)numKi.Value, (double)numKd.Value); }
        private void numUpdateInterval_ValueChanged(object sender, EventArgs e) { if (_chillerPIDService != null) _chillerPIDService.UpdateInterval = (double)numUpdateInterval.Value; }
        private void UpdateChillerPIDControls(bool enabled) { numCh2Target.Enabled = enabled; numChillerBase.Enabled = enabled; grpPIDParams.Enabled = enabled; numUpdateInterval.Enabled = enabled; }

        #endregion

        #region CH1 자동 시작/정지 타이머

        // === 이벤트 핸들러 추가 ===

        /// <summary>
        /// ScientificPressureInput 값 변경 이벤트
        /// </summary>
        private void scientificPressureInput1_ValueChanged(object sender, EventArgs e)
        {
            _ch1TargetPressure = scientificPressureInput1.Value;
        }

        /// <summary>
        /// 도달 횟수 변경 이벤트
        /// </summary>
        private void numCh1ReachCount_ValueChanged(object sender, EventArgs e)
        {
            _ch1RequiredReachCount = (int)numCh1ReachCount.Value;
        }

        /// <summary>
        /// 자동 시작 활성화 체크박스 이벤트
        /// </summary>
        private void chkCh1AutoStartEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _ch1AutoStartEnabled = chkCh1AutoStartEnabled.Checked;
            UpdateCh1TimerControls();
        }

        /// <summary>
        /// 목표 압력 변경 이벤트
        /// </summary>
        private void numCh1TargetPressure_ValueChanged(object sender, EventArgs e)
        {
            _ch1TargetPressure = (double)numCh1TargetPressure.Value;
        }

        private void chkCh1TimerEnabled_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCh1TimerControls();
        }

        private void UpdateCh1TimerControls()
        {
            bool timerEnabled = chkCh1TimerEnabled.Checked;
            bool autoStartEnabled = chkCh1AutoStartEnabled?.Checked ?? false;
            bool anyProcessActive = _ch1TimerActive || _ch1WaitingForTargetTemp || _ch1WaitingForVacuum;

            // ★ 램프 진행 중 여부 추가
            bool rampRunning = simpleRampControl1?.IsRunning ?? false;
            bool isLocked = anyProcessActive || rampRunning;

            // 타이머 설정
            numCh1Hours.Enabled = timerEnabled && !isLocked;
            numCh1Minutes.Enabled = timerEnabled && !isLocked;
            numCh1Seconds.Enabled = timerEnabled && !isLocked;
            numVentTargetTemp.Enabled = timerEnabled && !isLocked;

            // 자동 시작 설정
            if (chkCh1AutoStartEnabled != null)
                chkCh1AutoStartEnabled.Enabled = timerEnabled && !isLocked;
            if (scientificPressureInput1 != null)
                scientificPressureInput1.Enabled = timerEnabled && autoStartEnabled && !isLocked;
            if (numCh1ReachCount != null)
                numCh1ReachCount.Enabled = timerEnabled && autoStartEnabled && !isLocked;

            // ★ 추가: 베이크 아웃 설정 버튼
            if (btnBakeoutSettings != null)
                btnBakeoutSettings.Enabled = !isLocked;

            // ★ 추가: 타이머 활성화 체크박스
            chkCh1TimerEnabled.Enabled = !isLocked;

            if (!timerEnabled && anyProcessActive)
                StopCh1Timer();
        }

        /// <summary>
        /// 자동 시작 대기 시작 (버튼 클릭 또는 외부 호출)
        /// </summary>
        public void StartCh1AutoStartWaiting()
        {
            if (!chkCh1TimerEnabled.Checked || !_ch1AutoStartEnabled)
            {
                MessageBox.Show("타이머와 자동 시작이 활성화되어야 합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int hours = (int)numCh1Hours.Value;
            int minutes = (int)numCh1Minutes.Value;
            int seconds = (int)numCh1Seconds.Value;

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show("타이머 시간을 설정해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _ch1Duration = new TimeSpan(hours, minutes, seconds);
            _ch1WaitingForVacuum = true;
            _ch1WaitingForTargetTemp = false;
            _ch1TimerActive = false;
            _ch1PressureReachCount = 0;  // 카운터 초기화

            _ch1AutoStopTimer.Start();
            UpdateCh1TimerControls();
            lblCh1TimeRemainingValue.Text = "진공 대기중...";
            lblCh1TimeRemainingValue.ForeColor = Color.Purple;

            LogInfo($"CH1 자동 시작 대기 - 목표: {_ch1TargetPressure:E1} Torr ({_ch1RequiredReachCount}회 확인), 타이머: {_ch1Duration}");
        }

        /// <summary>
        /// 자동 시작 버튼 클릭 이벤트
        /// </summary>
        private void btnCh1AutoStart_Click(object sender, EventArgs e)
        {
            if (_ch1WaitingForVacuum || _ch1WaitingForTargetTemp || _ch1TimerActive)
            {
                StopCh1Timer();
                LogInfo("CH1 자동 시작/정지 취소됨");
            }
            else
            {
                StartCh1AutoStartWaiting();
            }
        }

        private void Ch1AutoStopTimer_Tick(object sender, EventArgs e)
        {
            if (!_ch1TimerActive && !_ch1WaitingForTargetTemp && !_ch1WaitingForVacuum)
            {
                _ch1AutoStopTimer.Stop();
                return;
            }

            // 1단계: 진공 대기 (자동 시작)
            if (_ch1WaitingForVacuum)
            {
                double currentPressure = GetCurrentIonGaugePressure();

                if (currentPressure > 0 && currentPressure <= _ch1TargetPressure)
                {
                    _ch1PressureReachCount++;

                    if (_ch1PressureReachCount >= _ch1RequiredReachCount)
                    {
                        _ch1WaitingForVacuum = false;
                        _ch1PressureReachCount = 0;  // 카운터 리셋
                        LogInfo($"목표 진공 도달 ({currentPressure:E2} Torr, {_ch1RequiredReachCount}회 연속) - CH1 자동 시작");

                        Task.Run(async () =>
                        {
                            try
                            {
                                await Task.Run(() => _tempController.Start(1));

                                RunOnUIThread(() =>
                                {
                                    LogInfo("CH1 자동 시작됨");

                                    // ★ 베이크 아웃 램프 업 사용 여부에 따라 분기
                                    if (_bakeoutSettings?.EnableAutoRampUp == true)
                                    {
                                        // 램프 사용 → CH2 감시 (TargetReached 이벤트로 타이머 시작)
                                        StartBakeoutRampUp();
                                        _ch1WaitingForTargetTemp = false;  // CH1 감시 건너뜀
                                        lblCh1TimeRemainingValue.Text = "램프 진행중...";
                                        lblCh1TimeRemainingValue.ForeColor = Color.Green;
                                    }
                                    else
                                    {
                                        // 기존 방식 → CH1 감시
                                        _ch1WaitingForTargetTemp = true;
                                        lblCh1TimeRemainingValue.Text = "온도 대기중...";
                                        lblCh1TimeRemainingValue.ForeColor = Color.Orange;
                                    }
                                });
                            }
                            catch (Exception ex)
                            {
                                RunOnUIThread(() => { LogError($"CH1 자동 시작 실패: {ex.Message}"); StopCh1Timer(); });
                            }
                        });
                    }
                    else
                    {
                        // 아직 필요 횟수에 도달하지 않음
                        string pressureText = $"{currentPressure:E2}";
                        lblCh1TimeRemainingValue.Text = $"진공 확인 중... ({_ch1PressureReachCount}/{_ch1RequiredReachCount})";
                        lblCh1TimeRemainingValue.ForeColor = Color.Purple;
                    }
                }
                else
                {
                    // 압력이 목표보다 높으면 카운터 리셋
                    _ch1PressureReachCount = 0;
                    string pressureText = currentPressure > 0 ? $"{currentPressure:E2}" : "N/A";
                    lblCh1TimeRemainingValue.Text = $"진공 대기 ({pressureText} / {_ch1TargetPressure:E1} Torr)";
                    lblCh1TimeRemainingValue.ForeColor = Color.Purple;
                }
                return;
            }


            // 2단계: 온도 대기
            if (_ch1WaitingForTargetTemp)
            {
                if (_tempController?.Status?.ChannelStatus != null && _tempController.Status.ChannelStatus.Length > 0)
                {
                    var ch1 = _tempController.Status.ChannelStatus[0];
                    double pv = ch1.PresentValue;
                    double sv = ch1.SetValue;
                    if (ch1.Dot > 0) { pv /= Math.Pow(10, ch1.Dot); sv /= Math.Pow(10, ch1.Dot); }

                    if (Math.Abs(pv - sv) <= _ch1TargetTolerance)
                    {
                        _ch1WaitingForTargetTemp = false;
                        _ch1TimerActive = true;
                        _ch1StartTime = DateTime.Now;
                        lblCh1TimeRemainingValue.ForeColor = Color.Blue;
                        LogInfo($"CH1 목표 온도 도달 (PV:{pv:F1}°C, SV:{sv:F1}°C) - 타이머 시작");
                    }
                    else
                    {
                        lblCh1TimeRemainingValue.Text = $"온도 대기 ({pv:F1}/{sv:F1}°C)";
                        lblCh1TimeRemainingValue.ForeColor = Color.Orange;
                    }
                }
                return;
            }

            // 3단계: 타이머 카운트다운
            if (_ch1TimerActive)
            {
                TimeSpan remaining = _ch1Duration - (DateTime.Now - _ch1StartTime);

                if (remaining.TotalSeconds <= 0)
                {
                    StopCh1WithTimer();
                    lblCh1TimeRemainingValue.Text = "00:00:00";
                    lblCh1TimeRemainingValue.ForeColor = Color.Red;
                }
                else
                {
                    lblCh1TimeRemainingValue.Text = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                    lblCh1TimeRemainingValue.ForeColor = remaining.TotalSeconds <= 60 ? Color.Red : remaining.TotalMinutes <= 5 ? Color.Orange : Color.Blue;
                }
            }
        }

        /// <summary>
        /// 베이크 아웃 램프 업 자동 시작
        /// </summary>
        private async void StartBakeoutRampUp()
        {
            if (_bakeoutSettings == null || !_bakeoutSettings.EnableAutoRampUp)
            {
                LogInfo("[베이크 아웃] 램프 업 비활성화됨");
                return;
            }

            if (simpleRampControl1 == null)
            {
                LogWarning("[베이크 아웃] SimpleRampControl이 초기화되지 않음");
                return;
            }

            try
            {
                // 타이머 자동 시작 옵션 설정
                simpleRampControl1.AutoStartTimerOnTargetReached = _bakeoutSettings.AutoStartTimerOnTargetReached;

                // ★ 추가: 종료 동작 설정
                simpleRampControl1.EndAction = _bakeoutSettings.EndAction;
                simpleRampControl1.HoldAfterComplete = (_bakeoutSettings.EndAction == BakeoutEndAction.MaintainTemperature);

                // 램프 시작
                bool success = await simpleRampControl1.StartRampAsync(
                    _bakeoutSettings.TargetTemperature,
                    _bakeoutSettings.RampRate,
                    _bakeoutSettings.ProfileName
                );

                if (success)
                {
                    LogInfo($"[베이크 아웃] 램프 업 시작: {_bakeoutSettings.TargetTemperature}°C, {_bakeoutSettings.RampRate}°C/min");
                    UpdateCh1TimerControls();  // ★ 추가
                }
                else
                {
                    LogWarning("[베이크 아웃] 램프 업 시작 실패");
                }
            }
            catch (Exception ex)
            {
                LogError($"[베이크 아웃] 램프 업 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 이온게이지 압력 조회
        /// </summary>
        private double GetCurrentIonGaugePressure()
        {
            try
            {
                var aiData = _dataCollectionService?.GetLatestAIData();
                if (aiData != null && _ionGauge != null)
                    return _ionGauge.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[2]);
            }
            catch { }
            return -1;
        }

        private void StartCh1Timer()
        {
            if (!chkCh1TimerEnabled.Checked) return;

            int hours = (int)numCh1Hours.Value;
            int minutes = (int)numCh1Minutes.Value;
            int seconds = (int)numCh1Seconds.Value;

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show("타이머 시간을 설정해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _ch1Duration = new TimeSpan(hours, minutes, seconds);
            _ch1WaitingForVacuum = false;
            _ch1WaitingForTargetTemp = true;
            _ch1TimerActive = false;

            _ch1AutoStopTimer.Start();
            UpdateCh1TimerControls();
            lblCh1TimeRemainingValue.Text = "온도 대기중...";
            lblCh1TimeRemainingValue.ForeColor = Color.Orange;

            LogInfo($"CH1 타이머: {_ch1Duration} (목표 온도 도달 후 시작)");
        }

        private void StopCh1Timer()
        {
            _ch1TimerActive = false;
            _ch1WaitingForTargetTemp = false;
            _ch1WaitingForVacuum = false;
            _ch1AutoStopTimer.Stop();
            lblCh1TimeRemainingValue.Text = "00:00:00";
            lblCh1TimeRemainingValue.ForeColor = Color.Blue;
            UpdateCh1TimerControls();
            LogInfo("CH1 타이머 정지");
        }

        private async void StopCh1WithTimer()
        {
            LogInfo("CH1 타이머 만료 - 종료 시퀀스 시작");
            try
            {
                StopCh1Timer();

                bool needCh1Stop = _tempController?.Status?.ChannelStatus[0].IsRunning == true;
                bool needIonGaugeOff = false;
                bool needTurboPumpStop = _turboPump?.Status?.IsRunning == true || _turboPump?.Status?.CurrentSpeed > 0;
                bool needDryPumpStop = _dryPump?.Status?.IsRunning == true;
                bool needGateValveClose = false;
                bool needVentValvesOpen = false;

                if (_dataCollectionService != null)
                {
                    var (ventOpen, exhaustOpen, ionGaugeHV) = _dataCollectionService.GetValveStates();
                    needIonGaugeOff = ionGaugeHV;
                    needVentValvesOpen = !ventOpen || !exhaustOpen;
                    needGateValveClose = _dataCollectionService.GetGateValveStatus() != "Closed";
                }

                if (needCh1Stop) await ExecuteWithRetry("CH1 정지", async () => { await Task.Run(() => _tempController.Stop(1)); await Task.Delay(1000); return _tempController.Status?.ChannelStatus[0].IsRunning == false; }, btnCh1Stop, 3, 2000);
                if (needIonGaugeOff) await ExecuteWithRetry("이온게이지 OFF", async () => { bool r = await _ioModule.ControlIonGaugeHVAsync(false); if (!r) return false; await Task.Delay(1000); var (_, _, hv) = _dataCollectionService.GetValveStates(); return !hv; }, btn_iongauge, 3, 1000);
                if (needTurboPumpStop) { await ExecuteWithRetry("터보펌프 정지", async () => { await Task.Run(() => _turboPump.Stop()); return true; }, btnTurboPumpStop, 3, 2000); await WaitForCondition(() => !_turboPump.IsRunning || (_turboPump.Status?.CurrentSpeed ?? 0) == 0, 1800, 30, null); }
                if (needDryPumpStop) await ExecuteWithRetry("드라이펌프 정지", async () => { await Task.Run(() => _dryPump.Stop()); await Task.Delay(2000); return _dryPump.Status?.IsRunning == false; }, btnDryPumpStop, 3, 2000);
                if (needGateValveClose) await ExecuteWithRetry("게이트 밸브 닫기", async () => { bool r = await _ioModule.ControlGateValveAsync(false); if (!r) return false; await Task.Delay(3000); return _dataCollectionService.GetGateValveStatus() == "Closed"; }, btn_GV, 3, 2000);
                if (needVentValvesOpen) { var (vo, eo, _) = _dataCollectionService.GetValveStates(); if (!vo) await ExecuteWithRetry("VV 열기", async () => { bool r = await _ioModule.ControlVentValveAsync(true); await Task.Delay(1000); return r; }, btn_VV, 3, 1000); if (!eo) await ExecuteWithRetry("EV 열기", async () => { bool r = await _ioModule.ControlExhaustValveAsync(true); await Task.Delay(1000); return r; }, btn_EV, 3, 1000); }

                if (_tempController.Status.ChannelStatus[0].PresentValue > _ch1VentTargetTemp * 10)
                {
                    LogInfo($"CH1 온도 {_ch1VentTargetTemp}°C 도달 대기");
                    await WaitForCondition(() => (_tempController?.Status?.ChannelStatus[0].PresentValue / 10.0 ?? 999) <= _ch1VentTargetTemp, 7200, 10, null);
                }

                var (fvo, feo, _) = _dataCollectionService.GetValveStates();
                if (fvo) await ExecuteWithRetry("VV 닫기", async () => { bool r = await _ioModule.ControlVentValveAsync(false); await Task.Delay(1000); return r; }, btn_VV, 3, 1000);
                if (feo) await ExecuteWithRetry("EV 닫기", async () => { bool r = await _ioModule.ControlExhaustValveAsync(false); await Task.Delay(1000); return r; }, btn_EV, 3, 1000);

                LogInfo("CH1 종료 시퀀스 완료");
                ShowMessageBox("CH1 종료 시퀀스 완료", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError($"CH1 종료 시퀀스 오류: {ex.Message}");
                ShowMessageBox($"종료 시퀀스 오류:\n{ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> ExecuteWithRetry(string name, Func<Task<bool>> op, Button btn, int maxRetries, int delay)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try { LogInfo($"{name} 시도 {i + 1}/{maxRetries}"); SetButtonEnabled(btn, false); if (await op()) { LogInfo($"{name} 완료"); return true; } }
                catch (Exception ex) { LogError($"{name} 오류: {ex.Message}"); }
                finally { SetButtonEnabled(btn, true); }
                if (i < maxRetries - 1) await Task.Delay(delay);
            }
            LogWarning($"{name} 실패");
            return false;
        }

        private async Task<bool> WaitForCondition(Func<bool> cond, int maxSec, int interval, Action log)
        {
            int cnt = 0;
            while (cnt < maxSec) { if (cond()) return true; await Task.Delay(1000); if (cnt % interval == 0 && log != null) log(); cnt++; }
            return false;
        }

        private void numVentTargetTemp_ValueChanged(object sender, EventArgs e) { _ch1VentTargetTemp = Convert.ToDouble(numVentTargetTemp.Value); }

        #endregion

        #region 로깅

        private void LogInfo(string message) => AsyncLoggingService.Instance.LogInfo(message);
        private void LogError(string message, Exception ex = null) => AsyncLoggingService.Instance.LogError(message, ex);
        private void LogWarning(string message) => AsyncLoggingService.Instance.LogWarning(message);
        private void LogDebug(string message) => AsyncLoggingService.Instance.LogDebug(message);

        private void OnLogAdded(object sender, string logMessage)
        {
            if (InvokeRequired) { BeginInvoke(new Action<object, string>(OnLogAdded), sender, logMessage); return; }
            try { if (txtLog != null && !txtLog.IsDisposed) { if (txtLog.Lines.Length > 1000) txtLog.Lines = txtLog.Lines.Skip(500).ToArray(); txtLog.AppendText(logMessage + Environment.NewLine); txtLog.SelectionStart = txtLog.Text.Length; txtLog.ScrollToCaret(); } } catch { }
        }

        private void InitializeDataLogging()
        {
            DataLoggerService.Instance.StartLogging("Pressure", new List<string> { "ATM(kPa)", "Pirani(Torr)", "Ion(Torr)", "IonStatus", "GateValve", "VentValve", "ExhaustValve", "IonGaugeHV" });
            DataLoggerService.Instance.StartLogging("DryPump", new List<string> { "Status", "Frequency(Hz)", "Current(A)", "Temperature(°C)", "HasWarning", "HasFault" });
            DataLoggerService.Instance.StartLogging("TurboPump", new List<string> { "Status", "Speed(RPM)", "Current(A)", "Temperature(°C)", "HasWarning", "HasError" });
            DataLoggerService.Instance.StartLogging("BathCirculator", new List<string> { "Status", "CurrentTemp(°C)", "TargetTemp(°C)", "Mode", "Time", "HasError", "HasWarning" });
            DataLoggerService.Instance.StartLogging("TempController", new List<string> { "Ch1_PV(°C)", "Ch1_SV(°C)", "Ch1_HeatingMV(%)", "Ch1_Status", "Ch2_PV(°C)", "Ch2_SV(°C)", "Ch2_HeatingMV(%)", "Ch2_Status", "Ch3_PV(°C)", "Ch4_PV(°C)", "Ch5_PV(°C)" });
            DataLoggerService.Instance.StartLogging("ChillerPID", new List<string> { "Ch2_PV(°C)", "Ch2_Target(°C)", "PID_Output", "Chiller_Setpoint(°C)", "Kp", "Ki", "Kd", "Integral", "Error" });
        }

        private void AddLoggingMenu()
        {
            if (menuStrip == null) return;
            var menuLogging = new ToolStripMenuItem("로깅(&L)");
            _menuStartLogging = new ToolStripMenuItem("로깅 시작"); _menuStartLogging.Click += (s, e) => ToggleLogging(true);
            _menuStopLogging = new ToolStripMenuItem("로깅 중지"); _menuStopLogging.Click += (s, e) => ToggleLogging(false);
            var menuOpenLogFolder = new ToolStripMenuItem("로그 폴더 열기"); menuOpenLogFolder.Click += (s, e) => OpenFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs"));
            var menuOpenDataFolder = new ToolStripMenuItem("데이터 폴더 열기"); menuOpenDataFolder.Click += (s, e) => OpenFolder(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"));
            menuLogging.DropDownItems.AddRange(new ToolStripItem[] { _menuStartLogging, _menuStopLogging, new ToolStripSeparator(), menuOpenLogFolder, menuOpenDataFolder });
            menuStrip.Items.Add(menuLogging);
            UpdateLoggingMenuState();
        }

        private void ToggleLogging(bool enable) { _isLoggingEnabled = enable; LogInfo($"데이터 로깅 {(enable ? "활성화" : "비활성화")}"); UpdateLoggingMenuState(); }
        private void UpdateLoggingMenuState() { if (_menuStartLogging != null && _menuStopLogging != null) { _menuStartLogging.Enabled = !_isLoggingEnabled; _menuStopLogging.Enabled = _isLoggingEnabled; } }
        private void OpenFolder(string path) { try { if (!Directory.Exists(path)) Directory.CreateDirectory(path); System.Diagnostics.Process.Start("explorer.exe", path); } catch (Exception ex) { MessageBox.Show($"폴더 열기 실패: {ex.Message}", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
        public void HandleDeviceCommunicationError(string deviceName) { try { string logType = deviceName switch { "IOModule" => "Pressure", "DryPump" => "DryPump", "TurboPump" => "TurboPump", "BathCirculator" => "BathCirculator", "TempController" => "TempController", _ => null }; if (!string.IsNullOrEmpty(logType)) { DataLoggerService.Instance.StopLogging(logType); LogWarning($"{deviceName} 통신 오류로 데이터 로깅 중단"); } } catch (Exception ex) { LogError($"{deviceName} 통신 오류 처리 실패", ex); } }
        private void StopDeviceDataLogging(string deviceName) { try { string logType = deviceName switch { "ECODRY 25 plus" => "DryPump", "MAG W 1300" => "TurboPump", "LK-1000" => "BathCirculator", _ when deviceName.Contains("TM4") => "TempController", "IO Module" => "Pressure", _ => null }; if (!string.IsNullOrEmpty(logType)) { DataLoggerService.Instance.StopLogging(logType); LogInfo($"{deviceName} 데이터 로깅 중단됨"); } } catch { } }

        #endregion

        #region 종료 처리

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_autoRunService?.IsRunning == true) { if (MessageBox.Show("AutoRun이 실행 중입니다.\n종료하시겠습니까?", "종료 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) { e.Cancel = true; return; } _autoRunService.Stop(); }
            if (_ch1WaitingForVacuum || _ch1WaitingForTargetTemp || _ch1TimerActive) StopCh1Timer();

            LogInfo("시스템 종료 시작");
            using (var loadingForm = new LoadingForm())
            {
                loadingForm.Show(); Application.DoEvents();
                loadingForm.UpdateStatus("서비스 종료 중..."); _dataCollectionService?.Stop(); _uiUpdateService?.Stop();
                loadingForm.UpdateStatus("연결 종료 중..."); foreach (var device in _deviceList) { try { if (device.IsConnected) device.Disconnect(); } catch { } }
                loadingForm.UpdateStatus("로깅 종료 중..."); DataLoggerService.Instance.StopAllLogging(); AsyncLoggingService.Instance.Stop();
                loadingForm.UpdateStatus("리소스 정리 중..."); _dataCollectionService?.Dispose(); _uiUpdateService?.Dispose(); _chillerPIDService?.Dispose();
            }

            _ch1AutoStopTimer?.Stop(); _ch1AutoStopTimer?.Dispose();
            _autoRunTimer?.Stop(); _autoRunTimer?.Dispose();
            _autoRunService?.Dispose();
            LogInfo("시스템 종료 완료");
        }

        #endregion

        #region 기타 이벤트

        private void menuFileExit_Click(object sender, EventArgs e) => Close();
        private void MenuCommSettings_Click(object sender, EventArgs e) { MessageBox.Show("통신 설정은 메인 화면에서 변경 가능합니다.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void MenuHelpAbout_Click(object sender, EventArgs e) { MessageBox.Show("VacX OutSense System Controller\nv1.0.0\n\n© 2024 VacX Inc.", "정보", MessageBoxButtons.OK, MessageBoxIcon.Information); }
        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e) { }
        private void rampSettingControl1_Load(object sender, EventArgs e) { }

        #endregion

        #region UI 헬퍼

        private void SetButtonEnabled(Button button, bool enabled) { if (button == null) return; if (InvokeRequired) BeginInvoke(new Action(() => button.Enabled = enabled)); else button.Enabled = enabled; }
        private void ShowMessageBox(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon) { if (InvokeRequired) BeginInvoke(new Action(() => MessageBox.Show(this, message, title, buttons, icon))); else MessageBox.Show(this, message, title, buttons, icon); }
        private void RunOnUIThread(Action action) { if (InvokeRequired) BeginInvoke(action); else action(); }

        private void btnBakeoutSettings_Click(object sender, EventArgs e)
        {
            if (_profileManager == null)
            {
                _profileManager = new ThermalRampProfileManager();
            }

            using (var form = new BakeoutSettingsForm(_profileManager))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _bakeoutSettings = form.Settings;
                    LogInfo($"[베이크 아웃] 설정 저장: {_bakeoutSettings.TargetTemperature}°C, {_bakeoutSettings.HoldTimeMinutes}분");
                }
            }
        }

        #endregion

        #region UI 성능 최적화

        protected override CreateParams CreateParams { get { CreateParams cp = base.CreateParams; cp.ExStyle |= 0x02000000; return cp; } }

        #endregion
    }
}