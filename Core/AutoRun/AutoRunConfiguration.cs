using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using VacX_OutSense.Core.Control;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// AutoRun 시퀀스 설정
    /// </summary>
    [Serializable]
    public class AutoRunConfiguration
    {
        #region 압력 설정

        /// <summary>
        /// 터보펌프 시작을 위한 목표 압력 (Torr)
        /// </summary>
        public double TargetPressureForTurboPump { get; set; } = 1.0;

        /// <summary>
        /// 이온게이지 활성화를 위한 목표 압력 (Torr)
        /// </summary>
        public double TargetPressureForIonGauge { get; set; } = 7.5E-4;

        /// <summary>
        /// 히터 시작을 위한 목표 압력 (Torr)
        /// </summary>
        public double TargetPressureForHeater { get; set; } = 7.5E-6;

        /// <summary>
        /// 실험 중 최대 허용 압력 (Torr)
        /// </summary>
        public double MaxPressureDuringExperiment { get; set; } = 1E-4;

        /// <summary>
        /// 압력 인터락 지속 시간 (초) — 이 시간 이상 연속 초과 시 SV 감소/중단 동작
        /// 0이면 기본값 사용 (SV감소: 15초, 중단: 30초)
        /// </summary>
        public int PressureInterlockDurationSeconds { get; set; } = 0;

        #endregion

        #region 온도 설정

        /// <summary>
        /// 히터 CH1 설정 온도 (°C)
        /// </summary>
        public double HeaterCh1SetTemperature { get; set; } = 100.0;

        /// <summary>
        /// 히터 램프 업 속도 (°C/min)
        /// </summary>
        public double HeaterRampUpRate { get; set; } = 5.0;

        /// <summary>
        /// 온도 안정성 허용 범위 (±°C)
        /// </summary>
        public double TemperatureStabilityTolerance { get; set; } = 0.5;

        #endregion

        #region 시간 설정

        /// <summary>
        /// 실험 지속 시간 (분) — 기본값 1440분 (24시간)
        /// </summary>
        public int ExperimentDurationMinutes { get; set; } = 1440;

        /// <summary>
        /// 데이터 로깅 간격 (초)
        /// </summary>
        public int DataLoggingIntervalSeconds { get; set; } = 60;

        #endregion

        #region 타임아웃 설정

        /// <summary>
        /// 초기화 타임아웃 (초)
        /// </summary>
        public int InitializationTimeout { get; set; } = 60;

        /// <summary>
        /// 밸브 작동 타임아웃 (초)
        /// </summary>
        public int ValveOperationTimeout { get; set; } = 30;

        /// <summary>
        /// 드라이펌프 시작 타임아웃 (초)
        /// </summary>
        public int DryPumpStartTimeout { get; set; } = 60;

        /// <summary>
        /// 터보펌프 시작 타임아웃 (초)
        /// </summary>
        public int TurboPumpStartTimeout { get; set; } = 600; // 10분

        /// <summary>
        /// 이온게이지 활성화 타임아웃 (초)
        /// </summary>
        public int IonGaugeActivationTimeout { get; set; } = 30;

        /// <summary>
        /// 고진공 도달 타임아웃 (초)
        /// </summary>
        public int HighVacuumTimeout { get; set; } = 3600; // 60분

        /// <summary>
        /// 히터 시작 타임아웃 (초)
        /// </summary>
        public int HeaterStartTimeout { get; set; } = 60;

        /// <summary>
        /// 종료 시퀀스 타임아웃 (초) — 쿨링 대기 포함, 기본 7200초 (2시간)
        /// </summary>
        public int ShutdownTimeout { get; set; } = 7200; // 2시간

        /// <summary>
        /// 종료 시 CH1 쿨링 목표 온도 (°C) — 이 온도 이하로 내려가면 벤트/배기 밸브 닫기
        /// </summary>
        public double CoolingTargetTemperature { get; set; } = 50.0;

        /// <summary>
        /// 벤팅 시작 온도 (°C) — CH1이 이 온도 이하로 내려가야 벤트 밸브를 연다
        /// (너무 뜨거운 상태에서 벤트하면 챔버 과열 위험)
        /// </summary>
        public double VentingStartTemperature { get; set; } = 125.0;

        /// <summary>
        /// 벤트 후 배기 밸브 오픈 기준 압력 (kPa) — ATM 스위치 기준
        /// </summary>
        public double VentTargetPressure_kPa { get; set; } = 90.0;

        /// <summary>
        /// 벤팅 온도 대기 타임아웃 (초) — 벤팅 시작 온도까지 대기하는 최대 시간
        /// </summary>
        public int VentingTempWaitTimeout { get; set; } = 5400; // 90분

        /// <summary>
        /// ATM 압력 대기 타임아웃 (초) — 벤트 후 대기압 도달까지 대기하는 최대 시간
        /// </summary>
        public int AtmPressureWaitTimeout { get; set; } = 600; // 10분

        /// <summary>
        /// 쿨링 대기 타임아웃 (초) — 쿨링 목표 온도까지 대기하는 최대 시간
        /// </summary>
        public int CoolingWaitTimeout { get; set; } = 5400; // 90분

        /// <summary>
        /// 터보펌프 감속 대기 타임아웃 (초)
        /// </summary>
        public int TurboPumpDecelerationTimeout { get; set; } = 600; // 10분

        #endregion

        #region 기타 설정

        /// <summary>
        /// 실행 모드
        /// </summary>
        public AutoRunMode RunMode { get; set; } = AutoRunMode.FullAuto;

        /// <summary>
        /// 오류 발생 시 자동 재시도 횟수
        /// </summary>
        public int MaxRetryCount { get; set; } = 3;

        /// <summary>
        /// 재시도 대기 시간 (초)
        /// </summary>
        public int RetryDelaySeconds { get; set; } = 10;

        /// <summary>
        /// 상세 로깅 활성화
        /// </summary>
        public bool EnableDetailedLogging { get; set; } = true;

        /// <summary>
        /// 실패 시 안전 종료 활성화
        /// </summary>
        public bool EnableSafeShutdownOnFailure { get; set; } = true;

        /// <summary>
        /// 오류 발생 시 알람 활성화
        /// </summary>
        public bool EnableAlarmOnError { get; set; } = false;

        #endregion

        #region 실험 유형 설정

        /// <summary>
        /// 실험 유형 (OutgassingRate 또는 Bakeout)
        /// </summary>
        public ExperimentType ExperimentType { get; set; } = ExperimentType.OutgassingRate;

        /// <summary>
        /// [Bakeout] 목표 온도 (°C)
        /// </summary>
        public double BakeoutTargetTemperature { get; set; } = 100.0;

        /// <summary>
        /// [Bakeout] 승온 속도 (°C/h)
        /// </summary>
        public double BakeoutRampRate { get; set; } = 2.0;

        /// <summary>
        /// [Bakeout] 유지 시간 (분) — 목표 온도 도달 후
        /// </summary>
        public int BakeoutHoldTimeMinutes { get; set; } = 30;

        /// <summary>
        /// [Bakeout] 종료 동작
        /// </summary>
        public BakeoutEndAction BakeoutEndAction { get; set; } = BakeoutEndAction.HeaterOff;

        /// <summary>
        /// [Bakeout] 히터(CH1) 안전 상한 온도 (°C) — 피드백 제어 시 CH1이 이 온도를 초과하지 않음
        /// </summary>
        public double BakeoutHeaterMaxTemperature { get; set; } = 150.0;

        /// <summary>
        /// [Bakeout] 승온 중 CH1-샘플 최대 허용 온도차 (°C)
        /// CH1 SV가 샘플 온도 + 이 값을 초과하지 않도록 제한합니다.
        /// 0이면 제한 없음 (절대 상한만 적용).
        /// </summary>
        public double BakeoutMaxDeltaT { get; set; } = 50.0;

        /// <summary>
        /// [Bakeout] 승온 타임아웃 (분) — 0이면 자동 계산 (램프 속도 기반 × 3 + 30분)
        /// </summary>
        public int BakeoutRiseTimeoutMinutes { get; set; } = 0;

        /// <summary>
        /// [Bakeout] 감속 구간 — 더 이상 사용하지 않음 (적응형 자동 계산으로 대체)
        /// XML 하위호환을 위해 속성만 유지
        /// </summary>
        public double BakeoutDecelerationZone { get; set; } = 0;

        /// <summary>
        /// [Bakeout] PI 피드백 주기 (초) — CH1 SV 변경 간격
        /// </summary>
        public double BakeoutFeedbackIntervalSec { get; set; } = 5.0;

        /// <summary>
        /// [Bakeout] 목표 온도 도달 허용오차 (°C) — 목표 - 허용오차 이상이면 도달로 판정
        /// </summary>
        public double BakeoutTolerance { get; set; } = 1.0;

        /// <summary>
        /// [Bakeout] 온도 안정화 유지 시간 (초) — 목표±허용오차 범위 내에서 이 시간 동안
        /// 연속 유지되어야 홀드 타이머가 시작됨. 0이면 즉시 시작 (기존 동작).
        /// </summary>
        public int BakeoutStabilizationSeconds { get; set; } = 600;

        /// <summary>
        /// [Bakeout] 샘플 온도 모니터링 채널 (1~5) — 하위호환용 (단일 채널)
        /// </summary>
        public int BakeoutMonitorChannel { get; set; } = 2;

        /// <summary>
        /// [Bakeout] 다중 모니터 채널 선택 — 선택된 채널 중 MAX 온도로 제어
        /// </summary>
        public bool BakeoutMonitorCh1 { get; set; } = false;
        public bool BakeoutMonitorCh2 { get; set; } = true;
        public bool BakeoutMonitorCh3 { get; set; } = false;
        public bool BakeoutMonitorCh4 { get; set; } = false;
        public bool BakeoutMonitorCh5 { get; set; } = false;
        public bool BakeoutMonitorCh6 { get; set; } = false;
        public bool BakeoutMonitorCh7 { get; set; } = false;
        public bool BakeoutMonitorCh8 { get; set; } = false;

        /// <summary>
        /// [Bakeout] 프로파일 이름
        /// </summary>
        public string BakeoutProfileName { get; set; } = "일반 시편";

        #endregion

        #region 다중 모니터 채널 헬퍼

        /// <summary>
        /// 선택된 모니터 채널 목록 반환.
        /// 다중 채널이 설정되어 있으면 사용, 없으면 기존 BakeoutMonitorChannel 폴백.
        /// </summary>
        public List<int> GetBakeoutMonitorChannels()
        {
            var channels = new List<int>();
            if (BakeoutMonitorCh1) channels.Add(1);
            if (BakeoutMonitorCh2) channels.Add(2);
            if (BakeoutMonitorCh3) channels.Add(3);
            if (BakeoutMonitorCh4) channels.Add(4);
            if (BakeoutMonitorCh5) channels.Add(5);
            if (BakeoutMonitorCh6) channels.Add(6);
            if (BakeoutMonitorCh7) channels.Add(7);
            if (BakeoutMonitorCh8) channels.Add(8);

            if (channels.Count == 0)
                channels.Add(BakeoutMonitorChannel);

            return channels;
        }

        /// <summary>
        /// 모니터 채널 표시 텍스트 (예: "CH2", "CH2+3 MAX")
        /// </summary>
        public string GetBakeoutMonitorLabel()
        {
            var channels = GetBakeoutMonitorChannels();
            if (channels.Count == 1) return $"CH{channels[0]}";
            return $"CH{string.Join("+", channels)} MAX";
        }

        #endregion

        #region XML 하위호환 (기존 설정 파일 로드용)

        /// <summary>
        /// [하위호환] 기존 ExperimentDurationHours 로드 시 분으로 변환
        /// </summary>
        [XmlElement("ExperimentDurationHours")]
        public int ExperimentDurationHoursCompat
        {
            get => 0; // 저장 시 0 → XML에 포함 안 됨 (ShouldSerialize로 제어)
            set
            {
                if (value > 0)
                    ExperimentDurationMinutes = value * 60;
            }
        }

        /// <summary>
        /// ExperimentDurationHoursCompat는 저장하지 않음
        /// </summary>
        public bool ShouldSerializeExperimentDurationHoursCompat() => false;

        /// <summary>
        /// [하위호환] 기존 HeaterCh2SetTemperature 로드 시 무시
        /// </summary>
        [XmlElement("HeaterCh2SetTemperature")]
        public double HeaterCh2SetTemperatureCompat
        {
            get => 0;
            set { /* 무시 — CH2는 칠러 PID가 제어 */ }
        }

        /// <summary>
        /// HeaterCh2SetTemperatureCompat는 저장하지 않음
        /// </summary>
        public bool ShouldSerializeHeaterCh2SetTemperatureCompat() => false;

        /// <summary>
        /// [하위호환] 기존 XML에 ChillerSetTemperature가 있으면 읽기만 함 (PID가 제어하므로 불필요)
        /// </summary>
        [XmlElement("ChillerSetTemperature")]
        public double ChillerSetTemperatureCompat
        {
            get => 0;
            set { /* 무시 — 칠러 온도는 PID가 제어 */ }
        }

        /// <summary>
        /// ChillerSetTemperatureCompat는 저장하지 않음
        /// </summary>
        public bool ShouldSerializeChillerSetTemperatureCompat() => false;

        #endregion

        #region 메서드

        /// <summary>
        /// 설정을 파일로 저장
        /// </summary>
        public void SaveToFile(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AutoRunConfiguration));
                using (var writer = new System.IO.StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"설정 저장 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 파일에서 설정 불러오기
        /// </summary>
        public static AutoRunConfiguration LoadFromFile(string filePath)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(AutoRunConfiguration));
                using (var reader = new System.IO.StreamReader(filePath))
                {
                    return (AutoRunConfiguration)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"설정 불러오기 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 기본 설정으로 초기화
        /// </summary>
        public void ResetToDefaults()
        {
            var defaultConfig = new AutoRunConfiguration();

            // 압력 설정
            TargetPressureForTurboPump = defaultConfig.TargetPressureForTurboPump;
            TargetPressureForIonGauge = defaultConfig.TargetPressureForIonGauge;
            TargetPressureForHeater = defaultConfig.TargetPressureForHeater;
            MaxPressureDuringExperiment = defaultConfig.MaxPressureDuringExperiment;
            PressureInterlockDurationSeconds = defaultConfig.PressureInterlockDurationSeconds;

            // 온도 설정
            HeaterCh1SetTemperature = defaultConfig.HeaterCh1SetTemperature;
            HeaterRampUpRate = defaultConfig.HeaterRampUpRate;
            TemperatureStabilityTolerance = defaultConfig.TemperatureStabilityTolerance;

            // 시간 설정
            ExperimentDurationMinutes = defaultConfig.ExperimentDurationMinutes;
            DataLoggingIntervalSeconds = defaultConfig.DataLoggingIntervalSeconds;

            // 타임아웃 설정
            InitializationTimeout = defaultConfig.InitializationTimeout;
            ValveOperationTimeout = defaultConfig.ValveOperationTimeout;
            DryPumpStartTimeout = defaultConfig.DryPumpStartTimeout;
            TurboPumpStartTimeout = defaultConfig.TurboPumpStartTimeout;
            IonGaugeActivationTimeout = defaultConfig.IonGaugeActivationTimeout;
            HighVacuumTimeout = defaultConfig.HighVacuumTimeout;
            HeaterStartTimeout = defaultConfig.HeaterStartTimeout;
            ShutdownTimeout = defaultConfig.ShutdownTimeout;
            CoolingTargetTemperature = defaultConfig.CoolingTargetTemperature;
            VentingStartTemperature = defaultConfig.VentingStartTemperature;
            VentTargetPressure_kPa = defaultConfig.VentTargetPressure_kPa;
            VentingTempWaitTimeout = defaultConfig.VentingTempWaitTimeout;
            AtmPressureWaitTimeout = defaultConfig.AtmPressureWaitTimeout;
            CoolingWaitTimeout = defaultConfig.CoolingWaitTimeout;
            TurboPumpDecelerationTimeout = defaultConfig.TurboPumpDecelerationTimeout;

            // 기타 설정
            RunMode = defaultConfig.RunMode;
            MaxRetryCount = defaultConfig.MaxRetryCount;
            RetryDelaySeconds = defaultConfig.RetryDelaySeconds;
            EnableDetailedLogging = defaultConfig.EnableDetailedLogging;
            EnableSafeShutdownOnFailure = defaultConfig.EnableSafeShutdownOnFailure;
            EnableAlarmOnError = defaultConfig.EnableAlarmOnError;

            // 실험 유형 설정
            ExperimentType = defaultConfig.ExperimentType;
            BakeoutTargetTemperature = defaultConfig.BakeoutTargetTemperature;
            BakeoutRampRate = defaultConfig.BakeoutRampRate;
            BakeoutHoldTimeMinutes = defaultConfig.BakeoutHoldTimeMinutes;
            BakeoutEndAction = defaultConfig.BakeoutEndAction;
            BakeoutHeaterMaxTemperature = defaultConfig.BakeoutHeaterMaxTemperature;
            BakeoutMaxDeltaT = defaultConfig.BakeoutMaxDeltaT;
            BakeoutRiseTimeoutMinutes = defaultConfig.BakeoutRiseTimeoutMinutes;
            BakeoutDecelerationZone = defaultConfig.BakeoutDecelerationZone;
            BakeoutFeedbackIntervalSec = defaultConfig.BakeoutFeedbackIntervalSec;
            BakeoutStabilizationSeconds = defaultConfig.BakeoutStabilizationSeconds;
            BakeoutMonitorChannel = defaultConfig.BakeoutMonitorChannel;
            BakeoutMonitorCh1 = defaultConfig.BakeoutMonitorCh1;
            BakeoutMonitorCh2 = defaultConfig.BakeoutMonitorCh2;
            BakeoutMonitorCh3 = defaultConfig.BakeoutMonitorCh3;
            BakeoutMonitorCh4 = defaultConfig.BakeoutMonitorCh4;
            BakeoutMonitorCh5 = defaultConfig.BakeoutMonitorCh5;
            BakeoutMonitorCh6 = defaultConfig.BakeoutMonitorCh6;
            BakeoutMonitorCh7 = defaultConfig.BakeoutMonitorCh7;
            BakeoutMonitorCh8 = defaultConfig.BakeoutMonitorCh8;
            BakeoutProfileName = defaultConfig.BakeoutProfileName;
        }

        #endregion
    }
}