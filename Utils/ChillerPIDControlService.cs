using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Forms;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 선택된 온도 채널 기준으로 칠러를 자동 제어하는 PID 서비스
    /// - 타겟 채널 선택 가능 (CH1~CH5)
    /// - 칠러 자동 시작/정지
    /// - 센서 에러 감지 시 안전 정지
    /// - 연결 끊김 복구
    /// - 설정 자동 저장/로드
    /// </summary>
    public class ChillerPIDControlService : IDisposable
    {
        #region 필드 및 속성

        private readonly MainForm _mainForm;
        private readonly PIDController _pidController;
        private readonly System.Timers.Timer _controlTimer;

        private bool _isEnabled;
        private double _targetTemperature = 25.0;
        private double _updateInterval = 30.0; // 칠러 dead-time 매칭 (이전: 10s)
        private int _targetChannelIndex = 1; // 기본: CH2 (인덱스 1)

        private static readonly string[] ChannelNames = {
            "CH1", "CH2", "CH3", "CH4", "CH5", "CH6",
            "CH7", "CH8", "CH9", "CH10", "CH11", "CH12"
        };

        // 칠러 출력 온도 범위
        private const double CHILLER_MIN_TEMP = -10.0;
        private const double CHILLER_MAX_TEMP = 80.0;

        // 안전 관련
        private int _consecutiveErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 5;
        private DateTime _lastSuccessTime = DateTime.MinValue;
        private bool _chillerAutoStarted = false;

        // 적응 학습
        private readonly ChillerPIDAdaptive _adaptive = new ChillerPIDAdaptive();

        // 사용자 baseline 게인 — 학습 drift 방지용. 학습은 _pidController.Kp/Ki/Kd만 바꾸고
        // 이 값은 사용자가 명시적으로 SetPIDParameters를 호출할 때만 갱신.
        private double _baseKp = 0.5;
        private double _baseKi = 0.005;
        private double _baseKd = 0.7;

        // 칠러 setpoint 변화율 제한 (°C/cycle) — dead-time 큰 시스템 진동 억제
        private const double CHILLER_SETPOINT_MAX_DELTA_PER_CYCLE = 2.0;
        private double _lastSentSetpoint = double.NaN;

        // === 하위 호환 속성 (AutoRun 등 기존 코드에서 사용) ===

        /// <summary>Ch2 목표온도 (하위 호환)</summary>
        public double Ch2TargetTemperature
        {
            get => _targetTemperature;
            set => TargetTemperature = value;
        }

        /// <summary>칠러 기준온도 (하위 호환 - 더 이상 사용하지 않지만 set은 무시)</summary>
        public double ChillerBaseTemperature
        {
            get => _targetTemperature;
            set { /* 무시 - 이제 PID가 직접 제어 */ }
        }

        // === 신규 속성 ===

        /// <summary>PID 제어 기준 채널 인덱스 (0=CH1, 1=CH2, ..., 4=CH5)</summary>
        public int TargetChannelIndex
        {
            get => _targetChannelIndex;
            set
            {
                int clamped = Math.Max(0, Math.Min(11, value));
                if (_targetChannelIndex != clamped)
                {
                    _targetChannelIndex = clamped;
                    _pidController?.Reset();
                    SaveSettings();
                    LogInfo($"타겟 채널 변경: {ChannelNames[_targetChannelIndex]}");
                }
            }
        }

        /// <summary>현재 타겟 채널 이름</summary>
        public string TargetChannelName => ChannelNames[_targetChannelIndex];

        /// <summary>목표 온도 (°C) - 이것만 설정하면 됨</summary>
        public double TargetTemperature
        {
            get => _targetTemperature;
            set
            {
                _targetTemperature = value;
                SaveSettings();
            }
        }

        /// <summary>PID 활성화 여부</summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                        Start();
                    else
                        Stop();
                    SaveSettings();
                }
            }
        }

        /// <summary>업데이트 주기 (초)</summary>
        public double UpdateInterval
        {
            get => _updateInterval;
            set
            {
                _updateInterval = Math.Max(1.0, value);
                if (_controlTimer != null)
                    _controlTimer.Interval = _updateInterval * 1000;
                SaveSettings();
            }
        }

        /// <summary>PID 데드밴드 (°C). 변경 시 즉시 PID 컨트롤러에 반영 + 저장.</summary>
        public double Deadband
        {
            get => _pidController.Deadband;
            set
            {
                _pidController.Deadband = Math.Max(0, value);
                SaveSettings();
            }
        }

        public PIDController PID => _pidController;
        public ChillerPIDAdaptive Adaptive => _adaptive;
        public double LastOutput { get; private set; }
        /// <summary>마지막 측정 온도 (선택된 채널)</summary>
        public double LastChannelTemperature { get; private set; }
        /// <summary>하위 호환</summary>
        public double LastCh2Temperature => LastChannelTemperature;
        public double LastChillerSetpoint { get; private set; }

        /// <summary>적응 학습 활성화 여부</summary>
        public bool AdaptiveEnabled
        {
            get => _adaptive.Enabled;
            set
            {
                _adaptive.Enabled = value;
                if (value)
                {
                    // baseline은 사용자 의도 게인. 적응 ON 시 학습된 게인을 baseline으로 다시 잡지 않는다.
                    _adaptive.ResetBaseline(_baseKp, _baseKi, _baseKd);
                    // 현재 PID 게인이 baseline에서 너무 벗어났다면 baseline으로 되돌린다 — 사용자가 의도적으로
                    // "다시 학습 시작"하는 신호로 해석.
                    _pidController.SetParameters(_baseKp, _baseKi, _baseKd);
                    _pidController.ResetIntegral();
                }
                SaveSettings();
                LogInfo($"적응 학습 {(value ? "활성화 (baseline으로 복귀)" : "비활성화")}");
            }
        }

        #endregion

        #region 생성자

        public ChillerPIDControlService(MainForm mainForm)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));

            // PID 기본 파라미터 — 출력은 목표온도 기준 보정값 (±범위)
            double correctionRange = 15.0; // 최대 ±15°C 보정
            _pidController = new PIDController(
                kp: _baseKp,
                ki: _baseKi,
                kd: _baseKd,
                outputMin: -correctionRange,
                outputMax: correctionRange
            );
            _pidController.Deadband = 0.5; // ★ 0.3 → 0.5: 칠러 dead-time 큰 시스템의 미세 진동 차단

            _controlTimer = new System.Timers.Timer(_updateInterval * 1000);
            _controlTimer.Elapsed += ControlTimer_Elapsed;
            _controlTimer.AutoReset = true;

            // 적응 학습 이벤트
            _adaptive.GainAdjusted += (s, e) =>
            {
                LogInfo($"적응 학습 — Kp:{e.Kp:F3} Ki:{e.Ki:F4} Kd:{e.Kd:F3} ({e.Reason})");
            };

            // 저장된 설정 로드
            LoadSettings();
        }

        #endregion

        #region 제어

        private void Start()
        {
            if (!_isEnabled) return;

            // 장치 연결 확인
            if (_mainForm._tempController?.IsConnected != true)
            {
                LogError("온도 컨트롤러 미연결 — PID 시작 불가");
                return;
            }

            if (_mainForm._bathCirculator?.IsConnected != true)
            {
                LogError("칠러 미연결 — PID 시작 불가");
                return;
            }

            // 칠러가 꺼져있으면 자동 시작
            EnsureChillerRunning();

            _pidController.Reset();
            // ★ baseline은 항상 사용자 의도 값으로. 학습된 게인을 baseline으로 잡으면 drift 발생.
            _adaptive.ResetBaseline(_baseKp, _baseKi, _baseKd);
            _consecutiveErrors = 0;
            _lastSentSetpoint = double.NaN;
            _controlTimer.Start();

            LogInfo($"칠러 PID 시작 ({ChannelNames[_targetChannelIndex]} 목표: {_targetTemperature:F1}°C, " +
                    $"Kp:{_pidController.Kp:F3}/Ki:{_pidController.Ki:F4}/Kd:{_pidController.Kd:F3}, 적응:{_adaptive.Enabled})");
        }

        private void Stop()
        {
            _controlTimer.Stop();

            // PID가 자동으로 칠러를 켰으면 원래 온도로 복원
            if (_chillerAutoStarted && _mainForm._bathCirculator?.IsConnected == true)
            {
                try
                {
                    _mainForm._bathCirculator.SetTemperature(_targetTemperature);
                    LogInfo($"칠러 온도 복원: {_targetTemperature:F1}°C");
                }
                catch { }
                _chillerAutoStarted = false;
            }

            LogInfo("칠러 PID 정지");
        }

        /// <summary>
        /// 사용자 수동 PID 파라미터 변경. baseline 갱신 + 적응 학습 baseline도 새 값으로 reset.
        /// (자동 학습 결과는 이 경로를 사용하지 않는다)
        /// </summary>
        public void SetPIDParameters(double kp, double ki, double kd)
        {
            _baseKp = Math.Max(0, kp);
            _baseKi = Math.Max(0, ki);
            _baseKd = Math.Max(0, kd);
            _pidController.SetParameters(_baseKp, _baseKi, _baseKd);
            _adaptive.ResetBaseline(_baseKp, _baseKi, _baseKd);
            SaveSettings();
            LogInfo($"PID 파라미터 변경 — Kp:{kp:F3} Ki:{ki:F3} Kd:{kd:F3} (baseline 동시 갱신)");
        }

        /// <summary>
        /// AutoRun 등 외부 이벤트에서 호출. 적분 windup 제거 + 적응 학습 윈도우 초기화.
        /// PID 게인은 그대로 두고 상태만 리셋.
        /// </summary>
        public void OnExperimentHoldStarted()
        {
            if (!_isEnabled) return;
            _pidController.ResetIntegral();
            _adaptive.ResetBaseline(_pidController.Kp, _pidController.Ki, _pidController.Kd);
            _lastSentSetpoint = double.NaN;
            LogInfo("Hold 진입 — PID 적분 클리어, adaptive 윈도우 재시작");
        }

        #endregion

        #region PID 실행

        private async void ControlTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _controlTimer.Stop();
                await ExecutePIDControl();
            }
            catch (Exception ex)
            {
                LogError($"PID 오류: {ex.Message}");
                HandleError();
            }
            finally
            {
                if (_isEnabled)
                    _controlTimer.Start();
            }
        }

        private async Task ExecutePIDControl()
        {
            // 연결 확인
            if (_mainForm._tempController?.IsConnected != true ||
                _mainForm._bathCirculator?.IsConnected != true)
            {
                HandleError();
                LogWarning("장치 연결 끊김 — 대기 중");
                return;
            }

            // 타겟 채널 현재 온도 읽기
            string chName = ChannelNames[_targetChannelIndex];
            var chStatus = _mainForm._tempController.Status?.ChannelStatus?[_targetChannelIndex];
            if (chStatus == null)
            {
                HandleError();
                return;
            }

            // 센서 에러 체크
            if (!string.IsNullOrEmpty(chStatus.SensorError))
            {
                HandleError();
                LogWarning($"{chName} 센서 에러: {chStatus.SensorError} — 안전 정지");
                if (_consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                    SafeStop("센서 에러 지속");
                return;
            }

            double chTemp = chStatus.CalibratedTemperature;
            LastChannelTemperature = chTemp;

            // 온도 이상치 체크 (센서 고장 감지)
            if (chTemp < -50 || chTemp > 300)
            {
                HandleError();
                LogWarning($"{chName} 온도 이상: {chTemp:F1}°C — 무시");
                return;
            }

            // PID 계산 — 출력은 목표온도 기준 보정값
            double pidCorrection = _pidController.Calculate(_targetTemperature, chTemp);
            LastOutput = pidCorrection;

            // 칠러 설정온도 = 목표온도 + PID 보정값 (feedforward 방식)
            double chillerSetpoint = _targetTemperature + pidCorrection;
            chillerSetpoint = Math.Max(CHILLER_MIN_TEMP, Math.Min(CHILLER_MAX_TEMP, chillerSetpoint));

            // ★ 변화율 제한 — 칠러는 dead-time이 분 단위라 매 사이클 큰 점프는 진동 유발.
            // 첫 사이클(_lastSentSetpoint=NaN)은 제한 없이 보낸다.
            if (!double.IsNaN(_lastSentSetpoint))
            {
                double maxDelta = CHILLER_SETPOINT_MAX_DELTA_PER_CYCLE;
                double clamped = _lastSentSetpoint
                                 + Math.Max(-maxDelta, Math.Min(maxDelta, chillerSetpoint - _lastSentSetpoint));
                if (Math.Abs(clamped - chillerSetpoint) > 0.01)
                    LogDebug($"setpoint 변화율 제한: {chillerSetpoint:F1} → {clamped:F1}°C (이전:{_lastSentSetpoint:F1})");
                chillerSetpoint = clamped;
            }
            LastChillerSetpoint = chillerSetpoint;

            // 칠러에 온도 설정
            bool result = await Task.Run(() => _mainForm._bathCirculator.SetTemperature(chillerSetpoint));

            if (result)
            {
                _consecutiveErrors = 0;
                _lastSuccessTime = DateTime.Now;
                _lastSentSetpoint = chillerSetpoint;
                LogDebug($"PID — {chName}: {chTemp:F1}°C (목표: {_targetTemperature:F1}), 보정:{pidCorrection:F1}, 칠러→{chillerSetpoint:F1}°C");
            }
            else
            {
                HandleError();
                LogWarning("칠러 온도 설정 실패");
            }

            // 적응 학습 — 매 사이클 샘플 수집, 주기적으로 게인 조정
            double error = _targetTemperature - chTemp;
            var adjusted = _adaptive.Update(error, chTemp,
                _pidController.Kp, _pidController.Ki, _pidController.Kd);
            if (adjusted.HasValue)
            {
                _pidController.SetParameters(adjusted.Value.kp, adjusted.Value.ki, adjusted.Value.kd);
                SaveSettings();
            }

            LogPIDData(chTemp, pidCorrection, chillerSetpoint);
        }

        #endregion

        #region 안전 및 자동화

        private void EnsureChillerRunning()
        {
            try
            {
                var status = _mainForm._bathCirculator?.Status;
                if (status != null && !status.IsRunning)
                {
                    LogInfo("칠러 자동 시작");
                    _mainForm._bathCirculator.SetTemperature(_targetTemperature);
                    Task.Run(() => _mainForm._bathCirculator.StartPriority());
                    _chillerAutoStarted = true;
                }
            }
            catch (Exception ex)
            {
                LogWarning($"칠러 자동 시작 실패: {ex.Message}");
            }
        }

        private void HandleError()
        {
            _consecutiveErrors++;
        }

        private void SafeStop(string reason)
        {
            LogError($"안전 정지: {reason}");
            _isEnabled = false;
            _controlTimer.Stop();

            // UI 동기화
            try
            {
                _mainForm.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var chk = _mainForm.Controls.Find("chkChillerPIDEnabled", true);
                        if (chk.Length > 0 && chk[0] is System.Windows.Forms.CheckBox cb)
                            cb.Checked = false;
                    }
                    catch { }
                }));
            }
            catch { }
        }

        #endregion

        #region 설정 저장/로드

        private static string SettingsPath =>
            Path.Combine(PathSettings.Instance.ConfigPath, "ChillerPIDSettings.xml");

        private bool _suppressSave = false;

        private void SaveSettings()
        {
            if (_suppressSave) return;
            try
            {
                var settings = new ChillerPIDSettings
                {
                    TargetTemperature = _targetTemperature,
                    TargetChannelIndex = _targetChannelIndex,
                    UpdateInterval = _updateInterval,
                    Kp = _pidController.Kp,
                    Ki = _pidController.Ki,
                    Kd = _pidController.Kd,
                    BaseKp = _baseKp,
                    BaseKi = _baseKi,
                    BaseKd = _baseKd,
                    Deadband = _pidController.Deadband,
                    AdaptiveEnabled = _adaptive.Enabled
                };

                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(ChillerPIDSettings));
                using (var writer = new StreamWriter(SettingsPath))
                    serializer.Serialize(writer, settings);
            }
            catch (Exception ex)
            {
                LogDebug($"PID 설정 저장 실패: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;

                var serializer = new XmlSerializer(typeof(ChillerPIDSettings));
                ChillerPIDSettings settings;
                using (var reader = new StreamReader(SettingsPath))
                    settings = (ChillerPIDSettings)serializer.Deserialize(reader);

                _suppressSave = true;
                _targetTemperature = settings.TargetTemperature;
                _targetChannelIndex = Math.Max(0, Math.Min(11, settings.TargetChannelIndex));
                _updateInterval = Math.Max(1.0, settings.UpdateInterval);
                _controlTimer.Interval = _updateInterval * 1000;
                _pidController.SetParameters(settings.Kp, settings.Ki, settings.Kd);

                // baseline 복원 — 저장된 값이 없으면(0) 현재 게인을 baseline으로 채택.
                // 이렇게 두면 학습된 Kp/Ki/Kd가 다음 실행의 baseline을 덮어쓰지 않는다.
                _baseKp = settings.BaseKp > 0 ? settings.BaseKp : settings.Kp;
                _baseKi = settings.BaseKi > 0 ? settings.BaseKi : settings.Ki;
                _baseKd = settings.BaseKd > 0 ? settings.BaseKd : settings.Kd;

                if (settings.Deadband > 0)
                    _pidController.Deadband = settings.Deadband;

                _adaptive.Enabled = settings.AdaptiveEnabled;
                _suppressSave = false;

                LogInfo($"PID 설정 로드 — 채널:{ChannelNames[_targetChannelIndex]}, 목표:{_targetTemperature:F1}°C, " +
                        $"Kp:{settings.Kp:F3}/Ki:{settings.Ki:F4}/Kd:{settings.Kd:F3} " +
                        $"(base Kp:{_baseKp:F3}/Ki:{_baseKi:F4}/Kd:{_baseKd:F3})");
            }
            catch
            {
                _suppressSave = false;
            }
        }

        #endregion

        #region 로깅

        private void LogPIDData(double chTemp, double pidOutput, double chillerSetpoint)
        {
            var dataList = new List<string>
            {
                ChannelNames[_targetChannelIndex],
                chTemp.ToString("F2"),
                _targetTemperature.ToString("F2"),
                pidOutput.ToString("F2"),
                chillerSetpoint.ToString("F2"),
                _pidController.Kp.ToString("F3"),
                _pidController.Ki.ToString("F3"),
                _pidController.Kd.ToString("F3"),
                _pidController.IntegralTerm.ToString("F3"),
                _pidController.LastError.ToString("F3")
            };
            DataLoggerService.Instance.LogDataAsync("ChillerPID", dataList);
        }

        private void LogInfo(string msg) => LoggerService.Instance.LogInfo($"[ChillerPID] {msg}");
        private void LogWarning(string msg) => LoggerService.Instance.LogWarning($"[ChillerPID] {msg}");
        private void LogError(string msg) => LoggerService.Instance.LogError($"[ChillerPID] {msg}");
        private void LogDebug(string msg) => LoggerService.Instance.LogDebug($"[ChillerPID] {msg}");

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Stop();
            _controlTimer?.Dispose();
        }

        #endregion
    }

    /// <summary>PID 설정 저장용 모델</summary>
    public class ChillerPIDSettings
    {
        public double TargetTemperature { get; set; } = 25.0;
        /// <summary>타겟 채널 인덱스 (0=CH1, 1=CH2, ..., 4=CH5)</summary>
        public int TargetChannelIndex { get; set; } = 1;
        public double UpdateInterval { get; set; } = 30.0;

        /// <summary>현재(학습 반영된) 게인. 다음 시작 시 PID에 적용.</summary>
        public double Kp { get; set; } = 0.5;
        public double Ki { get; set; } = 0.005;
        public double Kd { get; set; } = 0.7;

        /// <summary>
        /// 사용자가 의도한 baseline 게인. 학습 drift의 기준이 되는 값이라
        /// 학습 결과가 baseline을 덮어쓰지 않도록 별도 보관.
        /// 비어 있으면(0) 첫 로드 시 현재 게인을 baseline으로 채택.
        /// </summary>
        public double BaseKp { get; set; } = 0.0;
        public double BaseKi { get; set; } = 0.0;
        public double BaseKd { get; set; } = 0.0;

        /// <summary>PID 데드밴드 (°C). 0이면 코드 기본값 사용.</summary>
        public double Deadband { get; set; } = 0.0;

        public bool AdaptiveEnabled { get; set; } = true;
    }
}
