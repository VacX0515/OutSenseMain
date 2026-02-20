using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// COM 포트 자동 감지 서비스
    /// 
    /// 각 장치의 BaudRate + Parity 조합이 모두 유일하므로,
    /// 해당 설정으로 포트를 열고 프로토콜 응답을 확인하는 것만으로 100% 식별 가능합니다.
    /// 
    /// 장치별 고유 설정:
    ///   19200/Even → Turbo Pump  (USS 프로토콜)
    ///   38400/Even → Dry Pump    (Modbus RTU)
    ///   9600/Even  → Bath Circulator (Modbus RTU)
    ///   19200/None → Temp Controller (Modbus RTU)  ← 기존 9600에서 변경
    ///   9600/None  → I/O Module  (Modbus RTU)
    /// </summary>
    public class PortAutoDetectionService
    {
        #region 내부 클래스

        public class DetectedDevice
        {
            public string PortName { get; set; }
            public string DeviceName { get; set; }
            public CommunicationSettings Settings { get; set; }
            public int ExpectedResponseLength { get; set; }
            public int TimeoutMs { get; set; }
        }

        public class DetectionResult
        {
            public bool Success { get; set; }
            public Dictionary<string, DetectedDevice> DeviceMap { get; set; } = new Dictionary<string, DetectedDevice>();
            public List<string> UndetectedDevices { get; set; } = new List<string>();
            public List<string> Messages { get; set; } = new List<string>();

            /// <summary>COM 포트 → 장치 이름 역매핑</summary>
            public Dictionary<string, string> PortToDeviceMap { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public string GetPortName(string deviceName)
                => DeviceMap.TryGetValue(deviceName, out var d) ? d.PortName : null;

            public string GetDeviceName(string portName)
                => PortToDeviceMap.TryGetValue(portName, out var name) ? name : null;
        }

        #endregion

        #region 상수

        private const int DETECT_TIMEOUT_MS = 500;
        private const int RESPONSE_WAIT_MS = 300;
        private const int PORT_SETTLE_MS = 100;

        public const string DEVICE_TURBO_PUMP = "TurboPump";
        public const string DEVICE_DRY_PUMP = "DryPump";
        public const string DEVICE_BATH_CIRCULATOR = "BathCirculator";
        public const string DEVICE_TEMP_CONTROLLER = "TempController";
        public const string DEVICE_IO_MODULE = "IOModule";

        #endregion

        #region 이벤트

        public event EventHandler<string> ProgressUpdated;

        #endregion

        #region 감지 정의

        /// <summary>
        /// 장치별 감지 정보 (순서대로 실행)
        /// </summary>
        private class DeviceProbe
        {
            public string DeviceName { get; set; }
            public CommunicationSettings Settings { get; set; }
            public Func<byte[]> CreateTestPacket { get; set; }
            public Func<byte[], bool> ValidateResponse { get; set; }
            public int ExpectedResponseLength { get; set; }
            public int ChannelTimeoutMs { get; set; }

            /// <summary>
            /// 1차 패킷 실패 시 시도할 fallback 패킷/검증 쌍 (선택)
            /// </summary>
            public Func<byte[]> CreateFallbackPacket { get; set; }
            public Func<byte[], bool> ValidateFallbackResponse { get; set; }
        }

        private List<DeviceProbe> BuildProbeList()
        {
            return new List<DeviceProbe>
            {
                // 1. Turbo Pump: 19200/Even, USS
                new DeviceProbe
                {
                    DeviceName = DEVICE_TURBO_PUMP,
                    Settings = new CommunicationSettings
                    {
                        BaudRate = 19200, DataBits = 8, Parity = Parity.Even,
                        StopBits = StopBits.One, ReadTimeout = 2000, WriteTimeout = 2000
                    },
                    CreateTestPacket = CreateUssTelegram,
                    ValidateResponse = ValidateUssResponse,
                    ExpectedResponseLength = 14,
                    ChannelTimeoutMs = 500
                },

                // 2. Dry Pump: 38400/Even, Modbus RTU
                new DeviceProbe
                {
                    DeviceName = DEVICE_DRY_PUMP,
                    Settings = new CommunicationSettings
                    {
                        BaudRate = 38400, DataBits = 8, Parity = Parity.Even,
                        StopBits = StopBits.One, ReadTimeout = 2000, WriteTimeout = 2000
                    },
                    CreateTestPacket = () => CreateModbusReadRequest(0x01, 0x0000, 1),
                    ValidateResponse = resp => IsValidModbusResponse(resp, 0x01),
                    ExpectedResponseLength = 0,
                    ChannelTimeoutMs = 500
                },

                // 3. Bath Circulator: 9600/Even, Modbus RTU
                new DeviceProbe
                {
                    DeviceName = DEVICE_BATH_CIRCULATOR,
                    Settings = new CommunicationSettings
                    {
                        BaudRate = 9600, DataBits = 8, Parity = Parity.Even,
                        StopBits = StopBits.One, ReadTimeout = 2000, WriteTimeout = 2000
                    },
                    CreateTestPacket = () => CreateModbusReadRequest(0x01, 0x0001, 1),
                    ValidateResponse = resp => IsValidModbusResponse(resp, 0x01),
                    ExpectedResponseLength = 0,
                    ChannelTimeoutMs = 500
                },

                // 4. Temp Controller: 19200/None, Modbus RTU  ★ 변경됨
                new DeviceProbe
                {
                    DeviceName = DEVICE_TEMP_CONTROLLER,
                    Settings = new CommunicationSettings
                    {
                        BaudRate = 19200, DataBits = 8, Parity = Parity.None,
                        StopBits = StopBits.One, ReadTimeout = 2000, WriteTimeout = 2000
                    },
                    CreateTestPacket = () => CreateModbusReadRequest(0x01, 0x03E8, 2),
                    ValidateResponse = resp => IsValidModbusResponse(resp, 0x01),
                    ExpectedResponseLength = 0,
                    ChannelTimeoutMs = 500
                },

                // 5. I/O Module: 9600/None, Modbus RTU (M31-XAXA0404G)
                //    Function 03 (Holding Registers: AO 값) 또는 Function 04 (Input Registers: AI 값)
                new DeviceProbe
                {
                    DeviceName = DEVICE_IO_MODULE,
                    Settings = new CommunicationSettings
                    {
                        BaudRate = 9600, DataBits = 8, Parity = Parity.None,
                        StopBits = StopBits.One, ReadTimeout = 2000, WriteTimeout = 2000
                    },
                    CreateTestPacket = () => CreateModbusReadRequest(0x01, 0x0000, 4),
                    ValidateResponse = resp => IsValidModbusResponse(resp, 0x01),
                    // Holding 실패 시 Input Registers 시도
                    CreateFallbackPacket = () => CreateModbusReadInputRequest(0x01, 0x0000, 4),
                    ValidateFallbackResponse = resp => IsValidModbusResponse(resp, 0x01),
                    ExpectedResponseLength = 0,
                    ChannelTimeoutMs = 300
                }
            };
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 모든 COM 포트를 스캔하여 장치를 자동 감지합니다.
        /// 각 장치의 BaudRate+Parity 조합이 유일하므로 설정값만으로 식별합니다.
        /// </summary>
        public async Task<DetectionResult> DetectAllDevicesAsync(CancellationToken cancellationToken = default)
        {
            var result = new DetectionResult();
            var availablePorts = GetAvailablePorts();

            if (availablePorts.Count == 0)
            {
                result.Success = false;
                result.Messages.Add("사용 가능한 COM 포트가 없습니다.");
                return result;
            }

            ReportProgress($"사용 가능한 포트: {string.Join(", ", availablePorts)}");
            var remainingPorts = new List<string>(availablePorts);
            var probes = BuildProbeList();

            for (int i = 0; i < probes.Count; i++)
            {
                var probe = probes[i];
                cancellationToken.ThrowIfCancellationRequested();

                ReportProgress($"{i + 1}/{probes.Count} {probe.DeviceName} 감지 중 " +
                    $"({probe.Settings.BaudRate}/{probe.Settings.Parity})...");

                string foundPort = await ScanPortsForDeviceAsync(
                    remainingPorts, probe, cancellationToken);

                if (foundPort != null)
                {
                    RegisterDevice(result, foundPort, probe);
                    remainingPorts.Remove(foundPort);
                    ReportProgress($"  → {probe.DeviceName}: {foundPort}");
                }
                else
                {
                    result.UndetectedDevices.Add(probe.DeviceName);
                    ReportProgress($"  → {probe.DeviceName}: 감지 실패 (전원 꺼짐 또는 미연결)");
                }
            }

            // 결과 요약
            result.Success = result.UndetectedDevices.Count == 0;
            result.Messages.Add($"감지 완료: {result.DeviceMap.Count}/{probes.Count} 장치 식별됨");
            if (result.UndetectedDevices.Count > 0)
                result.Messages.Add($"미감지 장치: {string.Join(", ", result.UndetectedDevices)}");

            return result;
        }

        #endregion

        #region 포트 스캔

        /// <summary>
        /// 남은 포트 목록에서 특정 장치를 찾습니다.
        /// </summary>
        private async Task<string> ScanPortsForDeviceAsync(
            List<string> ports, DeviceProbe probe, CancellationToken ct)
        {
            byte[] testPacket = probe.CreateTestPacket();
            byte[] fallbackPacket = probe.CreateFallbackPacket?.Invoke();

            foreach (var port in ports)
            {
                if (ct.IsCancellationRequested) break;

                try
                {
                    // 1차: 기본 패킷
                    byte[] response = await SendAndReceiveAsync(port, probe.Settings, testPacket, ct);
                    if (response != null && probe.ValidateResponse(response))
                        return port;

                    // 2차: fallback 패킷 (있을 경우)
                    if (fallbackPacket != null && probe.ValidateFallbackResponse != null)
                    {
                        response = await SendAndReceiveAsync(port, probe.Settings, fallbackPacket, ct);
                        if (response != null && probe.ValidateFallbackResponse(response))
                            return port;
                    }
                }
                catch
                {
                    // 다음 포트 시도
                }
            }

            return null;
        }

        #endregion

        #region 저수준 통신

        private async Task<byte[]> SendAndReceiveAsync(
            string portName, CommunicationSettings settings, byte[] data, CancellationToken ct)
        {
            return await Task.Run(() =>
            {
                SerialPort port = null;
                try
                {
                    port = new SerialPort
                    {
                        PortName = portName,
                        BaudRate = settings.BaudRate,
                        DataBits = settings.DataBits,
                        Parity = settings.Parity,
                        StopBits = settings.StopBits,
                        Handshake = Handshake.None,
                        ReadTimeout = DETECT_TIMEOUT_MS,
                        WriteTimeout = DETECT_TIMEOUT_MS,
                        ReadBufferSize = 4096,
                        WriteBufferSize = 4096
                    };

                    port.Open();
                    Thread.Sleep(PORT_SETTLE_MS);
                    port.DiscardInBuffer();
                    port.DiscardOutBuffer();

                    port.Write(data, 0, data.Length);
                    Thread.Sleep(RESPONSE_WAIT_MS);

                    int bytesAvailable = port.BytesToRead;
                    if (bytesAvailable > 0)
                    {
                        byte[] response = new byte[bytesAvailable];
                        port.Read(response, 0, bytesAvailable);
                        return response;
                    }
                    return null;
                }
                finally
                {
                    if (port != null && port.IsOpen)
                    {
                        try { port.Close(); } catch { }
                    }
                    port?.Dispose();
                }
            }, ct);
        }

        #endregion

        #region 프로토콜 검증

        /// <summary>USS 텔레그램 생성 (Turbo Pump 상태 읽기)</summary>
        private byte[] CreateUssTelegram()
        {
            byte[] t = new byte[24];
            t[0] = 0x02; // STX
            t[1] = 0x16; // LGE
            t[2] = 0x00; // ADR
            // 3~22: 0x00 (PKE=NoAccess, PZD 모두 0)
            byte bcc = t[0];
            for (int i = 1; i < 23; i++) bcc ^= t[i];
            t[23] = bcc;
            return t;
        }

        /// <summary>USS 응답 검증: 24바이트, STX=0x02, BCC 일치</summary>
        private bool ValidateUssResponse(byte[] resp)
        {
            if (resp == null || resp.Length < 24 || resp[0] != 0x02) return false;
            byte bcc = resp[0];
            for (int i = 1; i < 23; i++) bcc ^= resp[i];
            return bcc == resp[23];
        }

        /// <summary>Modbus RTU Read Holding Registers (FC 03)</summary>
        private byte[] CreateModbusReadRequest(byte slaveId, ushort addr, ushort qty)
        {
            byte[] r = new byte[8];
            r[0] = slaveId; r[1] = 0x03;
            r[2] = (byte)(addr >> 8); r[3] = (byte)(addr & 0xFF);
            r[4] = (byte)(qty >> 8); r[5] = (byte)(qty & 0xFF);
            ushort crc = CalcCRC(r, 6);
            r[6] = (byte)(crc & 0xFF); r[7] = (byte)(crc >> 8);
            return r;
        }

        /// <summary>Modbus RTU Read Input Registers (FC 04)</summary>
        private byte[] CreateModbusReadInputRequest(byte slaveId, ushort addr, ushort qty)
        {
            byte[] r = new byte[8];
            r[0] = slaveId; r[1] = 0x04;
            r[2] = (byte)(addr >> 8); r[3] = (byte)(addr & 0xFF);
            r[4] = (byte)(qty >> 8); r[5] = (byte)(qty & 0xFF);
            ushort crc = CalcCRC(r, 6);
            r[6] = (byte)(crc & 0xFF); r[7] = (byte)(crc >> 8);
            return r;
        }

        /// <summary>Modbus 응답 검증 (정상 응답 또는 Exception 응답)</summary>
        private bool IsValidModbusResponse(byte[] resp, byte expectedSlaveId)
        {
            if (resp == null || resp.Length < 5) return false;
            if (resp[0] != expectedSlaveId) return false;

            byte fc = resp[1];
            if (fc == 0x03 || fc == 0x04)
            {
                int dataLen = resp[2];
                int expLen = 3 + dataLen + 2;
                if (resp.Length >= expLen)
                {
                    ushort rxCrc = (ushort)(resp[expLen - 2] | (resp[expLen - 1] << 8));
                    return rxCrc == CalcCRC(resp, expLen - 2);
                }
                return true; // 길이 부족해도 FC 맞으면 장치 존재
            }
            return (fc == 0x83 || fc == 0x84); // Exception = 장치 존재
        }

        private ushort CalcCRC(byte[] buf, int len)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < len; i++)
            {
                crc ^= buf[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0) { crc >>= 1; crc ^= 0xA001; }
                    else crc >>= 1;
                }
            }
            return crc;
        }

        #endregion

        #region 유틸리티

        private List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames()
                .OrderBy(p => int.TryParse(p.Replace("COM", ""), out int n) ? n : 999)
                .ToList();
        }

        private void RegisterDevice(DetectionResult result, string portName, DeviceProbe probe)
        {
            result.DeviceMap[probe.DeviceName] = new DetectedDevice
            {
                PortName = portName,
                DeviceName = probe.DeviceName,
                Settings = probe.Settings,
                ExpectedResponseLength = probe.ExpectedResponseLength,
                TimeoutMs = probe.ChannelTimeoutMs
            };
            result.PortToDeviceMap[portName.ToUpper()] = probe.DeviceName;
        }

        private void ReportProgress(string msg) => ProgressUpdated?.Invoke(this, msg);

        #endregion
    }
}