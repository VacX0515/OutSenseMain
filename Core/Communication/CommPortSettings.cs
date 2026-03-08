using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 장치별 통신 포트 설정
    /// </summary>
    public class DevicePortConfig
    {
        /// <summary>COM 포트 이름 (예: "COM3")</summary>
        public string PortName { get; set; } = "";

        /// <summary>통신 속도 (bps)</summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>패리티 ("None" / "Even" / "Odd")</summary>
        public string Parity { get; set; } = "None";

        /// <summary>데이터 비트</summary>
        public int DataBits { get; set; } = 8;

        /// <summary>정지 비트 ("One" / "Two")</summary>
        public string StopBits { get; set; } = "One";

        /// <summary>Modbus 슬레이브 주소 (1~247)</summary>
        public int SlaveAddress { get; set; } = 1;

        /// <summary>DevicePortConfig → CommunicationSettings 변환</summary>
        public CommunicationSettings ToCommunicationSettings()
        {
            return new CommunicationSettings
            {
                BaudRate = BaudRate,
                DataBits = DataBits,
                Parity = Enum.Parse<System.IO.Ports.Parity>(Parity),
                StopBits = Enum.Parse<System.IO.Ports.StopBits>(StopBits),
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };
        }
    }

    /// <summary>
    /// 전체 통신 포트 설정 — JSON으로 저장/로드
    /// </summary>
    public class CommPortSettings
    {
        /// <summary>true면 자동 감지를 건너뛰고 수동 설정 사용</summary>
        public bool UseManualSettings { get; set; } = false;

        /// <summary>장치별 포트 설정 (키: 장치 상수명)</summary>
        public Dictionary<string, DevicePortConfig> Devices { get; set; } = new();

        /// <summary>기본 설정 생성 (현재 하드코딩 값 기준)</summary>
        public static CommPortSettings CreateDefault()
        {
            return new CommPortSettings
            {
                UseManualSettings = false,
                Devices = new Dictionary<string, DevicePortConfig>
                {
                    ["TurboPump"] = new DevicePortConfig
                    {
                        BaudRate = 19200, Parity = "Even", DataBits = 8, StopBits = "One", SlaveAddress = 1
                    },
                    ["DryPump"] = new DevicePortConfig
                    {
                        BaudRate = 38400, Parity = "Even", DataBits = 8, StopBits = "One", SlaveAddress = 1
                    },
                    ["BathCirculator"] = new DevicePortConfig
                    {
                        BaudRate = 9600, Parity = "Even", DataBits = 8, StopBits = "One", SlaveAddress = 1
                    },
                    ["TempController"] = new DevicePortConfig
                    {
                        BaudRate = 19200, Parity = "None", DataBits = 8, StopBits = "One", SlaveAddress = 1
                    },
                    ["IOModule"] = new DevicePortConfig
                    {
                        BaudRate = 9600, Parity = "None", DataBits = 8, StopBits = "One", SlaveAddress = 1
                    }
                }
            };
        }

        /// <summary>JSON 파일로 저장</summary>
        public void Save(string path = null)
        {
            try
            {
                path ??= GetDefaultPath();
                string dir = Path.GetDirectoryName(path);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                File.WriteAllText(path, JsonSerializer.Serialize(this, options));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommPortSettings 저장 실패: {ex.Message}");
            }
        }

        /// <summary>JSON 파일에서 로드 (없으면 null 반환)</summary>
        public static CommPortSettings Load(string path = null)
        {
            try
            {
                path ??= GetDefaultPath();
                if (!File.Exists(path))
                    return null;

                string json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<CommPortSettings>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CommPortSettings 로드 실패: {ex.Message}");
                return null;
            }
        }

        private static string GetDefaultPath()
        {
            return Path.Combine(PathSettings.Instance.ConfigPath, "CommPortSettings.json");
        }

        /// <summary>장치 한글 표시명</summary>
        public static string GetDeviceDisplayName(string deviceKey)
        {
            return deviceKey switch
            {
                "TurboPump" => "터보펌프",
                "DryPump" => "드라이펌프",
                "BathCirculator" => "칠러",
                "TempController" => "온도제어기",
                "IOModule" => "I/O 모듈",
                _ => deviceKey
            };
        }
    }
}
