using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using VacX_OutSense.Core.Communication.Interfaces;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 여러 시리얼 포트를 동시에 관리할 수 있는 클래스입니다.
    /// 싱글톤 패턴을 사용하여 애플리케이션 전체에서 하나의 인스턴스만 사용합니다.
    /// </summary>
    public class MultiPortSerialManager : ICommunicationManager
    {
        #region 싱글톤 구현

        private static readonly Lazy<MultiPortSerialManager> _instance = new Lazy<MultiPortSerialManager>(() => new MultiPortSerialManager());

        /// <summary>
        /// MultiPortSerialManager의 싱글톤 인스턴스를 가져옵니다.
        /// </summary>
        public static MultiPortSerialManager Instance => _instance.Value;

        #endregion

        #region 필드 및 속성

        // 포트별 세마포어 추가
        private Dictionary<string, SemaphoreSlim> _portSemaphores = new Dictionary<string, SemaphoreSlim>();

        // 각 포트별 정보를 저장하는 클래스
        private class PortInfo
        {
            public SerialPort Port { get; set; }
            public CommunicationSettings Settings { get; set; }
            public bool IsConnected { get; set; }
            public string PortName { get; set; }
            public readonly object LockObject = new object();
            public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

        }

        // 포트 이름으로 PortInfo를 관리하는 딕셔너리
        private readonly Dictionary<string, PortInfo> _portDictionary = new Dictionary<string, PortInfo>();
        private readonly object _dictionaryLock = new object();

        // 응답 수신 관련 필드
        private readonly int _maxReadRetries = 10;
        private readonly int _readRetryDelayMs = 20;

        /// <summary>
        /// 통신 상태 변경 이벤트
        /// </summary>
        public event EventHandler<CommunicationStatusEventArgs> StatusChanged;

        /// <summary>
        /// 데이터 수신 이벤트 - 포트 이름과 데이터가 함께 전달됨
        /// </summary>
        public event EventHandler<PortDataReceivedEventArgs> PortDataReceived;

        /// <summary>
        /// 데이터 수신 이벤트 - ICommunicationManager 인터페이스 구현용
        /// </summary>
        public event EventHandler<byte[]> DataReceived;

        // 마지막으로 사용한 포트 (ICommunicationManager 인터페이스 호환성)
        private string _lastUsedPort;

        /// <summary>
        /// 현재 연결된 포트 목록을 가져옵니다.
        /// </summary>
        public IEnumerable<string> ConnectedPorts
        {
            get
            {
                List<string> ports = new List<string>();
                lock (_dictionaryLock)
                {
                    foreach (var kvp in _portDictionary)
                    {
                        if (kvp.Value.IsConnected)
                        {
                            ports.Add(kvp.Key);
                        }
                    }
                }
                return ports;
            }
        }

        /// <summary>
        /// 연결 상태 (마지막으로 사용한 포트 기준)
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (string.IsNullOrEmpty(_lastUsedPort))
                {
                    return false;
                }

                lock (_dictionaryLock)
                {
                    if (_portDictionary.TryGetValue(_lastUsedPort, out PortInfo portInfo))
                    {
                        lock (portInfo.LockObject)
                        {
                            return portInfo.IsConnected && portInfo.Port != null && portInfo.Port.IsOpen;
                        }
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// 연결 ID (포트 이름) - 마지막으로 사용한 포트
        /// </summary>
        public string ConnectionId => _lastUsedPort;

        /// <summary>
        /// 통신 설정 (마지막으로 사용한 포트 기준)
        /// </summary>
        public CommunicationSettings Settings
        {
            get
            {
                if (string.IsNullOrEmpty(_lastUsedPort))
                {
                    return new CommunicationSettings();
                }

                lock (_dictionaryLock)
                {
                    if (_portDictionary.TryGetValue(_lastUsedPort, out PortInfo portInfo))
                    {
                        return portInfo.Settings;
                    }
                }
                return new CommunicationSettings();
            }
        }

        #endregion

        #region 생성자 및 소멸자

        /// <summary>
        /// MultiPortSerialManager 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        private MultiPortSerialManager()
        {
            _lastUsedPort = null;
        }

        /// <summary>
        /// 객체가 가비지 컬렉션될 때 리소스를 해제합니다.
        /// </summary>
        ~MultiPortSerialManager()
        {
            DisconnectAll();
        }

        #endregion

        #region ICommunicationManager 구현 및 확장

        /// <summary>
        /// 특정 시리얼 포트에 연결되어 있는지 확인합니다.
        /// </summary>
        /// <param name="portName">포트 이름 (예: COM1)</param>
        /// <returns>연결 상태</returns>
        public bool IsPortConnected(string portName)
        {
            lock (_dictionaryLock)
            {
                if (_portDictionary.TryGetValue(portName, out PortInfo portInfo))
                {
                    lock (portInfo.LockObject)
                    {
                        return portInfo.IsConnected && portInfo.Port != null && portInfo.Port.IsOpen;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 지정된 시리얼 포트에 연결합니다.
        /// </summary>
        /// <param name="portName">포트 이름 (예: COM1)</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>연결 성공 여부</returns>
        public bool Connect(string portName, CommunicationSettings settings)
        {
            if (string.IsNullOrEmpty(portName))
            {
                OnStatusChanged(portName, new CommunicationStatusEventArgs(false, "포트 이름이 지정되지 않았습니다."));
                return false;
            }

            lock (_dictionaryLock)
            {
                // 이미 연결된 포트인 경우 현재 상태 반환
                if (_portDictionary.TryGetValue(portName, out PortInfo existingPort))
                {
                    lock (existingPort.LockObject)
                    {
                        if (existingPort.IsConnected && existingPort.Port != null && existingPort.Port.IsOpen)
                        {
                            _lastUsedPort = portName;
                            return true;
                        }
                    }
                }

                // 새 포트 정보 생성
                PortInfo portInfo = new PortInfo
                {
                    PortName = portName,
                    Settings = settings,
                    IsConnected = false
                };

                try
                {
                    // 새 SerialPort 인스턴스 생성
                    SerialPort serialPort = new SerialPort
                    {
                        PortName = portName,
                        BaudRate = settings.BaudRate,
                        DataBits = settings.DataBits,
                        Parity = settings.Parity,
                        StopBits = settings.StopBits,
                        Handshake = settings.Handshake,
                        ReadTimeout = settings.ReadTimeout,
                        WriteTimeout = settings.WriteTimeout,
                        ReceivedBytesThreshold = 1,  // 1바이트가 들어오면 이벤트 발생
                        ReadBufferSize = 4096,       // 읽기 버퍼 크기 증가
                        WriteBufferSize = 4096       // 쓰기 버퍼 크기 증가
                    };

                    // 데이터 수신 이벤트 핸들러 설정
                    serialPort.DataReceived += (sender, e) => SerialPort_DataReceived(portName, sender, e);

                    // 포트 열기
                    serialPort.Open();

                    // 포트 정보 저장
                    portInfo.Port = serialPort;
                    portInfo.IsConnected = true;

                    // 딕셔너리에 추가 또는 업데이트
                    _portDictionary[portName] = portInfo;
                    _lastUsedPort = portName;

                    // 잔여 데이터 비우기
                    DiscardInBuffer(portName);
                    DiscardOutBuffer(portName);

                    // 상태 이벤트 발생
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(true, $"포트 {portName}에 연결됨"));

                    return true;
                }
                catch (Exception ex)
                {
                    // 오류 발생 시 리소스 정리
                    if (portInfo.Port != null)
                    {
                        try
                        {
                            portInfo.Port.Dispose();
                            portInfo.Port = null;
                        }
                        catch { /* 무시 */ }
                    }

                    // 상태 이벤트 발생
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(false, $"연결 실패: {ex.Message}", ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// 기본 설정으로 시리얼 포트에 연결합니다.
        /// </summary>
        /// <param name="connectionId">포트 이름 (예: COM1)</param>
        /// <returns>연결 성공 여부</returns>
        public bool Connect(string connectionId)
        {
            return Connect(connectionId, new CommunicationSettings());
        }

        /// <summary>
        /// 지정된 시리얼 포트의 연결을 해제합니다.
        /// </summary>
        /// <param name="portName">포트 이름 (예: COM1)</param>
        public void Disconnect(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return;
            }

            lock (_dictionaryLock)
            {
                if (_portDictionary.TryGetValue(portName, out PortInfo portInfo))
                {
                    lock (portInfo.LockObject)
                    {
                        try
                        {
                            if (portInfo.Port != null)
                            {
                                try
                                {
                                    // 이벤트 핸들러 제거는 내부에서 처리됨

                                    if (portInfo.Port.IsOpen)
                                    {
                                        portInfo.Port.Close();
                                    }

                                    portInfo.Port.Dispose();
                                }
                                catch (Exception ex)
                                {
                                    OnStatusChanged(portName, new CommunicationStatusEventArgs(false, $"연결 해제 오류: {ex.Message}", ex));
                                }
                                finally
                                {
                                    portInfo.Port = null;
                                    portInfo.IsConnected = false;
                                }
                            }
                        }
                        finally
                        {
                            // 상태 이벤트 발생
                            OnStatusChanged(portName, new CommunicationStatusEventArgs(false, $"포트 {portName} 연결 해제됨"));
                        }
                    }
                }
            }

            // 마지막 사용 포트가 해제한 포트인 경우 null로 설정
            if (_lastUsedPort == portName)
            {
                _lastUsedPort = null;
            }
        }

        /// <summary>
        /// 현재 연결된 모든 시리얼 포트의 연결을 해제합니다.
        /// </summary>
        public void DisconnectAll()
        {
            lock (_dictionaryLock)
            {
                foreach (var portName in new List<string>(_portDictionary.Keys))
                {
                    Disconnect(portName);
                }
                _portDictionary.Clear();
            }
            _lastUsedPort = null;
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트 연결 해제
        /// </summary>
        public void Disconnect()
        {
            if (!string.IsNullOrEmpty(_lastUsedPort))
            {
                Disconnect(_lastUsedPort);
                _lastUsedPort = null;
            }
        }

        /// <summary>
        /// 지정된 포트로 데이터를 전송합니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="count">바이트 수</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(string portName, byte[] buffer, int offset, int count)
        {
            if (string.IsNullOrEmpty(portName) || buffer == null || count <= 0 || offset < 0 || offset + count > buffer.Length)
            {
                return false;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return false;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    // 쓰기 전에 출력 버퍼 비우기
                    portInfo.Port.DiscardOutBuffer();

                    // 데이터 전송
                    portInfo.Port.Write(buffer, offset, count);

                    // 모든 데이터가 전송될 때까지 대기
                    portInfo.Port.BaseStream.Flush();

                    _lastUsedPort = portName;
                    return true;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"데이터 전송 오류: {ex.Message}", ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// 지정된 포트로 데이터를 전송합니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(string portName, byte[] buffer)
        {
            if (buffer == null)
            {
                return false;
            }
            return Write(portName, buffer, 0, buffer.Length);
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트로 데이터 전송
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="count">바이트 수</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(byte[] buffer, int offset, int count)
        {
            if (string.IsNullOrEmpty(_lastUsedPort))
            {
                return false;
            }
            return Write(_lastUsedPort, buffer, offset, count);
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트로 데이터 전송
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(byte[] buffer)
        {
            if (string.IsNullOrEmpty(_lastUsedPort))
            {
                return false;
            }
            return Write(_lastUsedPort, buffer);
        }

        /// <summary>
        /// 지정된 포트에서 모든 가용한 데이터를 읽습니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] ReadAll(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return null;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return null;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    // 데이터가 도착할 때까지 여러 번 시도
                    for (int retry = 0; retry < _maxReadRetries; retry++)
                    {
                        int bytesToRead = portInfo.Port.BytesToRead;
                        if (bytesToRead > 0)
                        {
                            byte[] buffer = new byte[bytesToRead];
                            int bytesRead = portInfo.Port.Read(buffer, 0, bytesToRead);

                            if (bytesRead > 0)
                            {
                                if (bytesRead < bytesToRead)
                                {
                                    byte[] result = new byte[bytesRead];
                                    Array.Copy(buffer, result, bytesRead);
                                    _lastUsedPort = portName;
                                    return result;
                                }
                                _lastUsedPort = portName;
                                return buffer;
                            }
                        }

                        // 바로 다시 시도하기 전에 짧게 대기
                        Thread.Sleep(_readRetryDelayMs);
                    }

                    // 최대 시도 횟수 이후에도 데이터가 없으면 null 반환
                    return null;
                }
                catch (TimeoutException)
                {
                    // 타임아웃은 오류로 간주하지 않음
                    return null;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"데이터 읽기 오류: {ex.Message}", ex));
                    return null;
                }
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트에서 모든 데이터 읽기
        /// </summary>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] ReadAll()
        {
            if (string.IsNullOrEmpty(_lastUsedPort))
            {
                return null;
            }
            return ReadAll(_lastUsedPort);
        }

        /// <summary>
        /// 지정된 포트에서 지정된 길이만큼 데이터를 읽습니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="count">읽을 바이트 수</param>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] Read(string portName, int count)
        {
            if (string.IsNullOrEmpty(portName) || count <= 0)
            {
                return null;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return null;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    byte[] buffer = new byte[count];
                    int totalBytesRead = 0;
                    int bytesRead = 0;
                    DateTime startTime = DateTime.Now;
                    int timeout = portInfo.Port.ReadTimeout > 0 ? portInfo.Port.ReadTimeout : 1000;

                    // 요청한 바이트 수를 모두 읽거나 타임아웃까지 반복
                    while (totalBytesRead < count &&
                           (DateTime.Now - startTime).TotalMilliseconds < timeout)
                    {
                        // 가용한 데이터가 있는지 확인
                        if (portInfo.Port.BytesToRead == 0)
                        {
                            // 데이터가 올 때까지 잠시 대기 후 다시 시도
                            Thread.Sleep(_readRetryDelayMs);
                            continue;
                        }

                        int bytesToRead = Math.Min(count - totalBytesRead, portInfo.Port.BytesToRead);
                        bytesRead = portInfo.Port.Read(buffer, totalBytesRead, bytesToRead);
                        totalBytesRead += bytesRead;

                        if (bytesRead == 0)
                        {
                            // 읽기 실패 시 잠시 대기 후 재시도
                            Thread.Sleep(_readRetryDelayMs);
                        }
                    }

                    _lastUsedPort = portName;

                    if (totalBytesRead <= 0)
                    {
                        return null;
                    }

                    if (totalBytesRead < count)
                    {
                        // 요청한 만큼 데이터를 받지 못했지만, 일부 데이터가 있는 경우
                        byte[] result = new byte[totalBytesRead];
                        Array.Copy(buffer, result, totalBytesRead);
                        return result;
                    }

                    return buffer;
                }
                catch (TimeoutException)
                {
                    // 타임아웃은 오류로 간주하지 않음
                    return null;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"데이터 읽기 오류: {ex.Message}", ex));
                    return null;
                }
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트에서 읽기
        /// </summary>
        /// <param name="count">읽을 바이트 수</param>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] Read(int count)
        {
            if (string.IsNullOrEmpty(_lastUsedPort))
            {
                return null;
            }
            return Read(_lastUsedPort, count);
        }

        /// <summary>
        /// 지정된 포트의 시간 초과 값을 설정합니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="timeout">시간 초과 (밀리초)</param>
        /// <returns>설정 성공 여부</returns>
        public bool SetTimeout(string portName, int timeout)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return false;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return false;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    portInfo.Port.ReadTimeout = timeout;
                    portInfo.Port.WriteTimeout = timeout;
                    _lastUsedPort = portName;
                    return true;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"시간 초과 설정 오류: {ex.Message}", ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트의 시간 초과 설정
        /// </summary>
        /// <param name="timeout">시간 초과 (밀리초)</param>
        public void SetTimeout(int timeout)
        {
            if (!string.IsNullOrEmpty(_lastUsedPort))
            {
                SetTimeout(_lastUsedPort, timeout);
            }
        }

        /// <summary>
        /// 지정된 포트의 입력 버퍼를 비웁니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <returns>성공 여부</returns>
        public bool DiscardInBuffer(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return false;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return false;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    portInfo.Port.DiscardInBuffer();
                    _lastUsedPort = portName;
                    return true;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"입력 버퍼 비우기 오류: {ex.Message}", ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트의 입력 버퍼 비우기
        /// </summary>
        public void DiscardInBuffer()
        {
            if (!string.IsNullOrEmpty(_lastUsedPort))
            {
                DiscardInBuffer(_lastUsedPort);
            }
        }

        /// <summary>
        /// 지정된 포트의 출력 버퍼를 비웁니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <returns>성공 여부</returns>
        public bool DiscardOutBuffer(string portName)
        {
            if (string.IsNullOrEmpty(portName))
            {
                return false;
            }

            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || portInfo.Port == null || !portInfo.Port.IsOpen)
                {
                    return false;
                }
            }

            // 포트 정보의 락을 사용
            lock (portInfo.LockObject)
            {
                try
                {
                    portInfo.Port.DiscardOutBuffer();
                    _lastUsedPort = portName;
                    return true;
                }
                catch (Exception ex)
                {
                    OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"출력 버퍼 비우기 오류: {ex.Message}", ex));
                    return false;
                }
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 구현 - 마지막으로 사용한 포트의 출력 버퍼 비우기
        /// </summary>
        public void DiscardOutBuffer()
        {
            if (!string.IsNullOrEmpty(_lastUsedPort))
            {
                DiscardOutBuffer(_lastUsedPort);
            }
        }

        #endregion

        #region 이벤트 핸들러

        /// <summary>
        /// 시리얼 포트 데이터 수신 이벤트 핸들러
        /// </summary>
        private void SerialPort_DataReceived(string portName, object sender, SerialDataReceivedEventArgs e)
        {
            PortInfo portInfo = null;
            lock (_dictionaryLock)
            {
                if (!_portDictionary.TryGetValue(portName, out portInfo) || !portInfo.IsConnected || portInfo.Port == null)
                {
                    return;
                }
            }

            try
            {
                // 모든 데이터가 도착할 때까지 대기
                // 더 긴 시간 대기하여 패킷 전체가 도착할 확률을 높임
                Thread.Sleep(50);

                // 데이터를 읽어 이벤트로 전달
                byte[] data = ReadAll(portName);
                if (data != null && data.Length > 0)
                {
                    // 포트별 이벤트 발생
                    OnPortDataReceived(portName, data);

                    // 마지막 사용 포트를 현재 포트로 설정
                    _lastUsedPort = portName;

                    // ICommunicationManager 호환성을 위한 이벤트
                    OnDataReceived(data);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged(portName, new CommunicationStatusEventArgs(portInfo.IsConnected, $"데이터 수신 오류: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// 통신 상태 변경 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="e">이벤트 인자</param>
        private void OnStatusChanged(string portName, CommunicationStatusEventArgs e)
        {
            // 이벤트 복사 (스레드 안전)
            var handler = StatusChanged;
            if (handler != null)
            {
                handler.Invoke(this, e);
            }
        }

        /// <summary>
        /// 특정 포트의 데이터 수신 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="portName">포트 이름</param>
        /// <param name="data">수신된 데이터</param>
        private void OnPortDataReceived(string portName, byte[] data)
        {
            // 이벤트 복사 (스레드 안전)
            var handler = PortDataReceived;
            if (handler != null)
            {
                handler.Invoke(this, new PortDataReceivedEventArgs(portName, data));
            }
        }

        /// <summary>
        /// ICommunicationManager 인터페이스 호환을 위한 데이터 수신 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="data">수신된 데이터</param>
        private void OnDataReceived(byte[] data)
        {
            // 이벤트 복사 (스레드 안전)
            var handler = DataReceived;
            if (handler != null)
            {
                handler.Invoke(this, data);
            }
        }
        #endregion
    }
}