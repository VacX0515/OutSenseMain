using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;

namespace VacX_OutSense.Core.Devices.TempController
{
    /// <summary>
    /// TM4 시리즈 온도 컨트롤러를 제어하는 클래스입니다.
    /// Modbus RTU 프로토콜을 사용하여, COM 포트를 통해 통신합니다.
    /// TM4-N2SE 확장 모듈 지원 (같은 COM 포트, 다른 슬레이브 주소)
    /// </summary>
    public class TempController : DeviceBase
    {
        #region 상수 및 열거형

        // Modbus 함수 코드
        private const byte FUNC_READ_COILS = 0x01;
        private const byte FUNC_READ_INPUTS = 0x02;
        private const byte FUNC_READ_HOLDING_REGS = 0x03;
        private const byte FUNC_READ_INPUT_REGS = 0x04;
        private const byte FUNC_WRITE_SINGLE_COIL = 0x05;
        private const byte FUNC_WRITE_SINGLE_REG = 0x06;
        private const byte FUNC_WRITE_MULTI_REGS = 0x10;

        // TM4 시리즈 주요 레지스터 주소
        private const ushort REG_CH1_PV = 0x03E8;
        private const ushort REG_CH1_DOT = 0x03E9;
        private const ushort REG_CH1_UNIT = 0x03EA;
        private const ushort REG_CH1_SV = 0x03EB;
        private const ushort REG_CH1_HEATING_MV = 0x03EC;
        private const ushort REG_CH1_COOLING_MV = 0x03ED;

        private const ushort REG_CH2_START = 0x03EE;
        private const ushort REG_CH3_START = 0x03F4;
        private const ushort REG_CH4_START = 0x03FA;

        // 보유 레지스터
        private const ushort REG_HOLD_CH1_SV = 0x0000;
        private const ushort REG_HOLD_CH2_SV = 0x03E8;

        // Ramp 관련 레지스터 주소
        private const ushort REG_HOLD_CH1_RAMP_UP = 0x0073;
        private const ushort REG_HOLD_CH1_RAMP_DOWN = 0x0074;
        private const ushort REG_HOLD_CH1_RAMP_UNIT = 0x0075;

        private const ushort REG_HOLD_CH2_RAMP_UP = 0x044C + (0x0073 - 0x0064);
        private const ushort REG_HOLD_CH2_RAMP_DOWN = 0x044C + (0x0074 - 0x0064);
        private const ushort REG_HOLD_CH2_RAMP_UNIT = 0x044C + (0x0075 - 0x0064);

        // 코일 주소
        private const ushort COIL_CH1_RUN_STOP = 0x0000;
        private const ushort COIL_CH1_AUTO_TUNING = 0x0001;
        private const ushort COIL_CH2_RUN_STOP = 0x0002;
        private const ushort COIL_CH2_AUTO_TUNING = 0x0003;

        public enum RampTimeUnit : ushort
        {
            Second = 0,
            Minute = 1,
            Hour = 2
        }

        private enum TemperatureSensorType : ushort
        {
            K_CA_H = 0,
            K_CA_L = 1,
            J_IC_H = 2,
            J_IC_L = 3,
            E_CR_H = 4,
            E_CR_L = 5,
            T_CC_H = 6,
            T_CC_L = 7
        }

        #endregion

        #region 필드 및 속성

        private int _deviceAddress = 1;
        private readonly int _mainModuleAddress;  // ★ 메인 모듈 주소 저장 (변경되지 않음)
        private readonly int _numChannels;
        private int _timeout = 500;
        private bool _isUpdatingStatus = false;
        private readonly object _commandLock = new object();

        private const int _maxTemp = 1500;

        // 확장 모듈 관련 필드
        private readonly bool _hasExpansion = false;
        private readonly int _expansionSlaveAddress = 2;
        private readonly int _expansionNumChannels = 0;
        private readonly int _totalChannels;

        private TemperatureControllerStatus _status = new TemperatureControllerStatus();
        private DateTime _lastStatusUpdateTime = DateTime.MinValue;

        public int DeviceAddress
        {
            get => _deviceAddress;
            set
            {
                if (value >= 1 && value <= 31)
                    _deviceAddress = value;
                else
                    throw new ArgumentOutOfRangeException(nameof(value), "장치 주소는 1에서 31 사이여야 합니다.");
            }
        }

        public int Timeout
        {
            get => _timeout;
            set
            {
                _timeout = value;
                SetTimeout(value);
            }
        }

        public TemperatureControllerStatus Status => _status;

        /// <summary>
        /// 메인 모듈 채널 수
        /// </summary>
        public int ChannelCount => _numChannels;

        /// <summary>
        /// 전체 채널 수 (메인 + 확장)
        /// </summary>
        public int TotalChannelCount => _totalChannels;

        /// <summary>
        /// 확장 모듈 채널 수
        /// </summary>
        public int ExpansionChannelCount => _expansionNumChannels;

        /// <summary>
        /// 확장 모듈 사용 여부
        /// </summary>
        public bool HasExpansion => _hasExpansion;

        public override string DeviceName => _hasExpansion ? "TM4 + TM4-N2SE" : "TM4 Temperature Controller";

        public override string Model => _hasExpansion ? "TM4 + N2SE" : "TM4 Series";

        #endregion

        #region 생성자

        /// <summary>
        /// TempController 생성자 (메인 모듈만)
        /// </summary>
        public TempController(ICommunicationManager communicationManager, int deviceAddress = 1, int numChannels = 2)
            : base(communicationManager)
        {
            DeviceAddress = deviceAddress;
            _mainModuleAddress = deviceAddress;  // ★ 메인 모듈 주소 저장
            _numChannels = numChannels;
            _totalChannels = numChannels;
            _hasExpansion = false;
            DeviceId = $"TM4-{deviceAddress:D2}";

            InitializeStatus();
        }

        /// <summary>
        /// TempController 생성자 (확장 모듈 포함)
        /// </summary>
        /// <param name="communicationManager">통신 관리자</param>
        /// <param name="deviceAddress">메인 모듈 슬레이브 주소</param>
        /// <param name="numChannels">메인 모듈 채널 수</param>
        /// <param name="expansionSlaveAddress">확장 모듈 슬레이브 주소</param>
        /// <param name="expansionChannels">확장 모듈 채널 수 (1-3)</param>
        public TempController(ICommunicationManager communicationManager, int deviceAddress, int numChannels,
            int expansionSlaveAddress, int expansionChannels)
            : base(communicationManager)
        {
            DeviceAddress = deviceAddress;
            _mainModuleAddress = deviceAddress;  // ★ 메인 모듈 주소 저장
            _numChannels = numChannels;
            _hasExpansion = true;
            _expansionSlaveAddress = expansionSlaveAddress;
            _expansionNumChannels = Math.Min(expansionChannels, 3);
            _totalChannels = _numChannels + _expansionNumChannels;
            DeviceId = $"TM4-{deviceAddress:D2}+EXP";

            InitializeStatus();
        }

        /// <summary>
        /// TempController 생성자 (포트명, 메인 모듈만)
        /// </summary>
        public TempController(string portName, int deviceAddress = 1, int numChannels = 2)
            : this(new DevicePortAdapter(portName, MultiPortSerialManager.Instance), deviceAddress, numChannels)
        {
        }

        /// <summary>
        /// TempController 생성자 (포트명, 확장 모듈 포함)
        /// </summary>
        public TempController(string portName, int deviceAddress, int numChannels,
            int expansionSlaveAddress, int expansionChannels)
            : this(new DevicePortAdapter(portName, MultiPortSerialManager.Instance),
                   deviceAddress, numChannels, expansionSlaveAddress, expansionChannels)
        {
        }

        private void InitializeStatus()
        {
            _status.ChannelCount = _totalChannels;

            for (int i = 0; i < _totalChannels; i++)
            {
                _status.ChannelStatus[i] = new ChannelStatus
                {
                    ChannelNumber = i + 1,
                    IsRunning = false,
                    IsAutoTuning = false,
                    PresentValue = 0,
                    SetValue = 0,
                    Dot = 0,
                    TemperatureUnit = "°C",
                    HeatingMV = 0,
                    CoolingMV = 0,
                    RampUpRate = 0,
                    RampDownRate = 0,
                    RampTimeUnit = 1,
                    IsRampActive = false,
                    IsExpansionChannel = (i >= _numChannels)
                };
            }
        }

        #endregion

        #region IDevice 구현

        protected override void InitializeAfterConnection()
        {
            try
            {
                OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 초기화 중...", DeviceStatusCode.Initializing));

                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();

                Thread.Sleep(100);

                ReadDeviceInfo();

                bool statusCheck = UpdateStatus();

                if (statusCheck)
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 초기화 성공", DeviceStatusCode.Ready));
                else
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 상태 확인 실패, 다시 시도하세요", DeviceStatusCode.Warning));
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"온도 컨트롤러 초기화 실패: {ex.Message}");
            }
        }

        public override bool CheckStatus()
        {
            EnsureConnected();

            try
            {
                return UpdateStatus();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"상태 확인 실패: {ex.Message}");
                return false;
            }
        }

        private void ReadDeviceInfo()
        {
            try
            {
                ushort[] registers = ReadInputRegisters(0x0064, 4);
                if (registers != null && registers.Length >= 4)
                {
                    _status.ProductNumberH = registers[0];
                    _status.ProductNumberL = registers[1];
                    _status.HardwareVersion = registers[2];
                    _status.SoftwareVersion = registers[3];

                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, "온도 컨트롤러 정보 읽기 성공", DeviceStatusCode.Ready));
                }

                registers = ReadInputRegisters(0x0068, 6);
                if (registers != null && registers.Length >= 6)
                {
                    char[] modelChars = new char[12];
                    for (int i = 0; i < 6; i++)
                    {
                        modelChars[i * 2] = (char)((registers[i] >> 8) & 0xFF);
                        modelChars[i * 2 + 1] = (char)(registers[i] & 0xFF);
                    }

                    string modelName = new string(modelChars).TrimEnd('\0', ' ');
                    _status.ModelName = modelName;
                }

                registers = ReadHoldingRegisters(0x012C, 7);
                if (registers != null && registers.Length >= 7)
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
        /// 전체 채널 상태 업데이트 (메인 + 확장)
        /// </summary>
        public bool UpdateStatus()
        {
            if (_isUpdatingStatus)
                return false;

            EnsureConnected();
            _isUpdatingStatus = true;

            try
            {
                // 메인 모듈 채널 업데이트
                for (int ch = 1; ch <= _numChannels; ch++)
                {
                    UpdateChannelStatus(ch);
                }

                // 확장 모듈 채널 업데이트
                if (_hasExpansion)
                {
                    for (int ch = 1; ch <= _expansionNumChannels; ch++)
                    {
                        UpdateExpansionChannelStatus(ch);
                    }
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

        public async Task<bool> UpdateStatusAsync()
        {
            return await Task.Run(() => UpdateStatus());
        }

        /// <summary>
        /// 메인 모듈 채널 상태 업데이트
        /// </summary>
        private bool UpdateChannelStatus(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            try
            {
                ushort baseAddress = (ushort)(REG_CH1_PV + (channelNumber - 1) * 6);
                ushort[] registers = ReadInputRegisters(baseAddress, 6);

                if (registers != null && registers.Length >= 6)
                {
                    int index = channelNumber - 1;
                    ProcessChannelRegisters(index, registers);

                    // RUN/STOP 상태 확인
                    ushort runStopCoilAddr = (ushort)((channelNumber - 1) * 2);
                    bool isRunning = !ReadCoil(runStopCoilAddr);
                    _status.ChannelStatus[index].IsRunning = isRunning;

                    // 오토튜닝 상태 확인
                    ushort autoTuningCoilAddr = (ushort)((channelNumber - 1) * 2 + 1);
                    bool isAutoTuning = ReadCoil(autoTuningCoilAddr);
                    _status.ChannelStatus[index].IsAutoTuning = isAutoTuning;

                    // Ramp 설정 읽기
                    GetRampConfiguration(channelNumber);

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

        /// <summary>
        /// 확장 모듈 채널 상태 업데이트
        /// </summary>
        private bool UpdateExpansionChannelStatus(int expansionChannelNumber)
        {
            if (expansionChannelNumber < 1 || expansionChannelNumber > _expansionNumChannels)
                return false;

            int originalAddress = _deviceAddress;

            try
            {
                // 슬레이브 주소를 확장 모듈로 변경
                _deviceAddress = _expansionSlaveAddress;
                Thread.Sleep(10);

                // 확장 모듈 레지스터 읽기
                ushort baseAddress = (ushort)(REG_CH1_PV + (expansionChannelNumber - 1) * 6);
                ushort[] registers = ReadInputRegisters(baseAddress, 6);

                // 슬레이브 주소 복원
                _deviceAddress = originalAddress;

                if (registers != null && registers.Length >= 6)
                {
                    // 스냅샷 인덱스: 확장 CH1 → index 2 (전체 CH3)
                    int index = _numChannels + (expansionChannelNumber - 1);
                    ProcessChannelRegisters(index, registers);

                    // 확장 모듈은 입력 전용
                    _status.ChannelStatus[index].IsRunning = false;
                    _status.ChannelStatus[index].IsAutoTuning = false;
                    _status.ChannelStatus[index].IsExpansionChannel = true;

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                // 슬레이브 주소 복원
                _deviceAddress = originalAddress;
                OnErrorOccurred($"확장 채널 {expansionChannelNumber} 상태 업데이트 오류: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 레지스터 데이터를 ChannelStatus에 저장
        /// </summary>
        private void ProcessChannelRegisters(int index, ushort[] registers)
        {
            short pvValue = (short)registers[0];

            // 센서 에러 체크
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

            _status.ChannelStatus[index].Dot = registers[1];
            _status.ChannelStatus[index].TemperatureUnit = registers[2] == 0 ? "°C" : "°F";
            _status.ChannelStatus[index].SetValue = (short)registers[3];
            _status.ChannelStatus[index].HeatingMV = registers[4] / 10.0f;
            _status.ChannelStatus[index].CoolingMV = registers[5] / 10.0f;
        }

        #endregion

        #region 제어 메서드

        public bool SetTemperature(int channelNumber, short setValue)
        {
            // 확장 채널은 입력 전용
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            if (setValue > _maxTemp)
            {
                MessageBox.Show($"설정 온도는 {_maxTemp / 10} 이하여야 합니다.", "Interlock", MessageBoxButtons.OK);
                throw new ArgumentOutOfRangeException(nameof(setValue), $"설정 온도는 {_maxTemp} 이하여야 합니다.");
            }

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정 - 확장 모듈 상태 업데이트 중에도 안전
                _deviceAddress = _mainModuleAddress;

                ushort registerAddress;

                if (channelNumber == 1)
                    registerAddress = REG_HOLD_CH1_SV;
                else if (channelNumber == 2)
                    registerAddress = REG_HOLD_CH2_SV;
                else
                    registerAddress = (ushort)((channelNumber - 1) * 0x03E8);

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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool Start(int channelNumber)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정
                _deviceAddress = _mainModuleAddress;

                ushort coilAddress;
                if (channelNumber == 1)
                    coilAddress = COIL_CH1_RUN_STOP;
                else if (channelNumber == 2)
                    coilAddress = COIL_CH2_RUN_STOP;
                else
                    coilAddress = (ushort)((channelNumber - 1) * 2);

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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool Stop(int channelNumber)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정
                _deviceAddress = _mainModuleAddress;

                ushort coilAddress;
                if (channelNumber == 1)
                    coilAddress = COIL_CH1_RUN_STOP;
                else if (channelNumber == 2)
                    coilAddress = COIL_CH2_RUN_STOP;
                else
                    coilAddress = (ushort)((channelNumber - 1) * 2);

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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool StartAutoTuning(int channelNumber)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정
                _deviceAddress = _mainModuleAddress;

                ushort coilAddress;
                if (channelNumber == 1)
                    coilAddress = COIL_CH1_AUTO_TUNING;
                else if (channelNumber == 2)
                    coilAddress = COIL_CH2_AUTO_TUNING;
                else
                    coilAddress = (ushort)((channelNumber - 1) * 2 + 1);

                bool result = WriteSingleCoil(coilAddress, true);

                if (result)
                {
                    _status.ChannelStatus[channelNumber - 1].IsAutoTuning = true;
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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool StopAutoTuning(int channelNumber)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정
                _deviceAddress = _mainModuleAddress;

                ushort coilAddress;
                if (channelNumber == 1)
                    coilAddress = COIL_CH1_AUTO_TUNING;
                else if (channelNumber == 2)
                    coilAddress = COIL_CH2_AUTO_TUNING;
                else
                    coilAddress = (ushort)((channelNumber - 1) * 2 + 1);

                bool result = WriteSingleCoil(coilAddress, false);

                if (result)
                {
                    _status.ChannelStatus[channelNumber - 1].IsAutoTuning = false;
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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool SetPIDParameters(int channelNumber, float heatingP, int heatingI, int heatingD)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            try
            {
                // ★ 항상 메인 모듈 주소로 설정
                _deviceAddress = _mainModuleAddress;

                ushort baseAddress = (ushort)(0x0065 + (channelNumber - 1) * 0x03E8);
                ushort heatingPValue = (ushort)(heatingP * 10);

                bool result1 = WriteSingleRegister(baseAddress, heatingPValue);
                bool result2 = WriteSingleRegister((ushort)(baseAddress + 2), (ushort)heatingI);
                bool result3 = WriteSingleRegister((ushort)(baseAddress + 4), (ushort)heatingD);

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
            finally
            {
                // ★ 주소 복원
                _deviceAddress = savedAddress;
            }
        }

        public bool SetRampConfiguration(int channelNumber, ushort rampUpRate, ushort rampDownRate, RampTimeUnit timeUnit)
        {
            if (channelNumber > _numChannels)
                throw new InvalidOperationException($"채널 {channelNumber}은 확장 모듈 채널로 입력 전용입니다.");

            if (channelNumber < 1 || channelNumber > _numChannels)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), $"채널 번호는 1에서 {_numChannels} 사이여야 합니다.");

            if (rampUpRate > 9999 || rampDownRate > 9999)
                throw new ArgumentOutOfRangeException("램프 변화율은 0-9999 사이여야 합니다.");

            EnsureConnected();

            // ★ 메인 모듈 주소 백업 및 강제 설정
            int savedAddress = _deviceAddress;

            lock (_commandLock)
            {
                try
                {
                    // ★ 항상 메인 모듈 주소로 설정
                    _deviceAddress = _mainModuleAddress;

                    ushort baseAddress;
                    if (channelNumber == 1)
                        baseAddress = REG_HOLD_CH1_RAMP_UP;
                    else
                        baseAddress = REG_HOLD_CH2_RAMP_UP;

                    ushort[] values = new ushort[] { rampUpRate, rampDownRate, (ushort)timeUnit };
                    bool result = WriteMultipleRegisters(baseAddress, values);

                    if (result)
                    {
                        int index = channelNumber - 1;
                        _status.ChannelStatus[index].RampUpRate = rampUpRate;
                        _status.ChannelStatus[index].RampDownRate = rampDownRate;
                        _status.ChannelStatus[index].RampTimeUnit = (ushort)timeUnit;

                        OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId,
                            $"채널 {channelNumber} Ramp 설정 성공: {_status.ChannelStatus[index].RampStatusText}",
                            DeviceStatusCode.Ready));
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"채널 {channelNumber} Ramp 설정 실패: {ex.Message}");
                    return false;
                }
                finally
                {
                    // ★ 주소 복원
                    _deviceAddress = savedAddress;
                }
            }
        }

        public bool GetRampConfiguration(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _numChannels)
                return false;

            EnsureConnected();

            try
            {
                ushort baseAddress;
                if (channelNumber == 1)
                    baseAddress = REG_HOLD_CH1_RAMP_UP;
                else
                    baseAddress = REG_HOLD_CH2_RAMP_UP;

                ushort[] registers = ReadHoldingRegisters(baseAddress, 3);

                if (registers != null && registers.Length >= 3)
                {
                    int index = channelNumber - 1;
                    _status.ChannelStatus[index].RampUpRate = registers[0];
                    _status.ChannelStatus[index].RampDownRate = registers[1];
                    _status.ChannelStatus[index].RampTimeUnit = registers[2];

                    CheckRampProgress(channelNumber);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"채널 {channelNumber} Ramp 설정 읽기 실패: {ex.Message}");
                return false;
            }
        }

        private void CheckRampProgress(int channelNumber)
        {
            var status = _status.ChannelStatus[channelNumber - 1];

            if (status.IsRampEnabled && status.IsRunning)
            {
                float diff = Math.Abs(status.SetValue - status.PresentValue);
                float tolerance = status.Dot == 0 ? 2.0f : 0.2f;
                status.IsRampActive = diff > tolerance;
            }
            else
            {
                status.IsRampActive = false;
            }
        }

        public async Task<bool> SetRampConfigurationAsync(int channelNumber, ushort rampUpRate,
            ushort rampDownRate, RampTimeUnit timeUnit)
        {
            return await Task.Run(() => SetRampConfiguration(channelNumber, rampUpRate, rampDownRate, timeUnit));
        }

        public async Task<bool> GetRampConfigurationAsync(int channelNumber)
        {
            return await Task.Run(() => GetRampConfiguration(channelNumber));
        }

        #endregion

        #region Modbus RTU 통신 메서드

        private bool ReadCoil(ushort address)
        {
            lock (_commandLock)
            {
                byte[] request = CreateModbusRtuRequest(FUNC_READ_COILS, address, 1);

                _communicationManager.DiscardInBuffer();

                bool success = _communicationManager.Write(request);
                if (!success)
                    throw new IOException($"코일 읽기 요청 전송 실패 (주소: 0x{address:X4})");

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < 6 || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_COILS)
                    throw new IOException($"코일 읽기 응답 오류 (주소: 0x{address:X4})");

                if ((response[1] & 0x80) == 0x80)
                    throw new IOException($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");

                return (response[3] & 0x01) == 0x01;
            }
        }

        private bool ReadInput(ushort address)
        {
            lock (_commandLock)
            {
                byte[] request = CreateModbusRtuRequest(FUNC_READ_INPUTS, address, 1);

                _communicationManager.DiscardInBuffer();

                bool success = _communicationManager.Write(request);
                if (!success)
                    throw new IOException($"입력 읽기 요청 전송 실패 (주소: 0x{address:X4})");

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < 6 || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_INPUTS)
                    throw new IOException($"입력 읽기 응답 오류 (주소: 0x{address:X4})");

                if ((response[1] & 0x80) == 0x80)
                    throw new IOException($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");

                return (response[3] & 0x01) == 0x01;
            }
        }

        private ushort[] ReadHoldingRegisters(ushort address, ushort count)
        {
            lock (_commandLock)
            {
                byte[] request = CreateModbusRtuRequest(FUNC_READ_HOLDING_REGS, address, count);

                _communicationManager.DiscardInBuffer();

                bool success = _communicationManager.Write(request);
                if (!success)
                    throw new IOException($"보유 레지스터 읽기 요청 전송 실패 (주소: 0x{address:X4})");

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < (5 + count * 2) || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_HOLDING_REGS)
                    throw new IOException($"보유 레지스터 읽기 응답 오류 (주소: 0x{address:X4})");

                if ((response[1] & 0x80) == 0x80)
                    throw new IOException($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");

                int byteCount = response[2];
                if (byteCount != count * 2)
                    throw new IOException($"보유 레지스터 읽기 응답 길이 불일치 (주소: 0x{address:X4})");

                ushort[] registers = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    registers[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
                }

                return registers;
            }
        }

        private ushort[] ReadInputRegisters(ushort address, ushort count)
        {
            lock (_commandLock)
            {
                byte[] request = CreateModbusRtuRequest(FUNC_READ_INPUT_REGS, address, count);

                _communicationManager.DiscardInBuffer();

                bool success = _communicationManager.Write(request);
                if (!success)
                    throw new IOException($"입력 레지스터 읽기 요청 전송 실패 (주소: 0x{address:X4})");

                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < (5 + count * 2) || response[0] != (byte)_deviceAddress || response[1] != FUNC_READ_INPUT_REGS)
                    throw new IOException($"입력 레지스터 읽기 응답 오류 (주소: 0x{address:X4})");

                if ((response[1] & 0x80) == 0x80)
                    throw new IOException($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");

                int byteCount = response[2];
                if (byteCount != count * 2)
                    throw new IOException($"입력 레지스터 읽기 응답 길이 불일치 (주소: 0x{address:X4})");

                ushort[] registers = new ushort[count];
                for (int i = 0; i < count; i++)
                {
                    registers[i] = (ushort)((response[3 + i * 2] << 8) | response[4 + i * 2]);
                }

                return registers;
            }
        }

        private bool WriteSingleCoil(ushort address, bool value)
        {
            lock (_commandLock)
            {
                try
                {
                    ushort coilValue = value ? (ushort)0xFF00 : (ushort)0x0000;
                    byte[] request = CreateModbusRtuWriteSingleCoilRequest(address, coilValue);

                    _communicationManager.DiscardInBuffer();

                    bool success = _communicationManager.Write(request);
                    if (!success)
                    {
                        OnErrorOccurred($"코일 쓰기 요청 전송 실패 (주소: 0x{address:X4})");
                        return false;
                    }

                    Thread.Sleep(50);

                    byte[] response = _communicationManager.ReadAll();

                    if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_SINGLE_COIL)
                    {
                        OnErrorOccurred($"코일 쓰기 응답 유효성 검사 실패 (주소: 0x{address:X4})");
                        return false;
                    }

                    if ((response[1] & 0x80) == 0x80)
                    {
                        OnErrorOccurred($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");
                        return false;
                    }

                    ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                    return respAddress == address;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"코일 쓰기 예외: {ex.Message} (주소: 0x{address:X4})");
                    return false;
                }
            }
        }

        private bool WriteSingleRegister(ushort address, ushort value)
        {
            lock (_commandLock)
            {
                try
                {
                    byte[] request = CreateModbusRtuWriteSingleRegisterRequest(address, value);

                    _communicationManager.DiscardInBuffer();

                    bool success = _communicationManager.Write(request);
                    if (!success)
                    {
                        OnErrorOccurred($"레지스터 쓰기 요청 전송 실패 (주소: 0x{address:X4})");
                        return false;
                    }

                    Thread.Sleep(50);

                    byte[] response = _communicationManager.ReadAll();

                    if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_SINGLE_REG)
                    {
                        OnErrorOccurred($"레지스터 쓰기 응답 유효성 검사 실패 (주소: 0x{address:X4})");
                        return false;
                    }

                    if ((response[1] & 0x80) == 0x80)
                    {
                        OnErrorOccurred($"Modbus 예외 응답: {response[2]} (주소: 0x{address:X4})");
                        return false;
                    }

                    ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                    return respAddress == address;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"레지스터 쓰기 예외: {ex.Message} (주소: 0x{address:X4})");
                    return false;
                }
            }
        }

        private bool WriteMultipleRegisters(ushort address, ushort[] values)
        {
            lock (_commandLock)
            {
                try
                {
                    byte[] request = CreateModbusRtuWriteMultipleRegistersRequest(address, values);

                    _communicationManager.DiscardInBuffer();

                    bool success = _communicationManager.Write(request);
                    if (!success)
                        return false;

                    byte[] response = _communicationManager.ReadAll();

                    if (response == null || response.Length < 8 || response[0] != (byte)_deviceAddress || response[1] != FUNC_WRITE_MULTI_REGS)
                        return false;

                    if ((response[1] & 0x80) == 0x80)
                        return false;

                    ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                    ushort respCount = (ushort)((response[4] << 8) | response[5]);

                    return respAddress == address && respCount == values.Length;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"다중 레지스터 쓰기 예외: {ex.Message}");
                    return false;
                }
            }
        }

        private byte[] CreateModbusRtuRequest(byte functionCode, ushort address, ushort count)
        {
            byte[] frame = new byte[8];

            frame[0] = (byte)_deviceAddress;
            frame[1] = functionCode;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(count >> 8);
            frame[5] = (byte)(count & 0xFF);

            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        private byte[] CreateModbusRtuWriteSingleCoilRequest(ushort address, ushort value)
        {
            byte[] frame = new byte[8];

            frame[0] = (byte)_deviceAddress;
            frame[1] = FUNC_WRITE_SINGLE_COIL;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        private byte[] CreateModbusRtuWriteSingleRegisterRequest(ushort address, ushort value)
        {
            byte[] frame = new byte[8];

            frame[0] = (byte)_deviceAddress;
            frame[1] = FUNC_WRITE_SINGLE_REG;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(value >> 8);
            frame[5] = (byte)(value & 0xFF);

            ushort crc = CalculateCRC(frame, 0, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        private byte[] CreateModbusRtuWriteMultipleRegistersRequest(ushort address, ushort[] values)
        {
            int count = values.Length;
            int byteCount = count * 2;

            byte[] frame = new byte[9 + byteCount];

            frame[0] = (byte)_deviceAddress;
            frame[1] = FUNC_WRITE_MULTI_REGS;
            frame[2] = (byte)(address >> 8);
            frame[3] = (byte)(address & 0xFF);
            frame[4] = (byte)(count >> 8);
            frame[5] = (byte)(count & 0xFF);
            frame[6] = (byte)byteCount;

            for (int i = 0; i < count; i++)
            {
                frame[7 + i * 2] = (byte)(values[i] >> 8);
                frame[8 + i * 2] = (byte)(values[i] & 0xFF);
            }

            ushort crc = CalculateCRC(frame, 0, 7 + byteCount);
            frame[7 + byteCount] = (byte)(crc & 0xFF);
            frame[8 + byteCount] = (byte)(crc >> 8);

            return frame;
        }

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

        public static string FormatTemperature(short value, int dot, string unit)
        {
            if (dot == 0)
                return $"{value}{unit}";
            else
                return $"{value / 10.0:F1}{unit}";
        }

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

        public string GetChannelStatusText(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > _totalChannels)
                return "유효하지 않은 채널";

            var channelStatus = _status.ChannelStatus[channelNumber - 1];

            if (!string.IsNullOrEmpty(channelStatus.SensorError))
                return $"센서 오류: {GetSensorErrorMessage(channelStatus.SensorError)}";

            // 확장 채널은 입력 전용
            if (channelStatus.IsExpansionChannel)
                return "입력 전용";

            if (channelStatus.IsAutoTuning)
                return "오토튜닝 진행 중";

            if (channelStatus.IsRampActive)
                return $"Ramp 진행 중 ({channelStatus.RampStatusText})";

            if (channelStatus.IsRunning)
            {
                float tolerance = channelStatus.Dot == 0 ? 3.0f : 0.3f;
                if (Math.Abs(channelStatus.PresentValue - channelStatus.SetValue) <= tolerance)
                    return "안정 상태";
                else if (channelStatus.PresentValue < channelStatus.SetValue)
                    return channelStatus.IsRampEnabled ? "승온 중 (Ramp)" : "승온 중";
                else
                    return channelStatus.IsRampEnabled ? "냉각 중 (Ramp)" : "냉각 중";
            }
            else
            {
                return "정지";
            }
        }

        #endregion
    }
}