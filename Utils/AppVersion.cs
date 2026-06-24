namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 소프트웨어 버전 및 패치 노트 관리
    /// </summary>
    public static class AppVersion
    {
        public const string Version = "2.7.4";
        public const string AppTitle = "VacX OutSense";

        /// <summary>
        /// 실제 빌드 시각 — 어셈블리 파일 LastWriteTime 기반.
        /// 어떤 빌드가 실행 중인지 정보 다이얼로그에서 확인 가능.
        /// </summary>
        public static string BuildDate
        {
            get
            {
                try
                {
                    var asmPath = typeof(AppVersion).Assembly.Location;
                    if (string.IsNullOrEmpty(asmPath))
                        return "unknown";
                    var info = new System.IO.FileInfo(asmPath);
                    return info.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch
                {
                    return "unknown";
                }
            }
        }

        public static string FullTitle => $"{AppTitle} v{Version}";

        /// <summary>
        /// 패치 노트 (최신 순)
        /// </summary>
        public static readonly string[] PatchNotes = new[]
        {
            "v2.7.4 (2026-06-24)",
            "─────────────────────────────────",
            "[신규] 칠러 PID 설정 UI — 메뉴 [설정 → 칠러 PID 설정...]",
            "       Kp/Ki/Kd, 목표 채널/온도, Update 주기, Deadband, Adaptive on/off 편집",
            "       적용 시 baseline + PID 게인 동시 갱신 → XML 저장",
            "[개선] Deadband를 XML 설정에 포함 (이전: 코드 하드코드 0.5)",
            "",
            "v2.7.3 (2026-06-24)",
            "─────────────────────────────────",
            "[수정] 칠러 PID saturation anti-windup 추가",
            "       출력이 한계(-15/+15)에 박힌 채로 같은 방향 오차가 지속되면 적분 동결",
            "       기존엔 deadband freeze만 있어서 saturate 구간 5분간 적분이 -882까지 누적",
            "       → ±2.8°C, 25분 주기 대형 limit cycle 유발하던 windup 차단",
            "",
            "v2.7.2 (2026-06-18)",
            "─────────────────────────────────",
            "[수정] 칠러 PID limit cycle 차단 — deadband 안에서 적분 freeze",
            "       기존엔 deadband 내 P=0 상태로 적분만 누적해 6분 주기 ±0.4°C 진동 유발",
            "[개선] Adaptive 임계값 하향 — limit cycle 자동 감지",
            "       OscillationThreshold 0.4 → 0.3 (분당 영점교차)",
            "       SwingAmplitudeThreshold 1.5 → 0.6°C (min-max swing)",
            "[개선] UI — 헤더에서 버전 제거(VacX OutSense만), 상태바에 빌드 일시 동적 표시",
            "",
            "v2.7.1 (2026-06-15)",
            "─────────────────────────────────",
            "[수정] 칠러 PID 적응 학습 발산 차단",
            "       · 오버슈트+정상오차 동시 출현을 oscillation으로 판정 (이전: Ki 계속 증가)",
            "       · 저주파 진동(min-max swing) 감지 추가 — 칠러 5~10분 주기 사이클",
            "       · 진동 감지 시 Ki 강제 감소(이전: Kp만 감소)",
            "[수정] Adaptive baseline drift 방지 — 사용자 baseline을 별도 저장,",
            "       학습된 게인이 다음 실행의 baseline을 덮어쓰지 못하도록 분리",
            "[개선] 칠러 setpoint 변화율 제한 (±2°C/cycle) — dead-time 큰 시스템 진동 억제",
            "[개선] PID Deadband 0.3 → 0.5°C, UpdateInterval 기본 10s → 30s",
            "[신규] AutoRun Hold 진입 시 ChillerPID 적분 windup 자동 제거",
            "[개선] PID 기본 게인 보수적 값으로 (Kp:0.8/Ki:0.003/Kd:0.5 → 0.5/0.005/0.7)",
            "",
            "v2.7.0 (2026-04-15)",
            "─────────────────────────────────",
            "[개선] ΔT 제한 자동화: 관측 열지연 기반 자동 계산 (수동 설정도 가능)",
            "[개선] 승온 오버슈트 감소: 목표 근접 시 음의 적분 감쇠 가속 (proximity 기반)",
            "[개선] 홀드 제어 전면 개편: PI 재계산 → 평형 SV 유지 + 편차 비례 보정",
            "[개선] 홀드 안정성 강화: SV 변화 ±0.2°C/사이클 제한, 적분 소멸 방지",
            "[개선] 실험 데이터 로깅 기록 컬럼 선택 기능 추가",
            "",
            "v2.1.0 (2026-03-06)",
            "─────────────────────────────────",
            "[신규] AutoRun 실시간 온도/압력 차트 추가 (ScottPlot)",
            "[신규] 압력 인터락 지속 시간 설정 (PressureInterlockDurationSeconds)",
            "[신규] Outgassing 모드 TM4 램프 설정 적용 (HeaterRampUpRate)",
            "[개선] 비상 정지 시 터보펌프 감속 대기 후 안전 순서 준수",
            "[개선] Outgassing 모드 센서 에러/이상값 감지 추가",
            "[개선] Outgassing 모드 과압 시 CH1 SV 자동 감소/복원",
            "[개선] Outgassing 모드 하드웨어 최대 온도 사전 검증",
            "[개선] Outgassing 모드 승온 타임아웃 동적 계산",
            "[개선] Outgassing 홀드 루프 5초 간격으로 단축 (기존 60초)",
            "[개선] Outgassing 홀드 CH1 무응답 감지 및 자동 중단",
            "[수정] PI 감속 구간 적분 강제 클램핑 → 증가율 감속으로 변경 (Fix#14)",
            "[수정] 차트 시간축 24시간 형식 적용 (AM/PM 깨짐 수정)",
            "[수정] 온도 차트에 목표 온도 기준선 표시",
            "",
            "v2.0.0 (2026-02-27)",
            "─────────────────────────────────",
            "[신규] 설정 가능한 인터락 시스템 구현",
            "[신규] 온도제어기 SVH/MVH 설정",
            "[개선] 압력 표시 개선",
            "[개선] 이온게이지 신호(pin6) DI3 적용",
            "",
            "v1.9.0 (2026-02-27)",
            "─────────────────────────────────",
            "[신규] Bakeout AutoRun 1차 구현",
            "[신규] IO모듈 마스터 교체 (DIO)",
            "",
            "v1.8.0 (2026-02-20)",
            "─────────────────────────────────",
            "[신규] AutoRun 상태 감지 재개 기능",
            "[신규] 실험 데이터 통합 로거",
            "[신규] 시리얼 자동 수색 기능",
            "[개선] 통신 아키텍처 전환",
            "[수정] 칠러 실행 트리거 메서드 수정",
            "",
            "v1.7.0 (2026-02-10)",
            "─────────────────────────────────",
            "[신규] 온도 유지(Hold) 기능",
            "[수정] NumericUpDown 범위 에러 수정",
            "[개선] Ramp 제어 기능 개선",
            "",
            "v1.6.0 (2026-01-29)",
            "─────────────────────────────────",
            "[신규] Bakeout Ramp-Up 기능",
            "[수정] PID 제어 출력 부호 수정",
        };
    }
}
