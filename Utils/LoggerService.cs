using System;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 로깅 서비스의 추상 기본 클래스
    /// 기존 코드와의 호환성을 위해 유지
    /// </summary>
    public abstract class LoggerService
    {
        private static LoggerService _instance;

        /// <summary>
        /// LoggerService의 싱글톤 인스턴스
        /// </summary>
        public static LoggerService Instance
        {
            get => _instance;
            set => _instance = value;
        }

        /// <summary>
        /// 정보 로그를 기록합니다.
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public abstract void LogInfo(string message);

        /// <summary>
        /// 경고 로그를 기록합니다.
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public abstract void LogWarning(string message);

        /// <summary>
        /// 오류 로그를 기록합니다.
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public abstract void LogError(string message);

        /// <summary>
        /// 오류 로그를 기록합니다. (예외 포함)
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="ex">예외 객체</param>
        public abstract void LogError(string message, Exception ex);

        /// <summary>
        /// 디버그 로그를 기록합니다.
        /// </summary>
        /// <param name="message">로그 메시지</param>
        public abstract void LogDebug(string message);

        /// <summary>
        /// 로그 추가 이벤트
        /// </summary>
        public event EventHandler<string> LogAdded;

        /// <summary>
        /// 로그 추가 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="logMessage">로그 메시지</param>
        protected virtual void OnLogAdded(string logMessage)
        {
            LogAdded?.Invoke(this, logMessage);
        }
    }
}