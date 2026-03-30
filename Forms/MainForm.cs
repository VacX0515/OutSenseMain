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
using VacX_OutSense.Core.Safety;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.DryPump;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.IO_Module;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Devices.TurboPump;
using ScottPlot.WinForms;
using VacX_OutSense.Forms;
using VacX_OutSense.Models;
using VacX_OutSense.Utils;

namespace VacX_OutSense
{
    public partial class MainForm : Form
    {
        #region 필드 및 속성

        #region 포트 자동 감지
        private PortAutoDetectionService _portDetectionService;
        private PortAutoDetectionService.DetectionResult _detectionResult;
        #endregion

        #region AutoRun 관련 필드
        internal AutoRunService _autoRunService;
        internal AutoRunConfiguration _autoRunConfig;
        private System.Windows.Forms.Timer _autoRunTimer;
        private System.Windows.Forms.Timer _chartTimer;
        private DateTime _lastChartErrorLog;
        private bool _chartDiagLogged;
        private bool _chartUserInteracting;
        private DateTime _chartLastInteraction;
        private int _autoRunElapsedSeconds = 0;
        private int _experimentElapsedSeconds = 0;

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
        private Label _lblAutoRunBanner;
        private Panel[] _stepPanels;
        private Label[] _stepLabels;

        // 토글 버튼 외관 업데이트 억제 (클릭 직후 폴링 지연으로 인한 깜빡임 방지)
        private readonly Dictionary<string, DateTime> _toggleSuppressUntil = new Dictionary<string, DateTime>();

        // 실시간 측정값 라벨
        private GroupBox _groupBoxMeasurements;
        private Label _lblMeasPressure;
        private Label _lblMeasCH1;
        private Label _lblMeasSample;
        private Label _lblMeasPump;
        private Label _lblMeasConfig;
        private Label _lblMeasThermal;
        private GroupBox _groupBoxAutoRunStatus;
        private GroupBox _grpLiveAdjust;
        private NumericUpDown _numAdjTarget, _numAdjMaxTemp, _numAdjTolerance, _numAdjStabilization;
        private Button _btnAdjApply;

        // AutoRun 차트 관련 필드
        private Panel _panelAutoRun;
        private FormsPlot _formsPlotTemp;
        private FormsPlot _formsPlotPressure;
        private GroupBox _groupBoxTempChart;
        private GroupBox _groupBoxPressChart;
        private ChartDataBuffer[] _tempChartBuffers;
        private ChartDataBuffer _pressureChartBuffer;
        // 확장모듈2 (CH9~12) 동적 PV 텍스트박스
        private TextBox[] _expansionPVLabels;
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
        private double _ch1FinalTargetTemp = 0;  // 타이머 시작 시 저장된 최종 목표 온도
        private double _ch1VentTargetTemp = 50.0;

        // 자동 시작 관련 필드
        private bool _ch1AutoStartEnabled = false;
        private bool _ch1WaitingForVacuum = false;
        private double _ch1TargetPressure = 1E-5;
        private int _ch1PressureReachCount = 0;
        private int _ch1RequiredReachCount = 3;
        private bool _igAutoActivating = false;  // IG 자동 활성화 진행 중 플래그
        #endregion

        #region 서비스
        private OptimizedDataCollectionService _dataCollectionService;
        private SimplifiedUIUpdateService _uiUpdateService;
        private ChillerPIDControlService _chillerPIDService;
        internal ChillerPIDControlService ChillerPIDService => _chillerPIDService;
        private ExperimentDataLogger _experimentDataLogger;
        #endregion

        #region 장치 인스턴스
        public IO_Module _ioModule;
        public DryPump _dryPump;
        public TurboPump _turboPump;
        public BathCirculator _bathCirculator;
        public TempController _tempController;
        #endregion

        internal TempCalibrationConfig _tempCalibrationConfig;

        #region 게이지 인스턴스
        public ATMswitch _atmSwitch;
        public PiraniGauge _piraniGauge;
        public IonGauge _ionGauge;
        #endregion

        #region 통신 관리
        private SerialPortChannelManager _channelManager;
        private Dictionary<string, ChannelCommunicationManager> _commManagers = new Dictionary<string, ChannelCommunicationManager>();
        private List<IDevice> _deviceList = new List<IDevice>();
        #endregion

        #region 통신 설정
        private readonly CommunicationSettings _defaultSettings = new CommunicationSettings
        {
            BaudRate = 19200,
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
        private int _dataLoggingIntervalSeconds = 1;
        private DateTime _lastDataLogTime = DateTime.MinValue;
        private ToolStripMenuItem _menuStartLogging;
        private ToolStripMenuItem _menuStopLogging;
        #endregion

        #region 인터락
        private InterlockConfiguration _interlockConfig;
        internal SafetyInterlockService _safetyInterlock;
        private AutoRunLock _autoRunLock;
        private Panel _chamberWorkOverlay;
        private bool _chamberWorkMode;
        #endregion

        #region 통신 포트 설정
        private CommPortSettings _commPortSettings;
        #endregion

        #endregion

        #region 생성자 및 초기화

        public MainForm()
        {
            InitializeComponent();
            this.Text = AppVersion.FullTitle;
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

                    loadingForm.UpdateStatus("캘리브레이션 적용 중...");
                    ApplyCalibrationToChannels();

                    loadingForm.UpdateStatus("AutoRun 초기화 중...");
                    InitializeAutoRun();

                    loadingForm.UpdateStatus("초기화 완료!");
                    await Task.Delay(200);
                }

                LogInfo("시스템 초기화 완료");

                // ReadOnly 텍스트박스: 커서/탭 비활성화
                DisableReadOnlyTextBoxFocus(this);


                // 이전 오토런 상태 복원 확인
                CheckAutoRunResume();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                LogError("초기화 실패", ex);
            }
            finally
            {
                this.Enabled = true;
                this.Focus();
            }
        }

        #region 챔버 작업 모드

        private void EnterChamberWorkMode()
        {
            if (_autoRunService?.IsRunning == true)
            {
                MessageBox.Show("AutoRun 실행 중에는 챔버 작업 모드를 사용할 수 없습니다.",
                    "알림", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show("챔버 작업 모드로 전환합니다.\n\n모든 장비 조작이 차단되고 화면에 작업 중 표시가 나타납니다.\n작업 완료 버튼을 누르면 해제됩니다.",
                "챔버 작업 모드", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK)
                return;

            _chamberWorkMode = true;
            LogInfo("챔버 작업 모드 진입");

            // 오버레이 패널 생성
            _chamberWorkOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(180, 0, 0, 0), // 반투명 검정
            };

            var lblWork = new Label
            {
                Text = "챔버 작업 중",
                Font = new Font("맑은 고딕", 36F, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 255, 200, 0),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(600, 80),
            };

            var lblSub = new Label
            {
                Text = "장비 조작이 차단되었습니다\n챔버 작업이 완료되면 아래 버튼을 눌러주세요",
                Font = new Font("맑은 고딕", 12F),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
                AutoSize = false,
                Size = new Size(600, 60),
            };

            var btnExit = new Button
            {
                Text = "작업 완료",
                Font = new Font("맑은 고딕", 16F, FontStyle.Bold),
                Size = new Size(200, 60),
                BackColor = Color.FromArgb(0, 150, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
            };
            btnExit.Click += (s2, e2) => ExitChamberWorkMode();

            // 중앙 정렬
            _chamberWorkOverlay.Resize += (s2, e2) =>
            {
                lblWork.Location = new Point((_chamberWorkOverlay.Width - lblWork.Width) / 2, _chamberWorkOverlay.Height / 2 - 120);
                lblSub.Location = new Point((_chamberWorkOverlay.Width - lblSub.Width) / 2, _chamberWorkOverlay.Height / 2 - 30);
                btnExit.Location = new Point((_chamberWorkOverlay.Width - btnExit.Width) / 2, _chamberWorkOverlay.Height / 2 + 50);
            };

            _chamberWorkOverlay.Controls.Add(lblWork);
            _chamberWorkOverlay.Controls.Add(lblSub);
            _chamberWorkOverlay.Controls.Add(btnExit);

            Controls.Add(_chamberWorkOverlay);
            _chamberWorkOverlay.BringToFront();

            // 초기 위치 설정
            lblWork.Location = new Point((_chamberWorkOverlay.Width - lblWork.Width) / 2, _chamberWorkOverlay.Height / 2 - 120);
            lblSub.Location = new Point((_chamberWorkOverlay.Width - lblSub.Width) / 2, _chamberWorkOverlay.Height / 2 - 30);
            btnExit.Location = new Point((_chamberWorkOverlay.Width - btnExit.Width) / 2, _chamberWorkOverlay.Height / 2 + 50);

            menuStrip.Enabled = false;
        }

        private void ExitChamberWorkMode()
        {
            _chamberWorkMode = false;

            if (_chamberWorkOverlay != null)
            {
                Controls.Remove(_chamberWorkOverlay);
                _chamberWorkOverlay.Dispose();
                _chamberWorkOverlay = null;
            }

            menuStrip.Enabled = true;
            LogInfo("챔버 작업 모드 해제");
        }

        #endregion

        /// <summary>
        /// 안전 워치독 트리거 — 장비 자동 정지
        /// </summary>
        private void SafetyWatchdog_Triggered(object sender, WatchdogAction action)
        {
            try
            {
                switch (action)
                {
                    case WatchdogAction.StopTurboPump:
                        if (_turboPump?.IsConnected == true && _turboPump.Status?.IsRunning == true)
                        {
                            _turboPump.Stop();
                            LogError("[워치독] 터보펌프 자동 정지 실행");
                            if (InvokeRequired)
                                BeginInvoke(new Action(() => ForceToggleButtonAppearance("turbopump", false)));
                            else
                                ForceToggleButtonAppearance("turbopump", false);
                        }
                        break;

                    case WatchdogAction.StopHeater:
                        if (_tempController?.IsConnected == true)
                        {
                            _tempController.Stop(1);
                            LogError("[워치독] 히터 CH1 자동 정지 실행");
                            if (InvokeRequired)
                                BeginInvoke(new Action(() => ForceToggleButtonAppearance("ch1", false)));
                            else
                                ForceToggleButtonAppearance("ch1", false);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                LogError($"[워치독] 장비 정지 실행 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// AutoRun 중 Main 탭 잠금/해제
        /// </summary>
        private void SetMainTabLocked(bool locked)
        {
            // Main 탭의 장비 제어 컨트롤 비활성화
            if (tabPage1 != null)
            {
                foreach (Control ctrl in tabPage1.Controls)
                    SetControlsEnabled(ctrl, !locked);
            }

            // 메뉴바 비활성화 (설정 변경 방지)
            if (menuStrip != null)
                menuStrip.Enabled = !locked;
        }

        private void SetControlsEnabled(Control parent, bool enabled)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is Button || ctrl is NumericUpDown || ctrl is ComboBox || ctrl is CheckBox)
                    ctrl.Enabled = enabled;
                if (ctrl.HasChildren)
                    SetControlsEnabled(ctrl, enabled);
            }
        }

        /// <summary>비밀번호 확인 다이얼로그</summary>
        private bool ShowPasswordDialog(string title)
        {
            if (_autoRunLock == null || !_autoRunLock.HasPassword)
                return true;

            using var dlg = new Form
            {
                Text = title,
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            };

            var lbl = new Label { Text = "비밀번호:", Location = new Point(15, 20), Size = new Size(70, 20) };
            var txt = new TextBox { Location = new Point(90, 18), Size = new Size(180, 23), PasswordChar = '*' };
            var btnOk = new Button { Text = "확인", Location = new Point(110, 55), Size = new Size(75, 30), DialogResult = DialogResult.OK };
            var btnCancel = new Button { Text = "취소", Location = new Point(195, 55), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            if (dlg.ShowDialog(this) != DialogResult.OK)
                return false;

            if (_autoRunLock.VerifyPassword(txt.Text))
                return true;

            MessageBox.Show("비밀번호가 올바르지 않습니다.", "인증 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        /// <summary>
        /// ReadOnly 텍스트박스의 TabStop/커서를 재귀적으로 비활성화
        /// </summary>
        private void DisableReadOnlyTextBoxFocus(Control parent)
        {
            foreach (Control ctrl in parent.Controls)
            {
                if (ctrl is TextBox tb && tb.ReadOnly)
                {
                    tb.TabStop = false;
                    tb.GotFocus += (s, e) => { if (s is TextBox t) t.Parent?.Focus(); };
                }
                if (ctrl.HasChildren)
                    DisableReadOnlyTextBoxFocus(ctrl);
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
                AddInterlockMenu();
                AddPathSettingsMenu();
            });
        }

        private async Task InitializeCommunicationAsync()
        {
            _channelManager = SerialPortChannelManager.Instance;

            // ── 통신 설정 로드 ──
            _commPortSettings = CommPortSettings.Load() ?? CommPortSettings.CreateDefault();

            if (_commPortSettings.UseManualSettings)
            {
                // ── 수동 모드: 저장된 설정으로 직접 연결 ──
                LogInfo("[통신] 수동 포트 설정 모드");
                _detectionResult = new PortAutoDetectionService.DetectionResult
                {
                    Success = true,
                    DeviceMap = new Dictionary<string, PortAutoDetectionService.DetectedDevice>(),
                    UndetectedDevices = new List<string>(),
                    Messages = new List<string>(),
                    PortToDeviceMap = new Dictionary<string, string>()
                };

                foreach (var kvp in _commPortSettings.Devices)
                {
                    var deviceName = kvp.Key;
                    var cfg = kvp.Value;

                    if (string.IsNullOrEmpty(cfg.PortName))
                    {
                        LogWarning($"포트 미지정: {CommPortSettings.GetDeviceDisplayName(deviceName)}");
                        continue;
                    }

                    try
                    {
                        var commSettings = cfg.ToCommunicationSettings();
                        var commManager = _channelManager.CreateCommunicationManager(
                            cfg.PortName,
                            commSettings,
                            defaultExpectedResponseLength: 0,
                            defaultTimeoutMs: 500);

                        _commManagers[deviceName] = commManager;
                        _detectionResult.DeviceMap[deviceName] = new PortAutoDetectionService.DetectedDevice
                        {
                            DeviceName = deviceName,
                            PortName = cfg.PortName,
                            Settings = commSettings,
                            ExpectedResponseLength = 0,
                            TimeoutMs = 500
                        };
                        _detectionResult.PortToDeviceMap[cfg.PortName] = deviceName;

                        LogInfo($"수동 매핑: {CommPortSettings.GetDeviceDisplayName(deviceName)} → {cfg.PortName} " +
                                $"({cfg.BaudRate}/{cfg.Parity}, Addr={cfg.SlaveAddress})");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"수동 매핑 실패: {deviceName} → {cfg.PortName}: {ex.Message}");
                    }
                }
            }
            else
            {
                // ── 자동 모드: 기존 자동 감지 로직 ──
                LogInfo("[통신] 자동 포트 감지 모드");
                _portDetectionService = new PortAutoDetectionService();
                _portDetectionService.ProgressUpdated += (s, msg) => LogInfo($"[포트감지] {msg}");
                _detectionResult = await _portDetectionService.DetectAllDevicesAsync();

                foreach (var kvp in _detectionResult.DeviceMap)
                {
                    var device = kvp.Value;
                    var commManager = _channelManager.CreateCommunicationManager(
                        device.PortName,
                        device.Settings,
                        defaultExpectedResponseLength: device.ExpectedResponseLength,
                        defaultTimeoutMs: device.TimeoutMs);

                    _commManagers[device.DeviceName] = commManager;
                    LogInfo($"포트 매핑: {device.DeviceName} → {device.PortName} " +
                            $"({device.Settings.BaudRate}/{device.Settings.Parity})");
                }

                foreach (var name in _detectionResult.UndetectedDevices)
                    LogWarning($"포트 매핑 실패: {name} (장치 전원 또는 연결 상태 확인 필요)");
            }

            _channelManager.PortConnectionChanged += ChannelManager_PortConnectionChanged;
        }

        // ★ 새 코드:
        private async Task CreateDevicesAsync()
        {
            await Task.Run(() =>
            {
                if (_commManagers.ContainsKey(PortAutoDetectionService.DEVICE_IO_MODULE))
                    _ioModule = new IO_Module(_commManagers[PortAutoDetectionService.DEVICE_IO_MODULE], 1);

                if (_commManagers.ContainsKey(PortAutoDetectionService.DEVICE_DRY_PUMP))
                    _dryPump = new DryPump(_commManagers[PortAutoDetectionService.DEVICE_DRY_PUMP], "ECODRY 25 plus", 1);

                if (_commManagers.ContainsKey(PortAutoDetectionService.DEVICE_TURBO_PUMP))
                    _turboPump = new TurboPump(_commManagers[PortAutoDetectionService.DEVICE_TURBO_PUMP], "MAG W 1300", 1);

                if (_commManagers.ContainsKey(PortAutoDetectionService.DEVICE_BATH_CIRCULATOR))
                    _bathCirculator = new BathCirculator(_commManagers[PortAutoDetectionService.DEVICE_BATH_CIRCULATOR], "LK-1000", 1);

                if (_commManagers.ContainsKey(PortAutoDetectionService.DEVICE_TEMP_CONTROLLER))
                {
                    _tempController = new TempController(
                        _commManagers[PortAutoDetectionService.DEVICE_TEMP_CONTROLLER],
                        deviceAddress: 1, numChannels: 4,
                        new[] { (2, 4), (3, 4) }); // 확장모듈 2개: 슬레이브2(4ch) + 슬레이브3(4ch)
                }

                _atmSwitch = new ATMswitch();
                _piraniGauge = new PiraniGauge();
                _ionGauge = new IonGauge();

                if (_ioModule != null) _deviceList.Add(_ioModule);
                if (_dryPump != null) _deviceList.Add(_dryPump);
                if (_turboPump != null) _deviceList.Add(_turboPump);
                if (_bathCirculator != null) _deviceList.Add(_bathCirculator);
                if (_tempController != null) _deviceList.Add(_tempController);
            });

            SetupDataBindings();
        }

        // ★ 새 코드:
        private async Task ConnectDevicesAsync()
        {
            var connectTasks = new List<(IDevice device, string portName, Task<bool> task)>();

            foreach (var kvp in _detectionResult.DeviceMap)
            {
                var info = kvp.Value;
                IDevice device = kvp.Key switch
                {
                    PortAutoDetectionService.DEVICE_IO_MODULE => _ioModule,
                    PortAutoDetectionService.DEVICE_DRY_PUMP => _dryPump,
                    PortAutoDetectionService.DEVICE_TURBO_PUMP => _turboPump,
                    PortAutoDetectionService.DEVICE_BATH_CIRCULATOR => _bathCirculator,
                    PortAutoDetectionService.DEVICE_TEMP_CONTROLLER => _tempController,
                    _ => null
                };

                if (device != null)
                {
                    var task = Task.Run(() => device.Connect(info.PortName, info.Settings));
                    connectTasks.Add((device, info.PortName, task));
                }
            }

            await Task.WhenAll(connectTasks.Select(t => t.task));

            foreach (var (device, portName, task) in connectTasks)
            {
                bool connected = task.Result;
                LogInfo($"{device.DeviceName} 연결 {(connected ? "성공" : "실패")} ({portName})");
                if (!connected)
                    LogWarning($"{device.DeviceName} 연결 실패 — 재연결은 자동으로 시도됩니다.");
            }

            // 추가 AI 채널 초기화
            if (_ioModule?.IsConnected == true)
            {
                _ioModule.AdditionalAIChannel = 5;
                bool success = await _ioModule.InitializeAdditionalAIChannelAsync();
                if (success)
                    LogInfo("추가 AI 채널 5: ±10V 레인지 설정 완료");
                else
                    LogWarning("추가 AI 레인지 설정 실패 - 소프트웨어 변환만 적용");
            }

            if (_tempController?.HasExpansion == true)
                LogInfo($"TM4-N2SE 확장 모듈 통합됨 (총 {_tempController.TotalChannelCount}채널)");

            // 미감지 장치 알림
            if (_detectionResult.UndetectedDevices.Count > 0)
            {
                string msg = "다음 장치를 감지하지 못했습니다:\n\n" +
                             string.Join("\n", _detectionResult.UndetectedDevices.Select(d => $"  • {d}")) +
                             "\n\n장치 전원과 케이블 연결 상태를 확인하세요.";
                MessageBox.Show(msg, "장치 감지 실패", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        private void SetupDataBindings()
        {
            try
            {
                if (_ioModule != null)
                {
                    connectionIndicator_iomodule.DataSource = _ioModule;
                    connectionIndicator_iomodule.DataMember = "IsConnected";
                    _ioModule.PropertyChanged += Device_PropertyChanged;
                }
                if (_dryPump != null)
                {
                    connectionIndicator_drypump.DataSource = _dryPump;
                    connectionIndicator_drypump.DataMember = "IsConnected";
                    _dryPump.PropertyChanged += Device_PropertyChanged;
                }
                if (_turboPump != null)
                {
                    connectionIndicator_turbopump.DataSource = _turboPump;
                    connectionIndicator_turbopump.DataMember = "IsConnected";
                    _turboPump.PropertyChanged += Device_PropertyChanged;
                }
                if (_bathCirculator != null)
                {
                    connectionIndicator_bathcirculator.DataSource = _bathCirculator;
                    connectionIndicator_bathcirculator.DataMember = "IsConnected";
                    _bathCirculator.PropertyChanged += Device_PropertyChanged;
                }
                if (_tempController != null)
                {
                    connectionIndicator_tempcontroller.DataSource = _tempController;
                    connectionIndicator_tempcontroller.DataMember = "IsConnected";
                    _tempController.PropertyChanged += Device_PropertyChanged;
                }
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
                {
                    LogInfo($"{device.DeviceName} 연결됨");
                }
            }
        }

        private void StartServices()
        {
            SetupToggleButtons();

            _uiUpdateService = new SimplifiedUIUpdateService(this);
            _uiUpdateService.Start();

            _dataCollectionService = new OptimizedDataCollectionService(this);
            _dataCollectionService.DataUpdated += OnDataUpdated;
            _dataCollectionService.Start();

            _chillerPIDService = new ChillerPIDControlService(this);

            // 저장된 PID 설정을 UI에 반영
            numCh2Target.Value = (decimal)_chillerPIDService.TargetTemperature;
            cmbPIDChannel.SelectedIndex = _chillerPIDService.TargetChannelIndex;

            InitializeBakeoutSettings();
            InitializeSimpleRampControl();

            LogInfo("서비스 시작 완료");
        }

        private void InitializeBakeoutSettings()
        {
            _profileManager = new ThermalRampProfileManager();
            _bakeoutSettings = BakeoutSettings.Load();
            LogInfo("베이크 아웃 설정 로드 완료");
        }

        private void InitializeSimpleRampControl()
        {
            if (_tempController == null || simpleRampControl1 == null)
                return;

            simpleRampControl1.Initialize(_tempController);
            simpleRampControl1.TargetTemperatureReached += SimpleRampControl_TargetReached;
            simpleRampControl1.LogMessage += (s, msg) => LogInfo(msg);

            if (simpleRampControl1.RampController != null)
            {
                simpleRampControl1.RampController.RampCompleted += (s, e) =>
                {
                    RunOnUIThread(() => UpdateCh1TimerControls());
                };
            }

            LogInfo("SimpleRampControl 초기화 완료");
        }

        private void SimpleRampControl_TargetReached(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SimpleRampControl_TargetReached(sender, e)));
                return;
            }

            if (!simpleRampControl1.AutoStartTimerOnTargetReached)
                return;

            if (!chkCh1TimerEnabled.Checked)
                return;

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

        /// <summary>
        /// SerialPortChannel 연결 상태 변경 이벤트 핸들러.
        /// 통신 실패 3회 연속 시 자동 감지되어 이 이벤트가 발생합니다.
        /// </summary>
        private void ChannelManager_PortConnectionChanged(object sender, PortConnectionChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ChannelManager_PortConnectionChanged(sender, e)));
                return;
            }

            string deviceName = GetDeviceNameByPort(e.PortName);

            if (e.IsConnected)
            {
                LogInfo($"[{e.PortName}] {e.Message}");
                if (deviceName != null)
                    SetConnectionStatus(deviceName, true);
            }
            else
            {
                LogWarning($"[{e.PortName}] {e.Message}");

                if (deviceName != null)
                {
                    // LED 빨강으로 변경
                    SetConnectionStatus(deviceName, false);

                    // 데이터 로깅 중단
                    StopDeviceDataLogging(deviceName);
                }
            }
        }

        private string GetDeviceNameByPort(string portName)
        {
            return _detectionResult?.GetDeviceName(portName);
        }

        #endregion

        #region AutoRun

        /// <summary>
        /// 프로그램 시작 시 이전 오토런 상태가 남아있는지 확인하고 이어하기 제안
        /// </summary>
        private async void CheckAutoRunResume()
        {
            try
            {
                var snapshot = AutoRunStateSnapshot.Load();
                if (snapshot == null) return;

                // 24시간 이상 경과했으면 무효
                if (snapshot.IsExpired)
                {
                    AutoRunStateSnapshot.Clear();
                    LogInfo("이전 오토런 상태가 24시간 이상 경과하여 삭제됨");
                    return;
                }

                // 시스템 상태 감지
                var assessment = _autoRunService.DetectCurrentSystemState();

                string msg = snapshot.GetSummaryText()
                    + "\n현재 시스템 상태:\n"
                    + assessment.GetSummaryText()
                    + "\n─────────────────────────────\n"
                    + $"[예] → {assessment.RecommendedStartStep}단계({SystemStateAssessment.GetStepName(assessment.RecommendedStartStep)})부터 이어서 실행\n"
                    + "[아니오] → 이전 상태 삭제 (새로 시작 가능)";

                var dialogResult = MessageBox.Show(msg,
                    "이전 오토런 이어하기",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (dialogResult != DialogResult.Yes)
                {
                    AutoRunStateSnapshot.Clear();
                    return;
                }

                // 이어하기 실행
                int startStep = assessment.RecommendedStartStep;

                // 실험 데이터 로거 (이전 파일명에 _resumed 추가)
                string experimentName = $"{snapshot.ExperimentName}_resumed";
                _experimentDataLogger?.Dispose();
                _experimentDataLogger = new ExperimentDataLogger();
                string filePath = _experimentDataLogger.Start(experimentName);
                if (filePath != null)
                    AddAutoRunLog("DATA", $"실험 데이터 저장: {filePath}");

                // UI 초기화
                btnAutoRunStart.Enabled = false;
                btnAutoRunStop.Enabled = true;
                btnAutoRunPause.Enabled = true;
                btnAutoRunConfig.Enabled = false;
                listViewAutoRunLog.Items.Clear();

                // 이전 경과시간 복원
                _autoRunElapsedSeconds = snapshot.AutoRunElapsedSeconds
                    + (int)(DateTime.Now - snapshot.SavedAt).TotalSeconds;
                _experimentElapsedSeconds = snapshot.IsExperimentTimerRunning
                    ? snapshot.ExperimentElapsedSeconds
                        + (int)(DateTime.Now - snapshot.SavedAt).TotalSeconds
                    : 0;

                ClearAutoRunCharts();
                _lblAutoRunBanner.Visible = false;
                tableLayoutPanel3.BackColor = SystemColors.Control;
                _autoRunTimer.Start();

                _autoRunService.ExperimentName = experimentName;

                AddAutoRunLog("RESUME", $"이전 오토런 이어하기 — {startStep}단계({SystemStateAssessment.GetStepName(startStep)})부터");
                LogInfo($"오토런 이어하기 시작 (이전: {snapshot.CurrentStepNumber}단계, 현재: {startStep}단계)");

                if (!await _autoRunService.StartAsync(startStep))
                {
                    MessageBox.Show("AutoRun 이어하기 실패", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _experimentDataLogger?.Stop();
                    AutoRunStateSnapshot.Clear();
                    UpdateAutoRunUI();
                }
            }
            catch (Exception ex)
            {
                LogError($"오토런 이어하기 확인 실패: {ex.Message}");
                AutoRunStateSnapshot.Clear();
            }
        }

        private void InitializeAutoRun()
        {
            try
            {
                _autoRunConfig = LoadAutoRunConfiguration() ?? new AutoRunConfiguration();
                _interlockConfig = InterlockConfiguration.LoadFromFile();
                _safetyInterlock = new SafetyInterlockService(this, _interlockConfig);
                _autoRunLock = new AutoRunLock();
                _safetyInterlock.InterlockViolation += (s, msg) => LogWarning(msg);
                _safetyInterlock.WatchdogTriggered += SafetyWatchdog_Triggered;
                // IO_Module에 최상단 인터락 주입
                if (_ioModule != null) _ioModule.SafetyInterlock = _safetyInterlock;

                _tempCalibrationConfig = TempCalibrationConfig.LoadFromFile();
                ApplyCalibrationToChannels();

                _autoRunService = new AutoRunService(this, _autoRunConfig);
                _autoRunService.StateChanged += OnAutoRunStateChanged;
                _autoRunService.ProgressUpdated += OnAutoRunProgressUpdated;
                _autoRunService.ErrorOccurred += OnAutoRunErrorOccurred;
                _autoRunService.Completed += OnAutoRunCompleted;

                _autoRunTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _autoRunTimer.Tick += AutoRunTimer_Tick;

                _chartTimer = new System.Windows.Forms.Timer { Interval = 1000 };
                _chartTimer.Tick += ChartTimer_Tick;
                _chartTimer.Start();

                InitializeAutoRunUI();
                InitializeAutoRunBanner();
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
            _panelAutoRun = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            tabPageAutoRun.Controls.Add(_panelAutoRun);

            // ── 상태 그룹 (좌측) ──
            _groupBoxAutoRunStatus = new GroupBox
            {
                Text = "AutoRun 상태",
                Location = new Point(10, 10),
                Size = new Size(500, 110)
            };
            _panelAutoRun.Controls.Add(_groupBoxAutoRunStatus);
            var groupBoxStatus = _groupBoxAutoRunStatus;

            lblAutoRunStatus = new Label
            {
                Location = new Point(10, 22),
                Size = new Size(480, 20),
                Text = "상태: 대기 중",
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold)
            };
            lblAutoRunStep = new Label
            {
                Location = new Point(10, 45),
                Size = new Size(480, 20),
                Text = "단계: -",
                Font = new Font("맑은 고딕", 9F)
            };
            progressBarAutoRun = new ProgressBar { Location = new Point(10, 68), Size = new Size(400, 18), Style = ProgressBarStyle.Continuous };
            lblAutoRunProgress = new Label { Location = new Point(415, 68), Size = new Size(75, 18), Text = "0%", TextAlign = ContentAlignment.MiddleLeft };
            lblAutoRunElapsedTime = new Label { Location = new Point(10, 90), Size = new Size(180, 18), Text = "경과: 00:00:00" };
            lblAutoRunRemainingTime = new Label { Location = new Point(200, 90), Size = new Size(200, 18), Text = "남은: --:--:--" };

            groupBoxStatus.Controls.AddRange(new Control[]
            {
                lblAutoRunStatus, lblAutoRunStep, progressBarAutoRun,
                lblAutoRunProgress, lblAutoRunElapsedTime, lblAutoRunRemainingTime
            });

            // ── 제어 그룹 (우측, 독립) ──
            GroupBox groupBoxControl = new GroupBox
            {
                Text = "제어",
                Location = new Point(520, 10),
                Size = new Size(250, 110)
            };
            _panelAutoRun.Controls.Add(groupBoxControl);

            btnAutoRunStart = new Button { Location = new Point(10, 22), Size = new Size(110, 28), Text = "시작" };
            btnAutoRunStart.Click += BtnAutoRunStart_Click;
            btnAutoRunStop = new Button { Location = new Point(130, 22), Size = new Size(110, 28), Text = "중지", Enabled = false };
            btnAutoRunStop.Click += BtnAutoRunStop_Click;
            btnAutoRunPause = new Button { Location = new Point(10, 55), Size = new Size(110, 28), Text = "일시정지", Enabled = false };
            btnAutoRunPause.Click += BtnAutoRunPause_Click;
            btnAutoRunResume = new Button { Location = new Point(130, 55), Size = new Size(110, 28), Text = "재개", Enabled = false };
            btnAutoRunResume.Click += BtnAutoRunResume_Click;
            btnAutoRunConfig = new Button { Location = new Point(70, 86), Size = new Size(110, 22), Text = "설정", Font = new Font("맑은 고딕", 8F) };
            btnAutoRunConfig.Click += BtnAutoRunConfig_Click;

            groupBoxControl.Controls.AddRange(new Control[]
            {
                btnAutoRunStart, btnAutoRunStop, btnAutoRunPause, btnAutoRunResume, btnAutoRunConfig
            });

            // ── 실시간 온도 조정 그룹 (실행 중 변경 가능) ──
            _grpLiveAdjust = new GroupBox
            {
                Text = "실시간 조정",
                Location = new Point(780, 10),
                Size = new Size(360, 110),
                Visible = false
            };
            _panelAutoRun.Controls.Add(_grpLiveAdjust);

            var lblAdj1 = new Label { Text = "목표(°C):", Location = new Point(10, 25), Size = new Size(60, 20), TextAlign = ContentAlignment.MiddleRight };
            _numAdjTarget = new NumericUpDown { Location = new Point(75, 23), Size = new Size(65, 23), DecimalPlaces = 1, Minimum = 30, Maximum = 400, Value = 200 };
            var lblAdj2 = new Label { Text = "상한(°C):", Location = new Point(145, 25), Size = new Size(60, 20), TextAlign = ContentAlignment.MiddleRight };
            _numAdjMaxTemp = new NumericUpDown { Location = new Point(210, 23), Size = new Size(65, 23), DecimalPlaces = 1, Minimum = 50, Maximum = 500, Value = 230 };
            var lblAdj3 = new Label { Text = "허용오차:", Location = new Point(10, 55), Size = new Size(60, 20), TextAlign = ContentAlignment.MiddleRight };
            _numAdjTolerance = new NumericUpDown { Location = new Point(75, 53), Size = new Size(65, 23), DecimalPlaces = 1, Minimum = 0.1M, Maximum = 10, Value = 1, Increment = 0.1M };
            var lblAdj4 = new Label { Text = "안정화(초):", Location = new Point(145, 55), Size = new Size(65, 20), TextAlign = ContentAlignment.MiddleRight };
            _numAdjStabilization = new NumericUpDown { Location = new Point(210, 53), Size = new Size(65, 23), Minimum = 0, Maximum = 3600, Value = 600, Increment = 30 };
            _btnAdjApply = new Button { Text = "적용", Location = new Point(285, 23), Size = new Size(65, 53), BackColor = Color.LightYellow };
            _btnAdjApply.Click += BtnLiveAdjust_Click;

            _grpLiveAdjust.Controls.AddRange(new Control[]
            {
                lblAdj1, _numAdjTarget, lblAdj2, _numAdjMaxTemp,
                lblAdj3, _numAdjTolerance, lblAdj4, _numAdjStabilization, _btnAdjApply
            });

            var lblAdjNote = new Label
            {
                Text = "※ 적용 버튼을 눌러야 반영됩니다",
                Location = new Point(10, 82), Size = new Size(340, 18),
                Font = new Font("맑은 고딕", 7.5F), ForeColor = Color.Gray
            };
            _grpLiveAdjust.Controls.Add(lblAdjNote);

            // ── 실시간 측정값 그룹 ──
            _groupBoxMeasurements = new GroupBox
            {
                Text = "실시간 측정값",
                Location = new Point(10, 125),
                Size = new Size(1130, 98),
                Visible = false
            };
            _panelAutoRun.Controls.Add(_groupBoxMeasurements);

            _lblMeasPressure = new Label
            {
                Location = new Point(10, 18),
                Size = new Size(360, 18),
                Text = "압력: --",
                Font = new Font("맑은 고딕", 9F)
            };
            _lblMeasCH1 = new Label
            {
                Location = new Point(380, 18),
                Size = new Size(740, 18),
                Text = "온도: --",
                Font = new Font("맑은 고딕", 8F)
            };
            _lblMeasPump = new Label
            {
                Location = new Point(10, 38),
                Size = new Size(360, 18),
                Text = "펌프: --",
                Font = new Font("맑은 고딕", 9F)
            };
            _lblMeasSample = new Label
            {
                Location = new Point(380, 38),
                Size = new Size(740, 18),
                Text = "확장: --",
                Font = new Font("맑은 고딕", 8F)
            };
            _lblMeasConfig = new Label
            {
                Location = new Point(10, 58),
                Size = new Size(1110, 18),
                Text = "",
                Font = new Font("맑은 고딕", 8F),
                ForeColor = Color.DimGray
            };
            _lblMeasThermal = new Label
            {
                Location = new Point(10, 76),
                Size = new Size(1110, 18),
                Text = "",
                Font = new Font("맑은 고딕", 8F),
                ForeColor = Color.DarkSlateBlue
            };
            _groupBoxMeasurements.Controls.AddRange(new Control[]
            {
                _lblMeasPressure, _lblMeasCH1, _lblMeasPump, _lblMeasSample, _lblMeasConfig, _lblMeasThermal
            });

            // ── 시퀀스 진행 표시 ──
            GroupBox groupBoxSequence = new GroupBox
            {
                Text = "시퀀스 진행",
                Location = new Point(10, 228),
                Size = new Size(1130, 70)
            };
            _panelAutoRun.Controls.Add(groupBoxSequence);

            string[] stepNames = { "초기화", "진공준비", "드라이", "터보", "IG", "고진공", "히터", "실험", "종료" };
            _stepPanels = new Panel[9];
            _stepLabels = new Label[9];

            int stepWidth = 115;
            int gap = 6;
            int startX = 10;
            int stepY = 22;

            for (int i = 0; i < 9; i++)
            {
                _stepPanels[i] = new Panel
                {
                    Location = new Point(startX + i * (stepWidth + gap), stepY),
                    Size = new Size(stepWidth, 36),
                    BackColor = Color.FromArgb(230, 230, 230),
                    BorderStyle = BorderStyle.FixedSingle
                };

                _stepLabels[i] = new Label
                {
                    Dock = DockStyle.Fill,
                    Text = $"{i + 1}. {stepNames[i]}",
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("맑은 고딕", 8F),
                    ForeColor = Color.Gray
                };
                _stepPanels[i].Controls.Add(_stepLabels[i]);
                groupBoxSequence.Controls.Add(_stepPanels[i]);
            }

            // ── 로그 그룹 ──
            GroupBox groupBoxLog = new GroupBox
            {
                Text = "실행 로그",
                Location = new Point(10, 303),
                Size = new Size(1130, 200)
            };

            listViewAutoRunLog = new ListView
            {
                Location = new Point(10, 22),
                Size = new Size(1110, 170),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listViewAutoRunLog.Columns.Add("시간", 160);
            listViewAutoRunLog.Columns.Add("상태", 140);
            listViewAutoRunLog.Columns.Add("메시지", 790);
            groupBoxLog.Controls.Add(listViewAutoRunLog);
            _panelAutoRun.Controls.Add(groupBoxLog);

            // ── 실시간 차트 초기화 ──
            InitializeAutoRunCharts();
        }

        private void InitializeAutoRunCharts()
        {
            // ScottPlot 기본 폰트를 한글 지원 폰트로 변경
            ScottPlot.Fonts.Default = "Malgun Gothic";

            // 데이터 버퍼 초기화
            int totalTempChannels = _tempController?.TotalChannelCount ?? 12;
            _tempChartBuffers = new ChartDataBuffer[totalTempChannels];
            for (int i = 0; i < totalTempChannels; i++)
                _tempChartBuffers[i] = new ChartDataBuffer(3600);
            _pressureChartBuffer = new ChartDataBuffer(3600);

            // ── 온도 차트 ──
            _groupBoxTempChart = new GroupBox
            {
                Text = "Temperature — M1~4 / E1~4",
                Location = new Point(10, 508),
                Size = new Size(560, 310)
            };

            _formsPlotTemp = new FormsPlot
            {
                Location = new Point(5, 18),
                Size = new Size(550, 285)
            };

            ConfigureTemperatureChart();
            _formsPlotTemp.MouseDown += ChartPlot_MouseDown;
            _formsPlotTemp.MouseDoubleClick += ChartPlot_DoubleClick;
            _groupBoxTempChart.Controls.Add(_formsPlotTemp);
            _panelAutoRun.Controls.Add(_groupBoxTempChart);

            // ── 압력 차트 ──
            _groupBoxPressChart = new GroupBox
            {
                Text = "압력 추이 (Pressure)",
                Location = new Point(580, 508),
                Size = new Size(560, 310)
            };

            _formsPlotPressure = new FormsPlot
            {
                Location = new Point(5, 18),
                Size = new Size(550, 285)
            };

            ConfigurePressureChart();
            _formsPlotPressure.MouseDown += ChartPlot_MouseDown;
            _formsPlotPressure.MouseDoubleClick += ChartPlot_DoubleClick;
            _groupBoxPressChart.Controls.Add(_formsPlotPressure);
            _panelAutoRun.Controls.Add(_groupBoxPressChart);
        }

        private void ConfigureTemperatureChart()
        {
            var plot = _formsPlotTemp.Plot;
            plot.Title("Temperature");
            plot.YLabel("°C");
            plot.XLabel("Time");
            ApplyDateTimeAxis24H(plot);
            plot.Axes.Left.TickLabelStyle.FontName = "Malgun Gothic";
        }

        private void ConfigurePressureChart()
        {
            var plot = _formsPlotPressure.Plot;
            plot.Title("Pressure (log)");
            plot.YLabel("Torr");
            plot.XLabel("Time");
            ApplyDateTimeAxis24H(plot);
            plot.Axes.Left.TickLabelStyle.FontName = "Malgun Gothic";
        }

        /// <summary>
        /// X축을 24시간 형식(HH:mm:ss)으로 설정 — AM/PM 표시 제거
        /// </summary>
        private static void ApplyDateTimeAxis24H(ScottPlot.Plot plot)
        {
            var tickGen = new ScottPlot.TickGenerators.DateTimeAutomatic
            {
                LabelFormatter = dt => dt.ToString("HH:mm:ss")
            };
            plot.Axes.Bottom.TickGenerator = tickGen;
            plot.Axes.Bottom.TickLabelStyle.FontName = "Malgun Gothic";
        }

        private void BtnLiveAdjust_Click(object sender, EventArgs e)
        {
            if (_autoRunConfig == null || _autoRunService == null || !_autoRunService.IsRunning)
                return;

            double newTarget = (double)_numAdjTarget.Value;
            double newMax = (double)_numAdjMaxTemp.Value;
            double newTol = (double)_numAdjTolerance.Value;
            int newStab = (int)_numAdjStabilization.Value;

            if (newMax <= newTarget)
            {
                MessageBox.Show("CH1 상한은 목표 온도보다 높아야 합니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"실행 중 설정을 변경합니다:\n\n" +
                $"목표 온도: {_autoRunConfig.BakeoutTargetTemperature:F1} → {newTarget:F1}°C\n" +
                $"CH1 상한: {_autoRunConfig.BakeoutHeaterMaxTemperature:F1} → {newMax:F1}°C\n" +
                $"허용오차: {_autoRunConfig.BakeoutTolerance:F1} → {newTol:F1}°C\n" +
                $"안정화 시간: {_autoRunConfig.BakeoutStabilizationSeconds} → {newStab}초\n\n" +
                $"즉시 적용됩니다. 계속하시겠습니까?",
                "실시간 설정 변경",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            string changes = "";
            if (_autoRunConfig.BakeoutTargetTemperature != newTarget)
                changes += $"목표:{_autoRunConfig.BakeoutTargetTemperature:F1}→{newTarget:F1}°C ";
            if (_autoRunConfig.BakeoutHeaterMaxTemperature != newMax)
                changes += $"상한:{_autoRunConfig.BakeoutHeaterMaxTemperature:F1}→{newMax:F1}°C ";
            if (_autoRunConfig.BakeoutTolerance != newTol)
                changes += $"허용오차:{_autoRunConfig.BakeoutTolerance:F1}→{newTol:F1}°C ";
            if (_autoRunConfig.BakeoutStabilizationSeconds != newStab)
                changes += $"안정화:{_autoRunConfig.BakeoutStabilizationSeconds}→{newStab}초 ";

            _autoRunConfig.BakeoutTargetTemperature = newTarget;
            _autoRunConfig.BakeoutHeaterMaxTemperature = newMax;
            _autoRunConfig.BakeoutTolerance = newTol;
            _autoRunConfig.BakeoutStabilizationSeconds = newStab;

            LogInfo($"[실시간 조정] {changes.Trim()}");
            AddAutoRunLog("CONFIG", $"설정 변경: {changes.Trim()}");
        }

        private void ChartPlot_MouseDown(object sender, MouseEventArgs e)
        {
            // 사용자가 차트를 클릭/드래그 → AutoScale 10초간 중지
            _chartUserInteracting = true;
            _chartLastInteraction = DateTime.Now;
        }

        private void ChartPlot_DoubleClick(object sender, MouseEventArgs e)
        {
            // 더블클릭 → AutoScale 복원
            _chartUserInteracting = false;
        }

        private void UpdateAutoRunCharts()
        {
            if (_formsPlotTemp == null || _formsPlotPressure == null) return;

            try
            {
                var now = DateTime.Now;

                // 1. 온도 데이터 샘플링
                if (_tempController?.IsConnected == true)
                {
                    var channels = _tempController.Status.ChannelStatus;
                    for (int i = 0; i < Math.Min(8, channels.Length); i++)
                    {
                        var ch = channels[i];
                        // 센서 에러 없고, 값이 유효 범위(-50~1500°C)인 경우만 수집
                        if (!string.IsNullOrEmpty(ch.SensorError)) continue;
                        double pv = ch.Dot == 1
                            ? ch.PresentValue / 10.0
                            : ch.PresentValue;
                        if (pv > -50 && pv < 1500)
                            _tempChartBuffers[i].Add(now, pv);
                    }
                }

                // 2. 압력 데이터 샘플링
                double pressure = _dataCollectionService?.GetLatestPressure() ?? 0;
                if (pressure > 0)
                    _pressureChartBuffer.Add(now, Math.Log10(pressure));

                // ★ 진단: 데이터 수집 상태 확인 (최초 1회만)
                if (_chartDiagLogged == false)
                {
                    int totalPoints = 0;
                    for (int j = 0; j < 5; j++) totalPoints += _tempChartBuffers[j].Count;
                    int pressPoints = _pressureChartBuffer.Count;
                    bool tcConn = _tempController?.IsConnected == true;
                    LogInfo($"[차트 진단] TC연결={tcConn}, 온도포인트={totalPoints}, 압력포인트={pressPoints}, " +
                        $"FormsPlot={_formsPlotTemp != null}/{_formsPlotPressure != null}");
                    _chartDiagLogged = true;
                }

                // 3. 온도 차트 재구성
                var tempPlot = _formsPlotTemp.Plot;
                tempPlot.Clear();

                ScottPlot.Color[] chColors =
                {
                    ScottPlot.Color.FromHex("#E74C3C"),  // M-1 빨강
                    ScottPlot.Color.FromHex("#3498DB"),  // M-2 파랑
                    ScottPlot.Color.FromHex("#2ECC71"),  // M-3 초록
                    ScottPlot.Color.FromHex("#E67E22"),  // M-4 주황
                    ScottPlot.Color.FromHex("#9B59B6"),  // E-1 보라
                    ScottPlot.Color.FromHex("#1ABC9C"),  // E-2 청록
                    ScottPlot.Color.FromHex("#F39C12"),  // E-3 금색
                    ScottPlot.Color.FromHex("#95A5A6")   // E-4 회색
                };
                string[] chNames = { "M-1(Heater)", "M-2", "M-3", "M-4", "E-1", "E-2", "E-3", "E-4" };

                for (int i = 0; i < 8; i++)
                {
                    var (xs, ys) = _tempChartBuffers[i].GetData();
                    if (xs.Length >= 2)
                    {
                        var scatter = tempPlot.Add.Scatter(xs, ys);
                        scatter.Color = chColors[i];
                        scatter.LegendText = chNames[i];
                        scatter.LineWidth = 2;
                        scatter.MarkerSize = 0;
                    }
                }

                // 목표 온도 수평선 추가
                if (_autoRunService?.IsRunning == true)
                {
                    double targetTemp = _autoRunService.Configuration.BakeoutTargetTemperature;
                    var hLine = tempPlot.Add.HorizontalLine(targetTemp);
                    hLine.Color = ScottPlot.Color.FromHex("#E74C3C");
                    hLine.LineWidth = 1.5f;
                    hLine.LinePattern = ScottPlot.LinePattern.Dashed;
                    hLine.LegendText = $"Target {targetTemp:F0}°C";
                }

                ApplyDateTimeAxis24H(tempPlot);
                tempPlot.Axes.Left.TickLabelStyle.FontName = "Malgun Gothic";
                tempPlot.HideLegend();
                // 사용자 조작 후 10초 경과하면 AutoScale 복원
                if (_chartUserInteracting && (DateTime.Now - _chartLastInteraction).TotalSeconds > 10)
                    _chartUserInteracting = false;
                if (!_chartUserInteracting)
                    tempPlot.Axes.AutoScale();
                _formsPlotTemp.Refresh();

                // 4. 압력 차트 재구성
                var pressPlot = _formsPlotPressure.Plot;
                pressPlot.Clear();

                var (pxs, pys) = _pressureChartBuffer.GetData();
                if (pxs.Length >= 2)
                {
                    var scatter = pressPlot.Add.Scatter(pxs, pys);
                    scatter.Color = ScottPlot.Color.FromHex("#27AE60");
                    scatter.LineWidth = 2;
                    scatter.MarkerSize = 0;
                }

                ApplyDateTimeAxis24H(pressPlot);
                // Y축: log10 값을 실제 압력(E 포맷)으로 변환 표시
                var pressTickGen = new ScottPlot.TickGenerators.NumericAutomatic();
                pressTickGen.LabelFormatter = v => Math.Pow(10, v).ToString("E1");
                pressPlot.Axes.Left.TickGenerator = pressTickGen;
                pressPlot.Axes.Left.TickLabelStyle.FontName = "Malgun Gothic";
                pressPlot.HideLegend();
                if (!_chartUserInteracting)
                    pressPlot.Axes.AutoScale();
                _formsPlotPressure.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"차트 업데이트 오류: {ex.Message}\n{ex.StackTrace}");
                // 반복 로그 방지: 1분에 1회만 로그
                var now2 = DateTime.Now;
                if (_lastChartErrorLog == default || (now2 - _lastChartErrorLog).TotalSeconds > 60)
                {
                    _lastChartErrorLog = now2;
                    LogError($"차트 업데이트 오류: {ex.Message}");
                }
            }
        }

        private void ClearAutoRunCharts()
        {
            _chartDiagLogged = false;
            if (_tempChartBuffers != null)
            {
                foreach (var buf in _tempChartBuffers)
                    buf.Clear();
            }
            _pressureChartBuffer?.Clear();

            if (_formsPlotTemp != null)
            {
                _formsPlotTemp.Plot.Clear();
                ConfigureTemperatureChart();
                _formsPlotTemp.Refresh();
            }

            if (_formsPlotPressure != null)
            {
                _formsPlotPressure.Plot.Clear();
                ConfigurePressureChart();
                _formsPlotPressure.Refresh();
            }
        }

        /// <summary>
        /// Main 탭 상단 빈 공간에 AutoRun 상태 배너 추가
        /// </summary>
        private void InitializeAutoRunBanner()
        {
            _lblAutoRunBanner = new Label
            {
                Text = "",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Padding = new Padding(8, 0, 0, 0),
                Visible = false
            };

            tableLayoutPanel3.Controls.Add(_lblAutoRunBanner, 0, 0);
        }

        /// <summary>
        /// AutoRun 배너 상태 업데이트
        /// </summary>
        private void UpdateAutoRunBanner()
        {
            if (_lblAutoRunBanner == null) return;

            var state = _autoRunService?.CurrentState ?? AutoRunState.Idle;
            bool isRunning = _autoRunService?.IsRunning ?? false;

            if (state == AutoRunState.Idle)
            {
                if (_lblAutoRunBanner.Visible)
                {
                    _lblAutoRunBanner.Visible = false;
                    tableLayoutPanel3.BackColor = SystemColors.Control;
                }
                return;
            }

            // 완료/중단/오류: 배너를 숨기지 않고 최종 상태 표시
            var finalElapsed = TimeSpan.FromSeconds(_autoRunElapsedSeconds);
            if (state == AutoRunState.Completed)
            {
                _lblAutoRunBanner.Text = $"✓ 완료  [{FmtTime(finalElapsed)}]";
                _lblAutoRunBanner.Visible = true;
                tableLayoutPanel3.BackColor = Color.FromArgb(0, 120, 60);
                return;
            }
            if (state == AutoRunState.Aborted)
            {
                _lblAutoRunBanner.Text = $"■ 중단됨  [{FmtTime(finalElapsed)}]";
                _lblAutoRunBanner.Visible = true;
                tableLayoutPanel3.BackColor = Color.FromArgb(130, 80, 0);
                return;
            }
            if (state == AutoRunState.Error)
            {
                _lblAutoRunBanner.Text = $"✗ 오류 발생  [{FmtTime(finalElapsed)}]";
                _lblAutoRunBanner.Visible = true;
                tableLayoutPanel3.BackColor = Color.FromArgb(180, 30, 30);
                return;
            }

            _lblAutoRunBanner.Visible = true;
            var elapsed = TimeSpan.FromSeconds(_autoRunElapsedSeconds);
            int stepNum = _autoRunService?.CurrentStepNumber ?? 0;
            string stepTag = stepNum > 0 ? $"[{stepNum}/9] " : "";

            string stateText;
            Color bannerColor;

            bool isBakeout = _autoRunConfig.ExperimentType == ExperimentType.Bakeout;
            string modeLabel = isBakeout ? "베이크아웃" : "AutoRun";

            // 배너용 센서 요약 (가벼운 읽기 — 타이머에서 이미 갱신된 값 재사용)
            string sensorSuffix = GetBannerSensorSuffix(state, isBakeout);

            switch (state)
            {
                case AutoRunState.Initializing:
                    stateText = $"▶ {modeLabel} {stepTag}초기화 중...";
                    bannerColor = Color.FromArgb(60, 60, 60);
                    break;
                case AutoRunState.PreparingVacuum:
                    stateText = $"▶ {modeLabel} {stepTag}진공 준비 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(60, 60, 60);
                    break;
                case AutoRunState.StartingDryPump:
                    stateText = $"▶ {modeLabel} {stepTag}드라이펌프 시작 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(0, 100, 150);
                    break;
                case AutoRunState.StartingTurboPump:
                    stateText = $"▶ {modeLabel} {stepTag}터보펌프 가속 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(0, 100, 150);
                    break;
                case AutoRunState.ActivatingIonGauge:
                    stateText = $"▶ {modeLabel} {stepTag}이온게이지 활성화 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(0, 100, 150);
                    break;
                case AutoRunState.WaitingHighVacuum:
                    stateText = $"▶ {modeLabel} {stepTag}고진공 대기 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(130, 80, 0);
                    break;
                case AutoRunState.StartingHeater:
                    stateText = $"▶ {modeLabel} {stepTag}히터 시작 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(180, 60, 0);
                    break;
                case AutoRunState.RunningExperiment:
                    int totalMinutes = isBakeout
                        ? _autoRunConfig.BakeoutHoldTimeMinutes
                        : _autoRunConfig.ExperimentDurationMinutes;
                    string expLabel = isBakeout ? "베이크아웃" : "실험";

                    if (_autoRunService.IsExperimentTimerRunning)
                    {
                        var expElapsed = DateTime.Now - _autoRunService.ExperimentStartTime;
                        var expRemaining = TimeSpan.FromMinutes(totalMinutes) - expElapsed;
                        if (expRemaining.TotalSeconds < 0) expRemaining = TimeSpan.Zero;
                        stateText = $"▶ {expLabel} {stepTag}유지 중  [{FmtTime(expElapsed)} / {FmtTime(TimeSpan.FromMinutes(totalMinutes))}]  남은: {FmtTime(expRemaining)}{sensorSuffix}";
                        bannerColor = Color.FromArgb(0, 120, 60);
                    }
                    else
                    {
                        stateText = $"▶ {expLabel} {stepTag}승온 중...  [{FmtTime(elapsed)}]{sensorSuffix}";
                        bannerColor = Color.FromArgb(180, 100, 0);
                    }
                    break;
                case AutoRunState.ShuttingDown:
                    stateText = $"▶ {modeLabel} {stepTag}종료 시퀀스 진행 중...{sensorSuffix}";
                    bannerColor = Color.FromArgb(60, 60, 60);
                    break;
                case AutoRunState.Paused:
                    stateText = $"⏸ {modeLabel} 일시정지  [{FmtTime(elapsed)}]{sensorSuffix}";
                    bannerColor = Color.FromArgb(180, 130, 0);
                    break;
                default:
                    stateText = $"▶ {modeLabel} {stepTag}실행 중  [{FmtTime(elapsed)}]";
                    bannerColor = Color.FromArgb(0, 100, 150);
                    break;
            }

            _lblAutoRunBanner.Text = stateText;
            tableLayoutPanel3.BackColor = bannerColor;
        }

        private string GetBannerSensorSuffix(AutoRunState state, bool isBakeout)
        {
            try
            {
                var parts = new System.Collections.Generic.List<string>();

                // 실험 진행 중: 온도 정보 추가
                if (state == AutoRunState.RunningExperiment || state == AutoRunState.StartingHeater)
                {
                    if (isBakeout && _tempController?.IsConnected == true)
                    {
                        int monCh = _autoRunConfig.BakeoutMonitorChannel;
                        if (monCh >= 1 && monCh <= 5)
                        {
                            var chSt = _tempController.Status.ChannelStatus[monCh - 1];
                            double pv = chSt.Dot == 1 ? chSt.PresentValue / 10.0 : chSt.PresentValue;
                            parts.Add($"CH{monCh}: {pv:F1}°C");
                        }
                    }
                    else if (!isBakeout && _tempController?.IsConnected == true)
                    {
                        var ch1St = _tempController.Status.ChannelStatus[0];
                        double ch1PV = ch1St.Dot == 1 ? ch1St.PresentValue / 10.0 : ch1St.PresentValue;
                        parts.Add($"CH1: {ch1PV:F1}°C");
                    }
                }

                // 펌프 단계: 터보 속도
                if (state == AutoRunState.StartingTurboPump && _turboPump?.IsConnected == true)
                {
                    int speed = _turboPump.Status?.CurrentSpeed ?? 0;
                    parts.Add($"터보: {speed} Hz");
                }

                // 대부분의 단계: 압력
                double pressure = _dataCollectionService?.GetLatestPressure() ?? 0;
                if (pressure > 0)
                    parts.Add($"{pressure:E1} Torr");

                return parts.Count > 0 ? "  |  " + string.Join("  |  ", parts) : "";
            }
            catch
            {
                return "";
            }
        }

        private async void BtnAutoRunStart_Click(object sender, EventArgs e)
        {
            // ── 1. 시스템 상태 감지 ──
            var assessment = _autoRunService.DetectCurrentSystemState();
            int startStep = 1;

            if (assessment.RecommendedStartStep > 1)
            {
                // 이미 진행된 상태가 감지됨 → 사용자에게 선택권 제공
                var msg = assessment.GetSummaryText()
                    + "\n─────────────────────────────\n"
                    + $"[예] → {assessment.RecommendedStartStep}단계({SystemStateAssessment.GetStepName(assessment.RecommendedStartStep)})부터 시작\n"
                    + "[아니오] → 1단계(처음)부터 시작\n"
                    + "[취소] → 시작하지 않음";

                var dialogResult = MessageBox.Show(msg,
                    "시스템 상태 감지",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Information);

                if (dialogResult == DialogResult.Cancel)
                    return;

                startStep = (dialogResult == DialogResult.Yes)
                    ? assessment.RecommendedStartStep
                    : 1;
            }

            // ── 2. 실험 이름 입력 ──
            string experimentName = Microsoft.VisualBasic.Interaction.InputBox(
                "실험 이름을 입력하세요.\n(파일명에 사용됩니다)",
                "실험 데이터 파일 이름",
                $"Experiment_{DateTime.Now:yyyyMMdd}");

            if (string.IsNullOrWhiteSpace(experimentName))
                return;

            // ── 3. 최종 확인 ──
            string startInfo = startStep > 1
                ? $"시작 단계: {startStep}단계 ({SystemStateAssessment.GetStepName(startStep)})"
                : "시작 단계: 처음부터";

            string experimentTypeInfo;
            if (_autoRunConfig.ExperimentType == ExperimentType.Bakeout)
            {
                experimentTypeInfo = $"실험 유형: 베이크아웃\n" +
                    $"목표 온도: {_autoRunConfig.BakeoutTargetTemperature}°C\n" +
                    $"승온 속도: {_autoRunConfig.BakeoutRampRate:F0}°C/h\n" +
                    $"모니터 채널: {_autoRunConfig.GetBakeoutMonitorLabel()} (MAX 기준)\n" +
                    $"CH1 안전 상한: {_autoRunConfig.BakeoutHeaterMaxTemperature}°C\n" +
                    $"CH1-샘플 최대 온도차: {(_autoRunConfig.BakeoutMaxDeltaT > 0 ? $"{_autoRunConfig.BakeoutMaxDeltaT}°C" : "제한없음")}\n" +
                    $"유지 시간: {_autoRunConfig.BakeoutHoldTimeMinutes}분\n" +
                    $"종료 동작: {(_autoRunConfig.BakeoutEndAction == BakeoutEndAction.HeaterOff ? "히터 OFF (전체 셧다운)" : _autoRunConfig.BakeoutEndAction == BakeoutEndAction.MaintainTemperature ? "온도 유지" : "알림만")}";
            }
            else
            {
                experimentTypeInfo = $"실험 유형: 탈가스율 측정\n" +
                    $"목표 온도: {_autoRunConfig.HeaterCh1SetTemperature}°C\n" +
                    $"실험 시간: {_autoRunConfig.ExperimentDurationMinutes}분";
            }

            if (MessageBox.Show(
                $"AutoRun을 시작하시겠습니까?\n\n실험명: {experimentName}\n{experimentTypeInfo}\n{startInfo}",
                "AutoRun 시작 확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            // ── 4. 실험 데이터 로거 시작 ──
            _experimentDataLogger?.Dispose();
            _experimentDataLogger = new ExperimentDataLogger();
            string filePath = _experimentDataLogger.Start(experimentName);
            if (filePath != null)
                AddAutoRunLog("DATA", $"실험 데이터 저장: {filePath}");

            // ── 5. AutoRun 시작 ──
            btnAutoRunStart.Enabled = false;
            btnAutoRunStop.Enabled = true;
            btnAutoRunPause.Enabled = true;
            btnAutoRunConfig.Enabled = false;
            listViewAutoRunLog.Items.Clear();
            _autoRunElapsedSeconds = 0;
            _experimentElapsedSeconds = 0;
            ClearAutoRunCharts();
            // 이전 최종 상태 배너 리셋
            _lblAutoRunBanner.Visible = false;
            tableLayoutPanel3.BackColor = SystemColors.Control;
            _autoRunTimer.Start();
            _autoRunLock?.Lock();
            SetMainTabLocked(true);

            _autoRunService.ExperimentName = experimentName;

            if (startStep > 1)
                AddAutoRunLog("RESUME", $"{startStep}단계({SystemStateAssessment.GetStepName(startStep)})부터 시작");

            if (!await _autoRunService.StartAsync(startStep))
            {
                MessageBox.Show("AutoRun 시작 실패", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _experimentDataLogger?.Stop();
                UpdateAutoRunUI();
            }
        }

        private async void BtnAutoRunStop_Click(object sender, EventArgs e)
        {
            // 비밀번호 확인
            if (_autoRunLock?.HasPassword == true && !ShowPasswordDialog("AutoRun 중지 인증"))
                return;

            using (var dlg = new Form
            {
                Text = "AutoRun 중지",
                Size = new Size(380, 220),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false, MinimizeBox = false
            })
            {
                var lbl = new Label
                {
                    Text = "AutoRun을 중지합니다.\n종료 방식을 선택하세요:",
                    Location = new Point(15, 15), Size = new Size(340, 35)
                };
                var btnShutdown = new Button
                {
                    Text = "종료 시퀀스 실행\n(냉각→벤트→펌프 정지)",
                    Location = new Point(15, 55), Size = new Size(165, 50),
                    BackColor = Color.LightCoral
                };
                var btnKeep = new Button
                {
                    Text = "현재 상태 유지\n(수동 제어 전환)",
                    Location = new Point(190, 55), Size = new Size(165, 50),
                    BackColor = Color.LightYellow
                };
                var btnHeaterOff = new Button
                {
                    Text = "히터만 정지\n(펌프/밸브 유지)",
                    Location = new Point(15, 115), Size = new Size(165, 50),
                    BackColor = Color.LightBlue
                };
                var btnCancel = new Button
                {
                    Text = "취소\n(계속 실행)",
                    Location = new Point(190, 115), Size = new Size(165, 50),
                    DialogResult = DialogResult.Cancel
                };

                string choice = null;
                btnShutdown.Click += (s2, e2) => { choice = "shutdown"; dlg.Close(); };
                btnKeep.Click += (s2, e2) => { choice = "keep"; dlg.Close(); };
                btnHeaterOff.Click += (s2, e2) => { choice = "heater_off"; dlg.Close(); };
                btnCancel.Click += (s2, e2) => dlg.Close();

                dlg.Controls.AddRange(new Control[] { lbl, btnShutdown, btnKeep, btnHeaterOff, btnCancel });
                dlg.CancelButton = btnCancel;
                dlg.ShowDialog(this);

                if (choice == null) return; // 취소

                // AutoRun 중지 + 백그라운드 태스크 완료 대기
                _autoRunService.Stop();
                _autoRunTimer.Stop();
                btnAutoRunStop.Enabled = false;
                btnAutoRunStop.Text = "중지 중...";

                await _autoRunService.WaitForStopAsync();

                StopCh1Timer();
                StopExperimentDataLogger("사용자 중지");

                switch (choice)
                {
                    case "shutdown":
                        AddAutoRunLog("SHUTDOWN", "사용자 중지 → 종료 시퀀스 실행 중...");
                        LogInfo("사용자 중지 후 종료 시퀀스 시작");
                        try
                        {
                            await _autoRunService.RunShutdownSequenceAsync();
                            AddAutoRunLog("SHUTDOWN", "종료 시퀀스 완료");
                            LogInfo("종료 시퀀스 정상 완료");
                        }
                        catch (Exception ex)
                        {
                            LogError($"종료 시퀀스 오류: {ex.Message}");
                            AddAutoRunLog("ERROR", $"종료 시퀀스 오류: {ex.Message}");
                        }
                        break;

                    case "heater_off":
                        AddAutoRunLog("STOP", "히터 정지 (펌프/밸브 유지)");
                        LogInfo("히터 CH1 정지 — 펌프/밸브 현재 상태 유지");
                        try
                        {
                            if (_tempController?.IsConnected == true)
                                _tempController.Stop(1);
                        }
                        catch (Exception ex)
                        {
                            LogError($"히터 정지 오류: {ex.Message}");
                        }
                        break;

                    case "keep":
                        AddAutoRunLog("STOP", "즉시 중지 (현재 상태 유지)");
                        LogInfo("즉시 중지 — 모든 장비 현재 상태 유지");
                        break;
                }

                btnAutoRunStop.Text = "중지";
                _autoRunLock?.Unlock();
                SetMainTabLocked(false);
                UpdateAutoRunUI();
            }
        }

        private void BtnAutoRunPause_Click(object sender, EventArgs e)
        {
            _autoRunService.Pause();
            UpdateAutoRunUI();
        }

        private void BtnAutoRunResume_Click(object sender, EventArgs e)
        {
            _autoRunService.Resume();
            UpdateAutoRunUI();
        }

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

        private void ChartTimer_Tick(object sender, EventArgs e)
        {
            // AutoRun 비실행 중 + AutoRun 탭 선택 시에만 차트 업데이트
            if (tabControlMain.SelectedTab == tabPageAutoRun && !(_autoRunService?.IsRunning ?? false))
                UpdateAutoRunCharts();
        }

        private void AutoRunTimer_Tick(object sender, EventArgs e)
        {
            if (!_autoRunService.IsRunning)
                return;

            bool isPaused = _autoRunService.IsPaused;

            // 일시정지 중에는 경과 시간을 증가시키지 않음
            if (!isPaused)
            {
                _autoRunElapsedSeconds++;
            }
            lblAutoRunElapsedTime.Text = isPaused
                ? $"경과: {FmtTime(TimeSpan.FromSeconds(_autoRunElapsedSeconds))} (일시정지)"
                : $"경과: {FmtTime(TimeSpan.FromSeconds(_autoRunElapsedSeconds))}";

            // 30초마다 상태 스냅샷 저장 (재시작 시 이어하기용)
            if (_autoRunElapsedSeconds % 30 == 0)
                _autoRunService?.SaveStateSnapshot();

            // 실시간 측정값 업데이트 (일시정지 중에도 표시)
            UpdateAutoRunMeasurements();

            // 실시간 진행률 갱신 (실험 단계에서 시간 기반)
            if (_autoRunService.CurrentState == AutoRunState.RunningExperiment
                && _autoRunService.IsExperimentTimerRunning)
            {
                int totalMin = _autoRunConfig?.ExperimentType == ExperimentType.Bakeout
                    ? _autoRunConfig.BakeoutHoldTimeMinutes
                    : _autoRunConfig?.ExperimentDurationMinutes ?? 1;
                var expElapsed = DateTime.Now - _autoRunService.ExperimentStartTime;
                double expPct = Math.Min(100, expElapsed.TotalMinutes / totalMin * 100);
                double overall = 27.0 + expPct * 0.63;
                progressBarAutoRun.Value = (int)Math.Max(0, Math.Min(100, overall));
                lblAutoRunProgress.Text = $"{overall:F0}%";
            }

            // 실시간 차트 업데이트
            UpdateAutoRunCharts();

            // AutoRun 배너 업데이트
            UpdateAutoRunBanner();

            if (_autoRunService.CurrentState == AutoRunState.RunningExperiment)
            {
                if (_autoRunService.IsExperimentTimerRunning)
                {
                    // 실제 시간 기반으로 남은 시간 계산 (카운터 불일치 방지)
                    int totalExperimentMinutes = _autoRunConfig.ExperimentType == ExperimentType.Bakeout
                        ? _autoRunConfig.BakeoutHoldTimeMinutes
                        : _autoRunConfig.ExperimentDurationMinutes;
                    var expElapsed = DateTime.Now - _autoRunService.ExperimentStartTime;
                    var expRemaining = TimeSpan.FromMinutes(totalExperimentMinutes) - expElapsed;
                    if (expRemaining.TotalSeconds < 0) expRemaining = TimeSpan.Zero;
                    _experimentElapsedSeconds = (int)expElapsed.TotalSeconds;
                    lblAutoRunRemainingTime.Text = $"남은: {FmtTime(expRemaining)}";
                }
                else
                {
                    lblAutoRunRemainingTime.Text = isPaused ? "남은: 일시정지" : "남은: 승온 대기중...";
                }
            }
        }

        private void OnAutoRunStateChanged(object sender, AutoRunStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<AutoRunStateChangedEventArgs>(OnAutoRunStateChanged), sender, e);
                return;
            }
            lblAutoRunStatus.Text = $"상태: {GetAutoRunStateText(e.CurrentState)}";
            AddAutoRunLog(e.CurrentState.ToString(), e.Message ?? GetAutoRunStateText(e.CurrentState));

            // 실험 시작 시 실험 경과시간 리셋
            if (e.CurrentState == AutoRunState.RunningExperiment && e.PreviousState != AutoRunState.Paused)
            {
                _experimentElapsedSeconds = 0;
            }

            UpdateStepSequenceDisplay(e.CurrentState);
            UpdateAutoRunUI();
        }

        private void OnAutoRunProgressUpdated(object sender, AutoRunProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<AutoRunProgressEventArgs>(OnAutoRunProgressUpdated), sender, e);
                return;
            }

            int step = _autoRunService?.CurrentStepNumber ?? 0;
            double displayProgress = e.OverallProgress;

            // 실험 단계(8): 시간 기반 진행률로 대체 (더 정확)
            if (_autoRunService?.CurrentState == AutoRunState.RunningExperiment
                && _autoRunService.IsExperimentTimerRunning)
            {
                int totalMin = _autoRunConfig?.ExperimentType == ExperimentType.Bakeout
                    ? _autoRunConfig.BakeoutHoldTimeMinutes
                    : _autoRunConfig?.ExperimentDurationMinutes ?? 1;
                var expElapsed = DateTime.Now - _autoRunService.ExperimentStartTime;
                double expProgress = Math.Min(100, expElapsed.TotalMinutes / totalMin * 100);

                // 전체 진행률: 단계 1~7 가중치(27%) + 실험 진행률(63%) + 종료(10%)
                displayProgress = 27.0 + expProgress * 0.63;
            }

            progressBarAutoRun.Value = (int)Math.Max(0, Math.Min(100, displayProgress));
            lblAutoRunProgress.Text = $"{displayProgress:F0}%";
            lblAutoRunStep.Text = step > 0 ? $"[{step}/9] {e.Message}" : e.Message;
        }

        private void OnAutoRunErrorOccurred(object sender, AutoRunErrorEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<AutoRunErrorEventArgs>(OnAutoRunErrorOccurred), sender, e);
                return;
            }
            AddAutoRunLog("ERROR", e.ErrorMessage, Color.Red);
            MessageBox.Show($"AutoRun 오류:\n{e.ErrorMessage}", "오류",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnAutoRunCompleted(object sender, AutoRunCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new EventHandler<AutoRunCompletedEventArgs>(OnAutoRunCompleted), sender, e);
                return;
            }
            _autoRunTimer.Stop();
            StopExperimentDataLogger(e.IsSuccess ? "정상 완료" : "중단됨");
            UpdateStepSequenceDisplay(e.IsSuccess ? AutoRunState.Completed : AutoRunState.Aborted);

            AddAutoRunLog("COMPLETE",
                e.IsSuccess ? "정상 완료" : "중단됨",
                e.IsSuccess ? Color.Green : Color.Orange);

            if (!string.IsNullOrEmpty(e.Summary))
            {
                MessageBox.Show(e.Summary, "완료",
                    MessageBoxButtons.OK,
                    e.IsSuccess ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            _autoRunLock?.Unlock();
            SetMainTabLocked(false);
            UpdateAutoRunUI();
        }

        private void UpdateAutoRunUI()
        {
            bool isRunning = _autoRunService?.IsRunning ?? false;
            bool isPaused = _autoRunService?.IsPaused ?? false;

            // 실험 유형 표시
            if (_groupBoxAutoRunStatus != null && _autoRunConfig != null)
            {
                string expType = _autoRunConfig.ExperimentType == ExperimentType.Bakeout
                    ? "베이크아웃" : "탈가스율 측정";
                _groupBoxAutoRunStatus.Text = isRunning
                    ? $"AutoRun 상태 — {expType}"
                    : $"AutoRun 상태 [{expType}]";
            }

            btnAutoRunStart.Enabled = !isRunning;
            btnAutoRunStop.Enabled = isRunning;
            btnAutoRunPause.Enabled = isRunning && !isPaused;
            btnAutoRunResume.Enabled = isRunning && isPaused;
            btnAutoRunConfig.Enabled = !isRunning;

            var state = _autoRunService?.CurrentState ?? AutoRunState.Idle;

            if (!isRunning)
            {
                _groupBoxMeasurements.Visible = false;
                if (_grpLiveAdjust != null) _grpLiveAdjust.Visible = false;

                // 완료/중단/오류 시 최종 상태를 유지, Idle이면 리셋
                if (state == AutoRunState.Idle)
                {
                    progressBarAutoRun.Value = 0;
                    lblAutoRunProgress.Text = "0%";
                    lblAutoRunStep.Text = "단계: -";
                    lblAutoRunRemainingTime.Text = "남은: --:--:--";
                    lblAutoRunStatus.Text = "상태: 대기 중";
                }
                else if (state == AutoRunState.Completed)
                {
                    progressBarAutoRun.Value = 100;
                    lblAutoRunProgress.Text = "100%";
                    lblAutoRunRemainingTime.Text = "완료";
                }
                else if (state == AutoRunState.Aborted)
                {
                    lblAutoRunRemainingTime.Text = "중단됨";
                }
                else if (state == AutoRunState.Error)
                {
                    lblAutoRunRemainingTime.Text = "오류";
                }
            }
            else
            {
                _groupBoxMeasurements.Visible = true;

                // 실시간 조정 그룹: 베이크아웃 모드에서만 표시, 현재 값 동기화
                bool isBakeoutRunning = _autoRunConfig?.ExperimentType == ExperimentType.Bakeout;
                if (_grpLiveAdjust != null)
                {
                    _grpLiveAdjust.Visible = isBakeoutRunning;
                    if (isBakeoutRunning && _autoRunConfig != null)
                    {
                        try
                        {
                            _numAdjTarget.Value = (decimal)_autoRunConfig.BakeoutTargetTemperature;
                            _numAdjMaxTemp.Value = (decimal)_autoRunConfig.BakeoutHeaterMaxTemperature;
                            _numAdjTolerance.Value = (decimal)_autoRunConfig.BakeoutTolerance;
                            _numAdjStabilization.Value = _autoRunConfig.BakeoutStabilizationSeconds;
                        }
                        catch { }
                    }
                }
            }

            // AutoRun 배너 업데이트
            UpdateAutoRunBanner();
        }

        private void UpdateAutoRunMeasurements()
        {
            if (_groupBoxMeasurements == null || !_groupBoxMeasurements.Visible) return;

            try
            {
                // ── 압력 ──
                double pressure = _dataCollectionService?.GetLatestPressure() ?? 0;
                if (pressure > 0)
                {
                    _lblMeasPressure.Text = $"압력: {pressure:E2} Torr";
                    _lblMeasPressure.ForeColor = pressure > (_autoRunConfig?.MaxPressureDuringExperiment ?? 1E-4)
                        ? Color.OrangeRed : Color.Black;
                }
                else
                {
                    _lblMeasPressure.Text = "압력: --";
                    _lblMeasPressure.ForeColor = Color.Gray;
                }

                // ── 전체 채널 온도 표시 ──
                if (_tempController?.IsConnected == true)
                {
                    var allCh = _tempController.Status.ChannelStatus;
                    int count = Math.Min(8, allCh.Length);

                    // CH1 라인: 메인 채널 (M-1~4)
                    var mainParts = new System.Collections.Generic.List<string>();
                    for (int i = 0; i < Math.Min(4, count); i++)
                    {
                        var ch = allCh[i];
                        if (!string.IsNullOrEmpty(ch.SensorError)) { mainParts.Add($"M{i + 1}:ERR"); continue; }
                        double pv = ch.Dot == 1 ? ch.PresentValue / 10.0 : ch.PresentValue;
                        string sv = ch.IsRunning ? $"(SV:{(ch.Dot == 1 ? ch.SetValue / 10.0 : ch.SetValue):F0})" : "";
                        mainParts.Add($"M{i + 1}:{pv:F1}{sv}");
                    }
                    _lblMeasCH1.Text = string.Join("  ", mainParts);
                    _lblMeasCH1.ForeColor = Color.Black;

                    // 샘플 라인: 확장 채널 (E-1~4) + 목표 온도
                    var expParts = new System.Collections.Generic.List<string>();
                    for (int i = 4; i < count; i++)
                    {
                        var ch = allCh[i];
                        if (!string.IsNullOrEmpty(ch.SensorError)) { expParts.Add($"E{i - 3}:ERR"); continue; }
                        double pv = ch.Dot == 1 ? ch.PresentValue / 10.0 : ch.PresentValue;
                        expParts.Add($"E{i - 3}:{pv:F1}");
                    }
                    bool isBakeout = _autoRunConfig?.ExperimentType == ExperimentType.Bakeout;
                    if (isBakeout)
                    {
                        double target = _autoRunConfig.BakeoutTargetTemperature;
                        double tol = _autoRunConfig.BakeoutTolerance > 0 ? _autoRunConfig.BakeoutTolerance : 1.0;
                        _lblMeasSample.Text = $"{string.Join("  ", expParts)}  (목표:{target:F0}±{tol:F0}°C)";
                    }
                    else
                    {
                        _lblMeasSample.Text = string.Join("  ", expParts);
                    }
                    _lblMeasSample.ForeColor = Color.Black;
                }
                else
                {
                    _lblMeasCH1.Text = "온도: 미연결";
                    _lblMeasCH1.ForeColor = Color.Gray;
                    _lblMeasSample.Text = "";
                }

                // ── 펌프 상태 ──
                string pumpInfo = "";
                if (_turboPump?.IsConnected == true)
                {
                    int speed = _turboPump.Status?.CurrentSpeed ?? 0;
                    bool running = _turboPump.Status?.IsRunning ?? false;
                    pumpInfo = running ? $"터보: {speed} Hz" : "터보: 정지";
                }
                else
                {
                    pumpInfo = "터보: 미연결";
                }

                if (_dryPump?.IsConnected == true)
                {
                    bool dpRunning = _dryPump.Status?.IsRunning ?? false;
                    pumpInfo += dpRunning ? "  |  DP: 가동" : "  |  DP: 정지";
                }

                _lblMeasPump.Text = pumpInfo;
                _lblMeasPump.ForeColor = Color.Black;

                // ── 설정값 요약 ──
                bool isBakeoutMode = _autoRunConfig?.ExperimentType == ExperimentType.Bakeout;
                if (_lblMeasConfig != null && _autoRunConfig != null)
                {
                    if (isBakeoutMode)
                    {
                        _lblMeasConfig.Text = $"[설정] 목표:{_autoRunConfig.BakeoutTargetTemperature:F0}°C  " +
                            $"램프:{_autoRunConfig.BakeoutRampRate:F0}°C/h  " +
                            $"모니터:{_autoRunConfig.GetBakeoutMonitorLabel()}  " +
                            $"CH1상한:{_autoRunConfig.BakeoutHeaterMaxTemperature:F0}°C  " +
                            $"ΔT:{(_autoRunConfig.BakeoutMaxDeltaT > 0 ? $"{_autoRunConfig.BakeoutMaxDeltaT:F0}°C" : "제한없음")}  " +
                            $"홀드:{_autoRunConfig.BakeoutHoldTimeMinutes}분  " +
                            $"종료:{(_autoRunConfig.BakeoutEndAction == BakeoutEndAction.HeaterOff ? "셧다운" : _autoRunConfig.BakeoutEndAction == BakeoutEndAction.MaintainTemperature ? "유지" : "알림")}";
                    }
                    else
                    {
                        _lblMeasConfig.Text = $"[설정] 목표:{_autoRunConfig.HeaterCh1SetTemperature:F0}°C  " +
                            $"램프:{_autoRunConfig.HeaterRampUpRate:F0}°C/h  " +
                            $"실험:{_autoRunConfig.ExperimentDurationMinutes}분  " +
                            $"최대압력:{_autoRunConfig.MaxPressureDuringExperiment:E1} Torr";
                    }
                }

                // ── 열 특성 계수 (베이크아웃 PI 피드백 시) ──
                if (isBakeoutMode && _lblMeasThermal != null && _autoRunService?.IsRunning == true)
                {
                    var tp = _autoRunService.ThermalParams;
                    if (tp.ThermalLag > 0)
                    {
                        _lblMeasThermal.Text =
                            $"[열특성] 열지연:{tp.ThermalLag:F1}°C  " +
                            $"현재Lag:{tp.CurrentLag:F1}°C  " +
                            $"변화율:{tp.SampleRate:F2}°C/cyc  " +
                            $"시정수:{(tp.EstimatedThermalTimeConstant > 0 ? $"{tp.EstimatedThermalTimeConstant:F0}초" : "--")}  " +
                            $"열저항:{(tp.EstimatedThermalResistance > 0 ? $"{tp.EstimatedThermalResistance:F2}" : "--")}  " +
                            $"Kp:{tp.Kp:F2} Kd:{tp.Kd:F2}  " +
                            $"I:{tp.IntegralTerm:F1}  " +
                            $"정상SV:{tp.EstimatedSteadyStateSV:F1}°C";
                    }
                    else
                    {
                        _lblMeasThermal.Text = "[열특성] 측정 중...";
                    }
                }
                else if (_lblMeasThermal != null)
                {
                    _lblMeasThermal.Text = "";
                }
            }
            catch
            {
                // 측정값 읽기 실패 시 무시 (UI 안정성 유지)
            }
        }

        private void AddAutoRunLog(string state, string message, Color? color = null)
        {
            var item = new ListViewItem(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            item.SubItems.Add(state);
            item.SubItems.Add(message);
            if (color.HasValue)
                item.ForeColor = color.Value;

            listViewAutoRunLog.Items.Insert(0, item);
            while (listViewAutoRunLog.Items.Count > 100)
                listViewAutoRunLog.Items.RemoveAt(listViewAutoRunLog.Items.Count - 1);
        }

        private void UpdateStepSequenceDisplay(AutoRunState currentState)
        {
            if (_stepPanels == null || _stepLabels == null) return;

            string[] stepNames = { "초기화", "진공준비", "드라이", "터보", "IG", "고진공", "히터", "실험", "종료" };

            // Paused: 현재 단계 번호를 유지하여 ⏸ 표시
            bool isPausedState = currentState == AutoRunState.Paused;
            int currentStep = currentState switch
            {
                AutoRunState.Initializing => 1,
                AutoRunState.PreparingVacuum => 2,
                AutoRunState.StartingDryPump => 3,
                AutoRunState.StartingTurboPump => 4,
                AutoRunState.ActivatingIonGauge => 5,
                AutoRunState.WaitingHighVacuum => 6,
                AutoRunState.StartingHeater => 7,
                AutoRunState.RunningExperiment => 8,
                AutoRunState.ShuttingDown => 9,
                AutoRunState.Completed => 10,
                AutoRunState.Aborted or AutoRunState.Error => -1,
                AutoRunState.Paused => _autoRunService?.CurrentStepNumber ?? 0,
                _ => 0
            };

            for (int i = 0; i < 9; i++)
            {
                int stepNum = i + 1;
                string name = stepNames[i];

                if (currentStep == -1)
                {
                    int failedAt = _autoRunService?.CurrentStepNumber ?? 0;
                    if (stepNum < failedAt)
                    {
                        _stepPanels[i].BackColor = Color.FromArgb(200, 230, 200);
                        _stepLabels[i].ForeColor = Color.DarkGreen;
                        _stepLabels[i].Font = new Font("맑은 고딕", 8F);
                        _stepLabels[i].Text = $"\u2713 {name}";
                    }
                    else if (stepNum == failedAt)
                    {
                        _stepPanels[i].BackColor = Color.FromArgb(255, 180, 180);
                        _stepLabels[i].ForeColor = Color.DarkRed;
                        _stepLabels[i].Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
                        _stepLabels[i].Text = $"\u2717 {name}";
                    }
                    else
                    {
                        _stepPanels[i].BackColor = Color.FromArgb(230, 230, 230);
                        _stepLabels[i].ForeColor = Color.Gray;
                        _stepLabels[i].Font = new Font("맑은 고딕", 8F);
                        _stepLabels[i].Text = $"{stepNum}. {name}";
                    }
                }
                else if (currentStep == 0)
                {
                    _stepPanels[i].BackColor = Color.FromArgb(230, 230, 230);
                    _stepLabels[i].ForeColor = Color.Gray;
                    _stepLabels[i].Font = new Font("맑은 고딕", 8F);
                    _stepLabels[i].Text = $"{stepNum}. {name}";
                }
                else if (stepNum < currentStep)
                {
                    _stepPanels[i].BackColor = Color.FromArgb(200, 230, 200);
                    _stepLabels[i].ForeColor = Color.DarkGreen;
                    _stepLabels[i].Font = new Font("맑은 고딕", 8F);
                    _stepLabels[i].Text = $"\u2713 {name}";
                }
                else if (stepNum == currentStep)
                {
                    if (isPausedState)
                    {
                        // 일시정지: 현재 단계를 주황색 ⏸ 표시
                        _stepPanels[i].BackColor = Color.FromArgb(255, 235, 180);
                        _stepLabels[i].ForeColor = Color.FromArgb(150, 100, 0);
                        _stepLabels[i].Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
                        _stepLabels[i].Text = $"\u23F8 {name}";
                    }
                    else
                    {
                        _stepPanels[i].BackColor = Color.FromArgb(180, 210, 255);
                        _stepLabels[i].ForeColor = Color.DarkBlue;
                        _stepLabels[i].Font = new Font("맑은 고딕", 8F, FontStyle.Bold);
                        _stepLabels[i].Text = $"\u25B6 {name}";
                    }
                }
                else
                {
                    _stepPanels[i].BackColor = Color.FromArgb(230, 230, 230);
                    _stepLabels[i].ForeColor = Color.Gray;
                    _stepLabels[i].Font = new Font("맑은 고딕", 8F);
                    _stepLabels[i].Text = $"{stepNum}. {name}";
                }
            }
        }

        private string GetAutoRunStateText(AutoRunState state) => state switch
        {
            AutoRunState.Idle => "대기 중",
            AutoRunState.Initializing => "초기화",
            AutoRunState.PreparingVacuum => "진공 준비",
            AutoRunState.StartingDryPump => "드라이펌프 시작",
            AutoRunState.StartingTurboPump => "터보펌프 시작",
            AutoRunState.ActivatingIonGauge => "이온게이지 활성화",
            AutoRunState.WaitingHighVacuum => "고진공 대기",
            AutoRunState.StartingHeater => "히터 시작",
            AutoRunState.RunningExperiment => "실험 진행",
            AutoRunState.ShuttingDown => "종료 중",
            AutoRunState.Completed => "완료",
            AutoRunState.Aborted => "중단됨",
            AutoRunState.Error => "오류",
            AutoRunState.Paused => "일시정지",
            _ => state.ToString()
        };

        private AutoRunConfiguration LoadAutoRunConfiguration()
        {
            try
            {
                string configPath = Path.Combine(PathSettings.Instance.ConfigPath, "AutoRunConfig.xml");
                if (File.Exists(configPath))
                {
                    using (var reader = new StreamReader(configPath))
                    {
                        return (AutoRunConfiguration)new XmlSerializer(
                            typeof(AutoRunConfiguration)).Deserialize(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"AutoRun 설정 로드 실패: {ex.Message}", ex);
            }
            return null;
        }

        private void SaveAutoRunConfiguration(AutoRunConfiguration config)
        {
            try
            {
                string configDir = PathSettings.Instance.ConfigPath;
                if (!Directory.Exists(configDir))
                    Directory.CreateDirectory(configDir);

                using (var writer = new StreamWriter(Path.Combine(configDir, "AutoRunConfig.xml")))
                {
                    new XmlSerializer(typeof(AutoRunConfiguration)).Serialize(writer, config);
                }
            }
            catch (Exception ex)
            {
                LogError($"AutoRun 설정 저장 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 실험 데이터 로거 종료 및 파일 경로 로그 기록
        /// </summary>
        private void StopExperimentDataLogger(string reason)
        {
            if (_experimentDataLogger?.IsRunning != true)
                return;

            string filePath = _experimentDataLogger.FilePath;
            _experimentDataLogger.Stop();

            AddAutoRunLog("DATA", $"실험 데이터 저장 완료 ({reason})");
            LogInfo($"[실험 데이터] 파일: {filePath}");
        }

        #endregion

        #region 데이터 수집 이벤트

        private void OnDataUpdated(object sender, UIDataSnapshot snapshot)
        {
            _uiUpdateService.RequestUpdate(snapshot);
            ApplyThermalRampPendingSetpoint();

            // ★ 인터락: 벤트밸브 열림 + ATM ≥ 90 kPa → 배기밸브 자동 열림 (과압 방지)
            CheckVentOverpressureInterlock(snapshot);

            // ★ 실시간 안전 워치독
            _safetyInterlock?.RunWatchdog();

            // 실험 데이터 로깅 (AutoRun 실행 중일 때)
            if (_experimentDataLogger?.IsRunning == true)
            {
                string state = _autoRunService?.CurrentState.ToString() ?? "";
                _experimentDataLogger.LogSnapshot(snapshot, state);
            }

            if (!_isLoggingEnabled)
                return;

            // 설정된 수집 주기에 따라 로깅
            var now = DateTime.Now;
            if ((now - _lastDataLogTime).TotalSeconds < _dataLoggingIntervalSeconds)
                return;
            _lastDataLogTime = now;

            Task.Run(() =>
            {
                try
                {
                    if (snapshot.Connections.IOModule)
                    {
                        DataLoggerService.Instance.LogDataAsync("Pressure", new List<string>
                        {
                            snapshot.AtmPressure.ToString("F2"),
                            snapshot.PiraniPressure.ToString("E2"),
                            snapshot.IonPressure.ToString("E2"),
                            snapshot.IonGaugeStatus,
                            snapshot.GateValveStatus,
                            snapshot.VentValveStatus,
                            snapshot.ExhaustValveStatus,
                            snapshot.IonGaugeHVStatus,
                            snapshot.AdditionalAIValue.ToString("F6")
                        });
                    }

                    if (snapshot.Connections.DryPump && _dryPump?.Status != null)
                    {
                        DataLoggerService.Instance.LogDataAsync("DryPump", new List<string>
                        {
                            snapshot.DryPump.Status,
                            _dryPump.Status.MotorFrequency.ToString("F1"),
                            _dryPump.Status.MotorCurrent.ToString("F2"),
                            _dryPump.Status.MotorTemperature.ToString("F1"),
                            snapshot.DryPump.HasWarning.ToString(),
                            snapshot.DryPump.HasError.ToString()
                        });
                    }

                    if (snapshot.Connections.TurboPump && _turboPump?.Status != null)
                    {
                        DataLoggerService.Instance.LogDataAsync("TurboPump", new List<string>
                        {
                            snapshot.TurboPump.Status,
                            _turboPump.Status.CurrentSpeed.ToString(),
                            _turboPump.Status.MotorCurrent.ToString("F2"),
                            _turboPump.Status.MotorTemperature.ToString(),
                            snapshot.TurboPump.HasWarning.ToString(),
                            snapshot.TurboPump.HasError.ToString()
                        });
                    }

                    if (snapshot.Connections.BathCirculator && _bathCirculator?.Status != null)
                    {
                        DataLoggerService.Instance.LogDataAsync("BathCirculator", new List<string>
                        {
                            snapshot.BathCirculator.Status,
                            _bathCirculator.Status.CurrentTemperature.ToString("F1"),
                            _bathCirculator.Status.TargetTemperature.ToString("F1"),
                            snapshot.BathCirculator.Mode,
                            snapshot.BathCirculator.Time,
                            snapshot.BathCirculator.HasError.ToString(),
                            snapshot.BathCirculator.HasWarning.ToString()
                        });
                    }

                    if (snapshot.Connections.TempController && _tempController?.Status != null)
                    {
                        var tcData = new List<string>();
                        for (int i = 0; i < Math.Min(8, snapshot.TempController.Channels.Length); i++)
                        {
                            var ch = snapshot.TempController.Channels[i];
                            tcData.Add(ch?.PresentValue ?? "");
                            tcData.Add(ch?.SetValue ?? "");
                            tcData.Add(ch?.HeatingMV?.Replace(" %", "") ?? "");
                            tcData.Add(ch?.Status ?? "");
                        }
                        DataLoggerService.Instance.LogDataAsync("TempController", tcData);
                    }
                }
                catch (Exception ex)
                {
                    LogError($"데이터 로깅 오류: {ex.Message}", ex);
                }
            });
        }

        private void ApplyThermalRampPendingSetpoint()
        {
            if (simpleRampControl1 == null || !simpleRampControl1.IsRunning)
                return;

            try
            {
                simpleRampControl1.RampController?.ApplyPendingSetpoint();
            }
            catch { }
        }

        #endregion

        #region UI 업데이트 메서드

        public void SetAtmPressureText(string value)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetAtmPressureText), value); return; }
            try { if (txtATM != null) txtATM.TextValue = value; } catch { }
        }

        public void SetPiraniPressureText(string value)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetPiraniPressureText), value); return; }
            try
            {
                if (txtPG != null)
                {
                    txtPG.TextValue = value;
                    txtPG.ForeColor = SystemColors.WindowText;
                }
            }
            catch { }
        }

        public void SetIonPressureText(string value)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonPressureText), value); return; }
            try { if (txtIG != null) txtIG.TextValue = value; } catch { }
        }

        public void SetIonGaugeStatusText(string value)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonGaugeStatusText), value); return; }
            try { if (txtIGStatus != null) txtIGStatus.TextValue = value; } catch { }
        }

        public void SetGateValveStatus(string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetGateValveStatus), status); return; }
            try
            {
                if (btn_GV != null && !btn_GV.Focused)
                {
                    btn_GV.Text = status;
                    btn_GV.BackColor = status == "Moving" ? Color.Yellow
                                     : status == "Opened" ? Color.LightGreen
                                     : SystemColors.Control;
                }
            }
            catch { }
        }

        public void SetVentValveStatus(string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetVentValveStatus), status); return; }
            try
            {
                if (btn_VV != null && !btn_VV.Focused)
                {
                    btn_VV.Text = status;
                    btn_VV.BackColor = status == "Opened" ? Color.LightBlue : SystemColors.Control;
                }
            }
            catch { }
        }

        public void SetExhaustValveStatus(string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetExhaustValveStatus), status); return; }
            try
            {
                if (btn_EV != null && !btn_EV.Focused)
                {
                    btn_EV.Text = status;
                    btn_EV.BackColor = status == "Opened" ? Color.LightCoral : SystemColors.Control;
                }
            }
            catch { }
        }

        public void SetIonGaugeHVStatus(string status)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(SetIonGaugeHVStatus), status); return; }
            try
            {
                if (btn_iongauge != null && !btn_iongauge.Focused)
                {
                    btn_iongauge.Text = status;
                    btn_iongauge.BackColor = status == "HV on" ? Color.Orange : SystemColors.Control;
                }
            }
            catch { }
        }

        public void SetDryPumpStatus(string status, string speed, string current,
            string temperature, bool hasWarning, bool hasError, string warningMessage = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, bool, bool, string>(
                    SetDryPumpStatus), status, speed, current, temperature, hasWarning, hasError, warningMessage);
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
                    lblDryPumpWarning.Visible = hasError || hasWarning;
                    if (hasError || hasWarning)
                    {
                        lblDryPumpWarning.Text = warningMessage;
                        lblDryPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange;
                    }
                }
            }
            catch { }
        }

        public void SetDryPumpExtendedStatus(string power, string runningTime, bool isServiceDue)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, bool>(SetDryPumpExtendedStatus), power, runningTime, isServiceDue);
                return;
            }
            try
            {
                if (txtDryPumpPower != null) txtDryPumpPower.Text = power;
                if (txtDryPumpRunTime != null) txtDryPumpRunTime.Text = runningTime;
                if (lblDryPumpService != null)
                {
                    lblDryPumpService.Text = isServiceDue ? "서비스 필요" : "-";
                    lblDryPumpService.ForeColor = isServiceDue ? Color.Orange : Color.White;
                }
            }
            catch { }
        }

        public void SetTurboPumpStatus(string status, string speed, string current,
            string temperature, bool hasWarning, bool hasError, string warningMessage = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, bool, bool, string>(
                    SetTurboPumpStatus), status, speed, current, temperature, hasWarning, hasError, warningMessage);
                return;
            }
            try
            {
                if (txtTurboPumpStatus != null) txtTurboPumpStatus.Text = status;
                if (txtTurboPumpSpeed != null) txtTurboPumpSpeed.Text = speed;
                if (txtTurboPumpCurrent != null) txtTurboPumpCurrent.Text = current;
                if (txtTurboPumpMotorTemp != null) txtTurboPumpMotorTemp.Text = temperature;

                // 속도 ProgressBar 업데이트 (정격 대비 %)
                if (progressBarTurboPumpSpeed != null && _turboPump != null)
                {
                    try { progressBarTurboPumpSpeed.Value = _turboPump.GetSpeedPercentage(); }
                    catch { }
                }
                if (lblTurboPumpWarning != null)
                {
                    lblTurboPumpWarning.Visible = hasError || hasWarning;
                    if (hasError || hasWarning)
                    {
                        lblTurboPumpWarning.Text = warningMessage;
                        lblTurboPumpWarning.ForeColor = hasError ? Color.Red : Color.Orange;
                    }
                }
            }
            catch { }
        }

        public void SetTurboPumpExtendedStatus(string bearingTemp, string electronicsTemp,
            string runningTime, bool isReady, bool isRemoteActive, bool isNormalOperation)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, bool, bool, bool>(
                    SetTurboPumpExtendedStatus), bearingTemp, electronicsTemp, runningTime, isReady, isRemoteActive, isNormalOperation);
                return;
            }
            try
            {
                if (txtTurboPumpBearingTemp != null) txtTurboPumpBearingTemp.Text = bearingTemp;
                if (txtTurboPumpElectronicsTemp != null) txtTurboPumpElectronicsTemp.Text = electronicsTemp;
                if (txtTurboPumpRunTime != null) txtTurboPumpRunTime.Text = runningTime;
                if (txtTurboPumpReady != null) txtTurboPumpReady.Text = isReady ? "Ready" : "-";
                if (txtTurboPumpRemote != null) txtTurboPumpRemote.Text = isRemoteActive ? "Remote" : "Local";
                if (txtTurboPumpNormal != null) txtTurboPumpNormal.Text = isNormalOperation ? "Normal" : "-";
            }
            catch { }
        }

        public void SetBathCirculatorStatus(string status, string currentTemp, string targetTemp,
            string time, string mode, bool hasError, bool hasWarning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string, string, string, string, bool, bool>(
                    SetBathCirculatorStatus), status, currentTemp, targetTemp, time, mode, hasError, hasWarning);
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
            catch { }
        }

        public void SetTempControllerChannelStatus(int channel, string presentValue,
            string setValue, string status, string heatingMV, bool isAutoTuning)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, string, string, string, string, bool>(
                    SetTempControllerChannelStatus), channel, presentValue, setValue, status, heatingMV, isAutoTuning);
                return;
            }
            try
            {
                // PV는 항상 업데이트
                switch (channel)
                {
                    case 1: if (txtCh1PresentValue != null) txtCh1PresentValue.Text = $"{presentValue}℃"; break;
                    case 2: if (txtCh2PresentValue != null) txtCh2PresentValue.Text = $"{presentValue}℃"; break;
                    case 3: if (txtCh3PresentValue != null) txtCh3PresentValue.Text = $"{presentValue}℃"; break;
                    case 4: if (txtCh4PresentValue != null) txtCh4PresentValue.Text = $"{presentValue}℃"; break;
                    case 5: if (txtCh5PresentValue != null) txtCh5PresentValue.Text = $"{presentValue}℃"; break;
                    case 6: if (txtCh6PresentValue != null) txtCh6PresentValue.Text = $"{presentValue}℃"; break;
                    case 7: if (txtCh7PresentValue != null) txtCh7PresentValue.Text = $"{presentValue}℃"; break;
                    case 8: if (txtCh8PresentValue != null) txtCh8PresentValue.Text = $"{presentValue}℃"; break;
                    default:
                        // CH9~12: 동적 생성된 텍스트박스
                        if (_expansionPVLabels != null && channel - 9 >= 0 && channel - 9 < _expansionPVLabels.Length)
                            _expansionPVLabels[channel - 9].Text = $"{presentValue}℃";
                        break;
                }

                // 상세 정보는 선택된 채널만 업데이트
                int selectedCh = (cmbTempChannel?.SelectedIndex ?? -1) + 1;
                if (selectedCh > 0 && channel == selectedCh)
                {
                    if (txtCh1SetValue != null) txtCh1SetValue.Text = $"{setValue}℃";
                    if (txtCh1Status != null) txtCh1Status.Text = status;
                    if (txtCh1HeatingMV != null) txtCh1HeatingMV.Text = heatingMV;
                    if (txtCh1IsAutotune != null) txtCh1IsAutotune.Text = isAutoTuning ? "On" : "Off";

                    // Start/Stop 버튼 외관도 실시간 반영
                    bool running = (status == "Run");
                    btnCh1Start.Text = running ? "Stop" : "Start";
                    btnCh1Start.BackColor = running ? Color.FromArgb(255, 200, 200) : SystemColors.Control;
                }
            }
            catch { }
        }

        /// <summary>
        /// 초기화: 불필요한 버튼 숨김, 펌프 속도 초기값 로딩, 툴팁 설정
        /// </summary>
        public void SetupToggleButtons()
        {
            btnCh1Stop.Visible = false;

            // ── 확장모듈2 (E2-CH1~4 = CH9~12) UI 동적 생성 ──
            CreateExpansion2UI();

            // Ch1(Heater) 강조: 굵은 폰트 + 배경색
            if (txtCh1PresentValue != null)
            {
                txtCh1PresentValue.Font = new Font(txtCh1PresentValue.Font.FontFamily, 9F, FontStyle.Bold);
                txtCh1PresentValue.BackColor = Color.FromArgb(255, 240, 240);
                txtCh1PresentValue.BorderStyle = BorderStyle.Fixed3D;
            }

            // 온도 컨트롤러 채널 선택 초기화
            if (cmbTempChannel != null)
                cmbTempChannel.SelectedIndex = 0;

            // 펌프 속도 컨트롤 툴팁
            var toolTip = new ToolTip();
            toolTip.SetToolTip(btnDryPumpSpeedSet, "현재 세션에만 적용 (재시작 시 초기화)");
            toolTip.SetToolTip(btnDryPumpSpeedStore, "비휘발성 메모리에 저장 (재시작 후에도 유지)");
            toolTip.SetToolTip(btnTurboPumpSpeedSet, "실시간 적용 (재시작 시 초기화)");
            toolTip.SetToolTip(btnTurboPumpSpeedPerm, "EEPROM에 영구 저장 (재시작 후에도 유지)");
            toolTip.SetToolTip(btnTurboPumpStandbySet, "대기 모드 진입 시 저속 회전 주파수");

            // 펌프에서 현재 속도 설정값 읽어오기
            Task.Run(() =>
            {
                try
                {
                    if (_dryPump?.IsConnected == true)
                    {
                        _dryPump.GetSpeedDemand(); // Hz 반환, 내부적으로 SpeedDemandPercent 갱신
                        double pct = _dryPump.Status?.SpeedDemandPercent ?? 0;
                        if (pct >= 50 && pct <= 100)
                            BeginInvoke(new Action(() => numDryPumpSpeed.Value = (decimal)Math.Round(pct)));
                    }
                }
                catch { }

                try
                {
                    if (_turboPump?.IsConnected == true)
                    {
                        ushort freq = _turboPump.CurrentSetpointFrequency;
                        if (freq >= 600 && freq <= 1200)
                            BeginInvoke(new Action(() => numTurboPumpSpeed.Value = freq));
                    }
                }
                catch { }
            });
        }

        /// <summary>
        /// 토글 버튼 외관 업데이트 (UI 타이머에서 호출)
        /// </summary>
        public void UpdateToggleButtonAppearance(string deviceName, bool isRunning)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string, bool>(UpdateToggleButtonAppearance), deviceName, isRunning); return; }
            try
            {
                // 클릭 직후 suppress 기간이면 타이머에서의 업데이트 무시
                if (_toggleSuppressUntil.TryGetValue(deviceName, out var until) && DateTime.Now < until)
                    return;

                Button btn = deviceName switch
                {
                    "drypump" => btnDryPumpStart,
                    "turbopump" => btnTurboPumpStart,
                    "bathcirculator" => btnBathCirculatorStart,
                    "ch1" => btnCh1Start,
                    _ => null
                };
                if (btn == null) return;

                string newText = isRunning ? "Stop" : "Start";
                var newColor = isRunning ? Color.FromArgb(255, 200, 200) : SystemColors.Control;

                if (btn.Text != newText) btn.Text = newText;
                if (btn.BackColor != newColor) btn.BackColor = newColor;
            }
            catch { }
        }

        /// <summary>
        /// 토글 버튼 외관을 강제 설정하고, 일정 시간 동안 타이머의 덮어쓰기를 억제
        /// 클릭 핸들러에서 사용 — 폴링 지연으로 인한 상태 오락가락 방지
        /// </summary>
        private void ForceToggleButtonAppearance(string deviceName, bool isRunning, int suppressMs = 2000)
        {
            _toggleSuppressUntil[deviceName] = DateTime.Now.AddMilliseconds(suppressMs);

            Button btn = deviceName switch
            {
                "drypump" => btnDryPumpStart,
                "turbopump" => btnTurboPumpStart,
                "bathcirculator" => btnBathCirculatorStart,
                var name when name.StartsWith("ch") => btnCh1Start,
                _ => null
            };
            if (btn == null) return;

            btn.Text = isRunning ? "Stop" : "Start";
            btn.BackColor = isRunning ? Color.FromArgb(255, 200, 200) : SystemColors.Control;
        }

        public void SetButtonEnabled(string buttonName, bool enabled)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string, bool>(SetButtonEnabled), buttonName, enabled); return; }
            try
            {
                var btn = GetButtonByName(buttonName);
                if (btn != null && btn.Enabled != enabled)
                    btn.Enabled = enabled;
            }
            catch { }
        }

        public void SetConnectionStatus(string deviceName, bool isConnected)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string, bool>(SetConnectionStatus), deviceName, isConnected); return; }
            try
            {
                var indicator = GetConnectionIndicatorByName(deviceName);
                if (indicator is Forms.UserControls.ConnectionIndicator ci)
                    ci.IsConnected = isConnected;
            }
            catch { }
        }

        public void SetPanelEnabled(string deviceName, bool enabled)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string, bool>(SetPanelEnabled), deviceName, enabled); return; }
            try
            {
                var panels = GetPanelsByDevice(deviceName);
                if (panels != null)
                {
                    foreach (var panel in panels)
                    {
                        if (panel != null && panel.Enabled != enabled)
                            panel.Enabled = enabled;
                    }
                }
            }
            catch { }
        }

        private Control[] GetPanelsByDevice(string deviceName) => deviceName.ToLower() switch
        {
            "iomodule" => new Control[] { tableLayoutPanel5 },
            "drypump" => new Control[] { panel2 },
            "turbopump" => new Control[] { panel3 },
            "bathcirculator" => new Control[] { grpChillerPID },
            "tempcontroller" => new Control[] { panel5, grpCh1Timer },
            _ => null
        };

        private Button GetButtonByName(string buttonName) => buttonName.ToLower() switch
        {
            "iongauge" or "btn_iongauge" => btn_iongauge,
            "ventvalve" or "btn_vv" => btn_VV,
            "exhaustvalve" or "btn_ev" => btn_EV,
            "drypumpstart" => btnDryPumpStart,
            "drypumpstandby" => btnDryPumpStandby,
            "drypumpnormal" => btnDryPumpNormal,
            "turbopumpstart" => btnTurboPumpStart,
            "turbopumpvent" => btnTurboPumpVent,
            "turbopumpreset" => btnTurboPumpReset,
            "bathcirculatorstart" => btnBathCirculatorStart,
            "ch1start" => btnCh1Start,
            "ch1stop" => btnCh1Stop,
            _ => null
        };

        private Control GetConnectionIndicatorByName(string deviceName) => deviceName.ToLower() switch
        {
            "iomodule" => connectionIndicator_iomodule,
            "drypump" => connectionIndicator_drypump,
            "turbopump" => connectionIndicator_turbopump,
            "bathcirculator" => connectionIndicator_bathcirculator,
            "tempcontroller" => connectionIndicator_tempcontroller,
            _ => null
        };

        public void UpdatePIDStatus()
        {
            if (_chillerPIDService != null && _chillerPIDService.IsEnabled)
            {
                lblLastOutputValue.Text = $"{_chillerPIDService.TargetChannelName}: {_chillerPIDService.LastChannelTemperature:F1}°C → " +
                    $"칠러: {_chillerPIDService.LastChillerSetpoint:F1}°C";
            }
            else
            {
                lblLastOutputValue.Text = "-";
            }
        }

        #endregion

        #region 밸브 제어

        private async void btn_GV_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualValveControl, "밸브"))
                return;

            bool isOpen = btn_GV.Text == "Opened";

            // 열기 시: ATM 압력 80 kPa 이상이어야 함
            if (!isOpen && !CheckInterlock(
                GetCurrentAtmPressure() < 80,
                _interlockConfig.GateValveOpen_RequireAtmPressure,
                $"ATM 압력이 80 kPa 미만입니다. (현재: {GetCurrentAtmPressure():F1} kPa)\n게이트밸브를 열려면 대기압 상태(≥ 80 kPa)여야 합니다."))
                return;

            // 닫기 시: 터보펌프 작동 중이면 차단
            if (isOpen && !CheckInterlock(
                _turboPump?.Status?.CurrentSpeed > 100,
                _interlockConfig.GateValveClose_BlockIfTurboRunning,
                "터보펌프가 작동 중입니다. 게이트밸브를 닫을 수 없습니다."))
                return;

            btn_GV.Enabled = false;
            try
            {
                if (await _ioModule.ControlGateValveAsync(!isOpen))
                    LogInfo($"게이트 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
            }
            finally { btn_GV.Enabled = true; }
        }

        private async void btn_VV_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualValveControl, "밸브"))
                return;
            if (!CheckInterlock(
                _turboPump?.Status?.CurrentSpeed > 100,
                _interlockConfig.VentValve_BlockIfTurboRunning,
                "터보펌프가 작동 중입니다."))
                return;

            btn_VV.Enabled = false;
            try
            {
                bool isOpen = btn_VV.Text == "Opened";
                if (await _ioModule.ControlVentValveAsync(!isOpen))
                    LogInfo($"벤트 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
            }
            finally { btn_VV.Enabled = true; }
        }

        private async void btn_EV_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualValveControl, "밸브"))
                return;
            if (!CheckInterlock(
                _turboPump?.Status?.CurrentSpeed > 100,
                _interlockConfig.ExhaustValve_BlockIfTurboRunning,
                "터보펌프가 작동 중입니다."))
                return;

            btn_EV.Enabled = false;
            try
            {
                bool isOpen = btn_EV.Text == "Opened";
                if (await _ioModule.ControlExhaustValveAsync(!isOpen))
                    LogInfo($"배기 밸브 {(!isOpen ? "열기" : "닫기")} 성공");
            }
            finally { btn_EV.Enabled = true; }
        }

        private async void btn_iongauge_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualIonGaugeControl, "이온게이지"))
                return;

            bool isOn = btn_iongauge.Text == "HV on";

            // ON 시: 피라니 압력 ≤ 7.5E-4 Torr 체크
            double piraniPressure = GetCurrentPiraniPressure();
            if (!isOn && !CheckInterlock(
                piraniPressure > 7.5E-4 || piraniPressure <= 0,
                _interlockConfig.IonGaugeHV_RequireLowPressure,
                $"피라니 압력이 너무 높습니다.\n현재: {(piraniPressure > 0 ? $"{piraniPressure:E2}" : "N/A")} Torr (기준: ≤ 7.5E-4 Torr)\n이온게이지가 손상될 수 있습니다."))
                return;

            btn_iongauge.Enabled = false;
            try
            {
                if (await _ioModule.ControlIonGaugeHVAsync(!isOn))
                    LogInfo($"이온 게이지 HV {(!isOn ? "ON" : "OFF")} 성공");
            }
            finally { btn_iongauge.Enabled = true; }
        }

        #endregion

        #region 펌프 제어

        private async void btnDryPumpStart_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualPumpControl, "펌프"))
                return;

            btnDryPumpStart.Enabled = false;
            try
            {
                // 실제 장비 상태를 직접 조회
                bool checkOk = await Task.Run(() => _dryPump.CheckStatus());
                bool isRunning = checkOk && (_dryPump.Status?.IsRunning == true);

                if (isRunning)
                {
                    // Stop 로직
                    if (!CheckInterlock(
                        _turboPump?.Status?.IsRunning == true,
                        _interlockConfig.DryPumpStop_BlockIfTurboRunning,
                        "터보펌프가 작동 중입니다."))
                        return;

                    await Task.Run(() => _dryPump.Stop());
                    ForceToggleButtonAppearance("drypump", false);
                    LogInfo("드라이펌프 정지");
                }
                else
                {
                    // Start 로직
                    if (!CheckInterlock(
                        btn_GV.Text != "Opened",
                        _interlockConfig.DryPump_RequireGateValveOpen,
                        "게이트 밸브가 열려있지 않습니다."))
                        return;
                    if (!CheckInterlock(
                        btn_VV.Text == "Opened" || btn_EV.Text == "Opened",
                        _interlockConfig.DryPump_RequireVentExhaustClosed,
                        "벤트 또는 배기 밸브가 열려있습니다."))
                        return;

                    await Task.Run(() => _dryPump.Start());
                    ForceToggleButtonAppearance("drypump", true);
                    LogInfo("드라이펌프 시작");
                }
            }
            finally { btnDryPumpStart.Enabled = true; }
        }

private async void btnDryPumpStandby_Click(object sender, EventArgs e)
        {
            btnDryPumpStandby.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.SetStandby());
                LogInfo("드라이펌프 대기모드");
            }
            finally { btnDryPumpStandby.Enabled = true; }
        }

        private async void btnDryPumpNormal_Click(object sender, EventArgs e)
        {
            btnDryPumpNormal.Enabled = false;
            try
            {
                await Task.Run(() => _dryPump.SetNormalMode());
                LogInfo("드라이펌프 정상모드");
            }
            finally { btnDryPumpNormal.Enabled = true; }
        }

        private async void btnTurboPumpStart_Click(object sender, EventArgs e)
        {
            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualPumpControl, "펌프"))
                return;

            btnTurboPumpStart.Enabled = false;
            try
            {
                // 실제 장비 상태를 직접 조회
                bool checkOk = await Task.Run(() => _turboPump.CheckStatus());
                bool isRunning = checkOk && (_turboPump.Status?.IsRunning == true);

                if (isRunning)
                {
                    // Stop 로직
                    await Task.Run(() => _turboPump.Stop());
                    ForceToggleButtonAppearance("turbopump", false);
                    LogInfo("터보펌프 정지");
                }
                else
                {
                    // Start 로직
                    if (!CheckInterlock(
                        !(_dryPump?.Status?.IsRunning ?? false),
                        _interlockConfig.TurboPump_RequireDryPumpRunning,
                        "드라이펌프가 작동중이 아닙니다."))
                        return;
                    if (!CheckInterlock(
                        _dataCollectionService?.GetLatestPressure() > 1,
                        _interlockConfig.TurboPump_RequirePressureBelow1Torr,
                        "챔버 압력이 너무 높습니다."))
                        return;
                    if (!CheckInterlock(
                        !(_bathCirculator?.Status?.IsRunning ?? false),
                        _interlockConfig.TurboPump_RequireChillerRunning,
                        "칠러가 작동중이 아닙니다."))
                        return;
                    if (!CheckInterlock(
                        btn_GV.Text != "Opened",
                        _interlockConfig.TurboPump_RequireGateValveOpen,
                        "게이트밸브가 열려있지 않습니다."))
                        return;

                    await Task.Run(() => _turboPump.Start());
                    ForceToggleButtonAppearance("turbopump", true);
                    LogInfo("터보펌프 시작");
                }
            }
            finally { btnTurboPumpStart.Enabled = true; }
        }

private async void btnTurboPumpVent_Click(object sender, EventArgs e)
        {
            btnTurboPumpVent.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.Vent());
                LogInfo("터보펌프 벤트");
            }
            finally { btnTurboPumpVent.Enabled = true; }
        }

        private async void btnTurboPumpReset_Click(object sender, EventArgs e)
        {
            btnTurboPumpReset.Enabled = false;
            try
            {
                await Task.Run(() => _turboPump.ResetError());
                LogInfo("터보펌프 리셋");
            }
            finally { btnTurboPumpReset.Enabled = true; }
        }

        // ── 드라이펌프 속도 조절 ──
        private async void btnDryPumpSpeedSet_Click(object sender, EventArgs e)
        {
            btnDryPumpSpeedSet.Enabled = false;
            try
            {
                double percent = (double)numDryPumpSpeed.Value;
                bool ok = await Task.Run(() => _dryPump.SetSpeedDemandPercent(percent));
                LogInfo(ok ? $"드라이펌프 속도 설정: {percent:F0}%" : "드라이펌프 속도 설정 실패");
            }
            finally { btnDryPumpSpeedSet.Enabled = true; }
        }

        private async void btnDryPumpSpeedStore_Click(object sender, EventArgs e)
        {
            btnDryPumpSpeedStore.Enabled = false;
            try
            {
                bool ok = await Task.Run(() => _dryPump.StoreConfiguration());
                LogInfo(ok ? "드라이펌프 설정 비휘발성 저장 완료" : "드라이펌프 저장 실패");
            }
            finally { btnDryPumpSpeedStore.Enabled = true; }
        }

        // ── 터보펌프 속도 조절 ──
        private async void btnTurboPumpSpeedSet_Click(object sender, EventArgs e)
        {
            btnTurboPumpSpeedSet.Enabled = false;
            try
            {
                ushort hz = (ushort)numTurboPumpSpeed.Value;
                ushort beforeSpeed = _turboPump.Status?.CurrentSpeed ?? 0;
                bool ok = await Task.Run(() => _turboPump.SetRotationSpeed(hz));
                if (ok)
                {
                    await Task.Delay(500);
                    await Task.Run(() => _turboPump.UpdateStatus());
                    ushort afterSpeed = _turboPump.Status?.CurrentSpeed ?? 0;
                    LogInfo($"터보펌프 속도 설정: {hz} Hz (변경 전: {beforeSpeed} Hz → 현재: {afterSpeed} Hz, Bit6={(_turboPump.CurrentControlWord & 0x0040) != 0})");
                }
                else
                {
                    LogInfo("터보펌프 속도 설정 실패");
                }
            }
            finally { btnTurboPumpSpeedSet.Enabled = true; }
        }

        private async void btnTurboPumpSpeedPerm_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show($"터보펌프 속도를 {numTurboPumpSpeed.Value} Hz로 영구 저장하시겠습니까?\n(EEPROM에 기록됩니다)",
                "영구 저장 확인", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes) return;

            btnTurboPumpSpeedPerm.Enabled = false;
            try
            {
                ushort hz = (ushort)numTurboPumpSpeed.Value;
                bool ok = await Task.Run(() => _turboPump.SetRotationSpeedPermanent(hz));
                LogInfo(ok ? $"터보펌프 속도 영구 저장: {hz} Hz" : "터보펌프 영구 저장 실패");
            }
            finally { btnTurboPumpSpeedPerm.Enabled = true; }
        }

        private async void btnTurboPumpStandbySet_Click(object sender, EventArgs e)
        {
            btnTurboPumpStandbySet.Enabled = false;
            try
            {
                ushort hz = (ushort)numTurboPumpStandbySpeed.Value;
                bool ok = await Task.Run(() => _turboPump.SetStandbySpeed(hz));
                LogInfo(ok ? $"터보펌프 대기 속도 설정: {hz} Hz" : "터보펌프 대기 속도 설정 실패");
            }
            finally { btnTurboPumpStandbySet.Enabled = true; }
        }

        #endregion

        #region 온도 제어

        private async void btnBathCirculatorStart_Click(object sender, EventArgs e)
        {
            // ★ 칠러는 토글 방식 — Start()와 Stop()이 동일한 0→1 트리거.
            //    버튼 텍스트가 아닌 실제 장비 상태를 확인하여 의도 판단.
            btnBathCirculatorStart.Enabled = false;
            try
            {
                // 실제 장비 상태를 직접 조회
                bool checkOk = await Task.Run(() => _bathCirculator.CheckStatus());
                bool actuallyRunning = checkOk && _bathCirculator.Status.IsRunning;

                if (actuallyRunning)
                {
                    // 실제 작동 중 → 정지 의도
                    if (!CheckInterlock(
                        _turboPump?.Status?.IsRunning == true,
                        _interlockConfig.ChillerStop_BlockIfTurboRunning,
                        "터보 펌프가 작동 중입니다."))
                        return;

                    await Task.Run(() => _bathCirculator.Stop());
                    ForceToggleButtonAppearance("bathcirculator", false);
                    LogInfo("칠러 정지");
                }
                else
                {
                    // 실제 정지 중 → 시작 의도
                    await Task.Run(() => _bathCirculator.Start());
                    ForceToggleButtonAppearance("bathcirculator", true);
                    LogInfo("칠러 시작");
                }
            }
            finally { btnBathCirculatorStart.Enabled = true; }
        }


        private void btnBathCirculatorSetTemp_Click(object sender, EventArgs e)
        {
            if (_bathCirculator == null || !_bathCirculator.IsConnected)
                return;

            double temp = (double)numChillerManualTemp.Value;
            if (_bathCirculator.SetTemperature(temp))
                LogInfo($"칠러 수동 온도 설정: {temp}℃");
            else
                MessageBox.Show("온도 설정에 실패했습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void btnBathCirculatorSetTime_Click(object sender, EventArgs e) { /* 기존 코드 유지 */ }

        private async void btnCh1Start_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            if (!CheckAutoRunInterlock(_interlockConfig.AutoRun_BlockManualHeaterControl, "히터"))
                return;

            int ch = (cmbTempChannel?.SelectedIndex ?? 0) + 1;
            int chIdx = ch - 1;

            // 실제 장비 상태를 직접 조회
            await _tempController.UpdateStatusAsync();
            bool isRunning = _tempController.Status.ChannelStatus[chIdx].IsRunning;

            if (isRunning)
            {
                // Stop 로직
                var chStatus = _tempController.Status.ChannelStatus[chIdx];
                string pvDisplay = chStatus.Dot == 1
                    ? $"{chStatus.PresentValue / 10.0:F1}"
                    : $"{chStatus.PresentValue}";

                string warning = chStatus.IsRampActive
                    ? "\n\n⚠ Ramp 진행 중입니다. 정지하면 Ramp가 중단됩니다."
                    : "";

                var result = MessageBox.Show(
                    $"채널 {ch} 히터를 정지하시겠습니까?\n\n" +
                    $"현재 온도: {pvDisplay}{chStatus.TemperatureUnit}" + warning,
                    "히터 정지 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result != DialogResult.Yes)
                    return;

                btnCh1Start.Enabled = false;
                try
                {
                    await Task.Run(() => _tempController.Stop(ch));
                    ForceToggleButtonAppearance($"ch{ch}", false);
                    LogInfo($"온도컨트롤러 CH{ch} 정지");

                    if (ch == 1 && _ch1TimerActive)
                        StopCh1Timer();
                }
                finally { btnCh1Start.Enabled = true; }
            }
            else
            {
                // Start 로직
                if (_interlockConfig.HeaterStart_WarnIfNoVacuum)
                {
                    double pressure = _dataCollectionService?.GetLatestPressure() ?? 0;
                    if (pressure <= 0 || pressure > 1E-3)
                    {
                        var warn = MessageBox.Show(
                            $"현재 진공 상태가 충분하지 않습니다.\n압력: {(pressure > 0 ? $"{pressure:E2} Torr" : "N/A")}\n\n계속 진행하시겠습니까?",
                            "진공 경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (warn != DialogResult.Yes)
                            return;
                    }
                }

                var chStatus = _tempController.Status.ChannelStatus[chIdx];
                string svDisplay = chStatus.Dot == 1
                    ? $"{chStatus.SetValue / 10.0:F1}"
                    : $"{chStatus.SetValue}";

                var result = MessageBox.Show(
                    $"채널 {ch} 히터를 시작하시겠습니까?\n\n" +
                    $"설정 온도: {svDisplay}{chStatus.TemperatureUnit}\n" +
                    $"Ramp: {chStatus.RampStatusText}",
                    "히터 시작 확인",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                btnCh1Start.Enabled = false;
                try
                {
                    await Task.Run(() => _tempController.Start(ch));
                    ForceToggleButtonAppearance($"ch{ch}", true);
                    LogInfo($"온도컨트롤러 CH{ch} 시작");

                    if (ch == 1 && chkCh1TimerEnabled.Checked)
                        StartCh1Timer();
                }
                finally { btnCh1Start.Enabled = true; }
            }
        }

        private async void btnCh1Stop_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            var chStatus = _tempController.Status.ChannelStatus[0];
            string pvDisplay = chStatus.Dot == 1
                ? $"{chStatus.PresentValue / 10.0:F1}"
                : $"{chStatus.PresentValue}";

            string warning = chStatus.IsRampActive
                ? "\n\n⚠ Ramp 진행 중입니다. 정지하면 Ramp가 중단됩니다."
                : "";

            var result = MessageBox.Show(
                $"채널 1 히터를 정지하시겠습니까?\n\n" +
                $"현재 온도: {pvDisplay}{chStatus.TemperatureUnit}" + warning,
                "히터 정지 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
                return;

            btnCh1Stop.Enabled = false;
            try
            {
                await Task.Run(() => _tempController.Stop(1));
                LogInfo("온도컨트롤러 CH1 정지");

                if (_ch1TimerActive)
                    StopCh1Timer();
            }
            finally { btnCh1Stop.Enabled = true; }
        }

        private void btnCh1SetTemp_Click(object sender, EventArgs e)
        {
            int ch = (cmbTempChannel?.SelectedIndex ?? 0) + 1;
            ShowTemperatureSetDialog(ch);
        }

        private async void btnCh1AutoTuning_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            int ch = (cmbTempChannel?.SelectedIndex ?? 0) + 1;

            if (MessageBox.Show($"CH{ch} 오토튜닝을 시작하시겠습니까?", "확인",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            btnCh1AutoTuning.Enabled = false;
            try
            {
                if (await Task.Run(() => _tempController.StartAutoTuning(ch)))
                {
                    LogInfo($"CH{ch} 오토튜닝 시작");
                    MessageBox.Show($"CH{ch} 오토튜닝 시작됨", "알림",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogError($"CH{ch} 오토튜닝 시작 실패");
                    MessageBox.Show("오토튜닝 실패", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally { btnCh1AutoTuning.Enabled = true; }
        }

        /// <summary>확장모듈2 (E2-CH1~4 = CH9~12) UI를 panel5에 동적 추가</summary>
        private void CreateExpansion2UI()
        {
            try
            {
                if (panel5 == null) return;

                string[] e2Names = { "Ch9", "Ch10", "Ch11", "Ch12" };
                _expansionPVLabels = new TextBox[4];

                int headerY = 116;
                int pvY = 136;
                int[] xPositions = { 3, 113, 223, 333 };

                for (int i = 0; i < 4; i++)
                {
                    // 헤더 레이블
                    var header = new Label
                    {
                        BackColor = SystemColors.Info,
                        BorderStyle = BorderStyle.FixedSingle,
                        Location = new System.Drawing.Point(xPositions[i], headerY),
                        Size = new System.Drawing.Size(110, 20),
                        Text = e2Names[i],
                        TextAlign = ContentAlignment.MiddleCenter,
                        Font = panel5.Font
                    };
                    panel5.Controls.Add(header);

                    // PV 텍스트박스
                    _expansionPVLabels[i] = new TextBox
                    {
                        Location = new System.Drawing.Point(xPositions[i], pvY),
                        Size = new System.Drawing.Size(110, 23),
                        ReadOnly = true,
                        TextAlign = HorizontalAlignment.Center,
                        Name = $"txtCh{9 + i}PresentValue",
                        Font = panel5.Font
                    };
                    panel5.Controls.Add(_expansionPVLabels[i]);
                }
            }
            catch { }
        }

        private int _lastSelectedChannel = -1;

        private void cmbTempChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            int selectedIdx = cmbTempChannel?.SelectedIndex ?? -1;
            if (selectedIdx < 0)
                return;

            int ch = selectedIdx + 1;
            int chIdx = ch - 1;

            if (chIdx >= _tempController.Status.ChannelStatus.Length)
                return;

            try
            {
                var chStatus = _tempController.Status.ChannelStatus[chIdx];
                int dot = chStatus.Dot;

                string sv = dot == 1 ? $"{chStatus.SetValue / 10.0:F1}℃" : $"{chStatus.SetValue}℃";
                string status = chStatus.IsRunning ? "Run" : "Stop";
                string mv = dot == 1 ? $"{chStatus.HeatingMV / 10.0:F1}%" : $"{chStatus.HeatingMV}%";

                if (txtCh1SetValue != null) txtCh1SetValue.Text = sv;
                if (txtCh1Status != null) txtCh1Status.Text = status;
                if (txtCh1HeatingMV != null) txtCh1HeatingMV.Text = mv;
                if (txtCh1IsAutotune != null) txtCh1IsAutotune.Text = chStatus.IsAutoTuning ? "On" : "Off";

                // Start/Stop 버튼 상태를 선택한 채널에 맞게 업데이트
                btnCh1Start.Text = chStatus.IsRunning ? "Stop" : "Start";
                btnCh1Start.BackColor = chStatus.IsRunning ? Color.FromArgb(255, 200, 200) : SystemColors.Control;
            }
            catch { }
        }

        private void btnRampSetting_Click(object sender, EventArgs e)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            int ch = (cmbTempChannel?.SelectedIndex ?? 0) + 1;

            using (var dlg = new Form())
            {
                dlg.Text = $"CH{ch} Ramp 설정";
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.Size = new Size(490, 200);

                var rampCtrl = new UI.Controls.RampSettingControl();
                rampCtrl.Dock = DockStyle.Fill;
                dlg.Controls.Add(rampCtrl);
                rampCtrl.Initialize(_tempController, ch);

                dlg.ShowDialog(this);
            }
        }

        /// <summary>
        /// 온도 설정 다이얼로그를 표시합니다.
        /// PV, Ramp 상태를 함께 표시하고 NUD로 입력 범위를 제한합니다.
        /// </summary>
        private void ShowTemperatureSetDialog(int channel)
        {
            if (_tempController == null || !_tempController.IsConnected)
                return;

            var chStatus = _tempController.Status.ChannelStatus[channel - 1];
            int dot = chStatus.Dot;
            string unit = chStatus.TemperatureUnit;
            // TM4 하드웨어 SVH 읽기 → 소프트웨어 상한과 동기화
            int hwSVH = _tempController.ReadSVHighLimit(channel);
            int maxTempRaw;
            if (hwSVH > 0)
            {
                maxTempRaw = hwSVH;
                _tempController.MaxTemperatureRaw = hwSVH;  // 소프트웨어도 동기화
            }
            else
            {
                maxTempRaw = _tempController.MaxTemperatureRaw;
            }

            string pvDisplay = dot == 1 ? $"{chStatus.PresentValue / 10.0:F1}" : $"{chStatus.PresentValue}";
            string svDisplay = dot == 1 ? $"{chStatus.SetValue / 10.0:F1}" : $"{chStatus.SetValue}";
            decimal maxDisplay = dot == 1 ? (decimal)(maxTempRaw / 10.0) : (decimal)maxTempRaw;

            using (Form dlg = new Form())
            {
                dlg.Text = $"채널 {channel} 온도 설정";
                dlg.Width = 340;
                dlg.Height = 355;
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                int y = 15;

                // 현재 온도 (PV)
                var lblPV = new Label
                {
                    Text = $"현재 온도 (PV):  {pvDisplay}{unit}",
                    Location = new Point(20, y),
                    AutoSize = true,
                    Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold)
                };
                dlg.Controls.Add(lblPV);
                y += 28;

                // 현재 설정값 (SV)
                var lblSV = new Label
                {
                    Text = $"현재 설정값 (SV):  {svDisplay}{unit}",
                    Location = new Point(20, y),
                    AutoSize = true
                };
                dlg.Controls.Add(lblSV);
                y += 28;

                // Ramp 상태
                var lblRamp = new Label
                {
                    Text = $"Ramp 상태:  {chStatus.RampStatusText}",
                    Location = new Point(20, y),
                    AutoSize = true,
                    ForeColor = chStatus.IsRampEnabled ? Color.Blue : Color.Gray
                };
                dlg.Controls.Add(lblRamp);
                y += 25;

                // 상한 온도 표시 + 변경 링크
                var lblMaxTemp = new Label
                {
                    Text = $"상한 온도:  {maxDisplay}{unit}",
                    Location = new Point(20, y),
                    AutoSize = true
                };
                dlg.Controls.Add(lblMaxTemp);

                var lnkChangeMax = new LinkLabel
                {
                    Text = "변경",
                    Location = new Point(200, y),
                    AutoSize = true
                };
                dlg.Controls.Add(lnkChangeMax);
                y += 22;

                // MV 상한 표시 + 변경 링크
                int hwMVH = _tempController.ReadMVHighLimit(channel);
                decimal mvhDisplay = hwMVH > 0 ? (dot == 1 ? (decimal)(hwMVH / 10.0) : (decimal)hwMVH) : 0;
                var lblMVH = new Label
                {
                    Text = hwMVH > 0 ? $"조작량 상한:  {mvhDisplay}%" : "조작량 상한:  --",
                    Location = new Point(20, y),
                    AutoSize = true
                };
                dlg.Controls.Add(lblMVH);

                var lnkChangeMVH = new LinkLabel
                {
                    Text = "변경",
                    Location = new Point(200, y),
                    AutoSize = true
                };
                lnkChangeMVH.LinkClicked += (s, ev) =>
                {
                    using (var mvhDlg = new Form())
                    {
                        mvhDlg.Text = $"CH{channel} 조작량 상한 변경";
                        mvhDlg.Size = new Size(280, 140);
                        mvhDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        mvhDlg.StartPosition = FormStartPosition.CenterParent;
                        mvhDlg.MaximizeBox = false;
                        mvhDlg.MinimizeBox = false;

                        var lblMvhNew = new Label { Text = "조작량 상한 (%):", Location = new Point(20, 20), AutoSize = true };
                        var nudMvh = new NumericUpDown
                        {
                            Location = new Point(140, 17),
                            Size = new Size(80, 23),
                            DecimalPlaces = dot == 1 ? 1 : 0,
                            Minimum = 0,
                            Maximum = dot == 1 ? 100.0m : 1000m,
                            Increment = dot == 1 ? 0.1m : 1m,
                            Value = mvhDisplay > 0 ? Math.Min(mvhDisplay, dot == 1 ? 100.0m : 1000m) : (dot == 1 ? 100.0m : 100m)
                        };
                        var btnMvhOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(50, 60), Size = new Size(75, 28) };
                        var btnMvhCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(140, 60), Size = new Size(75, 28) };
                        mvhDlg.Controls.AddRange(new Control[] { lblMvhNew, nudMvh, btnMvhOk, btnMvhCancel });
                        mvhDlg.AcceptButton = btnMvhOk;
                        mvhDlg.CancelButton = btnMvhCancel;

                        if (mvhDlg.ShowDialog(dlg) == DialogResult.OK)
                        {
                            short newMvhRaw = dot == 1
                                ? (short)((double)nudMvh.Value * 10)
                                : (short)nudMvh.Value;

                            if (_tempController.WriteMVHighLimit(channel, newMvhRaw))
                            {
                                mvhDisplay = dot == 1 ? (decimal)(newMvhRaw / 10.0) : (decimal)newMvhRaw;
                                lblMVH.Text = $"조작량 상한:  {mvhDisplay}%";
                                LogInfo($"CH{channel} 조작량 상한 변경: {mvhDisplay}% (raw: {newMvhRaw})");
                            }
                            else
                            {
                                MessageBox.Show("조작량 상한 변경에 실패했습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                };
                dlg.Controls.Add(lnkChangeMVH);
                y += 28;

                // 구분선
                var separator = new Label
                {
                    BorderStyle = BorderStyle.Fixed3D,
                    Location = new Point(20, y),
                    Size = new Size(280, 2)
                };
                dlg.Controls.Add(separator);
                y += 12;

                // 새 온도 입력
                var lblNew = new Label
                {
                    Text = $"새 설정 온도 ({unit}, 최대 {maxDisplay}):",
                    Location = new Point(20, y + 3),
                    AutoSize = true
                };
                dlg.Controls.Add(lblNew);

                var nudTemp = new NumericUpDown
                {
                    Location = new Point(220, y),
                    Size = new Size(80, 23),
                    TextAlign = HorizontalAlignment.Center
                };

                if (dot == 1)
                {
                    nudTemp.DecimalPlaces = 1;
                    nudTemp.Minimum = 0;
                    nudTemp.Maximum = maxDisplay;
                    nudTemp.Increment = 0.1m;
                    nudTemp.Value = Math.Min((decimal)(chStatus.SetValue / 10.0), nudTemp.Maximum);
                }
                else
                {
                    nudTemp.DecimalPlaces = 0;
                    nudTemp.Minimum = 0;
                    nudTemp.Maximum = maxDisplay;
                    nudTemp.Increment = 1;
                    nudTemp.Value = Math.Min((decimal)chStatus.SetValue, nudTemp.Maximum);
                }
                dlg.Controls.Add(nudTemp);

                // 상한 변경 링크 핸들러 등록 (nudTemp, lblNew 선언 후)
                lnkChangeMax.LinkClicked += (s, ev) =>
                {
                    using (Form maxDlg = new Form())
                    {
                        maxDlg.Text = "상한 온도 변경";
                        maxDlg.Width = 300;
                        maxDlg.Height = 160;
                        maxDlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                        maxDlg.StartPosition = FormStartPosition.CenterParent;
                        maxDlg.MaximizeBox = false;
                        maxDlg.MinimizeBox = false;

                        var lblCur = new Label
                        {
                            Text = $"현재 상한: {maxDisplay}{unit}",
                            Location = new Point(20, 15),
                            AutoSize = true
                        };
                        maxDlg.Controls.Add(lblCur);

                        var lblNewMax = new Label
                        {
                            Text = $"새 상한 ({unit}):",
                            Location = new Point(20, 48),
                            AutoSize = true
                        };
                        maxDlg.Controls.Add(lblNewMax);

                        var nudMax = new NumericUpDown
                        {
                            Location = new Point(150, 45),
                            Size = new Size(100, 23),
                            TextAlign = HorizontalAlignment.Center
                        };
                        if (dot == 1)
                        {
                            nudMax.DecimalPlaces = 1;
                            nudMax.Minimum = 10;
                            nudMax.Maximum = 9999;
                            nudMax.Increment = 1m;
                            nudMax.Value = (decimal)(maxTempRaw / 10.0);
                        }
                        else
                        {
                            nudMax.DecimalPlaces = 0;
                            nudMax.Minimum = 10;
                            nudMax.Maximum = 9999;
                            nudMax.Increment = 10;
                            nudMax.Value = (decimal)maxTempRaw;
                        }
                        maxDlg.Controls.Add(nudMax);

                        var btnMaxOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(60, 85), Size = new Size(75, 28) };
                        var btnMaxCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(150, 85), Size = new Size(75, 28) };
                        maxDlg.Controls.Add(btnMaxOk);
                        maxDlg.Controls.Add(btnMaxCancel);
                        maxDlg.AcceptButton = btnMaxOk;
                        maxDlg.CancelButton = btnMaxCancel;

                        if (maxDlg.ShowDialog(dlg) == DialogResult.OK)
                        {
                            int newMaxRaw = dot == 1
                                ? (int)((double)nudMax.Value * 10)
                                : (int)nudMax.Value;

                            // TM4 하드웨어 SVH도 함께 변경
                            if (_tempController.WriteSVHighLimit(channel, (short)newMaxRaw))
                            {
                                LogInfo($"TM4 하드웨어 SVH 변경 완료 (CH{channel}, raw: {newMaxRaw})");
                            }
                            else
                            {
                                LogWarning($"TM4 하드웨어 SVH 변경 실패 — 소프트웨어 상한만 적용");
                            }

                            _tempController.MaxTemperatureRaw = newMaxRaw;
                            maxTempRaw = newMaxRaw;
                            maxDisplay = dot == 1 ? (decimal)(newMaxRaw / 10.0) : (decimal)newMaxRaw;

                            lblMaxTemp.Text = $"상한 온도:  {maxDisplay}{unit}";
                            lblNew.Text = $"새 설정 온도 ({unit}, 최대 {maxDisplay}):";
                            nudTemp.Maximum = maxDisplay;

                            LogInfo($"히터 상한 온도 변경: {maxDisplay}{unit} (raw: {newMaxRaw})");
                        }
                    }
                };

                y += 35;

                // Ramp 경고 라벨
                var lblWarning = new Label
                {
                    Location = new Point(20, y),
                    Size = new Size(280, 30),
                    ForeColor = Color.OrangeRed,
                    Font = new Font(Font.FontFamily, 8.5f),
                    Visible = false
                };
                dlg.Controls.Add(lblWarning);

                nudTemp.ValueChanged += (s, ev) =>
                {
                    double newTemp = (double)nudTemp.Value;
                    double currentPV = dot == 1 ? chStatus.PresentValue / 10.0 : chStatus.PresentValue;
                    double diff = Math.Abs(newTemp - currentPV);

                    if (diff > 50 && !chStatus.IsRampEnabled)
                    {
                        lblWarning.Text = $"⚠ Ramp 없이 {diff:F0}{unit} 변화 → 급격한 온도 변화 주의";
                        lblWarning.Visible = true;
                    }
                    else if (diff > 100)
                    {
                        lblWarning.Text = $"⚠ {diff:F0}{unit} 큰 온도 변화 → 도달 시간이 길 수 있음";
                        lblWarning.Visible = true;
                    }
                    else
                    {
                        lblWarning.Visible = false;
                    }
                };
                y += 35;

                // 버튼
                var btnOK = new Button
                {
                    Text = "설정",
                    DialogResult = DialogResult.OK,
                    Location = new Point(100, y),
                    Size = new Size(80, 30)
                };
                var btnCancel = new Button
                {
                    Text = "취소",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(195, y),
                    Size = new Size(80, 30)
                };
                dlg.Controls.Add(btnOK);
                dlg.Controls.Add(btnCancel);
                dlg.AcceptButton = btnOK;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    short rawValue;
                    double displayValue = (double)nudTemp.Value;

                    rawValue = dot == 1 ? (short)(displayValue * 10) : (short)displayValue;

                    if (_tempController.SetTemperature(channel, rawValue))
                    {
                        string formatted = dot == 1 ? $"{displayValue:F1}" : $"{displayValue:F0}";
                        LogInfo($"CH{channel} 온도 설정: {formatted}{unit}");
                    }
                    else
                    {
                        MessageBox.Show("온도 설정에 실패했습니다.\n로그를 확인하세요.",
                            "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        #endregion

        #region 칠러 PID 제어

        private void chkChillerPIDEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (_chillerPIDService == null) return;

            bool enabled = chkChillerPIDEnabled.Checked;
            _chillerPIDService.IsEnabled = enabled;
            UpdateChillerPIDControls(enabled);

            lblPIDStatusValue.Text = enabled ? "실행 중" : "정지됨";
            lblPIDStatusValue.ForeColor = enabled ? Color.Green : Color.Red;
            LogInfo($"칠러 PID 제어 {(enabled ? "활성화" : "비활성화")}");
        }

        private void numCh2Target_ValueChanged(object sender, EventArgs e)
        {
            if (_chillerPIDService != null)
                _chillerPIDService.Ch2TargetTemperature = (double)numCh2Target.Value;
        }

        private void cmbPIDChannel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_chillerPIDService != null && cmbPIDChannel.SelectedIndex >= 0)
                _chillerPIDService.TargetChannelIndex = cmbPIDChannel.SelectedIndex;
        }

        private void numChillerBase_ValueChanged(object sender, EventArgs e)
        {
            // 더 이상 사용하지 않음 (하위 호환 유지)
        }

        private void PIDParams_ValueChanged(object sender, EventArgs e)
        {
            // PID 파라미터는 자동 관리됨 (하위 호환 유지)
        }

        private void numUpdateInterval_ValueChanged(object sender, EventArgs e)
        {
            // 업데이트 주기는 자동 관리됨 (하위 호환 유지)
        }

        private void UpdateChillerPIDControls(bool enabled)
        {
            numCh2Target.Enabled = enabled;
            cmbPIDChannel.Enabled = !enabled; // 실행 중에는 채널 변경 불가

            // PID 활성 시 수동 설정 비활성화, PID 비활성 시 수동 설정 활성화
            numChillerManualTemp.Enabled = !enabled;
            btnBathCirculatorSetTemp.Enabled = !enabled;
        }

        #endregion

        #region 온도 센서 캘리브레이션

        private void ApplyCalibrationToChannels()
        {
            if (_tempCalibrationConfig == null) return;

            // 이온게이지 모델 적용
            if (_ionGauge != null)
                _ionGauge.Model = _tempCalibrationConfig.IonGauge.Model;

            // PTR90: IG와 동일하게 표시하되 HV 버튼만 숨김
            bool isPTR90 = _tempCalibrationConfig.IonGauge.Model == IonGaugeModel.PTR90;
            if (txtIG != null)
            {
                txtIG.Visible = true;
                txtIG.LabelText = isPTR90 ? "PTR90(Torr)" : "IG(Torr)";
            }
            if (btn_iongauge != null)
                btn_iongauge.Visible = !isPTR90;

            if (_tempController?.Status?.ChannelStatus == null) return;

            int chCount = Math.Min(8, Math.Min(
                _tempCalibrationConfig.Channels.Length,
                _tempController.Status.ChannelStatus.Length));
            for (int i = 0; i < chCount; i++)
            {
                var cal = _tempCalibrationConfig.Channels[i];
                var ch = _tempController.Status.ChannelStatus[i];
                ch.CalibrationEnabled = cal.Enabled;
                ch.CalibrationOffset = cal.Offset;
                ch.CalibrationGain = cal.Gain;
            }
        }

        private void ShowTempCalibrationDialog()
        {
            using (var dlg = new TempCalibrationForm(_tempCalibrationConfig))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _tempCalibrationConfig = dlg.Configuration;
                    _tempCalibrationConfig.SaveToFile();
                    ApplyCalibrationToChannels();
                    LogInfo("센서/게이지 교정 설정 저장됨");
                }
            }
        }

        #endregion

        #region CH1 자동 시작/정지 타이머

        private void scientificPressureInput1_ValueChanged(object sender, EventArgs e)
        {
            _ch1TargetPressure = scientificPressureInput1.Value;
        }

        private void numCh1ReachCount_ValueChanged(object sender, EventArgs e)
        {
            _ch1RequiredReachCount = (int)numCh1ReachCount.Value;
        }

        private void chkCh1AutoStartEnabled_CheckedChanged(object sender, EventArgs e)
        {
            _ch1AutoStartEnabled = chkCh1AutoStartEnabled.Checked;
            UpdateCh1TimerControls();
        }

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
            bool timerEnabled = chkCh1TimerEnabled?.Checked ?? false;
            bool isLocked = _ch1TimerActive;

            // 타이머 설정 컨트롤 활성/비활성
            numCh1Hours.Enabled = timerEnabled && !isLocked;
            numCh1Minutes.Enabled = timerEnabled && !isLocked;
            numCh1Seconds.Enabled = timerEnabled && !isLocked;
            numVentTargetTemp.Enabled = timerEnabled && !isLocked;
            numCh1Tolerance.Enabled = timerEnabled && !isLocked;
            chkCh1TimerEnabled.Enabled = !isLocked;

            // 종료 동작 라디오 버튼
            if (rdoFullShutdown != null) rdoFullShutdown.Enabled = timerEnabled && !isLocked;
            if (rdoHeaterOnly != null) rdoHeaterOnly.Enabled = timerEnabled && !isLocked;

            if (!timerEnabled && _ch1TimerActive)
                StopCh1Timer();
        }

        public void StartCh1AutoStartWaiting()
        {
            if (!chkCh1TimerEnabled.Checked || !_ch1AutoStartEnabled)
            {
                MessageBox.Show("타이머와 자동 시작이 활성화되어야 합니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int hours = (int)numCh1Hours.Value;
            int minutes = (int)numCh1Minutes.Value;
            int seconds = (int)numCh1Seconds.Value;

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show("타이머 시간을 설정해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _ch1Duration = new TimeSpan(hours, minutes, seconds);
            _ch1WaitingForVacuum = true;
            _ch1WaitingForTargetTemp = false;
            _ch1TimerActive = false;
            _ch1PressureReachCount = 0;

            // 최종 목표 온도 저장 (컨트롤러의 현재 SV — 이미 설정되어 있어야 함)
            if (_tempController?.Status?.ChannelStatus?.Length > 0)
            {
                var ch1 = _tempController.Status.ChannelStatus[0];
                _ch1FinalTargetTemp = ch1.Dot > 0
                    ? ch1.SetValue / Math.Pow(10, ch1.Dot)
                    : ch1.SetValue;
            }

            _ch1AutoStopTimer.Start();
            UpdateCh1TimerControls();
            lblCh1TimeRemainingValue.Text = "진공 대기중...";
            lblCh1TimeRemainingValue.ForeColor = Color.Purple;

            LogInfo($"CH1 자동 시작 대기 - 목표: {_ch1TargetPressure:E1} Torr " +
                    $"({_ch1RequiredReachCount}회 확인), 타이머: {_ch1Duration}");
        }

        private void btnCh1AutoStart_Click(object sender, EventArgs e)
        {
            bool anyProcessActive = _ch1WaitingForVacuum || _ch1WaitingForTargetTemp || _ch1TimerActive;
            bool rampRunning = simpleRampControl1?.IsRunning ?? false;

            if (anyProcessActive || rampRunning)
            {
                string statusText = _ch1WaitingForVacuum ? "진공 대기 중"
                    : _ch1WaitingForTargetTemp ? "온도 대기 중"
                    : _ch1TimerActive ? "타이머 진행 중"
                    : rampRunning ? "램프 진행 중"
                    : "프로세스 진행 중";

                var result = MessageBox.Show(
                    $"현재 상태: {statusText}\n\n" +
                    "자동 시작/정지 프로세스를 취소하시겠습니까?\n" +
                    (rampRunning ? "진행 중인 램프도 함께 정지됩니다." : ""),
                    "자동 시작 취소",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result != DialogResult.Yes)
                    return;

                if (rampRunning)
                {
                    try
                    {
                        simpleRampControl1.StopWithEndAction(
                            _bakeoutSettings?.EndAction ?? BakeoutEndAction.HeaterOff);
                        LogInfo("[베이크 아웃] 램프 정지됨 (사용자 취소)");
                    }
                    catch (Exception ex)
                    {
                        LogWarning($"램프 정지 실패: {ex.Message}");
                    }
                }

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
                HandleVacuumWaitPhase();
                return;
            }

            // 2단계: 온도 대기
            if (_ch1WaitingForTargetTemp)
            {
                HandleTemperatureWaitPhase();
                return;
            }

            // 3단계: 타이머 카운트다운
            if (_ch1TimerActive)
            {
                HandleTimerCountdownPhase();
            }
        }

        private void HandleVacuumWaitPhase()
        {
            // ── 이온게이지 자동 활성화 ──
            // DO 기반으로 IG HV 상태 확인
            bool igOn = _ioModule?.LastValidDOValues?.IsIonGaugeHVOn == true;

            if (!igOn)
            {
                double piraniPressure = GetCurrentPiraniPressure();

                if (piraniPressure > 0 && piraniPressure <= 1E-3 && !_igAutoActivating)
                {
                    _igAutoActivating = true;
                    LogInfo($"피라니 압력 {piraniPressure:E2} Torr — 이온게이지 자동 활성화");
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (await _ioModule.ControlIonGaugeHVAsync(true))
                            {
                                // IG 안정화 대기 (3초)
                                await Task.Delay(3000);
                                RunOnUIThread(() => LogInfo("이온게이지 HV ON 성공 (자동)"));
                            }
                            else
                            {
                                RunOnUIThread(() => LogWarning("이온게이지 자동 활성화 실패 (명령 실패)"));
                            }
                        }
                        catch (Exception ex)
                        {
                            RunOnUIThread(() => LogWarning($"이온게이지 자동 활성화 실패: {ex.Message}"));
                        }
                        finally
                        {
                            _igAutoActivating = false;
                        }
                    });

                    // IG 안정화 대기 — 이번 틱에서는 압력 체크 스킵
                    lblCh1TimeRemainingValue.Text = "이온게이지 활성화 중...";
                    lblCh1TimeRemainingValue.ForeColor = Color.Cyan;
                    return;
                }
                else if (_igAutoActivating)
                {
                    // IG 활성화 진행 중 (이전 틱에서 시작됨)
                    lblCh1TimeRemainingValue.Text = "이온게이지 활성화 중...";
                    lblCh1TimeRemainingValue.ForeColor = Color.Cyan;
                    return;
                }
                else
                {
                    // IG 꺼져있고 피라니도 아직 높음 → 피라니 압력 표시
                    string pressureText = piraniPressure > 0 ? $"{piraniPressure:E2}" : "N/A";
                    lblCh1TimeRemainingValue.Text =
                        $"진공 대기 ({pressureText} / {_ch1TargetPressure:E1} Torr) [IG OFF]";
                    lblCh1TimeRemainingValue.ForeColor = Color.Purple;
                    _ch1PressureReachCount = 0;
                    return;
                }
            }

            // ── IG가 켜져 있으면 이온게이지 압력으로 목표 확인 ──
            double currentPressure = GetCurrentIonGaugePressure();

            if (currentPressure > 0 && currentPressure <= _ch1TargetPressure)
            {
                _ch1PressureReachCount++;

                if (_ch1PressureReachCount >= _ch1RequiredReachCount)
                {
                    _ch1WaitingForVacuum = false;
                    _ch1PressureReachCount = 0;
                    LogInfo($"목표 진공 도달 ({currentPressure:E2} Torr, " +
                            $"{_ch1RequiredReachCount}회 연속) - CH1 자동 시작");

                    Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Run(() => _tempController.Start(1));
                            RunOnUIThread(() =>
                            {
                                LogInfo("CH1 자동 시작됨");

                                if (_bakeoutSettings?.EnableAutoRampUp == true)
                                {
                                    StartBakeoutRampUp();
                                    _ch1WaitingForTargetTemp = false;
                                    lblCh1TimeRemainingValue.Text = "램프 진행중...";
                                    lblCh1TimeRemainingValue.ForeColor = Color.Green;
                                }
                                else
                                {
                                    _ch1WaitingForTargetTemp = true;
                                    lblCh1TimeRemainingValue.Text = "온도 대기중...";
                                    lblCh1TimeRemainingValue.ForeColor = Color.Orange;
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            RunOnUIThread(() =>
                            {
                                LogError($"CH1 자동 시작 실패: {ex.Message}");
                                StopCh1Timer();
                            });
                        }
                    });
                }
                else
                {
                    lblCh1TimeRemainingValue.Text =
                        $"진공 확인 중... ({_ch1PressureReachCount}/{_ch1RequiredReachCount})";
                    lblCh1TimeRemainingValue.ForeColor = Color.Purple;
                }
            }
            else
            {
                _ch1PressureReachCount = 0;
                string pressureText = currentPressure > 0 ? $"{currentPressure:E2}" : "N/A";
                lblCh1TimeRemainingValue.Text =
                    $"진공 대기 ({pressureText} / {_ch1TargetPressure:E1} Torr)";
                lblCh1TimeRemainingValue.ForeColor = Color.Purple;
            }
        }

        private void HandleTemperatureWaitPhase()
        {
            if (_tempController?.Status?.ChannelStatus == null ||
                _tempController.Status.ChannelStatus.Length == 0)
                return;

            var ch1 = _tempController.Status.ChannelStatus[0];
            double pv = ch1.PresentValue;
            if (ch1.Dot > 0)
            {
                pv /= Math.Pow(10, ch1.Dot);
            }

            // 최종 목표 온도와 비교 (live SV가 아닌 저장된 값 사용 — 램프 중 SV 변동 무시)
            double target = _ch1FinalTargetTemp;

            if (target > 0 && Math.Abs(pv - target) <= _ch1TargetTolerance)
            {
                _ch1WaitingForTargetTemp = false;
                _ch1TimerActive = true;
                _ch1StartTime = DateTime.Now;
                lblCh1TimeRemainingValue.ForeColor = Color.Blue;
                LogInfo($"CH1 목표 온도 도달 (PV:{pv:F1}°C, 목표:{target:F1}°C) - 타이머 시작");
            }
            else
            {
                lblCh1TimeRemainingValue.Text = $"온도 대기 ({pv:F1}/{target:F1}°C)";
                lblCh1TimeRemainingValue.ForeColor = Color.Orange;
            }
        }

        private void HandleTimerCountdownPhase()
        {
            TimeSpan remaining = _ch1Duration - (DateTime.Now - _ch1StartTime);

            if (remaining.TotalSeconds <= 0)
            {
                LogInfo($"[타이머] 카운트다운 만료 → StopCh1WithTimer 호출 " +
                        $"(경과: {FmtTime(DateTime.Now - _ch1StartTime)})");
                StopCh1WithTimer();
                lblCh1TimeRemainingValue.Text = "00:00:00";
                lblCh1TimeRemainingValue.ForeColor = Color.Red;
            }
            else
            {
                lblCh1TimeRemainingValue.Text =
                    $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                lblCh1TimeRemainingValue.ForeColor =
                    remaining.TotalSeconds <= 60 ? Color.Red
                    : remaining.TotalMinutes <= 5 ? Color.Orange
                    : Color.Blue;
            }
        }

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
                // 설정값 범위 검증
                double targetTemp = Math.Max(25, Math.Min(300, _bakeoutSettings.TargetTemperature));
                double rampRate = Math.Max(0.5, Math.Min(30, _bakeoutSettings.RampRate));

                if (targetTemp != _bakeoutSettings.TargetTemperature || rampRate != _bakeoutSettings.RampRate)
                {
                    LogWarning($"[베이크 아웃] 설정값 범위 보정: " +
                        $"온도 {_bakeoutSettings.TargetTemperature} → {targetTemp}°C, " +
                        $"속도 {_bakeoutSettings.RampRate} → {rampRate}°C/min");
                }

                simpleRampControl1.AutoStartTimerOnTargetReached = _bakeoutSettings.AutoStartTimerOnTargetReached;
                simpleRampControl1.EndAction = _bakeoutSettings.EndAction;
                simpleRampControl1.HoldAfterComplete =
                    (_bakeoutSettings.EndAction == BakeoutEndAction.MaintainTemperature);

                bool success = await simpleRampControl1.StartRampAsync(
                    targetTemp, rampRate, _bakeoutSettings.ProfileName);

                if (success)
                {
                    LogInfo($"[베이크 아웃] 램프 업 시작: {targetTemp}°C, {rampRate}°C/min");
                    UpdateCh1TimerControls();
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

        private double GetCurrentPiraniPressure()
        {
            try
            {
                var aiData = _dataCollectionService?.GetLatestAIData();
                if (aiData != null && _piraniGauge != null)
                    return _piraniGauge.ConvertVoltageToPressureInTorr(aiData.ExpansionVoltageValues[1]);
            }
            catch { }
            return -1;
        }

        private double GetCurrentAtmPressure()
        {
            try
            {
                var aiData = _dataCollectionService?.GetLatestAIData();
                if (aiData != null && _atmSwitch != null)
                    return _atmSwitch.ConvertVoltageToPressureInkPa(aiData.ExpansionVoltageValues[0]);
            }
            catch { }
            return -1;
        }

        /// <summary>
        /// 인터락: 벤트밸브 열림 상태에서 ATM 압력 ≥ 90 kPa → 배기밸브 자동 열림
        /// 챔버 과압 방지 안전장치
        /// </summary>
        private bool _ventOverpressureInterlockTriggered = false;
        private void CheckVentOverpressureInterlock(UIDataSnapshot snapshot)
        {
            if (_interlockConfig == null || !_interlockConfig.VentValve_AutoOpenExhaustAtHighPressure)
                return;
            if (snapshot.VentValveStatus != "Opened")
            {
                _ventOverpressureInterlockTriggered = false;
                return;
            }

            if (snapshot.AtmPressure >= 90 && !_ventOverpressureInterlockTriggered)
            {
                _ventOverpressureInterlockTriggered = true;
                // 배기밸브가 아직 닫혀있으면 자동으로 열기
                if (snapshot.ExhaustValveStatus != "Opened")
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            bool result = await _ioModule.ControlExhaustValveAsync(true);
                            if (result)
                            {
                                LogInfo($"[인터락] 벤트 중 과압 감지 (ATM: {snapshot.AtmPressure:F1} kPa ≥ 90) → 배기밸브 자동 열림");
                                SetExhaustValveStatus("Opened");
                            }
                            else
                                LogInfo($"[인터락] 배기밸브 자동 열림 실패 (ATM: {snapshot.AtmPressure:F1} kPa)");
                        }
                        catch (Exception ex)
                        {
                            LogInfo($"[인터락] 배기밸브 제어 오류: {ex.Message}");
                        }
                    });
                }
            }
            else if (snapshot.AtmPressure < 85)
            {
                // 히스테리시스: 85 kPa 미만으로 내려가야 리셋 (떨림 방지)
                _ventOverpressureInterlockTriggered = false;
            }
        }

        private void StartCh1Timer()
        {
            if (!chkCh1TimerEnabled.Checked) return;

            int hours = (int)numCh1Hours.Value;
            int minutes = (int)numCh1Minutes.Value;
            int seconds = (int)numCh1Seconds.Value;

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show("타이머 시간을 설정해주세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _ch1Duration = new TimeSpan(hours, minutes, seconds);
            _ch1WaitingForVacuum = false;
            _ch1WaitingForTargetTemp = true;
            _ch1TimerActive = false;

            // 최종 목표 온도 저장 (컨트롤러의 현재 SV — 램프 시작 전)
            if (_tempController?.Status?.ChannelStatus?.Length > 0)
            {
                var ch1 = _tempController.Status.ChannelStatus[0];
                _ch1FinalTargetTemp = ch1.Dot > 0
                    ? ch1.SetValue / Math.Pow(10, ch1.Dot)
                    : ch1.SetValue;
            }

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
            _igAutoActivating = false;
            _ch1AutoStopTimer.Stop();

            lblCh1TimeRemainingValue.Text = "00:00:00";
            lblCh1TimeRemainingValue.ForeColor = Color.Blue;
            UpdateCh1TimerControls();
            LogInfo("CH1 타이머 정지");
        }

        private async void StopCh1WithTimer()
        {
            // "히터만 정지" 체크 시 → 히터만 끄고 나머지 유지
            if (rdoHeaterOnly?.Checked == true)
            {
                LogInfo("═══ CH1 타이머 만료 — 히터만 정지 (펌프/밸브 유지) ═══");
                StopCh1Timer();
                try
                {
                    if (_tempController?.IsConnected == true)
                        await Task.Run(() => _tempController.Stop(1));
                    LogInfo("CH1 히터 정지 완료");
                }
                catch (Exception ex) { LogError($"히터 정지 오류: {ex.Message}"); }
                return;
            }

            LogInfo("═══ CH1 타이머 만료 - 종료 시퀀스 시작 ═══");

            // 타이머 플래그 먼저 정리
            StopCh1Timer();

            // ── 1단계: 현재 상태 수집 (실패해도 기본값으로 진행) ──
            bool needCh1Stop = false;
            bool needIonGaugeOff = false;
            bool needTurboPumpStop = false;
            bool needDryPumpStop = false;
            bool needGateValveClose = false;
            bool needVentValvesOpen = false;

            try
            {
                // 상태 갱신을 위해 잠시 대기 (마지막 폴링 결과 반영)
                await Task.Delay(500);

                needCh1Stop = _tempController?.IsConnected == true
                    && _tempController.Status?.ChannelStatus != null
                    && _tempController.Status.ChannelStatus.Length > 0
                    && _tempController.Status.ChannelStatus[0].IsRunning;

                needTurboPumpStop = _turboPump?.IsConnected == true
                    && (_turboPump.Status?.IsRunning == true || _turboPump.Status?.CurrentSpeed > 0);

                needDryPumpStop = _dryPump?.IsConnected == true
                    && _dryPump.Status?.IsRunning == true;

                // DI/DO 기반으로 밸브 상태 확인
                var doValues = _ioModule?.LastValidDOValues;
                needIonGaugeOff = doValues?.IsIonGaugeHVOn == true;
                needVentValvesOpen = !(doValues?.IsVentValveOn == true) || !(doValues?.IsExhaustValveOn == true);
                needGateValveClose = _ioModule?.GateValvePosition != "Closed";

                LogInfo($"[종료 시퀀스] 상태 판단: CH1정지={needCh1Stop}, IG OFF={needIonGaugeOff}, " +
                        $"터보정지={needTurboPumpStop}, 드라이정지={needDryPumpStop}, " +
                        $"GV닫기={needGateValveClose}, VV/EV열기={needVentValvesOpen}");
            }
            catch (Exception ex)
            {
                // 상태 수집 실패해도 안전한 기본값으로 진행: 모두 실행
                needCh1Stop = _tempController?.IsConnected == true;
                needTurboPumpStop = _turboPump?.IsConnected == true;
                needDryPumpStop = _dryPump?.IsConnected == true;
                needIonGaugeOff = true;
                needGateValveClose = true;
                needVentValvesOpen = true;
                LogWarning($"[종료 시퀀스] 상태 수집 실패 → 전체 실행 모드: {ex.Message}");
            }

            // ── 2단계: CH1 히터 정지 ──
            if (needCh1Stop)
            {
                try
                {
                    await ExecuteWithRetry("CH1 정지", async () =>
                    {
                        await Task.Run(() => _tempController.Stop(1));
                        await Task.Delay(1000);
                        return _tempController.Status?.ChannelStatus?[0].IsRunning == false;
                    }, btnCh1Stop, 3, 2000);
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] CH1 정지 실패: {ex.Message}"); }
            }

            // ── 3단계: 이온게이지 HV OFF ──
            if (needIonGaugeOff)
            {
                try
                {
                    await ExecuteWithRetry("이온게이지 OFF", async () =>
                    {
                        bool r = await _ioModule.ControlIonGaugeHVAsync(false);
                        if (!r) return false;
                        await Task.Delay(1000);
                        // ★ DO 기반으로 IG HV 상태 확인
                        return _ioModule?.LastValidDOValues?.IsIonGaugeHVOn != true;
                    }, btn_iongauge, 3, 1000);
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] 이온게이지 OFF 실패: {ex.Message}"); }
            }

            // ── 4단계: 터보펌프 정지 + 감속 대기 ──
            if (needTurboPumpStop)
            {
                try
                {
                    await ExecuteWithRetry("터보펌프 정지", async () =>
                    {
                        await Task.Run(() => _turboPump.Stop());
                        return true;
                    }, null, 3, 2000);

                    LogInfo("[종료 시퀀스] 터보펌프 감속 대기 중...");
                    await WaitForCondition(
                        () => !_turboPump.IsRunning || (_turboPump.Status?.CurrentSpeed ?? 0) == 0,
                        1800, 30, null);
                    LogInfo("[종료 시퀀스] 터보펌프 감속 완료");
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] 터보펌프 정지 실패: {ex.Message}"); }
            }

            // ── 5단계: 게이트 밸브 닫기 (챔버 격리 후 펌프 정지) ──
            if (needGateValveClose)
            {
                try
                {
                    await ExecuteWithRetry("게이트 밸브 닫기", async () =>
                    {
                        bool r = await _ioModule.ControlGateValveAsync(false);
                        if (!r) return false;
                        await Task.Delay(3000);
                        // ★ DI 기반으로 게이트 밸브 상태 확인
                        return _ioModule?.GateValvePosition == "Closed";
                    }, btn_GV, 3, 2000);
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] 게이트 밸브 닫기 실패: {ex.Message}"); }
            }

            // ── 6단계: 드라이펌프 정지 ──
            if (needDryPumpStop)
            {
                try
                {
                    await ExecuteWithRetry("드라이펌프 정지", async () =>
                    {
                        await Task.Run(() => _dryPump.Stop());
                        await Task.Delay(2000);
                        return _dryPump.Status?.IsRunning == false;
                    }, null, 3, 2000);
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] 드라이펌프 정지 실패: {ex.Message}"); }
            }

            // ── 7단계: 벤트 밸브 열기 (벤트 먼저 → 대기 → 배기) ──
            if (needVentValvesOpen)
            {
                try
                {
                    // ★ DO 기반으로 밸브 상태 확인
                    var curDO = _ioModule?.LastValidDOValues;
                    bool vo = curDO?.IsVentValveOn == true;
                    bool eo = curDO?.IsExhaustValveOn == true;

                    // 벤트 밸브 먼저 열기
                    if (!vo)
                    {
                        await ExecuteWithRetry("VV 열기", async () =>
                        {
                            bool r = await _ioModule.ControlVentValveAsync(true);
                            await Task.Delay(1000);
                            return r;
                        }, btn_VV, 3, 1000);
                    }

                    // 벤트 후 충분한 대기 시간 (챔버 압력 안정화)
                    LogInfo("[종료 시퀀스] 벤트 밸브 열림 → 배기 밸브 열기 전 30초 대기");
                    await Task.Delay(30000);

                    // 배기 밸브 열기
                    if (!eo)
                    {
                        await ExecuteWithRetry("EV 열기", async () =>
                        {
                            bool r = await _ioModule.ControlExhaustValveAsync(true);
                            await Task.Delay(1000);
                            return r;
                        }, btn_EV, 3, 1000);
                    }
                }
                catch (Exception ex) { LogError($"[종료 시퀀스] 벤트 밸브 열기 실패: {ex.Message}"); }
            }

            // ── 8단계: 냉각 대기 ──
            try
            {
                double currentTempRaw = _tempController?.Status?.ChannelStatus?[0].PresentValue ?? 0;
                if (currentTempRaw > _ch1VentTargetTemp * 10)
                {
                    LogInfo($"[종료 시퀀스] CH1 온도 {_ch1VentTargetTemp}°C 도달 대기 " +
                            $"(현재: {currentTempRaw / 10.0:F1}°C)");
                    await WaitForCondition(
                        () => (_tempController?.Status?.ChannelStatus?[0].PresentValue / 10.0 ?? 999) <= _ch1VentTargetTemp,
                        7200, 10, null);
                    LogInfo("[종료 시퀀스] 냉각 완료");
                }
            }
            catch (Exception ex) { LogWarning($"[종료 시퀀스] 냉각 대기 스킵: {ex.Message}"); }

            // ── 9단계: 벤트 밸브 닫기 ──
            try
            {
                // ★ DO 기반으로 밸브 상태 확인
                var finalDO = _ioModule?.LastValidDOValues;
                if (finalDO?.IsVentValveOn == true)
                {
                    await ExecuteWithRetry("VV 닫기", async () =>
                    {
                        bool r = await _ioModule.ControlVentValveAsync(false);
                        await Task.Delay(1000);
                        return r;
                    }, btn_VV, 3, 1000);
                }
                if (finalDO?.IsExhaustValveOn == true)
                {
                    await ExecuteWithRetry("EV 닫기", async () =>
                    {
                        bool r = await _ioModule.ControlExhaustValveAsync(false);
                        await Task.Delay(1000);
                        return r;
                    }, btn_EV, 3, 1000);
                }
            }
            catch (Exception ex) { LogWarning($"[종료 시퀀스] 벤트 밸브 닫기 실패: {ex.Message}"); }

            LogInfo("═══ CH1 종료 시퀀스 완료 ═══");
            ShowMessageBox("CH1 종료 시퀀스 완료", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private async Task<bool> ExecuteWithRetry(string name, Func<Task<bool>> op,
            Button btn, int maxRetries, int delay)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    LogInfo($"{name} 시도 {i + 1}/{maxRetries}");
                    SetButtonEnabled(btn, false);
                    if (await op())
                    {
                        LogInfo($"{name} 완료");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogError($"{name} 오류: {ex.Message}");
                }
                finally
                {
                    SetButtonEnabled(btn, true);
                }
                if (i < maxRetries - 1)
                    await Task.Delay(delay);
            }
            LogWarning($"{name} 실패");
            return false;
        }

        private async Task<bool> WaitForCondition(Func<bool> cond, int maxSec,
            int interval, Action log)
        {
            int cnt = 0;
            while (cnt < maxSec)
            {
                if (cond()) return true;
                await Task.Delay(1000);
                if (cnt % interval == 0 && log != null)
                    log();
                cnt++;
            }
            return false;
        }

        private void numVentTargetTemp_ValueChanged(object sender, EventArgs e)
        {
            _ch1VentTargetTemp = Convert.ToDouble(numVentTargetTemp.Value);
        }

        /// <summary>
        /// 목표 온도 도달 허용 오차 변경 이벤트.
        /// Designer에서 numCh1Tolerance NUD를 배치하고 이 핸들러를 연결하세요.
        /// 권장: Minimum=0.1, Maximum=10.0, DecimalPlaces=1, Increment=0.1, Value=1.0
        /// </summary>
        private void numCh1Tolerance_ValueChanged(object sender, EventArgs e)
        {
            _ch1TargetTolerance = Convert.ToDouble(numCh1Tolerance.Value);
        }

        #endregion

        #region 인터락 체크

        private bool CheckInterlock(bool blockCondition, bool interlockEnabled, string message)
        {
            if (!interlockEnabled || !blockCondition)
                return true;
            MessageBox.Show(message, "인터락", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        private bool CheckAutoRunInterlock(bool interlockEnabled, string target)
        {
            if (_autoRunService?.IsRunning != true || !interlockEnabled)
                return true;
            MessageBox.Show($"오토런 실행 중에는 {target}을(를) 조작할 수 없습니다.", "인터락",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        #endregion

        #region 유틸리티

        /// <summary>
        /// TimeSpan을 총 시간:분:초 형태로 포맷 (24시간 이상 지원)
        /// </summary>
        private static string FmtTime(TimeSpan ts)
        {
            return $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        #endregion

        #region 로깅

        private void LogInfo(string message) => AsyncLoggingService.Instance.LogInfo(message);
        private void LogError(string message, Exception ex = null) => AsyncLoggingService.Instance.LogError(message, ex);
        private void LogWarning(string message) => AsyncLoggingService.Instance.LogWarning(message);
        private void LogDebug(string message) => AsyncLoggingService.Instance.LogDebug(message);

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
                    if (txtLog.Lines.Length > 1000)
                        txtLog.Lines = txtLog.Lines.Skip(500).ToArray();

                    txtLog.AppendText(logMessage + Environment.NewLine);
                    txtLog.SelectionStart = txtLog.Text.Length;
                    txtLog.ScrollToCaret();
                }
            }
            catch { }
        }

        private void InitializeDataLogging()
        {
            DataLoggerService.Instance.StartLogging("Pressure", new List<string>
                { "ATM(kPa)", "Pirani(Torr)", "Ion(Torr)", "IonStatus", "GateValve",
                  "VentValve", "ExhaustValve", "IonGaugeHV", "Delta Freq" });

            DataLoggerService.Instance.StartLogging("DryPump", new List<string>
                { "Status", "Frequency(Hz)", "Current(A)", "Temperature(°C)", "HasWarning", "HasFault" });

            DataLoggerService.Instance.StartLogging("TurboPump", new List<string>
                { "Status", "Speed(RPM)", "Current(A)", "Temperature(°C)", "HasWarning", "HasError" });

            DataLoggerService.Instance.StartLogging("BathCirculator", new List<string>
                { "Status", "CurrentTemp(°C)", "TargetTemp(°C)", "Mode", "Time", "HasError", "HasWarning" });

            var tcHeaders = new List<string>();
            string[] chNames = { "M1", "M2", "M3", "M4", "E1", "E2", "E3", "E4" };
            foreach (var n in chNames)
            {
                tcHeaders.Add($"{n}_PV(°C)");
                tcHeaders.Add($"{n}_SV(°C)");
                tcHeaders.Add($"{n}_MV(%)");
                tcHeaders.Add($"{n}_Status");
            }
            DataLoggerService.Instance.StartLogging("TempController", tcHeaders);

            DataLoggerService.Instance.StartLogging("ChillerPID", new List<string>
                { "Ch2_PV(°C)", "Ch2_Target(°C)", "PID_Output", "Chiller_Setpoint(°C)",
                  "Kp", "Ki", "Kd", "Integral", "Error" });
        }

        private void AddLoggingMenu()
        {
            if (menuStrip == null) return;

            var menuLogging = new ToolStripMenuItem("로깅(&L)");
            _menuStartLogging = new ToolStripMenuItem("로깅 시작");
            _menuStartLogging.Click += (s, e) => ToggleLogging(true);
            _menuStopLogging = new ToolStripMenuItem("로깅 중지");
            _menuStopLogging.Click += (s, e) => ToggleLogging(false);

            var menuSetInterval = new ToolStripMenuItem($"수집 주기 설정 ({_dataLoggingIntervalSeconds}초)");
            menuSetInterval.Click += (s, e) => ShowLoggingIntervalDialog(menuSetInterval);

            var menuOpenLogFolder = new ToolStripMenuItem("로그 폴더 열기");
            menuOpenLogFolder.Click += (s, e) => OpenFolder(PathSettings.Instance.LogsPath);
            var menuOpenDataFolder = new ToolStripMenuItem("데이터 폴더 열기");
            menuOpenDataFolder.Click += (s, e) => OpenFolder(PathSettings.Instance.DataPath);

            menuLogging.DropDownItems.AddRange(new ToolStripItem[]
            {
                _menuStartLogging, _menuStopLogging, new ToolStripSeparator(),
                menuSetInterval, new ToolStripSeparator(),
                menuOpenLogFolder, menuOpenDataFolder
            });
            menuStrip.Items.Add(menuLogging);
            UpdateLoggingMenuState();
        }

        private void AddInterlockMenu()
        {
            if (menuStrip == null) return;

            var menuInterlock = new ToolStripMenuItem("인터락(&I)");
            var menuInterlockSettings = new ToolStripMenuItem("인터락 설정");
            menuInterlockSettings.Click += (s, e) =>
            {
                using (var dlg = new InterlockSettingsForm(_interlockConfig))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _interlockConfig = dlg.Configuration;
                        _interlockConfig.SaveToFile();
                        LogInfo("인터락 설정이 변경되었습니다.");
                    }
                }
            };
            menuInterlock.DropDownItems.Add(menuInterlockSettings);
            menuStrip.Items.Add(menuInterlock);
        }

        private void AddPathSettingsMenu()
        {
            if (menuStrip == null) return;

            var menuSettings = new ToolStripMenuItem("설정(&S)");
            var menuPathSettings = new ToolStripMenuItem("파일 경로 설정");
            menuPathSettings.Click += (s, e) =>
            {
                using (var dlg = new PathSettingsForm())
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        LogInfo("파일 경로 설정이 변경되었습니다. 앱 재시작 후 적용됩니다.");
                    }
                }
            };
            var menuCommSettings = new ToolStripMenuItem("통신 포트 설정");
            menuCommSettings.Click += (s, e) =>
            {
                using (var dlg = new CommSettingsForm(_commPortSettings))
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        _commPortSettings = CommPortSettings.Load() ?? CommPortSettings.CreateDefault();
                        LogInfo("통신 포트 설정이 변경되었습니다. 앱 재시작 후 적용됩니다.");
                    }
                }
            };

            var menuTempCalibration = new ToolStripMenuItem("센서 / 게이지 교정");
            menuTempCalibration.Click += (s, e) => ShowTempCalibrationDialog();

            menuSettings.DropDownItems.Add(menuPathSettings);
            menuSettings.DropDownItems.Add(menuCommSettings);
            menuSettings.DropDownItems.Add(new ToolStripSeparator());
            menuSettings.DropDownItems.Add(menuTempCalibration);
            menuSettings.DropDownItems.Add(new ToolStripSeparator());
            var menuLockPassword = new ToolStripMenuItem("AutoRun 잠금 비밀번호");
            menuLockPassword.Click += (s, ev) =>
            {
                using var dlg = new Form
                {
                    Text = "AutoRun 잠금 비밀번호 설정",
                    Size = new Size(320, 180),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false, MinimizeBox = false
                };
                var lblCur = new Label { Text = "현재:", Location = new Point(15, 18), Size = new Size(50, 20) };
                var txtCur = new TextBox { Location = new Point(70, 16), Size = new Size(220, 23), PasswordChar = '*' };
                var lblNew = new Label { Text = "새 비번:", Location = new Point(15, 48), Size = new Size(50, 20) };
                var txtNew = new TextBox { Location = new Point(70, 46), Size = new Size(220, 23), PasswordChar = '*' };
                var lblInfo = new Label { Text = "비어있으면 비밀번호 해제", Location = new Point(70, 75), Size = new Size(220, 18), ForeColor = Color.Gray, Font = new Font("맑은 고딕", 8F) };
                var btnSave = new Button { Text = "저장", Location = new Point(130, 100), Size = new Size(75, 30), DialogResult = DialogResult.OK };
                var btnCncl = new Button { Text = "취소", Location = new Point(215, 100), Size = new Size(75, 30), DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { lblCur, txtCur, lblNew, txtNew, lblInfo, btnSave, btnCncl });
                dlg.AcceptButton = btnSave;
                dlg.CancelButton = btnCncl;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                if (_autoRunLock.HasPassword && !_autoRunLock.VerifyPassword(txtCur.Text))
                {
                    MessageBox.Show("현재 비밀번호가 틀렸습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(txtNew.Text))
                {
                    _autoRunLock.ClearPassword();
                    MessageBox.Show("비밀번호가 해제되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    _autoRunLock.SetPassword(txtNew.Text);
                    MessageBox.Show("비밀번호가 설정되었습니다.", "완료", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            menuSettings.DropDownItems.Add(menuLockPassword);
            menuStrip.Items.Add(menuSettings);

            // 도구 메뉴
            var menuTools = new ToolStripMenuItem("도구(&T)");
            var menuBakeoutSim = new ToolStripMenuItem("베이크아웃 시뮬레이션");
            menuBakeoutSim.Click += (s, e) =>
            {
                using (var dlg = new BakeoutSimulationForm())
                    dlg.ShowDialog(this);
            };
            menuTools.DropDownItems.Add(menuBakeoutSim);
            menuStrip.Items.Add(menuTools);

            // 챔버 작업 모드 버튼 (메뉴바 우측 독립)
            var menuChamberWork = new ToolStripMenuItem("챔버 작업")
            {
                BackColor = Color.FromArgb(255, 200, 50),
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                Alignment = ToolStripItemAlignment.Right
            };
            menuChamberWork.Click += (s, e) => EnterChamberWorkMode();
            menuStrip.Items.Add(menuChamberWork);
        }

        private void ToggleLogging(bool enable)
        {
            _isLoggingEnabled = enable;
            LogInfo($"데이터 로깅 {(enable ? "활성화" : "비활성화")}");
            UpdateLoggingMenuState();
        }

        private void ShowLoggingIntervalDialog(ToolStripMenuItem menuItem)
        {
            using (var dlg = new Form())
            {
                dlg.Text = "데이터 수집 주기 설정";
                dlg.Size = new Size(300, 150);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;

                var lbl = new Label { Text = "수집 주기 (초):", Location = new Point(20, 25), AutoSize = true };
                var nud = new NumericUpDown
                {
                    Location = new Point(130, 22),
                    Size = new Size(80, 23),
                    Minimum = 1,
                    Maximum = 3600,
                    Value = _dataLoggingIntervalSeconds
                };
                var btnOk = new Button { Text = "확인", DialogResult = DialogResult.OK, Location = new Point(70, 65), Size = new Size(70, 28) };
                var btnCancel = new Button { Text = "취소", DialogResult = DialogResult.Cancel, Location = new Point(150, 65), Size = new Size(70, 28) };

                dlg.Controls.AddRange(new Control[] { lbl, nud, btnOk, btnCancel });
                dlg.AcceptButton = btnOk;
                dlg.CancelButton = btnCancel;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _dataLoggingIntervalSeconds = (int)nud.Value;
                    menuItem.Text = $"수집 주기 설정 ({_dataLoggingIntervalSeconds}초)";
                    LogInfo($"데이터 수집 주기 변경: {_dataLoggingIntervalSeconds}초");
                }
            }
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
                MessageBox.Show($"폴더 열기 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void HandleDeviceCommunicationError(string deviceName)
        {
            try
            {
                // 연결 표시기 업데이트 (LED → 빨강)
                SetConnectionStatus(deviceName, false);

                // 장비를 Disconnected 상태로 전환 (포트는 닫지 않음 — DeviceBase.Disconnect 구조)
                // → IsConnected = false → 데이터 수집 폴링도 중단됨
                switch (deviceName)
                {
                    case "IOModule":
                        _ioModule?.Disconnect();
                        break;
                    case "DryPump":
                        _dryPump?.Disconnect();
                        break;
                    case "TurboPump":
                        _turboPump?.Disconnect();
                        break;
                    case "BathCirculator":
                        _bathCirculator?.Disconnect();
                        break;
                    case "TempController":
                        _tempController?.Disconnect();
                        break;
                }

                // 데이터 로깅 중단
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
                }

                LogWarning($"{deviceName} 통신 오류 다수 발생 — 연결 끊김 처리 (LED 빨강 + 폴링 중단)");
            }
            catch (Exception ex)
            {
                LogError($"{deviceName} 통신 오류 처리 실패", ex);
            }
        }

        private void StopDeviceDataLogging(string deviceName)
        {
            try
            {
                string logType = deviceName switch
                {
                    "ECODRY 25 plus" => "DryPump",
                    "MAG W 1300" => "TurboPump",
                    "LK-1000" => "BathCirculator",
                    _ when deviceName.Contains("TM4") => "TempController",
                    "IO Module" => "Pressure",
                    _ => null
                };

                if (!string.IsNullOrEmpty(logType))
                {
                    DataLoggerService.Instance.StopLogging(logType);
                    LogInfo($"{deviceName} 데이터 로깅 중단됨");
                }
            }
            catch { }
        }

        #endregion

        #region 종료 처리

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 챔버 작업 모드 중 종료 차단
            if (_chamberWorkMode)
            {
                MessageBox.Show("챔버 작업 중에는 프로그램을 종료할 수 없습니다.",
                    "잠금", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            // AutoRun 실행 중 잠금 체크
            if (_autoRunLock?.IsLocked == true)
            {
                MessageBox.Show("AutoRun 실행 중에는 프로그램을 종료할 수 없습니다.\n오토런을 먼저 중지하세요.",
                    "잠금", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (_autoRunService?.IsRunning == true)
            {
                if (MessageBox.Show("AutoRun이 실행 중입니다.\n종료하시겠습니까?", "종료 확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
                _autoRunService.Stop();
            }

            // 타이머 정리
            if (_ch1WaitingForVacuum || _ch1WaitingForTargetTemp || _ch1TimerActive)
                StopCh1Timer();

            LogInfo("시스템 종료 시작");

            using (var loadingForm = new LoadingForm())
            {
                loadingForm.Show();
                Application.DoEvents();

                loadingForm.UpdateStatus("서비스 종료 중...");
                _dataCollectionService?.Stop();
                _uiUpdateService?.Stop();

                loadingForm.UpdateStatus("연결 종료 중...");
                foreach (var device in _deviceList)
                {
                    try { if (device.IsConnected) device.Disconnect(); }
                    catch { }
                }

                loadingForm.UpdateStatus("로깅 종료 중...");
                DataLoggerService.Instance.StopAllLogging();
                AsyncLoggingService.Instance.Stop();

                loadingForm.UpdateStatus("리소스 정리 중...");
                _dataCollectionService?.Dispose();
                _uiUpdateService?.Dispose();
                _chillerPIDService?.Dispose();
                _experimentDataLogger?.Dispose();
                _channelManager?.Dispose();
            }

            _ch1AutoStopTimer?.Stop();
            _ch1AutoStopTimer?.Dispose();
            _autoRunTimer?.Stop();
            _autoRunTimer?.Dispose();
            _chartTimer?.Stop();
            _chartTimer?.Dispose();
            _formsPlotTemp?.Dispose();
            _formsPlotPressure?.Dispose();
            _autoRunService?.Dispose();

            LogInfo("시스템 종료 완료");
        }

        #endregion

        #region 기타 이벤트

        private void menuFileExit_Click(object sender, EventArgs e) => Close();

        private void MenuHelpAbout_Click(object sender, EventArgs e)
        {
            MessageBox.Show(
                $"{AppVersion.AppTitle}\nv{AppVersion.Version} ({AppVersion.BuildDate})\n\n© 2024-2026 VacX Inc.",
                "정보", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MenuHelpPatchNotes_Click(object sender, EventArgs e)
        {
            var form = new Form
            {
                Text = $"패치 노트 — {AppVersion.FullTitle}",
                Size = new Size(520, 600),
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                FormBorderStyle = FormBorderStyle.FixedDialog
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                Font = new Font("Malgun Gothic", 9.5f),
                BackColor = Color.White,
                Text = string.Join(Environment.NewLine, AppVersion.PatchNotes)
            };

            form.Controls.Add(textBox);
            form.ShowDialog(this);
        }

        // Designer 생성 이벤트 핸들러 (제거 시 Designer.cs 수정 필요)
        private void tableLayoutPanel3_Paint(object sender, PaintEventArgs e) { }
        private void tabControlMain_SelectedIndexChanged(object sender, EventArgs e) { }

        #endregion

        #region UI 헬퍼

        private void SetButtonEnabled(Button button, bool enabled)
        {
            if (button == null) return;
            if (InvokeRequired)
                BeginInvoke(new Action(() => button.Enabled = enabled));
            else
                button.Enabled = enabled;
        }

        private void ShowMessageBox(string message, string title,
            MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            if (InvokeRequired)
                BeginInvoke(new Action(() => MessageBox.Show(this, message, title, buttons, icon)));
            else
                MessageBox.Show(this, message, title, buttons, icon);
        }

        private void RunOnUIThread(Action action)
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }

        private void btnBakeoutSettings_Click(object sender, EventArgs e)
        {
            if (_profileManager == null)
                _profileManager = new ThermalRampProfileManager();

            using (var form = new BakeoutSettingsForm(_profileManager))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _bakeoutSettings = form.Settings;
                    LogInfo($"[베이크 아웃] 설정 저장: {_bakeoutSettings.TargetTemperature}°C, " +
                            $"{_bakeoutSettings.HoldTimeMinutes}분");
                }
            }
        }

        private void btnHoldSettings_Click(object sender, EventArgs e)
        {
            var currentSettings = simpleRampControl1?.RampController?.HoldSettings
                                  ?? HoldModeSettings.Load();

            using (var form = new HoldModeSettingsForm(currentSettings))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    if (simpleRampControl1?.RampController != null)
                        simpleRampControl1.RampController.HoldSettings = form.Settings;

                    LogInfo($"[Hold] 설정 변경: {form.Settings.GetSourceText()}, " +
                            $"간격={form.Settings.CheckIntervalMinutes}분, " +
                            $"SV 범위={form.Settings.MinHeaterTemp}~{form.Settings.MaxHeaterTemp}°C");
                }
            }
        }

        #endregion

        #region UI 성능 최적화

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED — 더블 버퍼링
                return cp;
            }
        }

        #endregion
    }
}