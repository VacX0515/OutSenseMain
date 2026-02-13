using System;
using System.Threading;
using System.Threading.Tasks;

namespace VacX_OutSense.Core.Communication
{
    /// <summary>
    /// 시리얼 포트 커맨드 큐의 기본 단위입니다.
    /// Write→Read 트랜잭션을 하나의 원자적 작업으로 캡슐화합니다.
    /// </summary>
    public class SerialCommand
    {
        /// <summary>
        /// 전송할 요청 데이터
        /// </summary>
        public byte[] Request { get; }

        /// <summary>
        /// 예상 응답 길이 (바이트).
        /// 0이면 길이를 모르므로 타임아웃까지 수신합니다.
        /// </summary>
        public int ExpectedResponseLength { get; }

        /// <summary>
        /// 응답 대기 타임아웃 (밀리초)
        /// </summary>
        public int TimeoutMs { get; }

        /// <summary>
        /// 명령 우선순위. 값이 작을수록 높은 우선순위입니다.
        /// 0 = 최우선(긴급 명령), 5 = 일반 명령, 10 = 폴링
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// 명령 생성 시각 (디버깅/모니터링용)
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// 명령 설명 (디버깅/로깅용)
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// 응답을 비동기로 전달하기 위한 TaskCompletionSource
        /// </summary>
        internal TaskCompletionSource<byte[]> ResponseTcs { get; }

        /// <summary>
        /// 응답을 기다리는 Task. 명령 처리가 완료되면 결과가 설정됩니다.
        /// </summary>
        public Task<byte[]> ResponseTask => ResponseTcs.Task;

        /// <summary>
        /// SerialCommand의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="request">전송할 요청 데이터</param>
        /// <param name="expectedResponseLength">예상 응답 길이 (0이면 타임아웃까지 수신)</param>
        /// <param name="timeoutMs">응답 대기 타임아웃 (밀리초)</param>
        /// <param name="priority">우선순위 (0=최우선, 10=폴링)</param>
        public SerialCommand(byte[] request, int expectedResponseLength = 0, int timeoutMs = 500, int priority = 5)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            ExpectedResponseLength = expectedResponseLength;
            TimeoutMs = timeoutMs;
            Priority = priority;
            CreatedAt = DateTime.Now;
            ResponseTcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        /// <summary>
        /// 지정된 시간 내에 응답을 대기합니다.
        /// 채널의 처리 스레드가 응답을 설정할 때까지 블로킹합니다.
        /// </summary>
        /// <returns>수신된 응답 데이터. 타임아웃이면 null.</returns>
        public byte[] WaitForResponse()
        {
            try
            {
                // 채널 처리 스레드가 TCS에 결과를 설정할 때까지 대기
                // 전체 타임아웃 + 큐 대기 여유분(500ms)
                if (ResponseTcs.Task.Wait(TimeoutMs + 500))
                {
                    return ResponseTcs.Task.Result;
                }
                return null;
            }
            catch (AggregateException ae)
            {
                // 내부 예외가 IOException 등이면 null 반환
                // 호출자가 null을 통신 실패로 처리하도록 함
                System.Diagnostics.Debug.WriteLine($"SerialCommand 응답 대기 중 예외: {ae.InnerException?.Message}");
                return null;
            }
        }
    }
}