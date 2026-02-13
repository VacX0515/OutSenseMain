namespace VacX_OutSense.UI.Controls
{
    partial class RampSettingControl
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.GroupBox grpRampSetting;
        private System.Windows.Forms.NumericUpDown nudRampUpRate;
        private System.Windows.Forms.NumericUpDown nudRampDownRate;
        private System.Windows.Forms.ComboBox cmbTimeUnit;
        private System.Windows.Forms.Label lblRampUpRate;
        private System.Windows.Forms.Label lblRampDownRate;
        private System.Windows.Forms.Label lblTimeUnit;
        private System.Windows.Forms.Label lblUpRateUnit;
        private System.Windows.Forms.Label lblDownRateUnit;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnReset;        // ★ 추가: 램프 초기화 버튼
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
            this.nudRampUpRate = new System.Windows.Forms.NumericUpDown();
            this.nudRampDownRate = new System.Windows.Forms.NumericUpDown();
            this.cmbTimeUnit = new System.Windows.Forms.ComboBox();
            this.lblRampUpRate = new System.Windows.Forms.Label();
            this.lblRampDownRate = new System.Windows.Forms.Label();
            this.lblTimeUnit = new System.Windows.Forms.Label();
            this.lblUpRateUnit = new System.Windows.Forms.Label();
            this.lblDownRateUnit = new System.Windows.Forms.Label();
            this.lblNote = new System.Windows.Forms.Label();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();    // ★ 추가
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
            this.grpRampSetting.Controls.Add(this.lblRampUpRate);
            this.grpRampSetting.Controls.Add(this.nudRampUpRate);
            this.grpRampSetting.Controls.Add(this.lblUpRateUnit);
            this.grpRampSetting.Controls.Add(this.lblTimeUnit);
            this.grpRampSetting.Controls.Add(this.cmbTimeUnit);
            this.grpRampSetting.Controls.Add(this.lblRampDownRate);
            this.grpRampSetting.Controls.Add(this.nudRampDownRate);
            this.grpRampSetting.Controls.Add(this.lblDownRateUnit);
            this.grpRampSetting.Controls.Add(this.btnApply);
            this.grpRampSetting.Controls.Add(this.btnReset);      // ★ 추가
            this.grpRampSetting.Controls.Add(this.btnRefresh);
            this.grpRampSetting.Controls.Add(this.lblNote);
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

            // ═══════════ Row 1 (y=25): 상승 변화율 + 시간 단위 ═══════════
            // 
            // lblRampUpRate
            // 
            this.lblRampUpRate.AutoSize = true;
            this.lblRampUpRate.Location = new System.Drawing.Point(12, 28);
            this.lblRampUpRate.Name = "lblRampUpRate";
            this.lblRampUpRate.Size = new System.Drawing.Size(79, 13);
            this.lblRampUpRate.TabIndex = 0;
            this.lblRampUpRate.Text = "상승 변화율:";

            // 
            // nudRampUpRate
            // 
            this.nudRampUpRate.Location = new System.Drawing.Point(100, 25);
            this.nudRampUpRate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.nudRampUpRate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudRampUpRate.Name = "nudRampUpRate";
            this.nudRampUpRate.Size = new System.Drawing.Size(75, 20);
            this.nudRampUpRate.TabIndex = 1;
            this.nudRampUpRate.Value = new decimal(new int[] { 0, 0, 0, 0 });

            // 
            // lblUpRateUnit
            // 
            this.lblUpRateUnit.AutoSize = true;
            this.lblUpRateUnit.Location = new System.Drawing.Point(178, 28);
            this.lblUpRateUnit.Name = "lblUpRateUnit";
            this.lblUpRateUnit.Size = new System.Drawing.Size(40, 13);
            this.lblUpRateUnit.TabIndex = 2;
            this.lblUpRateUnit.Text = "°C/분";

            // 
            // lblTimeUnit
            // 
            this.lblTimeUnit.AutoSize = true;
            this.lblTimeUnit.Location = new System.Drawing.Point(248, 28);
            this.lblTimeUnit.Name = "lblTimeUnit";
            this.lblTimeUnit.Size = new System.Drawing.Size(61, 13);
            this.lblTimeUnit.TabIndex = 3;
            this.lblTimeUnit.Text = "시간 단위:";

            // 
            // cmbTimeUnit
            // 
            this.cmbTimeUnit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimeUnit.FormattingEnabled = true;
            this.cmbTimeUnit.Location = new System.Drawing.Point(315, 24);
            this.cmbTimeUnit.Name = "cmbTimeUnit";
            this.cmbTimeUnit.Size = new System.Drawing.Size(72, 21);
            this.cmbTimeUnit.TabIndex = 4;
            this.cmbTimeUnit.SelectedIndexChanged += new System.EventHandler(this.cmbTimeUnit_SelectedIndexChanged);

            // ═══════════ Row 2 (y=55): 하강 변화율 + 버튼 3개 ═══════════
            // 
            // lblRampDownRate
            // 
            this.lblRampDownRate.AutoSize = true;
            this.lblRampDownRate.Location = new System.Drawing.Point(12, 58);
            this.lblRampDownRate.Name = "lblRampDownRate";
            this.lblRampDownRate.Size = new System.Drawing.Size(79, 13);
            this.lblRampDownRate.TabIndex = 5;
            this.lblRampDownRate.Text = "하강 변화율:";

            // 
            // nudRampDownRate
            // 
            this.nudRampDownRate.Location = new System.Drawing.Point(100, 55);
            this.nudRampDownRate.Maximum = new decimal(new int[] { 9999, 0, 0, 0 });
            this.nudRampDownRate.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudRampDownRate.Name = "nudRampDownRate";
            this.nudRampDownRate.Size = new System.Drawing.Size(75, 20);
            this.nudRampDownRate.TabIndex = 6;
            this.nudRampDownRate.Value = new decimal(new int[] { 0, 0, 0, 0 });

            // 
            // lblDownRateUnit
            // 
            this.lblDownRateUnit.AutoSize = true;
            this.lblDownRateUnit.Location = new System.Drawing.Point(178, 58);
            this.lblDownRateUnit.Name = "lblDownRateUnit";
            this.lblDownRateUnit.Size = new System.Drawing.Size(40, 13);
            this.lblDownRateUnit.TabIndex = 7;
            this.lblDownRateUnit.Text = "°C/분";

            // 
            // btnApply  ★ 위치 조정: 3개 버튼 배치
            // 
            this.btnApply.Location = new System.Drawing.Point(232, 53);
            this.btnApply.Size = new System.Drawing.Size(52, 25);
            this.btnApply.Text = "적용";
            this.btnApply.TabIndex = 8;
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);

            // ★ 추가: 램프 초기화 버튼
            // 
            // btnReset
            // 
            this.btnReset.Location = new System.Drawing.Point(288, 53);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(60, 25);
            this.btnReset.TabIndex = 15;
            this.btnReset.Text = "초기화";
            this.btnReset.ForeColor = System.Drawing.Color.Red;
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);

            // 
            // btnRefresh  ★ 위치 조정
            // 
            this.btnRefresh.Location = new System.Drawing.Point(352, 53);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 25);
            this.btnRefresh.TabIndex = 9;
            this.btnRefresh.Text = "새로고침";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

            // ═══════════ Row 3 (y=85): 안내 메시지 ═══════════
            // 
            // lblNote
            // 
            this.lblNote.AutoSize = true;
            this.lblNote.ForeColor = System.Drawing.Color.Gray;
            this.lblNote.Location = new System.Drawing.Point(12, 85);
            this.lblNote.Name = "lblNote";
            this.lblNote.Size = new System.Drawing.Size(250, 13);
            this.lblNote.TabIndex = 10;
            this.lblNote.Text = "※ 변화율 0 = 해당 방향 Ramp OFF";

            // ═══════════ Row 4 (y=110): 상태 표시 ═══════════
            // 
            // lblRampStatus
            // 
            this.lblRampStatus.AutoSize = true;
            this.lblRampStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblRampStatus.Location = new System.Drawing.Point(12, 110);
            this.lblRampStatus.Name = "lblRampStatus";
            this.lblRampStatus.Size = new System.Drawing.Size(69, 15);
            this.lblRampStatus.TabIndex = 11;
            this.lblRampStatus.Text = "Ramp OFF";

            // 
            // lblRunStatus
            // 
            this.lblRunStatus.AutoSize = true;
            this.lblRunStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.lblRunStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRunStatus.Location = new System.Drawing.Point(340, 110);
            this.lblRunStatus.Name = "lblRunStatus";
            this.lblRunStatus.Size = new System.Drawing.Size(31, 15);
            this.lblRunStatus.TabIndex = 12;
            this.lblRunStatus.Text = "정지";

            // ═══════════ Row 5 (y=135): 진행 바 ═══════════
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(12, 135);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(375, 20);
            this.progressBar.TabIndex = 13;
            this.progressBar.Visible = false;

            // ═══════════ Row 6 (y=162): 진행 텍스트 ═══════════
            // 
            // lblProgress
            // 
            this.lblProgress.AutoSize = true;
            this.lblProgress.Location = new System.Drawing.Point(12, 162);
            this.lblProgress.Name = "lblProgress";
            this.lblProgress.Size = new System.Drawing.Size(0, 13);
            this.lblProgress.TabIndex = 14;
            this.lblProgress.Visible = false;

            // 
            // grpRampSetting - ResumeLayout
            // 
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