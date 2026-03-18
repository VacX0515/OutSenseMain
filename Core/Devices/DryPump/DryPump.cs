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
        private const byte FUNCTION_READ_INPUT_REGISTERS = 0x04;
        private const byte FUNCTION_WRITE_MULTIPLE_REGISTERS = 0x10;

        // 시스템 명령 레지스터 (홀딩 레지스터)
        private const ushort REG_SYSTEM_COMMAND = 0x0050;
        private const ushort REG_CONFIGURATION_COMMAND = 0x0051;

        // 속도 제어 레지스터 (홀딩 레지스터)
        private const ushort REG_SPEED_DEMAND = 0x0055;
        private const ushort REG_RUN_SPEED_SETTING = 0x00E0;

        // 명령 값
        private const ushort CMD_NULL = 0x0000;
        private const ushort CMD_STOP = 0x0001;
        private const ushort CMD_START = 0x0002;
        private const ushort CMD_STANDBY = 0x0003;
        private const ushort CMD_RUN = 0x0004;

        // 설정 명령 값
        private const ushort CFG_NULL = 0x0000;
        private const ushort CFG_STORE = 0x0001;

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
        private const ushort REG_ROTOR_SPEED = 0x007B;

        // 운영 이력 레지스터 (인풋 레지스터)
        private const ushort REG_RUN_SECONDS = 0x0080;
        private const ushort REG_POWERED_SECONDS = 0x0082;

        // Full Scale Values (매뉴얼 6.11.5절)
        private const double FSV_FREQUENCY = 400.0;   // Hz
        private const double FSV_VOLTAGE = 500.0;      // V
        private const double FSV_CURRENT = 25.0;       // A
        private const double FSV_TEMPERATURE = 200.0;  // °C

        // 속도 제한값 (매뉴얼 6.5절, 6.6절)
        private const double NOMINAL_SPEED_HZ = 250.0;         // 100% = 250 Hz
        private const double MIN_SPEED_PERCENT = 50.0;          // 최소 50%
        private const double MAX_SPEED_PERCENT = 100.0;         // 최대 100%
        private const double MIN_SPEED_HZ = NOMINAL_SPEED_HZ * MIN_SPEED_PERCENT / 100.0;  // 125 Hz
        private const double MAX_SPEED_HZ = NOMINAL_SPEED_HZ;  // 250 Hz

        #endregion

        #region 필드 및 속성

        // 통신 설정
        private byte _slaveId = 0x01;
        private int _timeout = 1000;
        private bool _isUpdatingStatus = false;

        // 상태 정보
        private DryPumpStatus _currentStatus = new DryPumpStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        /// <summary>현재 펌프 상태 정보를 가져옵니다.</summary>
        public DryPumpStatus Status => _currentStatus;

        /// <summary>장치 이름</summary>
        public override string DeviceName => "ECODRY Dry Pump";

        /// <summary>장치 모델</summary>
        public override string Model => _model;

        /// <summary>슬레이브 ID</summary>
        public byte SlaveId
        {
            get => _slaveId;
            set => _slaveId = value;
        }

        /// <summary>통신 타임아웃(ms) - 기본값: 1000ms</summary>
        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                SetTimeout(value);
            }
        }

        private string _model;

        /// <summary>펌프가 실행 중인지 여부</summary>
        public bool IsRunning => _currentStatus.IsRunning;

        /// <summary>펌프가 대기 모드인지 여부</summary>
        public bool IsStandby => _currentStatus.IsStandby;

        /// <summary>펌프에 경고가 있는지 여부</summary>
        public bool HasWarning => _currentStatus.HasWarning;

        /// <summary>펌프에 오류가 있는지 여부</summary>
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
        public DryPump(string model = "ECODRY 25 plus", byte slaveId = 1)
            : this(MultiPortSerialManager.Instance, model, slaveId)
        {
        }

        #endregion

        #region IDevice 구현

        /// <summary>연결 후 초기화 작업을 수행합니다.</summary>
        protected override void InitializeAfterConnection()
        {
            try
            {
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

        /// <summary>장치 상태를 확인합니다.</summary>
        public override bool CheckStatus()
        {
            EnsureConnected();

            try
            {
                ushort[] statusRegisters = ReadInputRegisters(REG_SYSTEM_STATUS_1, 2);

                if (statusRegisters != null && statusRegisters.Length >= 2)
                {
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

        /// <summary>펌프를 시작합니다.</summary>
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

        /// <summary>펌프를 정지합니다.</summary>
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

        /// <summary>펌프를 대기 모드로 전환합니다.</summary>
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

        /// <summary>펌프를 다시 정상 모드로 전환합니다.</summary>
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

        #region 속도 제어 메서드

        /// <summary>
        /// 펌프 속도를 백분율(%)로 설정합니다.
        /// 매뉴얼 6.5절/6.6절 기준, 유효 범위는 50% ~ 100%이며
        /// 50% 미만은 50%로, 100% 초과는 100%로 클램핑됩니다.
        /// </summary>
        /// <param name="percent">목표 속도 (50.0 ~ 100.0%)</param>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// - 100% = 250 Hz (NOMINAL_SPEED_HZ)
        /// - 50% = 125 Hz (MIN_SPEED_HZ)
        /// - 레지스터 0x0055 (Speed Demand)에 Q15 형식으로 기록합니다.
        /// - 속도 변경 후 StoreConfiguration()을 호출해야 비휘발성 메모리에 저장됩니다.
        /// </remarks>
        public bool SetSpeedDemandPercent(double percent)
        {
            // 범위 클램핑 (매뉴얼: 50% ~ 100%)
            double clampedPercent = Math.Max(MIN_SPEED_PERCENT, Math.Min(MAX_SPEED_PERCENT, percent));

            if (Math.Abs(clampedPercent - percent) > 0.01)
            {
                OnErrorOccurred($"속도 값이 유효 범위로 클램핑됨: {percent:F1}% → {clampedPercent:F1}% (허용 범위: {MIN_SPEED_PERCENT}% ~ {MAX_SPEED_PERCENT}%)");
            }

            double targetHz = NOMINAL_SPEED_HZ * clampedPercent / 100.0;
            return SetSpeedDemandHz(targetHz);
        }

        /// <summary>
        /// 펌프 속도를 Hz 단위로 설정합니다.
        /// 매뉴얼 6.6절 기준, 유효 범위는 125 Hz ~ 250 Hz이며
        /// 범위 밖 값은 클램핑됩니다.
        /// </summary>
        /// <param name="frequencyHz">목표 속도 (125.0 ~ 250.0 Hz)</param>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// - 레지스터 0x0055 (Speed Demand)에 Q15 형식으로 기록합니다.
        /// - Full Scale Value = 400 Hz (매뉴얼 6.11.5절)
        /// </remarks>
        public bool SetSpeedDemandHz(double frequencyHz)
        {
            EnsureConnected();

            // 범위 클램핑 (매뉴얼: 125 Hz ~ 250 Hz)
            double clampedHz = Math.Max(MIN_SPEED_HZ, Math.Min(MAX_SPEED_HZ, frequencyHz));

            if (Math.Abs(clampedHz - frequencyHz) > 0.01)
            {
                OnErrorOccurred($"속도 값이 유효 범위로 클램핑됨: {frequencyHz:F1} Hz → {clampedHz:F1} Hz (허용 범위: {MIN_SPEED_HZ} ~ {MAX_SPEED_HZ} Hz)");
            }

            try
            {
                // Hz → Q15 변환 (FSV_FREQUENCY = 400 Hz)
                ushort q15Value = ConvertRealToQ15(clampedHz, FSV_FREQUENCY);

                bool result = WriteHoldingRegister(REG_SPEED_DEMAND, q15Value);
                if (result)
                {
                    double actualPercent = clampedHz / NOMINAL_SPEED_HZ * 100.0;
                    _currentStatus.SpeedDemandPercent = actualPercent;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"속도 설정: {clampedHz:F1} Hz ({actualPercent:F1}%)", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Run Speed Setting 레지스터(0x00E0)를 통해 속도를 설정합니다.
        /// Speed Demand(0x0055)와 동일한 기능이지만 별도의 레지스터입니다.
        /// </summary>
        /// <param name="percent">목표 속도 (50.0 ~ 100.0%)</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetRunSpeedSettingPercent(double percent)
        {
            EnsureConnected();

            double clampedPercent = Math.Max(MIN_SPEED_PERCENT, Math.Min(MAX_SPEED_PERCENT, percent));
            double targetHz = NOMINAL_SPEED_HZ * clampedPercent / 100.0;

            try
            {
                ushort q15Value = ConvertRealToQ15(targetHz, FSV_FREQUENCY);

                bool result = WriteHoldingRegister(REG_RUN_SPEED_SETTING, q15Value);
                if (result)
                {
                    _currentStatus.SpeedDemandPercent = clampedPercent;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        $"Run Speed 설정: {targetHz:F1} Hz ({clampedPercent:F1}%)", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Run Speed 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 현재 로터 속도를 읽어옵니다.
        /// 레지스터 0x007B (Rotor Speed, Q15/Hz, FSV=400Hz)
        /// </summary>
        /// <returns>현재 로터 속도(Hz), 실패 시 -1</returns>
        public double GetRotorSpeed()
        {
            EnsureConnected();

            try
            {
                ushort[] registers = ReadInputRegisters(REG_ROTOR_SPEED, 1);
                if (registers != null && registers.Length >= 1)
                {
                    double speedHz = ConvertQ15ToReal(registers[0], FSV_FREQUENCY);
                    _currentStatus.RotorSpeed = speedHz;
                    return speedHz;
                }
                return -1;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"로터 속도 읽기 실패: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 현재 로터 속도를 백분율(%)로 반환합니다.
        /// </summary>
        /// <returns>현재 로터 속도(%), 실패 시 -1</returns>
        public double GetRotorSpeedPercent()
        {
            double speedHz = GetRotorSpeed();
            if (speedHz < 0) return -1;
            return speedHz / NOMINAL_SPEED_HZ * 100.0;
        }

        /// <summary>
        /// 현재 속도 설정값(Speed Demand)을 홀딩 레지스터에서 읽어옵니다.
        /// </summary>
        /// <returns>현재 Speed Demand(Hz), 실패 시 -1</returns>
        public double GetSpeedDemand()
        {
            EnsureConnected();

            try
            {
                ushort[] registers = ReadHoldingRegisters(REG_SPEED_DEMAND, 1);
                if (registers != null && registers.Length >= 1)
                {
                    double demandHz = ConvertQ15ToReal(registers[0], FSV_FREQUENCY);
                    _currentStatus.SpeedDemandPercent = demandHz / NOMINAL_SPEED_HZ * 100.0;
                    return demandHz;
                }
                return -1;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"Speed Demand 읽기 실패: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 변경된 사용자 설정을 비휘발성 메모리에 저장합니다.
        /// 매뉴얼 Table 18: Configuration command register 01 (0x0051)에 STORE(0x0001) 기록.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        /// <remarks>
        /// - 저장에 시간이 걸릴 수 있으며, 응답이 저장 완료 전에 올 수 있습니다.
        /// - 운영 이력(Operational History)은 이 명령으로 저장되지 않습니다.
        /// </remarks>
        public bool StoreConfiguration()
        {
            EnsureConnected();

            try
            {
                bool result = WriteHoldingRegister(REG_CONFIGURATION_COMMAND, CFG_STORE);
                if (result)
                {
                    // 매뉴얼: store 명령은 상당한 시간이 걸릴 수 있음
                    Thread.Sleep(500);
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                        "설정이 비휘발성 메모리에 저장됨", DeviceStatusCode.Ready));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"설정 저장 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 펌프를 대기 모드로 전환하고 지정된 속도로 설정합니다.
        /// STANDBY 명령 → Speed Demand 설정 순서로 실행합니다.
        /// </summary>
        /// <param name="percent">대기 속도 (50.0 ~ 100.0%)</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetStandbyWithSpeed(double percent)
        {
            EnsureConnected();

            try
            {
                // 1. STANDBY 모드 진입
                bool standbyResult = WriteSystemCommand(CMD_STANDBY);
                if (!standbyResult)
                {
                    OnErrorOccurred("STANDBY 모드 전환 실패");
                    return false;
                }

                Thread.Sleep(200);

                // 2. 속도 설정
                bool speedResult = SetSpeedDemandPercent(percent);
                if (!speedResult)
                {
                    OnErrorOccurred("속도 설정 실패");
                    return false;
                }

                Thread.Sleep(100);
                UpdateStatus();

                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                    $"대기 모드 설정 완료: {percent:F1}%", DeviceStatusCode.Standby));

                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"대기 모드 속도 설정 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 상태 모니터링 메서드

        /// <summary>비동기적으로 펌프 상태를 업데이트합니다.</summary>
        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

        /// <summary>펌프 상태를 업데이트합니다.</summary>
        public bool UpdateStatus()
        {
            if (_isUpdatingStatus)
            {
                return false;
            }

            EnsureConnected();
            _isUpdatingStatus = true;

            try
            {
                // 시스템 상태 레지스터 읽기
                ushort[] statusRegisters = ReadInputRegisters(REG_SYSTEM_STATUS_1, 2);
                if (statusRegisters == null || statusRegisters.Length < 2)
                {
                    return false;
                }

                // 경고 및 오류 레지스터 읽기
                ushort[] warningRegister = ReadInputRegisters(REG_WARNING, 1);
                ushort[] faultRegister = ReadInputRegisters(REG_LATCHED_FAULT, 1);

                // 모터 상태 레지스터 읽기 (0x0071 ~ 0x0079, 9개)
                ushort[] motorParams = ReadInputRegisters(REG_MOTOR_FREQUENCY, 9);

                // 로터 속도 읽기 (0x007B)
                ushort[] rotorSpeedReg = ReadInputRegisters(REG_ROTOR_SPEED, 1);

                // Frequency Reference 읽기 (0x0070)
                ushort[] freqRefReg = ReadInputRegisters(REG_FREQUENCY_REFERENCE, 1);

                // 운영 시간 레지스터 읽기
                ushort[] runTimeRegisters = ReadInputRegisters(REG_RUN_SECONDS, 4);

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
                        _currentStatus.MotorFrequency = ConvertQ15ToReal(motorParams[0], FSV_FREQUENCY);
                        _currentStatus.MotorCurrent = ConvertQ15ToReal(motorParams[3], FSV_CURRENT);
                        _currentStatus.DCLinkVoltage = ConvertQ15ToReal(motorParams[4], FSV_VOLTAGE);
                        _currentStatus.MotorPower = ConvertQ15ToReal(motorParams[5], 1000);  // W (매뉴얼에 FSV 명시 없음)
                        _currentStatus.MotorTemperature = ConvertQ15ToReal(motorParams[6], FSV_TEMPERATURE);
                        _currentStatus.SinkTemperature = ConvertQ15ToReal(motorParams[7], FSV_TEMPERATURE);
                        _currentStatus.ControllerTemperature = ConvertQ15ToReal(motorParams[8], FSV_TEMPERATURE);
                    }

                    // 로터 속도 업데이트
                    if (rotorSpeedReg != null && rotorSpeedReg.Length >= 1)
                    {
                        _currentStatus.RotorSpeed = ConvertQ15ToReal(rotorSpeedReg[0], FSV_FREQUENCY);
                    }

                    // Frequency Reference 업데이트
                    if (freqRefReg != null && freqRefReg.Length >= 1)
                    {
                        _currentStatus.FrequencyReference = ConvertQ15ToReal(freqRefReg[0], FSV_FREQUENCY);
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

        /// <summary>상태 레지스터 값으로부터 상태 정보를 업데이트합니다.</summary>
        private void UpdateStatusFromRegisters(ushort[] statusRegisters)
        {
            if (statusRegisters == null || statusRegisters.Length < 2)
            {
                return;
            }

            ushort status1 = statusRegisters[0];
            ushort status2 = statusRegisters[1];

            // 시스템 상태 레지스터 1 처리
            _currentStatus.IsRunning = (status1 & 0x0002) != 0;
            _currentStatus.IsStopping = (status1 & 0x0001) != 0;
            _currentStatus.IsStandby = (status1 & 0x0004) != 0;
            _currentStatus.IsNormalSpeed = (status1 & 0x0008) != 0;
            _currentStatus.IsAboveRampSpeed = (status1 & 0x0010) != 0;
            _currentStatus.IsAboveOverloadSpeed = (status1 & 0x0020) != 0;

            // 시스템 상태 레지스터 2 처리
            _currentStatus.HasWarning = (status2 & 0x0040) != 0;
            _currentStatus.HasFault = (status2 & 0x0080) != 0;
            _currentStatus.IsServiceDue = (status2 & 0x0008) != 0;
        }

        /// <summary>현재 펌프 상태에 대한 알람 코드를 분석합니다.</summary>
        public string GetAlarmDescription()
        {
            if (!_currentStatus.HasFault)
            {
                return null;
            }

            ushort faultCode = _currentStatus.FaultRegister;

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

        /// <summary>현재 펌프 상태에 대한 경고 코드를 분석합니다.</summary>
        public string GetWarningDescription()
        {
            if (!_currentStatus.HasWarning)
            {
                return null;
            }

            ushort warningCode = _currentStatus.WarningRegister;

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

        /// <summary>펌프의 대략적인 운영 상태를 텍스트로 반환합니다.</summary>
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
                return $"대기 모드 ({_currentStatus.RotorSpeed:F1} Hz)";
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

        /// <summary>시스템 명령 레지스터에 명령을 씁니다.</summary>
        private bool WriteSystemCommand(ushort command)
        {
            return WriteHoldingRegister(REG_SYSTEM_COMMAND, command);
        }

        /// <summary>
        /// 단일 홀딩 레지스터에 값을 씁니다.
        /// Function Code 0x10 (Write Multiple Registers), 개수 1로 사용합니다.
        /// </summary>
        /// <param name="registerAddress">레지스터 주소</param>
        /// <param name="value">기록할 값</param>
        /// <returns>성공 여부</returns>
        private bool WriteHoldingRegister(ushort registerAddress, ushort value)
        {
            Thread.Sleep(100);
            try
            {
                byte[] request = new byte[13];

                request[0] = _slaveId;
                request[1] = FUNCTION_WRITE_MULTIPLE_REGISTERS;

                // 레지스터 주소
                request[2] = (byte)((registerAddress >> 8) & 0xFF);
                request[3] = (byte)(registerAddress & 0xFF);

                // 레지스터 개수 = 1
                request[4] = 0x00;
                request[5] = 0x01;

                // 바이트 수 = 2
                request[6] = 0x02;

                // 값 (상위 바이트 먼저)
                request[7] = (byte)((value >> 8) & 0xFF);
                request[8] = (byte)(value & 0xFF);

                // CRC
                ushort crc = CalculateCRC(request, 9);
                request[9] = (byte)(crc & 0xFF);
                request[10] = (byte)((crc >> 8) & 0xFF);

                _communicationManager.DiscardInBuffer();

                bool writeResult = _communicationManager.Write(request, 0, 11);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return false;
                }

                Thread.Sleep(200);

                byte[] response = _communicationManager.ReadAll();

                if (response != null && response.Length >= 8)
                {
                    if (response[0] == _slaveId && response[1] == FUNCTION_WRITE_MULTIPLE_REGISTERS)
                    {
                        return true;
                    }
                    else if (response[0] == _slaveId && response[1] == (FUNCTION_WRITE_MULTIPLE_REGISTERS | 0x80))
                    {
                        OnErrorOccurred($"MODBUS 오류: {GetModbusErrorMessage(response[2])} (레지스터: 0x{registerAddress:X4})");
                        return false;
                    }
                }

                OnErrorOccurred($"유효하지 않은 응답 (레지스터: 0x{registerAddress:X4})");
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"레지스터 쓰기 오류 (0x{registerAddress:X4}): {ex.Message}");
                return false;
            }
        }

        /// <summary>Input 레지스터를 읽습니다.</summary>
        private ushort[] ReadInputRegisters(ushort startAddress, int count)
        {
            Thread.Sleep(100);

            try
            {
                byte[] request = new byte[8];

                request[0] = _slaveId;
                request[1] = FUNCTION_READ_INPUT_REGISTERS;

                request[2] = (byte)((startAddress >> 8) & 0xFF);
                request[3] = (byte)(startAddress & 0xFF);

                request[4] = (byte)((count >> 8) & 0xFF);
                request[5] = (byte)(count & 0xFF);

                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                _communicationManager.DiscardInBuffer();

                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return null;
                }

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < 3)
                {
                    return null;
                }

                if (response[0] == _slaveId && response[1] == (FUNCTION_READ_INPUT_REGISTERS | 0x80))
                {
                    string errorMsg = GetModbusErrorMessage(response[2]);
                    OnErrorOccurred($"MODBUS 오류: {errorMsg} (코드: {response[2]}, 레지스터: 0x{startAddress:X4})");
                    return null;
                }

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

        /// <summary>Holding 레지스터를 읽습니다.</summary>
        private ushort[] ReadHoldingRegisters(ushort startAddress, int count)
        {
            Thread.Sleep(100);
            try
            {
                byte[] request = new byte[8];

                request[0] = _slaveId;
                request[1] = FUNCTION_READ_HOLDING_REGISTERS;

                request[2] = (byte)((startAddress >> 8) & 0xFF);
                request[3] = (byte)(startAddress & 0xFF);

                request[4] = (byte)((count >> 8) & 0xFF);
                request[5] = (byte)(count & 0xFF);

                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                _communicationManager.DiscardInBuffer();

                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return null;
                }

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < 3)
                {
                    return null;
                }

                if (response[0] == _slaveId && response[1] == (FUNCTION_READ_HOLDING_REGISTERS | 0x80))
                {
                    string errorMsg = GetModbusErrorMessage(response[2]);
                    OnErrorOccurred($"MODBUS 오류: {errorMsg} (코드: {response[2]}, 레지스터: 0x{startAddress:X4})");
                    return null;
                }

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

        /// <summary>Q15 형식의 값을 실제 값으로 변환합니다.</summary>
        /// <param name="q15Value">Q15 형식 값</param>
        /// <param name="fullScale">Full Scale Value</param>
        /// <returns>변환된 실제 값</returns>
        private double ConvertQ15ToReal(ushort q15Value, double fullScale)
        {
            // 매뉴얼 6.11.5절:
            // ≤ 0x7FFF (양수): Vreal = (VQ15 / 2^15) * VFSV
            // > 0x7FFF (음수): Vreal = [(VQ15 - 2^16) / 2^15] * VFSV
            if (q15Value <= 0x7FFF)
            {
                return (q15Value / 32768.0) * fullScale;
            }
            else
            {
                return ((q15Value - 65536) / 32768.0) * fullScale;
            }
        }

        /// <summary>실제 값을 Q15 형식으로 변환합니다.</summary>
        /// <param name="realValue">실제 값</param>
        /// <param name="fullScale">Full Scale Value</param>
        /// <returns>Q15 형식 값</returns>
        /// <remarks>
        /// 매뉴얼 6.11.5절 역변환:
        /// 양수: VQ15 = (Vreal / VFSV) * 2^15
        /// 음수: VQ15 = (Vreal / VFSV) * 2^15 + 2^16
        /// </remarks>
        private ushort ConvertRealToQ15(double realValue, double fullScale)
        {
            double q15Double = (realValue / fullScale) * 32768.0;

            if (realValue >= 0)
            {
                // 양수: 0 ~ 0x7FFF 범위로 클램핑
                int q15Int = (int)Math.Round(q15Double);
                q15Int = Math.Max(0, Math.Min(0x7FFF, q15Int));
                return (ushort)q15Int;
            }
            else
            {
                // 음수: 2^16 보정 후 0x8000 ~ 0xFFFF 범위
                int q15Int = (int)Math.Round(q15Double + 65536.0);
                q15Int = Math.Max(0x8000, Math.Min(0xFFFF, q15Int));
                return (ushort)q15Int;
            }
        }

        /// <summary>CRC 체크섬을 계산합니다.</summary>
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

        /// <summary>Modbus 오류 코드에 해당하는 오류 메시지를 반환합니다.</summary>
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
        private double _rotorSpeed;
        private double _frequencyReference;
        private double _speedDemandPercent;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 기존 속성

        /// <summary>펌프가 실행 중인지 여부</summary>
        public bool IsRunning
        {
            get => _isRunning;
            internal set
            {
                if (_isRunning != value) { _isRunning = value; OnPropertyChanged(nameof(IsRunning)); }
            }
        }

        /// <summary>펌프가 정지 중인지 여부</summary>
        public bool IsStopping
        {
            get => _isStopping;
            internal set
            {
                if (_isStopping != value) { _isStopping = value; OnPropertyChanged(nameof(IsStopping)); }
            }
        }

        /// <summary>펌프가 대기 모드인지 여부</summary>
        public bool IsStandby
        {
            get => _isStandby;
            internal set
            {
                if (_isStandby != value) { _isStandby = value; OnPropertyChanged(nameof(IsStandby)); }
            }
        }

        /// <summary>펌프가 정상 속도인지 여부</summary>
        public bool IsNormalSpeed
        {
            get => _isNormalSpeed;
            internal set
            {
                if (_isNormalSpeed != value) { _isNormalSpeed = value; OnPropertyChanged(nameof(IsNormalSpeed)); }
            }
        }

        /// <summary>펌프가 램프 속도 이상인지 여부</summary>
        public bool IsAboveRampSpeed
        {
            get => _isAboveRampSpeed;
            internal set
            {
                if (_isAboveRampSpeed != value) { _isAboveRampSpeed = value; OnPropertyChanged(nameof(IsAboveRampSpeed)); }
            }
        }

        /// <summary>펌프가 과부하 속도 이상인지 여부</summary>
        public bool IsAboveOverloadSpeed
        {
            get => _isAboveOverloadSpeed;
            internal set
            {
                if (_isAboveOverloadSpeed != value) { _isAboveOverloadSpeed = value; OnPropertyChanged(nameof(IsAboveOverloadSpeed)); }
            }
        }

        /// <summary>펌프에 경고가 있는지 여부</summary>
        public bool HasWarning
        {
            get => _hasWarning;
            internal set
            {
                if (_hasWarning != value) { _hasWarning = value; OnPropertyChanged(nameof(HasWarning)); }
            }
        }

        /// <summary>펌프에 오류가 있는지 여부</summary>
        public bool HasFault
        {
            get => _hasFault;
            internal set
            {
                if (_hasFault != value) { _hasFault = value; OnPropertyChanged(nameof(HasFault)); }
            }
        }

        /// <summary>서비스가 필요한지 여부</summary>
        public bool IsServiceDue
        {
            get => _isServiceDue;
            internal set
            {
                if (_isServiceDue != value) { _isServiceDue = value; OnPropertyChanged(nameof(IsServiceDue)); }
            }
        }

        /// <summary>경고 레지스터 값</summary>
        public ushort WarningRegister
        {
            get => _warningRegister;
            internal set
            {
                if (_warningRegister != value) { _warningRegister = value; OnPropertyChanged(nameof(WarningRegister)); }
            }
        }

        /// <summary>오류 레지스터 값</summary>
        public ushort FaultRegister
        {
            get => _faultRegister;
            internal set
            {
                if (_faultRegister != value) { _faultRegister = value; OnPropertyChanged(nameof(FaultRegister)); }
            }
        }

        /// <summary>모터 주파수 (Hz)</summary>
        public double MotorFrequency
        {
            get => _motorFrequency;
            internal set
            {
                if (_motorFrequency != value) { _motorFrequency = value; OnPropertyChanged(nameof(MotorFrequency)); }
            }
        }

        /// <summary>모터 전류 (A)</summary>
        public double MotorCurrent
        {
            get => _motorCurrent;
            internal set
            {
                if (_motorCurrent != value) { _motorCurrent = value; OnPropertyChanged(nameof(MotorCurrent)); }
            }
        }

        /// <summary>DC 링크 전압 (V)</summary>
        public double DCLinkVoltage
        {
            get => _dcLinkVoltage;
            internal set
            {
                if (_dcLinkVoltage != value) { _dcLinkVoltage = value; OnPropertyChanged(nameof(DCLinkVoltage)); }
            }
        }

        /// <summary>모터 전력 (W)</summary>
        public double MotorPower
        {
            get => _motorPower;
            internal set
            {
                if (_motorPower != value) { _motorPower = value; OnPropertyChanged(nameof(MotorPower)); }
            }
        }

        /// <summary>모터 온도 (°C)</summary>
        public double MotorTemperature
        {
            get => _motorTemperature;
            internal set
            {
                if (_motorTemperature != value) { _motorTemperature = value; OnPropertyChanged(nameof(MotorTemperature)); }
            }
        }

        /// <summary>방열판 온도 (°C)</summary>
        public double SinkTemperature
        {
            get => _sinkTemperature;
            internal set
            {
                if (_sinkTemperature != value) { _sinkTemperature = value; OnPropertyChanged(nameof(SinkTemperature)); }
            }
        }

        /// <summary>컨트롤러 온도 (°C)</summary>
        public double ControllerTemperature
        {
            get => _controllerTemperature;
            internal set
            {
                if (_controllerTemperature != value) { _controllerTemperature = value; OnPropertyChanged(nameof(ControllerTemperature)); }
            }
        }

        /// <summary>실행 시간 (시간)</summary>
        public double RunTimeHours
        {
            get => _runTimeHours;
            internal set
            {
                if (_runTimeHours != value) { _runTimeHours = value; OnPropertyChanged(nameof(RunTimeHours)); }
            }
        }

        /// <summary>전원 켜진 시간 (시간)</summary>
        public double PoweredTimeHours
        {
            get => _poweredTimeHours;
            internal set
            {
                if (_poweredTimeHours != value) { _poweredTimeHours = value; OnPropertyChanged(nameof(PoweredTimeHours)); }
            }
        }

        #endregion

        #region 속도 관련 속성 (신규)

        /// <summary>
        /// 현재 로터 속도 (Hz).
        /// 레지스터 0x007B에서 읽은 값. 250 Hz = 100% 풀 스피드.
        /// </summary>
        public double RotorSpeed
        {
            get => _rotorSpeed;
            internal set
            {
                if (_rotorSpeed != value) { _rotorSpeed = value; OnPropertyChanged(nameof(RotorSpeed)); OnPropertyChanged(nameof(RotorSpeedPercent)); }
            }
        }

        /// <summary>
        /// 현재 로터 속도 (%).
        /// RotorSpeed를 기반으로 계산. 250 Hz = 100%.
        /// </summary>
        public double RotorSpeedPercent => _rotorSpeed / 250.0 * 100.0;

        /// <summary>
        /// Frequency Reference (Hz).
        /// 레지스터 0x0070에서 읽은 명목 속도.
        /// </summary>
        public double FrequencyReference
        {
            get => _frequencyReference;
            internal set
            {
                if (_frequencyReference != value) { _frequencyReference = value; OnPropertyChanged(nameof(FrequencyReference)); }
            }
        }

        /// <summary>
        /// 현재 Speed Demand 설정값 (%).
        /// 마지막으로 설정하거나 읽어온 속도 요구값.
        /// </summary>
        public double SpeedDemandPercent
        {
            get => _speedDemandPercent;
            internal set
            {
                if (_speedDemandPercent != value) { _speedDemandPercent = value; OnPropertyChanged(nameof(SpeedDemandPercent)); }
            }
        }

        #endregion
    }
}