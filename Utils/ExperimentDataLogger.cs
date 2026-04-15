using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using VacX_OutSense.Core.AutoRun;
using VacX_OutSense.Models;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// AutoRun 실험 통합 데이터 로거.
    /// 실험 중 선택된 센서 데이터를 CSV 파일에 기록합니다.
    /// 파일이 MaxRowsPerFile에 도달하면 자동 분할됩니다.
    /// </summary>
    public class ExperimentDataLogger : IDisposable
    {
        private StreamWriter _writer;
        private string _filePath;
        private readonly object _lock = new object();
        private bool _isRunning;
        private DateTime _startTime;

        // 파일 분할 관련
        private const int MaxRowsPerFile = 150_000;
        private int _rowCount;
        private int _fileIndex;
        private string _baseDirectory;
        private string _baseName;

        // 컬럼 설정 (Start 시 캡처)
        private bool _logPressure = true;
        private bool _logValves = true;
        private bool _logDryPump = true;
        private bool _logTurboPump = true;
        private bool _logChiller = true;
        private bool[] _logTempCh = new bool[12];
        private bool _logAdditionalAI = true;

        /// <summary>
        /// 현재 로깅 중인지 여부
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 현재 로그 파일 경로
        /// </summary>
        public string FilePath => _filePath;

        /// <summary>
        /// 실험 데이터 로깅을 시작합니다.
        /// </summary>
        /// <param name="experimentName">실험 이름 (파일명에 사용)</param>
        /// <param name="config">컬럼 선택 설정 (null이면 전체 기록)</param>
        /// <returns>생성된 파일 경로</returns>
        public string Start(string experimentName, AutoRunConfiguration config = null)
        {
            if (_isRunning)
                Stop();

            try
            {
                _startTime = DateTime.Now;

                // 컬럼 설정 캡처
                CaptureColumnSettings(config);

                // 파일명 생성: 실험명_yyyyMMdd_HHmmss.csv
                string safeName = SanitizeFileName(experimentName);
                string fileName = $"{safeName}_{_startTime:yyyyMMdd_HHmmss}.csv";

                // 저장 경로: Data/Experiments/
                string directory = Path.Combine(
                    PathSettings.Instance.DataPath, "Experiments");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                _baseDirectory = directory;
                _baseName = safeName;
                _fileIndex = 1;
                _rowCount = 0;

                _filePath = Path.Combine(directory, fileName);

                // UTF-8 BOM으로 생성 (Excel 한글 호환)
                _writer = new StreamWriter(_filePath, false, new UTF8Encoding(true));
                _writer.AutoFlush = true;

                // 메타 정보 기록
                _writer.WriteLine($"# 실험명: {experimentName}");
                _writer.WriteLine($"# 시작 시각: {_startTime:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"#");

                // CSV 헤더
                _writer.WriteLine(string.Join(",", GetHeaders()));

                _isRunning = true;

                AsyncLoggingService.Instance.LogInfo(
                    $"[실험 데이터] 로깅 시작: {_filePath}");

                return _filePath;
            }
            catch (Exception ex)
            {
                AsyncLoggingService.Instance.LogError(
                    $"[실험 데이터] 로깅 시작 실패: {ex.Message}", ex);
                _isRunning = false;
                return null;
            }
        }

        /// <summary>
        /// config에서 컬럼 설정을 캡처합니다.
        /// </summary>
        private void CaptureColumnSettings(AutoRunConfiguration config)
        {
            if (config == null)
            {
                // config 없으면 전체 기록
                _logPressure = true;
                _logValves = true;
                _logDryPump = true;
                _logTurboPump = true;
                _logChiller = true;
                for (int i = 0; i < 12; i++) _logTempCh[i] = true;
                _logAdditionalAI = true;
                return;
            }

            _logPressure = config.LogColumnPressure;
            _logValves = config.LogColumnValves;
            _logDryPump = config.LogColumnDryPump;
            _logTurboPump = config.LogColumnTurboPump;
            _logChiller = config.LogColumnChiller;
            for (int i = 0; i < 12; i++)
                _logTempCh[i] = config.IsLogColumnTempChEnabled(i + 1);
            _logAdditionalAI = config.LogColumnAdditionalAI;
        }

        /// <summary>
        /// UIDataSnapshot을 CSV 행으로 기록합니다.
        /// </summary>
        /// <param name="snapshot">데이터 스냅샷</param>
        /// <param name="autoRunState">현재 AutoRun 상태 (nullable)</param>
        public void LogSnapshot(UIDataSnapshot snapshot, string autoRunState = "")
        {
            if (!_isRunning || _writer == null || snapshot == null)
                return;

            lock (_lock)
            {
                try
                {
                    var elapsed = DateTime.Now - _startTime;
                    var sb = new StringBuilder();

                    // 시간 (항상 기록)
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    sb.Append(',');
                    sb.Append($"{elapsed.TotalSeconds:F1}");
                    sb.Append(',');
                    sb.Append(autoRunState);

                    // 압력
                    if (_logPressure)
                    {
                        sb.Append(','); sb.Append(snapshot.AtmPressure.ToString("F2"));
                        sb.Append(','); sb.Append(snapshot.PiraniPressure.ToString("E3"));
                        sb.Append(','); sb.Append(snapshot.IonPressure.ToString("E3"));
                        sb.Append(','); sb.Append(snapshot.IonGaugeStatus);
                    }

                    // 밸브 상태
                    if (_logValves)
                    {
                        sb.Append(','); sb.Append(snapshot.GateValveStatus);
                        sb.Append(','); sb.Append(snapshot.VentValveStatus);
                        sb.Append(','); sb.Append(snapshot.ExhaustValveStatus);
                        sb.Append(','); sb.Append(snapshot.IonGaugeHVStatus);
                    }

                    // 드라이펌프
                    if (_logDryPump)
                    {
                        sb.Append(','); sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Status : "N/C");
                        sb.Append(','); sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Speed : "");
                        sb.Append(','); sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Current : "");
                        sb.Append(','); sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Temperature : "");
                    }

                    // 터보펌프
                    if (_logTurboPump)
                    {
                        sb.Append(','); sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Status : "N/C");
                        sb.Append(','); sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Speed : "");
                        sb.Append(','); sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Current : "");
                        sb.Append(','); sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Temperature : "");
                    }

                    // 칠러
                    if (_logChiller)
                    {
                        sb.Append(','); sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.Status : "N/C");
                        sb.Append(','); sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.CurrentTemp : "");
                        sb.Append(','); sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.TargetTemp : "");
                    }

                    // 온도 컨트롤러 (선택된 채널만)
                    if (snapshot.Connections.TempController && snapshot.TempController.Channels != null)
                    {
                        var channels = snapshot.TempController.Channels;
                        for (int i = 0; i < 12; i++)
                        {
                            if (!_logTempCh[i]) continue;
                            if (i < channels.Length)
                            {
                                sb.Append(','); sb.Append(channels[i].PresentValue);
                                sb.Append(','); sb.Append(channels[i].SetValue);
                                sb.Append(','); sb.Append(channels[i].HeatingMV?.Replace(" %", ""));
                                sb.Append(','); sb.Append(channels[i].Status);
                            }
                            else
                            {
                                sb.Append(",,,,");
                            }
                        }
                    }
                    else
                    {
                        // 온도 컨트롤러 미연결 시 선택된 채널만큼 빈 칸
                        for (int i = 0; i < 12; i++)
                        {
                            if (!_logTempCh[i]) continue;
                            sb.Append(",,,,");
                        }
                    }

                    // 추가 AI
                    if (_logAdditionalAI)
                    {
                        sb.Append(',');
                        sb.Append(snapshot.AdditionalAIValue.ToString("F6"));
                    }

                    _writer.WriteLine(sb.ToString());
                    _rowCount++;

                    // 15만 행에서 분할
                    if (_rowCount >= MaxRowsPerFile)
                        SplitToNewFile();
                }
                catch (Exception ex)
                {
                    AsyncLoggingService.Instance.LogError(
                        $"[실험 데이터] 기록 오류: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 로깅을 종료하고 파일을 닫습니다.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
                return;

            lock (_lock)
            {
                try
                {
                    var elapsed = DateTime.Now - _startTime;

                    // 종료 메타 정보
                    _writer?.WriteLine($"#");
                    _writer?.WriteLine($"# 종료 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                    _writer?.WriteLine($"# 총 실험 시간: {elapsed:hh\\:mm\\:ss}");

                    _writer?.Flush();
                    _writer?.Dispose();
                    _writer = null;
                    _isRunning = false;

                    AsyncLoggingService.Instance.LogInfo(
                        $"[실험 데이터] 로깅 종료 ({elapsed:hh\\:mm\\:ss}): {_filePath}");
                }
                catch (Exception ex)
                {
                    AsyncLoggingService.Instance.LogError(
                        $"[실험 데이터] 종료 오류: {ex.Message}", ex);
                    _isRunning = false;
                }
            }
        }

        /// <summary>
        /// 현재 파일을 닫고 새 파일로 분할합니다.
        /// </summary>
        private void SplitToNewFile()
        {
            try
            {
                _writer?.Flush();
                _writer?.Dispose();

                _fileIndex++;
                _rowCount = 0;

                string fileName = $"{_baseName}_{_startTime:yyyyMMdd_HHmmss}_Part{_fileIndex}.csv";
                _filePath = Path.Combine(_baseDirectory, fileName);

                _writer = new StreamWriter(_filePath, false, new UTF8Encoding(true));
                _writer.AutoFlush = true;

                _writer.WriteLine($"# 분할 파일 Part {_fileIndex}");
                _writer.WriteLine($"# 원본 시작: {_startTime:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"# 분할 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine($"#");
                _writer.WriteLine(string.Join(",", GetHeaders()));

                AsyncLoggingService.Instance.LogInfo(
                    $"[실험 데이터] 파일 분할 (Part {_fileIndex}): {_filePath}");
            }
            catch (Exception ex)
            {
                AsyncLoggingService.Instance.LogError(
                    $"[실험 데이터] 파일 분할 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 선택된 컬럼에 맞는 CSV 헤더 생성
        /// </summary>
        private string[] GetHeaders()
        {
            var headers = new List<string>
            {
                "Timestamp", "Elapsed(s)", "AutoRunState"
            };

            if (_logPressure)
                headers.AddRange(new[] { "ATM(kPa)", "Pirani(Torr)", "Ion(Torr)", "IonGaugeStatus" });

            if (_logValves)
                headers.AddRange(new[] { "GateValve", "VentValve", "ExhaustValve", "IonGaugeHV" });

            if (_logDryPump)
                headers.AddRange(new[] { "DryPump_Status", "DryPump_Freq", "DryPump_Current", "DryPump_Temp" });

            if (_logTurboPump)
                headers.AddRange(new[] { "TurboPump_Status", "TurboPump_Speed", "TurboPump_Current", "TurboPump_Temp" });

            if (_logChiller)
                headers.AddRange(new[] { "Chiller_Status", "Chiller_CurrentTemp", "Chiller_TargetTemp" });

            for (int i = 0; i < 12; i++)
            {
                if (!_logTempCh[i]) continue;
                int ch = i + 1;
                headers.AddRange(new[] {
                    $"Ch{ch}_PV(°C)", $"Ch{ch}_SV(°C)", $"Ch{ch}_MV(%)", $"Ch{ch}_Status"
                });
            }

            if (_logAdditionalAI)
                headers.Add("AdditionalAI(V)");

            return headers.ToArray();
        }

        /// <summary>
        /// 파일명에 사용할 수 없는 문자 제거
        /// </summary>
        private string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "Experiment";

            char[] invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder();
            foreach (char c in name.Trim())
            {
                sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            string result = sb.ToString();
            return result.Length > 50 ? result.Substring(0, 50) : result;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
