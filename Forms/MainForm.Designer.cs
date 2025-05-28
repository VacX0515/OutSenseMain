using System.Windows.Forms;

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
            tabControlMain = new TabControl();
            tabPage1 = new TabPage();
            tableLayoutPanel2 = new TableLayoutPanel();
            tableLayoutPanel3 = new TableLayoutPanel();
            tableLayoutPanel4 = new TableLayoutPanel();
            connectionIndicator_bathcirculator = new Forms.UserControls.ConnectionIndicator();
            connectionIndicator_tempcontroller = new Forms.UserControls.ConnectionIndicator();
            connectionIndicator_iomodule = new Forms.UserControls.ConnectionIndicator();
            connectionIndicator_drypump = new Forms.UserControls.ConnectionIndicator();
            connectionIndicator_turbopump = new Forms.UserControls.ConnectionIndicator();
            panel1 = new Panel();
            txtIGStatus = new Forms.UserControls.BindableTextBox();
            txtIG = new Forms.UserControls.BindableTextBox();
            txtPG = new Forms.UserControls.BindableTextBox();
            txtATM = new Forms.UserControls.BindableTextBox();
            panel5 = new Panel();
            tableLayoutPanel8 = new TableLayoutPanel();
            btnCh2Start = new Button();
            btnCh1Stop = new Button();
            txtCh2HeatingMV = new TextBox();
            txtCh2Status = new TextBox();
            label29 = new Label();
            label30 = new Label();
            label33 = new Label();
            label35 = new Label();
            label37 = new Label();
            txtCh1HeatingMV = new TextBox();
            txtCh1Status = new TextBox();
            btnCh1Start = new Button();
            btnCh2Stop = new Button();
            label39 = new Label();
            label41 = new Label();
            label42 = new Label();
            label38 = new Label();
            label40 = new Label();
            label43 = new Label();
            txtCh1PresentValue = new TextBox();
            txtCh1SetValue = new TextBox();
            txtCh2PresentValue = new TextBox();
            txtCh2SetValue = new TextBox();
            btnCh1AutoTuning = new Button();
            button2 = new Button();
            btnCh2AutoTuning = new Button();
            button4 = new Button();
            txtCh2IsAutotune = new TextBox();
            txtCh1IsAutotune = new TextBox();
            panel4 = new Panel();
            tableLayoutPanel7 = new TableLayoutPanel();
            btnBathCirculatorSetTemp = new Button();
            btnBathCirculatorStop = new Button();
            txtBathCirculatorTargetTemp = new TextBox();
            txtBathCirculatorCurrentTemp = new TextBox();
            label28 = new Label();
            label27 = new Label();
            label26 = new Label();
            label31 = new Label();
            label32 = new Label();
            lblBathCirculatorWarning = new Label();
            txtBathCirculatorMode = new TextBox();
            txtBathCirculatorTime = new TextBox();
            btnBathCirculatorStart = new Button();
            btnBathCirculatorSetTime = new Button();
            label34 = new Label();
            txtBathCirculatorStatus = new TextBox();
            label36 = new Label();
            panel3 = new Panel();
            tableLayoutPanel6 = new TableLayoutPanel();
            txtTurboPumpReady = new TextBox();
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
            btnTurboPumpStop = new Button();
            btnTurboPumpVent = new Button();
            btnTurboPumpReset = new Button();
            label6 = new Label();
            txtTurboPumpStatus = new TextBox();
            txtTurboPumpingRate = new TextBox();
            panel2 = new Panel();
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
            btnDryPumpStop = new Button();
            btnDryPumpStandby = new Button();
            btnDryPumpNormal = new Button();
            label4 = new Label();
            txtDryPumpStatus = new TextBox();
            lblDryPumpService = new Label();
            label5 = new Label();
            tableLayoutPanel5 = new TableLayoutPanel();
            btn_GV = new Button();
            btn_VV = new Button();
            btn_EV = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            btn_iongauge = new Button();
            tabPage2 = new TabPage();
            txtLog = new RichTextBox();
            menuStrip = new MenuStrip();
            menuFile = new ToolStripMenuItem();
            menuFileExit = new ToolStripMenuItem();
            menuComm = new ToolStripMenuItem();
            menuCommSettings = new ToolStripMenuItem();
            menuHelp = new ToolStripMenuItem();
            menuHelpAbout = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            toolStripStatusConnection = new ToolStripStatusLabel();
            gridViewMaster = new DataGridView();
            gridViewExpansion = new DataGridView();
            tableLayoutPanelMain.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPage1.SuspendLayout();
            tableLayoutPanel2.SuspendLayout();
            tableLayoutPanel3.SuspendLayout();
            tableLayoutPanel4.SuspendLayout();
            panel1.SuspendLayout();
            panel5.SuspendLayout();
            tableLayoutPanel8.SuspendLayout();
            panel4.SuspendLayout();
            tableLayoutPanel7.SuspendLayout();
            panel3.SuspendLayout();
            tableLayoutPanel6.SuspendLayout();
            panel2.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel5.SuspendLayout();
            tabPage2.SuspendLayout();
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
            tableLayoutPanelMain.Controls.Add(tabControlMain, 0, 0);
            tableLayoutPanelMain.Dock = DockStyle.Fill;
            tableLayoutPanelMain.Location = new Point(0, 24);
            tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            tableLayoutPanelMain.RowCount = 1;
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanelMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanelMain.Size = new Size(1186, 683);
            tableLayoutPanelMain.TabIndex = 0;
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabPage1);
            tabControlMain.Controls.Add(tabPage2);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Location = new Point(3, 3);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(1180, 677);
            tabControlMain.TabIndex = 1;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(tableLayoutPanel2);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1172, 649);
            tabPage1.TabIndex = 2;
            tabPage1.Text = "Main";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel2
            // 
            tableLayoutPanel2.ColumnCount = 1;
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel2.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel2.Controls.Add(tableLayoutPanel3, 0, 0);
            tableLayoutPanel2.Controls.Add(panel1, 0, 1);
            tableLayoutPanel2.Dock = DockStyle.Fill;
            tableLayoutPanel2.Location = new Point(3, 3);
            tableLayoutPanel2.Name = "tableLayoutPanel2";
            tableLayoutPanel2.RowCount = 2;
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 10F));
            tableLayoutPanel2.RowStyles.Add(new RowStyle(SizeType.Percent, 90F));
            tableLayoutPanel2.Size = new Size(1166, 643);
            tableLayoutPanel2.TabIndex = 0;
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
            tableLayoutPanel3.Size = new Size(1160, 58);
            tableLayoutPanel3.TabIndex = 0;
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
            tableLayoutPanel4.Location = new Point(699, 3);
            tableLayoutPanel4.Name = "tableLayoutPanel4";
            tableLayoutPanel4.RowCount = 1;
            tableLayoutPanel4.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel4.Size = new Size(458, 52);
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
            connectionIndicator_bathcirculator.Location = new Point(367, 4);
            connectionIndicator_bathcirculator.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_bathcirculator.Name = "connectionIndicator_bathcirculator";
            connectionIndicator_bathcirculator.Size = new Size(70, 44);
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
            connectionIndicator_tempcontroller.Location = new Point(276, 4);
            connectionIndicator_tempcontroller.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_tempcontroller.Name = "connectionIndicator_tempcontroller";
            connectionIndicator_tempcontroller.Size = new Size(70, 44);
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
            connectionIndicator_iomodule.Location = new Point(185, 4);
            connectionIndicator_iomodule.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_iomodule.Name = "connectionIndicator_iomodule";
            connectionIndicator_iomodule.Size = new Size(70, 44);
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
            connectionIndicator_drypump.Location = new Point(94, 4);
            connectionIndicator_drypump.Margin = new Padding(3, 4, 3, 4);
            connectionIndicator_drypump.Name = "connectionIndicator_drypump";
            connectionIndicator_drypump.Size = new Size(70, 44);
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
            connectionIndicator_turbopump.Size = new Size(70, 44);
            connectionIndicator_turbopump.TabIndex = 0;
            // 
            // panel1
            // 
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Controls.Add(txtIGStatus);
            panel1.Controls.Add(txtIG);
            panel1.Controls.Add(txtPG);
            panel1.Controls.Add(txtATM);
            panel1.Controls.Add(panel5);
            panel1.Controls.Add(panel4);
            panel1.Controls.Add(panel3);
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(tableLayoutPanel5);
            panel1.Controls.Add(btn_iongauge);
            panel1.Dock = DockStyle.Fill;
            panel1.Location = new Point(3, 67);
            panel1.Name = "panel1";
            panel1.Size = new Size(1160, 573);
            panel1.TabIndex = 1;
            // 
            // txtIGStatus
            // 
            txtIGStatus.DataMember = null;
            txtIGStatus.DataSource = null;
            txtIGStatus.FormatString = null;
            txtIGStatus.IsReadOnly = false;
            txtIGStatus.LabelText = "IG status";
            txtIGStatus.Location = new Point(810, 55);
            txtIGStatus.Name = "txtIGStatus";
            txtIGStatus.Padding = new Padding(0, 0, 0, 3);
            txtIGStatus.Size = new Size(250, 32);
            txtIGStatus.TabIndex = 19;
            txtIGStatus.TextValue = "";
            // 
            // txtIG
            // 
            txtIG.DataMember = null;
            txtIG.DataSource = null;
            txtIG.FormatString = null;
            txtIG.IsReadOnly = false;
            txtIG.LabelText = "IG(Torr)";
            txtIG.Location = new Point(809, 19);
            txtIG.Name = "txtIG";
            txtIG.Padding = new Padding(0, 0, 0, 3);
            txtIG.Size = new Size(250, 32);
            txtIG.TabIndex = 18;
            txtIG.TextValue = "";
            // 
            // txtPG
            // 
            txtPG.DataMember = null;
            txtPG.DataSource = null;
            txtPG.FormatString = null;
            txtPG.IsReadOnly = false;
            txtPG.LabelText = "PG(Torr)";
            txtPG.Location = new Point(531, 19);
            txtPG.Name = "txtPG";
            txtPG.Padding = new Padding(0, 0, 0, 3);
            txtPG.Size = new Size(250, 32);
            txtPG.TabIndex = 17;
            txtPG.TextValue = "";
            // 
            // txtATM
            // 
            txtATM.DataMember = null;
            txtATM.DataSource = null;
            txtATM.FormatString = null;
            txtATM.IsReadOnly = false;
            txtATM.LabelText = "ATM(kPa)";
            txtATM.Location = new Point(249, 19);
            txtATM.Name = "txtATM";
            txtATM.Padding = new Padding(0, 0, 0, 3);
            txtATM.Size = new Size(250, 32);
            txtATM.TabIndex = 16;
            txtATM.TextValue = "";
            // 
            // panel5
            // 
            panel5.BorderStyle = BorderStyle.FixedSingle;
            panel5.Controls.Add(tableLayoutPanel8);
            panel5.Location = new Point(660, 203);
            panel5.Name = "panel5";
            panel5.Size = new Size(464, 165);
            panel5.TabIndex = 15;
            // 
            // tableLayoutPanel8
            // 
            tableLayoutPanel8.ColumnCount = 7;
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666641F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666679F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666679F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666679F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666679F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6666641F));
            tableLayoutPanel8.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20F));
            tableLayoutPanel8.Controls.Add(btnCh2Start, 5, 1);
            tableLayoutPanel8.Controls.Add(btnCh1Stop, 2, 1);
            tableLayoutPanel8.Controls.Add(txtCh2HeatingMV, 6, 3);
            tableLayoutPanel8.Controls.Add(txtCh2Status, 5, 3);
            tableLayoutPanel8.Controls.Add(label29, 6, 2);
            tableLayoutPanel8.Controls.Add(label30, 5, 2);
            tableLayoutPanel8.Controls.Add(label33, 2, 4);
            tableLayoutPanel8.Controls.Add(label35, 2, 2);
            tableLayoutPanel8.Controls.Add(label37, 1, 2);
            tableLayoutPanel8.Controls.Add(txtCh1HeatingMV, 2, 3);
            tableLayoutPanel8.Controls.Add(txtCh1Status, 1, 3);
            tableLayoutPanel8.Controls.Add(btnCh1Start, 1, 1);
            tableLayoutPanel8.Controls.Add(btnCh2Stop, 6, 1);
            tableLayoutPanel8.Controls.Add(label39, 0, 1);
            tableLayoutPanel8.Controls.Add(label41, 1, 0);
            tableLayoutPanel8.Controls.Add(label42, 5, 0);
            tableLayoutPanel8.Controls.Add(label38, 1, 4);
            tableLayoutPanel8.Controls.Add(label40, 5, 4);
            tableLayoutPanel8.Controls.Add(label43, 6, 4);
            tableLayoutPanel8.Controls.Add(txtCh1PresentValue, 1, 5);
            tableLayoutPanel8.Controls.Add(txtCh1SetValue, 2, 5);
            tableLayoutPanel8.Controls.Add(txtCh2PresentValue, 5, 5);
            tableLayoutPanel8.Controls.Add(txtCh2SetValue, 6, 5);
            tableLayoutPanel8.Controls.Add(btnCh1AutoTuning, 0, 3);
            tableLayoutPanel8.Controls.Add(button2, 0, 5);
            tableLayoutPanel8.Controls.Add(btnCh2AutoTuning, 4, 3);
            tableLayoutPanel8.Controls.Add(button4, 4, 5);
            tableLayoutPanel8.Controls.Add(txtCh2IsAutotune, 4, 2);
            tableLayoutPanel8.Controls.Add(txtCh1IsAutotune, 0, 2);
            tableLayoutPanel8.Dock = DockStyle.Fill;
            tableLayoutPanel8.Location = new Point(0, 0);
            tableLayoutPanel8.Name = "tableLayoutPanel8";
            tableLayoutPanel8.RowCount = 6;
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel8.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel8.Size = new Size(462, 163);
            tableLayoutPanel8.TabIndex = 11;
            // 
            // btnCh2Start
            // 
            btnCh2Start.Dock = DockStyle.Fill;
            btnCh2Start.Location = new Point(315, 23);
            btnCh2Start.Name = "btnCh2Start";
            btnCh2Start.Size = new Size(67, 37);
            btnCh2Start.TabIndex = 26;
            btnCh2Start.Text = "Start";
            btnCh2Start.UseVisualStyleBackColor = true;
            btnCh2Start.Click += btnCh2Start_Click;
            // 
            // btnCh1Stop
            // 
            btnCh1Stop.Dock = DockStyle.Fill;
            btnCh1Stop.Location = new Point(149, 23);
            btnCh1Stop.Name = "btnCh1Stop";
            btnCh1Stop.Size = new Size(67, 37);
            btnCh1Stop.TabIndex = 25;
            btnCh1Stop.Text = "Stop";
            btnCh1Stop.UseVisualStyleBackColor = true;
            btnCh1Stop.Click += btnCh1Stop_Click;
            // 
            // txtCh2HeatingMV
            // 
            txtCh2HeatingMV.Location = new Point(388, 86);
            txtCh2HeatingMV.Name = "txtCh2HeatingMV";
            txtCh2HeatingMV.ReadOnly = true;
            txtCh2HeatingMV.Size = new Size(71, 23);
            txtCh2HeatingMV.TabIndex = 24;
            txtCh2HeatingMV.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh2Status
            // 
            txtCh2Status.Location = new Point(315, 86);
            txtCh2Status.Name = "txtCh2Status";
            txtCh2Status.ReadOnly = true;
            txtCh2Status.Size = new Size(67, 23);
            txtCh2Status.TabIndex = 23;
            txtCh2Status.TextAlign = HorizontalAlignment.Center;
            // 
            // label29
            // 
            label29.AutoSize = true;
            label29.BackColor = SystemColors.Info;
            label29.BorderStyle = BorderStyle.FixedSingle;
            label29.Dock = DockStyle.Fill;
            label29.Location = new Point(388, 63);
            label29.Name = "label29";
            label29.Size = new Size(71, 20);
            label29.TabIndex = 22;
            label29.Text = "가열량";
            label29.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label30
            // 
            label30.AutoSize = true;
            label30.BackColor = SystemColors.Info;
            label30.BorderStyle = BorderStyle.FixedSingle;
            label30.Dock = DockStyle.Fill;
            label30.Location = new Point(315, 63);
            label30.Name = "label30";
            label30.Size = new Size(67, 20);
            label30.TabIndex = 21;
            label30.Text = "상태";
            label30.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label33
            // 
            label33.AutoSize = true;
            label33.BackColor = SystemColors.Info;
            label33.BorderStyle = BorderStyle.FixedSingle;
            label33.Dock = DockStyle.Fill;
            label33.Location = new Point(149, 113);
            label33.Name = "label33";
            label33.Size = new Size(67, 20);
            label33.TabIndex = 20;
            label33.Text = "SV";
            label33.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label35
            // 
            label35.AutoSize = true;
            label35.BackColor = SystemColors.Info;
            label35.BorderStyle = BorderStyle.FixedSingle;
            label35.Dock = DockStyle.Fill;
            label35.Location = new Point(149, 63);
            label35.Name = "label35";
            label35.Size = new Size(67, 20);
            label35.TabIndex = 15;
            label35.Text = "가열량";
            label35.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label37
            // 
            label37.AutoSize = true;
            label37.BackColor = SystemColors.Info;
            label37.BorderStyle = BorderStyle.FixedSingle;
            label37.Dock = DockStyle.Fill;
            label37.Location = new Point(76, 63);
            label37.Name = "label37";
            label37.Size = new Size(67, 20);
            label37.TabIndex = 14;
            label37.Text = "상태";
            label37.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtCh1HeatingMV
            // 
            txtCh1HeatingMV.Location = new Point(149, 86);
            txtCh1HeatingMV.Name = "txtCh1HeatingMV";
            txtCh1HeatingMV.ReadOnly = true;
            txtCh1HeatingMV.Size = new Size(67, 23);
            txtCh1HeatingMV.TabIndex = 7;
            txtCh1HeatingMV.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1Status
            // 
            txtCh1Status.Dock = DockStyle.Fill;
            txtCh1Status.Location = new Point(76, 86);
            txtCh1Status.Name = "txtCh1Status";
            txtCh1Status.ReadOnly = true;
            txtCh1Status.Size = new Size(67, 23);
            txtCh1Status.TabIndex = 6;
            txtCh1Status.TextAlign = HorizontalAlignment.Center;
            // 
            // btnCh1Start
            // 
            btnCh1Start.Dock = DockStyle.Fill;
            btnCh1Start.Location = new Point(76, 23);
            btnCh1Start.Name = "btnCh1Start";
            btnCh1Start.Size = new Size(67, 37);
            btnCh1Start.TabIndex = 0;
            btnCh1Start.Text = "Start";
            btnCh1Start.UseVisualStyleBackColor = true;
            btnCh1Start.Click += btnCh1Start_Click;
            // 
            // btnCh2Stop
            // 
            btnCh2Stop.Dock = DockStyle.Fill;
            btnCh2Stop.Location = new Point(388, 23);
            btnCh2Stop.Name = "btnCh2Stop";
            btnCh2Stop.Size = new Size(71, 37);
            btnCh2Stop.TabIndex = 3;
            btnCh2Stop.Text = "Stop";
            btnCh2Stop.UseVisualStyleBackColor = true;
            btnCh2Stop.Click += btnCh2Stop_Click;
            // 
            // label39
            // 
            label39.AutoSize = true;
            label39.BorderStyle = BorderStyle.FixedSingle;
            label39.Dock = DockStyle.Fill;
            label39.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label39.Location = new Point(3, 20);
            label39.Name = "label39";
            label39.Size = new Size(67, 43);
            label39.TabIndex = 4;
            label39.Text = "PID";
            label39.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label41
            // 
            label41.AutoSize = true;
            label41.BackColor = SystemColors.Info;
            label41.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel8.SetColumnSpan(label41, 2);
            label41.Dock = DockStyle.Fill;
            label41.Location = new Point(76, 0);
            label41.Name = "label41";
            label41.Size = new Size(140, 20);
            label41.TabIndex = 27;
            label41.Text = "Ch.1";
            label41.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label42
            // 
            label42.AutoSize = true;
            label42.BackColor = SystemColors.Info;
            label42.BorderStyle = BorderStyle.FixedSingle;
            tableLayoutPanel8.SetColumnSpan(label42, 2);
            label42.Dock = DockStyle.Fill;
            label42.Location = new Point(315, 0);
            label42.Name = "label42";
            label42.Size = new Size(144, 20);
            label42.TabIndex = 28;
            label42.Text = "Ch.2";
            label42.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label38
            // 
            label38.AutoSize = true;
            label38.BackColor = SystemColors.Info;
            label38.BorderStyle = BorderStyle.FixedSingle;
            label38.Dock = DockStyle.Fill;
            label38.Location = new Point(76, 113);
            label38.Name = "label38";
            label38.Size = new Size(67, 20);
            label38.TabIndex = 29;
            label38.Text = "PV";
            label38.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label40
            // 
            label40.AutoSize = true;
            label40.BackColor = SystemColors.Info;
            label40.BorderStyle = BorderStyle.FixedSingle;
            label40.Dock = DockStyle.Fill;
            label40.Location = new Point(315, 113);
            label40.Name = "label40";
            label40.Size = new Size(67, 20);
            label40.TabIndex = 30;
            label40.Text = "PV";
            label40.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label43
            // 
            label43.AutoSize = true;
            label43.BackColor = SystemColors.Info;
            label43.BorderStyle = BorderStyle.FixedSingle;
            label43.Dock = DockStyle.Fill;
            label43.Location = new Point(388, 113);
            label43.Name = "label43";
            label43.Size = new Size(71, 20);
            label43.TabIndex = 31;
            label43.Text = "SV";
            label43.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtCh1PresentValue
            // 
            txtCh1PresentValue.Dock = DockStyle.Fill;
            txtCh1PresentValue.Location = new Point(76, 136);
            txtCh1PresentValue.Name = "txtCh1PresentValue";
            txtCh1PresentValue.ReadOnly = true;
            txtCh1PresentValue.Size = new Size(67, 23);
            txtCh1PresentValue.TabIndex = 32;
            txtCh1PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1SetValue
            // 
            txtCh1SetValue.Dock = DockStyle.Fill;
            txtCh1SetValue.Location = new Point(149, 136);
            txtCh1SetValue.Name = "txtCh1SetValue";
            txtCh1SetValue.ReadOnly = true;
            txtCh1SetValue.Size = new Size(67, 23);
            txtCh1SetValue.TabIndex = 33;
            txtCh1SetValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh2PresentValue
            // 
            txtCh2PresentValue.Dock = DockStyle.Fill;
            txtCh2PresentValue.Location = new Point(315, 136);
            txtCh2PresentValue.Name = "txtCh2PresentValue";
            txtCh2PresentValue.ReadOnly = true;
            txtCh2PresentValue.Size = new Size(67, 23);
            txtCh2PresentValue.TabIndex = 34;
            txtCh2PresentValue.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh2SetValue
            // 
            txtCh2SetValue.Dock = DockStyle.Fill;
            txtCh2SetValue.Location = new Point(388, 136);
            txtCh2SetValue.Name = "txtCh2SetValue";
            txtCh2SetValue.ReadOnly = true;
            txtCh2SetValue.Size = new Size(71, 23);
            txtCh2SetValue.TabIndex = 35;
            txtCh2SetValue.TextAlign = HorizontalAlignment.Center;
            // 
            // btnCh1AutoTuning
            // 
            btnCh1AutoTuning.Dock = DockStyle.Fill;
            btnCh1AutoTuning.Location = new Point(3, 86);
            btnCh1AutoTuning.Name = "btnCh1AutoTuning";
            btnCh1AutoTuning.Size = new Size(67, 24);
            btnCh1AutoTuning.TabIndex = 36;
            btnCh1AutoTuning.Text = "AutoTune";
            btnCh1AutoTuning.UseVisualStyleBackColor = true;
            btnCh1AutoTuning.Click += btnCh1AutoTuning_Click;
            // 
            // button2
            // 
            button2.Dock = DockStyle.Fill;
            button2.Location = new Point(3, 136);
            button2.Name = "button2";
            button2.Size = new Size(67, 24);
            button2.TabIndex = 37;
            button2.Text = "SetTemp";
            button2.UseVisualStyleBackColor = true;
            button2.Click += btnCh1SetTemp_Click;
            // 
            // btnCh2AutoTuning
            // 
            btnCh2AutoTuning.Dock = DockStyle.Fill;
            btnCh2AutoTuning.Location = new Point(242, 86);
            btnCh2AutoTuning.Name = "btnCh2AutoTuning";
            btnCh2AutoTuning.Size = new Size(67, 24);
            btnCh2AutoTuning.TabIndex = 38;
            btnCh2AutoTuning.Text = "AutoTune";
            btnCh2AutoTuning.UseVisualStyleBackColor = true;
            btnCh2AutoTuning.Click += btnCh2AutoTuning_Click;
            // 
            // button4
            // 
            button4.Dock = DockStyle.Fill;
            button4.Location = new Point(242, 136);
            button4.Name = "button4";
            button4.Size = new Size(67, 24);
            button4.TabIndex = 39;
            button4.Text = "SetTemp";
            button4.UseVisualStyleBackColor = true;
            button4.Click += btnCh2SetTemp_Click;
            // 
            // txtCh2IsAutotune
            // 
            txtCh2IsAutotune.Dock = DockStyle.Fill;
            txtCh2IsAutotune.Location = new Point(242, 66);
            txtCh2IsAutotune.Name = "txtCh2IsAutotune";
            txtCh2IsAutotune.ReadOnly = true;
            txtCh2IsAutotune.Size = new Size(67, 23);
            txtCh2IsAutotune.TabIndex = 41;
            txtCh2IsAutotune.TextAlign = HorizontalAlignment.Center;
            // 
            // txtCh1IsAutotune
            // 
            txtCh1IsAutotune.Dock = DockStyle.Fill;
            txtCh1IsAutotune.Location = new Point(3, 66);
            txtCh1IsAutotune.Name = "txtCh1IsAutotune";
            txtCh1IsAutotune.ReadOnly = true;
            txtCh1IsAutotune.Size = new Size(67, 23);
            txtCh1IsAutotune.TabIndex = 40;
            txtCh1IsAutotune.TextAlign = HorizontalAlignment.Center;
            // 
            // panel4
            // 
            panel4.BorderStyle = BorderStyle.FixedSingle;
            panel4.Controls.Add(tableLayoutPanel7);
            panel4.Location = new Point(659, 392);
            panel4.Name = "panel4";
            panel4.Size = new Size(464, 165);
            panel4.TabIndex = 14;
            // 
            // tableLayoutPanel7
            // 
            tableLayoutPanel7.ColumnCount = 5;
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel7.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tableLayoutPanel7.Controls.Add(btnBathCirculatorSetTemp, 3, 0);
            tableLayoutPanel7.Controls.Add(btnBathCirculatorStop, 2, 0);
            tableLayoutPanel7.Controls.Add(txtBathCirculatorTargetTemp, 4, 2);
            tableLayoutPanel7.Controls.Add(txtBathCirculatorCurrentTemp, 3, 2);
            tableLayoutPanel7.Controls.Add(label28, 4, 1);
            tableLayoutPanel7.Controls.Add(label27, 3, 1);
            tableLayoutPanel7.Controls.Add(label26, 2, 3);
            tableLayoutPanel7.Controls.Add(label31, 2, 1);
            tableLayoutPanel7.Controls.Add(label32, 1, 1);
            tableLayoutPanel7.Controls.Add(lblBathCirculatorWarning, 2, 4);
            tableLayoutPanel7.Controls.Add(txtBathCirculatorMode, 2, 2);
            tableLayoutPanel7.Controls.Add(txtBathCirculatorTime, 1, 2);
            tableLayoutPanel7.Controls.Add(btnBathCirculatorStart, 1, 0);
            tableLayoutPanel7.Controls.Add(btnBathCirculatorSetTime, 4, 0);
            tableLayoutPanel7.Controls.Add(label34, 0, 0);
            tableLayoutPanel7.Controls.Add(txtBathCirculatorStatus, 0, 2);
            tableLayoutPanel7.Controls.Add(label36, 0, 1);
            tableLayoutPanel7.Dock = DockStyle.Fill;
            tableLayoutPanel7.Location = new Point(0, 0);
            tableLayoutPanel7.Name = "tableLayoutPanel7";
            tableLayoutPanel7.RowCount = 5;
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel7.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel7.Size = new Size(462, 163);
            tableLayoutPanel7.TabIndex = 11;
            // 
            // btnBathCirculatorSetTemp
            // 
            btnBathCirculatorSetTemp.Dock = DockStyle.Fill;
            btnBathCirculatorSetTemp.Location = new Point(279, 3);
            btnBathCirculatorSetTemp.Name = "btnBathCirculatorSetTemp";
            btnBathCirculatorSetTemp.Size = new Size(86, 57);
            btnBathCirculatorSetTemp.TabIndex = 26;
            btnBathCirculatorSetTemp.Text = "SetTemp";
            btnBathCirculatorSetTemp.UseVisualStyleBackColor = true;
            btnBathCirculatorSetTemp.Click += btnBathCirculatorSetTemp_Click;
            // 
            // btnBathCirculatorStop
            // 
            btnBathCirculatorStop.Dock = DockStyle.Fill;
            btnBathCirculatorStop.Location = new Point(187, 3);
            btnBathCirculatorStop.Name = "btnBathCirculatorStop";
            btnBathCirculatorStop.Size = new Size(86, 57);
            btnBathCirculatorStop.TabIndex = 25;
            btnBathCirculatorStop.Text = "Stop";
            btnBathCirculatorStop.UseVisualStyleBackColor = true;
            btnBathCirculatorStop.Click += btnBathCirculatorStop_Click;
            // 
            // txtBathCirculatorTargetTemp
            // 
            txtBathCirculatorTargetTemp.Location = new Point(371, 86);
            txtBathCirculatorTargetTemp.Name = "txtBathCirculatorTargetTemp";
            txtBathCirculatorTargetTemp.ReadOnly = true;
            txtBathCirculatorTargetTemp.Size = new Size(86, 23);
            txtBathCirculatorTargetTemp.TabIndex = 24;
            txtBathCirculatorTargetTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtBathCirculatorCurrentTemp
            // 
            txtBathCirculatorCurrentTemp.Location = new Point(279, 86);
            txtBathCirculatorCurrentTemp.Name = "txtBathCirculatorCurrentTemp";
            txtBathCirculatorCurrentTemp.ReadOnly = true;
            txtBathCirculatorCurrentTemp.Size = new Size(86, 23);
            txtBathCirculatorCurrentTemp.TabIndex = 23;
            txtBathCirculatorCurrentTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // label28
            // 
            label28.AutoSize = true;
            label28.BackColor = SystemColors.Info;
            label28.BorderStyle = BorderStyle.FixedSingle;
            label28.Dock = DockStyle.Fill;
            label28.Location = new Point(371, 63);
            label28.Name = "label28";
            label28.Size = new Size(88, 20);
            label28.TabIndex = 22;
            label28.Text = "설정온도";
            label28.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label27
            // 
            label27.AutoSize = true;
            label27.BackColor = SystemColors.Info;
            label27.BorderStyle = BorderStyle.FixedSingle;
            label27.Dock = DockStyle.Fill;
            label27.Location = new Point(279, 63);
            label27.Name = "label27";
            label27.Size = new Size(86, 20);
            label27.TabIndex = 21;
            label27.Text = "현재온도";
            label27.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label26
            // 
            label26.AutoSize = true;
            label26.BackColor = SystemColors.Info;
            label26.BorderStyle = BorderStyle.FixedSingle;
            label26.Dock = DockStyle.Fill;
            label26.Location = new Point(187, 113);
            label26.Name = "label26";
            label26.Size = new Size(86, 20);
            label26.TabIndex = 20;
            label26.Text = "경고";
            label26.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label31
            // 
            label31.AutoSize = true;
            label31.BackColor = SystemColors.Info;
            label31.BorderStyle = BorderStyle.FixedSingle;
            label31.Dock = DockStyle.Fill;
            label31.Location = new Point(187, 63);
            label31.Name = "label31";
            label31.Size = new Size(86, 20);
            label31.TabIndex = 15;
            label31.Text = "모드";
            label31.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label32
            // 
            label32.AutoSize = true;
            label32.BackColor = SystemColors.Info;
            label32.BorderStyle = BorderStyle.FixedSingle;
            label32.Dock = DockStyle.Fill;
            label32.Location = new Point(95, 63);
            label32.Name = "label32";
            label32.Size = new Size(86, 20);
            label32.TabIndex = 14;
            label32.Text = "시간";
            label32.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblBathCirculatorWarning
            // 
            lblBathCirculatorWarning.AutoSize = true;
            lblBathCirculatorWarning.Dock = DockStyle.Fill;
            lblBathCirculatorWarning.Location = new Point(187, 133);
            lblBathCirculatorWarning.Name = "lblBathCirculatorWarning";
            lblBathCirculatorWarning.Size = new Size(86, 30);
            lblBathCirculatorWarning.TabIndex = 12;
            lblBathCirculatorWarning.Text = "-";
            // 
            // txtBathCirculatorMode
            // 
            txtBathCirculatorMode.Location = new Point(187, 86);
            txtBathCirculatorMode.Name = "txtBathCirculatorMode";
            txtBathCirculatorMode.ReadOnly = true;
            txtBathCirculatorMode.Size = new Size(86, 23);
            txtBathCirculatorMode.TabIndex = 7;
            txtBathCirculatorMode.TextAlign = HorizontalAlignment.Center;
            // 
            // txtBathCirculatorTime
            // 
            txtBathCirculatorTime.Dock = DockStyle.Fill;
            txtBathCirculatorTime.Location = new Point(95, 86);
            txtBathCirculatorTime.Name = "txtBathCirculatorTime";
            txtBathCirculatorTime.ReadOnly = true;
            txtBathCirculatorTime.Size = new Size(86, 23);
            txtBathCirculatorTime.TabIndex = 6;
            txtBathCirculatorTime.TextAlign = HorizontalAlignment.Center;
            // 
            // btnBathCirculatorStart
            // 
            btnBathCirculatorStart.Dock = DockStyle.Fill;
            btnBathCirculatorStart.Location = new Point(95, 3);
            btnBathCirculatorStart.Name = "btnBathCirculatorStart";
            btnBathCirculatorStart.Size = new Size(86, 57);
            btnBathCirculatorStart.TabIndex = 0;
            btnBathCirculatorStart.Text = "Start";
            btnBathCirculatorStart.UseVisualStyleBackColor = true;
            btnBathCirculatorStart.Click += btnBathCirculatorStart_Click;
            // 
            // btnBathCirculatorSetTime
            // 
            btnBathCirculatorSetTime.Dock = DockStyle.Fill;
            btnBathCirculatorSetTime.Location = new Point(371, 3);
            btnBathCirculatorSetTime.Name = "btnBathCirculatorSetTime";
            btnBathCirculatorSetTime.Size = new Size(88, 57);
            btnBathCirculatorSetTime.TabIndex = 3;
            btnBathCirculatorSetTime.Text = "SetTime";
            btnBathCirculatorSetTime.UseVisualStyleBackColor = true;
            btnBathCirculatorSetTime.Click += btnBathCirculatorSetTime_Click;
            // 
            // label34
            // 
            label34.AutoSize = true;
            label34.BorderStyle = BorderStyle.FixedSingle;
            label34.Dock = DockStyle.Fill;
            label34.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label34.Location = new Point(3, 0);
            label34.Name = "label34";
            label34.Size = new Size(86, 63);
            label34.TabIndex = 4;
            label34.Text = "Chiller";
            label34.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtBathCirculatorStatus
            // 
            txtBathCirculatorStatus.Dock = DockStyle.Fill;
            txtBathCirculatorStatus.Location = new Point(3, 86);
            txtBathCirculatorStatus.Name = "txtBathCirculatorStatus";
            txtBathCirculatorStatus.ReadOnly = true;
            txtBathCirculatorStatus.Size = new Size(86, 23);
            txtBathCirculatorStatus.TabIndex = 5;
            txtBathCirculatorStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // label36
            // 
            label36.AutoSize = true;
            label36.BackColor = SystemColors.Info;
            label36.BorderStyle = BorderStyle.FixedSingle;
            label36.Dock = DockStyle.Fill;
            label36.Location = new Point(3, 63);
            label36.Name = "label36";
            label36.Size = new Size(86, 20);
            label36.TabIndex = 13;
            label36.Text = "상태";
            label36.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel3
            // 
            panel3.BorderStyle = BorderStyle.FixedSingle;
            panel3.Controls.Add(tableLayoutPanel6);
            panel3.Location = new Point(34, 389);
            panel3.Name = "panel3";
            panel3.Size = new Size(537, 168);
            panel3.TabIndex = 13;
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
            tableLayoutPanel6.Controls.Add(btnTurboPumpStart, 1, 0);
            tableLayoutPanel6.Controls.Add(btnTurboPumpStop, 2, 0);
            tableLayoutPanel6.Controls.Add(btnTurboPumpVent, 3, 0);
            tableLayoutPanel6.Controls.Add(btnTurboPumpReset, 4, 0);
            tableLayoutPanel6.Controls.Add(label6, 0, 0);
            tableLayoutPanel6.Controls.Add(txtTurboPumpStatus, 0, 2);
            tableLayoutPanel6.Controls.Add(txtTurboPumpingRate, 3, 4);
            tableLayoutPanel6.Dock = DockStyle.Fill;
            tableLayoutPanel6.Location = new Point(0, 0);
            tableLayoutPanel6.Name = "tableLayoutPanel6";
            tableLayoutPanel6.RowCount = 5;
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel6.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel6.Size = new Size(535, 166);
            tableLayoutPanel6.TabIndex = 11;
            // 
            // txtTurboPumpReady
            // 
            txtTurboPumpReady.Dock = DockStyle.Fill;
            txtTurboPumpReady.Location = new Point(448, 139);
            txtTurboPumpReady.Name = "txtTurboPumpReady";
            txtTurboPumpReady.ReadOnly = true;
            txtTurboPumpReady.Size = new Size(84, 23);
            txtTurboPumpReady.TabIndex = 30;
            txtTurboPumpReady.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpNormal
            // 
            txtTurboPumpNormal.Dock = DockStyle.Fill;
            txtTurboPumpNormal.Location = new Point(359, 139);
            txtTurboPumpNormal.Name = "txtTurboPumpNormal";
            txtTurboPumpNormal.ReadOnly = true;
            txtTurboPumpNormal.Size = new Size(83, 23);
            txtTurboPumpNormal.TabIndex = 29;
            txtTurboPumpNormal.TextAlign = HorizontalAlignment.Center;
            // 
            // label25
            // 
            label25.AutoSize = true;
            label25.BackColor = SystemColors.Info;
            label25.BorderStyle = BorderStyle.FixedSingle;
            label25.Dock = DockStyle.Fill;
            label25.Location = new Point(448, 116);
            label25.Name = "label25";
            label25.Size = new Size(84, 20);
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
            label24.Location = new Point(359, 116);
            label24.Name = "label24";
            label24.Size = new Size(83, 20);
            label24.TabIndex = 27;
            label24.Text = "Normal";
            label24.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtTurboPumpRemote
            // 
            txtTurboPumpRemote.Dock = DockStyle.Fill;
            txtTurboPumpRemote.Location = new Point(448, 89);
            txtTurboPumpRemote.Name = "txtTurboPumpRemote";
            txtTurboPumpRemote.ReadOnly = true;
            txtTurboPumpRemote.Size = new Size(84, 23);
            txtTurboPumpRemote.TabIndex = 26;
            txtTurboPumpRemote.TextAlign = HorizontalAlignment.Center;
            // 
            // label23
            // 
            label23.AutoSize = true;
            label23.BackColor = SystemColors.Info;
            label23.BorderStyle = BorderStyle.FixedSingle;
            label23.Dock = DockStyle.Fill;
            label23.Location = new Point(448, 66);
            label23.Name = "label23";
            label23.Size = new Size(84, 20);
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
            label22.Location = new Point(270, 116);
            label22.Name = "label22";
            label22.Size = new Size(83, 20);
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
            label21.Location = new Point(181, 116);
            label21.Name = "label21";
            label21.Size = new Size(83, 20);
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
            label20.Location = new Point(92, 116);
            label20.Name = "label20";
            label20.Size = new Size(83, 20);
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
            label19.Location = new Point(3, 116);
            label19.Name = "label19";
            label19.Size = new Size(83, 20);
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
            label18.Location = new Point(359, 66);
            label18.Name = "label18";
            label18.Size = new Size(83, 20);
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
            label17.Location = new Point(270, 66);
            label17.Name = "label17";
            label17.Size = new Size(83, 20);
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
            label16.Location = new Point(181, 66);
            label16.Name = "label16";
            label16.Size = new Size(83, 20);
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
            label15.Location = new Point(92, 66);
            label15.Name = "label15";
            label15.Size = new Size(83, 20);
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
            label14.Location = new Point(3, 66);
            label14.Name = "label14";
            label14.Size = new Size(83, 20);
            label14.TabIndex = 15;
            label14.Text = "상태";
            label14.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtTurboPumpRunTime
            // 
            txtTurboPumpRunTime.Dock = DockStyle.Fill;
            txtTurboPumpRunTime.Location = new Point(92, 139);
            txtTurboPumpRunTime.Name = "txtTurboPumpRunTime";
            txtTurboPumpRunTime.ReadOnly = true;
            txtTurboPumpRunTime.Size = new Size(83, 23);
            txtTurboPumpRunTime.TabIndex = 13;
            txtTurboPumpRunTime.TextAlign = HorizontalAlignment.Center;
            // 
            // lblTurboPumpWarning
            // 
            lblTurboPumpWarning.AutoSize = true;
            lblTurboPumpWarning.Dock = DockStyle.Fill;
            lblTurboPumpWarning.Location = new Point(181, 136);
            lblTurboPumpWarning.Name = "lblTurboPumpWarning";
            lblTurboPumpWarning.Size = new Size(83, 30);
            lblTurboPumpWarning.TabIndex = 12;
            lblTurboPumpWarning.Text = "-";
            // 
            // txtTurboPumpBearingTemp
            // 
            txtTurboPumpBearingTemp.Dock = DockStyle.Fill;
            txtTurboPumpBearingTemp.Location = new Point(3, 139);
            txtTurboPumpBearingTemp.Name = "txtTurboPumpBearingTemp";
            txtTurboPumpBearingTemp.ReadOnly = true;
            txtTurboPumpBearingTemp.Size = new Size(83, 23);
            txtTurboPumpBearingTemp.TabIndex = 10;
            txtTurboPumpBearingTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpElectronicsTemp
            // 
            txtTurboPumpElectronicsTemp.Dock = DockStyle.Fill;
            txtTurboPumpElectronicsTemp.Location = new Point(359, 89);
            txtTurboPumpElectronicsTemp.Name = "txtTurboPumpElectronicsTemp";
            txtTurboPumpElectronicsTemp.ReadOnly = true;
            txtTurboPumpElectronicsTemp.Size = new Size(83, 23);
            txtTurboPumpElectronicsTemp.TabIndex = 9;
            txtTurboPumpElectronicsTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpMotorTemp
            // 
            txtTurboPumpMotorTemp.Dock = DockStyle.Fill;
            txtTurboPumpMotorTemp.Location = new Point(270, 89);
            txtTurboPumpMotorTemp.Name = "txtTurboPumpMotorTemp";
            txtTurboPumpMotorTemp.ReadOnly = true;
            txtTurboPumpMotorTemp.Size = new Size(83, 23);
            txtTurboPumpMotorTemp.TabIndex = 8;
            txtTurboPumpMotorTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpCurrent
            // 
            txtTurboPumpCurrent.Dock = DockStyle.Fill;
            txtTurboPumpCurrent.Location = new Point(181, 89);
            txtTurboPumpCurrent.Name = "txtTurboPumpCurrent";
            txtTurboPumpCurrent.ReadOnly = true;
            txtTurboPumpCurrent.Size = new Size(83, 23);
            txtTurboPumpCurrent.TabIndex = 7;
            txtTurboPumpCurrent.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpSpeed
            // 
            txtTurboPumpSpeed.Dock = DockStyle.Fill;
            txtTurboPumpSpeed.Location = new Point(92, 89);
            txtTurboPumpSpeed.Name = "txtTurboPumpSpeed";
            txtTurboPumpSpeed.ReadOnly = true;
            txtTurboPumpSpeed.Size = new Size(83, 23);
            txtTurboPumpSpeed.TabIndex = 6;
            txtTurboPumpSpeed.TextAlign = HorizontalAlignment.Center;
            // 
            // btnTurboPumpStart
            // 
            btnTurboPumpStart.Dock = DockStyle.Fill;
            btnTurboPumpStart.Location = new Point(92, 3);
            btnTurboPumpStart.Name = "btnTurboPumpStart";
            btnTurboPumpStart.Size = new Size(83, 60);
            btnTurboPumpStart.TabIndex = 0;
            btnTurboPumpStart.Text = "Start";
            btnTurboPumpStart.UseVisualStyleBackColor = true;
            btnTurboPumpStart.Click += btnTurboPumpStart_Click;
            // 
            // btnTurboPumpStop
            // 
            btnTurboPumpStop.Dock = DockStyle.Fill;
            btnTurboPumpStop.Location = new Point(181, 3);
            btnTurboPumpStop.Name = "btnTurboPumpStop";
            btnTurboPumpStop.Size = new Size(83, 60);
            btnTurboPumpStop.TabIndex = 1;
            btnTurboPumpStop.Text = "Stop";
            btnTurboPumpStop.UseVisualStyleBackColor = true;
            btnTurboPumpStop.Click += btnTurboPumpStop_Click;
            // 
            // btnTurboPumpVent
            // 
            btnTurboPumpVent.Dock = DockStyle.Fill;
            btnTurboPumpVent.Location = new Point(270, 3);
            btnTurboPumpVent.Name = "btnTurboPumpVent";
            btnTurboPumpVent.Size = new Size(83, 60);
            btnTurboPumpVent.TabIndex = 2;
            btnTurboPumpVent.Text = "Vent";
            btnTurboPumpVent.UseVisualStyleBackColor = true;
            btnTurboPumpVent.Click += btnTurboPumpVent_Click;
            // 
            // btnTurboPumpReset
            // 
            btnTurboPumpReset.Dock = DockStyle.Fill;
            btnTurboPumpReset.Location = new Point(359, 3);
            btnTurboPumpReset.Name = "btnTurboPumpReset";
            btnTurboPumpReset.Size = new Size(83, 60);
            btnTurboPumpReset.TabIndex = 3;
            btnTurboPumpReset.Text = "Reset";
            btnTurboPumpReset.UseVisualStyleBackColor = true;
            btnTurboPumpReset.Click += btnTurboPumpReset_Click;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.BorderStyle = BorderStyle.FixedSingle;
            label6.Dock = DockStyle.Fill;
            label6.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label6.Location = new Point(3, 0);
            label6.Name = "label6";
            label6.Size = new Size(83, 66);
            label6.TabIndex = 4;
            label6.Text = "Tutbo\r\nPump";
            label6.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtTurboPumpStatus
            // 
            txtTurboPumpStatus.Dock = DockStyle.Fill;
            txtTurboPumpStatus.Location = new Point(3, 89);
            txtTurboPumpStatus.Name = "txtTurboPumpStatus";
            txtTurboPumpStatus.ReadOnly = true;
            txtTurboPumpStatus.Size = new Size(83, 23);
            txtTurboPumpStatus.TabIndex = 5;
            txtTurboPumpStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // txtTurboPumpingRate
            // 
            txtTurboPumpingRate.Dock = DockStyle.Fill;
            txtTurboPumpingRate.Location = new Point(270, 139);
            txtTurboPumpingRate.Name = "txtTurboPumpingRate";
            txtTurboPumpingRate.ReadOnly = true;
            txtTurboPumpingRate.Size = new Size(83, 23);
            txtTurboPumpingRate.TabIndex = 31;
            txtTurboPumpingRate.TextAlign = HorizontalAlignment.Center;
            // 
            // panel2
            // 
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Controls.Add(tableLayoutPanel1);
            panel2.Location = new Point(35, 199);
            panel2.Name = "panel2";
            panel2.Size = new Size(464, 165);
            panel2.TabIndex = 12;
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
            tableLayoutPanel1.Controls.Add(btnDryPumpStart, 1, 0);
            tableLayoutPanel1.Controls.Add(btnDryPumpStop, 2, 0);
            tableLayoutPanel1.Controls.Add(btnDryPumpStandby, 3, 0);
            tableLayoutPanel1.Controls.Add(btnDryPumpNormal, 4, 0);
            tableLayoutPanel1.Controls.Add(label4, 0, 0);
            tableLayoutPanel1.Controls.Add(txtDryPumpStatus, 0, 2);
            tableLayoutPanel1.Controls.Add(lblDryPumpService, 1, 4);
            tableLayoutPanel1.Controls.Add(label5, 0, 1);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 5;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            tableLayoutPanel1.Size = new Size(462, 163);
            tableLayoutPanel1.TabIndex = 11;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.BackColor = SystemColors.Info;
            label13.BorderStyle = BorderStyle.FixedSingle;
            label13.Dock = DockStyle.Fill;
            label13.Location = new Point(187, 113);
            label13.Name = "label13";
            label13.Size = new Size(86, 20);
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
            label12.Location = new Point(95, 113);
            label12.Name = "label12";
            label12.Size = new Size(86, 20);
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
            label11.Location = new Point(3, 113);
            label11.Name = "label11";
            label11.Size = new Size(86, 20);
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
            label10.Location = new Point(371, 63);
            label10.Name = "label10";
            label10.Size = new Size(88, 20);
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
            label9.Location = new Point(279, 63);
            label9.Name = "label9";
            label9.Size = new Size(86, 20);
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
            label8.Location = new Point(187, 63);
            label8.Name = "label8";
            label8.Size = new Size(86, 20);
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
            label7.Location = new Point(95, 63);
            label7.Name = "label7";
            label7.Size = new Size(86, 20);
            label7.TabIndex = 14;
            label7.Text = "속도";
            label7.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // lblDryPumpWarning
            // 
            lblDryPumpWarning.AutoSize = true;
            lblDryPumpWarning.Dock = DockStyle.Fill;
            lblDryPumpWarning.Location = new Point(187, 133);
            lblDryPumpWarning.Name = "lblDryPumpWarning";
            lblDryPumpWarning.Size = new Size(86, 30);
            lblDryPumpWarning.TabIndex = 12;
            lblDryPumpWarning.Text = "-";
            // 
            // txtDryPumpRunTime
            // 
            txtDryPumpRunTime.Dock = DockStyle.Fill;
            txtDryPumpRunTime.Location = new Point(3, 136);
            txtDryPumpRunTime.Name = "txtDryPumpRunTime";
            txtDryPumpRunTime.ReadOnly = true;
            txtDryPumpRunTime.Size = new Size(86, 23);
            txtDryPumpRunTime.TabIndex = 10;
            txtDryPumpRunTime.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpMotorTemp
            // 
            txtDryPumpMotorTemp.Dock = DockStyle.Fill;
            txtDryPumpMotorTemp.Location = new Point(371, 86);
            txtDryPumpMotorTemp.Name = "txtDryPumpMotorTemp";
            txtDryPumpMotorTemp.ReadOnly = true;
            txtDryPumpMotorTemp.Size = new Size(88, 23);
            txtDryPumpMotorTemp.TabIndex = 9;
            txtDryPumpMotorTemp.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpPower
            // 
            txtDryPumpPower.Dock = DockStyle.Fill;
            txtDryPumpPower.Location = new Point(279, 86);
            txtDryPumpPower.Name = "txtDryPumpPower";
            txtDryPumpPower.ReadOnly = true;
            txtDryPumpPower.Size = new Size(86, 23);
            txtDryPumpPower.TabIndex = 8;
            txtDryPumpPower.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpCurrent
            // 
            txtDryPumpCurrent.Dock = DockStyle.Fill;
            txtDryPumpCurrent.Location = new Point(187, 86);
            txtDryPumpCurrent.Name = "txtDryPumpCurrent";
            txtDryPumpCurrent.ReadOnly = true;
            txtDryPumpCurrent.Size = new Size(86, 23);
            txtDryPumpCurrent.TabIndex = 7;
            txtDryPumpCurrent.TextAlign = HorizontalAlignment.Center;
            // 
            // txtDryPumpFrequency
            // 
            txtDryPumpFrequency.Dock = DockStyle.Fill;
            txtDryPumpFrequency.Location = new Point(95, 86);
            txtDryPumpFrequency.Name = "txtDryPumpFrequency";
            txtDryPumpFrequency.ReadOnly = true;
            txtDryPumpFrequency.Size = new Size(86, 23);
            txtDryPumpFrequency.TabIndex = 6;
            txtDryPumpFrequency.TextAlign = HorizontalAlignment.Center;
            // 
            // btnDryPumpStart
            // 
            btnDryPumpStart.Dock = DockStyle.Fill;
            btnDryPumpStart.Location = new Point(95, 3);
            btnDryPumpStart.Name = "btnDryPumpStart";
            btnDryPumpStart.Size = new Size(86, 57);
            btnDryPumpStart.TabIndex = 0;
            btnDryPumpStart.Text = "Start";
            btnDryPumpStart.UseVisualStyleBackColor = true;
            btnDryPumpStart.Click += btnDryPumpStart_Click;
            // 
            // btnDryPumpStop
            // 
            btnDryPumpStop.Dock = DockStyle.Fill;
            btnDryPumpStop.Location = new Point(187, 3);
            btnDryPumpStop.Name = "btnDryPumpStop";
            btnDryPumpStop.Size = new Size(86, 57);
            btnDryPumpStop.TabIndex = 1;
            btnDryPumpStop.Text = "Stop";
            btnDryPumpStop.UseVisualStyleBackColor = true;
            btnDryPumpStop.Click += btnDryPumpStop_Click;
            // 
            // btnDryPumpStandby
            // 
            btnDryPumpStandby.Dock = DockStyle.Fill;
            btnDryPumpStandby.Location = new Point(279, 3);
            btnDryPumpStandby.Name = "btnDryPumpStandby";
            btnDryPumpStandby.Size = new Size(86, 57);
            btnDryPumpStandby.TabIndex = 2;
            btnDryPumpStandby.Text = "Standby";
            btnDryPumpStandby.UseVisualStyleBackColor = true;
            btnDryPumpStandby.Click += btnDryPumpStandby_Click;
            // 
            // btnDryPumpNormal
            // 
            btnDryPumpNormal.Dock = DockStyle.Fill;
            btnDryPumpNormal.Location = new Point(371, 3);
            btnDryPumpNormal.Name = "btnDryPumpNormal";
            btnDryPumpNormal.Size = new Size(88, 57);
            btnDryPumpNormal.TabIndex = 3;
            btnDryPumpNormal.Text = "Restart";
            btnDryPumpNormal.UseVisualStyleBackColor = true;
            btnDryPumpNormal.Click += btnDryPumpNormal_Click;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.BorderStyle = BorderStyle.FixedSingle;
            label4.Dock = DockStyle.Fill;
            label4.Font = new Font("맑은 고딕", 12F, FontStyle.Regular, GraphicsUnit.Point, 129);
            label4.Location = new Point(3, 0);
            label4.Name = "label4";
            label4.Size = new Size(86, 63);
            label4.TabIndex = 4;
            label4.Text = "DryPump";
            label4.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // txtDryPumpStatus
            // 
            txtDryPumpStatus.Dock = DockStyle.Fill;
            txtDryPumpStatus.Location = new Point(3, 86);
            txtDryPumpStatus.Name = "txtDryPumpStatus";
            txtDryPumpStatus.ReadOnly = true;
            txtDryPumpStatus.Size = new Size(86, 23);
            txtDryPumpStatus.TabIndex = 5;
            txtDryPumpStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // lblDryPumpService
            // 
            lblDryPumpService.AutoSize = true;
            lblDryPumpService.Dock = DockStyle.Fill;
            lblDryPumpService.Location = new Point(95, 133);
            lblDryPumpService.Name = "lblDryPumpService";
            lblDryPumpService.Size = new Size(86, 30);
            lblDryPumpService.TabIndex = 11;
            lblDryPumpService.Text = "-";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.BackColor = SystemColors.Info;
            label5.BorderStyle = BorderStyle.FixedSingle;
            label5.Dock = DockStyle.Fill;
            label5.Location = new Point(3, 63);
            label5.Name = "label5";
            label5.Size = new Size(86, 20);
            label5.TabIndex = 13;
            label5.Text = "상태";
            label5.TextAlign = ContentAlignment.MiddleCenter;
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
            tableLayoutPanel5.Location = new Point(31, 66);
            tableLayoutPanel5.Name = "tableLayoutPanel5";
            tableLayoutPanel5.RowCount = 2;
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            tableLayoutPanel5.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel5.Size = new Size(294, 127);
            tableLayoutPanel5.TabIndex = 8;
            // 
            // btn_GV
            // 
            btn_GV.Dock = DockStyle.Fill;
            btn_GV.Location = new Point(3, 23);
            btn_GV.Name = "btn_GV";
            btn_GV.Size = new Size(91, 101);
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
            btn_VV.Size = new Size(92, 101);
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
            btn_EV.Size = new Size(93, 101);
            btn_EV.TabIndex = 2;
            btn_EV.Text = "-";
            btn_EV.UseVisualStyleBackColor = true;
            btn_EV.Click += btn_EV_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.BorderStyle = BorderStyle.FixedSingle;
            label1.Dock = DockStyle.Fill;
            label1.FlatStyle = FlatStyle.Flat;
            label1.Location = new Point(3, 0);
            label1.Name = "label1";
            label1.Size = new Size(91, 20);
            label1.TabIndex = 3;
            label1.Text = "GateVavle";
            label1.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BorderStyle = BorderStyle.FixedSingle;
            label2.Dock = DockStyle.Fill;
            label2.FlatStyle = FlatStyle.Flat;
            label2.Location = new Point(100, 0);
            label2.Name = "label2";
            label2.Size = new Size(92, 20);
            label2.TabIndex = 4;
            label2.Text = "VentValve";
            label2.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BorderStyle = BorderStyle.FixedSingle;
            label3.Dock = DockStyle.Fill;
            label3.FlatStyle = FlatStyle.Flat;
            label3.Location = new Point(198, 0);
            label3.Name = "label3";
            label3.Size = new Size(93, 20);
            label3.TabIndex = 5;
            label3.Text = "ExhaustValve";
            label3.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // btn_iongauge
            // 
            btn_iongauge.Location = new Point(1065, 16);
            btn_iongauge.Name = "btn_iongauge";
            btn_iongauge.Size = new Size(75, 33);
            btn_iongauge.TabIndex = 7;
            btn_iongauge.Text = "-";
            btn_iongauge.UseVisualStyleBackColor = true;
            btn_iongauge.Click += btn_iongauge_Click;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(txtLog);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1172, 649);
            tabPage2.TabIndex = 3;
            tabPage2.Text = "Log";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            txtLog.Dock = DockStyle.Fill;
            txtLog.Location = new Point(3, 3);
            txtLog.Name = "txtLog";
            txtLog.Size = new Size(1166, 643);
            txtLog.TabIndex = 0;
            txtLog.Text = "";
            // 
            // menuStrip
            // 
            menuStrip.ImageScalingSize = new Size(24, 24);
            menuStrip.Items.AddRange(new ToolStripItem[] { menuFile, menuComm, menuHelp });
            menuStrip.Location = new Point(0, 0);
            menuStrip.Name = "menuStrip";
            menuStrip.Size = new Size(1186, 24);
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
            // menuComm
            // 
            menuComm.DropDownItems.AddRange(new ToolStripItem[] { menuCommSettings });
            menuComm.Name = "menuComm";
            menuComm.Size = new Size(59, 20);
            menuComm.Text = "통신(&C)";
            // 
            // menuCommSettings
            // 
            menuCommSettings.Name = "menuCommSettings";
            menuCommSettings.Size = new Size(141, 22);
            menuCommSettings.Text = "통신 설정(&S)";
            menuCommSettings.Click += MenuCommSettings_Click;
            // 
            // menuHelp
            // 
            menuHelp.DropDownItems.AddRange(new ToolStripItem[] { menuHelpAbout });
            menuHelp.Name = "menuHelp";
            menuHelp.Size = new Size(72, 20);
            menuHelp.Text = "도움말(&H)";
            // 
            // menuHelpAbout
            // 
            menuHelpAbout.Name = "menuHelpAbout";
            menuHelpAbout.Size = new Size(114, 22);
            menuHelpAbout.Text = "정보(&A)";
            menuHelpAbout.Click += MenuHelpAbout_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { toolStripStatusConnection });
            statusStrip.Location = new Point(0, 707);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(1186, 22);
            statusStrip.TabIndex = 2;
            // 
            // toolStripStatusConnection
            // 
            toolStripStatusConnection.Name = "toolStripStatusConnection";
            toolStripStatusConnection.Size = new Size(31, 17);
            toolStripStatusConnection.Text = "준비";
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
            ClientSize = new Size(1186, 729);
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
            tabControlMain.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tableLayoutPanel2.ResumeLayout(false);
            tableLayoutPanel3.ResumeLayout(false);
            tableLayoutPanel4.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel5.ResumeLayout(false);
            tableLayoutPanel8.ResumeLayout(false);
            tableLayoutPanel8.PerformLayout();
            panel4.ResumeLayout(false);
            tableLayoutPanel7.ResumeLayout(false);
            tableLayoutPanel7.PerformLayout();
            panel3.ResumeLayout(false);
            tableLayoutPanel6.ResumeLayout(false);
            tableLayoutPanel6.PerformLayout();
            panel2.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            tableLayoutPanel5.ResumeLayout(false);
            tableLayoutPanel5.PerformLayout();
            tabPage2.ResumeLayout(false);
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
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.DataGridView gridViewMaster;
        private System.Windows.Forms.DataGridView gridViewExpansion;

        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusConnection;

        private MenuStrip menuStrip;
        private ToolStripMenuItem menuFile;
        private ToolStripMenuItem menuFileExit;
        private ToolStripMenuItem menuComm;
        private ToolStripMenuItem menuCommSettings;
        private ToolStripMenuItem menuHelp;
        private ToolStripMenuItem menuHelpAbout;
        private StatusStrip statusStrip;

        #endregion
        private TabPage tabPage1;
        private TableLayoutPanel tableLayoutPanel2;
        private TableLayoutPanel tableLayoutPanel3;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_turbopump;
        private TableLayoutPanel tableLayoutPanel4;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_tempcontroller;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_bathcirculator;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_iomodule;
        private Forms.UserControls.ConnectionIndicator connectionIndicator_drypump;
        private Panel panel1;
        private Forms.UserControls.BindableTextBox txtATM;
        private Forms.UserControls.BindableTextBox txtIG;
        private Forms.UserControls.BindableTextBox txtPG;
        private Forms.UserControls.BindableTextBox txtIGStatus;
        private Button btn_iongauge;
        private TableLayoutPanel tableLayoutPanel5;
        private Button btn_VV;
        private Button btn_EV;
        private Button btn_GV;
        private Label label1;
        private Label label2;
        private Label label3;
        private Panel panel2;
        private TableLayoutPanel tableLayoutPanel1;
        private Button btnDryPumpStart;
        private Button btnDryPumpStop;
        private Button btnDryPumpStandby;
        private Button btnDryPumpNormal;
        private Label label4;
        private TextBox txtDryPumpStatus;
        private Label lblDryPumpWarning;
        private TextBox txtDryPumpRunTime;
        private TextBox txtDryPumpMotorTemp;
        private TextBox txtDryPumpPower;
        private TextBox txtDryPumpCurrent;
        private TextBox txtDryPumpFrequency;
        private Label lblDryPumpService;
        private Panel panel3;
        private TableLayoutPanel tableLayoutPanel6;
        private Label lblTurboPumpWarning;
        private TextBox txtTurboPumpBearingTemp;
        private TextBox txtTurboPumpElectronicsTemp;
        private TextBox txtTurboPumpMotorTemp;
        private TextBox txtTurboPumpCurrent;
        private TextBox txtTurboPumpSpeed;
        private Button btnTurboPumpStart;
        private Button btnTurboPumpStop;
        private Button btnTurboPumpVent;
        private Button btnTurboPumpReset;
        private Label label6;
        private TextBox txtTurboPumpStatus;
        private TextBox txtTurboPumpRunTime;
        private Label label5;
        private Label label13;
        private Label label12;
        private Label label11;
        private Label label10;
        private Label label9;
        private Label label8;
        private Label label7;
        private Label label22;
        private Label label21;
        private Label label20;
        private Label label19;
        private Label label18;
        private Label label17;
        private Label label16;
        private Label label15;
        private Label label14;
        private Button btnCh1AutoTuning;
        private TextBox txtTurboPumpRemote;
        private Label label23;
        private Label label24;
        private TextBox txtTurboPumpReady;
        private TextBox txtTurboPumpNormal;
        private Label label25;
        private Panel panel4;
        private TableLayoutPanel tableLayoutPanel7;
        private Label label26;
        private Label label31;
        private Label label32;
        private Label lblBathCirculatorWarning;
        private TextBox txtBathCirculatorMode;
        private TextBox txtBathCirculatorTime;
        private Button btnBathCirculatorStart;
        private Button btnCh2AutoTuning;
        private Button button4;
        private Button btnBathCirculatorSetTime;
        private Label label34;
        private TextBox txtBathCirculatorStatus;
        private Label label36;
        private Label label28;
        private Label label27;
        private TextBox txtBathCirculatorTargetTemp;
        private TextBox txtBathCirculatorCurrentTemp;
        private Button btnBathCirculatorSetTemp;
        private Button btnBathCirculatorStop;
        private TabPage tabPage2;
        private RichTextBox txtLog;
        private Panel panel5;
        private TableLayoutPanel tableLayoutPanel8;
        private Button btnCh2Start;
        private Button btnCh1Stop;
        private TextBox txtCh2HeatingMV;
        private TextBox txtCh2Status;
        private Label label29;
        private Label label30;
        private Label label33;
        private Label label35;
        private Label label37;
        private TextBox txtCh1HeatingMV;
        private TextBox txtCh1Status;
        private Button btnCh1Start;
        private Button btnCh2Stop;
        private Label label39;
        private Label label41;
        private Label label42;
        private Label label38;
        private Label label40;
        private Label label43;
        private TextBox txtCh1PresentValue;
        private TextBox txtCh1SetValue;
        private TextBox txtCh2PresentValue;
        private TextBox txtCh2SetValue;
        private Button button2;
        private TextBox txtCh2IsAutotune;
        private TextBox txtCh1IsAutotune;
        private TextBox txtTurboPumpingRate;
    }
}