using System;
using System.Collections.Generic;
using VacX_OutSense.Core.Devices.Relay_Module.Enum;

namespace VacX_OutSense.Core.Devices.Relay_Module.Models
{
    /// <summary>
    /// 릴레이 모듈의 상태 값을 나타내는 클래스
    /// </summary>
    public class RelayModuleValues
    {
        /// <summary>
        /// 각 릴레이 채널(1-8)의 상태 (true = ON, false = OFF)
        /// </summary>
        public bool[] RelayStates { get; private set; }

        /// <summary>
        /// 각 릴레이 채널(1-8)의 동작 모드
        /// </summary>
        public RelayMode[] RelayModes { get; private set; }

        /// <summary>
        /// 릴레이 모듈 값 객체의 새 인스턴스를 초기화합니다.
        /// </summary>
        public RelayModuleValues()
        {
            RelayStates = new bool[8];
            RelayModes = new RelayMode[8];

            // 기본값 설정
            for (int i = 0; i < 8; i++)
            {
                RelayStates[i] = false; // 기본적으로 모든 릴레이는 OFF 상태
                RelayModes[i] = RelayMode.Normal; // 기본 모드 설정
            }
        }

        /// <summary>
        /// 특정 채널의 릴레이 상태를 설정합니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (0-7)</param>
        /// <param name="state">상태 (true = ON, false = OFF)</param>
        public void SetRelayState(int channel, bool state)
        {
            if (channel >= 0 && channel < 8)
            {
                RelayStates[channel] = state;
            }
        }

        /// <summary>
        /// 특정 채널의 릴레이 모드를 설정합니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (0-7)</param>
        /// <param name="mode">동작 모드</param>
        public void SetRelayMode(int channel, RelayMode mode)
        {
            if (channel >= 0 && channel < 8)
            {
                RelayModes[channel] = mode;
            }
        }

        /// <summary>
        /// 특정 채널의 릴레이 상태를 가져옵니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (0-7)</param>
        /// <returns>릴레이 상태 (true = ON, false = OFF) 또는 잘못된 채널인 경우 false</returns>
        public bool GetRelayState(int channel)
        {
            if (channel >= 0 && channel < 8)
            {
                return RelayStates[channel];
            }
            return false;
        }

        /// <summary>
        /// 특정 채널의 릴레이 모드를 가져옵니다.
        /// </summary>
        /// <param name="channel">릴레이 채널 (0-7)</param>
        /// <returns>릴레이 모드 또는 잘못된 채널인 경우 Normal</returns>
        public RelayMode GetRelayMode(int channel)
        {
            if (channel >= 0 && channel < 8)
            {
                return RelayModes[channel];
            }
            return RelayMode.Normal;
        }

        /// <summary>
        /// 모든 릴레이 상태를 설정합니다.
        /// </summary>
        /// <param name="states">릴레이 상태 배열 (길이가 8이 아닌 경우 처리 가능한 채널까지만 설정)</param>
        public void SetAllRelayStates(bool[] states)
        {
            if (states == null)
                return;

            for (int i = 0; i < Math.Min(states.Length, 8); i++)
            {
                RelayStates[i] = states[i];
            }
        }

        /// <summary>
        /// 응답 패킷으로부터 릴레이 상태를 업데이트합니다.
        /// </summary>
        /// <param name="response">응답 패킷 바이트 배열</param>
        /// <returns>업데이트 성공 여부</returns>
        public bool UpdateFromResponse(byte[] response)
        {
            if (response == null || response.Length < 8)
                return false;

            // 응답 패킷 형식이 적절한지 확인
            if (response[0] == 0x33 && response[1] == 0x3C)
            {
                byte channelByte = response[5];
                byte statusByte = response[6];

                // 상태 코드에 따라 릴레이 상태 업데이트
                // 1 = 열림(ON), 2 = 닫힘(OFF)
                bool isOn = (statusByte == 0x01);

                // 단일 채널 명령인 경우
                if (channelByte >= 1 && channelByte <= 8)
                {
                    int channel = channelByte - 1;  // 0-based 인덱스로 변환
                    SetRelayState(channel, isOn);
                    return true;
                }
                // 여러 채널 제어 명령인 경우 (각 비트가 채널을 나타내는 경우)
                else
                {
                    // 이 부분은 실제 프로토콜에 따라 조정 필요
                    for (int i = 0; i < 8; i++)
                    {
                        if ((channelByte & (1 << i)) != 0)
                        {
                            SetRelayState(i, isOn);
                        }
                    }
                    return true;
                }
            }

            return false;
        }
    }
}