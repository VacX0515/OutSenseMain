using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace VacX_OutSense.Core.Devices.TempController
{
    /// <summary>
    /// 온도 컨트롤러 채널별 상태 정보를 저장하는 클래스입니다.
    /// </summary>
    public class ChannelStatus : INotifyPropertyChanged
    {
        private int _channelNumber;
        private bool _isRunning;
        private short _presentValue;
        private short _setValue;
        private int _dot;
        private string _temperatureUnit;
        private float _heatingMV;
        private float _coolingMV;
        private string _sensorError;
        private bool _isAutoTuning;

        // Ramp 관련 필드
        private ushort _rampUpRate;
        private ushort _rampDownRate;
        private ushort _rampTimeUnit;
        private bool _isRampActive;

        // 확장 모듈 관련 필드
        private bool _isExpansionChannel;

        /// <summary>
        /// 속성 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ChannelStatus()
        {
            // 기본값 설정
            _temperatureUnit = "°C";
            _dot = 0;
            _isRunning = false;
            _isAutoTuning = false;
            _heatingMV = 0.0f;
            _coolingMV = 0.0f;
            _rampUpRate = 0;
            _rampDownRate = 0;
            _rampTimeUnit = 1; // 기본값: 분
            _isRampActive = false;
            _isExpansionChannel = false;
        }

        /// <summary>
        /// 속성 변경 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="propertyName">속성 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 채널 번호
        /// </summary>
        public int ChannelNumber
        {
            get => _channelNumber;
            internal set
            {
                if (_channelNumber != value)
                {
                    _channelNumber = value;
                    OnPropertyChanged(nameof(ChannelNumber));
                }
            }
        }

        /// <summary>
        /// 채널이 실행 중인지 여부
        /// </summary>
        public bool IsRunning
        {
            get => _isRunning;
            internal set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    OnPropertyChanged(nameof(IsRunning));
                }
            }
        }

        /// <summary>
        /// 현재 측정값 (PV)
        /// </summary>
        public short PresentValue
        {
            get => _presentValue;
            internal set
            {
                if (_presentValue != value)
                {
                    _presentValue = value;
                    OnPropertyChanged(nameof(PresentValue));
                    OnPropertyChanged(nameof(FormattedPresentValue));
                }
            }
        }

        /// <summary>
        /// 설정값 (SV)
        /// </summary>
        public short SetValue
        {
            get => _setValue;
            internal set
            {
                if (_setValue != value)
                {
                    _setValue = value;
                    OnPropertyChanged(nameof(SetValue));
                    OnPropertyChanged(nameof(FormattedSetValue));
                }
            }
        }

        /// <summary>
        /// 소수점 위치 (0: 정수, 1: 소수점 한자리)
        /// </summary>
        public int Dot
        {
            get => _dot;
            internal set
            {
                if (_dot != value)
                {
                    _dot = value;
                    OnPropertyChanged(nameof(Dot));
                    OnPropertyChanged(nameof(FormattedPresentValue));
                    OnPropertyChanged(nameof(FormattedSetValue));
                }
            }
        }

        /// <summary>
        /// 온도 단위 ("°C" 또는 "°F")
        /// </summary>
        public string TemperatureUnit
        {
            get => _temperatureUnit;
            internal set
            {
                if (_temperatureUnit != value)
                {
                    _temperatureUnit = value;
                    OnPropertyChanged(nameof(TemperatureUnit));
                    OnPropertyChanged(nameof(FormattedPresentValue));
                    OnPropertyChanged(nameof(FormattedSetValue));
                    OnPropertyChanged(nameof(RampStatusText));
                }
            }
        }

        /// <summary>
        /// 가열측 조작량 (0.0 ~ 100.0%)
        /// </summary>
        public float HeatingMV
        {
            get => _heatingMV;
            internal set
            {
                if (_heatingMV != value)
                {
                    _heatingMV = value;
                    OnPropertyChanged(nameof(HeatingMV));
                }
            }
        }

        /// <summary>
        /// 냉각측 조작량 (0.0 ~ 100.0%)
        /// </summary>
        public float CoolingMV
        {
            get => _coolingMV;
            internal set
            {
                if (_coolingMV != value)
                {
                    _coolingMV = value;
                    OnPropertyChanged(nameof(CoolingMV));
                }
            }
        }

        /// <summary>
        /// 센서 에러 코드 (없으면 null)
        /// </summary>
        public string SensorError
        {
            get => _sensorError;
            internal set
            {
                if (_sensorError != value)
                {
                    _sensorError = value;
                    OnPropertyChanged(nameof(SensorError));
                    OnPropertyChanged(nameof(FormattedPresentValue));
                }
            }
        }

        /// <summary>
        /// 오토튜닝 실행 중인지 여부
        /// </summary>
        public bool IsAutoTuning
        {
            get => _isAutoTuning;
            internal set
            {
                if (_isAutoTuning != value)
                {
                    _isAutoTuning = value;
                    OnPropertyChanged(nameof(IsAutoTuning));
                }
            }
        }

        /// <summary>
        /// 램프 상승 변화율 (0: OFF, 1-9999)
        /// </summary>
        public ushort RampUpRate
        {
            get => _rampUpRate;
            internal set
            {
                if (_rampUpRate != value)
                {
                    _rampUpRate = value;
                    OnPropertyChanged(nameof(RampUpRate));
                    OnPropertyChanged(nameof(IsRampEnabled));
                    OnPropertyChanged(nameof(RampStatusText));
                }
            }
        }

        /// <summary>
        /// 램프 하강 변화율 (0: OFF, 1-9999)
        /// </summary>
        public ushort RampDownRate
        {
            get => _rampDownRate;
            internal set
            {
                if (_rampDownRate != value)
                {
                    _rampDownRate = value;
                    OnPropertyChanged(nameof(RampDownRate));
                    OnPropertyChanged(nameof(IsRampEnabled));
                    OnPropertyChanged(nameof(RampStatusText));
                }
            }
        }

        /// <summary>
        /// 램프 시간 단위 (0: 초, 1: 분, 2: 시간)
        /// </summary>
        public ushort RampTimeUnit
        {
            get => _rampTimeUnit;
            internal set
            {
                if (_rampTimeUnit != value)
                {
                    _rampTimeUnit = value;
                    OnPropertyChanged(nameof(RampTimeUnit));
                    OnPropertyChanged(nameof(RampTimeUnitText));
                    OnPropertyChanged(nameof(RampStatusText));
                }
            }
        }

        /// <summary>
        /// Ramp 기능 활성화 여부
        /// </summary>
        public bool IsRampEnabled => _rampUpRate > 0 || _rampDownRate > 0;

        /// <summary>
        /// Ramp 진행 중인지 여부
        /// </summary>
        public bool IsRampActive
        {
            get => _isRampActive;
            internal set
            {
                if (_isRampActive != value)
                {
                    _isRampActive = value;
                    OnPropertyChanged(nameof(IsRampActive));
                }
            }
        }

        /// <summary>
        /// 확장 모듈 채널 여부 (true: 입력 전용 채널)
        /// </summary>
        public bool IsExpansionChannel
        {
            get => _isExpansionChannel;
            internal set
            {
                if (_isExpansionChannel != value)
                {
                    _isExpansionChannel = value;
                    OnPropertyChanged(nameof(IsExpansionChannel));
                }
            }
        }

        /// <summary>
        /// 램프 시간 단위 텍스트
        /// </summary>
        public string RampTimeUnitText
        {
            get
            {
                return _rampTimeUnit switch
                {
                    0 => "초",
                    1 => "분",
                    2 => "시간",
                    _ => "알 수 없음"
                };
            }
        }

        /// <summary>
        /// Ramp 상태 텍스트
        /// </summary>
        public string RampStatusText
        {
            get
            {
                if (!IsRampEnabled)
                    return "Ramp OFF";

                string unit = _rampTimeUnit switch
                {
                    0 => "/초",
                    1 => "/분",
                    2 => "/시간",
                    _ => ""
                };

                string status = "";
                if (_rampUpRate > 0)
                    status += $"상승: {_rampUpRate}{_temperatureUnit}{unit}";
                if (_rampDownRate > 0)
                {
                    if (status.Length > 0) status += ", ";
                    status += $"하강: {_rampDownRate}{_temperatureUnit}{unit}";
                }

                return status;
            }
        }

        /// <summary>
        /// 형식이 지정된 현재 측정값
        /// </summary>
        public string FormattedPresentValue
        {
            get
            {
                if (!string.IsNullOrEmpty(_sensorError))
                {
                    return _sensorError;
                }

                return FormatTemperature(_presentValue, _dot, _temperatureUnit);
            }
        }

        /// <summary>
        /// 형식이 지정된 설정값
        /// </summary>
        public string FormattedSetValue
        {
            get
            {
                return FormatTemperature(_setValue, _dot, _temperatureUnit);
            }
        }

        /// <summary>
        /// 온도 값을 표시용 문자열로 변환합니다.
        /// </summary>
        private static string FormatTemperature(short value, int dot, string unit)
        {
            if (dot == 0)
            {
                return $"{value}";
            }
            else
            {
                return $"{value / 10.0:F1}";
            }
        }
    }

    /// <summary>
    /// 온도 컨트롤러의 전체 상태 정보를 저장하는 클래스입니다.
    /// </summary>
    public class TemperatureControllerStatus : INotifyPropertyChanged
    {
        private int _channelCount;
        private ChannelStatus[] _channelStatus;
        private ushort _productNumberH;
        private ushort _productNumberL;
        private ushort _hardwareVersion;
        private ushort _softwareVersion;
        private string _modelName;
        private ushort _baudRate;
        private ushort _parityBit;
        private ushort _stopBit;
        private ushort _responseWaitingTime;
        private ushort _communicationWrite;
        private ushort _communicationProtocol;

        /// <summary>
        /// 속성 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 속성 변경 이벤트를 발생시킵니다.
        /// </summary>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 기본 생성자 (5채널 지원: 메인 2 + 확장 3)
        /// </summary>
        public TemperatureControllerStatus()
        {
            _channelCount = 5;  // 메인 2채널 + 확장 3채널
            _channelStatus = new ChannelStatus[_channelCount];
            _modelName = "";

            for (int i = 0; i < _channelCount; i++)
            {
                _channelStatus[i] = new ChannelStatus
                {
                    ChannelNumber = i + 1,
                    IsExpansionChannel = (i >= 2)  // CH3 이상은 확장 채널
                };
            }
        }

        /// <summary>
        /// 채널 수
        /// </summary>
        public int ChannelCount
        {
            get => _channelCount;
            internal set
            {
                if (_channelCount != value)
                {
                    _channelCount = value;

                    // 채널 수가 변경되면 채널 상태 배열 재생성
                    if (_channelStatus == null || _channelStatus.Length != value)
                    {
                        ChannelStatus[] newStatus = new ChannelStatus[value];

                        // 기존 채널 상태 복사
                        if (_channelStatus != null)
                        {
                            for (int i = 0; i < Math.Min(_channelStatus.Length, value); i++)
                            {
                                newStatus[i] = _channelStatus[i];
                            }
                        }

                        // 새로운 채널 초기화
                        for (int i = (_channelStatus?.Length ?? 0); i < value; i++)
                        {
                            newStatus[i] = new ChannelStatus
                            {
                                ChannelNumber = i + 1,
                                IsExpansionChannel = (i >= 2)  // CH3 이상은 확장 채널
                            };
                        }

                        _channelStatus = newStatus;
                    }

                    OnPropertyChanged(nameof(ChannelCount));
                    OnPropertyChanged(nameof(ChannelStatus));
                }
            }
        }

        /// <summary>
        /// 채널별 상태 정보
        /// </summary>
        public ChannelStatus[] ChannelStatus
        {
            get => _channelStatus;
        }

        /// <summary>
        /// 제품 번호 H
        /// </summary>
        public ushort ProductNumberH
        {
            get => _productNumberH;
            internal set
            {
                if (_productNumberH != value)
                {
                    _productNumberH = value;
                    OnPropertyChanged(nameof(ProductNumberH));
                }
            }
        }

        /// <summary>
        /// 제품 번호 L
        /// </summary>
        public ushort ProductNumberL
        {
            get => _productNumberL;
            internal set
            {
                if (_productNumberL != value)
                {
                    _productNumberL = value;
                    OnPropertyChanged(nameof(ProductNumberL));
                }
            }
        }

        /// <summary>
        /// 하드웨어 버전
        /// </summary>
        public ushort HardwareVersion
        {
            get => _hardwareVersion;
            internal set
            {
                if (_hardwareVersion != value)
                {
                    _hardwareVersion = value;
                    OnPropertyChanged(nameof(HardwareVersion));
                }
            }
        }

        /// <summary>
        /// 소프트웨어 버전
        /// </summary>
        public ushort SoftwareVersion
        {
            get => _softwareVersion;
            internal set
            {
                if (_softwareVersion != value)
                {
                    _softwareVersion = value;
                    OnPropertyChanged(nameof(SoftwareVersion));
                }
            }
        }

        /// <summary>
        /// 모델명
        /// </summary>
        public string ModelName
        {
            get => _modelName;
            internal set
            {
                if (_modelName != value)
                {
                    _modelName = value;
                    OnPropertyChanged(nameof(ModelName));
                }
            }
        }

        /// <summary>
        /// 통신 속도
        /// </summary>
        public ushort BaudRate
        {
            get => _baudRate;
            internal set
            {
                if (_baudRate != value)
                {
                    _baudRate = value;
                    OnPropertyChanged(nameof(BaudRate));
                    OnPropertyChanged(nameof(BaudRateString));
                }
            }
        }

        /// <summary>
        /// 패리티 비트
        /// </summary>
        public ushort ParityBit
        {
            get => _parityBit;
            internal set
            {
                if (_parityBit != value)
                {
                    _parityBit = value;
                    OnPropertyChanged(nameof(ParityBit));
                    OnPropertyChanged(nameof(ParityBitString));
                }
            }
        }

        /// <summary>
        /// 정지 비트
        /// </summary>
        public ushort StopBit
        {
            get => _stopBit;
            internal set
            {
                if (_stopBit != value)
                {
                    _stopBit = value;
                    OnPropertyChanged(nameof(StopBit));
                    OnPropertyChanged(nameof(StopBitString));
                }
            }
        }

        /// <summary>
        /// 응답 대기 시간
        /// </summary>
        public ushort ResponseWaitingTime
        {
            get => _responseWaitingTime;
            internal set
            {
                if (_responseWaitingTime != value)
                {
                    _responseWaitingTime = value;
                    OnPropertyChanged(nameof(ResponseWaitingTime));
                }
            }
        }

        /// <summary>
        /// 통신 쓰기 허용 (0: ENABLE, 1: DISABLE)
        /// </summary>
        public ushort CommunicationWrite
        {
            get => _communicationWrite;
            internal set
            {
                if (_communicationWrite != value)
                {
                    _communicationWrite = value;
                    OnPropertyChanged(nameof(CommunicationWrite));
                }
            }
        }

        /// <summary>
        /// 통신 프로토콜 (0: RTU, 1: ASCII)
        /// </summary>
        public ushort CommunicationProtocol
        {
            get => _communicationProtocol;
            internal set
            {
                if (_communicationProtocol != value)
                {
                    _communicationProtocol = value;
                    OnPropertyChanged(nameof(CommunicationProtocol));
                }
            }
        }

        /// <summary>
        /// 포맷된 통신 속도 문자열 반환
        /// </summary>
        public string BaudRateString
        {
            get
            {
                switch (_baudRate)
                {
                    case 0: return "2400 bps";
                    case 1: return "4800 bps";
                    case 2: return "9600 bps";
                    case 3: return "19200 bps";
                    case 4: return "38400 bps";
                    case 5: return "57600 bps";
                    case 6: return "115200 bps";
                    default: return $"알 수 없음 ({_baudRate})";
                }
            }
        }

        /// <summary>
        /// 포맷된 패리티 비트 문자열 반환
        /// </summary>
        public string ParityBitString
        {
            get
            {
                switch (_parityBit)
                {
                    case 0: return "NONE";
                    case 1: return "EVEN";
                    case 2: return "ODD";
                    default: return $"알 수 없음 ({_parityBit})";
                }
            }
        }

        /// <summary>
        /// 포맷된 정지 비트 문자열 반환
        /// </summary>
        public string StopBitString
        {
            get
            {
                switch (_stopBit)
                {
                    case 0: return "1 Bit";
                    case 1: return "2 Bit";
                    default: return $"알 수 없음 ({_stopBit})";
                }
            }
        }
    }
}