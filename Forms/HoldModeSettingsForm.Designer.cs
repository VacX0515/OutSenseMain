namespace VacX_OutSense.Forms
{
    partial class HoldModeSettingsForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.grpChannels = new System.Windows.Forms.GroupBox();
            this.chkCh2 = new System.Windows.Forms.CheckBox();
            this.chkCh3 = new System.Windows.Forms.CheckBox();
            this.chkCh4 = new System.Windows.Forms.CheckBox();
            this.chkCh5 = new System.Windows.Forms.CheckBox();
            this.lblPreview = new System.Windows.Forms.Label();

            this.grpParams = new System.Windows.Forms.GroupBox();
            this.lblCheckInterval = new System.Windows.Forms.Label();
            this.numCheckInterval = new System.Windows.Forms.NumericUpDown();
            this.lblCheckIntervalUnit = new System.Windows.Forms.Label();
            this.lblErrorTolerance = new System.Windows.Forms.Label();
            this.numErrorTolerance = new System.Windows.Forms.NumericUpDown();
            this.lblErrorToleranceUnit = new System.Windows.Forms.Label();
            this.lblMinHeaterTemp = new System.Windows.Forms.Label();
            this.numMinHeaterTemp = new System.Windows.Forms.NumericUpDown();
            this.lblMinHeaterTempUnit = new System.Windows.Forms.Label();
            this.lblMaxHeaterTemp = new System.Windows.Forms.Label();
            this.numMaxHeaterTemp = new System.Windows.Forms.NumericUpDown();
            this.lblMaxHeaterTempUnit = new System.Windows.Forms.Label();
            this.lblAdjustMultiplier = new System.Windows.Forms.Label();
            this.numAdjustMultiplier = new System.Windows.Forms.NumericUpDown();

            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();

            this.grpChannels.SuspendLayout();
            this.grpParams.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCheckInterval)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numErrorTolerance)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinHeaterTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxHeaterTemp)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAdjustMultiplier)).BeginInit();
            this.SuspendLayout();

            // 
            // grpChannels
            // 
            this.grpChannels.Controls.Add(this.chkCh2);
            this.grpChannels.Controls.Add(this.chkCh3);
            this.grpChannels.Controls.Add(this.chkCh4);
            this.grpChannels.Controls.Add(this.chkCh5);
            this.grpChannels.Controls.Add(this.lblPreview);
            this.grpChannels.Location = new System.Drawing.Point(12, 12);
            this.grpChannels.Name = "grpChannels";
            this.grpChannels.Size = new System.Drawing.Size(260, 100);
            this.grpChannels.TabIndex = 0;
            this.grpChannels.TabStop = false;
            this.grpChannels.Text = "온도 소스 (다중 선택 시 평균)";

            // 
            // chkCh2
            // 
            this.chkCh2.AutoSize = true;
            this.chkCh2.Location = new System.Drawing.Point(15, 25);
            this.chkCh2.Name = "chkCh2";
            this.chkCh2.Size = new System.Drawing.Size(50, 19);
            this.chkCh2.TabIndex = 0;
            this.chkCh2.Text = "CH2";
            this.chkCh2.CheckedChanged += new System.EventHandler(this.chkChannel_CheckedChanged);

            // 
            // chkCh3
            // 
            this.chkCh3.AutoSize = true;
            this.chkCh3.Location = new System.Drawing.Point(75, 25);
            this.chkCh3.Name = "chkCh3";
            this.chkCh3.Size = new System.Drawing.Size(50, 19);
            this.chkCh3.TabIndex = 1;
            this.chkCh3.Text = "CH3";
            this.chkCh3.CheckedChanged += new System.EventHandler(this.chkChannel_CheckedChanged);

            // 
            // chkCh4
            // 
            this.chkCh4.AutoSize = true;
            this.chkCh4.Location = new System.Drawing.Point(135, 25);
            this.chkCh4.Name = "chkCh4";
            this.chkCh4.Size = new System.Drawing.Size(50, 19);
            this.chkCh4.TabIndex = 2;
            this.chkCh4.Text = "CH4";
            this.chkCh4.CheckedChanged += new System.EventHandler(this.chkChannel_CheckedChanged);

            // 
            // chkCh5
            // 
            this.chkCh5.AutoSize = true;
            this.chkCh5.Location = new System.Drawing.Point(195, 25);
            this.chkCh5.Name = "chkCh5";
            this.chkCh5.Size = new System.Drawing.Size(50, 19);
            this.chkCh5.TabIndex = 3;
            this.chkCh5.Text = "CH5";
            this.chkCh5.CheckedChanged += new System.EventHandler(this.chkChannel_CheckedChanged);

            // 
            // lblPreview
            // 
            this.lblPreview.AutoSize = true;
            this.lblPreview.ForeColor = System.Drawing.Color.Blue;
            this.lblPreview.Location = new System.Drawing.Point(15, 60);
            this.lblPreview.Name = "lblPreview";
            this.lblPreview.Size = new System.Drawing.Size(100, 15);
            this.lblPreview.TabIndex = 4;
            this.lblPreview.Text = "제어 온도: CH2";

            // 
            // grpParams
            // 
            this.grpParams.Controls.Add(this.lblCheckInterval);
            this.grpParams.Controls.Add(this.numCheckInterval);
            this.grpParams.Controls.Add(this.lblCheckIntervalUnit);
            this.grpParams.Controls.Add(this.lblErrorTolerance);
            this.grpParams.Controls.Add(this.numErrorTolerance);
            this.grpParams.Controls.Add(this.lblErrorToleranceUnit);
            this.grpParams.Controls.Add(this.lblMinHeaterTemp);
            this.grpParams.Controls.Add(this.numMinHeaterTemp);
            this.grpParams.Controls.Add(this.lblMinHeaterTempUnit);
            this.grpParams.Controls.Add(this.lblMaxHeaterTemp);
            this.grpParams.Controls.Add(this.numMaxHeaterTemp);
            this.grpParams.Controls.Add(this.lblMaxHeaterTempUnit);
            this.grpParams.Controls.Add(this.lblAdjustMultiplier);
            this.grpParams.Controls.Add(this.numAdjustMultiplier);
            this.grpParams.Location = new System.Drawing.Point(12, 118);
            this.grpParams.Name = "grpParams";
            this.grpParams.Size = new System.Drawing.Size(260, 175);
            this.grpParams.TabIndex = 1;
            this.grpParams.TabStop = false;
            this.grpParams.Text = "제어 파라미터";

            // 
            // lblCheckInterval
            // 
            this.lblCheckInterval.AutoSize = true;
            this.lblCheckInterval.Location = new System.Drawing.Point(15, 28);
            this.lblCheckInterval.Name = "lblCheckInterval";
            this.lblCheckInterval.Size = new System.Drawing.Size(67, 15);
            this.lblCheckInterval.Text = "체크 간격:";

            // 
            // numCheckInterval
            // 
            this.numCheckInterval.Location = new System.Drawing.Point(130, 25);
            this.numCheckInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
            this.numCheckInterval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numCheckInterval.Name = "numCheckInterval";
            this.numCheckInterval.Size = new System.Drawing.Size(60, 23);
            this.numCheckInterval.TabIndex = 0;
            this.numCheckInterval.Value = new decimal(new int[] { 10, 0, 0, 0 });

            // 
            // lblCheckIntervalUnit
            // 
            this.lblCheckIntervalUnit.AutoSize = true;
            this.lblCheckIntervalUnit.Location = new System.Drawing.Point(195, 28);
            this.lblCheckIntervalUnit.Name = "lblCheckIntervalUnit";
            this.lblCheckIntervalUnit.Size = new System.Drawing.Size(17, 15);
            this.lblCheckIntervalUnit.Text = "분";

            // 
            // lblErrorTolerance
            // 
            this.lblErrorTolerance.AutoSize = true;
            this.lblErrorTolerance.Location = new System.Drawing.Point(15, 58);
            this.lblErrorTolerance.Name = "lblErrorTolerance";
            this.lblErrorTolerance.Size = new System.Drawing.Size(79, 15);
            this.lblErrorTolerance.Text = "허용 오차:";

            // 
            // numErrorTolerance
            // 
            this.numErrorTolerance.DecimalPlaces = 1;
            this.numErrorTolerance.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numErrorTolerance.Location = new System.Drawing.Point(130, 55);
            this.numErrorTolerance.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numErrorTolerance.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numErrorTolerance.Name = "numErrorTolerance";
            this.numErrorTolerance.Size = new System.Drawing.Size(60, 23);
            this.numErrorTolerance.TabIndex = 1;
            this.numErrorTolerance.Value = new decimal(new int[] { 1, 0, 0, 65536 });

            // 
            // lblErrorToleranceUnit
            // 
            this.lblErrorToleranceUnit.AutoSize = true;
            this.lblErrorToleranceUnit.Location = new System.Drawing.Point(195, 58);
            this.lblErrorToleranceUnit.Name = "lblErrorToleranceUnit";
            this.lblErrorToleranceUnit.Size = new System.Drawing.Size(18, 15);
            this.lblErrorToleranceUnit.Text = "°C";

            // 
            // lblMinHeaterTemp
            // 
            this.lblMinHeaterTemp.AutoSize = true;
            this.lblMinHeaterTemp.Location = new System.Drawing.Point(15, 88);
            this.lblMinHeaterTemp.Name = "lblMinHeaterTemp";
            this.lblMinHeaterTemp.Size = new System.Drawing.Size(103, 15);
            this.lblMinHeaterTemp.Text = "CH1 SV 최소값:";

            // 
            // numMinHeaterTemp
            // 
            this.numMinHeaterTemp.Location = new System.Drawing.Point(130, 85);
            this.numMinHeaterTemp.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numMinHeaterTemp.Name = "numMinHeaterTemp";
            this.numMinHeaterTemp.Size = new System.Drawing.Size(60, 23);
            this.numMinHeaterTemp.TabIndex = 2;
            this.numMinHeaterTemp.Value = new decimal(new int[] { 30, 0, 0, 0 });

            // 
            // lblMinHeaterTempUnit
            // 
            this.lblMinHeaterTempUnit.AutoSize = true;
            this.lblMinHeaterTempUnit.Location = new System.Drawing.Point(195, 88);
            this.lblMinHeaterTempUnit.Name = "lblMinHeaterTempUnit";
            this.lblMinHeaterTempUnit.Size = new System.Drawing.Size(18, 15);
            this.lblMinHeaterTempUnit.Text = "°C";

            // 
            // lblMaxHeaterTemp
            // 
            this.lblMaxHeaterTemp.AutoSize = true;
            this.lblMaxHeaterTemp.Location = new System.Drawing.Point(15, 118);
            this.lblMaxHeaterTemp.Name = "lblMaxHeaterTemp";
            this.lblMaxHeaterTemp.Size = new System.Drawing.Size(103, 15);
            this.lblMaxHeaterTemp.Text = "CH1 SV 최대값:";

            // 
            // numMaxHeaterTemp
            // 
            this.numMaxHeaterTemp.Location = new System.Drawing.Point(130, 115);
            this.numMaxHeaterTemp.Maximum = new decimal(new int[] { 500, 0, 0, 0 });
            this.numMaxHeaterTemp.Name = "numMaxHeaterTemp";
            this.numMaxHeaterTemp.Size = new System.Drawing.Size(60, 23);
            this.numMaxHeaterTemp.TabIndex = 3;
            this.numMaxHeaterTemp.Value = new decimal(new int[] { 200, 0, 0, 0 });

            // 
            // lblMaxHeaterTempUnit
            // 
            this.lblMaxHeaterTempUnit.AutoSize = true;
            this.lblMaxHeaterTempUnit.Location = new System.Drawing.Point(195, 118);
            this.lblMaxHeaterTempUnit.Name = "lblMaxHeaterTempUnit";
            this.lblMaxHeaterTempUnit.Size = new System.Drawing.Size(18, 15);
            this.lblMaxHeaterTempUnit.Text = "°C";

            // 
            // lblAdjustMultiplier
            // 
            this.lblAdjustMultiplier.AutoSize = true;
            this.lblAdjustMultiplier.Location = new System.Drawing.Point(15, 148);
            this.lblAdjustMultiplier.Name = "lblAdjustMultiplier";
            this.lblAdjustMultiplier.Size = new System.Drawing.Size(67, 15);
            this.lblAdjustMultiplier.Text = "조정 배율:";

            // 
            // numAdjustMultiplier
            // 
            this.numAdjustMultiplier.DecimalPlaces = 1;
            this.numAdjustMultiplier.Increment = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numAdjustMultiplier.Location = new System.Drawing.Point(130, 145);
            this.numAdjustMultiplier.Maximum = new decimal(new int[] { 5, 0, 0, 0 });
            this.numAdjustMultiplier.Minimum = new decimal(new int[] { 1, 0, 0, 65536 });
            this.numAdjustMultiplier.Name = "numAdjustMultiplier";
            this.numAdjustMultiplier.Size = new System.Drawing.Size(60, 23);
            this.numAdjustMultiplier.TabIndex = 4;
            this.numAdjustMultiplier.Value = new decimal(new int[] { 10, 0, 0, 65536 });

            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(66, 305);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 30);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "확인";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(152, 305);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 30);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "취소";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // 
            // HoldModeSettingsForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(284, 347);
            this.Controls.Add(this.grpChannels);
            this.Controls.Add(this.grpParams);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "HoldModeSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "온도 유지 모드 설정";
            this.grpChannels.ResumeLayout(false);
            this.grpChannels.PerformLayout();
            this.grpParams.ResumeLayout(false);
            this.grpParams.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCheckInterval)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numErrorTolerance)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinHeaterTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMaxHeaterTemp)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAdjustMultiplier)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.GroupBox grpChannels;
        private System.Windows.Forms.CheckBox chkCh2;
        private System.Windows.Forms.CheckBox chkCh3;
        private System.Windows.Forms.CheckBox chkCh4;
        private System.Windows.Forms.CheckBox chkCh5;
        private System.Windows.Forms.Label lblPreview;

        private System.Windows.Forms.GroupBox grpParams;
        private System.Windows.Forms.Label lblCheckInterval;
        private System.Windows.Forms.NumericUpDown numCheckInterval;
        private System.Windows.Forms.Label lblCheckIntervalUnit;
        private System.Windows.Forms.Label lblErrorTolerance;
        private System.Windows.Forms.NumericUpDown numErrorTolerance;
        private System.Windows.Forms.Label lblErrorToleranceUnit;
        private System.Windows.Forms.Label lblMinHeaterTemp;
        private System.Windows.Forms.NumericUpDown numMinHeaterTemp;
        private System.Windows.Forms.Label lblMinHeaterTempUnit;
        private System.Windows.Forms.Label lblMaxHeaterTemp;
        private System.Windows.Forms.NumericUpDown numMaxHeaterTemp;
        private System.Windows.Forms.Label lblMaxHeaterTempUnit;
        private System.Windows.Forms.Label lblAdjustMultiplier;
        private System.Windows.Forms.NumericUpDown numAdjustMultiplier;

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}