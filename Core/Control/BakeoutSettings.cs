using System;
using System.IO;
using System.Text.Json;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 베이크 아웃 램프 업 설정
    /// </summary>
    public class BakeoutSettings
    {
        #region 온도 설정

        /// <summary>
        /// 목표 온도 (°C)
        /// </summary>
        public double TargetTemperature { get; set; } = 100;

        /// <summary>
        /// 승온 속도 (°C/min)
        /// </summary>
        public double RampRate { get; set; } = 5;

        /// <summary>
        /// 사용할 프로파일 이름
        /// </summary>
        public string ProfileName { get; set; } = "일반 시편";

        #endregion

        #region 타이머 설정

        /// <summary>
        /// 목표 온도 도달 후 유지 시간 (분)
        /// </summary>
        public int HoldTimeMinutes { get; set; } = 30;

        /// <summary>
        /// 타이머 종료 시 동작
        /// </summary>
        public BakeoutEndAction EndAction { get; set; } = BakeoutEndAction.HeaterOff;

        /// <summary>
        /// 목표 온도 도달 시 타이머 자동 시작
        /// </summary>
        public bool AutoStartTimerOnTargetReached { get; set; } = true;

        #endregion

        #region 램프 업 활성화

        /// <summary>
        /// 진공도 도달 시 램프 업 자동 시작 여부
        /// </summary>
        public bool EnableAutoRampUp { get; set; } = true;

        #endregion

        #region 저장/로드

        private static string GetSettingsPath()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(exePath, "Config");
            
            if (!Directory.Exists(configPath))
            {
                Directory.CreateDirectory(configPath);
            }
            
            return Path.Combine(configPath, "BakeoutSettings.json");
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(GetSettingsPath(), json);
            }
            catch { }
        }

        public static BakeoutSettings Load()
        {
            try
            {
                string path = GetSettingsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    return JsonSerializer.Deserialize<BakeoutSettings>(json) ?? new BakeoutSettings();
                }
            }
            catch { }
            
            return new BakeoutSettings();
        }

        #endregion
    }

    /// <summary>
    /// 베이크 아웃 종료 시 동작
    /// </summary>
    public enum BakeoutEndAction
    {
        /// <summary>
        /// 히터 OFF
        /// </summary>
        HeaterOff,

        /// <summary>
        /// 현재 온도 유지
        /// </summary>
        MaintainTemperature,

        /// <summary>
        /// 알림만 (수동 조작)
        /// </summary>
        NotifyOnly
    }
}
