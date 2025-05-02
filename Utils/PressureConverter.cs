using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 압력 단위 변환 클래스
    /// </summary>
    public static class PressureConverter
    {
        /// <summary>
        /// 압력 단위 열거형
        /// </summary>
        public enum PressureUnit
        {
            Torr,       // 토르 (기본 단위)
            Mbar,       // 밀리바
            Pascal,     // 파스칼
            Bar,        // 바
            Psi,        // 파운드/평방인치
            AtmAtm      // 기압
        }

        // 변환 상수
        private const double TORR_TO_MBAR = 1.33322;      // 1 Torr = 1.33322 mbar
        private const double TORR_TO_PASCAL = 133.322;    // 1 Torr = 133.322 Pa
        private const double TORR_TO_BAR = 0.00133322;    // 1 Torr = 0.00133322 bar
        private const double TORR_TO_PSI = 0.0193368;     // 1 Torr = 0.0193368 psi
        private const double TORR_TO_ATM = 0.00131578;    // 1 Torr = 0.00131578 atm

        /// <summary>
        /// 토르에서 지정된 단위로 압력 변환
        /// </summary>
        /// <param name="torr">토르 단위 압력</param>
        /// <param name="targetUnit">변환할 대상 단위</param>
        /// <returns>변환된 압력 값</returns>
        public static double FromTorr(double torr, PressureUnit targetUnit)
        {
            switch (targetUnit)
            {
                case PressureUnit.Torr:
                    return torr;
                case PressureUnit.Mbar:
                    return torr * TORR_TO_MBAR;
                case PressureUnit.Pascal:
                    return torr * TORR_TO_PASCAL;
                case PressureUnit.Bar:
                    return torr * TORR_TO_BAR;
                case PressureUnit.Psi:
                    return torr * TORR_TO_PSI;
                case PressureUnit.AtmAtm:
                    return torr * TORR_TO_ATM;
                default:
                    return torr;
            }
        }

        /// <summary>
        /// 지정된 단위에서 토르로 압력 변환
        /// </summary>
        /// <param name="pressure">변환할 압력 값</param>
        /// <param name="sourceUnit">원본 압력 단위</param>
        /// <returns>토르 단위로 변환된 압력 값</returns>
        public static double ToTorr(double pressure, PressureUnit sourceUnit)
        {
            switch (sourceUnit)
            {
                case PressureUnit.Torr:
                    return pressure;
                case PressureUnit.Mbar:
                    return pressure / TORR_TO_MBAR;
                case PressureUnit.Pascal:
                    return pressure / TORR_TO_PASCAL;
                case PressureUnit.Bar:
                    return pressure / TORR_TO_BAR;
                case PressureUnit.Psi:
                    return pressure / TORR_TO_PSI;
                case PressureUnit.AtmAtm:
                    return pressure / TORR_TO_ATM;
                default:
                    return pressure;
            }
        }

        /// <summary>
        /// 압력값을 한 단위에서 다른 단위로 변환
        /// </summary>
        /// <param name="pressure">변환할 압력 값</param>
        /// <param name="sourceUnit">원본 압력 단위</param>
        /// <param name="targetUnit">변환할 대상 단위</param>
        /// <returns>변환된 압력 값</returns>
        public static double Convert(double pressure, PressureUnit sourceUnit, PressureUnit targetUnit)
        {
            // 동일한 단위면 변환 없이 반환
            if (sourceUnit == targetUnit)
                return pressure;

            // 먼저 Torr로 변환 후 대상 단위로 변환
            double torr = ToTorr(pressure, sourceUnit);
            return FromTorr(torr, targetUnit);
        }

        /// <summary>
        /// 단위에 해당하는 단위 문자열 반환
        /// </summary>
        /// <param name="unit">압력 단위</param>
        /// <returns>단위 문자열</returns>
        public static string GetUnitString(PressureUnit unit)
        {
            switch (unit)
            {
                case PressureUnit.Torr:
                    return "Torr";
                case PressureUnit.Mbar:
                    return "mbar";
                case PressureUnit.Pascal:
                    return "Pa";
                case PressureUnit.Bar:
                    return "bar";
                case PressureUnit.Psi:
                    return "psi";
                case PressureUnit.AtmAtm:
                    return "atm";
                default:
                    return "";
            }
        }
    }

}
