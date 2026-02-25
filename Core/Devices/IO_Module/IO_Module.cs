using System;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.IO_Module.Enum;
using VacX_OutSense.Core.Devices.IO_Module.Models;

namespace VacX_OutSense.Core.Devices.IO_Module
{
    /// <summary>
    /// M31-AXAX8080G 마스터 + M31-XGXX0800G 확장 모듈
    /// 
    /// 마스터 (AXAX8080G): 8DI + 8DO
    ///   DI1: GV Close Reed Switch
    ///   DI2: GV Open Reed Switch
    ///   DI3~DI8: 예비
    ///   DO1: GV Solenoid Valve
    ///   DO2: VV Solenoid Valve
    ///   DO3: EV Solenoid Valve
    ///   DO4: IG HV (Ion Gauge High Voltage)
    ///   DO5~DO8: 예비
    /// 
    /// 확장 (XGXX0800G): 8AI 차동 전압
    ///   AI1~AI4: 압력 센서 (0-10V)
    ///   AI5: 추가 AI (±10V)
    ///   AI6~AI8: 예비
    /// 
    /// Modbus 레지스터 맵 (M31 매뉴얼 기준):
    ///   DI: Function 0x02, 주소 0x0000~0x0007 (Zone 1)
    ///   DO: Function 0x01/0x05/0x0F, 주소 0x0000~0x0007 (Zone 0)
    ///   AI: Function 0x04, 주소 0x0000~0x0007 (확장 모듈, 마스터에 AI 없으므로 0x0000부터 시작)
    ///   AI Float: Function 0x04, 주소 0x03E8~ (채널당 2 레지스터)
    /// </summary>
    public class IO_Module : DeviceBase
    {
        #region 상수

        // Modbus Function Codes
        private const byte FUNCTION_READ_COILS = 0x01;           // DO 상태 읽기
        private const byte FUNCTION_READ_DISCRETE_INPUTS = 0x02; // DI 상태 읽기
        private const byte FUNCTION_READ_HOLDING_REGISTERS = 0x03;
        private const byte FUNCTION_READ_INPUT_REGISTERS = 0x04; // AI 값 읽기
        private const byte FUNCTION_WRITE_SINGLE_COIL = 0x05;    // DO 단일 출력
        private const byte FUNCTION_WRITE_SINGLE_REGISTER = 0x06;
        private const byte FUNCTION_WRITE_MULTIPLE_COILS = 0x0F; // DO 다중 출력

        // 레지스터 주소 - M31 매뉴얼 Section 4.8 기준
        // DI/DO: 마스터 0x0000~0x0007
        // AI: 확장 모듈 (마스터에 AI 없으므로 Input Register 0x0000부터)
        private const ushort REG_ADDR_DI_START = 0x0000;
        private const ushort REG_ADDR_DO_START = 0x0000;
        private const ushort REG_ADDR_AI_START = 0x0000;  // 확장 모듈 AI Integer 값
        private const ushort REG_ADDR_AI_FLOAT = 0x03E8;  // AI Floating-point 시작 주소
        private const ushort REG_ADDR_AI_RANGE = 0x0DAC;  // AI 레인지 설정

        // DI/DO 채널 수
        private const int DI_CHANNEL_COUNT = 8;
        private const int DO_CHANNEL_COUNT = 8;
        private const int AI_CHANNEL_COUNT = 8; // 확장 모듈

        // 통신 설정
        private const int COMM_RETRY_COUNT = 3;
        private const int COMM_RETRY_DELAY = 50;
        private const int BUFFER_CLEAR_DELAY = 20;

        #endregion

        #region 필드

        private byte _slaveId;
        private readonly SemaphoreSlim _commSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastReadTime = DateTime.MinValue;
        private readonly TimeSpan _minReadInterval = TimeSpan.FromMilliseconds(50);

        // 캐시된 값
        private DigitalInputValues _lastValidDIValues;
        private DigitalOutputValues _lastValidDOValues;
        private AnalogInputValues _lastValidAIValues;

        // 추가 AI 채널 설정 (확장 모듈 기준, 1-based)
        private int _additionalAIChannel = 5;
        private VoltageRange _additionalAIRange = VoltageRange.Range_Neg10_Pos10V;

        #endregion

        #region 속성

        public override string DeviceName => "IO Module";
        public override string Model => "M31-AXAX8080G";
        public byte SlaveId { get => _slaveId; set => _slaveId = value; }

        public DigitalInputValues LastValidDIValues => _lastValidDIValues;
        public DigitalOutputValues LastValidDOValues => _lastValidDOValues;
        public AnalogInputValues LastValidAIValues => _lastValidAIValues;

        /// <summary>
        /// 게이트 밸브 Close 리드 스위치 상태 (DI1)
        /// </summary>
        public bool IsGateValveClosed => _lastValidDIValues?.ChannelStates[0] ?? false;

        /// <summary>
        /// 게이트 밸브 Open 리드 스위치 상태 (DI2)
        /// </summary>
        public bool IsGateValveOpened => _lastValidDIValues?.ChannelStates[1] ?? false;

        /// <summary>
        /// 게이트 밸브 위치 상태 문자열
        /// </summary>
        public string GateValvePosition
        {
            get
            {
                bool closed = IsGateValveClosed;
                bool opened = IsGateValveOpened;

                if (opened && !closed) return "Opened";
                if (!opened && closed) return "Closed";
                if (opened && closed) return "Error";  // 양쪽 센서 모두 ON
                return "Moving"; // 양쪽 센서 모두 OFF (이동 중)
            }
        }

        /// <summary>
        /// 추가 AI 채널 번호 (확장 모듈 기준 1~8)
        /// </summary>
        public int AdditionalAIChannel
        {
            get => _additionalAIChannel;
            set
            {
                if (value >= 1 && value <= 8)
                    _additionalAIChannel = value;
            }
        }

        /// <summary>
        /// 추가 AI 채널 인덱스 (0-based)
        /// </summary>
        public int AdditionalAIChannelIndex => _additionalAIChannel - 1;

        /// <summary>
        /// 추가 AI 레인지
        /// </summary>
        public VoltageRange AdditionalAIRange
        {
            get => _additionalAIRange;
            set => _additionalAIRange = value;
        }

        #endregion

        #region 생성자

        public IO_Module(ICommunicationManager communicationManager, byte slaveId = 1)
            : base(communicationManager)
        {
            _slaveId = slaveId;
            DeviceId = $"IOModule_{slaveId}";
        }

        #endregion

        #region 디지털 입력 읽기 (DI)

        /// <summary>
        /// 8채널 DI 상태 읽기 (Function Code 0x02)
        /// </summary>
        public async Task<DigitalInputValues> ReadDigitalInputsAsync()
        {
            await _commSemaphore.WaitAsync();
            try
            {
                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        byte[] request = CreateReadDIRequest();

                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("DI 읽기 요청 전송 실패");
                        }

                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        if (response == null || response.Length < 6)
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("DI 응답 데이터 부족");
                        }

                        var result = ParseDIResponse(response);
                        if (result != null)
                        {
                            _lastValidDIValues = result;
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"DI 읽기 오류: {ex.Message}");
                            return _lastValidDIValues;
                        }
                        await Task.Delay(COMM_RETRY_DELAY);
                    }
                }

                return _lastValidDIValues;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        #endregion

        #region 디지털 출력 읽기 (DO)

        /// <summary>
        /// 8채널 DO 상태 읽기 (Function Code 0x01)
        /// </summary>
        public async Task<DigitalOutputValues> ReadDigitalOutputsAsync()
        {
            await _commSemaphore.WaitAsync();
            try
            {
                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        byte[] request = CreateReadDORequest();

                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("DO 읽기 요청 전송 실패");
                        }

                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        if (response == null || response.Length < 6)
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("DO 응답 데이터 부족");
                        }

                        var result = ParseDOResponse(response);
                        if (result != null)
                        {
                            _lastValidDOValues = result;
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"DO 읽기 오류: {ex.Message}");
                            return _lastValidDOValues;
                        }
                        await Task.Delay(COMM_RETRY_DELAY);
                    }
                }

                return _lastValidDOValues;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        #endregion

        #region 디지털 출력 제어 (DO)

        /// <summary>
        /// 단일 DO 채널 제어 (Function Code 0x05)
        /// </summary>
        /// <param name="channel">채널 번호 (1-based: DO1=1, DO2=2, ...)</param>
        /// <param name="state">true=ON, false=OFF</param>
        public async Task<bool> SetDigitalOutputAsync(int channel, bool state)
        {
            if (channel < 1 || channel > DO_CHANNEL_COUNT)
            {
                OnErrorOccurred($"잘못된 DO 채널: {channel}");
                return false;
            }

            await _commSemaphore.WaitAsync();
            try
            {
                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        // Modbus 주소는 0-based
                        ushort address = (ushort)(REG_ADDR_DO_START + channel - 1);
                        byte[] request = CreateWriteSingleCoilRequest(address, state);

                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception($"DO{channel} 쓰기 요청 전송 실패");
                        }

                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        // 에코백 응답 검증
                        if (response != null && response.Length >= 8 &&
                            response[0] == _slaveId &&
                            response[1] == FUNCTION_WRITE_SINGLE_COIL)
                        {
                            ushort respAddress = (ushort)((response[2] << 8) | response[3]);
                            if (respAddress == address)
                                return true;
                        }

                        if (retry < COMM_RETRY_COUNT - 1)
                            await Task.Delay(COMM_RETRY_DELAY);
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"DO{channel} 쓰기 오류: {ex.Message}");
                            return false;
                        }
                        await Task.Delay(COMM_RETRY_DELAY);
                    }
                }

                return false;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        #endregion

        #region 밸브 제어 메서드

        /// <summary>GV Solenoid (DO1)</summary>
        public async Task<bool> ControlGateValveAsync(bool open) => await SetDigitalOutputAsync(1, open);

        /// <summary>VV Solenoid (DO2)</summary>
        public async Task<bool> ControlVentValveAsync(bool open) => await SetDigitalOutputAsync(2, open);

        /// <summary>EV Solenoid (DO3)</summary>
        public async Task<bool> ControlExhaustValveAsync(bool open) => await SetDigitalOutputAsync(3, open);

        /// <summary>IG HV (DO4)</summary>
        public async Task<bool> ControlIonGaugeHVAsync(bool on) => await SetDigitalOutputAsync(4, on);

        // 동기 버전 (기존 호환성)
        public bool ControlGateValve(bool open) => Task.Run(async () => await ControlGateValveAsync(open)).Result;
        public bool ControlVentValve(bool open) => Task.Run(async () => await ControlVentValveAsync(open)).Result;
        public bool ControlExhaustValve(bool open) => Task.Run(async () => await ControlExhaustValveAsync(open)).Result;
        public bool ControlIonGaugeHV(bool on) => Task.Run(async () => await ControlIonGaugeHVAsync(on)).Result;

        #endregion

        #region 아날로그 입력 읽기 (확장 모듈 AI)

        /// <summary>
        /// 확장 모듈 AI 읽기 (Function Code 0x04, Integer 방식)
        /// 마스터에 AI가 없으므로 Input Register 0x0000부터 확장 모듈 데이터 시작
        /// </summary>
        public async Task<AnalogInputValues> ReadAnalogInputsAsync()
        {
            var timeSinceLastRead = DateTime.Now - _lastReadTime;
            if (timeSinceLastRead < _minReadInterval)
            {
                await Task.Delay(_minReadInterval - timeSinceLastRead);
            }

            await _commSemaphore.WaitAsync();
            try
            {
                _lastReadTime = DateTime.Now;

                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        byte[] request = CreateReadAIRequest();

                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("AI 읽기 요청 전송 실패");
                        }

                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        // 응답 길이: 슬레이브ID(1) + FC(1) + 바이트수(1) + 데이터(8*2=16) + CRC(2) = 21
                        if (response == null || response.Length < 21)
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("AI 응답 데이터 부족");
                        }

                        var result = ParseAIResponse(response);

                        if (IsValidAIData(result))
                        {
                            _lastValidAIValues = result;
                            return result;
                        }
                        else if (_lastValidAIValues != null)
                        {
                            return _lastValidAIValues;
                        }

                        if (retry < COMM_RETRY_COUNT - 1)
                            await Task.Delay(COMM_RETRY_DELAY);
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"AI 읽기 오류: {ex.Message}");
                            return _lastValidAIValues;
                        }
                        await Task.Delay(COMM_RETRY_DELAY);
                    }
                }

                return _lastValidAIValues;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        /// <summary>
        /// 특정 AI 채널의 Floating-point 값 읽기 (고정밀)
        /// 매뉴얼: 시작 주소 0x03E8, 채널당 2 레지스터
        /// </summary>
        /// <param name="channel">확장 모듈 채널 (1~8)</param>
        public async Task<float?> ReadAIChannelFloatAsync(int channel)
        {
            if (channel < 1 || channel > AI_CHANNEL_COUNT) return null;

            await _commSemaphore.WaitAsync();
            try
            {
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();
                await Task.Delay(BUFFER_CLEAR_DELAY);

                // 마스터에 AI 없으므로 오프셋 = (channel - 1) * 2
                ushort regAddress = (ushort)(REG_ADDR_AI_FLOAT + (channel - 1) * 2);

                byte[] request = new byte[8];
                request[0] = _slaveId;
                request[1] = FUNCTION_READ_INPUT_REGISTERS;
                request[2] = (byte)((regAddress >> 8) & 0xFF);
                request[3] = (byte)(regAddress & 0xFF);
                request[4] = 0x00;
                request[5] = 0x02; // 2 registers per channel

                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                if (!_communicationManager.Write(request, 0, request.Length))
                    return null;

                await Task.Delay(50);
                byte[] response = _communicationManager.ReadAll();

                // 응답: ID(1) + FC(1) + ByteCount(1) + Data(4) + CRC(2) = 9
                if (response == null || response.Length < 9 ||
                    response[0] != _slaveId || response[1] != FUNCTION_READ_INPUT_REGISTERS)
                    return null;

                // Big-endian → Little-endian 변환 (IEEE 754)
                byte[] floatBytes = new byte[4];
                floatBytes[3] = response[3];
                floatBytes[2] = response[4];
                floatBytes[1] = response[5];
                floatBytes[0] = response[6];

                return BitConverter.ToSingle(floatBytes, 0);
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"AI Float 읽기 오류: {ex.Message}");
                return null;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        #endregion

        #region AI 레인지 설정

        /// <summary>
        /// 확장 모듈 개별 AI 채널 레인지 설정 (Function Code 0x06)
        /// </summary>
        /// <param name="channel">확장 모듈 채널 (1~8)</param>
        /// <param name="range">전압 레인지</param>
        public async Task<bool> SetExpansionAIRangeAsync(int channel, VoltageRange range)
        {
            if (channel < 1 || channel > AI_CHANNEL_COUNT) return false;

            await _commSemaphore.WaitAsync();
            try
            {
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();
                await Task.Delay(BUFFER_CLEAR_DELAY);

                // 마스터에 AI 없으므로 오프셋 = channel - 1
                ushort regAddress = (ushort)(REG_ADDR_AI_RANGE + (channel - 1));
                ushort rangeValue = (ushort)range;

                byte[] request = CreateWriteSingleRegisterRequest(regAddress, rangeValue);

                if (!_communicationManager.Write(request, 0, request.Length))
                    return false;

                await Task.Delay(50);
                byte[] response = _communicationManager.ReadAll();

                return response != null && response.Length >= 8 &&
                       response[0] == _slaveId &&
                       response[1] == FUNCTION_WRITE_SINGLE_REGISTER;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"AI 레인지 설정 오류: {ex.Message}");
                return false;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        /// <summary>
        /// 추가 AI 채널 초기화 (±10V 레인지 설정)
        /// </summary>
        public async Task<bool> InitializeAdditionalAIChannelAsync()
        {
            bool success = await SetExpansionAIRangeAsync(_additionalAIChannel, VoltageRange.Range_Neg10_Pos10V);
            if (success)
            {
                _additionalAIRange = VoltageRange.Range_Neg10_Pos10V;
            }
            return success;
        }

        #endregion

        #region Modbus 프레임 생성

        /// <summary>
        /// DI 읽기 요청 (Function Code 0x02, 8채널)
        /// </summary>
        private byte[] CreateReadDIRequest()
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_READ_DISCRETE_INPUTS;
            request[2] = (byte)((REG_ADDR_DI_START >> 8) & 0xFF);
            request[3] = (byte)(REG_ADDR_DI_START & 0xFF);
            request[4] = 0x00;
            request[5] = (byte)DI_CHANNEL_COUNT;

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        /// <summary>
        /// DO 읽기 요청 (Function Code 0x01, 8채널)
        /// </summary>
        private byte[] CreateReadDORequest()
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_READ_COILS;
            request[2] = (byte)((REG_ADDR_DO_START >> 8) & 0xFF);
            request[3] = (byte)(REG_ADDR_DO_START & 0xFF);
            request[4] = 0x00;
            request[5] = (byte)DO_CHANNEL_COUNT;

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        /// <summary>
        /// AI 읽기 요청 (Function Code 0x04, 확장 모듈 8채널)
        /// </summary>
        private byte[] CreateReadAIRequest()
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_READ_INPUT_REGISTERS;
            request[2] = (byte)((REG_ADDR_AI_START >> 8) & 0xFF);
            request[3] = (byte)(REG_ADDR_AI_START & 0xFF);
            request[4] = 0x00;
            request[5] = (byte)AI_CHANNEL_COUNT;

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        /// <summary>
        /// 단일 코일 쓰기 요청 (Function Code 0x05)
        /// </summary>
        private byte[] CreateWriteSingleCoilRequest(ushort address, bool state)
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_WRITE_SINGLE_COIL;
            request[2] = (byte)((address >> 8) & 0xFF);
            request[3] = (byte)(address & 0xFF);
            request[4] = state ? (byte)0xFF : (byte)0x00;
            request[5] = 0x00;

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        /// <summary>
        /// 단일 레지스터 쓰기 요청 (Function Code 0x06)
        /// </summary>
        private byte[] CreateWriteSingleRegisterRequest(ushort address, ushort value)
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_WRITE_SINGLE_REGISTER;
            request[2] = (byte)((address >> 8) & 0xFF);
            request[3] = (byte)(address & 0xFF);
            request[4] = (byte)((value >> 8) & 0xFF);
            request[5] = (byte)(value & 0xFF);

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        #endregion

        #region 응답 파싱

        /// <summary>
        /// DI 응답 파싱 - 비트 단위로 8채널 상태 추출
        /// 응답 포맷: [SlaveID][0x02][ByteCount][Data...][CRC_L][CRC_H]
        /// </summary>
        private DigitalInputValues ParseDIResponse(byte[] response)
        {
            if (response[0] != _slaveId || response[1] != FUNCTION_READ_DISCRETE_INPUTS)
                return null;

            var result = new DigitalInputValues();
            byte statusByte = response[3]; // 8채널이므로 1바이트

            for (int i = 0; i < DI_CHANNEL_COUNT; i++)
            {
                result.ChannelStates[i] = (statusByte & (1 << i)) != 0;
            }

            result.Timestamp = DateTime.Now;
            return result;
        }

        /// <summary>
        /// DO 응답 파싱 - 비트 단위로 8채널 상태 추출
        /// 응답 포맷: [SlaveID][0x01][ByteCount][Data...][CRC_L][CRC_H]
        /// </summary>
        private DigitalOutputValues ParseDOResponse(byte[] response)
        {
            if (response[0] != _slaveId || response[1] != FUNCTION_READ_COILS)
                return null;

            var result = new DigitalOutputValues();
            byte statusByte = response[3];

            for (int i = 0; i < DO_CHANNEL_COUNT; i++)
            {
                result.ChannelStates[i] = (statusByte & (1 << i)) != 0;
            }

            result.Timestamp = DateTime.Now;
            return result;
        }

        /// <summary>
        /// AI 응답 파싱 - 확장 모듈 8채널 전압 값 추출
        /// 응답 포맷: [SlaveID][0x04][ByteCount][Data(16bytes)][CRC_L][CRC_H]
        /// Integer 방식: rawValue / 1000.0 = 전압(V)
        /// </summary>
        private AnalogInputValues ParseAIResponse(byte[] response)
        {
            if (response[0] != _slaveId || response[1] != FUNCTION_READ_INPUT_REGISTERS)
                return null;

            var result = new AnalogInputValues
            {
                ExpansionVoltageRange = VoltageRange.Range_0_10V,
                AdditionalAIChannelIndex = AdditionalAIChannelIndex,
                AdditionalAIRange = _additionalAIRange,
                Timestamp = DateTime.Now
            };

            for (int i = 0; i < AI_CHANNEL_COUNT; i++)
            {
                int dataIndex = 3 + i * 2;
                int rawValue = (response[dataIndex] << 8) | response[dataIndex + 1];

                // 추가 AI 채널 (±10V) → signed 변환
                if (i == AdditionalAIChannelIndex)
                {
                    short signedValue = (short)rawValue;
                    result.ExpansionVoltageValues[i] = signedValue / 1000.0;
                }
                else
                {
                    // 기본 (0-10V) → unsigned
                    result.ExpansionVoltageValues[i] = rawValue / 1000.0;
                }
            }

            return result;
        }

        /// <summary>
        /// AI 데이터 유효성 검사 (최소 1개 채널이 0이 아닌 값)
        /// </summary>
        private bool IsValidAIData(AnalogInputValues data)
        {
            if (data == null) return false;

            for (int i = 0; i < AI_CHANNEL_COUNT; i++)
            {
                if (Math.Abs(data.ExpansionVoltageValues[i]) > 0.001)
                    return true;
            }

            return false;
        }

        #endregion

        #region CRC / 초기화 / 상태 확인

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

        protected override void InitializeAfterConnection()
        {
            Task.Run(async () =>
            {
                await Task.Delay(100);
                await CheckStatusAsync();

                // 추가 AI 채널 레인지 설정
                if (_additionalAIRange == VoltageRange.Range_Neg10_Pos10V)
                {
                    await InitializeAdditionalAIChannelAsync();
                }
            });
        }

        public override bool CheckStatus()
        {
            return Task.Run(async () => await CheckStatusAsync()).Result;
        }

        private async Task<bool> CheckStatusAsync()
        {
            try
            {
                // DI 읽기로 연결 상태 확인
                var diData = await ReadDigitalInputsAsync();
                return diData != null;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            _commSemaphore?.Dispose();
        }

        #endregion
    }
}