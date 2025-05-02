using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Core.Devices.Gauges
{
    public class IonGauge
    {
        // PTR 225 Penning Transmitter 게이지의 전압-압력 변환
        // 출력 전압 범위: 0.66V - 10.0V (로그 스케일, 1.333V/decade)
        // 측정 압력 범위: 1×10^-9 - 1×10^-2 mbar

        // 기본 상태값 (게이지 점화되지 않음)
        private const double NotIgnitedVoltage = 0.4; // 점화되지 않았을 때 전압

        // 전압-압력 변환을 위한 파라미터
        private const double VoltPerDecade = 1.333; // 전압 변화량 (V/decade)
        private const double BaseVoltage = 0.667;   // 기준 전압 (V) - 1×10^-9 mbar에 해당
        private const double BasePressure = 1e-9;   // 기준 압력 (mbar)

        /// <summary>
        /// PTR 225 게이지의 출력 전압을 압력(mbar)으로 변환
        /// </summary>
        /// <param name="voltage">게이지 출력 전압 (V)</param>
        /// <returns>변환된 압력 (mbar)</returns>
        public double ConvertVoltageToPressure(double voltage)
        {
            // 게이지가 점화되지 않은 상태 확인
            if (Math.Abs(voltage - NotIgnitedVoltage) < 0.1)
            {
                return 0.0; // 게이지가 점화되지 않은 경우 0을 반환
            }

            // 전압값이 측정 범위를 벗어나면 제한
            if (voltage < BaseVoltage)
                voltage = BaseVoltage;
            else if (voltage > 10.0)
                voltage = 10.0;

            // 로그 스케일 변환
            // P = BasePressure * 10^((V - BaseVoltage) / VoltPerDecade)
            double exponent = (voltage - BaseVoltage) / VoltPerDecade;
            double pressure = BasePressure * Math.Pow(10, exponent);

            return pressure;
        }

        /// <summary>
        /// 게이지 상태 확인
        /// </summary>
        /// <param name="voltage">게이지 출력 전압 (V)</param>
        /// <param name="statusvoltage">상태 출력 신호 (V)</param>
        /// <returns>게이지 상태 문자열</returns>
        public string CheckGaugeStatus(double voltage, double statusvoltage)
        {
            if (statusvoltage < 1.0) // Low 상태 (0V)
            {
                if (Math.Abs(voltage - NotIgnitedVoltage) < 0.1)
                {
                    return "고전압 꺼짐";
                }
                else
                {
                    return "고전압 켜짐, 점화되지 않음 (압력 < 3×10^-9 mbar)";
                }
            }
            else // High 상태 (13.5-32V)
            {
                return "준비됨 (점화됨, 압력 > 3×10^-9 mbar)";
            }
        }
    }
}