using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Devices.BathCirculator;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Forms;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// Ch2 온도를 기반으로 칠러를 PID 제어하는 서비스
    /// </summary>
    public class ChillerPIDControlService : IDisposable
    {
        #region 필드 및 속성

        private readonly MainForm _mainForm;
        private readonly PIDController _pidController;
        private readonly System.Timers.Timer _controlTimer;

        private bool _isEnabled;
        private double _ch2TargetTemperature;
        private double _chillerBaseTemperature;
        private double _updateInterval; // 초 단위

        // 칠러 온도 제한
        private const double CHILLER_MIN_TEMP = -10.0;
        private const double CHILLER_MAX_TEMP = 80.0;

        // PID 출력 제한 (칠러 온도 오프셋)
        private const double PID_OUTPUT_MIN = -20.0; // 최대 20도 낮출 수 있음
        private const double PID_OUTPUT_MAX = 20.0;  // 최대 20도 높일 수 있음

        /// <summary>
        /// PID 제어 활성화 여부
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    if (_isEnabled)
                    {
                        Start();
                    }
                    else
                    {
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Ch2 목표 온도
        /// </summary>
        public double Ch2TargetTemperature
        {
            get => _ch2TargetTemperature;
            set => _ch2TargetTemperature = value;
        }

        /// <summary>
        /// 칠러 기준 온도
        /// </summary>
        public double ChillerBaseTemperature
        {
            get => _chillerBaseTemperature;
            set => _chillerBaseTemperature = value;
        }

        /// <summary>
        /// 업데이트 주기 (초)
        /// </summary>
        public double UpdateInterval
        {
            get => _updateInterval;
            set
            {
                _updateInterval = Math.Max(1.0, value);
                if (_controlTimer != null)
                {
                    _controlTimer.Interval = _updateInterval * 1000;
                }
            }
        }

        /// <summary>
        /// PID 제어기
        /// </summary>
        public PIDController PID => _pidController;

        /// <summary>
        /// 마지막 제어 출력값
        /// </summary>
        public double LastOutput { get; private set; }

        /// <summary>
        /// 마지막 Ch2 온도
        /// </summary>
        public double LastCh2Temperature { get; private set; }

        /// <summary>
        /// 마지막 칠러 설정 온도
        /// </summary>
        public double LastChillerSetpoint { get; private set; }

        #endregion

        #region 생성자

        /// <summary>
        /// ChillerPIDControlService 생성자
        /// </summary>
        /// <param name="mainForm">메인 폼 참조</param>
        public ChillerPIDControlService(MainForm mainForm)
        {
            _mainForm = mainForm ?? throw new ArgumentNullException(nameof(mainForm));

            // PID 제어기 초기화 (기본값)
            _pidController = new PIDController(
                kp: 1.0,     // 비례 게인
                ki: 0.01,     // 적분 게인
                kd: 0.5,     // 미분 게인
                outputMin: PID_OUTPUT_MIN,
                outputMax: PID_OUTPUT_MAX
            );

            // 기본값 설정
            _ch2TargetTemperature = 25.0;
            _chillerBaseTemperature = 23.5;
            _updateInterval = 10.0; // 5초마다 업데이트

            // 타이머 초기화
            _controlTimer = new System.Timers.Timer(_updateInterval * 1000);
            _controlTimer.Elapsed += ControlTimer_Elapsed;
            _controlTimer.AutoReset = true;
        }

        #endregion

        #region 제어 메서드

        /// <summary>
        /// PID 제어 시작
        /// </summary>
        public void Start()
        {
            if (!_isEnabled) return;

            // 장치 연결 확인
            if (!_mainForm._tempController?.IsConnected ?? true)
            {
                LogError("온도 컨트롤러가 연결되지 않았습니다.");
                return;
            }

            if (!_mainForm._bathCirculator?.IsConnected ?? true)
            {
                LogError("칠러가 연결되지 않았습니다.");
                return;
            }

            // PID 제어기 리셋
            _pidController.Reset();

            // 타이머 시작
            _controlTimer.Start();

            LogInfo($"칠러 PID 제어 시작 (Ch2 목표: {_ch2TargetTemperature}°C, 업데이트 주기: {_updateInterval}초)");
        }

        /// <summary>
        /// PID 제어 정지
        /// </summary>
        public void Stop()
        {
            _controlTimer.Stop();
            LogInfo("칠러 PID 제어 정지");
        }

        /// <summary>
        /// PID 파라미터 설정
        /// </summary>
        /// <param name="kp">비례 게인</param>
        /// <param name="ki">적분 게인</param>
        /// <param name="kd">미분 게인</param>
        public void SetPIDParameters(double kp, double ki, double kd)
        {
            _pidController.SetParameters(kp, ki, kd);
            LogInfo($"PID 파라미터 변경 - Kp: {kp}, Ki: {ki}, Kd: {kd}");
        }

        #endregion

        #region 타이머 이벤트

        /// <summary>
        /// 제어 타이머 이벤트 핸들러
        /// </summary>
        private async void ControlTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // 타이머 중복 실행 방지
                _controlTimer.Stop();

                // PID 제어 실행
                await ExecutePIDControl();
            }
            catch (Exception ex)
            {
                LogError($"PID 제어 오류: {ex.Message}");
            }
            finally
            {
                // 타이머 재시작
                if (_isEnabled)
                {
                    _controlTimer.Start();
                }
            }
        }

        /// <summary>
        /// PID 제어 실행
        /// </summary>
        private async Task ExecutePIDControl()
        {
            // 1. Ch2 현재 온도 읽기
            //await _mainForm._tempController.UpdateStatusAsync();
            var ch2Status = _mainForm._tempController.Status?.ChannelStatus[1]; // Ch2는 인덱스 1

            if (ch2Status == null)
            {
                LogWarning("Ch2 상태를 읽을 수 없습니다.");
                return;
            }

            // 온도값 계산 (소수점 처리)
            double ch2CurrentTemp = ch2Status.PresentValue;
            if (ch2Status.Dot > 0)
            {
                ch2CurrentTemp = ch2CurrentTemp / Math.Pow(10, ch2Status.Dot);
            }

            LastCh2Temperature = ch2CurrentTemp;

            // 2. PID 계산
            double pidOutput = _pidController.Calculate(_ch2TargetTemperature, ch2CurrentTemp);
            LastOutput = pidOutput;

            // 3. 칠러 설정 온도 계산
            // 칠러 온도 = 기준 온도 - PID 출력
            // (PID 출력이 양수이면 Ch2가 목표보다 낮으므로 칠러 온도를 낮춰야 함)
            double chillerSetpoint = _chillerBaseTemperature + pidOutput;

            // 칠러 온도 제한
            chillerSetpoint = Math.Max(CHILLER_MIN_TEMP, Math.Min(CHILLER_MAX_TEMP, chillerSetpoint));
            LastChillerSetpoint = chillerSetpoint;

            // 4. 칠러에 새로운 온도 설정
            bool result = await Task.Run(() => _mainForm._bathCirculator.SetTemperature(chillerSetpoint));

            if (result)
            {
                LogDebug($"PID 제어 - Ch2: {ch2CurrentTemp:F2}°C (목표: {_ch2TargetTemperature:F2}°C), " +
                        $"PID 출력: {pidOutput:F2}, 칠러 설정: {chillerSetpoint:F2}°C");
            }
            else
            {
                LogWarning("칠러 온도 설정 실패");
            }

            // 5. 데이터 로깅
            LogPIDData(ch2CurrentTemp, pidOutput, chillerSetpoint);
        }

        #endregion

        #region 로깅 메서드

        /// <summary>
        /// PID 제어 데이터 로깅
        /// </summary>
        private void LogPIDData(double ch2Temp, double pidOutput, double chillerSetpoint)
        {
            // CSV 형식으로 데이터 로깅
            var dataList = new List<string>
            {
                ch2Temp.ToString("F2"),
                _ch2TargetTemperature.ToString("F2"),
                pidOutput.ToString("F2"),
                chillerSetpoint.ToString("F2"),
                _pidController.Kp.ToString("F3"),
                _pidController.Ki.ToString("F3"),
                _pidController.Kd.ToString("F3"),
                _pidController.IntegralTerm.ToString("F3"),
                _pidController.LastError.ToString("F3")
            };

            DataLoggerService.Instance.LogDataAsync("ChillerPID", dataList);
        }

        private void LogInfo(string message)
        {
            LoggerService.Instance.LogInfo($"[ChillerPID] {message}");
        }

        private void LogWarning(string message)
        {
            LoggerService.Instance.LogWarning($"[ChillerPID] {message}");
        }

        private void LogError(string message)
        {
            LoggerService.Instance.LogError($"[ChillerPID] {message}");
        }

        private void LogDebug(string message)
        {
            LoggerService.Instance.LogDebug($"[ChillerPID] {message}");
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// 리소스 해제
        /// </summary>
        public void Dispose()
        {
            Stop();
            _controlTimer?.Dispose();
        }

        #endregion
    }
}