using System;
using VacX_OutSense.Core.Devices.IO_Module.Enum;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// IO 모듈의 아날로그 입력 값을 담는 클래스입니다.
    /// 마스터: M31-XAXA0404G (4AI 전류)
    /// 확장: M31-XGXX0800G (8AI 차동 전압)
    /// </summary>
    public class AnalogInputValues
    {
        /// <summary>
        /// 마스터 모듈 전류 값 (mA) - 4채널
        /// </summary>
        public double[] MasterCurrentValues { get; set; } = new double[4];

        /// <summary>
        /// 확장 모듈 전압 값 (V) - 8채널
        /// [0]: ATM 스위치 (0-10V)
        /// [1]: 피라니 게이지 (0-10V)
        /// [2]: 이온 게이지 (0-10V)
        /// [3]: 이온 게이지 상태 (0-10V)
        /// [4]: 추가 AI (±10V) - AdditionalAIChannel 설정에 따라 변경 가능
        /// [5-7]: 예비
        /// </summary>
        public double[] ExpansionVoltageValues { get; set; } = new double[8];

        /// <summary>
        /// 마스터 모듈 전류 범위 설정
        /// </summary>
        public CurrentRange MasterCurrentRange { get; set; } = CurrentRange.Range_0_20mA;

        /// <summary>
        /// 추가 AI 고정밀 값 (Floating-point)
        /// </summary>
        public double AdditionalAIValueFloat { get; set; } = double.NaN;

        /// <summary>
        /// 확장 모듈 전압 범위 설정 (기본)
        /// </summary>
        public VoltageRange ExpansionVoltageRange { get; set; } = VoltageRange.Range_0_10V;

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
                // Float 값이 있으면 우선 사용
                if (!double.IsNaN(AdditionalAIValueFloat) && AdditionalAIValueFloat != 0)
                    return AdditionalAIValueFloat;

                // Integer 값 사용
                return ExpansionVoltageValues[AdditionalAIChannelIndex];
            }
        }

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        #endregion

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

        #region 편의 속성 - 압력 센서

        /// <summary>
        /// ATM 스위치 전압 (확장 모듈 채널 1)
        /// </summary>
        public double AtmSwitchVoltage => ExpansionVoltageValues[0];

        /// <summary>
        /// 피라니 게이지 전압 (확장 모듈 채널 2)
        /// </summary>
        public double PiraniVoltage => ExpansionVoltageValues[1];

        /// <summary>
        /// 이온 게이지 전압 (확장 모듈 채널 3)
        /// </summary>
        public double IonGaugeVoltage => ExpansionVoltageValues[2];

        /// <summary>
        /// 이온 게이지 상태 전압 (확장 모듈 채널 4)
        /// </summary>
        public double IonGaugeStatusVoltage => ExpansionVoltageValues[3];

        #endregion

        #region 유틸리티 메서드

        /// <summary>
        /// 추가 AI 정보를 문자열로 반환
        /// </summary>
        public string GetAdditionalAIInfo()
        {
            string rangeStr = AdditionalAIRange == VoltageRange.Range_Neg10_Pos10V ? "±10V" : "0-10V";
            return $"추가 AI (CH{AdditionalAIChannelIndex + 1}): {AdditionalAIValue:F4}V ({rangeStr})";
        }

        /// <summary>
        /// 모든 확장 모듈 채널 정보 반환
        /// </summary>
        public string GetAllChannelInfo()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < ExpansionVoltageValues.Length; i++)
            {
                string rangeStr = (i == AdditionalAIChannelIndex && AdditionalAIRange == VoltageRange.Range_Neg10_Pos10V)
                    ? "±10V" : "0-10V";
                sb.AppendLine($"CH{i + 1}: {ExpansionVoltageValues[i]:F3}V ({rangeStr})");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 현재 상태를 복사하여 새로운 인스턴스를 생성
        /// </summary>
        public AnalogInputValues Clone()
        {
            var clone = new AnalogInputValues
            {
                MasterCurrentRange = MasterCurrentRange,
                ExpansionVoltageRange = ExpansionVoltageRange,
                AdditionalAIChannelIndex = AdditionalAIChannelIndex,
                AdditionalAIRange = AdditionalAIRange,
                Timestamp = Timestamp
            };

            Array.Copy(MasterCurrentValues, clone.MasterCurrentValues, MasterCurrentValues.Length);
            Array.Copy(ExpansionVoltageValues, clone.ExpansionVoltageValues, ExpansionVoltageValues.Length);

            return clone;
        }

        #endregion
    }
}