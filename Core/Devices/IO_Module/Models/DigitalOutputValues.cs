using System;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// 디지털 출력 값 (8채널)
    /// 
    /// 채널 매핑 (AXAX8080G 마스터):
    ///   DO1 (index 0): GV Solenoid Valve
    ///   DO2 (index 1): VV Solenoid Valve
    ///   DO3 (index 2): EV Solenoid Valve
    ///   DO4 (index 3): IG HV (Ion Gauge High Voltage)
    ///   DO5~DO8: 예비
    /// </summary>
    public class DigitalOutputValues
    {
        /// <summary>
        /// 8채널 DO 상태 (true=ON/Closed, false=OFF/Open)
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

        #region 밸브/출력 편의 속성

        /// <summary>GV Solenoid (DO1)</summary>
        public bool IsGateValveOn => ChannelStates[0];

        /// <summary>VV Solenoid (DO2)</summary>
        public bool IsVentValveOn => ChannelStates[1];

        /// <summary>EV Solenoid (DO3)</summary>
        public bool IsExhaustValveOn => ChannelStates[2];

        /// <summary>IG HV (DO4)</summary>
        public bool IsIonGaugeHVOn => ChannelStates[3];

        #endregion

        public override string ToString()
        {
            return $"DO: GV={IsGateValveOn}, VV={IsVentValveOn}, " +
                   $"EV={IsExhaustValveOn}, IG_HV={IsIonGaugeHVOn}";
        }
    }
}