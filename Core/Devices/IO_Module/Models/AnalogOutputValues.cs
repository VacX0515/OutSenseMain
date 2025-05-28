using VacX_OutSense.Core.Devices.IO_Module.Enum;

namespace VacX_OutSense.Core.Devices.IO_Module.Models
{
    /// <summary>
    /// 아날로그 출력 값들을 저장하는 클래스
    /// M31 시리즈 IO 모듈의 AO 채널 상태를 나타냅니다.
    /// </summary>
    public class AnalogOutputValues
    {
        /// <summary>
        /// 마스터 모듈의 아날로그 출력 값 (4채널, mA 단위)
        /// </summary>
        public double[] CurrentValues { get; set; } = new double[4];

        /// <summary>
        /// 출력 범위 설정 (기본값: 0-20mA)
        /// </summary>
        public CurrentRange OutputRange { get; set; } = CurrentRange.Range_0_20mA;

        /// <summary>
        /// 특정 채널이 디지털 ON 상태인지 확인 (15mA 이상을 ON으로 간주)
        /// </summary>
        /// <param name="channel">채널 번호 (0-3)</param>
        /// <returns>ON 상태 여부</returns>
        public bool IsChannelOn(int channel)
        {
            if (channel < 0 || channel >= CurrentValues.Length)
                return false;

            return CurrentValues[channel] > 15.0; // 15mA 이상을 ON으로 간주
        }

        /// <summary>
        /// 게이트 밸브 상태 (AO 채널 1)
        /// </summary>
        public bool IsGateValveOpen => IsChannelOn(0);

        /// <summary>
        /// 벤트 밸브 상태 (AO 채널 2)
        /// </summary>
        public bool IsVentValveOpen => IsChannelOn(1);

        /// <summary>
        /// 배기 밸브 상태 (AO 채널 3)
        /// </summary>
        public bool IsExhaustValveOpen => IsChannelOn(2);

        /// <summary>
        /// 이온 게이지 HV 상태 (AO 채널 4)
        /// </summary>
        public bool IsIonGaugeHVOn => IsChannelOn(3);

        /// <summary>
        /// 특정 채널의 현재 출력 값을 가져옵니다.
        /// </summary>
        /// <param name="channel">채널 번호 (1-4)</param>
        /// <returns>출력 값 (mA) 또는 -1 (잘못된 채널)</returns>
        public double GetChannelValue(int channel)
        {
            if (channel < 1 || channel > 4)
                return -1;

            return CurrentValues[channel - 1];
        }

        /// <summary>
        /// 특정 채널의 출력 상태를 문자열로 반환합니다.
        /// </summary>
        /// <param name="channel">채널 번호 (1-4)</param>
        /// <returns>상태 문자열</returns>
        public string GetChannelStatusText(int channel)
        {
            if (channel < 1 || channel > 4)
                return "Invalid Channel";

            double value = CurrentValues[channel - 1];
            bool isOn = value > 15.0;

            return $"{(isOn ? "ON" : "OFF")} ({value:F1}mA)";
        }

        /// <summary>
        /// 모든 밸브 상태를 문자열로 반환합니다.
        /// </summary>
        /// <returns>밸브 상태 문자열</returns>
        public override string ToString()
        {
            return $"GV:{(IsGateValveOpen ? "Open" : "Close")} " +
                   $"VV:{(IsVentValveOpen ? "Open" : "Close")} " +
                   $"EV:{(IsExhaustValveOpen ? "Open" : "Close")} " +
                   $"IG:{(IsIonGaugeHVOn ? "On" : "Off")}";
        }

        /// <summary>
        /// 모든 채널의 상세 정보를 반환합니다.
        /// </summary>
        /// <returns>상세 정보 문자열</returns>
        public string GetDetailedStatus()
        {
            return $"AO1(GV): {GetChannelStatusText(1)}\n" +
                   $"AO2(VV): {GetChannelStatusText(2)}\n" +
                   $"AO3(EV): {GetChannelStatusText(3)}\n" +
                   $"AO4(IG): {GetChannelStatusText(4)}";
        }

        /// <summary>
        /// 현재 상태를 복사하여 새로운 인스턴스를 생성합니다.
        /// </summary>
        /// <returns>복사된 AnalogOutputValues 인스턴스</returns>
        public AnalogOutputValues Clone()
        {
            return new AnalogOutputValues
            {
                CurrentValues = (double[])CurrentValues.Clone(),
                OutputRange = OutputRange
            };
        }

        /// <summary>
        /// 다른 AnalogOutputValues와 값이 같은지 비교합니다.
        /// </summary>
        /// <param name="other">비교할 객체</param>
        /// <param name="tolerance">허용 오차 (mA)</param>
        /// <returns>같으면 true, 다르면 false</returns>
        public bool IsEqual(AnalogOutputValues other, double tolerance = 0.1)
        {
            if (other == null)
                return false;

            if (OutputRange != other.OutputRange)
                return false;

            for (int i = 0; i < CurrentValues.Length; i++)
            {
                if (Math.Abs(CurrentValues[i] - other.CurrentValues[i]) > tolerance)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 모든 채널이 OFF 상태인지 확인합니다.
        /// </summary>
        /// <returns>모든 채널이 OFF이면 true</returns>
        public bool IsAllChannelsOff()
        {
            for (int i = 0; i < CurrentValues.Length; i++)
            {
                if (IsChannelOn(i))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 활성화된 채널 개수를 반환합니다.
        /// </summary>
        /// <returns>활성화된 채널 개수</returns>
        public int GetActiveChannelCount()
        {
            int count = 0;
            for (int i = 0; i < CurrentValues.Length; i++)
            {
                if (IsChannelOn(i))
                    count++;
            }
            return count;
        }
    }
}