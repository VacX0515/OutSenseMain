using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Core.Control;

namespace VacX_OutSense.Forms
{
    public partial class HoldModeSettingsForm : Form
    {
        public HoldModeSettings Settings { get; private set; }

        public HoldModeSettingsForm(HoldModeSettings current)
        {
            InitializeComponent();
            Settings = current ?? new HoldModeSettings();
            LoadSettings();
        }

        private void LoadSettings()
        {
            // 채널 선택
            chkCh2.Checked = Settings.UseCh2;
            chkCh3.Checked = Settings.UseCh3;
            chkCh4.Checked = Settings.UseCh4;
            chkCh5.Checked = Settings.UseCh5;

            // 파라미터
            numCheckInterval.Value = Settings.CheckIntervalMinutes;
            numErrorTolerance.Value = (decimal)Settings.ErrorTolerance;
            numMinHeaterTemp.Value = (decimal)Settings.MinHeaterTemp;
            numMaxHeaterTemp.Value = (decimal)Settings.MaxHeaterTemp;
            numAdjustMultiplier.Value = (decimal)Settings.AdjustmentMultiplier;

            UpdatePreview();
        }

        private void UpdatePreview()
        {
            var tempSettings = GetCurrentSettings();
            lblPreview.Text = $"제어 온도: {tempSettings.GetSourceText()}";
        }

        private HoldModeSettings GetCurrentSettings()
        {
            return new HoldModeSettings
            {
                UseCh2 = chkCh2.Checked,
                UseCh3 = chkCh3.Checked,
                UseCh4 = chkCh4.Checked,
                UseCh5 = chkCh5.Checked,
                CheckIntervalMinutes = (int)numCheckInterval.Value,
                ErrorTolerance = (double)numErrorTolerance.Value,
                MinHeaterTemp = (double)numMinHeaterTemp.Value,
                MaxHeaterTemp = (double)numMaxHeaterTemp.Value,
                AdjustmentMultiplier = (double)numAdjustMultiplier.Value
            };
        }

        private void chkChannel_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreview();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            // 유효성 검사
            if (!chkCh2.Checked && !chkCh3.Checked && !chkCh4.Checked && !chkCh5.Checked)
            {
                MessageBox.Show("최소 하나의 채널을 선택하세요.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (numMinHeaterTemp.Value >= numMaxHeaterTemp.Value)
            {
                MessageBox.Show("최소 온도는 최대 온도보다 작아야 합니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Settings = GetCurrentSettings();
            Settings.Save();

            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}