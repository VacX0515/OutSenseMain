using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Controls
{
    /// <summary>
    /// 온도 컨트롤러의 채널 정보를 표시하고 제어하기 위한 사용자 컨트롤
    /// </summary>
    public partial class TempControllerChanelPanel : UserControl
    {
        #region 필드 및 속성

        private int _channelNumber = 1;
        private ChannelStatus _channelStatus;
        private bool _isRunning = false;
        private bool _isAutoTuning = false;
        private string _statusText = "대기 중";

        // 델리게이트 선언
        public delegate void ChannelButtonClickEventHandler(object sender, int channelNumber);

        // 이벤트 선언
        public event ChannelButtonClickEventHandler StartButtonClick;
        public event ChannelButtonClickEventHandler StopButtonClick;
        public event ChannelButtonClickEventHandler SetTempButtonClick;
        public event ChannelButtonClickEventHandler AutoTuningButtonClick;

        /// <summary>
        /// 채널 번호
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("채널 번호를 설정합니다.")]
        public int ChannelNumber
        {
            get => _channelNumber;
            set
            {
                _channelNumber = value;
                lblChannelTitle.Text = $"채널 {value}";
                Invalidate();
            }
        }

        /// <summary>
        /// 채널이 실행 중인지 여부
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("채널이 실행 중인지 여부를 설정합니다.")]
        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                UpdateControlState();
            }
        }

        /// <summary>
        /// 오토튜닝이 실행 중인지 여부
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("오토튜닝이 실행 중인지 여부를 설정합니다.")]
        public bool IsAutoTuning
        {
            get => _isAutoTuning;
            set
            {
                _isAutoTuning = value;
                UpdateControlState();
            }
        }

        /// <summary>
        /// 현재 측정값 표시
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("현재 측정값을 설정합니다.")]
        public string PresentValue
        {
            get => txtPresentValue.Text;
            set => txtPresentValue.Text = value;
        }

        /// <summary>
        /// 설정값 표시
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("설정값을 설정합니다.")]
        public string SetValue
        {
            get => txtSetValue.Text;
            set => txtSetValue.Text = value;
        }

        /// <summary>
        /// 가열 출력 표시
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("가열 출력값을 설정합니다.")]
        public string HeatingOutput
        {
            get => txtHeatingOutput.Text;
            set => txtHeatingOutput.Text = value;
        }

        /// <summary>
        /// 채널 상태 텍스트
        /// </summary>
        [Category("TemperatureChannel")]
        [Description("채널 상태 텍스트를 설정합니다.")]
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                lblStatus.Text = value;

                // 상태에 따른 색상 변경
                if (value.Contains("오류") || value.Contains("센서"))
                {
                    lblStatus.ForeColor = Color.Red;
                }
                else if (value.Contains("안정"))
                {
                    lblStatus.ForeColor = Color.Green;
                }
                else if (value.Contains("승온") || value.Contains("냉각"))
                {
                    lblStatus.ForeColor = Color.Blue;
                }
                else
                {
                    lblStatus.ForeColor = SystemColors.ControlText;
                }
            }
        }

        /// <summary>
        /// 채널 상태 객체
        /// </summary>
        [Browsable(false)]
        public ChannelStatus ChannelStatus
        {
            get => _channelStatus;
            set
            {
                _channelStatus = value;
                if (value != null)
                {
                    UpdateFromChannelStatus();
                }
            }
        }

        #endregion

        #region 생성자 및 초기화

        /// <summary>
        /// 생성자
        /// </summary>
        public TempControllerChanelPanel()
        {
            InitializeComponent();
            SetupControl();
        }

        /// <summary>
        /// 컨트롤 설정
        /// </summary>
        private void SetupControl()
        {
            // 버튼 이벤트 연결
            btnStart.Click += BtnStart_Click;
            btnStop.Click += BtnStop_Click;
            btnSetTemp.Click += BtnSetTemp_Click;
            btnAutoTuning.Click += BtnAutoTuning_Click;

            // 초기 상태 설정
            UpdateControlState();
        }

        #endregion

        #region 컨트롤 업데이트

        /// <summary>
        /// 채널 상태에 따라 컨트롤을 업데이트합니다.
        /// </summary>
        private void UpdateFromChannelStatus()
        {
            if (_channelStatus == null)
                return;

            // 값 업데이트
            PresentValue = _channelStatus.FormattedPresentValue;
            SetValue = _channelStatus.FormattedSetValue;
            HeatingOutput = $"{_channelStatus.HeatingMV:F1}%";
            IsRunning = _channelStatus.IsRunning;
            IsAutoTuning = _channelStatus.IsAutoTuning;

            // 상태에 따른 텍스트 설정
            if (!string.IsNullOrEmpty(_channelStatus.SensorError))
            {
                StatusText = $"센서 오류: {_channelStatus.SensorError}";
            }
            else if (_channelStatus.IsAutoTuning)
            {
                StatusText = "오토튜닝 중";
            }
            else if (_channelStatus.IsRunning)
            {
                int pv = _channelStatus.PresentValue;
                int sv = _channelStatus.SetValue;

                // 안정 판정: raw 단위 비교 (물리 기준 ±3°C)
                // Dot=0: 3 raw = 3°C, Dot=1: 30 raw = 3.0°C
                int tolerance = _channelStatus.Dot == 0 ? 3 : 30;

                if (Math.Abs(pv - sv) <= tolerance)
                {
                    StatusText = "안정 상태";
                }
                else if (_channelStatus.IsRampActive)
                {
                    // Ramp가 실제로 진행 중
                    StatusText = pv < sv ? "Ramp 승온 중" : "Ramp 냉각 중";
                }
                else if (pv < sv)
                {
                    // Ramp 설정은 있지만 목표 도달 또는 비활성
                    StatusText = _channelStatus.IsRampEnabled ? "승온 중 (Ramp)" : "승온 중";
                }
                else
                {
                    StatusText = _channelStatus.IsRampEnabled ? "냉각 중 (Ramp)" : "냉각 중";
                }
            }
            else
            {
                StatusText = "정지";
            }
        }

        /// <summary>
        /// 컨트롤 상태를 업데이트합니다.
        /// </summary>
        private void UpdateControlState()
        {
            btnStart.Enabled = !_isRunning;
            btnStop.Enabled = _isRunning;
            btnSetTemp.Enabled = true;
            btnAutoTuning.Enabled = _isRunning && !_isAutoTuning;

            // 실행 상태에 따른 인디케이터 색상 변경
            pnlRunningIndicator.BackColor = _isRunning ? Color.LightGreen : Color.LightGray;

            // 오토튜닝 상태 표시
            if (_isAutoTuning)
            {
                btnAutoTuning.Text = "튜닝 중";
                btnAutoTuning.BackColor = Color.Orange;
            }
            else
            {
                btnAutoTuning.Text = "오토튜닝";
                btnAutoTuning.BackColor = SystemColors.Control;
            }
        }

        #endregion

        #region 이벤트 핸들러

        private void BtnStart_Click(object sender, EventArgs e)
        {
            StartButtonClick?.Invoke(this, _channelNumber);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopButtonClick?.Invoke(this, _channelNumber);
        }

        private void BtnSetTemp_Click(object sender, EventArgs e)
        {
            SetTempButtonClick?.Invoke(this, _channelNumber);
        }

        private void BtnAutoTuning_Click(object sender, EventArgs e)
        {
            AutoTuningButtonClick?.Invoke(this, _channelNumber);
        }

        #endregion
    }
}