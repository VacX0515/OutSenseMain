using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Core.Devices.Gauges;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Forms
{
    public class TempCalibrationForm : Form
    {
        private TempCalibrationConfig _config;
        public TempCalibrationConfig Configuration => _config;

        private static readonly string[] ChannelNames = { "CH1 (메인1)", "CH2 (메인2)", "CH3 (확장1)", "CH4 (확장2)", "CH5 (확장3)" };

        private CheckBox[] chkEnabled = new CheckBox[5];
        private NumericUpDown[] numOffset = new NumericUpDown[5];
        private NumericUpDown[] numGain = new NumericUpDown[5];
        private TextBox[] txtNote = new TextBox[5];

        // 이온게이지
        private CheckBox chkIGEnabled;
        private ComboBox cmbIGModel;
        private NumericUpDown numIGGain;
        private NumericUpDown numIGVoltageOffset;
        private TextBox txtIGNote;

        public TempCalibrationForm(TempCalibrationConfig config)
        {
            _config = config ?? new TempCalibrationConfig();
            InitializeUI();
            LoadFromConfig();
        }

        private void InitializeUI()
        {
            Text = "센서 캘리브레이션";
            Size = new Size(560, 530);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;

            // ── 온도 센서 섹션 ──
            var lblTempTitle = new Label
            {
                Text = "■ 온도 센서  (보정값 = 측정값 × 게인 + 오프셋)",
                Location = new Point(15, 10),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            Controls.Add(lblTempTitle);

            int y = 32;
            AddHeaderLabel("채널", 15, y, 100);
            AddHeaderLabel("사용", 120, y, 40);
            AddHeaderLabel("오프셋 (°C)", 170, y, 100);
            AddHeaderLabel("게인", 280, y, 80);
            AddHeaderLabel("메모", 370, y, 160);

            y = 55;
            for (int i = 0; i < 5; i++)
            {
                int rowY = y + i * 30;

                Controls.Add(new Label
                {
                    Text = ChannelNames[i],
                    Location = new Point(15, rowY + 4),
                    AutoSize = true,
                    ForeColor = Color.White
                });

                chkEnabled[i] = new CheckBox
                {
                    Location = new Point(130, rowY + 3),
                    AutoSize = true
                };
                Controls.Add(chkEnabled[i]);

                numOffset[i] = new NumericUpDown
                {
                    Location = new Point(170, rowY),
                    Size = new Size(100, 23),
                    Minimum = -100,
                    Maximum = 100,
                    DecimalPlaces = 1,
                    Increment = 0.1m,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White
                };
                Controls.Add(numOffset[i]);

                numGain[i] = new NumericUpDown
                {
                    Location = new Point(280, rowY),
                    Size = new Size(80, 23),
                    Minimum = 0.5m,
                    Maximum = 2.0m,
                    DecimalPlaces = 4,
                    Increment = 0.001m,
                    Value = 1.0m,
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White
                };
                Controls.Add(numGain[i]);

                txtNote[i] = new TextBox
                {
                    Location = new Point(370, rowY),
                    Size = new Size(160, 23),
                    BackColor = Color.FromArgb(45, 45, 45),
                    ForeColor = Color.White
                };
                Controls.Add(txtNote[i]);
            }

            // ── 이온게이지 섹션 ──
            int igY = y + 5 * 30 + 15;

            // 구분선
            Controls.Add(new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(15, igY),
                Size = new Size(515, 2)
            });
            igY += 10;

            var lblIGTitle = new Label
            {
                Text = "■ 이온게이지",
                Location = new Point(15, igY),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            Controls.Add(lblIGTitle);
            igY += 25;

            // 모델 선택 행
            Controls.Add(new Label { Text = "모델", Location = new Point(15, igY + 4), AutoSize = true, ForeColor = Color.LightGray });
            cmbIGModel = new ComboBox
            {
                Location = new Point(60, igY),
                Size = new Size(120, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            foreach (IonGaugeModel model in Enum.GetValues(typeof(IonGaugeModel)))
                cmbIGModel.Items.Add(IonGauge.GetModelDisplayName(model));
            cmbIGModel.SelectedIndex = 0;
            Controls.Add(cmbIGModel);

            Controls.Add(new Label { Text = "사용", Location = new Point(200, igY + 4), AutoSize = true, ForeColor = Color.LightGray });
            chkIGEnabled = new CheckBox { Location = new Point(240, igY + 3), AutoSize = true };
            Controls.Add(chkIGEnabled);

            igY += 28;

            // 캘리브레이션 행
            Controls.Add(new Label { Text = "전압 오프셋 (V)", Location = new Point(15, igY + 4), AutoSize = true, ForeColor = Color.LightGray });
            numIGVoltageOffset = new NumericUpDown
            {
                Location = new Point(120, igY),
                Size = new Size(80, 23),
                Minimum = -1.0m,
                Maximum = 1.0m,
                DecimalPlaces = 3,
                Increment = 0.001m,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            Controls.Add(numIGVoltageOffset);

            Controls.Add(new Label { Text = "압력 게인", Location = new Point(220, igY + 4), AutoSize = true, ForeColor = Color.LightGray });
            numIGGain = new NumericUpDown
            {
                Location = new Point(295, igY),
                Size = new Size(80, 23),
                Minimum = 0.1m,
                Maximum = 10.0m,
                DecimalPlaces = 3,
                Increment = 0.01m,
                Value = 1.0m,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            Controls.Add(numIGGain);

            igY += 28;
            Controls.Add(new Label { Text = "메모", Location = new Point(15, igY + 4), AutoSize = true, ForeColor = Color.LightGray });
            txtIGNote = new TextBox
            {
                Location = new Point(60, igY),
                Size = new Size(470, 23),
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            Controls.Add(txtIGNote);

            // ── 버튼 ──
            int btnY = igY + 40;

            var btnReset = new Button
            {
                Text = "초기화",
                Location = new Point(15, btnY),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnReset.Click += (s, e) =>
            {
                if (MessageBox.Show("모든 캘리브레이션을 초기화하시겠습니까?", "확인",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _config = new TempCalibrationConfig();
                    LoadFromConfig();
                }
            };
            Controls.Add(btnReset);

            var btnOK = new Button
            {
                Text = "저장",
                Location = new Point(340, btnY),
                Size = new Size(90, 30),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnOK.Click += (s, e) => SaveToConfig();
            Controls.Add(btnOK);

            var btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(440, btnY),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            Controls.Add(btnCancel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void AddHeaderLabel(string text, int x, int y, int width)
        {
            Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                ForeColor = Color.LightGray,
                Font = new Font(Font, FontStyle.Bold)
            });
        }

        private void LoadFromConfig()
        {
            for (int i = 0; i < 5; i++)
            {
                var cal = _config.Channels[i];
                chkEnabled[i].Checked = cal.Enabled;
                numOffset[i].Value = ClampDecimal(cal.Offset, numOffset[i]);
                numGain[i].Value = ClampDecimal(cal.Gain, numGain[i]);
                txtNote[i].Text = cal.Note ?? "";
            }

            var ig = _config.IonGauge;
            cmbIGModel.SelectedIndex = (int)ig.Model;
            chkIGEnabled.Checked = ig.Enabled;
            numIGGain.Value = ClampDecimal(ig.Gain, numIGGain);
            numIGVoltageOffset.Value = ClampDecimal(ig.VoltageOffset, numIGVoltageOffset);
            txtIGNote.Text = ig.Note ?? "";
        }

        private void SaveToConfig()
        {
            for (int i = 0; i < 5; i++)
            {
                _config.Channels[i].Enabled = chkEnabled[i].Checked;
                _config.Channels[i].Offset = (double)numOffset[i].Value;
                _config.Channels[i].Gain = (double)numGain[i].Value;
                _config.Channels[i].Note = txtNote[i].Text;
            }

            _config.IonGauge.Model = (IonGaugeModel)cmbIGModel.SelectedIndex;
            _config.IonGauge.Enabled = chkIGEnabled.Checked;
            _config.IonGauge.Gain = (double)numIGGain.Value;
            _config.IonGauge.VoltageOffset = (double)numIGVoltageOffset.Value;
            _config.IonGauge.Note = txtIGNote.Text;
        }

        private static decimal ClampDecimal(double value, NumericUpDown nud)
        {
            return (decimal)Math.Max((double)nud.Minimum, Math.Min((double)nud.Maximum, value));
        }
    }
}
