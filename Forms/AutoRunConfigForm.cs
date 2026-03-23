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
            SetupToolTips();
            SetupHelpLabels();
            SetupHelpButton();
            SetupChannelInterlock();
            LoadConfiguration();
        }

        private void SetupChannelInterlock()
        {
            chkBakeoutMonitorCh1.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh2.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh3.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh4.CheckedChanged += (s, e) => UpdateChannelInterlock();
            chkBakeoutMonitorCh5.CheckedChanged += (s, e) => UpdateChannelInterlock();
        }

        private void UpdateChannelInterlock()
        {
            if (chkBakeoutMonitorCh1.Checked)
            {
                // CH1 선택 → 다른 채널 비활성화 + 해제
                chkBakeoutMonitorCh2.Checked = false; chkBakeoutMonitorCh2.Enabled = false;
                chkBakeoutMonitorCh3.Checked = false; chkBakeoutMonitorCh3.Enabled = false;
                chkBakeoutMonitorCh4.Checked = false; chkBakeoutMonitorCh4.Enabled = false;
                chkBakeoutMonitorCh5.Checked = false; chkBakeoutMonitorCh5.Enabled = false;
                lblBakeoutMonitorChannel.Text = "모니터 채널 (TM4 PID):";
            }
            else
            {
                // CH1 해제 → 다른 채널 활성화
                chkBakeoutMonitorCh2.Enabled = true;
                chkBakeoutMonitorCh3.Enabled = true;
                chkBakeoutMonitorCh4.Enabled = true;
                chkBakeoutMonitorCh5.Enabled = true;
                lblBakeoutMonitorChannel.Text = "모니터 채널 (MAX):";
            }
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
            txtBakeoutHeaterMax.Text = _config.BakeoutHeaterMaxTemperature.ToString("F1");
            txtBakeoutMaxDeltaT.Text = _config.BakeoutMaxDeltaT.ToString("F0");
            txtBakeoutTolerance.Text = _config.BakeoutTolerance.ToString("F1");
            txtBakeoutStabilization.Text = _config.BakeoutStabilizationSeconds.ToString();
            txtBakeoutRiseTimeout.Text = _config.BakeoutRiseTimeoutMinutes.ToString();
            txtBakeoutFeedbackInterval.Text = _config.BakeoutFeedbackIntervalSec.ToString("F1");

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
                var selected = _config.GetBakeoutMonitorChannels();
                _config.BakeoutMonitorChannel = selected.Count > 0 ? selected[0] : 2;
                _config.BakeoutHeaterMaxTemperature = double.Parse(txtBakeoutHeaterMax.Text);
                _config.BakeoutMaxDeltaT = double.Parse(txtBakeoutMaxDeltaT.Text);
                _config.BakeoutTolerance = double.Parse(txtBakeoutTolerance.Text);
                _config.BakeoutStabilizationSeconds = int.Parse(txtBakeoutStabilization.Text);
                _config.BakeoutRiseTimeoutMinutes = int.Parse(txtBakeoutRiseTimeout.Text);
                _config.BakeoutFeedbackIntervalSec = double.Parse(txtBakeoutFeedbackInterval.Text);
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

            // 온도 탭: 탈가스율 vs 베이크아웃 컨트롤 전환
            txtHeaterCh1SetTemperature.Visible = !isBakeout;
            lblHeaterCh1SetTemperature.Visible = !isBakeout;
            txtHeaterRampUpRate.Visible = !isBakeout;
            lblHeaterRampUpRate.Visible = !isBakeout;
            txtTemperatureStabilityTolerance.Visible = !isBakeout;
            lblTemperatureStabilityTolerance.Visible = !isBakeout;

            txtBakeoutTargetTemp.Visible = isBakeout;
            lblBakeoutTargetTemp.Visible = isBakeout;
            txtBakeoutRampRate.Visible = isBakeout;
            lblBakeoutRampRate.Visible = isBakeout;
            lblBakeoutMonitorChannel.Visible = isBakeout;
            chkBakeoutMonitorCh1.Visible = isBakeout;
            chkBakeoutMonitorCh2.Visible = isBakeout;
            chkBakeoutMonitorCh3.Visible = isBakeout;
            chkBakeoutMonitorCh4.Visible = isBakeout;
            chkBakeoutMonitorCh5.Visible = isBakeout;
            lblBakeoutHeaterMax.Visible = isBakeout;
            txtBakeoutHeaterMax.Visible = isBakeout;
            lblBakeoutMaxDeltaT.Visible = isBakeout;
            txtBakeoutMaxDeltaT.Visible = isBakeout;
            lblBakeoutTolerance.Visible = isBakeout;
            txtBakeoutTolerance.Visible = isBakeout;
            lblBakeoutStabilization.Visible = isBakeout;
            txtBakeoutStabilization.Visible = isBakeout;
            lblBakeoutRiseTimeout.Visible = isBakeout;
            txtBakeoutRiseTimeout.Visible = isBakeout;
            lblBakeoutDecelZone.Visible = false;
            txtBakeoutDecelZone.Visible = false;
            lblBakeoutFeedbackInterval.Visible = isBakeout;
            txtBakeoutFeedbackInterval.Visible = isBakeout;

            // 시간 탭: 탈가스율 vs 베이크아웃 컨트롤 전환
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

            // 온도 탭: 베이크아웃 모드에서 항목이 4개(+허용오차)로 늘어나므로
            // TemperatureStabilityTolerance 이하 컨트롤을 아래로 이동
            int yTolerance = isBakeout ? 165 : 131;
            int yShutdownHeader = isBakeout ? 295 : 175;
            int yCooling = isBakeout ? 320 : 200;
            int yVentTemp = isBakeout ? 357 : 237;
            int yVentPressure = isBakeout ? 394 : 274;
            int yNote = isBakeout ? 430 : 317;

            txtTemperatureStabilityTolerance.Location = new Point(230, yTolerance);
            lblTemperatureStabilityTolerance.Location = new Point(20, yTolerance + 3);
            lblShutdownTempHeader.Location = new Point(20, yShutdownHeader);
            txtCoolingTargetTemperature.Location = new Point(230, yCooling);
            lblCoolingTargetTemperature.Location = new Point(20, yCooling + 3);
            txtVentingStartTemperature.Location = new Point(230, yVentTemp);
            lblVentingStartTemperature.Location = new Point(20, yVentTemp + 3);
            txtVentTargetPressure.Location = new Point(230, yVentPressure);
            lblVentTargetPressure.Location = new Point(20, yVentPressure + 3);
            lblTempNote.Location = new Point(20, yNote);

            // 온도 탭: 종료 시퀀스 설정은 HeaterOff일 때만 의미 있음
            bool showShutdownSettings = !isBakeout ||
                (cmbBakeoutEndAction.SelectedIndex == 0); // 냉각 → 벤트 → 종료
            lblShutdownTempHeader.Visible = showShutdownSettings;
            txtCoolingTargetTemperature.Visible = showShutdownSettings;
            lblCoolingTargetTemperature.Visible = showShutdownSettings;
            txtVentingStartTemperature.Visible = showShutdownSettings;
            lblVentingStartTemperature.Visible = showShutdownSettings;
            txtVentTargetPressure.Visible = showShutdownSettings;
            lblVentTargetPressure.Visible = showShutdownSettings;
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
            _toolTip.SetToolTip(txtBakeoutMaxDeltaT,
                "승온/홀드 중 CH1 SV가 샘플 온도 + 이 값을 초과하지 않습니다.\n" +
                "불균일 가열 방지용입니다. 0이면 제한 없음 (절대 상한만 적용).");
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
                "냉각 대기 포함이므로 충분히 설정하세요.");

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
