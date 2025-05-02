namespace VacX_OutSense.Core.Devices.IO_Module.Enum
{
    /// <summary>
    /// 전류 입력 범위 열거형
    /// </summary>
    public enum CurrentRange
    {
        /// <summary>
        /// 0-20mA 범위
        /// </summary>
        Range_0_20mA = 0,

        /// <summary>
        /// 4-20mA 범위
        /// </summary>
        Range_4_20mA = 1,

        /// <summary>
        /// ±20mA 범위
        /// </summary>
        Range_Neg20_Pos20mA = 2
    }

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