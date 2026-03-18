using System;
using System.IO;
using System.Text.Json;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 파일 경로 설정 싱글톤 — Data/Logs/Config/Profiles 경로를 중앙 관리
    /// </summary>
    public class PathSettings
    {
        #region 싱글톤 패턴

        private static readonly Lazy<PathSettings> _instance =
            new Lazy<PathSettings>(() =>
            {
                var settings = new PathSettings();
                settings.Load();
                settings.EnsureDirectoriesExist();
                return settings;
            });

        public static PathSettings Instance => _instance.Value;

        #endregion

        #region 기본 경로

        private static readonly string RootBase = @"C:\OutSense";

        public static string DefaultDataPath => Path.Combine(RootBase, "Data");
        public static string DefaultLogsPath => Path.Combine(RootBase, "Logs");
        public static string DefaultConfigPath => Path.Combine(RootBase, "Config");
        public static string DefaultProfilesPath => Path.Combine(RootBase, "Profiles");

        #endregion

        #region 속성

        public string DataPath { get; set; }
        public string LogsPath { get; set; }
        public string ConfigPath { get; set; }
        public string ProfilesPath { get; set; }

        #endregion

        #region 생성자

        private PathSettings()
        {
            ResetToDefaults();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 모든 경로를 기본값으로 리셋
        /// </summary>
        public void ResetToDefaults()
        {
            DataPath = DefaultDataPath;
            LogsPath = DefaultLogsPath;
            ConfigPath = DefaultConfigPath;
            ProfilesPath = DefaultProfilesPath;
        }

        /// <summary>
        /// 설정된 모든 디렉토리가 존재하는지 확인하고, 없으면 생성
        /// </summary>
        public void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(RootBase);
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(ConfigPath);
            Directory.CreateDirectory(ProfilesPath);
        }

        /// <summary>
        /// 설정을 JSON 파일로 저장 (항상 앱 루트에 저장)
        /// </summary>
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(new PathSettingsData
                {
                    DataPath = DataPath,
                    LogsPath = LogsPath,
                    ConfigPath = ConfigPath,
                    ProfilesPath = ProfilesPath
                }, options);

                File.WriteAllText(GetSettingsFilePath(), json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PathSettings 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// JSON 파일에서 설정 로드
        /// </summary>
        public void Load()
        {
            try
            {
                string path = GetSettingsFilePath();
                if (!File.Exists(path))
                    return;

                string json = File.ReadAllText(path);
                var data = JsonSerializer.Deserialize<PathSettingsData>(json);

                if (data != null)
                {
                    if (!string.IsNullOrWhiteSpace(data.DataPath)) DataPath = data.DataPath;
                    if (!string.IsNullOrWhiteSpace(data.LogsPath)) LogsPath = data.LogsPath;
                    if (!string.IsNullOrWhiteSpace(data.ConfigPath)) ConfigPath = data.ConfigPath;
                    if (!string.IsNullOrWhiteSpace(data.ProfilesPath)) ProfilesPath = data.ProfilesPath;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"PathSettings 로드 실패: {ex.Message}");
            }
        }

        #endregion

        #region 내부

        private static string GetSettingsFilePath()
        {
            return Path.Combine(RootBase, "PathSettings.json");
        }

        /// <summary>
        /// JSON 직렬화용 내부 DTO
        /// </summary>
        private class PathSettingsData
        {
            public string DataPath { get; set; }
            public string LogsPath { get; set; }
            public string ConfigPath { get; set; }
            public string ProfilesPath { get; set; }
        }

        #endregion
    }
}
