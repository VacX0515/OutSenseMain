using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.CommandExecution
{
    /// <summary>
    /// 명령 실행 우선순위를 관리하는 매니저
    /// 실행 명령이 조회 명령보다 우선적으로 처리되도록 보장
    /// </summary>
    public class CommandPriorityManager : IDisposable
    {
        #region 필드

        private readonly ConcurrentDictionary<string, PriorityQueue> _deviceQueues;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _deviceLocks;
        private readonly OptimizedDataCollectionService _dataCollectionService;
        private bool _disposed = false;

        #endregion

        #region 내부 클래스

        /// <summary>
        /// 명령 타입
        /// </summary>
        public enum CommandType
        {
            Query = 0,      // 조회 명령 (낮은 우선순위)
            Execute = 1     // 실행 명령 (높은 우선순위)
        }

        /// <summary>
        /// 명령 래퍼
        /// </summary>
        private class CommandWrapper
        {
            public Func<Task<object>> Command { get; set; }
            public CommandType Type { get; set; }
            public TaskCompletionSource<object> CompletionSource { get; set; }
            public DateTime Timestamp { get; set; }
        }

        /// <summary>
        /// 우선순위 큐
        /// </summary>
        private class PriorityQueue
        {
            private readonly object _lock = new object();
            private readonly List<CommandWrapper> _executeCommands = new List<CommandWrapper>();
            private readonly Queue<CommandWrapper> _queryCommands = new Queue<CommandWrapper>();

            public void Enqueue(CommandWrapper command)
            {
                lock (_lock)
                {
                    if (command.Type == CommandType.Execute)
                    {
                        _executeCommands.Add(command);
                    }
                    else
                    {
                        _queryCommands.Enqueue(command);
                    }
                }
            }

            public CommandWrapper Dequeue()
            {
                lock (_lock)
                {
                    // 실행 명령이 있으면 우선 처리
                    if (_executeCommands.Count > 0)
                    {
                        var command = _executeCommands[0];
                        _executeCommands.RemoveAt(0);
                        return command;
                    }

                    // 조회 명령 처리
                    if (_queryCommands.Count > 0)
                    {
                        return _queryCommands.Dequeue();
                    }

                    return null;
                }
            }

            public bool HasCommands
            {
                get
                {
                    lock (_lock)
                    {
                        return _executeCommands.Count > 0 || _queryCommands.Count > 0;
                    }
                }
            }

            public int ExecuteCommandCount
            {
                get
                {
                    lock (_lock)
                    {
                        return _executeCommands.Count;
                    }
                }
            }

            public void ClearAll()
            {
                lock (_lock)
                {
                    // 모든 대기 중인 명령 취소
                    foreach (var cmd in _executeCommands)
                    {
                        cmd.CompletionSource.TrySetCanceled();
                    }
                    _executeCommands.Clear();

                    while (_queryCommands.Count > 0)
                    {
                        var cmd = _queryCommands.Dequeue();
                        cmd.CompletionSource.TrySetCanceled();
                    }
                }
            }
        }

        #endregion

        #region 생성자

        public CommandPriorityManager(OptimizedDataCollectionService dataCollectionService)
        {
            _dataCollectionService = dataCollectionService ?? throw new ArgumentNullException(nameof(dataCollectionService));
            _deviceQueues = new ConcurrentDictionary<string, PriorityQueue>();
            _deviceLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

            // 각 디바이스별 큐와 락 초기화
            InitializeDevice("IOModule");
            InitializeDevice("DryPump");
            InitializeDevice("TurboPump");
            InitializeDevice("BathCirculator");
            InitializeDevice("TempController");
            InitializeDevice("RelayModule");
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 실행 명령을 큐에 추가하고 처리
        /// </summary>
        public async Task<T> ExecuteCommandAsync<T>(string deviceName, Func<Task<T>> command, int timeoutMs = 5000)
        {
            EnsureNotDisposed();

            if (!_deviceQueues.ContainsKey(deviceName))
            {
                throw new ArgumentException($"Unknown device: {deviceName}");
            }

            var wrapper = new CommandWrapper
            {
                Command = async () => await command(),
                Type = CommandType.Execute,
                CompletionSource = new TaskCompletionSource<object>(),
                Timestamp = DateTime.Now
            };

            // 실행 명령이 있으면 데이터 수집 일시 정지
            if (_dataCollectionService != null)
            {
                _dataCollectionService.PauseCollection();
            }

            try
            {
                // 큐에 추가
                _deviceQueues[deviceName].Enqueue(wrapper);

                // 처리 시작
                _ = ProcessQueueAsync(deviceName);

                // 타임아웃 설정
                using (var cts = new CancellationTokenSource(timeoutMs))
                {
                    var tcs = wrapper.CompletionSource;
                    using (cts.Token.Register(() => tcs.TrySetCanceled()))
                    {
                        var result = await tcs.Task;
                        return (T)result;
                    }
                }
            }
            finally
            {
                // 실행 명령이 모두 처리되면 데이터 수집 재개
                if (!HasPendingExecuteCommands())
                {
                    _dataCollectionService?.ResumeCollection();
                }
            }
        }

        /// <summary>
        /// 조회 명령을 큐에 추가하고 처리
        /// </summary>
        public async Task<T> QueryAsync<T>(string deviceName, Func<Task<T>> query, int timeoutMs = 1000)
        {
            EnsureNotDisposed();

            if (!_deviceQueues.ContainsKey(deviceName))
            {
                throw new ArgumentException($"Unknown device: {deviceName}");
            }

            // 실행 명령이 대기 중이면 조회 스킵
            if (_deviceQueues[deviceName].ExecuteCommandCount > 0)
            {
                return default(T);
            }

            var wrapper = new CommandWrapper
            {
                Command = async () => await query(),
                Type = CommandType.Query,
                CompletionSource = new TaskCompletionSource<object>(),
                Timestamp = DateTime.Now
            };

            // 큐에 추가
            _deviceQueues[deviceName].Enqueue(wrapper);

            // 처리 시작
            _ = ProcessQueueAsync(deviceName);

            // 타임아웃 설정
            using (var cts = new CancellationTokenSource(timeoutMs))
            {
                var tcs = wrapper.CompletionSource;
                using (cts.Token.Register(() => tcs.TrySetCanceled()))
                {
                    try
                    {
                        var result = await tcs.Task;
                        return (T)result;
                    }
                    catch (TaskCanceledException)
                    {
                        // 타임아웃 시 기본값 반환
                        return default(T);
                    }
                }
            }
        }

        #endregion

        #region 내부 메서드

        private void InitializeDevice(string deviceName)
        {
            _deviceQueues[deviceName] = new PriorityQueue();
            _deviceLocks[deviceName] = new SemaphoreSlim(1, 1);
        }

        private async Task ProcessQueueAsync(string deviceName)
        {
            var queue = _deviceQueues[deviceName];
            var semaphore = _deviceLocks[deviceName];

            while (queue.HasCommands)
            {
                CommandWrapper wrapper = null;

                try
                {
                    // 락 획득 (실행 명령은 무제한 대기, 조회 명령은 짧은 대기)
                    wrapper = queue.Dequeue();
                    if (wrapper == null) continue;

                    var waitTime = wrapper.Type == CommandType.Execute ? -1 : 50;
                    if (!await semaphore.WaitAsync(waitTime))
                    {
                        // 조회 명령이 락을 못 얻으면 취소
                        wrapper.CompletionSource.TrySetCanceled();
                        continue;
                    }

                    // 명령 실행
                    var result = await wrapper.Command();
                    wrapper.CompletionSource.TrySetResult(result);
                }
                catch (Exception ex)
                {
                    wrapper?.CompletionSource.TrySetException(ex);
                }
                finally
                {
                    semaphore.Release();
                }
            }
        }

        private bool HasPendingExecuteCommands()
        {
            foreach (var queue in _deviceQueues.Values)
            {
                if (queue.ExecuteCommandCount > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 대기 중인 명령이 있는지 확인
        /// </summary>
        public bool HasPendingCommands
        {
            get
            {
                foreach (var queue in _deviceQueues.Values)
                {
                    if (queue.HasCommands)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 특정 장치의 대기 중인 명령 수 확인
        /// </summary>
        public int GetPendingCommandCount(string deviceName)
        {
            if (_deviceQueues.TryGetValue(deviceName, out var queue))
            {
                return queue.ExecuteCommandCount;
            }
            return 0;
        }

        /// <summary>
        /// 모든 대기 중인 명령 취소
        /// </summary>
        public void CancelAllPendingCommands()
        {
            foreach (var queue in _deviceQueues.Values)
            {
                queue.ClearAll();
            }
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CommandPriorityManager));
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

            foreach (var semaphore in _deviceLocks.Values)
            {
                semaphore?.Dispose();
            }

            _disposed = true;
        }

        #endregion
    }
}