using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.BathCirculator
{
    /// <summary>
    /// Bath Circulator(칠러) 장치 제어 및 모니터링을 위한 클래스입니다.
    /// DeviceBase 클래스를 상속하여 표준화된 장치 인터페이스를 제공합니다.
    /// </summary>
    public class BathCirculator : DeviceBase
    {
        #region 상수 및 열거형

        // Modbus 함수 코드
        private const byte FUNCTION_READ_HOLDING_REGISTERS = 0x03;
        private const byte FUNCTION_WRITE_SINGLE_REGISTER = 0x06;
        private const byte FUNCTION_WRITE_MULTIPLE_REGISTERS = 0x10;

        // 레지스터 주소 (D-Map 참조)
        private const ushort REG_NPV = 0x0001;         // 현재 측정 온도값
        private const ushort REG_NSV = 0x0002;         // 현재 목표 온도값
        private const ushort REG_NMV = 0x0003;         // 현재 출력값
        private const ushort REG_NTM = 0x0004;         // 현재 동작시간
        private const ushort REG_ERROR = 0x0007;       // 에러 상태
        private const ushort REG_STATUS = 0x0008;      // 장치 상태
        private const ushort REG_OPERATION = 0x0009;   // 운전 상태
        private const ushort REG_STEP_ING = 0x000A;    // 현재 스텝

        // 설정 레지스터
        private const ushort REG_FIX_SV = 0x0015;      // FIX Mode 목표 온도 값
        private const ushort REG_FIX_TIME = 0x0016;    // FIX Mode 시간 설정
        private const ushort REG_COMM_RS = 0x0019;     // Run/Stop 명령

        // 명령 값 (트리거 방식이므로 모두 1을 사용함)
        private const ushort CMD_TRIGGER = 0x0001;  // 모든 트리거 명령에 사용됨

        #endregion

        #region 필드 및 속성

        // 통신 설정
        private byte _slaveId = 0x01;
        private int _timeout = 1000;  // 통신 타임아웃(ms)
        private bool _isUpdatingStatus = false;

        // 상태 정보
        private BathCirculatorStatus _currentStatus = new BathCirculatorStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        /// <summary>
        /// 현재 장치 상태 정보를 가져옵니다.
        /// </summary>
        public BathCirculatorStatus Status => _currentStatus;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "Bath Circulator";

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
        /// 장치 모델
        /// </summary>
        private string _model;

        /// <summary>
        /// 장치가 실행 중인지 여부
        /// </summary>
        public bool IsRunning => _currentStatus.IsRunning;

        /// <summary>
        /// 장치에 경고가 있는지 여부
        /// </summary>
        public bool HasWarning => _currentStatus.HasWarning;

        /// <summary>
        /// 장치에 오류가 있는지 여부
        /// </summary>
        public bool HasError => _currentStatus.HasError;

        #endregion

        #region 생성자

        /// <summary>
        /// BathCirculator 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="model">장치 모델</param>
        /// <param name="slaveId">슬레이브 ID (기본값: 1)</param>
        public BathCirculator(ICommunicationManager communicationManager, string model = "LK-1000", byte slaveId = 1)
            : base(communicationManager)
        {
            _model = model;
            _slaveId = slaveId;
            DeviceId = $"{model}_{slaveId}";
        }

        /// <summary>
        /// BathCirculator 클래스의 새 인스턴스를 초기화합니다.
        /// SerialManager 싱글톤 인스턴스를 사용합니다.
        /// </summary>
        /// <param name="model">장치 모델</param>
        /// <param name="slaveId">슬레이브 ID (기본값: 1)</param>
        public BathCirculator(string model = "LK-1000", byte slaveId = 1)
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
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "장치 상태 확인 성공", DeviceStatusCode.Ready));
                }
                else
                {
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "장치 상태 확인 실패, 다시 시도하세요", DeviceStatusCode.Warning));
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"장치 초기화 실패: {ex.Message}");
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
                // 상태 레지스터를 읽어 장치 동작 상태 확인
                ushort[] statusRegisters = ReadHoldingRegisters(REG_ERROR, 3);

                if (statusRegisters != null && statusRegisters.Length >= 3)
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

        #region 장치 제어 메서드

        /// <summary>
        /// 장치를 시작합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Start()
        {
            EnsureConnected();

            try
            {
                // 트리거 방식으로 시작 명령 전송 (무조건 1 사용)
                bool result = WriteRunStopCommand(1);
                if (result)
                {
                    Thread.Sleep(200);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "장치 시작 명령 성공", DeviceStatusCode.Running));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"장치 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 장치를 정지합니다.
        /// </summary>
        /// <returns>명령 성공 여부</returns>
        public bool Stop()
        {
            EnsureConnected();

            try
            {
                // 트리거 방식으로 정지 명령 전송 (0/1 간 변화가 필요하므로 1 사용)
                bool result = WriteRunStopCommand(1);
                if (result)
                {
                    Thread.Sleep(200);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "장치 정지 명령 성공", DeviceStatusCode.Idle));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"장치 정지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 목표 온도를 설정합니다.
        /// </summary>
        /// <param name="temperature">설정할 온도 (℃)</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetTemperature(double temperature)
        {
            EnsureConnected();

            try
            {
                // 온도값을 레지스터 값으로 변환 (100배)
                ushort tempValue = (ushort)(temperature * 100);

                // 목표 온도 레지스터에 값 쓰기
                bool result = WriteSingleRegister(REG_FIX_SV, tempValue);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, $"목표 온도 설정 성공: {temperature}℃", DeviceStatusCode.Ready));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"목표 온도 설정 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 작동 시간을 설정합니다.
        /// </summary>
        /// <param name="minutes">설정할 시간 (분), -1은 시간 제한 없음, 0은 종료 시간</param>
        /// <returns>명령 성공 여부</returns>
        public bool SetOperationTime(int minutes)
        {
            EnsureConnected();

            try
            {
                // 시간을 레지스터 값으로 변환
                short timeValue = (short)minutes;

                // 시간 레지스터에 값 쓰기
                bool result = WriteSingleRegister(REG_FIX_TIME, (ushort)timeValue);
                if (result)
                {
                    Thread.Sleep(100);
                    UpdateStatus();
                    string timeDesc = minutes == -1 ? "제한 없음" : (minutes == 0 ? "종료 시간" : $"{minutes}분");
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, $"작동 시간 설정 성공: {timeDesc}", DeviceStatusCode.Ready));
                }
                return result;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"작동 시간 설정 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 상태 모니터링 메서드

        /// <summary>
        /// 비동기적으로 장치 상태를 업데이트합니다.
        /// </summary>
        /// <returns>상태 업데이트 성공 여부를 포함하는 Task</returns>
        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

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
                // 주요 상태 레지스터 읽기
                ushort[] mainStatusRegisters = ReadHoldingRegisters(REG_NPV, 4);
                if (mainStatusRegisters == null || mainStatusRegisters.Length < 4)
                {
                    return false;
                }

                // 오류 및 상태 레지스터 읽기
                ushort[] errorStatusRegisters = ReadHoldingRegisters(REG_ERROR, 3);
                if (errorStatusRegisters == null || errorStatusRegisters.Length < 3)
                {
                    return false;
                }

                // 목표 설정값 레지스터 읽기
                ushort[] settingsRegisters = ReadHoldingRegisters(REG_FIX_SV, 2);
                if (settingsRegisters == null || settingsRegisters.Length < 2)
                {
                    return false;
                }

                // 상태 정보 업데이트
                UpdateStatusFromRegisters(errorStatusRegisters);

                // 측정값 업데이트
                _currentStatus.CurrentTemperature = (double)mainStatusRegisters[0] / 100.0;
                _currentStatus.TargetTemperature = (double)mainStatusRegisters[1] / 100.0;
                _currentStatus.OutputPower = mainStatusRegisters[2];
                _currentStatus.OperationTimeMinutes = mainStatusRegisters[3];

                // 설정값 업데이트
                _currentStatus.SetTemperature = (double)settingsRegisters[0] / 100.0;
                _currentStatus.SetTimeMinutes = (short)settingsRegisters[1];

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
        /// 상태 레지스터 값으로부터 상태 정보를 업데이트합니다.
        /// </summary>
        /// <param name="statusRegisters">상태 레지스터 값 배열</param>
        private void UpdateStatusFromRegisters(ushort[] statusRegisters)
        {
            if (statusRegisters == null || statusRegisters.Length < 3)
            {
                return;
            }

            ushort errorReg = statusRegisters[0];
            ushort statusReg = statusRegisters[1];
            ushort operationReg = statusRegisters[2];

            // 에러 상태 처리
            _currentStatus.ErrorRegister = errorReg;
            _currentStatus.HasError = errorReg != 0;

            // 경고 상태 처리 (에러 레지스터의 경보 비트)
            _currentStatus.HasWarning = (errorReg & 0x0040) != 0;

            // 운전 상태 처리
            _currentStatus.StatusRegister = statusReg;
            _currentStatus.OperationRegister = operationReg;

            // 운전 모드 분석
            _currentStatus.IsFixMode = (statusReg & 0x0002) == 0;
            _currentStatus.IsProgMode = (statusReg & 0x0002) != 0;
            _currentStatus.IsWaiting = (statusReg & 0x0001) != 0;

            // 운전 상태 분석
            _currentStatus.IsRunning = operationReg == 1;
            _currentStatus.IsAutotuning = operationReg == 2;
            _currentStatus.IsHolding = operationReg == 4;
        }

        /// <summary>
        /// 현재 장치의 오류 코드를 분석합니다.
        /// </summary>
        /// <returns>오류 코드 설명 또는 null (오류가 없는 경우)</returns>
        public string GetErrorDescription()
        {
            if (!_currentStatus.HasError)
            {
                return null;
            }

            ushort errorCode = _currentStatus.ErrorRegister;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            // 오류 코드 해석 (D-Map 해설 참조)
            if ((errorCode & 0x0001) != 0) sb.Append("AD 에러, ");
            if ((errorCode & 0x0002) != 0) sb.Append("교정 에러, ");
            if ((errorCode & 0x0004) != 0) sb.Append("센서 Burn-out, ");
            if ((errorCode & 0x0008) != 0) sb.Append("Auto-tuning 에러, ");
            if ((errorCode & 0x0040) != 0) sb.Append("경보 발생, ");
            if ((errorCode & 0x0080) != 0) sb.Append("LBA 에러, ");
            if ((errorCode & 0x0100) != 0) sb.Append("DI 발생, ");
            if ((errorCode & 0x0200) != 0) sb.Append("Motor 에러, ");
            if ((errorCode & 0x0400) != 0) sb.Append("EEPROM 에러, ");
            if ((errorCode & 0x0800) != 0) sb.Append("RJC 에러, ");
            if ((errorCode & 0x1000) != 0) sb.Append("+Over, ");
            if ((errorCode & 0x2000) != 0) sb.Append("-Over, ");
            if ((errorCode & 0x4000) != 0) sb.Append("Door open, ");

            if (sb.Length > 0)
            {
                sb.Length -= 2; // 마지막 쉼표와 공백 제거
                return sb.ToString();
            }

            return "알 수 없는 오류";
        }

        /// <summary>
        /// 장치의 대략적인 운영 상태를 텍스트로 반환합니다.
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

            if (!_currentStatus.IsRunning)
            {
                return "정지됨";
            }

            if (_currentStatus.IsAutotuning)
            {
                return "Auto-tuning 중";
            }

            if (_currentStatus.IsHolding)
            {
                return "Holding 중 (Door open)";
            }

            if (_currentStatus.IsRunning)
            {
                if (_currentStatus.IsWaiting)
                {
                    return _currentStatus.IsFixMode ? "FIX 모드 Wait 중" : "PROG 모드 Wait 중";
                }

                return _currentStatus.IsFixMode ? "FIX 모드 실행 중" : "PROG 모드 실행 중";
            }

            return "상태 확인 중";
        }

        #endregion

        #region MODBUS 통신 메서드

        /// <summary>
        /// Run/Stop 명령을 전송합니다.
        /// </summary>
        /// <param name="command">명령 코드 (0: Stop, 1: Run)</param>
        /// <returns>성공 여부</returns>
        private bool WriteRunStopCommand(ushort command)
        {
            // 트리거 타입 명령이므로, 1을 써서 명령을 실행합니다.
            // 필요에 따라 장치가 자동으로 0으로 리셋할 수 있습니다.
            bool result = WriteSingleRegister(REG_COMM_RS, command);

            // 트리거 타입 명령이므로 잠시 대기
            if (result)
            {
                Thread.Sleep(300);
            }

            return result;
        }

        /// <summary>
        /// 단일 레지스터에 값을 씁니다.
        /// </summary>
        /// <param name="registerAddress">레지스터 주소</param>
        /// <param name="value">쓸 값</param>
        /// <returns>성공 여부</returns>
        private bool WriteSingleRegister(ushort registerAddress, ushort value)
        {
            // 버퍼 타임
            Thread.Sleep(100);
            try
            {
                // Modbus 프레임 구성
                byte[] request = new byte[8]; // Slave ID + Func code + Addr(2) + Value(2) + CRC(2)

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (06 = Single Holding Register 쓰기)
                request[1] = FUNCTION_WRITE_SINGLE_REGISTER;

                // 레지스터 주소
                request[2] = (byte)((registerAddress >> 8) & 0xFF);
                request[3] = (byte)(registerAddress & 0xFF);

                // 값 (상위 바이트 먼저)
                request[4] = (byte)((value >> 8) & 0xFF);
                request[5] = (byte)(value & 0xFF);

                // CRC 계산 및 추가
                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, 8);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return false;
                }

                // 응답 대기
                Thread.Sleep(200);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 확인
                if (response != null && response.Length >= 8)
                {
                    if (response[0] == _slaveId && response[1] == FUNCTION_WRITE_SINGLE_REGISTER)
                    {
                        // 성공적으로 응답 받음
                        return true;
                    }
                    else if (response[0] == _slaveId && response[1] == (FUNCTION_WRITE_SINGLE_REGISTER | 0x80))
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
        /// Holding 레지스터를 읽습니다.
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
    /// Bath Circulator(칠러)의 현재 상태 정보를 저장하는 클래스입니다.
    /// </summary>
    public class BathCirculatorStatus : INotifyPropertyChanged
    {
        private double _currentTemperature;
        private double _targetTemperature;
        private ushort _outputPower;
        private int _operationTimeMinutes;
        private double _setTemperature;
        private short _setTimeMinutes;
        private ushort _errorRegister;
        private ushort _statusRegister;
        private ushort _operationRegister;
        private bool _isRunning;
        private bool _isAutotuning;
        private bool _isHolding;
        private bool _hasError;
        private bool _hasWarning;
        private bool _isFixMode;
        private bool _isProgMode;
        private bool _isWaiting;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region 속성

        /// <summary>
        /// 현재 측정 온도 (℃)
        /// </summary>
        public double CurrentTemperature
        {
            get => _currentTemperature;
            internal set
            {
                if (_currentTemperature != value)
                {
                    _currentTemperature = value;
                    OnPropertyChanged(nameof(CurrentTemperature));
                }
            }
        }

        /// <summary>
        /// 현재 목표 온도 (℃)
        /// </summary>
        public double TargetTemperature
        {
            get => _targetTemperature;
            internal set
            {
                if (_targetTemperature != value)
                {
                    _targetTemperature = value;
                    OnPropertyChanged(nameof(TargetTemperature));
                }
            }
        }

        /// <summary>
        /// 출력 전력
        /// </summary>
        public ushort OutputPower
        {
            get => _outputPower;
            internal set
            {
                if (_outputPower != value)
                {
                    _outputPower = value;
                    OnPropertyChanged(nameof(OutputPower));
                }
            }
        }

        /// <summary>
        /// 동작 시간 (분)
        /// </summary>
        public int OperationTimeMinutes
        {
            get => _operationTimeMinutes;
            internal set
            {
                if (_operationTimeMinutes != value)
                {
                    _operationTimeMinutes = value;
                    OnPropertyChanged(nameof(OperationTimeMinutes));
                }
            }
        }

        /// <summary>
        /// 설정된 온도 (℃)
        /// </summary>
        public double SetTemperature
        {
            get => _setTemperature;
            internal set
            {
                if (_setTemperature != value)
                {
                    _setTemperature = value;
                    OnPropertyChanged(nameof(SetTemperature));
                }
            }
        }

        /// <summary>
        /// 설정된 시간 (분)
        /// </summary>
        public short SetTimeMinutes
        {
            get => _setTimeMinutes;
            internal set
            {
                if (_setTimeMinutes != value)
                {
                    _setTimeMinutes = value;
                    OnPropertyChanged(nameof(SetTimeMinutes));
                }
            }
        }

        /// <summary>
        /// 오류 레지스터 값
        /// </summary>
        public ushort ErrorRegister
        {
            get => _errorRegister;
            internal set
            {
                if (_errorRegister != value)
                {
                    _errorRegister = value;
                    OnPropertyChanged(nameof(ErrorRegister));
                }
            }
        }

        /// <summary>
        /// 상태 레지스터 값
        /// </summary>
        public ushort StatusRegister
        {
            get => _statusRegister;
            internal set
            {
                if (_statusRegister != value)
                {
                    _statusRegister = value;
                    OnPropertyChanged(nameof(StatusRegister));
                }
            }
        }

        /// <summary>
        /// 운전 레지스터 값
        /// </summary>
        public ushort OperationRegister
        {
            get => _operationRegister;
            internal set
            {
                if (_operationRegister != value)
                {
                    _operationRegister = value;
                    OnPropertyChanged(nameof(OperationRegister));
                }
            }
        }

        /// <summary>
        /// 장치가 실행 중인지 여부
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
        /// 장치가 Auto-tuning 중인지 여부
        /// </summary>
        public bool IsAutotuning
        {
            get => _isAutotuning;
            internal set
            {
                if (_isAutotuning != value)
                {
                    _isAutotuning = value;
                    OnPropertyChanged(nameof(IsAutotuning));
                }
            }
        }

        /// <summary>
        /// 장치가 Holding 중인지 여부
        /// </summary>
        public bool IsHolding
        {
            get => _isHolding;
            internal set
            {
                if (_isHolding != value)
                {
                    _isHolding = value;
                    OnPropertyChanged(nameof(IsHolding));
                }
            }
        }

        /// <summary>
        /// 장치에 오류가 있는지 여부
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
        /// 장치에 경고가 있는지 여부
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
        /// 장치가 FIX 모드인지 여부
        /// </summary>
        public bool IsFixMode
        {
            get => _isFixMode;
            internal set
            {
                if (_isFixMode != value)
                {
                    _isFixMode = value;
                    OnPropertyChanged(nameof(IsFixMode));
                }
            }
        }

        /// <summary>
        /// 장치가 PROG 모드인지 여부
        /// </summary>
        public bool IsProgMode
        {
            get => _isProgMode;
            internal set
            {
                if (_isProgMode != value)
                {
                    _isProgMode = value;
                    OnPropertyChanged(nameof(IsProgMode));
                }
            }
        }

        /// <summary>
        /// 장치가 Wait 상태인지 여부
        /// </summary>
        public bool IsWaiting
        {
            get => _isWaiting;
            internal set
            {
                if (_isWaiting != value)
                {
                    _isWaiting = value;
                    OnPropertyChanged(nameof(IsWaiting));
                }
            }
        }

        #endregion
    }
}