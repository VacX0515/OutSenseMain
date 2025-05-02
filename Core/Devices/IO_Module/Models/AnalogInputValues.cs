using VacX_OutSense.Core.Devices.IO_Module.Enum;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// IO 모듈의 아날로그 입력 값을 담는 클래스입니다.
    /// </summary>
    public class AnalogInputValues
    {
        /// <summary>
        /// 마스터 모듈 전류 값 (mA)
        /// </summary>
        public double[] MasterCurrentValues { get; set; } = new double[4];

        /// <summary>
        /// 확장 모듈 전압 값 (V)
        /// </summary>
        public double[] ExpansionVoltageValues { get; set; } = new double[8];

        /// <summary>
        /// 마스터 모듈 전류 범위 설정
        /// </summary>
        public CurrentRange MasterCurrentRange { get; set; } = CurrentRange.Range_0_20mA;

        /// <summary>
        /// 확장 모듈 전압 범위 설정
        /// </summary>
        public VoltageRange ExpansionVoltageRange { get; set; } = VoltageRange.Range_0_10V;

        /// <summary>
        /// AnalogInputValues 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public AnalogInputValues()
        {
            // 기본 값으로 초기화
            for (int i = 0; i < MasterCurrentValues.Length; i++)
            {
                MasterCurrentValues[i] = 0.0;
            }

            for (int i = 0; i < ExpansionVoltageValues.Length; i++)
            {
                ExpansionVoltageValues[i] = 0.0;
            }
        }
    }
}