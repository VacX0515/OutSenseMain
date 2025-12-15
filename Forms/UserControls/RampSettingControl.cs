using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.UI.Controls
{
    public partial class RampSettingControl : UserControl
    {
        private TempController _tempController;
        private int _channelNumber;
        private System.Windows.Forms.Timer _updateTimer;
        private bool _isUpdating = false;

        [Browsable(true)]
        [Category("Appearance")]
        [Description("컨트롤 제목")]
        public string Title
        {
            get => grpRampSetting?.Text ?? "Ramp 설정";
            set
            {
                if (grpRampSetting != null)
                    grpRampSetting.Text = value;
            }
        }

        public RampSettingControl()
        {
            InitializeComponent();

            // 디자인 모드가 아닐 때만 타이머 초기화
            if (!DesignMode)
            {
                InitializeTimer();
            }
        }

        private void InitializeTimer()
        {
            if (DesignMode) return;

            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 1000;
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        /// <summary>
        /// 컨트롤을 초기화합니다. (런타임에만 호출)
        /// </summary>
        public void Initialize(TempController controller, int channelNumber)
        {
            if (DesignMode) return;

            _tempController = controller;
            _channelNumber = channelNumber;

            // 제목 설정
            Title = $"채널 {channelNumber} Ramp 설정";

            // 콤보박스 초기화
            if (cmbTimeUnit != null)
            {
                cmbTimeUnit.Items.Clear();
                cmbTimeUnit.Items.AddRange(new object[] { "초", "분", "시간" });
                cmbTimeUnit.SelectedIndex = 1; // 기본값: 분
            }

            // 타이머가 없으면 초기화
            if (_updateTimer == null)
            {
                InitializeTimer();
            }

            // 초기 설정 로드
            LoadCurrentSettings();

            // 타이머 시작
            if (_updateTimer != null)
            {
                _updateTimer.Start();
            }
        }

        /// <summary>
        /// 현재 Ramp 설정을 로드합니다.
        /// </summary>
        private void LoadCurrentSettings()
        {
            // 디자인 모드이거나 컨트롤러가 없으면 반환
            if (DesignMode || _tempController == null || _isUpdating)
                return;

            try
            {
                _isUpdating = true;

                if (_tempController.GetRampConfiguration(_channelNumber))
                {
                    var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                    // UI 업데이트 - null 체크 추가
                    if (nudRampUpRate != null)
                        nudRampUpRate.Value = status.RampUpRate;

                    if (nudRampDownRate != null)
                        nudRampDownRate.Value = status.RampDownRate;

                    if (cmbTimeUnit != null)
                        cmbTimeUnit.SelectedIndex = (int)status.RampTimeUnit;

                    // 활성화 상태 업데이트
                    if (chkEnableRamp != null)
                        chkEnableRamp.Checked = status.IsRampEnabled;

                    UpdateStatusDisplay();
                }
            }
            catch (Exception ex)
            {
                if (!DesignMode)
                {
                    MessageBox.Show($"설정 로드 오류: {ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// Ramp 설정을 적용합니다.
        /// </summary>
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (DesignMode || _tempController == null) return;

            try
            {
                // 입력 값 검증
                if (!ValidateInput()) return;

                ushort rampUp = (ushort)nudRampUpRate.Value;
                ushort rampDown = (ushort)nudRampDownRate.Value;
                TempController.RampTimeUnit timeUnit =
                    (TempController.RampTimeUnit)cmbTimeUnit.SelectedIndex;

                // 설정 적용
                if (_tempController.SetRampConfiguration(_channelNumber, rampUp, rampDown, timeUnit))
                {
                    MessageBox.Show($"채널 {_channelNumber} Ramp 설정이 적용되었습니다.", "성공",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // 상태 업데이트
                    LoadCurrentSettings();
                }
                else
                {
                    MessageBox.Show("Ramp 설정 적용에 실패했습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 적용 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 입력 값을 검증합니다.
        /// </summary>
        private bool ValidateInput()
        {
            if (nudRampUpRate == null || nudRampDownRate == null)
                return false;

            if (nudRampUpRate.Value == 0 && nudRampDownRate.Value == 0)
            {
                var result = MessageBox.Show("Ramp 기능이 비활성화됩니다. 계속하시겠습니까?",
                    "확인", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                return result == DialogResult.Yes;
            }

            if (nudRampUpRate.Value > 9999 || nudRampDownRate.Value > 9999)
            {
                MessageBox.Show("Ramp 변화율은 0-9999 사이여야 합니다.", "입력 오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ramp 활성화 체크박스 변경 이벤트
        /// </summary>
        private void chkEnableRamp_CheckedChanged(object sender, EventArgs e)
        {
            if (DesignMode || _isUpdating) return;

            bool isEnabled = chkEnableRamp?.Checked ?? false;

            // 컨트롤 활성화/비활성화
            if (nudRampUpRate != null)
                nudRampUpRate.Enabled = isEnabled;

            if (nudRampDownRate != null)
                nudRampDownRate.Enabled = isEnabled;

            if (cmbTimeUnit != null)
                cmbTimeUnit.Enabled = isEnabled;

            if (btnApply != null)
                btnApply.Enabled = isEnabled;

            if (!isEnabled)
            {
                // Ramp OFF 설정
                if (nudRampUpRate != null)
                    nudRampUpRate.Value = 0;

                if (nudRampDownRate != null)
                    nudRampDownRate.Value = 0;
            }
            else
            {
                // 기본값 설정
                if (nudRampUpRate != null && nudRampDownRate != null)
                {
                    if (nudRampUpRate.Value == 0 && nudRampDownRate.Value == 0)
                    {
                        nudRampUpRate.Value = 10;
                        nudRampDownRate.Value = 10;
                    }
                }
            }
        }

        /// <summary>
        /// 상태 표시를 업데이트합니다.
        /// </summary>
        private void UpdateStatusDisplay()
        {
            // 디자인 모드이거나 필수 객체가 없으면 반환
            if (DesignMode || _tempController == null || _channelNumber < 1)
                return;

            try
            {
                var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                // 현재 상태 텍스트 업데이트
                if (lblRampStatus != null)
                {
                    lblRampStatus.Text = status.RampStatusText;

                    // Ramp 진행 상태 표시
                    if (status.IsRampActive)
                    {
                        lblRampStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        lblRampStatus.ForeColor = status.IsRampEnabled ? Color.Blue : Color.Gray;
                    }
                }

                // 진행 상태 표시
                if (progressBar != null)
                {
                    progressBar.Visible = status.IsRampActive;
                    if (status.IsRampActive)
                    {
                        progressBar.Style = ProgressBarStyle.Marquee;
                    }
                }

                // 진행 상태 텍스트
                if (lblProgress != null)
                {
                    if (status.IsRampActive)
                    {
                        lblProgress.Text = $"현재: {status.FormattedPresentValue}{status.TemperatureUnit}, " +
                                          $"목표: {status.FormattedSetValue}{status.TemperatureUnit}";
                        lblProgress.Visible = true;
                    }
                    else
                    {
                        lblProgress.Visible = false;
                    }
                }

                // 채널 운전 상태 표시
                if (lblRunStatus != null)
                {
                    lblRunStatus.Text = status.IsRunning ? "운전 중" : "정지";
                    lblRunStatus.ForeColor = status.IsRunning ? Color.Green : Color.Red;
                }
            }
            catch (Exception ex)
            {
                if (lblRampStatus != null)
                {
                    lblRampStatus.Text = $"상태 확인 오류";
                    lblRampStatus.ForeColor = Color.Red;
                }
            }
        }

        /// <summary>
        /// 타이머 틱 이벤트
        /// </summary>
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!DesignMode && _tempController != null)
            {
                UpdateStatusDisplay();
            }
        }

        /// <summary>
        /// 현재 설정 새로고침
        /// </summary>
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (!DesignMode && _tempController != null)
            {
                LoadCurrentSettings();
            }
        }

        /// <summary>
        /// 컨트롤이 로드될 때
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // 디자인 모드에서는 기본값 표시
            if (DesignMode)
            {
                if (lblRampStatus != null)
                    lblRampStatus.Text = "Ramp OFF (Design Mode)";

                if (lblRunStatus != null)
                    lblRunStatus.Text = "정지 (Design Mode)";

                if (progressBar != null)
                    progressBar.Visible = false;

                if (lblProgress != null)
                    lblProgress.Visible = false;
            }
        }

        /// <summary>
        /// 컨트롤이 제거될 때
        /// </summary>
        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }
            base.OnHandleDestroyed(e);
        }

        /// <summary>
        /// 디자인 모드 체크 (더 정확한 방법)
        /// </summary>
        protected new bool DesignMode
        {
            get
            {
                if (base.DesignMode)
                    return true;

                return System.ComponentModel.LicenseManager.UsageMode ==
                       System.ComponentModel.LicenseUsageMode.Designtime;
            }
        }
    }
}