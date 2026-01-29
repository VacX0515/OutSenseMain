using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 열 램프 프로파일 관리자
    /// JSON 파일로 저장/로드, 기본 프로파일 제공
    /// </summary>
    public class ThermalRampProfileManager
    {
        #region 필드 및 속성

        private readonly string _configPath;
        private readonly string _profilesFilePath;
        private ThermalRampProfileCollection _collection;

        /// <summary>
        /// 현재 로드된 모든 프로파일
        /// </summary>
        public List<ThermalRampProfile> Profiles => _collection.Profiles;

        /// <summary>
        /// 마지막으로 선택된 프로파일 이름
        /// </summary>
        public string LastSelectedProfileName
        {
            get => _collection.LastSelectedProfile;
            set
            {
                _collection.LastSelectedProfile = value;
                Save();
            }
        }

        #endregion

        #region 생성자

        public ThermalRampProfileManager()
        {
            // 설정 파일 경로: 실행 파일 위치/Config/
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _configPath = Path.Combine(exePath, "Config");
            _profilesFilePath = Path.Combine(_configPath, "ThermalRampProfiles.json");

            // 디렉토리 생성
            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }

            // 프로파일 로드 또는 기본값 생성
            Load();
        }

        public ThermalRampProfileManager(string customPath)
        {
            _configPath = Path.GetDirectoryName(customPath);
            _profilesFilePath = customPath;

            if (!Directory.Exists(_configPath))
            {
                Directory.CreateDirectory(_configPath);
            }

            Load();
        }

        #endregion

        #region 저장/로드

        /// <summary>
        /// 프로파일을 JSON 파일로 저장
        /// </summary>
        public void Save()
        {
            try
            {
                _collection.LastModified = DateTime.Now;

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                string json = JsonSerializer.Serialize(_collection, options);
                File.WriteAllText(_profilesFilePath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"프로파일 저장 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// JSON 파일에서 프로파일 로드
        /// </summary>
        public void Load()
        {
            try
            {
                if (File.Exists(_profilesFilePath))
                {
                    string json = File.ReadAllText(_profilesFilePath);
                    _collection = JsonSerializer.Deserialize<ThermalRampProfileCollection>(json);

                    // 시스템 기본 프로파일이 없으면 추가
                    EnsureDefaultProfiles();
                }
                else
                {
                    // 파일이 없으면 기본 프로파일로 초기화
                    _collection = new ThermalRampProfileCollection();
                    CreateDefaultProfiles();
                    Save();
                }
            }
            catch (Exception)
            {
                // 로드 실패 시 기본값으로
                _collection = new ThermalRampProfileCollection();
                CreateDefaultProfiles();
                Save();
            }
        }

        #endregion

        #region 프로파일 관리

        /// <summary>
        /// 이름으로 프로파일 찾기
        /// </summary>
        public ThermalRampProfile GetProfile(string name)
        {
            return Profiles.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// 프로파일 추가
        /// </summary>
        public void AddProfile(ThermalRampProfile profile)
        {
            // 중복 이름 체크
            if (Profiles.Any(p => p.Name == profile.Name))
            {
                throw new ArgumentException($"'{profile.Name}' 이름의 프로파일이 이미 존재합니다.");
            }

            profile.IsSystemDefault = false;
            Profiles.Add(profile);
            Save();
        }

        /// <summary>
        /// 프로파일 수정 (기존 프로파일 대체)
        /// </summary>
        public void UpdateProfile(string originalName, ThermalRampProfile updatedProfile)
        {
            var existing = Profiles.FirstOrDefault(p => p.Name == originalName);
            if (existing == null)
            {
                throw new ArgumentException($"'{originalName}' 프로파일을 찾을 수 없습니다.");
            }

            // 이름이 변경된 경우 중복 체크
            if (originalName != updatedProfile.Name && Profiles.Any(p => p.Name == updatedProfile.Name))
            {
                throw new ArgumentException($"'{updatedProfile.Name}' 이름의 프로파일이 이미 존재합니다.");
            }

            int index = Profiles.IndexOf(existing);
            updatedProfile.IsSystemDefault = existing.IsSystemDefault;
            Profiles[index] = updatedProfile;
            Save();
        }

        /// <summary>
        /// 프로파일 삭제
        /// </summary>
        public bool DeleteProfile(string name)
        {
            var profile = Profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null) return false;

            if (profile.IsSystemDefault)
            {
                throw new InvalidOperationException("시스템 기본 프로파일은 삭제할 수 없습니다.");
            }

            Profiles.Remove(profile);
            Save();
            return true;
        }

        /// <summary>
        /// 프로파일 복제
        /// </summary>
        public ThermalRampProfile DuplicateProfile(string name)
        {
            var original = GetProfile(name);
            if (original == null)
            {
                throw new ArgumentException($"'{name}' 프로파일을 찾을 수 없습니다.");
            }

            var clone = (ThermalRampProfile)original.Clone();
            
            // 고유한 이름 생성
            string baseName = clone.Name;
            int counter = 1;
            while (Profiles.Any(p => p.Name == clone.Name))
            {
                clone.Name = $"{baseName} ({counter++})";
            }

            AddProfile(clone);
            return clone;
        }

        #endregion

        #region 기본 프로파일

        /// <summary>
        /// 시스템 기본 프로파일 생성
        /// </summary>
        private void CreateDefaultProfiles()
        {
            Profiles.Clear();
            Profiles.AddRange(GetSystemDefaultProfiles());
            _collection.LastSelectedProfile = Profiles.First().Name;
        }

        /// <summary>
        /// 기본 프로파일이 누락된 경우 추가
        /// </summary>
        private void EnsureDefaultProfiles()
        {
            var defaults = GetSystemDefaultProfiles();
            foreach (var defaultProfile in defaults)
            {
                if (!Profiles.Any(p => p.Name == defaultProfile.Name))
                {
                    Profiles.Insert(0, defaultProfile);
                }
            }
        }

        /// <summary>
        /// 시스템 기본 프로파일 목록
        /// </summary>
        public static List<ThermalRampProfile> GetSystemDefaultProfiles()
        {
            return new List<ThermalRampProfile>
            {
                new ThermalRampProfile
                {
                    Name = "박막/웨이퍼",
                    Description = "Si 웨이퍼, 박막 코팅, 얇은 기판 등\n열 전달이 빠르고 열용량이 작은 샘플",
                    IsSystemDefault = true,
                    // 기본 설정값
                    DefaultTargetTemperature = 100,
                    DefaultRampRate = 10,
                    MinTargetTemperature = 50,
                    MinRampRate = 5,
                    // 열 특성
                    HeatTransferDelay = 10,
                    MaxHeaterSampleGap = 25,
                    TemperatureStabilityRange = 1.5,
                    StabilizationTime = 20,
                    // 제어 설정
                    ControlResponsiveness = 6,
                    UseInitialBoost = true,
                    UseTargetSlowdown = true,
                    // 안전 설정
                    MaxHeaterTemperature = 220,
                    EmergencyStopGap = 50,
                    MaxRampRate = 30
                },
                new ThermalRampProfile
                {
                    Name = "일반 시편",
                    Description = "일반적인 금속/세라믹 시편\n대부분의 실험에 적합한 균형 잡힌 설정",
                    IsSystemDefault = true,
                    // 기본 설정값
                    DefaultTargetTemperature = 100,
                    DefaultRampRate = 5,
                    MinTargetTemperature = 50,
                    MinRampRate = 2,
                    // 열 특성
                    HeatTransferDelay = 30,
                    MaxHeaterSampleGap = 40,
                    TemperatureStabilityRange = 2.0,
                    StabilizationTime = 30,
                    // 제어 설정
                    ControlResponsiveness = 5,
                    UseInitialBoost = true,
                    UseTargetSlowdown = true,
                    // 안전 설정
                    MaxHeaterTemperature = 200,
                    EmergencyStopGap = 70,
                    MaxRampRate = 15
                },
                new ThermalRampProfile
                {
                    Name = "벌크 금속",
                    Description = "두꺼운 금속 블록, 대형 시편\n열용량이 크고 열 전달이 느린 샘플",
                    IsSystemDefault = true,
                    // 기본 설정값
                    DefaultTargetTemperature = 80,
                    DefaultRampRate = 3,
                    MinTargetTemperature = 50,
                    MinRampRate = 1,
                    // 열 특성
                    HeatTransferDelay = 60,
                    MaxHeaterSampleGap = 60,
                    TemperatureStabilityRange = 3.0,
                    StabilizationTime = 60,
                    // 제어 설정
                    ControlResponsiveness = 4,
                    UseInitialBoost = true,
                    UseTargetSlowdown = true,
                    // 안전 설정
                    MaxHeaterTemperature = 250,
                    EmergencyStopGap = 100,
                    MaxRampRate = 10
                },
                new ThermalRampProfile
                {
                    Name = "분말/다공성",
                    Description = "파우더, 과립, 다공성 물질\n열 접촉이 불균일한 샘플",
                    IsSystemDefault = true,
                    // 기본 설정값
                    DefaultTargetTemperature = 80,
                    DefaultRampRate = 2,
                    MinTargetTemperature = 40,
                    MinRampRate = 0.5,
                    // 열 특성
                    HeatTransferDelay = 45,
                    MaxHeaterSampleGap = 50,
                    TemperatureStabilityRange = 3.0,
                    StabilizationTime = 45,
                    // 제어 설정
                    ControlResponsiveness = 4,
                    UseInitialBoost = false,
                    UseTargetSlowdown = true,
                    // 안전 설정
                    MaxHeaterTemperature = 180,
                    EmergencyStopGap = 80,
                    MaxRampRate = 8
                },
                new ThermalRampProfile
                {
                    Name = "고온 실험",
                    Description = "150°C 이상 고온 실험용\n열 손실 보상이 강화된 설정",
                    IsSystemDefault = true,
                    // 기본 설정값
                    DefaultTargetTemperature = 150,
                    DefaultRampRate = 5,
                    MinTargetTemperature = 100,
                    MinRampRate = 2,
                    // 열 특성
                    HeatTransferDelay = 40,
                    MaxHeaterSampleGap = 70,
                    TemperatureStabilityRange = 2.5,
                    StabilizationTime = 40,
                    // 제어 설정
                    ControlResponsiveness = 6,
                    UseInitialBoost = true,
                    UseTargetSlowdown = true,
                    // 안전 설정
                    MaxHeaterTemperature = 300,
                    EmergencyStopGap = 100,
                    MaxRampRate = 20
                }
            };
        }

        #endregion

        #region 내보내기/가져오기

        /// <summary>
        /// 단일 프로파일을 파일로 내보내기
        /// </summary>
        public void ExportProfile(string name, string filePath)
        {
            var profile = GetProfile(name);
            if (profile == null)
            {
                throw new ArgumentException($"'{name}' 프로파일을 찾을 수 없습니다.");
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(profile, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 파일에서 프로파일 가져오기
        /// </summary>
        public ThermalRampProfile ImportProfile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filePath}");
            }

            string json = File.ReadAllText(filePath);
            var profile = JsonSerializer.Deserialize<ThermalRampProfile>(json);
            profile.IsSystemDefault = false;

            // 중복 이름 처리
            string baseName = profile.Name;
            int counter = 1;
            while (Profiles.Any(p => p.Name == profile.Name))
            {
                profile.Name = $"{baseName} ({counter++})";
            }

            AddProfile(profile);
            return profile;
        }

        #endregion
    }
}
