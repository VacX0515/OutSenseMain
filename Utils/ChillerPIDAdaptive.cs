using System;
using System.Collections.Generic;
using System.Linq;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 칠러 PID 온라인 적응 학습기
    ///
    /// 정상 PID 제어를 유지하면서 슬라이딩 윈도우로 응답 특성을 관측하고
    /// Kp/Ki/Kd를 점진적으로 조정합니다.
    ///
    /// 관측 지표:
    ///   - 진동 지수 (영점교차 빈도) → Kp 감소, Kd 증가
    ///   - 오버슈트 비율 → Kp 감소
    ///   - 정상상태 오차 지속 → Ki 증가
    ///   - 응답 둔감 (큰 오차 미감소) → Kp 증가
    /// </summary>
    public class ChillerPIDAdaptive
    {
        #region 설정

        /// <summary>적응 학습 활성화</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>평가 주기 (초) — 이 간격마다 계수 조정 평가</summary>
        public double EvaluationIntervalSec { get; set; } = 300.0; // 5분

        /// <summary>슬라이딩 윈도우 크기 (초)</summary>
        public double WindowSec { get; set; } = 600.0; // 10분

        /// <summary>1회 조정 최대 비율 (0.05 = 5%)</summary>
        public double MaxAdjustRate { get; set; } = 0.05;

        /// <summary>기준값 대비 최대 누적 변동 비율 (0.5 = ±50%)</summary>
        public double MaxDriftRatio { get; set; } = 0.5;

        /// <summary>진동 판정 임계값 — 윈도우 내 영점교차 횟수/분. 6~10분 주기 limit cycle도 감지하도록 0.3.</summary>
        public double OscillationThreshold { get; set; } = 0.3;

        /// <summary>저주파 진동 진폭(min-max swing) 임계값 (°C). 작은 limit cycle(±0.4°C)도 잡도록 0.6.</summary>
        public double SwingAmplitudeThreshold { get; set; } = 0.6;

        /// <summary>오버슈트 판정 임계값 (°C)</summary>
        public double OvershootThreshold { get; set; } = 0.5;

        /// <summary>정상상태 오차 판정 임계값 (°C)</summary>
        public double SteadyStateErrorThreshold { get; set; } = 0.3;

        /// <summary>응답 둔감 판정 — 오차가 이 값 이상이면서 감소하지 않음</summary>
        public double SluggishErrorThreshold { get; set; } = 1.0;

        #endregion

        #region 상태

        private readonly struct Sample
        {
            public readonly DateTime Time;
            public readonly double Error;       // setpoint - measured
            public readonly double Temperature;

            public Sample(DateTime time, double error, double temp)
            {
                Time = time;
                Error = error;
                Temperature = temp;
            }
        }

        private readonly Queue<Sample> _window = new Queue<Sample>();
        private DateTime _lastEvaluation = DateTime.MinValue;

        // 기준 게인 (학습 시작 시점 또는 리셋 시점의 값)
        private double _baseKp, _baseKi, _baseKd;
        private bool _baseInitialized;

        // 현재 조정된 게인
        private double _currentKp, _currentKi, _currentKd;

        // 통계
        public int TotalAdjustments { get; private set; }
        public string LastAdjustmentReason { get; private set; } = "";
        public DateTime LastAdjustmentTime { get; private set; }

        #endregion

        #region 이벤트

        public event EventHandler<GainAdjustedEventArgs> GainAdjusted;

        public class GainAdjustedEventArgs : EventArgs
        {
            public double Kp { get; set; }
            public double Ki { get; set; }
            public double Kd { get; set; }
            public string Reason { get; set; }
            public double OscillationIndex { get; set; }
            public double MeanError { get; set; }
            public double MaxOvershoot { get; set; }
        }

        #endregion

        /// <summary>
        /// 매 제어 사이클마다 호출. 샘플을 수집하고 주기적으로 게인 조정을 평가합니다.
        /// </summary>
        /// <param name="error">현재 오차 (setpoint - measured)</param>
        /// <param name="temperature">현재 측정 온도</param>
        /// <param name="currentKp">현재 PID Kp</param>
        /// <param name="currentKi">현재 PID Ki</param>
        /// <param name="currentKd">현재 PID Kd</param>
        /// <returns>조정이 발생한 경우 (newKp, newKi, newKd), 아니면 null</returns>
        public (double kp, double ki, double kd)? Update(
            double error, double temperature,
            double currentKp, double currentKi, double currentKd)
        {
            if (!Enabled) return null;

            var now = DateTime.Now;

            // 기준값 초기화
            if (!_baseInitialized)
            {
                _baseKp = currentKp;
                _baseKi = currentKi;
                _baseKd = currentKd;
                _currentKp = currentKp;
                _currentKi = currentKi;
                _currentKd = currentKd;
                _baseInitialized = true;
                _lastEvaluation = now;
            }

            // 샘플 수집
            _window.Enqueue(new Sample(now, error, temperature));

            // 오래된 샘플 제거
            var cutoff = now.AddSeconds(-WindowSec);
            while (_window.Count > 0 && _window.Peek().Time < cutoff)
                _window.Dequeue();

            // 평가 주기 확인
            if ((now - _lastEvaluation).TotalSeconds < EvaluationIntervalSec)
                return null;

            // 최소 데이터 확인 (윈도우의 50% 이상 채워져야)
            if (_window.Count < 10)
                return null;

            _lastEvaluation = now;
            return Evaluate(currentKp, currentKi, currentKd);
        }

        /// <summary>기준값 리셋 (목표온도 변경 등)</summary>
        public void ResetBaseline(double kp, double ki, double kd)
        {
            _baseKp = kp;
            _baseKi = ki;
            _baseKd = kd;
            _currentKp = kp;
            _currentKi = ki;
            _currentKd = kd;
            _baseInitialized = true;
            _window.Clear();
            _lastEvaluation = DateTime.Now;
        }

        /// <summary>전체 리셋</summary>
        public void Reset()
        {
            _window.Clear();
            _baseInitialized = false;
            _lastEvaluation = DateTime.MinValue;
            TotalAdjustments = 0;
        }

        #region 평가 로직

        private (double kp, double ki, double kd)? Evaluate(
            double currentKp, double currentKi, double currentKd)
        {
            var samples = _window.ToArray();
            if (samples.Length < 5) return null;

            // 1. 진동 지수 (영점교차 빈도)
            double oscillationIndex = CalculateOscillationIndex(samples);

            // 2. 오버슈트 (목표값을 넘어간 최대 크기)
            double maxOvershoot = CalculateMaxOvershoot(samples);

            // 3. 평균 절대 오차
            double meanAbsError = samples.Average(s => Math.Abs(s.Error));

            // 4. 오차 추세 (감소 중인지 정체 중인지)
            double errorTrend = CalculateErrorTrend(samples);

            // 5. 저주파 진동 진폭 — 영점교차로는 못 잡히는 칠러의 5~10분 사이클 감지
            double swingAmplitude = CalculateSwingAmplitude(samples);

            // 통합 진동 신호: 영점교차 OR 큰 진폭 OR (오버슈트 + 정상오차 동시)
            bool highFreqOsc = oscillationIndex > OscillationThreshold;
            bool lowFreqOsc = swingAmplitude > SwingAmplitudeThreshold;
            bool overshootAndError = maxOvershoot > OvershootThreshold &&
                                     meanAbsError > SteadyStateErrorThreshold;
            bool isOscillating = highFreqOsc || lowFreqOsc || overshootAndError;

            // 조정 결정
            double dKp = 0, dKi = 0, dKd = 0;
            var reasons = new List<string>();

            // 진동 감지 → Kp/Ki 줄이고 Kd 올림.
            // Ki 감소 폭이 핵심 — 칠러 진동의 주범은 적분 windup이므로 강하게 줄인다.
            if (isOscillating)
            {
                double oscSeverity = 0.5;
                if (highFreqOsc)
                    oscSeverity = Math.Max(oscSeverity, Math.Min(1.0, oscillationIndex / OscillationThreshold));
                if (lowFreqOsc)
                    oscSeverity = Math.Max(oscSeverity, Math.Min(1.0, swingAmplitude / (SwingAmplitudeThreshold * 2)));

                dKp -= MaxAdjustRate * (0.5 + 0.5 * oscSeverity);
                dKi -= MaxAdjustRate * (0.8 + 0.4 * oscSeverity); // ★ Ki 강하게 감소
                dKd += MaxAdjustRate * 0.3 * (1 + oscSeverity);

                string tag = highFreqOsc ? $"진동({oscillationIndex:F2}/min)"
                           : lowFreqOsc  ? $"저주파진동(swing {swingAmplitude:F1}°C)"
                                         : $"오버+오차동시({maxOvershoot:F1}/{meanAbsError:F2}°C)";
                reasons.Add(tag);
            }
            else if (maxOvershoot > OvershootThreshold)
            {
                // 진동 없는 단일 오버슈트 → Kp만 줄임 (Ki도 살짝)
                double severity = Math.Min(1.0, (maxOvershoot - OvershootThreshold) / 2.0);
                dKp -= MaxAdjustRate * (0.3 + 0.3 * severity);
                dKi -= MaxAdjustRate * 0.2 * severity;
                reasons.Add($"오버슈트({maxOvershoot:F1}°C)");
            }

            // 정상상태 오차 지속 + 진동 없음 + 오버슈트 없음 → Ki 올림
            // ★ overshootAndError가 isOscillating에 포함되므로 여기까지 오면 진짜 단조 오차
            if (!isOscillating && maxOvershoot <= OvershootThreshold &&
                meanAbsError > SteadyStateErrorThreshold)
            {
                double severity = Math.Min(1.0, (meanAbsError - SteadyStateErrorThreshold) / 2.0);
                dKi += MaxAdjustRate * (0.3 + 0.3 * severity);
                reasons.Add($"정상오차({meanAbsError:F2}°C)");
            }

            // 응답 둔감 (큰 오차 + 감소 안 함 + 진동/오버슈트 모두 없음) → Kp 올림
            if (!isOscillating && maxOvershoot <= OvershootThreshold * 0.5 &&
                meanAbsError > SluggishErrorThreshold && errorTrend > -0.001)
            {
                double severity = Math.Min(1.0, (meanAbsError - SluggishErrorThreshold) / 3.0);
                dKp += MaxAdjustRate * (0.3 + 0.3 * severity);
                reasons.Add($"둔감({meanAbsError:F1}°C)");
            }

            // 조정할 것이 없으면 스킵
            if (Math.Abs(dKp) < 0.001 && Math.Abs(dKi) < 0.001 && Math.Abs(dKd) < 0.001)
                return null;

            // 비율 적용
            double newKp = currentKp * (1.0 + dKp);
            double newKi = currentKi * (1.0 + dKi);
            double newKd = currentKd * (1.0 + dKd);

            // 기준값 대비 최대 변동 제한
            newKp = Clamp(newKp, _baseKp * (1.0 - MaxDriftRatio), _baseKp * (1.0 + MaxDriftRatio));
            newKi = Clamp(newKi, _baseKi * (1.0 - MaxDriftRatio), _baseKi * (1.0 + MaxDriftRatio));
            newKd = Clamp(newKd, _baseKd * (1.0 - MaxDriftRatio), _baseKd * (1.0 + MaxDriftRatio));

            // 최소값 보장
            newKp = Math.Max(0.05, newKp);
            newKi = Math.Max(0.0005, newKi);
            newKd = Math.Max(0.05, newKd);

            // 실제 변화가 있는지 확인
            if (Math.Abs(newKp - currentKp) < 0.001 &&
                Math.Abs(newKi - currentKi) < 0.0001 &&
                Math.Abs(newKd - currentKd) < 0.001)
                return null;

            _currentKp = newKp;
            _currentKi = newKi;
            _currentKd = newKd;

            string reason = string.Join(", ", reasons);
            LastAdjustmentReason = reason;
            LastAdjustmentTime = DateTime.Now;
            TotalAdjustments++;

            GainAdjusted?.Invoke(this, new GainAdjustedEventArgs
            {
                Kp = newKp,
                Ki = newKi,
                Kd = newKd,
                Reason = reason,
                OscillationIndex = oscillationIndex,
                MeanError = meanAbsError,
                MaxOvershoot = maxOvershoot
            });

            return (newKp, newKi, newKd);
        }

        /// <summary>영점교차 빈도 (회/분)</summary>
        private double CalculateOscillationIndex(Sample[] samples)
        {
            int crossings = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                // 오차 부호가 바뀌면 영점교차
                if (samples[i - 1].Error * samples[i].Error < 0)
                    crossings++;
            }

            double windowMinutes = (samples.Last().Time - samples.First().Time).TotalMinutes;
            return windowMinutes > 0.5 ? crossings / windowMinutes : 0;
        }

        /// <summary>최대 오버슈트 크기 (°C)</summary>
        private double CalculateMaxOvershoot(Sample[] samples)
        {
            // 오버슈트 = 오차가 음수 (측정값 > 목표값)인 구간의 최대 크기
            double maxOvershoot = 0;
            foreach (var s in samples)
            {
                if (s.Error < 0)
                    maxOvershoot = Math.Max(maxOvershoot, Math.Abs(s.Error));
            }
            return maxOvershoot;
        }

        /// <summary>
        /// 오차의 min-max swing (°C). 윈도우 내 최대오차와 최소오차의 차.
        /// 영점교차로는 못 잡히는 저주파(주기가 윈도우 길이 비슷) 진동의 진폭 지표.
        /// </summary>
        private double CalculateSwingAmplitude(Sample[] samples)
        {
            double minErr = double.PositiveInfinity, maxErr = double.NegativeInfinity;
            foreach (var s in samples)
            {
                if (s.Error < minErr) minErr = s.Error;
                if (s.Error > maxErr) maxErr = s.Error;
            }
            return double.IsInfinity(minErr) ? 0 : (maxErr - minErr);
        }

        /// <summary>오차 추세 (음수 = 감소 중, 양수 = 증가/정체)</summary>
        private double CalculateErrorTrend(Sample[] samples)
        {
            if (samples.Length < 5) return 0;

            // 윈도우 전반부 vs 후반부 평균 절대 오차 비교
            int half = samples.Length / 2;
            double firstHalf = 0, secondHalf = 0;

            for (int i = 0; i < half; i++)
                firstHalf += Math.Abs(samples[i].Error);
            firstHalf /= half;

            for (int i = half; i < samples.Length; i++)
                secondHalf += Math.Abs(samples[i].Error);
            secondHalf /= (samples.Length - half);

            // 양수면 오차 증가(둔감), 음수면 감소(수렴 중)
            return secondHalf - firstHalf;
        }

        private static double Clamp(double value, double min, double max)
        {
            return Math.Max(min, Math.Min(max, value));
        }

        #endregion
    }
}
