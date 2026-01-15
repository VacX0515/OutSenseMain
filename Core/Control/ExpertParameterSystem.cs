using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.ComponentModel;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 실험자 경험 기반 파라미터 관리 시스템
    /// </summary>
    public class ExpertParameterSystem
    {
        #region 샘플 프리셋 정의

        /// <summary>
        /// 샘플 타입별 열적 특성 및 제어 파라미터
        /// </summary>
        public class SamplePreset : ICloneable
        {
            [Category("기본 정보")]
            [Description("프리셋 이름")]
            public string Name { get; set; }

            [Category("기본 정보")]
            [Description("상세 설명")]
            public string Description { get; set; }

            [Category("열적 특성")]
            [Description("열용량 (0.1=낮음, 1.0=높음)")]
            [DefaultValue(0.5)]
            public double ThermalMass { get; set; } = 0.5;

            [Category("열적 특성")]
            [Description("방사율 (0.1=낮음, 1.0=높음)")]
            [DefaultValue(0.5)]
            public double Emissivity { get; set; } = 0.5;

            [Category("열적 특성")]
            [Description("열접촉 품질 (0.1=나쁨, 1.0=좋음)")]
            [DefaultValue(0.5)]
            public double ContactQuality { get; set; } = 0.5;

            [Category("제어 파라미터")]
            [Description("초기 오버슈트 온도 (°C)")]
            [DefaultValue(15.0)]
            public double InitialOvershoot { get; set; } = 15.0;

            [Category("제어 파라미터")]
            [Description("평형 상태 온도차 (°C)")]
            [DefaultValue(10.0)]
            public double SteadyStateOffset { get; set; } = 10.0;

            [Category("제어 파라미터")]
            [Description("응답 지연 시간 (초)")]
            [DefaultValue(30.0)]
            public double ResponseDelay { get; set; } = 30.0;

            [Category("온도별 보정")]
            [Description("온도 구간별 오프셋 설정")]
            public Dictionary<int, double> TemperatureOffsets { get; set; }

            [Category("안전 설정")]
            [Description("최대 히터 온도 (°C)")]
            [DefaultValue(200.0)]
            public double MaxHeaterTemp { get; set; } = 200.0;

            [Category("안전 설정")]
            [Description("최대 승온 속도 (°C/min)")]
            [DefaultValue(20.0)]
            public double MaxRampRate { get; set; } = 20.0;

            public SamplePreset()
            {
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 5 },
                    { 100, 10 },
                    { 150, 15 }
                };
            }

            public object Clone()
            {
                var clone = (SamplePreset)MemberwiseClone();
                clone.TemperatureOffsets = new Dictionary<int, double>(TemperatureOffsets);
                return clone;
            }
        }

        #endregion

        #region 사전 정의 프리셋

        public static class PresetLibrary
        {
            public static SamplePreset ThinFilm => new SamplePreset
            {
                Name = "박막 샘플 (Thin Film)",
                Description = "Si 웨이퍼, 박막, 코팅 샘플 등",
                ThermalMass = 0.2,
                Emissivity = 0.3,
                ContactQuality = 0.8,
                InitialOvershoot = 20,
                SteadyStateOffset = 5,
                ResponseDelay = 10,
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 3 },
                    { 100, 7 },
                    { 150, 12 },
                    { 200, 18 }
                },
                MaxHeaterTemp = 220,
                MaxRampRate = 30
            };

            public static SamplePreset BulkMetal => new SamplePreset
            {
                Name = "벌크 금속 (Bulk Metal)",
                Description = "금속 블록, 두꺼운 시편",
                ThermalMass = 0.9,
                Emissivity = 0.2,
                ContactQuality = 0.6,
                InitialOvershoot = 35,
                SteadyStateOffset = 15,
                ResponseDelay = 60,
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 8 },
                    { 100, 18 },
                    { 150, 28 },
                    { 200, 40 }
                },
                MaxHeaterTemp = 250,
                MaxRampRate = 10
            };

            public static SamplePreset Powder => new SamplePreset
            {
                Name = "분말 샘플 (Powder)",
                Description = "파우더, 과립, 다공성 물질",
                ThermalMass = 0.5,
                Emissivity = 0.7,
                ContactQuality = 0.3,
                InitialOvershoot = 25,
                SteadyStateOffset = 20,
                ResponseDelay = 30,
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 10 },
                    { 100, 22 },
                    { 150, 35 },
                    { 200, 50 }
                },
                MaxHeaterTemp = 180,
                MaxRampRate = 5
            };

            public static SamplePreset Ceramic => new SamplePreset
            {
                Name = "세라믹 (Ceramic)",
                Description = "세라믹, 유리, 절연체",
                ThermalMass = 0.7,
                Emissivity = 0.8,
                ContactQuality = 0.5,
                InitialOvershoot = 30,
                SteadyStateOffset = 12,
                ResponseDelay = 45,
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 6 },
                    { 100, 14 },
                    { 150, 22 },
                    { 200, 32 }
                },
                MaxHeaterTemp = 300,
                MaxRampRate = 15
            };

            public static SamplePreset Polymer => new SamplePreset
            {
                Name = "폴리머 (Polymer)",
                Description = "플라스틱, 고분자, 유기물",
                ThermalMass = 0.4,
                Emissivity = 0.6,
                ContactQuality = 0.7,
                InitialOvershoot = 15,
                SteadyStateOffset = 8,
                ResponseDelay = 20,
                TemperatureOffsets = new Dictionary<int, double>
                {
                    { 50, 4 },
                    { 100, 9 },
                    { 150, 15 }
                },
                MaxHeaterTemp = 150,
                MaxRampRate = 8
            };

            public static SamplePreset Custom => new SamplePreset
            {
                Name = "사용자 정의 (Custom)",
                Description = "수동으로 모든 파라미터 설정",
                ThermalMass = 0.5,
                Emissivity = 0.5,
                ContactQuality = 0.5,
                InitialOvershoot = 20,
                SteadyStateOffset = 10,
                ResponseDelay = 30,
                MaxHeaterTemp = 200,
                MaxRampRate = 10
            };

            public static List<SamplePreset> GetAllPresets()
            {
                return new List<SamplePreset>
                {
                    ThinFilm,
                    BulkMetal,
                    Powder,
                    Ceramic,
                    Polymer,
                    Custom
                };
            }
        }

        #endregion

        #region PID 파라미터

        /// <summary>
        /// 사용자 조정 가능한 PID 게인
        /// </summary>
        public class PIDParameters : ICloneable
        {
            [Category("PID 게인")]
            [Description("비례 게인 (반응 속도)")]
            [DefaultValue(2.0)]
            public double ProportionalGain { get; set; } = 2.0;

            [Category("PID 게인")]
            [Description("적분 게인 (정상상태 오차 제거)")]
            [DefaultValue(0.5)]
            public double IntegralGain { get; set; } = 0.5;

            [Category("PID 게인")]
            [Description("미분 게인 (진동 억제)")]
            [DefaultValue(0.1)]
            public double DerivativeGain { get; set; } = 0.1;

            [Category("PID 설정")]
            [Description("적분 와인드업 제한")]
            [DefaultValue(100.0)]
            public double IntegralLimit { get; set; } = 100.0;

            [Category("PID 설정")]
            [Description("출력 최소값 (%)")]
            [DefaultValue(0.0)]
            public double OutputMin { get; set; } = 0.0;

            [Category("PID 설정")]
            [Description("출력 최대값 (%)")]
            [DefaultValue(100.0)]
            public double OutputMax { get; set; } = 100.0;

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        #endregion

        #region 보상 전략

        /// <summary>
        /// 온도 보상 전략 설정
        /// </summary>
        public class CompensationStrategy : ICloneable
        {
            [Category("보상 설정")]
            [Description("초기 부스트 강도 (0.5~2.0)")]
            [DefaultValue(1.2)]
            public double InitialBoostFactor { get; set; } = 1.2;

            [Category("보상 설정")]
            [Description("예측 제어 가중치 (0=반응형, 1=예측형)")]
            [DefaultValue(0.7)]
            public double PredictiveWeight { get; set; } = 0.7;

            [Category("보상 설정")]
            [Description("온도 필터링 윈도우 크기")]
            [DefaultValue(5)]
            public int FilterWindow { get; set; } = 5;

            [Category("보상 설정")]
            [Description("동적 보상 활성화")]
            [DefaultValue(true)]
            public bool EnableDynamicCompensation { get; set; } = true;

            [Category("보상 설정")]
            [Description("오버슈트 방지 활성화")]
            [DefaultValue(true)]
            public bool EnableOvershootPrevention { get; set; } = true;

            [Category("보상 설정")]
            [Description("목표 근처 감속 시작 온도차 (°C)")]
            [DefaultValue(10.0)]
            public double SlowdownThreshold { get; set; } = 10.0;

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        #endregion

        #region 안전 설정

        /// <summary>
        /// 안전 제한 설정
        /// </summary>
        public class SafetySettings : ICloneable
        {
            [Category("온도 제한")]
            [Description("히터-샘플 최대 온도차 (°C)")]
            [DefaultValue(50.0)]
            public double MaxTempDifference { get; set; } = 50.0;

            [Category("온도 제한")]
            [Description("비상 정지 온도차 (°C)")]
            [DefaultValue(70.0)]
            public double EmergencyStopDelta { get; set; } = 70.0;

            [Category("변화율 제한")]
            [Description("최대 온도 변화율 (°C/sec)")]
            [DefaultValue(1.0)]
            public double MaxChangeRate { get; set; } = 1.0;

            [Category("변화율 제한")]
            [Description("히터 조정 최대값 (°C/100ms)")]
            [DefaultValue(5.0)]
            public double MaxHeaterAdjustment { get; set; } = 5.0;

            [Category("타임아웃")]
            [Description("목표 도달 타임아웃 (분)")]
            [DefaultValue(30.0)]
            public double TargetReachTimeout { get; set; } = 30.0;

            [Category("타임아웃")]
            [Description("안정화 타임아웃 (초)")]
            [DefaultValue(60.0)]
            public double StabilizationTimeout { get; set; } = 60.0;

            public object Clone()
            {
                return MemberwiseClone();
            }
        }

        #endregion

        #region 실험 프로파일

        /// <summary>
        /// 완전한 실험 프로파일
        /// </summary>
        public class ExperimentProfile : ICloneable
        {
            [Category("프로파일 정보")]
            public string ProfileName { get; set; }

            [Category("프로파일 정보")]
            public DateTime CreatedDate { get; set; }

            [Category("프로파일 정보")]
            public string CreatedBy { get; set; }

            [Category("프로파일 정보")]
            public string Version { get; set; } = "1.0";

            [Category("프로파일 정보")]
            [Description("프로파일 설명 및 메모")]
            public string Notes { get; set; }

            [Category("파라미터")]
            public SamplePreset SampleSettings { get; set; }

            [Category("파라미터")]
            public PIDParameters PIDSettings { get; set; }

            [Category("파라미터")]
            public CompensationStrategy CompensationSettings { get; set; }

            [Category("파라미터")]
            public SafetySettings SafetyLimits { get; set; }

            [Category("운영 모드")]
            [Description("초보자 모드 (추가 안전 기능)")]
            public bool BeginnerMode { get; set; } = false;

            [Category("운영 모드")]
            [Description("자동 조정 허용")]
            public bool AllowAutoAdjustment { get; set; } = true;

            public ExperimentProfile()
            {
                CreatedDate = DateTime.Now;
                CreatedBy = Environment.UserName;
                SampleSettings = new SamplePreset();
                PIDSettings = new PIDParameters();
                CompensationSettings = new CompensationStrategy();
                SafetyLimits = new SafetySettings();
            }

            public object Clone()
            {
                var clone = new ExperimentProfile
                {
                    ProfileName = ProfileName + "_Copy",
                    CreatedDate = DateTime.Now,
                    CreatedBy = Environment.UserName,
                    Version = Version,
                    Notes = Notes,
                    SampleSettings = (SamplePreset)SampleSettings?.Clone(),
                    PIDSettings = (PIDParameters)PIDSettings?.Clone(),
                    CompensationSettings = (CompensationStrategy)CompensationSettings?.Clone(),
                    SafetyLimits = (SafetySettings)SafetyLimits?.Clone(),
                    BeginnerMode = BeginnerMode,
                    AllowAutoAdjustment = AllowAutoAdjustment
                };
                return clone;
            }

            #region 파일 저장/로드

            public void SaveToFile(string filepath)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    string json = JsonSerializer.Serialize(this, options);
                    File.WriteAllText(filepath, json);
                }
                catch (Exception ex)
                {
                    throw new Exception($"프로파일 저장 실패: {ex.Message}");
                }
            }

            public static ExperimentProfile LoadFromFile(string filepath)
            {
                try
                {
                    if (!File.Exists(filepath))
                        throw new FileNotFoundException($"파일을 찾을 수 없습니다: {filepath}");

                    string json = File.ReadAllText(filepath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };

                    return JsonSerializer.Deserialize<ExperimentProfile>(json, options);
                }
                catch (Exception ex)
                {
                    throw new Exception($"프로파일 로드 실패: {ex.Message}");
                }
            }

            #endregion
        }

        #endregion

        #region 기본 프로파일 생성

        public static class DefaultProfiles
        {
            public static ExperimentProfile BeginnerSafe => new ExperimentProfile
            {
                ProfileName = "초보자_안전모드",
                Notes = "안전을 최우선으로 하는 느린 제어",
                BeginnerMode = true,
                SampleSettings = PresetLibrary.ThinFilm,
                PIDSettings = new PIDParameters
                {
                    ProportionalGain = 1.0,
                    IntegralGain = 0.2,
                    DerivativeGain = 0.05
                },
                CompensationSettings = new CompensationStrategy
                {
                    InitialBoostFactor = 1.0,
                    PredictiveWeight = 0.3,
                    EnableOvershootPrevention = true,
                    SlowdownThreshold = 20
                },
                SafetyLimits = new SafetySettings
                {
                    MaxTempDifference = 30,
                    EmergencyStopDelta = 40,
                    MaxChangeRate = 0.5,
                    MaxHeaterAdjustment = 2
                }
            };

            public static ExperimentProfile StandardBalanced => new ExperimentProfile
            {
                ProfileName = "표준_균형모드",
                Notes = "속도와 안정성의 균형",
                BeginnerMode = false,
                SampleSettings = PresetLibrary.ThinFilm,
                PIDSettings = new PIDParameters
                {
                    ProportionalGain = 2.0,
                    IntegralGain = 0.5,
                    DerivativeGain = 0.1
                },
                CompensationSettings = new CompensationStrategy
                {
                    InitialBoostFactor = 1.2,
                    PredictiveWeight = 0.6,
                    EnableOvershootPrevention = true,
                    SlowdownThreshold = 10
                },
                SafetyLimits = new SafetySettings
                {
                    MaxTempDifference = 50,
                    EmergencyStopDelta = 70,
                    MaxChangeRate = 1.0,
                    MaxHeaterAdjustment = 5
                }
            };

            public static ExperimentProfile ExpertFast => new ExperimentProfile
            {
                ProfileName = "전문가_고속모드",
                Notes = "빠른 응답, 전문가용",
                BeginnerMode = false,
                AllowAutoAdjustment = true,
                SampleSettings = PresetLibrary.ThinFilm,
                PIDSettings = new PIDParameters
                {
                    ProportionalGain = 3.0,
                    IntegralGain = 0.8,
                    DerivativeGain = 0.2
                },
                CompensationSettings = new CompensationStrategy
                {
                    InitialBoostFactor = 1.5,
                    PredictiveWeight = 0.8,
                    EnableOvershootPrevention = false,
                    SlowdownThreshold = 5
                },
                SafetyLimits = new SafetySettings
                {
                    MaxTempDifference = 70,
                    EmergencyStopDelta = 100,
                    MaxChangeRate = 2.0,
                    MaxHeaterAdjustment = 10
                }
            };
        }

        #endregion
    }
}