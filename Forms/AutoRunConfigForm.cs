using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Core.AutoRun;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// AutoRun 설정 폼
    /// </summary>
    public partial class AutoRunConfigForm : Form
    {
        private AutoRunConfiguration _config;

        public AutoRunConfiguration Configuration => _config;

        public AutoRunConfigForm(AutoRunConfiguration config)
        {
            _config = config ?? new AutoRunConfiguration();
            InitializeComponent();
            LoadConfiguration();
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
            }
            catch (Exception ex)
            {
                throw new Exception($"설정 저장 중 오류: {ex.Message}");
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
    }
}