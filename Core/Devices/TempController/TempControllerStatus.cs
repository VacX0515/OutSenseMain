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

        /// <summary>
        /// 속성 변경 이벤트
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

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
            set
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
            set
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
            set
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
            set
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
            set
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
            set
            {
                if (_temperatureUnit != value)
                {
                    _temperatureUnit = value;
                    OnPropertyChanged(nameof(TemperatureUnit));
                    OnPropertyChanged(nameof(FormattedPresentValue));
                    OnPropertyChanged(nameof(FormattedSetValue));
                }
            }
        }

        /// <summary>
        /// 가열측 조작량 (0.0 ~ 100.0%)
        /// </summary>
        public float HeatingMV
        {
            get => _heatingMV;
            set
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
            set
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
            set
            {
                if (_sensorError != value)
                {
                    _sensorError = value;
                    OnPropertyChanged(nameof(SensorError));
                }
            }
        }

        /// <summary>
        /// 오토튜닝 실행 중인지 여부
        /// </summary>
        public bool IsAutoTuning
        {
            get => _isAutoTuning;
            set
            {
                if (_isAutoTuning != value)
                {
                    _isAutoTuning = value;
                    OnPropertyChanged(nameof(IsAutoTuning));
                }
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

                return TempController.FormatTemperature(_presentValue, _dot, _temperatureUnit);
            }
        }

        /// <summary>
        /// 형식이 지정된 설정값
        /// </summary>
        public string FormattedSetValue
        {
            get => TempController.FormatTemperature(_setValue, _dot, _temperatureUnit);
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
        /// <param name="propertyName">속성 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public TemperatureControllerStatus()
        {
            _channelCount = 4;
            _channelStatus = new ChannelStatus[_channelCount];

            for (int i = 0; i < _channelCount; i++)
            {
                _channelStatus[i] = new ChannelStatus
                {
                    ChannelNumber = i + 1
                };
            }
        }

        /// <summary>
        /// 채널 수
        /// </summary>
        public int ChannelCount
        {
            get => _channelCount;
            set
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
                                ChannelNumber = i + 1
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
            set
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
            set
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
            set
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
            set
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
            set
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
            set
            {
                if (_baudRate != value)
                {
                    _baudRate = value;
                    OnPropertyChanged(nameof(BaudRate));
                }
            }
        }

        /// <summary>
        /// 패리티 비트
        /// </summary>
        public ushort ParityBit
        {
            get => _parityBit;
            set
            {
                if (_parityBit != value)
                {
                    _parityBit = value;
                    OnPropertyChanged(nameof(ParityBit));
                }
            }
        }

        /// <summary>
        /// 정지 비트
        /// </summary>
        public ushort StopBit
        {
            get => _stopBit;
            set
            {
                if (_stopBit != value)
                {
                    _stopBit = value;
                    OnPropertyChanged(nameof(StopBit));
                }
            }
        }

        /// <summary>
        /// 응답 대기 시간
        /// </summary>
        public ushort ResponseWaitingTime
        {
            get => _responseWaitingTime;
            set
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
            set
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
            set
            {
                if (_communicationProtocol != value)
                {
                    _communicationProtocol = value;
                    OnPropertyChanged(nameof(CommunicationProtocol));
                }
            }
        }

        /// <summary>
        /// 포맷된 통신 속도 문자열 반환 (2: 9600 bps)
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
        /// 포맷된 패리티 비트 문자열 반환 (0: NONE)
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
        /// 포맷된 정지 비트 문자열 반환 (0: 1 Bit, 1: 2 Bit)
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