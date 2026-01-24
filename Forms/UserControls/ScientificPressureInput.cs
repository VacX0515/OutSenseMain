using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// 과학적 표기법 압력 입력 컨트롤
    /// 예: 1.5 × 10^-6 Torr 형태로 입력
    /// </summary>
    public class ScientificPressureInput : UserControl
    {
        #region 컨트롤

        private NumericUpDown numMantissa;
        private Label lblMultiplier;
        private ComboBox cmbExponent;
        private Label lblUnit;
        private ToolTip toolTip;

        #endregion

        #region 필드

        private double _value = 1E-5;
        private string _unit = "Torr";
        private int _minExponent = -9;
        private int _maxExponent = -1;
        private bool _updating = false;

        #endregion

        #region 속성

        /// <summary>
        /// 현재 압력 값
        /// </summary>
        [Category("Data")]
        [Description("현재 압력 값")]
        [DefaultValue(1E-5)]
        public double Value
        {
            get => _value;
            set
            {
                if (Math.Abs(_value - value) > double.Epsilon)
                {
                    _value = value;
                    UpdateControlsFromValue();
                    OnValueChanged();
                }
            }
        }

        /// <summary>
        /// 단위 문자열
        /// </summary>
        [Category("Appearance")]
        [Description("단위 표시 문자열")]
        [DefaultValue("Torr")]
        public string Unit
        {
            get => _unit;
            set
            {
                _unit = value;
                if (lblUnit != null)
                    lblUnit.Text = value;
            }
        }

        /// <summary>
        /// 최소 지수 값
        /// </summary>
        [Category("Data")]
        [Description("최소 지수 값 (예: -9)")]
        [DefaultValue(-9)]
        public int MinExponent
        {
            get => _minExponent;
            set
            {
                if (_minExponent != value)
                {
                    _minExponent = value;
                    RefreshExponentItems();
                }
            }
        }

        /// <summary>
        /// 최대 지수 값
        /// </summary>
        [Category("Data")]
        [Description("최대 지수 값 (예: -1)")]
        [DefaultValue(-1)]
        public int MaxExponent
        {
            get => _maxExponent;
            set
            {
                if (_maxExponent != value)
                {
                    _maxExponent = value;
                    RefreshExponentItems();
                }
            }
        }

        #endregion

        #region 이벤트

        /// <summary>
        /// 값 변경 이벤트
        /// </summary>
        [Category("Action")]
        [Description("값이 변경되었을 때 발생")]
        public event EventHandler ValueChanged;

        #endregion

        #region 생성자

        public ScientificPressureInput()
        {
            InitializeComponents();
            UpdateControlsFromValue();
        }

        #endregion

        #region 초기화

        private void InitializeComponents()
        {
            this.SuspendLayout();

            this.Size = new Size(185, 25);
            this.MinimumSize = new Size(150, 25);

            // 가수 입력 (1.0 ~ 9.9)
            numMantissa = new NumericUpDown
            {
                Location = new Point(0, 1),
                Size = new Size(50, 23),
                Minimum = 1.0m,
                Maximum = 9.9m,
                DecimalPlaces = 1,
                Increment = 0.1m,
                Value = 1.0m,
                TextAlign = HorizontalAlignment.Center,
                Font = new Font("Consolas", 9F)
            };
            numMantissa.ValueChanged += OnInputChanged;

            // "× 10^" 라벨
            lblMultiplier = new Label
            {
                Location = new Point(52, 4),
                Size = new Size(40, 18),
                Text = "× 10^",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("맑은 고딕", 9F)
            };

            // 지수 선택
            cmbExponent = new ComboBox
            {
                Location = new Point(92, 1),
                Size = new Size(48, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Consolas", 9F)
            };
            RefreshExponentItems();
            cmbExponent.SelectedIndexChanged += OnInputChanged;

            // 단위 라벨
            lblUnit = new Label
            {
                Location = new Point(143, 4),
                Size = new Size(40, 18),
                Text = _unit,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("맑은 고딕", 9F)
            };

            // 툴팁
            toolTip = new ToolTip
            {
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 100,
                ShowAlways = true
            };

            this.Controls.AddRange(new Control[] { numMantissa, lblMultiplier, cmbExponent, lblUnit });

            this.ResumeLayout(false);
        }

        private void RefreshExponentItems()
        {
            if (cmbExponent == null) return;

            _updating = true;

            int currentExponent = -5;
            if (cmbExponent.SelectedItem != null)
            {
                int.TryParse(cmbExponent.SelectedItem.ToString(), out currentExponent);
            }

            cmbExponent.Items.Clear();
            for (int i = _minExponent; i <= _maxExponent; i++)
            {
                cmbExponent.Items.Add(i.ToString());
            }

            // 이전 선택 복원 또는 기본값
            int targetIndex = currentExponent - _minExponent;
            if (targetIndex >= 0 && targetIndex < cmbExponent.Items.Count)
            {
                cmbExponent.SelectedIndex = targetIndex;
            }
            else if (cmbExponent.Items.Count > 0)
            {
                // 기본값: -5 또는 중간값
                int defaultIndex = -5 - _minExponent;
                if (defaultIndex >= 0 && defaultIndex < cmbExponent.Items.Count)
                    cmbExponent.SelectedIndex = defaultIndex;
                else
                    cmbExponent.SelectedIndex = cmbExponent.Items.Count / 2;
            }

            _updating = false;
        }

        #endregion

        #region 이벤트 핸들러

        private void OnInputChanged(object sender, EventArgs e)
        {
            if (_updating) return;

            double mantissa = (double)numMantissa.Value;

            if (cmbExponent.SelectedItem == null) return;

            if (!int.TryParse(cmbExponent.SelectedItem.ToString(), out int exponent))
                return;

            _value = mantissa * Math.Pow(10, exponent);

            UpdateTooltip();
            OnValueChanged();
        }

        private void UpdateControlsFromValue()
        {
            if (_value <= 0) return;

            _updating = true;

            try
            {
                // 지수 계산
                int exponent = (int)Math.Floor(Math.Log10(_value));
                exponent = Math.Max(_minExponent, Math.Min(_maxExponent, exponent));

                // 가수 계산
                double mantissa = _value / Math.Pow(10, exponent);

                // 범위 조정
                if (mantissa < 1.0)
                {
                    mantissa *= 10;
                    exponent--;
                    exponent = Math.Max(_minExponent, exponent);
                }
                else if (mantissa >= 10.0)
                {
                    mantissa /= 10;
                    exponent++;
                    exponent = Math.Min(_maxExponent, exponent);
                }

                mantissa = Math.Max(1.0, Math.Min(9.9, mantissa));

                // UI 업데이트
                numMantissa.Value = (decimal)Math.Round(mantissa, 1);

                int index = exponent - _minExponent;
                if (index >= 0 && index < cmbExponent.Items.Count)
                {
                    cmbExponent.SelectedIndex = index;
                }

                UpdateTooltip();
            }
            finally
            {
                _updating = false;
            }
        }

        private void UpdateTooltip()
        {
            string tip = $"= {_value:E2} {_unit}";
            toolTip.SetToolTip(numMantissa, tip);
            toolTip.SetToolTip(cmbExponent, tip);
            toolTip.SetToolTip(this, tip);
        }

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 공용 메서드

        /// <summary>
        /// 문자열에서 값 설정 (예: "1E-6", "1.5E-5")
        /// </summary>
        public bool TrySetFromString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            input = input.Trim().Replace(" ", "");

            // "×10^" 또는 "x10^" 형식 처리
            input = System.Text.RegularExpressions.Regex.Replace(
                input, @"[×xX]10\^", "E",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (double.TryParse(input,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out double result))
            {
                Value = result;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 현재 값을 문자열로 반환
        /// </summary>
        public override string ToString()
        {
            return $"{_value:E1} {_unit}";
        }

        #endregion

        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                toolTip?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}