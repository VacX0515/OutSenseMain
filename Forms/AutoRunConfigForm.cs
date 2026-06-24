using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Core.AutoRun;
using VacX_OutSense.Core.Control;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// AutoRun 설정 폼
    /// </summary>
    public partial class AutoRunConfigForm : Form
    {
        private AutoRunConfiguration _config;
        private ToolTip _toolTip;
        private Label _lblHighVacuumTimeoutHours;
        private Label _lblShutdownTimeoutHours;
        private NumericUpDown nudVentTempWaitTimeout;
        private NumericUpDown nudAtmPressureWaitTimeout;
        private NumericUpDown nudCoolingWaitTimeout;
        private NumericUpDown nudTurboPumpDecelTimeout;

        // 데이터 기록 컬럼 선택 체크박스
        private CheckBox _chkLogPressure, _chkLogValves, _chkLogDryPump, _chkLogTurboPump, _chkLogChiller;
        private CheckBox[] _chkLogTempCh = new CheckBox[12];
        private CheckBox _chkLogAdditionalAI;

        // AutoCap 컨트롤 (런타임 생성)
        private TabPage _tabAutoCap;
        private CheckBox _chkUseAutoCap;
        private NumericUpDown _nudAutoCapMaxStep;
        private NumericUpDown _nudAutoCapPanicStep;
        private NumericUpDown _nudAutoCapStableRate;
        private NumericUpDown _nudAutoCapStableDur;
        private NumericUpDown _nudAutoCapTEnv;

        // 런타임 생성 탭 (UI 재정리)
        private TabPage _tabBakeout;
        private TabPage _tabShutdown;

        private static string FormatSecondsToHM(int seconds)
        {
            int h = seconds / 3600;
            int m = (seconds % 3600) / 60;
            return h > 0 ? $"= {h}시간 {m}분" : $"= {m}분";
        }

        public AutoRunConfiguration Configuration => _config;

        public AutoRunConfigForm(AutoRunConfiguration config)
        {
            _config = config ?? new AutoRunConfiguration();
            InitializeComponent();
            SetupDataLogTab();
            SetupToolTips();
            SetupHelpLabels();
            SetupHelpButton();
            SetupChannelInterlock();
            SetupAutoCapTab();
            ReorganizeTabsForReadability();   // ★ UI 정리 (베이크아웃/종료 탭 분리)
            LoadConfiguration();
        }

        /// <summary>
        /// 가독성 개선: 온도 탭의 베이크아웃 컨트롤을 별도 탭으로 분리하고 GroupBox 섹션화.
        /// 종료 시퀀스도 별도 탭으로.
        /// </summary>
        private void ReorganizeTabsForReadability()
        {
            _tabBakeout = new TabPage("베이크아웃") { Padding = new Padding(3) };
            _tabShutdown = new TabPage("종료 시퀀스") { Padding = new Padding(3) };

            // AutoCap 탭은 이미 추가됨 — 위치 조정 위해 임시 제거 후 순서 재배치
            int autoCapIdx = _tabAutoCap != null ? tabControl1.TabPages.IndexOf(_tabAutoCap) : -1;
            if (autoCapIdx >= 0) tabControl1.TabPages.Remove(_tabAutoCap);

            // 온도 탭 다음에 [베이크아웃] [AutoCap] [종료시퀀스] 순으로 삽입
            int tempIdx = tabControl1.TabPages.IndexOf(tabTemperature);
            tabControl1.TabPages.Insert(tempIdx + 1, _tabBakeout);
            if (_tabAutoCap != null) tabControl1.TabPages.Insert(tempIdx + 2, _tabAutoCap);
            tabControl1.TabPages.Insert(
                tabControl1.TabPages.IndexOf(_tabAutoCap ?? _tabBakeout) + 1, _tabShutdown);

            // 베이크아웃 탭 — 4개 GroupBox 섹션
            var grpBasic = new GroupBox
            {
                Text = "기본 설정",
                Location = new Point(8, 8),
                Size = new Size(420, 110),
                Font = new Font(Font, FontStyle.Bold),
            };
            var grpMonitor = new GroupBox
            {
                Text = "샘플 온도 모니터링 채널 (선택된 채널 중 MAX로 제어)",
                Location = new Point(8, 124),
                Size = new Size(420, 90),
                Font = new Font(Font, FontStyle.Bold),
            };
            var grpSafety = new GroupBox
            {
                Text = "안전 설정",
                Location = new Point(8, 220),
                Size = new Size(420, 130),
                Font = new Font(Font, FontStyle.Bold),
            };
            var grpAdvanced = new GroupBox
            {
                Text = "고급 (PI 제어 — v2 기본 컨트롤러용)",
                Location = new Point(8, 356),
                Size = new Size(420, 100),
                Font = new Font(Font, FontStyle.Bold),
            };
            _tabBakeout.Controls.Add(grpBasic);
            _tabBakeout.Controls.Add(grpMonitor);
            _tabBakeout.Controls.Add(grpSafety);
            _tabBakeout.Controls.Add(grpAdvanced);

            // 기본 설정
            MoveControl(lblBakeoutTargetTemp, grpBasic, 10, 25);
            MoveControl(txtBakeoutTargetTemp, grpBasic, 220, 22);
            AddUnitLabel(grpBasic, "°C", 350, 25);
            MoveControl(lblBakeoutRampRate, grpBasic, 10, 55);
            MoveControl(txtBakeoutRampRate, grpBasic, 220, 52);
            AddUnitLabel(grpBasic, "°C/h", 350, 55);
            var lblHoldHint = new Label
            {
                Text = "▸ 홀드 시간·종료 동작은 [시간 설정] 탭에서 설정",
                Location = new Point(10, 82),
                Size = new Size(400, 18),
                ForeColor = Color.DarkSlateGray,
                Font = new Font(Font.FontFamily, 8),
            };
            grpBasic.Controls.Add(lblHoldHint);

            // 모니터 채널 (4열 × 3행)
            for (int i = 0; i < 12; i++)
            {
                int col = i % 4;
                int row = i / 4;
                CheckBox? chk = i switch
                {
                    0 => chkBakeoutMonitorCh1, 1 => chkBakeoutMonitorCh2, 2 => chkBakeoutMonitorCh3,
                    3 => chkBakeoutMonitorCh4, 4 => chkBakeoutMonitorCh5, 5 => chkBakeoutMonitorCh6,
                    6 => chkBakeoutMonitorCh7, 7 => chkBakeoutMonitorCh8, 8 => chkBakeoutMonitorCh9,
                    9 => chkBakeoutMonitorCh10, 10 => chkBakeoutMonitorCh11, 11 => chkBakeoutMonitorCh12,
                    _ => null,
                };
                if (chk != null)
                {
                    chk.Text = $"CH{i + 1}";
                    MoveControl(chk, grpMonitor, 12 + col * 100, 22 + row * 22);
                    chk.Size = new Size(80, 20);
                }
            }
            lblBakeoutMonitorChannel.Visible = false;

            // 안전 설정
            MoveControl(lblBakeoutHeaterMax, grpSafety, 10, 25);
            MoveControl(txtBakeoutHeaterMax, grpSafety, 220, 22);
            AddUnitLabel(grpSafety, "°C", 350, 25);
            MoveControl(lblBakeoutMaxDeltaT, grpSafety, 10, 55);
            MoveControl(chkBakeoutMaxDeltaTAuto, grpSafety, 220, 55);
            chkBakeoutMaxDeltaTAuto.Text = "자동";
            chkBakeoutMaxDeltaTAuto.Size = new Size(50, 20);
            MoveControl(txtBakeoutMaxDeltaT, grpSafety, 275, 52);
            txtBakeoutMaxDeltaT.Size = new Size(70, 23);
            AddUnitLabel(grpSafety, "°C", 350, 55);
            MoveControl(lblBakeoutTolerance, grpSafety, 10, 85);
            MoveControl(txtBakeoutTolerance, grpSafety, 220, 82);
            AddUnitLabel(grpSafety, "±°C", 350, 85);

            // 고급
            MoveControl(lblBakeoutStabilization, grpAdvanced, 10, 25);
            MoveControl(txtBakeoutStabilization, grpAdvanced, 220, 22);
            AddUnitLabel(grpAdvanced, "초", 350, 25);
            MoveControl(lblBakeoutRiseTimeout, grpAdvanced, 10, 55);
            MoveControl(txtBakeoutRiseTimeout, grpAdvanced, 220, 52);
            AddUnitLabel(grpAdvanced, "분 (0=자동)", 350, 55);
            MoveControl(lblBakeoutFeedbackInterval, grpAdvanced, 10, 75);
            MoveControl(txtBakeoutFeedbackInterval, grpAdvanced, 220, 72);
            AddUnitLabel(grpAdvanced, "초", 350, 75);

            // 종료 시퀀스 탭
            var grpCoolDown = new GroupBox
            {
                Text = "쿨링 / 벤팅 온도",
                Location = new Point(8, 8),
                Size = new Size(420, 130),
                Font = new Font(Font, FontStyle.Bold),
            };
            MoveControl(lblCoolingTargetTemperature, grpCoolDown, 10, 25);
            MoveControl(txtCoolingTargetTemperature, grpCoolDown, 220, 22);
            AddUnitLabel(grpCoolDown, "°C", 350, 25);
            MoveControl(lblVentingStartTemperature, grpCoolDown, 10, 55);
            MoveControl(txtVentingStartTemperature, grpCoolDown, 220, 52);
            AddUnitLabel(grpCoolDown, "°C", 350, 55);
            MoveControl(lblVentTargetPressure, grpCoolDown, 10, 85);
            MoveControl(txtVentTargetPressure, grpCoolDown, 220, 82);
            AddUnitLabel(grpCoolDown, "kPa", 350, 85);
            _tabShutdown.Controls.Add(grpCoolDown);

            lblShutdownTempHeader.Visible = false;
            lblBakeoutDecelZone.Visible = false;
            txtBakeoutDecelZone.Visible = false;

            // 온도 탭 안내
            var lblTempTabHint = new Label
            {
                Text = "▸ 베이크아웃은 [베이크아웃] 탭에서 설정\n▸ 종료 시퀀스(쿨링/벤팅)는 [종료 시퀀스] 탭",
                Location = new Point(20, 200),
                Size = new Size(400, 40),
                ForeColor = Color.DarkSlateGray,
                Font = new Font(Font.FontFamily, 8),
            };
            tabTemperature.Controls.Add(lblTempTabHint);
        }

        private static void MoveControl(Control ctrl, Control newParent, int x, int y)
        {
            ctrl.Parent?.Controls.Remove(ctrl);
            newParent.Controls.Add(ctrl);
            ctrl.Location = new Point(x, y);
            ctrl.Visible = true;
        }

        private static void AddUnitLabel(Control parent, string text, int x, int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(70, 18),
                ForeColor = Color.DarkSlateGray,
            });
        }

        /// <summary>
        /// AutoCap Bakeout 설정 탭 (Option 5 — 계단식 cap 제어).
        /// </summary>
        private void SetupAutoCapTab()
        {
            var tabControl = (TabControl)Controls.Find("tabControl1", true)[0];
            _tabAutoCap = new TabPage("AutoCap (신규)") { Padding = new Padding(3) };
            tabControl.Controls.Add(_tabAutoCap);

            int y = 10;
            int xLbl = 10, xCtrl = 260, w = 140;

            var lblTitle = new Label
            {
                Text = "AutoCap Bakeout — 계단식 cap 제어 (오버슈트 수학적 보장)",
                Location = new Point(xLbl, y),
                Size = new Size(450, 20),
                Font = new Font(Font, FontStyle.Bold),
                ForeColor = Color.DarkGreen,
            };
            _tabAutoCap.Controls.Add(lblTitle);
            y += 28;

            _chkUseAutoCap = new CheckBox
            {
                Text = "AutoCap 컨트롤러 사용 (체크 해제 시 기존 v2 PI)",
                Location = new Point(xLbl, y),
                Size = new Size(450, 22),
                Font = new Font(Font, FontStyle.Bold),
            };
            _tabAutoCap.Controls.Add(_chkUseAutoCap);
            y += 30;

            // Max step up
            _tabAutoCap.Controls.Add(new Label
            {
                Text = "Cap 한번에 올릴 최대값 (°C):",
                Location = new Point(xLbl, y + 4),
                Size = new Size(240, 20),
            });
            _nudAutoCapMaxStep = new NumericUpDown
            {
                Location = new Point(xCtrl, y),
                Size = new Size(w, 23),
                Minimum = 1M, Maximum = 30M,
                DecimalPlaces = 1, Increment = 0.5M, Value = 5M,
            };
            _toolTip.SetToolTip(_nudAutoCapMaxStep,
                "안정 관측 후 cap을 한 번에 올리는 최대값. 작으면 안전하지만 느림.\n" +
                "기본 5°C, 범위 1-30°C. 느린 샘플에는 10-15°C 권장.");
            _tabAutoCap.Controls.Add(_nudAutoCapMaxStep);
            y += 30;

            // Panic step
            _tabAutoCap.Controls.Add(new Label
            {
                Text = "Panic 시 cap 즉시 감소량 (°C):",
                Location = new Point(xLbl, y + 4),
                Size = new Size(240, 20),
            });
            _nudAutoCapPanicStep = new NumericUpDown
            {
                Location = new Point(xCtrl, y),
                Size = new Size(w, 23),
                Minimum = 1M, Maximum = 30M,
                DecimalPlaces = 1, Increment = 1M, Value = 5M,
            };
            _toolTip.SetToolTip(_nudAutoCapPanicStep,
                "T_s가 target+tol×0.7 초과 감지 시 cap을 즉시 이만큼 감소. 기본 5°C.");
            _tabAutoCap.Controls.Add(_nudAutoCapPanicStep);
            y += 30;

            // Stable rate threshold
            _tabAutoCap.Controls.Add(new Label
            {
                Text = "안정 판정 rate 임계값 (°C/min):",
                Location = new Point(xLbl, y + 4),
                Size = new Size(240, 20),
            });
            _nudAutoCapStableRate = new NumericUpDown
            {
                Location = new Point(xCtrl, y),
                Size = new Size(w, 23),
                Minimum = 0.01M, Maximum = 1M,
                DecimalPlaces = 3, Increment = 0.01M, Value = 0.05M,
            };
            _toolTip.SetToolTip(_nudAutoCapStableRate,
                "|dT_s/dt| < 이 값이면 안정 판정. 기본 0.05°C/min.\n" +
                "작을수록 엄격 (안전하지만 느림), 클수록 관대 (빠르지만 노이즈 영향).");
            _tabAutoCap.Controls.Add(_nudAutoCapStableRate);
            y += 30;

            // Stable duration
            _tabAutoCap.Controls.Add(new Label
            {
                Text = "안정 판정 유지 시간 (초):",
                Location = new Point(xLbl, y + 4),
                Size = new Size(240, 20),
            });
            _nudAutoCapStableDur = new NumericUpDown
            {
                Location = new Point(xCtrl, y),
                Size = new Size(w, 23),
                Minimum = 30M, Maximum = 3600M,
                Increment = 30M, Value = 180M,
            };
            _toolTip.SetToolTip(_nudAutoCapStableDur,
                "안정 상태를 이 시간 동안 유지해야 다음 cap 단계 상승. 기본 180초(3분).\n" +
                "느린 샘플은 더 길게 (600-1200초).");
            _tabAutoCap.Controls.Add(_nudAutoCapStableDur);
            y += 30;

            // T_env
            _tabAutoCap.Controls.Add(new Label
            {
                Text = "환경(챔버 벽) 온도 (°C):",
                Location = new Point(xLbl, y + 4),
                Size = new Size(240, 20),
            });
            _nudAutoCapTEnv = new NumericUpDown
            {
                Location = new Point(xCtrl, y),
                Size = new Size(w, 23),
                Minimum = 0M, Maximum = 200M,
                DecimalPlaces = 1, Increment = 1M, Value = 25M,
            };
            _toolTip.SetToolTip(_nudAutoCapTEnv, "챔버 벽/주변 온도. 보통 실온 25°C.");
            _tabAutoCap.Controls.Add(_nudAutoCapTEnv);
            y += 40;

            // 안내
            var lblNote = new Label
            {
                Location = new Point(xLbl, y),
                Size = new Size(450, 140),
                Text = "▸ 작동 원리:\n" +
                       "  1. 초기 cap = target + tolerance (절대 안전 시작점)\n" +
                       "  2. 샘플이 cap에서 안정 관측되면 β 관측 후 cap 단계 상승\n" +
                       "  3. T_s가 target+tol×0.7 초과 시 cap 즉시 감소 (panic)\n" +
                       "  4. target 도달 후 holding\n\n" +
                       "▸ 오버슈트 수학적 보장: T_s ≤ T_h ≤ cap (열역학 2법칙)\n" +
                       "▸ β 학습/모델 추정 없음 → 구조적으로 단순, 실패 지점 적음\n" +
                       "▸ 단점: 매우 느린 샘플은 수렴 시간 길 수 있음 (물리 한계)",
                ForeColor = Color.DarkSlateGray,
                Font = new Font(Font.FontFamily, 8),
            };
            _tabAutoCap.Controls.Add(lblNote);
        }

        private void SetupChannelInterlock()
        {
            chkBakeoutMonitorCh1.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh2.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh3.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh4.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh5.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh6.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh7.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh8.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh9.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh10.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh11.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh12.CheckedChanged += (s, e) => UpdateChannelInterlock();
        }

        private void UpdateChannelInterlock()
        {
            if (chkBakeoutMonitorCh1.Checked)
            {
                // CH1 선택 → 다른 채널 비활성화 + 해제
                chkBakeoutMonitorCh2.Checked = false; chkBakeoutMonitorCh2.Enabled = false;
                chkBakeoutMonitorCh3.Checked = false; chkBakeoutMonitorCh3.Enabled = false;
                chkBakeoutMonitorCh4.Checked = false; chkBakeoutMonitorCh4.Enabled = false;
                chkBakeoutMonitorCh5.Checked = false; chkBakeoutMonitorCh5.Enabled = false; chkBakeoutMonitorCh5.Visible = false;
                chkBakeoutMonitorCh6.Checked = false; chkBakeoutMonitorCh6.Enabled = false; chkBakeoutMonitorCh6.Visible = false;
                chkBakeoutMonitorCh7.Checked = false; chkBakeoutMonitorCh7.Enabled = false; chkBakeoutMonitorCh7.Visible = false;
                chkBakeoutMonitorCh8.Checked = false; chkBakeoutMonitorCh8.Enabled = false; chkBakeoutMonitorCh8.Visible = false;
                chkBakeoutMonitorCh9.Checked = false; chkBakeoutMonitorCh9.Enabled = false; chkBakeoutMonitorCh9.Visible = false;
                chkBakeoutMonitorCh10.Checked = false; chkBakeoutMonitorCh10.Enabled = false; chkBakeoutMonitorCh10.Visible = false;
                chkBakeoutMonitorCh11.Checked = false; chkBakeoutMonitorCh11.Enabled = false; chkBakeoutMonitorCh11.Visible = false;
                chkBakeoutMonitorCh12.Checked = false; chkBakeoutMonitorCh12.Enabled = false; chkBakeoutMonitorCh12.Visible = false;
                lblBakeoutMonitorChannel.Text = "모니터 채널 (TM4 PID):";
            }
            else
            {
                // CH1 해제 → 다른 채널 활성화
                chkBakeoutMonitorCh2.Enabled = true;
                chkBakeoutMonitorCh3.Enabled = true;
                chkBakeoutMonitorCh4.Enabled = true;
                chkBakeoutMonitorCh5.Enabled = true; chkBakeoutMonitorCh5.Visible = true;
                chkBakeoutMonitorCh6.Enabled = true; chkBakeoutMonitorCh6.Visible = true;
                chkBakeoutMonitorCh7.Enabled = true; chkBakeoutMonitorCh7.Visible = true;
                chkBakeoutMonitorCh8.Enabled = true; chkBakeoutMonitorCh8.Visible = true;
                chkBakeoutMonitorCh9.Enabled = true; chkBakeoutMonitorCh9.Visible = true;
                chkBakeoutMonitorCh10.Enabled = true; chkBakeoutMonitorCh10.Visible = true;
                chkBakeoutMonitorCh11.Enabled = true; chkBakeoutMonitorCh11.Visible = true;
                chkBakeoutMonitorCh12.Enabled = true; chkBakeoutMonitorCh12.Visible = true;
                lblBakeoutMonitorChannel.Text = "모니터 채널 (MAX):";
            }
        }

        private void SetupDataLogTab()
        {
            var tabData = new TabPage
            {
                Text = "데이터 기록",
                UseVisualStyleBackColor = true,
                AutoScroll = true
            };
            tabControl1.Controls.Add(tabData);

            int y = 15;

            var lblTitle = new Label
            {
                Text = "CSV 파일에 기록할 데이터 그룹을 선택하세요:",
                Location = new Point(20, y),
                Size = new Size(400, 20),
                ForeColor = SystemColors.GrayText
            };
            tabData.Controls.Add(lblTitle);
            y += 30;

            // ── 장비 그룹 ──
            var lblEquip = new Label
            {
                Text = "■ 장비 데이터",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };
            tabData.Controls.Add(lblEquip);
            y += 22;

            _chkLogPressure = AddLogCheckBox(tabData, "압력 (ATM, Pirani, Ion)", ref y);
            _chkLogValves = AddLogCheckBox(tabData, "밸브 상태 (GV, VV, EV, IG HV)", ref y);
            _chkLogDryPump = AddLogCheckBox(tabData, "드라이펌프 (상태, 주파수, 전류, 온도)", ref y);
            _chkLogTurboPump = AddLogCheckBox(tabData, "터보펌프 (상태, 속도, 전류, 온도)", ref y);
            _chkLogChiller = AddLogCheckBox(tabData, "칠러 (상태, 현재온도, 목표온도)", ref y);
            _chkLogAdditionalAI = AddLogCheckBox(tabData, "추가 AI (전압)", ref y);

            y += 10;

            // ── 온도 채널 ──
            var lblTemp = new Label
            {
                Text = "■ 온도 채널 (PV, SV, MV, Status)",
                Location = new Point(15, y),
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold)
            };
            tabData.Controls.Add(lblTemp);
            y += 22;

            // 전체 선택/해제
            var btnAll = new Button { Text = "전체", Location = new Point(25, y), Size = new Size(55, 25) };
            var btnNone = new Button { Text = "해제", Location = new Point(85, y), Size = new Size(55, 25) };
            btnAll.Click += (s, e) => { foreach (var c in _chkLogTempCh) c.Checked = true; };
            btnNone.Click += (s, e) => { foreach (var c in _chkLogTempCh) c.Checked = false; };
            tabData.Controls.Add(btnAll);
            tabData.Controls.Add(btnNone);
            y += 32;

            // CH1~CH12 (3열 × 4행)
            for (int i = 0; i < 12; i++)
            {
                int col = i % 3;
                int row = i / 3;
                _chkLogTempCh[i] = new CheckBox
                {
                    Text = $"CH{i + 1}",
                    Location = new Point(30 + col * 130, y + row * 24),
                    Size = new Size(120, 20)
                };
                tabData.Controls.Add(_chkLogTempCh[i]);
            }
        }

        private CheckBox AddLogCheckBox(TabPage tab, string text, ref int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(30, y),
                Size = new Size(380, 20),
                Checked = true
            };
            tab.Controls.Add(chk);
            y += 24;
            return chk;
        }

        private void LoadConfiguration()
        {
            // 압력 설정
            txtTargetPressureForTurboPump.Text = _config.TargetPressureForTurboPump.ToString("E2");
            txtTargetPressureForIonGauge.Text = _config.TargetPressureForIonGauge.ToString("E2");
            txtTargetPressureForHeater.Text = _config.TargetPressureForHeater.ToString("E2");
            txtMaxPressureDuringExperiment.Text = _config.MaxPressureDuringExperiment.ToString("E2");

            // 온도 설정
            txtHeaterCh1SetTemperature.Text = _config.HeaterCh1SetTemperature.ToString("F1");
            txtHeaterRampUpRate.Text = _config.HeaterRampUpRate.ToString("F1");
            txtTemperatureStabilityTolerance.Text = _config.TemperatureStabilityTolerance.ToString("F1");
            txtCoolingTargetTemperature.Text = _config.CoolingTargetTemperature.ToString("F1");
            txtVentingStartTemperature.Text = _config.VentingStartTemperature.ToString("F1");
            txtVentTargetPressure.Text = _config.VentTargetPressure_kPa.ToString("F1");

            // 시간 설정 (총 분 → 시간 + 분)
            nudExperimentHours.Value = _config.ExperimentDurationMinutes / 60;
            nudExperimentMinutes.Value = _config.ExperimentDurationMinutes % 60;
            nudDataLoggingIntervalSeconds.Value = _config.DataLoggingIntervalSeconds;

            // 타임아웃 설정
            nudInitializationTimeout.Value = _config.InitializationTimeout;
            nudValveOperationTimeout.Value = _config.ValveOperationTimeout;
            nudDryPumpStartTimeout.Value = _config.DryPumpStartTimeout;
            nudTurboPumpStartTimeout.Value = _config.TurboPumpStartTimeout;
            nudIonGaugeActivationTimeout.Value = _config.IonGaugeActivationTimeout;
            nudHighVacuumTimeout.Value = _config.HighVacuumTimeout;
            nudHeaterStartTimeout.Value = _config.HeaterStartTimeout;
            nudShutdownTimeout.Value = Math.Max(nudShutdownTimeout.Minimum, Math.Min(nudShutdownTimeout.Maximum, _config.ShutdownTimeout));

            // 기타 설정
            cmbRunMode.Items.Clear();
            cmbRunMode.Items.AddRange(Enum.GetNames(typeof(AutoRunMode)));
            cmbRunMode.SelectedIndex = (int)_config.RunMode;
            nudMaxRetryCount.Value = _config.MaxRetryCount;
            nudRetryDelaySeconds.Value = _config.RetryDelaySeconds;
            chkEnableDetailedLogging.Checked = _config.EnableDetailedLogging;
            chkEnableSafeShutdownOnFailure.Checked = _config.EnableSafeShutdownOnFailure;
            chkEnableAlarmOnError.Checked = _config.EnableAlarmOnError;

            // 실험 유형 설정
            cmbExperimentType.SelectedIndex = (int)_config.ExperimentType;

            // 베이크아웃 설정
            txtBakeoutTargetTemp.Text = _config.BakeoutTargetTemperature.ToString("F1");
            txtBakeoutRampRate.Text = _config.BakeoutRampRate.ToString("F1");
            nudBakeoutHoldHours.Value = _config.BakeoutHoldTimeMinutes / 60;
            nudBakeoutHoldMinutes.Value = _config.BakeoutHoldTimeMinutes % 60;
            cmbBakeoutEndAction.SelectedIndex = (int)_config.BakeoutEndAction;
            chkBakeoutMonitorCh1.Checked = _config.BakeoutMonitorCh1;
            chkBakeoutMonitorCh2.Checked = _config.BakeoutMonitorCh2;
            chkBakeoutMonitorCh3.Checked = _config.BakeoutMonitorCh3;
            chkBakeoutMonitorCh4.Checked = _config.BakeoutMonitorCh4;
            chkBakeoutMonitorCh5.Checked = _config.BakeoutMonitorCh5;
            chkBakeoutMonitorCh6.Checked = _config.BakeoutMonitorCh6;
            chkBakeoutMonitorCh7.Checked = _config.BakeoutMonitorCh7;
            chkBakeoutMonitorCh8.Checked = _config.BakeoutMonitorCh8;
            chkBakeoutMonitorCh9.Checked = _config.BakeoutMonitorCh9;
            chkBakeoutMonitorCh10.Checked = _config.BakeoutMonitorCh10;
            chkBakeoutMonitorCh11.Checked = _config.BakeoutMonitorCh11;
            chkBakeoutMonitorCh12.Checked = _config.BakeoutMonitorCh12;
            txtBakeoutHeaterMax.Text = _config.BakeoutHeaterMaxTemperature.ToString("F1");
            chkBakeoutMaxDeltaTAuto.Checked = _config.BakeoutMaxDeltaT == 0;
            txtBakeoutMaxDeltaT.Text = _config.BakeoutMaxDeltaT > 0 ? _config.BakeoutMaxDeltaT.ToString("F0") : "50";
            txtBakeoutMaxDeltaT.Enabled = !chkBakeoutMaxDeltaTAuto.Checked;
            chkBakeoutMaxDeltaTAuto.CheckedChanged += (s, e) =>
            {
                txtBakeoutMaxDeltaT.Enabled = !chkBakeoutMaxDeltaTAuto.Checked;
            };
            txtBakeoutTolerance.Text = _config.BakeoutTolerance.ToString("F1");
            txtBakeoutStabilization.Text = _config.BakeoutStabilizationSeconds.ToString();
            txtBakeoutRiseTimeout.Text = _config.BakeoutRiseTimeoutMinutes.ToString();
            txtBakeoutFeedbackInterval.Text = _config.BakeoutFeedbackIntervalSec.ToString("F1");

            // AutoCap Bakeout 설정 로드
            _chkUseAutoCap.Checked = _config.UseAutoCapBakeout;
            _nudAutoCapMaxStep.Value = (decimal)Math.Clamp(_config.AutoCap_MaxStepUp, 1, 30);
            _nudAutoCapPanicStep.Value = (decimal)Math.Clamp(_config.AutoCap_PanicStep, 1, 30);
            _nudAutoCapStableRate.Value = (decimal)Math.Clamp(_config.AutoCap_StableRateThreshold, 0.01, 1);
            _nudAutoCapStableDur.Value = (decimal)Math.Clamp(_config.AutoCap_StableDurationSec, 30, 3600);
            _nudAutoCapTEnv.Value = (decimal)Math.Clamp(_config.AutoCap_EnvironmentTemperature, 0, 200);

            // 데이터 기록 컬럼 설정
            _chkLogPressure.Checked = _config.LogColumnPressure;
            _chkLogValves.Checked = _config.LogColumnValves;
            _chkLogDryPump.Checked = _config.LogColumnDryPump;
            _chkLogTurboPump.Checked = _config.LogColumnTurboPump;
            _chkLogChiller.Checked = _config.LogColumnChiller;
            _chkLogAdditionalAI.Checked = _config.LogColumnAdditionalAI;
            _chkLogTempCh[0].Checked = _config.LogColumnTempCh1;
            _chkLogTempCh[1].Checked = _config.LogColumnTempCh2;
            _chkLogTempCh[2].Checked = _config.LogColumnTempCh3;
            _chkLogTempCh[3].Checked = _config.LogColumnTempCh4;
            _chkLogTempCh[4].Checked = _config.LogColumnTempCh5;
            _chkLogTempCh[5].Checked = _config.LogColumnTempCh6;
            _chkLogTempCh[6].Checked = _config.LogColumnTempCh7;
            _chkLogTempCh[7].Checked = _config.LogColumnTempCh8;
            _chkLogTempCh[8].Checked = _config.LogColumnTempCh9;
            _chkLogTempCh[9].Checked = _config.LogColumnTempCh10;
            _chkLogTempCh[10].Checked = _config.LogColumnTempCh11;
            _chkLogTempCh[11].Checked = _config.LogColumnTempCh12;

            // 실험 유형에 따라 컨트롤 표시/숨김
            UpdateExperimentTypeUI();
            UpdateChannelInterlock();
        }

        private void SaveConfiguration()
        {
            try
            {
                // 압력 설정
                _config.TargetPressureForTurboPump = double.Parse(txtTargetPressureForTurboPump.Text);
                _config.TargetPressureForIonGauge = double.Parse(txtTargetPressureForIonGauge.Text);
                _config.TargetPressureForHeater = double.Parse(txtTargetPressureForHeater.Text);
                _config.MaxPressureDuringExperiment = double.Parse(txtMaxPressureDuringExperiment.Text);

                // 온도 설정
                _config.HeaterCh1SetTemperature = double.Parse(txtHeaterCh1SetTemperature.Text);
                _config.HeaterRampUpRate = double.Parse(txtHeaterRampUpRate.Text);
                _config.TemperatureStabilityTolerance = double.Parse(txtTemperatureStabilityTolerance.Text);
                _config.CoolingTargetTemperature = double.Parse(txtCoolingTargetTemperature.Text);
                _config.VentingStartTemperature = double.Parse(txtVentingStartTemperature.Text);
                _config.VentTargetPressure_kPa = double.Parse(txtVentTargetPressure.Text);

                // 시간 설정 (시간 + 분 → 총 분)
                _config.ExperimentDurationMinutes = (int)nudExperimentHours.Value * 60 + (int)nudExperimentMinutes.Value;
                _config.DataLoggingIntervalSeconds = (int)nudDataLoggingIntervalSeconds.Value;

                // 타임아웃 설정
                _config.InitializationTimeout = (int)nudInitializationTimeout.Value;
                _config.ValveOperationTimeout = (int)nudValveOperationTimeout.Value;
                _config.DryPumpStartTimeout = (int)nudDryPumpStartTimeout.Value;
                _config.TurboPumpStartTimeout = (int)nudTurboPumpStartTimeout.Value;
                _config.IonGaugeActivationTimeout = (int)nudIonGaugeActivationTimeout.Value;
                _config.HighVacuumTimeout = (int)nudHighVacuumTimeout.Value;
                _config.HeaterStartTimeout = (int)nudHeaterStartTimeout.Value;
                _config.ShutdownTimeout = (int)nudShutdownTimeout.Value;
                if (nudVentTempWaitTimeout != null) _config.VentingTempWaitTimeout = (int)nudVentTempWaitTimeout.Value;
                if (nudAtmPressureWaitTimeout != null) _config.AtmPressureWaitTimeout = (int)nudAtmPressureWaitTimeout.Value;
                if (nudCoolingWaitTimeout != null) _config.CoolingWaitTimeout = (int)nudCoolingWaitTimeout.Value;
                if (nudTurboPumpDecelTimeout != null) _config.TurboPumpDecelerationTimeout = (int)nudTurboPumpDecelTimeout.Value;

                // 기타 설정
                _config.RunMode = (AutoRunMode)cmbRunMode.SelectedIndex;
                _config.MaxRetryCount = (int)nudMaxRetryCount.Value;
                _config.RetryDelaySeconds = (int)nudRetryDelaySeconds.Value;
                _config.EnableDetailedLogging = chkEnableDetailedLogging.Checked;
                _config.EnableSafeShutdownOnFailure = chkEnableSafeShutdownOnFailure.Checked;
                _config.EnableAlarmOnError = chkEnableAlarmOnError.Checked;

                // 실험 유형 설정
                _config.ExperimentType = (ExperimentType)cmbExperimentType.SelectedIndex;

                // 베이크아웃 설정
                _config.BakeoutTargetTemperature = double.Parse(txtBakeoutTargetTemp.Text);
                _config.BakeoutRampRate = double.Parse(txtBakeoutRampRate.Text);
                _config.BakeoutHoldTimeMinutes = (int)nudBakeoutHoldHours.Value * 60 + (int)nudBakeoutHoldMinutes.Value;
                _config.BakeoutEndAction = (BakeoutEndAction)cmbBakeoutEndAction.SelectedIndex;
                _config.BakeoutMonitorCh1 = chkBakeoutMonitorCh1.Checked;
                _config.BakeoutMonitorCh2 = chkBakeoutMonitorCh2.Checked;
                _config.BakeoutMonitorCh3 = chkBakeoutMonitorCh3.Checked;
                _config.BakeoutMonitorCh4 = chkBakeoutMonitorCh4.Checked;
                _config.BakeoutMonitorCh5 = chkBakeoutMonitorCh5.Checked;
                _config.BakeoutMonitorCh6 = chkBakeoutMonitorCh6.Checked;
                _config.BakeoutMonitorCh7 = chkBakeoutMonitorCh7.Checked;
                _config.BakeoutMonitorCh8 = chkBakeoutMonitorCh8.Checked;
                _config.BakeoutMonitorCh9 = chkBakeoutMonitorCh9.Checked;
                _config.BakeoutMonitorCh10 = chkBakeoutMonitorCh10.Checked;
                _config.BakeoutMonitorCh11 = chkBakeoutMonitorCh11.Checked;
                _config.BakeoutMonitorCh12 = chkBakeoutMonitorCh12.Checked;
                var selected = _config.GetBakeoutMonitorChannels();
                _config.BakeoutMonitorChannel = selected.Count > 0 ? selected[0] : 2;
                _config.BakeoutHeaterMaxTemperature = double.Parse(txtBakeoutHeaterMax.Text);
                _config.BakeoutMaxDeltaT = chkBakeoutMaxDeltaTAuto.Checked ? 0 : double.Parse(txtBakeoutMaxDeltaT.Text);
                _config.BakeoutTolerance = double.Parse(txtBakeoutTolerance.Text);
                _config.BakeoutStabilizationSeconds = int.Parse(txtBakeoutStabilization.Text);
                _config.BakeoutRiseTimeoutMinutes = int.Parse(txtBakeoutRiseTimeout.Text);
                _config.BakeoutFeedbackIntervalSec = double.Parse(txtBakeoutFeedbackInterval.Text);

                // AutoCap Bakeout 설정 저장
                _config.UseAutoCapBakeout = _chkUseAutoCap.Checked;
                _config.AutoCap_MaxStepUp = (double)_nudAutoCapMaxStep.Value;
                _config.AutoCap_PanicStep = (double)_nudAutoCapPanicStep.Value;
                _config.AutoCap_StableRateThreshold = (double)_nudAutoCapStableRate.Value;
                _config.AutoCap_StableDurationSec = (int)_nudAutoCapStableDur.Value;
                _config.AutoCap_EnvironmentTemperature = (double)_nudAutoCapTEnv.Value;

                // 데이터 기록 컬럼 설정
                _config.LogColumnPressure = _chkLogPressure.Checked;
                _config.LogColumnValves = _chkLogValves.Checked;
                _config.LogColumnDryPump = _chkLogDryPump.Checked;
                _config.LogColumnTurboPump = _chkLogTurboPump.Checked;
                _config.LogColumnChiller = _chkLogChiller.Checked;
                _config.LogColumnAdditionalAI = _chkLogAdditionalAI.Checked;
                _config.LogColumnTempCh1 = _chkLogTempCh[0].Checked;
                _config.LogColumnTempCh2 = _chkLogTempCh[1].Checked;
                _config.LogColumnTempCh3 = _chkLogTempCh[2].Checked;
                _config.LogColumnTempCh4 = _chkLogTempCh[3].Checked;
                _config.LogColumnTempCh5 = _chkLogTempCh[4].Checked;
                _config.LogColumnTempCh6 = _chkLogTempCh[5].Checked;
                _config.LogColumnTempCh7 = _chkLogTempCh[6].Checked;
                _config.LogColumnTempCh8 = _chkLogTempCh[7].Checked;
                _config.LogColumnTempCh9 = _chkLogTempCh[8].Checked;
                _config.LogColumnTempCh10 = _chkLogTempCh[9].Checked;
                _config.LogColumnTempCh11 = _chkLogTempCh[10].Checked;
                _config.LogColumnTempCh12 = _chkLogTempCh[11].Checked;
            }
            catch (Exception ex)
            {
                throw new Exception($"설정 저장 중 오류: {ex.Message}");
            }
        }

        private void CmbExperimentType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateExperimentTypeUI();
        }

        private void UpdateExperimentTypeUI()
        {
            bool isBakeout = cmbExperimentType.SelectedIndex == 1;

            // 온도 탭 — 아웃게싱 전용 (베이크아웃은 별도 탭)
            txtHeaterCh1SetTemperature.Visible = !isBakeout;
            lblHeaterCh1SetTemperature.Visible = !isBakeout;
            txtHeaterRampUpRate.Visible = !isBakeout;
            lblHeaterRampUpRate.Visible = !isBakeout;
            txtTemperatureStabilityTolerance.Visible = !isBakeout;
            lblTemperatureStabilityTolerance.Visible = !isBakeout;
            lblTempNote.Visible = !isBakeout;

            // 베이크아웃/AutoCap 탭 — 베이크아웃 모드일 때만
            ToggleTab(_tabBakeout, isBakeout);
            ToggleTab(_tabAutoCap, isBakeout);
            // 종료 시퀀스 탭은 항상 표시 (아웃게싱도 사용)

            // 시간 탭 — 실험 유형별 전환
            nudExperimentHours.Visible = !isBakeout;
            nudExperimentMinutes.Visible = !isBakeout;
            lblExperimentDuration.Visible = !isBakeout;
            lblExpHoursUnit.Visible = !isBakeout;
            lblExpMinutesUnit.Visible = !isBakeout;

            lblBakeoutHoldTime.Visible = isBakeout;
            nudBakeoutHoldHours.Visible = isBakeout;
            lblBakeoutHoldHUnit.Visible = isBakeout;
            nudBakeoutHoldMinutes.Visible = isBakeout;
            lblBakeoutHoldMUnit.Visible = isBakeout;
            lblBakeoutEndAction.Visible = isBakeout;
            cmbBakeoutEndAction.Visible = isBakeout;

            // 모드 시각화 (콤보박스 배경색)
            cmbExperimentType.BackColor = isBakeout
                ? Color.FromArgb(255, 230, 230)   // 연빨강 — 베이크아웃
                : Color.FromArgb(220, 255, 220);  // 연녹색 — 아웃게싱
        }

        /// <summary>탭 표시/숨김 (TabPages add/remove).</summary>
        private void ToggleTab(TabPage? tab, bool show)
        {
            if (tab == null) return;
            bool present = tabControl1.TabPages.Contains(tab);
            if (show && !present)
            {
                // 베이크아웃/AutoCap: 온도 다음에 삽입
                int insertAt = tabControl1.TabPages.IndexOf(tabTemperature) + 1;
                if (tab == _tabAutoCap && _tabBakeout != null && tabControl1.TabPages.Contains(_tabBakeout))
                    insertAt = tabControl1.TabPages.IndexOf(_tabBakeout) + 1;
                tabControl1.TabPages.Insert(Math.Min(insertAt, tabControl1.TabPages.Count), tab);
            }
            else if (!show && present)
            {
                tabControl1.TabPages.Remove(tab);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            try
            {
                SaveConfiguration();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnLoadDefault_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                "기본값 로드",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _config = new AutoRunConfiguration();
                LoadConfiguration();
            }
        }

        #region 도움말 시스템

        /// <summary>
        /// 모든 컨트롤에 ToolTip 설정
        /// </summary>
        private void SetupToolTips()
        {
            _toolTip = new ToolTip
            {
                AutoPopDelay = 10000,
                InitialDelay = 300,
                ReshowDelay = 200,
                ShowAlways = true
            };

            // ── 실험 유형 ──
            _toolTip.SetToolTip(cmbExperimentType,
                "탈가스율 측정: 일정 온도에서 아웃개싱 측정\n" +
                "베이크 아웃: 목표 온도까지 서서히 가열 후 유지");

            // ── 압력 탭 ──
            _toolTip.SetToolTip(txtTargetPressureForTurboPump,
                "드라이펌프로 이 압력까지 도달하면 터보펌프를 시작합니다.\n" +
                "일반적으로 1 Torr 이하에서 시작합니다.");
            _toolTip.SetToolTip(txtTargetPressureForIonGauge,
                "터보펌프 가동 후 이 압력에 도달하면 이온게이지를 켭니다.\n" +
                "피라니 게이지 측정 범위 하한 근처로 설정합니다.");
            _toolTip.SetToolTip(txtTargetPressureForHeater,
                "이온게이지 측정 기준, 이 고진공에 도달해야 히터를 시작합니다.\n" +
                "충분한 진공에서 가열해야 산화를 방지할 수 있습니다.");
            _toolTip.SetToolTip(txtMaxPressureDuringExperiment,
                "실험/베이크아웃 중 이 압력을 초과하면 경고를 표시합니다.\n" +
                "급격한 누출이나 탈가스를 감시합니다.");

            // ── 온도 탭: 탈가스율 ──
            _toolTip.SetToolTip(txtHeaterCh1SetTemperature,
                "히터 CH1의 목표 온도입니다.\n" +
                "TM4 온도 컨트롤러의 SV(설정값)으로 전송됩니다.");
            _toolTip.SetToolTip(txtHeaterRampUpRate,
                "히터 승온 속도 (°C/분).\n" +
                "TM4 하드웨어 램프를 통해 서서히 가열합니다.");

            // ── 온도 탭: 베이크아웃 ──
            _toolTip.SetToolTip(txtBakeoutTargetTemp,
                "베이크아웃 목표 온도입니다.\n" +
                "모니터 채널의 온도가 이 값에 도달하면 홀드 타이머가 시작됩니다.");
            _toolTip.SetToolTip(txtBakeoutRampRate,
                "승온 속도 (°C/시간).\n" +
                "너무 빠르면 챔버 내 온도 편차가 커질 수 있습니다.\n" +
                "일반적으로 1~5°C/h를 권장합니다.");
            _toolTip.SetToolTip(chkBakeoutMonitorCh1,
                "CH1 = 히터 자체 센서.\n" +
                "CH1 선택 시 TM4 내장 PID + 기본 램프업으로 제어합니다.\n" +
                "CH1과 다른 채널은 동시에 선택할 수 없습니다.");
            _toolTip.SetToolTip(chkBakeoutMonitorCh2,
                "CH2 = 샘플 센서 2번.\n" +
                "비-CH1 채널 선택 시 소프트웨어 PI 피드백으로 제어합니다.");
            _toolTip.SetToolTip(chkBakeoutMonitorCh3, "CH3 = 샘플 센서 3번 (확장 채널)");
            _toolTip.SetToolTip(chkBakeoutMonitorCh4, "CH4 = 샘플 센서 4번 (확장 채널)");
            _toolTip.SetToolTip(chkBakeoutMonitorCh5, "CH5 = 샘플 센서 5번 (확장 채널)");
            _toolTip.SetToolTip(txtBakeoutHeaterMax,
                "PI 피드백 제어 시 CH1이 절대 초과하지 않는 온도입니다.\n" +
                "히터 과열 방지용 안전 상한입니다.");
            _toolTip.SetToolTip(chkBakeoutMaxDeltaTAuto,
                "자동: 관측된 열지연 × 2.5 (최소 15°C)로 자동 계산합니다.\n" +
                "해제 시 수동으로 값을 지정합니다.");
            _toolTip.SetToolTip(txtBakeoutMaxDeltaT,
                "승온/홀드 중 CH1 SV가 샘플 온도 + 이 값을 초과하지 않습니다.\n" +
                "불균일 가열 방지용입니다.");
            _toolTip.SetToolTip(txtBakeoutStabilization,
                "목표±허용오차 범위 내 + 변화율 안정 상태에서\n" +
                "이 시간(초) 동안 연속 유지되어야 홀드 타이머가 시작됩니다.\n" +
                "범위 이탈 또는 변화율 과대 시 카운터가 0으로 리셋됩니다.\n" +
                "홀드(실험) 타이머 자체는 일시정지되지 않습니다.\n" +
                "0이면 즉시 시작 (한 번 도달 시 바로 홀드).");
            _toolTip.SetToolTip(txtBakeoutRiseTimeout,
                "목표 온도 도달까지 허용되는 최대 시간 (분).\n" +
                "0이면 자동 계산 (램프 속도 기반 × 3 + 30분, 최소 60분).");
            _toolTip.SetToolTip(txtBakeoutFeedbackInterval,
                "PI 피드백으로 CH1 SV를 변경하는 주기 (초).\n" +
                "짧으면 반응이 빠르지만 진동 위험, 길면 안정적이지만 느림.\n" +
                "권장: 3~10초");

            // ── 온도 탭: 공통 ──
            _toolTip.SetToolTip(txtTemperatureStabilityTolerance,
                "온도 안정 판정 허용 범위 (±°C).\n" +
                "모니터 온도가 목표±이 범위 안이면 안정 상태로 간주합니다.");
            _toolTip.SetToolTip(txtCoolingTargetTemperature,
                "종료 시 이 온도 이하로 냉각되면 밸브를 닫고 마무리합니다.");
            _toolTip.SetToolTip(txtVentingStartTemperature,
                "CH1이 이 온도 이하로 내려가야 벤트 밸브를 엽니다.\n" +
                "너무 뜨거운 상태에서 벤트하면 챔버 과열 위험이 있습니다.");
            _toolTip.SetToolTip(txtVentTargetPressure,
                "벤트 밸브를 열어 이 압력(kPa) 이상이 되면 대기압 도달로 판단합니다.");

            // ── 시간 탭 ──
            _toolTip.SetToolTip(nudExperimentHours, "탈가스율 측정 실험의 총 지속 시간 (시간 부분)");
            _toolTip.SetToolTip(nudExperimentMinutes, "탈가스율 측정 실험의 총 지속 시간 (분 부분)");
            _toolTip.SetToolTip(nudBakeoutHoldHours,
                "목표 온도 도달 후 유지할 시간 (시간 부분).\n" +
                "승온 시간은 포함되지 않습니다.");
            _toolTip.SetToolTip(nudBakeoutHoldMinutes, "유지 시간의 분 부분");
            _toolTip.SetToolTip(cmbBakeoutEndAction,
                "냉각→벤트→종료: 히터 OFF 후 냉각 대기 → 벤트 → 펌프 정지 (전체 셧다운)\n" +
                "온도 유지: 홀드 완료 후 현재 온도를 유지하며 수동 종료 대기\n" +
                "알림만: 소리만 울리고 현재 상태를 유지");
            _toolTip.SetToolTip(nudDataLoggingIntervalSeconds,
                "데이터 파일에 온도/압력을 기록하는 간격입니다.\n" +
                "너무 짧으면 파일이 커지고, 너무 길면 데이터 해상도가 낮아집니다.");

            // ── 타임아웃 탭 ──
            _toolTip.SetToolTip(nudInitializationTimeout, "초기 장비 연결 및 상태 확인 제한 시간");
            _toolTip.SetToolTip(nudValveOperationTimeout, "게이트밸브/벤트밸브 동작 확인 제한 시간");
            _toolTip.SetToolTip(nudDryPumpStartTimeout, "드라이펌프 시작 후 정상 가동 확인 제한 시간");
            _toolTip.SetToolTip(nudTurboPumpStartTimeout,
                "터보펌프 시작 후 정상 속도 도달 제한 시간.\n" +
                "터보펌프는 가속에 수 분이 소요됩니다.");
            _toolTip.SetToolTip(nudIonGaugeActivationTimeout, "이온게이지 HV ON 후 안정화 제한 시간");
            _toolTip.SetToolTip(nudHighVacuumTimeout,
                "이온게이지 ON 후 히터 시작 압력까지 도달 제한 시간.\n" +
                "챔버 상태에 따라 수십 분 이상 걸릴 수 있습니다.");
            _toolTip.SetToolTip(nudHeaterStartTimeout, "히터 시작 명령 후 동작 확인 제한 시간");
            _toolTip.SetToolTip(nudShutdownTimeout,
                "종료 시퀀스 전체 제한 시간.\n" +
                "냉각 대기 포함이므로 충분히 설정하세요.\n" +
                "아래 세부 타임아웃의 합보다 크게 설정하세요.");

            // ── 기타 탭 ──
            _toolTip.SetToolTip(cmbRunMode,
                "FullAuto: 전체 자동 실행\n" +
                "StepByStep: 단계별 확인 후 진행\n" +
                "Simulation: 장비 없이 시퀀스 테스트");
            _toolTip.SetToolTip(nudMaxRetryCount, "각 단계 실패 시 자동 재시도 횟수");
            _toolTip.SetToolTip(nudRetryDelaySeconds, "재시도 전 대기 시간 (초)");
            _toolTip.SetToolTip(chkEnableDetailedLogging, "활성화 시 모든 단계의 상세 로그를 기록합니다.");
            _toolTip.SetToolTip(chkEnableSafeShutdownOnFailure,
                "실패 시 히터 OFF → 밸브 닫기 등 안전 종료를 자동 수행합니다.\n" +
                "비활성화하면 실패 시 현재 상태로 정지합니다.");
            _toolTip.SetToolTip(chkEnableAlarmOnError, "오류 발생 시 시스템 알림음을 울립니다.");
        }

        /// <summary>
        /// 각 탭에 안내 문구 추가
        /// </summary>
        private void SetupHelpLabels()
        {
            // 압력 탭: 기존 lblPressureInfo 텍스트를 확장 (Y=170, 이미 존재)
            lblPressureInfo.Text =
                "※ 압력값은 과학적 표기법(예: 1.00E-05)으로 입력하세요.\n\n" +
                "AutoRun 순서: 드라이펌프 → 터보펌프 → 이온게이지 → 고진공 → 히터\n" +
                "각 단계 전환 기준 압력을 위에서 설정합니다.";
            lblPressureInfo.Size = new Size(400, 65);
            lblPressureInfo.ForeColor = Color.FromArgb(80, 80, 80);

            // 타임아웃 탭: 하단에 안내 (기존 컨트롤 아래 Y=315)
            var lblTimeoutGuide = new Label
            {
                Text = "※ 각 단계에서 조건을 만족하지 못하면 이 시간까지 대기합니다.\n" +
                       "   타임아웃 초과 시 해당 단계가 실패로 처리됩니다.\n" +
                       "   넉넉하게 설정하는 것이 안전합니다.",
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font(Font.FontFamily, 8f),
                Location = new Point(20, 320),
                Size = new Size(400, 50),
                Name = "lblTimeoutGuide"
            };
            tabTimeout.Controls.Add(lblTimeoutGuide);

            // ── 종료 시퀀스 세부 타임아웃 ──
            int ySD = 380;
            var lblSDHeader = new Label
            {
                Text = "── 종료 시퀀스 세부 타임아웃",
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Location = new Point(20, ySD), AutoSize = true
            };
            tabTimeout.Controls.Add(lblSDHeader);
            ySD += 25;

            tabTimeout.Controls.Add(new Label { Text = "벤팅 온도 대기 (초):", Location = new Point(20, ySD + 2), AutoSize = true });
            nudVentTempWaitTimeout = new NumericUpDown { Location = new Point(260, ySD), Size = new Size(80, 23), Minimum = 60, Maximum = 86400, Increment = 600, Value = _config.VentingTempWaitTimeout };
            tabTimeout.Controls.Add(nudVentTempWaitTimeout);
            var lblVTH = new Label { Location = new Point(345, ySD + 2), Size = new Size(80, 20), ForeColor = Color.DimGray, Font = new Font(Font.FontFamily, 8f) };
            lblVTH.Text = FormatSecondsToHM((int)nudVentTempWaitTimeout.Value);
            nudVentTempWaitTimeout.ValueChanged += (s2, e2) => lblVTH.Text = FormatSecondsToHM((int)nudVentTempWaitTimeout.Value);
            tabTimeout.Controls.Add(lblVTH);
            ySD += 35;

            tabTimeout.Controls.Add(new Label { Text = "터보 감속 대기 (초):", Location = new Point(20, ySD + 2), AutoSize = true });
            nudTurboPumpDecelTimeout = new NumericUpDown { Location = new Point(260, ySD), Size = new Size(80, 23), Minimum = 60, Maximum = 3600, Increment = 60, Value = _config.TurboPumpDecelerationTimeout };
            tabTimeout.Controls.Add(nudTurboPumpDecelTimeout);
            ySD += 35;

            tabTimeout.Controls.Add(new Label { Text = "ATM 압력 대기 (초):", Location = new Point(20, ySD + 2), AutoSize = true });
            nudAtmPressureWaitTimeout = new NumericUpDown { Location = new Point(260, ySD), Size = new Size(80, 23), Minimum = 60, Maximum = 3600, Increment = 60, Value = _config.AtmPressureWaitTimeout };
            tabTimeout.Controls.Add(nudAtmPressureWaitTimeout);
            ySD += 35;

            tabTimeout.Controls.Add(new Label { Text = "쿨링 대기 (초):", Location = new Point(20, ySD + 2), AutoSize = true });
            nudCoolingWaitTimeout = new NumericUpDown { Location = new Point(260, ySD), Size = new Size(80, 23), Minimum = 60, Maximum = 86400, Increment = 600, Value = _config.CoolingWaitTimeout };
            tabTimeout.Controls.Add(nudCoolingWaitTimeout);
            var lblCWH = new Label { Location = new Point(345, ySD + 2), Size = new Size(80, 20), ForeColor = Color.DimGray, Font = new Font(Font.FontFamily, 8f) };
            lblCWH.Text = FormatSecondsToHM((int)nudCoolingWaitTimeout.Value);
            nudCoolingWaitTimeout.ValueChanged += (s2, e2) => lblCWH.Text = FormatSecondsToHM((int)nudCoolingWaitTimeout.Value);
            tabTimeout.Controls.Add(lblCWH);

            // 세부 타임아웃 툴팁
            _toolTip.SetToolTip(nudVentTempWaitTimeout,
                "히터 OFF 후 벤팅 시작 온도(125°C)까지 대기하는 최대 시간.\n" +
                "이 동안 펌프는 계속 가동되어 진공을 유지합니다.\n" +
                "타임아웃 시 현재 온도로 벤트를 진행합니다.");
            _toolTip.SetToolTip(nudTurboPumpDecelTimeout,
                "터보펌프 정지 명령 후 완전히 멈출 때까지 대기하는 최대 시간.");
            _toolTip.SetToolTip(nudAtmPressureWaitTimeout,
                "벤트 밸브를 연 후 대기압(ATM)에 도달할 때까지 대기하는 최대 시간.\n" +
                "타임아웃 시 배기 밸브를 강제로 엽니다.");
            _toolTip.SetToolTip(nudCoolingWaitTimeout,
                "벤트/배기 밸브를 연 후 쿨링 목표 온도까지 대기하는 최대 시간.\n" +
                "대기 순환으로 챔버를 냉각합니다.\n" +
                "타임아웃 시 밸브를 닫고 종료합니다.");

            // 고진공/종료 타임아웃 시간 변환 표시
            _lblHighVacuumTimeoutHours = new Label
            {
                Location = new Point(345, 212), Size = new Size(80, 20),
                ForeColor = Color.DimGray, Font = new Font(Font.FontFamily, 8f),
                Text = FormatSecondsToHM((int)nudHighVacuumTimeout.Value)
            };
            tabTimeout.Controls.Add(_lblHighVacuumTimeoutHours);
            nudHighVacuumTimeout.ValueChanged += (s, ev) =>
                _lblHighVacuumTimeoutHours.Text = FormatSecondsToHM((int)nudHighVacuumTimeout.Value);

            _lblShutdownTimeoutHours = new Label
            {
                Location = new Point(345, 282), Size = new Size(80, 20),
                ForeColor = Color.DimGray, Font = new Font(Font.FontFamily, 8f),
                Text = FormatSecondsToHM((int)nudShutdownTimeout.Value)
            };
            tabTimeout.Controls.Add(_lblShutdownTimeoutHours);
            nudShutdownTimeout.ValueChanged += (s, ev) =>
                _lblShutdownTimeoutHours.Text = FormatSecondsToHM((int)nudShutdownTimeout.Value);

            // 시간 탭: 기존 컨트롤 아래 안내 (Y=140)
            var lblTimeGuide = new Label
            {
                Text = "※ 베이크아웃 유지 시간은 승온 완료 후부터 카운트됩니다.\n" +
                       "   승온에 걸리는 시간은 별도로 자동 계산됩니다.",
                ForeColor = Color.FromArgb(80, 80, 80),
                Font = new Font(Font.FontFamily, 8f),
                Location = new Point(20, 140),
                Size = new Size(400, 30),
                Name = "lblTimeGuide"
            };
            tabTime.Controls.Add(lblTimeGuide);
        }

        /// <summary>
        /// [?] 도움말 버튼 추가
        /// </summary>
        private void SetupHelpButton()
        {
            var btnHelp = new Button
            {
                Text = "?",
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Size = new Size(30, 30),
                Location = new Point(100, 10),
                FlatStyle = FlatStyle.System
            };
            btnHelp.Click += BtnHelp_Click;
            _toolTip.SetToolTip(btnHelp, "AutoRun 도움말 보기");
            panelButtons.Controls.Add(btnHelp);
        }

        private void BtnHelp_Click(object sender, EventArgs e)
        {
            var helpText =
@"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   AutoRun 도움말
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

■ AutoRun이란?
  진공 챔버의 전체 실험 과정을 자동으로 실행하는 기능입니다.
  배기 → 가열 → 실험/베이크아웃 → 냉각 → 벤트까지
  모든 단계를 순서대로 자동 제어합니다.

■ 실험 유형
  ┌ 탈가스율 측정 (OutgassingRate)
  │  일정 온도에서 아웃개싱 양을 측정합니다.
  │  CH1 히터로 직접 온도를 제어합니다.
  │
  └ 베이크 아웃 (Bakeout)
     목표 온도까지 서서히 가열하여 표면 탈가스를 촉진합니다.
     샘플 센서(CH2~5)를 기준으로 PI 피드백 제어합니다.

■ 실행 순서 (9단계)
  1. 초기화      — 장비 연결 확인
  2. 배기 준비    — 게이트밸브 닫기, 벤트밸브 닫기
  3. 드라이펌프   — 러프 배기 (대기압 → ~1 Torr)
  4. 터보펌프     — 고진공 배기 시작
  5. 이온게이지   — 고진공 압력 측정 시작
  6. 고진공 대기   — 히터 시작 압력까지 대기
  7. 히터 시작    — CH1 가열 시작
  8. 실험 진행    — 승온 → 안정화 대기 → 홀드 (데이터 기록)
  9. 종료 시퀀스   — 냉각 → 벤트 → 밸브 → 펌프 정지

■ 베이크아웃 모니터 채널
  · CH1: 히터 내장 열전대 (TM4 자체 PID 제어)
  · CH2~5: 외부 샘플 센서 (소프트웨어 PI 피드백)

  복수 선택 시 선택된 채널 중 최대 온도(MAX)를 기준으로
  제어합니다. 샘플의 어느 부분도 목표 온도를 초과하지
  않도록 가장 뜨거운 지점을 추적합니다.

  예) CH2+CH3 선택 → MAX(CH2, CH3)로 PI 피드백

■ PI 피드백 제어 (베이크아웃, CH2~5 선택 시)
  CH1 히터의 설정 온도(SV)를 자동 조절하여
  샘플 센서 온도가 목표에 도달하도록 제어합니다.
  · 승온 속도: BakeoutRampRate (°C/h) 이내로 제한
  · CH1 상한: BakeoutHeaterMaxTemperature 초과 불가
  · 센서 이상 시: 해당 채널 제외 후 나머지로 계속

■ 안정화 유지 시간 (베이크아웃)
  목표 온도 ± 허용오차 범위 내에서 설정 시간 동안
  연속으로 유지되어야 홀드 타이머가 시작됩니다.

  판정 조건 (모두 충족 시 카운트):
  · 샘플 온도가 목표 ± 허용오차 범위 내
  · 온도 변화율이 충분히 작음 (0.3°C/사이클 미만)

  도중에 범위를 벗어나거나 변화율이 크면 카운터가
  0으로 리셋되어 처음부터 다시 카운트합니다.
  0초로 설정하면 한 번 도달 시 즉시 홀드를 시작합니다.
  (기본값: 600초 = 10분)

  ※ 안정화 시간은 승온 단계에만 적용됩니다.
     홀드(실험) 타이머는 일시정지되지 않습니다.

■ 종료 동작 (베이크아웃)
  · 냉각→벤트→종료: 히터 OFF → 벤팅 시작 온도 대기 → 벤트 → 완전 종료
  · 온도 유지: 홀드 완료 후 현재 온도를 유지하며 수동 종료 대기
  · 알림만: 소리만 울리고 현재 상태 유지

■ 안전 기능
  · 벤팅 시작 온도: CH1이 설정 온도 이하로 냉각되어야 벤트
  · 압력 감시: 실험 중 압력 상승 시 경고
  · 센서 이상 감지: 에러 플래그, 이상값, 급변 자동 감지
  · 안전 종료: 실패 시 히터 OFF → 밸브 닫기 자동 실행

■ 팁
  · 처음 사용 시 StepByStep 모드로 각 단계를 확인하세요.
  · 타임아웃은 넉넉하게 설정하는 것이 안전합니다.
  · 설정 변경 후 [기본값] 버튼으로 초기화할 수 있습니다.
  · 각 설정 항목에 마우스를 올리면 상세 설명이 나타납니다.";

            using (var helpForm = new Form
            {
                Text = "AutoRun 도움말",
                Size = new Size(520, 620),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            })
            {
                var txtHelp = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Dock = DockStyle.Fill,
                    Font = new Font("맑은 고딕", 9f),
                    BackColor = Color.White,
                    Text = helpText
                };

                var btnClose = new Button
                {
                    Text = "닫기",
                    DialogResult = DialogResult.OK,
                    Size = new Size(80, 30),
                    Dock = DockStyle.Bottom
                };

                helpForm.Controls.Add(txtHelp);
                helpForm.Controls.Add(btnClose);
                helpForm.AcceptButton = btnClose;
                helpForm.ShowDialog(this);
            }
        }

        #endregion
    }
}
