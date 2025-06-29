using System;

namespace VacX_OutSense.Core.Control
{
    /// <summary>
    /// PID 제어기 클래스
    /// </summary>
    public class PIDController
    {
        #region 필드 및 속성

        // PID 게인
        private double _kp; // 비례 게인
        private double _ki; // 적분 게인
        private double _kd; // 미분 게인

        // 적분항 누적값
        private double _integral;

        // 이전 오차값 (미분 계산용)
        private double _previousError;

        // 출력 제한
        private double _outputMin;
        private double _outputMax;

        // 적분 와인드업 방지를 위한 적분항 제한
        private double _integralMin;
        private double _integralMax;

        // 데드밴드 (불감대)
        private double _deadband;

        // 마지막 업데이트 시간
        private DateTime _lastUpdateTime;

        /// <summary>
        /// 비례 게인 (Kp)
        /// </summary>
        public double Kp
        {
            get => _kp;
            set => _kp = Math.Max(0, value);
        }

        /// <summary>
        /// 적분 게인 (Ki)
        /// </summary>
        public double Ki
        {
            get => _ki;
            set => _ki = Math.Max(0, value);
        }

        /// <summary>
        /// 미분 게인 (Kd)
        /// </summary>
        public double Kd
        {
            get => _kd;
            set => _kd = Math.Max(0, value);
        }

        /// <summary>
        /// 출력 최소값
        /// </summary>
        public double OutputMin
        {
            get => _outputMin;
            set => _outputMin = value;
        }

        /// <summary>
        /// 출력 최대값
        /// </summary>
        public double OutputMax
        {
            get => _outputMax;
            set => _outputMax = value;
        }

        /// <summary>
        /// 데드밴드 (불감대)
        /// </summary>
        public double Deadband
        {
            get => _deadband;
            set => _deadband = Math.Max(0, value);
        }

        /// <summary>
        /// 현재 적분항 값
        /// </summary>
        public double IntegralTerm => _integral;

        /// <summary>
        /// 마지막 오차값
        /// </summary>
        public double LastError => _previousError;

        #endregion

        #region 생성자

        /// <summary>
        /// PID 제어기 생성자
        /// </summary>
        /// <param name="kp">비례 게인</param>
        /// <param name="ki">적분 게인</param>
        /// <param name="kd">미분 게인</param>
        /// <param name="outputMin">출력 최소값</param>
        /// <param name="outputMax">출력 최대값</param>
        public PIDController(double kp, double ki, double kd, double outputMin, double outputMax)
        {
            _kp = Math.Max(0, kp);
            _ki = Math.Max(0, ki);
            _kd = Math.Max(0, kd);
            _outputMin = outputMin;
            _outputMax = outputMax;

            // 적분항 제한을 출력 제한의 2배로 설정
            _integralMin = outputMin * 2;
            _integralMax = outputMax * 2;

            _deadband = 0.1; // 기본 데드밴드 0.1도

            Reset();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// PID 제어 계산
        /// </summary>
        /// <param name="setpoint">목표값</param>
        /// <param name="processValue">현재값</param>
        /// <returns>제어 출력값</returns>
        public double Calculate(double setpoint, double processValue)
        {
            DateTime currentTime = DateTime.Now;
            double dt = 1.0; // 기본 시간 간격 (초)

            if (_lastUpdateTime != DateTime.MinValue)
            {
                dt = (currentTime - _lastUpdateTime).TotalSeconds;
                if (dt <= 0) dt = 1.0; // 시간이 역행하는 경우 방지
            }

            // 오차 계산
            double error = setpoint - processValue;

            // 데드밴드 적용
            if (Math.Abs(error) < _deadband)
            {
                error = 0;
            }

            // 비례항 계산
            double proportional = _kp * error;

            // 적분항 계산
            _integral += error * dt;

            // 적분 와인드업 방지
            _integral = Math.Max(_integralMin, Math.Min(_integralMax, _integral));
            double integral = _ki * _integral;

            // 미분항 계산
            double derivative = 0;
            if (_lastUpdateTime != DateTime.MinValue)
            {
                double errorRate = (error - _previousError) / dt;
                derivative = _kd * errorRate;
            }

            // 전체 출력 계산
            double output = proportional + integral + derivative;

            // 출력 제한
            output = Math.Max(_outputMin, Math.Min(_outputMax, output));

            // 상태 업데이트
            _previousError = error;
            _lastUpdateTime = currentTime;

            return output;
        }

        /// <summary>
        /// PID 제어기 리셋
        /// </summary>
        public void Reset()
        {
            _integral = 0;
            _previousError = 0;
            _lastUpdateTime = DateTime.MinValue;
        }

        /// <summary>
        /// 적분항만 리셋
        /// </summary>
        public void ResetIntegral()
        {
            _integral = 0;
        }

        /// <summary>
        /// PID 파라미터 설정
        /// </summary>
        /// <param name="kp">비례 게인</param>
        /// <param name="ki">적분 게인</param>
        /// <param name="kd">미분 게인</param>
        public void SetParameters(double kp, double ki, double kd)
        {
            _kp = Math.Max(0, kp);
            _ki = Math.Max(0, ki);
            _kd = Math.Max(0, kd);
        }

        /// <summary>
        /// 적분항 제한 설정
        /// </summary>
        /// <param name="min">최소값</param>
        /// <param name="max">최대값</param>
        public void SetIntegralLimits(double min, double max)
        {
            _integralMin = min;
            _integralMax = max;
        }

        #endregion
    }
}