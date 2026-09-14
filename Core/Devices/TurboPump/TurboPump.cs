using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Utils;

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
        // Query Designator (호스트 → 펌프)
        private const int PKE_NO_ACCESS = 0x0000;     // 액세스 없음
        private const int PKE_READ_16BIT = 0x1000;    // 16비트 파라미터 값 요청
        private const int PKE_WRITE_16BIT = 0x2000;   // 16비트 파라미터 값 쓰기
        private const int PKE_WRITE_32BIT = 0x3000;   // 32비트 파라미터 값 쓰기
        private const int PKE_READ_ARRAY = 0x6000;    // 배열 값 요청
        // 주의: 0x7000/0x8000은 '쓰기' 요청 지정자가 아니라 응답 지정자
        // (명령 실행 불가/쓰기 권한 없음)이다 — 매뉴얼 p.15 참조.

        // Reply Designator (펌프 → 호스트)
        private const int PKE_REPLY_16BIT = 0x1000;   // 16비트 값 응답
        private const int PKE_REPLY_32BIT = 0x2000;   // 32비트 값 응답
        private const int PKE_REPLY_CANNOT_EXECUTE = 0x7000;  // 명령 실행 불가
        private const int PKE_REPLY_NO_WRITE_PERM = 0x8000;   // 쓰기 권한 없음

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
        private const ushort FREQ_MIN_SETPOINT = 60;           // 최소 주파수
        private const ushort FREQ_MAX_SETPOINT = 630;          // TURBOVAC 1300 정격 주파수 (실측 최대)
        private const ushort FREQ_MIN_STANDBY = 0;             // Parameter 150 최소값
        private const ushort FREQ_MAX_STANDBY = 630;           // Parameter 150 최대값
        private const ushort FREQ_DEFAULT_SETPOINT = 630;      // Parameter 24 기본값 (정격)
        private const ushort FREQ_DEFAULT_STANDBY = 250;       // Parameter 150 기본값

        #endregion

        #region 필드 및 속성

        private readonly ICommunicationManager _communicationManager;
        private int _timeout = 1000;
        private int _statusUpdateGate = 0;   // UpdateStatus 중복 진입 방지 (Interlocked)
        private int _deviceAddress = 0;
        private string _model;

        private TurboPumpStatus _currentStatus = new TurboPumpStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        private int _currentControlWord = 0;
        private ushort _currentSetpointFrequency = 0;  // PZD2로 전송할 속도 설정값
        private bool _isInitialized = false;
        private bool _hasValidStatusWord = false;    // 유효한 상태 워드를 한 번이라도 읽었는지
        private bool _controlWordInitialized = false; // 재시작 후 초기 제어 워드 결정을 마쳤는지
        private int _failureReported = 0;            // 드라이브 오류 진단을 이미 기록했는지 (Interlocked)
        private readonly object _controlWordLock = new object();

        // 통신 트랜잭션(버퍼 정리→전송→수신)을 직렬화하는 잠금.
        // 폴링 스레드와 UI 명령 스레드가 같은 포트에서 교차하면
        // 응답이 뒤섞이거나, 정지 직후 폴링이 예전 제어 워드(START=1)를
        // 다시 보내는 경합이 발생한다.
        private readonly object _commLock = new object();
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

        /// <summary>
        /// 시작 명령이 걸려 있는지 (제어 워드 Bit 0).
        /// 상태 워드의 회전 비트(Bit 11)는 정지 후 관성 회전 중에도 참이므로,
        /// '펌프를 세워야 하는가' 판단에는 이 명령 상태를 사용해야 한다.
        /// </summary>
        public bool IsStartCommanded => (_currentControlWord & CTL_START_STOP) != 0;

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

                // 상태 워드 읽기 — 재시작 시 START 유지 판정의 근거이므로
                // 첫 응답이 깨질 수 있는 포트 오픈 직후를 감안해 최대 3회 재시도
                ushort statusWord;
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (ReadStatusWord(out statusWord))
                    {
                        UpdateStatusFromStatusWord(statusWord);
                        break;
                    }
                    Thread.Sleep(150);
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
                // 이미 초기 결정이 끝났거나 사용자가 명시적 명령을 내렸다면
                // 절대 다시 결정하지 않는다 — 방금 시작한 펌프를 지연 초기화가
                // 덮어써서 정지시키는 사고를 막는다
                if (_controlWordInitialized)
                    return;

                // 유효한 상태 워드를 한 번도 읽지 못했다면 제어 워드를 보내지 않는다.
                // Bit 10(원격 제어)이 켜진 텔레그램의 Bit 0은 곧바로 명령으로 적용되므로,
                // 상태를 모르는 채 START=0을 보내면 구동 중인 펌프가 정지된다.
                // 이후 첫 성공한 상태 갱신(UpdateStatus)에서 재시도한다.
                if (!_hasValidStatusWord)
                {
                    OnErrorOccurred("초기 상태 확인 실패 — 제어 워드 전송을 보류합니다 (상태 확인 후 자동 재시도).");
                    return;
                }

                // 프로그램 재시작 시 구동 중인 펌프를 멈추지 않도록 시작 비트를 복원한다.
                // 회전 비트(Bit 11)는 정지 명령 후 관성 회전(run-out) 중에도 참이므로
                // 상태 비트만으로는 '구동 명령 상태'를 판별할 수 없다. 따라서:
                //  1순위: 마지막으로 저장된 명령 이력 (대기 모드/설정 주파수도 함께 복원)
                //  2순위(이력 없음): 회전 중이면서 감속 중이 아닐 때만 유지
                // 어떤 경우든 오류 상태거나 회전하지 않는 펌프에는 START를 보내지 않는다.
                bool startCommanded, standbyCommanded, setpointEnabled;
                ushort savedSetpoint;
                bool hasSaved = TryLoadCommandedState(out startCommanded, out standbyCommanded,
                                                     out setpointEnabled, out savedSetpoint);

                bool keepStart;
                if (_currentStatus.HasError || !_currentStatus.IsRunning)
                {
                    keepStart = false;
                }
                else if (hasSaved)
                {
                    keepStart = startCommanded;

                    // 이력은 '전속 구동(대기/설정값 없음)'인데 드라이브가 감속 중이면
                    // 앱이 꺼진 사이 외부(전면 패널 등)에서 정지시킨 것으로 판단한다.
                    // 이때 START를 재전송하면 남이 세운 펌프를 재가속시키게 된다
                    if (keepStart && _currentStatus.IsDecelerating &&
                        !standbyCommanded && !setpointEnabled)
                    {
                        keepStart = false;
                        OnErrorOccurred("저장된 구동 이력과 달리 펌프가 감속 중입니다 — " +
                            "외부 정지로 판단해 시작 명령을 복원하지 않습니다.");
                    }
                }
                else
                {
                    keepStart = !_currentStatus.IsDecelerating;
                }

                // 제어 워드는 로컬로 완성한 뒤 한 번에 게시한다 — 필드를 단계적으로
                // 고치면 그 사이 폴링 텔레그램이 START=0 중간 상태를 전송한다
                int word = CTL_ENABLE_REMOTE;
                if (keepStart)
                {
                    word |= CTL_START_STOP;
                    if (standbyCommanded)
                        word |= CTL_STANDBY;
                    if (setpointEnabled &&
                        savedSetpoint >= FREQ_MIN_SETPOINT && savedSetpoint <= FREQ_MAX_SETPOINT)
                    {
                        // 설정값을 먼저 게시한 뒤 Bit 6이 담긴 워드를 게시한다
                        _currentSetpointFrequency = savedSetpoint;
                        word |= CTL_ENABLE_SETPOINT;
                    }
                }
                _currentControlWord = word;

                // 결정은 유효한 상태에서 내렸으므로 확정한다. 전송이 실패해도
                // 이후 폴링 텔레그램이 같은 제어 워드를 계속 전달한다.
                _controlWordInitialized = true;
                SaveCommandedState();
                SendControlCommand(_currentControlWord, _currentSetpointFrequency);
            }
        }

        /// <summary>
        /// 마지막으로 '명령한' 운전 상태를 파일에 기록합니다.
        /// 재시작 시 상태 유지의 근거로 사용됩니다 — 상태 워드의 회전 비트는
        /// 관성 회전 중에도 참이라 구동 의도의 근거가 될 수 없습니다.
        /// </summary>
        private static string CommandedStateFilePath =>
            Path.Combine(PathSettings.Instance.ConfigPath, "TurboPumpCommandedState.txt");

        private void SaveCommandedState()
        {
            try
            {
                string content =
                    $"start={(((_currentControlWord & CTL_START_STOP) != 0) ? 1 : 0)}\r\n" +
                    $"standby={(((_currentControlWord & CTL_STANDBY) != 0) ? 1 : 0)}\r\n" +
                    $"setpointEnabled={(((_currentControlWord & CTL_ENABLE_SETPOINT) != 0) ? 1 : 0)}\r\n" +
                    $"setpointFrequency={_currentSetpointFrequency}\r\n" +
                    $"savedAt={DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                // 원자적 교체: 쓰기 도중 전원이 나가도 빈/반쪽 파일이 남지 않도록
                // 임시 파일에 쓴 뒤 이동한다
                string tempPath = CommandedStateFilePath + ".tmp";
                File.WriteAllText(tempPath, content);
                File.Move(tempPath, CommandedStateFilePath, overwrite: true);
            }
            catch
            {
                // 이력 저장 실패는 동작에 치명적이지 않음 (재시작 시 상태 추정으로 폴백)
            }
        }

        private bool TryLoadCommandedState(out bool startCommanded, out bool standbyCommanded,
                                           out bool setpointEnabled, out ushort setpointFrequency)
        {
            startCommanded = false;
            standbyCommanded = false;
            setpointEnabled = false;
            setpointFrequency = 0;

            try
            {
                if (!File.Exists(CommandedStateFilePath))
                    return false;

                bool sawStart = false;
                foreach (var line in File.ReadAllLines(CommandedStateFilePath))
                {
                    var parts = line.Split('=');
                    if (parts.Length != 2) continue;

                    switch (parts[0].Trim())
                    {
                        case "start": startCommanded = parts[1].Trim() == "1"; sawStart = true; break;
                        case "standby": standbyCommanded = parts[1].Trim() == "1"; break;
                        case "setpointEnabled": setpointEnabled = parts[1].Trim() == "1"; break;
                        case "setpointFrequency": ushort.TryParse(parts[1].Trim(), out setpointFrequency); break;
                    }
                }

                // 핵심 키가 없으면(빈 파일/손상) 이력 없음으로 처리해
                // 상태 기반 추정으로 폴백한다
                return sawStart;
            }
            catch
            {
                return false;
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
                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    int previousWord = _currentControlWord;
                    _currentControlWord |= CTL_ENABLE_REMOTE | CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        SaveCommandedState();
                        Thread.Sleep(100);
                        CheckStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "터보 펌프 시작", DeviceStatusCode.Running));
                    }
                    else
                    {
                        // 전송이 확인되지 않은 START를 남겨두면 이후 폴링이
                        // 승인되지 않은 시작 명령을 계속 내보내므로 원복한다
                        _currentControlWord = previousWord;
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
                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    // [수정] Bit 0(START)과 Bit 6(ENABLE_SETPOINT)을 모두 해제
                    // 정지 시 PZD2 설정값 활성화를 유지할 이유가 없으며,
                    // 이후 Start() 호출 시 Parameter 24 기반으로 깨끗하게 시작
                    _currentControlWord &= ~(CTL_START_STOP | CTL_ENABLE_SETPOINT);
                    _currentControlWord |= CTL_ENABLE_REMOTE;

                    _currentSetpointFrequency = 0;

                    // 정지 의도는 전송 성패와 무관하게 유지된다 (fail-safe):
                    // 전송이 실패해도 이후 폴링 텔레그램이 정지 명령을 계속 전달한다
                    bool result = SendControlCommand(_currentControlWord, 0);
                    SaveCommandedState();
                    if (result)
                    {
                        Thread.Sleep(100);
                        CheckStatus();
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
                    // '명령된' 시작 상태에서만 허용한다. 상태 워드의 회전 비트(Bit 11)는
                    // 정지 후 관성 회전(run-out) 중에도 참이므로, 그것을 근거로 START를
                    // 다시 켜면 사용자가 정지시킨 펌프가 재가속된다.
                    if ((_currentControlWord & CTL_START_STOP) == 0)
                    {
                        OnErrorOccurred("펌프가 시작 명령 상태가 아닙니다. 속도 설정은 펌프 시작 후에 가능합니다.");
                        return false;
                    }

                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    // (가드 통과 후에만 — 거부된 명령이 복원을 취소하면 안 됨)
                    _controlWordInitialized = true;

                    int previousWord = _currentControlWord;
                    ushort previousSetpoint = _currentSetpointFrequency;

                    // 폴링이 (제어 워드, 설정값) 쌍을 찢어 읽어도 안전하도록
                    // 설정값을 먼저 기록한 뒤 Bit 6을 켠다
                    _currentSetpointFrequency = frequencyHz;
                    _currentControlWord |= CTL_ENABLE_REMOTE | CTL_ENABLE_SETPOINT;

                    bool result = SendControlCommand(_currentControlWord, frequencyHz);
                    if (result)
                    {
                        SaveCommandedState();
                        Thread.Sleep(100);
                        CheckStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            $"속도 설정: {frequencyHz} Hz", DeviceStatusCode.Running));
                    }
                    else
                    {
                        _currentControlWord = previousWord;
                        _currentSetpointFrequency = previousSetpoint;
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
                lock (_controlWordLock)
                {
                    // 명령된 시작 상태에서만 허용 (회전 비트는 run-out에도 참이므로 부적합)
                    if ((_currentControlWord & CTL_START_STOP) == 0)
                    {
                        OnErrorOccurred("펌프가 시작 명령 상태가 아닐 때는 대기 모드로 전환할 수 없습니다.");
                        return false;
                    }

                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    int previousWord = _currentControlWord;
                    _currentControlWord |= CTL_STANDBY | CTL_ENABLE_REMOTE;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        SaveCommandedState();
                        Thread.Sleep(100);
                        CheckStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "대기 모드 활성화", DeviceStatusCode.Standby));
                    }
                    else
                    {
                        _currentControlWord = previousWord;
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
                lock (_controlWordLock)
                {
                    // 명령된 시작 상태에서만 허용 (회전 비트는 run-out에도 참이므로 부적합)
                    if ((_currentControlWord & CTL_START_STOP) == 0)
                    {
                        OnErrorOccurred("펌프가 시작 명령 상태가 아닐 때는 정상 모드로 전환할 수 없습니다.");
                        return false;
                    }

                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    int previousWord = _currentControlWord;
                    _currentControlWord &= ~CTL_STANDBY;
                    _currentControlWord |= CTL_ENABLE_REMOTE;

                    bool result = SendControlCommand(_currentControlWord, _currentSetpointFrequency);
                    if (result)
                    {
                        SaveCommandedState();
                        Thread.Sleep(100);
                        CheckStatus();
                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            "정상 모드 전환", DeviceStatusCode.Running));
                    }
                    else
                    {
                        _currentControlWord = previousWord;
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
                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    // 벤트 밸브 상태(Bit 12)는 리셋과 무관하므로 유지한다 —
                    // 제어 워드는 매 텔레그램에 적용되므로 지우면 벤트 중 밸브가 닫힌다
                    int keepBits = _currentControlWord & CTL_VENTING;

                    // START 비트 해제 상태에서 리셋 (0→1 전환 시에만 동작)
                    // 필드에도 즉시 반영해, 펄스 도중 폴링이 예전 제어 워드
                    // (START 포함 가능)를 다시 보내지 않도록 한다
                    _currentControlWord = CTL_ENABLE_REMOTE | CTL_ERROR_RESET | keepBits;
                    _currentSetpointFrequency = 0;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (!result)
                    {
                        // 전송 실패 시에도 START는 되살리지 않는다(성공 경로와 대칭):
                        // 응답만 유실됐다면 드라이브는 이미 정지+리셋 펄스를 받았을 수
                        // 있어, START를 복원하면 폴링이 펌프를 임의로 재시작하게 된다.
                        // 리셋 비트만 지워 다음 시도의 0→1 에지를 보장한다
                        _currentControlWord = CTL_ENABLE_REMOTE | keepBits;
                        SaveCommandedState();
                        return false;
                    }

                    Thread.Sleep(200);

                    // 리셋 비트 해제
                    _currentControlWord = CTL_ENABLE_REMOTE | keepBits;
                    SendControlCommand(_currentControlWord, 0);
                    SaveCommandedState();

                    Thread.Sleep(100);
                    CheckStatus();

                    // 원인이 남아 있으면 드라이브는 리셋을 무시한다 (매뉴얼 p.17 Bit 7).
                    // 예: 감속 중 발생한 컨버터 오류(코드 2xx)는 로터 정지 후에만 해제된다
                    if (_currentStatus.HasError)
                    {
                        OnErrorOccurred(
                            $"오류 리셋이 적용되지 않았습니다 (코드: {_currentStatus.ErrorCode}). " +
                            "원인이 해소되지 않은 상태입니다 — 로터가 완전히 정지한 뒤 다시 시도하세요.");
                        return false;
                    }

                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        "오류 리셋 완료", DeviceStatusCode.Ready));
                    return true;
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
                // CheckStatus: 폴링과 겹쳐도 건너뛰지 않고 최신 상태 워드를 읽는다.
                // 확인에 실패하면 오래된 상태로 판단하지 않고 벤트를 보류한다
                // (회전 중 벤트는 매뉴얼이 명시한 위험 상황)
                if (!CheckStatus())
                {
                    OnErrorOccurred("상태 확인 실패 — 벤트를 보류합니다. 통신 상태를 확인하세요.");
                    return false;
                }
                if (_currentStatus.IsRunning)
                {
                    OnErrorOccurred("펌프가 회전 중일 때는 벤트할 수 없습니다. 먼저 정지하세요.");
                    return false;
                }

                lock (_controlWordLock)
                {
                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    _currentControlWord |= CTL_VENTING | CTL_ENABLE_REMOTE;
                    _currentControlWord &= ~CTL_START_STOP;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (result)
                    {
                        SaveCommandedState();
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
                    // 명시적 명령은 재시작 시 지연 복원(SetInitialControlWord)을 대체한다
                    _controlWordInitialized = true;

                    _currentControlWord &= ~CTL_VENTING;
                    _currentControlWord |= CTL_ENABLE_REMOTE;

                    bool result = SendControlCommand(_currentControlWord, 0);
                    if (result)
                    {
                        SaveCommandedState();
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
                // 이미 0이면 다시 쓰지 않는다 — 연결할 때마다 쓰고 EEPROM에
                // 저장하면 비휘발성 메모리가 불필요하게 마모된다 (매뉴얼 p.12)
                bool rs232Written, profibusWritten;
                bool rs232Result = EnsureParameterValue(PARAM_RS232_WATCHDOG, 0, out rs232Written);
                bool profibusResult = EnsureParameterValue(PARAM_PROFIBUS_WATCHDOG, 0, out profibusWritten);

                if (rs232Result && profibusResult && (rs232Written || profibusWritten))
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
        /// 파라미터가 목표 값이 아닐 때만 씁니다.
        /// </summary>
        /// <returns>파라미터가 목표 값임을 보장하면 true</returns>
        private bool EnsureParameterValue(ushort paramNumber, ushort targetValue, out bool written)
        {
            written = false;

            ushort current;
            if (ReadParameter(paramNumber, out current) && current == targetValue)
                return true;

            written = WriteParameter(paramNumber, targetValue);
            return written;
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
        /// 매뉴얼 p.13의 응답 PZD 영역에서 주요 실시간 데이터를 추출하고,
        /// PZD에 포함되지 않는 파라미터만 별도 요청합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부</returns>
        public bool UpdateStatus()
        {
            EnsureConnected();

            if (Interlocked.CompareExchange(ref _statusUpdateGate, 1, 0) != 0) return false;

            try
            {
                // [수정] 텔레그램 1회로 PZD1~PZD6 전체를 가져옴
                // 제어 워드는 SendAndReceive가 _commLock 획득 후에 읽으므로
                // 직전에 완료된 제어 명령(정지 등)이 항상 반영된다
                byte[] response;
                if (!SendAndReceive(0, PKE_NO_ACCESS, 0, out response))
                {
                    return false;
                }

                // PZD1 (상태 워드): byte 11-12
                ushort statusWord = (ushort)((response[11] << 8) | response[12]);
                UpdateStatusFromStatusWord(statusWord);

                // 재시작 직후 초기 제어 워드 결정이 보류됐다면(초기 상태 읽기 실패)
                // 유효한 상태를 얻은 지금 다시 시도한다
                if (!_controlWordInitialized)
                    SetInitialControlWord();

                // PZD2 (실제 로터 주파수 = P3): byte 13-14
                _currentStatus.CurrentSpeed = (ushort)((response[13] << 8) | response[14]);

                // PZD3 (변환기 온도 = P11): byte 15-16
                _currentStatus.ElectronicsTemperature = (ushort)((response[15] << 8) | response[16]);

                // PZD4 (모터 전류 = P5, 0.1A 단위): byte 17-18
                _currentStatus.MotorCurrent = ((response[17] << 8) | response[18]) / 10.0;

                // PZD5(P127 펌프 온도)는 이 드라이브 파라미터 목록에 없어 항상 0이 오므로
                // 사용하지 않는다 (매뉴얼 p.20). 실측 모터 온도는 P7에서 읽고,
                // 읽기 실패 시 마지막 값을 유지한다 (0으로 덮어쓰지 않음)
                ushort motorTemp;
                if (ReadParameter(PARAM_MOTOR_TEMP, out motorTemp))
                    _currentStatus.MotorTemperature = motorTemp;

                // PZD에 포함되지 않는 파라미터만 별도 요청
                ushort temp;
                if (ReadParameter(PARAM_BEARING_TEMP, out temp))
                    _currentStatus.BearingTemperature = temp;

                // P171은 인덱스 파라미터 — index 0이 최신 오류 코드 (매뉴얼 p.21)
                ushort code;
                if (ReadParameterField(PARAM_ERROR_CODE, 0, out code))
                    _currentStatus.ErrorCode = code;
                if (ReadParameter(PARAM_WARNING_BITS1, out code))
                    _currentStatus.WarningCode = code;

                // ★ 추가: 런타임 읽기 (P184, u16, 단위: h)
                ushort hours;
                if (ReadParameter(184, out hours))
                    _currentStatus.RunningTimeHours = hours;

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
                Interlocked.Exchange(ref _statusUpdateGate, 0);
            }
        }

        /// <summary>
        /// 드라이브 FAILURE 비트(상태 워드 Bit 3)의 진입/해제를 감지합니다.
        /// 어떤 경로(폴링 UpdateStatus, CheckStatus, 초기화)로 상태 워드를 읽었든
        /// 오류 진입 시점의 진단을 정확히 한 번만 기록하기 위해 중앙에서 처리합니다.
        /// </summary>
        private void DetectFailureTransition()
        {
            if (_currentStatus.HasError)
            {
                if (Interlocked.CompareExchange(ref _failureReported, 1, 0) == 0)
                {
                    // 진단 수집은 통신 왕복 수 회(최대 수 초)가 걸릴 수 있다.
                    // 이 메서드는 명령 경로의 CheckStatus에서 _controlWordLock을 쥔 채
                    // 호출될 수 있으므로, 잠금 밖(백그라운드)에서 읽는다 —
                    // 워치독 정지 등 다른 명령을 지연시키지 않기 위함
                    Task.Run(() => ReportDriveFailure());
                }
            }
            else
            {
                Interlocked.Exchange(ref _failureReported, 0);
            }
        }

        /// <summary>
        /// 드라이브 FAILURE 비트(상태 워드 Bit 3) 감지 시 진단 정보를 수집해
        /// 오류 이벤트로 알립니다. P174(오류 시점 주파수)와 P176(오류 시점
        /// 운전 시간)은 오류 메모리(P171)와 같은 인덱스로 저장됩니다 (매뉴얼 p.21).
        /// </summary>
        private void ReportDriveFailure()
        {
            // 오류 코드는 폴링 주기와 무관하게 이 시점에 직접 읽는다
            ushort code;
            if (ReadParameterField(PARAM_ERROR_CODE, 0, out code))
                _currentStatus.ErrorCode = code;

            string description = GetErrorDescription() ?? "코드 미확인";

            string detail = "";
            ushort errorFrequency;
            if (ReadParameterField(PARAM_ERROR_FREQUENCY, 0, out errorFrequency))
                detail += $", 발생 시점 주파수: {errorFrequency} Hz";

            uint errorHours;
            if (ReadParameterField32(PARAM_ERROR_HOURS, 0, out errorHours))
                detail += $", 발생 시점 운전 시간: {errorHours * 0.01:F1} h";

            OnErrorOccurred($"터보 펌프 드라이브 오류 발생: {description} " +
                $"(현재 주파수: {_currentStatus.CurrentSpeed} Hz{detail})");
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

            _hasValidStatusWord = true;

            // 오류 진입/해제 감지 — 진단 기록은 에피소드당 한 번
            DetectFailureTransition();
        }

        private void ReadPumpInfo()
        {
            try
            {
                // P184: Converter operating hours (u16, h)
                ushort runningHours;
                if (ReadParameter(184, out runningHours))
                {
                    _currentStatus.RunningTimeHours = runningHours;
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
                case 71: return "최초 초기화 실패 (First Time Initialisation Failure)";
                case 73: return "작동 사이클 한계 초과";
                case 74: return "작동 시간 한계 초과";
                case 75: return "펌프 초기화 실패 (Pump Initialisation Failure)";
                case 77: return "베어링 접촉 횟수 오류";
                case 78: return "베어링 접촉 시간 오류";
                case 79: return "컨버터 내부 통신 오류 (Internal Communication Failure)";
                case 80: return "인터페이스 모듈 조합 오류 (Invalid Interface Module Combination)";
                case 81: return "RS232/RS485 통신 중단";
                case 82: return "필드버스 통신 중단";
                case 90: return "속도 조정 오류 (Pump Speed Adjustment Failure)";
                case 91: return "펌프 케이블 길이 감지 오류 (Pump Cable Length Failure)";
                case 92: return "외부 컨버터 케이블 미인식 (Cable Length 0 m Failure)";
                case 93: return "케이블 파라미터 오류 (Cable Parameter Faulty)";
                case 201: return "컨버터 컨트롤러 하드웨어 오류 (Controller Hardware Failure)";
                case 203: return "컨버터 셀프테스트 오류 (Failure during Selftest)";
                case 204: return "컨버터 RAM 오류 (RAM Array Insufficient)";
                case 206: return "펌프 파라미터 오류 (Pump Parameter Failure)";
                case 209: return "펌프 초기화 오류 (Pump Initialisation Failure)";
                case 213: return "공급 전압 과전압 (Supply Voltage Too High)";
                default:
                    // 200번대는 문서화되지 않은 코드라도 컨버터 내부 오류 계열이다.
                    // 매뉴얼 p.29: 정지 대기 → 전원 재투입, 지속 시 Leybold 문의
                    if (_currentStatus.ErrorCode >= 200 && _currentStatus.ErrorCode < 300)
                        return $"컨버터 내부 오류 (코드: {_currentStatus.ErrorCode}) — " +
                               "로터 정지 후 전원 재투입 필요, 지속 시 Leybold 문의";
                    return $"알 수 없는 오류 (코드: {_currentStatus.ErrorCode})";
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
        /// 텔레그램을 전송하고 응답을 수신하는 공통 메서드입니다.
        /// 모든 USS 통신은 이 메서드를 통해 수행됩니다.
        /// </summary>
        /// <param name="paramNumber">파라미터 번호</param>
        /// <param name="pke">PKE 값 (액세스 타입 + 파라미터 번호)</param>
        /// <param name="paramValue">파라미터 값 (PWE)</param>
        /// <param name="controlWord">제어 워드 (PZD1 STW)</param>
        /// <param name="setpointFrequency">속도 설정값 (PZD2 HSW)</param>
        /// <param name="response">수신된 응답 바이트 배열</param>
        /// <returns>통신 및 검증 성공 여부</returns>
        /// <summary>
        /// 현재 제어 워드를 실어 보내는 트랜잭션 (상태 폴링/파라미터 액세스용).
        /// 제어 워드와 설정값은 _commLock 획득 '후'에 읽으므로, 직전에 완료된
        /// 제어 명령(정지 등)이 뒤늦게 예전 제어 워드로 덮어써지지 않습니다.
        /// </summary>
        private bool SendAndReceive(ushort paramNumber, int pke, ushort paramValue,
                                     out byte[] response)
        {
            return SendAndReceive(paramNumber, pke, paramValue, (byte)0, out response);
        }

        /// <summary>
        /// 현재 제어 워드를 실어 보내는 트랜잭션 (인덱스 파라미터 액세스용).
        /// </summary>
        private bool SendAndReceive(ushort paramNumber, int pke, ushort paramValue,
                                     byte index, out byte[] response)
        {
            lock (_commLock)
            {
                return SendAndReceiveCore(paramNumber, pke, paramValue, index,
                                          _currentControlWord, _currentSetpointFrequency,
                                          out response);
            }
        }

        /// <summary>
        /// 명시적 제어 워드로 보내는 트랜잭션 (제어 명령용).
        /// </summary>
        private bool SendAndReceive(ushort paramNumber, int pke, ushort paramValue,
                                     int controlWord, ushort setpointFrequency,
                                     out byte[] response)
        {
            lock (_commLock)
            {
                return SendAndReceiveCore(paramNumber, pke, paramValue, 0,
                                          controlWord, setpointFrequency, out response);
            }
        }

        /// <summary>
        /// 텔레그램 전송~응답 수신. 반드시 _commLock을 잡은 상태에서 호출해야
        /// 버퍼 정리/전송/수신이 다른 스레드의 트랜잭션과 뒤섞이지 않습니다.
        /// </summary>
        private bool SendAndReceiveCore(ushort paramNumber, int pke, ushort paramValue,
                                        byte index, int controlWord, ushort setpointFrequency,
                                        out byte[] response)
        {
            response = null;

            byte[] telegram = CreateUssTelegram(paramNumber, pke, paramValue, index,
                                                controlWord, setpointFrequency);

            _communicationManager.DiscardInBuffer();
            if (!_communicationManager.Write(telegram))
            {
                return false;
            }

            response = _communicationManager.ReadAll();
            return ValidateResponse(response);
        }

        /// <summary>
        /// 상태 워드만 읽습니다 (PZD1 ZSW).
        /// [수정] 현재 _currentSetpointFrequency를 PZD2로 전송하여
        /// Bit 6 활성화 시 의도치 않은 0 Hz 설정을 방지합니다.
        /// </summary>
        private bool ReadStatusWord(out ushort statusWord)
        {
            statusWord = 0;

            byte[] response;
            if (!SendAndReceive(0, PKE_NO_ACCESS, 0, out response))
            {
                OnErrorOccurred("상태 읽기 요청 실패");
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
            byte[] response;
            if (!SendAndReceive(0, PKE_NO_ACCESS, 0,
                                controlWord, setpointFrequency, out response))
            {
                OnErrorOccurred("제어 명령 전송 실패");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 16비트 파라미터를 읽습니다.
        /// </summary>
        private bool ReadParameter(ushort paramNumber, out ushort value)
        {
            value = 0;

            int pke = PKE_READ_16BIT | (paramNumber & 0x07FF);

            byte[] response;
            if (!SendAndReceive(paramNumber, pke, 0, out response))
            {
                OnErrorOccurred($"파라미터 {paramNumber} 읽기 요청 실패");
                return false;
            }

            if (!ValidateParameterReply(response, paramNumber, "읽기"))
                return false;

            // PWE에서 값 추출: byte 9-10 (16비트)
            value = (ushort)((response[9] << 8) | response[10]);
            return true;
        }

        /// <summary>
        /// 인덱스(필드) 파라미터를 읽습니다.
        /// </summary>
        /// <remarks>
        /// 매뉴얼 p.15: 필드 값 요청은 액세스 타입 0110, 요소는 IND로 선택.
        /// 펌웨어가 필드 액세스를 거부하면 기존에 동작이 확인된 일반 읽기로
        /// 폴백합니다 (이 경우에도 IND는 함께 전송됨).
        /// </remarks>
        private bool ReadParameterField(ushort paramNumber, byte index, out ushort value)
        {
            value = 0;

            int pke = PKE_READ_ARRAY | (paramNumber & 0x07FF);

            byte[] response;
            if (!SendAndReceive(paramNumber, pke, 0, index, out response))
            {
                OnErrorOccurred($"파라미터 {paramNumber}[{index}] 읽기 요청 실패");
                return false;
            }

            int responseType = ((response[3] << 8) | response[4]) & 0xF000;
            if (responseType == PKE_REPLY_CANNOT_EXECUTE)
            {
                // 폴백: 일반 읽기 — 일반 읽기는 IND를 무시하고 요소 0을 돌려주므로
                // index 0 요청에만 의미가 같다. 다른 인덱스는 실패로 처리한다
                if (index != 0)
                    return false;

                pke = PKE_READ_16BIT | (paramNumber & 0x07FF);
                if (!SendAndReceive(paramNumber, pke, 0, index, out response))
                    return false;
            }

            if (!ValidateParameterReply(response, paramNumber, "읽기"))
                return false;

            // PWE에서 값 추출: byte 9-10 (16비트)
            value = (ushort)((response[9] << 8) | response[10]);
            return true;
        }

        /// <summary>
        /// 32비트 인덱스(필드) 파라미터를 읽습니다.
        /// </summary>
        private bool ReadParameterField32(ushort paramNumber, byte index, out uint value)
        {
            value = 0;

            int pke = PKE_READ_ARRAY | (paramNumber & 0x07FF);

            byte[] response;
            if (!SendAndReceive(paramNumber, pke, 0, index, out response))
                return false;

            int responseType = ((response[3] << 8) | response[4]) & 0xF000;
            if (responseType == PKE_REPLY_CANNOT_EXECUTE)
            {
                // 폴백: 일반 읽기 — index 0 요청에만 의미가 같다 (위 16비트판과 동일)
                if (index != 0)
                    return false;

                pke = PKE_READ_16BIT | (paramNumber & 0x07FF);
                if (!SendAndReceive(paramNumber, pke, 0, index, out response))
                    return false;
            }

            if (!ValidateParameterReply(response, paramNumber, "읽기"))
                return false;

            // PWE에서 32비트 값 추출: byte 7-10
            value = (uint)((response[7] << 24) | (response[8] << 16) |
                          (response[9] << 8) | response[10]);
            return true;
        }

        /// <summary>
        /// 32비트 파라미터를 읽습니다.
        /// </summary>
        private bool ReadParameter32Bit(ushort paramNumber, out uint value)
        {
            value = 0;

            int pke = PKE_READ_16BIT | (paramNumber & 0x07FF);

            byte[] response;
            if (!SendAndReceive(paramNumber, pke, 0, out response))
            {
                return false;
            }

            if (!ValidateParameterReply(response, paramNumber, "읽기"))
                return false;

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
            int pke = PKE_WRITE_16BIT | (paramNumber & 0x07FF);

            byte[] response;
            if (!SendAndReceive(paramNumber, pke, value, out response))
            {
                OnErrorOccurred($"파라미터 {paramNumber} 쓰기 요청 실패");
                return false;
            }

            return ValidateParameterReply(response, paramNumber, "쓰기");
        }

        /// <summary>
        /// 파라미터 응답의 액세스 타입과 파라미터 번호를 검증합니다.
        /// 다른 요청의 응답이 뒤섞여 도착한 경우를 걸러냅니다.
        /// </summary>
        private bool ValidateParameterReply(byte[] response, ushort paramNumber, string operation)
        {
            int pkeResponse = (response[3] << 8) | response[4];
            int responseType = pkeResponse & 0xF000;

            if (responseType == PKE_REPLY_CANNOT_EXECUTE)
            {
                // 실행 불가 응답의 PWE에는 사유 코드가 들어있다 (매뉴얼 p.16)
                int faultNumber = (response[9] << 8) | response[10];
                OnErrorOccurred($"파라미터 {paramNumber} {operation} 실패: 명령 실행 불가 (사유 코드 {faultNumber})");
                return false;
            }

            if (responseType == PKE_REPLY_NO_WRITE_PERM)
            {
                OnErrorOccurred($"파라미터 {paramNumber}: 쓰기 권한 없음");
                return false;
            }

            // 응답 PKE의 PNU(bit 0-10)가 요청한 파라미터인지 확인 (매뉴얼 p.15 Fig 4.1)
            int replyParamNumber = pkeResponse & 0x07FF;
            if (replyParamNumber != paramNumber)
            {
                OnErrorOccurred($"파라미터 {paramNumber} {operation} 응답 불일치 (응답 파라미터: {replyParamNumber})");
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
                                          byte index, int controlWord, ushort setpointFrequency)
        {
            byte[] telegram = new byte[24];

            // 헤더
            telegram[0] = STX;
            telegram[1] = USS_LGE;
            telegram[2] = (byte)_deviceAddress;

            // PKE (파라미터 번호 + 액세스 타입)
            telegram[3] = (byte)(pke >> 8);
            telegram[4] = (byte)(pke & 0xFF);

            // Reserved + IND (인덱스 파라미터의 요소 선택, 매뉴얼 p.15)
            telegram[5] = 0;
            telegram[6] = index;

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