using System;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;

namespace VacX_OutSense.Core.Devices.Base
{
    /// <summary>
    /// 모든 장치 클래스가 구현해야 하는 인터페이스입니다.
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// 장치 상태 변경 이벤트
        /// </summary>
        event EventHandler<DeviceStatusEventArgs> StatusChanged;

        /// <summary>
        /// 장치 오류 이벤트
        /// </summary>
        event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// 장치 이름
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// 장치 모델
        /// </summary>
        string Model { get; }

        /// <summary>
        /// 장치 고유 ID 또는 일련번호
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 장치와 통신하는데 사용되는 통신 관리자
        /// </summary>
        ICommunicationManager CommunicationManager { get; }

        /// <summary>
        /// 연결 상태
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 장치에 연결합니다.
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>연결 성공 여부</returns>
        bool Connect(string connectionId, CommunicationSettings settings);

        /// <summary>
        /// 장치에 연결합니다. (기본 설정 사용)
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <returns>연결 성공 여부</returns>
        bool Connect(string connectionId);

        /// <summary>
        /// 장치 연결을 해제합니다.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 장치 상태를 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true, 그렇지 않으면 false</returns>
        bool CheckStatus();

        /// <summary>
        /// 장치 상태를 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true, 그렇지 않으면 false를 포함하는 태스크</returns>
        Task<bool> CheckStatusAsync();
    }
}