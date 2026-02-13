using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 모든 SerialPortChannel을 중앙에서 관리하는 레지스트리입니다.
    /// MultiPortSerialManager의 역할을 대체합니다.
    /// 
    /// 주요 차이점:
    /// - MultiPortSerialManager: 하나의 싱글톤이 모든 포트를 lock 기반으로 관리
    /// - SerialPortChannelManager: 포트별 독립 채널을 생성/관리, 각 채널은 자체 스레드
    /// 
    /// 사용 예:
    /// var manager = SerialPortChannelManager.Instance;
    /// var channel = manager.CreateChannel("COM3", settings);
    /// var commManager = manager.CreateCommunicationManager("COM3", expectedLen, timeout);
    /// var dryPump = new DryPump(commManager);
    /// </summary>
    public class SerialPortChannelManager : IDisposable
    {
        #region 싱글톤

        private static readonly Lazy<SerialPortChannelManager> _instance =
            new Lazy<SerialPortChannelManager>(() => new SerialPortChannelManager());

        /// <summary>
        /// SerialPortChannelManager의 싱글톤 인스턴스
        /// </summary>
        public static SerialPortChannelManager Instance => _instance.Value;

        #endregion

        #region 필드

        /// <summary>
        /// 포트 이름 → 채널 매핑
        /// </summary>
        private readonly ConcurrentDictionary<string, SerialPortChannel> _channels;

        /// <summary>
        /// 포트 이름 → 해당 포트를 사용하는 CommunicationManager 목록
        /// (하나의 포트를 여러 장치가 공유할 수 있음 — 예: TempController COM6)
        /// </summary>
        private readonly ConcurrentDictionary<string, List<ChannelCommunicationManager>> _managers;

        private volatile bool _isDisposed;

        #endregion

        #region 이벤트

        /// <summary>
        /// 어떤 포트든 연결 상태가 변경되면 발생합니다.
        /// MainForm의 ConnectionCheckTimer를 대체할 수 있습니다.
        /// </summary>
        public event EventHandler<PortConnectionChangedEventArgs> PortConnectionChanged;

        #endregion

        #region 속성

        /// <summary>
        /// 현재 등록된 채널의 포트 이름 목록
        /// </summary>
        public IEnumerable<string> RegisteredPorts => _channels.Keys;

        /// <summary>
        /// 현재 연결된 포트 이름 목록
        /// </summary>
        public IEnumerable<string> ConnectedPorts =>
            _channels.Where(kvp => kvp.Value.IsConnected).Select(kvp => kvp.Key);

        #endregion

        #region 생성자

        private SerialPortChannelManager()
        {
            _channels = new ConcurrentDictionary<string, SerialPortChannel>(StringComparer.OrdinalIgnoreCase);
            _managers = new ConcurrentDictionary<string, List<ChannelCommunicationManager>>(StringComparer.OrdinalIgnoreCase);
            _isDisposed = false;
        }

        #endregion

        #region 채널 관리

        /// <summary>
        /// 지정된 포트에 대한 채널을 생성하고 등록합니다.
        /// 이미 해당 포트의 채널이 있으면 기존 채널을 반환합니다.
        /// </summary>
        /// <param name="portName">COM 포트 이름</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>생성된 또는 기존 채널</returns>
        public SerialPortChannel GetOrCreateChannel(string portName, CommunicationSettings settings)
        {
            if (string.IsNullOrEmpty(portName))
                throw new ArgumentNullException(nameof(portName));
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            return _channels.GetOrAdd(portName, name =>
            {
                var channel = new SerialPortChannel(name, settings);
                channel.ConnectionChanged += Channel_ConnectionChanged;
                return channel;
            });
        }

        /// <summary>
        /// 지정된 포트의 채널을 가져옵니다.
        /// </summary>
        /// <param name="portName">COM 포트 이름</param>
        /// <returns>채널 인스턴스. 없으면 null.</returns>
        public SerialPortChannel GetChannel(string portName)
        {
            if (string.IsNullOrEmpty(portName))
                return null;

            _channels.TryGetValue(portName, out var channel);
            return channel;
        }

        /// <summary>
        /// 지정된 포트에 대한 ChannelCommunicationManager를 생성합니다.
        /// 같은 포트에 대해 여러 개를 생성할 수 있습니다 (장치별로 하나씩).
        /// 
        /// 예: COM6에 TempController(addr=1)와 확장모듈(addr=2)이 있으면
        ///     둘 다 같은 채널을 공유하되 각자의 CommunicationManager를 가짐
        /// </summary>
        /// <param name="portName">COM 포트 이름</param>
        /// <param name="settings">통신 설정 (채널이 없으면 생성에 사용)</param>
        /// <param name="defaultExpectedResponseLength">기본 예상 응답 길이</param>
        /// <param name="defaultTimeoutMs">기본 타임아웃 (ms)</param>
        /// <returns>생성된 ChannelCommunicationManager</returns>
        public ChannelCommunicationManager CreateCommunicationManager(
            string portName,
            CommunicationSettings settings,
            int defaultExpectedResponseLength = 0,
            int defaultTimeoutMs = 500)
        {
            var channel = GetOrCreateChannel(portName, settings);

            var manager = new ChannelCommunicationManager(
                channel,
                defaultExpectedResponseLength,
                defaultTimeoutMs);

            // 매니저 목록에 등록 (모니터링/정리용)
            var managerList = _managers.GetOrAdd(portName, _ => new List<ChannelCommunicationManager>());
            lock (managerList)
            {
                managerList.Add(manager);
            }

            return manager;
        }

        /// <summary>
        /// 모든 채널을 열어 통신을 시작합니다.
        /// 애플리케이션 시작 시 호출합니다.
        /// </summary>
        /// <returns>성공한 포트 수</returns>
        public int OpenAll()
        {
            int successCount = 0;
            foreach (var kvp in _channels)
            {
                if (kvp.Value.Open())
                    successCount++;
            }
            return successCount;
        }

        /// <summary>
        /// 지정된 포트의 채널을 엽니다.
        /// </summary>
        public bool Open(string portName)
        {
            var channel = GetChannel(portName);
            return channel?.Open() ?? false;
        }

        /// <summary>
        /// 지정된 포트의 채널을 닫습니다.
        /// </summary>
        public void Close(string portName)
        {
            var channel = GetChannel(portName);
            channel?.Close();
        }

        /// <summary>
        /// 모든 채널을 닫습니다.
        /// </summary>
        public void CloseAll()
        {
            foreach (var kvp in _channels)
            {
                try { kvp.Value.Close(); } catch { }
            }
        }

        /// <summary>
        /// 특정 포트의 연결 상태를 확인합니다.
        /// </summary>
        public bool IsPortConnected(string portName)
        {
            var channel = GetChannel(portName);
            return channel?.IsConnected ?? false;
        }

        /// <summary>
        /// 모든 채널의 상태 요약을 반환합니다 (디버깅/모니터링용).
        /// </summary>
        public Dictionary<string, ChannelStatus> GetAllChannelStatus()
        {
            var result = new Dictionary<string, ChannelStatus>();
            foreach (var kvp in _channels)
            {
                var ch = kvp.Value;
                result[kvp.Key] = new ChannelStatus
                {
                    PortName = ch.PortName,
                    IsConnected = ch.IsConnected,
                    PendingCommands = ch.PendingCommandCount,
                    TotalProcessed = ch.TotalCommandsProcessed,
                    LastSuccessTime = ch.LastSuccessTime,
                    ConsecutiveFailures = ch.ConsecutiveFailures
                };
            }
            return result;
        }

        #endregion

        #region 이벤트 핸들러

        private void Channel_ConnectionChanged(object sender, PortConnectionChangedEventArgs e)
        {
            // 개별 채널의 연결 변경을 상위로 전달
            PortConnectionChanged?.Invoke(this, e);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            foreach (var kvp in _channels)
            {
                try { kvp.Value.Dispose(); } catch { }
            }

            _channels.Clear();
            _managers.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 채널 상태 스냅샷 (모니터링용)
    /// </summary>
    public class ChannelStatus
    {
        public string PortName { get; set; }
        public bool IsConnected { get; set; }
        public int PendingCommands { get; set; }
        public long TotalProcessed { get; set; }
        public DateTime? LastSuccessTime { get; set; }
        public int ConsecutiveFailures { get; set; }
    }
}