using System;
using System.IO;
using System.Xml.Serialization;

namespace VacX_OutSense.Core.Safety
{
    /// <summary>
    /// 인터락 설정 — 각 안전 인터락의 활성화/비활성화를 관리
    /// </summary>
    [Serializable]
    public class InterlockConfiguration
    {
        #region 밸브 인터락

        /// <summary>
        /// 벤트밸브: 터보펌프 작동 시 조작 차단
        /// </summary>
        public bool VentValve_BlockIfTurboRunning { get; set; } = true;

        /// <summary>
        /// 배기밸브: 터보펌프 작동 시 조작 차단
        /// </summary>
        public bool ExhaustValve_BlockIfTurboRunning { get; set; } = true;

        /// <summary>
        /// 게이트밸브: 터보펌프 작동 시 닫기 차단
        /// </summary>
        public bool GateValveClose_BlockIfTurboRunning { get; set; } = true;

        #endregion

        #region 드라이펌프 인터락

        /// <summary>
        /// 드라이펌프 시작: 게이트밸브 열림 필요
        /// </summary>
        public bool DryPump_RequireGateValveOpen { get; set; } = true;

        /// <summary>
        /// 드라이펌프 시작: 벤트/배기밸브 닫힘 필요
        /// </summary>
        public bool DryPump_RequireVentExhaustClosed { get; set; } = true;

        /// <summary>
        /// 드라이펌프 정지: 터보펌프 작동 시 차단
        /// </summary>
        public bool DryPumpStop_BlockIfTurboRunning { get; set; } = true;

        #endregion

        #region 터보펌프 인터락

        /// <summary>
        /// 터보펌프 시작: 드라이펌프 작동 필요
        /// </summary>
        public bool TurboPump_RequireDryPumpRunning { get; set; } = true;

        /// <summary>
        /// 터보펌프 시작: 챔버 압력 ≤ 1 Torr
        /// </summary>
        public bool TurboPump_RequirePressureBelow1Torr { get; set; } = true;

        /// <summary>
        /// 터보펌프 시작: 칠러 작동 필요
        /// </summary>
        public bool TurboPump_RequireChillerRunning { get; set; } = true;

        /// <summary>
        /// 터보펌프 시작: 게이트밸브 열림 필요
        /// </summary>
        public bool TurboPump_RequireGateValveOpen { get; set; } = true;

        #endregion

        #region 이온게이지 인터락

        /// <summary>
        /// 이온게이지 HV ON: 피라니 압력 ≤ 1E-3 Torr 필요
        /// </summary>
        public bool IonGaugeHV_RequireLowPressure { get; set; } = true;

        #endregion

        #region 히터 인터락

        /// <summary>
        /// 히터 시작: 진공 미달 시 경고
        /// </summary>
        public bool HeaterStart_WarnIfNoVacuum { get; set; } = true;

        #endregion

        #region 칠러 인터락

        /// <summary>
        /// 칠러 정지: 터보펌프 작동 시 차단
        /// </summary>
        public bool ChillerStop_BlockIfTurboRunning { get; set; } = true;

        #endregion

        #region 오토런 보호

        /// <summary>
        /// 오토런 실행 중 밸브 수동 조작 차단
        /// </summary>
        public bool AutoRun_BlockManualValveControl { get; set; } = true;

        /// <summary>
        /// 오토런 실행 중 펌프 수동 조작 차단
        /// </summary>
        public bool AutoRun_BlockManualPumpControl { get; set; } = true;

        /// <summary>
        /// 오토런 실행 중 이온게이지 수동 조작 차단
        /// </summary>
        public bool AutoRun_BlockManualIonGaugeControl { get; set; } = true;

        /// <summary>
        /// 오토런 실행 중 히터 수동 조작 차단
        /// </summary>
        public bool AutoRun_BlockManualHeaterControl { get; set; } = false;

        #endregion

        #region 메서드

        private static readonly string DefaultPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config", "InterlockConfig.xml");

        public void SaveToFile(string filePath = null)
        {
            filePath = filePath ?? DefaultPath;
            try
            {
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(InterlockConfiguration));
                using (var writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"인터락 설정 저장 실패: {ex.Message}", ex);
            }
        }

        public static InterlockConfiguration LoadFromFile(string filePath = null)
        {
            filePath = filePath ?? DefaultPath;
            try
            {
                if (!File.Exists(filePath))
                    return new InterlockConfiguration();

                var serializer = new XmlSerializer(typeof(InterlockConfiguration));
                using (var reader = new StreamReader(filePath))
                {
                    return (InterlockConfiguration)serializer.Deserialize(reader);
                }
            }
            catch
            {
                return new InterlockConfiguration();
            }
        }

        public void ResetToDefaults()
        {
            var defaults = new InterlockConfiguration();

            VentValve_BlockIfTurboRunning = defaults.VentValve_BlockIfTurboRunning;
            ExhaustValve_BlockIfTurboRunning = defaults.ExhaustValve_BlockIfTurboRunning;
            GateValveClose_BlockIfTurboRunning = defaults.GateValveClose_BlockIfTurboRunning;

            DryPump_RequireGateValveOpen = defaults.DryPump_RequireGateValveOpen;
            DryPump_RequireVentExhaustClosed = defaults.DryPump_RequireVentExhaustClosed;
            DryPumpStop_BlockIfTurboRunning = defaults.DryPumpStop_BlockIfTurboRunning;

            TurboPump_RequireDryPumpRunning = defaults.TurboPump_RequireDryPumpRunning;
            TurboPump_RequirePressureBelow1Torr = defaults.TurboPump_RequirePressureBelow1Torr;
            TurboPump_RequireChillerRunning = defaults.TurboPump_RequireChillerRunning;
            TurboPump_RequireGateValveOpen = defaults.TurboPump_RequireGateValveOpen;

            IonGaugeHV_RequireLowPressure = defaults.IonGaugeHV_RequireLowPressure;
            HeaterStart_WarnIfNoVacuum = defaults.HeaterStart_WarnIfNoVacuum;
            ChillerStop_BlockIfTurboRunning = defaults.ChillerStop_BlockIfTurboRunning;

            AutoRun_BlockManualValveControl = defaults.AutoRun_BlockManualValveControl;
            AutoRun_BlockManualPumpControl = defaults.AutoRun_BlockManualPumpControl;
            AutoRun_BlockManualIonGaugeControl = defaults.AutoRun_BlockManualIonGaugeControl;
            AutoRun_BlockManualHeaterControl = defaults.AutoRun_BlockManualHeaterControl;
        }

        #endregion
    }
}
