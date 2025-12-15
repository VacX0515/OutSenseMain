namespace VacX_OutSense.UI.Controls
{
    partial class RampSettingControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.GroupBox grpRampSetting;
        private System.Windows.Forms.CheckBox chkEnableRamp;
        private System.Windows.Forms.NumericUpDown nudRampUpRate;
        private System.Windows.Forms.NumericUpDown nudRampDownRate;
        private System.Windows.Forms.ComboBox cmbTimeUnit;
        private System.Windows.Forms.Label lblRampUpRate;
        private System.Windows.Forms.Label lblRampDownRate;
        private System.Windows.Forms.Label lblTimeUnit;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Label lblRampStatus;
        private System.Windows.Forms.Label lblRunStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Label lblProgress;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.grpRampSetting = new System.Windows.Forms.GroupBox();
            this.chkEnableRamp = new System.Windows.Forms.CheckBox();
            this.nudRampUpRate = new System.Windows.Forms.NumericUpDown();
            this.nudRampDownRate = new System.Windows.Forms.NumericUpDown();
            this.cmbTimeUnit = new System.Windows.Forms.ComboBox();
            this.lblRampUpRate = new System.Windows.Forms.Label();
            this.lblRampDownRate = new System.Windows.Forms.Label();
            this.lblTimeUnit = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lblRampStatus = new System.Windows.Forms.Label();
            this.lblRunStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.lblProgress = new System.Windows.Forms.Label();

            this.grpRampSetting.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRampUpRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRampDownRate)).BeginInit();
            this.SuspendLayout();

            // 
            // grpRampSetting
            // 
            this.grpRampSetting.Controls.Add(this.chkEnableRamp);
            this.grpRampSetting.Controls.Add(this.lblRampUpRate);
            this.grpRampSetting.Controls.Add(this.nudRampUpRate);
            this.grpRampSetting.Controls.Add(this.lblRampDownRate);
            this.grpRampSetting.Controls.Add(this.nudRampDownRate);
            this.grpRampSetting.Controls.Add(this.lblTimeUnit);
            this.grpRampSetting.Controls.Add(this.cmbTimeUnit);
            this.grpRampSetting.Controls.Add(this.btnApply);
            this.grpRampSetting.Controls.Add(this.btnRefresh);
            this.grpRampSetting.Controls.Add(this.lblRampStatus);
            this.grpRampSetting.Controls.Add(this.lblRunStatus);
            this.grpRampSetting.Controls.Add(this.progressBar);
            this.grpRampSetting.Controls.Add(this.lblProgress);
            this.grpRampSetting.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpRampSetting.Location = new System.Drawing.Point(0, 0);
            this.grpRampSetting.Name = "grpRampSetting";
            this.grpRampSetting.Size = new System.Drawing.Size(400, 250);
            this.grpRampSetting.TabIndex = 0;
            this.grpRampSetting.TabStop = false;
            this.grpRampSetting.Text = "Ramp 설정";

            // 
            // chkEnableRamp
            // 
            this.chkEnableRamp.AutoSize = true;
            this.chkEnableRamp.Location = new System.Drawing.Point(15, 25);
            this.chkEnableRamp.Name = "chkEnableRamp";
            this.chkEnableRamp.Size = new System.Drawing.Size(100, 17);
            this.chkEnableRamp.TabIndex = 0;
            this.chkEnableRamp.Text = "Ramp 활성화";
            this.chkEnableRamp.UseVisualStyleBackColor = true;
            this.chkEnableRamp.CheckedChanged += new System.EventHandler(this.chkEnableRamp_CheckedChanged);

            // 
            // lblRampUpRate
            // 
            this.lblRampUpRate.AutoSize = true;
            this.lblRampUpRate.Location = new System.Drawing.Point(15, 55);
            this.lblRampUpRate.Name = "lblRampUpRate";
            this.lblRampUpRate.Size = new System.Drawing.Size(85, 13);
            this.lblRampUpRate.TabIndex = 1;
            this.lblRampUpRate.Text = "상승 변화율:";

            // 
            // nudRampUpRate
            // 
            this.nudRampUpRate.Location = new System.Drawing.Point(110, 53);
            this.nudRampUpRate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.nudRampUpRate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudRampUpRate.Name = "nudRampUpRate";
            this.nudRampUpRate.Size = new System.Drawing.Size(80, 20);
            this.nudRampUpRate.TabIndex = 2;
            this.nudRampUpRate.Value = new decimal(new int[] { 0, 0, 0, 0 });

            // 
            // lblRampDownRate
            // 
            this.lblRampDownRate.AutoSize = true;
            this.lblRampDownRate.Location = new System.Drawing.Point(15, 85);
            this.lblRampDownRate.Name = "lblRampDownRate";
            this.lblRampDownRate.Size = new System.Drawing.Size(85, 13);
            this.lblRampDownRate.TabIndex = 3;
            this.lblRampDownRate.Text = "하강 변화율:";

            // 
            // nudRampDownRate
            // 
            this.nudRampDownRate.Location = new System.Drawing.Point(110, 83);
            this.nudRampDownRate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.nudRampDownRate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudRampDownRate.Name = "nudRampDownRate";
            this.nudRampDownRate.Size = new System.Drawing.Size(80, 20);
            this.nudRampDownRate.TabIndex = 4;
            this.nudRampDownRate.Value = new decimal(new int[] { 0, 0, 0, 0 });

            // 
            // lblTimeUnit
            // 
            this.lblTimeUnit.AutoSize = true;
            this.lblTimeUnit.Location = new System.Drawing.Point(210, 55);
            this.lblTimeUnit.Name = "lblTimeUnit";
            this.lblTimeUnit.Size = new System.Drawing.Size(70, 13);
            this.lblTimeUnit.TabIndex = 5;
            this.lblTimeUnit.Text = "시간 단위:";

            // 
            // cmbTimeUnit
            // 
            this.cmbTimeUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimeUnit.FormattingEnabled = true;
            this.cmbTimeUnit.Location = new System.Drawing.Point(285, 52);
            this.cmbTimeUnit.Name = "cmbTimeUnit";
            this.cmbTimeUnit.Size = new System.Drawing.Size(90, 21);
            this.cmbTimeUnit.TabIndex = 6;

            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(210, 83);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 23);
            this.btnApply.TabIndex = 7;
            this.btnApply.Text = "적용";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(300, 83);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 8;
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // 
            // lblRampStatus
            // 
            this.lblRampStatus.AutoSize = true;
            this.lblRampStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblRampStatus.Location = new System.Drawing.Point(15, 125);
            this.lblRampStatus.Name = "lblRampStatus";
            this.lblRampStatus.Size = new System.Drawing.Size(69, 15);
            this.lblRampStatus.TabIndex = 9;
            this.lblRampStatus.Text = "Ramp OFF";

            // 
            // lblRunStatus
            // 
            this.lblRunStatus.AutoSize = true;
            this.lblRunStatus.Location = new System.Drawing.Point(300, 127);
            this.lblRunStatus.Name = "lblRunStatus";
            this.lblRunStatus.Size = new System.Drawing.Size(31, 13);
            this.lblRunStatus.TabIndex = 10;
            this.lblRunStatus.Text = "정지";

            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(15, 150);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(360, 23);
            this.progressBar.TabIndex = 11;
            this.progressBar.Visible = false;

            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(15, 180);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(0, 13);
            this.lblProgress.TabIndex = 12;
            this.lblProgress.Visible = false;

            this.grpRampSetting.ResumeLayout(false);
            this.grpRampSetting.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudRampUpRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudRampDownRate)).EndInit();
            this.ResumeLayout(false);

            // 
            // RampSettingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.grpRampSetting);
            this.Name = "RampSettingControl";
            this.Size = new System.Drawing.Size(400, 250);
        }
    }
}