using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VacX_OutSense.Core.AutoRun;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// AutoRun 설정 폼
    /// </summary>
    public class AutoRunConfigForm : Form
    {
        private AutoRunConfiguration _config;
        private TabControl tabControl;
        private Button btnOk;
        private Button btnCancel;
        private Button btnReset;
        private Button btnSave;
        private Button btnLoad;

        public AutoRunConfigForm(AutoRunConfiguration config)
        {
            _config = config ?? new AutoRunConfiguration();
            InitializeComponent();
            LoadConfiguration();
        }

        private void InitializeComponent()
        {
            this.Text = "AutoRun 설정";
            this.Size = new Size(600, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tabControl = new TabControl();
            tabControl.Location = new Point(10, 10);
            tabControl.Size = new Size(565, 550);

            // 압력 설정 탭
            var tabPressure = new TabPage("압력 설정");
            CreatePressureControls(tabPressure);
            tabControl.TabPages.Add(tabPressure);

            // 온도 설정 탭
            var tabTemperature = new TabPage("온도 설정");
            CreateTemperatureControls(tabTemperature);
            tabControl.TabPages.Add(tabTemperature);

            // 시간 설정 탭
            var tabTime = new TabPage("시간 설정");
            CreateTimeControls(tabTime);
            tabControl.TabPages.Add(tabTime);

            // 타임아웃 설정 탭
            var tabTimeout = new TabPage("타임아웃 설정");
            CreateTimeoutControls(tabTimeout);
            tabControl.TabPages.Add(tabTimeout);

            // 기타 설정 탭
            var tabMisc = new TabPage("기타 설정");
            CreateMiscControls(tabMisc);
            tabControl.TabPages.Add(tabMisc);

            // 버튼들
            btnOk = new Button();
            btnOk.Text = "확인";
            btnOk.Location = new Point(315, 570);
            btnOk.Size = new Size(80, 30);
            btnOk.DialogResult = DialogResult.OK;
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button();
            btnCancel.Text = "취소";
            btnCancel.Location = new Point(405, 570);
            btnCancel.Size = new Size(80, 30);
            btnCancel.DialogResult = DialogResult.Cancel;

            btnReset = new Button();
            btnReset.Text = "기본값";
            btnReset.Location = new Point(495, 570);
            btnReset.Size = new Size(80, 30);
            btnReset.Click += BtnReset_Click;

            btnSave = new Button();
            btnSave.Text = "저장...";
            btnSave.Location = new Point(10, 570);
            btnSave.Size = new Size(80, 30);
            btnSave.Click += BtnSave_Click;

            btnLoad = new Button();
            btnLoad.Text = "불러오기...";
            btnLoad.Location = new Point(100, 570);
            btnLoad.Size = new Size(80, 30);
            btnLoad.Click += BtnLoad_Click;

            this.Controls.AddRange(new Control[] { tabControl, btnOk, btnCancel, btnReset, btnSave, btnLoad });
        }

        private void CreatePressureControls(TabPage tab)
        {
            int y = 20;

            var lblTitle = new Label();
            lblTitle.Text = "각 단계별 압력 조건을 설정합니다.";
            lblTitle.Location = new Point(20, y);
            lblTitle.Size = new Size(500, 20);
            tab.Controls.Add(lblTitle);
            y += 40;

            AddNumericControl(tab, "터보펌프 시작 압력 (Torr):", ref y,
                "TurboPumpPressure", 0.01, 10, 2, _config.TargetPressureForTurboPump,
                "드라이펌프로 이 압력까지 도달하면 터보펌프를 시작합니다.");

            AddNumericControl(tab, "이온게이지 활성화 압력 (Torr):", ref y,
                "IonGaugePressure", 1E-5, 1E-2, 6, _config.TargetPressureForIonGauge,
                "이 압력 이하에서만 이온게이지를 켤 수 있습니다.");

            AddNumericControl(tab, "히터 시작 압력 (Torr):", ref y,
                "HeaterPressure", 1E-7, 1E-4, 6, _config.TargetPressureForHeater,
                "고진공 상태에서 히터를 시작하기 위한 목표 압력입니다.");

            AddNumericControl(tab, "실험 중 최대 압력 (Torr):", ref y,
                "MaxExperimentPressure", 1E-6, 1E-3, 6, _config.MaxPressureDuringExperiment,
                "실험 중 이 압력을 초과하면 경고가 발생합니다.");
        }

        private void CreateTemperatureControls(TabPage tab)
        {
            int y = 20;

            var lblTitle = new Label();
            lblTitle.Text = "온도 관련 설정을 구성합니다.";
            lblTitle.Location = new Point(20, y);
            lblTitle.Size = new Size(500, 20);
            tab.Controls.Add(lblTitle);
            y += 40;

            AddNumericControl(tab, "칠러 설정 온도 (°C):", ref y,
                "ChillerTemp", -20, 50, 1, _config.ChillerSetTemperature,
                "터보펌프 냉각을 위한 칠러 온도입니다.");

            AddNumericControl(tab, "히터 CH1 설정 온도 (°C):", ref y,
                "HeaterCh1Temp", 0, 300, 1, _config.HeaterCh1SetTemperature,
                "채널 1 히터의 목표 온도입니다.");

            AddNumericControl(tab, "히터 CH2 설정 온도 (°C):", ref y,
                "HeaterCh2Temp", 0, 300, 1, _config.HeaterCh2SetTemperature,
                "채널 2 히터의 목표 온도입니다.");

            AddNumericControl(tab, "히터 램프 속도 (°C/min):", ref y,
                "HeaterRampRate", 0.5, 20, 1, _config.HeaterRampUpRate,
                "온도 상승 속도입니다.");

            AddNumericControl(tab, "온도 안정성 허용 범위 (±°C):", ref y,
                "TempTolerance", 0.1, 5, 1, _config.TemperatureStabilityTolerance,
                "설정 온도에서 허용되는 편차 범위입니다.");
        }

        private void CreateTimeControls(TabPage tab)
        {
            int y = 20;

            var lblTitle = new Label();
            lblTitle.Text = "실험 시간 관련 설정입니다.";
            lblTitle.Location = new Point(20, y);
            lblTitle.Size = new Size(500, 20);
            tab.Controls.Add(lblTitle);
            y += 40;

            AddNumericControl(tab, "실험 시간 (시간):", ref y,
                "ExperimentHours", 1, 168, 0, _config.ExperimentDurationHours,
                "설정 온도 도달 후 유지할 시간입니다. (최대 7일)");

            AddNumericControl(tab, "데이터 로깅 간격 (초):", ref y,
                "LoggingInterval", 1, 300, 0, _config.DataLoggingIntervalSeconds,
                "측정 데이터를 기록하는 간격입니다.");
        }

        private void CreateTimeoutControls(TabPage tab)
        {
            int y = 20;

            var lblTitle = new Label();
            lblTitle.Text = "각 단계별 최대 대기 시간을 설정합니다.";
            lblTitle.Location = new Point(20, y);
            lblTitle.Size = new Size(500, 20);
            tab.Controls.Add(lblTitle);
            y += 40;

            AddNumericControl(tab, "초기화 타임아웃 (초):", ref y,
                "InitTimeout", 30, 300, 0, _config.InitializationTimeout);

            AddNumericControl(tab, "밸브 작동 타임아웃 (초):", ref y,
                "ValveTimeout", 10, 60, 0, _config.ValveOperationTimeout);

            AddNumericControl(tab, "드라이펌프 시작 타임아웃 (초):", ref y,
                "DryPumpTimeout", 30, 180, 0, _config.DryPumpStartTimeout);

            AddNumericControl(tab, "터보펌프 시작 타임아웃 (분):", ref y,
                "TurboPumpTimeout", 5, 30, 0, _config.TurboPumpStartTimeout / 60);

            AddNumericControl(tab, "고진공 도달 타임아웃 (분):", ref y,
                "HighVacuumTimeout", 10, 120, 0, _config.HighVacuumTimeout / 60);

            AddNumericControl(tab, "종료 시퀀스 타임아웃 (분):", ref y,
                "ShutdownTimeout", 5, 30, 0, _config.ShutdownTimeout / 60);
        }

        private void CreateMiscControls(TabPage tab)
        {
            int y = 20;

            var lblTitle = new Label();
            lblTitle.Text = "기타 AutoRun 옵션을 설정합니다.";
            lblTitle.Location = new Point(20, y);
            lblTitle.Size = new Size(500, 20);
            tab.Controls.Add(lblTitle);
            y += 40;

            // 실행 모드
            var lblMode = new Label();
            lblMode.Text = "실행 모드:";
            lblMode.Location = new Point(20, y);
            lblMode.Size = new Size(150, 20);
            tab.Controls.Add(lblMode);

            var cmbMode = new ComboBox();
            cmbMode.Name = "RunMode";
            cmbMode.Location = new Point(180, y);
            cmbMode.Size = new Size(200, 25);
            cmbMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbMode.Items.AddRange(new[] { "전체 자동", "단계별 확인", "시뮬레이션" });
            cmbMode.SelectedIndex = (int)_config.RunMode;
            tab.Controls.Add(cmbMode);
            y += 40;

            // 재시도 설정
            AddNumericControl(tab, "오류 시 재시도 횟수:", ref y,
                "RetryCount", 0, 10, 0, _config.MaxRetryCount);

            AddNumericControl(tab, "재시도 대기 시간 (초):", ref y,
                "RetryDelay", 5, 60, 0, _config.RetryDelaySeconds);

            y += 20;

            // 체크박스 옵션들
            var chkDetailedLog = new CheckBox();
            chkDetailedLog.Name = "DetailedLogging";
            chkDetailedLog.Text = "상세 로깅 활성화";
            chkDetailedLog.Location = new Point(20, y);
            chkDetailedLog.Size = new Size(300, 25);
            chkDetailedLog.Checked = _config.EnableDetailedLogging;
            tab.Controls.Add(chkDetailedLog);
            y += 30;

            var chkSafeShutdown = new CheckBox();
            chkSafeShutdown.Name = "SafeShutdown";
            chkSafeShutdown.Text = "실패 시 안전 종료 활성화";
            chkSafeShutdown.Location = new Point(20, y);
            chkSafeShutdown.Size = new Size(300, 25);
            chkSafeShutdown.Checked = _config.EnableSafeShutdownOnFailure;
            tab.Controls.Add(chkSafeShutdown);
        }

        private void AddNumericControl(TabPage tab, string label, ref int y,
            string name, double min, double max, int decimalPlaces, double value,
            string tooltip = null)
        {
            var lbl = new Label();
            lbl.Text = label;
            lbl.Location = new Point(20, y);
            lbl.Size = new Size(250, 20);

            var num = new NumericUpDown();
            num.Name = name;
            num.Location = new Point(280, y);
            num.Size = new Size(120, 25);
            num.Minimum = (decimal)min;
            num.Maximum = (decimal)max;
            num.DecimalPlaces = decimalPlaces;
            num.Value = (decimal)value;

            tab.Controls.Add(lbl);
            tab.Controls.Add(num);

            if (!string.IsNullOrEmpty(tooltip))
            {
                var lblHelp = new Label();
                lblHelp.Text = tooltip;
                lblHelp.Location = new Point(420, y);
                lblHelp.Size = new Size(120, 40);
                lblHelp.ForeColor = Color.Gray;
                lblHelp.Font = new Font("맑은 고딕", 8);
                tab.Controls.Add(lblHelp);
            }

            y += 35;
        }

        private void LoadConfiguration()
        {
            // 압력 설정
            SetNumericValue("TurboPumpPressure", _config.TargetPressureForTurboPump);
            SetNumericValue("IonGaugePressure", _config.TargetPressureForIonGauge);
            SetNumericValue("HeaterPressure", _config.TargetPressureForHeater);
            SetNumericValue("MaxExperimentPressure", _config.MaxPressureDuringExperiment);

            // 온도 설정
            SetNumericValue("ChillerTemp", _config.ChillerSetTemperature);
            SetNumericValue("HeaterCh1Temp", _config.HeaterCh1SetTemperature);
            SetNumericValue("HeaterCh2Temp", _config.HeaterCh2SetTemperature);
            SetNumericValue("HeaterRampRate", _config.HeaterRampUpRate);
            SetNumericValue("TempTolerance", _config.TemperatureStabilityTolerance);

            // 시간 설정
            SetNumericValue("ExperimentHours", _config.ExperimentDurationHours);
            SetNumericValue("LoggingInterval", _config.DataLoggingIntervalSeconds);

            // 타임아웃 설정
            SetNumericValue("InitTimeout", _config.InitializationTimeout);
            SetNumericValue("ValveTimeout", _config.ValveOperationTimeout);
            SetNumericValue("DryPumpTimeout", _config.DryPumpStartTimeout);
            SetNumericValue("TurboPumpTimeout", _config.TurboPumpStartTimeout / 60);
            SetNumericValue("HighVacuumTimeout", _config.HighVacuumTimeout / 60);
            SetNumericValue("ShutdownTimeout", _config.ShutdownTimeout / 60);

            // 기타 설정
            SetComboValue("RunMode", (int)_config.RunMode);
            SetNumericValue("RetryCount", _config.MaxRetryCount);
            SetNumericValue("RetryDelay", _config.RetryDelaySeconds);
            SetCheckValue("DetailedLogging", _config.EnableDetailedLogging);
            SetCheckValue("SafeShutdown", _config.EnableSafeShutdownOnFailure);
        }

        private void SetNumericValue(string name, double value)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as NumericUpDown;
            if (control != null)
            {
                control.Value = (decimal)value;
            }
        }

        private double GetNumericValue(string name)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as NumericUpDown;
            return control != null ? (double)control.Value : 0;
        }

        private void SetComboValue(string name, int index)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as ComboBox;
            if (control != null && index >= 0 && index < control.Items.Count)
            {
                control.SelectedIndex = index;
            }
        }

        private int GetComboValue(string name)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as ComboBox;
            return control?.SelectedIndex ?? 0;
        }

        private void SetCheckValue(string name, bool value)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as CheckBox;
            if (control != null)
            {
                control.Checked = value;
            }
        }

        private bool GetCheckValue(string name)
        {
            var control = this.Controls.Find(name, true).FirstOrDefault() as CheckBox;
            return control?.Checked ?? false;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // 설정 저장
            _config.TargetPressureForTurboPump = GetNumericValue("TurboPumpPressure");
            _config.TargetPressureForIonGauge = GetNumericValue("IonGaugePressure");
            _config.TargetPressureForHeater = GetNumericValue("HeaterPressure");
            _config.MaxPressureDuringExperiment = GetNumericValue("MaxExperimentPressure");

            _config.ChillerSetTemperature = GetNumericValue("ChillerTemp");
            _config.HeaterCh1SetTemperature = GetNumericValue("HeaterCh1Temp");
            _config.HeaterCh2SetTemperature = GetNumericValue("HeaterCh2Temp");
            _config.HeaterRampUpRate = GetNumericValue("HeaterRampRate");
            _config.TemperatureStabilityTolerance = GetNumericValue("TempTolerance");

            _config.ExperimentDurationHours = (int)GetNumericValue("ExperimentHours");
            _config.DataLoggingIntervalSeconds = (int)GetNumericValue("LoggingInterval");

            _config.InitializationTimeout = (int)GetNumericValue("InitTimeout");
            _config.ValveOperationTimeout = (int)GetNumericValue("ValveTimeout");
            _config.DryPumpStartTimeout = (int)GetNumericValue("DryPumpTimeout");
            _config.TurboPumpStartTimeout = (int)(GetNumericValue("TurboPumpTimeout") * 60);
            _config.HighVacuumTimeout = (int)(GetNumericValue("HighVacuumTimeout") * 60);
            _config.ShutdownTimeout = (int)(GetNumericValue("ShutdownTimeout") * 60);

            _config.RunMode = (AutoRunMode)GetComboValue("RunMode");
            _config.MaxRetryCount = (int)GetNumericValue("RetryCount");
            _config.RetryDelaySeconds = (int)GetNumericValue("RetryDelay");
            _config.EnableDetailedLogging = GetCheckValue("DetailedLogging");
            _config.EnableSafeShutdownOnFailure = GetCheckValue("SafeShutdown");
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 설정을 기본값으로 초기화하시겠습니까?",
                "초기화 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _config.ResetToDefaults();
                LoadConfiguration();
                MessageBox.Show("기본값으로 초기화되었습니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            using (var saveDialog = new SaveFileDialog())
            {
                saveDialog.Filter = "XML 파일 (*.xml)|*.xml|모든 파일 (*.*)|*.*";
                saveDialog.DefaultExt = "xml";
                saveDialog.FileName = "AutoRunConfig.xml";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        BtnOk_Click(sender, e); // 현재 설정 저장
                        _config.SaveToFile(saveDialog.FileName);
                        MessageBox.Show("설정이 저장되었습니다.", "저장 완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            using (var openDialog = new OpenFileDialog())
            {
                openDialog.Filter = "XML 파일 (*.xml)|*.xml|모든 파일 (*.*)|*.*";
                openDialog.DefaultExt = "xml";

                if (openDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _config = AutoRunConfiguration.LoadFromFile(openDialog.FileName);
                        LoadConfiguration();
                        MessageBox.Show("설정을 불러왔습니다.", "불러오기 완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"불러오기 실패: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}