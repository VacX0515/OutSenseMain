using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// 간편 열 램프 제어 UserControl
    /// 직관적인 UI로 온도 램프 기능 제공
    /// </summary>
    public partial class SimpleRampControl : UserControl
    {
        #region 필드

        private TempController _tempController;
        private SimpleThermalRampController _rampController;
        private ThermalRampProfileManager _profileManager;
        private System.Windows.Forms.Timer _updateTimer;

        #endregion

        #region 이벤트

        /// <summary>
        /// 목표 온도 도달 시 발생 (타이머 시작용)
        /// </summary>
        public event EventHandler TargetTemperatureReached;

        /// <summary>
        /// 로그 메시지
        /// </summary>
        public event EventHandler<string> LogMessage;

        #endregion

        #region 속성

        /// <summary>
        /// 목표 온도 도달 시 자동으로 타이머 시작 여부
        /// </summary>
        public bool AutoStartTimerOnTargetReached
        {
            get => chkAutoStartTimer.Checked;
            set => chkAutoStartTimer.Checked = value;
        }

        /// <summary>
        /// 현재 실행 중 여부
        /// </summary>
        public bool IsRunning => _rampController?.IsRunning ?? false;

        /// <summary>
        /// 내부 램프 컨트롤러 (외부 통신 조율용)
        /// </summary>
        public SimpleThermalRampController RampController => _rampController;

        /// <summary>
        /// 종료 동작 설정
        /// </summary>
        public BakeoutEndAction EndAction { get; set; } = BakeoutEndAction.MaintainTemperature;

        /// <summary>
        /// 램프 완료 후 Hold 모드 유지 여부 설정
        /// </summary>
        public bool HoldAfterComplete
        {
            get => _rampController?.HoldAfterComplete ?? true;
            set
            {
                if (_rampController != null)
                {
                    _rampController.HoldAfterComplete = value;
                }
            }
        }

        #endregion

        #region 컨트롤 선언

        private GroupBox grpSettings;
        private GroupBox grpStatus;
        private GroupBox grpControl;

        private Label lblTargetTemp;
        private NumericUpDown numTargetTemp;
        private Label lblTempUnit1;

        private Label lblRampRate;
        private NumericUpDown numRampRate;
        private Label lblRampRateUnit;

        private Label lblProfile;
        private ComboBox cboProfile;
        private Button btnProfileManager;

        private CheckBox chkAutoStartTimer;

        private Label lblHeaterTemp;
        private Label lblHeaterTempValue;
        private Label lblHeaterSetpoint;
        private Label lblHeaterSetpointValue;
        private Label lblSampleTemp;
        private Label lblSampleTempValue;
        private Label lblTargetTempStatus;
        private Label lblTargetTempStatusValue;
        private Label lblProgress;
        private ProgressBar progressBar;
        private Label lblProgressPercent;
        private Label lblStatus;
        private Label lblStatusValue;
        private Label lblElapsedTime;
        private Label lblElapsedTimeValue;

        private Button btnStart;
        private Button btnStop;
        private Button btnEmergencyStop;

        private RichTextBox txtLog;

        #endregion

        #region 생성자

        public SimpleRampControl()
        {
            InitializeComponent();

            if (!DesignMode)
            {
                InitializeTimer();
            }
        }

        #endregion

        #region 초기화

        private void InitializeComponent()
        {
            this.Size = new Size(450, 650);
            this.BackColor = SystemColors.Control;

            int y = 10;

            // === 설정 그룹 ===
            grpSettings = new GroupBox
            {
                Text = "램프 설정",
                Location = new Point(10, y),
                Size = new Size(430, 180)
            };

            int innerY = 25;

            // 목표 온도
            lblTargetTemp = new Label
            {
                Text = "목표 온도 (CH2):",
                Location = new Point(15, innerY + 3),
                Size = new Size(100, 20)
            };
            numTargetTemp = new NumericUpDown
            {
                Location = new Point(120, innerY),
                Size = new Size(80, 23),
                Minimum = 25,
                Maximum = 300,
                Value = 100,
                DecimalPlaces = 1
            };
            lblTempUnit1 = new Label
            {
                Text = "°C",
                Location = new Point(205, innerY + 3),
                Size = new Size(30, 20)
            };
            grpSettings.Controls.AddRange(new Control[] { lblTargetTemp, numTargetTemp, lblTempUnit1 });
            innerY += 30;

            // 램프 속도
            lblRampRate = new Label
            {
                Text = "승온 속도:",
                Location = new Point(15, innerY + 3),
                Size = new Size(100, 20)
            };
            numRampRate = new NumericUpDown
            {
                Location = new Point(120, innerY),
                Size = new Size(80, 23),
                Minimum = 0.5M,
                Maximum = 30,
                Value = 5,
                DecimalPlaces = 1,
                Increment = 0.5M
            };
            lblRampRateUnit = new Label
            {
                Text = "°C/min",
                Location = new Point(205, innerY + 3),
                Size = new Size(60, 20)
            };
            grpSettings.Controls.AddRange(new Control[] { lblRampRate, numRampRate, lblRampRateUnit });
            innerY += 30;

            // 프로파일 선택
            lblProfile = new Label
            {
                Text = "샘플 프로파일:",
                Location = new Point(15, innerY + 3),
                Size = new Size(100, 20)
            };
            cboProfile = new ComboBox
            {
                Location = new Point(120, innerY),
                Size = new Size(180, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboProfile.SelectedIndexChanged += CboProfile_SelectedIndexChanged;

            btnProfileManager = new Button
            {
                Text = "관리...",
                Location = new Point(310, innerY - 1),
                Size = new Size(60, 25)
            };
            btnProfileManager.Click += BtnProfileManager_Click;
            grpSettings.Controls.AddRange(new Control[] { lblProfile, cboProfile, btnProfileManager });
            innerY += 35;

            // 프로파일 설명
            var lblProfileDesc = new Label
            {
                Name = "lblProfileDesc",
                Location = new Point(120, innerY),
                Size = new Size(250, 35),
                ForeColor = Color.Gray,
                Text = ""
            };
            grpSettings.Controls.Add(lblProfileDesc);
            innerY += 40;

            // 타이머 자동 시작 옵션
            chkAutoStartTimer = new CheckBox
            {
                Text = "목표 온도 도달 시 타이머 자동 시작",
                Location = new Point(15, innerY),
                Size = new Size(250, 20),
                Checked = true
            };
            grpSettings.Controls.Add(chkAutoStartTimer);

            this.Controls.Add(grpSettings);
            y += grpSettings.Height + 10;

            // === 상태 그룹 ===
            grpStatus = new GroupBox
            {
                Text = "현재 상태",
                Location = new Point(10, y),
                Size = new Size(430, 200)
            };

            innerY = 25;
            int col1 = 15, col2 = 120, col3 = 230, col4 = 335;

            // 히터 온도 (CH1)
            lblHeaterTemp = new Label { Text = "히터 (CH1):", Location = new Point(col1, innerY), Size = new Size(100, 20) };
            lblHeaterTempValue = new Label { Text = "--.--°C", Location = new Point(col2, innerY), Size = new Size(80, 20), Font = new Font(this.Font, FontStyle.Bold) };
            lblHeaterSetpoint = new Label { Text = "설정:", Location = new Point(col3, innerY), Size = new Size(40, 20) };
            lblHeaterSetpointValue = new Label { Text = "--.--°C", Location = new Point(col4, innerY), Size = new Size(70, 20) };
            grpStatus.Controls.AddRange(new Control[] { lblHeaterTemp, lblHeaterTempValue, lblHeaterSetpoint, lblHeaterSetpointValue });
            innerY += 25;

            // 샘플 온도 (CH2)
            lblSampleTemp = new Label { Text = "샘플 (CH2):", Location = new Point(col1, innerY), Size = new Size(100, 20) };
            lblSampleTempValue = new Label { Text = "--.--°C", Location = new Point(col2, innerY), Size = new Size(80, 20), Font = new Font(this.Font.FontFamily, 12, FontStyle.Bold), ForeColor = Color.Blue };
            lblTargetTempStatus = new Label { Text = "목표:", Location = new Point(col3, innerY), Size = new Size(40, 20) };
            lblTargetTempStatusValue = new Label { Text = "--.--°C", Location = new Point(col4, innerY), Size = new Size(70, 20) };
            grpStatus.Controls.AddRange(new Control[] { lblSampleTemp, lblSampleTempValue, lblTargetTempStatus, lblTargetTempStatusValue });
            innerY += 30;

            // 진행률
            lblProgress = new Label { Text = "진행률:", Location = new Point(col1, innerY), Size = new Size(60, 20) };
            progressBar = new ProgressBar { Location = new Point(col2, innerY), Size = new Size(200, 20), Minimum = 0, Maximum = 100 };
            lblProgressPercent = new Label { Text = "0%", Location = new Point(col4, innerY), Size = new Size(50, 20) };
            grpStatus.Controls.AddRange(new Control[] { lblProgress, progressBar, lblProgressPercent });
            innerY += 30;

            // 상태
            lblStatus = new Label { Text = "상태:", Location = new Point(col1, innerY), Size = new Size(60, 20) };
            lblStatusValue = new Label { Text = "대기", Location = new Point(col2, innerY), Size = new Size(280, 20), ForeColor = Color.Gray };
            grpStatus.Controls.AddRange(new Control[] { lblStatus, lblStatusValue });
            innerY += 25;

            // 경과 시간
            lblElapsedTime = new Label { Text = "경과 시간:", Location = new Point(col1, innerY), Size = new Size(80, 20) };
            lblElapsedTimeValue = new Label { Text = "00:00:00", Location = new Point(col2, innerY), Size = new Size(100, 20) };
            grpStatus.Controls.AddRange(new Control[] { lblElapsedTime, lblElapsedTimeValue });

            this.Controls.Add(grpStatus);
            y += grpStatus.Height + 10;

            // === 제어 버튼 그룹 ===
            grpControl = new GroupBox
            {
                Text = "제어",
                Location = new Point(10, y),
                Size = new Size(430, 60)
            };

            btnStart = new Button
            {
                Text = "▶ 시작",
                Location = new Point(15, 22),
                Size = new Size(100, 30),
                BackColor = Color.LightGreen
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "■ 정지",
                Location = new Point(125, 22),
                Size = new Size(100, 30),
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            btnEmergencyStop = new Button
            {
                Text = "⚠ 비상정지",
                Location = new Point(300, 22),
                Size = new Size(110, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnEmergencyStop.Click += BtnEmergencyStop_Click;

            grpControl.Controls.AddRange(new Control[] { btnStart, btnStop, btnEmergencyStop });
            this.Controls.Add(grpControl);
            y += grpControl.Height + 10;

            // === 로그 ===
            var lblLog = new Label
            {
                Text = "로그:",
                Location = new Point(10, y),
                Size = new Size(50, 20)
            };
            this.Controls.Add(lblLog);
            y += 20;

            txtLog = new RichTextBox
            {
                Location = new Point(10, y),
                Size = new Size(430, 100),
                ReadOnly = true,
                BackColor = Color.White,
                Font = new Font("Consolas", 9)
            };
            this.Controls.Add(txtLog);
        }

        private void InitializeTimer()
        {
            _updateTimer = new System.Windows.Forms.Timer { Interval = 500 };
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        /// <summary>
        /// 컨트롤 초기화 (런타임에서 호출)
        /// </summary>
        public void Initialize(TempController tempController)
        {
            if (DesignMode) return;

            _tempController = tempController ?? throw new ArgumentNullException(nameof(tempController));

            // 프로파일 매니저 초기화
            _profileManager = new ThermalRampProfileManager();
            LoadProfiles();

            // 컨트롤러 초기화
            _rampController = new SimpleThermalRampController(_tempController);
            _rampController.ProgressUpdated += RampController_ProgressUpdated;
            _rampController.RampCompleted += RampController_RampCompleted;
            _rampController.TargetReached += RampController_TargetReached;
            _rampController.ErrorOccurred += RampController_ErrorOccurred;
            _rampController.LogMessage += RampController_LogMessage;

            // UI 업데이트 타이머 시작
            _updateTimer?.Start();

            AppendLog("초기화 완료");
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

            // 마지막 선택 프로파일 복원
            if (!string.IsNullOrEmpty(_profileManager.LastSelectedProfileName))
            {
                int index = cboProfile.Items.IndexOf(_profileManager.LastSelectedProfileName);
                if (index >= 0)
                {
                    cboProfile.SelectedIndex = index;
                }
            }

            if (cboProfile.SelectedIndex < 0 && cboProfile.Items.Count > 0)
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
                // 프로파일 설명 업데이트 (기본 설명만 표시)
                var lblDesc = grpSettings.Controls["lblProfileDesc"] as Label;
                if (lblDesc != null)
                {
                    lblDesc.Text = profile.Description ?? "";
                }

                // 최소/최대 범위 설정
                numRampRate.Minimum = (decimal)profile.MinRampRate;
                numRampRate.Maximum = (decimal)profile.MaxRampRate;
                numTargetTemp.Minimum = (decimal)profile.MinTargetTemperature;
                numTargetTemp.Maximum = (decimal)profile.MaxHeaterTemperature;

                // 기본값 적용
                numRampRate.Value = Math.Max(numRampRate.Minimum,
                    Math.Min(numRampRate.Maximum, (decimal)profile.DefaultRampRate));
                numTargetTemp.Value = Math.Max(numTargetTemp.Minimum,
                    Math.Min(numTargetTemp.Maximum, (decimal)profile.DefaultTargetTemperature));

                // 선택 저장
                _profileManager.LastSelectedProfileName = profile.Name;
            }
        }

        private void BtnProfileManager_Click(object sender, EventArgs e)
        {
            using (var form = new ThermalRampProfileEditorForm(_profileManager))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    LoadProfiles();
                }
            }
        }

        #endregion

        #region 제어 버튼

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (cboProfile.SelectedIndex < 0)
            {
                MessageBox.Show("프로파일을 선택하세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var profile = _profileManager.GetProfile(cboProfile.SelectedItem.ToString());
            if (profile == null)
            {
                MessageBox.Show("선택된 프로파일을 찾을 수 없습니다.", "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            double targetTemp = (double)numTargetTemp.Value;
            double rampRate = (double)numRampRate.Value;

            // 확인
            var result = MessageBox.Show(
                $"램프를 시작하시겠습니까?\n\n" +
                $"목표 온도: {targetTemp:F1}°C\n" +
                $"승온 속도: {rampRate:F1}°C/min\n" +
                $"프로파일: {profile.Name}",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            await StartRampInternalAsync(profile, targetTemp, rampRate);
        }

        /// <summary>
        /// 외부에서 램프 시작 (자동 시작용)
        /// </summary>
        /// <param name="targetTemp">목표 온도 (°C)</param>
        /// <param name="rampRate">승온 속도 (°C/min)</param>
        /// <param name="profileName">프로파일 이름</param>
        /// <returns>시작 성공 여부</returns>
        public async System.Threading.Tasks.Task<bool> StartRampAsync(double targetTemp, double rampRate, string profileName)
        {
            if (_rampController == null || _profileManager == null)
            {
                return false;
            }

            var profile = _profileManager.GetProfile(profileName);
            if (profile == null)
            {
                profile = _profileManager.Profiles.FirstOrDefault();
                if (profile == null) return false;
            }

            // UI 업데이트 (메인 스레드에서)
            if (InvokeRequired)
            {
                return await (System.Threading.Tasks.Task<bool>)Invoke(new Func<System.Threading.Tasks.Task<bool>>(async () =>
                {
                    return await StartRampInternalAsync(profile, targetTemp, rampRate);
                }));
            }

            return await StartRampInternalAsync(profile, targetTemp, rampRate);
        }

        private async System.Threading.Tasks.Task<bool> StartRampInternalAsync(ThermalRampProfile profile, double targetTemp, double rampRate)
        {
            // UI 값 업데이트 - 프로파일 먼저 선택 (Min/Max가 업데이트됨)
            if (cboProfile.Items.Contains(profile.Name))
            {
                cboProfile.SelectedItem = profile.Name;
            }

            // 값 범위 체크 후 설정
            decimal targetTempDecimal = (decimal)targetTemp;
            decimal rampRateDecimal = (decimal)rampRate;

            // Min/Max 범위 내로 제한
            targetTempDecimal = Math.Max(numTargetTemp.Minimum, Math.Min(numTargetTemp.Maximum, targetTempDecimal));
            rampRateDecimal = Math.Max(numRampRate.Minimum, Math.Min(numRampRate.Maximum, rampRateDecimal));

            numTargetTemp.Value = targetTempDecimal;
            numRampRate.Value = rampRateDecimal;

            // UI 상태 변경
            SetControlState(true);

            // 램프 시작
            bool success = await _rampController.StartRampAsync(profile, targetTemp, rampRate);

            if (!success)
            {
                SetControlState(false);
            }

            return success;
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "램프를 정지하시겠습니까?\n히터는 현재 상태를 유지합니다.",
                "확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _rampController?.Stop();
                SetControlState(false);
            }
        }

        private void BtnEmergencyStop_Click(object sender, EventArgs e)
        {
            _rampController?.EmergencyStop();
            SetControlState(false);
        }

        /// <summary>
        /// 종료 동작에 따라 정지 (외부 호출용)
        /// </summary>
        /// <param name="endAction">종료 동작</param>
        public void StopWithEndAction(BakeoutEndAction endAction)
        {
            if (_rampController == null) return;

            _rampController.StopWithAction(endAction);

            // MaintainTemperature인 경우 UI는 계속 실행 상태 유지
            if (endAction != BakeoutEndAction.MaintainTemperature)
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => SetControlState(false)));
                }
                else
                {
                    SetControlState(false);
                }
            }
        }

        private void SetControlState(bool running)
        {
            btnStart.Enabled = !running;
            btnStop.Enabled = running;
            btnEmergencyStop.Enabled = running;
            grpSettings.Enabled = !running;
        }

        #endregion

        #region 이벤트 핸들러

        private void RampController_ProgressUpdated(object sender, SimpleThermalRampController.RampProgressEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RampController_ProgressUpdated(sender, e)));
                return;
            }

            lblHeaterTempValue.Text = $"{e.HeaterTemp:F1}°C";
            lblHeaterSetpointValue.Text = $"{e.HeaterSetpoint:F1}°C";
            lblSampleTempValue.Text = $"{e.SampleTemp:F1}°C";
            lblTargetTempStatusValue.Text = $"{e.TargetTemp:F1}°C";

            progressBar.Value = (int)Math.Min(100, Math.Max(0, e.ProgressPercent));
            lblProgressPercent.Text = $"{e.ProgressPercent:F0}%";

            lblStatusValue.Text = e.StatusMessage;
            lblStatusValue.ForeColor = e.State switch
            {
                SimpleThermalRampController.RampState.Ramping => Color.Blue,
                SimpleThermalRampController.RampState.Stabilizing => Color.Orange,
                SimpleThermalRampController.RampState.Completed => Color.Green,
                SimpleThermalRampController.RampState.Error => Color.Red,
                _ => Color.Gray
            };

            lblElapsedTimeValue.Text = e.ElapsedTime.ToString(@"hh\:mm\:ss");
        }

        private void RampController_RampCompleted(object sender, SimpleThermalRampController.RampCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RampController_RampCompleted(sender, e)));
                return;
            }

            SetControlState(false);

            if (e.Success)
            {
                AppendLog($"램프 완료: {e.FinalSampleTemp:F1}°C, 소요시간: {e.TotalTime:hh\\:mm\\:ss}");
                MessageBox.Show(
                    $"목표 온도에 도달하여 안정화되었습니다.\n\n" +
                    $"최종 온도: {e.FinalSampleTemp:F1}°C\n" +
                    $"소요 시간: {e.TotalTime:hh\\:mm\\:ss}",
                    "완료",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void RampController_TargetReached(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RampController_TargetReached(sender, e)));
                return;
            }

            AppendLog("목표 온도 도달!");

            // 외부 이벤트 발생 (타이머 시작용)
            TargetTemperatureReached?.Invoke(this, EventArgs.Empty);

            // 자동 타이머 시작 옵션이 켜져 있으면 처리
            if (chkAutoStartTimer.Checked)
            {
                AppendLog("타이머 자동 시작 트리거됨");
            }
        }

        private void RampController_ErrorOccurred(object sender, string e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RampController_ErrorOccurred(sender, e)));
                return;
            }

            AppendLog($"[오류] {e}");
            MessageBox.Show(e, "오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void RampController_LogMessage(object sender, string e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RampController_LogMessage(sender, e)));
                return;
            }

            AppendLog(e);
            LogMessage?.Invoke(this, e);
        }

        #endregion

        #region UI 업데이트

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_tempController == null || DesignMode) return;

            // 실행 중이 아닐 때도 현재 온도 표시
            if (!IsRunning)
            {
                try
                {
                    double heaterTemp = GetChannelTemp(1);
                    double sampleTemp = GetChannelTemp(2);
                    double heaterSetpoint = GetChannelSetpoint(1);

                    lblHeaterTempValue.Text = $"{heaterTemp:F1}°C";
                    lblHeaterSetpointValue.Text = $"{heaterSetpoint:F1}°C";
                    lblSampleTempValue.Text = $"{sampleTemp:F1}°C";
                }
                catch { }
            }
        }

        private double GetChannelTemp(int channel)
        {
            var status = _tempController.Status.ChannelStatus[channel - 1];
            return status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        private double GetChannelSetpoint(int channel)
        {
            var status = _tempController.Status.ChannelStatus[channel - 1];
            return status.SetValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        private void AppendLog(string message)
        {
            if (txtLog == null) return;

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string line = $"[{timestamp}] {message}\n";

            txtLog.AppendText(line);
            txtLog.ScrollToCaret();

            // 로그 길이 제한
            if (txtLog.TextLength > 10000)
            {
                txtLog.Text = txtLog.Text.Substring(txtLog.TextLength - 5000);
            }
        }

        #endregion

        /// <summary>
        /// Hold 모드 직접 시작 (램프 없이 바로 유지)
        /// </summary>
        public async Task<bool> StartHoldModeAsync(double targetTemp, string profileName)
        {
            if (RampController == null || _tempController == null)
                return false;

            // 프로파일 로드
            ThermalRampProfile profile = null;

            if (!string.IsNullOrEmpty(profileName))
                profile = _profileManager?.GetProfile(profileName);

            // 프로파일 없으면 목록에서 첫 번째 사용
            if (profile == null && _profileManager?.Profiles != null && _profileManager.Profiles.Count > 0)
            {
                profile = _profileManager.Profiles[0];
            }

            if (profile == null)
            {
                LogMessage?.Invoke(this, "프로파일을 찾을 수 없습니다.");
                return false;
            }

            return await RampController.StartHoldOnlyAsync(profile, targetTemp);
        }

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                _rampController?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}