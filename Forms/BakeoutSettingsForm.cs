using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VacX_OutSense.Core.Control;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// 베이크 아웃 램프 업 설정 창
    /// </summary>
    public class BakeoutSettingsForm : Form
    {
        #region 필드

        private ThermalRampProfileManager _profileManager;
        private BakeoutSettings _settings;

        // 온도 설정 컨트롤
        private NumericUpDown numTargetTemp;
        private NumericUpDown numRampRate;
        private ComboBox cboProfile;
        private Label lblProfileDesc;

        // 타이머 설정 컨트롤
        private NumericUpDown numHoldTime;
        private ComboBox cboEndAction;
        private CheckBox chkAutoStartTimer;

        // 램프 업 활성화
        private CheckBox chkEnableAutoRampUp;

        // 버튼
        private Button btnOK;
        private Button btnCancel;

        #endregion

        #region 속성

        /// <summary>
        /// 현재 설정
        /// </summary>
        public BakeoutSettings Settings => _settings;

        #endregion

        #region 생성자

        public BakeoutSettingsForm(ThermalRampProfileManager profileManager)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            _settings = BakeoutSettings.Load();

            InitializeComponent();
            LoadSettings();
        }

        #endregion

        #region 초기화

        private void InitializeComponent()
        {
            this.Text = "베이크 아웃 램프 업 설정";
            this.Size = new Size(420, 420);
            this.MinimumSize = new Size(400, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int y = 15;

            // === 램프 업 활성화 ===
            chkEnableAutoRampUp = new CheckBox
            {
                Text = "진공도 도달 시 램프 업 자동 시작",
                Location = new Point(20, y),
                Size = new Size(360, 24),
                Font = new Font(this.Font.FontFamily, 9.5f, FontStyle.Bold),
                Checked = true
            };
            chkEnableAutoRampUp.CheckedChanged += ChkEnableAutoRampUp_CheckedChanged;
            this.Controls.Add(chkEnableAutoRampUp);
            y += 35;

            // === 온도 설정 그룹 ===
            var grpTemp = new GroupBox
            {
                Text = "온도 설정",
                Location = new Point(15, y),
                Size = new Size(375, 145)
            };

            int innerY = 25;

            // 목표 온도
            var lblTargetTemp = new Label
            {
                Text = "목표 온도:",
                Location = new Point(15, innerY + 3),
                Size = new Size(80, 20)
            };
            numTargetTemp = new NumericUpDown
            {
                Location = new Point(100, innerY),
                Size = new Size(80, 23),
                Minimum = 30,
                Maximum = 300,
                Value = 100,
                DecimalPlaces = 1
            };
            var lblTempUnit = new Label
            {
                Text = "°C",
                Location = new Point(185, innerY + 3),
                Size = new Size(30, 20)
            };
            grpTemp.Controls.AddRange(new Control[] { lblTargetTemp, numTargetTemp, lblTempUnit });
            innerY += 30;

            // 승온 속도
            var lblRampRate = new Label
            {
                Text = "승온 속도:",
                Location = new Point(15, innerY + 3),
                Size = new Size(80, 20)
            };
            numRampRate = new NumericUpDown
            {
                Location = new Point(100, innerY),
                Size = new Size(80, 23),
                Minimum = 0.5M,
                Maximum = 30,
                Value = 5,
                DecimalPlaces = 1,
                Increment = 0.5M
            };
            var lblRateUnit = new Label
            {
                Text = "°C/min",
                Location = new Point(185, innerY + 3),
                Size = new Size(60, 20)
            };
            grpTemp.Controls.AddRange(new Control[] { lblRampRate, numRampRate, lblRateUnit });
            innerY += 30;

            // 프로파일 선택
            var lblProfile = new Label
            {
                Text = "프로파일:",
                Location = new Point(15, innerY + 3),
                Size = new Size(80, 20)
            };
            cboProfile = new ComboBox
            {
                Location = new Point(100, innerY),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboProfile.SelectedIndexChanged += CboProfile_SelectedIndexChanged;

            var btnManageProfile = new Button
            {
                Text = "관리...",
                Location = new Point(290, innerY - 1),
                Size = new Size(60, 25)
            };
            btnManageProfile.Click += BtnManageProfile_Click;
            grpTemp.Controls.AddRange(new Control[] { lblProfile, cboProfile, btnManageProfile });
            innerY += 30;

            // 프로파일 설명
            lblProfileDesc = new Label
            {
                Location = new Point(100, innerY),
                Size = new Size(250, 30),
                ForeColor = Color.Gray,
                Text = ""
            };
            grpTemp.Controls.Add(lblProfileDesc);

            this.Controls.Add(grpTemp);
            y += grpTemp.Height + 10;

            // === 타이머 설정 그룹 ===
            var grpTimer = new GroupBox
            {
                Text = "타이머 설정",
                Location = new Point(15, y),
                Size = new Size(375, 115)
            };

            innerY = 25;

            // 유지 시간
            var lblHoldTime = new Label
            {
                Text = "유지 시간:",
                Location = new Point(15, innerY + 3),
                Size = new Size(80, 20)
            };
            numHoldTime = new NumericUpDown
            {
                Location = new Point(100, innerY),
                Size = new Size(80, 23),
                Minimum = 1,
                Maximum = 1440,
                Value = 30
            };
            var lblTimeUnit = new Label
            {
                Text = "분",
                Location = new Point(185, innerY + 3),
                Size = new Size(30, 20)
            };
            grpTimer.Controls.AddRange(new Control[] { lblHoldTime, numHoldTime, lblTimeUnit });
            innerY += 30;

            // 종료 동작
            var lblEndAction = new Label
            {
                Text = "종료 동작:",
                Location = new Point(15, innerY + 3),
                Size = new Size(80, 20)
            };
            cboEndAction = new ComboBox
            {
                Location = new Point(100, innerY),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboEndAction.Items.AddRange(new object[] { "히터 OFF", "현재 온도 유지", "알림만 (수동 조작)" });
            cboEndAction.SelectedIndex = 0;
            grpTimer.Controls.AddRange(new Control[] { lblEndAction, cboEndAction });
            innerY += 30;

            // 타이머 자동 시작
            chkAutoStartTimer = new CheckBox
            {
                Text = "목표 온도 도달 시 타이머 자동 시작",
                Location = new Point(15, innerY),
                Size = new Size(300, 20),
                Checked = true
            };
            grpTimer.Controls.Add(chkAutoStartTimer);

            this.Controls.Add(grpTimer);
            y += grpTimer.Height + 15;

            // === 버튼 ===
            btnOK = new Button
            {
                Text = "확인",
                Location = new Point(200, y),
                Size = new Size(85, 30),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "취소",
                Location = new Point(295, y),
                Size = new Size(85, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] { btnOK, btnCancel });
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;

            // 프로파일 목록 로드
            LoadProfiles();
        }

        #endregion

        #region 프로파일 관리

        private void LoadProfiles()
        {
            cboProfile.Items.Clear();
            foreach (var profile in _profileManager.Profiles)
            {
                cboProfile.Items.Add(profile.Name);
            }

            // 저장된 프로파일 선택
            int index = cboProfile.Items.IndexOf(_settings.ProfileName);
            if (index >= 0)
            {
                cboProfile.SelectedIndex = index;
            }
            else if (cboProfile.Items.Count > 0)
            {
                cboProfile.SelectedIndex = 0;
            }
        }

        private void CboProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboProfile.SelectedIndex < 0) return;

            var profile = _profileManager.GetProfile(cboProfile.SelectedItem.ToString());
            if (profile != null)
            {
                lblProfileDesc.Text = profile.Description?.Split('\n')[0] ?? "";

                // 범위 제한 적용
                numRampRate.Minimum = (decimal)profile.MinRampRate;
                numRampRate.Maximum = (decimal)profile.MaxRampRate;
                numTargetTemp.Minimum = (decimal)profile.MinTargetTemperature;
                numTargetTemp.Maximum = (decimal)profile.MaxHeaterTemperature;

                // 현재 값이 범위를 벗어나면 조정
                if (numRampRate.Value < numRampRate.Minimum)
                    numRampRate.Value = numRampRate.Minimum;
                if (numRampRate.Value > numRampRate.Maximum)
                    numRampRate.Value = numRampRate.Maximum;
                if (numTargetTemp.Value < numTargetTemp.Minimum)
                    numTargetTemp.Value = numTargetTemp.Minimum;
                if (numTargetTemp.Value > numTargetTemp.Maximum)
                    numTargetTemp.Value = numTargetTemp.Maximum;
            }
        }

        private void BtnManageProfile_Click(object sender, EventArgs e)
        {
            using (var form = new ThermalRampProfileEditorForm(_profileManager))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string currentProfile = cboProfile.SelectedItem?.ToString();
                    LoadProfiles();

                    // 이전 선택 복원
                    int index = cboProfile.Items.IndexOf(currentProfile);
                    if (index >= 0)
                        cboProfile.SelectedIndex = index;
                }
            }
        }

        #endregion

        #region 설정 로드/저장

        private void LoadSettings()
        {
            numTargetTemp.Value = (decimal)_settings.TargetTemperature;
            numRampRate.Value = (decimal)_settings.RampRate;
            numHoldTime.Value = _settings.HoldTimeMinutes;
            cboEndAction.SelectedIndex = (int)_settings.EndAction;
            chkAutoStartTimer.Checked = _settings.AutoStartTimerOnTargetReached;
            chkEnableAutoRampUp.Checked = _settings.EnableAutoRampUp;

            // 프로파일은 LoadProfiles에서 처리
        }

        private void SaveSettings()
        {
            _settings.TargetTemperature = (double)numTargetTemp.Value;
            _settings.RampRate = (double)numRampRate.Value;
            _settings.ProfileName = cboProfile.SelectedItem?.ToString() ?? "일반 시편";
            _settings.HoldTimeMinutes = (int)numHoldTime.Value;
            _settings.EndAction = (BakeoutEndAction)cboEndAction.SelectedIndex;
            _settings.AutoStartTimerOnTargetReached = chkAutoStartTimer.Checked;
            _settings.EnableAutoRampUp = chkEnableAutoRampUp.Checked;

            _settings.Save();
        }

        #endregion

        #region 이벤트 핸들러

        private void ChkEnableAutoRampUp_CheckedChanged(object sender, EventArgs e)
        {
            bool enabled = chkEnableAutoRampUp.Checked;

            // 온도/타이머 설정 활성화/비활성화
            numTargetTemp.Enabled = enabled;
            numRampRate.Enabled = enabled;
            cboProfile.Enabled = enabled;
            numHoldTime.Enabled = enabled;
            cboEndAction.Enabled = enabled;
            chkAutoStartTimer.Enabled = enabled;
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        #endregion
    }
}
