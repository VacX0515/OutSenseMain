using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.TempController
{
    /// <summary>
    /// TM4 시리즈 온도 컨트롤러를 제어하는 클래스입니다.
    /// Modbus RTU 프로토콜을 사용하여, COM 포트를 통해 통신합니다.
    /// </summary>
    public class TempController : DeviceBase
    {
        #region 상수 및 열거형

        // Modbus 함수 코드
        private const byte FUNC_READ_COILS = 0x01;           // 코일 읽기
        private const byte FUNC_READ_INPUTS = 0x02;          // 입력 상태 읽기
        private const byte FUNC_READ_HOLDING_REGS = 0x03;    // 보유 레지스터 읽기
        private const byte FUNC_READ_INPUT_REGS = 0x04;      // 입력 레지스터 읽기
        private const byte FUNC_WRITE_SINGLE_COIL = 0x05;    // 단일 코일 쓰기
        private const byte FUNC_WRITE_SINGLE_REG = 0x06;     // 단일 레지스터 쓰기
        private const byte FUNC_WRITE_MULTI_REGS = 0x10;     // 다중 레지스터 쓰기

        // TM4 시리즈 주요 레지스터 주소
        private const ushort REG_CH1_PV = 0x03E8;            // 현재 측정값 (PV) - 301001
        private const ushort REG_CH1_DOT = 0x03E9;           // 소수점 위치
        private const ushort REG_CH1_UNIT = 0x03EA;          // 온도 단위 (0:°C, 1:°F)
        private const ushort REG_CH1_SV = 0x03EB;            // 설정값 (SV)
        private const ushort REG_CH1_HEATING_MV = 0x03EC;    // 가열측 조작량
        private const ushort REG_CH1_COOLING_MV = 0x03ED;    // 냉각측 조작량

        private const ushort REG_CH2_START = 0x03EE;         // CH2 파라미터 시작 주소
        private const ushort REG_CH3_START = 0x03F4;         // CH3 파라미터 시작 주소
        private const ushort REG_CH4_START = 0x03FA;         // CH4 파라미터 시작 주소

        // 보유 레지스터
        private const ushort REG_HOLD_CH1_SV = 0x0000;       // SV 설정값 - 400001
        private const ushort REG_HOLD_CH1_RUN_STOP = 0x0032; // 제어 출력 운전/정지 - 400051
        private const ushort REG_HOLD_CH1_AUTO_TUNING = 0x0064; // 오토튜닝 실행/정지 - 400101

        // 온도 센서 타입
        private enum TemperatureSensorType : ushort
        {
            K_CA_H = 0,             // K (CA).H
            K_CA_L = 1,             // K (CA).L
            J_IC_H = 2,             // J (IC).H
            J_IC_L = 3,             // J (IC).L
            E_CR_H = 4,             // E (CR).H
            E_CR_L = 5,             // E (CR).L
            T_CC_H = 6,             // T (CC).H
            T_CC_L = 7              // T (CC).L
            // 기타 타입은 추가 가능
        }

        #endregion

        #region 필드 및 속성

        private int _deviceAddress = 1;            // 장치 국번 (기본값: 1)
        private readonly int _numChannels = 2;     // 채널 수 (TM4용) - 2개만 씀
        private int _timeout = 500;                // 통신 타임아웃(ms)
        private bool _isUpdatingStatus = false;    // 상태 업데이트 진행 중 여부
        private readonly object _commandLock = new object(); // 명령 동기화를 위한 락 객체

        private const int _maxTemp = 1500;

        private TemperatureControllerStatus _status = new TemperatureControllerStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        /// <summary>
        /// 장치 주소 (국번)
        /// </summary>
        public int DeviceAddress
        {
            get => _deviceAddress;
            set
            {
                if (value >= 1 && value <= 31) // 주소 범위 1-31
                {
                    _deviceAddress = value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(value), "장치 주소는 1에서 31 사이여야 합니다.");
                }
            }
        }

        /// <summary>
        /// 통신 타임아웃(ms) - 기본값: 500ms
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
        /// 온도 컨트롤러 상태 정보
        /// </summary>
        public TemperatureControllerStatus Status => _status;

        /// <summary>
        /// 채널 수
        /// </summary>
        public int ChannelCount => _numChannels;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "TM4 Temperature Controller";

        /// <summary>
        /// 장치 모델
        /// </summary>
        public override string Model => "TM4 Series";

        #endregion

        #region 생성자

        /// <summary>
        /// TempController 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="deviceAddress">장치 주소(국번) (기본값: 1)</param>
        public TempController(ICommunicationManager communicationManager, int deviceAddress = 1)
            : base(communicationManager)
        {
            DeviceAddress = deviceAddress;
            DeviceId = $"TM4-{deviceAddress:D2}";

            // 상태 객체 초기화
            InitializeStatus();
        }

        /// <summary>
        /// TempController 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="portName">COM 포트 이름</param>
        /// <param name="deviceAddress">장치 주소(국번) (기본값: 1)</param>
        public TempController(string portName, int deviceAddress = 1)
            : this(new DevicePortAdapter(portName, MultiPortSerialManager.Instance), deviceAddress)
        {
        }

        /// <summary>
        /// 상태 객체를 초기화합니다.
        /// </summary>
        private void InitializeStatus()
        {
            _status.ChannelCount = _numChannels;

            // 각 채널별 상태 초기화
            for (int i = 0; i < _numChannels; i++)
            {
                _status.ChannelStatus[i] = new ChannelStatus
                {
                    ChannelNumber = i + 1,
                    IsRunning = false,
                    PresentValue = 0,
                    SetValue = 0,
                    Dot = 0,
                    TemperatureUnit = "°C",
                    HeatingMV = 0,
                    CoolingMV = 0
                };
            }
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
                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 초기화 중...", DeviceStatusCode.Initializing));

                // 입출력 버퍼 비우기
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();

                // 초기 통신 딜레이
                Thread.Sleep(100);

                // 장치 정보 읽기
                ReadDeviceInfo();

                // 초기 상태 확인
                bool statusCheck = UpdateStatus();

                if (statusCheck)
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 초기화 성공", DeviceStatusCode.Ready));
                }
                else
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 상태 확인 실패, 다시 시도하세요", DeviceStatusCode.Warning));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"온도 컨트롤러 초기화 실패: {ex.Message}");
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
                // 장치 상태 업데이트
                return UpdateStatus();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"상태 확인 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 장치의 기본 정보를 읽습니다.
        /// </summary>
        private void ReadDeviceInfo()
        {
            try
            {
                // 제품 번호 및 버전 정보 읽기
                ushort[] registers = ReadInputRegisters(0x0064, 4); // 300101 ~ 300104
                if (registers != null && registers.Length >= 4)
                {
                    _status.ProductNumberH = registers[0];
                    _status.ProductNumberL = registers[1];
                    _status.HardwareVersion = registers[2];
                    _status.SoftwareVersion = registers[3];

                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 정보 읽기 성공", DeviceStatusCode.Ready));
                }

                // 모델명 읽기
                registers = ReadInputRegisters(0x0068, 6); // 300105 ~ 300110
                if (registers != null && registers.Length >= 6)
                {
                    char[] modelChars = new char[12]; // 6개 레지스터에 각 2글자씩 (ASCII)
                    for (int i = 0; i < 6; i++)
                    {
                        modelChars[i * 2] = (char)((registers[i] >> 8) & 0xFF);
                        modelChars[i * 2 + 1] = (char)(registers[i] & 0xFF);
                    }

                    string modelName = new string(modelChars).TrimEnd('\0', ' ');
                    _status.ModelName = modelName;
                }

                // 통신 설정 정보 읽기
                registers = ReadHoldingRegisters(0x012C, 6); // 400301 ~ 400306
                if (registers != null && registers.Length >= 6)
                {
                    _status.BaudRate = registers[0];
                    _status.ParityBit = registers[1];
                    _status.StopBit = registers[2];
                    _status.ResponseWaitingTime = registers[3];
                    _status.CommunicationWrite = registers[4];
                    _status.CommunicationProtocol = registers[6];
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"장치 정보 읽기 실패: {ex.Message}");
            }
        }

        #endregion

        #region 상태 업데이트 메서드

        /// <summary>
        /// 장치 상태를 업데이트합니다.
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
                // 각 채널별 현재 값 및 상태 읽기
                for (int ch = 0; ch < _numChannels; ch++)
                {
                    UpdateChannelStatus(ch + 1);
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
        /// 비동기로 장치 상태를 업데이트합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부를 포함하는 태스크</returns>
        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

        /// <summary>
        /// 지정된 채널의 상태를 업데이트합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <returns>상태 업데이트 성공 여부</returns>
        private bool UpdateChannelStatus(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            try
            {
                // 채널별 레지스터 주소 계산
                ushort baseAddress = (ushort)(REG_CH1_PV + (channelNumber - 1) * 6);

                // 채널 값 읽기 (PV, Dot, Unit, SV, Heating MV, Cooling MV)
                ushort[] registers = ReadInputRegisters(baseAddress, 6);

                if (registers != null && registers.Length >= 6)
                {
                    int index = channelNumber - 1;

                    // PV 값 처리 (에러 코드 확인)
                    short pvValue = (short)registers[0];
                    if (pvValue == 31000)
                    {
                        _status.ChannelStatus[index].SensorError = "OPEN";
                        _status.ChannelStatus[index].PresentValue = 0;
                    }
                    else if (pvValue == 30000)
                    {
                        _status.ChannelStatus[index].SensorError = "HHHH";
                        _status.ChannelStatus[index].PresentValue = 0;
                    }
                    else if (pvValue == -30000)
                    {
                        _status.ChannelStatus[index].SensorError = "LLLL";
                        _status.ChannelStatus[index].PresentValue = 0;
                    }
                    else
                    {
                        _status.ChannelStatus[index].SensorError = null;
                        _status.ChannelStatus[index].PresentValue = pvValue;
                    }

                    // 소수점 및 단위 설정
                    _status.ChannelStatus[index].Dot = registers[1];
                    _status.ChannelStatus[index].TemperatureUnit = registers[2] == 0 ? "°C" : "°F";

                    // 설정값 및 제어량
                    _status.ChannelStatus[index].SetValue = (short)registers[3];
                    _status.ChannelStatus[index].HeatingMV = registers[4] / 10.0f; // 0.1% 단위
                    _status.ChannelStatus[index].CoolingMV = registers[5] / 10.0f; // 0.1% 단위

                    // RUN/STOP 상태 확인
                    bool isRunning = ReadCoil((ushort)(channelNumber - 1)) == false; // 0: RUN, 1: STOP
                    _status.ChannelStatus[index].IsRunning = isRunning;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 상태 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 제어 메서드

        /// <summary>
        /// 지정된 채널의 온도 설정값을 변경합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <param name="setValue">설정 온도 값</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetTemperature(int channelNumber, short setValue)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            if(setValue > _maxTemp)
            {
                MessageBox.Show($"설정 온도는 {_maxTemp/10} 이하여야 합니다.", "Interlock", MessageBoxButtons.OK);
                throw new ArgumentOutOfRangeException(nameof(setValue), $"설정 온도는 {_maxTemp} 이하여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // 채널별 레지스터 주소 계산 (400001, 401001, 402001, 403001)
                ushort registerAddress = (ushort)((channelNumber - 1) * 0x03E8);

                // 설정 값 쓰기
                bool result = WriteSingleRegister(registerAddress, (ushort)setValue);

                if (result)
                {
                    _status.ChannelStatus[channelNumber - 1].SetValue = setValue;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} 온도 설정 성공: {setValue}{_status.ChannelStatus[channelNumber - 1].TemperatureUnit}",
                        DeviceStatusCode.Ready));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 온도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널을 시작합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <returns>명령 성공 여부</returns>
        public bool Start(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // RUN/STOP 코일 주소 계산 (0: RUN, 1: STOP)
                ushort coilAddress = (ushort)(channelNumber - 1);

                // RUN 상태 설정 (OFF 값으로 쓰기)
                bool result = WriteSingleCoil(coilAddress, false);

                if (result)
                {
                    _status.ChannelStatus[channelNumber - 1].IsRunning = true;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} 시작 성공",
                        DeviceStatusCode.Running));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널을 정지합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <returns>명령 성공 여부</returns>
        public bool Stop(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // RUN/STOP 코일 주소 계산 (0: RUN, 1: STOP)
                ushort coilAddress = (ushort)(channelNumber - 1);

                // STOP 상태 설정 (ON 값으로 쓰기)
                bool result = WriteSingleCoil(coilAddress, true);

                if (result)
                {
                    _status.ChannelStatus[channelNumber - 1].IsRunning = false;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} 정지 성공",
                        DeviceStatusCode.Idle));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 정지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 오토튜닝을 시작합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <returns>명령 성공 여부</returns>
        public bool StartAutoTuning(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // 오토튜닝 코일 주소 계산
                ushort coilAddress = (ushort)(channelNumber + 1);

                // 오토튜닝 시작 (ON 값으로 쓰기)
                bool result = WriteSingleCoil(coilAddress, true);

                if (result)
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} 오토튜닝 시작 성공",
                        DeviceStatusCode.Running));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 오토튜닝 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 오토튜닝을 정지합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <returns>명령 성공 여부</returns>
        public bool StopAutoTuning(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // 오토튜닝 코일 주소 계산
                ushort coilAddress = (ushort)(channelNumber + 1);

                // 오토튜닝 정지 (OFF 값으로 쓰기)
                bool result = WriteSingleCoil(coilAddress, false);

                if (result)
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} 오토튜닝 정지 성공",
                        DeviceStatusCode.Idle));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} 오토튜닝 정지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 PID 파라미터를 설정합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <param name="heatingP">가열측 비례대</param>
        /// <param name="heatingI">가열측 적분 시간</param>
        /// <param name="heatingD">가열측 미분 시간</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetPIDParameters(int channelNumber, float heatingP, int heatingI, int heatingD)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");
            }

            EnsureConnected();

            try
            {
                // PID 파라미터 레지스터 주소 계산
                // 400102, 400104, 400106 (CH1) / 401102, 401104, 401106 (CH2) / ...
                ushort baseAddress = (ushort)(0x0065 + (channelNumber - 1) * 0x03E8);

                // 비례대 값 변환 (0.1 단위로 저장)
                ushort heatingPValue = (ushort)(heatingP * 10);

                // 파라미터 쓰기
                bool result1 = WriteSingleRegister(baseAddress, heatingPValue); // 비례대
                bool result2 = WriteSingleRegister((ushort)(baseAddress + 2), (ushort)heatingI); // 적분 시간
                bool result3 = WriteSingleRegister((ushort)(baseAddress + 4), (ushort)heatingD); // 미분 시간

                bool result = result1 && result2 && result3;

                if (result)
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"채널 {channelNumber} PID 파라미터 설정 성공",
                        DeviceStatusCode.Ready));
                }

                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} PID 파라미터 설정 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Modbus RTU 통신 메서드

        /// <summary>
        /// 코일 상태를 읽습니다.
        /// </summary>
        /// <param name="address">코일 주소</param>
        /// <returns>코일 상태 (true: ON, false: OFF)</returns>
        private bool ReadCoil(ushort address)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuRequest(FUNC_READ_COILS, address, 1);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    throw new IOException("코일 읽기 요청 전송 실패");
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < 6 || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_COILS)
                {
                    throw new IOException("코일 읽기 응답 오류");
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new IOException($"Modbus 예외 응답: {response[2]}");
                }

                // 코일 상태 반환 (비트 확인)
                return (response[3] & 0x01) == 0x01;
            }
        }

        /// <summary>
        /// 입력 상태를 읽습니다.
        /// </summary>
        /// <param name="address">입력 주소</param>
        /// <returns>입력 상태 (true: ON, false: OFF)</returns>
        private bool ReadInput(ushort address)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuRequest(FUNC_READ_INPUTS, address, 1);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    throw new IOException("입력 읽기 요청 전송 실패");
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < 6 || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_INPUTS)
                {
                    throw new IOException("입력 읽기 응답 오류");
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new IOException($"Modbus 예외 응답: {response[2]}");
                }

                // 입력 상태 반환 (비트 확인)
                return (response[3] & 0x01) == 0x01;
            }
        }

        /// <summary>
        /// 보유 레지스터를 읽습니다.
        /// </summary>
        /// <param name="address">레지스터 시작 주소</param>
        /// <param name="count">레지스터 개수</param>
        /// <returns>읽은 레지스터 값 배열</returns>
        private ushort[] ReadHoldingRegisters(ushort address, ushort count)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuRequest(FUNC_READ_HOLDING_REGS, address, count);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    throw new IOException("보유 레지스터 읽기 요청 전송 실패");
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < (5 + count * 2) || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_HOLDING_REGS)
                {
                    throw new IOException("보유 레지스터 읽기 응답 오류");
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new IOException($"Modbus 예외 응답: {response[2]}");
                }

                // 바이트 수 확인
                int byteCount = response[2];
                if (byteCount != count * 2)
                {
                    throw new IOException("보유 레지스터 읽기 응답 길이 불일치");
                }

                // 레지스터 값 추출
                ushort[] registers = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    registers[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
                }

                return registers;
            }
        }

        /// <summary>
        /// 입력 레지스터를 읽습니다.
        /// </summary>
        /// <param name="address">레지스터 시작 주소</param>
        /// <param name="count">레지스터 개수</param>
        /// <returns>읽은 레지스터 값 배열</returns>
        private ushort[] ReadInputRegisters(ushort address, ushort count)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuRequest(FUNC_READ_INPUT_REGS, address, count);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    throw new IOException("입력 레지스터 읽기 요청 전송 실패");
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < (5 + count * 2) || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_INPUT_REGS)
                {
                    throw new IOException("입력 레지스터 읽기 응답 오류");
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    throw new IOException($"Modbus 예외 응답: {response[2]}");
                }

                // 바이트 수 확인
                int byteCount = response[2];
                if (byteCount != count * 2)
                {
                    throw new IOException("입력 레지스터 읽기 응답 길이 불일치");
                }

                // 레지스터 값 추출
                ushort[] registers = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    registers[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
                }

                return registers;
            }
        }

        /// <summary>
        /// 단일 코일을 씁니다.
        /// </summary>
        /// <param name="address">코일 주소</param>
        /// <param name="value">코일 값 (true: ON, false: OFF)</param>
        /// <returns>명령 성공 여부</returns>
        private bool WriteSingleCoil(ushort address, bool value)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                ushort coilValue = value ? (ushort)0xFF00 : (ushort)0x0000;
                byte[] request = CreateModbusRtuWriteSingleCoilRequest(address, coilValue);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    return false;
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_SINGLE_COIL)
                {
                    return false;
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    return false;
                }

                // 주소 확인
                ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                return respAddress == address;
            }
        }

        /// <summary>
        /// 단일 레지스터를 씁니다.
        /// </summary>
        /// <param name="address">레지스터 주소</param>
        /// <param name="value">레지스터 값</param>
        /// <returns>명령 성공 여부</returns>
        private bool WriteSingleRegister(ushort address, ushort value)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuWriteSingleRegisterRequest(address, value);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    return false;
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_SINGLE_REG)
                {
                    return false;
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    return false;
                }

                // 주소 확인
                ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                return respAddress == address;
            }
        }

        /// <summary>
        /// 다중 레지스터를 씁니다.
        /// </summary>
        /// <param name="address">시작 레지스터 주소</param>
        /// <param name="values">레지스터 값 배열</param>
        /// <returns>명령 성공 여부</returns>
        private bool WriteMultipleRegisters(ushort address, ushort[] values)
        {
            lock (_commandLock)
            {
                // Modbus RTU 프레임 생성
                byte[] request = CreateModbusRtuWriteMultipleRegistersRequest(address, values);

                // 전송 전 입력 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 전송
                bool success = _communicationManager.Write(request);
                if (!success)
                {
                    return false;
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 유효성 검사
                if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_MULTI_REGS)
                {
                    return false;
                }

                // 예외 응답 확인
                if ((response[1] & 0x80) == 0x80)
                {
                    return false;
                }

                // 주소 확인
                ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                // 레지스터 수 확인
                ushort respCount = (ushort)((response[4] << 8) | response[5]);

                return respAddress == address && respCount == values.Length;
            }
        }

        /// <summary>
        /// Modbus RTU 요청 프레임을 생성합니다.
        /// </summary>
        /// <param name="functionCode">기능 코드</param>
        /// <param name="address">시작 주소</param>
        /// <param name="count">레지스터/코일 개수</param>
        /// <returns>요청 프레임 바이트 배열</returns>
        private byte[] CreateModbusRtuRequest(byte functionCode, ushort address, ushort count)
        {
            byte[] frame = new byte[8];

            // 장치 주소
            frame[0] = (byte)_deviceAddress;

            // 기능 코드
            frame[1] = functionCode;

            // 시작 주소 (상위 바이트, 하위 바이트)
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);

            // 개수 (상위 바이트, 하위 바이트)
            frame[4] = (byte)(count >> 8);
            frame[5] = (byte)(count & 0xFF);

            // CRC 계산 및 추가
            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);         // 하위 바이트
            frame[7] = (byte)(crc >> 8);           // 상위 바이트

            return frame;
        }

        /// <summary>
        /// Modbus RTU 단일 코일 쓰기 요청 프레임을 생성합니다.
        /// </summary>
        /// <param name="address">코일 주소</param>
        /// <param name="value">코일 값 (0xFF00: ON, 0x0000: OFF)</param>
        /// <returns>요청 프레임 바이트 배열</returns>
        private byte[] CreateModbusRtuWriteSingleCoilRequest(ushort address, ushort value)
        {
            byte[] frame = new byte[8];

            // 장치 주소
            frame[0] = (byte)_deviceAddress;

            // 기능 코드
            frame[1] = FUNC_WRITE_SINGLE_COIL;

            // 코일 주소 (상위 바이트, 하위 바이트)
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);

            // 값 (0xFF00: ON, 0x0000: OFF)
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            // CRC 계산 및 추가
            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);         // 하위 바이트
            frame[7] = (byte)(crc >> 8);           // 상위 바이트

            return frame;
        }

        /// <summary>
        /// Modbus RTU 단일 레지스터 쓰기 요청 프레임을 생성합니다.
        /// </summary>
        /// <param name="address">레지스터 주소</param>
        /// <param name="value">레지스터 값</param>
        /// <returns>요청 프레임 바이트 배열</returns>
        private byte[] CreateModbusRtuWriteSingleRegisterRequest(ushort address, ushort value)
        {
            byte[] frame = new byte[8];

            // 장치 주소
            frame[0] = (byte)_deviceAddress;

            // 기능 코드
            frame[1] = FUNC_WRITE_SINGLE_REG;

            // 레지스터 주소 (상위 바이트, 하위 바이트)
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);

            // 값 (상위 바이트, 하위 바이트)
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            // CRC 계산 및 추가
            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);         // 하위 바이트
            frame[7] = (byte)(crc >> 8);           // 상위 바이트

            return frame;
        }

        /// <summary>
        /// Modbus RTU 다중 레지스터 쓰기 요청 프레임을 생성합니다.
        /// </summary>
        /// <param name="address">시작 레지스터 주소</param>
        /// <param name="values">레지스터 값 배열</param>
        /// <returns>요청 프레임 바이트 배열</returns>
        private byte[] CreateModbusRtuWriteMultipleRegistersRequest(ushort address, ushort[] values)
        {
            int count = values.Length;
            int byteCount = count * 2;

            byte[] frame = new byte[9 + byteCount];

            // 장치 주소
            frame[0] = (byte)_deviceAddress;

            // 기능 코드
            frame[1] = FUNC_WRITE_MULTI_REGS;

            // 시작 주소 (상위 바이트, 하위 바이트)
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);

            // 레지스터 개수 (상위 바이트, 하위 바이트)
            frame[4] = (byte)(count >> 8);
            frame[5] = (byte)(count & 0xFF);

            // 바이트 수
            frame[6] = (byte)byteCount;

            // 레지스터 값들
            for (int i = 0; i < count; i++)
            {
                frame[7 + i * 2] = (byte)(values[i] >> 8);       // 상위 바이트
                frame[8 + i * 2] = (byte)(values[i] & 0xFF);     // 하위 바이트
            }

            // CRC 계산 및 추가
            ushort crc = CalculateCRC(frame, 0, 7 + byteCount);
            frame[7 + byteCount] = (byte)(crc & 0xFF);           // 하위 바이트
            frame[8 + byteCount] = (byte)(crc >> 8);             // 상위 바이트

            return frame;
        }

        /// <summary>
        /// Modbus RTU CRC-16을 계산합니다.
        /// </summary>
        /// <param name="buffer">데이터 버퍼</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="count">바이트 수</param>
        /// <returns>CRC-16 값</returns>
        private ushort CalculateCRC(byte[] buffer, int offset, int count)
        {
            ushort crc = 0xFFFF;

            for (int i = offset; i < offset + count; i++)
            {
                crc ^= buffer[i];

                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }

            return crc;
        }

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 온도 값을 표시용 문자열로 변환합니다.
        /// </summary>
        /// <param name="value">온도 값</param>
        /// <param name="dot">소수점 위치 (0: 정수, 1: 소수점 한자리)</param>
        /// <param name="unit">온도 단위 ("°C" 또는 "°F")</param>
        /// <returns>표시용 온도 문자열</returns>
        public static string FormatTemperature(short value, int dot, string unit)
        {
            if (dot == 0)
            {
                return $"{value}{unit}";
            }
            else
            {
                return $"{value / 10.0:F1}{unit}";
            }
        }

        /// <summary>
        /// 센서 에러 메시지를 가져옵니다.
        /// </summary>
        /// <param name="errorCode">에러 코드</param>
        /// <returns>에러 메시지</returns>
        public static string GetSensorErrorMessage(string errorCode)
        {
            if (string.IsNullOrEmpty(errorCode))
                return null;

            switch (errorCode)
            {
                case "OPEN":
                    return "센서 회로 단선";
                case "HHHH":
                    return "측정값이 상한을 초과함";
                case "LLLL":
                    return "측정값이 하한 미만임";
                default:
                    return $"알 수 없는 오류: {errorCode}";
            }
        }

        /// <summary>
        /// 채널 상태 텍스트를 반환합니다.
        /// </summary>
        /// <param name="channelNumber">채널 번호</param>
        /// <returns>상태 텍스트</returns>
        public string GetChannelStatusText(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
            {
                return "유효하지 않은 채널";
            }

            var channelStatus = _status.ChannelStatus[channelNumber - 1];

            if (!string.IsNullOrEmpty(channelStatus.SensorError))
            {
                return $"센서 오류: {GetSensorErrorMessage(channelStatus.SensorError)}";
            }

            if (channelStatus.IsRunning)
            {
                // 측정값과 설정값 비교
                if (Math.Abs(channelStatus.PresentValue - channelStatus.SetValue) <= 3)
                {
                    return "안정 상태";
                }
                else if (channelStatus.PresentValue < channelStatus.SetValue)
                {
                    return "승온 중";
                }
                else
                {
                    return "냉각 중";
                }
            }
            else
            {
                return "정지";
            }
        }

        #endregion
    }
}