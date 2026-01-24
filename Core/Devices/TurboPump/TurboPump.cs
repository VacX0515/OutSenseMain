using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.TurboPump
{
    /// <summary>
    /// MAG integra 터보 분자 펌프 제어 및 모니터링을 위한 클래스입니다.
    /// USS 프로토콜을 사용하여 RS-232/RS-485 인터페이스를 통해 통신합니다.
    /// 
    /// 참조 매뉴얼:
    /// - MAG integra Installation & Operating Instructions (300324726_002_C2)
    /// - MAG.DRIVE S/iS Serial Interfaces Operating Instructions (17200308_002_C1)
    /// </summary>
    public class TurboPump : DeviceBase
    {
        #region 상수 및 열거형

        // USS 프로토콜 상수 (매뉴얼 p.13)
        private const byte STX = 0x02;               // 시작 바이트
        private const byte USS_LGE = 0x16;           // 페이로드 데이터 길이 (22 = 바이트 3-22 + 2)
        private const byte USS_ADR = 0x00;           // RS-232의 경우 기본 주소는 0

        // USS PKE 액세스 타입 (매뉴얼 p.15)
        private const int PKE_NO_ACCESS = 0x0000;     // 액세스 없음
        private const int PKE_READ_16BIT = 0x1000;    // 16비트 파라미터 값 요청
        private const int PKE_READ_32BIT = 0x2000;    // 32비트 파라미터 값 요청 (응답용)
        private const int PKE_WRITE_16BIT = 0x2000;   // 16비트 파라미터 값 쓰기
        private const int PKE_WRITE_32BIT = 0x3000;   // 32비트 파라미터 값 쓰기
        private const int PKE_READ_ARRAY = 0x6000;    // 배열 값 요청
        private const int PKE_WRITE_ARRAY_16BIT = 0x7000;  // 16비트 배열 값 쓰기
        private const int PKE_WRITE_ARRAY_32BIT = 0x8000;  // 32비트 배열 값 쓰기

        // 파라미터 번호 (매뉴얼 p.19-23)
        private const ushort PARAM_ACTUAL_FREQUENCY = 3;          // 현재 로터 주파수 (Hz)
        private const ushort PARAM_ACTUAL_VOLTAGE = 4;            // 현재 중간 회로 전압 (0.1V)
        private const ushort PARAM_ACTUAL_CURRENT = 5;            // 현재 모터 전류 (0.1A)
        private const ushort PARAM_ACTUAL_POWER = 6;              // 현재 전기 전력 (0.1W)
        private const ushort PARAM_MOTOR_TEMP = 7;                // 모터 온도 (°C)
        private const ushort PARAM_SAVE_DATA = 8;                 // 데이터 저장 명령
        private const ushort PARAM_CONVERTER_TEMP = 11;           // 변환기 온도 (°C)
        private const ushort PARAM_MAX_FREQUENCY = 18;            // 최대 허용 주파수 (Hz)
        private const ushort PARAM_MIN_FREQUENCY = 19;            // 최소 허용 주파수 (Hz)
        private const ushort PARAM_SETPOINT_FREQUENCY = 24;       // 주파수 설정값 (600-1200 Hz, 기본 980)
        private const ushort PARAM_NORMAL_OPERATION_LEVEL = 25;   // 정상 운전 레벨 (35-99%, 기본 90)
        private const ushort PARAM_BEARING_TEMP = 125;            // 베어링 온도 (°C)
        private const ushort PARAM_STANDBY_FREQUENCY = 150;       // 대기 모드 주파수 (0-1200 Hz, 기본 250)
        private const ushort PARAM_ERROR_CODE = 171;              // 오류 코드 메모리 (인덱스 0-39)
        private const ushort PARAM_ERROR_FREQUENCY = 174;         // 오류 발생 시 주파수
        private const ushort PARAM_ERROR_HOURS = 176;             // 오류 발생 시 운전 시간
        private const ushort PARAM_PROFIBUS_WATCHDOG = 181;       // Profibus 와치독 (0.1s, 기본 200)
        private const ushort PARAM_RS232_WATCHDOG = 182;          // RS232/485 와치독 (0.1s, 기본 0)
        private const ushort PARAM_WARNING_BITS1 = 227;           // 경고 비트 1
        private const ushort PARAM_WARNING_BITS2 = 228;           // 경고 비트 2
        private const ushort PARAM_WARNING_BITS3 = 230;           // 경고 비트 3
        private const ushort PARAM_WARNING_BITS4 = 232;           // 경고 비트 4

        // 제어 워드 비트 - PZD1 STW (매뉴얼 p.17)
        private const int CTL_START_STOP = 0x0001;             // Bit 0: 시작/정지 (1=시작, 0=정지)
        private const int CTL_ENABLE_SETPOINT = 0x0040;        // Bit 6: PZD2 설정값 활성화
        private const int CTL_ERROR_RESET = 0x0080;            // Bit 7: 오류 리셋 (0→1 전환 시)
        private const int CTL_STANDBY = 0x0100;                // Bit 8: 대기 모드 활성화
        private const int CTL_ENABLE_REMOTE = 0x0400;          // Bit 10: 원격 제어 활성화 (필수)
        private const int CTL_PURGE_GAS = 0x0800;              // Bit 11: 퍼지 가스 on/off
        private const int CTL_VENTING = 0x1000;                // Bit 12: 벤트 밸브 on/off
        private const int CTL_AUTO_VENTING = 0x8000;           // Bit 15: 자동 벤트 (P134=21 필요)

        // 상태 워드 비트 - PZD1 ZSW (매뉴얼 p.18)
        private const int STS_READY = 0x0001;                  // Bit 0: 준비됨 (오류 없음)
        private const int STS_OPERATION_ENABLED = 0x0004;      // Bit 2: 작동 활성화됨
        private const int STS_FAILURE = 0x0008;                // Bit 3: 오류 발생
        private const int STS_ACCELERATION = 0x0010;           // Bit 4: 가속 중
        private const int STS_DECELERATION = 0x0020;           // Bit 5: 감속 중
        private const int STS_SWITCH_ON_LOCK = 0x0040;         // Bit 6: 스위치 온 잠금
        private const int STS_TEMP_WARNING = 0x0080;           // Bit 7: 온도 경고
        private const int STS_PARAM_ACCEPTED = 0x0200;         // Bit 9: 파라미터 채널 준비
        private const int STS_NORMAL_OPERATION = 0x0400;       // Bit 10: 정상 운영 도달
        private const int STS_PUMP_ROTATING = 0x0800;          // Bit 11: 펌프 회전 중 (f > 3Hz)
        private const int STS_FAILURE_COUNTER = 0x1000;        // Bit 12: 내부 카운터 알람
        private const int STS_OVERLOAD_WARNING = 0x2000;       // Bit 13: 과부하 경고
        private const int STS_REMOTE_ACTIVE = 0x8000;          // Bit 15: 원격 제어 활성화됨

        // 주파수 범위 상수 (매뉴얼 p.19, 21)
        private const ushort FREQ_MIN_SETPOINT = 600;          // Parameter 24 최소값
        private const ushort FREQ_MAX_SETPOINT = 1200;         // Parameter 24 최대값
        private const ushort FREQ_MIN_STANDBY = 0;             // Parameter 150 최소값
        private const ushort FREQ_MAX_STANDBY = 1200;          // Parameter 150 최대값
        private const ushort FREQ_DEFAULT_SETPOINT = 980;      // Parameter 24 기본값
        private const ushort FREQ_DEFAULT_STANDBY = 250;       // Parameter 150 기본값

        #endregion

        #region 필드 및 속성

        private readonly ICommunicationManager _communicationManager;
        private int _timeout = 1000;
        private bool _isUpdatingStatus = false;
        private int _deviceAddress = 0;
        private string _model;

        private TurboPumpStatus _currentStatus = new TurboPumpStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        private int _currentControlWord = 0;
        private ushort _currentSetpointFrequency = 0;  // PZD2로 전송할 속도 설정값
        private bool _isInitialized = false;
        private readonly object _controlWordLock = new object();
        private Dictionary<ushort, ushort> _parameterCache = new Dictionary<ushort, ushort>();

        /// <summary>
        /// 현재 펌프 상태 정보
        /// </summary>
        public TurboPumpStatus Status => _currentStatus;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "MAG integra Turbo Pump";

        /// <summary>
        /// 장치 모델
        /// </summary>
        public override string Model => _model;

        /// <summary>
        /// 장치 주소 (RS-485: 0-31, Profibus: 1-126)
        /// </summary>
        public int DeviceAddress
        {
            get => _deviceAddress;
            set
            {
                if (value >= 0 && value <= 126)
                {
                    _deviceAddress = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "장치 주소는 0에서 126 사이여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 통신 타임아웃(ms)
        /// </summary>
        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                SetTimeout(value);
            }
        }

        /// <summary>
        /// 펌프 초기화 완료 여부
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 제어 워드 값
        /// </summary>
        public int CurrentControlWord => _currentControlWord;

        /// <summary>
        /// 현재 설정된 목표 주파수 (PZD2)
        /// </summary>
        public ushort CurrentSetpointFrequency => _currentSetpointFrequency;

        // 상태 프로퍼티들
        public bool IsRunning => _currentStatus.IsRunning;
        public bool IsAccelerating => _currentStatus.IsAccelerating;
        public bool IsDecelerating => _currentStatus.IsDecelerating;
        public bool IsInNormalOperation => _currentStatus.IsInNormalOperation;
        public bool HasWarning => _currentStatus.HasWarning;
        public bool HasError => _currentStatus.HasError;
        public bool IsVented => _currentStatus.IsVented;

        #endregion

        #region 생성자

        /// <summary>
        /// TurboPump 인스턴스 초기화
        /// </summary>
        /// <param name="communicationManager">통신 관리자</param>
        /// <param name="model">펌프 모델명</param>
        /// <param name="deviceAddress">장치 주소</param>
        public TurboPump(ICommunicationManager communicationManager,
                         string model = "MAG W 1300",
                         int deviceAddress = 0)
            : base(communicationManager)
        {
            _communicationManager = communicationManager;
            _model = model;
            DeviceAddress = deviceAddress;
            DeviceId = $"{model}";
        }

        /// <summary>
        /// TurboPump 인스턴스 초기화 (싱글톤 통신 관리자 사용)
        /// </summary>
        public TurboPump(string model = "MAG W 1300", int deviceAddress = 0)
            : this(MultiPortSerialManager.Instance, model, deviceAddress)
        {
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 연결 후 초기화 작업 수행
        /// </summary>
        protected override void InitializeAfterConnection()
        {
            try
            {
                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                    "터보 펌프 초기화 중...", DeviceStatusCode.Initializing));

                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();
                Thread.Sleep(300);

                // 초기 파라미터 읽기
                InitializeParameters();

                // 와치독 비활성화
                DisableWatchdog();

                // 초기 제어 워드 설정
                SetInitialControlWord();

                // 상태 확인
                if (CheckStatus())
                {
                    ReadPumpInfo();
                    _isInitialized = true;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        "터보 펌프 초기화 완료", DeviceStatusCode.Ready));
                }
                else
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        "터보 펌프 상태 확인 실패", DeviceStatusCode.Warning));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 초기화 실패: {ex.Message}");
            }
        }

        private void InitializeParameters()
        {
            try
            {
                // 현재 설정값 읽기
                ushort value;
                if (ReadParameter(PARAM_SETPOINT_FREQUENCY, out value))
                {
                    _parameterCache[PARAM_SETPOINT_FREQUENCY] = value;
                    _currentSetpointFrequency = value;
                }

                if (ReadParameter(PARAM_STANDBY_FREQUENCY, out value))
                {
                    _parameterCache[PARAM_STANDBY_FREQUENCY] = value;
                }

                // 상태 워드 읽기
                ushort statusWord;
                if (ReadStatusWord(out statusWord))
                {
                    UpdateStatusFromStatusWord(statusWord);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"초기 파라미터 읽기 실패: {ex.Message}");
            }
        }

        private void SetInitialControlWord()
        {
            lock (_controlWordLock)
            {
                // 원격 제어 활성화
                _currentControlWord = CTL_ENABLE_REMOTE;

                // 펌프가 이미 실행 중이면 시작 비트 유지
                if (_currentStatus.IsRunning)
                {
                    _currentControlWord |= CTL_START_STOP;
                }

                SendControlCommand(_currentControlWord, _currentSetpointFrequency);
            }
        }

        #endregion

        #region 펌프 제어 메서드

        /// <summary>
        /// 펌프를 시작합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Start()
        {
            EnsureConnected();

            try
            {
                lock (_controlWordLock)
                {
                    _currentControlWord |= CTL_ENABLE_REMOTE | CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "터보 펌프 시작", DeviceStatusCode.Running));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 정지합니다.
        /// 주의: 메인 스위치로 정지하지 마세요. 터치다운 베어링이 마모됩니다. (매뉴얼 p.40)
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Stop()
        {
            EnsureConnected();

            try
            {
                lock (_controlWordLock)
                {
                    _currentControlWord &= ~CTL_START_STOP;
                    _currentControlWord |= CTL_ENABLE_REMOTE;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "터보 펌프 정지 명령 전송", DeviceStatusCode.Idle));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 정지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 실시간으로 펌프 회전 속도를 설정합니다. (PZD2 사용)
        /// Control Word Bit 6이 활성화되어 PZD2 값이 속도 설정값으로 사용됩니다.
        /// </summary>
        /// <param name="frequencyHz">목표 주파수 (Hz), 유효 범위: 600-1200 Hz</param>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.17 (Control Word Bit 6), p.19 (Parameter 24)
        /// 이 메서드는 PZD2를 통해 실시간으로 속도를 제어합니다.
        /// 영구 저장하려면 SetRotationSpeedPermanent()를 사용하세요.
        /// </remarks>
        public bool SetRotationSpeed(ushort frequencyHz)
        {
            EnsureConnected();

            try
            {
                // 주파수 범위 검증 (매뉴얼 p.19: 600-1200 Hz)
                if (frequencyHz < FREQ_MIN_SETPOINT || frequencyHz > FREQ_MAX_SETPOINT)
                {
                    OnErrorOccurred($"유효하지 않은 주파수입니다. " +
                        $"허용 범위: {FREQ_MIN_SETPOINT}-{FREQ_MAX_SETPOINT} Hz");
                    return false;
                }

                lock (_controlWordLock)
                {
                    // PZD2 설정값 활성화 (Bit 6)
                    _currentControlWord |= CTL_ENABLE_REMOTE | CTL_ENABLE_SETPOINT;

                    // 펌프가 실행 중이면 시작 비트 유지
                    if (_currentStatus.IsRunning)
                    {
                        _currentControlWord |= CTL_START_STOP;
                    }

                    _currentSetpointFrequency = frequencyHz;

                    bool result = SendControlCommand(_currentControlWord, frequencyHz);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            $"속도 설정: {frequencyHz} Hz", DeviceStatusCode.Running));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프 회전 속도를 영구적으로 설정합니다. (Parameter 24 사용)
        /// </summary>
        /// <param name="frequencyHz">목표 주파수 (Hz), 유효 범위: 600-1200 Hz</param>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.19 (Parameter 24), p.37 (Example 4)
        /// 저장 과정은 수 초가 소요되며, 이 동안 전원을 차단하지 마세요.
        /// </remarks>
        public bool SetRotationSpeedPermanent(ushort frequencyHz)
        {
            EnsureConnected();

            try
            {
                if (frequencyHz < FREQ_MIN_SETPOINT || frequencyHz > FREQ_MAX_SETPOINT)
                {
                    OnErrorOccurred($"유효하지 않은 주파수입니다. " +
                        $"허용 범위: {FREQ_MIN_SETPOINT}-{FREQ_MAX_SETPOINT} Hz");
                    return false;
                }

                // Parameter 24에 값 쓰기
                if (!WriteParameter(PARAM_SETPOINT_FREQUENCY, frequencyHz))
                {
                    return false;
                }

                // 영구 저장 (매뉴얼 p.12, 37)
                if (!SaveParameters())
                {
                    OnErrorOccurred("파라미터 저장 실패");
                    return false;
                }

                _parameterCache[PARAM_SETPOINT_FREQUENCY] = frequencyHz;
                _currentSetpointFrequency = frequencyHz;

                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                    $"속도 영구 설정 완료: {frequencyHz} Hz", DeviceStatusCode.Ready));
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"속도 영구 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 대기 모드 속도를 설정합니다.
        /// </summary>
        /// <param name="frequencyHz">대기 모드 주파수 (Hz), 유효 범위: 0-1200 Hz (기본: 250)</param>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.21 (Parameter 150)
        /// 스탠바이 속도 범위: 13,800 min⁻¹ (230 Hz) ~ 정격 속도 (매뉴얼 p.13)
        /// </remarks>
        public bool SetStandbySpeed(ushort frequencyHz)
        {
            EnsureConnected();

            try
            {
                if (frequencyHz > FREQ_MAX_STANDBY)
                {
                    OnErrorOccurred($"유효하지 않은 대기 모드 주파수입니다. " +
                        $"최대값: {FREQ_MAX_STANDBY} Hz");
                    return false;
                }

                if (!WriteParameter(PARAM_STANDBY_FREQUENCY, frequencyHz))
                {
                    return false;
                }

                if (!SaveParameters())
                {
                    OnErrorOccurred("파라미터 저장 실패");
                    return false;
                }

                _parameterCache[PARAM_STANDBY_FREQUENCY] = frequencyHz;

                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                    $"대기 모드 속도 설정 완료: {frequencyHz} Hz", DeviceStatusCode.Ready));
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"대기 모드 속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 대기 모드로 전환합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.17 (Control Word Bit 8)
        /// 대기 모드 활성화 시 Parameter 150의 주파수로 감속됩니다.
        /// </remarks>
        public bool SetStandbyMode()
        {
            EnsureConnected();

            try
            {
                if (!_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 실행 중이 아닐 때는 대기 모드로 전환할 수 없습니다.");
                    return false;
                }

                lock (_controlWordLock)
                {
                    _currentControlWord |= CTL_STANDBY | CTL_ENABLE_REMOTE | CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "대기 모드 활성화", DeviceStatusCode.Standby));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"대기 모드 전환 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 정상 모드로 전환합니다 (대기 모드 해제).
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool SetNormalMode()
        {
            EnsureConnected();

            try
            {
                if (!_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 실행 중이 아닐 때는 정상 모드로 전환할 수 없습니다.");
                    return false;
                }

                lock (_controlWordLock)
                {
                    _currentControlWord &= ~CTL_STANDBY;
                    _currentControlWord |= CTL_ENABLE_REMOTE | CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "정상 모드 전환", DeviceStatusCode.Running));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"정상 모드 전환 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프 오류를 리셋합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.17 (Control Word Bit 7)
        /// 주의: Bit 0(START)이 비활성 상태일 때만 리셋 가능
        /// 0→1 전환 시에만 리셋이 동작합니다.
        /// </remarks>
        public bool ResetError()
        {
            EnsureConnected();

            try
            {
                lock (_controlWordLock)
                {
                    // START 비트 해제 상태에서 리셋
                    int resetControlWord = CTL_ENABLE_REMOTE | CTL_ERROR_RESET;

                    bool result = SendControlCommand(resetControlWord, 0);
                    if (result)
                    {
                        Thread.Sleep(200);

                        // 리셋 비트 해제
                        _currentControlWord = CTL_ENABLE_REMOTE;
                        SendControlCommand(_currentControlWord, 0);

                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "오류 리셋 완료", DeviceStatusCode.Ready));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"오류 리셋 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 벤트합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.41 (Venting), p.17 (Control Word Bit 12)
        /// 주의: 펌프가 정지된 상태에서만 벤트해야 합니다.
        /// 압력 상승 곡선을 준수하세요 (매뉴얼 p.41 Fig. 4.1)
        /// </remarks>
        public bool Vent()
        {
            EnsureConnected();

            try
            {
                UpdateStatus();
                if (_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 회전 중일 때는 벤트할 수 없습니다. 먼저 정지하세요.");
                    return false;
                }

                lock (_controlWordLock)
                {
                    _currentControlWord |= CTL_VENTING | CTL_ENABLE_REMOTE;
                    _currentControlWord &= ~CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (result)
                    {
                        Thread.Sleep(100);
                        _currentStatus.IsVented = true;
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "벤트 활성화", DeviceStatusCode.Idle));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"벤트 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 벤트 밸브를 닫습니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool CloseVent()
        {
            EnsureConnected();

            try
            {
                lock (_controlWordLock)
                {
                    _currentControlWord &= ~CTL_VENTING;
                    _currentControlWord |= CTL_ENABLE_REMOTE;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (result)
                    {
                        Thread.Sleep(100);
                        _currentStatus.IsVented = false;
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "벤트 밸브 닫힘", DeviceStatusCode.Ready));
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"벤트 밸브 닫기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 통신 와치독을 비활성화합니다.
        /// </summary>
        /// <returns>설정 성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.21 (Parameter 181, 182)
        /// Parameter 181: Profibus 와치독 (기본 200 = 20초)
        /// Parameter 182: RS232/485 와치독 (기본 0 = 비활성)
        /// </remarks>
        public bool DisableWatchdog()
        {
            EnsureConnected();

            try
            {
                bool rs232Result = WriteParameter(PARAM_RS232_WATCHDOG, 0);
                bool profibusResult = WriteParameter(PARAM_PROFIBUS_WATCHDOG, 0);

                if (rs232Result && profibusResult)
                {
                    SaveParameters();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        "와치독 비활성화 완료", DeviceStatusCode.Ready));
                }

                return rs232Result && profibusResult;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"와치독 비활성화 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 파라미터를 영구 저장합니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        /// <remarks>
        /// 매뉴얼 참조: p.12, p.37 (Example 4)
        /// 저장 과정은 수 초가 소요됩니다. LED가 순차적으로 점등됩니다.
        /// 저장 중 전원을 차단하지 마세요.
        /// </remarks>
        public bool SaveParameters()
        {
            // Parameter 8에 아무 값이나 쓰면 영구 저장 (매뉴얼 p.19)
            bool result = WriteParameter(PARAM_SAVE_DATA, 1);
            if (result)
            {
                // 저장 완료 대기 (LED 순차 점등)
                Thread.Sleep(500);
            }
            return result;
        }

        #endregion

        #region 상태 모니터링 메서드

        /// <summary>
        /// 장치 상태를 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true</returns>
        public override bool CheckStatus()
        {
            EnsureConnected();

            try
            {
                ushort statusWord;
                if (ReadStatusWord(out statusWord))
                {
                    UpdateStatusFromStatusWord(statusWord);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"상태 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프 상태를 업데이트합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부</returns>
        public bool UpdateStatus()
        {
            if (_isUpdatingStatus) return false;

            EnsureConnected();
            _isUpdatingStatus = true;

            try
            {
                // 상태 워드 읽기
                ushort statusWord;
                if (!ReadStatusWord(out statusWord))
                {
                    return false;
                }
                UpdateStatusFromStatusWord(statusWord);

                // 현재 주파수 읽기
                ushort frequency;
                if (ReadParameter(PARAM_ACTUAL_FREQUENCY, out frequency))
                {
                    _currentStatus.CurrentSpeed = frequency;
                }

                // 전류 읽기
                ushort current;
                if (ReadParameter(PARAM_ACTUAL_CURRENT, out current))
                {
                    _currentStatus.MotorCurrent = current / 10.0; // 0.1A 단위
                }

                // 온도 읽기
                ushort temp;
                if (ReadParameter(PARAM_MOTOR_TEMP, out temp))
                    _currentStatus.MotorTemperature = temp;
                if (ReadParameter(PARAM_CONVERTER_TEMP, out temp))
                    _currentStatus.ElectronicsTemperature = temp;
                if (ReadParameter(PARAM_BEARING_TEMP, out temp))
                    _currentStatus.BearingTemperature = temp;

                // 오류/경고 코드 읽기
                ushort code;
                if (ReadParameter(PARAM_ERROR_CODE, out code))
                    _currentStatus.ErrorCode = code;
                if (ReadParameter(PARAM_WARNING_BITS1, out code))
                    _currentStatus.WarningCode = code;

                _lastStatusUpdateTime = DateTime.Now;
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"상태 업데이트 오류: {ex.Message}");
                return false;
            }
            finally
            {
                _isUpdatingStatus = false;
            }
        }

        /// <summary>
        /// 비동기적으로 펌프 상태를 업데이트합니다.
        /// </summary>
        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

        private void UpdateStatusFromStatusWord(ushort statusWord)
        {
            _currentStatus.IsRunning = (statusWord & STS_PUMP_ROTATING) != 0;
            _currentStatus.IsAccelerating = (statusWord & STS_ACCELERATION) != 0;
            _currentStatus.IsDecelerating = (statusWord & STS_DECELERATION) != 0;
            _currentStatus.IsInNormalOperation = (statusWord & STS_NORMAL_OPERATION) != 0;
            _currentStatus.HasWarning = (statusWord & STS_TEMP_WARNING) != 0 ||
                                        (statusWord & STS_OVERLOAD_WARNING) != 0;
            _currentStatus.HasError = (statusWord & STS_FAILURE) != 0;
            _currentStatus.IsReady = (statusWord & STS_READY) != 0;
            _currentStatus.IsRemoteActive = (statusWord & STS_REMOTE_ACTIVE) != 0;
        }

        private void ReadPumpInfo()
        {
            try
            {
                uint runningHours;
                if (ReadParameter32Bit(60, out runningHours))
                {
                    _currentStatus.RunningTimeHours = runningHours / 100; // 0.01h 단위
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 정보 읽기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 오류 코드 설명을 반환합니다.
        /// </summary>
        /// <remarks>
        /// 매뉴얼 참조: p.24-29 (Error Memory)
        /// </remarks>
        public string GetErrorDescription()
        {
            if (!_currentStatus.HasError || _currentStatus.ErrorCode == 0)
                return null;

            // 매뉴얼 p.24-29 참조
            switch (_currentStatus.ErrorCode)
            {
                case 2: return "모터 온도 과열 (Motor Temperature too high)";
                case 3: return "공급 전압 오류 (Supply Voltage Failure)";
                case 4: return "변환기 온도 오류 (Converter Temperature Failure)";
                case 6: return "과부하 (Overload Failure)";
                case 7: return "가속 시간 초과 (Accel. Time)";
                case 9: return "베어링 온도 과열 (Bearing Temperature too high)";
                case 12: return "상부 자기 베어링 불균형 (Radial Bearing Unbalance Upper)";
                case 13: return "하부 자기 베어링 불균형 (Radial Bearing Unbalance Lower)";
                case 14: return "축방향 베어링 불균형 (Axial Bearing Unbalance)";
                case 16: return "과부하 지속 오류 (Overload Duration Failure)";
                case 17: return "모터 전류 오류 (Pump Motor Current Failure)";
                case 19: return "시작 시간 초과 (Starting Time exceeded)";
                case 26: return "베어링 온도 센서 오류";
                case 28: return "모터 온도 센서 오류";
                case 31: return "높은 부하 지속 오류 (Highload Duration Failure)";
                case 39: return "자기 베어링 시작 오류 (Magnetic Bearing Startup Failure)";
                case 43: return "과속 (Overspeed)";
                case 63: return "내부 파라미터 오류";
                case 65: return "펌프 통신 오류";
                case 66: return "자기 베어링 전류 과부하";
                case 67: return "내부 과부하";
                case 73: return "작동 사이클 한계 초과";
                case 74: return "작동 시간 한계 초과";
                case 77: return "베어링 접촉 횟수 오류";
                case 78: return "베어링 접촉 시간 오류";
                case 81: return "RS232/RS485 통신 중단";
                case 82: return "필드버스 통신 중단";
                case 90: return "속도 조정 오류 (Pump Speed Adjustment Failure)";
                default: return $"알 수 없는 오류 (코드: {_currentStatus.ErrorCode})";
            }
        }

        /// <summary>
        /// 펌프 상태 텍스트를 반환합니다.
        /// </summary>
        public string GetStatusText()
        {
            if (!IsConnected) return "연결 안됨";
            if (_currentStatus.HasError) return "오류: " + GetErrorDescription();
            if (_currentStatus.IsDecelerating) return "감속 중";
            if (!_currentStatus.IsRunning) return _currentStatus.IsVented ? "벤트됨" : "정지됨";
            if (_currentStatus.IsAccelerating) return "가속 중";
            if (_currentStatus.IsInNormalOperation) return "정상 운전 중";
            return "운전 중";
        }

        /// <summary>
        /// 속도를 백분율로 반환합니다.
        /// </summary>
        public int GetSpeedPercentage()
        {
            // 정격 속도 (매뉴얼 p.13)
            int nominalFreq;
            switch (_model)
            {
                case "MAG W 1300":
                    nominalFreq = 630; // 37,800 RPM
                    break;
                case "MAG W 1600":
                case "MAG 1601":
                case "MAG W 1700":
                    nominalFreq = 550; // 33,000 RPM
                    break;
                case "MAG W 2200":
                case "MAG 2201":
                    nominalFreq = 510; // 30,600 RPM
                    break;
                default:
                    nominalFreq = 630;
                    break;
            }

            int percentage = (int)Math.Round((_currentStatus.CurrentSpeed / (double)nominalFreq) * 100);
            return Math.Max(0, Math.Min(100, percentage));
        }

        #endregion

        #region USS 프로토콜 통신

        /// <summary>
        /// 상태 워드만 읽습니다 (PZD1 ZSW).
        /// </summary>
        private bool ReadStatusWord(out ushort statusWord)
        {
            statusWord = 0;

            byte[] telegram = CreateUssTelegram(0, PKE_NO_ACCESS, 0, _currentControlWord, 0);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                OnErrorOccurred("상태 읽기 요청 실패");
                return false;
            }

            byte[] response = _communicationManager.ReadAll();
            if (!ValidateResponse(response))
            {
                return false;
            }

            // PZD1 (상태 워드) 추출: byte 11-12
            statusWord = (ushort)((response[11] << 8) | response[12]);
            return true;
        }

        /// <summary>
        /// 제어 명령을 전송합니다.
        /// </summary>
        /// <param name="controlWord">제어 워드 (STW)</param>
        /// <param name="setpointFrequency">속도 설정값 (HSW, PZD2)</param>
        /// <returns>성공 여부</returns>
        private bool SendControlCommand(int controlWord, ushort setpointFrequency)
        {
            byte[] telegram = CreateUssTelegram(0, PKE_NO_ACCESS, 0, controlWord, setpointFrequency);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                OnErrorOccurred("제어 명령 전송 실패");
                return false;
            }

            byte[] response = _communicationManager.ReadAll();
            return ValidateResponse(response);
        }

        /// <summary>
        /// 16비트 파라미터를 읽습니다.
        /// </summary>
        private bool ReadParameter(ushort paramNumber, out ushort value)
        {
            value = 0;

            int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
            byte[] telegram = CreateUssTelegram(paramNumber, pke, 0, _currentControlWord, _currentSetpointFrequency);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                OnErrorOccurred($"파라미터 {paramNumber} 읽기 요청 실패");
                return false;
            }

            byte[] response = _communicationManager.ReadAll();
            if (!ValidateResponse(response))
            {
                return false;
            }

            // PKE 응답 확인
            int pkeResponse = (response[3] << 8) | response[4];
            int responseType = pkeResponse & 0xF000;

            if (responseType == 0x7000)
            {
                OnErrorOccurred($"파라미터 {paramNumber}: 명령 실행 불가");
                return false;
            }

            // PWE에서 값 추출: byte 9-10 (16비트)
            value = (ushort)((response[9] << 8) | response[10]);
            return true;
        }

        /// <summary>
        /// 32비트 파라미터를 읽습니다.
        /// </summary>
        private bool ReadParameter32Bit(ushort paramNumber, out uint value)
        {
            value = 0;

            int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
            byte[] telegram = CreateUssTelegram(paramNumber, pke, 0, _currentControlWord, _currentSetpointFrequency);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                return false;
            }

            byte[] response = _communicationManager.ReadAll();
            if (!ValidateResponse(response))
            {
                return false;
            }

            // PWE에서 32비트 값 추출: byte 7-10
            value = (uint)((response[7] << 24) | (response[8] << 16) |
                          (response[9] << 8) | response[10]);
            return true;
        }

        /// <summary>
        /// 16비트 파라미터를 씁니다.
        /// </summary>
        private bool WriteParameter(ushort paramNumber, ushort value)
        {
            // PKE_WRITE_16BIT 사용 (수정됨)
            int pke = PKE_WRITE_16BIT | (paramNumber & 0x0FFF);
            byte[] telegram = CreateUssTelegram(paramNumber, pke, value, _currentControlWord, _currentSetpointFrequency);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                OnErrorOccurred($"파라미터 {paramNumber} 쓰기 요청 실패");
                return false;
            }

            byte[] response = _communicationManager.ReadAll();
            if (!ValidateResponse(response))
            {
                return false;
            }

            // PKE 응답 확인
            int pkeResponse = (response[3] << 8) | response[4];
            int responseType = pkeResponse & 0xF000;

            if (responseType == 0x7000)
            {
                int errorCode = (response[9] << 8) | response[10];
                OnErrorOccurred($"파라미터 {paramNumber} 쓰기 실패: 오류 코드 {errorCode}");
                return false;
            }

            if (responseType == 0x8000)
            {
                OnErrorOccurred($"파라미터 {paramNumber}: 쓰기 권한 없음");
                return false;
            }

            return true;
        }

        /// <summary>
        /// USS 텔레그램을 생성합니다.
        /// </summary>
        /// <remarks>
        /// 매뉴얼 참조: p.13 (Telegram structure)
        /// 
        /// 텔레그램 구조 (24 바이트):
        /// [0]     STX: 시작 바이트 (0x02)
        /// [1]     LGE: 페이로드 길이 + 2 (0x16 = 22)
        /// [2]     ADR: 주소 (RS-232: 0, RS-485: 0-31)
        /// [3-4]   PKE: 파라미터 번호 + 액세스 타입
        /// [5]     Reserved
        /// [6]     IND: 파라미터 인덱스
        /// [7-10]  PWE: 파라미터 값 (32비트)
        /// [11-12] PZD1 (STW/ZSW): 제어/상태 워드
        /// [13-14] PZD2 (HSW/HIW): 속도 설정값/실제 주파수
        /// [15-16] PZD3: 변환기 온도
        /// [17-18] PZD4: 모터 전류
        /// [19-20] PZD5: 펌프 온도
        /// [21-22] PZD6: 중간 회로 전압
        /// [23]    BCC: 체크섬 (XOR)
        /// </remarks>
        private byte[] CreateUssTelegram(ushort paramNumber, int pke, ushort paramValue,
                                          int controlWord, ushort setpointFrequency)
        {
            byte[] telegram = new byte[24];

            // 헤더
            telegram[0] = STX;
            telegram[1] = USS_LGE;
            telegram[2] = (byte)_deviceAddress;

            // PKE (파라미터 번호 + 액세스 타입)
            telegram[3] = (byte)(pke >> 8);
            telegram[4] = (byte)(pke & 0xFF);

            // Reserved + IND
            telegram[5] = 0;
            telegram[6] = 0;

            // PWE (파라미터 값) - 16비트 값은 하위 2바이트에
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = (byte)(paramValue >> 8);
            telegram[10] = (byte)(paramValue & 0xFF);

            // PZD1 (STW: 제어 워드)
            telegram[11] = (byte)(controlWord >> 8);
            telegram[12] = (byte)(controlWord & 0xFF);

            // PZD2 (HSW: 속도 설정값)
            telegram[13] = (byte)(setpointFrequency >> 8);
            telegram[14] = (byte)(setpointFrequency & 0xFF);

            // PZD3-6: 0으로 설정
            for (int i = 15; i < 23; i++)
            {
                telegram[i] = 0;
            }

            // BCC 체크섬 계산
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// 응답을 검증합니다.
        /// </summary>
        private bool ValidateResponse(byte[] response)
        {
            if (response == null || response.Length < 24)
            {
                OnErrorOccurred("응답 길이 오류");
                return false;
            }

            if (response[0] != STX)
            {
                OnErrorOccurred("응답 시작 바이트 오류");
                return false;
            }

            // BCC 검증
            byte calculatedBcc = CalculateBCC(response, 0, 23);
            if (response[23] != calculatedBcc)
            {
                OnErrorOccurred("응답 체크섬 오류");
                return false;
            }

            return true;
        }

        /// <summary>
        /// BCC (Block Check Character) 체크섬을 계산합니다.
        /// </summary>
        /// <remarks>
        /// 매뉴얼 참조: p.13
        /// Checksum(i) = Checksum(i-1) XOR byte(i)
        /// </remarks>
        private byte CalculateBCC(byte[] data, int offset, int length)
        {
            byte bcc = data[offset];
            for (int i = offset + 1; i < offset + length; i++)
            {
                bcc ^= data[i];
            }
            return bcc;
        }

        #endregion
    }

    /// <summary>
    /// 터보 펌프 상태 정보 클래스
    /// </summary>
    public class TurboPumpStatus : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isAccelerating;
        private bool _isDecelerating;
        private bool _isInNormalOperation;
        private bool _hasWarning;
        private bool _hasError;
        private bool _isVented;
        private bool _isReady;
        private bool _isRemoteActive;
        private ushort _currentSpeed;
        private double _motorCurrent;
        private ushort _electronicsTemperature;
        private ushort _bearingTemperature;
        private ushort _motorTemperature;
        private ushort _errorCode;
        private ushort _warningCode;
        private uint _runningTimeHours;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsRunning
        {
            get => _isRunning;
            internal set { if (_isRunning != value) { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); } }
        }

        public bool IsAccelerating
        {
            get => _isAccelerating;
            internal set { if (_isAccelerating != value) { _isAccelerating = value; OnPropertyChanged(nameof(IsAccelerating)); } }
        }

        public bool IsDecelerating
        {
            get => _isDecelerating;
            internal set { if (_isDecelerating != value) { _isDecelerating = value; OnPropertyChanged(nameof(IsDecelerating)); } }
        }

        public bool IsInNormalOperation
        {
            get => _isInNormalOperation;
            internal set { if (_isInNormalOperation != value) { _isInNormalOperation = value; OnPropertyChanged(nameof(IsInNormalOperation)); } }
        }

        public bool HasWarning
        {
            get => _hasWarning;
            internal set { if (_hasWarning != value) { _hasWarning = value; OnPropertyChanged(nameof(HasWarning)); } }
        }

        public bool HasError
        {
            get => _hasError;
            internal set { if (_hasError != value) { _hasError = value; OnPropertyChanged(nameof(HasError)); } }
        }

        public bool IsVented
        {
            get => _isVented;
            internal set { if (_isVented != value) { _isVented = value; OnPropertyChanged(nameof(IsVented)); } }
        }

        public bool IsReady
        {
            get => _isReady;
            internal set { if (_isReady != value) { _isReady = value; OnPropertyChanged(nameof(IsReady)); } }
        }

        public bool IsRemoteActive
        {
            get => _isRemoteActive;
            internal set { if (_isRemoteActive != value) { _isRemoteActive = value; OnPropertyChanged(nameof(IsRemoteActive)); } }
        }

        /// <summary>
        /// 현재 속도 (Hz)
        /// </summary>
        public ushort CurrentSpeed
        {
            get => _currentSpeed;
            internal set { if (_currentSpeed != value) { _currentSpeed = value; OnPropertyChanged(nameof(CurrentSpeed)); } }
        }

        /// <summary>
        /// 모터 전류 (A)
        /// </summary>
        public double MotorCurrent
        {
            get => _motorCurrent;
            internal set { if (_motorCurrent != value) { _motorCurrent = value; OnPropertyChanged(nameof(MotorCurrent)); } }
        }

        /// <summary>
        /// 전자장치 온도 (°C)
        /// </summary>
        public ushort ElectronicsTemperature
        {
            get => _electronicsTemperature;
            internal set { if (_electronicsTemperature != value) { _electronicsTemperature = value; OnPropertyChanged(nameof(ElectronicsTemperature)); } }
        }

        /// <summary>
        /// 베어링 온도 (°C)
        /// </summary>
        public ushort BearingTemperature
        {
            get => _bearingTemperature;
            internal set { if (_bearingTemperature != value) { _bearingTemperature = value; OnPropertyChanged(nameof(BearingTemperature)); } }
        }

        /// <summary>
        /// 모터 온도 (°C)
        /// </summary>
        public ushort MotorTemperature
        {
            get => _motorTemperature;
            internal set { if (_motorTemperature != value) { _motorTemperature = value; OnPropertyChanged(nameof(MotorTemperature)); } }
        }

        /// <summary>
        /// 오류 코드
        /// </summary>
        public ushort ErrorCode
        {
            get => _errorCode;
            internal set { if (_errorCode != value) { _errorCode = value; OnPropertyChanged(nameof(ErrorCode)); } }
        }

        /// <summary>
        /// 경고 코드
        /// </summary>
        public ushort WarningCode
        {
            get => _warningCode;
            internal set { if (_warningCode != value) { _warningCode = value; OnPropertyChanged(nameof(WarningCode)); } }
        }

        /// <summary>
        /// 누적 운전 시간 (시간)
        /// </summary>
        public uint RunningTimeHours
        {
            get => _runningTimeHours;
            internal set { if (_runningTimeHours != value) { _runningTimeHours = value; OnPropertyChanged(nameof(RunningTimeHours)); } }
        }
    }
}