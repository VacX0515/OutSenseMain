using System;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// 디지털 입력 값 (8채널)
    /// 
    /// 채널 매핑 (AXAX8080G 마스터):
    ///   DI1 (index 0): GV Close Reed Switch
    ///   DI2 (index 1): GV Open Reed Switch
    ///   DI3~DI8: 예비
    /// </summary>
    public class DigitalInputValues
    {
        /// <summary>
        /// 8채널 DI 상태 (true=ON, false=OFF)
        /// </summary>
        public bool[] ChannelStates { get; set; } = new bool[8];

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// 특정 채널 상태 (1-based)
        /// </summary>
        public bool GetChannelState(int channel)
        {
            if (channel < 1 || channel > 8) return false;
            return ChannelStates[channel - 1];
        }

        #region 게이트 밸브 편의 속성

        /// <summary>GV Close Reed (DI1)</summary>
        public bool IsGateValveClosed => ChannelStates[0];

        /// <summary>GV Open Reed (DI2)</summary>
        public bool IsGateValveOpened => ChannelStates[1];

        /// <summary>
        /// 게이트 밸브 위치 상태
        /// </summary>
        public string GateValvePosition
        {
            get
            {
                if (IsGateValveOpened && !IsGateValveClosed) return "Opened";
                if (!IsGateValveOpened && IsGateValveClosed) return "Closed";
                if (IsGateValveOpened && IsGateValveClosed) return "Error";
                return "Moving";
            }
        }

        #endregion

        public override string ToString()
        {
            return $"DI: GV_Close={IsGateValveClosed}, GV_Open={IsGateValveOpened}, " +
                   $"DI3={ChannelStates[2]}, DI4={ChannelStates[3]}, " +
                   $"DI5={ChannelStates[4]}, DI6={ChannelStates[5]}, " +
                   $"DI7={ChannelStates[6]}, DI8={ChannelStates[7]}";
        }
    }
}