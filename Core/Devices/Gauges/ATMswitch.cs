using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace VacX_OutSense.Core.Devices.Gauges
{
    public class ATMswitch
    {
        // PG-35 모델 게이지의 전압-압력 변환
        // 출력 전압 범위: 1-5V
        // 측정 압력 범위: 0-10kPa
        private const double MinVoltage = 1.0;    // 최소 전압값 (V)
        private const double MaxVoltage = 5.0;    // 최대 전압값 (V)
        private const double MinPressure = 0.0;   // 최소 압력값 (kPa)
        private const double MaxPressure = 100.0;  // 최대 압력값 (kPa)

        public double ConvertVoltageToPressure(double atm_voltage)
        {
            double atm_pressure = 0;

            // 전압값이 범위를 벗어나면 제한
            if (atm_voltage < MinVoltage)
                atm_voltage = MinVoltage;
            else if (atm_voltage > MaxVoltage)
                atm_voltage = MaxVoltage;

            // 선형 변환 적용 (PG-35 모델에 맞춤)
            // 공식: P = (V - Vmin) * (Pmax - Pmin) / (Vmax - Vmin) + Pmin
            atm_pressure = (atm_voltage - MinVoltage) * (MaxPressure - MinPressure) / (MaxVoltage - MinVoltage) + MinPressure;

            return Math.Round(atm_pressure, 1);
        }
    }
}
