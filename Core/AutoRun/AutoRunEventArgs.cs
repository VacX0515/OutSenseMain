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
        #region 압력 데이터

        /// <summary>
        /// 현재 압력 (Torr) - 피라니/이온게이지 중 활성화된 값
        /// </summary>
        public double CurrentPressure { get; set; }

        /// <summary>
        /// 대기압 (kPa)
        /// </summary>
        public double AtmPressure { get; set; }

        /// <summary>
        /// 피라니 게이지 압력 (Torr)
        /// </summary>
        public double PiraniPressure { get; set; }

        /// <summary>
        /// 이온 게이지 압력 (Torr)
        /// </summary>
        public double IonPressure { get; set; }

        /// <summary>
        /// 이온게이지 상태
        /// </summary>
        public string IonGaugeStatus { get; set; }

        #endregion

        #region 온도 데이터

        /// <summary>
        /// 칠러 현재 온도 (°C)
        /// </summary>
        public double ChillerTemperature { get; set; }

        /// <summary>
        /// 칠러 설정 온도 (°C)
        /// </summary>
        public double ChillerSetTemperature { get; set; }

        /// <summary>
        /// 히터 CH1 현재 온도 (°C)
        /// </summary>
        public double HeaterCh1Temperature { get; set; }

        /// <summary>
        /// 히터 CH1 설정 온도 (°C)
        /// </summary>
        public double HeaterCh1SetTemperature { get; set; }

        /// <summary>
        /// 히터 CH2 현재 온도 (°C)
        /// </summary>
        public double HeaterCh2Temperature { get; set; }

        /// <summary>
        /// 히터 CH2 설정 온도 (°C)
        /// </summary>
        public double HeaterCh2SetTemperature { get; set; }

        #endregion

        #region 펌프 상태

        /// <summary>
        /// 드라이펌프 작동 여부
        /// </summary>
        public bool IsDryPumpRunning { get; set; }

        /// <summary>
        /// 드라이펌프 상태
        /// </summary>
        public string DryPumpStatus { get; set; }

        /// <summary>
        /// 터보펌프 작동 여부
        /// </summary>
        public bool IsTurboPumpRunning { get; set; }

        /// <summary>
        /// 터보펌프 속도 (RPM)
        /// </summary>
        public int TurboPumpSpeed { get; set; }

        /// <summary>
        /// 터보펌프 상태
        /// </summary>
        public string TurboPumpStatus { get; set; }

        #endregion

        #region 밸브 상태

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

        #endregion

        #region 기타

        /// <summary>
        /// 측정 시간
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 전체 시스템 상태 정상 여부
        /// </summary>
        public bool IsSystemNormal { get; set; }

        /// <summary>
        /// 경고 메시지 목록
        /// </summary>
        public List<string> Warnings { get; set; }

        #endregion

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
            IonGaugeStatus = "Unknown";
            Warnings = new List<string>();
            IsSystemNormal = true;
        }

        #region CSV 출력 메서드

        /// <summary>
        /// CSV 헤더 생성
        /// </summary>
        public static string GetCsvHeader()
        {
            return "Timestamp,AtmPressure_kPa,CurrentPressure_Torr,PiraniPressure_Torr,IonPressure_Torr," +
                   "IonGaugeStatus,ChillerTemp_C,ChillerSetTemp_C,HeaterCh1Temp_C,HeaterCh1SetTemp_C," +
                   "HeaterCh2Temp_C,HeaterCh2SetTemp_C,GateValve,VentValve,ExhaustValve," +
                   "DryPumpRunning,DryPumpStatus,TurboPumpRunning,TurboPumpSpeed_RPM,TurboPumpStatus," +
                   "SystemNormal,Warnings";
        }

        /// <summary>
        /// CSV 데이터 행 생성
        /// </summary>
        public string ToCsvRow()
        {
            return $"{Timestamp:yyyy-MM-dd HH:mm:ss}," +
                   $"{AtmPressure:F2}," +
                   $"{CurrentPressure:E2}," +
                   $"{PiraniPressure:E2}," +
                   $"{IonPressure:E2}," +
                   $"{IonGaugeStatus}," +
                   $"{ChillerTemperature:F1}," +
                   $"{ChillerSetTemperature:F1}," +
                   $"{HeaterCh1Temperature:F1}," +
                   $"{HeaterCh1SetTemperature:F1}," +
                   $"{HeaterCh2Temperature:F1}," +
                   $"{HeaterCh2SetTemperature:F1}," +
                   $"{GateValveStatus}," +
                   $"{VentValveStatus}," +
                   $"{ExhaustValveStatus}," +
                   $"{IsDryPumpRunning}," +
                   $"{DryPumpStatus}," +
                   $"{IsTurboPumpRunning}," +
                   $"{TurboPumpSpeed}," +
                   $"{TurboPumpStatus}," +
                   $"{IsSystemNormal}," +
                   $"\"{string.Join(";", Warnings)}\"";
        }

        /// <summary>
        /// 요약 문자열 생성
        /// </summary>
        public override string ToString()
        {
            return $"[{Timestamp:HH:mm:ss}] " +
                   $"압력: {CurrentPressure:E2} Torr, " +
                   $"CH1: {HeaterCh1Temperature:F1}°C, " +
                   $"CH2: {HeaterCh2Temperature:F1}°C, " +
                   $"터보: {TurboPumpSpeed} RPM";
        }

        #endregion
    }
}