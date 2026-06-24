using System;
using System.Collections.Generic;
using System.Linq;

namespace VacX_OutSense.Core.AutoRun.AutoCapBakeout
{
    public enum AutoCapPhase
    {
        Warmup = 0,   // 초기 관측 (첫 예측 대기 중)
        Ramp = 1,     // 반복 보정 중 (iter > 0)
        Hold = 2,     // target ± tol 안착, 유지
    }

    public sealed class AutoCapConfig
    {
        public double Target { get; set; } = 65.0;            // °C
        public double Tolerance { get; set; } = 0.5;          // °C (하드 제약)
        public double HeaterMax { get; set; } = 120.0;        // °C
        public double TEnv { get; set; } = 25.0;              // °C
        public double FeedbackIntervalSec { get; set; } = 5.0;
        public int HoldTimeMinutes { get; set; } = 1440;
        /// <summary>T_s 가 target±tol 에 연속 유지되어야 Hold 진입하는 시간 (sec). "안정화 유지시간".</summary>
        public double StabilizationSec { get; set; } = 600.0;  // 10분 기본 (사용자 "안정화 유지시간" 설정 연동 가능)

        // ── v3 Trajectory Prediction 파라미터 ──
        /// <summary>Rate 계산 윈도우 (sec). 한 rate 점당 데이터.</summary>
        public double RateWindowSec { get; set; } = 600.0;  // 10분
        /// <summary>예측 실행 간격 (sec).</summary>
        public double PredictionIntervalSec { get; set; } = 300.0;  // 5분
        /// <summary>(T_s, rate) 샘플 최소 개수.</summary>
        public int PredictionSamplesMin { get; set; } = 5;
        /// <summary>샘플 간 간격 (sec).</summary>
        public double SampleSpacingSec { get; set; } = 300.0;  // 5분
        /// <summary>연속 예측 안정성 필요 수.</summary>
        public int PredictionStabilityCount { get; set; } = 4;
        /// <summary>연속 예측 표준편차 한계 (°C).</summary>
        public double PredictionStabilityStd { get; set; } = 0.2;
        /// <summary>Fit 품질: 잔차 RMS 상한.</summary>
        public double FitRmsMax { get; set; } = 1.0;
        /// <summary>iter 시작 후 최소 관측 시간 (sec).</summary>
        public double ObsMinSec { get; set; } = 900.0;  // 15분
        /// <summary>예측 불가 시 max_iter_sec 후 강제 step.</summary>
        public double MaxIterSec { get; set; } = 4 * 3600;

        // β 학습 점프
        /// <summary>β 2회 일치 시 needed_cap 직접 계산 활성화.</summary>
        public bool EnableBetaJump { get; set; } = true;
        /// <summary>β 일치 판정 (상대차).</summary>
        public double BetaConsistencyThreshold { get; set; } = 0.08;
        /// <summary>점프 aim 마진: target - tol × this.</summary>
        public double BetaJumpAimMargin { get; set; } = 0.5;
        public double BetaPhysicalMin { get; set; } = 0.05;
        public double BetaPhysicalMax { get; set; } = 5.0;

        /// <summary>계단식 스텝 계수.</summary>
        public double StepK { get; set; } = 0.7;

        /// <summary>SV cap 도달 후 대기 시간 (heater settle).</summary>
        public double SvAtCapMinSec { get; set; } = 600.0;  // 10분
        public double RampUpRatePerHour { get; set; } = 20.0;
        public double RampDownRatePerHour { get; set; } = 40.0;

        public double EmaAlpha { get; set; } = 0.1;

        // Panic
        public double PanicThresholdFactor { get; set; } = 0.7;
        public double PanicStep { get; set; } = 5.0;
        public double PanicCooldownSec { get; set; } = 120.0;

        // Legacy (deprecated)
        [Obsolete("v3: 사용 안 함")] public double MaxStepUp { get; set; } = 10.0;
        [Obsolete("v3: 사용 안 함")] public double RateDecreaseFactor { get; set; } = 0.5;
        [Obsolete("v3: 사용 안 함")] public double MinWaitAfterCapSec { get; set; } = 600.0;
        [Obsolete("v3: 사용 안 함")] public double MaxWaitAfterCapSec { get; set; } = 2400.0;
        [Obsolete("v3: 사용 안 함")] public int ApproachingConfirmCount { get; set; } = 2;
        [Obsolete("v3: 사용 안 함")] public double BetaSafetyFactor { get; set; } = 0.9;
        [Obsolete("v3: 사용 안 함")] public double BetaStableFactor { get; set; } = 0.97;
        [Obsolete("v3: 사용 안 함")] public double InitialOverhead { get; set; } = 5.0;
        [Obsolete("v3: 사용 안 함")] public double PlateauHoldSec { get; set; } = 600.0;
        [Obsolete("v3: 사용 안 함")] public double PlateauMinSec { get; set; } = 1800.0;
        [Obsolete("v3: 사용 안 함")] public double PlateauMaxSec { get; set; } = 6 * 3600;
        [Obsolete("v3: PredictionIntervalSec 사용")] public double RateWindowForPlateauSec { get; set; } = 600.0;
        [Obsolete("v3: 사용 안 함")] public double RateThresholdPerMin { get; set; } = 0.02;
        [Obsolete("v3: 사용 안 함")] public double ObsAvgWindowSec { get; set; } = 600.0;
        [Obsolete("v3: 사용 안 함")] public double DeltaWindowSec { get; set; } = 1800.0;
        [Obsolete("v3: 사용 안 함")] public double DeltaThresholdFactor { get; set; } = 0.3;
        [Obsolete("v3: 사용 안 함")] public double DeltaThresholdAbsMin { get; set; } = 0.15;
    }

    public sealed class AutoCapTelemetry
    {
        public AutoCapPhase Phase { get; set; }
        public double Cap { get; set; }
        public double LastSV { get; set; }
        public double SmoothedRatePerMin { get; set; }
        public int StepCount { get; set; }
        public int PanicCount { get; set; }
        public double BetaObservedLast { get; set; }
        public double TimeSinceCapChangeSec { get; set; }
        public int ApproachingCount { get; set; }
        public bool FullyStableDetected { get; set; }
        public double HoldStartTimeSec { get; set; } = -1;
        public double LastTsObserved { get; set; }
        public double LastGap { get; set; }
        public double LastTsEqPredicted { get; set; }  // v3: trajectory 예측값
        public double LastTauPredicted { get; set; }   // v3
        public int PredictionCount { get; set; }       // v3
    }

    /// <summary>
    /// Iterative v3 — Continuous Trajectory Prediction Controller.
    ///
    /// 핵심 원리:
    ///   1차 시스템: T_s(t) = T_s_eq - A·exp(-t/τ_eff)
    ///              rate(t) = (T_s_eq - T_s) / τ_eff
    ///   선형 관계:  T_s = T_s_eq - τ_eff × rate
    ///   (T_s_i, rate_i) 점들 선형회귀 → T_s_eq (intercept), τ_eff (-slope) 동시 추출.
    ///
    /// 알고리즘:
    ///   1. cap = target 시작 (β 무관 안전)
    ///   2. SV 가 cap 도달 후 sv_at_cap_min_sec + obs_min_sec 관측
    ///   3. 매 5분: (T_s, rate) 샘플 수집 → 회귀 → T_s_eq 예측
    ///   4. 최근 N회(=4) 예측 표준편차 &lt; 0.2°C 이면 안정
    ///   5. 안정 시: β 계산, cap += gap × k 또는 β 점프
    ///   6. β 2회 연속 일치 시 needed_cap 직접 계산 (점프)
    ///   7. |gap| ≤ tol → Hold
    ///   8. Panic: T_s > target + tol × 0.7 시 cap 감소
    ///
    /// v2 대비 장점: 평형 기다리지 않고 과도기 기울기로 예측 → 2~3배 가속.
    /// v2 대비 단점: tol &lt; 0.2°C 에서 예측 노이즈가 tol 수준 → 미세 위반 가능.
    ///
    /// 시뮬레이션 레퍼런스: VacX_TestManager/auto_cap_simulator/iterative_v3.py
    /// </summary>
    public sealed class AutoCapBakeoutController
    {
        private readonly AutoCapConfig _cfg;
        private AutoCapPhase _phase = AutoCapPhase.Warmup;
        private double _cap;
        private double _svRamped;
        private double _svAtCapT = double.NegativeInfinity;
        private double _svLast;
        private double _tsFiltered;
        private double _thFiltered;
        private double _smoothedRatePerSec;
        private double _lastStepT;
        private double _lastHistoryT = double.NegativeInfinity;
        private double _iterStartT;
        private double _lastPredictionT = double.NegativeInfinity;
        private readonly List<double> _recentPredictions = new();
        private double _lastTsEqPred = double.NaN;
        private double _lastTauPred = double.NaN;
        private double _holdStartT = -1.0;
        private int _iterationCount;
        private int _panicCount;
        private bool _plateauDetectedThisCycle;
        private double _betaObsLast;
        private double _lastTsObserved;
        private double _lastGap;
        private double _lastPanicT = double.NegativeInfinity;
        private double _capChangeT;
        private readonly Queue<(double t, double Ts, double Th)> _history;
        private readonly List<double> _betaHistory = new();
        private bool _betaJumpDone;
        // 도달/안정화 추적 (iter 와 독립적)
        private double _tsInTolStartT = double.NegativeInfinity;  // T_s 가 tol 진입 시점
        private bool _hasArrivedOnce;  // 한 번이라도 tol 안 들어온 적 있음
        // 실시간 target 변경 감지
        private double _targetLast;
        private double _toleranceLast;

        public readonly List<(double t, string kind, string detail)> Events = new();

        public AutoCapBakeoutController(AutoCapConfig cfg, double T_s0, double T_h0)
        {
            _cfg = cfg;
            // 초기 cap 결정 — 사용자 원칙:
            //   A) T_s 가 이미 target+tol 초과: cap = target+tol (강제 냉각)
            //   B) target > T_s 이고 T_h 가 이미 target 위: cap = T_h (히터 유지, 식히지 마)
            //      샘플이 plateau 면 β 즉시 관측 가능 → 헛 cool-down 없이 needed_cap 점프.
            //   C) 그 외: cap = target (β 무관 안전, 점진 가열)
            double safeCap = cfg.Target + cfg.Tolerance;
            if (T_s0 > cfg.Target + cfg.Tolerance)
                _cap = Math.Min(safeCap, cfg.HeaterMax);
            else if (T_h0 > cfg.Target + 0.5 && T_s0 < cfg.Target - cfg.Tolerance)
                _cap = Math.Min(T_h0, cfg.HeaterMax);
            else
                _cap = Math.Min(cfg.Target, cfg.HeaterMax);
            _svRamped = Math.Min(T_h0, _cap);
            _svLast = _svRamped;
            _tsFiltered = T_s0;
            _thFiltered = T_h0;
            _capChangeT = 0.0;
            _iterStartT = 0.0;
            int histCap = Math.Max(200, (int)(cfg.RateWindowSec * 4 / cfg.FeedbackIntervalSec) + 100);
            _history = new Queue<(double, double, double)>(histCap);
            _targetLast = cfg.Target;
            _toleranceLast = cfg.Tolerance;
        }

        public AutoCapPhase Phase => _phase;
        public double Cap => _cap;
        public double HoldStartTimeSec => _holdStartT;

        /// <summary>t_center 에서 rate (과거 rate_window 시간 linear fit of T_s vs t).</summary>
        private double? RateAt(double tCenter)
        {
            double tStart = tCenter - _cfg.RateWindowSec;
            var pts = _history.Where(h => h.t >= tStart && h.t <= tCenter).ToList();
            if (pts.Count < 5) return null;
            int n = pts.Count;
            double meanT = pts.Sum(p => p.t) / n;
            double meanY = pts.Sum(p => p.Ts) / n;
            double num = pts.Sum(p => (p.t - meanT) * (p.Ts - meanY));
            double den = pts.Sum(p => (p.t - meanT) * (p.t - meanT));
            if (den < 1e-9) return null;
            return num / den;
        }

        /// <summary>현 iter 내에서 (T_s, rate) 샘플 수집.</summary>
        private List<(double Ts, double rate)> CollectSamples(double t)
        {
            var result = new List<(double, double)>();
            if (_svAtCapT <= double.NegativeInfinity) return result;
            double startT = _svAtCapT + _cfg.SvAtCapMinSec;
            double earliestSampleT = startT + _cfg.RateWindowSec;
            double lastSampleT = double.NegativeInfinity;
            foreach (var h in _history)
            {
                if (h.t < earliestSampleT) continue;
                if (h.t - lastSampleT < _cfg.SampleSpacingSec) continue;
                double? rate = RateAt(h.t);
                if (!rate.HasValue) continue;
                result.Add((h.Ts, rate.Value));
                lastSampleT = h.t;
            }
            return result;
        }

        /// <summary>(T_s, rate) → τ_eff (slope-only). T_s_eq 는 현 상태 외삽으로 별도 계산.
        /// Python v3 검증: intercept 기반 T_s_eq 는 (T_s - T_env) 작을 때 노이즈 폭주 → β 4.88 (실제 2.2).
        /// slope 만 사용하면 robust.</summary>
        private (double tau, double rms)? FitTau(List<(double Ts, double rate)> samples)
        {
            int n = samples.Count;
            if (n < _cfg.PredictionSamplesMin) return null;
            double meanX = samples.Sum(s => s.rate) / n;
            double meanY = samples.Sum(s => s.Ts) / n;
            double num = samples.Sum(s => (s.rate - meanX) * (s.Ts - meanY));
            double den = samples.Sum(s => (s.rate - meanX) * (s.rate - meanX));
            if (den < 1e-12) return null;
            double slope = num / den;        // dT_s / drate = -τ_eff (음수)
            double intercept = meanY - slope * meanX;
            if (slope >= 0) return null;     // 비물리적
            double tau = -slope;
            double rss = samples.Sum(s =>
            {
                double residual = s.Ts - (intercept + slope * s.rate);
                return residual * residual;
            });
            double rms = Math.Sqrt(rss / n);
            if (rms > _cfg.FitRmsMax) return null;
            return (tau, rms);
        }

        private bool IsPredictionStable()
        {
            int need = _cfg.PredictionStabilityCount;
            if (_recentPredictions.Count < need) return false;
            var recent = _recentPredictions.Skip(_recentPredictions.Count - need).ToList();
            double mean = recent.Average();
            double var = recent.Sum(p => (p - mean) * (p - mean)) / recent.Count;
            double std = Math.Sqrt(var);
            return std < _cfg.PredictionStabilityStd;
        }

        public double Step(double t, double tSMeas, double tHMeas)
        {
            var cfg = _cfg;

            // ── 0. 실시간 target/tolerance 변경 감지 ──
            bool targetChanged = Math.Abs(cfg.Target - _targetLast) > 0.01;
            bool tolChanged = Math.Abs(cfg.Tolerance - _toleranceLast) > 0.001;
            if (targetChanged || tolChanged)
            {
                double targetDelta = cfg.Target - _targetLast;
                Events.Add((t, "target_change",
                    $"target {_targetLast:F2}→{cfg.Target:F2}, tol {_toleranceLast:F3}→{cfg.Tolerance:F3}"));
                _targetLast = cfg.Target;
                _toleranceLast = cfg.Tolerance;
                if (_phase == AutoCapPhase.Hold)
                {
                    _phase = AutoCapPhase.Ramp;
                    _holdStartT = -1.0;
                }
                _recentPredictions.Clear();
                _lastPredictionT = double.NegativeInfinity;
                _iterStartT = t;
                if (targetDelta < 0 && _tsFiltered >= cfg.Target - cfg.Tolerance * 0.5)
                {
                    double newSafeCap = cfg.Target + cfg.Tolerance;
                    if (_cap > newSafeCap)
                    {
                        _cap = newSafeCap;
                        _capChangeT = t;
                        _svAtCapT = double.NegativeInfinity;
                    }
                }
            }

            // 1. EMA
            double alpha = cfg.EmaAlpha;
            _tsFiltered = (1 - alpha) * _tsFiltered + alpha * tSMeas;
            _thFiltered = (1 - alpha) * _thFiltered + alpha * tHMeas;

            // 2. History
            if (_history.Count == 0 || t - _lastHistoryT >= cfg.FeedbackIntervalSec * 0.99)
            {
                _history.Enqueue((t, _tsFiltered, _thFiltered));
                _lastHistoryT = t;
                double keepSec = cfg.RateWindowSec * 4 + 60;
                while (_history.Count > 0 && t - _history.Peek().t > keepSec)
                    _history.Dequeue();
            }

            double? rateNow = RateAt(t);
            if (rateNow.HasValue) _smoothedRatePerSec = rateNow.Value;

            // 3. Panic
            double panicThr = cfg.Target + cfg.Tolerance * cfg.PanicThresholdFactor;
            if (_tsFiltered > panicThr && (t - _lastPanicT) >= cfg.PanicCooldownSec)
            {
                _cap = Math.Max(cfg.TEnv, _cap - cfg.PanicStep);
                _lastPanicT = t;
                _capChangeT = t;
                _panicCount++;
                _recentPredictions.Clear();
                _lastPredictionT = double.NegativeInfinity;
                _iterStartT = t;
                _svAtCapT = double.NegativeInfinity;
                Events.Add((t, "panic",
                    $"T_s={_tsFiltered:F2} > {panicThr:F2}, cap→{_cap:F1}"));
            }

            // ── 4. 도달/안정화 추적 (iter 와 독립) ──
            // T_s 가 target±tol 에 있으면 타이머 유지, 벗어나면 리셋.
            // 타이머가 StabilizationSec 지나면 Hold 진입.
            bool inTol = Math.Abs(_tsFiltered - cfg.Target) <= cfg.Tolerance;
            if (_phase != AutoCapPhase.Hold)
            {
                if (inTol)
                {
                    if (_tsInTolStartT == double.NegativeInfinity)
                    {
                        _tsInTolStartT = t;
                        if (!_hasArrivedOnce)
                        {
                            _hasArrivedOnce = true;
                            Events.Add((t, "arrival", $"T_s={_tsFiltered:F2} first reached target±tol"));
                        }
                    }
                    else if (t - _tsInTolStartT >= cfg.StabilizationSec)
                    {
                        _phase = AutoCapPhase.Hold;
                        _holdStartT = t;
                        Events.Add((t, "hold",
                            $"T_s={_tsFiltered:F2} stabilized for {(t - _tsInTolStartT) / 60:F1}min"));
                    }
                }
                else
                {
                    _tsInTolStartT = double.NegativeInfinity;  // 벗어남 → 리셋
                }
            }

            // 5. Observe (예측 → cap 조정). Hold 아니고 아직 안정화 안 됐으면.
            if (_phase != AutoCapPhase.Hold && !inTol)
            {
                // 주: 여기 안쪽 로직은 cap 을 어떻게 조정할지만 결정.
                //     Hold 진입 결정은 위의 도달/안정화 블록이 담당.
                {
                    bool svSettled = _svAtCapT > double.NegativeInfinity
                                     && (t - _svAtCapT) >= cfg.SvAtCapMinSec;
                    double iterElapsed = t - _iterStartT;

                    // ── 즉시 plateau 감지: rate ≈ 0 sustained → β 직접 계산 ──
                    //   허용 조건:
                    //     (A) iter==0 (초기 상태) — 처음 진짜 평형
                    //     (B) gap > tol·5 (충분히 멀어 overshoot 위험 없는 안전 영역)
                    //   cap 변경 후 transient 가 plateau 로 오판되더라도 (B) 조건에서는
                    //   target 한참 아래라 cap 좀 올려도 안전.
                    double rateMinAbs = Math.Abs(_smoothedRatePerSec * 60.0);
                    double rateThr = cfg.RateThresholdPerMin > 0 ? cfg.RateThresholdPerMin : 0.02;
                    double gapForSafety = cfg.Target - _tsFiltered;
                    bool inSafeFarRegion = gapForSafety > cfg.Tolerance * 5.0;
                    bool plateauGate = _iterationCount == 0 || inSafeFarRegion;
                    // iter>0 안전영역 plateau 는 추가 가드 필요 (5월 7일 transient false positive 회피):
                    //   1) iterElapsed >= 30min (cap 변경 후 transient 안정화)
                    //   2) 최근 5분 T_s spread < 0.3°C (진짜 flat 확인)
                    double iterMinForPlateau = _iterationCount == 0 ? 600.0 : 1800.0;
                    bool spreadOk = true;
                    if (_iterationCount > 0)
                    {
                        double tWinStart = t - 300.0;
                        double tsMinW = double.MaxValue, tsMaxW = double.MinValue;
                        int cntW = 0;
                        foreach (var h in _history)
                        {
                            if (h.t >= tWinStart)
                            {
                                if (h.Ts < tsMinW) tsMinW = h.Ts;
                                if (h.Ts > tsMaxW) tsMaxW = h.Ts;
                                cntW++;
                            }
                        }
                        spreadOk = cntW >= 5 && (tsMaxW - tsMinW) < 0.3;
                    }
                    if (plateauGate
                        && svSettled && rateMinAbs < rateThr
                        && iterElapsed >= iterMinForPlateau
                        && spreadOk
                        && _tsFiltered > cfg.TEnv + 1.0
                        && _thFiltered > _tsFiltered + 1.0)
                    {
                        double denom = _tsFiltered - cfg.TEnv;
                        // iter>0 안전영역 케이스: T_h가 cap에 도달했으므로 (cap - T_s)/denom 사용
                        //   (T_h - T_s) 대신 (cap - T_s) — T_h 잡음 회피
                        double bp = _iterationCount == 0
                            ? (_thFiltered - _tsFiltered) / denom
                            : (_cap - _tsFiltered) / denom;
                        if (bp > cfg.BetaPhysicalMin && bp < cfg.BetaPhysicalMax)
                        {
                            // β 점프: target 도달 위한 needed_cap 직접 계산
                            double aim = cfg.Target - cfg.Tolerance * cfg.BetaJumpAimMargin;
                            double nc = (1 + bp) * aim - bp * cfg.TEnv;
                            // ★ 한 번에 너무 큰 점프 금지: cap_new ≤ current + gap·1.5
                            //   β 과대추정 시 폭주 방지 (2026-05-11 사례).
                            double gapHere = Math.Abs(cfg.Target - _tsFiltered);
                            double ncCapped = Math.Min(nc, _cap + gapHere * 1.5);
                            double newCap = Math.Min(Math.Max(ncCapped, _cap + 0.1), cfg.HeaterMax);
                            _betaObsLast = bp;
                            _betaHistory.Add(bp);
                            _lastTsObserved = _tsFiltered;
                            _lastGap = cfg.Target - _tsFiltered;
                            _cap = newCap;
                            _iterationCount++;
                            _recentPredictions.Clear();
                            _lastPredictionT = double.NegativeInfinity;
                            _iterStartT = t;
                            _capChangeT = t;
                            _svAtCapT = double.NegativeInfinity;
                            if (_phase == AutoCapPhase.Warmup) _phase = AutoCapPhase.Ramp;
                            Events.Add((t, $"iter{_iterationCount}_plateau",
                                $"plateau β={bp:F3} (T_h={_thFiltered:F2}, T_s={_tsFiltered:F2}) → cap→{_cap:F2}"));
                            // 이번 사이클은 여기까지 — SV ramp 단계로 넘김
                            goto SkipPrediction;
                        }
                    }

                    bool shouldPredict = svSettled
                                         && iterElapsed >= cfg.ObsMinSec
                                         && t - _lastPredictionT >= cfg.PredictionIntervalSec;
                    if (shouldPredict)
                    {
                        _lastPredictionT = t;
                        var samples = CollectSamples(t);
                        var result = FitTau(samples);
                        if (result.HasValue && rateNow.HasValue)
                        {
                            _lastTauPred = result.Value.tau;
                            // T_s_eq 외삽 (현재 T_s + rate × τ_eff, 단위: °C/s × s = °C)
                            // intercept 기반 회귀보다 노이즈에 강건 (Python v3 검증).
                            double TsEqExtrap = _tsFiltered + rateNow.Value * _lastTauPred;
                            _lastTsEqPred = TsEqExtrap;
                            _recentPredictions.Add(TsEqExtrap);
                            int maxKeep = cfg.PredictionStabilityCount * 2;
                            if (_recentPredictions.Count > maxKeep)
                                _recentPredictions.RemoveAt(0);
                            _plateauDetectedThisCycle = true;
                        }
                    }

                    // 안정 판정
                    if (IsPredictionStable())
                    {
                        int n = cfg.PredictionStabilityCount;
                        double TsEqEst = _recentPredictions.Skip(_recentPredictions.Count - n).Average();
                        double gap = cfg.Target - TsEqEst;
                        _lastTsObserved = TsEqEst;
                        _lastGap = gap;
                        double denom = TsEqEst - cfg.TEnv;
                        double? betaObs = null;
                        if (Math.Abs(denom) > 1.0)
                        {
                            double bRaw = Math.Max(0.0, _cap - TsEqEst) / denom;
                            if (bRaw > cfg.BetaPhysicalMin && bRaw < cfg.BetaPhysicalMax)
                            {
                                // β EMA 평활화 (Python v3 검증): 0.6·old + 0.4·new.
                                // 노이즈로 인한 cap 진동 방지 (raw β 사용시 8회 iter 이상 진동 관측됨).
                                double bSmoothed = _betaHistory.Count == 0
                                    ? bRaw
                                    : 0.6 * _betaHistory[_betaHistory.Count - 1] + 0.4 * bRaw;
                                betaObs = bSmoothed;
                                _betaObsLast = bSmoothed;
                                _betaHistory.Add(bSmoothed);
                            }
                        }

                        if (Math.Abs(gap) <= cfg.Tolerance)
                        {
                            // 예측상 이미 target 근처 평형. cap 유지, T_s 실제 도달 대기.
                            // Hold 결정은 위의 도달/안정화 블록이 담당.
                            Events.Add((t, "cap_holding",
                                $"T_s_eq_pred={TsEqEst:F2} gap={gap:+0.00;-0.00} waiting for T_s arrival"));
                        }
                        else
                        {
                            // β 점프 가능?
                            double? newCap = null;
                            bool isJump = false;
                            if (cfg.EnableBetaJump && !_betaJumpDone && _betaHistory.Count >= 2)
                            {
                                double b1 = _betaHistory[_betaHistory.Count - 2];
                                double b2 = _betaHistory[_betaHistory.Count - 1];
                                double avgBeta = (b1 + b2) / 2;
                                double consistency = avgBeta > 0 ? Math.Abs(b1 - b2) / avgBeta : 1;
                                if (consistency < cfg.BetaConsistencyThreshold)
                                {
                                    double aim = cfg.Target - cfg.Tolerance * cfg.BetaJumpAimMargin;
                                    double nc = (1 + avgBeta) * aim - avgBeta * cfg.TEnv;
                                    // ★ 한 번에 너무 큰 점프 금지: cap_new ≤ current + gap·1.5
                                    double ncCapped = Math.Min(nc, _cap + Math.Abs(gap) * 1.5);
                                    newCap = Math.Min(Math.Max(ncCapped, _cap + 0.1), cfg.HeaterMax);
                                    _betaJumpDone = true;
                                    isJump = true;
                                }
                            }
                            if (!newCap.HasValue)
                            {
                                // 적응형 k: gap 크면 빠르게, target 근접 시 신중
                                double absGap = Math.Abs(gap);
                                double adaptiveK = absGap > 15.0 ? 0.7
                                                 : absGap > 5.0 ? cfg.StepK
                                                 : 0.3;
                                double step = gap * adaptiveK;
                                double nc = Math.Min(_cap + step, cfg.HeaterMax);
                                newCap = Math.Max(nc, cfg.TEnv);
                            }
                            _cap = newCap.Value;
                            _iterationCount++;
                            _recentPredictions.Clear();
                            _lastPredictionT = double.NegativeInfinity;
                            _iterStartT = t;
                            _capChangeT = t;
                            _svAtCapT = double.NegativeInfinity;
                            if (_phase == AutoCapPhase.Warmup) _phase = AutoCapPhase.Ramp;
                            string kind = isJump ? $"iter{_iterationCount}_jump" : $"iter{_iterationCount}";
                            string btext = betaObs.HasValue ? $" β={betaObs.Value:F3}" : "";
                            Events.Add((t, kind,
                                $"T_s_eq={TsEqEst:F2} gap={gap:+0.00;-0.00} cap→{_cap:F2}{btext}"));
                        }
                    }
                    // Fallback: 예측 실패 + max_iter_sec 초과 → gap 기반 강제 step
                    else if (iterElapsed > cfg.MaxIterSec)
                    {
                        double gap = cfg.Target - _tsFiltered;
                        if (Math.Abs(gap) > cfg.Tolerance)
                        {
                            double step = gap * cfg.StepK;
                            double nc = Math.Min(Math.Max(_cap + step, cfg.TEnv), cfg.HeaterMax);
                            _cap = nc;
                            _iterationCount++;
                            _recentPredictions.Clear();
                            _lastPredictionT = double.NegativeInfinity;
                            _iterStartT = t;
                            _capChangeT = t;
                            _svAtCapT = double.NegativeInfinity;
                            if (_phase == AutoCapPhase.Warmup) _phase = AutoCapPhase.Ramp;
                            Events.Add((t, $"iter{_iterationCount}_timeout",
                                $"T_s={_tsFiltered:F2} gap={gap:+0.00;-0.00} cap→{_cap:F2}"));
                        }
                    }
                    SkipPrediction:;
                }
            }

            // 5. SV ramp-limited
            double targetSv = Math.Min(Math.Max(_cap, cfg.TEnv), cfg.HeaterMax);
            double dtStep = _lastStepT > 0 ? Math.Max(t - _lastStepT, 0.0) : cfg.FeedbackIntervalSec;
            if (cfg.RampUpRatePerHour > 0)
            {
                double upStep = (cfg.RampUpRatePerHour / 3600.0) * dtStep;
                double downRate = cfg.RampDownRatePerHour > 0 ? cfg.RampDownRatePerHour : cfg.RampUpRatePerHour * 2;
                double downStep = (downRate / 3600.0) * dtStep;
                if (targetSv > _svRamped)
                    _svRamped = Math.Min(_svRamped + upStep, targetSv);
                else if (targetSv < _svRamped)
                    _svRamped = Math.Max(_svRamped - downStep, targetSv);
            }
            else
            {
                _svRamped = targetSv;
            }
            // SV-at-cap 갱신
            if (Math.Abs(_svRamped - targetSv) < 0.15)
            {
                if (_svAtCapT == double.NegativeInfinity)
                    _svAtCapT = t;
            }
            else
            {
                _svAtCapT = double.NegativeInfinity;
            }

            double SV = _svRamped;
            _svLast = SV;
            _lastStepT = t;
            return SV;
        }

        public AutoCapTelemetry GetTelemetry()
        {
            return new AutoCapTelemetry
            {
                Phase = _phase,
                Cap = _cap,
                LastSV = _svLast,
                SmoothedRatePerMin = _smoothedRatePerSec * 60.0,
                StepCount = _iterationCount,
                PanicCount = _panicCount,
                BetaObservedLast = _betaObsLast,
                TimeSinceCapChangeSec = _lastStepT - _capChangeT,
                ApproachingCount = _recentPredictions.Count,
                FullyStableDetected = _plateauDetectedThisCycle,
                HoldStartTimeSec = _holdStartT,
                LastTsObserved = _lastTsObserved,
                LastGap = _lastGap,
                LastTsEqPredicted = _lastTsEqPred,
                LastTauPredicted = _lastTauPred,
                PredictionCount = _recentPredictions.Count,
            };
        }
    }
}
