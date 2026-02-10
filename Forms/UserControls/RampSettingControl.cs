using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Extensions;

namespace VacX_OutSense.UI.Controls
{
    public partial class RampSettingControl : UserControl
    {
        private TempController _tempController;
        private int _channelNumber;
        private int _dot = 0;              // 소수점 위치 (0: 정수, 1: 소수 1자리)
        private string _tempUnit = "°C";   // 온도 단위
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

            // ★ 콤보박스 기본 초기화
            if (cmbTimeUnit != null && cmbTimeUnit.Items.Count == 0)
            {
                cmbTimeUnit.Items.AddRange(new object[] { "초", "분", "시간" });
                cmbTimeUnit.SelectedIndex = 1;  // 기본: 분
            }

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

            Title = $"채널 {channelNumber} Ramp 설정";

            // 콤보박스 초기화
            if (cmbTimeUnit != null)
            {
                cmbTimeUnit.Items.Clear();
                cmbTimeUnit.Items.AddRange(new object[] { "초", "분", "시간" });
                cmbTimeUnit.SelectedIndex = 1; // 기본값: 분
            }

            if (_updateTimer == null)
            {
                InitializeTimer();
            }

            // 초기 설정 로드
            LoadCurrentSettings();

            if (_updateTimer != null)
            {
                _updateTimer.Start();
            }
        }

        /// <summary>
        /// 현재 Ramp 설정을 컨트롤러에서 읽어 UI에 반영합니다.
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (DesignMode || _tempController == null || _isUpdating)
                return;

            try
            {
                _isUpdating = true;

                if (_tempController.GetRampConfiguration(_channelNumber))
                {
                    var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                    // ★ Dot, 온도 단위 동기화
                    _dot = (int)status.Dot;
                    _tempUnit = status.TemperatureUnit;

                    // ★ Dot에 따라 NUD 설정 변경
                    ConfigureNudForDot(nudRampUpRate, status.RampUpRate);
                    ConfigureNudForDot(nudRampDownRate, status.RampDownRate);

                    // 시간 단위 콤보박스
                    if (cmbTimeUnit != null && status.RampTimeUnit <= 2)
                        cmbTimeUnit.SelectedIndex = (int)status.RampTimeUnit;

                    // 단위 라벨 갱신
                    UpdateRateUnitLabels();

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
        /// Dot 설정에 따라 NumericUpDown의 표시 형식과 값을 설정합니다.
        /// 
        /// TM4 매뉴얼: Ramp Rate 단위는 ℃/℉/Digit per 시간단위
        /// - Dot=0: 레지스터 값 10 = 10°C
        /// - Dot=1: 레지스터 값 10 = 1.0°C (10 digit = 10 × 0.1°C)
        /// 
        /// ★ 수정: 범위 에러 방지를 위해 Minimum/Maximum을 먼저 확장 후 값 설정
        /// </summary>
        private void ConfigureNudForDot(NumericUpDown nud, ushort rawValue)
        {
            if (nud == null) return;

            // ★ 범위 에러 방지: 먼저 범위를 최대로 확장
            nud.Minimum = 0;
            nud.Maximum = 99999m;  // 임시 확장 (어떤 값이든 수용 가능)

            if (_dot == 1)
            {
                nud.DecimalPlaces = 1;
                nud.Increment = 0.1m;

                decimal targetValue = Math.Max(0, Math.Min(rawValue / 10.0m, 999.9m));
                nud.Value = targetValue;

                // 실제 범위 설정 (값 설정 후)
                nud.Maximum = 999.9m;       // raw 9999 → 999.9°C
            }
            else
            {
                nud.DecimalPlaces = 0;
                nud.Increment = 1;

                decimal targetValue = Math.Max(0, Math.Min((decimal)rawValue, 9999m));
                nud.Value = targetValue;

                // 실제 범위 설정 (값 설정 후)
                nud.Maximum = 9999;
            }
        }

        /// <summary>
        /// NUD 값을 레지스터에 쓸 raw 값으로 변환합니다.
        /// </summary>
        private ushort NudToRawValue(NumericUpDown nud)
        {
            if (nud == null) return 0;

            if (_dot == 1)
                return (ushort)(nud.Value * 10);
            else
                return (ushort)nud.Value;
        }

        /// <summary>
        /// 시간 단위 콤보박스 변경 시 단위 라벨을 갱신합니다.
        /// </summary>
        private void cmbTimeUnit_SelectedIndexChanged(object sender, EventArgs e)
        {
            string unit = cmbTimeUnit.SelectedIndex switch
            {
                0 => "°C/초",
                1 => "°C/분",
                2 => "°C/시",
                _ => "°C/분"
            };

            lblUpRateUnit.Text = unit;
            lblDownRateUnit.Text = unit;
        }

        /// <summary>
        /// 변화율 단위 라벨을 현재 온도 단위 + 시간 단위로 갱신합니다.
        /// 예: "°C/분", "°C/시간"
        /// </summary>
        private void UpdateRateUnitLabels()
        {
            if (cmbTimeUnit == null || lblUpRateUnit == null || lblDownRateUnit == null)
                return;

            string timeStr = cmbTimeUnit.SelectedIndex switch
            {
                0 => "초",
                1 => "분",
                2 => "시간",
                _ => "분"
            };

            string unit = $"{_tempUnit}/{timeStr}";
            lblUpRateUnit.Text = unit;
            lblDownRateUnit.Text = unit;
        }

        /// <summary>
        /// Ramp 설정을 적용합니다.
        /// ★ 운전 중 변경 시 경고, Ramp OFF 시 열충격 경고
        /// </summary>
        private void btnApply_Click(object sender, EventArgs e)
        {
            if (DesignMode || _tempController == null) return;

            try
            {
                ushort rampUp = NudToRawValue(nudRampUpRate);
                ushort rampDown = NudToRawValue(nudRampDownRate);

                var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                // ★ 운전 중 경고
                if (status.IsRunning)
                {
                    bool wasEnabled = status.IsRampEnabled;
                    bool willDisable = (rampUp == 0 && rampDown == 0);

                    if (wasEnabled && willDisable)
                    {
                        // Ramp ON → OFF: SV가 즉시 목표값으로 점프
                        var result = MessageBox.Show(
                            "운전 중에 Ramp를 끄면 설정값(SV)이 즉시 목표값으로 변경됩니다.\n" +
                            "급격한 온도 변화가 발생할 수 있습니다.\n\n계속하시겠습니까?",
                            "⚠ 열충격 주의",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result != DialogResult.Yes) return;
                    }
                    else
                    {
                        // 운전 중 Ramp 설정 변경
                        var result = MessageBox.Show(
                            "운전 중에 Ramp 설정을 변경합니다.\n" +
                            "새 설정은 남은 구간에 즉시 적용됩니다.\n\n계속하시겠습니까?",
                            "운전 중 설정 변경",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (result != DialogResult.Yes) return;
                    }
                }

                TempController.RampTimeUnit timeUnit =
                    (TempController.RampTimeUnit)cmbTimeUnit.SelectedIndex;

                if (_tempController.SetRampConfiguration(_channelNumber, rampUp, rampDown, timeUnit))
                {
                    // 성공 시 현재 설정 다시 로드
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
        /// ★ 추가: Ramp 설정을 0으로 초기화합니다 (Ramp OFF).
        /// </summary>
        private void btnReset_Click(object sender, EventArgs e)
        {
            if (DesignMode || _tempController == null) return;

            var result = MessageBox.Show(
                "Ramp 설정을 초기화하시겠습니까?\n상승/하강 변화율이 모두 0으로 설정됩니다. (Ramp OFF)",
                "램프 초기화",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result != DialogResult.Yes) return;

            try
            {
                var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                // 운전 중이고 Ramp가 활성화되어 있으면 열충격 경고
                if (status.IsRunning && status.IsRampEnabled)
                {
                    var confirm = MessageBox.Show(
                        "운전 중에 Ramp를 끄면 설정값(SV)이 즉시 목표값으로 변경됩니다.\n" +
                        "급격한 온도 변화가 발생할 수 있습니다.\n\n계속하시겠습니까?",
                        "⚠ 열충격 주의",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (confirm != DialogResult.Yes) return;
                }

                TempController.RampTimeUnit timeUnit =
                    (TempController.RampTimeUnit)cmbTimeUnit.SelectedIndex;

                if (_tempController.SetRampConfiguration(_channelNumber, 0, 0, timeUnit))
                {
                    LoadCurrentSettings();
                }
                else
                {
                    MessageBox.Show("램프 초기화에 실패했습니다.", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"초기화 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 상태 표시를 업데이트합니다.
        /// ChannelStatus.RampStatusText가 Dot을 반영하므로 직접 사용
        /// </summary>
        private void UpdateStatusDisplay()
        {
            if (DesignMode || _tempController == null || _channelNumber < 1)
                return;

            try
            {
                var status = _tempController.Status.ChannelStatus[_channelNumber - 1];

                if (lblRampStatus != null)
                {
                    lblRampStatus.Text = status.RampStatusText;

                    if (status.IsRampActive)
                        lblRampStatus.ForeColor = Color.Green;
                    else if (status.IsRampEnabled)
                        lblRampStatus.ForeColor = Color.Blue;
                    else
                        lblRampStatus.ForeColor = Color.Gray;
                }

                // 프로그레스 바
                if (progressBar != null)
                {
                    progressBar.Visible = status.IsRampActive;
                    if (status.IsRampActive)
                        progressBar.Style = ProgressBarStyle.Marquee;
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
            catch
            {
                if (lblRampStatus != null)
                {
                    lblRampStatus.Text = "상태 확인 오류";
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
        /// 현재 설정 새로고침 - 컨트롤러에서 최신 상태를 읽어 UI에 동기화
        /// </summary>
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            if (DesignMode || _tempController == null) return;

            try
            {
                // ★ 컨트롤러 상태를 먼저 갱신
                _tempController.UpdateAllChannelStatus();

                // 설정 로드 (ConfigureNudForDot 포함)
                LoadCurrentSettings();

                // 상태 표시 갱신
                UpdateStatusDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"새로고침 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// 컨트롤이 로드될 때
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

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

                return LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            }
        }
    }
}