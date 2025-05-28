using System;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// LoggerService 인터페이스 래퍼 (기존 코드 호환성)
    /// AsyncLoggingService를 LoggerService로 래핑합니다.
    /// </summary>
    public class LoggerServiceWrapper : LoggerService
    {
        private readonly AsyncLoggingService _asyncLoggingService;

        public LoggerServiceWrapper(AsyncLoggingService asyncLoggingService)
        {
            _asyncLoggingService = asyncLoggingService;

            // AsyncLoggingService의 LogAdded 이벤트를 LoggerService의 LogAdded 이벤트로 전달
            _asyncLoggingService.LogAdded += (sender, message) => OnLogAdded(message);
        }

        public override void LogInfo(string message)
        {
            _asyncLoggingService.LogInfo(message);
        }

        public override void LogWarning(string message)
        {
            _asyncLoggingService.LogWarning(message);
        }

        public override void LogError(string message)
        {
            _asyncLoggingService.LogError(message);
        }

        public override void LogError(string message, Exception ex)
        {
            _asyncLoggingService.LogError(message, ex);
        }

        public override void LogDebug(string message)
        {
            _asyncLoggingService.LogDebug(message);
        }
    }
}