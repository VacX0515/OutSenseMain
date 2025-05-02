using System;
using System.Collections.Generic;
using VacX_OutSense.Core.Communication;
using VacX_OutSense.Core.Communication.Interfaces;

namespace VacX_OutSense.Core.Devices.Base
{
    /// <summary>
    /// 멀티포트 시리얼 매니저를 ICommunicationManager로 변환하는 어댑터 클래스
    /// </summary>
    public class DevicePortAdapter : ICommunicationManager
    {
        private readonly MultiPortSerialManager _multiPortManager;
        private readonly string _portName;
        private bool _isConnected;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected
        {
            get { return _isConnected && _multiPortManager.IsPortConnected(_portName); }
            private set { _isConnected = value; }
        }

        /// <summary>
        /// 연결 ID (포트 이름)
        /// </summary>
        public string ConnectionId => _portName;

        /// <summary>
        /// 통신 설정
        /// </summary>
        public CommunicationSettings Settings { get; private set; }

        /// <summary>
        /// 통신 상태 변경 이벤트
        /// </summary>
        public event EventHandler<CommunicationStatusEventArgs> StatusChanged;

        /// <summary>
        /// 데이터 수신 이벤트
        /// </summary>
        public event EventHandler<byte[]> DataReceived;

        /// <summary>
        /// DevicePortAdapter 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="portName">연결할 포트 이름</param>
        /// <param name="multiPortManager">사용할 멀티포트 매니저 인스턴스</param>
        public DevicePortAdapter(string portName, MultiPortSerialManager multiPortManager)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _multiPortManager = multiPortManager ?? throw new ArgumentNullException(nameof(multiPortManager));
            _isConnected = false;
            Settings = new CommunicationSettings();

            // 포트별 데이터 수신 이벤트 구독
            _multiPortManager.PortDataReceived += MultiPortManager_PortDataReceived;
            _multiPortManager.StatusChanged += MultiPortManager_StatusChanged;
        }

        /// <summary>
        /// 장치의 리소스를 해제합니다.
        /// </summary>
        public void Dispose()
        {
            // 이벤트 구독 해제
            _multiPortManager.PortDataReceived -= MultiPortManager_PortDataReceived;
            _multiPortManager.StatusChanged -= MultiPortManager_StatusChanged;

            // 연결 해제
            if (IsConnected)
            {
                Disconnect();
            }
        }

        private void MultiPortManager_StatusChanged(object sender, CommunicationStatusEventArgs e)
        {
            // 상태 변경 이벤트 전달 (필터링)
            // 같은 포트에 대한 이벤트만 전달
            OnStatusChanged(e);
        }

        private void MultiPortManager_PortDataReceived(object sender, PortDataReceivedEventArgs e)
        {
            // 현재 포트에 대한 데이터만 처리
            if (e.PortName == _portName)
            {
                OnDataReceived(e.Data);
            }
        }

        /// <summary>
        /// 시리얼 포트에 연결합니다.
        /// </summary>
        /// <param name="connectionId">포트 이름 (무시됨, 생성자에서 설정한 포트만 사용)</param>
        /// <param name="settings">통신 설정</param>
        /// <returns>연결 성공 여부</returns>
        public bool Connect(string connectionId, CommunicationSettings settings)
        {
            // connectionId는 무시하고 생성자에서 설정한 포트 이름 사용
            Settings = settings;
            bool result = _multiPortManager.Connect(_portName, settings);
            IsConnected = result;
            return result;
        }

        /// <summary>
        /// 시리얼 포트에 연결합니다. (기본 설정 사용)
        /// </summary>
        /// <param name="connectionId">포트 이름 (무시됨)</param>
        /// <returns>연결 성공 여부</returns>
        public bool Connect(string connectionId)
        {
            // connectionId는 무시하고 생성자에서 설정한 포트 이름 사용
            bool result = _multiPortManager.Connect(_portName, Settings);
            IsConnected = result;
            return result;
        }

        /// <summary>
        /// 시리얼 포트 연결을 해제합니다.
        /// </summary>
        public void Disconnect()
        {
            if (IsConnected)
            {
                _multiPortManager.Disconnect(_portName);
                IsConnected = false;
            }
        }

        /// <summary>
        /// 데이터를 전송합니다.
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <param name="offset">시작 오프셋</param>
        /// <param name="count">바이트 수</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
            {
                return false;
            }

            return _multiPortManager.Write(_portName, buffer, offset, count);
        }

        /// <summary>
        /// 데이터를 전송합니다.
        /// </summary>
        /// <param name="buffer">전송할 데이터 버퍼</param>
        /// <returns>전송 성공 여부</returns>
        public bool Write(byte[] buffer)
        {
            if (!IsConnected)
            {
                return false;
            }

            return _multiPortManager.Write(_portName, buffer);
        }

        /// <summary>
        /// 모든 가용한 데이터를 읽습니다.
        /// </summary>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] ReadAll()
        {
            if (!IsConnected)
            {
                return null;
            }

            return _multiPortManager.ReadAll(_portName);
        }

        /// <summary>
        /// 지정된 길이만큼 데이터를 읽습니다.
        /// </summary>
        /// <param name="count">읽을 바이트 수</param>
        /// <returns>읽은 데이터 또는 데이터가 없으면 null</returns>
        public byte[] Read(int count)
        {
            if (!IsConnected)
            {
                return null;
            }

            return _multiPortManager.Read(_portName, count);
        }

        /// <summary>
        /// 시간 초과 값을 설정합니다.
        /// </summary>
        /// <param name="timeout">시간 초과 (밀리초)</param>
        public void SetTimeout(int timeout)
        {
            if (IsConnected)
            {
                _multiPortManager.SetTimeout(_portName, timeout);
            }
        }

        /// <summary>
        /// 입력 버퍼를 비웁니다.
        /// </summary>
        public void DiscardInBuffer()
        {
            if (IsConnected)
            {
                _multiPortManager.DiscardInBuffer(_portName);
            }
        }

        /// <summary>
        /// 출력 버퍼를 비웁니다.
        /// </summary>
        public void DiscardOutBuffer()
        {
            if (IsConnected)
            {
                _multiPortManager.DiscardOutBuffer(_portName);
            }
        }

        /// <summary>
        /// 통신 상태 변경 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="e">이벤트 인자</param>
        private void OnStatusChanged(CommunicationStatusEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 데이터 수신 이벤트를 발생시킵니다.
        /// </summary>
        /// <param name="data">수신된 데이터</param>
        private void OnDataReceived(byte[] data)
        {
            DataReceived?.Invoke(this, data);
        }
    }
}