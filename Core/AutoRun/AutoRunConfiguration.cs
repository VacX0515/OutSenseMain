using System;
using System.Xml.Serialization;

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
        public double TargetPressureForIonGauge { get; set; } = 1E-3;

        /// <summary>
        /// 히터 시작을 위한 목표 압력 (Torr)
        /// </summary>
        public double TargetPressureForHeater { get; set; } = 1E-5;

        /// <summary>
        /// 실험 중 최대 허용 압력 (Torr)
        /// </summary>
        public double MaxPressureDuringExperiment { get; set; } = 1E-4;

        #endregion

        #region 온도 설정

        /// <summary>
        /// 칠러 설정 온도 (°C)
        /// </summary>
        public double ChillerSetTemperature { get; set; } = 20.0;

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
        public double TemperatureStabilityTolerance { get; set; } = 1.0;

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
        public double CoolingTargetTemperature { get; set; } = 40.0;

        /// <summary>
        /// 벤트 후 배기 밸브 오픈 기준 압력 (kPa) — ATM 스위치 기준
        /// </summary>
        public double VentTargetPressure_kPa { get; set; } = 80.0;

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

            // 온도 설정
            ChillerSetTemperature = defaultConfig.ChillerSetTemperature;
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
            VentTargetPressure_kPa = defaultConfig.VentTargetPressure_kPa;

            // 기타 설정
            RunMode = defaultConfig.RunMode;
            MaxRetryCount = defaultConfig.MaxRetryCount;
            RetryDelaySeconds = defaultConfig.RetryDelaySeconds;
            EnableDetailedLogging = defaultConfig.EnableDetailedLogging;
            EnableSafeShutdownOnFailure = defaultConfig.EnableSafeShutdownOnFailure;
            EnableAlarmOnError = defaultConfig.EnableAlarmOnError;
        }

        #endregion
    }
}