using System.Windows.Forms;
using VacX_OutSense.Forms.UserControls;

namespace VacX_OutSense
{
    partial class MainForm
    {
        /// <summary>
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 해제해야 하면 true, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 디자이너에서 생성한 코드

        /// <summary>
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            tableLayoutPanelMain = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            connectionIndicator_bathcirculator = new ConnectionIndicator();
            connectionIndicator_tempcontroller = new ConnectionIndicator();
            connectionIndicator_iomodule = new ConnectionIndicator();
            connectionIndicator_drypump = new ConnectionIndicator();
            connectionIndicator_turbopump = new ConnectionIndicator();
            tabControlMain = new TabControl();
            tabPage1 = new TabPage();
            tableLayoutPanel2 = new TableLayoutPanel();
            panel1 = new Panel();
            grpPressureValve = new GroupBox();
            txtATM = new BindableTextBox();
            txtPG = new BindableTextBox();
            txtIG = new BindableTextBox();
            txtIGStatus = new BindableTextBox();
            btn_iongauge = new Button();
            tableLayoutPanel5 = new TableLayoutPanel();
            btn_GV = new Button();
            btn_VV = new Button();
            btn_EV = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            grpCh1Timer = new GroupBox();
            lblVentTempUnit = new Label();
            numVentTargetTemp = new NumericUpDown();
            lblVentTargetTemp = new Label();
            label43 = new Label();
            numCh1Tolerance = new NumericUpDown();
            label44 = new Label();
            rdoHeaterOnly = new RadioButton();
            rdoFullShutdown = new RadioButton();
            lblEndAction = new Label();
            lblCh1TimeRemainingValue = new Label();
            lblCh1TimeRemaining = new Label();
            chkCh1TimerEnabled = new CheckBox();
            lblCh1Seconds = new Label();
            lblCh1Minutes = new Label();
            lblCh1Hours = new Label();
            numCh1Seconds = new NumericUpDown();
            numCh1Minutes = new NumericUpDown();
            numCh1Hours = new NumericUpDown();
            panel5 = new GroupBox();
            txtCh5PresentValue = new TextBox();
            txtCh4PresentValue = new TextBox();
            txtCh3PresentValue = new TextBox();
            txtCh2PresentValue = new TextBox();
            txtCh1PresentValue = new TextBox();
            txtCh6PresentValue = new TextBox();
            txtCh7PresentValue = new TextBox();
            txtCh8PresentValue = new TextBox();
            label41 = new Label();
            label40 = new Label();
            label29 = new Label();
            label30 = new Label();
            label42 = new Label();
            lblCh6Header = new Label();
            lblCh7Header = new Label();
            lblCh8Header = new Label();
            lblTempControlHeader = new Label();
            cmbTempChannel = new ComboBox();
            btnCh1Start = new Button();
            btnCh1AutoTuning = new Button();
            button2 = new Button();
            btnRampSetting = new Button();
            label37 = new Label();
            label33 = new Label();
            label35 = new Label();
            label38 = new Label();
            txtCh1Status = new TextBox();
            txtCh1SetValue = new TextBox();
            txtCh1HeatingMV = new TextBox();
            txtCh1IsAutotune = new TextBox();
            btnCh1Stop = new Button();
            grpChillerPID = new GroupBox();
            lblLastOutputValue = new Label();
            lblPIDStatusValue = new Label();
            lblPIDStatus = new Label();
            lblBathCirculatorWarning = new Label();
            label26 = new Label();
            txtBathCirculatorTargetTemp = new TextBox();
            txtBathCirculatorCurrentTemp = new TextBox();
            txtBathCirculatorMode = new TextBox();
            txtBathCirculatorTime = new TextBox();
            txtBathCirculatorStatus = new TextBox();
            label28 = new Label();
            label27 = new Label();
            label31 = new Label();
            label32 = new Label();
            label36 = new Label();
            numCh2Target = new NumericUpDown();
            lblCh2Target = new Label();
            cmbPIDChannel = new ComboBox();
            lblPIDChannel = new Label();
            chkChillerPIDEnabled = new CheckBox();
            btnBathCirculatorStart = new Button();
            lblChillerManualTemp = new Label();
            numChillerManualTemp = new NumericUpDown();
            btnBathCirculatorSetTemp = new Button();
            panel3 = new GroupBox();
            tableLayoutPanel6 = new TableLayoutPanel();
            txtTurboPumpReady = new TextBox();
            progressBarTurboPumpSpeed = new ProgressBar();
            txtTurboPumpNormal = new TextBox();
            label25 = new Label();
            label24 = new Label();
            txtTurboPumpRemote = new TextBox();
            label23 = new Label();
            label22 = new Label();
            label21 = new Label();
            label20 = new Label();
            label19 = new Label();
            label18 = new Label();
            label17 = new Label();
            label16 = new Label();
            label15 = new Label();
            label14 = new Label();
            txtTurboPumpRunTime = new TextBox();
            lblTurboPumpWarning = new Label();
            txtTurboPumpBearingTemp = new TextBox();
            txtTurboPumpElectronicsTemp = new TextBox();
            txtTurboPumpMotorTemp = new TextBox();
            txtTurboPumpCurrent = new TextBox();
            txtTurboPumpSpeed = new TextBox();
            btnTurboPumpStart = new Button();
            btnTurboPumpVent = new Button();
            btnTurboPumpReset = new Button();
            txtTurboPumpStatus = new TextBox();
            lblTurboPumpSpeedHeader = new Label();
            lblTurboPumpSpeedSet = new Label();
            numTurboPumpSpeed = new NumericUpDown();
            btnTurboPumpSpeedSet = new Button();
            btnTurboPumpSpeedPerm = new Button();
            lblTurboPumpStandby = new Label();
            numTurboPumpStandbySpeed = new NumericUpDown();
            btnTurboPumpStandbySet = new Button();
            panel2 = new GroupBox();
            tableLayoutPanel1 = new TableLayoutPanel();
            label13 = new Label();
            label12 = new Label();
            label11 = new Label();
            label10 = new Label();
            label9 = new Label();
            label8 = new Label();
            label7 = new Label();
            lblDryPumpWarning = new Label();
            txtDryPumpRunTime = new TextBox();
            txtDryPumpMotorTemp = new TextBox();
            txtDryPumpPower = new TextBox();
            txtDryPumpCurrent = new TextBox();
            txtDryPumpFrequency = new TextBox();
            btnDryPumpStart = new Button();
            btnDryPumpStandby = new Button();
            btnDryPumpNormal = new Button();
            txtDryPumpStatus = new TextBox();
            lblDryPumpService = new Label();
            label5 = new Label();
            lblDryPumpSpeedHeader = new Label();
            lblDryPumpSpeedSet = new Label();
            numDryPumpSpeed = new NumericUpDown();
            btnDryPumpSpeedSet = new Button();
            btnDryPumpSpeedStore = new Button();
            tabPage2 = new TabPage();
            txtLog = new RichTextBox();
            tabPageAutoRun = new TabPage();
            btnHoldSettings = new Button();
            btnBakeoutSettings = new Button();
            numCh1ReachCount = new NumericUpDown();
            lblCh1ReachCount = new Label();
            scientificPressureInput1 = new ScientificPressureInput();
            btnCh1AutoStart = new Button();
            lblCh1TargetPressure = new Label();
            chkCh1AutoStartEnabled = new CheckBox();
            lblLastOutput = new Label();
            lblSeconds = new Label();
            numUpdateInterval = new NumericUpDown();
            lblUpdateInterval = new Label();
            grpPIDParams = new GroupBox();
            numKd = new NumericUpDown();
            lblKd = new Label();
            numKi = new NumericUpDown();
            lblKi = new Label();
            numKp = new NumericUpDown();
            lblKp = new Label();
            numChillerBase = new NumericUpDown();
            lblChillerBase = new Label();
            panel4 = new Panel();
            tableLayoutPanel7 = new TableLayoutPanel();
            btnBathCirculatorSetTime = new Button();
            label34 = new Label();
            tabPageThermalRamp = new TabPage();
            simpleRampControl1 = new SimpleRampControl();
            menuStrip = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuFileExit = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuHelpPatchNotes = new ToolStripMenuItem();
            menuHelpAbout = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            toolStripStatusConnection = new ToolStripStatusLabel();
            toolStripStatusVersion = new ToolStripStatusLabel();
            gridViewMaster = new DataGridView();
            gridViewExpansion = new DataGridView();
            tableLayoutPanelMain.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPage1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            panel1.SuspendLayout();
            grpPressureValve.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            grpCh1Timer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numVentTargetTemp).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Tolerance).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Seconds).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Minutes).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Hours).BeginInit();
            panel5.SuspendLayout();
            grpChillerPID.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCh2Target).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numChillerManualTemp).BeginInit();
            panel3.SuspendLayout();
            tableLayoutPanel6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numTurboPumpSpeed).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numTurboPumpStandbySpeed).BeginInit();
            panel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numDryPumpSpeed).BeginInit();
            tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numCh1ReachCount).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numUpdateInterval).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numKd).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numKi).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numKp).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numChillerBase).BeginInit();
            tabPageThermalRamp.SuspendLayout();
            menuStrip.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)gridViewMaster).BeginInit();
            ((System.ComponentModel.ISupportInitialize)gridViewExpansion).BeginInit();
            SuspendLayout();
            // 
            // tableLayoutPanelMain
            // 
            tableLayoutPanelMain.ColumnCount = 1;
            tableLayoutPanelMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanelMain.Controls.Add(tabControlMain, 0, 1);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 24);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 2;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 49F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.Size = new Size(1171, 879);
            tableLayoutPanelMain.TabIndex = 0;
            // 
            // tableLayoutPanel3
            // 
            tableLayoutPanel3.ColumnCount = 2;
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tableLayoutPanel3.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            tableLayoutPanel3.Controls.Add(tableLayoutPanel4, 1, 0);
            tableLayoutPanel3.Dock = DockStyle.Fill;
            tableLayoutPanel3.Location = new Point(3, 3);
            tableLayoutPanel3.Name = "tableLayoutPanel3";
            tableLayoutPanel3.RowCount = 1;
            tableLayoutPanel3.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel3.Size = new Size(1165, 43);
            tableLayoutPanel3.TabIndex = 0;
            tableLayoutPanel3.Paint += tableLayoutPanel3_Paint;
            // 
            // tableLayoutPanel4
            // 
            tableLayoutPanel4.ColumnCount = 5;
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20.0000038F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.9999981F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.9999981F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20.0000038F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19.9999981F));
            tableLayoutPanel4.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel4.Controls.Add(connectionIndicator_bathcirculator, 4, 0);
            tableLayoutPanel4.Controls.Add(connectionIndicator_tempcontroller, 3, 0);
            tableLayoutPanel4.Controls.Add(connectionIndicator_iomodule, 2, 0);
            tableLayoutPanel4.Controls.Add(connectionIndicator_drypump, 1, 0);
            tableLayoutPanel4.Controls.Add(connectionIndicator_turbopump, 0, 0);
            tableLayoutPanel4.Dock = DockStyle.Fill;
            tableLayoutPanel4.Location = new Point(702, 3);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(460, 37);
            tableLayoutPanel4.TabIndex = 1;
            // 
            // connectionIndicator_bathcirculator
            // 
            connectionIndicator_bathcirculator.ComponentName = "Chiller";
            connectionIndicator_bathcirculator.ConnectedColor = Color.LimeGreen;
            connectionIndicator_bathcirculator.DataMember = null;
            connectionIndicator_bathcirculator.DataSource = null;
            connectionIndicator_bathcirculator.DisconnectedColor = Color.Red;
            connectionIndicator_bathcirculator.IsConnected = false;
            connectionIndicator_bathcirculator.Location = new Point(369, 4);
            connectionIndicator_bathcirculator.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_bathcirculator.Name = "connectionIndicator_bathcirculator";
            connectionIndicator_bathcirculator.Size = new Size(70, 29);
            connectionIndicator_bathcirculator.TabIndex = 5;
            // 
            // connectionIndicator_tempcontroller
            // 
            connectionIndicator_tempcontroller.ComponentName = "PIDCon";
            connectionIndicator_tempcontroller.ConnectedColor = Color.LimeGreen;
            connectionIndicator_tempcontroller.DataMember = null;
            connectionIndicator_tempcontroller.DataSource = null;
            connectionIndicator_tempcontroller.DisconnectedColor = Color.Red;
            connectionIndicator_tempcontroller.IsConnected = false;
            connectionIndicator_tempcontroller.Location = new Point(277, 4);
            connectionIndicator_tempcontroller.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_tempcontroller.Name = "connectionIndicator_tempcontroller";
            connectionIndicator_tempcontroller.Size = new Size(70, 29);
            connectionIndicator_tempcontroller.TabIndex = 4;
            // 
            // connectionIndicator_iomodule
            // 
            connectionIndicator_iomodule.ComponentName = "IOModule";
            connectionIndicator_iomodule.ConnectedColor = Color.LimeGreen;
            connectionIndicator_iomodule.DataMember = null;
            connectionIndicator_iomodule.DataSource = null;
            connectionIndicator_iomodule.DisconnectedColor = Color.Red;
            connectionIndicator_iomodule.IsConnected = false;
            connectionIndicator_iomodule.Location = new Point(186, 4);
            connectionIndicator_iomodule.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_iomodule.Name = "connectionIndicator_iomodule";
            connectionIndicator_iomodule.Size = new Size(70, 29);
            connectionIndicator_iomodule.TabIndex = 2;
            // 
            // connectionIndicator_drypump
            // 
            connectionIndicator_drypump.ComponentName = "DryPump";
            connectionIndicator_drypump.ConnectedColor = Color.LimeGreen;
            connectionIndicator_drypump.DataMember = null;
            connectionIndicator_drypump.DataSource = null;
            connectionIndicator_drypump.DisconnectedColor = Color.Red;
            connectionIndicator_drypump.IsConnected = false;
            connectionIndicator_drypump.Location = new Point(95, 4);
            connectionIndicator_drypump.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_drypump.Name = "connectionIndicator_drypump";
            connectionIndicator_drypump.Size = new Size(70, 29);
            connectionIndicator_drypump.TabIndex = 1;
            // 
            // connectionIndicator_turbopump
            // 
            connectionIndicator_turbopump.ComponentName = "TurboPump";
            connectionIndicator_turbopump.ConnectedColor = Color.LimeGreen;
            connectionIndicator_turbopump.DataMember = null;
            connectionIndicator_turbopump.DataSource = null;
            connectionIndicator_turbopump.DisconnectedColor = Color.Red;
            connectionIndicator_turbopump.IsConnected = false;
            connectionIndicator_turbopump.Location = new Point(3, 4);
            connectionIndicator_turbopump.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_turbopump.Name = "connectionIndicator_turbopump";
            connectionIndicator_turbopump.Size = new Size(70, 29);
            connectionIndicator_turbopump.TabIndex = 0;
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabPage1);
            tabControlMain.Controls.Add(tabPage2);
            tabControlMain.Controls.Add(tabPageAutoRun);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Location = new Point(3, 52);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1165, 824);
            tabControlMain.TabIndex = 1;
            tabControlMain.SelectedIndexChanged += tabControlMain_SelectedIndexChanged;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(tableLayoutPanel2);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1157, 796);
            tabPage1.TabIndex = 2;
            tabPage1.Text = "Main";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Controls.Add(panel1, 0, 0);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 1;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.Size = new Size(1151, 790);
            tableLayoutPanel2.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(grpPressureValve);
            panel1.Controls.Add(grpCh1Timer);
            panel1.Controls.Add(panel5);
            panel1.Controls.Add(grpChillerPID);
            panel1.Controls.Add(panel3);
            panel1.Controls.Add(panel2);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 3);
            panel1.Name = "panel1";
            panel1.Size = new Size(1145, 784);
            panel1.TabIndex = 1;
            // 
            // grpPressureValve
            // 
            grpPressureValve.Controls.Add(txtATM);
            grpPressureValve.Controls.Add(txtPG);
            grpPressureValve.Controls.Add(txtIG);
            grpPressureValve.Controls.Add(txtIGStatus);
            grpPressureValve.Controls.Add(btn_iongauge);
            grpPressureValve.Controls.Add(tableLayoutPanel5);
            grpPressureValve.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            grpPressureValve.Location = new Point(3, 3);
            grpPressureValve.Name = "grpPressureValve";
            grpPressureValve.Size = new Size(1138, 115);
            grpPressureValve.TabIndex = 54;
            grpPressureValve.TabStop = false;
            grpPressureValve.Text = "압력 / 밸브";
            // 
            // txtATM
            // 
            txtATM.BackColor = Color.Silver;
            txtATM.DataMember = null;
            txtATM.DataSource = null;
            txtATM.FormatString = null;
            txtATM.IsReadOnly = true;
            txtATM.LabelText = "ATM(kPa)";
            txtATM.Location = new Point(5, 20);
            txtATM.Name = "txtATM";
            txtATM.NoFocus = true;
            txtATM.Padding = new Padding(0, 0, 0, 3);
            txtATM.Size = new Size(250, 29);
            txtATM.TabIndex = 16;
            txtATM.TextAlignment = HorizontalAlignment.Center;
            txtATM.TextValue = "";
            // 
            // txtPG
            // 
            txtPG.BackColor = Color.Silver;
            txtPG.DataMember = null;
            txtPG.DataSource = null;
            txtPG.FormatString = null;
            txtPG.IsReadOnly = true;
            txtPG.LabelText = "PG(Torr)";
            txtPG.Location = new Point(260, 20);
            txtPG.Name = "txtPG";
            txtPG.NoFocus = true;
            txtPG.Padding = new Padding(0, 0, 0, 3);
            txtPG.Size = new Size(250, 29);
            txtPG.TabIndex = 17;
            txtPG.TextAlignment = HorizontalAlignment.Center;
            txtPG.TextValue = "";
            // 
            // txtIG
            // 
            txtIG.BackColor = Color.Silver;
            txtIG.DataMember = null;
            txtIG.DataSource = null;
            txtIG.FormatString = null;
            txtIG.IsReadOnly = true;
            txtIG.LabelText = "IG(Torr)";
            txtIG.Location = new Point(515, 20);
            txtIG.Name = "txtIG";
            txtIG.NoFocus = true;
            txtIG.Padding = new Padding(0, 0, 0, 3);
            txtIG.Size = new Size(250, 29);
            txtIG.TabIndex = 18;
            txtIG.TextAlignment = HorizontalAlignment.Center;
            txtIG.TextValue = "";
            // 
            // txtIGStatus
            // 
            txtIGStatus.DataMember = null;
            txtIGStatus.DataSource = null;
            txtIGStatus.FormatString = null;
            txtIGStatus.IsReadOnly = true;
            txtIGStatus.LabelText = "IG status";
            txtIGStatus.Location = new Point(850, 20);
            txtIGStatus.Name = "txtIGStatus";
            txtIGStatus.NoFocus = true;
            txtIGStatus.Padding = new Padding(0, 0, 0, 3);
            txtIGStatus.Size = new Size(250, 30);
            txtIGStatus.TabIndex = 19;
            txtIGStatus.TextAlignment = HorizontalAlignment.Left;
            txtIGStatus.TextValue = "";
            txtIGStatus.Visible = false;
            // 
            // btn_iongauge
            // 
            btn_iongauge.Location = new Point(770, 21);
            btn_iongauge.Name = "btn_iongauge";
            btn_iongauge.Size = new Size(75, 28);
            btn_iongauge.TabIndex = 7;
            btn_iongauge.Text = "-";
            btn_iongauge.UseVisualStyleBackColor = true;
            btn_iongauge.Click += btn_iongauge_Click;
            // 
            // tableLayoutPanel5
            // 
            tableLayoutPanel5.ColumnCount = 3;
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333321F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel5.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333359F));
            tableLayoutPanel5.Controls.Add(btn_GV, 0, 1);
            tableLayoutPanel5.Controls.Add(btn_VV, 1, 1);
            tableLayoutPanel5.Controls.Add(btn_EV, 2, 1);
            tableLayoutPanel5.Controls.Add(label1, 0, 0);
            tableLayoutPanel5.Controls.Add(label2, 1, 0);
            tableLayoutPanel5.Controls.Add(label3, 2, 0);
            tableLayoutPanel5.Location = new Point(5, 55);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 2;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
            tableLayoutPanel5.Size = new Size(294, 55);
            tableLayoutPanel5.TabIndex = 8;
            // 
            // btn_GV
            // 
            btn_GV.Dock = DockStyle.Fill;
            btn_GV.Location = new Point(3, 23);
            btn_GV.Name = "btn_GV";
            btn_GV.Size = new Size(91, 29);
            btn_GV.TabIndex = 0;
            btn_GV.Text = "-";
            btn_GV.UseVisualStyleBackColor = true;
            btn_GV.Click += btn_GV_Click;
            // 
            // btn_VV
            // 
            btn_VV.Dock = DockStyle.Fill;
            btn_VV.Location = new Point(100, 23);
            btn_VV.Name = "btn_VV";
            btn_VV.Size = new Size(92, 29);
            btn_VV.TabIndex = 1;
            btn_VV.Text = "-";
            btn_VV.UseVisualStyleBackColor = true;
            btn_VV.Click += btn_VV_Click;
            // 
            // btn_EV
            // 
            btn_EV.Dock = DockStyle.Fill;
            btn_EV.Location = new Point(198, 23);
            btn_EV.Name = "btn_EV";
            btn_EV.Size = new Size(93, 29);
            btn_EV.TabIndex = 2;
            btn_EV.Text = "-";
            btn_EV.UseVisualStyleBackColor = true;
            btn_EV.Click += btn_EV_Click;
            // 
            // label1
            // 
            label1.BackColor = SystemColors.Info;
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Dock = DockStyle.Fill;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(91, 20);
            label1.TabIndex = 3;
            label1.Text = "GateValve";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.BackColor = SystemColors.Info;
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Dock = DockStyle.Fill;
            label2.Location = new Point(100, 0);
            label2.Name = "label2";
            label2.Size = new Size(92, 20);
            label2.TabIndex = 4;
            label2.Text = "VentValve";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.BackColor = SystemColors.Info;
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Dock = DockStyle.Fill;
            label3.Location = new Point(198, 0);
            label3.Name = "label3";
            label3.Size = new Size(93, 20);
            label3.TabIndex = 5;
            label3.Text = "ExhaustValve";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // grpCh1Timer
            // 
            grpCh1Timer.Controls.Add(lblVentTempUnit);
            grpCh1Timer.Controls.Add(numVentTargetTemp);
            grpCh1Timer.Controls.Add(lblVentTargetTemp);
            grpCh1Timer.Controls.Add(label43);
            grpCh1Timer.Controls.Add(numCh1Tolerance);
            grpCh1Timer.Controls.Add(label44);
            grpCh1Timer.Controls.Add(rdoHeaterOnly);
            grpCh1Timer.Controls.Add(rdoFullShutdown);
            grpCh1Timer.Controls.Add(lblEndAction);
            grpCh1Timer.Controls.Add(lblCh1TimeRemainingValue);
            grpCh1Timer.Controls.Add(lblCh1TimeRemaining);
            grpCh1Timer.Controls.Add(chkCh1TimerEnabled);
            grpCh1Timer.Controls.Add(lblCh1Seconds);
            grpCh1Timer.Controls.Add(lblCh1Minutes);
            grpCh1Timer.Controls.Add(lblCh1Hours);
            grpCh1Timer.Controls.Add(numCh1Seconds);
            grpCh1Timer.Controls.Add(numCh1Minutes);
            grpCh1Timer.Controls.Add(numCh1Hours);
            grpCh1Timer.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            grpCh1Timer.Location = new Point(550, 128);
            grpCh1Timer.Name = "grpCh1Timer";
            grpCh1Timer.Size = new Size(465, 145);
            grpCh1Timer.TabIndex = 50;
            grpCh1Timer.TabStop = false;
            grpCh1Timer.Text = "CH1 타이머";
            // 
            // lblVentTempUnit
            // 
            lblVentTempUnit.AutoSize = true;
            lblVentTempUnit.Location = new Point(391, 105);
            lblVentTempUnit.Name = "lblVentTempUnit";
            lblVentTempUnit.Size = new Size(20, 15);
            lblVentTempUnit.TabIndex = 11;
            lblVentTempUnit.Text = "°C";
            // 
            // numVentTargetTemp
            // 
            numVentTargetTemp.DecimalPlaces = 1;
            numVentTargetTemp.Location = new Point(315, 103);
            numVentTargetTemp.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numVentTargetTemp.Name = "numVentTargetTemp";
            numVentTargetTemp.Size = new Size(70, 23);
            numVentTargetTemp.TabIndex = 10;
            numVentTargetTemp.TextAlign = HorizontalAlignment.Center;
            numVentTargetTemp.Value = new decimal(new int[] { 50, 0, 0, 0 });
            // 
            // lblVentTargetTemp
            // 
            lblVentTargetTemp.AutoSize = true;
            lblVentTargetTemp.Location = new Point(220, 105);
            lblVentTargetTemp.Name = "lblVentTargetTemp";
            lblVentTargetTemp.Size = new Size(90, 15);
            lblVentTargetTemp.TabIndex = 9;
            lblVentTargetTemp.Text = "벤트 타겟 온도:";
            // 
            // label43
            // 
            label43.AutoSize = true;
            label43.Location = new Point(175, 105);
            label43.Name = "label43";
            label43.Size = new Size(20, 15);
            label43.TabIndex = 23;
            label43.Text = "°C";
            // 
            // numCh1Tolerance
            // 
            numCh1Tolerance.DecimalPlaces = 1;
            numCh1Tolerance.Location = new Point(110, 103);
            numCh1Tolerance.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numCh1Tolerance.Name = "numCh1Tolerance";
            numCh1Tolerance.Size = new Size(60, 23);
            numCh1Tolerance.TabIndex = 22;
            numCh1Tolerance.TextAlign = HorizontalAlignment.Center;
            numCh1Tolerance.ValueChanged += numCh1Tolerance_ValueChanged;
            // 
            // label44
            // 
            label44.AutoSize = true;
            label44.Location = new Point(15, 105);
            label44.Name = "label44";
            label44.Size = new Size(87, 15);
            label44.TabIndex = 21;
            label44.Text = "허용 오차 온도";
            // 
            // rdoHeaterOnly
            // 
            rdoHeaterOnly.AutoSize = true;
            rdoHeaterOnly.Location = new Point(175, 75);
            rdoHeaterOnly.Name = "rdoHeaterOnly";
            rdoHeaterOnly.Size = new Size(89, 19);
            rdoHeaterOnly.TabIndex = 24;
            rdoHeaterOnly.Text = "히터만 정지";
            rdoHeaterOnly.UseVisualStyleBackColor = true;
            // 
            // rdoFullShutdown
            // 
            rdoFullShutdown.AutoSize = true;
            rdoFullShutdown.Checked = true;
            rdoFullShutdown.Location = new Point(85, 75);
            rdoFullShutdown.Name = "rdoFullShutdown";
            rdoFullShutdown.Size = new Size(77, 19);
            rdoFullShutdown.TabIndex = 25;
            rdoFullShutdown.TabStop = true;
            rdoFullShutdown.Text = "전체 종료";
            rdoFullShutdown.UseVisualStyleBackColor = true;
            // 
            // lblEndAction
            // 
            lblEndAction.AutoSize = true;
            lblEndAction.Location = new Point(15, 77);
            lblEndAction.Name = "lblEndAction";
            lblEndAction.Size = new Size(62, 15);
            lblEndAction.TabIndex = 26;
            lblEndAction.Text = "종료 동작:";
            // 
            // lblCh1TimeRemainingValue
            // 
            lblCh1TimeRemainingValue.AutoSize = true;
            lblCh1TimeRemainingValue.Font = new Font("굴림", 9F, FontStyle.Bold);
            lblCh1TimeRemainingValue.ForeColor = Color.Blue;
            lblCh1TimeRemainingValue.Location = new Point(190, 22);
            lblCh1TimeRemainingValue.Name = "lblCh1TimeRemainingValue";
            lblCh1TimeRemainingValue.Size = new Size(57, 12);
            lblCh1TimeRemainingValue.TabIndex = 8;
            lblCh1TimeRemainingValue.Text = "00:00:00";
            // 
            // lblCh1TimeRemaining
            // 
            lblCh1TimeRemaining.AutoSize = true;
            lblCh1TimeRemaining.Location = new Point(120, 22);
            lblCh1TimeRemaining.Name = "lblCh1TimeRemaining";
            lblCh1TimeRemaining.Size = new Size(62, 15);
            lblCh1TimeRemaining.TabIndex = 7;
            lblCh1TimeRemaining.Text = "남은 시간:";
            // 
            // chkCh1TimerEnabled
            // 
            chkCh1TimerEnabled.AutoSize = true;
            chkCh1TimerEnabled.Location = new Point(15, 22);
            chkCh1TimerEnabled.Name = "chkCh1TimerEnabled";
            chkCh1TimerEnabled.Size = new Size(90, 19);
            chkCh1TimerEnabled.TabIndex = 0;
            chkCh1TimerEnabled.Text = "타이머 사용";
            chkCh1TimerEnabled.UseVisualStyleBackColor = true;
            chkCh1TimerEnabled.CheckedChanged += chkCh1TimerEnabled_CheckedChanged;
            // 
            // lblCh1Seconds
            // 
            lblCh1Seconds.AutoSize = true;
            lblCh1Seconds.Location = new Point(240, 49);
            lblCh1Seconds.Name = "lblCh1Seconds";
            lblCh1Seconds.Size = new Size(19, 15);
            lblCh1Seconds.TabIndex = 6;
            lblCh1Seconds.Text = "초";
            // 
            // lblCh1Minutes
            // 
            lblCh1Minutes.AutoSize = true;
            lblCh1Minutes.Location = new Point(160, 49);
            lblCh1Minutes.Name = "lblCh1Minutes";
            lblCh1Minutes.Size = new Size(19, 15);
            lblCh1Minutes.TabIndex = 4;
            lblCh1Minutes.Text = "분";
            // 
            // lblCh1Hours
            // 
            lblCh1Hours.AutoSize = true;
            lblCh1Hours.Location = new Point(70, 49);
            lblCh1Hours.Name = "lblCh1Hours";
            lblCh1Hours.Size = new Size(31, 15);
            lblCh1Hours.TabIndex = 2;
            lblCh1Hours.Text = "시간";
            // 
            // numCh1Seconds
            // 
            numCh1Seconds.Location = new Point(185, 45);
            numCh1Seconds.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numCh1Seconds.Name = "numCh1Seconds";
            numCh1Seconds.Size = new Size(50, 23);
            numCh1Seconds.TabIndex = 5;
            numCh1Seconds.TextAlign = HorizontalAlignment.Center;
            // 
            // numCh1Minutes
            // 
            numCh1Minutes.Location = new Point(105, 45);
            numCh1Minutes.Maximum = new decimal(new int[] { 59, 0, 0, 0 });
            numCh1Minutes.Name = "numCh1Minutes";
            numCh1Minutes.Size = new Size(50, 23);
            numCh1Minutes.TabIndex = 3;
            numCh1Minutes.TextAlign = HorizontalAlignment.Center;
            numCh1Minutes.Value = new decimal(new int[] { 30, 0, 0, 0 });
            // 
            // numCh1Hours
            // 
            numCh1Hours.Location = new Point(15, 45);
            numCh1Hours.Maximum = new decimal(new int[] { 99, 0, 0, 0 });
            numCh1Hours.Name = "numCh1Hours";
            numCh1Hours.Size = new Size(50, 23);
            numCh1Hours.TabIndex = 1;
            numCh1Hours.TextAlign = HorizontalAlignment.Center;
            // 
            // panel5
            // 
            panel5.Controls.Add(txtCh5PresentValue);
            panel5.Controls.Add(txtCh4PresentValue);
            panel5.Controls.Add(txtCh3PresentValue);
            panel5.Controls.Add(txtCh2PresentValue);
            panel5.Controls.Add(txtCh1PresentValue);
            panel5.Controls.Add(txtCh6PresentValue);
            panel5.Controls.Add(txtCh7PresentValue);
            panel5.Controls.Add(txtCh8PresentValue);
            panel5.Controls.Add(label41);
            panel5.Controls.Add(label40);
            panel5.Controls.Add(label29);
            panel5.Controls.Add(label30);
            panel5.Controls.Add(label42);
            panel5.Controls.Add(lblCh6Header);
            panel5.Controls.Add(lblCh7Header);
            panel5.Controls.Add(lblCh8Header);
            panel5.Controls.Add(lblTempControlHeader);
            panel5.Controls.Add(cmbTempChannel);
            panel5.Controls.Add(btnCh1Start);
            panel5.Controls.Add(btnCh1AutoTuning);
            panel5.Controls.Add(button2);
            panel5.Controls.Add(btnRampSetting);
            panel5.Controls.Add(label37);
            panel5.Controls.Add(label33);
            panel5.Controls.Add(label35);
            panel5.Controls.Add(label38);
            panel5.Controls.Add(txtCh1Status);
            panel5.Controls.Add(txtCh1SetValue);
            panel5.Controls.Add(txtCh1HeatingMV);
            panel5.Controls.Add(txtCh1IsAutotune);
            panel5.Controls.Add(btnCh1Stop);
            panel5.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            panel5.Location = new Point(550, 280);
            panel5.Name = "panel5";
            panel5.Size = new Size(464, 266);
            panel5.TabIndex = 15;
            panel5.TabStop = false;
            panel5.Text = "온도 컨트롤러";
            // 
            // txtCh5PresentValue
            // 
            txtCh5PresentValue.Location = new Point(3, 87);
            txtCh5PresentValue.Name = "txtCh5PresentValue";
            txtCh5PresentValue.ReadOnly = true;
            txtCh5PresentValue.Size = new Size(110, 23);
            txtCh5PresentValue.TabIndex = 46;
            txtCh5PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh4PresentValue
            // 
            txtCh4PresentValue.Location = new Point(333, 38);
            txtCh4PresentValue.Name = "txtCh4PresentValue";
            txtCh4PresentValue.ReadOnly = true;
            txtCh4PresentValue.Size = new Size(110, 23);
            txtCh4PresentValue.TabIndex = 45;
            txtCh4PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh3PresentValue
            // 
            txtCh3PresentValue.Location = new Point(223, 38);
            txtCh3PresentValue.Name = "txtCh3PresentValue";
            txtCh3PresentValue.ReadOnly = true;
            txtCh3PresentValue.Size = new Size(110, 23);
            txtCh3PresentValue.TabIndex = 44;
            txtCh3PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh2PresentValue
            // 
            txtCh2PresentValue.Location = new Point(113, 38);
            txtCh2PresentValue.Name = "txtCh2PresentValue";
            txtCh2PresentValue.ReadOnly = true;
            txtCh2PresentValue.Size = new Size(110, 23);
            txtCh2PresentValue.TabIndex = 34;
            txtCh2PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1PresentValue
            // 
            txtCh1PresentValue.Location = new Point(3, 38);
            txtCh1PresentValue.Name = "txtCh1PresentValue";
            txtCh1PresentValue.ReadOnly = true;
            txtCh1PresentValue.Size = new Size(110, 23);
            txtCh1PresentValue.TabIndex = 32;
            txtCh1PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh6PresentValue
            // 
            txtCh6PresentValue.Location = new Point(113, 87);
            txtCh6PresentValue.Name = "txtCh6PresentValue";
            txtCh6PresentValue.ReadOnly = true;
            txtCh6PresentValue.Size = new Size(110, 23);
            txtCh6PresentValue.TabIndex = 47;
            txtCh6PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh7PresentValue
            // 
            txtCh7PresentValue.Location = new Point(223, 87);
            txtCh7PresentValue.Name = "txtCh7PresentValue";
            txtCh7PresentValue.ReadOnly = true;
            txtCh7PresentValue.Size = new Size(110, 23);
            txtCh7PresentValue.TabIndex = 48;
            txtCh7PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh8PresentValue
            // 
            txtCh8PresentValue.Location = new Point(333, 87);
            txtCh8PresentValue.Name = "txtCh8PresentValue";
            txtCh8PresentValue.ReadOnly = true;
            txtCh8PresentValue.Size = new Size(110, 23);
            txtCh8PresentValue.TabIndex = 49;
            txtCh8PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // label41
            // 
            label41.BackColor = Color.FromArgb(255, 220, 220);
            label41.BorderStyle = BorderStyle.FixedSingle;
            label41.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            label41.Location = new Point(3, 18);
            label41.Name = "label41";
            label41.Size = new Size(110, 20);
            label41.TabIndex = 27;
            label41.Text = "Ch1(Heater)";
            label41.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label40
            // 
            label40.BackColor = SystemColors.Info;
            label40.BorderStyle = BorderStyle.FixedSingle;
            label40.Location = new Point(113, 18);
            label40.Name = "label40";
            label40.Size = new Size(110, 20);
            label40.TabIndex = 30;
            label40.Text = "Ch2";
            label40.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label29
            // 
            label29.BackColor = SystemColors.Info;
            label29.BorderStyle = BorderStyle.FixedSingle;
            label29.Location = new Point(223, 18);
            label29.Name = "label29";
            label29.Size = new Size(110, 20);
            label29.TabIndex = 41;
            label29.Text = "Ch3";
            label29.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label30
            // 
            label30.BackColor = SystemColors.Info;
            label30.BorderStyle = BorderStyle.FixedSingle;
            label30.Location = new Point(333, 18);
            label30.Name = "label30";
            label30.Size = new Size(110, 20);
            label30.TabIndex = 42;
            label30.Text = "Ch4";
            label30.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label42
            // 
            label42.BackColor = SystemColors.Info;
            label42.BorderStyle = BorderStyle.FixedSingle;
            label42.Location = new Point(3, 67);
            label42.Name = "label42";
            label42.Size = new Size(110, 20);
            label42.TabIndex = 43;
            label42.Text = "Ch5";
            label42.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCh6Header
            // 
            lblCh6Header.BackColor = SystemColors.Info;
            lblCh6Header.BorderStyle = BorderStyle.FixedSingle;
            lblCh6Header.Location = new Point(113, 67);
            lblCh6Header.Name = "lblCh6Header";
            lblCh6Header.Size = new Size(110, 20);
            lblCh6Header.TabIndex = 50;
            lblCh6Header.Text = "Ch6";
            lblCh6Header.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCh7Header
            // 
            lblCh7Header.BackColor = SystemColors.Info;
            lblCh7Header.BorderStyle = BorderStyle.FixedSingle;
            lblCh7Header.Location = new Point(223, 67);
            lblCh7Header.Name = "lblCh7Header";
            lblCh7Header.Size = new Size(110, 20);
            lblCh7Header.TabIndex = 51;
            lblCh7Header.Text = "Ch7";
            lblCh7Header.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblCh8Header
            // 
            lblCh8Header.BackColor = SystemColors.Info;
            lblCh8Header.BorderStyle = BorderStyle.FixedSingle;
            lblCh8Header.Location = new Point(333, 67);
            lblCh8Header.Name = "lblCh8Header";
            lblCh8Header.Size = new Size(110, 20);
            lblCh8Header.TabIndex = 52;
            lblCh8Header.Text = "Ch8";
            lblCh8Header.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblTempControlHeader
            // 
            lblTempControlHeader.BackColor = SystemColors.Info;
            lblTempControlHeader.BorderStyle = BorderStyle.FixedSingle;
            lblTempControlHeader.Location = new Point(3, 165);
            lblTempControlHeader.Name = "lblTempControlHeader";
            lblTempControlHeader.Padding = new Padding(5, 0, 0, 0);
            lblTempControlHeader.Size = new Size(455, 20);
            lblTempControlHeader.TabIndex = 50;
            lblTempControlHeader.Text = "채널 제어";
            lblTempControlHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // cmbTempChannel
            // 
            cmbTempChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTempChannel.Items.AddRange(new object[] { "Ch1", "Ch2", "Ch3", "Ch4", "Ch5", "Ch6", "Ch7", "Ch8", "Ch9", "Ch10", "Ch11", "Ch12" });
            cmbTempChannel.Location = new Point(5, 189);
            cmbTempChannel.Name = "cmbTempChannel";
            cmbTempChannel.Size = new Size(75, 23);
            cmbTempChannel.TabIndex = 51;
            cmbTempChannel.SelectedIndexChanged += cmbTempChannel_SelectedIndexChanged;
            // 
            // btnCh1Start
            // 
            btnCh1Start.Location = new Point(85, 188);
            btnCh1Start.Name = "btnCh1Start";
            btnCh1Start.Size = new Size(65, 25);
            btnCh1Start.TabIndex = 0;
            btnCh1Start.Text = "Start";
            btnCh1Start.UseVisualStyleBackColor = true;
            btnCh1Start.Click += btnCh1Start_Click;
            // 
            // btnCh1AutoTuning
            // 
            btnCh1AutoTuning.Location = new Point(165, 188);
            btnCh1AutoTuning.Name = "btnCh1AutoTuning";
            btnCh1AutoTuning.Size = new Size(80, 25);
            btnCh1AutoTuning.TabIndex = 36;
            btnCh1AutoTuning.Text = "AutoTune";
            btnCh1AutoTuning.UseVisualStyleBackColor = true;
            btnCh1AutoTuning.Click += btnCh1AutoTuning_Click;
            // 
            // button2
            // 
            button2.Location = new Point(250, 188);
            button2.Name = "button2";
            button2.Size = new Size(75, 25);
            button2.TabIndex = 37;
            button2.Text = "SetTemp";
            button2.UseVisualStyleBackColor = true;
            button2.Click += btnCh1SetTemp_Click;
            // 
            // btnRampSetting
            // 
            btnRampSetting.Location = new Point(330, 188);
            btnRampSetting.Name = "btnRampSetting";
            btnRampSetting.Size = new Size(65, 25);
            btnRampSetting.TabIndex = 52;
            btnRampSetting.Text = "Ramp";
            btnRampSetting.UseVisualStyleBackColor = true;
            btnRampSetting.Click += btnRampSetting_Click;
            // 
            // label37
            // 
            label37.BackColor = SystemColors.Info;
            label37.BorderStyle = BorderStyle.FixedSingle;
            label37.Location = new Point(3, 217);
            label37.Name = "label37";
            label37.Size = new Size(110, 20);
            label37.TabIndex = 14;
            label37.Text = "상태";
            label37.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label33
            // 
            label33.BackColor = SystemColors.Info;
            label33.BorderStyle = BorderStyle.FixedSingle;
            label33.Location = new Point(115, 217);
            label33.Name = "label33";
            label33.Size = new Size(110, 20);
            label33.TabIndex = 20;
            label33.Text = "SV";
            label33.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label35
            // 
            label35.BackColor = SystemColors.Info;
            label35.BorderStyle = BorderStyle.FixedSingle;
            label35.Location = new Point(227, 217);
            label35.Name = "label35";
            label35.Size = new Size(110, 20);
            label35.TabIndex = 15;
            label35.Text = "가열량";
            label35.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label38
            // 
            label38.BackColor = SystemColors.Info;
            label38.BorderStyle = BorderStyle.FixedSingle;
            label38.Location = new Point(339, 217);
            label38.Name = "label38";
            label38.Size = new Size(119, 20);
            label38.TabIndex = 29;
            label38.Text = "AutoTune";
            label38.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtCh1Status
            // 
            txtCh1Status.Location = new Point(3, 237);
            txtCh1Status.Name = "txtCh1Status";
            txtCh1Status.ReadOnly = true;
            txtCh1Status.Size = new Size(110, 23);
            txtCh1Status.TabIndex = 6;
            txtCh1Status.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1SetValue
            // 
            txtCh1SetValue.Location = new Point(115, 237);
            txtCh1SetValue.Name = "txtCh1SetValue";
            txtCh1SetValue.ReadOnly = true;
            txtCh1SetValue.Size = new Size(110, 23);
            txtCh1SetValue.TabIndex = 33;
            txtCh1SetValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1HeatingMV
            // 
            txtCh1HeatingMV.Location = new Point(227, 237);
            txtCh1HeatingMV.Name = "txtCh1HeatingMV";
            txtCh1HeatingMV.ReadOnly = true;
            txtCh1HeatingMV.Size = new Size(110, 23);
            txtCh1HeatingMV.TabIndex = 7;
            txtCh1HeatingMV.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1IsAutotune
            // 
            txtCh1IsAutotune.Location = new Point(339, 237);
            txtCh1IsAutotune.Name = "txtCh1IsAutotune";
            txtCh1IsAutotune.ReadOnly = true;
            txtCh1IsAutotune.Size = new Size(119, 23);
            txtCh1IsAutotune.TabIndex = 40;
            txtCh1IsAutotune.TextAlign = HorizontalAlignment.Center;
            // 
            // btnCh1Stop
            // 
            btnCh1Stop.Location = new Point(0, 0);
            btnCh1Stop.Name = "btnCh1Stop";
            btnCh1Stop.Size = new Size(0, 0);
            btnCh1Stop.TabIndex = 25;
            btnCh1Stop.Text = "Stop";
            btnCh1Stop.Visible = false;
            btnCh1Stop.Click += btnCh1Stop_Click;
            // 
            // grpChillerPID
            // 
            grpChillerPID.Controls.Add(lblLastOutputValue);
            grpChillerPID.Controls.Add(lblPIDStatusValue);
            grpChillerPID.Controls.Add(lblPIDStatus);
            grpChillerPID.Controls.Add(lblBathCirculatorWarning);
            grpChillerPID.Controls.Add(label26);
            grpChillerPID.Controls.Add(txtBathCirculatorTargetTemp);
            grpChillerPID.Controls.Add(txtBathCirculatorCurrentTemp);
            grpChillerPID.Controls.Add(txtBathCirculatorMode);
            grpChillerPID.Controls.Add(txtBathCirculatorTime);
            grpChillerPID.Controls.Add(txtBathCirculatorStatus);
            grpChillerPID.Controls.Add(label28);
            grpChillerPID.Controls.Add(label27);
            grpChillerPID.Controls.Add(label31);
            grpChillerPID.Controls.Add(label32);
            grpChillerPID.Controls.Add(label36);
            grpChillerPID.Controls.Add(numCh2Target);
            grpChillerPID.Controls.Add(lblCh2Target);
            grpChillerPID.Controls.Add(cmbPIDChannel);
            grpChillerPID.Controls.Add(lblPIDChannel);
            grpChillerPID.Controls.Add(chkChillerPIDEnabled);
            grpChillerPID.Controls.Add(btnBathCirculatorStart);
            grpChillerPID.Controls.Add(lblChillerManualTemp);
            grpChillerPID.Controls.Add(numChillerManualTemp);
            grpChillerPID.Controls.Add(btnBathCirculatorSetTemp);
            grpChillerPID.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            grpChillerPID.Location = new Point(3, 565);
            grpChillerPID.Name = "grpChillerPID";
            grpChillerPID.Size = new Size(537, 179);
            grpChillerPID.TabIndex = 53;
            grpChillerPID.TabStop = false;
            grpChillerPID.Text = "칠러 제어";
            // 
            // lblLastOutputValue
            // 
            lblLastOutputValue.AutoSize = true;
            lblLastOutputValue.Font = new Font("굴림", 9F);
            lblLastOutputValue.Location = new Point(295, 155);
            lblLastOutputValue.Name = "lblLastOutputValue";
            lblLastOutputValue.Size = new Size(11, 12);
            lblLastOutputValue.TabIndex = 12;
            lblLastOutputValue.Text = "-";
            // 
            // lblPIDStatusValue
            // 
            lblPIDStatusValue.AutoSize = true;
            lblPIDStatusValue.Font = new Font("굴림", 9F, FontStyle.Bold);
            lblPIDStatusValue.ForeColor = Color.Red;
            lblPIDStatusValue.Location = new Point(74, 155);
            lblPIDStatusValue.Name = "lblPIDStatusValue";
            lblPIDStatusValue.Size = new Size(44, 12);
            lblPIDStatusValue.TabIndex = 10;
            lblPIDStatusValue.Text = "정지됨";
            // 
            // lblPIDStatus
            // 
            lblPIDStatus.AutoSize = true;
            lblPIDStatus.Location = new Point(10, 155);
            lblPIDStatus.Name = "lblPIDStatus";
            lblPIDStatus.Size = new Size(58, 15);
            lblPIDStatus.TabIndex = 9;
            lblPIDStatus.Text = "자동제어:";
            // 
            // lblBathCirculatorWarning
            // 
            lblBathCirculatorWarning.Location = new Point(222, 125);
            lblBathCirculatorWarning.Name = "lblBathCirculatorWarning";
            lblBathCirculatorWarning.Size = new Size(100, 23);
            lblBathCirculatorWarning.TabIndex = 12;
            lblBathCirculatorWarning.Text = "-";
            lblBathCirculatorWarning.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label26
            // 
            label26.BackColor = SystemColors.Info;
            label26.BorderStyle = BorderStyle.FixedSingle;
            label26.Location = new Point(222, 105);
            label26.Name = "label26";
            label26.Size = new Size(100, 20);
            label26.TabIndex = 20;
            label26.Text = "경고";
            label26.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtBathCirculatorTargetTemp
            // 
            txtBathCirculatorTargetTemp.Location = new Point(434, 125);
            txtBathCirculatorTargetTemp.Name = "txtBathCirculatorTargetTemp";
            txtBathCirculatorTargetTemp.ReadOnly = true;
            txtBathCirculatorTargetTemp.Size = new Size(90, 23);
            txtBathCirculatorTargetTemp.TabIndex = 24;
            txtBathCirculatorTargetTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtBathCirculatorCurrentTemp
            // 
            txtBathCirculatorCurrentTemp.Location = new Point(328, 125);
            txtBathCirculatorCurrentTemp.Name = "txtBathCirculatorCurrentTemp";
            txtBathCirculatorCurrentTemp.ReadOnly = true;
            txtBathCirculatorCurrentTemp.Size = new Size(100, 23);
            txtBathCirculatorCurrentTemp.TabIndex = 23;
            txtBathCirculatorCurrentTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtBathCirculatorMode
            // 
            txtBathCirculatorMode.Location = new Point(116, 125);
            txtBathCirculatorMode.Name = "txtBathCirculatorMode";
            txtBathCirculatorMode.ReadOnly = true;
            txtBathCirculatorMode.Size = new Size(100, 23);
            txtBathCirculatorMode.TabIndex = 7;
            txtBathCirculatorMode.TextAlign = HorizontalAlignment.Center;
            // 
            // txtBathCirculatorTime
            // 
            txtBathCirculatorTime.Location = new Point(205, 64);
            txtBathCirculatorTime.Name = "txtBathCirculatorTime";
            txtBathCirculatorTime.Size = new Size(100, 23);
            txtBathCirculatorTime.TabIndex = 25;
            txtBathCirculatorTime.Visible = false;
            // 
            // txtBathCirculatorStatus
            // 
            txtBathCirculatorStatus.Location = new Point(10, 125);
            txtBathCirculatorStatus.Name = "txtBathCirculatorStatus";
            txtBathCirculatorStatus.ReadOnly = true;
            txtBathCirculatorStatus.Size = new Size(100, 23);
            txtBathCirculatorStatus.TabIndex = 5;
            txtBathCirculatorStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // label28
            // 
            label28.BackColor = SystemColors.Info;
            label28.BorderStyle = BorderStyle.FixedSingle;
            label28.Location = new Point(434, 105);
            label28.Name = "label28";
            label28.Size = new Size(90, 20);
            label28.TabIndex = 22;
            label28.Text = "설정온도";
            label28.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label27
            // 
            label27.BackColor = SystemColors.Info;
            label27.BorderStyle = BorderStyle.FixedSingle;
            label27.Location = new Point(328, 105);
            label27.Name = "label27";
            label27.Size = new Size(100, 20);
            label27.TabIndex = 21;
            label27.Text = "칠러온도";
            label27.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label31
            // 
            label31.BackColor = SystemColors.Info;
            label31.BorderStyle = BorderStyle.FixedSingle;
            label31.Location = new Point(116, 105);
            label31.Name = "label31";
            label31.Size = new Size(100, 20);
            label31.TabIndex = 15;
            label31.Text = "모드";
            label31.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label32
            // 
            label32.Location = new Point(0, 0);
            label32.Name = "label32";
            label32.Size = new Size(100, 23);
            label32.TabIndex = 26;
            label32.Visible = false;
            // 
            // label36
            // 
            label36.BackColor = SystemColors.Info;
            label36.BorderStyle = BorderStyle.FixedSingle;
            label36.Location = new Point(10, 105);
            label36.Name = "label36";
            label36.Size = new Size(100, 20);
            label36.TabIndex = 13;
            label36.Text = "상태";
            label36.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // numCh2Target
            // 
            numCh2Target.DecimalPlaces = 1;
            numCh2Target.Location = new Point(443, 27);
            numCh2Target.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numCh2Target.Minimum = new decimal(new int[] { 200, 0, 0, int.MinValue });
            numCh2Target.Name = "numCh2Target";
            numCh2Target.Size = new Size(70, 23);
            numCh2Target.TabIndex = 4;
            numCh2Target.TextAlign = HorizontalAlignment.Center;
            numCh2Target.Value = new decimal(new int[] { 25, 0, 0, 0 });
            numCh2Target.ValueChanged += numCh2Target_ValueChanged;
            // 
            // lblCh2Target
            // 
            lblCh2Target.AutoSize = true;
            lblCh2Target.Location = new Point(381, 31);
            lblCh2Target.Name = "lblCh2Target";
            lblCh2Target.Size = new Size(55, 15);
            lblCh2Target.TabIndex = 3;
            lblCh2Target.Text = "목표(°C):";
            // 
            // cmbPIDChannel
            // 
            cmbPIDChannel.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbPIDChannel.FormattingEnabled = true;
            cmbPIDChannel.Items.AddRange(new object[] { "Ch1", "Ch2", "Ch3", "Ch4", "Ch5", "Ch6", "Ch7", "Ch8", "Ch9", "Ch10", "Ch11", "Ch12" });
            cmbPIDChannel.Location = new Point(253, 27);
            cmbPIDChannel.Name = "cmbPIDChannel";
            cmbPIDChannel.Size = new Size(120, 23);
            cmbPIDChannel.TabIndex = 14;
            cmbPIDChannel.SelectedIndexChanged += cmbPIDChannel_SelectedIndexChanged;
            // 
            // lblPIDChannel
            // 
            lblPIDChannel.AutoSize = true;
            lblPIDChannel.Location = new Point(208, 31);
            lblPIDChannel.Name = "lblPIDChannel";
            lblPIDChannel.Size = new Size(34, 15);
            lblPIDChannel.TabIndex = 13;
            lblPIDChannel.Text = "채널:";
            // 
            // chkChillerPIDEnabled
            // 
            chkChillerPIDEnabled.AutoSize = true;
            chkChillerPIDEnabled.Location = new Point(91, 31);
            chkChillerPIDEnabled.Name = "chkChillerPIDEnabled";
            chkChillerPIDEnabled.Size = new Size(106, 19);
            chkChillerPIDEnabled.TabIndex = 2;
            chkChillerPIDEnabled.Text = "자동 온도 제어";
            chkChillerPIDEnabled.UseVisualStyleBackColor = true;
            chkChillerPIDEnabled.CheckedChanged += chkChillerPIDEnabled_CheckedChanged;
            // 
            // btnBathCirculatorStart
            // 
            btnBathCirculatorStart.Location = new Point(10, 24);
            btnBathCirculatorStart.Name = "btnBathCirculatorStart";
            btnBathCirculatorStart.Size = new Size(75, 30);
            btnBathCirculatorStart.TabIndex = 0;
            btnBathCirculatorStart.Text = "Start";
            btnBathCirculatorStart.UseVisualStyleBackColor = true;
            btnBathCirculatorStart.Click += btnBathCirculatorStart_Click;
            // 
            // lblChillerManualTemp
            // 
            lblChillerManualTemp.Location = new Point(316, 68);
            lblChillerManualTemp.Name = "lblChillerManualTemp";
            lblChillerManualTemp.Size = new Size(65, 17);
            lblChillerManualTemp.TabIndex = 56;
            lblChillerManualTemp.Text = "수동 온도:";
            lblChillerManualTemp.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numChillerManualTemp
            // 
            numChillerManualTemp.DecimalPlaces = 1;
            numChillerManualTemp.Location = new Point(381, 65);
            numChillerManualTemp.Maximum = new decimal(new int[] { 200, 0, 0, 0 });
            numChillerManualTemp.Minimum = new decimal(new int[] { 200, 0, 0, int.MinValue });
            numChillerManualTemp.Name = "numChillerManualTemp";
            numChillerManualTemp.Size = new Size(70, 23);
            numChillerManualTemp.TabIndex = 57;
            numChillerManualTemp.TextAlign = HorizontalAlignment.Center;
            numChillerManualTemp.Value = new decimal(new int[] { 25, 0, 0, 0 });
            // 
            // btnBathCirculatorSetTemp
            // 
            btnBathCirculatorSetTemp.Location = new Point(457, 64);
            btnBathCirculatorSetTemp.Name = "btnBathCirculatorSetTemp";
            btnBathCirculatorSetTemp.Size = new Size(55, 25);
            btnBathCirculatorSetTemp.TabIndex = 55;
            btnBathCirculatorSetTemp.Text = "설정";
            btnBathCirculatorSetTemp.UseVisualStyleBackColor = true;
            btnBathCirculatorSetTemp.Click += btnBathCirculatorSetTemp_Click;
            // 
            // panel3
            // 
            panel3.Controls.Add(tableLayoutPanel6);
            panel3.Controls.Add(lblTurboPumpSpeedHeader);
            panel3.Controls.Add(lblTurboPumpSpeedSet);
            panel3.Controls.Add(numTurboPumpSpeed);
            panel3.Controls.Add(btnTurboPumpSpeedSet);
            panel3.Controls.Add(btnTurboPumpSpeedPerm);
            panel3.Controls.Add(lblTurboPumpStandby);
            panel3.Controls.Add(numTurboPumpStandbySpeed);
            panel3.Controls.Add(btnTurboPumpStandbySet);
            panel3.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            panel3.Location = new Point(3, 339);
            panel3.Name = "panel3";
            panel3.Size = new Size(537, 220);
            panel3.TabIndex = 13;
            panel3.TabStop = false;
            panel3.Text = "터보 펌프";
            // 
            // tableLayoutPanel6
            // 
            tableLayoutPanel6.ColumnCount = 6;
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.666666F));
            tableLayoutPanel6.Controls.Add(txtTurboPumpReady, 5, 4);
            tableLayoutPanel6.Controls.Add(progressBarTurboPumpSpeed, 3, 4);
            tableLayoutPanel6.Controls.Add(txtTurboPumpNormal, 4, 4);
            tableLayoutPanel6.Controls.Add(label25, 5, 3);
            tableLayoutPanel6.Controls.Add(label24, 4, 3);
            tableLayoutPanel6.Controls.Add(txtTurboPumpRemote, 5, 2);
            tableLayoutPanel6.Controls.Add(label23, 5, 1);
            tableLayoutPanel6.Controls.Add(label22, 3, 3);
            tableLayoutPanel6.Controls.Add(label21, 2, 3);
            tableLayoutPanel6.Controls.Add(label20, 1, 3);
            tableLayoutPanel6.Controls.Add(label19, 0, 3);
            tableLayoutPanel6.Controls.Add(label18, 4, 1);
            tableLayoutPanel6.Controls.Add(label17, 3, 1);
            tableLayoutPanel6.Controls.Add(label16, 2, 1);
            tableLayoutPanel6.Controls.Add(label15, 1, 1);
            tableLayoutPanel6.Controls.Add(label14, 0, 1);
            tableLayoutPanel6.Controls.Add(txtTurboPumpRunTime, 1, 4);
            tableLayoutPanel6.Controls.Add(lblTurboPumpWarning, 2, 4);
            tableLayoutPanel6.Controls.Add(txtTurboPumpBearingTemp, 0, 4);
            tableLayoutPanel6.Controls.Add(txtTurboPumpElectronicsTemp, 4, 2);
            tableLayoutPanel6.Controls.Add(txtTurboPumpMotorTemp, 3, 2);
            tableLayoutPanel6.Controls.Add(txtTurboPumpCurrent, 2, 2);
            tableLayoutPanel6.Controls.Add(txtTurboPumpSpeed, 1, 2);
            tableLayoutPanel6.Controls.Add(btnTurboPumpStart, 0, 0);
            tableLayoutPanel6.Controls.Add(btnTurboPumpVent, 1, 0);
            tableLayoutPanel6.Controls.Add(btnTurboPumpReset, 2, 0);
            tableLayoutPanel6.Controls.Add(txtTurboPumpStatus, 0, 2);
            tableLayoutPanel6.Location = new Point(3, 18);
            tableLayoutPanel6.Name = "tableLayoutPanel6";
            tableLayoutPanel6.RowCount = 5;
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel6.Size = new Size(527, 130);
            tableLayoutPanel6.TabIndex = 11;
            // 
            // txtTurboPumpReady
            // 
            txtTurboPumpReady.Dock = DockStyle.Fill;
            txtTurboPumpReady.Location = new Point(438, 98);
            txtTurboPumpReady.Name = "txtTurboPumpReady";
            txtTurboPumpReady.ReadOnly = true;
            txtTurboPumpReady.Size = new Size(86, 23);
            txtTurboPumpReady.TabIndex = 30;
            txtTurboPumpReady.TextAlign = HorizontalAlignment.Center;
            // 
            // progressBarTurboPumpSpeed
            // 
            progressBarTurboPumpSpeed.Dock = DockStyle.Fill;
            progressBarTurboPumpSpeed.Location = new Point(264, 98);
            progressBarTurboPumpSpeed.Name = "progressBarTurboPumpSpeed";
            progressBarTurboPumpSpeed.Size = new Size(81, 29);
            progressBarTurboPumpSpeed.TabIndex = 40;
            // 
            // txtTurboPumpNormal
            // 
            txtTurboPumpNormal.Dock = DockStyle.Fill;
            txtTurboPumpNormal.Location = new Point(351, 98);
            txtTurboPumpNormal.Name = "txtTurboPumpNormal";
            txtTurboPumpNormal.ReadOnly = true;
            txtTurboPumpNormal.Size = new Size(81, 23);
            txtTurboPumpNormal.TabIndex = 29;
            txtTurboPumpNormal.TextAlign = HorizontalAlignment.Center;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.BackColor = SystemColors.Info;
            label25.BorderStyle = BorderStyle.FixedSingle;
            label25.Dock = DockStyle.Fill;
            label25.Location = new Point(438, 75);
            label25.Name = "label25";
            label25.Size = new Size(86, 20);
            label25.TabIndex = 28;
            label25.Text = "IsReady";
            label25.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label24
            // 
            label24.AutoSize = true;
            label24.BackColor = SystemColors.Info;
            label24.BorderStyle = BorderStyle.FixedSingle;
            label24.Dock = DockStyle.Fill;
            label24.Location = new Point(351, 75);
            label24.Name = "label24";
            label24.Size = new Size(81, 20);
            label24.TabIndex = 27;
            label24.Text = "Normal";
            label24.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtTurboPumpRemote
            // 
            txtTurboPumpRemote.Dock = DockStyle.Fill;
            txtTurboPumpRemote.Location = new Point(438, 53);
            txtTurboPumpRemote.Name = "txtTurboPumpRemote";
            txtTurboPumpRemote.ReadOnly = true;
            txtTurboPumpRemote.Size = new Size(86, 23);
            txtTurboPumpRemote.TabIndex = 26;
            txtTurboPumpRemote.TextAlign = HorizontalAlignment.Center;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.BackColor = SystemColors.Info;
            label23.BorderStyle = BorderStyle.FixedSingle;
            label23.Dock = DockStyle.Fill;
            label23.Location = new Point(438, 30);
            label23.Name = "label23";
            label23.Size = new Size(86, 20);
            label23.TabIndex = 25;
            label23.Text = "REMOTE";
            label23.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label22
            // 
            label22.AutoSize = true;
            label22.BackColor = SystemColors.Info;
            label22.BorderStyle = BorderStyle.FixedSingle;
            label22.Dock = DockStyle.Fill;
            label22.Location = new Point(264, 75);
            label22.Name = "label22";
            label22.Size = new Size(81, 20);
            label22.TabIndex = 23;
            label22.Text = "펌핑속도";
            label22.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label21
            // 
            label21.AutoSize = true;
            label21.BackColor = SystemColors.Info;
            label21.BorderStyle = BorderStyle.FixedSingle;
            label21.Dock = DockStyle.Fill;
            label21.Location = new Point(177, 75);
            label21.Name = "label21";
            label21.Size = new Size(81, 20);
            label21.TabIndex = 22;
            label21.Text = "경고";
            label21.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label20
            // 
            label20.AutoSize = true;
            label20.BackColor = SystemColors.Info;
            label20.BorderStyle = BorderStyle.FixedSingle;
            label20.Dock = DockStyle.Fill;
            label20.Location = new Point(90, 75);
            label20.Name = "label20";
            label20.Size = new Size(81, 20);
            label20.TabIndex = 21;
            label20.Text = "런타임";
            label20.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label19
            // 
            label19.AutoSize = true;
            label19.BackColor = SystemColors.Info;
            label19.BorderStyle = BorderStyle.FixedSingle;
            label19.Dock = DockStyle.Fill;
            label19.Location = new Point(3, 75);
            label19.Name = "label19";
            label19.Size = new Size(81, 20);
            label19.TabIndex = 20;
            label19.Text = "베어링온도";
            label19.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label18
            // 
            label18.AutoSize = true;
            label18.BackColor = SystemColors.Info;
            label18.BorderStyle = BorderStyle.FixedSingle;
            label18.Dock = DockStyle.Fill;
            label18.Location = new Point(351, 30);
            label18.Name = "label18";
            label18.Size = new Size(81, 20);
            label18.TabIndex = 19;
            label18.Text = "장치온도";
            label18.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label17
            // 
            label17.AutoSize = true;
            label17.BackColor = SystemColors.Info;
            label17.BorderStyle = BorderStyle.FixedSingle;
            label17.Dock = DockStyle.Fill;
            label17.Location = new Point(264, 30);
            label17.Name = "label17";
            label17.Size = new Size(81, 20);
            label17.TabIndex = 18;
            label17.Text = "모터온도";
            label17.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label16
            // 
            label16.AutoSize = true;
            label16.BackColor = SystemColors.Info;
            label16.BorderStyle = BorderStyle.FixedSingle;
            label16.Dock = DockStyle.Fill;
            label16.Location = new Point(177, 30);
            label16.Name = "label16";
            label16.Size = new Size(81, 20);
            label16.TabIndex = 17;
            label16.Text = "전류";
            label16.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.BackColor = SystemColors.Info;
            label15.BorderStyle = BorderStyle.FixedSingle;
            label15.Dock = DockStyle.Fill;
            label15.Location = new Point(90, 30);
            label15.Name = "label15";
            label15.Size = new Size(81, 20);
            label15.TabIndex = 16;
            label15.Text = "속도";
            label15.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.BackColor = SystemColors.Info;
            label14.BorderStyle = BorderStyle.FixedSingle;
            label14.Dock = DockStyle.Fill;
            label14.Location = new Point(3, 30);
            label14.Name = "label14";
            label14.Size = new Size(81, 20);
            label14.TabIndex = 15;
            label14.Text = "상태";
            label14.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtTurboPumpRunTime
            // 
            txtTurboPumpRunTime.Dock = DockStyle.Fill;
            txtTurboPumpRunTime.Location = new Point(90, 98);
            txtTurboPumpRunTime.Name = "txtTurboPumpRunTime";
            txtTurboPumpRunTime.ReadOnly = true;
            txtTurboPumpRunTime.Size = new Size(81, 23);
            txtTurboPumpRunTime.TabIndex = 13;
            txtTurboPumpRunTime.TextAlign = HorizontalAlignment.Center;
            // 
            // lblTurboPumpWarning
            // 
            lblTurboPumpWarning.AutoSize = true;
            lblTurboPumpWarning.Dock = DockStyle.Fill;
            lblTurboPumpWarning.Location = new Point(177, 95);
            lblTurboPumpWarning.Name = "lblTurboPumpWarning";
            lblTurboPumpWarning.Size = new Size(81, 35);
            lblTurboPumpWarning.TabIndex = 12;
            lblTurboPumpWarning.Text = "-";
            // 
            // txtTurboPumpBearingTemp
            // 
            txtTurboPumpBearingTemp.Dock = DockStyle.Fill;
            txtTurboPumpBearingTemp.Location = new Point(3, 98);
            txtTurboPumpBearingTemp.Name = "txtTurboPumpBearingTemp";
            txtTurboPumpBearingTemp.ReadOnly = true;
            txtTurboPumpBearingTemp.Size = new Size(81, 23);
            txtTurboPumpBearingTemp.TabIndex = 10;
            txtTurboPumpBearingTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpElectronicsTemp
            // 
            txtTurboPumpElectronicsTemp.Dock = DockStyle.Fill;
            txtTurboPumpElectronicsTemp.Location = new Point(351, 53);
            txtTurboPumpElectronicsTemp.Name = "txtTurboPumpElectronicsTemp";
            txtTurboPumpElectronicsTemp.ReadOnly = true;
            txtTurboPumpElectronicsTemp.Size = new Size(81, 23);
            txtTurboPumpElectronicsTemp.TabIndex = 9;
            txtTurboPumpElectronicsTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpMotorTemp
            // 
            txtTurboPumpMotorTemp.Dock = DockStyle.Fill;
            txtTurboPumpMotorTemp.Location = new Point(264, 53);
            txtTurboPumpMotorTemp.Name = "txtTurboPumpMotorTemp";
            txtTurboPumpMotorTemp.ReadOnly = true;
            txtTurboPumpMotorTemp.Size = new Size(81, 23);
            txtTurboPumpMotorTemp.TabIndex = 8;
            txtTurboPumpMotorTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpCurrent
            // 
            txtTurboPumpCurrent.Dock = DockStyle.Fill;
            txtTurboPumpCurrent.Location = new Point(177, 53);
            txtTurboPumpCurrent.Name = "txtTurboPumpCurrent";
            txtTurboPumpCurrent.ReadOnly = true;
            txtTurboPumpCurrent.Size = new Size(81, 23);
            txtTurboPumpCurrent.TabIndex = 7;
            txtTurboPumpCurrent.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpSpeed
            // 
            txtTurboPumpSpeed.Dock = DockStyle.Fill;
            txtTurboPumpSpeed.Location = new Point(90, 53);
            txtTurboPumpSpeed.Name = "txtTurboPumpSpeed";
            txtTurboPumpSpeed.ReadOnly = true;
            txtTurboPumpSpeed.Size = new Size(81, 23);
            txtTurboPumpSpeed.TabIndex = 6;
            txtTurboPumpSpeed.TextAlign = HorizontalAlignment.Center;
            // 
            // btnTurboPumpStart
            // 
            btnTurboPumpStart.Dock = DockStyle.Fill;
            btnTurboPumpStart.Location = new Point(3, 3);
            btnTurboPumpStart.Name = "btnTurboPumpStart";
            btnTurboPumpStart.Size = new Size(81, 24);
            btnTurboPumpStart.TabIndex = 0;
            btnTurboPumpStart.Text = "Start";
            btnTurboPumpStart.UseVisualStyleBackColor = true;
            btnTurboPumpStart.Click += btnTurboPumpStart_Click;
            // 
            // btnTurboPumpVent
            // 
            btnTurboPumpVent.Dock = DockStyle.Fill;
            btnTurboPumpVent.Location = new Point(90, 3);
            btnTurboPumpVent.Name = "btnTurboPumpVent";
            btnTurboPumpVent.Size = new Size(81, 24);
            btnTurboPumpVent.TabIndex = 2;
            btnTurboPumpVent.Text = "Vent";
            btnTurboPumpVent.UseVisualStyleBackColor = true;
            btnTurboPumpVent.Click += btnTurboPumpVent_Click;
            // 
            // btnTurboPumpReset
            // 
            btnTurboPumpReset.Dock = DockStyle.Fill;
            btnTurboPumpReset.Location = new Point(177, 3);
            btnTurboPumpReset.Name = "btnTurboPumpReset";
            btnTurboPumpReset.Size = new Size(81, 24);
            btnTurboPumpReset.TabIndex = 3;
            btnTurboPumpReset.Text = "Reset";
            btnTurboPumpReset.UseVisualStyleBackColor = true;
            btnTurboPumpReset.Click += btnTurboPumpReset_Click;
            // 
            // txtTurboPumpStatus
            // 
            txtTurboPumpStatus.Dock = DockStyle.Fill;
            txtTurboPumpStatus.Location = new Point(3, 53);
            txtTurboPumpStatus.Name = "txtTurboPumpStatus";
            txtTurboPumpStatus.ReadOnly = true;
            txtTurboPumpStatus.Size = new Size(81, 23);
            txtTurboPumpStatus.TabIndex = 5;
            txtTurboPumpStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // lblTurboPumpSpeedHeader
            // 
            lblTurboPumpSpeedHeader.BackColor = SystemColors.Info;
            lblTurboPumpSpeedHeader.BorderStyle = BorderStyle.FixedSingle;
            lblTurboPumpSpeedHeader.Location = new Point(3, 152);
            lblTurboPumpSpeedHeader.Name = "lblTurboPumpSpeedHeader";
            lblTurboPumpSpeedHeader.Padding = new Padding(5, 0, 0, 0);
            lblTurboPumpSpeedHeader.Size = new Size(527, 20);
            lblTurboPumpSpeedHeader.TabIndex = 12;
            lblTurboPumpSpeedHeader.Text = "속도 설정 (60~630 Hz)";
            lblTurboPumpSpeedHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblTurboPumpSpeedSet
            // 
            lblTurboPumpSpeedSet.Location = new Point(8, 175);
            lblTurboPumpSpeedSet.Name = "lblTurboPumpSpeedSet";
            lblTurboPumpSpeedSet.Size = new Size(65, 23);
            lblTurboPumpSpeedSet.TabIndex = 13;
            lblTurboPumpSpeedSet.Text = "속도 (Hz)";
            lblTurboPumpSpeedSet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numTurboPumpSpeed
            // 
            numTurboPumpSpeed.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numTurboPumpSpeed.Location = new Point(73, 175);
            numTurboPumpSpeed.Maximum = new decimal(new int[] { 630, 0, 0, 0 });
            numTurboPumpSpeed.Minimum = new decimal(new int[] { 60, 0, 0, 0 });
            numTurboPumpSpeed.Name = "numTurboPumpSpeed";
            numTurboPumpSpeed.Size = new Size(70, 23);
            numTurboPumpSpeed.TabIndex = 14;
            numTurboPumpSpeed.TextAlign = HorizontalAlignment.Center;
            numTurboPumpSpeed.Value = new decimal(new int[] { 630, 0, 0, 0 });
            // 
            // btnTurboPumpSpeedSet
            // 
            btnTurboPumpSpeedSet.Location = new Point(148, 174);
            btnTurboPumpSpeedSet.Name = "btnTurboPumpSpeedSet";
            btnTurboPumpSpeedSet.Size = new Size(55, 25);
            btnTurboPumpSpeedSet.TabIndex = 15;
            btnTurboPumpSpeedSet.Text = "적용";
            btnTurboPumpSpeedSet.Click += btnTurboPumpSpeedSet_Click;
            // 
            // btnTurboPumpSpeedPerm
            // 
            btnTurboPumpSpeedPerm.Location = new Point(208, 174);
            btnTurboPumpSpeedPerm.Name = "btnTurboPumpSpeedPerm";
            btnTurboPumpSpeedPerm.Size = new Size(70, 25);
            btnTurboPumpSpeedPerm.TabIndex = 16;
            btnTurboPumpSpeedPerm.Text = "영구저장";
            btnTurboPumpSpeedPerm.Click += btnTurboPumpSpeedPerm_Click;
            // 
            // lblTurboPumpStandby
            // 
            lblTurboPumpStandby.Location = new Point(288, 175);
            lblTurboPumpStandby.Name = "lblTurboPumpStandby";
            lblTurboPumpStandby.Size = new Size(65, 23);
            lblTurboPumpStandby.TabIndex = 17;
            lblTurboPumpStandby.Text = "대기 (Hz)";
            lblTurboPumpStandby.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numTurboPumpStandbySpeed
            // 
            numTurboPumpStandbySpeed.Increment = new decimal(new int[] { 10, 0, 0, 0 });
            numTurboPumpStandbySpeed.Location = new Point(353, 175);
            numTurboPumpStandbySpeed.Maximum = new decimal(new int[] { 630, 0, 0, 0 });
            numTurboPumpStandbySpeed.Name = "numTurboPumpStandbySpeed";
            numTurboPumpStandbySpeed.Size = new Size(70, 23);
            numTurboPumpStandbySpeed.TabIndex = 18;
            numTurboPumpStandbySpeed.TextAlign = HorizontalAlignment.Center;
            numTurboPumpStandbySpeed.Value = new decimal(new int[] { 250, 0, 0, 0 });
            // 
            // btnTurboPumpStandbySet
            // 
            btnTurboPumpStandbySet.Location = new Point(428, 174);
            btnTurboPumpStandbySet.Name = "btnTurboPumpStandbySet";
            btnTurboPumpStandbySet.Size = new Size(55, 25);
            btnTurboPumpStandbySet.TabIndex = 19;
            btnTurboPumpStandbySet.Text = "적용";
            btnTurboPumpStandbySet.Click += btnTurboPumpStandbySet_Click;
            // 
            // panel2
            // 
            panel2.Controls.Add(tableLayoutPanel1);
            panel2.Controls.Add(lblDryPumpSpeedHeader);
            panel2.Controls.Add(lblDryPumpSpeedSet);
            panel2.Controls.Add(numDryPumpSpeed);
            panel2.Controls.Add(btnDryPumpSpeedSet);
            panel2.Controls.Add(btnDryPumpSpeedStore);
            panel2.Font = new Font("맑은 고딕", 9F, FontStyle.Bold);
            panel2.Location = new Point(3, 128);
            panel2.Name = "panel2";
            panel2.Size = new Size(537, 205);
            panel2.TabIndex = 12;
            panel2.TabStop = false;
            panel2.Text = "드라이 펌프";
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 5;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel1.Controls.Add(label13, 2, 3);
            tableLayoutPanel1.Controls.Add(label12, 1, 3);
            tableLayoutPanel1.Controls.Add(label11, 0, 3);
            tableLayoutPanel1.Controls.Add(label10, 4, 1);
            tableLayoutPanel1.Controls.Add(label9, 3, 1);
            tableLayoutPanel1.Controls.Add(label8, 2, 1);
            tableLayoutPanel1.Controls.Add(label7, 1, 1);
            tableLayoutPanel1.Controls.Add(lblDryPumpWarning, 2, 4);
            tableLayoutPanel1.Controls.Add(txtDryPumpRunTime, 0, 4);
            tableLayoutPanel1.Controls.Add(txtDryPumpMotorTemp, 4, 2);
            tableLayoutPanel1.Controls.Add(txtDryPumpPower, 3, 2);
            tableLayoutPanel1.Controls.Add(txtDryPumpCurrent, 2, 2);
            tableLayoutPanel1.Controls.Add(txtDryPumpFrequency, 1, 2);
            tableLayoutPanel1.Controls.Add(btnDryPumpStart, 0, 0);
            tableLayoutPanel1.Controls.Add(btnDryPumpStandby, 1, 0);
            tableLayoutPanel1.Controls.Add(btnDryPumpNormal, 2, 0);
            tableLayoutPanel1.Controls.Add(txtDryPumpStatus, 0, 2);
            tableLayoutPanel1.Controls.Add(lblDryPumpService, 1, 4);
            tableLayoutPanel1.Controls.Add(label5, 0, 1);
            tableLayoutPanel1.Location = new Point(3, 18);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.Size = new Size(527, 120);
            tableLayoutPanel1.TabIndex = 11;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.BackColor = SystemColors.Info;
            label13.BorderStyle = BorderStyle.FixedSingle;
            label13.Dock = DockStyle.Fill;
            label13.Location = new Point(213, 75);
            label13.Name = "label13";
            label13.Size = new Size(99, 20);
            label13.TabIndex = 20;
            label13.Text = "경고";
            label13.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.BackColor = SystemColors.Info;
            label12.BorderStyle = BorderStyle.FixedSingle;
            label12.Dock = DockStyle.Fill;
            label12.Location = new Point(108, 75);
            label12.Name = "label12";
            label12.Size = new Size(99, 20);
            label12.TabIndex = 19;
            label12.Text = "서비스 상태";
            label12.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.BackColor = SystemColors.Info;
            label11.BorderStyle = BorderStyle.FixedSingle;
            label11.Dock = DockStyle.Fill;
            label11.Location = new Point(3, 75);
            label11.Name = "label11";
            label11.Size = new Size(99, 20);
            label11.TabIndex = 18;
            label11.Text = "런타임";
            label11.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.BackColor = SystemColors.Info;
            label10.BorderStyle = BorderStyle.FixedSingle;
            label10.Dock = DockStyle.Fill;
            label10.Location = new Point(423, 30);
            label10.Name = "label10";
            label10.Size = new Size(101, 20);
            label10.TabIndex = 17;
            label10.Text = "모터온도";
            label10.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.BackColor = SystemColors.Info;
            label9.BorderStyle = BorderStyle.FixedSingle;
            label9.Dock = DockStyle.Fill;
            label9.Location = new Point(318, 30);
            label9.Name = "label9";
            label9.Size = new Size(99, 20);
            label9.TabIndex = 16;
            label9.Text = "파워";
            label9.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = SystemColors.Info;
            label8.BorderStyle = BorderStyle.FixedSingle;
            label8.Dock = DockStyle.Fill;
            label8.Location = new Point(213, 30);
            label8.Name = "label8";
            label8.Size = new Size(99, 20);
            label8.TabIndex = 15;
            label8.Text = "전류";
            label8.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.BackColor = SystemColors.Info;
            label7.BorderStyle = BorderStyle.FixedSingle;
            label7.Dock = DockStyle.Fill;
            label7.Location = new Point(108, 30);
            label7.Name = "label7";
            label7.Size = new Size(99, 20);
            label7.TabIndex = 14;
            label7.Text = "속도";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDryPumpWarning
            // 
            lblDryPumpWarning.AutoSize = true;
            lblDryPumpWarning.Dock = DockStyle.Fill;
            lblDryPumpWarning.Location = new Point(213, 95);
            lblDryPumpWarning.Name = "lblDryPumpWarning";
            lblDryPumpWarning.Size = new Size(99, 25);
            lblDryPumpWarning.TabIndex = 12;
            lblDryPumpWarning.Text = "-";
            // 
            // txtDryPumpRunTime
            // 
            txtDryPumpRunTime.Dock = DockStyle.Fill;
            txtDryPumpRunTime.Location = new Point(3, 98);
            txtDryPumpRunTime.Name = "txtDryPumpRunTime";
            txtDryPumpRunTime.ReadOnly = true;
            txtDryPumpRunTime.Size = new Size(99, 23);
            txtDryPumpRunTime.TabIndex = 10;
            txtDryPumpRunTime.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpMotorTemp
            // 
            txtDryPumpMotorTemp.Dock = DockStyle.Fill;
            txtDryPumpMotorTemp.Location = new Point(423, 53);
            txtDryPumpMotorTemp.Name = "txtDryPumpMotorTemp";
            txtDryPumpMotorTemp.ReadOnly = true;
            txtDryPumpMotorTemp.Size = new Size(101, 23);
            txtDryPumpMotorTemp.TabIndex = 9;
            txtDryPumpMotorTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpPower
            // 
            txtDryPumpPower.Dock = DockStyle.Fill;
            txtDryPumpPower.Location = new Point(318, 53);
            txtDryPumpPower.Name = "txtDryPumpPower";
            txtDryPumpPower.ReadOnly = true;
            txtDryPumpPower.Size = new Size(99, 23);
            txtDryPumpPower.TabIndex = 8;
            txtDryPumpPower.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpCurrent
            // 
            txtDryPumpCurrent.Dock = DockStyle.Fill;
            txtDryPumpCurrent.Location = new Point(213, 53);
            txtDryPumpCurrent.Name = "txtDryPumpCurrent";
            txtDryPumpCurrent.ReadOnly = true;
            txtDryPumpCurrent.Size = new Size(99, 23);
            txtDryPumpCurrent.TabIndex = 7;
            txtDryPumpCurrent.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpFrequency
            // 
            txtDryPumpFrequency.Dock = DockStyle.Fill;
            txtDryPumpFrequency.Location = new Point(108, 53);
            txtDryPumpFrequency.Name = "txtDryPumpFrequency";
            txtDryPumpFrequency.ReadOnly = true;
            txtDryPumpFrequency.Size = new Size(99, 23);
            txtDryPumpFrequency.TabIndex = 6;
            txtDryPumpFrequency.TextAlign = HorizontalAlignment.Center;
            // 
            // btnDryPumpStart
            // 
            btnDryPumpStart.Dock = DockStyle.Fill;
            btnDryPumpStart.Location = new Point(3, 3);
            btnDryPumpStart.Name = "btnDryPumpStart";
            btnDryPumpStart.Size = new Size(99, 24);
            btnDryPumpStart.TabIndex = 0;
            btnDryPumpStart.Text = "Start";
            btnDryPumpStart.UseVisualStyleBackColor = true;
            btnDryPumpStart.Click += btnDryPumpStart_Click;
            // 
            // btnDryPumpStandby
            // 
            btnDryPumpStandby.Dock = DockStyle.Fill;
            btnDryPumpStandby.Location = new Point(108, 3);
            btnDryPumpStandby.Name = "btnDryPumpStandby";
            btnDryPumpStandby.Size = new Size(99, 24);
            btnDryPumpStandby.TabIndex = 2;
            btnDryPumpStandby.Text = "Standby";
            btnDryPumpStandby.UseVisualStyleBackColor = true;
            btnDryPumpStandby.Click += btnDryPumpStandby_Click;
            // 
            // btnDryPumpNormal
            // 
            btnDryPumpNormal.Dock = DockStyle.Fill;
            btnDryPumpNormal.Location = new Point(213, 3);
            btnDryPumpNormal.Name = "btnDryPumpNormal";
            btnDryPumpNormal.Size = new Size(99, 24);
            btnDryPumpNormal.TabIndex = 3;
            btnDryPumpNormal.Text = "Restart";
            btnDryPumpNormal.UseVisualStyleBackColor = true;
            btnDryPumpNormal.Click += btnDryPumpNormal_Click;
            // 
            // txtDryPumpStatus
            // 
            txtDryPumpStatus.Dock = DockStyle.Fill;
            txtDryPumpStatus.Location = new Point(3, 53);
            txtDryPumpStatus.Name = "txtDryPumpStatus";
            txtDryPumpStatus.ReadOnly = true;
            txtDryPumpStatus.Size = new Size(99, 23);
            txtDryPumpStatus.TabIndex = 5;
            txtDryPumpStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // lblDryPumpService
            // 
            lblDryPumpService.AutoSize = true;
            lblDryPumpService.Dock = DockStyle.Fill;
            lblDryPumpService.Location = new Point(108, 95);
            lblDryPumpService.Name = "lblDryPumpService";
            lblDryPumpService.Size = new Size(99, 25);
            lblDryPumpService.TabIndex = 11;
            lblDryPumpService.Text = "-";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = SystemColors.Info;
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Dock = DockStyle.Fill;
            label5.Location = new Point(3, 30);
            label5.Name = "label5";
            label5.Size = new Size(99, 20);
            label5.TabIndex = 13;
            label5.Text = "상태";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDryPumpSpeedHeader
            // 
            lblDryPumpSpeedHeader.BackColor = SystemColors.Info;
            lblDryPumpSpeedHeader.BorderStyle = BorderStyle.FixedSingle;
            lblDryPumpSpeedHeader.Location = new Point(3, 143);
            lblDryPumpSpeedHeader.Name = "lblDryPumpSpeedHeader";
            lblDryPumpSpeedHeader.Padding = new Padding(5, 0, 0, 0);
            lblDryPumpSpeedHeader.Size = new Size(527, 20);
            lblDryPumpSpeedHeader.TabIndex = 12;
            lblDryPumpSpeedHeader.Text = "속도 설정";
            lblDryPumpSpeedHeader.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // lblDryPumpSpeedSet
            // 
            lblDryPumpSpeedSet.Location = new Point(8, 166);
            lblDryPumpSpeedSet.Name = "lblDryPumpSpeedSet";
            lblDryPumpSpeedSet.Size = new Size(60, 23);
            lblDryPumpSpeedSet.TabIndex = 13;
            lblDryPumpSpeedSet.Text = "속도 (%)";
            lblDryPumpSpeedSet.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // numDryPumpSpeed
            // 
            numDryPumpSpeed.Increment = new decimal(new int[] { 5, 0, 0, 0 });
            numDryPumpSpeed.Location = new Point(68, 166);
            numDryPumpSpeed.Minimum = new decimal(new int[] { 50, 0, 0, 0 });
            numDryPumpSpeed.Name = "numDryPumpSpeed";
            numDryPumpSpeed.Size = new Size(70, 23);
            numDryPumpSpeed.TabIndex = 14;
            numDryPumpSpeed.TextAlign = HorizontalAlignment.Center;
            numDryPumpSpeed.Value = new decimal(new int[] { 100, 0, 0, 0 });
            // 
            // btnDryPumpSpeedSet
            // 
            btnDryPumpSpeedSet.Location = new Point(143, 165);
            btnDryPumpSpeedSet.Name = "btnDryPumpSpeedSet";
            btnDryPumpSpeedSet.Size = new Size(55, 25);
            btnDryPumpSpeedSet.TabIndex = 15;
            btnDryPumpSpeedSet.Text = "적용";
            btnDryPumpSpeedSet.Click += btnDryPumpSpeedSet_Click;
            // 
            // btnDryPumpSpeedStore
            // 
            btnDryPumpSpeedStore.Location = new Point(203, 165);
            btnDryPumpSpeedStore.Name = "btnDryPumpSpeedStore";
            btnDryPumpSpeedStore.Size = new Size(65, 25);
            btnDryPumpSpeedStore.TabIndex = 16;
            btnDryPumpSpeedStore.Text = "영구저장";
            btnDryPumpSpeedStore.Click += btnDryPumpSpeedStore_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(txtLog);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1157, 796);
            tabPage2.TabIndex = 3;
            tabPage2.Text = "Log";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Location = new Point(3, 3);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(1151, 790);
            txtLog.TabIndex = 0;
            txtLog.Text = "";
            // 
            // tabPageAutoRun
            // 
            tabPageAutoRun.Location = new Point(4, 24);
            tabPageAutoRun.Name = "tabPageAutoRun";
            tabPageAutoRun.Padding = new Padding(3);
            tabPageAutoRun.Size = new Size(1157, 796);
            tabPageAutoRun.TabIndex = 4;
            tabPageAutoRun.Text = "AutoRun";
            tabPageAutoRun.UseVisualStyleBackColor = true;
            // 
            // btnHoldSettings
            // 
            btnHoldSettings.Location = new Point(0, 0);
            btnHoldSettings.Name = "btnHoldSettings";
            btnHoldSettings.Size = new Size(75, 23);
            btnHoldSettings.TabIndex = 0;
            // 
            // btnBakeoutSettings
            // 
            btnBakeoutSettings.Location = new Point(0, 0);
            btnBakeoutSettings.Name = "btnBakeoutSettings";
            btnBakeoutSettings.Size = new Size(75, 23);
            btnBakeoutSettings.TabIndex = 0;
            // 
            // numCh1ReachCount
            // 
            numCh1ReachCount.Location = new Point(0, 0);
            numCh1ReachCount.Name = "numCh1ReachCount";
            numCh1ReachCount.Size = new Size(120, 23);
            numCh1ReachCount.TabIndex = 0;
            // 
            // lblCh1ReachCount
            // 
            lblCh1ReachCount.Location = new Point(0, 0);
            lblCh1ReachCount.Name = "lblCh1ReachCount";
            lblCh1ReachCount.Size = new Size(100, 23);
            lblCh1ReachCount.TabIndex = 0;
            // 
            // scientificPressureInput1
            // 
            scientificPressureInput1.Location = new Point(0, 0);
            scientificPressureInput1.MinimumSize = new Size(150, 25);
            scientificPressureInput1.Name = "scientificPressureInput1";
            scientificPressureInput1.Size = new Size(185, 25);
            scientificPressureInput1.TabIndex = 0;
            // 
            // btnCh1AutoStart
            // 
            btnCh1AutoStart.Location = new Point(0, 0);
            btnCh1AutoStart.Name = "btnCh1AutoStart";
            btnCh1AutoStart.Size = new Size(75, 23);
            btnCh1AutoStart.TabIndex = 0;
            // 
            // lblCh1TargetPressure
            // 
            lblCh1TargetPressure.Location = new Point(0, 0);
            lblCh1TargetPressure.Name = "lblCh1TargetPressure";
            lblCh1TargetPressure.Size = new Size(100, 23);
            lblCh1TargetPressure.TabIndex = 0;
            // 
            // chkCh1AutoStartEnabled
            // 
            chkCh1AutoStartEnabled.Location = new Point(0, 0);
            chkCh1AutoStartEnabled.Name = "chkCh1AutoStartEnabled";
            chkCh1AutoStartEnabled.Size = new Size(104, 24);
            chkCh1AutoStartEnabled.TabIndex = 0;
            // 
            // lblLastOutput
            // 
            lblLastOutput.Location = new Point(0, 0);
            lblLastOutput.Name = "lblLastOutput";
            lblLastOutput.Size = new Size(100, 23);
            lblLastOutput.TabIndex = 0;
            // 
            // lblSeconds
            // 
            lblSeconds.Location = new Point(0, 0);
            lblSeconds.Name = "lblSeconds";
            lblSeconds.Size = new Size(100, 23);
            lblSeconds.TabIndex = 0;
            lblSeconds.Visible = false;
            // 
            // numUpdateInterval
            // 
            numUpdateInterval.Location = new Point(0, 0);
            numUpdateInterval.Name = "numUpdateInterval";
            numUpdateInterval.Size = new Size(120, 23);
            numUpdateInterval.TabIndex = 0;
            numUpdateInterval.Visible = false;
            numUpdateInterval.ValueChanged += numUpdateInterval_ValueChanged;
            // 
            // lblUpdateInterval
            // 
            lblUpdateInterval.Location = new Point(0, 0);
            lblUpdateInterval.Name = "lblUpdateInterval";
            lblUpdateInterval.Size = new Size(100, 23);
            lblUpdateInterval.TabIndex = 0;
            lblUpdateInterval.Visible = false;
            // 
            // grpPIDParams
            // 
            grpPIDParams.Location = new Point(0, 0);
            grpPIDParams.Name = "grpPIDParams";
            grpPIDParams.Size = new Size(200, 100);
            grpPIDParams.TabIndex = 0;
            grpPIDParams.TabStop = false;
            grpPIDParams.Visible = false;
            // 
            // numKd
            // 
            numKd.Location = new Point(0, 0);
            numKd.Name = "numKd";
            numKd.Size = new Size(120, 23);
            numKd.TabIndex = 0;
            numKd.ValueChanged += PIDParams_ValueChanged;
            // 
            // lblKd
            // 
            lblKd.Location = new Point(0, 0);
            lblKd.Name = "lblKd";
            lblKd.Size = new Size(100, 23);
            lblKd.TabIndex = 0;
            // 
            // numKi
            // 
            numKi.Location = new Point(0, 0);
            numKi.Name = "numKi";
            numKi.Size = new Size(120, 23);
            numKi.TabIndex = 0;
            numKi.ValueChanged += PIDParams_ValueChanged;
            // 
            // lblKi
            // 
            lblKi.Location = new Point(0, 0);
            lblKi.Name = "lblKi";
            lblKi.Size = new Size(100, 23);
            lblKi.TabIndex = 0;
            // 
            // numKp
            // 
            numKp.Location = new Point(0, 0);
            numKp.Name = "numKp";
            numKp.Size = new Size(120, 23);
            numKp.TabIndex = 0;
            numKp.ValueChanged += PIDParams_ValueChanged;
            // 
            // lblKp
            // 
            lblKp.Location = new Point(0, 0);
            lblKp.Name = "lblKp";
            lblKp.Size = new Size(100, 23);
            lblKp.TabIndex = 0;
            // 
            // numChillerBase
            // 
            numChillerBase.Location = new Point(0, 0);
            numChillerBase.Name = "numChillerBase";
            numChillerBase.Size = new Size(120, 23);
            numChillerBase.TabIndex = 0;
            numChillerBase.Visible = false;
            numChillerBase.ValueChanged += numChillerBase_ValueChanged;
            // 
            // lblChillerBase
            // 
            lblChillerBase.Location = new Point(0, 0);
            lblChillerBase.Name = "lblChillerBase";
            lblChillerBase.Size = new Size(100, 23);
            lblChillerBase.TabIndex = 0;
            lblChillerBase.Visible = false;
            // 
            // panel4
            // 
            panel4.Location = new Point(0, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(0, 0);
            panel4.TabIndex = 0;
            panel4.Visible = false;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.Location = new Point(0, 0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.Size = new Size(200, 100);
            tableLayoutPanel7.TabIndex = 0;
            // 
            // btnBathCirculatorSetTime
            // 
            btnBathCirculatorSetTime.Location = new Point(0, 0);
            btnBathCirculatorSetTime.Name = "btnBathCirculatorSetTime";
            btnBathCirculatorSetTime.Size = new Size(75, 23);
            btnBathCirculatorSetTime.TabIndex = 0;
            btnBathCirculatorSetTime.Visible = false;
            btnBathCirculatorSetTime.Click += btnBathCirculatorSetTime_Click;
            // 
            // label34
            // 
            label34.Location = new Point(0, 0);
            label34.Name = "label34";
            label34.Size = new Size(100, 23);
            label34.TabIndex = 0;
            label34.Visible = false;
            // 
            // tabPageThermalRamp
            // 
            tabPageThermalRamp.Controls.Add(simpleRampControl1);
            tabPageThermalRamp.Location = new Point(4, 24);
            tabPageThermalRamp.Name = "tabPageThermalRamp";
            tabPageThermalRamp.Padding = new Padding(3);
            tabPageThermalRamp.Size = new Size(192, 72);
            tabPageThermalRamp.TabIndex = 5;
            tabPageThermalRamp.Text = "온도 램프(Debug)";
            tabPageThermalRamp.UseVisualStyleBackColor = true;
            // 
            // simpleRampControl1
            // 
            simpleRampControl1.AutoStartTimerOnTargetReached = true;
            simpleRampControl1.BackColor = SystemColors.Control;
            simpleRampControl1.Dock = DockStyle.Fill;
            simpleRampControl1.EndAction = Core.Control.BakeoutEndAction.MaintainTemperature;
            simpleRampControl1.HoldAfterComplete = true;
            simpleRampControl1.Location = new Point(3, 3);
            simpleRampControl1.Name = "simpleRampControl1";
            simpleRampControl1.Size = new Size(186, 66);
            simpleRampControl1.TabIndex = 0;
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(24, 24);
            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuHelp });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1171, 24);
            menuStrip.TabIndex = 1;
            // 
            // menuFile
            // 
            menuFile.DropDownItems.AddRange(new ToolStripItem[] { menuFileExit });
            menuFile.Name = "menuFile";
            menuFile.Size = new Size(57, 20);
            menuFile.Text = "파일(&F)";
            // 
            // menuFileExit
            // 
            menuFileExit.Name = "menuFileExit";
            menuFileExit.Size = new Size(113, 22);
            menuFileExit.Text = "종료(&X)";
            menuFileExit.Click += menuFileExit_Click;
            // 
            // menuHelp
            // 
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuHelpPatchNotes, menuHelpAbout });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(72, 20);
            menuHelp.Text = "도움말(&H)";
            // 
            // menuHelpPatchNotes
            // 
            menuHelpPatchNotes.Name = "menuHelpPatchNotes";
            menuHelpPatchNotes.Size = new Size(141, 22);
            menuHelpPatchNotes.Text = "패치 노트(&P)";
            menuHelpPatchNotes.Click += MenuHelpPatchNotes_Click;
            // 
            // menuHelpAbout
            // 
            menuHelpAbout.Name = "menuHelpAbout";
            menuHelpAbout.Size = new Size(141, 22);
            menuHelpAbout.Text = "정보(&A)";
            menuHelpAbout.Click += MenuHelpAbout_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusConnection, toolStripStatusVersion });
            statusStrip.Location = new Point(0, 903);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1171, 22);
            statusStrip.TabIndex = 2;
            // 
            // toolStripStatusConnection
            // 
            toolStripStatusConnection.Name = "toolStripStatusConnection";
            toolStripStatusConnection.Size = new Size(31, 17);
            toolStripStatusConnection.Text = "준비";
            // 
            // toolStripStatusVersion
            // 
            toolStripStatusVersion.Alignment = ToolStripItemAlignment.Right;
            toolStripStatusVersion.Name = "toolStripStatusVersion";
            toolStripStatusVersion.Size = new Size(40, 17);
            toolStripStatusVersion.Text = "v2.1.0";
            // 
            // gridViewMaster
            // 
            gridViewMaster.AllowUserToAddRows = false;
            gridViewMaster.AllowUserToDeleteRows = false;
            gridViewMaster.AllowUserToResizeRows = false;
            gridViewMaster.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridViewMaster.ColumnHeadersHeight = 34;
            gridViewMaster.Dock = DockStyle.Fill;
            gridViewMaster.Location = new Point(0, 0);
            gridViewMaster.Name = "gridViewMaster";
            gridViewMaster.ReadOnly = true;
            gridViewMaster.RowHeadersVisible = false;
            gridViewMaster.RowHeadersWidth = 62;
            gridViewMaster.Size = new Size(240, 150);
            gridViewMaster.TabIndex = 0;
            // 
            // gridViewExpansion
            // 
            gridViewExpansion.AllowUserToAddRows = false;
            gridViewExpansion.AllowUserToDeleteRows = false;
            gridViewExpansion.AllowUserToResizeRows = false;
            gridViewExpansion.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridViewExpansion.ColumnHeadersHeight = 34;
            gridViewExpansion.Dock = DockStyle.Fill;
            gridViewExpansion.Location = new Point(0, 0);
            gridViewExpansion.Name = "gridViewExpansion";
            gridViewExpansion.ReadOnly = true;
            gridViewExpansion.RowHeadersVisible = false;
            gridViewExpansion.RowHeadersWidth = 62;
            gridViewExpansion.Size = new Size(240, 150);
            gridViewExpansion.TabIndex = 0;
            // 
            // MainForm
            // 
            ClientSize = new Size(1171, 925);
            Controls.Add(tableLayoutPanelMain);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);
            MainMenuStrip = menuStrip;
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "VacX OutSense System Controller";
            FormClosing += MainForm_FormClosing;
            Load += MainForm_Load;
            tableLayoutPanelMain.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            tabControlMain.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            panel1.ResumeLayout(false);
            grpPressureValve.ResumeLayout(false);
            tableLayoutPanel5.ResumeLayout(false);
            grpCh1Timer.ResumeLayout(false);
            grpCh1Timer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numVentTargetTemp).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Tolerance).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Seconds).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Minutes).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCh1Hours).EndInit();
            panel5.ResumeLayout(false);
            panel5.PerformLayout();
            grpChillerPID.ResumeLayout(false);
            grpChillerPID.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numCh2Target).EndInit();
            ((System.ComponentModel.ISupportInitialize)numChillerManualTemp).EndInit();
            panel3.ResumeLayout(false);
            tableLayoutPanel6.ResumeLayout(false);
            tableLayoutPanel6.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numTurboPumpSpeed).EndInit();
            ((System.ComponentModel.ISupportInitialize)numTurboPumpStandbySpeed).EndInit();
            panel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numDryPumpSpeed).EndInit();
            tabPage2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numCh1ReachCount).EndInit();
            ((System.ComponentModel.ISupportInitialize)numUpdateInterval).EndInit();
            ((System.ComponentModel.ISupportInitialize)numKd).EndInit();
            ((System.ComponentModel.ISupportInitialize)numKi).EndInit();
            ((System.ComponentModel.ISupportInitialize)numKp).EndInit();
            ((System.ComponentModel.ISupportInitialize)numChillerBase).EndInit();
            tabPageThermalRamp.ResumeLayout(false);
            menuStrip.ResumeLayout(false);
            menuStrip.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)gridViewMaster).EndInit();
            ((System.ComponentModel.ISupportInitialize)gridViewExpansion).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        #region UI 컨트롤 변수

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.DataGridView gridViewMaster;
        private System.Windows.Forms.DataGridView gridViewExpansion;

        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusConnection;

        private MenuStrip menuStrip;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuFileExit;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuHelpPatchNotes;
        private ToolStripMenuItem menuHelpAbout;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel toolStripStatusVersion;

        #endregion

        private NumericUpDown numCh1ReachCount;
        private Label lblCh1ReachCount;
        private TabControl tabControlMain;
        private TabPage tabPage1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private TableLayoutPanel tableLayoutPanel4;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_bathcirculator;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_tempcontroller;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_iomodule;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_drypump;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_turbopump;
        private Panel panel1;
        private GroupBox grpPressureValve;
        private GroupBox grpChillerPID;
        private Label lblLastOutputValue;
        private Label lblLastOutput;
        private Label lblPIDStatusValue;
        private Label lblPIDStatus;
        private Label lblSeconds;
        private NumericUpDown numUpdateInterval;
        private Label lblUpdateInterval;
        private GroupBox grpPIDParams;
        private NumericUpDown numKd;
        private Label lblKd;
        private NumericUpDown numKi;
        private Label lblKi;
        private NumericUpDown numKp;
        private Label lblKp;
        private NumericUpDown numChillerBase;
        private Label lblChillerBase;
        private NumericUpDown numCh2Target;
        private Label lblCh2Target;
        private ComboBox cmbPIDChannel;
        private Label lblPIDChannel;
        private CheckBox chkChillerPIDEnabled;
        private GroupBox grpCh1Timer;
        private Label lblEndAction;
        private RadioButton rdoFullShutdown;
        private RadioButton rdoHeaterOnly;
        private Label lblVentTempUnit;
        private NumericUpDown numVentTargetTemp;
        private Label lblVentTargetTemp;
        private Label lblCh1TimeRemainingValue;
        private Label lblCh1TimeRemaining;
        private CheckBox chkCh1TimerEnabled;
        private Label lblCh1Seconds;
        private Label lblCh1Minutes;
        private Label lblCh1Hours;
        private NumericUpDown numCh1Seconds;
        private NumericUpDown numCh1Minutes;
        private NumericUpDown numCh1Hours;
        // 자동 시작 컨트롤
        private CheckBox chkCh1AutoStartEnabled;
        private Label lblCh1TargetPressure;
        private NumericUpDown numCh1TargetPressure;
        private ScientificPressureInput pressureInputCh1;
        private Button btnCh1AutoStart;
        private Forms.UserControls.BindableTextBox txtIGStatus;
        private Forms.UserControls.BindableTextBox txtIG;
        private Forms.UserControls.BindableTextBox txtPG;
        private Forms.UserControls.BindableTextBox txtATM;
        private GroupBox panel5;
        private ComboBox cmbTempChannel;
        private Label lblTempControlHeader;
        private Button btnCh1Stop;
        private Button btnRampSetting;
        private Label label33;
        private Label label35;
        private Label label37;
        private TextBox txtCh1HeatingMV;
        private TextBox txtCh1Status;
        private Button btnCh1Start;
        private Label label41;
        private Label label38;
        private Label label40;
        private TextBox txtCh1PresentValue;
        private TextBox txtCh1SetValue;
        private TextBox txtCh2PresentValue;
        private Button btnCh1AutoTuning;
        private Button button2;
        private TextBox txtCh1IsAutotune;
        private Panel panel4;
        private TableLayoutPanel tableLayoutPanel7;
        private Button btnBathCirculatorSetTemp;
        private TextBox txtBathCirculatorTargetTemp;
        private TextBox txtBathCirculatorCurrentTemp;
        private Label lblChillerManualTemp;
        private NumericUpDown numChillerManualTemp;
        private Label label28;
        private Label label27;
        private Label label26;
        private Label label31;
        private Label label32;
        private Label lblBathCirculatorWarning;
        private TextBox txtBathCirculatorMode;
        private TextBox txtBathCirculatorTime;
        private Button btnBathCirculatorStart;
        private Button btnBathCirculatorSetTime;
        private Label label34;
        private TextBox txtBathCirculatorStatus;
        private Label label36;
        private GroupBox panel3;
        private TableLayoutPanel tableLayoutPanel6;
        private TextBox txtTurboPumpReady;
        private TextBox txtTurboPumpNormal;
        private Label label25;
        private Label label24;
        private TextBox txtTurboPumpRemote;
        private Label label23;
        private Label label22;
        private Label label21;
        private Label label20;
        private Label label19;
        private Label label18;
        private Label label17;
        private Label label16;
        private Label label15;
        private Label label14;
        private TextBox txtTurboPumpRunTime;
        private Label lblTurboPumpWarning;
        private TextBox txtTurboPumpBearingTemp;
        private TextBox txtTurboPumpElectronicsTemp;
        private TextBox txtTurboPumpMotorTemp;
        private TextBox txtTurboPumpCurrent;
        private TextBox txtTurboPumpSpeed;
        private Button btnTurboPumpStart;
        private Button btnTurboPumpVent;
        private Button btnTurboPumpReset;
        private Label lblTurboPumpSpeedHeader;
        private Label lblTurboPumpSpeedSet;
        private NumericUpDown numTurboPumpSpeed;
        private Button btnTurboPumpSpeedSet;
        private Button btnTurboPumpSpeedPerm;
        private Label lblTurboPumpStandby;
        private NumericUpDown numTurboPumpStandbySpeed;
        private Button btnTurboPumpStandbySet;
        private TextBox txtTurboPumpStatus;
        private GroupBox panel2;
        private TableLayoutPanel tableLayoutPanel1;
        private Label label13;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label lblDryPumpWarning;
        private TextBox txtDryPumpRunTime;
        private TextBox txtDryPumpMotorTemp;
        private TextBox txtDryPumpPower;
        private TextBox txtDryPumpCurrent;
        private TextBox txtDryPumpFrequency;
        private Button btnDryPumpStart;
        private Button btnDryPumpStandby;
        private Button btnDryPumpNormal;
        private Label lblDryPumpSpeedSet;
        private NumericUpDown numDryPumpSpeed;
        private Label lblDryPumpSpeedHeader;
        private Button btnDryPumpSpeedSet;
        private Button btnDryPumpSpeedStore;
        private TextBox txtDryPumpStatus;
        private Label lblDryPumpService;
        private Label label5;
        private TableLayoutPanel tableLayoutPanel5;
        private Button btn_GV;
        private Button btn_VV;
        private Button btn_EV;
        private Label label1;
        private Label label2;
        private Label label3;
        private Button btn_iongauge;
        private TabPage tabPage2;
        private RichTextBox txtLog;
        private TabPage tabPageAutoRun;
        private Label label29;
        private Label label30;
        private Label label42;
        private TextBox txtCh3PresentValue;
        private TextBox txtCh4PresentValue;
        private TextBox txtCh5PresentValue;
        private TextBox txtCh6PresentValue;
        private TextBox txtCh7PresentValue;
        private TextBox txtCh8PresentValue;
        private Label lblCh6Header;
        private Label lblCh7Header;
        private Label lblCh8Header;
        private ScientificPressureInput scientificPressureInput1;

        private TabPage tabPageThermalRamp;
        private Forms.UserControls.SimpleRampControl simpleRampControl1;
        private Button btnBakeoutSettings;
        private Button btnHoldSettings;
        private Label label43;
        private NumericUpDown numCh1Tolerance;
        private Label label44;
        private ProgressBar progressBarTurboPumpSpeed;
    }
}