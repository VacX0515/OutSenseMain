using System;
using System.Collections.Generic;
using System.ComponentModel;
using System;
using System.Reflection;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// 데이터 바인딩을 지원하는 텍스트 박스 사용자 정의 컨트롤
    /// </summary>
    public partial class BindableTextBox : UserControl
    {
        #region 필드 및 속성

        private object _dataSource;
        private string _dataMember;
        private string _labelText = "레이블:";
        private bool _isUpdating = false;
        private bool _isReadOnly = false;
        private string _formatString = null;

        /// <summary>
        /// 데이터 소스 객체
        /// </summary>
        [Category("데이터")]
        [Description("바인딩할 데이터 소스 객체")]
        public object DataSource
        {
            get => _dataSource;
            set
            {
                if (_dataSource != value)
                {
                    UnsubscribeFromDataSource();
                    _dataSource = value;
                    SubscribeToDataSource();
                    UpdateTextFromSource();
                }
            }
        }

        /// <summary>
        /// 바인딩할 속성 이름
        /// </summary>
        [Category("데이터")]
        [Description("바인딩할 속성 이름")]
        public string DataMember
        {
            get => _dataMember;
            set
            {
                if (_dataMember != value)
                {
                    _dataMember = value;
                    UpdateTextFromSource();
                }
            }
        }

        /// <summary>
        /// 레이블 텍스트
        /// </summary>
        [Category("모양")]
        [Description("레이블 텍스트")]
        public string LabelText
        {
            get => _labelText;
            set
            {
                if (_labelText != value)
                {
                    _labelText = value;
                    if (label != null)
                    {
                        label.Text = value;
                    }
                }
            }
        }

        /// <summary>
        /// 텍스트 박스의 읽기 전용 여부
        /// </summary>
        [Category("동작")]
        [Description("텍스트 박스의 읽기 전용 여부")]
        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                if (_isReadOnly != value)
                {
                    _isReadOnly = value;
                    if (textBox != null)
                    {
                        textBox.ReadOnly = value;
                    }
                }
            }
        }

        /// <summary>
        /// 현재 텍스트 값
        /// </summary>
        [Category("모양")]
        [Description("현재 텍스트 값")]
        public string TextValue
        {
            get => textBox?.Text ?? string.Empty;
            set
            {
                if (textBox != null && textBox.Text != value)
                {
                    textBox.Text = value;
                }
            }
        }

        /// <summary>
        /// 형식 문자열 (숫자 형식 지정에 사용, 예: "F2")
        /// </summary>
        [Category("동작")]
        [Description("표시 형식 문자열 (예: F2)")]
        public string FormatString
        {
            get => _formatString;
            set
            {
                if (_formatString != value)
                {
                    _formatString = value;
                    UpdateTextFromSource();
                }
            }
        }

        #endregion

        #region 생성자 및 초기화

        public BindableTextBox()
        {
            InitializeComponent();

            // 기본 레이블 텍스트 설정
            if (label != null)
                label.Text = _labelText;

            // 읽기 전용 설정
            if (textBox != null)
                textBox.ReadOnly = _isReadOnly;

            // 이벤트 핸들러 연결
            if (textBox != null)
            {
                textBox.TextChanged += TextBox_TextChanged;
                textBox.Validated += TextBox_Validated;
            }
        }

        #endregion

        #region 이벤트 처리기

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (!_isUpdating && !_isReadOnly)
            {
                // 지연된 데이터 소스 업데이트
                // 텍스트 변경 중간에는 업데이트하지 않음
            }
        }

        private void TextBox_Validated(object sender, EventArgs e)
        {
            if (!_isUpdating && !_isReadOnly)
            {
                UpdateSourceFromText();
            }
        }

        #endregion

        #region 데이터 바인딩 메서드

        private void SubscribeToDataSource()
        {
            if (_dataSource is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += DataSource_PropertyChanged;
            }
        }

        private void UnsubscribeFromDataSource()
        {
            if (_dataSource is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged -= DataSource_PropertyChanged;
            }
        }

        private void DataSource_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == _dataMember || string.IsNullOrEmpty(e.PropertyName))
            {
                UpdateTextFromSource();
            }
        }

        /// <summary>
        /// 데이터 소스의 값을 텍스트 박스에 반영
        /// </summary>
        private void UpdateTextFromSource()
        {
            if (_dataSource == null || string.IsNullOrEmpty(_dataMember) || textBox == null)
                return;

            try
            {
                _isUpdating = true;

                // 데이터 소스에서 속성 검색
                PropertyInfo propertyInfo = _dataSource.GetType().GetProperty(_dataMember);
                if (propertyInfo == null)
                {
                    // 대소문자 구분 없이 속성 검색
                    foreach (PropertyInfo prop in _dataSource.GetType().GetProperties())
                    {
                        if (string.Equals(prop.Name, _dataMember, StringComparison.OrdinalIgnoreCase))
                        {
                            propertyInfo = prop;
                            break;
                        }
                    }
                }

                if (propertyInfo != null)
                {
                    // 속성 값 가져오기
                    object value = propertyInfo.GetValue(_dataSource);

                    // 값 변환 및 형식 지정
                    string textValue;
                    if (value == null)
                    {
                        textValue = string.Empty;
                    }
                    else if (!string.IsNullOrEmpty(_formatString) && value is IFormattable formattable)
                    {
                        textValue = formattable.ToString(_formatString, null);
                    }
                    else
                    {
                        textValue = value.ToString();
                    }

                    // UI 스레드 확인
                    if (textBox.InvokeRequired)
                    {
                        textBox.Invoke(new Action(() => textBox.Text = textValue));
                    }
                    else
                    {
                        textBox.Text = textValue;
                    }
                }
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"UpdateTextFromSource 오류: {ex.Message}");
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 텍스트 박스의 값을 데이터 소스에 반영
        /// </summary>
        private void UpdateSourceFromText()
        {
            if (_dataSource == null || string.IsNullOrEmpty(_dataMember) || _isReadOnly || textBox == null)
                return;

            try
            {
                // 데이터 소스에서 속성 검색
                PropertyInfo propertyInfo = _dataSource.GetType().GetProperty(_dataMember);
                if (propertyInfo == null)
                {
                    // 대소문자 구분 없이 속성 검색
                    foreach (PropertyInfo prop in _dataSource.GetType().GetProperties())
                    {
                        if (string.Equals(prop.Name, _dataMember, StringComparison.OrdinalIgnoreCase))
                        {
                            propertyInfo = prop;
                            break;
                        }
                    }
                }

                if (propertyInfo != null && propertyInfo.CanWrite)
                {
                    // 속성 유형 확인 및 변환
                    Type propertyType = propertyInfo.PropertyType;
                    object convertedValue = null;

                    // 문자열 -> 대상 타입으로 변환
                    try
                    {
                        if (propertyType == typeof(string))
                        {
                            convertedValue = textBox.Text;
                        }
                        else if (propertyType == typeof(int) || propertyType == typeof(int?))
                        {
                            if (int.TryParse(textBox.Text, out int intValue))
                                convertedValue = intValue;
                        }
                        else if (propertyType == typeof(double) || propertyType == typeof(double?))
                        {
                            if (double.TryParse(textBox.Text, out double doubleValue))
                                convertedValue = doubleValue;
                        }
                        else if (propertyType == typeof(float) || propertyType == typeof(float?))
                        {
                            if (float.TryParse(textBox.Text, out float floatValue))
                                convertedValue = floatValue;
                        }
                        else if (propertyType == typeof(decimal) || propertyType == typeof(decimal?))
                        {
                            if (decimal.TryParse(textBox.Text, out decimal decimalValue))
                                convertedValue = decimalValue;
                        }
                        else if (propertyType == typeof(bool) || propertyType == typeof(bool?))
                        {
                            if (bool.TryParse(textBox.Text, out bool boolValue))
                                convertedValue = boolValue;
                        }
                        else if (propertyType == typeof(DateTime) || propertyType == typeof(DateTime?))
                        {
                            if (DateTime.TryParse(textBox.Text, out DateTime dateValue))
                                convertedValue = dateValue;
                        }
                        else
                        {
                            // 기타 타입은 Convert를 통해 변환 시도
                            convertedValue = Convert.ChangeType(textBox.Text, propertyType);
                        }

                        // 값 설정
                        if (convertedValue != null)
                        {
                            propertyInfo.SetValue(_dataSource, convertedValue);
                        }
                    }
                    catch (Exception ex)
                    {
                        //System.Diagnostics.Debug.WriteLine($"값 변환 오류: {ex.Message}");

                        // 변환 오류 시 원래 값으로 다시 업데이트
                        UpdateTextFromSource();
                    }
                }
            }
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine($"UpdateSourceFromText 오류: {ex.Message}");
            }
        }

        /// <summary>
        /// 바인딩을 수동으로 새로고침
        /// </summary>
        public void RefreshBinding()
        {
            UpdateTextFromSource();
        }

        #endregion

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateTextFromSource();
        }
    }
}