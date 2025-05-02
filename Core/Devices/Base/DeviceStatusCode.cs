using System;

namespace VacX_OutSense.Core.Devices.Base
{
    /// <summary>
    /// 장치 상태 코드
    /// </summary>
    public enum DeviceStatusCode
    {
        /// <summary>
        /// 알 수 없음
        /// </summary>
        Unknown = 0,
        /// <summary>
        /// 연결됨
        /// </summary>
        Connected = 1,
        /// <summary>
        /// 연결 해제됨
        /// </summary>
        Disconnected = 2,
        /// <summary>
        /// 오류
        /// </summary>
        Error = 3,
        /// <summary>
        /// 초기화 중
        /// </summary>
        Initializing = 4,
        /// <summary>
        /// 초기화 완료
        /// </summary>
        Initialized = 5,
        /// <summary>
        /// 대기 중
        /// </summary>
        Idle = 6,
        /// <summary>
        /// 작업 중
        /// </summary>
        Busy = 7,
        /// <summary>
        /// 준비됨
        /// </summary>
        Ready = 8,
        /// <summary>
        /// 실행 중
        /// </summary>
        Running = 9,
        /// <summary>
        /// 대기 모드
        /// </summary>
        Standby = 10,
        /// <summary>
        /// 경고
        /// </summary>
        Warning = 11
    }

    /// <summary>
    /// 장치 상태 변경 이벤트 인자
    /// </summary>
    public class DeviceStatusEventArgs : EventArgs
    {
        /// <summary>
        /// 장치가 연결되어 있는지 여부
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 장치 식별자
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// 상태 메시지
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// 상태 코드
        /// </summary>
        public DeviceStatusCode StatusCode { get; }

        /// <summary>
        /// DeviceStatusEventArgs 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="isConnected">장치가 연결되어 있는지 여부</param>
        /// <param name="deviceId">장치 식별자</param>
        /// <param name="statusMessage">상태 메시지</param>
        /// <param name="statusCode">상태 코드</param>
        public DeviceStatusEventArgs(bool isConnected, string deviceId, string statusMessage, DeviceStatusCode statusCode)
        {
            IsConnected = isConnected;
            DeviceId = deviceId;
            StatusMessage = statusMessage;
            StatusCode = statusCode;
        }
    }
}

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 통신 상태 변경 이벤트 인자
    /// </summary>
    public class CommunicationStatusEventArgs : EventArgs
    {
        /// <summary>
        /// 연결되어 있는지 여부
        /// </summary>
        public bool IsConnected { get; }

        /// <summary>
        /// 상태 메시지
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// 발생한 예외 (없으면 null)
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// CommunicationStatusEventArgs 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="isConnected">연결되어 있는지 여부</param>
        /// <param name="statusMessage">상태 메시지</param>
        /// <param name="exception">발생한 예외 (없으면 null)</param>
        public CommunicationStatusEventArgs(bool isConnected, string statusMessage, Exception exception = null)
        {
            IsConnected = isConnected;
            StatusMessage = statusMessage;
            Exception = exception;
        }
    }
}