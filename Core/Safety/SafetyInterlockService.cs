using System;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.Safety
{
    public enum WatchdogAction
    {
        StopTurboPump,
        StopHeater,
    }

    /// <summary>
    /// 최상단 인터락 서비스 — 모든 장치 제어의 최종 안전 게이트
    /// UI, AutoRun, 내부 코드 등 어디서 호출하든 반드시 이 서비스를 통과해야 함
    /// </summary>
    public class SafetyInterlockService
    {
        private readonly MainForm _mainForm;

        public InterlockConfiguration Config { get; set; }

        /// <summary>종료 시퀀스 모드 — 인터락 우회 (밸브 열기 등)</summary>
        public bool ShutdownSequenceMode { get; set; }

        /// <summary>인터락 위반 시 발생 (로그/알림용)</summary>
        public event EventHandler<string> InterlockViolation;

        public SafetyInterlockService(MainForm mainForm, InterlockConfiguration config)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));
            Config = config ?? new InterlockConfiguration();
        }

        #region 밸브 인터락

        /// <summary>벤트 밸브 열기 가능 여부</summary>
        public bool CanOpenVentValve(out string reason)
        {
            reason = null;
            // 고온 인터락은 종료 시퀀스에서도 항상 적용
            if (!CheckHighTemperatureBlock(out reason))
                return false;
            if (!CheckHeaterRunningBlock(out reason))
                return false;
            if (ShutdownSequenceMode) return true; // 나머지(터보펌프 등)는 우회
            if (Config.VentValve_BlockIfTurboRunning && IsTurboPumpRunning())
            {
                reason = "터보펌프 작동 중에는 벤트 밸브를 열 수 없습니다.";
                return false;
            }
            return true;
        }

        /// <summary>배기 밸브 열기 가능 여부</summary>
        public bool CanOpenExhaustValve(out string reason)
        {
            reason = null;
            // 고온 인터락은 종료 시퀀스에서도 항상 적용
            if (!CheckHighTemperatureBlock(out reason))
                return false;
            if (!CheckHeaterRunningBlock(out reason))
                return false;
            if (ShutdownSequenceMode) return true; // 나머지(터보펌프 등)는 우회
            if (Config.ExhaustValve_BlockIfTurboRunning && IsTurboPumpRunning())
            {
                reason = "터보펌프 작동 중에는 배기 밸브를 열 수 없습니다.";
                return false;
            }
            return true;
        }

        /// <summary>히터 작동 중 벤트/배기 밸브 차단 체크</summary>
        private bool CheckHeaterRunningBlock(out string reason)
        {
            reason = null;
            if (!Config.VentExhaust_BlockIfHeaterRunning)
                return true;

            if (IsHeaterRunning())
            {
                reason = "히터가 작동 중에는 벤트/배기 밸브를 열 수 없습니다. 히터를 먼저 정지하세요.";
                return false;
            }
            return true;
        }

        /// <summary>고온 시 벤트/배기 밸브 차단 체크</summary>
        private bool CheckHighTemperatureBlock(out string reason)
        {
            reason = null;
            if (!Config.VentExhaust_BlockIfHighTemperature)
                return true;

            double ch1Temp = GetCh1Temperature();
            if (ch1Temp > Config.VentExhaust_MaxTemperature)
            {
                reason = $"CH1 온도가 {ch1Temp:F1}°C로 높습니다. ({Config.VentExhaust_MaxTemperature:F0}°C 이하에서만 밸브 열기 가능)";
                return false;
            }
            return true;
        }

        /// <summary>게이트 밸브 닫기 가능 여부</summary>
        public bool CanCloseGateValve(out string reason)
        {
            reason = null;
            if (Config.GateValveClose_BlockIfTurboRunning && IsTurboPumpRunning())
            {
                reason = "터보펌프 작동 중에는 게이트 밸브를 닫을 수 없습니다.";
                return false;
            }
            return true;
        }

        /// <summary>게이트 밸브 열기 가능 여부</summary>
        public bool CanOpenGateValve(out string reason)
        {
            reason = null;
            if (Config.GateValveOpen_RequireAtmPressure)
            {
                double atmPressure = GetAtmPressureKPa();
                if (atmPressure < 80)
                {
                    reason = $"대기압이 부족합니다. (현재: {atmPressure:F1} kPa, 필요: ≥80 kPa)";
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region 펌프 인터락

        /// <summary>드라이펌프 시작 가능 여부</summary>
        public bool CanStartDryPump(out string reason)
        {
            reason = null;
            if (Config.DryPump_RequireGateValveOpen && !IsGateValveOpen())
            {
                reason = "게이트 밸브가 열려있지 않습니다.";
                return false;
            }
            if (Config.DryPump_RequireVentExhaustClosed && (IsVentValveOpen() || IsExhaustValveOpen()))
            {
                reason = "벤트 또는 배기 밸브가 열려있습니다.";
                return false;
            }
            return true;
        }

        /// <summary>드라이펌프 정지 가능 여부</summary>
        public bool CanStopDryPump(out string reason)
        {
            reason = null;
            if (Config.DryPumpStop_BlockIfTurboRunning && IsTurboPumpRunning())
            {
                reason = "터보펌프 작동 중에는 드라이펌프를 정지할 수 없습니다.";
                return false;
            }
            return true;
        }

        /// <summary>터보펌프 시작 가능 여부</summary>
        public bool CanStartTurboPump(out string reason)
        {
            reason = null;
            if (Config.TurboPump_RequireDryPumpRunning && !IsDryPumpRunning())
            {
                reason = "드라이펌프가 작동 중이 아닙니다.";
                return false;
            }
            if (Config.TurboPump_RequirePressureBelow1Torr && GetPressureTorr() > 1)
            {
                reason = "챔버 압력이 너무 높습니다.";
                return false;
            }
            if (Config.TurboPump_RequireChillerRunning && !IsChillerRunning())
            {
                reason = "칠러가 작동 중이 아닙니다.";
                return false;
            }
            if (Config.TurboPump_RequireGateValveOpen && !IsGateValveOpen())
            {
                reason = "게이트 밸브가 열려있지 않습니다.";
                return false;
            }
            return true;
        }

        /// <summary>칠러 정지 가능 여부</summary>
        public bool CanStopChiller(out string reason)
        {
            reason = null;
            if (Config.ChillerStop_BlockIfTurboRunning && IsTurboPumpRunning())
            {
                reason = "터보펌프 작동 중에는 칠러를 정지할 수 없습니다.";
                return false;
            }
            return true;
        }

        #endregion

        #region 이온게이지 인터락

        /// <summary>이온게이지 HV ON 가능 여부</summary>
        public bool CanTurnOnIonGaugeHV(out string reason)
        {
            reason = null;
            if (Config.IonGaugeHV_RequireLowPressure)
            {
                double pressure = GetPressureTorr();
                if (pressure <= 0 || pressure > 7.5E-4)
                {
                    reason = $"압력이 너무 높습니다. (현재: {(pressure > 0 ? $"{pressure:E2}" : "N/A")} Torr, 필요: ≤7.5E-4)";
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region AutoRun 인터락

        /// <summary>AutoRun 중 수동 조작 가능 여부</summary>
        public bool CanManualControl(string target, out string reason)
        {
            reason = null;
            if (_mainForm._autoRunService?.IsRunning != true)
                return true;

            bool blocked = target switch
            {
                "밸브" => Config.AutoRun_BlockManualValveControl,
                "펌프" => Config.AutoRun_BlockManualPumpControl,
                "이온게이지" => Config.AutoRun_BlockManualIonGaugeControl,
                "히터" => Config.AutoRun_BlockManualHeaterControl,
                _ => false
            };

            if (blocked)
            {
                reason = $"오토런 실행 중에는 {target}을(를) 수동 조작할 수 없습니다.";
                return false;
            }
            return true;
        }

        #endregion

        #region 실시간 워치독 (주기적 호출)

        /// <summary>워치독 위반 시 발생 (장비 자동 정지용)</summary>
        public event EventHandler<WatchdogAction> WatchdogTriggered;

        private DateTime _lastWatchdogLog = DateTime.MinValue;

        /// <summary>
        /// 실시간 안전 워치독 — 데이터 수집 주기마다 호출
        /// 위험 상태 감지 시 장비 자동 정지
        /// </summary>
        public void RunWatchdog()
        {
            try
            {
                bool turboRunning = IsTurboPumpRunning();

                // 1. 터보 가동 중 칠러 정지 → 터보 자동 정지
                if (turboRunning && !IsChillerRunning() &&
                    _mainForm._bathCirculator?.IsConnected == true)
                {
                    TriggerWatchdog(WatchdogAction.StopTurboPump,
                        "칠러 정지 감지 — 터보펌프 과열 방지를 위해 자동 정지");
                }

                // 2. 터보 가동 중 드라이펌프 정지 → 터보 자동 정지
                if (turboRunning && !IsDryPumpRunning() &&
                    _mainForm._dryPump?.IsConnected == true)
                {
                    TriggerWatchdog(WatchdogAction.StopTurboPump,
                        "드라이펌프 정지 감지 — 터보펌프 배압 보호를 위해 자동 정지");
                }

                // 3. 터보 가동 중 게이트밸브 닫힘 → 터보 자동 정지
                if (turboRunning && !IsGateValveOpen() &&
                    _mainForm._ioModule?.IsConnected == true)
                {
                    TriggerWatchdog(WatchdogAction.StopTurboPump,
                        "게이트밸브 닫힘 감지 — 배기 경로 차단, 터보펌프 자동 정지");
                }

                // 4. 히터 과온도 → 히터 자동 정지
                //    모드별 상한 분리: Bakeout은 BakeoutHeaterMaxTemperature, Outgassing은 목표온도+50
                double ch1Temp = GetCh1Temperature();
                double heaterMaxTemp = _mainForm._autoRunConfig?.GetEffectiveHeaterMaxTemperature() ?? 300;
                if (ch1Temp > heaterMaxTemp + 20 && ch1Temp > 50) // 상한+20°C 초과 시
                {
                    TriggerWatchdog(WatchdogAction.StopHeater,
                        $"CH1 과온도 감지: {ch1Temp:F1}°C (상한+20: {heaterMaxTemp + 20:F0}°C) — 히터 자동 정지");
                }

                // 5. 터보 가동 중 압력 급상승 → 경고 (1 Torr 초과)
                if (turboRunning)
                {
                    double pressure = GetPressureTorr();
                    if (pressure > 1.0)
                    {
                        TriggerWatchdog(WatchdogAction.StopTurboPump,
                            $"압력 급상승 감지: {pressure:E1} Torr — 터보펌프 보호를 위해 자동 정지");
                    }
                }
            }
            catch { }
        }

        private void TriggerWatchdog(WatchdogAction action, string message)
        {
            // 반복 로그 방지: 10초에 1회
            var now = DateTime.Now;
            if ((now - _lastWatchdogLog).TotalSeconds < 10) return;
            _lastWatchdogLog = now;

            string fullMessage = $"[워치독] {message}";
            LogWarning(fullMessage);
            InterlockViolation?.Invoke(this, fullMessage);
            WatchdogTriggered?.Invoke(this, action);
        }

        #endregion

        #region 통합 검증 (로그 포함)

        /// <summary>인터락 검증 — 차단 시 로그+이벤트 발생</summary>
        public bool Check(bool allowed, string violationMessage)
        {
            if (allowed) return true;
            string message = $"[인터락] {violationMessage}";
            InterlockViolation?.Invoke(this, message);
            LogWarning(message);
            return false;
        }

        #endregion

        #region 상태 조회 헬퍼

        private bool IsTurboPumpRunning() =>
            _mainForm._turboPump?.Status?.IsRunning ?? false;

        private bool IsHeaterRunning()
        {
            try
            {
                if (_mainForm._tempController?.IsConnected != true) return false;
                return _mainForm._tempController.Status.ChannelStatus[0].IsRunning;
            }
            catch { return false; }
        }

        private double GetCh1Temperature()
        {
            try
            {
                if (_mainForm._tempController?.IsConnected != true) return 0;
                var ch1 = _mainForm._tempController.Status.ChannelStatus[0];
                return ch1.Dot == 1 ? ch1.PresentValue / 10.0 : ch1.PresentValue;
            }
            catch { return 0; }
        }

        private bool IsDryPumpRunning() =>
            _mainForm._dryPump?.Status?.IsRunning ?? false;

        private bool IsChillerRunning() =>
            _mainForm._bathCirculator?.Status?.IsRunning ?? false;

        private bool IsGateValveOpen()
        {
            var pos = _mainForm._ioModule?.GateValvePosition;
            return pos == "Opened" || pos == "Open";
        }

        private bool IsVentValveOpen() =>
            _mainForm._ioModule?.LastValidDOValues?.IsVentValveOn ?? false;

        private bool IsExhaustValveOpen() =>
            _mainForm._ioModule?.LastValidDOValues?.IsExhaustValveOn ?? false;

        private double GetPressureTorr()
        {
            try
            {
                var aiData = _mainForm._ioModule?.LastValidAIValues;
                if (aiData == null) return 0;
                return _mainForm._piraniGauge?.ConvertVoltageToPressureInTorr(
                    aiData.ExpansionVoltageValues[1]) ?? 0;
            }
            catch { return 0; }
        }

        private double GetAtmPressureKPa()
        {
            try
            {
                var aiData = _mainForm._ioModule?.LastValidAIValues;
                if (aiData == null) return 0;
                return _mainForm._atmSwitch?.ConvertVoltageToPressureInkPa(
                    aiData.ExpansionVoltageValues[0]) ?? 0;
            }
            catch { return 0; }
        }

        private void LogWarning(string message)
        {
            try { AsyncLoggingService.Instance?.LogWarning(message); }
            catch { }
        }

        #endregion
    }
}
