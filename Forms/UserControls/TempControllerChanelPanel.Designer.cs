namespace VacX_OutSense.Controls
{
    partial class TempControllerChanelPanel
    {
        /// <summary> 
        /// 필수 디자이너 변수입니다.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 사용 중인 모든 리소스를 정리합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 삭제해야 하면 true이고, 그렇지 않으면 false입니다.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 구성 요소 디자이너에서 생성한 코드

        /// <summary> 
        /// 디자이너 지원에 필요한 메서드입니다. 
        /// 이 메서드의 내용을 코드 편집기로 수정하지 마세요.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblChannelTitle = new System.Windows.Forms.Label();
            this.pnlRunningIndicator = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtPresentValue = new System.Windows.Forms.TextBox();
            this.txtSetValue = new System.Windows.Forms.TextBox();
            this.txtHeatingOutput = new System.Windows.Forms.TextBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.btnSetTemp = new System.Windows.Forms.Button();
            this.btnAutoTuning = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblChannelTitle
            // 
            this.lblChannelTitle.BackColor = System.Drawing.Color.Navy;
            this.lblChannelTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblChannelTitle.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblChannelTitle.ForeColor = System.Drawing.Color.White;
            this.lblChannelTitle.Location = new System.Drawing.Point(0, 0);
            this.lblChannelTitle.Name = "lblChannelTitle";
            this.lblChannelTitle.Size = new System.Drawing.Size(250, 25);
            this.lblChannelTitle.TabIndex = 0;
            this.lblChannelTitle.Text = "채널 1";
            this.lblChannelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pnlRunningIndicator
            // 
            this.pnlRunningIndicator.BackColor = System.Drawing.Color.LightGray;
            this.pnlRunningIndicator.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlRunningIndicator.Location = new System.Drawing.Point(10, 35);
            this.pnlRunningIndicator.Name = "pnlRunningIndicator";
            this.pnlRunningIndicator.Size = new System.Drawing.Size(20, 20);
            this.pnlRunningIndicator.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label1.Location = new System.Drawing.Point(36, 38);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 15);
            this.label1.TabIndex = 2;
            this.label1.Text = "현재값";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label2.Location = new System.Drawing.Point(36, 68);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(43, 15);
            this.label2.TabIndex = 3;
            this.label2.Text = "설정값";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.label3.Location = new System.Drawing.Point(24, 98);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(55, 15);
            this.label3.TabIndex = 4;
            this.label3.Text = "가열출력";
            // 
            // txtPresentValue
            // 
            this.txtPresentValue.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtPresentValue.Location = new System.Drawing.Point(85, 35);
            this.txtPresentValue.Name = "txtPresentValue";
            this.txtPresentValue.ReadOnly = true;
            this.txtPresentValue.Size = new System.Drawing.Size(80, 25);
            this.txtPresentValue.TabIndex = 5;
            this.txtPresentValue.Text = "0.0°C";
            this.txtPresentValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtSetValue
            // 
            this.txtSetValue.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtSetValue.Location = new System.Drawing.Point(85, 65);
            this.txtSetValue.Name = "txtSetValue";
            this.txtSetValue.ReadOnly = true;
            this.txtSetValue.Size = new System.Drawing.Size(80, 25);
            this.txtSetValue.TabIndex = 6;
            this.txtSetValue.Text = "0.0°C";
            this.txtSetValue.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // txtHeatingOutput
            // 
            this.txtHeatingOutput.Font = new System.Drawing.Font("맑은 고딕", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.txtHeatingOutput.Location = new System.Drawing.Point(85, 95);
            this.txtHeatingOutput.Name = "txtHeatingOutput";
            this.txtHeatingOutput.ReadOnly = true;
            this.txtHeatingOutput.Size = new System.Drawing.Size(80, 25);
            this.txtHeatingOutput.TabIndex = 7;
            this.txtHeatingOutput.Text = "0.0%";
            this.txtHeatingOutput.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnStart.Location = new System.Drawing.Point(175, 35);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(65, 25);
            this.btnStart.TabIndex = 8;
            this.btnStart.Text = "시작";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // btnStop
            // 
            this.btnStop.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnStop.Location = new System.Drawing.Point(175, 65);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(65, 25);
            this.btnStop.TabIndex = 9;
            this.btnStop.Text = "정지";
            this.btnStop.UseVisualStyleBackColor = true;
            // 
            // btnSetTemp
            // 
            this.btnSetTemp.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnSetTemp.Location = new System.Drawing.Point(175, 95);
            this.btnSetTemp.Name = "btnSetTemp";
            this.btnSetTemp.Size = new System.Drawing.Size(65, 25);
            this.btnSetTemp.TabIndex = 10;
            this.btnSetTemp.Text = "온도설정";
            this.btnSetTemp.UseVisualStyleBackColor = true;
            // 
            // btnAutoTuning
            // 
            this.btnAutoTuning.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.btnAutoTuning.Location = new System.Drawing.Point(33, 6);
            this.btnAutoTuning.Name = "btnAutoTuning";
            this.btnAutoTuning.Size = new System.Drawing.Size(80, 25);
            this.btnAutoTuning.TabIndex = 11;
            this.btnAutoTuning.Text = "오토튜닝";
            this.btnAutoTuning.UseVisualStyleBackColor = true;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.lblStatus.Location = new System.Drawing.Point(119, 11);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(47, 15);
            this.lblStatus.TabIndex = 12;
            this.lblStatus.Text = "대기 중";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.WhiteSmoke;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.btnAutoTuning);
            this.panel1.Controls.Add(this.lblStatus);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panel1.Location = new System.Drawing.Point(0, 130);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(250, 35);
            this.panel1.TabIndex = 13;
            // 
            // TemperatureChannelControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnSetTemp);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.txtHeatingOutput);
            this.Controls.Add(this.txtSetValue);
            this.Controls.Add(this.txtPresentValue);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pnlRunningIndicator);
            this.Controls.Add(this.lblChannelTitle);
            this.Name = "TempControllerChanelPanel";
            this.Size = new System.Drawing.Size(250, 165);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblChannelTitle;
        private System.Windows.Forms.Panel pnlRunningIndicator;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox txtPresentValue;
        private System.Windows.Forms.TextBox txtSetValue;
        private System.Windows.Forms.TextBox txtHeatingOutput;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.Button btnSetTemp;
        private System.Windows.Forms.Button btnAutoTuning;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Panel panel1;
    }
}