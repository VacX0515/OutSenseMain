using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Core.Devices.Gauges
{
    /// <summary>
    /// TTR91RN-16KF 모델용 피라니 게이지 클래스
    /// 텅스텐/레늄 필라멘트 사용, 5E-4 Torr ~ 750 Torr 측정 범위
    /// 표준 출력 특성 (1.9V ~ 10.0V)
    /// 기본 압력 단위: Torr
    /// </summary>
    public class PiraniGauge
    {
        // 출력 전압 범위 (표준 출력 특성)
        private const double MIN_VOLTAGE = 1.9;    // 최소 전압값 (V)
        private const double MAX_VOLTAGE = 10.0;   // 최대 전압값 (V)

        // 변환 계수 (Torr 단위 기준)
        private const double COEFFICIENT_A = 6.304;
        private const double COEFFICIENT_B = 1.286;

        /// <summary>
        /// 게이지에서 지원하는 가스 종류
        /// </summary>
        public enum Gas
        {
            Helium,         // 헬륨
            Neon,           // 네온
            Nitrogen,       // 질소
            Argon,          // 아르곤
            CarbonDioxide,  // 이산화탄소
            Krypton,        // 크립톤
            Xenon           // 제논
        }

        /// <summary>
        /// 전압을 압력으로 변환 (log-linear 변환 사용)
        /// 기본 단위는 Torr로 반환
        /// </summary>
        /// <param name="voltage">측정된 전압 값 (V)</param>
        /// <returns>변환된 압력 값 (Torr)</returns>
        public double ConvertVoltageToPressure(double voltage)
        {
            // 전압이 범위를 벗어나면 제한
            if (voltage < MIN_VOLTAGE)
                voltage = MIN_VOLTAGE;
            else if (voltage > MAX_VOLTAGE)
                voltage = MAX_VOLTAGE;

            // 로그 선형 변환 공식 적용: P = 10^((V-a)/b)
            double pressureInTorr = Math.Pow(10, (voltage - COEFFICIENT_A) / COEFFICIENT_B);

            return pressureInTorr;
        }

        /// <summary>
        /// 측정된 압력에 가스 보정 인자를 적용
        /// </summary>
        /// <param name="indicatedPressure">게이지가 표시한 압력 (보정 전)</param>
        /// <param name="gasType">가스 종류</param>
        /// <returns>실제 압력 (보정 후)</returns>
        public double ApplyGasCorrectionFactor(double indicatedPressure, Gas gasType)
        {
            // 1 Torr 이하에서의 가스 보정 인자 적용
            double gcf = GetGasCorrectionFactor(gasType);

            // 실제 압력 = 보정 인자 * 표시된 압력
            return gcf * indicatedPressure;
        }

        /// <summary>
        /// 가스 종류에 따른 보정 인자 반환
        /// </summary>
        /// <param name="gasType">가스 종류</param>
        /// <returns>가스 보정 인자</returns>
        private double GetGasCorrectionFactor(Gas gasType)
        {
            switch (gasType)
            {
                case Gas.Helium:
                    return 1.1;
                case Gas.Neon:
                    return 1.5;
                case Gas.Nitrogen:
                    return 1.0; // 기준 가스
                case Gas.Argon:
                    return 1.7;
                case Gas.CarbonDioxide:
                    return 1.0;
                case Gas.Krypton:
                    return 2.5;
                case Gas.Xenon:
                    return 3.0;
                default:
                    return 1.0; // 기본값은 질소 기준
            }
        }

        /// <summary>
        /// 오류 코드 확인 (LED 표시등 상태에 따라)
        /// </summary>
        /// <param name="voltage">측정된 전압</param>
        /// <returns>오류 상태. null이면 정상</returns>
        public string CheckErrorCode(double voltage)
        {
            // 오류 전압 값 (0.1V)
            const double ERROR_VOLTAGE = 0.1;

            if (Math.Abs(voltage - ERROR_VOLTAGE) < 0.05)
            {
                return "게이지 오류: 필라멘트 손상 또는 튜브 미삽입";
            }

            return null; // 정상
        }
    }
}