namespace VacX_OutSense.Forms.UserControls
{
    partial class BindableTextBox
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

                // 데이터 소스 구독 해제
                UnsubscribeFromDataSource();
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
            this.label = new System.Windows.Forms.Label();
            this.textBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label
            // 
            this.label.AutoSize = true;
            this.label.Location = new System.Drawing.Point(3, 6);
            this.label.Name = "label";
            this.label.Size = new System.Drawing.Size(45, 15);
            this.label.TabIndex = 0;
            this.label.Text = "레이블:";
            this.label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.label.MaximumSize = new System.Drawing.Size(80, 0); // 최대 너비 설정, 높이는 자동 조정
            this.label.Padding = new System.Windows.Forms.Padding(0, 0, 0, 2); // 하단 여백 추가
                                                                               // 
                                                                               // textBox
                                                                               // 
            this.textBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox.Location = new System.Drawing.Point(90, 3);
            this.textBox.Name = "textBox";
            this.textBox.Size = new System.Drawing.Size(150, 23);
            this.textBox.TabIndex = 1;
            // 
            // BindableTextBox
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.textBox);
            this.Controls.Add(this.label);
            this.Name = "BindableTextBox";
            this.Size = new System.Drawing.Size(250, 30);
            this.Padding = new System.Windows.Forms.Padding(0, 0, 0, 3); // 컨트롤 전체 하단 여백 추가
            this.ResumeLayout(false);
            this.PerformLayout();

            // 라벨 텍스트 변경 시 컨트롤 높이 조정을 위한 이벤트 핸들러 추가
            this.label.TextChanged += new System.EventHandler(this.Label_TextChanged);
        }

        private void Label_TextChanged(object sender, System.EventArgs e)
        {
            // 라벨이 AutoSize=true이므로 텍스트가 변경되면 라벨 크기도 자동으로 변경됨
            // 약간의 여유 공간을 더 확보하여 텍스트가 잘리지 않도록 함
            this.Height = Math.Max(30, this.label.Height + 15); // 여유 공간 증가

            // 텍스트박스 위치 조정 (라벨이 여러 줄일 경우 중앙에 배치)
            if (this.label.Height > 20) // 라벨이 한 줄보다 길어졌을 때만 조정
            {
                this.textBox.Top = (this.Height - this.textBox.Height) / 2;
            }

            // 라벨이 제대로 표시되는지 확인하기 위한 추가 처리
            this.label.Refresh();
            this.Refresh();
        }

        #endregion

        private System.Windows.Forms.Label label;
        private System.Windows.Forms.TextBox textBox;
    }
}