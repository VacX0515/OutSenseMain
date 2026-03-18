using System;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.Devices.Gauges
{
    /// <summary>
    /// 이온게이지 모델
    /// </summary>
    public enum IonGaugeModel
    {
        /// <summary>PTR 225 Penning Transmitter (0.66–10V, 1.333V/decade)</summary>
        PTR225,

        /// <summary>PTR 90 Penning Transmitter (P = 10^(1.667×U − 11.33) mbar)</summary>
        PTR90
    }

    public class IonGauge
    {
        public double PressureInTorr { get; set; }
        public string Status { get; set; }

        /// <summary>
        /// 현재 사용 중인 게이지 모델
        /// </summary>
        public IonGaugeModel Model { get; set; } = IonGaugeModel.PTR225;

        #region PTR 225 파라미터

        // PTR 225: P = 1e-9 * 10^((V - 0.667) / 1.333) mbar
        // 미점화 시 Pin 3 출력 = 0.4V
        private const double PTR225_NotIgnitedVoltage = 0.4;
        private const double PTR225_VoltPerDecade = 1.333;
        private const double PTR225_BaseVoltage = 0.667;
        private const double PTR225_BasePressure = 1e-9; // mbar
        private const double PTR225_MinVoltage = 0.667;
        private const double PTR225_MaxVoltage = 10.0;

        #endregion

        #region PTR 90 파라미터

        // PTR 90: P = 10^(1.667 × U − 11.33) mbar
        // 에러 조건: <0.5V = 전원 없음, >9.5V = Pirani 필라멘트 단선
        private const double PTR90_Coefficient = 1.667;
        private const double PTR90_Offset = -11.33;
        private const double PTR90_MinVoltage = 1.82;   // 5e-9 mbar (매뉴얼 기준)
        private const double PTR90_MaxVoltage = 8.6;    // 1000 mbar (매뉴얼 기준)
        private const double PTR90_ErrorLowVoltage = 0.5;
        private const double PTR90_ErrorHighVoltage = 9.5;

        #endregion

        /// <summary>
        /// 게이지 출력 전압을 압력(Torr)으로 변환
        /// </summary>
        public double ConvertVoltageToPressureInTorr(double voltage)
        {
            switch (Model)
            {
                case IonGaugeModel.PTR90:
                    return ConvertPTR90(voltage);

                case IonGaugeModel.PTR225:
                default:
                    return ConvertPTR225(voltage);
            }
        }

        private double ConvertPTR225(double voltage)
        {
            // PTR 225 전용: 미점화 시 0.4V 출력
            if (Math.Abs(voltage - PTR225_NotIgnitedVoltage) < 0.1)
                return 0.0;

            if (voltage < PTR225_MinVoltage)
                return 0.0;
            if (voltage > PTR225_MaxVoltage)
                voltage = PTR225_MaxVoltage;

            // P = BasePressure * 10^((V - BaseVoltage) / VoltPerDecade)
            double exponent = (voltage - PTR225_BaseVoltage) / PTR225_VoltPerDecade;
            double pressureMbar = PTR225_BasePressure * Math.Pow(10, exponent);
            PressureInTorr = PressureConverter.ToTorr(pressureMbar, PressureConverter.PressureUnit.mbar);
            return PressureInTorr;
        }

        private double ConvertPTR90(double voltage)
        {
            // PTR 90 에러 조건
            if (voltage < PTR90_ErrorLowVoltage)
                return 0.0; // 전원 없음 또는 에러
            if (voltage > PTR90_ErrorHighVoltage)
                return 0.0; // Pirani 필라멘트 단선

            // 측정 범위 밖 (underrange)
            if (voltage < PTR90_MinVoltage)
                return 0.0;
            if (voltage > PTR90_MaxVoltage)
                voltage = PTR90_MaxVoltage;

            // P = 10^(1.667 × U − 11.33) mbar
            double exponent = PTR90_Coefficient * voltage + PTR90_Offset;
            double pressureMbar = Math.Pow(10, exponent);
            PressureInTorr = PressureConverter.ToTorr(pressureMbar, PressureConverter.PressureUnit.mbar);
            return PressureInTorr;
        }

        public enum GaugeState
        {
            HvOff,             // PTR 225: HV 꺼짐 (대기)
            HvOnNotIgnited,    // PTR 225: HV ON, 방전 미점화
            Measuring,         // 정상 측정 중
            PiraniOnly,        // PTR 90: Pirani 단독 모드 (Cold Cathode 미점화 또는 p > 1e-2)
            BelowRange,        // 측정 하한 미만
            SensorFault        // 센서 이상
        }

        public GaugeState State { get; private set; }

        /// <summary>
        /// 게이지 상태 판별 (Pin 3 측정 신호 + Pin 6 상태 출력 기반)
        /// </summary>
        public GaugeState DetermineGaugeState(double signalVoltage, bool isStatusHigh)
        {
            switch (Model)
            {
                case IonGaugeModel.PTR90:
                    return DetermineStatePTR90(signalVoltage, isStatusHigh);

                case IonGaugeModel.PTR225:
                default:
                    return DetermineStatePTR225(signalVoltage, isStatusHigh);
            }
        }

        /// <summary>
        /// PTR 225 상태 판별
        /// - Pin 6 HIGH: 점화 성공 (p > 3e-9 mbar)
        /// - Pin 6 LOW:  HV OFF 또는 미점화 또는 p &lt; 3e-9
        /// - Pin 3 = 0.4V: 미점화 상태
        /// - Pin 3 > 10V: 센서 오염/단락
        /// </summary>
        private GaugeState DetermineStatePTR225(double signalVoltage, bool isStatusHigh)
        {
            bool isNotIgnited = Math.Abs(signalVoltage - PTR225_NotIgnitedVoltage) < 0.1;
            bool isInRange = signalVoltage >= PTR225_MinVoltage && signalVoltage <= PTR225_MaxVoltage;

            // STATUS HIGH + 신호 과다 → 센서 오염 (short circuit in sensing cell)
            if (isStatusHigh && signalVoltage > PTR225_MaxVoltage)
            {
                State = GaugeState.SensorFault;
                Status = "센서 이상 – 세척 필요 (출력 과다)";
            }
            // STATUS HIGH + 정상 범위 → 측정 중
            else if (isStatusHigh && isInRange)
            {
                State = GaugeState.Measuring;
                Status = $"정상 측정 중 ({signalVoltage:F2}V)";
            }
            // STATUS HIGH + 하한 미만 → 측정 범위 아래
            else if (isStatusHigh && signalVoltage < PTR225_MinVoltage)
            {
                State = GaugeState.BelowRange;
                Status = "측정 하한 미만";
            }
            // STATUS LOW + 0.4V → HV ON이지만 미점화
            else if (!isStatusHigh && isNotIgnited)
            {
                State = GaugeState.HvOnNotIgnited;
                Status = "HV ON – 방전 미점화";
            }
            // STATUS LOW + 신호 거의 없음 → HV OFF
            else if (!isStatusHigh && signalVoltage < 0.1)
            {
                State = GaugeState.HvOff;
                Status = "HV OFF – 대기 중";
            }
            else
            {
                State = GaugeState.SensorFault;
                Status = $"상태 불명 (신호: {signalVoltage:F2}V, 상태DI: {isStatusHigh})";
            }

            return State;
        }

        /// <summary>
        /// PTR 90 상태 판별
        /// - Pin 6 HIGH: Cold Cathode 점화됨 (결합 모드, p &lt; 1e-2 mbar)
        /// - Pin 6 LOW:  Pirani 단독 모드 (p > 1e-2 또는 Cold Cathode 미점화)
        /// - 신호 &lt; 0.5V: 전원 없음 (Error low)
        /// - 신호 > 9.5V: Pirani 필라멘트 단선 (Error high)
        /// </summary>
        private GaugeState DetermineStatePTR90(double signalVoltage, bool isStatusHigh)
        {
            // Error low: 전원 없음
            if (signalVoltage < PTR90_ErrorLowVoltage)
            {
                State = GaugeState.SensorFault;
                Status = "에러 – 전원 없음 또는 신호 이상 (< 0.5V)";
                return State;
            }

            // Error high: Pirani 필라멘트 단선
            if (signalVoltage > PTR90_ErrorHighVoltage)
            {
                State = GaugeState.SensorFault;
                Status = "에러 – Pirani 필라멘트 단선 (> 9.5V)";
                return State;
            }

            bool isInRange = signalVoltage >= PTR90_MinVoltage && signalVoltage <= PTR90_MaxVoltage;

            // STATUS HIGH → Cold Cathode 점화, 결합 측정 모드
            if (isStatusHigh && isInRange)
            {
                State = GaugeState.Measuring;
                Status = $"정상 측정 중 – 결합 모드 ({signalVoltage:F2}V)";
            }
            else if (isStatusHigh && signalVoltage > PTR90_MaxVoltage)
            {
                // STATUS HIGH인데 overrange → 8.6V 초과~9.5V 이하 구간
                State = GaugeState.Measuring;
                Status = $"측정 상한 근접 ({signalVoltage:F2}V)";
            }
            // STATUS LOW → Pirani 단독 모드 (정상 동작일 수 있음)
            else if (!isStatusHigh && isInRange)
            {
                State = GaugeState.PiraniOnly;
                Status = $"Pirani 단독 모드 ({signalVoltage:F2}V)";
            }
            else if (!isStatusHigh && signalVoltage >= PTR90_ErrorLowVoltage && signalVoltage < PTR90_MinVoltage)
            {
                // 0.5V ~ 1.82V 구간: underrange
                State = GaugeState.BelowRange;
                Status = "Pirani underrange – Cold Cathode 미점화";
            }
            else
            {
                State = GaugeState.SensorFault;
                Status = $"상태 불명 (신호: {signalVoltage:F2}V, 상태DI: {isStatusHigh})";
            }

            return State;
        }

        /// <summary>
        /// 모델 표시 이름
        /// </summary>
        public static string GetModelDisplayName(IonGaugeModel model) => model switch
        {
            IonGaugeModel.PTR225 => "PTR 225",
            IonGaugeModel.PTR90 => "PTR 90",
            _ => model.ToString()
        };
    }
}