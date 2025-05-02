using System.IO.Ports;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 통신 설정 정보를 담는 클래스입니다.
    /// </summary>
    public class CommunicationSettings
    {

        /// <summary>
        /// 통신 속도 (bps)
        /// </summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 데이터 비트
        /// </summary>
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 패리티 비트
        /// </summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>
        /// 정지 비트
        /// </summary>
        public StopBits StopBits { get; set; } = StopBits.One;

        /// <summary>
        /// 핸드셰이크 설정
        /// </summary>
        public Handshake Handshake { get; set; } = Handshake.None;

        /// <summary>
        /// 읽기 시간 초과 (ms)
        /// </summary>
        public int ReadTimeout { get; set; } = 1000;

        /// <summary>
        /// 쓰기 시간 초과 (ms)
        /// </summary>
        public int WriteTimeout { get; set; } = 1000;

        /// <summary>
        /// CommunicationSettings 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        public CommunicationSettings()
        {
        }

        /// <summary>
        /// CommunicationSettings 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="baudRate">통신 속도</param>
        /// <param name="dataBits">데이터 비트</param>
        /// <param name="parity">패리티 비트</param>
        /// <param name="stopBits">정지 비트</param>
        public CommunicationSettings(int baudRate, int dataBits, Parity parity, StopBits stopBits)
        {
            BaudRate = baudRate;
            DataBits = dataBits;
            Parity = parity;
            StopBits = stopBits;
        }
    }
}