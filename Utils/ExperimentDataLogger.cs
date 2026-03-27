using System;
using System.IO;
using System.Text;
using VacX_OutSense.Models;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// AutoRun 실험 통합 데이터 로거.
    /// 실험 중 모든 센서 데이터를 하나의 CSV 파일에 기록합니다.
    /// 
    /// 사용법:
    ///   var logger = new ExperimentDataLogger();
    ///   logger.Start("실험명");           // 파일 생성 + 헤더 기록
    ///   logger.LogSnapshot(snapshot, "RunningExperiment");  // 데이터 행 기록
    ///   logger.Stop();                    // 파일 닫기
    /// </summary>
    public class ExperimentDataLogger : IDisposable
    {
        private StreamWriter _writer;
        private string _filePath;
        private readonly object _lock = new object();
        private bool _isRunning;
        private DateTime _startTime;

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
        /// <returns>생성된 파일 경로</returns>
        public string Start(string experimentName)
        {
            if (_isRunning)
                Stop();

            try
            {
                _startTime = DateTime.Now;

                // 파일명 생성: 실험명_yyyyMMdd_HHmmss.csv
                string safeName = SanitizeFileName(experimentName);
                string fileName = $"{safeName}_{_startTime:yyyyMMdd_HHmmss}.csv";

                // 저장 경로: Data/Experiments/
                string directory = Path.Combine(
                    PathSettings.Instance.DataPath, "Experiments");

                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

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

                    // 시간
                    sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    sb.Append(',');
                    sb.Append($"{elapsed.TotalSeconds:F1}");
                    sb.Append(',');
                    sb.Append(autoRunState);

                    // 압력
                    sb.Append(',');
                    sb.Append(snapshot.AtmPressure.ToString("F2"));
                    sb.Append(',');
                    sb.Append(snapshot.PiraniPressure.ToString("E3"));
                    sb.Append(',');
                    sb.Append(snapshot.IonPressure.ToString("E3"));
                    sb.Append(',');
                    sb.Append(snapshot.IonGaugeStatus);

                    // 밸브 상태
                    sb.Append(',');
                    sb.Append(snapshot.GateValveStatus);
                    sb.Append(',');
                    sb.Append(snapshot.VentValveStatus);
                    sb.Append(',');
                    sb.Append(snapshot.ExhaustValveStatus);
                    sb.Append(',');
                    sb.Append(snapshot.IonGaugeHVStatus);

                    // 드라이펌프
                    sb.Append(',');
                    sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Status : "N/C");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Speed : "");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Current : "");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.DryPump ? snapshot.DryPump.Temperature : "");

                    // 터보펌프
                    sb.Append(',');
                    sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Status : "N/C");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Speed : "");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Current : "");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.TurboPump ? snapshot.TurboPump.Temperature : "");

                    // 칠러
                    sb.Append(',');
                    sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.Status : "N/C");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.CurrentTemp : "");
                    sb.Append(',');
                    sb.Append(snapshot.Connections.BathCirculator ? snapshot.BathCirculator.TargetTemp : "");

                    // 온도 컨트롤러 (5채널)
                    if (snapshot.Connections.TempController && snapshot.TempController.Channels != null)
                    {
                        var channels = snapshot.TempController.Channels;

                        // CH1 (PV, SV, MV, Status)
                        if (channels.Length > 0)
                        {
                            sb.Append(','); sb.Append(channels[0].PresentValue);
                            sb.Append(','); sb.Append(channels[0].SetValue);
                            sb.Append(','); sb.Append(channels[0].HeatingMV?.Replace(" %", ""));
                            sb.Append(','); sb.Append(channels[0].Status);
                        }
                        else
                        {
                            sb.Append(",,,,");
                        }

                        // CH2 (PV, SV, MV, Status)
                        if (channels.Length > 1)
                        {
                            sb.Append(','); sb.Append(channels[1].PresentValue);
                            sb.Append(','); sb.Append(channels[1].SetValue);
                            sb.Append(','); sb.Append(channels[1].HeatingMV?.Replace(" %", ""));
                            sb.Append(','); sb.Append(channels[1].Status);
                        }
                        else
                        {
                            sb.Append(",,,,");
                        }

                        // CH3~8 (PV, SV, MV, Status)
                        for (int i = 2; i < 8; i++)
                        {
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
                        // 온도 컨트롤러 미연결 시 빈 칸 (8채널 × 4컬럼)
                        for (int i = 0; i < 32; i++) sb.Append(',');
                    }

                    // 추가 AI
                    sb.Append(',');
                    sb.Append(snapshot.AdditionalAIValue.ToString("F6"));

                    _writer.WriteLine(sb.ToString());
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
        /// CSV 헤더 배열
        /// </summary>
        private string[] GetHeaders()
        {
            return new[]
            {
                // 시간
                "Timestamp", "Elapsed(s)", "AutoRunState",

                // 압력
                "ATM(kPa)", "Pirani(Torr)", "Ion(Torr)", "IonGaugeStatus",

                // 밸브
                "GateValve", "VentValve", "ExhaustValve", "IonGaugeHV",

                // 드라이펌프
                "DryPump_Status", "DryPump_Freq", "DryPump_Current", "DryPump_Temp",

                // 터보펌프
                "TurboPump_Status", "TurboPump_Speed", "TurboPump_Current", "TurboPump_Temp",

                // 칠러
                "Chiller_Status", "Chiller_CurrentTemp", "Chiller_TargetTemp",

                // 온도 컨트롤러 (메인 4 + 확장 4)
                "M1_PV(°C)", "M1_SV(°C)", "M1_MV(%)", "M1_Status",
                "M2_PV(°C)", "M2_SV(°C)", "M2_MV(%)", "M2_Status",
                "M3_PV(°C)", "M3_SV(°C)", "M3_MV(%)", "M3_Status",
                "M4_PV(°C)", "M4_SV(°C)", "M4_MV(%)", "M4_Status",
                "E1_PV(°C)", "E1_SV(°C)", "E1_MV(%)", "E1_Status",
                "E2_PV(°C)", "E2_SV(°C)", "E2_MV(%)", "E2_Status",
                "E3_PV(°C)", "E3_SV(°C)", "E3_MV(%)", "E3_Status",
                "E4_PV(°C)", "E4_SV(°C)", "E4_MV(%)", "E4_Status",

                // 추가
                "AdditionalAI(V)"
            };
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