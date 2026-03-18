using System;
using System.IO;
using System.Xml.Serialization;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// 오토런 진행 상태 스냅샷 — 프로그램 재시작 시 이어하기 위한 상태 저장
    /// </summary>
    [Serializable]
    public class AutoRunStateSnapshot
    {
        /// <summary>현재 실행 중인 단계 (1~9)</summary>
        public int CurrentStepNumber { get; set; }

        /// <summary>오토런 시작 시각</summary>
        public DateTime StartTime { get; set; }

        /// <summary>실험(홀드) 시작 시각 (목표온도 도달 후)</summary>
        public DateTime ExperimentStartTime { get; set; }

        /// <summary>실험 타이머가 카운트 중이었는지</summary>
        public bool IsExperimentTimerRunning { get; set; }

        /// <summary>실험 유형</summary>
        public ExperimentType ExperimentType { get; set; }

        /// <summary>실험 데이터 파일 이름</summary>
        public string ExperimentName { get; set; } = "";

        /// <summary>스냅샷 저장 시각</summary>
        public DateTime SavedAt { get; set; }

        /// <summary>UI 경과 시간 (초)</summary>
        public int AutoRunElapsedSeconds { get; set; }

        /// <summary>실험 경과 시간 (초)</summary>
        public int ExperimentElapsedSeconds { get; set; }

        #region 저장/로드

        private static string FilePath =>
            Path.Combine(PathSettings.Instance.ConfigPath, "AutoRunState.xml");

        /// <summary>
        /// 상태를 파일로 저장
        /// </summary>
        public void Save()
        {
            try
            {
                SavedAt = DateTime.Now;
                string dir = Path.GetDirectoryName(FilePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var serializer = new XmlSerializer(typeof(AutoRunStateSnapshot));
                using (var writer = new StreamWriter(FilePath))
                    serializer.Serialize(writer, this);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogDebug($"[AutoRunState] 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 저장된 상태가 있으면 로드
        /// </summary>
        public static AutoRunStateSnapshot Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                    return null;

                var serializer = new XmlSerializer(typeof(AutoRunStateSnapshot));
                using (var reader = new StreamReader(FilePath))
                    return (AutoRunStateSnapshot)serializer.Deserialize(reader);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 저장된 상태 파일 삭제 (정상 종료/중단 시)
        /// </summary>
        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath))
                    File.Delete(FilePath);
            }
            catch { }
        }

        /// <summary>
        /// 저장 후 경과 시간이 너무 오래되었는지 (24시간 초과)
        /// </summary>
        public bool IsExpired => (DateTime.Now - SavedAt).TotalHours > 24;

        /// <summary>
        /// 사용자에게 보여줄 요약 텍스트
        /// </summary>
        public string GetSummaryText()
        {
            var elapsed = DateTime.Now - SavedAt;
            string stepName = SystemStateAssessment.GetStepName(CurrentStepNumber);
            string expType = ExperimentType == ExperimentType.Bakeout ? "베이크아웃" : "탈가스율";

            return $"═══ 이전 오토런 진행 상태 ═══\n\n" +
                   $"  실험 유형:    {expType}\n" +
                   $"  실험명:       {ExperimentName}\n" +
                   $"  진행 단계:    {CurrentStepNumber}단계 ({stepName})\n" +
                   $"  시작 시각:    {StartTime:yyyy-MM-dd HH:mm:ss}\n" +
                   (IsExperimentTimerRunning
                       ? $"  실험 시작:    {ExperimentStartTime:yyyy-MM-dd HH:mm:ss}\n"
                       : "") +
                   $"  중단 시각:    {SavedAt:yyyy-MM-dd HH:mm:ss}\n" +
                   $"  경과 시간:    {elapsed.Hours}시간 {elapsed.Minutes}분 전\n";
        }

        #endregion
    }
}
