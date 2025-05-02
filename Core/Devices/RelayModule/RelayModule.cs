using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;
using VacX_OutSense.Core.Devices.Base;
using VacX_OutSense.Core.Devices.Relay_Module.Enum;
using VacX_OutSense.Core.Devices.Relay_Module.Models;

namespace VacX_OutSense.Core.Devices.Relay_Module
{
    /// <summary>
    /// RS232 릴레이 모듈을 제어하기 위한 클래스
    /// </summary>
    public class RelayModule : DeviceBase
    {
        #region 상수 및 프로토콜 정의

        // 프레임 헤더 및 종료자
        private const byte FRAME_HEADER_1 = 0x55;
        private const byte FRAME_HEADER_2 = 0x56;
        private const byte FRAME_END = 0xAA;

        // 응답 헤더
        private const byte RESPONSE_HEADER_1 = 0x33;
        private const byte RESPONSE_HEADER_2 = 0x3C;

        // 타임아웃 및 재시도 설정
        private const int DEFAULT_TIMEOUT = 1000;
        private const int DEFAULT_RETRY_COUNT = 3;
        private const int RESPONSE_WAIT_MS = 20;

        // 모든 릴레이 상태 읽기 명령어
        private const byte READ_ALL_RELAY_STATES = 0x07;

        #endregion

        #region 필드 및 속성

        private string _modelName;
        private int _timeout = DEFAULT_TIMEOUT;
        private int _retryCount = DEFAULT_RETRY_COUNT;
        private RelayModuleValues _currentValues;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public override string DeviceName => "Relay Module";

        /// <summary>
        /// 장치 모델
        /// </summary>
        public override string Model => _modelName;

        /// <summary>
        /// 타임아웃(ms)
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
        /// 명령 재시도 횟수
        /// </summary>
        public int RetryCount
        {
            get => _retryCount;
            set => _retryCount = Math.Max(1, value);
        }

        /// <summary>
        /// 현재 릴레이 모듈 상태
        /// </summary>
        public RelayModuleValues CurrentValues => _currentValues;

        #endregion

        #region 생성자

        /// <summary>
        /// RelayModule 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        /// <param name="model">모듈 모델명</param>
        public RelayModule(ICommunicationManager communicationManager, string model = "RS232-Relay")
            : base(communicationManager)
        {
            _modelName = model;
            DeviceId = $"{model}";
            _currentValues = new RelayModuleValues();
        }

        /// <summary>
        /// RelayModule 클래스의 새 인스턴스를 초기화합니다.
        /// SerialManager 싱글톤 인스턴스를 사용합니다.
        /// </summary>
        /// <param name="model">모듈 모델명</param>
        public RelayModule(string model = "RS232-Relay")
            : this(MultiPortSerialManager.Instance, model)
        {
        }

        #endregion

        #region IDevice 구현

        /// <summary>
        /// 장치에 연결한 후 초기화 작업을 수행합니다.
        /// </summary>
        protected override void InitializeAfterConnection()
        {
            // 연결 후 초기 상태 가져오기
            try
            {
                // 현재 모든 릴레이 상태 읽기
                ReadAllRelayStates();
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
                // 모든 릴레이 상태를 읽어서 장치 동작 확인
                return ReadAllRelayStates();
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"상태 확인 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 릴레이 제어 메서드

        /// <summary>
        /// 지정된 채널의 릴레이를 켭니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>성공 여부</returns>
        public bool TurnOnRelay(int channel)
        {
            if (channel < 1 || channel > 8)
                throw new ArgumentOutOfRangeException(nameof(channel), "채널은 1에서 8 사이여야 합니다.");

            EnsureConnected();

            try
            {
                byte[] response = SendCommand(RelayCommand.RelayOn, channel);
                if (response != null)
                {
                    // 응답 처리 및 상태 업데이트
                    _currentValues.UpdateFromResponse(response);
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"릴레이 켜기 실패 (채널 {channel}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 릴레이를 끕니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>성공 여부</returns>
        public bool TurnOffRelay(int channel)
        {
            if (channel < 1 || channel > 8)
                throw new ArgumentOutOfRangeException(nameof(channel), "채널은 1에서 8 사이여야 합니다.");

            EnsureConnected();

            try
            {
                byte[] response = SendCommand(RelayCommand.RelayOff, channel);
                if (response != null)
                {
                    // 응답 처리 및 상태 업데이트
                    _currentValues.UpdateFromResponse(response);
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"릴레이 끄기 실패 (채널 {channel}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 릴레이 상태를 토글합니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>성공 여부</returns>
        public bool ToggleRelay(int channel)
        {
            if (channel < 1 || channel > 8)
                throw new ArgumentOutOfRangeException(nameof(channel), "채널은 1에서 8 사이여야 합니다.");

            EnsureConnected();

            try
            {
                byte[] response = SendCommand(RelayCommand.RelayToggle, channel);
                if (response != null)
                {
                    // 응답 처리 및 상태 업데이트
                    _currentValues.UpdateFromResponse(response);
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"릴레이 토글 실패 (채널 {channel}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 지정된 채널의 릴레이를 모멘터리 모드로 작동시킵니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>성공 여부</returns>
        public bool PulseRelay(int channel)
        {
            if (channel < 1 || channel > 8)
                throw new ArgumentOutOfRangeException(nameof(channel), "채널은 1에서 8 사이여야 합니다.");

            EnsureConnected();

            try
            {
                byte[] response = SendCommand(RelayCommand.RelayMomentary, channel);
                if (response != null)
                {
                    // 응답 처리 및 상태 업데이트
                    _currentValues.UpdateFromResponse(response);
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"릴레이 펄스 실패 (채널 {channel}): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 모든 릴레이를 켭니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool TurnOnAllRelays()
        {
            EnsureConnected();

            try
            {
                // 모든 릴레이 켜기 명령
                byte[] command = new byte[8];
                command[0] = FRAME_HEADER_1;
                command[1] = FRAME_HEADER_2;
                command[2] = 0x00;
                command[3] = 0x00;
                command[4] = 0x00;
                command[5] = 0xFF; // 모든 비트 설정
                command[6] = (byte)RelayCommand.RelayAll;
                command[7] = FRAME_END;

                bool result = SendRawCommand(command);
                if (result)
                {
                    // 상태 업데이트
                    for (int i = 0; i < 8; i++)
                    {
                        _currentValues.SetRelayState(i, true);
                    }
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"모든 릴레이 켜기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 모든 릴레이를 끕니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool TurnOffAllRelays()
        {
            EnsureConnected();

            try
            {
                // 모든 릴레이 끄기 명령
                byte[] command = new byte[8];
                command[0] = FRAME_HEADER_1;
                command[1] = FRAME_HEADER_2;
                command[2] = 0x00;
                command[3] = 0x00;
                command[4] = 0x00;
                command[5] = 0x00; // 모든 비트 해제
                command[6] = (byte)RelayCommand.RelayAll;
                command[7] = FRAME_END;

                bool result = SendRawCommand(command);
                if (result)
                {
                    // 상태 업데이트
                    for (int i = 0; i < 8; i++)
                    {
                        _currentValues.SetRelayState(i, false);
                    }
                    OnPropertyChanged(nameof(CurrentValues));
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"모든 릴레이 끄기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 모든 릴레이의 상태를 한 번에 읽습니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool ReadAllRelayStates()
        {
            EnsureConnected();

            try
            {
                // 모든 릴레이 상태 읽기 명령 (55 56 00 00 00 00 07 AA)
                byte[] command = new byte[8];
                command[0] = FRAME_HEADER_1;  // 0x55
                command[1] = FRAME_HEADER_2;  // 0x56
                command[2] = 0x00;
                command[3] = 0x00;
                command[4] = 0x00;
                command[5] = 0x00;
                command[6] = READ_ALL_RELAY_STATES;  // 0x07
                command[7] = FRAME_END;  // 0xAA

                // 명령 전송 전 버퍼 비우기
                _communicationManager.DiscardInBuffer();

                bool writeResult = _communicationManager.Write(command);
                if (!writeResult)
                {
                    OnErrorOccurred("모든 릴레이 상태 읽기 명령 전송 실패");
                    return false;
                }

                //// 응답 대기
                //Thread.Sleep(RESPONSE_WAIT_MS);

                // 응답 읽기
                byte[] response = _communicationManager.ReadAll();
                if (response != null && response.Length >= 8)
                {
                    // 응답 헤더 확인
                    if (response[0] == RESPONSE_HEADER_1 && response[1] == RESPONSE_HEADER_2)
                    {
                        // 응답의 6번째 바이트에 모든 릴레이 상태 정보가 비트맵으로 포함됨
                        byte relayStatus = response[5]; // 릴레이 상태가 있는 바이트 (33 3C 00 00 00 XX 00 AA의 XX 부분)

                        // 각 비트 위치에 해당하는 릴레이 상태 설정
                        for (int i = 0; i < 8; i++)
                        {
                            bool isOn = ((relayStatus >> i) & 0x01) == 0x01;
                            _currentValues.SetRelayState(i, isOn);
                        }

                        OnPropertyChanged(nameof(CurrentValues));
                        return true;
                    }
                }

                OnErrorOccurred("모든 릴레이 상태 읽기에 대한 유효한 응답을 받지 못했습니다.");
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"모든 릴레이 상태 읽기 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 모든 릴레이의 상태를 비동기적으로 읽습니다.
        /// </summary>
        /// <returns>릴레이 모듈 값 객체 (성공 시) 또는 null (실패 시)</returns>
        public async Task<RelayModuleValues> ReadAllRelayStatesAsync()
        {
            return await Task.Run(() => {
                bool success = ReadAllRelayStates();
                return success ? _currentValues : null;
            });
        }

        /// <summary>
        /// 지정된 채널의 릴레이 상태를 읽습니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>성공 여부</returns>
        public bool ReadRelayState(int channel)
        {
            if (channel < 1 || channel > 8)
                throw new ArgumentOutOfRangeException(nameof(channel), "채널은 1에서 8 사이여야 합니다.");

            EnsureConnected();

            try
            {
                // 개별 채널 상태를 읽는 대신 모든 릴레이 상태를 읽고 해당 채널 상태만 반환
                bool success = ReadAllRelayStates();
                if (success)
                {
                    return _currentValues.GetRelayState(channel - 1); // 0-based index로 변환
                }
                return false;
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"릴레이 상태 읽기 실패 (채널 {channel}): {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 통신 메서드

        /// <summary>
        /// 지정된 명령과 채널에 대한 명령 패킷을 생성합니다.
        /// </summary>
        /// <param name="command">릴레이 명령</param>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>명령 패킷 바이트 배열</returns>
        private byte[] CreateCommandPacket(RelayCommand command, int channel)
        {
            byte[] packet = new byte[8];

            // 헤더
            packet[0] = FRAME_HEADER_1;
            packet[1] = FRAME_HEADER_2;

            // 예약된 필드 (일반적으로 0)
            packet[2] = 0x00;
            packet[3] = 0x00;
            packet[4] = 0x00;

            // 채널 값 설정
            packet[5] = (byte)channel;

            // 명령 코드
            packet[6] = (byte)command;

            // 체크섬 계산 (프로토콜에 따라 다름)
            packet[7] = CalculateChecksum(packet, command, channel);

            return packet;
        }

        /// <summary>
        /// 체크섬을 계산합니다.
        /// </summary>
        /// <param name="packet">명령 패킷</param>
        /// <param name="command">릴레이 명령</param>
        /// <param name="channel">릴레이 채널</param>
        /// <returns>체크섬 바이트</returns>
        private byte CalculateChecksum(byte[] packet, RelayCommand command, int channel)
        {
            // 프로토콜의 체크섬 계산 방식에 따라 구현
            // 프로토콜 문서에 제공된 체크섬 값 사용

            if (command == RelayCommand.ReadStatus)
            {
                // 읽기 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xAC;
                    case 2: return 0xAD;
                    case 3: return 0xAE;
                    case 4: return 0xAF;
                    case 5: return 0xB0;
                    case 6: return 0xB1;
                    case 7: return 0xB2;
                    case 8: return 0xB3;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayOn)
            {
                // 릴레이 켜기 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xAD;
                    case 2: return 0xAE;
                    case 3: return 0xAF;
                    case 4: return 0xB0;
                    case 5: return 0xB1;
                    case 6: return 0xB2;
                    case 7: return 0xB3;
                    case 8: return 0xB4;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayOff)
            {
                // 릴레이 끄기 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xAE;
                    case 2: return 0xAF;
                    case 3: return 0xB0;
                    case 4: return 0xB1;
                    case 5: return 0xB2;
                    case 6: return 0xB3;
                    case 7: return 0xB4;
                    case 8: return 0xB5;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayToggle)
            {
                // 릴레이 토글 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xAF;
                    case 2: return 0xB0;
                    case 3: return 0xB1;
                    case 4: return 0xB2;
                    case 5: return 0xB3;
                    case 6: return 0xB4;
                    case 7: return 0xB5;
                    case 8: return 0xB6;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayMomentary)
            {
                // 릴레이 모멘터리 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xB0;
                    case 2: return 0xB1;
                    case 3: return 0xB2;
                    case 4: return 0xB3;
                    case 5: return 0xB4;
                    case 6: return 0xB5;
                    case 7: return 0xB6;
                    case 8: return 0xB7;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayInterlock)
            {
                // 릴레이 인터록 명령의 체크섬 패턴
                switch (channel)
                {
                    case 1: return 0xB1;
                    case 2: return 0xB2;
                    case 3: return 0xB3;
                    case 4: return 0xB4;
                    case 5: return 0xB5;
                    case 6: return 0xB6;
                    case 7: return 0xB7;
                    case 8: return 0xB8;
                    default: return FRAME_END;
                }
            }
            else if (command == RelayCommand.RelayAll)
            {
                // 모든 릴레이 제어는 고정 체크섬 사용
                return FRAME_END;
            }

            // 기본값
            return FRAME_END;
        }

        /// <summary>
        /// 명령을 전송하고 응답을 받습니다.
        /// </summary>
        /// <param name="command">릴레이 명령</param>
        /// <param name="channel">릴레이 채널 (1-8)</param>
        /// <returns>응답 패킷 (성공 시) 또는 null (실패 시)</returns>
        private byte[] SendCommand(RelayCommand command, int channel)
        {
            byte[] commandPacket = CreateCommandPacket(command, channel);

            for (int retry = 0; retry < _retryCount; retry++)
            {
                try
                {
                    // 요청 전송 전 버퍼 비우기
                    _communicationManager.DiscardInBuffer();

                    // 명령 패킷 전송
                    bool writeResult = _communicationManager.Write(commandPacket);
                    if (!writeResult)
                    {
                        OnErrorOccurred($"데이터 전송 실패 (재시도 {retry + 1}/{_retryCount})");
                        Thread.Sleep(50);
                        continue;
                    }

                    //// 응답 대기
                    //Thread.Sleep(RESPONSE_WAIT_MS);

                    // 응답 읽기
                    byte[] response = _communicationManager.ReadAll();
                    if (response != null && response.Length >= 8)
                    {
                        // 응답 헤더 확인
                        if (response[0] == RESPONSE_HEADER_1 && response[1] == RESPONSE_HEADER_2)
                        {
                            return response;
                        }
                    }

                    // 응답이 없거나 올바르지 않은 경우 재시도
                    Thread.Sleep(20);
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"명령 실행 오류 (재시도 {retry + 1}/{_retryCount}): {ex.Message}");
                    Thread.Sleep(50);
                }
            }

            return null;
        }

        /// <summary>
        /// 원시 명령 패킷을 전송합니다.
        /// </summary>
        /// <param name="commandPacket">명령 패킷 바이트 배열</param>
        /// <returns>성공 여부</returns>
        private bool SendRawCommand(byte[] commandPacket)
        {
            for (int retry = 0; retry < _retryCount; retry++)
            {
                try
                {
                    // 요청 전송 전 버퍼 비우기
                    _communicationManager.DiscardInBuffer();

                    // 명령 패킷 전송
                    bool writeResult = _communicationManager.Write(commandPacket);
                    if (!writeResult)
                    {
                        OnErrorOccurred($"데이터 전송 실패 (재시도 {retry + 1}/{_retryCount})");
                        Thread.Sleep(50);
                        continue;
                    }

                    //// 응답 대기
                    //Thread.Sleep(RESPONSE_WAIT_MS);

                    // 응답 읽기 시도 (일부 명령은 응답이 없을 수 있음)
                    byte[] response = _communicationManager.ReadAll();
                    if (response != null && response.Length >= 8)
                    {
                        // 응답 헤더 확인
                        if (response[0] == RESPONSE_HEADER_1 && response[1] == RESPONSE_HEADER_2)
                        {
                            return true;
                        }
                    }

                    // 모든 릴레이 제어 명령은 응답이 없어도 성공으로 간주할 수 있음
                    return true;
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"명령 실행 오류 (재시도 {retry + 1}/{_retryCount}): {ex.Message}");
                    Thread.Sleep(50);
                }
            }
            return false;
        }

        #endregion
    }
}