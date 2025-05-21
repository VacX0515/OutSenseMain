using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 데이터를 CSV 파일로 로깅하는 서비스
    /// </summary>
    public class DataLoggerService
    {
        #region 싱글톤 패턴

        private static readonly Lazy<DataLoggerService> _instance = new Lazy<DataLoggerService>(() => new DataLoggerService());
        public static DataLoggerService Instance => _instance.Value;

        #endregion

        #region 필드 및 속성

        private readonly object _fileLock = new object();
        private readonly string _dataDirectory;
        private readonly int _maxFileSize = 50 * 1024 * 1024; // 50MB
        private Dictionary<string, StreamWriter> _dataWriters = new Dictionary<string, StreamWriter>();
        private Dictionary<string, string> _currentFiles = new Dictionary<string, string>();
        private Dictionary<string, List<string>> _headers = new Dictionary<string, List<string>>();
        private bool _isLoggingEnabled = true;

        /// <summary>
        /// 로깅 활성화 여부
        /// </summary>
        public bool IsLoggingEnabled
        {
            get => _isLoggingEnabled;
            set => _isLoggingEnabled = value;
        }

        #endregion

        #region 생성자 및 초기화

        /// <summary>
        /// DataLoggerService의 새 인스턴스를 초기화합니다.
        /// </summary>
        private DataLoggerService()
        {
            // 데이터 디렉토리 설정
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

            // 데이터 디렉토리가 없으면 생성
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
        }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~DataLoggerService()
        {
            CloseAllWriters();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 새 로그 세션을 시작하고 헤더를 설정합니다.
        /// </summary>
        /// <param name="logType">로그 타입 (파일명과 컴포넌트 폴더명에 사용)</param>
        /// <param name="headers">CSV 헤더 목록</param>
        /// <returns>성공 여부</returns>
        public bool StartLogging(string logType, List<string> headers)
        {
            if (!_isLoggingEnabled || string.IsNullOrEmpty(logType) || headers == null || headers.Count == 0)
                return false;

            lock (_fileLock)
            {
                try
                {
                    // 이전에 사용 중이던 로거 닫기
                    if (_dataWriters.ContainsKey(logType))
                    {
                        CloseWriter(logType);
                    }

                    // 헤더 저장
                    _headers[logType] = new List<string>(headers);

                    // 컴포넌트별 폴더 생성 확인
                    EnsureComponentDirectory(logType);

                    // 새 파일 생성
                    return CreateNewDataFile(logType);
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"데이터 로깅 시작 오류: {ex.Message}", ex);
                    return false;
                }
            }
        }

        /// <summary>
        /// 데이터 행을 로깅합니다.
        /// </summary>
        /// <param name="logType">로그 타입</param>
        /// <param name="values">로깅할 값 목록 (헤더 순서와 일치해야 함)</param>
        /// <returns>성공 여부</returns>
        public bool LogData(string logType, List<string> values)
        {
            if (!_isLoggingEnabled || string.IsNullOrEmpty(logType) || values == null)
                return false;

            lock (_fileLock)
            {
                try
                {
                    // 로거가 없으면 생성
                    if (!_dataWriters.ContainsKey(logType) || _dataWriters[logType] == null)
                    {
                        // 컴포넌트별 폴더 생성 확인
                        EnsureComponentDirectory(logType);

                        if (!CreateNewDataFile(logType))
                            return false;
                    }

                    // 파일 크기 확인
                    FileInfo fileInfo = new FileInfo(_currentFiles[logType]);
                    if (fileInfo.Length > _maxFileSize)
                    {
                        if (!CreateNewDataFile(logType))
                            return false;
                    }

                    // 현재 시간 추가 (첫 번째 열)
                    string timestamp = $"\"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\"";

                    // CSV 행 생성
                    StringBuilder sb = new StringBuilder();
                    sb.Append(timestamp);

                    foreach (string value in values)
                    {
                        sb.Append(',');

                        // 값에 쉼표가 포함되어 있는 경우 큰따옴표로 묶음
                        if (value != null && value.Contains(","))
                        {
                            sb.Append('"').Append(value.Replace("\"", "\"\"")).Append('"');
                        }
                        else
                        {
                            sb.Append(value ?? "");
                        }
                    }

                    // 데이터 쓰기
                    _dataWriters[logType].WriteLine(sb.ToString());
                    _dataWriters[logType].Flush();

                    return true;
                }
                catch (Exception ex)
                {
                    LoggerService.Instance.LogError($"데이터 로깅 오류: {ex.Message}", ex);

                    // 오류 발생 시 새 파일 생성 시도
                    try
                    {
                        CloseWriter(logType);
                        CreateNewDataFile(logType);
                    }
                    catch { /* 무시 */ }

                    return false;
                }
            }
        }

        /// <summary>
        /// 비동기로 데이터 행을 로깅합니다.
        /// </summary>
        /// <param name="logType">로그 타입</param>
        /// <param name="values">로깅할 값 목록</param>
        /// <returns>태스크</returns>
        public async Task<bool> LogDataAsync(string logType, List<string> values)
        {
            return await Task.Run(() => LogData(logType, values));
        }

        /// <summary>
        /// 로깅을 중지하고 파일을 닫습니다.
        /// </summary>
        /// <param name="logType">로그 타입</param>
        public void StopLogging(string logType)
        {
            lock (_fileLock)
            {
                CloseWriter(logType);
            }
        }

        /// <summary>
        /// 모든 로깅을 중지하고 파일을 닫습니다.
        /// </summary>
        public void StopAllLogging()
        {
            lock (_fileLock)
            {
                CloseAllWriters();
            }
        }

        #endregion

        #region 내부 메서드

        /// <summary>
        /// 컴포넌트별 디렉토리가 있는지 확인하고 없으면 생성합니다.
        /// </summary>
        /// <param name="logType">로그 타입 (컴포넌트명)</param>
        /// <returns>컴포넌트 디렉토리 경로</returns>
        private string EnsureComponentDirectory(string logType)
        {
            string componentDir = Path.Combine(_dataDirectory, logType);

            // 컴포넌트 디렉토리가 없으면 생성
            if (!Directory.Exists(componentDir))
            {
                Directory.CreateDirectory(componentDir);
            }

            return componentDir;
        }

        /// <summary>
        /// 새 데이터 파일 생성
        /// </summary>
        /// <param name="logType">로그 타입</param>
        /// <returns>성공 여부</returns>
        private bool CreateNewDataFile(string logType)
        {
            try
            {
                // 이전 파일 닫기
                CloseWriter(logType);

                // 컴포넌트 디렉토리 경로 가져오기
                string componentDir = EnsureComponentDirectory(logType);

                // 현재 시간을 사용한 파일명 생성
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = Path.Combine(componentDir, $"{logType}_{timestamp}.csv");
                _currentFiles[logType] = fileName;

                // 새 StreamWriter 생성
                _dataWriters[logType] = new StreamWriter(fileName, false, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // 헤더 쓰기
                if (_headers.ContainsKey(logType) && _headers[logType] != null)
                {
                    // 시간 헤더 추가
                    StringBuilder headerLine = new StringBuilder("Timestamp");

                    foreach (string header in _headers[logType])
                    {
                        headerLine.Append(',');

                        // 헤더에 쉼표가 포함되어 있는 경우 큰따옴표로 묶음
                        if (header.Contains(","))
                        {
                            headerLine.Append('"').Append(header.Replace("\"", "\"\"")).Append('"');
                        }
                        else
                        {
                            headerLine.Append(header);
                        }
                    }

                    _dataWriters[logType].WriteLine(headerLine.ToString());
                    _dataWriters[logType].Flush();
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"데이터 파일 생성 오류: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// 특정 로그 타입의 StreamWriter 닫기
        /// </summary>
        /// <param name="logType">로그 타입</param>
        private void CloseWriter(string logType)
        {
            if (_dataWriters.ContainsKey(logType) && _dataWriters[logType] != null)
            {
                try
                {
                    _dataWriters[logType].Flush();
                    _dataWriters[logType].Close();
                    _dataWriters[logType].Dispose();
                }
                catch { /* 무시 */ }
                finally
                {
                    _dataWriters[logType] = null;
                }
            }
        }

        /// <summary>
        /// 모든 StreamWriter 닫기
        /// </summary>
        private void CloseAllWriters()
        {
            foreach (string logType in _dataWriters.Keys)
            {
                CloseWriter(logType);
            }
            _dataWriters.Clear();
            _currentFiles.Clear();
        }

        #endregion
    }
}