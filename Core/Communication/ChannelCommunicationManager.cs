using System;
using System.Threading;
using VacX_OutSense.Core.Communication.Interfaces;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// SerialPortChannel을 ICommunicationManager 인터페이스로 래핑하는 어댑터입니다.
    /// 
    /// 기존 장치 클래스(DeviceBase 파생)는 ICommunicationManager를 통해 통신합니다.
    /// 이 어댑터를 DevicePortAdapter 대신 사용하면 기존 장치 코드를 수정하지 않고도
    /// 새로운 커맨드 큐 아키텍처의 이점을 얻을 수 있습니다.
    /// 
    /// 동작 방식:
    /// - Write() 호출 시 요청 데이터를 버퍼에 저장
    /// - ReadAll() 호출 시 버퍼의 요청을 SerialCommand로 만들어 채널에 전송
    /// - 이로써 기존의 Write → Sleep → ReadAll 패턴이
    ///   채널의 Write → ActiveRead 원자적 트랜잭션으로 변환됨
    /// 
    /// 사용 예:
    /// var channel = new SerialPortChannel("COM3", settings);
    /// var commManager = new ChannelCommunicationManager(channel, expectedResponseLength: 9);
    /// var dryPump = new DryPump(commManager); // 기존 코드 그대로 동작
    /// </summary>
    public class ChannelCommunicationManager : ICommunicationManager
    {
        #region 필드

        private readonly SerialPortChannel _channel;

        /// <summary>
        /// Write() 호출 시 요청 데이터를 임시 저장하는 버퍼.
        /// ReadAll() 호출 시 이 데이터가 SerialCommand의 Request로 사용됩니다.
        /// 장치별로 인스턴스가 분리되므로 스레드 안전합니다.
        /// </summary>
        private byte[] _pendingRequest;

        /// <summary>
        /// Modbus RTU 응답의 기본 예상 길이.
        /// 장치마다 다르며, 프로토콜에 따라 설정합니다.
        /// 0이면 타임아웃까지 수신합니다.
        /// </summary>
        private int _defaultExpectedResponseLength;

        /// <summary>
        /// 기본 응답 대기 타임아웃 (ms)
        /// </summary>
        private int _defaultTimeoutMs;

        #endregion

        #region 이벤트

        /// <summary>
        /// 통신 상태 변경 이벤트 (ICommunicationManager 인터페이스)
        /// SerialPortChannel의 ConnectionChanged 이벤트를 전달합니다.
        /// </summary>
        public event EventHandler<CommunicationStatusEventArgs> StatusChanged;

        /// <summary>
        /// 데이터 수신 이벤트 (ICommunicationManager 인터페이스)
        /// 새 아키텍처에서는 사용되지 않습니다.
        /// </summary>
        public event EventHandler<byte[]> DataReceived;

        #endregion

        #region 속성

        /// <summary>
        /// 연결 상태 — 채널의 연결 상태를 반영합니다.
        /// </summary>
        public bool IsConnected => _channel?.IsConnected ?? false;

        /// <summary>
        /// 연결 ID (포트 이름)
        /// </summary>
        public string ConnectionId => _channel?.PortName;

        /// <summary>
        /// 통신 설정
        /// </summary>
        public CommunicationSettings Settings => _channel?.Settings ?? new CommunicationSettings();

        /// <summary>
        /// 내부 채널에 대한 접근 (고급 사용)
        /// </summary>
        public SerialPortChannel Channel => _channel;

        /// <summary>
        /// 기본 예상 응답 길이를 설정/조회합니다.
        /// 장치별로 프로토콜에 맞게 설정할 수 있습니다.
        /// </summary>
        public int DefaultExpectedResponseLength
        {
            get => _defaultExpectedResponseLength;
            set => _defaultExpectedResponseLength = value;
        }

        /// <summary>
        /// 기본 응답 대기 타임아웃(ms)을 설정/조회합니다.
        /// </summary>
        public int DefaultTimeoutMs
        {
            get => _defaultTimeoutMs;
            set => _defaultTimeoutMs = value;
        }

        #endregion

        #region 생성자

        /// <summary>
        /// ChannelCommunicationManager의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="channel">사용할 SerialPortChannel</param>
        /// <param name="defaultExpectedResponseLength">기본 예상 응답 길이 (0=타임아웃까지 수신)</param>
        /// <param name="defaultTimeoutMs">기본 응답 타임아웃 (ms)</param>
        public ChannelCommunicationManager(
            SerialPortChannel channel,
            int defaultExpectedResponseLength = 0,
            int defaultTimeoutMs = 500)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _defaultExpectedResponseLength = defaultExpectedResponseLength;
            _defaultTimeoutMs = defaultTimeoutMs;

            // 채널의 연결 상태 변경을 ICommunicationManager 이벤트로 전달
            _channel.ConnectionChanged += Channel_ConnectionChanged;
        }

        #endregion

        #region ICommunicationManager 구현

        /// <summary>
        /// 포트에 연결합니다.
        /// 내부적으로 SerialPortChannel.Open()을 호출합니다.
        /// </summary>
        public bool Connect(string connectionId, CommunicationSettings settings)
        {
            // connectionId와 settings는 채널 생성 시 이미 설정되므로 무시
            // (기존 DeviceBase.Connect() 호출 호환성 유지)
            return _channel.Open();
        }

        /// <summary>
        /// 기본 설정으로 포트에 연결합니다.
        /// </summary>
        public bool Connect(string connectionId)
        {
            return _channel.Open();
        }

        /// <summary>
        /// 포트 연결을 해제합니다.
        /// </summary>
        public void Disconnect()
        {
            _channel.Close();
        }

        /// <summary>
        /// 데이터를 전송합니다.
        /// 
        /// ★ 핵심 동작: 실제 전송하지 않고 요청을 버퍼에 저장합니다.
        /// 후속 ReadAll() 호출 시 Write+Read를 하나의 원자적 트랜잭션으로 처리합니다.
        /// 
        /// 기존 장치 코드의 Write → Sleep → ReadAll 패턴이:
        /// Write(버퍼 저장) → Sleep(무시됨) → ReadAll(채널에 원자적 전송+수신)
        /// 으로 자연스럽게 변환됩니다.
        /// </summary>
        public bool Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected || buffer == null || count <= 0)
                return false;

            // 요청 데이터를 버퍼에 저장 (offset/count 반영)
            _pendingRequest = new byte[count];
            Array.Copy(buffer, offset, _pendingRequest, 0, count);
            return true;
        }

        /// <summary>
        /// 데이터를 전송합니다. (오프셋 없는 버전)
        /// </summary>
        public bool Write(byte[] buffer)
        {
            if (!IsConnected || buffer == null || buffer.Length == 0)
                return false;

            _pendingRequest = (byte[])buffer.Clone();
            return true;
        }

        /// <summary>
        /// 모든 가용 데이터를 읽습니다.
        /// 
        /// ★ 핵심 동작: 이전 Write()에서 저장된 요청을 SerialCommand로 만들어
        /// SerialPortChannel의 커맨드 큐에 전송하고, 전용 처리 스레드의 응답을 대기합니다.
        /// 
        /// 전용 처리 스레드에서 실행되므로:
        /// - Write → Read가 원자적 (다른 스레드의 개입 불가)
        /// - Active Response Polling으로 최적 타이밍에 응답 수신
        /// - 연속 실패 시 자동 끊김 감지
        /// </summary>
        /// <returns>수신된 데이터 또는 null</returns>
        public byte[] ReadAll()
        {
            if (!IsConnected)
                return null;

            if (_pendingRequest == null || _pendingRequest.Length == 0)
                return null;

            try
            {
                var command = new SerialCommand(
                    request: _pendingRequest,
                    expectedResponseLength: _defaultExpectedResponseLength,
                    timeoutMs: _defaultTimeoutMs,
                    priority: 10 // 폴링 우선순위
                );

                // 요청 소비 (재사용 방지)
                _pendingRequest = null;

                // 채널의 전용 스레드에서 처리하고 결과 대기
                return _channel.SendAndReceive(command);
            }
            catch (Exception)
            {
                _pendingRequest = null;
                return null;
            }
        }

        /// <summary>
        /// 지정된 길이만큼 데이터를 읽습니다.
        /// expectedResponseLength를 오버라이드하여 정확한 길이를 지정합니다.
        /// </summary>
        public byte[] Read(int count)
        {
            if (!IsConnected)
                return null;

            if (_pendingRequest == null || _pendingRequest.Length == 0)
                return null;

            try
            {
                var command = new SerialCommand(
                    request: _pendingRequest,
                    expectedResponseLength: count, // 명시적 길이
                    timeoutMs: _defaultTimeoutMs,
                    priority: 10
                );

                _pendingRequest = null;
                return _channel.SendAndReceive(command);
            }
            catch (Exception)
            {
                _pendingRequest = null;
                return null;
            }
        }

        /// <summary>
        /// 타임아웃을 설정합니다.
        /// </summary>
        public void SetTimeout(int timeout)
        {
            _defaultTimeoutMs = timeout;
        }

        /// <summary>
        /// 입력 버퍼를 비웁니다.
        /// 
        /// ★ 새 아키텍처에서는 no-op입니다.
        /// 채널의 전용 스레드가 각 명령 실행 전에 자동으로 버퍼를 비웁니다.
        /// 기존 장치 코드의 DiscardInBuffer() 호출이 오류를 일으키지 않도록 합니다.
        /// </summary>
        public void DiscardInBuffer()
        {
            // No-op: 채널이 명령 실행 전에 자동으로 처리
        }

        /// <summary>
        /// 출력 버퍼를 비웁니다. (no-op)
        /// </summary>
        public void DiscardOutBuffer()
        {
            // No-op: 채널이 자동으로 처리
        }

        #endregion

        #region 확장 메서드 (새 코드용)

        /// <summary>
        /// 우선순위가 높은 명령을 전송합니다.
        /// UI에서 즉시 실행해야 하는 명령(펌프 시작/정지 등)에 사용합니다.
        /// </summary>
        /// <param name="request">요청 데이터</param>
        /// <param name="expectedResponseLength">예상 응답 길이</param>
        /// <param name="timeoutMs">타임아웃</param>
        /// <returns>응답 데이터 또는 null</returns>
        public byte[] SendPriorityCommand(byte[] request, int expectedResponseLength = 0, int timeoutMs = 0)
        {
            if (!IsConnected || request == null)
                return null;

            var command = new SerialCommand(
                request: request,
                expectedResponseLength: expectedResponseLength,
                timeoutMs: timeoutMs > 0 ? timeoutMs : _defaultTimeoutMs,
                priority: 0 // 최우선
            );
            command.Description = "Priority Command";

            return _channel.SendAndReceive(command);
        }

        /// <summary>
        /// 우선순위가 높은 명령을 비동기로 전송합니다.
        /// </summary>
        public async System.Threading.Tasks.Task<byte[]> SendPriorityCommandAsync(
            byte[] request, int expectedResponseLength = 0, int timeoutMs = 0)
        {
            if (!IsConnected || request == null)
                return null;

            var command = new SerialCommand(
                request: request,
                expectedResponseLength: expectedResponseLength,
                timeoutMs: timeoutMs > 0 ? timeoutMs : _defaultTimeoutMs,
                priority: 0
            );
            command.Description = "Priority Command (Async)";

            return await _channel.SendAndReceiveAsync(command);
        }

        #endregion

        #region 이벤트 핸들러

        private void Channel_ConnectionChanged(object sender, PortConnectionChangedEventArgs e)
        {
            // SerialPortChannel의 연결 상태 변경을 ICommunicationManager의 StatusChanged로 전달
            // → DeviceBase.CommunicationManager_StatusChanged가 이를 받아 장치 상태 업데이트
            StatusChanged?.Invoke(this, new CommunicationStatusEventArgs(
                e.IsConnected,
                e.Message,
                e.Exception));
        }

        #endregion
    }
}