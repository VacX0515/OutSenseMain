using System;

namespace VacX_OutSense.Core.Communication.Interfaces
{
    /// <summary>
    /// 통신 관리자를 위한 인터페이스입니다.
    /// 이 인터페이스는 시리얼, 이더넷 등 다양한 통신 방식에 구현할 수 있습니다.
    /// </summary>
    public interface ICommunicationManager
    {
        /// <summary>
        /// 통신 상태 변경 이벤트
        /// </summary>
        event EventHandler<CommunicationStatusEventArgs> StatusChanged;

        /// <summary>
        /// 데이터 수신 이벤트
        /// </summary>
        event EventHandler<byte[]> DataReceived;

        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 연결 ID (예: 포트 이름, IP 주소)
        /// </summary>
        string ConnectionId { get; }

        /// <summary>
        /// 통신 설정
        /// </summary>
        CommunicationSettings Settings { get; }

        /// <summary>
        /// 통신 포트에 연결합니다.
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>연결 성공 여부</returns>
        bool Connect(string connectionId, CommunicationSettings settings);

        /// <summary>
        /// 통신 포트에 연결합니다. (기본 설정 사용)
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <returns>연결 성공 여부</returns>
        bool Connect(string connectionId);

        /// <summary>
        /// 통신 포트 연결을 해제합니다.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 데이터를 전송합니다.
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="count">바이트 수</param>
        /// <returns>전송 성공 여부</returns>
        bool Write(byte[] buffer, int offset, int count);

        /// <summary>
        /// 데이터를 전송합니다.
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <returns>전송 성공 여부</returns>
        bool Write(byte[] buffer);

        /// <summary>
        /// 모든 가용한 데이터를 읽습니다.
        /// </summary>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        byte[] ReadAll();

        /// <summary>
        /// 지정된 길이만큼 데이터를 읽습니다.
        /// </summary>
        /// <param name="count">읽을 바이트 수</param>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        byte[] Read(int count);

        /// <summary>
        /// 시간 초과 값을 설정합니다.
        /// </summary>
        /// <param name="timeout">시간 초과 (밀리초)</param>
        void SetTimeout(int timeout);

        /// <summary>
        /// 입력 버퍼를 비웁니다.
        /// </summary>
        void DiscardInBuffer();

        /// <summary>
        /// 출력 버퍼를 비웁니다.
        /// </summary>
        void DiscardOutBuffer();
    }
}