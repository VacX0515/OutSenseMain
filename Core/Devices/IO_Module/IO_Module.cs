using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.IO_Module.Enum;
using VacX_OutSense.Core.Devices.IO_Module.Models;

namespace VacX_OutSense.Core.Devices.IO_Module
{
    /// <summary>
    /// M31 시리즈 IO 모듈과 통신하기 위한 클래스
    /// DeviceBase 클래스를 상속하여 표준화된 장치 인터페이스를 제공합니다.
    /// </summary>
    public class IO_Module : DeviceBase
    {
        #region 상수 및 열거형

        // Modbus 함수 코드
        private const byte FUNCTION_READ_INPUT_REGISTERS = 0x04;
        private const byte FUNCTION_WRITE_MULTIPLE_REGISTERS = 0x10;

        // 레지스터 주소
        private const ushort REG_ADDR_AI_VALUES = 0x0000;         // AI 값 시작 주소
        private const ushort REG_ADDR_MASTER_RANGE = 0x0DAC;      // 마스터 모듈 AI 범위 시작 주소
        private const ushort REG_ADDR_EXPANSION_RANGE = 0x0DB0;   // 확장 모듈 AI 범위 시작 주소

        #endregion

        #region 필드 및 속성

        private byte _slaveId;
        private int _timeout = 1000;  // 통신 타임아웃(ms)
        private CurrentRange _masterCurrentRange = CurrentRange.Range_0_20mA;
        private VoltageRange _expansionVoltageRange = VoltageRange.Range_0_10V;
        private string _modelName;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "IO Module";

        /// <summary>
        /// 장치 모델
        /// </summary>
        public override string Model => _modelName;

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
        /// 마스터 모듈 전류 범위 설정
        /// </summary>
        public CurrentRange MasterCurrentRange
        {
            get => _masterCurrentRange;
            private set => _masterCurrentRange = value;
        }

        /// <summary>
        /// 확장 모듈 전압 범위 설정
        /// </summary>
        public VoltageRange ExpansionVoltageRange
        {
            get => _expansionVoltageRange;
            private set => _expansionVoltageRange = value;
        }

        #endregion

        #region 생성자

        /// <summary>
        /// M31IOModule 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="model">모듈 모델명</param>
        /// <param name="slaveId">슬레이브 ID</param>
        public IO_Module(ICommunicationManager communicationManager, string model = "M31-XAXA0404G-L", byte slaveId = 1)
            : base(communicationManager)
        {
            _modelName = model;
            _slaveId = slaveId;
            DeviceId = $"{model}_{slaveId}";
        }

        /// <summary>
        /// M31IOModule 클래스의 새 인스턴스를 초기화합니다.
        /// SerialManager 싱글톤 인스턴스를 사용합니다.
        /// </summary>
        /// <param name="model">모듈 모델명</param>
        /// <param name="slaveId">슬레이브 ID</param>
        public IO_Module(string model = "M31-XAXA0404G-L", byte slaveId = 1)
            : this(Communication.MultiPortSerialManager.Instance, model, slaveId)
        {
        }

        #endregion

        #region IDevice 구현

        /// <summary>
        /// 장치에 연결한 후 초기화 작업을 수행합니다.
        /// </summary>
        protected override void InitializeAfterConnection()
        {
            // 연결 후 초기 상태 가져오기 시도
            try
            {
                CheckStatus();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"장치 상태 확인 실패: {ex.Message}");
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
                // 레지스터 읽기 테스트로 장치 동작 상태 확인
                byte[] request = new byte[8];

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (04 = Input Registers 읽기)
                request[1] = FUNCTION_READ_INPUT_REGISTERS;

                // 시작 레지스터 주소 (AI 값은 0x0000부터 시작)
                request[2] = 0x00;
                request[3] = 0x00;

                // 레지스터 개수 (1개만 읽기)
                request[4] = 0x00;
                request[5] = 0x01;

                // CRC 계산
                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);        // 하위 바이트
                request[7] = (byte)((crc >> 8) & 0xFF); // 상위 바이트

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return false;
                }

                //// 응답 대기
                //Thread.Sleep(100);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 확인
                if (response != null && response.Length >= 5)
                {
                    if (response[0] == _slaveId && response[1] == FUNCTION_READ_INPUT_REGISTERS)
                    {
                        // 성공적으로 응답 받음
                        return true;
                    }
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

        #region 범위 설정 메서드

        /// <summary>
        /// 마스터 모듈의 아날로그 입력 전류 범위를 설정합니다.
        /// </summary>
        /// <param name="range">전류 범위</param>
        /// <returns>설정 성공 여부</returns>
        public bool SetMasterCurrentRange(CurrentRange range)
        {
            EnsureConnected();

            bool result = SetModuleAIRange(REG_ADDR_MASTER_RANGE, 4, (int)range);
            if (result)
            {
                MasterCurrentRange = range;
            }
            return result;
        }

        /// <summary>
        /// 확장 모듈의 아날로그 입력 전압 범위를 설정합니다.
        /// </summary>
        /// <param name="range">전압 범위</param>
        /// <returns>설정 성공 여부</returns>
        public bool SetExpansionVoltageRange(VoltageRange range)
        {
            EnsureConnected();

            bool result = SetModuleAIRange(REG_ADDR_EXPANSION_RANGE, 8, (int)range);
            if (result)
            {
                ExpansionVoltageRange = range;
            }
            return result;
        }

        /// <summary>
        /// 특정 모듈의 아날로그 입력 범위를 설정합니다.
        /// </summary>
        /// <param name="startRegister">시작 레지스터 주소</param>
        /// <param name="channelCount">채널 수</param>
        /// <param name="rangeValue">범위 값</param>
        /// <returns>설정 성공 여부</returns>
        private bool SetModuleAIRange(ushort startRegister, int channelCount, int rangeValue)
        {
            try
            {
                // Modbus RTU 프레임 구성
                byte[] request = new byte[7 + 2 * channelCount + 2]; // 헤더(7) + 데이터(채널당 2) + CRC(2)

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (16 = Multiple Holding Registers 쓰기)
                request[1] = FUNCTION_WRITE_MULTIPLE_REGISTERS;

                // 시작 레지스터 주소
                request[2] = (byte)((startRegister >> 8) & 0xFF);
                request[3] = (byte)(startRegister & 0xFF);

                // 레지스터 개수
                request[4] = 0x00;
                request[5] = (byte)channelCount;

                // 바이트 수
                request[6] = (byte)(channelCount * 2);

                // 각 채널의 AI 범위 값
                for (int i = 0; i < channelCount; i++)
                {
                    request[7 + i * 2] = 0x00; // 상위 바이트
                    request[8 + i * 2] = (byte)rangeValue; // 하위 바이트
                }

                // CRC 계산
                int dataLength = 7 + channelCount * 2;
                ushort crc = CalculateCRC(request, dataLength);
                request[dataLength] = (byte)(crc & 0xFF);         // 하위 바이트
                request[dataLength + 1] = (byte)((crc >> 8) & 0xFF);  // 상위 바이트

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return false;
                }

                //// 응답 대기
                //Thread.Sleep(100);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();

                // 응답 확인
                if (response != null && response.Length >= 8)
                {
                    if (response[0] == _slaveId && response[1] == FUNCTION_WRITE_MULTIPLE_REGISTERS)
                    {
                        // 성공적으로 설정됨
                        return true;
                    }
                    else if (response[0] == _slaveId && response[1] == (FUNCTION_WRITE_MULTIPLE_REGISTERS | 0x80))
                    {
                        // 오류 응답
                        string errorMsg = GetModbusErrorMessage(response[2]);
                        OnErrorOccurred($"Modbus 오류: {errorMsg}");
                        return false;
                    }
                }

                // 일부 장치에서는 응답이 없을 수 있음 (성공으로 간주)
                return true;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"범위 설정 오류: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 아날로그 입력 읽기 메서드

        /// <summary>
        /// 모든 아날로그 입력 값을 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>아날로그 입력 값 (성공 시) 또는 null (실패 시)</returns>
        public async Task<AnalogInputValues> ReadAnalogInputsAsync()
        {
            return await Task.Run(() => ReadAnalogInputs());
        }

        /// <summary>
        /// 모든 아날로그 입력 값을 읽습니다.
        /// </summary>
        /// <returns>아날로그 입력 값 (성공 시) 또는 null (실패 시)</returns>
        public AnalogInputValues ReadAnalogInputs()
        {
            EnsureConnected();

            try
            {
                // Modbus RTU 프레임 구성
                byte[] request = new byte[8];

                // 슬레이브 ID
                request[0] = _slaveId;

                // 함수 코드 (04 = Input Registers 읽기)
                request[1] = FUNCTION_READ_INPUT_REGISTERS;

                // 시작 레지스터 주소 (AI 값은 0x0000부터 시작)
                request[2] = 0x00;
                request[3] = 0x00;

                // 레지스터 개수 (12개 AI 채널 - 마스터 4개 + 확장 8개)
                request[4] = 0x00;
                request[5] = 0x0C;

                // CRC 계산
                ushort crc = CalculateCRC(request, 6);
                request[6] = (byte)(crc & 0xFF);        // 하위 바이트
                request[7] = (byte)((crc >> 8) & 0xFF); // 상위 바이트

                // 요청 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                // 요청 전송
                bool writeResult = _communicationManager.Write(request, 0, request.Length);
                if (!writeResult)
                {
                    OnErrorOccurred("데이터 전송 실패");
                    return null;
                }

                //// 응답 대기
                //Thread.Sleep(100);

                // 응답 읽기
                byte[] responseBytes = _communicationManager.ReadAll();
                if (responseBytes == null)
                {
                    //OnErrorOccurred("응답 읽기 실패");
                    return null;
                }

                List<byte> responseBuffer = new List<byte>(responseBytes);

                // 응답 데이터 처리
                if (responseBuffer.Count >= 29) // 슬레이브ID(1) + 함수코드(1) + 바이트 카운트(1) + 12채널 * 2바이트 + CRC(2)
                {
                    // 함수 코드 확인
                    if (responseBuffer[1] == FUNCTION_READ_INPUT_REGISTERS)
                    {
                        int byteCount = responseBuffer[2];

                        // 결과 객체 생성
                        AnalogInputValues result = new AnalogInputValues
                        {
                            MasterCurrentRange = _masterCurrentRange,
                            ExpansionVoltageRange = _expansionVoltageRange
                        };

                        // 마스터 모듈 값 (전류, 채널 0-3)
                        for (int i = 0; i < 4; i++)
                        {
                            int dataIndex = 3 + i * 2;
                            int rawValue = (responseBuffer[dataIndex] << 8) | responseBuffer[dataIndex + 1];
                            double current = rawValue / 1000.0; // μA -> mA 변환
                            result.MasterCurrentValues[i] = current;
                        }

                        // 확장 모듈 값 (전압, 채널 4-11)
                        for (int i = 4; i < 12; i++)
                        {
                            int dataIndex = 3 + i * 2;
                            int rawValue = (responseBuffer[dataIndex] << 8) | responseBuffer[dataIndex + 1];

                            // 양방향 범위에서 음수 처리
                            if ((_expansionVoltageRange == VoltageRange.Range_Neg5_Pos5V ||
                                 _expansionVoltageRange == VoltageRange.Range_Neg10_Pos10V) &&
                                (rawValue & 0x8000) != 0)
                            {
                                rawValue = -(65536 - rawValue);
                            }

                            double voltage = rawValue / 1000.0; // mV -> V 변환
                            result.ExpansionVoltageValues[i - 4] = voltage;
                        }

                        return result;
                    }
                    else if (responseBuffer[1] == (FUNCTION_READ_INPUT_REGISTERS | 0x80)) // 오류 응답
                    {
                        string errorMsg = GetModbusErrorMessage(responseBuffer[2]);
                        OnErrorOccurred($"Modbus 오류: {errorMsg}");
                        return null;
                    }
                }

                OnErrorOccurred("응답 데이터가 부족합니다");
                return null;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"입력 읽기 오류: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 유틸리티 메서드

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
}