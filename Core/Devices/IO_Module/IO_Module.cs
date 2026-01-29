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
    /// 개선된 IO 모듈 - 통신 안정성 향상 + ±10V 추가 AI 지원
    /// 마스터: M31-XAXA0404G (4AI 전류 + 4AO)
    /// 확장: M31-XGXX0800G (8AI 차동 전압)
    /// </summary>
    public class IO_Module : DeviceBase
    {
        #region 상수 및 열거형

        private const byte FUNCTION_READ_INPUT_REGISTERS = 0x04;
        private const byte FUNCTION_READ_HOLDING_REGISTERS = 0x03;
        private const byte FUNCTION_WRITE_SINGLE_REGISTER = 0x06;
        private const byte FUNCTION_WRITE_MULTIPLE_REGISTERS = 0x10;

        private const ushort REG_ADDR_AI_VALUES = 0x0000;
        private const ushort REG_ADDR_AO_VALUES = 0x0000;
        private const ushort REG_ADDR_AI_RANGE = 0x0DAC;  // AI 레인지 설정 레지스터

        // 통신 설정
        private const int COMM_RETRY_COUNT = 3;
        private const int COMM_RETRY_DELAY = 50; // ms
        private const int BUFFER_CLEAR_DELAY = 20; // ms

        #endregion

        #region 필드 및 속성

        private byte _slaveId;
        private readonly SemaphoreSlim _commSemaphore = new SemaphoreSlim(1, 1);
        private DateTime _lastReadTime = DateTime.MinValue;
        private readonly TimeSpan _minReadInterval = TimeSpan.FromMilliseconds(50);

        // 캐시된 값 (0값 방지용)
        private AnalogInputValues _lastValidAIValues;
        private AnalogOutputValues _lastValidAOValues;

        public AnalogInputValues LastValidAIValues => _lastValidAIValues;
        public AnalogOutputValues LastValidAOValues => _lastValidAOValues;

        public override string DeviceName => "IO Module";
        public override string Model => "M31-XAXA0404G-L";
        public byte SlaveId { get => _slaveId; set => _slaveId = value; }

        #endregion

        #region Floating-point AI 읽기 (고정밀)

        private const ushort REG_ADDR_AI_FLOAT = 0x03E8;  // Floating-point 시작 주소

        /// <summary>
        /// 특정 채널의 AI 값을 Floating-point로 읽기 (내부 호출용 - 세마포어 없음)
        /// </summary>
        public async Task<double?> ReadAIChannelFloatAsync(int channel)
        {
            if (channel < 1 || channel > 8) return null;

            // ★ 세마포어 제거 (호출부에서 이미 락 보유)
            try
            {
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();
                await Task.Delay(BUFFER_CLEAR_DELAY);

                int registerOffset = (4 + channel - 1) * 2;
                ushort regAddress = (ushort)(REG_ADDR_AI_FLOAT + registerOffset);

                byte[] request = new byte[8];
                request[0] = _slaveId;
                request[1] = FUNCTION_READ_INPUT_REGISTERS;
                request[2] = (byte)((regAddress >> 8) & 0xFF);
                request[3] = (byte)(regAddress & 0xFF);
                request[4] = 0x00;
                request[5] = 0x02;

                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);
                request[7] = (byte)((crc >> 8) & 0xFF);

                if (!_communicationManager.Write(request, 0, request.Length))
                    return null;

                await Task.Delay(50);
                byte[] response = _communicationManager.ReadAll();

                if (response == null || response.Length < 9)
                    return null;

                if (response[1] != FUNCTION_READ_INPUT_REGISTERS)
                    return null;

                // IEEE754 변환
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
        }

        #endregion

        #region 추가 AI 채널 설정 (±10V)

        // 추가 AI 채널 설정 (확장 모듈 기준, 1-based)
        // 확장 모듈 채널 1-4: 압력 센서 (0-10V)
        // 확장 모듈 채널 5: 추가 AI (±10V)
        private int _additionalAIChannel = 5;  // 확장 모듈 채널 5 (ExpansionVoltageValues[4])
        private VoltageRange _additionalAIRange = VoltageRange.Range_Neg10_Pos10V;

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
        /// 추가 AI 레인지
        /// </summary>
        public VoltageRange AdditionalAIRange
        {
            get => _additionalAIRange;
            set => _additionalAIRange = value;
        }

        /// <summary>
        /// 추가 AI 채널 인덱스 (0-based, ExpansionVoltageValues 배열용)
        /// </summary>
        public int AdditionalAIChannelIndex => _additionalAIChannel - 1;

        #endregion

        #region 생성자

        public IO_Module(ICommunicationManager communicationManager, byte slaveId = 1)
            : base(communicationManager)
        {
            _slaveId = slaveId;
            DeviceId = $"IOModule_{slaveId}";
        }

        #endregion

        #region AI 레인지 설정

        /// <summary>
        /// 개별 AI 채널 레인지 설정 (Function Code 0x06)
        /// 확장 모듈의 AI 채널 레인지를 변경합니다.
        /// </summary>
        /// <param name="channel">확장 모듈 채널 번호 (1~8)</param>
        /// <param name="range">전압 레인지</param>
        public async Task<bool> SetExpansionAIRangeAsync(int channel, VoltageRange range)
        {
            if (channel < 1 || channel > 8)
            {
                OnErrorOccurred($"AI 채널 범위 오류: {channel} (1~8 허용)");
                return false;
            }

            await _commSemaphore.WaitAsync();
            try
            {
                _communicationManager.DiscardInBuffer();
                _communicationManager.DiscardOutBuffer();
                await Task.Delay(BUFFER_CLEAR_DELAY);

                // M31 레인지 설정값 변환
                ushort rangeValue = range switch
                {
                    VoltageRange.Range_0_10V => 5,         // 0-10V
                    VoltageRange.Range_Neg10_Pos10V => 6,  // ±10V
                    VoltageRange.Range_Neg5_Pos5V => 4,    // ±5V
                    _ => 5  // 기본값 0-10V
                };

                // 확장 모듈 AI 레인지 레지스터 주소
                // 마스터 AI 4채널 이후 확장 모듈 시작
                ushort regAddress = (ushort)(REG_ADDR_AI_RANGE + 4 + (channel - 1));
                byte[] request = CreateWriteSingleRegisterRequest(regAddress, rangeValue);

                if (!_communicationManager.Write(request, 0, request.Length))
                {
                    OnErrorOccurred("AI 레인지 설정 요청 전송 실패");
                    return false;
                }

                await Task.Delay(50);
                byte[] response = _communicationManager.ReadAll();

                if (response != null && response.Length >= 8 && response[1] == FUNCTION_WRITE_SINGLE_REGISTER)
                {
                    return true;
                }

                return false;
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

        #region 개선된 통신 메서드

        /// <summary>
        /// 아날로그 입력 읽기 (개선된 버전 + ±10V 지원)
        /// </summary>
        public async Task<AnalogInputValues> ReadAnalogInputsAsync()
        {
            // 최소 읽기 간격 체크
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
                        // 버퍼 클리어
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        // Modbus 요청 생성
                        byte[] request = CreateReadAIRequest();

                        // 전송
                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("데이터 전송 실패");
                        }

                        // 응답 대기 및 읽기
                        await Task.Delay(50); // 응답 대기
                        byte[] response = _communicationManager.ReadAll();

                        if (response == null || response.Length < 29)
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("응답 데이터 부족");
                        }

                        // 응답 파싱 (±10V 변환 포함)
                        var result = ParseAIResponse(response);

                        // 유효성 검사 (0값 체크)
                        if (IsValidAIData(result))
                        {
                            _lastValidAIValues = result;
                            return result;
                        }
                        else if (_lastValidAIValues != null)
                        {
                            // 무효한 데이터면 마지막 유효 데이터 반환
                            return _lastValidAIValues;
                        }

                        if (retry < COMM_RETRY_COUNT - 1)
                        {
                            await Task.Delay(COMM_RETRY_DELAY);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"AI 읽기 오류: {ex.Message}");
                            return _lastValidAIValues; // 마지막 유효값 반환
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
        /// 아날로그 출력 읽기 (개선된 버전)
        /// </summary>
        public async Task<AnalogOutputValues> ReadAnalogOutputsAsync()
        {
            await _commSemaphore.WaitAsync();
            try
            {
                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        // 버퍼 클리어
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        // Modbus 요청 생성
                        byte[] request = CreateReadAORequest();

                        // 전송
                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("데이터 전송 실패");
                        }

                        // 응답 대기 및 읽기
                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        if (response == null || response.Length < 13)
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            throw new Exception("응답 데이터 부족");
                        }

                        // 응답 파싱
                        var result = ParseAOResponse(response);

                        if (result != null)
                        {
                            _lastValidAOValues = result;
                            return result;
                        }

                        if (retry < COMM_RETRY_COUNT - 1)
                        {
                            await Task.Delay(COMM_RETRY_DELAY);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"AO 읽기 오류: {ex.Message}");
                            return _lastValidAOValues;
                        }
                        await Task.Delay(COMM_RETRY_DELAY);
                    }
                }

                return _lastValidAOValues;
            }
            finally
            {
                _commSemaphore.Release();
            }
        }

        /// <summary>
        /// 디지털 출력 설정 (개선된 버전)
        /// </summary>
        public async Task<bool> SetDigitalOutputAsync(int channel, bool isOn)
        {
            if (channel < 1 || channel > 4) return false;

            await _commSemaphore.WaitAsync();
            try
            {
                double currentValue = isOn ? 20.0 : 0.0;

                for (int retry = 0; retry < COMM_RETRY_COUNT; retry++)
                {
                    try
                    {
                        // 버퍼 클리어
                        _communicationManager.DiscardInBuffer();
                        _communicationManager.DiscardOutBuffer();
                        await Task.Delay(BUFFER_CLEAR_DELAY);

                        // Modbus 요청 생성
                        byte[] request = CreateWriteSingleAORequest(channel, currentValue);

                        // 전송
                        if (!_communicationManager.Write(request, 0, request.Length))
                        {
                            if (retry < COMM_RETRY_COUNT - 1)
                            {
                                await Task.Delay(COMM_RETRY_DELAY);
                                continue;
                            }
                            return false;
                        }

                        // 응답 대기
                        await Task.Delay(50);
                        byte[] response = _communicationManager.ReadAll();

                        // 응답 확인 (일부 장치는 응답이 없을 수 있음)
                        if (response != null && response.Length >= 8)
                        {
                            if (response[1] == FUNCTION_WRITE_SINGLE_REGISTER)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // 응답이 없어도 성공으로 간주
                            return true;
                        }

                        if (retry < COMM_RETRY_COUNT - 1)
                        {
                            await Task.Delay(COMM_RETRY_DELAY);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (retry == COMM_RETRY_COUNT - 1)
                        {
                            OnErrorOccurred($"디지털 출력 설정 오류: {ex.Message}");
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

        #region 밸브 제어 메서드 (비동기)

        public async Task<bool> ControlGateValveAsync(bool open)
        {
            return await SetDigitalOutputAsync(1, open);
        }

        public async Task<bool> ControlVentValveAsync(bool open)
        {
            return await SetDigitalOutputAsync(2, open);
        }

        public async Task<bool> ControlExhaustValveAsync(bool open)
        {
            return await SetDigitalOutputAsync(3, open);
        }

        public async Task<bool> ControlIonGaugeHVAsync(bool on)
        {
            return await SetDigitalOutputAsync(4, on);
        }

        // 동기 버전 (기존 호환성)
        public bool ControlGateValve(bool open)
        {
            return Task.Run(async () => await ControlGateValveAsync(open)).Result;
        }

        public bool ControlVentValve(bool open)
        {
            return Task.Run(async () => await ControlVentValveAsync(open)).Result;
        }

        public bool ControlExhaustValve(bool open)
        {
            return Task.Run(async () => await ControlExhaustValveAsync(open)).Result;
        }

        public bool ControlIonGaugeHV(bool on)
        {
            return Task.Run(async () => await ControlIonGaugeHVAsync(on)).Result;
        }

        #endregion

        #region 헬퍼 메서드

        private byte[] CreateReadAIRequest()
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_READ_INPUT_REGISTERS;
            request[2] = 0x00;
            request[3] = 0x00;
            request[4] = 0x00;
            request[5] = 0x0C; // 12 registers (마스터 4 + 확장 8)

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        private byte[] CreateReadAORequest()
        {
            byte[] request = new byte[8];
            request[0] = _slaveId;
            request[1] = FUNCTION_READ_HOLDING_REGISTERS;
            request[2] = 0x00;
            request[3] = 0x00;
            request[4] = 0x00;
            request[5] = 0x04; // 4 registers

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        private byte[] CreateWriteSingleAORequest(int channel, double currentValue)
        {
            byte[] request = new byte[8];
            int rawValue = (int)(currentValue * 1000);
            ushort regAddress = (ushort)(REG_ADDR_AO_VALUES + (channel - 1));

            request[0] = _slaveId;
            request[1] = FUNCTION_WRITE_SINGLE_REGISTER;
            request[2] = (byte)((regAddress >> 8) & 0xFF);
            request[3] = (byte)(regAddress & 0xFF);
            request[4] = (byte)((rawValue >> 8) & 0xFF);
            request[5] = (byte)(rawValue & 0xFF);

            ushort crc = CalculateCRC(request, 6);
            request[6] = (byte)(crc & 0xFF);
            request[7] = (byte)((crc >> 8) & 0xFF);

            return request;
        }

        /// <summary>
        /// AI 응답 파싱 - ±10V 추가 AI 채널 지원
        /// </summary>
        private AnalogInputValues ParseAIResponse(byte[] response)
        {
            if (response[1] != FUNCTION_READ_INPUT_REGISTERS)
                return null;

            var result = new AnalogInputValues
            {
                MasterCurrentRange = CurrentRange.Range_0_20mA,
                ExpansionVoltageRange = VoltageRange.Range_0_10V,
                AdditionalAIChannelIndex = AdditionalAIChannelIndex,
                AdditionalAIRange = _additionalAIRange,
                Timestamp = DateTime.Now
            };

            // 마스터 모듈 값 (전류, 채널 0-3)
            for (int i = 0; i < 4; i++)
            {
                int dataIndex = 3 + i * 2;
                int rawValue = (response[dataIndex] << 8) | response[dataIndex + 1];
                result.MasterCurrentValues[i] = rawValue / 1000.0;
            }

            // 확장 모듈 값 (전압, 채널 4-11 → ExpansionVoltageValues[0-7])
            for (int i = 4; i < 12; i++)
            {
                int dataIndex = 3 + i * 2;
                int rawValue = (response[dataIndex] << 8) | response[dataIndex + 1];
                int expansionIndex = i - 4;

                // 추가 AI 채널인 경우 ±10V 변환 적용
                if (expansionIndex == AdditionalAIChannel - 1)
                {
                    short signedValue = (short)rawValue;
                    result.ExpansionVoltageValues[expansionIndex] = signedValue / 1000.0;
                }
                else
                {
                    // 기본 채널 (0-10V)
                    result.ExpansionVoltageValues[expansionIndex] = ConvertToVoltage(rawValue, VoltageRange.Range_0_10V);
                }
            }

            return result;
        }

        /// <summary>
        /// Raw 값을 전압으로 변환
        /// M31 시리즈: Process quantity 레지스터 사용 시 -27648 ~ +27648
        /// 일반 레지스터 사용 시 0~20000 (uV) 또는 직접 변환
        /// </summary>
        private double ConvertToVoltage(int rawValue, VoltageRange range)
        {
            switch (range)
            {
                case VoltageRange.Range_0_10V:
                    // 기존 방식 유지 (rawValue / 1000.0)
                    // 또는 M31 스펙에 맞게: rawValue * 10.0 / 27648.0
                    return rawValue / 1000.0;

                case VoltageRange.Range_Neg10_Pos10V:
                    // ±10V: 부호 있는 값으로 변환
                    // rawValue가 unsigned로 들어오면 signed로 변환
                    short signedValue = (short)rawValue;
                    // M31 Process quantity: -27648 ~ +27648 → -10V ~ +10V
return signedValue / 1000.0;

                case VoltageRange.Range_Neg5_Pos5V:
                    // ±5V
                    short signed5V = (short)rawValue;
                    return signed5V * 5.0 / 27648.0;

                default:
                    return rawValue / 1000.0;
            }
        }

        private AnalogOutputValues ParseAOResponse(byte[] response)
        {
            if (response[1] != FUNCTION_READ_HOLDING_REGISTERS)
                return null;

            var result = new AnalogOutputValues
            {
                OutputRange = CurrentRange.Range_0_20mA
            };

            for (int i = 0; i < 4; i++)
            {
                int dataIndex = 3 + i * 2;
                int rawValue = (response[dataIndex] << 8) | response[dataIndex + 1];
                result.CurrentValues[i] = rawValue / 1000.0;
            }

            return result;
        }

        private bool IsValidAIData(AnalogInputValues data)
        {
            if (data == null) return false;

            // 최소한 하나의 채널은 0이 아닌 값을 가져야 함
            bool hasNonZeroValue = false;

            // 전류 채널 확인
            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(data.MasterCurrentValues[i]) > 0.001)
                {
                    hasNonZeroValue = true;
                    break;
                }
            }

            // 전압 채널 확인 (특히 압력 센서 채널)
            if (!hasNonZeroValue)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (Math.Abs(data.ExpansionVoltageValues[i]) > 0.001)
                    {
                        hasNonZeroValue = true;
                        break;
                    }
                }
            }

            return hasNonZeroValue;
        }

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
            // 연결 후 초기화
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
                var aiData = await ReadAnalogInputsAsync();
                return aiData != null;
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