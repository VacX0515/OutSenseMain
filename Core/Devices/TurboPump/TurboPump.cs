using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.TurboPump
{
    /// <summary>
    /// MAG integra 터보 분자 펌프 제어 및 모니터링을 위한 클래스입니다.
    /// DeviceBase 클래스를 상속하여 표준화된 장치 인터페이스를 제공합니다.
    /// USS 프로토콜을 사용하여 RS-232/RS-485 인터페이스를 통해 통신합니다.
    /// </summary>
    public class TurboPump : DeviceBase
    {
        #region 상수 및 열거형



        // USS 프로토콜 상수
        private const byte STX = 0x02;               // 시작 바이트
        private const byte USS_LGE = 0x16;             // 페이로드 데이터 길이 (바이트 3-22) + 2
        private const byte USS_ADR = 0x00;           // RS-232의 경우 기본 주소는 0

        // USS PKE 액세스 타입 (상위 4비트)
        private const int PKE_READ_16BIT = 0x1000;    // 16비트 파라미터 값 요청
        private const int PKE_READ_32BIT = 0x2000;    // 32비트 파라미터 값 요청
        private const int PKE_WRITE_16BIT = 0x3000;   // 16비트 파라미터 값 쓰기
        private const int PKE_WRITE_32BIT = 0x4000;   // 32비트 파라미터 값 쓰기

        // 자주 사용하는 파라미터 번호 (MAG.DRIVE 매뉴얼 참조)
        private const ushort PARAM_ACTUAL_FREQUENCY = 3;          // 현재 로터 주파수
        private const ushort PARAM_ACTUAL_VOLTAGE = 4;            // 현재 중간 회로 전압
        private const ushort PARAM_ACTUAL_CURRENT = 5;            // 현재 모터 전류
        private const ushort PARAM_ACTUAL_POWER = 6;              // 현재 전기 전력
        private const ushort PARAM_MOTOR_TEMP = 7;                // 모터 온도
        private const ushort PARAM_CONVERTER_TEMP = 11;           // 변환기 온도
        private const ushort PARAM_BEARING_TEMP = 125;            // 베어링 온도
        private const ushort PARAM_ERROR_CODE = 171;              // 오류 코드 메모리
        private const ushort PARAM_WARNING_BITS1 = 227;           // 경고 비트 1
        private const ushort PARAM_WARNING_BITS2 = 228;           // 경고 비트 2
        private const ushort PARAM_WARNING_BITS3 = 230;           // 경고 비트 3
        private const ushort PARAM_SETPOINT_FREQUENCY = 24;       // 주파수 설정값
        private const ushort PARAM_STANDBY_FREQUENCY = 150;       // 대기 모드 주파수
        private const ushort PARAM_SAVE_DATA = 8;                 // 데이터 저장 명령

        // 제어 워드 비트 (PZD1)
        private const int CTL_START_STOP = 0x0001;             // 비트 0: 시작/정지
        private const int CTL_ENABLE_SETPOINT = 0x0040;        // 비트 6: 설정값 활성화
        private const int CTL_ERROR_RESET = 0x0080;            // 비트 7: 오류 리셋
        private const int CTL_STANDBY = 0x0100;                // 비트 8: 대기 모드 활성화
        private const int CTL_ENABLE_REMOTE = 0x0400;          // 비트 10: 원격 제어 활성화
        private const int CTL_PURGE_GAS = 0x0800;              // 비트 11: 퍼지 가스 on/off
        private const int CTL_VENTING = 0x1000;                // 비트 12: 벤트 밸브 on/off
        private const int CTL_AUTO_VENTING = 0x8000;           // 비트 15: 자동 벤트 활성화

        // 상태 워드 비트 (PZD1)
        private const int STS_READY = 0x0001;                  // 비트 0: 준비됨
        private const int STS_OPERATION_ENABLED = 0x0004;      // 비트 2: 작동 활성화됨
        private const int STS_FAILURE = 0x0008;                // 비트 3: 오류 발생
        private const int STS_ACCELERATION = 0x0010;           // 비트 4: 가속 중
        private const int STS_DECELERATION = 0x0020;           // 비트 5: 감속 중
        private const int STS_TEMP_WARNING = 0x0080;           // 비트 7: 온도 경고
        private const int STS_NORMAL_OPERATION = 0x0400;       // 비트 10: 정상 운영 중
        private const int STS_PUMP_ROTATING = 0x0800;          // 비트 11: 펌프 회전 중
        private const int STS_REMOTE_ACTIVE = 0x8000;          // 비트 15: 원격 제어 활성화
        #endregion

        #region 필드 및 속성

        // 통신 설정
        private int _timeout = 1000;  // 통신 타임아웃(ms)
        private bool _isUpdatingStatus = false;
        private int _deviceAddress = 1; // 기본 장치 주소

        // 상태 정보
        private TurboPumpStatus _currentStatus = new TurboPumpStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        // 현재 제어 워드
        private int _currentControlWord = 0;

        /// <summary>
        /// 현재 펌프 상태 정보를 가져옵니다.
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
        /// 장치 주소
        /// </summary>
        public int DeviceAddress
        {
            get => _deviceAddress;
            set
            {
                if (value >= 0 && value <= 126) // Profibus 주소 범위는 1-126
                {
                    _deviceAddress = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "장치 주소는 0에서 126 사이여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 통신 타임아웃(ms) - 기본값: 1000ms
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
        /// 펌프 모델
        /// </summary>
        private string _model;

        /// <summary>
        /// 펌프가 실행 중인지 여부
        /// </summary>
        public bool IsRunning => _currentStatus.IsRunning;

        /// <summary>
        /// 펌프가 가속 중인지 여부
        /// </summary>
        public bool IsAccelerating => _currentStatus.IsAccelerating;

        /// <summary>
        /// 펌프가 감속 중인지 여부
        /// </summary>
        public bool IsDecelerating => _currentStatus.IsDecelerating;

        /// <summary>
        /// 펌프가 정상 작동 중인지 여부
        /// </summary>
        public bool IsInNormalOperation => _currentStatus.IsInNormalOperation;

        /// <summary>
        /// 펌프에 경고가 있는지 여부
        /// </summary>
        public bool HasWarning => _currentStatus.HasWarning;

        /// <summary>
        /// 펌프에 오류가 있는지 여부
        /// </summary>
        public bool HasError => _currentStatus.HasError;

        /// <summary>
        /// 펌프가 벤트되었는지 여부
        /// </summary>
        public bool IsVented => _currentStatus.IsVented;


        // PZD1 값 유지를 위한 필드
        private bool _isInitialized = false;
        private readonly object _controlWordLock = new object();

        // 초기 파라미터 정보를 저장하는 딕셔너리
        private Dictionary<ushort, ushort> _parameterCache = new Dictionary<ushort, ushort>();

        /// <summary>
        /// 펌프가 초기화되었는지 여부
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 현재 제어 워드 값
        /// </summary>
        public int CurrentControlWord => _currentControlWord;

        #endregion

        #region 생성자

        /// <summary>
        /// TurboPump 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="model">펌프 모델 (기본값: "MAG W 1300")</param>
        /// <param name="deviceAddress">장치 주소 (기본값: 1)</param>
        public TurboPump(ICommunicationManager communicationManager, string model = "MAG W 1300", int deviceAddress = 1)
            : base(communicationManager)
        {
            _model = model;
            DeviceAddress = deviceAddress;
            DeviceId = $"{model}";
        }

        /// <summary>
        /// TurboPump 클래스의 새 인스턴스를 초기화합니다.
        /// SerialManager 싱글톤 인스턴스를 사용합니다.
        /// </summary>
        /// <param name="model">펌프 모델 (기본값: "MAG W 1300")</param>
        /// <param name="deviceAddress">장치 주소 (기본값: 1)</param>
        public TurboPump(string model = "MAG W 1300", int deviceAddress = 1)
            : this(MultiPortSerialManager.Instance, model, deviceAddress)
        {
        }

        #endregion

        #region IDevice 구현

        /// <summary>
        /// 연결 후 초기화 작업을 수행합니다.
        /// </summary>
        protected override void InitializeAfterConnection()
        {
            try
            {
                // 초기화 상태 이벤트 발생
                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 초기화 중...", DeviceStatusCode.Initializing));

                // 입출력 버퍼 비우기
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();

                // 초기 통신 딜레이
                Thread.Sleep(300);

                // 초기 파라미터 읽기
                InitializeParameters();

                // 와치독 비활성화 (선택적)
                DisableWatchdog();

                // 초기 제어 워드 설정
                SetInitialControlWord();

                // 연결 후 초기 상태 확인
                bool statusCheck = CheckStatus();
                if (statusCheck)
                {
                    // 추가 펌프 정보 읽기 (모델, 시리얼 번호 등)
                    ReadPumpInfo();

                    _isInitialized = true;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 상태 확인 성공", DeviceStatusCode.Ready));
                }
                else
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 상태 확인 실패, 다시 시도하세요", DeviceStatusCode.Warning));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 초기화 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 초기 파라미터를 읽어와 캐시에 저장합니다.
        /// </summary>
        private void InitializeParameters()
        {
            try
            {
                // 주요 파라미터 읽기 및 캐시에 저장
                ushort[] parameterNumbers = new ushort[]
                {
            PARAM_SETPOINT_FREQUENCY,    // 주파수 설정값
            PARAM_STANDBY_FREQUENCY,     // 대기 모드 주파수
            PARAM_ACTUAL_FREQUENCY,      // 현재 로터 주파수
            PARAM_MOTOR_TEMP,            // 모터 온도
            PARAM_CONVERTER_TEMP,        // 변환기 온도
            PARAM_BEARING_TEMP,          // 베어링 온도
            PARAM_ERROR_CODE,            // 오류 코드
            PARAM_WARNING_BITS1          // 경고 비트
                };

                foreach (ushort paramNumber in parameterNumbers)
                {
                    ushort value = 0;
                    if (ReadParameter(paramNumber, out value))
                    {
                        _parameterCache[paramNumber] = value;
                    }
                }

                // 상태 값 읽기
                ushort statusWord = 0;
                if (ReadParameter(0, out statusWord, true))
                {
                    UpdateStatusFromStatusWord(statusWord);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"초기 파라미터 읽기 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 초기 제어 워드를 설정합니다.
        /// </summary>
        private void SetInitialControlWord()
        {
            // 초기 제어 워드 설정 - 원격 제어만 활성화
            lock (_controlWordLock)
            {
                _currentControlWord = CTL_ENABLE_REMOTE;

                // 펌프가 이미 실행 중인 경우 시작 비트 추가
                if (_currentStatus.IsRunning)
                {
                    _currentControlWord |= CTL_START_STOP;
                }

                // 펌프가 대기 모드인 경우 대기 비트 추가
                if (_currentStatus.CurrentSpeed > 0 &&
                    _parameterCache.ContainsKey(PARAM_STANDBY_FREQUENCY) &&
                    _currentStatus.CurrentSpeed <= _parameterCache[PARAM_STANDBY_FREQUENCY] + 10)
                {
                    _currentControlWord |= CTL_STANDBY;
                }

                // 초기 제어 워드 전송
                SendControlWord(_currentControlWord);
            }
        }

        /// <summary>
        /// 장치 상태를 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true, 그렇지 않으면 false</returns>
        public override bool CheckStatus()
        {
            EnsureConnected();

            try
            {
                // 시스템 상태 확인 (PZD1 읽기)
                ushort statusWord = 0;
                if (ReadParameter(0, out statusWord, true)) // 0은 파라미터 번호 없이 현재 상태만 읽기
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
        /// 펌프 정보 읽기 (모델, 시리얼 번호 등)
        /// </summary>
        private void ReadPumpInfo()
        {
            try
            {
                // 실행 시간 읽기
                uint runningHours = 0;
                if (ReadParameter32Bit(60, out runningHours)) // Parameter 60: 마지막 서비스 이후 동작 시간
                {
                    _currentStatus.RunningTimeHours = runningHours / 100; // 0.01시간 단위에서 변환
                }

                // 펌프 카탈로그 번호 읽기 - 생략 (필요시 구현)
                // 펌프 시리얼 번호 읽기 - 생략 (필요시 구현)
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 정보 읽기 실패: {ex.Message}");
            }
        }

        #endregion

        #region 펌프 제어 메서드

  
        /// <summary>
        /// 펌프 오류를 리셋합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool ResetError()
        {
            EnsureConnected();

            try
            {
                // 주의: 오류 리셋은 START가 비활성 상태일 때만 가능
                ushort controlWord = CTL_ENABLE_REMOTE; // 리모트 제어 활성화
                controlWord |= CTL_ERROR_RESET;         // 오류 리셋 비트 활성화

                // START 비트는 비활성화 상태에서 리셋해야 함
                bool result = SendControlWord(controlWord);
                if (result)
                {
                    // RESET 비트는 일시적으로만 활성화하고 다시 해제
                    Thread.Sleep(200);

                    // 이전 제어 워드에서 RESET 비트만 제거
                    _currentControlWord &= ~CTL_ERROR_RESET;
                    _currentControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지

                    SendControlWord(_currentControlWord);

                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 오류 리셋 명령 성공", DeviceStatusCode.Ready));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 오류 리셋 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 벤트합니다. (공기 주입)
        /// 주의: 펌프가 정지된 상태에서만 벤트해야 합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Vent()
        {
            EnsureConnected();

            try
            {
                // 펌프가 여전히 회전하고 있는지 확인
                UpdateStatus();
                if (_currentStatus.IsRunning)
                {
                    OnErrorOccurred("터보 펌프가 회전 중일 때는 벤트할 수 없습니다. 먼저 펌프를 정지하세요.");
                    return false;
                }

                // 제어 워드 설정: VENTING 비트 활성화 + REMOTE 활성화
                _currentControlWord |= CTL_VENTING;
                _currentControlWord |= CTL_ENABLE_REMOTE;
                // 시작 비트는 비활성화 상태이어야 함
                _currentControlWord &= ~CTL_START_STOP;

                bool result = SendControlWord(_currentControlWord);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    _currentStatus.IsVented = true; // 명시적으로 벤트 상태 설정
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 벤트 명령 성공", DeviceStatusCode.Idle));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 벤트 실패: {ex.Message}");
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
                // 제어 워드 설정: VENTING 비트 비활성화
                _currentControlWord &= ~CTL_VENTING;
                _currentControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지

                bool result = SendControlWord(_currentControlWord);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    _currentStatus.IsVented = false; // 명시적으로 벤트 상태 해제
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 벤트 밸브 닫기 성공", DeviceStatusCode.Ready));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 벤트 밸브 닫기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 통신 와치독을 비활성화합니다.
        /// </summary>
        /// <returns>설정 성공 여부</returns>
        public bool DisableWatchdog()
        {
            EnsureConnected();

            try
            {

                ushort temp = 0;

                bool temp1 = ReadParameter(182, out temp);
                temp1 = ReadParameter(181, out temp);

                // RS232/485 통신 와치독 비활성화 (파라미터 182)
                bool rs232Result = WriteParameter(182, 0); // 0 = 감시 기능 없음

                // Profibus 통신 와치독 비활성화 (파라미터 181)
                bool profibusResult = WriteParameter(181, 0); // 0 = 감시 기능 없음

                // 변경사항 영구 저장
                if (rs232Result && profibusResult)
                {
                    SaveParameters();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "통신 와치독 기능 비활성화 성공", DeviceStatusCode.Ready));
                }

                temp = 0;

                temp1 = ReadParameter(182, out temp);
                temp1 = ReadParameter(181, out temp);


                return rs232Result && profibusResult;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"통신 와치독 비활성화 실패: {ex.Message}");
                return false;
            }
        }



        /// <summary>
        /// 펌프를 대기 모드로 전환합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool SetStandbyMode()
        {
            EnsureConnected();

            try
            {
                // 펌프가 실행 중인지 확인
                UpdateStatus();
                if (!_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 실행 중이 아닐 때는 대기 모드로 전환할 수 없습니다.");
                    return false;
                }

                // 대기 모드 활성화 - STANDBY 비트 설정
                _currentControlWord |= CTL_STANDBY;
                _currentControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지
                _currentControlWord |= CTL_START_STOP;    // 시작 상태 유지

                bool result = SendControlWord(_currentControlWord);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 대기 모드 활성화 성공", DeviceStatusCode.Standby));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 대기 모드 전환 실패: {ex.Message}");
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
                // 펌프가 실행 중인지 확인
                UpdateStatus();
                if (!_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 실행 중이 아닐 때는 정상 모드로 전환할 수 없습니다.");
                    return false;
                }

                // 대기 모드 비활성화 - STANDBY 비트 해제
                _currentControlWord &= ~CTL_STANDBY;
                _currentControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지
                _currentControlWord |= CTL_START_STOP;    // 시작 상태 유지

                bool result = SendControlWord(_currentControlWord);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 정상 모드 전환 성공", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 정상 모드 전환 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프의 회전 속도 설정값을 변경합니다.
        /// </summary>
        /// <param name="frequency">주파수 (Hz)</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetRotationSpeed(ushort frequency)
        {
            EnsureConnected();

            try
            {
                // 유효한 주파수 범위 확인 (모델에 따라 다름)
                ushort minFreq = 0;
                ushort maxFreq = 0;
                GetFrequencyRange(out minFreq, out maxFreq);

                if (frequency < minFreq || frequency > maxFreq)
                {
                    OnErrorOccurred($"유효하지 않은 주파수입니다. 유효 범위: {minFreq}-{maxFreq} Hz");
                    return false;
                }

                // 속도 설정값 파라미터 쓰기 (파라미터 24)
                if (WriteParameter(PARAM_SETPOINT_FREQUENCY, frequency))
                {
                    // 설정값 활성화 비트 설정
                    _currentControlWord |= CTL_ENABLE_SETPOINT;
                    _currentControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지
                    SendControlWord(_currentControlWord);

                    // 변경사항 영구 저장
                    SaveParameters();

                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, $"터보 펌프 속도 설정 성공: {frequency} Hz", DeviceStatusCode.Running));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 대기 모드 속도를 설정합니다.
        /// </summary>
        /// <param name="frequency">주파수 (Hz)</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetStandbySpeed(ushort frequency)
        {
            EnsureConnected();

            try
            {
                // 유효한 주파수 범위 확인
                ushort minFreq = 0;
                ushort maxFreq = 0;
                GetFrequencyRange(out minFreq, out maxFreq);

                if (frequency < minFreq || frequency > maxFreq)
                {
                    OnErrorOccurred($"유효하지 않은 대기 모드 주파수입니다. 유효 범위: {minFreq}-{maxFreq} Hz");
                    return false;
                }

                // 대기 모드 속도 파라미터 쓰기 (파라미터 150)
                if (WriteParameter(PARAM_STANDBY_FREQUENCY, frequency))
                {
                    // 변경사항 영구 저장
                    SaveParameters();

                    Thread.Sleep(100);
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, $"터보 펌프 대기 모드 속도 설정 성공: {frequency} Hz", DeviceStatusCode.Ready));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"터보 펌프 대기 모드 속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 해당 모델에 따른 유효한 주파수 범위를 반환합니다.
        /// </summary>
        /// <param name="minFrequency">최소 주파수 (출력 파라미터)</param>
        /// <param name="maxFrequency">최대 주파수 (출력 파라미터)</param>
        private void GetFrequencyRange(out ushort minFrequency, out ushort maxFrequency)
        {
            // 기본값 설정
            minFrequency = 230; // 대부분의 모델에 적용되는 최소 주파수 (standby)

            // 매뉴얼의 기술 데이터 참조하여 모델별 최대 주파수 반환
            switch (_model)
            {
                case "MAG W 1300":
                    maxFrequency = 630; // 37,800 RPM ÷ 60 = 630 Hz
                    break;
                case "MAG W 1600":
                case "MAG 1601":
                case "MAG W 1700":
                    maxFrequency = 550; // 33,000 RPM ÷ 60 = 550 Hz
                    break;
                case "MAG W 2200":
                case "MAG 2201":
                    maxFrequency = 510; // 30,600 RPM ÷ 60 = 510 Hz
                    break;
                default:
                    // 알 수 없는 모델에는 보수적인 값 사용
                    maxFrequency = 500;
                    break;
            }
        }

        #endregion

        #region 상태 모니터링 메서드

        /// <summary>
        /// 비동기적으로 펌프 상태를 업데이트합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부를 포함하는 Task</returns>
        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

        /// <summary>
        /// 펌프 상태를 업데이트합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부</returns>
        public bool UpdateStatus()
        {
            // 이미 업데이트 중이면 중복 실행 방지
            if (_isUpdatingStatus)
            {
                return false;
            }

            EnsureConnected();
            _isUpdatingStatus = true;

            try
            {
                // 현재 제어 워드를 유지하면서 상태 워드 읽기
                ushort statusWord = 0;
                if (!ReadParameterKeepingControlWord(0, out statusWord, true))
                {
                    return false;
                }

                // 상태 워드 기반으로 상태 업데이트
                UpdateStatusFromStatusWord(statusWord);

                // 현재 로터 주파수 읽기
                ushort frequency = 0;
                if (ReadParameterKeepingControlWord(PARAM_ACTUAL_FREQUENCY, out frequency))
                {
                    _currentStatus.CurrentSpeed = frequency;
                }

                // 모터 전류 읽기
                ushort current = 0;
                if (ReadParameterKeepingControlWord(PARAM_ACTUAL_CURRENT, out current))
                {
                    _currentStatus.MotorCurrent = current / 10.0; // 0.1A 단위
                }

                // 온도 정보 읽기
                ushort motorTemp = 0, converterTemp = 0, bearingTemp = 0;
                if (ReadParameterKeepingControlWord(PARAM_MOTOR_TEMP, out motorTemp))
                {
                    _currentStatus.MotorTemperature = motorTemp;
                }
                if (ReadParameterKeepingControlWord(PARAM_CONVERTER_TEMP, out converterTemp))
                {
                    _currentStatus.ElectronicsTemperature = converterTemp;
                }
                if (ReadParameterKeepingControlWord(PARAM_BEARING_TEMP, out bearingTemp))
                {
                    _currentStatus.BearingTemperature = bearingTemp;
                }

                // 오류 코드 읽기
                ushort errorCode = 0;
                if (ReadParameterKeepingControlWord(PARAM_ERROR_CODE, out errorCode))
                {
                    _currentStatus.ErrorCode = errorCode;
                }

                // 경고 비트 읽기
                ushort warningBits1 = 0;
                if (ReadParameterKeepingControlWord(PARAM_WARNING_BITS1, out warningBits1))
                {
                    _currentStatus.WarningCode = warningBits1;
                }

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
        /// 현재 제어 워드를 유지하면서 파라미터 값을 읽습니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="value">읽어온 값</param>
        /// <param name="statusOnly">상태만 읽을 경우 true (파라미터 번호 무시)</param>
        /// <returns>성공 여부</returns>
        private bool ReadParameterKeepingControlWord(ushort paramNumber, out ushort value, bool statusOnly = false)
        {
            value = 0;

            // 제어 워드가 설정되지 않은 경우 일반 읽기 사용
            if (!_isInitialized || _currentControlWord == 0)
            {
                return ReadParameter(paramNumber, out value, statusOnly);
            }

            // 파라미터 읽기를 위한 텔레그램 구성 (제어 워드 포함)
            byte[] telegram = CreateUssReadTelegramWithControlWord(paramNumber, _currentControlWord, statusOnly);

            // 전송 전 입력 버퍼 비우기
            _communicationManager.DiscardInBuffer();

            // 텔레그램 전송
            bool success = _communicationManager.Write(telegram);
            if (!success)
            {
                OnErrorOccurred("파라미터 읽기 요청 전송 실패");
                return false;
            }

            // 응답 읽기
            byte[] response = _communicationManager.ReadAll();

            // 응답 검증
            if (response == null || response.Length < 24 || response[0] != STX)
            {
                OnErrorOccurred("파라미터 읽기 응답 오류");
                return false;
            }

            if (statusOnly)
            {
                // 상태만 읽는 경우 PZD1(상태 워드) 추출
                value = (ushort)((response[11] << 8) | response[12]);
            }
            else
            {
                // 파라미터 값 추출 (PWE)
                value = (ushort)((response[9] << 8) | response[10]);
            }

            return true;
        }

        /// <summary>
        /// 제어 워드를 포함하여 USS 프로토콜로 파라미터 읽기 텔레그램을 생성합니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="controlWord">제어 워드</param>
        /// <param name="statusOnly">상태만 읽을 경우 true</param>
        /// <returns>텔레그램 바이트 배열</returns>
        private byte[] CreateUssReadTelegramWithControlWord(ushort paramNumber, int controlWord, bool statusOnly)
        {
            // 총 24바이트 텔레그램 (STX + LGE + ADR + 페이로드 20바이트 + BCC)
            byte[] telegram = new byte[24];

            // 헤더 설정
            telegram[0] = STX;       // 시작 바이트
            telegram[1] = USS_LGE;   // 페이로드 길이 + 2
            telegram[2] = USS_ADR;   // 주소 (RS-232의 경우 0)

            if (!statusOnly)
            {
                // PKE 설정 (파라미터 번호 + 액세스 타입)
                int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
                telegram[3] = (byte)(pke >> 8);
                telegram[4] = (byte)(pke & 0xFF);
            }
            else
            {
                // 상태만 읽는 경우 PKE = 0 (파라미터 없음)
                telegram[3] = 0;
                telegram[4] = 0;
            }

            // telegram[5] = 0; // 예약됨
            telegram[6] = 0; // IND 값 (인덱스)

            // PWE 필드 (4바이트): 읽기 요청의 경우 0으로 설정
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = 0;
            telegram[10] = 0;

            // PZD1 필드에 제어 워드 설정 - 현재 제어 워드 유지
            telegram[11] = (byte)(controlWord >> 8);
            telegram[12] = (byte)(controlWord & 0xFF);

            // 나머지 PZD 필드는 0으로 설정
            for (int i = 13; i < 23; i++)
            {
                telegram[i] = 0;
            }

            // 체크섬 계산 및 설정
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// 상태 워드로부터 펌프 상태를 업데이트합니다.
        /// </summary>
        /// <param name="statusWord">상태 워드</param>
        private void UpdateStatusFromStatusWord(ushort statusWord)
        {
            _currentStatus.IsRunning = (statusWord & STS_PUMP_ROTATING) != 0;
            _currentStatus.IsAccelerating = (statusWord & STS_ACCELERATION) != 0;
            _currentStatus.IsDecelerating = (statusWord & STS_DECELERATION) != 0;
            _currentStatus.IsInNormalOperation = (statusWord & STS_NORMAL_OPERATION) != 0;
            _currentStatus.HasWarning = (statusWord & STS_TEMP_WARNING) != 0;
            _currentStatus.HasError = (statusWord & STS_FAILURE) != 0;
            _currentStatus.IsReady = (statusWord & STS_READY) != 0;
            _currentStatus.IsRemoteActive = (statusWord & STS_REMOTE_ACTIVE) != 0;

            // 벤트 상태는 상태 워드에 직접 없으므로, 속도 및 기타 정보로 추정
            // 현재 회전하지 않고 있으면서 오류가 없는 경우 벤트된 것으로 추정
            // (벤트 컨트롤 상태가 전달되지 않아 정확한 상태는 알 수 없음)
            if (!_currentStatus.IsRunning && !_currentStatus.HasError &&
                !_currentStatus.IsAccelerating && !_currentStatus.IsDecelerating)
            {
                // 벤트 상태 유지, 컨트롤 명령으로 설정된 경우를 우선함
            }
        }

        /// <summary>
        /// 현재 펌프 상태에 대한 오류 코드를 분석합니다.
        /// </summary>
        /// <returns>오류 코드 설명 또는 null (오류가 없는 경우)</returns>
        public string GetErrorDescription()
        {
            if (!_currentStatus.HasError || _currentStatus.ErrorCode == 0)
            {
                return null;
            }

            // 오류 코드 해석 (MAG integra 매뉴얼 참조)
            switch (_currentStatus.ErrorCode)
            {
                case 2: return "모터 온도 과열";
                case 3: return "공급 전압 오류";
                case 4: return "변환기 온도 오류";
                case 6: return "과부하";
                case 7: return "가속 시간 초과";
                case 9: return "베어링 온도 과열";
                case 12: return "상부 자기 베어링 불균형";
                case 13: return "하부 자기 베어링 불균형";
                case 14: return "축 방향 베어링 불균형";
                case 16: return "과부하 지속 시간 오류";
                case 17: return "모터 전류 오류";
                case 19: return "시작 시간 초과";
                case 26: return "베어링 온도 센서 오류";
                case 28: return "모터 온도 센서 오류";
                case 31: return "높은 부하 지속 시간 오류";
                case 39: return "자기 베어링 시작 오류";
                case 43: return "과속";
                case 63: return "내부 파라미터 오류";
                case 65: return "펌프 통신 오류";
                case 66: return "자기 베어링 전류 과부하";
                case 67: return "내부 과부하";
                case 71: return "초기화 오류";
                case 73: return "작동 사이클 한계 초과";
                case 74: return "작동 시간 한계 초과";
                case 75: return "펌프 초기화 오류";
                case 77: return "베어링 접촉 횟수 오류";
                case 78: return "베어링 접촉 시간 오류";
                case 79: return "내부 통신 오류";
                case 80: return "인터페이스 모듈 조합 오류";
                case 81: return "RS232/RS485 통신 중단";
                case 82: return "필드버스 통신 중단";
                case 90: return "펌프 속도 조정 오류";
                case 91: return "펌프 케이블 길이 오류";
                case 92: return "외부 펌프 컨트롤러 오류";
                case 93: return "케이블 파라미터 오류";
                case 201: return "컨트롤러 하드웨어 오류";
                case 203: return "자가 테스트 중 오류";
                case 204: return "스코프 기능 RAM 부족";
                case 206: return "펌프 파라미터 오류";
                case 209: return "펌프 초기화 오류";
                case 213: return "공급 전압 너무 높음";
                default: return $"알 수 없는 오류 (코드: {_currentStatus.ErrorCode})";
            }
        }

        /// <summary>
        /// 현재 펌프 상태에 대한 경고 코드를 분석합니다.
        /// </summary>
        /// <returns>경고 코드 설명 또는 null (경고가 없는 경우)</returns>
        public string GetWarningDescription()
        {
            if (!_currentStatus.HasWarning || _currentStatus.WarningCode == 0)
            {
                return null;
            }

            // 경고 비트 해석 (MAG integra 매뉴얼 참조)
            string warnings = "";

            // 첫 번째 경고 비트 세트(Parameter 227) 파싱
            if ((_currentStatus.WarningCode & 0x0001) != 0) warnings += "펌프 모터 온도 높음, ";
            if ((_currentStatus.WarningCode & 0x0040) != 0) warnings += "과속, ";
            if ((_currentStatus.WarningCode & 0x0400) != 0) warnings += "상부 베어링 불균형, ";
            if ((_currentStatus.WarningCode & 0x0800) != 0) warnings += "하부 베어링 불균형, ";
            if ((_currentStatus.WarningCode & 0x1000) != 0) warnings += "축 방향 베어링 진동, ";

            // 마지막 쉼표와 공백 제거
            if (warnings.Length > 0)
            {
                warnings = warnings.Substring(0, warnings.Length - 2);
            }
            else
            {
                warnings = "경고 발생 (자세한 정보 없음)";
            }

            return warnings;
        }

        /// <summary>
        /// 펌프의 대략적인 운영 상태를 텍스트로 반환합니다.
        /// </summary>
        /// <returns>운영 상태 텍스트</returns>
        public string GetStatusText()
        {
            if (!IsConnected)
            {
                return "연결 안됨";
            }

            if (_currentStatus.HasError)
            {
                return "오류: " + GetErrorDescription();
            }

            if (_currentStatus.IsDecelerating)
            {
                return "감속 중";
            }

            if (!_currentStatus.IsRunning)
            {
                return _currentStatus.IsVented ? "벤트됨" : "정지됨";
            }

            if (_currentStatus.IsAccelerating)
            {
                return "가속 중";
            }

            if (_currentStatus.IsInNormalOperation)
            {
                if (_currentStatus.HasWarning)
                {
                    return "실행 중 (경고 있음: " + GetWarningDescription() + ")";
                }
                return "정상 실행 중";
            }

            return "실행 중";
        }

        /// <summary>
        /// 펌프의 현재 속도를 백분율로 반환합니다.
        /// </summary>
        /// <returns>백분율 속도 (0-100%)</returns>
        public int GetSpeedPercentage()
        {
            // 각 모델별 최대 속도
            int maxSpeed;

            switch (_model)
            {
                case "MAG W 1300":
                    maxSpeed = 37800;
                    break;
                case "MAG W 1600":
                case "MAG 1601":
                    maxSpeed = 33000;
                    break;
                case "MAG W 1700":
                    maxSpeed = 33000;
                    break;
                case "MAG W 2200":
                case "MAG 2201":
                    maxSpeed = 30600;
                    break;
                default:
                    maxSpeed = 37800; // 기본값
                    break;
            }

            // 백분율 계산 (주파수는 Hz 단위로, RPM은 Hz * 60)
            // CurrentSpeed는 Hz 단위로 제공됨
            int percentage = (int)Math.Round((_currentStatus.CurrentSpeed * 60 / (double)maxSpeed) * 100);

            // 범위 제한
            return Math.Max(0, Math.Min(100, percentage));
        }

        #endregion

        #region USS 프로토콜 통신 메서드

        /// <summary>
        /// 파라미터 값을 영구적으로 저장합니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool SaveParameters()
        {
            // 파라미터 8에 값 1을 쓰면 영구 저장 명령
            return WriteParameter(PARAM_SAVE_DATA, 1);
        }

        /// <summary>
        /// 제어 워드를 전송합니다.
        /// </summary>
        /// <param name="controlWord">제어 워드</param>
        /// <returns>성공 여부</returns>
        private bool SendControlWord(int controlWord)
        {
            lock (_controlWordLock)
            {
                // 제어 워드 전송을 위한 텔레그램 구성
                byte[] telegram = CreateUssWriteTelegram(0, controlWord);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 텔레그램 전송
                bool success = _communicationManager.Write(telegram);
                if (!success)
                {
                    OnErrorOccurred("제어 명령 전송 실패");
                    return false;
                }

                // 성공 시 현재 제어 워드 업데이트
                _currentControlWord = controlWord;
                return true;
            }
        }

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
                    // 제어 워드 설정: START + ENABLE_REMOTE
                    int newControlWord = _currentControlWord | CTL_ENABLE_REMOTE | CTL_START_STOP;

                    // 만약 이전에 설정된 다른 비트들(예: STANDBY)이 있으면 유지하고 싶은 경우
                    // 또는 특정 비트를 명시적으로 제거하고 싶은 경우 여기서 처리

                    bool result = SendControlWord(newControlWord);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 시작 명령 성공", DeviceStatusCode.Running));
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
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Stop()
        {
            EnsureConnected();

            try
            {
                lock (_controlWordLock)
                {
                    // 제어 워드 설정: 시작 비트 해제 (REMOTE는 유지)
                    int newControlWord = _currentControlWord & ~CTL_START_STOP;
                    newControlWord |= CTL_ENABLE_REMOTE; // 원격 제어는 유지

                    bool result = SendControlWord(newControlWord);
                    if (result)
                    {
                        Thread.Sleep(100);
                        UpdateStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "터보 펌프 정지 명령 성공", DeviceStatusCode.Idle));
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

        private bool SendControlWordForStart(int controlWord)
        {
            // 제어 워드 전송을 위한 텔레그램 구성
            byte[] telegram = CreateUssWriteTelegram(0, controlWord, true);

            // 전송 전 입력 버퍼 비우기
            _communicationManager.DiscardInBuffer();

            // 텔레그램 전송
            bool success = _communicationManager.Write(telegram);
            if (!success)
            {
                OnErrorOccurred("제어 명령 전송 실패");
                return false;
            }

            return true;
        }



        /// <summary>
        /// 파라미터 값을 읽습니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="value">읽어온 값</param>
        /// <param name="statusOnly">상태만 읽을 경우 true (파라미터 번호 무시)</param>
        /// <returns>성공 여부</returns>
        private bool ReadParameter(ushort paramNumber, out ushort value, bool statusOnly = false)
        {
            value = 0;

            // 파라미터 읽기를 위한 텔레그램 구성
            byte[] telegram = CreateUssReadTelegram(paramNumber, statusOnly);

            // 전송 전 입력 버퍼 비우기
            _communicationManager.DiscardInBuffer();

            // 텔레그램 전송
            bool success = _communicationManager.Write(telegram);
            if (!success)
            {
                OnErrorOccurred("파라미터 읽기 요청 전송 실패");
                return false;
            }

            // 응답 대기
            //Thread.Sleep(200);

            // 응답 읽기
            byte[] response = _communicationManager.ReadAll();

            // 응답 검증
            if (response == null || response.Length < 24 || response[0] != STX)
            {
                OnErrorOccurred("파라미터 읽기 응답 오류");
                return false;
            }

            if (statusOnly)
            {
                // 상태만 읽는 경우 PZD1(상태 워드) 추출
                value = (ushort)((response[11] << 8) | response[12]);
            }
            else
            {
                // 파라미터 값 추출 (PWE)
                value = (ushort)((response[9] << 8) | response[10]);
            }

            return true;
        }

        /// <summary>
        /// 32비트 파라미터 값을 읽습니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="value">읽어온 값</param>
        /// <returns>성공 여부</returns>
        private bool ReadParameter32Bit(ushort paramNumber, out uint value)
        {
            value = 0;

            // 32비트 파라미터 읽기를 위한 텔레그램 구성
            byte[] telegram = CreateUssRead32BitTelegram(paramNumber);

            // 전송 전 입력 버퍼 비우기
            _communicationManager.DiscardInBuffer();

            // 텔레그램 전송
            bool success = _communicationManager.Write(telegram);
            if (!success)
            {
                OnErrorOccurred("32비트 파라미터 읽기 요청 전송 실패");
                return false;
            }

            //// 응답 대기
            //Thread.Sleep(200);

            // 응답 읽기
            byte[] response = _communicationManager.ReadAll();

            // 응답 검증
            if (response == null || response.Length < 24 || response[0] != STX)
            {
                OnErrorOccurred("32비트 파라미터 읽기 응답 오류");
                return false;
            }

            // 32비트 파라미터 값 추출 (PWE)
            value = (uint)((response[7] << 24) | (response[8] << 16) | (response[9] << 8) | response[10]);

            return true;
        }

        /// <summary>
        /// 파라미터 값을 씁니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="value">쓰려는 값</param>
        /// <returns>성공 여부</returns>
        private bool WriteParameter(ushort paramNumber, ushort value)
        {
            // 파라미터 쓰기를 위한 텔레그램 구성
            byte[] telegram = CreateUssWriteParameterTelegram(paramNumber, value);

            // 전송 전 입력 버퍼 비우기
            _communicationManager.DiscardInBuffer();

            // 텔레그램 전송
            bool success = _communicationManager.Write(telegram);
            if (!success)
            {
                OnErrorOccurred("파라미터 쓰기 요청 전송 실패");
                return false;
            }

            //// 응답 대기
            //Thread.Sleep(200);

            // 응답 읽기
            byte[] response = _communicationManager.ReadAll();

            // 응답 검증
            if (response == null || response.Length < 24 || response[0] != STX)
            {
                OnErrorOccurred("파라미터 쓰기 응답 오류");
                return false;
            }

            // PKE 필드에서 응답 유형 확인 (비트 15-12)
            int pkeResponse = (response[3] << 8) | response[4];
            int responseType = pkeResponse & 0xF000;

            if (responseType == 0x7000) // 0x7000 = 명령을 실행할 수 없음
            {
                OnErrorOccurred("파라미터 쓰기 명령을 실행할 수 없음");
                return false;
            }

            if (responseType == 0x8000) // 0x8000 = 쓰기 권한 없음
            {
                OnErrorOccurred("파라미터 쓰기 권한이 없음");
                return false;
            }

            return true;
        }

        /// <summary>
        /// USS 프로토콜로 파라미터 읽기 텔레그램을 생성합니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="statusOnly">상태만 읽을 경우 true</param>
        /// <returns>텔레그램 바이트 배열</returns>
        private byte[] CreateUssReadTelegram(ushort paramNumber, bool statusOnly)
        {
            // 총 24바이트 텔레그램 (STX + LGE + ADR + 페이로드 20바이트 + BCC)
            byte[] telegram = new byte[24];

            // 헤더 설정
            telegram[0] = STX;       // 시작 바이트
            telegram[1] = USS_LGE;   // 페이로드 길이 + 2
            telegram[2] = USS_ADR;   // 주소 (RS-232의 경우 0)

            if (!statusOnly)
            {
                // PKE 설정 (파라미터 번호 + 액세스 타입)
                int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
                telegram[3] = (byte)(pke >> 8);
                telegram[4] = (byte)(pke & 0xFF);
            }
            else
            {
                // 상태만 읽는 경우 PKE = 0 (파라미터 없음)
                telegram[3] = 0;
                telegram[4] = 0;
            }

            // 다른 필드는 기본값으로 설정 (대부분 0)
            // telegram[5] = 0; // 예약됨
            telegram[6] = 0; // IND 값 (인덱스)

            // PWE 필드 (4바이트): 읽기 요청의 경우 0으로 설정
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = 0;
            telegram[10] = 0;

            // 체크섬 계산 및 설정
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// USS 프로토콜로 32비트 파라미터 읽기 텔레그램을 생성합니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <returns>텔레그램 바이트 배열</returns>
        private byte[] CreateUssRead32BitTelegram(ushort paramNumber)
        {
            // 총 24바이트 텔레그램 (STX + LGE + ADR + 페이로드 20바이트 + BCC)
            byte[] telegram = new byte[24];

            // 헤더 설정
            telegram[0] = STX;       // 시작 바이트
            telegram[1] = USS_LGE;   // 페이로드 길이 + 2
            telegram[2] = USS_ADR;   // 주소 (RS-232의 경우 0)

            // PKE 설정 (파라미터 번호 + 액세스 타입)
            int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
            telegram[3] = (byte)(pke >> 8);
            telegram[4] = (byte)(pke & 0xFF);

            // 다른 필드는 기본값으로 설정 (대부분 0)
            // telegram[5] = 0; // 예약됨
            telegram[6] = 0; // IND 값 (인덱스)

            // PWE 필드 (4바이트): 읽기 요청의 경우 0으로 설정
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = 0;
            telegram[10] = 0;

            // 체크섬 계산 및 설정
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// USS 프로토콜로 파라미터 쓰기 텔레그램을 생성합니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="value">쓰려는 값</param>
        /// <returns>텔레그램 바이트 배열</returns>
        private byte[] CreateUssWriteParameterTelegram(ushort paramNumber, ushort value)
        {
            // 총 24바이트 텔레그램 (STX + LGE + ADR + 페이로드 20바이트 + BCC)
            byte[] telegram = new byte[24];

            // 헤더 설정
            telegram[0] = STX;       // 시작 바이트
            telegram[1] = USS_LGE;   // 페이로드 길이 + 2
            telegram[2] = USS_ADR;   // 주소 (RS-232의 경우 0)

            // PKE 설정 (파라미터 번호 + 액세스 타입)
            int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
            telegram[3] = (byte)(pke >> 8);
            telegram[4] = (byte)(pke & 0xFF);

            // telegram[5] = 0; // 예약됨
            telegram[6] = 0; // IND 값 (인덱스)

            // PWE 필드에 값 설정 (2바이트 값을 상위 바이트에 설정)
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = (byte)(value >> 8);
            telegram[10] = (byte)(value & 0xFF);

            // 체크섬 계산 및 설정
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// USS 프로토콜로 제어 명령 텔레그램을 생성합니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호 (0 = 제어 워드만 전송)</param>
        /// <param name="controlWord">제어 워드</param>
        /// <returns>텔레그램 바이트 배열</returns>
        private byte[] CreateUssWriteTelegram(ushort paramNumber, int controlWord, bool iscontrolWord = false)
        {
            // 총 24바이트 텔레그램 (STX + LGE + ADR + 페이로드 20바이트 + BCC)
            byte[] telegram = new byte[24];

            // 헤더 설정
            telegram[0] = STX;       // 시작 바이트
            telegram[1] = iscontrolWord ? (byte)16 : USS_LGE;   // 페이로드 길이 + 2
            telegram[2] = iscontrolWord ? (byte)0x02 :USS_ADR;   // 주소 (RS-232의 경우 0)

            if (paramNumber > 0)
            {
                // PKE 설정 (파라미터 번호 + 액세스 타입)
                int pke = PKE_READ_16BIT | (paramNumber & 0x0FFF);
                telegram[3] = (byte)(pke >> 8);
                telegram[4] = (byte)(pke & 0xFF);
            }
            else
            {
                // 파라미터 없이 제어 워드만 전송
                telegram[3] = 0;
                telegram[4] = 0;
            }

            // telegram[5] = 0; // 예약됨
            telegram[6] = 0; // IND 값 (인덱스)

            // PWE 필드는 0으로 설정
            telegram[7] = 0;
            telegram[8] = 0;
            telegram[9] = 0;
            telegram[10] = 0;

            // PZD1 필드에 제어 워드 설정
            telegram[11] = (byte)(controlWord >> 8);
            telegram[12] = (byte)(controlWord & 0xFF);

            // 나머지 PZD 필드는 0으로 설정
            for (int i = 13; i < 23; i++)
            {
                telegram[i] = 0;
            }

            // 체크섬 계산 및 설정
            telegram[23] = CalculateBCC(telegram, 0, 23);

            return telegram;
        }

        /// <summary>
        /// USS 프로토콜의 BCC(Block Check Character) 체크섬을 계산합니다.
        /// </summary>
        /// <param name="data">데이터 배열</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="length">계산할 길이</param>
        /// <returns>체크섬 값</returns>
        private byte CalculateBCC(byte[] data, int offset, int length)
        {
            byte bcc = data[offset];
            for (int i = offset + 1; i < offset + length; i++)
            {
                bcc ^= data[i]; // XOR 연산
            }
            return bcc;
        }

        #endregion
    }

    /// <summary>
    /// 터보 펌프의 현재 상태 정보를 저장하는 클래스입니다.
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

        #region 속성

        /// <summary>
        /// 펌프가 실행 중인지 여부
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
        /// 펌프가 가속 중인지 여부
        /// </summary>
        public bool IsAccelerating
        {
            get => _isAccelerating;
            internal set
            {
                if (_isAccelerating != value)
                {
                    _isAccelerating = value;
                    OnPropertyChanged(nameof(IsAccelerating));
                }
            }
        }

        /// <summary>
        /// 펌프가 감속 중인지 여부
        /// </summary>
        public bool IsDecelerating
        {
            get => _isDecelerating;
            internal set
            {
                if (_isDecelerating != value)
                {
                    _isDecelerating = value;
                    OnPropertyChanged(nameof(IsDecelerating));
                }
            }
        }

        /// <summary>
        /// 펌프가 정상 작동 중인지 여부
        /// </summary>
        public bool IsInNormalOperation
        {
            get => _isInNormalOperation;
            internal set
            {
                if (_isInNormalOperation != value)
                {
                    _isInNormalOperation = value;
                    OnPropertyChanged(nameof(IsInNormalOperation));
                }
            }
        }

        /// <summary>
        /// 펌프에 경고가 있는지 여부
        /// </summary>
        public bool HasWarning
        {
            get => _hasWarning;
            internal set
            {
                if (_hasWarning != value)
                {
                    _hasWarning = value;
                    OnPropertyChanged(nameof(HasWarning));
                }
            }
        }

        /// <summary>
        /// 펌프에 오류가 있는지 여부
        /// </summary>
        public bool HasError
        {
            get => _hasError;
            internal set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    OnPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// 펌프가 벤트되었는지 여부
        /// </summary>
        public bool IsVented
        {
            get => _isVented;
            internal set
            {
                if (_isVented != value)
                {
                    _isVented = value;
                    OnPropertyChanged(nameof(IsVented));
                }
            }
        }

        /// <summary>
        /// 펌프가 준비 상태인지 여부
        /// </summary>
        public bool IsReady
        {
            get => _isReady;
            internal set
            {
                if (_isReady != value)
                {
                    _isReady = value;
                    OnPropertyChanged(nameof(IsReady));
                }
            }
        }

        /// <summary>
        /// 원격 제어가 활성화되어 있는지 여부
        /// </summary>
        public bool IsRemoteActive
        {
            get => _isRemoteActive;
            internal set
            {
                if (_isRemoteActive != value)
                {
                    _isRemoteActive = value;
                    OnPropertyChanged(nameof(IsRemoteActive));
                }
            }
        }

        /// <summary>
        /// 현재 펌프 속도 (Hz)
        /// </summary>
        public ushort CurrentSpeed
        {
            get => _currentSpeed;
            internal set
            {
                if (_currentSpeed != value)
                {
                    _currentSpeed = value;
                    OnPropertyChanged(nameof(CurrentSpeed));
                }
            }
        }

        /// <summary>
        /// 모터 전류 (A)
        /// </summary>
        public double MotorCurrent
        {
            get => _motorCurrent;
            internal set
            {
                if (_motorCurrent != value)
                {
                    _motorCurrent = value;
                    OnPropertyChanged(nameof(MotorCurrent));
                }
            }
        }

        /// <summary>
        /// 전자장치 온도 (°C)
        /// </summary>
        public ushort ElectronicsTemperature
        {
            get => _electronicsTemperature;
            internal set
            {
                if (_electronicsTemperature != value)
                {
                    _electronicsTemperature = value;
                    OnPropertyChanged(nameof(ElectronicsTemperature));
                }
            }
        }

        /// <summary>
        /// 베어링 온도 (°C)
        /// </summary>
        public ushort BearingTemperature
        {
            get => _bearingTemperature;
            internal set
            {
                if (_bearingTemperature != value)
                {
                    _bearingTemperature = value;
                    OnPropertyChanged(nameof(BearingTemperature));
                }
            }
        }

        /// <summary>
        /// 모터 온도 (°C)
        /// </summary>
        public ushort MotorTemperature
        {
            get => _motorTemperature;
            internal set
            {
                if (_motorTemperature != value)
                {
                    _motorTemperature = value;
                    OnPropertyChanged(nameof(MotorTemperature));
                }
            }
        }

        /// <summary>
        /// 오류 코드
        /// </summary>
        public ushort ErrorCode
        {
            get => _errorCode;
            internal set
            {
                if (_errorCode != value)
                {
                    _errorCode = value;
                    OnPropertyChanged(nameof(ErrorCode));
                }
            }
        }

        /// <summary>
        /// 경고 코드
        /// </summary>
        public ushort WarningCode
        {
            get => _warningCode;
            internal set
            {
                if (_warningCode != value)
                {
                    _warningCode = value;
                    OnPropertyChanged(nameof(WarningCode));
                }
            }
        }

        /// <summary>
        /// 누적 운영 시간 (시간)
        /// </summary>
        public uint RunningTimeHours
        {
            get => _runningTimeHours;
            internal set
            {
                if (_runningTimeHours != value)
                {
                    _runningTimeHours = value;
                    OnPropertyChanged(nameof(RunningTimeHours));
                }
            }
        }

        #endregion
    }
}