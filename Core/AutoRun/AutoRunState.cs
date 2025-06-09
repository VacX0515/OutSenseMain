using System;

namespace VacX_OutSense.Core.AutoRun
{
    /// <summary>
    /// AutoRun 시퀀스 상태
    /// </summary>
    public enum AutoRunState
    {
        /// <summary>
        /// 대기 중
        /// </summary>
        Idle,

        /// <summary>
        /// 초기화 단계
        /// </summary>
        Initializing,

        /// <summary>
        /// 진공 준비 단계
        /// </summary>
        PreparingVacuum,

        /// <summary>
        /// 드라이펌프 시작
        /// </summary>
        StartingDryPump,

        /// <summary>
        /// 터보펌프 시작
        /// </summary>
        StartingTurboPump,

        /// <summary>
        /// 이온게이지 활성화
        /// </summary>
        ActivatingIonGauge,

        /// <summary>
        /// 고진공 대기
        /// </summary>
        WaitingHighVacuum,

        /// <summary>
        /// 히터 시작
        /// </summary>
        StartingHeater,

        /// <summary>
        /// 실험 진행
        /// </summary>
        RunningExperiment,

        /// <summary>
        /// 종료 시퀀스
        /// </summary>
        ShuttingDown,

        /// <summary>
        /// 완료됨
        /// </summary>
        Completed,

        /// <summary>
        /// 중단됨
        /// </summary>
        Aborted,

        /// <summary>
        /// 오류 발생
        /// </summary>
        Error,

        /// <summary>
        /// 일시정지
        /// </summary>
        Paused
    }

    /// <summary>
    /// AutoRun 단계 결과
    /// </summary>
    public enum StepResult
    {
        /// <summary>
        /// 성공
        /// </summary>
        Success,

        /// <summary>
        /// 실패
        /// </summary>
        Failed,

        /// <summary>
        /// 시간 초과
        /// </summary>
        Timeout,

        /// <summary>
        /// 중단됨
        /// </summary>
        Aborted
    }

    /// <summary>
    /// AutoRun 모드
    /// </summary>
    public enum AutoRunMode
    {
        /// <summary>
        /// 전체 자동 실행
        /// </summary>
        FullAuto,

        /// <summary>
        /// 단계별 확인 모드
        /// </summary>
        StepByStep,

        /// <summary>
        /// 시뮬레이션 모드
        /// </summary>
        Simulation
    }
}