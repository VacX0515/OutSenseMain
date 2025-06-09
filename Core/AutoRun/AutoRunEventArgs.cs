using System;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// AutoRun 상태 변경 이벤트 인자
    /// </summary>
    public class AutoRunStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 이전 상태
        /// </summary>
        public AutoRunState PreviousState { get; set; }

        /// <summary>
        /// 현재 상태
        /// </summary>
        public AutoRunState CurrentState { get; set; }

        /// <summary>
        /// 상태 변경 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 상태 설명 메시지
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 전체 진행률 (0-100)
        /// </summary>
        public double OverallProgress { get; set; }

        /// <summary>
        /// 현재 단계 진행률 (0-100)
        /// </summary>
        public double StepProgress { get; set; }

        /// <summary>
        /// 시작 후 경과 시간
        /// </summary>
        public TimeSpan ElapsedTime { get; set; }

        /// <summary>
        /// 예상 남은 시간
        /// </summary>
        public TimeSpan? EstimatedTimeRemaining { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunStateChangedEventArgs(AutoRunState previousState, AutoRunState currentState, string message = "")
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Timestamp = DateTime.Now;
            Message = message;
        }
    }

    /// <summary>
    /// AutoRun 진행 상황 업데이트 이벤트 인자
    /// </summary>
    public class AutoRunProgressEventArgs : EventArgs
    {
        /// <summary>
        /// 현재 상태
        /// </summary>
        public AutoRunState CurrentState { get; set; }

        /// <summary>
        /// 진행 상황 메시지
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 현재 단계 진행률 (0-100)
        /// </summary>
        public double StepProgress { get; set; }

        /// <summary>
        /// 전체 진행률 (0-100)
        /// </summary>
        public double OverallProgress { get; set; }

        /// <summary>
        /// 현재 측정값들
        /// </summary>
        public AutoRunMeasurements CurrentValues { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunProgressEventArgs(AutoRunState state, string message, double stepProgress, double overallProgress)
        {
            CurrentState = state;
            Message = message;
            StepProgress = stepProgress;
            OverallProgress = overallProgress;
            CurrentValues = new AutoRunMeasurements();
        }
    }

    /// <summary>
    /// AutoRun 오류 이벤트 인자
    /// </summary>
    public class AutoRunErrorEventArgs : EventArgs
    {
        /// <summary>
        /// 오류 발생 상태
        /// </summary>
        public AutoRunState ErrorState { get; set; }

        /// <summary>
        /// 오류 메시지
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 예외 정보
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// 재시도 가능 여부
        /// </summary>
        public bool IsRetryable { get; set; }

        /// <summary>
        /// 현재 재시도 횟수
        /// </summary>
        public int RetryCount { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunErrorEventArgs(AutoRunState state, string message, Exception ex = null, bool retryable = false)
        {
            ErrorState = state;
            ErrorMessage = message;
            Exception = ex;
            IsRetryable = retryable;
        }
    }

    /// <summary>
    /// AutoRun 완료 이벤트 인자
    /// </summary>
    public class AutoRunCompletedEventArgs : EventArgs
    {
        /// <summary>
        /// 완료 상태
        /// </summary>
        public AutoRunState CompletionState { get; set; }

        /// <summary>
        /// 성공 여부
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 시작 시간
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 종료 시간
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 총 소요 시간
        /// </summary>
        public TimeSpan TotalDuration { get; set; }

        /// <summary>
        /// 실행 요약 보고서
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// 로그 파일 경로
        /// </summary>
        public string LogFilePath { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunCompletedEventArgs(bool success, DateTime startTime, DateTime endTime)
        {
            IsSuccess = success;
            StartTime = startTime;
            EndTime = endTime;
            TotalDuration = endTime - startTime;
            CompletionState = success ? AutoRunState.Completed : AutoRunState.Error;
        }
    }

    /// <summary>
    /// AutoRun 현재 측정값
    /// </summary>
    public class AutoRunMeasurements
    {
        /// <summary>
        /// 현재 압력 (Torr)
        /// </summary>
        public double CurrentPressure { get; set; }

        /// <summary>
        /// 대기압 (kPa)
        /// </summary>
        public double AtmPressure { get; set; }

        /// <summary>
        /// 칠러 현재 온도 (°C)
        /// </summary>
        public double ChillerTemperature { get; set; }

        /// <summary>
        /// 히터 CH1 현재 온도 (°C)
        /// </summary>
        public double HeaterCh1Temperature { get; set; }

        /// <summary>
        /// 히터 CH2 현재 온도 (°C)
        /// </summary>
        public double HeaterCh2Temperature { get; set; }

        /// <summary>
        /// 드라이펌프 상태
        /// </summary>
        public string DryPumpStatus { get; set; }

        /// <summary>
        /// 터보펌프 속도 (RPM)
        /// </summary>
        public int TurboPumpSpeed { get; set; }

        /// <summary>
        /// 터보펌프 상태
        /// </summary>
        public string TurboPumpStatus { get; set; }

        /// <summary>
        /// 게이트 밸브 상태
        /// </summary>
        public string GateValveStatus { get; set; }

        /// <summary>
        /// 벤트 밸브 상태
        /// </summary>
        public string VentValveStatus { get; set; }

        /// <summary>
        /// 배기 밸브 상태
        /// </summary>
        public string ExhaustValveStatus { get; set; }

        /// <summary>
        /// 측정 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        public AutoRunMeasurements()
        {
            Timestamp = DateTime.Now;
            DryPumpStatus = "Unknown";
            TurboPumpStatus = "Unknown";
            GateValveStatus = "Unknown";
            VentValveStatus = "Unknown";
            ExhaustValveStatus = "Unknown";
        }
    }
}