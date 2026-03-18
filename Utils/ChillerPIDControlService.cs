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
        private double _updateInterval = 10.0;
        private int _targetChannelIndex = 1; // 기본: CH2 (인덱스 1)

        private static readonly string[] ChannelNames = { "CH1", "CH2", "CH3", "CH4", "CH5" };

        // 칠러 출력 온도 범위
        private const double CHILLER_MIN_TEMP = -10.0;
        private const double CHILLER_MAX_TEMP = 80.0;

        // 안전 관련
        private int _consecutiveErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 5;
        private DateTime _lastSuccessTime = DateTime.MinValue;
        private bool _chillerAutoStarted = false;

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
                int clamped = Math.Max(0, Math.Min(4, value));
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

        public PIDController PID => _pidController;
        public double LastOutput { get; private set; }
        /// <summary>마지막 측정 온도 (선택된 채널)</summary>
        public double LastChannelTemperature { get; private set; }
        /// <summary>하위 호환</summary>
        public double LastCh2Temperature => LastChannelTemperature;
        public double LastChillerSetpoint { get; private set; }

        #endregion

        #region 생성자

        public ChillerPIDControlService(MainForm mainForm)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));

            // PID 기본 파라미터 (온도 제어에 적합한 보수적 값)
            _pidController = new PIDController(
                kp: 2.0,
                ki: 0.005,
                kd: 1.0,
                outputMin: CHILLER_MIN_TEMP,
                outputMax: CHILLER_MAX_TEMP
            );
            _pidController.Deadband = 0.2;

            _controlTimer = new System.Timers.Timer(_updateInterval * 1000);
            _controlTimer.Elapsed += ControlTimer_Elapsed;
            _controlTimer.AutoReset = true;

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
            _consecutiveErrors = 0;
            _controlTimer.Start();

            LogInfo($"칠러 PID 시작 ({ChannelNames[_targetChannelIndex]} 목표: {_targetTemperature:F1}°C)");
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

        public void SetPIDParameters(double kp, double ki, double kd)
        {
            _pidController.SetParameters(kp, ki, kd);
            SaveSettings();
            LogInfo($"PID 파라미터 변경 — Kp:{kp:F3} Ki:{ki:F3} Kd:{kd:F3}");
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

            // PID 계산 — 출력이 곧 칠러 설정온도
            double pidOutput = _pidController.Calculate(_targetTemperature, chTemp);
            LastOutput = pidOutput;

            // 칠러 설정온도 = PID 출력 (직접 제어)
            double chillerSetpoint = Math.Max(CHILLER_MIN_TEMP, Math.Min(CHILLER_MAX_TEMP, pidOutput));
            LastChillerSetpoint = chillerSetpoint;

            // 칠러에 온도 설정
            bool result = await Task.Run(() => _mainForm._bathCirculator.SetTemperature(chillerSetpoint));

            if (result)
            {
                _consecutiveErrors = 0;
                _lastSuccessTime = DateTime.Now;
                LogDebug($"PID — {chName}: {chTemp:F1}°C (목표: {_targetTemperature:F1}), 칠러→{chillerSetpoint:F1}°C");
            }
            else
            {
                HandleError();
                LogWarning("칠러 온도 설정 실패");
            }

            LogPIDData(chTemp, pidOutput, chillerSetpoint);
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
                    Kd = _pidController.Kd
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
                _targetChannelIndex = Math.Max(0, Math.Min(4, settings.TargetChannelIndex));
                _updateInterval = Math.Max(1.0, settings.UpdateInterval);
                _controlTimer.Interval = _updateInterval * 1000;
                _pidController.SetParameters(settings.Kp, settings.Ki, settings.Kd);
                _suppressSave = false;

                LogInfo($"PID 설정 로드 — 채널:{ChannelNames[_targetChannelIndex]}, 목표:{_targetTemperature:F1}°C, Kp:{settings.Kp:F3}, Ki:{settings.Ki:F3}, Kd:{settings.Kd:F3}");
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
        public double UpdateInterval { get; set; } = 10.0;
        public double Kp { get; set; } = 2.0;
        public double Ki { get; set; } = 0.005;
        public double Kd { get; set; } = 1.0;
    }
}
