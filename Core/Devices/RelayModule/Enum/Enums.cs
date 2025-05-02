namespace VacX_OutSense.Core.Devices.Relay_Module.Enum
{
    /// <summary>
    /// 릴레이 동작 모드를 정의하는 열거형
    /// </summary>
    public enum RelayMode
    {
        /// <summary>
        /// 일반 모드 - 켜기/끄기만 동작
        /// </summary>
        Normal = 0,

        /// <summary>
        /// 토글 모드 - 상태가 반전됨
        /// </summary>
        Toggle = 1,

        /// <summary>
        /// 모멘터리 모드 - 잠시 활성화 후 비활성화됨 (통상 200ms)
        /// </summary>
        Momentary = 2,

        /// <summary>
        /// 인터록 모드 - 하나의 릴레이만 활성화되도록 함
        /// </summary>
        Interlock = 3
    }

    /// <summary>
    /// 릴레이 제어 명령어를 정의하는 열거형
    /// </summary>
    public enum RelayCommand
    {
        /// <summary>
        /// 상태 읽기 명령
        /// </summary>
        ReadStatus = 0x00,

        /// <summary>
        /// 릴레이 켜기 명령 (COM이 NO에 연결)
        /// </summary>
        RelayOn = 0x01,

        /// <summary>
        /// 릴레이 끄기 명령 (COM이 NC에 연결)
        /// </summary>
        RelayOff = 0x02,

        /// <summary>
        /// 릴레이 토글 명령 (현재 상태 반전)
        /// </summary>
        RelayToggle = 0x03,

        /// <summary>
        /// 릴레이 모멘터리 명령 (잠시 켜짐 후 꺼짐)
        /// </summary>
        RelayMomentary = 0x04,

        /// <summary>
        /// 릴레이 인터록 명령
        /// </summary>
        RelayInterlock = 0x05,

        /// <summary>
        /// 모든 릴레이 제어 명령
        /// </summary>
        RelayAll = 0x06
    }
}