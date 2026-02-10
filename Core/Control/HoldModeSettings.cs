using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// Hold 모드 설정
    /// </summary>
    [Serializable]
    public class HoldModeSettings
    {
        /// <summary>
        /// 1회 최대 조정량 (°C)
        /// </summary>
        public double MaxAdjustment { get; set; } = 2.0;


        /// <summary>
        /// 사용할 채널 (다중 선택 가능)
        /// </summary>
        public bool UseCh2 { get; set; } = true;
        public bool UseCh3 { get; set; } = false;
        public bool UseCh4 { get; set; } = false;
        public bool UseCh5 { get; set; } = false;

        /// <summary>
        /// 체크 간격 (분)
        /// </summary>
        public int CheckIntervalMinutes { get; set; } = 10;

        /// <summary>
        /// 오차 허용 범위 (°C)
        /// </summary>
        public double ErrorTolerance { get; set; } = 0.1;

        /// <summary>
        /// CH1 SV 최소값 (°C)
        /// </summary>
        public double MinHeaterTemp { get; set; } = 30;

        /// <summary>
        /// CH1 SV 최대값 (°C)
        /// </summary>
        public double MaxHeaterTemp { get; set; } = 200;

        /// <summary>
        /// 조정 배율 (오차 × 배율 = SV 변화)
        /// </summary>
        public double AdjustmentMultiplier { get; set; } = 1.0;

        /// <summary>
        /// 선택된 채널 목록 반환
        /// </summary>
        public List<int> GetSelectedChannels()
        {
            var channels = new List<int>();
            if (UseCh2) channels.Add(2);
            if (UseCh3) channels.Add(3);
            if (UseCh4) channels.Add(4);
            if (UseCh5) channels.Add(5);
            return channels;
        }

        /// <summary>
        /// 선택된 채널 텍스트
        /// </summary>
        public string GetSourceText()
        {
            var channels = GetSelectedChannels();
            if (channels.Count == 0) return "없음";
            if (channels.Count == 1) return $"CH{channels[0]}";
            return $"CH{string.Join("+", channels)} 평균";
        }

        #region 저장/로드

        private static readonly string SettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Config", "HoldModeSettings.xml");

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (var writer = new StreamWriter(SettingsPath))
                {
                    new XmlSerializer(typeof(HoldModeSettings)).Serialize(writer, this);
                }
            }
            catch { }
        }

        public static HoldModeSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    using (var reader = new StreamReader(SettingsPath))
                    {
                        return (HoldModeSettings)new XmlSerializer(typeof(HoldModeSettings)).Deserialize(reader);
                    }
                }
            }
            catch { }

            return new HoldModeSettings();
        }

        #endregion
    }
}