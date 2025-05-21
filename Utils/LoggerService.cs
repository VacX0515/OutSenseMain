using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 애플리케이션 로깅을 관리하는 서비스
    /// </summary>
    public class LoggerService
    {
        #region 싱글톤 패턴

        private static readonly Lazy<LoggerService> _instance = new Lazy<LoggerService>(() => new LoggerService());
        public static LoggerService Instance => _instance.Value;

        #endregion

        #region 필드 및 속성

        private readonly object _logLock = new object();
        private readonly object _fileLock = new object();
        private readonly Queue<string> _logQueue = new Queue<string>();
        private readonly int _maxQueueSize = 1000;
        private readonly int _logProcessingIntervalMs = 500;
        private readonly int _maxLogFileSize = 10 * 1024 * 1024; // 10MB
        private readonly string _logDirectory;
        private string _currentLogFile;
        private StreamWriter _currentWriter;
        private System.Threading.Timer _processTimer;
        private bool _isProcessing = false;

        /// <summary>
        /// 로그 이벤트
        /// </summary>
        public event EventHandler<string> LogAdded;

        #endregion

        #region 생성자 및 초기화

        /// <summary>
        /// LoggerService의 새 인스턴스를 초기화합니다.
        /// </summary>
        private LoggerService()
        {
            // 로그 디렉토리 설정
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

            // 디렉토리가 없으면 생성
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }

            // 새 로그 파일 생성
            CreateNewLogFile();

            // 로그 처리 타이머 시작
            _processTimer = new System.Threading.Timer(ProcessLogQueue, null, _logProcessingIntervalMs, _logProcessingIntervalMs);
        }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~LoggerService()
        {
            CloseCurrentWriter();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 정보 로그 추가
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public void LogInfo(string message)
        {
            LogMessage("INFO", message);
        }

        /// <summary>
        /// 경고 로그 추가
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public void LogWarning(string message)
        {
            LogMessage("WARNING", message);
        }

        /// <summary>
        /// 오류 로그 추가
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public void LogError(string message)
        {
            LogMessage("ERROR", message);
        }

        /// <summary>
        /// 오류 로그 추가 (예외 포함)
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="ex">예외 객체</param>
        public void LogError(string message, Exception ex)
        {
            LogMessage("ERROR", $"{message} - {ex.Message}");
            LogMessage("ERROR", $"StackTrace: {ex.StackTrace}");
        }

        /// <summary>
        /// 디버그 로그 추가
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public void LogDebug(string message)
        {
            LogMessage("DEBUG", message);
        }

        #endregion

        #region 내부 메서드

        /// <summary>
        /// 로그 메시지 큐에 추가
        /// </summary>
        /// <param name="logLevel">로그 레벨</param>
        /// <param name="message">로그 메시지</param>
        private void LogMessage(string logLevel, string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string formattedMessage = $"[{timestamp}] [{logLevel}] {message}";

            lock (_logLock)
            {
                // 큐 크기 제한 확인
                if (_logQueue.Count >= _maxQueueSize)
                {
                    _logQueue.Dequeue(); // 가장 오래된 로그 제거
                }

                _logQueue.Enqueue(formattedMessage);
            }

            // 로그 이벤트 발생
            LogAdded?.Invoke(this, formattedMessage);
        }

        /// <summary>
        /// 로그 큐 처리 (타이머 콜백)
        /// </summary>
        private void ProcessLogQueue(object state)
        {
            if (_isProcessing)
                return;

            _isProcessing = true;

            try
            {
                List<string> logsToWrite = new List<string>();

                lock (_logLock)
                {
                    while (_logQueue.Count > 0)
                    {
                        logsToWrite.Add(_logQueue.Dequeue());
                    }
                }

                if (logsToWrite.Count > 0)
                {
                    WriteLogsToFile(logsToWrite);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 처리 오류: {ex.Message}");
            }
            finally
            {
                _isProcessing = false;
            }
        }

        /// <summary>
        /// 로그를 파일에 기록
        /// </summary>
        /// <param name="logs">로그 목록</param>
        private void WriteLogsToFile(List<string> logs)
        {
            lock (_fileLock)
            {
                try
                {
                    // 로그 파일 크기 확인
                    FileInfo fileInfo = new FileInfo(_currentLogFile);

                    // 파일이 없거나 크기 제한을 초과한 경우 새 파일 생성
                    if (!fileInfo.Exists || fileInfo.Length > _maxLogFileSize)
                    {
                        CloseCurrentWriter();
                        CreateNewLogFile();
                    }

                    // 로그 파일에 쓰기
                    if (_currentWriter != null)
                    {
                        foreach (string log in logs)
                        {
                            _currentWriter.WriteLine(log);
                        }
                        _currentWriter.Flush();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"로그 파일 쓰기 오류: {ex.Message}");

                    // 오류 발생 시 새 로그 파일 시도
                    try
                    {
                        CloseCurrentWriter();
                        CreateNewLogFile();
                    }
                    catch { /* 무시 */ }
                }
            }
        }

        /// <summary>
        /// 새 로그 파일 생성
        /// </summary>
        private void CreateNewLogFile()
        {
            try
            {
                // 현재 시간을 사용한 파일명 생성
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _currentLogFile = Path.Combine(_logDirectory, $"VacX_OutSense_Log_{timestamp}.txt");

                // 이전 StreamWriter 닫기
                CloseCurrentWriter();

                // 새 StreamWriter 생성
                _currentWriter = new StreamWriter(_currentLogFile, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // 로그 파일 시작 헤더 추가
                _currentWriter.WriteLine("===========================================");
                _currentWriter.WriteLine($"VacX OutSense 로그 시작: {DateTime.Now}");
                _currentWriter.WriteLine("===========================================");
                _currentWriter.Flush();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"로그 파일 생성 오류: {ex.Message}");
                _currentWriter = null;
            }
        }

        /// <summary>
        /// 현재 StreamWriter 닫기
        /// </summary>
        private void CloseCurrentWriter()
        {
            if (_currentWriter != null)
            {
                try
                {
                    _currentWriter.WriteLine("===========================================");
                    _currentWriter.WriteLine($"VacX OutSense 로그 종료: {DateTime.Now}");
                    _currentWriter.WriteLine("===========================================");
                    _currentWriter.Flush();
                    _currentWriter.Close();
                    _currentWriter.Dispose();
                }
                catch { /* 무시 */ }
                finally
                {
                    _currentWriter = null;
                }
            }
        }

        #endregion
    }
}