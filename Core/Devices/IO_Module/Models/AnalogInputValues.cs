using System;
using VacX_OutSense.Core.Devices.IO_Module.Enum;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// IO 모듈의 아날로그 입력 값
    /// 
    /// 마스터: M31-AXAX8080G (AI 없음)
    /// 확장: M31-XGXX0800G (8AI 차동 전압)
    /// 
    /// 확장 모듈 채널 매핑:
    ///   [0]: ATM 스위치 (0-10V)
    ///   [1]: 피라니 게이지 (0-10V)
    ///   [2]: 이온 게이지 (0-10V)
    ///   [3]: 이온 게이지 상태 (0-10V)
    ///   [4]: 추가 AI (±10V)
    ///   [5-7]: 예비
    /// </summary>
    public class AnalogInputValues
    {
        /// <summary>
        /// 확장 모듈 전압 값 (V) - 8채널
        /// </summary>
        public double[] ExpansionVoltageValues { get; set; } = new double[8];

        /// <summary>
        /// 확장 모듈 전압 범위 설정 (기본)
        /// </summary>
        public VoltageRange ExpansionVoltageRange { get; set; } = VoltageRange.Range_0_10V;

        /// <summary>
        /// 추가 AI 고정밀 값 (Floating-point)
        /// </summary>
        public double AdditionalAIValueFloat { get; set; } = double.NaN;

        #region 추가 AI 채널 (±10V)

        /// <summary>
        /// 추가 AI 채널 인덱스 (0-based, ExpansionVoltageValues 배열용)
        /// 기본값: 4 (확장 모듈 채널 5)
        /// </summary>
        public int AdditionalAIChannelIndex { get; set; } = 4;

        /// <summary>
        /// 추가 AI 레인지
        /// </summary>
        public VoltageRange AdditionalAIRange { get; set; } = VoltageRange.Range_Neg10_Pos10V;

        /// <summary>
        /// 추가 AI 값 - Float 우선, 없으면 Integer
        /// </summary>
        public double AdditionalAIValue
        {
            get
            {
                if (!double.IsNaN(AdditionalAIValueFloat) && AdditionalAIValueFloat != 0)
                    return AdditionalAIValueFloat;

                return ExpansionVoltageValues[AdditionalAIChannelIndex];
            }
        }

        #endregion

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        public override string ToString()
        {
            return $"AI: ATM={ExpansionVoltageValues[0]:F3}V, " +
                   $"Pirani={ExpansionVoltageValues[1]:F3}V, " +
                   $"Ion={ExpansionVoltageValues[2]:F3}V, " +
                   $"IonStat={ExpansionVoltageValues[3]:F3}V, " +
                   $"Additional={AdditionalAIValue:F4}V";
        }
    }
}