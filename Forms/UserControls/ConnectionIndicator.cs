using System.ComponentModel;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// 데이터 바인딩을 지원하는 연결 상태 표시기 컨트롤
    /// </summary>
    public partial class ConnectionIndicator : UserControl
    {
        // 기본 색상 정의
        private static readonly Color DEFAULT_CONNECTED_COLOR = Color.LimeGreen;
        private static readonly Color DEFAULT_DISCONNECTED_COLOR = Color.Red;

        private bool _isConnected = false;
        private string _componentName = "연결";
        private Color _connectedColor = DEFAULT_CONNECTED_COLOR;
        private Color _disconnectedColor = DEFAULT_DISCONNECTED_COLOR;

        // 데이터 바인딩을 위한 데이터 소스
        private object _dataSource;
        private string _dataMember;

        // 디자이너 속성
        [Category("연결 상태")]
        [Description("연결 상태를 설정하거나 가져옵니다.")]
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    Invalidate(); // 컨트롤 다시 그리기
                }
            }
        }

        [Category("연결 상태")]
        [Description("연결 컴포넌트의 이름을 설정하거나 가져옵니다.")]
        public string ComponentName
        {
            get { return _componentName; }
            set
            {
                if (_componentName != value)
                {
                    _componentName = value;
                    Invalidate(); // 컨트롤 다시 그리기
                }
            }
        }

        [Category("연결 상태")]
        [Description("연결됨 상태의 색상을 설정하거나 가져옵니다.")]
        public Color ConnectedColor
        {
            get { return _connectedColor; }
            set
            {
                if (_connectedColor != value)
                {
                    _connectedColor = value;
                    Invalidate(); // 컨트롤 다시 그리기
                }
            }
        }

        [Category("연결 상태")]
        [Description("연결 안됨 상태의 색상을 설정하거나 가져옵니다.")]
        public Color DisconnectedColor
        {
            get { return _disconnectedColor; }
            set
            {
                if (_disconnectedColor != value)
                {
                    _disconnectedColor = value;
                    Invalidate(); // 컨트롤 다시 그리기
                }
            }
        }

        [Category("데이터")]
        [Description("연결 상태에 바인딩할 데이터 소스를 설정하거나 가져옵니다.")]
        public object DataSource
        {
            get { return _dataSource; }
            set
            {
                if (_dataSource != value)
                {
                    // 이전 데이터 소스의 이벤트 구독 해제
                    UnsubscribeFromDataSource();

                    _dataSource = value;

                    // 새 데이터 소스에 이벤트 구독
                    SubscribeToDataSource();

                    // 바인딩 적용
                    UpdateBindings();
                }
            }
        }

        [Category("데이터")]
        [Description("연결 상태에 바인딩할 속성 이름을 설정하거나 가져옵니다.")]
        public string DataMember
        {
            get { return _dataMember; }
            set
            {
                if (_dataMember != value)
                {
                    _dataMember = value;

                    // 바인딩 적용
                    UpdateBindings();
                }
            }
        }

        public ConnectionIndicator()
        {
            InitializeComponent();

            // 사용자 컨트롤의 기본 크기 설정
            this.Size = new Size(80, 60);

            // 더블 버퍼링 설정 (깜빡임 방지)
            this.SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);
        }

        // 데이터 소스의 이벤트 구독
        private void SubscribeToDataSource()
        {
            if (_dataSource != null)
            {
                if (_dataSource is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged += DataSource_PropertyChanged;
                }
            }
        }

        // 데이터 소스의 이벤트 구독 해제
        private void UnsubscribeFromDataSource()
        {
            if (_dataSource != null)
            {
                if (_dataSource is INotifyPropertyChanged notifyPropertyChanged)
                {
                    notifyPropertyChanged.PropertyChanged -= DataSource_PropertyChanged;
                }
            }
        }

        // 데이터 소스의 PropertyChanged 이벤트 핸들러
        private void DataSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _dataMember || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateBindings();
            }
        }

        // 바인딩 업데이트
        private void UpdateBindings()
        {
            if (_dataSource != null && !string.IsNullOrEmpty(_dataMember))
            {
                // 리플렉션을 사용하여 데이터 소스에서 속성 값 가져오기
                try
                {
                    Type type = _dataSource.GetType();
                    System.Reflection.PropertyInfo propertyInfo = type.GetProperty(_dataMember);

                    if (propertyInfo != null)
                    {
                        object value = propertyInfo.GetValue(_dataSource);

                        if (value is bool boolValue)
                        {
                            // UI 스레드에서 실행 확인
                            if (this.InvokeRequired)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    IsConnected = boolValue;
                                }));
                            }
                            else
                            {
                                IsConnected = boolValue;
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // 바인딩 오류 처리
                }
            }
        }

        // Paint 이벤트 핸들러 - 컨트롤 그리기
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;

            // 안티앨리어싱 설정 (부드러운 원을 위해)
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // 컨트롤의 크기에 따라 원의 크기 계산
            int circleSize = Math.Min(Width, Height - 20); // 높이에서 텍스트 공간 확보
            int x = (Width - circleSize) / 2;
            int y = 0;

            // 원 그리기
            using (SolidBrush brush = new SolidBrush(_isConnected ? _connectedColor : _disconnectedColor))
            {
                g.FillEllipse(brush, x, y, circleSize, circleSize);
            }

            // 테두리 그리기
            using (Pen pen = new Pen(Color.DarkGray, 1))
            {
                g.DrawEllipse(pen, x, y, circleSize, circleSize);
            }

            // 컴포넌트 이름 그리기
            using (StringFormat sf = new StringFormat())
            {
                sf.Alignment = StringAlignment.Center;
                sf.LineAlignment = StringAlignment.Center;

                RectangleF textRect = new RectangleF(0, circleSize + 2, Width, Height - circleSize - 2);
                using (SolidBrush brush = new SolidBrush(ForeColor))
                {
                    g.DrawString(_componentName, Font, brush, textRect, sf);
                }
            }
        }
    }
}