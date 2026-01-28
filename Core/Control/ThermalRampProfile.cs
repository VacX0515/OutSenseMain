using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 사용자 친화적인 열 램프 프로파일
    /// 복잡한 PID 파라미터를 숨기고 이해하기 쉬운 용어로 제공
    /// </summary>
    public class ThermalRampProfile : ICloneable
    {
        #region 기본 정보

        /// <summary>
        /// 프로파일 이름 (예: "박막 샘플", "벌크 금속")
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("이름")]
        [Description("프로파일을 구분하는 이름입니다.")]
        public string Name { get; set; } = "새 프로파일";

        /// <summary>
        /// 프로파일 설명
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("설명")]
        [Description("이 프로파일이 적합한 샘플 종류나 특성을 설명합니다.")]
        public string Description { get; set; } = "";

        /// <summary>
        /// 시스템 기본 프로파일 여부 (삭제 불가)
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public bool IsSystemDefault { get; set; } = false;

        /// <summary>
        /// 전체 설명 (설명 + 범위 정보 자동 생성)
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public string FullDescription
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                
                if (!string.IsNullOrEmpty(Description))
                {
                    sb.AppendLine(Description);
                    sb.AppendLine();
                }
                
                sb.AppendLine("【권장 설정 범위】");
                sb.AppendLine($"• 목표 온도: {MinTargetTemperature:F0}~{MaxHeaterTemperature:F0}°C (기본: {DefaultTargetTemperature:F0}°C)");
                sb.AppendLine($"• 승온 속도: {MinRampRate:F1}~{MaxRampRate:F1}°C/min (기본: {DefaultRampRate:F1}°C/min)");
                sb.AppendLine($"• 열 전달 지연: {HeatTransferDelay:F0}초");
                sb.AppendLine($"• 최대 온도차: {MaxHeaterSampleGap:F0}°C");
                
                return sb.ToString();
            }
        }

        #endregion

        #region 기본 설정값

        /// <summary>
        /// 기본 목표 온도 (°C) - 프로파일 선택 시 자동 설정
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("기본 목표 온도 (°C)")]
        [Description("이 프로파일 선택 시 자동으로 설정되는 목표 온도입니다.\n\n" +
                     "【설정 범위: 최소 목표 온도 ~ 히터 최대 온도】")]
        [DefaultValue(100.0)]
        public double DefaultTargetTemperature { get; set; } = 100.0;

        /// <summary>
        /// 기본 승온 속도 (°C/min) - 프로파일 선택 시 자동 설정
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("기본 승온 속도 (°C/min)")]
        [Description("이 프로파일 선택 시 자동으로 설정되는 승온 속도입니다.\n\n" +
                     "【설정 범위: 최소 승온 속도 ~ 최대 램프 속도】")]
        [DefaultValue(5.0)]
        public double DefaultRampRate { get; set; } = 5.0;

        /// <summary>
        /// 최소 목표 온도 (°C)
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("최소 목표 온도 (°C)")]
        [Description("설정 가능한 최소 목표 온도입니다.\n\n" +
                     "【설정 범위: 20~100°C】")]
        [DefaultValue(30.0)]
        public double MinTargetTemperature { get; set; } = 30.0;

        /// <summary>
        /// 최소 승온 속도 (°C/min)
        /// </summary>
        [Category("1. 기본 정보")]
        [DisplayName("최소 승온 속도 (°C/min)")]
        [Description("설정 가능한 최소 승온 속도입니다.\n\n" +
                     "【설정 범위: 0.1~5°C/min】")]
        [DefaultValue(0.5)]
        public double MinRampRate { get; set; } = 0.5;

        #endregion

        #region 열 특성 (사용자 이해 가능한 용어)

        /// <summary>
        /// 열 전달 지연 시간 (초)
        /// 히터 온도 변화가 샘플에 반영되기까지 걸리는 시간
        /// </summary>
        [Category("2. 열 특성")]
        [DisplayName("열 전달 지연 (초)")]
        [Description("히터 온도가 변할 때 샘플 온도가 따라오기까지 걸리는 시간입니다.\n" +
                     "• 박막/웨이퍼: 5~15초\n" +
                     "• 일반 시편: 20~40초\n" +
                     "• 두꺼운 금속: 40~90초\n\n" +
                     "【설정 범위: 5~120초】")]
        [DefaultValue(30.0)]
        public double HeatTransferDelay { get; set; } = 30.0;

        /// <summary>
        /// 최대 히터-샘플 온도차 (°C)
        /// 이 온도차를 넘지 않도록 히터 온도 상승을 제한
        /// </summary>
        [Category("2. 열 특성")]
        [DisplayName("최대 온도차 (°C)")]
        [Description("히터와 샘플 사이의 최대 허용 온도차입니다.\n" +
                     "열 전달이 느린 샘플일수록 큰 값이 필요합니다.\n" +
                     "• 열 전달 좋음: 20~30°C\n" +
                     "• 열 전달 보통: 30~50°C\n" +
                     "• 열 전달 나쁨: 50~80°C\n\n" +
                     "【설정 범위: 10~100°C】")]
        [DefaultValue(40.0)]
        public double MaxHeaterSampleGap { get; set; } = 40.0;

        /// <summary>
        /// 온도 안정 판정 범위 (±°C)
        /// 목표 온도 ± 이 범위 안에 들어오면 "도달"로 판정
        /// </summary>
        [Category("2. 열 특성")]
        [DisplayName("안정 판정 범위 (±°C)")]
        [Description("목표 온도에 '도달'했다고 판정하는 범위입니다.\n" +
                     "예: ±2°C 설정 시, 목표 100°C면 98~102°C에서 도달로 판정\n\n" +
                     "【설정 범위: 0.5~10°C】")]
        [DefaultValue(2.0)]
        public double TemperatureStabilityRange { get; set; } = 2.0;

        /// <summary>
        /// 안정화 유지 시간 (초)
        /// 이 시간 동안 안정 범위 내에 있어야 "안정화 완료"로 판정
        /// </summary>
        [Category("2. 열 특성")]
        [DisplayName("안정화 유지 시간 (초)")]
        [Description("온도가 목표 범위 안에서 이 시간 동안 유지되어야 '안정화 완료'로 판정합니다.\n\n" +
                     "【설정 범위: 10~300초】")]
        [DefaultValue(30.0)]
        public double StabilizationTime { get; set; } = 30.0;

        #endregion

        #region 제어 강도 (단순화)

        /// <summary>
        /// 제어 반응 속도 (1~10)
        /// 1: 매우 느림/안정적, 10: 매우 빠름/민감
        /// </summary>
        [Category("3. 제어 설정")]
        [DisplayName("제어 반응 속도 (1~10)")]
        [Description("온도 변화에 대한 제어 반응 속도입니다.\n" +
                     "• 1~3: 느리고 안정적 (오버슈트 최소화)\n" +
                     "• 4~6: 균형 잡힌 반응\n" +
                     "• 7~10: 빠른 반응 (진동 가능성 있음)\n\n" +
                     "【설정 범위: 1~10】")]
        [DefaultValue(5)]
        public int ControlResponsiveness { get; set; } = 5;

        /// <summary>
        /// 램프 시작 시 부스트 사용
        /// </summary>
        [Category("3. 제어 설정")]
        [DisplayName("초기 부스트 사용")]
        [Description("램프 시작 시 히터를 목표보다 약간 높게 설정하여 빠른 승온을 유도합니다.")]
        [DefaultValue(true)]
        public bool UseInitialBoost { get; set; } = true;

        /// <summary>
        /// 목표 근처 감속 사용
        /// </summary>
        [Category("3. 제어 설정")]
        [DisplayName("목표 근처 감속")]
        [Description("목표 온도 근처에서 오버슈트를 방지하기 위해 승온 속도를 줄입니다.")]
        [DefaultValue(true)]
        public bool UseTargetSlowdown { get; set; } = true;

        #endregion

        #region 안전 설정

        /// <summary>
        /// 히터 최대 온도 (°C)
        /// </summary>
        [Category("4. 안전 설정")]
        [DisplayName("히터 최대 온도 (°C)")]
        [Description("히터가 절대 넘지 않는 최대 온도입니다.\n" +
                     "샘플이나 장비 보호를 위해 설정합니다.\n\n" +
                     "【설정 범위: 50~350°C】")]
        [DefaultValue(200.0)]
        public double MaxHeaterTemperature { get; set; } = 200.0;

        /// <summary>
        /// 비상정지 온도차 (°C)
        /// 히터-샘플 온도차가 이 값을 초과하면 즉시 정지
        /// </summary>
        [Category("4. 안전 설정")]
        [DisplayName("비상정지 온도차 (°C)")]
        [Description("히터와 샘플의 온도차가 이 값을 초과하면 즉시 정지합니다.\n" +
                     "최대 온도차보다 20~30°C 높게 설정하세요.\n\n" +
                     "【설정 범위: 30~150°C】")]
        [DefaultValue(70.0)]
        public double EmergencyStopGap { get; set; } = 70.0;

        /// <summary>
        /// 최대 램프 속도 (°C/min)
        /// </summary>
        [Category("4. 안전 설정")]
        [DisplayName("최대 램프 속도 (°C/min)")]
        [Description("허용되는 최대 승온 속도입니다.\n" +
                     "너무 빠른 승온은 열충격을 유발할 수 있습니다.\n\n" +
                     "【설정 범위: 0.5~50°C/min】")]
        [DefaultValue(20.0)]
        public double MaxRampRate { get; set; } = 20.0;

        #endregion

        #region 내부 변환 (PID 파라미터로)

        /// <summary>
        /// 사용자 설정을 내부 PID 게인으로 변환
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public double InternalKp => 0.5 + (ControlResponsiveness * 0.3);

        [Browsable(false)]
        [JsonIgnore]
        public double InternalKi => 0.1 + (ControlResponsiveness * 0.05);

        [Browsable(false)]
        [JsonIgnore]
        public double InternalKd => 0.02 + (ControlResponsiveness * 0.02);

        /// <summary>
        /// 열 전달 지연을 기반으로 한 보상 계수
        /// </summary>
        [Browsable(false)]
        [JsonIgnore]
        public double ThermalCompensationFactor => 1.0 + (HeatTransferDelay / 60.0);

        #endregion

        #region 메서드

        public object Clone()
        {
            return new ThermalRampProfile
            {
                Name = this.Name + " (복사)",
                Description = this.Description,
                IsSystemDefault = false,
                // 기본 설정값
                DefaultTargetTemperature = this.DefaultTargetTemperature,
                DefaultRampRate = this.DefaultRampRate,
                MinTargetTemperature = this.MinTargetTemperature,
                MinRampRate = this.MinRampRate,
                // 열 특성
                HeatTransferDelay = this.HeatTransferDelay,
                MaxHeaterSampleGap = this.MaxHeaterSampleGap,
                TemperatureStabilityRange = this.TemperatureStabilityRange,
                StabilizationTime = this.StabilizationTime,
                // 제어 설정
                ControlResponsiveness = this.ControlResponsiveness,
                UseInitialBoost = this.UseInitialBoost,
                UseTargetSlowdown = this.UseTargetSlowdown,
                // 안전 설정
                MaxHeaterTemperature = this.MaxHeaterTemperature,
                EmergencyStopGap = this.EmergencyStopGap,
                MaxRampRate = this.MaxRampRate
            };
        }

        public override string ToString() => Name;

        #endregion
    }

    /// <summary>
    /// 프로파일 컬렉션 (JSON 저장용)
    /// </summary>
    public class ThermalRampProfileCollection
    {
        public List<ThermalRampProfile> Profiles { get; set; } = new List<ThermalRampProfile>();
        public string LastSelectedProfile { get; set; } = "";
        public DateTime LastModified { get; set; } = DateTime.Now;
    }
}
