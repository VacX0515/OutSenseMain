using System;
using System.Collections.Generic;
using System.Windows.Forms;
using VacX_OutSense.Models;

namespace VacX_OutSense.Utils
{
    /// <summary>
    /// 간소화된 UI 업데이트 서비스
    /// 복잡한 로직 없이 효율적으로 UI만 업데이트
    /// </summary>
    public class SimplifiedUIUpdateService : IDisposable
    {
        private readonly MainForm _mainForm;
        private readonly System.Windows.Forms.Timer _uiTimer;
        private UIDataSnapshot _pendingSnapshot;
        private readonly object _snapshotLock = new object();

        // UI 업데이트 설정
        private const int UI_UPDATE_INTERVAL = 100; // 100ms - 압력값과 동기화
        private bool _isUpdating = false;

        public SimplifiedUIUpdateService(MainForm mainForm)
        {
            _mainForm = mainForm;

            _uiTimer = new System.Windows.Forms.Timer
            {
                Interval = UI_UPDATE_INTERVAL
            };
            _uiTimer.Tick += UITimer_Tick;

            LoggerService.Instance.LogInfo("간소화된 UI 서비스 초기화 완료");
        }

        public void Start()
        {
            _uiTimer.Start();
            LoggerService.Instance.LogInfo("UI 업데이트 서비스 시작됨");
        }

        public void Stop()
        {
            _uiTimer.Stop();
            LoggerService.Instance.LogInfo("UI 업데이트 서비스 중지됨");
        }

        /// <summary>
        /// UI 업데이트 요청 (비동기)
        /// </summary>
        public void RequestUpdate(UIDataSnapshot snapshot)
        {
            if (snapshot == null) return;

            lock (_snapshotLock)
            {
                _pendingSnapshot = snapshot;
            }
        }

        /// <summary>
        /// UI 타이머 틱 - 실제 업데이트 수행
        /// </summary>
        private void UITimer_Tick(object sender, EventArgs e)
        {
            if (_isUpdating) return;

            UIDataSnapshot snapshotToProcess = null;
            lock (_snapshotLock)
            {
                snapshotToProcess = _pendingSnapshot;
                _pendingSnapshot = null;
            }

            if (snapshotToProcess == null) return;

            _isUpdating = true;

            try
            {
                // 단순하고 빠른 업데이트
                UpdateUI(snapshotToProcess);
            }
            catch (Exception ex)
            {
                LoggerService.Instance.LogError($"UI 업데이트 오류: {ex.Message}", ex);
            }
            finally
            {
                _isUpdating = false;
            }
        }

        /// <summary>
        /// 실제 UI 업데이트 수행
        /// </summary>
        private void UpdateUI(UIDataSnapshot snapshot)
        {
            // BeginUpdate/EndUpdate를 사용하여 깜빡임 방지
            _mainForm.SuspendLayout();

            try
            {
                // 압력 값 업데이트 (가장 중요)
                UpdatePressureValues(snapshot);

                // 밸브 상태
                UpdateValveStatus(snapshot);

                // 펌프 상태
                UpdatePumpStatus(snapshot);

                // 온도 데이터 (저빈도)
                UpdateTemperatureData(snapshot);

                //// 연결 상태
                //UpdateConnectionStatus(snapshot);

                // 버튼 상태
                UpdateButtonStates(snapshot);

                // PID 상태 업데이트
                _mainForm.UpdatePIDStatus();
            }
            finally
            {
                _mainForm.ResumeLayout(false);
            }
        }

        private void UpdatePressureValues(UIDataSnapshot snapshot)
        {
            // MainForm의 public 메서드를 통해 업데이트
            _mainForm.SetAtmPressureText(snapshot.AtmPressure.ToString("F1"));
            _mainForm.SetPiraniPressureText(snapshot.PiraniPressure.ToString("E2"));
            _mainForm.SetIonPressureText(snapshot.IonPressure.ToString("E2"));
            _mainForm.SetIonGaugeStatusText(snapshot.IonGaugeStatus);
        }

        private void UpdateValveStatus(UIDataSnapshot snapshot)
        {
            _mainForm.SetGateValveStatus(snapshot.GateValveStatus);
            _mainForm.SetVentValveStatus(snapshot.VentValveStatus);
            _mainForm.SetExhaustValveStatus(snapshot.ExhaustValveStatus);
            _mainForm.SetIonGaugeHVStatus(snapshot.IonGaugeHVStatus);
        }

        private void UpdatePumpStatus(UIDataSnapshot snapshot)
        {
            // 드라이펌프
            _mainForm.SetDryPumpStatus(
                snapshot.DryPump.Status,
                snapshot.DryPump.Speed,
                snapshot.DryPump.Current,
                snapshot.DryPump.Temperature,
                snapshot.DryPump.HasWarning,
                snapshot.DryPump.HasError,
                snapshot.DryPump.Warning
            );

            // 터보펌프
            _mainForm.SetTurboPumpStatus(
                snapshot.TurboPump.Status,
                snapshot.TurboPump.Speed,
                snapshot.TurboPump.Current,
                snapshot.TurboPump.Temperature,
                snapshot.TurboPump.HasWarning,
                snapshot.TurboPump.HasError,
                snapshot.TurboPump.Warning
            );
        }

        /// <summary>
        /// 온도 데이터 업데이트 (5채널 지원)
        /// </summary>
        private void UpdateTemperatureData(UIDataSnapshot snapshot)
        {
            // 칠러
            _mainForm.SetBathCirculatorStatus(
                snapshot.BathCirculator.Status,
                snapshot.BathCirculator.CurrentTemp,
                snapshot.BathCirculator.TargetTemp,
                snapshot.BathCirculator.Time,
                snapshot.BathCirculator.Mode,
                snapshot.BathCirculator.HasError,
                snapshot.BathCirculator.HasWarning
            );

            // 온도 컨트롤러 (5채널: 메인 2 + 확장 3)
            for (int i = 0; i < 5; i++)
            {
                if (i < snapshot.TempController.Channels.Length)
                {
                    var ch = snapshot.TempController.Channels[i];
                    _mainForm.SetTempControllerChannelStatus(
                        i + 1,
                        ch.PresentValue,
                        ch.SetValue,
                        ch.Status,
                        ch.HeatingMV,
                        ch.IsAutoTuning
                    );
                }
            }
        }

        private void UpdateConnectionStatus(UIDataSnapshot snapshot)
        {
            _mainForm.SetConnectionStatus("iomodule", snapshot.Connections.IOModule);
            _mainForm.SetConnectionStatus("drypump", snapshot.Connections.DryPump);
            _mainForm.SetConnectionStatus("turbopump", snapshot.Connections.TurboPump);
            _mainForm.SetConnectionStatus("bathcirculator", snapshot.Connections.BathCirculator);
            _mainForm.SetConnectionStatus("tempcontroller", snapshot.Connections.TempController);
        }

        /// <summary>
        /// 버튼 상태 업데이트 (5채널 지원)
        /// </summary>
        private void UpdateButtonStates(UIDataSnapshot snapshot)
        {
            var states = snapshot.ButtonStates;

            //_mainForm.SetButtonEnabled("iongauge", states.IonGaugeEnabled);
            _mainForm.SetButtonEnabled("ventvalve", states.VentValveEnabled);
            _mainForm.SetButtonEnabled("exhaustvalve", states.ExhaustValveEnabled);

            _mainForm.SetButtonEnabled("drypumpstart", states.DryPumpStartEnabled);
            _mainForm.SetButtonEnabled("drypumpstop", states.DryPumpStopEnabled);
            _mainForm.SetButtonEnabled("drypumpstandby", states.DryPumpStandbyEnabled);
            _mainForm.SetButtonEnabled("drypumpnormal", states.DryPumpNormalEnabled);

            _mainForm.SetButtonEnabled("turbopumpstart", states.TurboPumpStartEnabled);
            _mainForm.SetButtonEnabled("turbopumpstop", states.TurboPumpStopEnabled);
            _mainForm.SetButtonEnabled("turbopumpvent", states.TurboPumpVentEnabled);
            _mainForm.SetButtonEnabled("turbopumpreset", states.TurboPumpResetEnabled);

            _mainForm.SetButtonEnabled("bathcirculatorstart", states.BathCirculatorStartEnabled);
            _mainForm.SetButtonEnabled("bathcirculatorstop", states.BathCirculatorStopEnabled);

            // 온도컨트롤러 버튼 (5채널)
            _mainForm.SetButtonEnabled("ch1start", states.TempControllerStartEnabled[0]);
            _mainForm.SetButtonEnabled("ch1stop", states.TempControllerStopEnabled[0]);
            _mainForm.SetButtonEnabled("ch2start", states.TempControllerStartEnabled[1]);
            _mainForm.SetButtonEnabled("ch2stop", states.TempControllerStopEnabled[1]);

            // 확장 채널은 버튼이 없으므로 (입력 전용) 별도 처리 불필요
            // 필요시 아래 주석 해제
            // _mainForm.SetButtonEnabled("ch3start", states.TempControllerStartEnabled[2]);
            // _mainForm.SetButtonEnabled("ch3stop", states.TempControllerStopEnabled[2]);
            // _mainForm.SetButtonEnabled("ch4start", states.TempControllerStartEnabled[3]);
            // _mainForm.SetButtonEnabled("ch4stop", states.TempControllerStopEnabled[3]);
            // _mainForm.SetButtonEnabled("ch5start", states.TempControllerStartEnabled[4]);
            // _mainForm.SetButtonEnabled("ch5stop", states.TempControllerStopEnabled[4]);
        }

        public void Dispose()
        {
            Stop();
            _uiTimer?.Dispose();
        }
    }
}