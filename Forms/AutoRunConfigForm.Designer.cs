namespace VacX_OutSense.Forms
{
    partial class AutoRunConfigForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabPressure = new TabPage();
            lblPressureInfo = new Label();
            txtMaxPressureDuringExperiment = new TextBox();
            lblMaxPressureDuringExperiment = new Label();
            txtTargetPressureForHeater = new TextBox();
            lblTargetPressureForHeater = new Label();
            txtTargetPressureForIonGauge = new TextBox();
            lblTargetPressureForIonGauge = new Label();
            txtTargetPressureForTurboPump = new TextBox();
            lblTargetPressureForTurboPump = new Label();
            tabTemperature = new TabPage();
            txtTemperatureStabilityTolerance = new TextBox();
            lblTemperatureStabilityTolerance = new Label();
            txtHeaterRampUpRate = new TextBox();
            lblHeaterRampUpRate = new Label();
            lblTempNote = new Label();
            txtHeaterCh1SetTemperature = new TextBox();
            lblHeaterCh1SetTemperature = new Label();
            lblShutdownTempHeader = new Label();
            txtCoolingTargetTemperature = new TextBox();
            lblCoolingTargetTemperature = new Label();
            txtVentTargetPressure = new TextBox();
            lblVentTargetPressure = new Label();
            tabTime = new TabPage();
            nudDataLoggingIntervalSeconds = new NumericUpDown();
            lblDataLoggingInterval = new Label();
            nudExperimentHours = new NumericUpDown();
            lblExpHoursUnit = new Label();
            nudExperimentMinutes = new NumericUpDown();
            lblExpMinutesUnit = new Label();
            lblExperimentDuration = new Label();
            tabTimeout = new TabPage();
            nudShutdownTimeout = new NumericUpDown();
            lblShutdownTimeout = new Label();
            nudHeaterStartTimeout = new NumericUpDown();
            lblHeaterStartTimeout = new Label();
            nudHighVacuumTimeout = new NumericUpDown();
            lblHighVacuumTimeout = new Label();
            nudIonGaugeActivationTimeout = new NumericUpDown();
            lblIonGaugeActivationTimeout = new Label();
            nudTurboPumpStartTimeout = new NumericUpDown();
            lblTurboPumpStartTimeout = new Label();
            nudDryPumpStartTimeout = new NumericUpDown();
            lblDryPumpStartTimeout = new Label();
            nudValveOperationTimeout = new NumericUpDown();
            lblValveOperationTimeout = new Label();
            nudInitializationTimeout = new NumericUpDown();
            lblInitializationTimeout = new Label();
            tabMisc = new TabPage();
            chkEnableAlarmOnError = new CheckBox();
            chkEnableSafeShutdownOnFailure = new CheckBox();
            chkEnableDetailedLogging = new CheckBox();
            nudRetryDelaySeconds = new NumericUpDown();
            lblRetryDelaySeconds = new Label();
            nudMaxRetryCount = new NumericUpDown();
            lblMaxRetryCount = new Label();
            cmbRunMode = new ComboBox();
            lblRunMode = new Label();
            lblRunModeDesc = new Label();
            panelButtons = new Panel();
            btnCancel = new Button();
            btnOk = new Button();
            btnLoadDefault = new Button();
            tabControl1.SuspendLayout();
            tabPressure.SuspendLayout();
            tabTemperature.SuspendLayout();
            tabTime.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudDataLoggingIntervalSeconds).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudExperimentHours).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudExperimentMinutes).BeginInit();
            tabTimeout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudShutdownTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudHeaterStartTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudHighVacuumTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudIonGaugeActivationTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudTurboPumpStartTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudDryPumpStartTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudValveOperationTimeout).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudInitializationTimeout).BeginInit();
            tabMisc.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)nudRetryDelaySeconds).BeginInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxRetryCount).BeginInit();
            panelButtons.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPressure);
            tabControl1.Controls.Add(tabTemperature);
            tabControl1.Controls.Add(tabTime);
            tabControl1.Controls.Add(tabTimeout);
            tabControl1.Controls.Add(tabMisc);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 0);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(444, 458);
            tabControl1.TabIndex = 0;
            // 
            // tabPressure
            // 
            tabPressure.Controls.Add(lblPressureInfo);
            tabPressure.Controls.Add(txtMaxPressureDuringExperiment);
            tabPressure.Controls.Add(lblMaxPressureDuringExperiment);
            tabPressure.Controls.Add(txtTargetPressureForHeater);
            tabPressure.Controls.Add(lblTargetPressureForHeater);
            tabPressure.Controls.Add(txtTargetPressureForIonGauge);
            tabPressure.Controls.Add(lblTargetPressureForIonGauge);
            tabPressure.Controls.Add(txtTargetPressureForTurboPump);
            tabPressure.Controls.Add(lblTargetPressureForTurboPump);
            tabPressure.Location = new Point(4, 24);
            tabPressure.Name = "tabPressure";
            tabPressure.Padding = new Padding(3);
            tabPressure.Size = new Size(436, 430);
            tabPressure.TabIndex = 0;
            tabPressure.Text = "압력 설정";
            tabPressure.UseVisualStyleBackColor = true;
            // 
            // lblPressureInfo
            // 
            lblPressureInfo.ForeColor = SystemColors.GrayText;
            lblPressureInfo.Location = new Point(20, 170);
            lblPressureInfo.Name = "lblPressureInfo";
            lblPressureInfo.Size = new Size(400, 40);
            lblPressureInfo.TabIndex = 8;
            lblPressureInfo.Text = "※ 압력값은 과학적 표기법(예: 1.00E-05)으로 입력하세요.";
            // 
            // txtMaxPressureDuringExperiment
            // 
            txtMaxPressureDuringExperiment.Location = new Point(230, 130);
            txtMaxPressureDuringExperiment.Name = "txtMaxPressureDuringExperiment";
            txtMaxPressureDuringExperiment.Size = new Size(120, 23);
            txtMaxPressureDuringExperiment.TabIndex = 7;
            // 
            // lblMaxPressureDuringExperiment
            // 
            lblMaxPressureDuringExperiment.AutoSize = true;
            lblMaxPressureDuringExperiment.Location = new Point(20, 133);
            lblMaxPressureDuringExperiment.Name = "lblMaxPressureDuringExperiment";
            lblMaxPressureDuringExperiment.Size = new Size(175, 15);
            lblMaxPressureDuringExperiment.TabIndex = 6;
            lblMaxPressureDuringExperiment.Text = "실험 중 최대 허용 압력 (Torr):";
            // 
            // txtTargetPressureForHeater
            // 
            txtTargetPressureForHeater.Location = new Point(230, 93);
            txtTargetPressureForHeater.Name = "txtTargetPressureForHeater";
            txtTargetPressureForHeater.Size = new Size(120, 23);
            txtTargetPressureForHeater.TabIndex = 5;
            // 
            // lblTargetPressureForHeater
            // 
            lblTargetPressureForHeater.AutoSize = true;
            lblTargetPressureForHeater.Location = new Point(20, 96);
            lblTargetPressureForHeater.Name = "lblTargetPressureForHeater";
            lblTargetPressureForHeater.Size = new Size(175, 15);
            lblTargetPressureForHeater.TabIndex = 4;
            lblTargetPressureForHeater.Text = "히터 시작 목표 압력 (Torr):";
            // 
            // txtTargetPressureForIonGauge
            // 
            txtTargetPressureForIonGauge.Location = new Point(230, 56);
            txtTargetPressureForIonGauge.Name = "txtTargetPressureForIonGauge";
            txtTargetPressureForIonGauge.Size = new Size(120, 23);
            txtTargetPressureForIonGauge.TabIndex = 3;
            // 
            // lblTargetPressureForIonGauge
            // 
            lblTargetPressureForIonGauge.AutoSize = true;
            lblTargetPressureForIonGauge.Location = new Point(20, 59);
            lblTargetPressureForIonGauge.Name = "lblTargetPressureForIonGauge";
            lblTargetPressureForIonGauge.Size = new Size(193, 15);
            lblTargetPressureForIonGauge.TabIndex = 2;
            lblTargetPressureForIonGauge.Text = "이온게이지 활성화 압력 (Torr):";
            // 
            // txtTargetPressureForTurboPump
            // 
            txtTargetPressureForTurboPump.Location = new Point(230, 20);
            txtTargetPressureForTurboPump.Name = "txtTargetPressureForTurboPump";
            txtTargetPressureForTurboPump.Size = new Size(120, 23);
            txtTargetPressureForTurboPump.TabIndex = 1;
            // 
            // lblTargetPressureForTurboPump
            // 
            lblTargetPressureForTurboPump.AutoSize = true;
            lblTargetPressureForTurboPump.Location = new Point(20, 23);
            lblTargetPressureForTurboPump.Name = "lblTargetPressureForTurboPump";
            lblTargetPressureForTurboPump.Size = new Size(193, 15);
            lblTargetPressureForTurboPump.TabIndex = 0;
            lblTargetPressureForTurboPump.Text = "터보펌프 시작 목표 압력 (Torr):";
            // 
            // tabTemperature
            // 
            tabTemperature.Controls.Add(lblTempNote);
            tabTemperature.Controls.Add(txtTemperatureStabilityTolerance);
            tabTemperature.Controls.Add(lblTemperatureStabilityTolerance);
            tabTemperature.Controls.Add(txtHeaterRampUpRate);
            tabTemperature.Controls.Add(lblHeaterRampUpRate);
            tabTemperature.Controls.Add(txtHeaterCh1SetTemperature);
            tabTemperature.Controls.Add(lblHeaterCh1SetTemperature);
            tabTemperature.Controls.Add(lblShutdownTempHeader);
            tabTemperature.Controls.Add(txtCoolingTargetTemperature);
            tabTemperature.Controls.Add(lblCoolingTargetTemperature);
            tabTemperature.Controls.Add(txtVentTargetPressure);
            tabTemperature.Controls.Add(lblVentTargetPressure);
            tabTemperature.Location = new Point(4, 24);
            tabTemperature.Name = "tabTemperature";
            tabTemperature.Size = new Size(436, 430);
            tabTemperature.TabIndex = 1;
            tabTemperature.Text = "온도 설정";
            tabTemperature.UseVisualStyleBackColor = true;
            // 
            // txtTemperatureStabilityTolerance
            // 
            txtTemperatureStabilityTolerance.Location = new Point(230, 131);
            txtTemperatureStabilityTolerance.Name = "txtTemperatureStabilityTolerance";
            txtTemperatureStabilityTolerance.Size = new Size(120, 23);
            txtTemperatureStabilityTolerance.TabIndex = 7;
            // 
            // lblTemperatureStabilityTolerance
            // 
            lblTemperatureStabilityTolerance.AutoSize = true;
            lblTemperatureStabilityTolerance.Location = new Point(20, 134);
            lblTemperatureStabilityTolerance.Name = "lblTemperatureStabilityTolerance";
            lblTemperatureStabilityTolerance.Size = new Size(147, 15);
            lblTemperatureStabilityTolerance.TabIndex = 6;
            lblTemperatureStabilityTolerance.Text = "온도 허용 오차 (±°C):";
            // 
            // txtHeaterRampUpRate
            // 
            txtHeaterRampUpRate.Location = new Point(230, 94);
            txtHeaterRampUpRate.Name = "txtHeaterRampUpRate";
            txtHeaterRampUpRate.Size = new Size(120, 23);
            txtHeaterRampUpRate.TabIndex = 5;
            // 
            // lblHeaterRampUpRate
            // 
            lblHeaterRampUpRate.AutoSize = true;
            lblHeaterRampUpRate.Location = new Point(20, 97);
            lblHeaterRampUpRate.Name = "lblHeaterRampUpRate";
            lblHeaterRampUpRate.Size = new Size(155, 15);
            lblHeaterRampUpRate.TabIndex = 4;
            lblHeaterRampUpRate.Text = "히터 램프 속도 (°C/min):";
            // 
            // txtHeaterCh1SetTemperature
            // 
            txtHeaterCh1SetTemperature.Location = new Point(230, 20);
            txtHeaterCh1SetTemperature.Name = "txtHeaterCh1SetTemperature";
            txtHeaterCh1SetTemperature.Size = new Size(120, 23);
            txtHeaterCh1SetTemperature.TabIndex = 1;
            // 
            // lblHeaterCh1SetTemperature
            // 
            lblHeaterCh1SetTemperature.AutoSize = true;
            lblHeaterCh1SetTemperature.Location = new Point(20, 23);
            lblHeaterCh1SetTemperature.Name = "lblHeaterCh1SetTemperature";
            lblHeaterCh1SetTemperature.Size = new Size(146, 15);
            lblHeaterCh1SetTemperature.TabIndex = 0;
            lblHeaterCh1SetTemperature.Text = "히터 CH1 설정 온도 (°C):";
            // 
            // lblShutdownTempHeader
            // 
            lblShutdownTempHeader.AutoSize = true;
            lblShutdownTempHeader.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            lblShutdownTempHeader.Location = new Point(20, 175);
            lblShutdownTempHeader.Name = "lblShutdownTempHeader";
            lblShutdownTempHeader.Size = new Size(120, 15);
            lblShutdownTempHeader.TabIndex = 9;
            lblShutdownTempHeader.Text = "── 종료 시퀀스 설정";
            // 
            // txtCoolingTargetTemperature
            // 
            txtCoolingTargetTemperature.Location = new Point(230, 200);
            txtCoolingTargetTemperature.Name = "txtCoolingTargetTemperature";
            txtCoolingTargetTemperature.Size = new Size(120, 23);
            txtCoolingTargetTemperature.TabIndex = 11;
            // 
            // lblCoolingTargetTemperature
            // 
            lblCoolingTargetTemperature.AutoSize = true;
            lblCoolingTargetTemperature.Location = new Point(20, 203);
            lblCoolingTargetTemperature.Name = "lblCoolingTargetTemperature";
            lblCoolingTargetTemperature.Size = new Size(152, 15);
            lblCoolingTargetTemperature.TabIndex = 10;
            lblCoolingTargetTemperature.Text = "쿨링 목표 온도 (°C):";
            // 
            // txtVentTargetPressure
            // 
            txtVentTargetPressure.Location = new Point(230, 237);
            txtVentTargetPressure.Name = "txtVentTargetPressure";
            txtVentTargetPressure.Size = new Size(120, 23);
            txtVentTargetPressure.TabIndex = 13;
            // 
            // lblVentTargetPressure
            // 
            lblVentTargetPressure.AutoSize = true;
            lblVentTargetPressure.Location = new Point(20, 240);
            lblVentTargetPressure.Name = "lblVentTargetPressure";
            lblVentTargetPressure.Size = new Size(175, 15);
            lblVentTargetPressure.TabIndex = 12;
            lblVentTargetPressure.Text = "벤트 목표 압력 (kPa, ATM):";
            // 
            // lblTempNote
            // 
            lblTempNote.ForeColor = SystemColors.GrayText;
            lblTempNote.Location = new Point(20, 280);
            lblTempNote.Name = "lblTempNote";
            lblTempNote.Size = new Size(400, 40);
            lblTempNote.TabIndex = 14;
            lblTempNote.Text = "※ CH2 온도는 칠러 PID가 자동 제어합니다.\r\n   (Main 탭 → 칠러 PID 설정에서 조정)";
            // 
            // tabTime
            // 
            tabTime.Controls.Add(nudDataLoggingIntervalSeconds);
            tabTime.Controls.Add(lblDataLoggingInterval);
            tabTime.Controls.Add(lblExperimentDuration);
            tabTime.Controls.Add(nudExperimentHours);
            tabTime.Controls.Add(lblExpHoursUnit);
            tabTime.Controls.Add(nudExperimentMinutes);
            tabTime.Controls.Add(lblExpMinutesUnit);
            tabTime.Location = new Point(4, 24);
            tabTime.Name = "tabTime";
            tabTime.Size = new Size(436, 430);
            tabTime.TabIndex = 2;
            tabTime.Text = "시간 설정";
            tabTime.UseVisualStyleBackColor = true;
            // 
            // lblExperimentDuration
            // 
            lblExperimentDuration.AutoSize = true;
            lblExperimentDuration.Location = new Point(20, 22);
            lblExperimentDuration.Name = "lblExperimentDuration";
            lblExperimentDuration.Size = new Size(90, 15);
            lblExperimentDuration.TabIndex = 0;
            lblExperimentDuration.Text = "실험 지속 시간:";
            // 
            // nudExperimentHours
            // 
            nudExperimentHours.Location = new Point(150, 20);
            nudExperimentHours.Maximum = new decimal(new int[] { 168, 0, 0, 0 });
            nudExperimentHours.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            nudExperimentHours.Name = "nudExperimentHours";
            nudExperimentHours.Size = new Size(60, 23);
            nudExperimentHours.TabIndex = 1;
            nudExperimentHours.Value = new decimal(new int[] { 24, 0, 0, 0 });
            // 
            // lblExpHoursUnit
            // 
            lblExpHoursUnit.AutoSize = true;
            lblExpHoursUnit.Location = new Point(214, 22);
            lblExpHoursUnit.Name = "lblExpHoursUnit";
            lblExpHoursUnit.Size = new Size(30, 15);
            lblExpHoursUnit.TabIndex = 2;
            lblExpHoursUnit.Text = "시간";
            // 
            // nudExperimentMinutes
            // 
            nudExperimentMinutes.Location = new Point(250, 20);
            nudExperimentMinutes.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            nudExperimentMinutes.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            nudExperimentMinutes.Name = "nudExperimentMinutes";
            nudExperimentMinutes.Size = new Size(60, 23);
            nudExperimentMinutes.TabIndex = 3;
            nudExperimentMinutes.Value = new decimal(new int[] { 0, 0, 0, 0 });
            // 
            // lblExpMinutesUnit
            // 
            lblExpMinutesUnit.AutoSize = true;
            lblExpMinutesUnit.Location = new Point(314, 22);
            lblExpMinutesUnit.Name = "lblExpMinutesUnit";
            lblExpMinutesUnit.Size = new Size(15, 15);
            lblExpMinutesUnit.TabIndex = 4;
            lblExpMinutesUnit.Text = "분";
            // 
            // nudDataLoggingIntervalSeconds
            // 
            nudDataLoggingIntervalSeconds.Location = new Point(230, 60);
            nudDataLoggingIntervalSeconds.Maximum = new decimal(new int[] { 3600, 0, 0, 0 });
            nudDataLoggingIntervalSeconds.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudDataLoggingIntervalSeconds.Name = "nudDataLoggingIntervalSeconds";
            nudDataLoggingIntervalSeconds.Size = new Size(100, 23);
            nudDataLoggingIntervalSeconds.TabIndex = 6;
            nudDataLoggingIntervalSeconds.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // lblDataLoggingInterval
            // 
            lblDataLoggingInterval.AutoSize = true;
            lblDataLoggingInterval.Location = new Point(20, 62);
            lblDataLoggingInterval.Name = "lblDataLoggingInterval";
            lblDataLoggingInterval.Size = new Size(126, 15);
            lblDataLoggingInterval.TabIndex = 5;
            lblDataLoggingInterval.Text = "데이터 로깅 간격 (초):";
            // 
            // tabTimeout
            // 
            tabTimeout.AutoScroll = true;
            tabTimeout.Controls.Add(nudShutdownTimeout);
            tabTimeout.Controls.Add(lblShutdownTimeout);
            tabTimeout.Controls.Add(nudHeaterStartTimeout);
            tabTimeout.Controls.Add(lblHeaterStartTimeout);
            tabTimeout.Controls.Add(nudHighVacuumTimeout);
            tabTimeout.Controls.Add(lblHighVacuumTimeout);
            tabTimeout.Controls.Add(nudIonGaugeActivationTimeout);
            tabTimeout.Controls.Add(lblIonGaugeActivationTimeout);
            tabTimeout.Controls.Add(nudTurboPumpStartTimeout);
            tabTimeout.Controls.Add(lblTurboPumpStartTimeout);
            tabTimeout.Controls.Add(nudDryPumpStartTimeout);
            tabTimeout.Controls.Add(lblDryPumpStartTimeout);
            tabTimeout.Controls.Add(nudValveOperationTimeout);
            tabTimeout.Controls.Add(lblValveOperationTimeout);
            tabTimeout.Controls.Add(nudInitializationTimeout);
            tabTimeout.Controls.Add(lblInitializationTimeout);
            tabTimeout.Location = new Point(4, 24);
            tabTimeout.Name = "tabTimeout";
            tabTimeout.Size = new Size(436, 430);
            tabTimeout.TabIndex = 3;
            tabTimeout.Text = "타임아웃 설정";
            tabTimeout.UseVisualStyleBackColor = true;
            // 
            // nudShutdownTimeout
            // 
            nudShutdownTimeout.Location = new Point(260, 280);
            nudShutdownTimeout.Maximum = new decimal(new int[] { 14400, 0, 0, 0 });
            nudShutdownTimeout.Minimum = new decimal(new int[] { 1800, 0, 0, 0 });
            nudShutdownTimeout.Name = "nudShutdownTimeout";
            nudShutdownTimeout.Size = new Size(100, 23);
            nudShutdownTimeout.TabIndex = 15;
            nudShutdownTimeout.Value = new decimal(new int[] { 7200, 0, 0, 0 });
            // 
            // lblShutdownTimeout
            // 
            lblShutdownTimeout.AutoSize = true;
            lblShutdownTimeout.Location = new Point(20, 282);
            lblShutdownTimeout.Name = "lblShutdownTimeout";
            lblShutdownTimeout.Size = new Size(150, 15);
            lblShutdownTimeout.TabIndex = 14;
            lblShutdownTimeout.Text = "종료 시퀀스 타임아웃 (초):";
            // 
            // nudHeaterStartTimeout
            // 
            nudHeaterStartTimeout.Location = new Point(260, 245);
            nudHeaterStartTimeout.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            nudHeaterStartTimeout.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudHeaterStartTimeout.Name = "nudHeaterStartTimeout";
            nudHeaterStartTimeout.Size = new Size(100, 23);
            nudHeaterStartTimeout.TabIndex = 13;
            nudHeaterStartTimeout.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // lblHeaterStartTimeout
            // 
            lblHeaterStartTimeout.AutoSize = true;
            lblHeaterStartTimeout.Location = new Point(20, 247);
            lblHeaterStartTimeout.Name = "lblHeaterStartTimeout";
            lblHeaterStartTimeout.Size = new Size(138, 15);
            lblHeaterStartTimeout.TabIndex = 12;
            lblHeaterStartTimeout.Text = "히터 시작 타임아웃 (초):";
            // 
            // nudHighVacuumTimeout
            // 
            nudHighVacuumTimeout.Location = new Point(260, 210);
            nudHighVacuumTimeout.Maximum = new decimal(new int[] { 7200, 0, 0, 0 });
            nudHighVacuumTimeout.Minimum = new decimal(new int[] { 60, 0, 0, 0 });
            nudHighVacuumTimeout.Name = "nudHighVacuumTimeout";
            nudHighVacuumTimeout.Size = new Size(100, 23);
            nudHighVacuumTimeout.TabIndex = 11;
            nudHighVacuumTimeout.Value = new decimal(new int[] { 3600, 0, 0, 0 });
            // 
            // lblHighVacuumTimeout
            // 
            lblHighVacuumTimeout.AutoSize = true;
            lblHighVacuumTimeout.Location = new Point(20, 212);
            lblHighVacuumTimeout.Name = "lblHighVacuumTimeout";
            lblHighVacuumTimeout.Size = new Size(150, 15);
            lblHighVacuumTimeout.TabIndex = 10;
            lblHighVacuumTimeout.Text = "고진공 도달 타임아웃 (초):";
            // 
            // nudIonGaugeActivationTimeout
            // 
            nudIonGaugeActivationTimeout.Location = new Point(260, 175);
            nudIonGaugeActivationTimeout.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
            nudIonGaugeActivationTimeout.Minimum = new decimal(new int[] { 30, 0, 0, 0 });
            nudIonGaugeActivationTimeout.Name = "nudIonGaugeActivationTimeout";
            nudIonGaugeActivationTimeout.Size = new Size(100, 23);
            nudIonGaugeActivationTimeout.TabIndex = 9;
            nudIonGaugeActivationTimeout.Value = new decimal(new int[] { 300, 0, 0, 0 });
            // 
            // lblIonGaugeActivationTimeout
            // 
            lblIonGaugeActivationTimeout.AutoSize = true;
            lblIonGaugeActivationTimeout.Location = new Point(20, 177);
            lblIonGaugeActivationTimeout.Name = "lblIonGaugeActivationTimeout";
            lblIonGaugeActivationTimeout.Size = new Size(193, 15);
            lblIonGaugeActivationTimeout.TabIndex = 8;
            lblIonGaugeActivationTimeout.Text = "이온게이지 활성화 타임아웃 (초):";
            // 
            // nudTurboPumpStartTimeout
            // 
            nudTurboPumpStartTimeout.Location = new Point(260, 140);
            nudTurboPumpStartTimeout.Maximum = new decimal(new int[] { 1800, 0, 0, 0 });
            nudTurboPumpStartTimeout.Minimum = new decimal(new int[] { 60, 0, 0, 0 });
            nudTurboPumpStartTimeout.Name = "nudTurboPumpStartTimeout";
            nudTurboPumpStartTimeout.Size = new Size(100, 23);
            nudTurboPumpStartTimeout.TabIndex = 7;
            nudTurboPumpStartTimeout.Value = new decimal(new int[] { 600, 0, 0, 0 });
            // 
            // lblTurboPumpStartTimeout
            // 
            lblTurboPumpStartTimeout.AutoSize = true;
            lblTurboPumpStartTimeout.Location = new Point(20, 142);
            lblTurboPumpStartTimeout.Name = "lblTurboPumpStartTimeout";
            lblTurboPumpStartTimeout.Size = new Size(162, 15);
            lblTurboPumpStartTimeout.TabIndex = 6;
            lblTurboPumpStartTimeout.Text = "터보펌프 시작 타임아웃 (초):";
            // 
            // nudDryPumpStartTimeout
            // 
            nudDryPumpStartTimeout.Location = new Point(260, 105);
            nudDryPumpStartTimeout.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            nudDryPumpStartTimeout.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudDryPumpStartTimeout.Name = "nudDryPumpStartTimeout";
            nudDryPumpStartTimeout.Size = new Size(100, 23);
            nudDryPumpStartTimeout.TabIndex = 5;
            nudDryPumpStartTimeout.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // lblDryPumpStartTimeout
            // 
            lblDryPumpStartTimeout.AutoSize = true;
            lblDryPumpStartTimeout.Location = new Point(20, 107);
            lblDryPumpStartTimeout.Name = "lblDryPumpStartTimeout";
            lblDryPumpStartTimeout.Size = new Size(174, 15);
            lblDryPumpStartTimeout.TabIndex = 4;
            lblDryPumpStartTimeout.Text = "드라이펌프 시작 타임아웃 (초):";
            // 
            // nudValveOperationTimeout
            // 
            nudValveOperationTimeout.Location = new Point(260, 70);
            nudValveOperationTimeout.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            nudValveOperationTimeout.Minimum = new decimal(new int[] { 5, 0, 0, 0 });
            nudValveOperationTimeout.Name = "nudValveOperationTimeout";
            nudValveOperationTimeout.Size = new Size(100, 23);
            nudValveOperationTimeout.TabIndex = 3;
            nudValveOperationTimeout.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // lblValveOperationTimeout
            // 
            lblValveOperationTimeout.AutoSize = true;
            lblValveOperationTimeout.Location = new Point(20, 72);
            lblValveOperationTimeout.Name = "lblValveOperationTimeout";
            lblValveOperationTimeout.Size = new Size(162, 15);
            lblValveOperationTimeout.TabIndex = 2;
            lblValveOperationTimeout.Text = "밸브 작동 타임아웃 (초):";
            // 
            // nudInitializationTimeout
            // 
            nudInitializationTimeout.Location = new Point(260, 35);
            nudInitializationTimeout.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            nudInitializationTimeout.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            nudInitializationTimeout.Name = "nudInitializationTimeout";
            nudInitializationTimeout.Size = new Size(100, 23);
            nudInitializationTimeout.TabIndex = 1;
            nudInitializationTimeout.Value = new decimal(new int[] { 60, 0, 0, 0 });
            // 
            // lblInitializationTimeout
            // 
            lblInitializationTimeout.AutoSize = true;
            lblInitializationTimeout.Location = new Point(20, 37);
            lblInitializationTimeout.Name = "lblInitializationTimeout";
            lblInitializationTimeout.Size = new Size(126, 15);
            lblInitializationTimeout.TabIndex = 0;
            lblInitializationTimeout.Text = "초기화 타임아웃 (초):";
            // 
            // tabMisc
            // 
            tabMisc.Controls.Add(chkEnableAlarmOnError);
            tabMisc.Controls.Add(chkEnableSafeShutdownOnFailure);
            tabMisc.Controls.Add(chkEnableDetailedLogging);
            tabMisc.Controls.Add(nudRetryDelaySeconds);
            tabMisc.Controls.Add(lblRetryDelaySeconds);
            tabMisc.Controls.Add(nudMaxRetryCount);
            tabMisc.Controls.Add(lblMaxRetryCount);
            tabMisc.Controls.Add(cmbRunMode);
            tabMisc.Controls.Add(lblRunMode);
            tabMisc.Controls.Add(lblRunModeDesc);
            tabMisc.Location = new Point(4, 24);
            tabMisc.Name = "tabMisc";
            tabMisc.Size = new Size(436, 430);
            tabMisc.TabIndex = 4;
            tabMisc.Text = "기타 설정";
            tabMisc.UseVisualStyleBackColor = true;
            // 
            // lblRunModeDesc
            // 
            lblRunModeDesc.ForeColor = SystemColors.GrayText;
            lblRunModeDesc.Location = new Point(20, 55);
            lblRunModeDesc.Name = "lblRunModeDesc";
            lblRunModeDesc.Size = new Size(400, 50);
            lblRunModeDesc.TabIndex = 9;
            lblRunModeDesc.Text = "· FullAuto: 모든 단계를 자동으로 연속 실행\r\n" +
                "· StepByStep: 각 단계 완료 후 사용자 확인 대기\r\n" +
                "· Simulation: 실제 장비 제어 없이 시퀀스 테스트";
            // 
            // chkEnableAlarmOnError
            // 
            chkEnableAlarmOnError.AutoSize = true;
            chkEnableAlarmOnError.Location = new Point(20, 255);
            chkEnableAlarmOnError.Name = "chkEnableAlarmOnError";
            chkEnableAlarmOnError.Size = new Size(156, 19);
            chkEnableAlarmOnError.TabIndex = 8;
            chkEnableAlarmOnError.Text = "오류 시 알람 활성화";
            chkEnableAlarmOnError.UseVisualStyleBackColor = true;
            // 
            // chkEnableSafeShutdownOnFailure
            // 
            chkEnableSafeShutdownOnFailure.AutoSize = true;
            chkEnableSafeShutdownOnFailure.Checked = true;
            chkEnableSafeShutdownOnFailure.CheckState = CheckState.Checked;
            chkEnableSafeShutdownOnFailure.Location = new Point(20, 225);
            chkEnableSafeShutdownOnFailure.Name = "chkEnableSafeShutdownOnFailure";
            chkEnableSafeShutdownOnFailure.Size = new Size(192, 19);
            chkEnableSafeShutdownOnFailure.TabIndex = 7;
            chkEnableSafeShutdownOnFailure.Text = "실패 시 안전 종료 활성화";
            chkEnableSafeShutdownOnFailure.UseVisualStyleBackColor = true;
            // 
            // chkEnableDetailedLogging
            // 
            chkEnableDetailedLogging.AutoSize = true;
            chkEnableDetailedLogging.Checked = true;
            chkEnableDetailedLogging.CheckState = CheckState.Checked;
            chkEnableDetailedLogging.Location = new Point(20, 195);
            chkEnableDetailedLogging.Name = "chkEnableDetailedLogging";
            chkEnableDetailedLogging.Size = new Size(132, 19);
            chkEnableDetailedLogging.TabIndex = 6;
            chkEnableDetailedLogging.Text = "상세 로깅 활성화";
            chkEnableDetailedLogging.UseVisualStyleBackColor = true;
            // 
            // nudRetryDelaySeconds
            // 
            nudRetryDelaySeconds.Location = new Point(230, 150);
            nudRetryDelaySeconds.Maximum = new decimal(new int[] { 120, 0, 0, 0 });
            nudRetryDelaySeconds.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            nudRetryDelaySeconds.Name = "nudRetryDelaySeconds";
            nudRetryDelaySeconds.Size = new Size(100, 23);
            nudRetryDelaySeconds.TabIndex = 5;
            nudRetryDelaySeconds.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // lblRetryDelaySeconds
            // 
            lblRetryDelaySeconds.AutoSize = true;
            lblRetryDelaySeconds.Location = new Point(20, 152);
            lblRetryDelaySeconds.Name = "lblRetryDelaySeconds";
            lblRetryDelaySeconds.Size = new Size(132, 15);
            lblRetryDelaySeconds.TabIndex = 4;
            lblRetryDelaySeconds.Text = "재시도 대기 시간 (초):";
            // 
            // nudMaxRetryCount
            // 
            nudMaxRetryCount.Location = new Point(230, 115);
            nudMaxRetryCount.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            nudMaxRetryCount.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            nudMaxRetryCount.Name = "nudMaxRetryCount";
            nudMaxRetryCount.Size = new Size(100, 23);
            nudMaxRetryCount.TabIndex = 3;
            nudMaxRetryCount.Value = new decimal(new int[] { 3, 0, 0, 0 });
            // 
            // lblMaxRetryCount
            // 
            lblMaxRetryCount.AutoSize = true;
            lblMaxRetryCount.Location = new Point(20, 117);
            lblMaxRetryCount.Name = "lblMaxRetryCount";
            lblMaxRetryCount.Size = new Size(120, 15);
            lblMaxRetryCount.TabIndex = 2;
            lblMaxRetryCount.Text = "최대 재시도 횟수:";
            // 
            // cmbRunMode
            // 
            cmbRunMode.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbRunMode.FormattingEnabled = true;
            cmbRunMode.Location = new Point(230, 25);
            cmbRunMode.Name = "cmbRunMode";
            cmbRunMode.Size = new Size(150, 23);
            cmbRunMode.TabIndex = 1;
            // 
            // lblRunMode
            // 
            lblRunMode.AutoSize = true;
            lblRunMode.Location = new Point(20, 28);
            lblRunMode.Name = "lblRunMode";
            lblRunMode.Size = new Size(62, 15);
            lblRunMode.TabIndex = 0;
            lblRunMode.Text = "실행 모드:";
            // 
            // panelButtons
            // 
            panelButtons.Controls.Add(btnLoadDefault);
            panelButtons.Controls.Add(btnCancel);
            panelButtons.Controls.Add(btnOk);
            panelButtons.Dock = DockStyle.Bottom;
            panelButtons.Location = new Point(0, 458);
            panelButtons.Name = "panelButtons";
            panelButtons.Size = new Size(444, 50);
            panelButtons.TabIndex = 1;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(260, 10);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 30);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "취소";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            btnOk.Location = new Point(355, 10);
            btnOk.Name = "btnOk";
            btnOk.Size = new Size(80, 30);
            btnOk.TabIndex = 1;
            btnOk.Text = "확인";
            btnOk.UseVisualStyleBackColor = true;
            btnOk.Click += BtnOk_Click;
            // 
            // btnLoadDefault
            // 
            btnLoadDefault.Location = new Point(12, 10);
            btnLoadDefault.Name = "btnLoadDefault";
            btnLoadDefault.Size = new Size(80, 30);
            btnLoadDefault.TabIndex = 0;
            btnLoadDefault.Text = "기본값";
            btnLoadDefault.UseVisualStyleBackColor = true;
            btnLoadDefault.Click += BtnLoadDefault_Click;
            // 
            // AutoRunConfigForm
            // 
            AcceptButton = btnOk;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(444, 508);
            Controls.Add(tabControl1);
            Controls.Add(panelButtons);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "AutoRunConfigForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "AutoRun 설정";
            tabControl1.ResumeLayout(false);
            tabPressure.ResumeLayout(false);
            tabPressure.PerformLayout();
            tabTemperature.ResumeLayout(false);
            tabTemperature.PerformLayout();
            tabTime.ResumeLayout(false);
            tabTime.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudDataLoggingIntervalSeconds).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudExperimentHours).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudExperimentMinutes).EndInit();
            tabTimeout.ResumeLayout(false);
            tabTimeout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudShutdownTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHeaterStartTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudHighVacuumTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudIonGaugeActivationTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudTurboPumpStartTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudDryPumpStartTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudValveOperationTimeout).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudInitializationTimeout).EndInit();
            tabMisc.ResumeLayout(false);
            tabMisc.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)nudRetryDelaySeconds).EndInit();
            ((System.ComponentModel.ISupportInitialize)nudMaxRetryCount).EndInit();
            panelButtons.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        // 탭 컨트롤
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPressure;
        private System.Windows.Forms.TabPage tabTemperature;
        private System.Windows.Forms.TabPage tabTime;
        private System.Windows.Forms.TabPage tabTimeout;
        private System.Windows.Forms.TabPage tabMisc;
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnLoadDefault;

        // 압력 설정 컨트롤
        private System.Windows.Forms.Label lblPressureInfo;
        private System.Windows.Forms.TextBox txtMaxPressureDuringExperiment;
        private System.Windows.Forms.Label lblMaxPressureDuringExperiment;
        private System.Windows.Forms.TextBox txtTargetPressureForHeater;
        private System.Windows.Forms.Label lblTargetPressureForHeater;
        private System.Windows.Forms.TextBox txtTargetPressureForIonGauge;
        private System.Windows.Forms.Label lblTargetPressureForIonGauge;
        private System.Windows.Forms.TextBox txtTargetPressureForTurboPump;
        private System.Windows.Forms.Label lblTargetPressureForTurboPump;

        // 온도 설정 컨트롤
        private System.Windows.Forms.TextBox txtHeaterCh1SetTemperature;
        private System.Windows.Forms.Label lblHeaterCh1SetTemperature;
        private System.Windows.Forms.TextBox txtHeaterRampUpRate;
        private System.Windows.Forms.Label lblHeaterRampUpRate;
        private System.Windows.Forms.TextBox txtTemperatureStabilityTolerance;
        private System.Windows.Forms.Label lblTemperatureStabilityTolerance;
        private System.Windows.Forms.Label lblTempNote;

        // 종료 시퀀스 설정 컨트롤 (온도 탭 내)
        private System.Windows.Forms.Label lblShutdownTempHeader;
        private System.Windows.Forms.TextBox txtCoolingTargetTemperature;
        private System.Windows.Forms.Label lblCoolingTargetTemperature;
        private System.Windows.Forms.TextBox txtVentTargetPressure;
        private System.Windows.Forms.Label lblVentTargetPressure;

        // 시간 설정 컨트롤
        private System.Windows.Forms.NumericUpDown nudExperimentHours;
        private System.Windows.Forms.Label lblExpHoursUnit;
        private System.Windows.Forms.NumericUpDown nudExperimentMinutes;
        private System.Windows.Forms.Label lblExpMinutesUnit;
        private System.Windows.Forms.Label lblExperimentDuration;
        private System.Windows.Forms.NumericUpDown nudDataLoggingIntervalSeconds;
        private System.Windows.Forms.Label lblDataLoggingInterval;

        // 타임아웃 설정 컨트롤
        private System.Windows.Forms.NumericUpDown nudInitializationTimeout;
        private System.Windows.Forms.Label lblInitializationTimeout;
        private System.Windows.Forms.NumericUpDown nudValveOperationTimeout;
        private System.Windows.Forms.Label lblValveOperationTimeout;
        private System.Windows.Forms.NumericUpDown nudDryPumpStartTimeout;
        private System.Windows.Forms.Label lblDryPumpStartTimeout;
        private System.Windows.Forms.NumericUpDown nudTurboPumpStartTimeout;
        private System.Windows.Forms.Label lblTurboPumpStartTimeout;
        private System.Windows.Forms.NumericUpDown nudIonGaugeActivationTimeout;
        private System.Windows.Forms.Label lblIonGaugeActivationTimeout;
        private System.Windows.Forms.NumericUpDown nudHighVacuumTimeout;
        private System.Windows.Forms.Label lblHighVacuumTimeout;
        private System.Windows.Forms.NumericUpDown nudHeaterStartTimeout;
        private System.Windows.Forms.Label lblHeaterStartTimeout;
        private System.Windows.Forms.NumericUpDown nudShutdownTimeout;
        private System.Windows.Forms.Label lblShutdownTimeout;

        // 기타 설정 컨트롤
        private System.Windows.Forms.ComboBox cmbRunMode;
        private System.Windows.Forms.Label lblRunMode;
        private System.Windows.Forms.Label lblRunModeDesc;
        private System.Windows.Forms.NumericUpDown nudMaxRetryCount;
        private System.Windows.Forms.Label lblMaxRetryCount;
        private System.Windows.Forms.NumericUpDown nudRetryDelaySeconds;
        private System.Windows.Forms.Label lblRetryDelaySeconds;
        private System.Windows.Forms.CheckBox chkEnableDetailedLogging;
        private System.Windows.Forms.CheckBox chkEnableSafeShutdownOnFailure;
        private System.Windows.Forms.CheckBox chkEnableAlarmOnError;
    }
}