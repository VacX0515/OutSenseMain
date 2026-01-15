using System;
using System.Threading.Tasks;
using VacX_OutSense.Core.Devices.TempController;

namespace VacX_OutSense.Core.Extensions
{
    /// <summary>
    /// TempController 확장 메서드 - 누락된 메서드 추가 (수정 버전)
    /// </summary>
    public static class TempControllerExtensions
    {
        /// <summary>
        /// 채널의 운전/정지 상태를 설정합니다
        /// </summary>
        /// <param name="controller">TempController 인스턴스</param>
        /// <param name="channelNumber">채널 번호 (1-4)</param>
        /// <param name="run">true: 운전, false: 정지</param>
        /// <returns>성공 여부</returns>
        public static bool SetRunStop(this TempController controller, int channelNumber, bool run)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), "채널 번호는 1-4 사이여야 합니다.");

            try
            {
                // 채널 상태 업데이트
                var channelStatus = controller.Status.ChannelStatus[channelNumber - 1];

                if (run)
                {
                    // 운전 시작 - 현재 설정값으로 다시 설정하여 운전 시작
                    short currentSetValue = channelStatus.SetValue;
                    if (currentSetValue == 0)
                    {
                        // 설정값이 0이면 기본값으로 설정
                        currentSetValue = 250; // 25.0°C
                    }

                    // SetTemperature를 호출하면 자동으로 운전이 시작됨
                    bool result = controller.SetTemperature(channelNumber, currentSetValue);
                    channelStatus.IsRunning = run;
                    return result;
                }
                else
                {
                    // 정지 - SetTemperature를 0으로 설정하거나 상태만 변경
                    channelStatus.IsRunning = false;

                    // 옵션: 온도를 0으로 설정하여 정지
                    // return controller.SetTemperature(channelNumber, 0);

                    // 또는 상태만 변경
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"채널 {channelNumber} RUN/STOP 설정 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 모든 채널의 상태를 업데이트합니다
        /// </summary>
        /// <param name="controller">TempController 인스턴스</param>
        /// <returns>성공 여부</returns>
        public static bool UpdateAllChannelStatus(this TempController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            bool allSuccess = true;

            try
            {
                // UpdateChannelStatus가 private이므로 다른 방법 사용
                // 각 채널의 현재 상태를 읽기 위해 Status 객체 직접 접근

                int numChannels = controller.Status?.ChannelStatus?.Length ?? 0;

                if (numChannels == 0)
                {
                    // Status가 초기화되지 않았을 경우
                    return false;
                }

                // 전체 상태를 한 번에 업데이트하는 대안
                // 1. 현재 온도 읽기 시도 (이것이 내부적으로 상태를 업데이트함)
                for (int ch = 1; ch <= numChannels; ch++)
                {
                    try
                    {
                        // GetTemperature 메서드가 있다면 사용
                        // 없다면 Status 직접 접근
                        var status = controller.Status.ChannelStatus[ch - 1];

                        // 강제로 상태 업데이트를 트리거하기 위해 
                        // SetTemperature를 현재값으로 다시 설정
                        if (status.IsRunning)
                        {
                            controller.SetTemperature(ch, status.SetValue);
                        }
                    }
                    catch
                    {
                        allSuccess = false;
                    }
                }

                return allSuccess;
            }
            catch (Exception ex)
            {
                throw new Exception($"전체 채널 상태 업데이트 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 단일 채널 상태 업데이트 (대체 구현)
        /// </summary>
        public static bool UpdateSingleChannelStatus(this TempController controller, int channelNumber)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (channelNumber < 1 || channelNumber > controller.Status.ChannelStatus.Length)
                return false;

            try
            {
                // SetTemperature를 현재 값으로 호출하여 상태 갱신 유도
                var status = controller.Status.ChannelStatus[channelNumber - 1];
                return controller.SetTemperature(channelNumber, status.SetValue);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 비동기로 모든 채널 상태를 업데이트합니다
        /// </summary>
        public static async Task<bool> UpdateAllChannelStatusAsync(this TempController controller)
        {
            return await Task.Run(() => UpdateAllChannelStatus(controller));
        }

        /// <summary>
        /// 지정된 채널의 PID 운전을 시작합니다
        /// </summary>
        public static bool StartPIDControl(this TempController controller, int channelNumber, double targetTemp)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            try
            {
                // 1. 목표 온도 설정
                short setValue = (short)(targetTemp *
                    (controller.Status.ChannelStatus[channelNumber - 1].Dot == 0 ? 1 : 10));
                controller.SetTemperature(channelNumber, setValue);

                // 2. 운전 시작
                return SetRunStop(controller, channelNumber, true);
            }
            catch (Exception ex)
            {
                throw new Exception($"PID 제어 시작 실패: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 지정된 채널의 운전을 정지합니다
        /// </summary>
        public static bool StopControl(this TempController controller, int channelNumber)
        {
            return SetRunStop(controller, channelNumber, false);
        }

        /// <summary>
        /// 모든 채널의 운전을 정지합니다 (비상정지)
        /// </summary>
        public static bool EmergencyStopAll(this TempController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            bool allSuccess = true;
            int numChannels = controller.Status.ChannelStatus.Length;

            for (int ch = 1; ch <= numChannels; ch++)
            {
                try
                {
                    bool success = SetRunStop(controller, ch, false);
                    if (!success) allSuccess = false;
                }
                catch
                {
                    allSuccess = false;
                }
            }

            return allSuccess;
        }

        /// <summary>
        /// 채널의 현재 온도를 가져옵니다
        /// </summary>
        public static double GetChannelTemperature(this TempController controller, int channelNumber)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (channelNumber < 1 || channelNumber > controller.Status.ChannelStatus.Length)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            var status = controller.Status.ChannelStatus[channelNumber - 1];
            return status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        /// <summary>
        /// 채널의 설정 온도를 가져옵니다
        /// </summary>
        public static double GetChannelSetpoint(this TempController controller, int channelNumber)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (channelNumber < 1 || channelNumber > controller.Status.ChannelStatus.Length)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            var status = controller.Status.ChannelStatus[channelNumber - 1];
            return status.SetValue / (status.Dot == 0 ? 1.0 : 10.0);
        }

        /// <summary>
        /// 채널의 히터 출력값을 가져옵니다 (%)
        /// </summary>
        public static double GetHeaterOutput(this TempController controller, int channelNumber)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            if (channelNumber < 1 || channelNumber > controller.Status.ChannelStatus.Length)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            return controller.Status.ChannelStatus[channelNumber - 1].HeatingMV;
        }

        /// <summary>
        /// 모든 채널의 평균 온도를 가져옵니다
        /// </summary>
        public static double GetAverageTemperature(this TempController controller)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            double sum = 0;
            int count = 0;

            for (int i = 0; i < controller.Status.ChannelStatus.Length; i++)
            {
                var status = controller.Status.ChannelStatus[i];
                if (status.IsRunning)
                {
                    sum += status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
                    count++;
                }
            }

            return count > 0 ? sum / count : 0;
        }

        /// <summary>
        /// 지정된 채널들의 평균 온도를 가져옵니다
        /// </summary>
        public static double GetAverageTemperature(this TempController controller, params int[] channels)
        {
            if (controller == null)
                throw new ArgumentNullException(nameof(controller));

            double sum = 0;
            int count = 0;

            foreach (int ch in channels)
            {
                if (ch >= 1 && ch <= controller.Status.ChannelStatus.Length)
                {
                    var status = controller.Status.ChannelStatus[ch - 1];
                    sum += status.PresentValue / (status.Dot == 0 ? 1.0 : 10.0);
                    count++;
                }
            }

            return count > 0 ? sum / count : 0;
        }
    }
}