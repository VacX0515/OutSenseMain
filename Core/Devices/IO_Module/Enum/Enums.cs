namespace VacX_OutSense.Core.Devices.IO_Module.Enum
{
    /// <summary>
    /// 전압 입력 범위 열거형
    /// </summary>
    public enum VoltageRange
    {
        /// <summary>
        /// 0-10V 범위
        /// </summary>
        Range_0_10V = 0,

        /// <summary>
        /// ±5V 범위
        /// </summary>
        Range_Neg5_Pos5V = 1,

        /// <summary>
        /// ±10V 범위
        /// </summary>
        Range_Neg10_Pos10V = 2
    }
}