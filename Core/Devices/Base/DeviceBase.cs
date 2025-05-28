using System;
using System.ComponentModel;
using System.Threading.Tasks;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;

namespace VacX_OutSense.Core.Devices.Base
{
    /// <summary>
    /// 장치 구현의 기본 추상 클래스입니다.
    /// 모든 장치 클래스는 이 클래스를 상속하여 공통 기능을 활용할 수 있습니다.
    /// </summary>
    public abstract class DeviceBase : IDevice, INotifyPropertyChanged
    {
        #region 필드 및 속성

        private bool _disposed;
        private bool _isConnected;
        private readonly object _lockObject = new object();

        /// <summary>
        /// 통신 관리자 인스턴스
        /// </summary>
        protected ICommunicationManager _communicationManager;

        /// <summary>
        /// 장치 상태 변경 이벤트
        /// </summary>
        public event EventHandler<DeviceStatusEventArgs> StatusChanged;

        /// <summary>
        /// 장치 오류 이벤트
        /// </summary>
        public event EventHandler<string> ErrorOccurred;

        /// <summary>
        /// 장치 이름
        /// </summary>
        public abstract string DeviceName { get; }

        /// <summary>
        /// 장치 모델
        /// </summary>
        public abstract string Model { get; }

        /// <summary>
        /// 장치 고유 ID 또는 일련번호
        /// </summary>
        public virtual string DeviceId { get; protected set; }

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get
            {
                lock (_lockObject)
                {
                    return _isConnected && _communicationManager != null && _communicationManager.IsConnected;
                }
            }
            protected set
            {
                lock (_lockObject)
                {
                    if (_isConnected != value)
                    {
                        _isConnected = value;
                        OnPropertyChanged(nameof(IsConnected)); // PropertyChanged 이벤트 발생
                    }
                }
            }
        }

        // INotifyPropertyChanged 구현
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// PropertyChanged 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="propertyName">변경된 속성의 이름</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 장치와 통신하는데 사용되는 통신 관리자
        /// </summary>
        public ICommunicationManager CommunicationManager => _communicationManager;

        #endregion

        #region 생성자 및 소멸자

        /// <summary>
        /// DeviceBase 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="communicationManager">통신 관리자 인스턴스</param>
        protected DeviceBase(ICommunicationManager communicationManager)
        {
            _communicationManager = communicationManager ?? throw new ArgumentNullException(nameof(communicationManager));
            _isConnected = false;
            _disposed = false;

            // 통신 관리자 이벤트 등록
            _communicationManager.StatusChanged += CommunicationManager_StatusChanged;
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 리소스를 해제합니다.
        /// </summary>
        /// <param name="disposing">관리되는 리소스를 해제해야 하면 true입니다.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 이벤트 구독 해제
                    if (_communicationManager != null)
                    {
                        _communicationManager.StatusChanged -= CommunicationManager_StatusChanged;
                    }

                    // 연결 해제
                    Disconnect();

                    // 통신 관리자는 공유 리소스이므로 여기서 해제하지 않음
                    // 필요한 경우 파생 클래스에서 처리
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// 통신 관리자 상태 변경 이벤트 핸들러
        /// </summary>
        private void CommunicationManager_StatusChanged(object sender, CommunicationStatusEventArgs e)
        {
            // 통신 관리자 연결 상태가 변경되면 장치 연결 상태도 업데이트
            if (!e.IsConnected && IsConnected)
            {
                IsConnected = false;
                OnStatusChanged(new DeviceStatusEventArgs(false, DeviceId, $"통신 상태 변경: {e.StatusMessage}", DeviceStatusCode.Disconnected));
            }

            // 통신 오류가 발생하면 오류 이벤트 발생
            if (!e.IsConnected && e.Exception != null)
            {
                OnErrorOccurred(e.StatusMessage);
            }
        }

        #endregion

        #region IDevice 구현

        /// <summary>
        /// 장치에 연결합니다.
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>연결 성공 여부</returns>
        public virtual bool Connect(string connectionId, CommunicationSettings settings)
        {
            if (_disposed)
                throw new ObjectDisposedException(DeviceName);

            try
            {
                // 이미 연결된 경우 먼저 연결 해제
                if (IsConnected)
                {
                    Disconnect();
                }

                // 연결 시도 전 잠시 대기 (이전 연결의 리소스가 완전히 해제되도록)
                Thread.Sleep(200);

                // 통신 관리자를 통해 연결
                bool connected = _communicationManager.Connect(connectionId, settings);
                if (connected)
                {
                    // 연결 안정화를 위한 짧은 지연
                    Thread.Sleep(300);

                    IsConnected = true;
                    OnStatusChanged(new DeviceStatusEventArgs(true, DeviceId, $"{DeviceName}에 연결되었습니다.", DeviceStatusCode.Connected));

                    // 장치별 연결 후 초기화 작업 수행 전 추가 지연
                    Thread.Sleep(200);

                    // 재시도 메커니즘으로 초기화 작업
                    const int maxRetries = 3;
                    bool initSuccess = false;

                    for (int retry = 0; retry < maxRetries; retry++)
                    {
                        try
                        {
                            // 장치별 연결 후 초기화 작업 수행
                            InitializeAfterConnection();
                            initSuccess = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (retry < maxRetries - 1)
                            {
                                OnErrorOccurred($"초기화 재시도 중 ({retry + 1}/{maxRetries}): {ex.Message}");
                                Thread.Sleep(300); // 재시도 전 대기
                            }
                            else
                            {
                                OnErrorOccurred($"초기화 실패: {ex.Message}");
                            }
                        }
                    }

                    // 초기화 실패 시에도 연결은 유지하고 경고만 표시
                    if (!initSuccess)
                    {
                        OnErrorOccurred($"{DeviceName} 연결은 성공했으나 초기화에 실패했습니다. 일부 기능이 제한될 수 있습니다.");
                    }

                    return true;
                }
                else
                {
                    OnErrorOccurred($"{DeviceName}에 연결하지 못했습니다.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred($"연결 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 장치에 연결합니다. (기본 설정 사용)
        /// </summary>
        /// <param name="connectionId">연결 ID (예: 포트 이름, IP 주소)</param>
        /// <returns>연결 성공 여부</returns>
        public virtual bool Connect(string connectionId)
        {
            return Connect(connectionId, new CommunicationSettings());
        }

        /// <summary>
        /// 장치 연결을 해제합니다.
        /// </summary>
        public virtual void Disconnect()
        {
            if (_disposed)
                throw new ObjectDisposedException(DeviceName);

            if (IsConnected)
            {
                try
                {
                    // 장치별 연결 해제 전 정리 작업 수행
                    CleanupBeforeDisconnection();

                    // 연결 상태 업데이트
                    IsConnected = false;
                    OnStatusChanged(new DeviceStatusEventArgs(false, DeviceId, $"{DeviceName} 연결이 해제되었습니다.", DeviceStatusCode.Disconnected));

                    // 참고: 실제 통신 포트는 닫지 않음 (다른 장치에서 사용 중일 수 있음)
                    // _communicationManager.Disconnect();
                }
                catch (Exception ex)
                {
                    OnErrorOccurred($"연결 해제 실패: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 장치 상태를 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true, 그렇지 않으면 false</returns>
        public abstract bool CheckStatus();

        /// <summary>
        /// 장치 상태를 비동기적으로 확인합니다.
        /// </summary>
        /// <returns>장치가 정상 작동 중이면 true, 그렇지 않으면 false를 포함하는 태스크</returns>
        public virtual async Task<bool> CheckStatusAsync()
        {
            return await Task.Run(() => CheckStatus());
        }

        #endregion

        #region 보호된 메서드

        /// <summary>
        /// 연결 후 초기화 작업을 수행합니다.
        /// 파생 클래스에서 재정의하여 장치별 초기화 작업을 구현할 수 있습니다.
        /// </summary>
        protected virtual void InitializeAfterConnection()
        {
            // 기본 구현 없음 - 파생 클래스에서 필요에 따라 재정의
        }

        /// <summary>
        /// 연결 해제 전 정리 작업을 수행합니다.
        /// 파생 클래스에서 재정의하여 장치별 정리 작업을 구현할 수 있습니다.
        /// </summary>
        protected virtual void CleanupBeforeDisconnection()
        {
            // 기본 구현 없음 - 파생 클래스에서 필요에 따라 재정의
        }

        /// <summary>
        /// 장치 상태 변경 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="e">이벤트 인자</param>
        protected virtual void OnStatusChanged(DeviceStatusEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 장치 오류 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="errorMessage">오류 메시지</param>
        protected virtual void OnErrorOccurred(string errorMessage)
        {
            ErrorOccurred?.Invoke(this, errorMessage);
        }

        /// <summary>
        /// 장치가 연결되어 있지 않으면 예외를 발생시킵니다.
        /// </summary>
        protected void EnsureConnected()
        {
            if (_disposed)
                throw new ObjectDisposedException(DeviceName);

            if (!IsConnected)
                MessageBox.Show($"{DeviceName}이(가) 연결되지 않았습니다.", "Error", MessageBoxButtons.OK);
        }

        /// <summary>
        /// 통신 시간 초과 값을 설정합니다.
        /// </summary>
        /// <param name="timeout">시간 초과 (밀리초)</param>
        protected void SetTimeout(int timeout)
        {
            if (_disposed)
                throw new ObjectDisposedException(DeviceName);

            if (_communicationManager != null)
            {
                _communicationManager.SetTimeout(timeout);
            }
        }

        #endregion
    }
}