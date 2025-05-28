using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Models;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 비동기 로깅 서비스 - UI와 통신에 영향을 주지 않음 (싱글톤)
    /// </summary>
    public class AsyncLoggingService : IDisposable
    {
        #region 싱글톤 패턴

        private static readonly Lazy<AsyncLoggingService> _instance =
            new Lazy<AsyncLoggingService>(() => new AsyncLoggingService());

        public static AsyncLoggingService Instance => _instance.Value;

        #endregion

        #region 필드

        private readonly ConcurrentQueue<LogEntry> _logQueue = new ConcurrentQueue<LogEntry>();
        private readonly ConcurrentQueue<DataLogEntry> _dataLogQueue = new ConcurrentQueue<DataLogEntry>();

        private Thread _loggingThread;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly AutoResetEvent _logEvent = new AutoResetEvent(false);

        private volatile bool _isRunning = false;
        private volatile bool _isLoggingEnabled = true;

        // 파일 경로
        private readonly string _logDirectory;
        private readonly string _dataDirectory;

        // 로그 버퍼 (배치 쓰기용)
        private const int BATCH_SIZE = 50;
        private const int FLUSH_INTERVAL_MS = 1000;

        // 이벤트
        public event EventHandler<string> LogAdded;

        #endregion

        #region 생성자

        private AsyncLoggingService()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            Directory.CreateDirectory(_logDirectory);
            Directory.CreateDirectory(_dataDirectory);
        }

        #endregion

        #region 공개 메서드

        public void Start()
        {
            if (_isRunning) return;

            _isRunning = true;

            // 백그라운드 스레드 시작
            _loggingThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Priority = ThreadPriority.BelowNormal,
                Name = "AsyncLoggingThread"
            };
            _loggingThread.Start();

            LogInfo("비동기 로깅 서비스 시작됨");
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource.Cancel();
            _logEvent.Set();

            if (_loggingThread != null && !_loggingThread.Join(3000))
            {
                try { _loggingThread.Abort(); } catch { }
            }

            // 남은 로그 모두 처리
            FlushAllLogs();
        }

        /// <summary>
        /// 로깅 활성화/비활성화
        /// </summary>
        public bool IsLoggingEnabled
        {
            get => _isLoggingEnabled;
            set => _isLoggingEnabled = value;
        }

        /// <summary>
        /// 정보 로그 추가 (비동기)
        /// </summary>
        public void LogInfo(string message)
        {
            if (!_isLoggingEnabled) return;
            EnqueueLog(LogLevel.Info, message);
        }

        /// <summary>
        /// 경고 로그 추가 (비동기)
        /// </summary>
        public void LogWarning(string message)
        {
            if (!_isLoggingEnabled) return;
            EnqueueLog(LogLevel.Warning, message);
        }

        /// <summary>
        /// 오류 로그 추가 (비동기)
        /// </summary>
        public void LogError(string message, Exception ex = null)
        {
            if (!_isLoggingEnabled) return;
            var fullMessage = ex != null ? $"{message} - {ex.Message}" : message;
            EnqueueLog(LogLevel.Error, fullMessage);

            if (ex != null)
            {
                EnqueueLog(LogLevel.Error, $"StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// 디버그 로그 추가 (비동기)
        /// </summary>
        public void LogDebug(string message)
        {
#if DEBUG
            if (!_isLoggingEnabled) return;
            EnqueueLog(LogLevel.Debug, message);
#endif
        }
        /// <summary>
        /// 데이터 로깅 (비동기)
        /// </summary>
        public void LogData(UIDataSnapshot snapshot)
        {
            if (!_isLoggingEnabled || snapshot == null) return;

            Task.Run(() =>
            {
                try
                {
                    // 압력 및 밸브 상태 데이터
                    var pressureEntry = new DataLogEntry
                    {
                        Category = "Pressure",
                        Timestamp = DateTime.Now,
                        Data = new[]
                        {
                    snapshot.AtmPressure.ToString("F2"),
                    snapshot.PiraniPressure.ToString("E2"),
                    snapshot.IonPressure.ToString("E2"),
                    snapshot.IonGaugeStatus,
                    snapshot.GateValveStatus,
                    snapshot.VentValveStatus,
                    snapshot.ExhaustValveStatus,
                    snapshot.IonGaugeHVStatus
                }
                    };
                    _dataLogQueue.Enqueue(pressureEntry);

                    // 펌프 데이터 (연결된 경우만)
                    if (snapshot.Connections.DryPump)
                    {
                        var dryPumpEntry = new DataLogEntry
                        {
                            Category = "DryPump",
                            Timestamp = DateTime.Now,
                            Data = new[]
                            {
                        snapshot.DryPump.Status,
                        snapshot.DryPump.Speed,
                        snapshot.DryPump.Current,
                        snapshot.DryPump.Temperature,
                        snapshot.DryPump.HasWarning.ToString(),
                        snapshot.DryPump.HasError.ToString()
                    }
                        };
                        _dataLogQueue.Enqueue(dryPumpEntry);
                    }

                    if (snapshot.Connections.TurboPump)
                    {
                        var turboPumpEntry = new DataLogEntry
                        {
                            Category = "TurboPump",
                            Timestamp = DateTime.Now,
                            Data = new[]
                            {
                        snapshot.TurboPump.Status,
                        snapshot.TurboPump.Speed,
                        snapshot.TurboPump.Current,
                        snapshot.TurboPump.Temperature,
                        snapshot.TurboPump.HasWarning.ToString(),
                        snapshot.TurboPump.HasError.ToString()
                    }
                        };
                        _dataLogQueue.Enqueue(turboPumpEntry);
                    }

                    if (snapshot.Connections.BathCirculator)
                    {
                        var bathCirculatorEntry = new DataLogEntry
                        {
                            Category = "BathCirculator",
                            Timestamp = DateTime.Now,
                            Data = new[]
                            {
                        snapshot.BathCirculator.Status,
                        snapshot.BathCirculator.CurrentTemp,
                        snapshot.BathCirculator.TargetTemp,
                        snapshot.BathCirculator.Mode,
                        snapshot.BathCirculator.Time,
                        snapshot.BathCirculator.HasError.ToString(),
                        snapshot.BathCirculator.HasWarning.ToString()
                    }
                        };
                        _dataLogQueue.Enqueue(bathCirculatorEntry);
                    }

                    if (snapshot.Connections.TempController)
                    {
                        var tempControllerEntry = new DataLogEntry
                        {
                            Category = "TempController",
                            Timestamp = DateTime.Now,
                            Data = new[]
                            {
                        snapshot.TempController.Channels[0].PresentValue,
                        snapshot.TempController.Channels[0].SetValue,
                        snapshot.TempController.Channels[0].HeatingMV,
                        snapshot.TempController.Channels[0].Status,
                        snapshot.TempController.Channels[1].PresentValue,
                        snapshot.TempController.Channels[1].SetValue,
                        snapshot.TempController.Channels[1].HeatingMV,
                        snapshot.TempController.Channels[1].Status
                    }
                        };
                        _dataLogQueue.Enqueue(tempControllerEntry);
                    }

                    _logEvent.Set();
                }
                catch
                {
                    // 로깅 오류는 무시
                }
            });
        }

        #endregion

        #region 내부 메서드

        private void EnqueueLog(LogLevel level, string message)
        {
            var entry = new LogEntry
            {
                Level = level,
                Timestamp = DateTime.Now,
                Message = message
            };

            _logQueue.Enqueue(entry);

            // 이벤트 발생 (UI 업데이트용)
            var formattedMessage = $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}";
            LogAdded?.Invoke(this, formattedMessage);

            _logEvent.Set();
        }

        /// <summary>
        /// 백그라운드 로그 처리 스레드
        /// </summary>
        private void ProcessLogQueue()
        {
            var logBuffer = new List<LogEntry>(BATCH_SIZE);
            var dataLogBuffer = new List<DataLogEntry>(BATCH_SIZE);
            var lastFlush = DateTime.Now;

            while (_isRunning)
            {
                try
                {
                    // 이벤트 대기 (최대 1초)
                    _logEvent.WaitOne(FLUSH_INTERVAL_MS);

                    // 로그 수집
                    while (_logQueue.TryDequeue(out var logEntry) && logBuffer.Count < BATCH_SIZE)
                    {
                        logBuffer.Add(logEntry);
                    }

                    // 데이터 로그 수집
                    while (_dataLogQueue.TryDequeue(out var dataEntry) && dataLogBuffer.Count < BATCH_SIZE)
                    {
                        dataLogBuffer.Add(dataEntry);
                    }

                    // 배치 쓰기 조건 확인
                    var now = DateTime.Now;
                    if (logBuffer.Count >= BATCH_SIZE ||
                        dataLogBuffer.Count >= BATCH_SIZE ||
                        (now - lastFlush).TotalMilliseconds >= FLUSH_INTERVAL_MS)
                    {
                        if (logBuffer.Count > 0)
                            WriteLogBatch(logBuffer);

                        if (dataLogBuffer.Count > 0)
                            WriteDataLogBatch(dataLogBuffer);

                        logBuffer.Clear();
                        dataLogBuffer.Clear();
                        lastFlush = now;
                    }
                }
                catch (Exception ex)
                {
                    // 로깅 스레드 오류는 콘솔에만 출력
                    Console.WriteLine($"로깅 스레드 오류: {ex.Message}");
                }
            }

            // 종료 시 남은 로그 처리
            if (logBuffer.Count > 0)
                WriteLogBatch(logBuffer);
            if (dataLogBuffer.Count > 0)
                WriteDataLogBatch(dataLogBuffer);
        }

        private void WriteLogBatch(List<LogEntry> entries)
        {
            try
            {
                var logFile = Path.Combine(_logDirectory, $"VacX_{DateTime.Now:yyyyMMdd}.log");

                using (var writer = new StreamWriter(logFile, true, Encoding.UTF8))
                {
                    foreach (var entry in entries)
                    {
                        writer.WriteLine($"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}");
                    }
                }
            }
            catch
            {
                // 파일 쓰기 오류 무시
            }
        }

        private void WriteDataLogBatch(List<DataLogEntry> entries)
        {
            try
            {
                var groupedEntries = entries.GroupBy(e => e.Category);

                foreach (var group in groupedEntries)
                {
                    var category = group.Key;
                    var categoryDir = Path.Combine(_dataDirectory, category);
                    Directory.CreateDirectory(categoryDir);

                    var dataFile = Path.Combine(categoryDir, $"{category}_{DateTime.Now:yyyyMMdd_HH}.csv");
                    var fileExists = File.Exists(dataFile);

                    using (var writer = new StreamWriter(dataFile, true, Encoding.UTF8))
                    {
                        // 헤더 쓰기 (새 파일인 경우)
                        if (!fileExists)
                        {
                            writer.WriteLine(GetHeaderForCategory(category));
                        }

                        // 데이터 쓰기
                        foreach (var entry in group)
                        {
                            writer.WriteLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{string.Join(",", entry.Data)}");
                        }
                    }
                }
            }
            catch
            {
                // 파일 쓰기 오류 무시
            }
        }

        private string GetHeaderForCategory(string category)
        {
            switch (category)
            {
                case "Pressure":
                    return "Timestamp,ATM(kPa),Pirani(Torr),Ion(Torr),IonStatus,GateValve,VentValve,ExhaustValve,IonGaugeHV";
                case "DryPump":
                    return "Timestamp,Status,Speed,Current,Temperature,HasWarning,HasError";
                case "TurboPump":
                    return "Timestamp,Status,Speed,Current,Temperature,HasWarning,HasError";
                case "BathCirculator":
                    return "Timestamp,Status,CurrentTemp,TargetTemp,Mode,Time,HasError,HasWarning";
                case "TempController":
                    return "Timestamp,Ch1_PV,Ch1_SV,Ch1_MV,Ch1_Status,Ch2_PV,Ch2_SV,Ch2_MV,Ch2_Status";
                default:
                    return "Timestamp,Data1,Data2,Data3,Data4";
            }
        }

        private void FlushAllLogs()
        {
            var logBuffer = new List<LogEntry>();
            var dataLogBuffer = new List<DataLogEntry>();

            while (_logQueue.TryDequeue(out var logEntry))
                logBuffer.Add(logEntry);

            while (_dataLogQueue.TryDequeue(out var dataEntry))
                dataLogBuffer.Add(dataEntry);

            if (logBuffer.Count > 0)
                WriteLogBatch(logBuffer);
            if (dataLogBuffer.Count > 0)
                WriteDataLogBatch(dataLogBuffer);
        }

        public void Dispose()
        {
            Stop();
            _logEvent?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        #endregion

        #region 내부 클래스

        private class LogEntry
        {
            public LogLevel Level { get; set; }
            public DateTime Timestamp { get; set; }
            public string Message { get; set; }
        }

        private class DataLogEntry
        {
            public string Category { get; set; }
            public DateTime Timestamp { get; set; }
            public string[] Data { get; set; }
        }

        private enum LogLevel
        {
            Debug,
            Info,
            Warning,
            Error
        }

        #endregion
    }
}