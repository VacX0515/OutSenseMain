using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// 칠러 PID 파라미터 편집 다이얼로그.
    /// 코드 전용 레이아웃 — Designer 파일 없음. AutoScaleMode.Dpi로 DPI 안전.
    /// </summary>
    public class ChillerPIDSettingsForm : Form
    {
        private readonly ChillerPIDControlService _service;

        private ComboBox _cmbChannel;
        private NumericUpDown _numTarget;
        private NumericUpDown _numUpdate;
        private NumericUpDown _numKp;
        private NumericUpDown _numKi;
        private NumericUpDown _numKd;
        private NumericUpDown _numDeadband;
        private CheckBox _chkAdaptive;
        private Label _lblCurrent;
        private Label _lblLearned;
        private Button _btnReset;
        private Button _btnApply;
        private Button _btnClose;

        public ChillerPIDSettingsForm(ChillerPIDControlService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
            BuildUi();
            LoadFromService();
        }

        private void BuildUi()
        {
            SuspendLayout();

            Text = "칠러 PID 설정";
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96f, 96f);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(360, 420);
            Font = new Font("맑은 고딕", 9f);

            int rowH = 28;
            int labelW = 110;
            int inputW = 160;
            int leftPad = 16;
            int y = 16;

            // 채널
            Controls.Add(new Label { Text = "목표 채널:", Left = leftPad, Top = y + 4, Width = labelW });
            _cmbChannel = new ComboBox
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            for (int i = 1; i <= 12; i++)
                _cmbChannel.Items.Add($"CH{i}");
            Controls.Add(_cmbChannel);
            y += rowH;

            // 목표 온도
            Controls.Add(new Label { Text = "목표 온도 (°C):", Left = leftPad, Top = y + 4, Width = labelW });
            _numTarget = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = -10m,
                Maximum = 80m,
                DecimalPlaces = 1,
                Increment = 0.5m
            };
            Controls.Add(_numTarget);
            y += rowH;

            // Update 주기
            Controls.Add(new Label { Text = "Update 주기 (s):", Left = leftPad, Top = y + 4, Width = labelW });
            _numUpdate = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = 1m,
                Maximum = 600m,
                DecimalPlaces = 0,
                Increment = 5m
            };
            Controls.Add(_numUpdate);
            y += rowH + 8;

            // 구분선
            var sep1 = new Label
            {
                Left = leftPad,
                Top = y,
                Width = ClientSize.Width - leftPad * 2,
                Height = 2,
                BorderStyle = BorderStyle.Fixed3D
            };
            Controls.Add(sep1);
            y += 12;

            // Kp
            Controls.Add(new Label { Text = "Kp:", Left = leftPad, Top = y + 4, Width = labelW });
            _numKp = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = 0.001m,
                Maximum = 10m,
                DecimalPlaces = 4,
                Increment = 0.05m
            };
            Controls.Add(_numKp);
            y += rowH;

            // Ki
            Controls.Add(new Label { Text = "Ki:", Left = leftPad, Top = y + 4, Width = labelW });
            _numKi = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = 0.0001m,
                Maximum = 1m,
                DecimalPlaces = 4,
                Increment = 0.001m
            };
            Controls.Add(_numKi);
            y += rowH;

            // Kd
            Controls.Add(new Label { Text = "Kd:", Left = leftPad, Top = y + 4, Width = labelW });
            _numKd = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = 0m,
                Maximum = 20m,
                DecimalPlaces = 3,
                Increment = 0.1m
            };
            Controls.Add(_numKd);
            y += rowH;

            // Deadband
            Controls.Add(new Label { Text = "Deadband (°C):", Left = leftPad, Top = y + 4, Width = labelW });
            _numDeadband = new NumericUpDown
            {
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Minimum = 0m,
                Maximum = 5m,
                DecimalPlaces = 2,
                Increment = 0.1m
            };
            Controls.Add(_numDeadband);
            y += rowH;

            // Adaptive
            _chkAdaptive = new CheckBox
            {
                Text = "Adaptive 학습 사용",
                Left = leftPad + labelW,
                Top = y,
                Width = inputW,
                Height = 24
            };
            Controls.Add(_chkAdaptive);
            y += rowH + 6;

            // 정보 라벨
            _lblCurrent = new Label
            {
                Left = leftPad,
                Top = y,
                Width = ClientSize.Width - leftPad * 2,
                Height = 18,
                ForeColor = Color.DarkSlateGray
            };
            Controls.Add(_lblCurrent);
            y += 18;

            _lblLearned = new Label
            {
                Left = leftPad,
                Top = y,
                Width = ClientSize.Width - leftPad * 2,
                Height = 18,
                ForeColor = Color.DarkBlue
            };
            Controls.Add(_lblLearned);
            y += 24;

            // 버튼
            int btnW = 90;
            int btnH = 28;
            int btnY = ClientSize.Height - btnH - 14;
            int gap = 8;

            _btnReset = new Button
            {
                Text = "기본값 복원",
                Left = leftPad,
                Top = btnY,
                Width = btnW,
                Height = btnH
            };
            _btnReset.Click += BtnReset_Click;
            Controls.Add(_btnReset);

            _btnApply = new Button
            {
                Text = "적용",
                Left = ClientSize.Width - leftPad - btnW * 2 - gap,
                Top = btnY,
                Width = btnW,
                Height = btnH
            };
            _btnApply.Click += BtnApply_Click;
            Controls.Add(_btnApply);

            _btnClose = new Button
            {
                Text = "닫기",
                Left = ClientSize.Width - leftPad - btnW,
                Top = btnY,
                Width = btnW,
                Height = btnH,
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_btnClose);
            CancelButton = _btnClose;
            AcceptButton = _btnApply;

            ResumeLayout(false);
        }

        private void LoadFromService()
        {
            _cmbChannel.SelectedIndex = Math.Max(0, Math.Min(11, _service.TargetChannelIndex));
            _numTarget.Value = ClampDecimal((decimal)_service.TargetTemperature, _numTarget.Minimum, _numTarget.Maximum);
            _numUpdate.Value = ClampDecimal((decimal)_service.UpdateInterval, _numUpdate.Minimum, _numUpdate.Maximum);
            _numKp.Value = ClampDecimal((decimal)_service.PID.Kp, _numKp.Minimum, _numKp.Maximum);
            _numKi.Value = ClampDecimal((decimal)_service.PID.Ki, _numKi.Minimum, _numKi.Maximum);
            _numKd.Value = ClampDecimal((decimal)_service.PID.Kd, _numKd.Minimum, _numKd.Maximum);

            double dead = _service.Deadband;
            if (dead <= 0) dead = 0.5;
            _numDeadband.Value = ClampDecimal((decimal)dead, _numDeadband.Minimum, _numDeadband.Maximum);

            _chkAdaptive.Checked = _service.AdaptiveEnabled;

            UpdateInfoLabels();
        }

        private void UpdateInfoLabels()
        {
            _lblCurrent.Text = $"현재값  Kp={_service.PID.Kp:F3}  Ki={_service.PID.Ki:F4}  Kd={_service.PID.Kd:F3}";
            _lblLearned.Text = $"Adaptive  조정횟수={_service.Adaptive.TotalAdjustments}  최근:{_service.Adaptive.LastAdjustmentReason}";
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            try
            {
                _service.TargetChannelIndex = _cmbChannel.SelectedIndex;
                _service.TargetTemperature = (double)_numTarget.Value;
                _service.UpdateInterval = (double)_numUpdate.Value;
                _service.Deadband = (double)_numDeadband.Value;
                _service.AdaptiveEnabled = _chkAdaptive.Checked;

                // PID 게인 — 한 번에 baseline까지 같이 갱신.
                _service.SetPIDParameters(
                    (double)_numKp.Value,
                    (double)_numKi.Value,
                    (double)_numKd.Value);

                UpdateInfoLabels();
                MessageBox.Show(this, "적용되었습니다.\nbaseline + PID 게인이 동시에 갱신되었고 XML에 저장되었습니다.",
                    "칠러 PID 설정", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"적용 실패: {ex.Message}",
                    "칠러 PID 설정", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            // 보수적인 안전 기본값 — 칠러 dead-time 큰 시스템 기준.
            _numKp.Value = 0.3m;
            _numKi.Value = 0.001m;
            _numKd.Value = 0.7m;
            _numDeadband.Value = 0.5m;
            _numUpdate.Value = 30m;
        }

        private static decimal ClampDecimal(decimal value, decimal min, decimal max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
