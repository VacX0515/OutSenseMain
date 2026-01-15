using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Models
{
    /// <summary>
    /// UI 표시용 데이터 모델 (비즈니스 로직과 완전 분리)
    /// </summary>
    public class UIDataSnapshot
    {
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // 압력 데이터
        public double AtmPressure { get; set; }
        public double PiraniPressure { get; set; }
        public double IonPressure { get; set; }
        public string IonGaugeStatus { get; set; } = "";

        // 밸브 상태
        public string GateValveStatus { get; set; } = "";
        public string VentValveStatus { get; set; } = "";
        public string ExhaustValveStatus { get; set; } = "";
        public string IonGaugeHVStatus { get; set; } = "";

        // 펌프 상태
        public PumpUIData DryPump { get; set; } = new PumpUIData();
        public PumpUIData TurboPump { get; set; } = new PumpUIData();

        // 온도 제어
        public TemperatureUIData BathCirculator { get; set; } = new TemperatureUIData();
        public TemperatureControllerUIData TempController { get; set; } = new TemperatureControllerUIData();

        // 연결 상태
        public ConnectionStates Connections { get; set; } = new ConnectionStates();

        // 버튼 활성화 상태
        public ButtonStates ButtonStates { get; set; } = new ButtonStates();
    }

    public class PumpUIData
    {
        public string Status { get; set; } = "";
        public string Speed { get; set; } = "";
        public string Current { get; set; } = "";
        public string Temperature { get; set; } = "";
        public string Warning { get; set; } = "";
        public bool HasWarning { get; set; }
        public bool HasError { get; set; }
    }

    public class TemperatureUIData
    {
        public string Status { get; set; } = "";
        public string CurrentTemp { get; set; } = "";
        public string TargetTemp { get; set; } = "";
        public string Time { get; set; } = "";
        public string Mode { get; set; } = "";
        public bool HasWarning { get; set; }
        public bool HasError { get; set; }
    }

    public class TemperatureControllerUIData
    {
        // 5채널 지원 (메인 2 + 확장 3)
        public ChannelUIData[] Channels { get; set; } = new ChannelUIData[5];

        public TemperatureControllerUIData()
        {
            for (int i = 0; i < 5; i++)
            {
                Channels[i] = new ChannelUIData();
            }
        }
    }

    public class ChannelUIData
    {
        public string PresentValue { get; set; } = "N/A";
        public string SetValue { get; set; } = "N/A";
        public string Status { get; set; } = "";
        public string HeatingMV { get; set; } = "";
        public bool IsRunning { get; set; }
        public bool IsAutoTuning { get; set; }
    }

    public class ConnectionStates
    {
        public bool IOModule { get; set; }
        public bool DryPump { get; set; }
        public bool TurboPump { get; set; }
        public bool BathCirculator { get; set; }
        public bool TempController { get; set; }
    }

    public class ButtonStates
    {
        // 밸브 버튼
        public bool GateValveEnabled { get; set; } = true;
        public bool VentValveEnabled { get; set; } = true;
        public bool ExhaustValveEnabled { get; set; } = true;
        public bool IonGaugeEnabled { get; set; } = true;

        // 드라이펌프 버튼
        public bool DryPumpStartEnabled { get; set; }
        public bool DryPumpStopEnabled { get; set; }
        public bool DryPumpStandbyEnabled { get; set; }
        public bool DryPumpNormalEnabled { get; set; }

        // 터보펌프 버튼
        public bool TurboPumpStartEnabled { get; set; }
        public bool TurboPumpStopEnabled { get; set; }
        public bool TurboPumpVentEnabled { get; set; }
        public bool TurboPumpResetEnabled { get; set; }

        // 칠러 버튼
        public bool BathCirculatorStartEnabled { get; set; }
        public bool BathCirculatorStopEnabled { get; set; }

        // 온도컨트롤러 버튼 (5채널: 메인 2 + 확장 3)
        public bool[] TempControllerStartEnabled { get; set; } = new bool[5];
        public bool[] TempControllerStopEnabled { get; set; } = new bool[5];
    }
}