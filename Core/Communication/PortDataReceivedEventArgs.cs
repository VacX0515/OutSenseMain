using System;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 포트 데이터 수신 이벤트의 인자를 나타내는 클래스입니다.
    /// </summary>
    public class PortDataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 데이터를 수신한 포트 이름
        /// </summary>
        public string PortName { get; private set; }

        /// <summary>
        /// 수신된 데이터
        /// </summary>
        public byte[] Data { get; private set; }

        /// <summary>
        /// PortDataReceivedEventArgs 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="portName">데이터를 수신한 포트 이름</param>
        /// <param name="data">수신된 데이터</param>
        public PortDataReceivedEventArgs(string portName, byte[] data)
        {
            PortName = portName;
            Data = data;
        }
    }
}