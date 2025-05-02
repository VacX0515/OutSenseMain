using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.DryPump
{
    /// <summary>
    /// ECODRY 25 plus, 35 plus 드라이 펌프 제어 및 모니터링을 위한 클래스입니다.
    /// DeviceBase 클래스를 상속하여 표준화된 장치 인터페이스를 제공합니다.
    /// </summary>
    public class DryPump : DeviceBase
    {
        #region 상수 및 열거형

        // Modbus 함수 코드
        private const byte FUNCTION_READ_HOLDING_REGISTERS = 0x03;
        private const byte FUNCTION_READ_INPUT_REGISTERS = 0x04;  // 추가된 함수 코드
        private const byte FUNCTION_WRITE_MULTIPLE_REGISTERS = 0x10;

        // 시스템 명령 레지스터 (홀딩 레지스터)
        private const ushort REG_SYSTEM_COMMAND = 0x0050;

        // 명령 값
        private const ushort CMD_NULL = 0x0000;
        private const ushort CMD_STOP = 0x0001;
        private const ushort CMD_START = 0x0002;
        private const ushort CMD_STANDBY = 0x0003;
        private const ushort CMD_RUN = 0x0004;

        // 상태 레지스터 (인풋 레지스터)
        private const ushort REG_SYSTEM_STATUS_1 = 0x0060;
        private const ushort REG_SYSTEM_STATUS_2 = 0x0061;
        private const ushort REG_WARNING = 0x0062;
        private const ushort REG_LATCHED_FAULT = 0x0063;

        // 모니터링 레지스터 (인풋 레지스터)
        private const ushort REG_FREQUENCY_REFERENCE = 0x0070;
        private const ushort REG_MOTOR_FREQUENCY = 0x0071;
        private const ushort REG_MOTOR_CURRENT = 0x0074;
        private const ushort REG_DC_LINK_VOLTAGE = 0x0075;
        private const ushort REG_MOTOR_POWER = 0x0076;
        private const ushort REG_MOTOR_TEMP = 0x0077;
        private const ushort REG_SINK_TEMP = 0x0078;
        private const ushort REG_CONTROLLER_TEMP = 0x0079;

        // 운영 이력 레지스터 (인풋 레지스터)
        private const ushort REG_RUN_SECONDS = 0x0080;
        private const ushort REG_POWERED_SECONDS = 0x0082;

        #endregion

        #region 필드 및 속성

        // 통신 설정
        private byte _slaveId = 0x01;
        private int _timeout = 1000;  // 통신 타임아웃(ms)
        private bool _isUpdatingStatus = false;

        // 상태 정보
        private DryPumpStatus _currentStatus = new DryPumpStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        /// <summary>
        /// 현재 펌프 상태 정보를 가져옵니다.
        /// </summary>
        public DryPumpStatus Status => _currentStatus;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "ECODRY Dry Pump";

        /// <summary>
        /// 장치 모델
        /// </summary>
        public override string Model => _model;

        /// <summary>
        /// 슬레이브 ID
        /// </summary>
        public byte SlaveId
        {
            get => _slaveId;
            set => _slaveId = value;
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
        /// 펌프 모델 (25 plus 또는 35 plus)
        /// </summary>
        private string _model;

        /// <summary>
        /// 펌프가 실행 중인지 여부
        /// </summary>
        public bool IsRunning => _currentStatus.IsRunning;

        /// <summary>
        /// 펌프가 대기 모드인지 여부
        /// </summary>
        public bool IsStandby => _currentStatus.IsStandby;

        /// <summary>
        /// 펌프에 경고가 있는지 여부
        /// </summary>
        public bool HasWarning => _currentStatus.HasWarning;

        /// <summary>
        /// 펌프에 오류가 있는지 여부
        /// </summary>
        public bool HasFault => _currentStatus.HasFault;

        #endregion

        #region 생성자

        /// <summary>
        /// DryPump 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="model">펌프 모델 (기본값: "ECODRY 25 plus")</param>
        /// <param name="slaveId">슬레이브 ID (기본값: 1)</param>
        public DryPump(ICommunicationManager communicationManager, string model = "ECODRY 25 plus", byte slaveId = 1)
            : base(communicationManager)
        {
            _model = model;
            _slaveId = slaveId;
            DeviceId = $"{model}_{slaveId}";
        }

        /// <summary>
        /// DryPump 클래스의 새 인스턴스를 초기화합니다.
        /// SerialManager 싱글톤 인스턴스를 사용합니다.
        /// </summary>
        /// <param name="model">펌프 모델 (기본값: "ECODRY 25 plus")</param>
        /// <param name="slaveId">슬레이브 ID (기본값: 1)</param>
        public DryPump(string model = "ECODRY 25 plus", byte slaveId = 1)
            : this(MultiPortSerialManager.Instance, model, slaveId)
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
                // 연결 후 초기 상태 확인
                bool statusCheck = CheckStatus();
                if (statusCheck)
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 상태 확인 성공", DeviceStatusCode.Ready));
                }
                else
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 상태 확인 실패, 다시 시도하세요", DeviceStatusCode.Warning));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 초기화 실패: {ex.Message}");
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
                // 시스템 상태 레지스터를 읽어 장치 동작 상태 확인 (인풋 레지스터 사용)
                ushort[] statusRegisters = ReadInputRegisters(REG_SYSTEM_STATUS_1, 2);

                if (statusRegisters != null && statusRegisters.Length >= 2)
                {
                    // 상태 정보 업데이트
                    UpdateStatusFromRegisters(statusRegisters);
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
                bool result = WriteSystemCommand(CMD_START);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 시작 명령 성공", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 시작 실패: {ex.Message}");
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
                bool result = WriteSystemCommand(CMD_STOP);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 정지 명령 성공", DeviceStatusCode.Idle));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 정지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 대기 모드로 전환합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool SetStandby()
        {
            EnsureConnected();

            try
            {
                bool result = WriteSystemCommand(CMD_STANDBY);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 대기 모드 명령 성공", DeviceStatusCode.Standby));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 대기 모드 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 다시 정상 모드로 전환합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool SetNormalMode()
        {
            EnsureConnected();

            try
            {
                bool result = WriteSystemCommand(CMD_RUN);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "펌프 정상 모드 명령 성공", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"펌프 정상 모드 설정 실패: {ex.Message}");
                return false;
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
                // 시스템 상태 레지스터 읽기 (인풋 레지스터 사용)
                ushort[] statusRegisters = ReadInputRegisters(REG_SYSTEM_STATUS_1, 2);
                if (statusRegisters == null || statusRegisters.Length < 2)
                {
                    return false;
                }

                // 경고 및 오류 레지스터 읽기 (인풋 레지스터 사용)
                ushort[] warningRegister = ReadInputRegisters(REG_WARNING, 1);
                ushort[] faultRegister = ReadInputRegisters(REG_LATCHED_FAULT, 1);

                // 모터 상태 레지스터 읽기 (인풋 레지스터 사용)
                ushort[] motorParams = ReadInputRegisters(REG_MOTOR_FREQUENCY, 9);

                // 운영 시간 레지스터 읽기 (인풋 레지스터 사용)
                ushort[] runTimeRegisters = ReadInputRegisters(REG_RUN_SECONDS, 4);

                // 모든 데이터가 정상적으로 읽혔는지 확인
                if (statusRegisters != null && warningRegister != null && faultRegister != null &&
                    motorParams != null && runTimeRegisters != null)
                {
                    // 상태 정보 업데이트
                    UpdateStatusFromRegisters(statusRegisters);

                    // 경고 및 오류 정보 업데이트
                    if (warningRegister.Length > 0)
                    {
                        _currentStatus.WarningRegister = warningRegister[0];
                    }

                    if (faultRegister.Length > 0)
                    {
                        _currentStatus.FaultRegister = faultRegister[0];
                    }

                    // 모터 파라미터 업데이트
                    if (motorParams.Length >= 9)
                    {
                        _currentStatus.MotorFrequency = ConvertQ15ToReal(motorParams[0], 400);  // 400Hz full scale
                        _currentStatus.MotorCurrent = ConvertQ15ToReal(motorParams[3], 25);    // 25A full scale
                        _currentStatus.DCLinkVoltage = ConvertQ15ToReal(motorParams[4], 500);  // 500V full scale
                        _currentStatus.MotorPower = ConvertQ15ToReal(motorParams[5], 1000);    // 1000W full scale
                        _currentStatus.MotorTemperature = ConvertQ15ToReal(motorParams[6], 200); // 200°C full scale
                        _currentStatus.SinkTemperature = ConvertQ15ToReal(motorParams[7], 200);  // 200°C full scale
                        _currentStatus.ControllerTemperature = ConvertQ15ToReal(motorParams[8], 200); // 200°C full scale
                    }

                    // 운영 시간 업데이트
                    if (runTimeRegisters.Length >= 4)
                    {
                        uint runSeconds = ((uint)runTimeRegisters[0] << 16) | runTimeRegisters[1];
                        uint poweredSeconds = ((uint)runTimeRegisters[2] << 16) | runTimeRegisters[3];

                        _currentStatus.RunTimeHours = runSeconds / 3600.0;
                        _currentStatus.PoweredTimeHours = poweredSeconds / 3600.0;
                    }

                    _lastStatusUpdateTime = DateTime.Now;
                    return true;
                }

                return false;
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
        /// 상태 레지스터 값으로부터 상태 정보를 업데이트합니다.
        /// </summary>
        /// <param name="statusRegisters">상태 레지스터 값 배열</param>
        private void UpdateStatusFromRegisters(ushort[] statusRegisters)
        {
            if (statusRegisters == null || statusRegisters.Length < 2)
            {
                return;
            }

            ushort status1 = statusRegisters[0];
            ushort status2 = statusRegisters[1];

            // 시스템 상태 레지스터 1 처리
            _currentStatus.IsRunning = (status1 & 0x0002) != 0;     // ACCEL/Running 비트
            _currentStatus.IsStopping = (status1 & 0x0001) != 0;    // DECEL/Stopping 비트
            _currentStatus.IsStandby = (status1 & 0x0004) != 0;     // STANDBY 비트
            _currentStatus.IsNormalSpeed = (status1 & 0x0008) != 0; // NORMAL 비트
            _currentStatus.IsAboveRampSpeed = (status1 & 0x0010) != 0; // ABOVE_RAMP_SPEED 비트
            _currentStatus.IsAboveOverloadSpeed = (status1 & 0x0020) != 0; // ABOVE_OVERL_SPEED 비트

            // 시스템 상태 레지스터 2 처리
            _currentStatus.HasWarning = (status2 & 0x0040) != 0;    // WARNING 비트
            _currentStatus.HasFault = (status2 & 0x0080) != 0;     // ALARM 비트
            _currentStatus.IsServiceDue = (status2 & 0x0008) != 0;  // SERVICE DUE 비트
        }

        /// <summary>
        /// 현재 펌프 상태에 대한 알람 코드를 분석합니다.
        /// </summary>
        /// <returns>알람 코드 설명 또는 null (알람이 없는 경우)</returns>
        public string GetAlarmDescription()
        {
            if (!_currentStatus.HasFault)
            {
                return null;
            }

            ushort faultCode = _currentStatus.FaultRegister;

            // 오류 코드 해석 (ECODRY plus 매뉴얼 참조)
            if ((faultCode & 0x8000) != 0) return "가속 타임아웃";
            if ((faultCode & 0x4000) != 0) return "과부하 타임아웃";
            if ((faultCode & 0x2000) != 0) return "직렬 제어 모드 인터록";
            if ((faultCode & 0x1000) != 0) return "자체 테스트 오류";
            if ((faultCode & 0x0800) != 0) return "파라미터 세트 없음";
            if ((faultCode & 0x0200) != 0) return "EEPROM 오류";
            if ((faultCode & 0x0100) != 0) return "PWM 트립";
            if ((faultCode & 0x0020) != 0) return "IPM 오류";
            if ((faultCode & 0x0010) != 0) return "온도 센서 오류 또는 낮은 온도";
            if ((faultCode & 0x0008) != 0) return "과열";
            if ((faultCode & 0x0004) != 0) return "과전류";
            if ((faultCode & 0x0002) != 0) return "과전압";
            if ((faultCode & 0x0001) != 0) return "과속도";

            return "알 수 없는 오류";
        }

        /// <summary>
        /// 현재 펌프 상태에 대한 경고 코드를 분석합니다.
        /// </summary>
        /// <returns>경고 코드 설명 또는 null (경고가 없는 경우)</returns>
        public string GetWarningDescription()
        {
            if (!_currentStatus.HasWarning)
            {
                return null;
            }

            ushort warningCode = _currentStatus.WarningRegister;

            // 경고 코드 해석 (ECODRY plus 매뉴얼 참조)
            if ((warningCode & 0x8000) != 0) return "자체 테스팅 경고";
            if ((warningCode & 0x4000) != 0) return "펌프 보호 레귤레이터 활성화";
            if ((warningCode & 0x1000) != 0) return "전원 실패";
            if ((warningCode & 0x0800) != 0) return "컨트롤러 고온";
            if ((warningCode & 0x0400) != 0) return "방열판 고온";
            if ((warningCode & 0x0200) != 0) return "모터 고온";
            if ((warningCode & 0x0080) != 0) return "컨트롤러 온도 레귤레이터 활성화";
            if ((warningCode & 0x0040) != 0) return "방열판 온도 레귤레이터 활성화";
            if ((warningCode & 0x0020) != 0) return "모터 온도 레귤레이터 활성화";
            if ((warningCode & 0x0010) != 0) return "낮은 전압 레귤레이터 활성화";
            if ((warningCode & 0x0008) != 0) return "낮은 보조 공급 전압";
            if ((warningCode & 0x0004) != 0) return "컨트롤러 온도 낮음";
            if ((warningCode & 0x0002) != 0) return "방열판 온도 낮음";
            if ((warningCode & 0x0001) != 0) return "모터 온도 낮음";

            return "알 수 없는 경고";
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

            if (_currentStatus.HasFault)
            {
                return "오류: " + GetAlarmDescription();
            }

            if (_currentStatus.IsStopping)
            {
                return "정지 중";
            }

            if (!_currentStatus.IsRunning)
            {
                return "정지됨";
            }

            if (_currentStatus.IsStandby)
            {
                return "대기 모드";
            }

            if (_currentStatus.IsRunning && _currentStatus.IsNormalSpeed)
            {
                if (_currentStatus.HasWarning)
                {
                    return "실행 중 (경고 있음)";
                }
                return "정상 실행 중";
            }

            if (_currentStatus.IsRunning && !_currentStatus.IsAboveRampSpeed)
            {
                return "가속 중";
            }

            return "실행 중";
        }

        #endregion

        #region MODBUS 통신 메서드

        /// <summary>
        /// 시스템 명령 레지스터에 명령을 씁니다.
        /// </summary>
        /// <param name="command">명령 코드</param>
        /// <returns>성공 여부</returns>
        private bool WriteSystemCommand(ushort command)
        {
            // 버퍼 타임
            Thread.Sleep(100);
            try
            {
                // Modbus 프레임 구성
                byte[] request = new byte[13]; // Slave ID + Func code + Addr(2) + Qty(2) + Byte count + Value(2) + CRC(2)

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (16 = Multiple Holding Registers 쓰기)
                request[1] = FUNCTION_WRITE_MULTIPLE_REGISTERS;

                // 시작 레지스터 주소
                request[2] = (byte)((REG_SYSTEM_COMMAND >> 8) & 0xFF);
                request[3] = (byte)(REG_SYSTEM_COMMAND & 0xFF);

                // 레지스터 개수
                request[4] = 0x00;
                request[5] = 0x01;

                // 바이트 수
                request[6] = 0x02;

                // 명령 값 (상위 바이트 먼저)
                request[7] = (byte)((command >> 8) & 0xFF);
                request[8] = (byte)(command & 0xFF);

                // CRC 계산 및 추가
                ushort crc = CalculateCRC(request, 9);
                request[9] = (byte)(crc & 0xFF);
                request[10] = (byte)((crc >> 8) & 0xFF);

                // 디버깅 로그
                string requestHex = BitConverter.ToString(request, 0, 11).Replace("-", " ");
                //System.Diagnostics.Debug.WriteLine($"Modbus 요청: {requestHex}");

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, 11);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return false;
                }

                // 응답 대기
                Thread.Sleep(200);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 디버깅 로그
                if (response != null)
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    //System.Diagnostics.Debug.WriteLine($"Modbus 응답: {responseHex}");
                }

                // 응답 확인
                if (response != null && response.Length >= 8)
                {
                    if (response[0] == _slaveId && response[1] == FUNCTION_WRITE_MULTIPLE_REGISTERS)
                    {
                        // 성공적으로 응답 받음
                        return true;
                    }
                    else if (response[0] == _slaveId && response[1] == (FUNCTION_WRITE_MULTIPLE_REGISTERS | 0x80))
                    {
                        // 오류 응답
                        OnErrorOccurred($"MODBUS 오류: {GetModbusErrorMessage(response[2])}");
                        return false;
                    }
                }

                OnErrorOccurred("유효하지 않은 응답");
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"명령 전송 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Input 레지스터를 읽습니다.
        /// </summary>
        /// <param name="startAddress">시작 레지스터 주소</param>
        /// <param name="count">읽을 레지스터 개수</param>
        /// <returns>레지스터 값 배열 또는 null (실패 시)</returns>
        private ushort[] ReadInputRegisters(ushort startAddress, int count)
        {

            // 버퍼 타임
            Thread.Sleep(100);

            try
            {
                // Modbus 프레임 구성
                byte[] request = new byte[8];

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (04 = Input Registers 읽기)
                request[1] = FUNCTION_READ_INPUT_REGISTERS;

                // 시작 레지스터 주소
                request[2] = (byte)((startAddress >> 8) & 0xFF);
                request[3] = (byte)(startAddress & 0xFF);

                // 레지스터 개수
                request[4] = (byte)((count >> 8) & 0xFF);
                request[5] = (byte)(count & 0xFF);

                // CRC 계산 및 추가
                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                // 디버깅 로그
                string requestHex = BitConverter.ToString(request).Replace("-", " ");
                //System.Diagnostics.Debug.WriteLine($"Modbus Input Register 요청: {requestHex}");

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return null;
                }

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 디버깅 로그
                if (response != null)
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    //System.Diagnostics.Debug.WriteLine($"Modbus Input Register 응답: {responseHex}");
                }

                // 응답 검증
                if (response == null || response.Length < 3)
                {
                    //System.Diagnostics.Debug.WriteLine("응답이 없거나 너무 짧습니다");
                    return null;
                }

                // 오류 응답 확인
                if (response[0] == _slaveId && response[1] == (FUNCTION_READ_INPUT_REGISTERS | 0x80))
                {
                    string errorMsg = GetModbusErrorMessage(response[2]);
                    OnErrorOccurred($"MODBUS 오류: {errorMsg} (코드: {response[2]}, 레지스터: 0x{startAddress:X4})");
                    return null;
                }

                // 정상 응답 확인
                if (response[0] == _slaveId && response[1] == FUNCTION_READ_INPUT_REGISTERS)
                {
                    int byteCount = response[2];
                    if (response.Length >= 3 + byteCount && byteCount == count * 2)
                    {
                        ushort[] registers = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            int idx = 3 + i * 2;
                            registers[i] = (ushort)((response[idx] << 8) | response[idx + 1]);
                        }
                        return registers;
                    }
                    else
                    {
                        OnErrorOccurred($"응답 데이터 길이 불일치: 예상 {count * 2}, 실제 {(response.Length >= 3 ? response[2] : 0)}");
                    }
                }
                else
                {
                    OnErrorOccurred($"예상치 못한 응답: 슬레이브 ID={response[0]}, 함수 코드={response[1]}");
                }

                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Input 레지스터 읽기 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Holding 레지스터를 읽습니다. (시스템 명령 레지스터용)
        /// </summary>
        /// <param name="startAddress">시작 레지스터 주소</param>
        /// <param name="count">읽을 레지스터 개수</param>
        /// <returns>레지스터 값 배열 또는 null (실패 시)</returns>
        private ushort[] ReadHoldingRegisters(ushort startAddress, int count)
        {
            // 버퍼 타임
            Thread.Sleep(100);
            try
            {
                // Modbus 프레임 구성
                byte[] request = new byte[8];

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (03 = Holding Registers 읽기)
                request[1] = FUNCTION_READ_HOLDING_REGISTERS;

                // 시작 레지스터 주소
                request[2] = (byte)((startAddress >> 8) & 0xFF);
                request[3] = (byte)(startAddress & 0xFF);

                // 레지스터 개수
                request[4] = (byte)((count >> 8) & 0xFF);
                request[5] = (byte)(count & 0xFF);

                // CRC 계산 및 추가
                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                // 디버깅 로그
                string requestHex = BitConverter.ToString(request).Replace("-", " ");
                //System.Diagnostics.Debug.WriteLine($"Modbus Holding Register 요청: {requestHex}");

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return null;
                }

                // 응답 대기
                //Thread.Sleep(200);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 디버깅 로그
                if (response != null)
                {
                    string responseHex = BitConverter.ToString(response).Replace("-", " ");
                    //System.Diagnostics.Debug.WriteLine($"Modbus Holding Register 응답: {responseHex}");
                }

                // 응답 검증
                if (response == null || response.Length < 3)
                {
                    return null;
                }

                // 오류 응답 확인
                if (response[0] == _slaveId && response[1] == (FUNCTION_READ_HOLDING_REGISTERS | 0x80))
                {
                    string errorMsg = GetModbusErrorMessage(response[2]);
                    OnErrorOccurred($"MODBUS 오류: {errorMsg} (코드: {response[2]}, 레지스터: 0x{startAddress:X4})");
                    return null;
                }

                // 정상 응답 확인
                if (response[0] == _slaveId && response[1] == FUNCTION_READ_HOLDING_REGISTERS)
                {
                    int byteCount = response[2];
                    if (response.Length >= 3 + byteCount && byteCount == count * 2)
                    {
                        ushort[] registers = new ushort[count];
                        for (int i = 0; i < count; i++)
                        {
                            int idx = 3 + i * 2;
                            registers[i] = (ushort)((response[idx] << 8) | response[idx + 1]);
                        }
                        return registers;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Holding 레지스터 읽기 오류: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Q15 형식의 값을 실제 값으로 변환합니다.
        /// </summary>
        /// <param name="q15Value">Q15 형식 값</param>
        /// <param name="fullScale">최대 스케일 값</param>
        /// <returns>변환된 실제 값</returns>
        private double ConvertQ15ToReal(ushort q15Value, double fullScale)
        {
            if (q15Value <= 0x7FFF)
            {
                // 양수 값
                return (q15Value / 32768.0) * fullScale;
            }
            else
            {
                // 음수 값
                return ((q15Value - 65536) / 32768.0) * fullScale;
            }
        }

        /// <summary>
        /// CRC 체크섬을 계산합니다.
        /// </summary>
        private ushort CalculateCRC(byte[] buffer, int length)
        {
            ushort crc = 0xFFFF;

            for (int i = 0; i < length; i++)
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

        /// <summary>
        /// Modbus 오류 코드에 해당하는 오류 메시지를 반환합니다.
        /// </summary>
        private string GetModbusErrorMessage(byte errorCode)
        {
            switch (errorCode)
            {
                case 1: return "잘못된 기능 코드";
                case 2: return "잘못된 데이터 주소";
                case 3: return "잘못된 데이터 값";
                case 4: return "슬레이브 장치 오류";
                default: return $"알 수 없는 오류 (코드: {errorCode})";
            }
        }

        #endregion
    }

    /// <summary>
    /// 드라이 펌프의 현재 상태 정보를 저장하는 클래스입니다.
    /// </summary>
    public class DryPumpStatus : INotifyPropertyChanged
    {
        private bool _isRunning;
        private bool _isStopping;
        private bool _isStandby;
        private bool _isNormalSpeed;
        private bool _isAboveRampSpeed;
        private bool _isAboveOverloadSpeed;
        private bool _hasWarning;
        private bool _hasFault;
        private bool _isServiceDue;
        private ushort _warningRegister;
        private ushort _faultRegister;
        private double _motorFrequency;
        private double _motorCurrent;
        private double _dcLinkVoltage;
        private double _motorPower;
        private double _motorTemperature;
        private double _sinkTemperature;
        private double _controllerTemperature;
        private double _runTimeHours;
        private double _poweredTimeHours;

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
        /// 펌프가 정지 중인지 여부
        /// </summary>
        public bool IsStopping
        {
            get => _isStopping;
            internal set
            {
                if (_isStopping != value)
                {
                    _isStopping = value;
                    OnPropertyChanged(nameof(IsStopping));
                }
            }
        }

        /// <summary>
        /// 펌프가 대기 모드인지 여부
        /// </summary>
        public bool IsStandby
        {
            get => _isStandby;
            internal set
            {
                if (_isStandby != value)
                {
                    _isStandby = value;
                    OnPropertyChanged(nameof(IsStandby));
                }
            }
        }

        /// <summary>
        /// 펌프가 정상 속도인지 여부
        /// </summary>
        public bool IsNormalSpeed
        {
            get => _isNormalSpeed;
            internal set
            {
                if (_isNormalSpeed != value)
                {
                    _isNormalSpeed = value;
                    OnPropertyChanged(nameof(IsNormalSpeed));
                }
            }
        }

        /// <summary>
        /// 펌프가 램프 속도 이상인지 여부
        /// </summary>
        public bool IsAboveRampSpeed
        {
            get => _isAboveRampSpeed;
            internal set
            {
                if (_isAboveRampSpeed != value)
                {
                    _isAboveRampSpeed = value;
                    OnPropertyChanged(nameof(IsAboveRampSpeed));
                }
            }
        }

        /// <summary>
        /// 펌프가 과부하 속도 이상인지 여부
        /// </summary>
        public bool IsAboveOverloadSpeed
        {
            get => _isAboveOverloadSpeed;
            internal set
            {
                if (_isAboveOverloadSpeed != value)
                {
                    _isAboveOverloadSpeed = value;
                    OnPropertyChanged(nameof(IsAboveOverloadSpeed));
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
        public bool HasFault
        {
            get => _hasFault;
            internal set
            {
                if (_hasFault != value)
                {
                    _hasFault = value;
                    OnPropertyChanged(nameof(HasFault));
                }
            }
        }

        /// <summary>
        /// 서비스가 필요한지 여부
        /// </summary>
        public bool IsServiceDue
        {
            get => _isServiceDue;
            internal set
            {
                if (_isServiceDue != value)
                {
                    _isServiceDue = value;
                    OnPropertyChanged(nameof(IsServiceDue));
                }
            }
        }

        /// <summary>
        /// 경고 레지스터 값
        /// </summary>
        public ushort WarningRegister
        {
            get => _warningRegister;
            internal set
            {
                if (_warningRegister != value)
                {
                    _warningRegister = value;
                    OnPropertyChanged(nameof(WarningRegister));
                }
            }
        }

        /// <summary>
        /// 오류 레지스터 값
        /// </summary>
        public ushort FaultRegister
        {
            get => _faultRegister;
            internal set
            {
                if (_faultRegister != value)
                {
                    _faultRegister = value;
                    OnPropertyChanged(nameof(FaultRegister));
                }
            }
        }

        /// <summary>
        /// 모터 주파수 (Hz)
        /// </summary>
        public double MotorFrequency
        {
            get => _motorFrequency;
            internal set
            {
                if (_motorFrequency != value)
                {
                    _motorFrequency = value;
                    OnPropertyChanged(nameof(MotorFrequency));
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
        /// DC 링크 전압 (V)
        /// </summary>
        public double DCLinkVoltage
        {
            get => _dcLinkVoltage;
            internal set
            {
                if (_dcLinkVoltage != value)
                {
                    _dcLinkVoltage = value;
                    OnPropertyChanged(nameof(DCLinkVoltage));
                }
            }
        }

        /// <summary>
        /// 모터 전력 (W)
        /// </summary>
        public double MotorPower
        {
            get => _motorPower;
            internal set
            {
                if (_motorPower != value)
                {
                    _motorPower = value;
                    OnPropertyChanged(nameof(MotorPower));
                }
            }
        }

        /// <summary>
        /// 모터 온도 (°C)
        /// </summary>
        public double MotorTemperature
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
        /// 방열판 온도 (°C)
        /// </summary>
        public double SinkTemperature
        {
            get => _sinkTemperature;
            internal set
            {
                if (_sinkTemperature != value)
                {
                    _sinkTemperature = value;
                    OnPropertyChanged(nameof(SinkTemperature));
                }
            }
        }

        /// <summary>
        /// 컨트롤러 온도 (°C)
        /// </summary>
        public double ControllerTemperature
        {
            get => _controllerTemperature;
            internal set
            {
                if (_controllerTemperature != value)
                {
                    _controllerTemperature = value;
                    OnPropertyChanged(nameof(ControllerTemperature));
                }
            }
        }

        /// <summary>
        /// 실행 시간 (시간)
        /// </summary>
        public double RunTimeHours
        {
            get => _runTimeHours;
            internal set
            {
                if (_runTimeHours != value)
                {
                    _runTimeHours = value;
                    OnPropertyChanged(nameof(RunTimeHours));
                }
            }
        }

        /// <summary>
        /// 전원 켜진 시간 (시간)
        /// </summary>
        public double PoweredTimeHours
        {
            get => _poweredTimeHours;
            internal set
            {
                if (_poweredTimeHours != value)
                {
                    _poweredTimeHours = value;
                    OnPropertyChanged(nameof(PoweredTimeHours));
                }
            }
        }

        #endregion
    }
}