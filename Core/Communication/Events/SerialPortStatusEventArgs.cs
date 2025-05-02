using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VacX_OutSense.Core.Communication.Events
{
    /// <summary>
    /// 시리얼 포트 상태 변경 이벤트의 인자를 정의합니다.
    /// </summary>
    public class SerialPortStatusEventArgs : EventArgs
    {
        /// <summary>
        /// 포트가 열려있는지 여부
        /// </summary>
        public bool IsOpen { get; }

        /// <summary>
        /// 포트 이름
        /// </summary>
        public string PortName { get; }

        /// <summary>
        /// 상태 메시지
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// SerialPortStatusEventArgs의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="isOpen">포트가 열려있는지 여부</param>
        /// <param name="portName">포트 이름</param>
        /// <param name="statusMessage">상태 메시지</param>
        public SerialPortStatusEventArgs(bool isOpen, string portName, string statusMessage)
        {
            IsOpen = isOpen;
            PortName = portName;
            StatusMessage = statusMessage;
        }
    }
}