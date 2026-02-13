using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 포트 연결 상태 변경 이벤트 인자
    /// </summary>
    public class PortConnectionChangedEventArgs : EventArgs
    {
        public string PortName { get; }
        public bool IsConnected { get; }
        public string Message { get; }
        public Exception Exception { get; }

        public PortConnectionChangedEventArgs(string portName, bool isConnected, string message, Exception ex = null)
        {
            PortName = portName;
            IsConnected = isConnected;
            Message = message;
            Exception = ex;
        }
    }

    /// <summary>
    /// 시리얼 포트별 전용 처리 채널입니다.
    /// 
    /// 각 COM 포트에 대해 하나의 인스턴스가 생성되며:
    /// - 전용 스레드에서 모든 통신을 순차적으로 처리
    /// - Write→Read를 원자적 트랜잭션으로 보장
    /// - 연속 실패 추적을 통한 자동 끊김 감지
    /// - DataReceived 이벤트 미사용 (폴링 충돌 제거)
    /// - Active response polling으로 고정 Sleep 제거
    /// </summary>
    public class SerialPortChannel : IDisposable
    {
        #region 상수

        /// <summary>
        /// 연속 통신 실패 시 끊김으로 판정하는 임계값
        /// </summary>
        private const int DISCONNECT_THRESHOLD = 3;

        /// <summary>
        /// 응답 폴링 시 CPU 과사용 방지를 위한 최소 대기 (ms)
        /// </summary>
        private const int RESPONSE_POLL_INTERVAL_MS = 1;

        /// <summary>
        /// 재연결 시도 간격 (ms)
        /// </summary>
        private const int RECONNECT_INTERVAL_MS = 3000;

        /// <summary>
        /// 큐에서 명령을 꺼내기 위해 대기하는 시간 (ms)
        /// </summary>
        private const int QUEUE_POLL_TIMEOUT_MS = 50;

        #endregion

        #region 필드

        private SerialPort _port;
        private readonly string _portName;
        private CommunicationSettings _settings;

        // 커맨드 큐: BlockingCollection은 스레드 안전 + Take 시 블로킹 지원
        private readonly BlockingCollection<SerialCommand> _commandQueue;

        // 전용 처리 스레드
        private Thread _processingThread;
        private readonly CancellationTokenSource _cts;

        // 연결 상태 추적
        private volatile bool _isConnected;
        private volatile bool _isDisposed;
        private int _consecutiveFailures;

        // 통계 (모니터링용)
        private long _totalCommandsProcessed;
        private long _totalFailures;
        private DateTime? _lastSuccessTime;
        private DateTime? _lastFailureTime;

        #endregion

        #region 이벤트

        /// <summary>
        /// 포트 연결 상태 변경 이벤트.
        /// 끊김 감지, 재연결 성공/실패 시 발생합니다.
        /// </summary>
        public event EventHandler<PortConnectionChangedEventArgs> ConnectionChanged;

        /// <summary>
        /// 통신 오류 발생 이벤트 (개별 명령 실패 시)
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        #endregion

        #region 속성

        /// <summary>
        /// 포트 이름 (예: COM1)
        /// </summary>
        public string PortName => _portName;

        /// <summary>
        /// 현재 연결 상태
        /// </summary>
        public bool IsConnected => _isConnected && !_isDisposed;

        /// <summary>
        /// 현재 큐에 대기 중인 명령 수
        /// </summary>
        public int PendingCommandCount => _commandQueue.Count;

        /// <summary>
        /// 총 처리된 명령 수
        /// </summary>
        public long TotalCommandsProcessed => Interlocked.Read(ref _totalCommandsProcessed);

        /// <summary>
        /// 마지막 성공 통신 시각
        /// </summary>
        public DateTime? LastSuccessTime => _lastSuccessTime;

        /// <summary>
        /// 연속 실패 횟수
        /// </summary>
        public int ConsecutiveFailures => _consecutiveFailures;

        /// <summary>
        /// 통신 설정
        /// </summary>
        public CommunicationSettings Settings => _settings;

        #endregion

        #region 생성자

        /// <summary>
        /// SerialPortChannel의 새 인스턴스를 초기화합니다.
        /// 인스턴스 생성 시에는 포트를 열지 않으며, Open()을 호출해야 합니다.
        /// </summary>
        /// <param name="portName">COM 포트 이름 (예: COM1)</param>
        /// <param name="settings">통신 설정</param>
        public SerialPortChannel(string portName, CommunicationSettings settings)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _commandQueue = new BlockingCollection<SerialCommand>(new ConcurrentQueue<SerialCommand>());
            _cts = new CancellationTokenSource();
            _isConnected = false;
            _isDisposed = false;
            _consecutiveFailures = 0;
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 포트를 열고 처리 스레드를 시작합니다.
        /// </summary>
        /// <returns>성공 여부</returns>
        public bool Open()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SerialPortChannel));

            if (_isConnected)
                return true;

            try
            {
                _port = CreateSerialPort(_portName, _settings);
                _port.Open();

                // ★ DataReceived 이벤트 등록하지 않음 — 폴링 충돌 제거
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();

                _isConnected = true;
                _consecutiveFailures = 0;

                // 전용 처리 스레드 시작
                _processingThread = new Thread(ProcessingLoop)
                {
                    Name = $"SerialChannel_{_portName}",
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal
                };
                _processingThread.Start();

                OnConnectionChanged(true, $"포트 {_portName} 열림");
                return true;
            }
            catch (Exception ex)
            {
                CleanupPort();
                OnConnectionChanged(false, $"포트 {_portName} 열기 실패: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 포트를 닫고 처리 스레드를 종료합니다.
        /// 큐에 남아있는 명령은 취소됩니다.
        /// </summary>
        public void Close()
        {
            if (!_isConnected && _port == null)
                return;

            _isConnected = false;

            // 큐에 남은 명령 모두 취소
            DrainAndCancelQueue();

            // 포트 정리
            CleanupPort();

            OnConnectionChanged(false, $"포트 {_portName} 닫힘");
        }

        /// <summary>
        /// 명령을 큐에 추가하고 응답을 동기적으로 대기합니다.
        /// 현재 스레드가 아닌 전용 처리 스레드에서 실행됩니다.
        /// </summary>
        /// <param name="command">실행할 명령</param>
        /// <returns>응답 데이터. 실패 시 null.</returns>
        public byte[] SendAndReceive(SerialCommand command)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SerialPortChannel));

            if (!_isConnected)
                return null;

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                _commandQueue.Add(command);
                return command.WaitForResponse();
            }
            catch (InvalidOperationException)
            {
                // 큐가 CompleteAdding 상태
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        /// <summary>
        /// 명령을 큐에 추가하고 응답을 비동기로 대기합니다.
        /// </summary>
        /// <param name="command">실행할 명령</param>
        /// <returns>응답 데이터의 Task. 실패 시 null.</returns>
        public async System.Threading.Tasks.Task<byte[]> SendAndReceiveAsync(SerialCommand command)
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(SerialPortChannel));

            if (!_isConnected)
                return null;

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            try
            {
                _commandQueue.Add(command);
                return await command.ResponseTask.ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        #endregion

        #region 전용 처리 스레드

        /// <summary>
        /// 전용 스레드의 메인 루프.
        /// 큐에서 명령을 하나씩 꺼내 순차적으로 처리합니다.
        /// </summary>
        private void ProcessingLoop()
        {
            Debug.WriteLine($"[{_portName}] 처리 스레드 시작");

            while (!_cts.Token.IsCancellationRequested)
            {
                // 끊김 상태면 재연결 시도
                if (!_isConnected)
                {
                    if (!TryReconnect())
                    {
                        Thread.Sleep(RECONNECT_INTERVAL_MS);
                        continue;
                    }
                }

                SerialCommand command = null;
                try
                {
                    // 큐에서 명령 대기 (타임아웃 있음)
                    if (!_commandQueue.TryTake(out command, QUEUE_POLL_TIMEOUT_MS, _cts.Token))
                        continue;

                    // 명령 실행
                    ExecuteCommand(command);
                }
                catch (OperationCanceledException)
                {
                    // 정상 종료
                    command?.ResponseTcs.TrySetCanceled();
                    break;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{_portName}] 처리 루프 예외: {ex.Message}");
                    command?.ResponseTcs.TrySetResult(null);
                }
            }

            Debug.WriteLine($"[{_portName}] 처리 스레드 종료");
        }

        /// <summary>
        /// 단일 명령을 실행합니다.
        /// Write → Active Read를 원자적으로 처리합니다.
        /// </summary>
        private void ExecuteCommand(SerialCommand command)
        {
            try
            {
                if (_port == null || !_port.IsOpen)
                {
                    HandleCommunicationFailure(command, "포트가 열려있지 않음");
                    return;
                }

                // 1. 입력 버퍼 비우기 (이전 잔여 데이터 제거)
                _port.DiscardInBuffer();

                // 2. 요청 전송
                _port.Write(command.Request, 0, command.Request.Length);
                _port.BaseStream.Flush();

                // 3. 응답 수신 (Active Polling)
                byte[] response = ReadResponse(command.ExpectedResponseLength, command.TimeoutMs);

                if (response != null && response.Length > 0)
                {
                    // 성공
                    _consecutiveFailures = 0;
                    _lastSuccessTime = DateTime.Now;
                    Interlocked.Increment(ref _totalCommandsProcessed);
                    command.ResponseTcs.TrySetResult(response);
                }
                else
                {
                    // 응답 없음 (타임아웃)
                    HandleCommunicationFailure(command, "응답 타임아웃");
                }
            }
            catch (IOException ex)
            {
                // 포트 I/O 오류 → 케이블 분리 가능성 높음
                HandleCommunicationFailure(command, $"IOException: {ex.Message}", ex, isCritical: true);
            }
            catch (UnauthorizedAccessException ex)
            {
                // 포트 접근 거부 → 포트가 시스템에서 제거됨
                HandleCommunicationFailure(command, $"포트 접근 불가: {ex.Message}", ex, isCritical: true);
            }
            catch (InvalidOperationException ex)
            {
                // 포트가 닫혀있음
                HandleCommunicationFailure(command, $"포트 닫힘: {ex.Message}", ex, isCritical: true);
            }
            catch (Exception ex)
            {
                HandleCommunicationFailure(command, $"알 수 없는 오류: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Active Response Polling으로 응답을 수신합니다.
        /// 고정 Thread.Sleep 대신 데이터가 도착하면 즉시 반환합니다.
        /// </summary>
        /// <param name="expectedLength">예상 응답 길이 (0이면 가용한 데이터 모두)</param>
        /// <param name="timeoutMs">타임아웃 (밀리초)</param>
        /// <returns>수신된 데이터. 타임아웃이면 null.</returns>
        private byte[] ReadResponse(int expectedLength, int timeoutMs)
        {
            var buffer = new List<byte>();
            var sw = Stopwatch.StartNew();

            // 최소 대기: 장치가 응답을 준비하는 데 필요한 최소 시간
            // 바이트 전송 시간 계산: (예상길이 × 10비트) / baudRate × 1000ms
            // 여기에 장치 처리 시간(~5ms) 추가
            int minWaitMs = Math.Max(5, (_settings.BaudRate > 0)
                ? (int)((expectedLength * 10.0 / _settings.BaudRate) * 1000) + 5
                : 10);

            // 첫 바이트 도착까지 최소 대기
            Thread.Sleep(minWaitMs);

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    int available = _port.BytesToRead;

                    if (available > 0)
                    {
                        var chunk = new byte[available];
                        int bytesRead = _port.Read(chunk, 0, available);

                        if (bytesRead > 0)
                        {
                            if (bytesRead < available)
                            {
                                var trimmed = new byte[bytesRead];
                                Array.Copy(chunk, trimmed, bytesRead);
                                buffer.AddRange(trimmed);
                            }
                            else
                            {
                                buffer.AddRange(chunk);
                            }

                            // 예상 길이에 도달하면 즉시 반환
                            if (expectedLength > 0 && buffer.Count >= expectedLength)
                            {
                                return buffer.ToArray();
                            }

                            // 예상 길이가 없으면 추가 데이터가 더 올 수 있으므로 짧게 대기 후 확인
                            if (expectedLength == 0)
                            {
                                // 추가 데이터 확인을 위해 짧은 대기
                                Thread.Sleep(5);
                                if (_port.BytesToRead == 0)
                                {
                                    // 추가 데이터 없음 → 수신 완료
                                    return buffer.ToArray();
                                }
                                // 추가 데이터 있으면 루프 계속
                            }
                        }
                    }
                    else
                    {
                        // 데이터 없음 → 짧은 대기 후 재시도
                        Thread.Sleep(RESPONSE_POLL_INTERVAL_MS);
                    }
                }
                catch
                {
                    // 읽기 중 예외 발생 → 호출자에게 전파
                    throw;
                }
            }

            // 타임아웃. 부분 데이터가 있으면 반환 (장치가 느린 경우)
            return buffer.Count > 0 ? buffer.ToArray() : null;
        }

        #endregion

        #region 끊김 감지 및 재연결

        /// <summary>
        /// 통신 실패를 처리하고 연속 실패 시 끊김으로 판정합니다.
        /// </summary>
        /// <param name="command">실패한 명령</param>
        /// <param name="message">실패 메시지</param>
        /// <param name="ex">예외 (있으면)</param>
        /// <param name="isCritical">포트 레벨 오류 여부 (IOException 등)</param>
        private void HandleCommunicationFailure(SerialCommand command, string message, Exception ex = null, bool isCritical = false)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.Now;
            Interlocked.Increment(ref _totalFailures);

            OnErrorOccurred($"[{_portName}] 통신 실패 ({_consecutiveFailures}/{DISCONNECT_THRESHOLD}): {message}");

            // 명령에 null 응답 설정 (호출자가 null을 받아 처리)
            command.ResponseTcs.TrySetResult(null);

            // Critical 오류(IOException, UnauthorizedAccess)면 즉시 끊김 판정
            // 일반 오류(타임아웃 등)면 임계값까지 누적
            if (isCritical || _consecutiveFailures >= DISCONNECT_THRESHOLD)
            {
                MarkAsDisconnected($"포트 {_portName} 연결 끊김 감지 " +
                    $"(연속 {_consecutiveFailures}회 실패, 마지막 오류: {message})", ex);
            }
        }

        /// <summary>
        /// 포트를 끊김 상태로 전환합니다.
        /// </summary>
        private void MarkAsDisconnected(string message, Exception ex = null)
        {
            if (!_isConnected)
                return; // 이미 끊김 상태

            _isConnected = false;

            // 큐에 남은 명령 모두 취소 (null 응답)
            DrainAndCancelQueue();

            // 포트 리소스 정리 (재연결 시 새로 생성)
            CleanupPort();

            OnConnectionChanged(false, message, ex);
        }

        /// <summary>
        /// 끊어진 포트에 대해 재연결을 시도합니다.
        /// </summary>
        /// <returns>재연결 성공 여부</returns>
        private bool TryReconnect()
        {
            try
            {
                // 포트가 시스템에 존재하는지 먼저 확인
                string[] availablePorts = SerialPort.GetPortNames();
                if (!availablePorts.Contains(_portName))
                {
                    // 포트가 시스템에 없음 (USB 케이블 뽑힘)
                    return false;
                }

                // 이전 포트 정리
                CleanupPort();

                // 새 포트 인스턴스로 재연결
                _port = CreateSerialPort(_portName, _settings);
                _port.Open();
                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();

                _isConnected = true;
                _consecutiveFailures = 0;

                OnConnectionChanged(true, $"포트 {_portName} 재연결 성공");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{_portName}] 재연결 실패: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region 내부 유틸리티

        /// <summary>
        /// 통신 설정에 따라 SerialPort 인스턴스를 생성합니다.
        /// DataReceived 이벤트는 등록하지 않습니다.
        /// </summary>
        private static SerialPort CreateSerialPort(string portName, CommunicationSettings settings)
        {
            return new SerialPort
            {
                PortName = portName,
                BaudRate = settings.BaudRate,
                DataBits = settings.DataBits,
                Parity = settings.Parity,
                StopBits = settings.StopBits,
                Handshake = settings.Handshake,
                ReadTimeout = settings.ReadTimeout,
                WriteTimeout = settings.WriteTimeout,
                ReadBufferSize = 4096,
                WriteBufferSize = 4096
                // ★ ReceivedBytesThreshold 설정하지 않음 — DataReceived 이벤트 사용 안 함
            };
        }

        /// <summary>
        /// 포트 리소스를 안전하게 정리합니다.
        /// </summary>
        private void CleanupPort()
        {
            try
            {
                if (_port != null)
                {
                    if (_port.IsOpen)
                    {
                        try { _port.DiscardInBuffer(); } catch { }
                        try { _port.DiscardOutBuffer(); } catch { }
                        _port.Close();
                    }
                    _port.Dispose();
                    _port = null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[{_portName}] 포트 정리 중 예외: {ex.Message}");
                _port = null;
            }
        }

        /// <summary>
        /// 큐에 남아있는 모든 명령을 꺼내 null 응답으로 완료시킵니다.
        /// </summary>
        private void DrainAndCancelQueue()
        {
            while (_commandQueue.TryTake(out var cmd))
            {
                cmd.ResponseTcs.TrySetResult(null);
            }
        }

        private void OnConnectionChanged(bool isConnected, string message, Exception ex = null)
        {
            ConnectionChanged?.Invoke(this, new PortConnectionChangedEventArgs(_portName, isConnected, message, ex));
        }

        private void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, message);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            _isConnected = false;

            // 처리 스레드 종료 요청
            _cts.Cancel();

            // 큐 완료 마킹
            try { _commandQueue.CompleteAdding(); } catch { }

            // 큐에 남은 명령 취소
            DrainAndCancelQueue();

            // 처리 스레드 종료 대기 (최대 2초)
            if (_processingThread != null && _processingThread.IsAlive)
            {
                _processingThread.Join(2000);
            }

            // 포트 정리
            CleanupPort();

            _cts.Dispose();
            _commandQueue.Dispose();
        }

        #endregion
    }
}