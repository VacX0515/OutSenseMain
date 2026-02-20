using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.Devices.Gauges
{
    public class IonGauge
    {
        // PTR 225 Penning Transmitter 게이지의 전압-압력 변환
        // 출력 전압 범위: 0.66V - 10.0V (로그 스케일, 1.333V/decade)
        // 측정 압력 범위: 1×10^-9 - 1×10^-2 mbar

        public double PressureInTorr { get; set; }
        public string Status { get; set; }

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
        public double ConvertVoltageToPressureInTorr(double voltage)
        {
            // 게이지가 점화되지 않은 상태 확인
            if (Math.Abs(voltage - NotIgnitedVoltage) < 0.1)
            {
                return 0.0; // 게이지가 점화되지 않은 경우 0을 반환
            }

            // 전압값이 측정 범위를 벗어나면 제한
            if (voltage < BaseVoltage)
                return 0.0;
            else if (voltage > 10.0)
                voltage = 10.0;

            // 로그 스케일 변환
            // P = BasePressure * 10^((V - BaseVoltage) / VoltPerDecade)
            double exponent = (voltage - BaseVoltage) / VoltPerDecade;
            PressureInTorr = PressureConverter.ToTorr(BasePressure * Math.Pow(10, exponent), PressureConverter.PressureUnit.mbar);

            return PressureInTorr;
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
                Status = "작동 대기 중";
            }
            else // High 상태 (13.5-32V)
            {
                Status = "정상 작동 중";
            }

            return Status;
        }

        public enum GaugeState
        {
            HvOff,             // HV 꺼짐 (대기)
            HvOnNotIgnited,    // HV ON, 방전 미점화
            Measuring,         // 정상 측정 중
            BelowRange,        // 측정 하한 미만 (< 3×10⁻⁹ mbar)
            SensorFault        // 센서 이상
        }

        public GaugeState State { get; private set; }

        /// <summary>
        /// 게이지 상태 판별 (Pin 3 측정 신호 + Pin 6 상태 출력 기반)
        /// </summary>
        /// <param name="signalVoltage">측정 신호 전압 Pin 3 (V)</param>
        /// <param name="statusVoltage">상태 출력 전압 Pin 6 (V)</param>
        /// <returns>판별된 게이지 상태</returns>
        public GaugeState DetermineGaugeState(double signalVoltage, double statusVoltage)
        {
            bool isStatusHigh = statusVoltage > 10.0;
            bool isNotIgnited = Math.Abs(signalVoltage - NotIgnitedVoltage) < 0.1;
            bool isInMeasureRange = signalVoltage >= BaseVoltage && signalVoltage <= 10.0;

            if (isStatusHigh && signalVoltage > 10.0)
            {
                // 문서 Troubleshooting: "measurement signal always > 10V" → 센서 단락
                State = GaugeState.SensorFault;
                Status = "센서 이상 – 세척 필요 (출력 과다)";
            }
            else if (isStatusHigh && isInMeasureRange)
            {
                State = GaugeState.Measuring;
                Status = $"정상 측정 중 ({signalVoltage:F2}V)";
            }
            else if (isStatusHigh && signalVoltage < BaseVoltage)
            {
                // 문서: p < 3×10⁻⁹ 이하에서도 status HIGH 유지
                State = GaugeState.BelowRange;
                Status = "측정 하한 미만 (< 3×10⁻⁹ mbar)";
            }
            else if (!isStatusHigh && isNotIgnited)
            {
                // 상태 Low + 0.4V → HV ON이지만 방전 미점화
                State = GaugeState.HvOnNotIgnited;
                Status = "HV ON – 방전 미점화";
            }
            else if (!isStatusHigh && signalVoltage < 0.1)
            {
                // 상태 Low + 신호 거의 없음 → HV OFF 대기
                State = GaugeState.HvOff;
                Status = "HV OFF – 대기 중";
            }
            else
            {
                State = GaugeState.SensorFault;
                Status = $"상태 불명 (신호: {signalVoltage:F2}V, 상태: {statusVoltage:F2}V)";
            }

            return State;
        }
    }
}