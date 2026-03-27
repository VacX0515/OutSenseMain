using System;
using System.IO;
using System.Xml.Serialization;
using VacX_OutSense.Core.Devices.Gauges;

namespace VacX_OutSense.Core.Devices.TempController
{
    /// <summary>
    /// 온도 센서 캘리브레이션 설정
    /// 각 채널별 오프셋(°C)과 게인(배율)을 관리
    /// 보정값 = (측정값 × Gain) + Offset
    /// </summary>
    public class TempCalibrationConfig
    {
        public ChannelCalibration[] Channels { get; set; } = new ChannelCalibration[8];

        /// <summary>이온게이지 캘리브레이션</summary>
        public GaugeCalibration IonGauge { get; set; } = new GaugeCalibration();

        public TempCalibrationConfig()
        {
            for (int i = 0; i < 8; i++)
                Channels[i] = new ChannelCalibration();
        }

        /// <summary>
        /// 보정된 온도 계산 (실수 °C 단위)
        /// </summary>
        public double Apply(int channelIndex, double rawTemperature)
        {
            if (channelIndex < 0 || channelIndex >= Channels.Length)
                return rawTemperature;

            var cal = Channels[channelIndex];
            if (!cal.Enabled)
                return rawTemperature;

            return (rawTemperature * cal.Gain) + cal.Offset;
        }

        /// <summary>
        /// 보정된 raw PV 값 계산 (short, Dot 반영)
        /// </summary>
        public short ApplyToRaw(int channelIndex, short rawPV, int dot)
        {
            if (channelIndex < 0 || channelIndex >= Channels.Length)
                return rawPV;

            var cal = Channels[channelIndex];
            if (!cal.Enabled)
                return rawPV;

            double temperature = dot == 1 ? rawPV / 10.0 : rawPV;
            double calibrated = (temperature * cal.Gain) + cal.Offset;
            return (short)(dot == 1 ? Math.Round(calibrated * 10) : Math.Round(calibrated));
        }

        #region 저장/로드

        private static string DefaultPath =>
            Path.Combine(Utils.PathSettings.Instance.ConfigPath, "TempCalibration.xml");

        public void SaveToFile(string filePath = null)
        {
            filePath = filePath ?? DefaultPath;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(TempCalibrationConfig));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"캘리브레이션 설정 저장 실패: {ex.Message}", ex);
            }
        }

        public static TempCalibrationConfig LoadFromFile(string filePath = null)
        {
            filePath = filePath ?? DefaultPath;
            try
            {
                if (!File.Exists(filePath))
                    return new TempCalibrationConfig();

                var serializer = new XmlSerializer(typeof(TempCalibrationConfig));
                using (var reader = new StreamReader(filePath))
                {
                    return (TempCalibrationConfig)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return new TempCalibrationConfig();
            }
        }

        #endregion
    }

    /// <summary>
    /// 채널별 캘리브레이션 파라미터
    /// </summary>
    public class ChannelCalibration
    {
        /// <summary>캘리브레이션 활성화</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>오프셋 (°C) - 측정값에 더할 값</summary>
        public double Offset { get; set; } = 0.0;

        /// <summary>게인 (배율) - 측정값에 곱할 값</summary>
        public double Gain { get; set; } = 1.0;

        /// <summary>참고 메모</summary>
        public string Note { get; set; } = "";
    }

    /// <summary>
    /// 게이지 캘리브레이션 파라미터
    /// 로그 스케일 압력: 보정값 = 측정값 × Gain (+ 로그 오프셋)
    /// </summary>
    public class GaugeCalibration
    {
        /// <summary>캘리브레이션 활성화</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>이온게이지 모델</summary>
        public IonGaugeModel Model { get; set; } = IonGaugeModel.PTR225;

        /// <summary>게인 (배율) - 압력값에 곱할 값</summary>
        public double Gain { get; set; } = 1.0;

        /// <summary>
        /// 전압 오프셋 (V) - 변환 전 아날로그 전압에 더할 보정값
        /// 센서 출력 전압의 편차를 보정
        /// </summary>
        public double VoltageOffset { get; set; } = 0.0;

        /// <summary>참고 메모</summary>
        public string Note { get; set; } = "";

        /// <summary>보정된 압력 계산</summary>
        public double Apply(double rawPressure)
        {
            if (!Enabled) return rawPressure;
            return rawPressure * Gain;
        }

        /// <summary>보정된 전압 계산 (변환 전 적용)</summary>
        public double ApplyVoltageOffset(double rawVoltage)
        {
            if (!Enabled) return rawVoltage;
            return rawVoltage + VoltageOffset;
        }
    }
}
