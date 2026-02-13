using System.Text;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// 현재 시스템 상태 평가 결과.
    /// AutoRunService.DetectCurrentSystemState()가 반환하며,
    /// 어느 단계부터 시작할 수 있는지 결정합니다.
    /// </summary>
    public class SystemStateAssessment
    {
        // 각 조건 충족 여부
        public bool AllDevicesConnected { get; set; }
        public bool ValvesReady { get; set; }           // GV=Opened, VV/EV=Closed
        public bool DryPumpRunning { get; set; }
        public bool TurboPumpAtSpeed { get; set; }
        public bool IonGaugeActive { get; set; }
        public bool HighVacuumReached { get; set; }
        public bool HeaterRunning { get; set; }

        // 측정값
        public double CurrentPressure { get; set; }
        public int TurboPumpSpeed { get; set; }
        public string GateValveStatus { get; set; } = "Unknown";
        public string VentValveStatus { get; set; } = "Unknown";
        public string ExhaustValveStatus { get; set; } = "Unknown";

        /// <summary>
        /// 현재 상태에서 시작 가능한 단계 (1~8)
        /// </summary>
        public int RecommendedStartStep { get; set; } = 1;

        /// <summary>
        /// 사용자에게 보여줄 상태 요약 텍스트
        /// </summary>
        public string GetSummaryText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("══ 현재 시스템 상태 ══");
            sb.AppendLine();
            sb.AppendLine($"  장치 연결:      {(AllDevicesConnected ? "✓ 전체 연결됨" : "✗ 일부 미연결")}");
            sb.AppendLine($"  밸브 상태:      GV={GateValveStatus}, VV={VentValveStatus}, EV={ExhaustValveStatus}");
            sb.AppendLine($"  드라이펌프:     {(DryPumpRunning ? "✓ 운전 중" : "✗ 정지")}");
            sb.AppendLine($"  터보펌프:       {(TurboPumpAtSpeed ? $"✓ 정격 ({TurboPumpSpeed} RPM)" : $"✗ ({TurboPumpSpeed} RPM)")}");
            sb.AppendLine($"  이온게이지:     {(IonGaugeActive ? "✓ HV ON" : "✗ HV OFF")}");
            sb.AppendLine($"  현재 압력:      {CurrentPressure:E2} Torr");
            sb.AppendLine($"  고진공:         {(HighVacuumReached ? "✓ 도달" : "✗ 미도달")}");
            sb.AppendLine($"  히터:           {(HeaterRunning ? "✓ 운전 중" : "✗ 정지")}");
            sb.AppendLine();
            sb.AppendLine($"  ▶ 권장 시작 단계: {RecommendedStartStep}단계 ({GetStepName(RecommendedStartStep)})");

            return sb.ToString();
        }

        /// <summary>
        /// 단계 번호 → 이름
        /// </summary>
        public static string GetStepName(int step)
        {
            switch (step)
            {
                case 1: return "초기화";
                case 2: return "진공 준비";
                case 3: return "드라이펌프 시작";
                case 4: return "터보펌프 시작";
                case 5: return "이온게이지 활성화";
                case 6: return "고진공 대기";
                case 7: return "히터 시작";
                case 8: return "실험 진행";
                case 9: return "종료 시퀀스";
                default: return "알 수 없음";
            }
        }
    }
}