using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// 자기 학습형 적응 램프 컨트롤러
    ///
    /// 제어 원리:
    ///   제어 변수 = heater offset (히터 SV - 샘플 온도)
    ///   제어 법칙 = offset += gain × (desired_rate - actual_rate) × dt
    ///
    /// K_eff를 명시적으로 추정하지 않고, offset/rate 비율을 직접 학습하여
    /// 어떤 샘플이든 사전 정보 없이 자동 적응합니다.
    ///
    /// 사용자 입력: 목표온도, 램프속도, 모니터채널, 유지시간, 히터상한, 종료동작 (6개)
    /// </summary>
    public class AdaptiveRampController : IDisposable
    {
        #region 상태 정의

        public enum Phase
        {
            Idle,
            Probe,
            Ramp,
            Converge,
            Hold,
            PressureHold,
            Stopped,
            Error
        }

        #endregion

        #region 이벤트

        public class ProgressEventArgs : EventArgs
        {
            public Phase State { get; set; }
            public double HeaterTemp { get; set; }
            public double SampleTemp { get; set; }
            public double TargetTemp { get; set; }
            public double HeaterSV { get; set; }
            public double Offset { get; set; }
            public double ActualRate { get; set; }
            public double ProgressPercent { get; set; }
            public string StatusMessage { get; set; }
            public TimeSpan ElapsedTime { get; set; }
        }

        public class CompletedEventArgs : EventArgs
        {
            public bool Success { get; set; }
            public string Message { get; set; }
            public TimeSpan TotalTime { get; set; }
            public double FinalSampleTemp { get; set; }
            public double EquilibriumOffset { get; set; }
        }

        public class HwLimitEventArgs : EventArgs
        {
            public double CurrentSampleTemp { get; set; }
            public double HeaterMax { get; set; }
            public double EstimatedMaxAchievable { get; set; }
            public double TargetTemp { get; set; }
        }

        public event EventHandler<ProgressEventArgs> ProgressUpdated;
        public event EventHandler<CompletedEventArgs> RampCompleted;
        public event EventHandler TargetReached;
        public event EventHandler<string> ErrorOccurred;
        public event EventHandler<string> LogMessage;
        public event EventHandler<HwLimitEventArgs> HardwareLimitReached;

        #endregion

        #region 필드

        private readonly TempController _tempController;
        private readonly System.Timers.Timer _controlTimer;
        private readonly object _lockObject = new object();

        private Phase _state = Phase.Idle;
        private DateTime _startTime;
        private DateTime _rampStartTime;
        private DateTime _convergeStartTime;
        private DateTime _holdStartTime;

        private double _targetTemp;
        private double _rampRatePerHour;
        private double _heaterMax;
        private double _initialSampleTemp;

        private double _offset;
        private double _equilibriumOffset;
        private double _holdCorrection;
        private double _heaterSV;

        private double _probeStep;
        private int _probeEscalations;
        private DateTime _probeLastCheck;
        private readonly List<double> _probeRates = new List<double>();

        private readonly Queue<(DateTime time, double temp)> _rateBuf = new Queue<(DateTime, double)>();
        private const int RATE_WINDOW_SEC = 30;

        private DateTime _holdCheckTime;
        private readonly List<double> _holdTempBuf = new List<double>();
        private const double HOLD_CHECK_INTERVAL_SEC = 60.0;

        private bool _hwSaturated;
        private DateTime _hwSaturationStart;
        private double? _eqTempPrediction;
        private readonly List<(DateTime time, double temp)> _saturationRates = new List<(DateTime, double)>();

        private double? _pendingSetpoint;
        private readonly object _setpointLock = new object();
        private DateTime _lastSetpointTime = DateTime.MinValue;
        private const int MIN_SETPOINT_INTERVAL_MS = 300;

        #endregion

        #region 속성

        public Phase State => _state;
        public bool IsRunning => _state == Phase.Probe || _state == Phase.Ramp ||
                                  _state == Phase.Converge || _state == Phase.Hold ||
                                  _state == Phase.PressureHold;

        public double TargetTemperature
        {
            get => _targetTemp;
            set => UpdateTargetTemperature(value);
        }

        public double RampRatePerHour
        {
            get => _rampRatePerHour;
            set { _rampRatePerHour = Math.Max(1, Math.Min(100, value)); }
        }

        public double HeaterMax
        {
            get => _heaterMax;
            set { _heaterMax = Math.Max(30, value); ClampHeaterSV(); }
        }

        public double CurrentOffset => _offset;
        public double EquilibriumOffset => _equilibriumOffset;
        public double ActualRate { get; private set; }
        public int ProbeEscalations => _probeEscalations;
        public bool IsHwSaturated => _hwSaturated;
        public int SampleChannel { get; set; } = 2;
        public int HeaterChannel { get; set; } = 1;
        public List<int> AdditionalMonitorChannels { get; set; } = new List<int>();
        public BakeoutEndAction EndAction { get; set; } = BakeoutEndAction.HeaterOff;
        public bool HoldAfterComplete { get; set; } = true;

        #endregion

        #region 생성자

        public AdaptiveRampController(TempController tempController)
        {
            _tempController = tempController ?? throw new ArgumentNullException(nameof(tempController));
            _controlTimer = new System.Timers.Timer(1000);
            _controlTimer.Elapsed += ControlTimer_Elapsed;
            _controlTimer.AutoReset = true;
        }

        #endregion

        #region 공개 메서드

        public bool Start(double targetTemp, double rampRatePerHour, double heaterMax)
        {
            if (IsRunning) { OnError("이미 실행 중입니다."); return false; }

            _targetTemp = targetTemp;
            _rampRatePerHour = rampRatePerHour;
            _heaterMax = heaterMax;

            double hwMax = GetHardwareMaxTemp();
            if (hwMax > 0 && _heaterMax > hwMax)
            {
                OnLog($"히터 상한 {_heaterMax:F0}°C → HW 상한 {hwMax:F0}°C로 제한");
                _heaterMax = hwMax;
            }

            _initialSampleTemp = ReadSampleTemp();
            if (double.IsNaN(_initialSampleTemp)) { OnError("샘플 온도를 읽을 수 없습니다."); return false; }

            ResetState();
            _offset = Math.Max(3.0, 0.05 * (_targetTemp - _initialSampleTemp));
            _probeStep = _offset;
            _heaterSV = _initialSampleTemp + _offset;
            _state = Phase.Probe;
            _startTime = DateTime.Now;
            _probeLastCheck = DateTime.Now;

            OnLog($"시작: {_initialSampleTemp:F1}°C → {_targetTemp:F1}°C @ {_rampRatePerHour:F0}°C/h");
            SetHeaterTemperature(_heaterSV);
            _controlTimer.Start();
            return true;
        }

        public void Stop() { _controlTimer.Stop(); _state = Phase.Stopped; OnLog("정지됨"); }

        public void EmergencyStop()
        {
            _controlTimer.Stop(); _state = Phase.Error;
            try { SetHeaterTemperature(_initialSampleTemp); } catch { }
            OnLog("비상 정지");
        }

        public void HoldCurrentTemperature()
        {
            if (_state == Phase.Ramp || _state == Phase.Converge)
            {
                double T_s = ReadSampleTemp(), T_h = ReadHeaterTemp();
                _equilibriumOffset = T_h - T_s;
                _holdCorrection = 0;
                _state = Phase.Hold;
                _holdStartTime = DateTime.Now;
                _holdCheckTime = DateTime.Now;
                _holdTempBuf.Clear();
                OnLog($"수동 Hold: eq_offset={_equilibriumOffset:F1}°C");
            }
        }

        public bool ApplyPendingSetpoint()
        {
            double? sp;
            lock (_setpointLock)
            {
                sp = _pendingSetpoint;
                if (sp == null) return false;
                if ((DateTime.Now - _lastSetpointTime).TotalMilliseconds < MIN_SETPOINT_INTERVAL_MS) return false;
                _pendingSetpoint = null;
                _lastSetpointTime = DateTime.Now;
            }
            try
            {
                var status = _tempController.Status.ChannelStatus[HeaterChannel - 1];
                short sv = (short)(sp.Value * (status.Dot == 0 ? 1 : 10));
                _tempController.SetTemperature(HeaterChannel, sv);
                return true;
            }
            catch { return false; }
        }

        public bool HasPendingSetpoint { get { lock (_setpointLock) { return _pendingSetpoint.HasValue; } } }

        public void PauseForPressure(double currentP, double maxP)
        {
            if (_state == Phase.Ramp) { _state = Phase.PressureHold; OnLog($"PressureHold: offset 동결"); }
        }

        public void ResumeFromPressureHold()
        {
            if (_state == Phase.PressureHold) { _state = Phase.Ramp; OnLog("압력 회복, Ramp 재개"); }
        }

        #endregion

        #region 제어 루프

        private void ControlTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_lockObject)
            {
                try { ExecuteControlCycle(); }
                catch (Exception ex) { OnError($"제어 오류: {ex.Message}"); }
            }
        }

        private void ExecuteControlCycle()
        {
            double T_s = ReadSampleTemp(), T_h = ReadHeaterTemp();
            if (double.IsNaN(T_s) || double.IsNaN(T_h)) return;

            UpdateRateEstimation(T_s);
            double rate = ActualRate;

            switch (_state)
            {
                case Phase.Probe: DoProbe(T_s, T_h, rate); break;
                case Phase.Ramp: DoRamp(T_s, T_h, rate); break;
                case Phase.Converge: DoConverge(T_s, T_h, rate); break;
                case Phase.Hold: DoHold(T_s, T_h, rate); break;
            }

            if (_state == Phase.Ramp || _state == Phase.Converge)
                TrackHwSaturation(T_s, T_h, rate);

            ReportProgress(T_s, T_h);
        }

        #endregion

        #region Phase: Probe

        private void DoProbe(double T_s, double T_h, double rate)
        {
            _heaterSV = Math.Min(T_s + _offset, _heaterMax);
            SetHeaterTemperature(_heaterSV);

            if ((DateTime.Now - _probeLastCheck).TotalSeconds < 20) return;
            _probeLastCheck = DateTime.Now;

            if (Math.Abs(rate) < 0.015)
            {
                _probeEscalations++;
                _offset += _probeStep;
                _offset = Math.Min(_offset, OffsetMax(T_s));
                OnLog($"Probe #{_probeEscalations}: offset→{_offset:F1}°C");
                if (_probeEscalations > 10) TransitionToRamp(T_s, rate);
            }
            else
            {
                _probeRates.Add(rate);
                if (_probeRates.Count >= 2 || (DateTime.Now - _startTime).TotalSeconds > 60)
                    TransitionToRamp(T_s, rate);
            }
        }

        private void TransitionToRamp(double T_s, double rate)
        {
            if (Math.Abs(rate) > 0.005)
            {
                double ratio = _offset / rate;
                _offset = (_rampRatePerHour / 60.0) * ratio;
                _offset = Math.Max(0.5, Math.Min(_offset, OffsetMax(T_s)));
            }
            _state = Phase.Ramp;
            _rampStartTime = DateTime.Now;
            OnLog($"→ Ramp: offset={_offset:F1}°C");
        }

        #endregion

        #region Phase: Ramp

        private void DoRamp(double T_s, double T_h, double rate)
        {
            double rateTarget = _rampRatePerHour / 60.0;
            double elapsedMin = (DateTime.Now - _startTime).TotalMinutes;
            double rampedTarget = Math.Min(_initialSampleTemp + rateTarget * elapsedMin, _targetTemp);

            double convBand = Math.Max(1.5, (_targetTemp - _initialSampleTemp) * 0.05);
            if (T_s >= _targetTemp - convBand)
            {
                _state = Phase.Converge;
                _convergeStartTime = DateTime.Now;
                _equilibriumOffset = _offset * 0.3;
                OnLog($"→ Converge: T_s={T_s:F1}°C");
                return;
            }

            double rateError = rateTarget - rate;
            double gain = 0.15 / Math.Max(1.0, _offset * 0.5);
            _offset += gain * rateError;
            _offset = Math.Max(0.2, Math.Min(_offset, OffsetMax(T_s)));

            double trackError = rampedTarget - T_s;
            double correction = Math.Max(-2.0, Math.Min(trackError * 0.1, 3.0));

            _heaterSV = Math.Min(T_s + _offset + correction, _heaterMax);
            SetHeaterTemperature(_heaterSV);
        }

        #endregion

        #region Phase: Converge

        private void DoConverge(double T_s, double T_h, double rate)
        {
            double remaining = _targetTemp - T_s;
            double convBand = Math.Max(1.5, (_targetTemp - _initialSampleTemp) * 0.05);
            double frac = Math.Max(0, Math.Min(1, remaining / convBand));
            double rateTarget = (_rampRatePerHour / 60.0) * frac;

            double rateError = rateTarget - rate;
            double gain = 0.1 / Math.Max(1.0, _offset * 0.5);
            _offset += gain * rateError;
            _offset = Math.Max(0.2, Math.Min(_offset, OffsetMax(T_s)));

            double pull = remaining * 0.15;
            _heaterSV = Math.Min(T_s + _offset + pull, _heaterMax);
            SetHeaterTemperature(_heaterSV);

            if (frac < 0.2)
                _equilibriumOffset = 0.95 * _equilibriumOffset + 0.05 * (T_h - _targetTemp);

            if (Math.Abs(remaining) < 0.5 && Math.Abs(rate) < 0.03 &&
                (DateTime.Now - _convergeStartTime).TotalSeconds > 60)
            {
                _equilibriumOffset = T_h - _targetTemp;
                TransitionToHold(T_s);
            }
        }

        private void TransitionToHold(double T_s)
        {
            _state = Phase.Hold;
            _holdStartTime = DateTime.Now;
            _holdCheckTime = DateTime.Now;
            _holdTempBuf.Clear();
            _holdCorrection = 0;
            OnLog($"→ Hold: eq_offset={_equilibriumOffset:F1}°C");
            TargetReached?.Invoke(this, EventArgs.Empty);
            RampCompleted?.Invoke(this, new CompletedEventArgs
            {
                Success = true, Message = "목표 온도 도달",
                TotalTime = DateTime.Now - _startTime,
                FinalSampleTemp = T_s, EquilibriumOffset = _equilibriumOffset
            });
        }

        #endregion

        #region Phase: Hold

        private void DoHold(double T_s, double T_h, double rate)
        {
            _holdTempBuf.Add(T_s);
            _heaterSV = Math.Min(_targetTemp + _equilibriumOffset + _holdCorrection, _heaterMax);
            SetHeaterTemperature(_heaterSV);

            if ((DateTime.Now - _holdCheckTime).TotalSeconds < HOLD_CHECK_INTERVAL_SEC) return;
            if (_holdTempBuf.Count < 5) return;

            double avg = _holdTempBuf.Average();
            double error = _targetTemp - avg;

            if (Math.Abs(error) > 0.3)
            {
                double adj = Math.Max(-1.0, Math.Min(error * 0.3, 1.0));
                _holdCorrection += adj;
                _equilibriumOffset += _holdCorrection * 0.1;
                _holdCorrection *= 0.9;
                OnLog($"Hold 보정: err={error:+0.1;-0.1}°C eq={_equilibriumOffset:F1}°C");
            }

            _holdCheckTime = DateTime.Now;
            _holdTempBuf.Clear();
        }

        #endregion

        #region HW 포화 감지

        private void TrackHwSaturation(double T_s, double T_h, double rate)
        {
            if (_heaterSV >= _heaterMax - 1)
            {
                if (!_hwSaturated) { _hwSaturated = true; _hwSaturationStart = DateTime.Now; _saturationRates.Clear(); }
                _saturationRates.Add((DateTime.Now, T_s));

                if (_eqTempPrediction == null && (DateTime.Now - _hwSaturationStart).TotalSeconds > 180 && _saturationRates.Count >= 3)
                    PredictEquilibriumTemp(T_s, rate);
            }
            else { _hwSaturated = false; _eqTempPrediction = null; }
        }

        private void PredictEquilibriumTemp(double T_s, double rate)
        {
            if (rate <= 0.001) return;
            var recent = _saturationRates.TakeLast(3).ToList();
            if (recent.Count < 3) return;
            double dt1 = (recent[1].time - recent[0].time).TotalMinutes;
            double dt2 = (recent[2].time - recent[1].time).TotalMinutes;
            if (dt1 < 0.5 || dt2 < 0.5) return;
            double rate1 = (recent[1].temp - recent[0].temp) / dt1;
            double rate2 = (recent[2].temp - recent[1].temp) / dt2;
            if (rate2 < rate1 * 0.9 && rate2 > 0.001)
            {
                double tau = -dt2 / Math.Log(rate2 / Math.Max(0.001, rate1));
                tau = Math.Max(1, Math.Min(tau, 120));
                _eqTempPrediction = T_s + rate * tau;
                OnLog($"평형 예측: ~{_eqTempPrediction:F0}°C");
                if (_eqTempPrediction < _targetTemp - 1)
                    HardwareLimitReached?.Invoke(this, new HwLimitEventArgs
                    { CurrentSampleTemp = T_s, HeaterMax = _heaterMax, EstimatedMaxAchievable = _eqTempPrediction.Value, TargetTemp = _targetTemp });
            }
        }

        #endregion

        #region 런타임 변경

        private void UpdateTargetTemperature(double newTarget)
        {
            double old = _targetTemp; _targetTemp = newTarget;
            if (_state == Phase.Hold)
            {
                double T_s = ReadSampleTemp();
                if (newTarget > T_s + 1) { _state = Phase.Ramp; _rampStartTime = DateTime.Now; OnLog($"Target {old:F0}→{newTarget:F0}°C: Ramp"); }
                else if (newTarget < T_s - 1) { _state = Phase.Converge; _convergeStartTime = DateTime.Now; OnLog($"Target {old:F0}→{newTarget:F0}°C: Converge"); }
            }
        }

        private void ClampHeaterSV() { if (_heaterSV > _heaterMax) { _heaterSV = _heaterMax; SetHeaterTemperature(_heaterSV); } }

        #endregion

        #region 변화율 추정

        private void UpdateRateEstimation(double T_s)
        {
            _rateBuf.Enqueue((DateTime.Now, T_s));
            while (_rateBuf.Count > 1 && (_rateBuf.Last().time - _rateBuf.First().time).TotalSeconds > RATE_WINDOW_SEC)
                _rateBuf.Dequeue();
            ActualRate = CalculateRate();
        }

        private double CalculateRate()
        {
            if (_rateBuf.Count < 5) return 0;
            var data = _rateBuf.ToArray();
            double t0 = data[0].time.Ticks; int n = data.Length;
            double sT = 0, sV = 0, sTT = 0, sTV = 0;
            for (int i = 0; i < n; i++)
            {
                double t = (data[i].time.Ticks - t0) / TimeSpan.TicksPerSecond;
                sT += t; sV += data[i].temp; sTT += t * t; sTV += t * data[i].temp;
            }
            double d = n * sTT - sT * sT;
            return Math.Abs(d) < 1e-10 ? 0 : (n * sTV - sT * sV) / d * 60.0;
        }

        #endregion

        #region 온도 읽기/쓰기

        private double ReadSampleTemp()
        {
            try
            {
                var temps = new List<double>();
                double main = ReadChannelTemp(SampleChannel);
                if (!double.IsNaN(main)) temps.Add(main);
                foreach (int ch in AdditionalMonitorChannels)
                { double t = ReadChannelTemp(ch); if (!double.IsNaN(t)) temps.Add(t); }
                return temps.Count > 0 ? temps.Min() : double.NaN;
            }
            catch { return double.NaN; }
        }

        private double ReadHeaterTemp() => ReadChannelTemp(HeaterChannel);

        private double ReadChannelTemp(int channel)
        {
            try
            {
                if (_tempController?.Status?.ChannelStatus == null) return double.NaN;
                int idx = channel - 1;
                if (idx < 0 || idx >= _tempController.Status.ChannelStatus.Length) return double.NaN;
                var ch = _tempController.Status.ChannelStatus[idx];
                return ch.Dot > 0 ? ch.PresentValue / Math.Pow(10, ch.Dot) : ch.PresentValue;
            }
            catch { return double.NaN; }
        }

        private void SetHeaterTemperature(double temp)
        {
            lock (_setpointLock) { _pendingSetpoint = Math.Min(temp, _heaterMax); }
        }

        private double GetHardwareMaxTemp()
        {
            try
            {
                if (_tempController?.IsConnected != true) return 0;
                var ch = _tempController.Status.ChannelStatus[HeaterChannel - 1];
                double raw = _tempController.MaxTemperatureRaw;
                return ch.Dot == 1 ? raw / 10.0 : raw;
            }
            catch { return 0; }
        }

        #endregion

        #region 헬퍼

        private double OffsetMax(double T_s) => Math.Max(1.0, _heaterMax - T_s - 3.0);

        private void ResetState()
        {
            _offset = 0; _equilibriumOffset = 0; _holdCorrection = 0;
            _heaterSV = _initialSampleTemp; _probeEscalations = 0;
            _probeRates.Clear(); _rateBuf.Clear(); _holdTempBuf.Clear();
            _hwSaturated = false; _eqTempPrediction = null; _saturationRates.Clear();
            ActualRate = 0;
            lock (_setpointLock) { _pendingSetpoint = null; }
        }

        private void ReportProgress(double T_s, double T_h)
        {
            double progress = _targetTemp > _initialSampleTemp
                ? Math.Max(0, Math.Min(100, (T_s - _initialSampleTemp) / (_targetTemp - _initialSampleTemp) * 100)) : 0;
            string msg = _state switch
            {
                Phase.Probe => $"Probe (offset={_offset:F1}°C, esc={_probeEscalations})",
                Phase.Ramp => $"Ramp ({T_s:F1}→{_targetTemp:F1}°C, {ActualRate:F2}°C/min)",
                Phase.Converge => $"Converge ({T_s:F1}→{_targetTemp:F1}°C)",
                Phase.Hold => $"Hold ({T_s:F1}°C, eq={_equilibriumOffset:F1}°C)",
                Phase.PressureHold => $"PressureHold (offset={_offset:F1}°C)",
                _ => _state.ToString()
            };
            ProgressUpdated?.Invoke(this, new ProgressEventArgs
            {
                State = _state, HeaterTemp = T_h, SampleTemp = T_s, TargetTemp = _targetTemp,
                HeaterSV = _heaterSV, Offset = _offset, ActualRate = ActualRate,
                ProgressPercent = progress, StatusMessage = msg, ElapsedTime = DateTime.Now - _startTime
            });
        }

        private void OnLog(string msg) => LogMessage?.Invoke(this, $"[Adaptive] {msg}");
        private void OnError(string msg) => ErrorOccurred?.Invoke(this, msg);

        #endregion

        #region IDisposable
        public void Dispose() { _controlTimer?.Stop(); _controlTimer?.Dispose(); }
        #endregion
    }
}
