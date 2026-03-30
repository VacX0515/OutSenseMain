using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ScottPlot.WinForms;
using Label = System.Windows.Forms.Label;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// 베이크아웃 온도 상승/유지 PI 제어 시뮬레이션 테스트 폼
    /// 실제 장비 없이 PI 알고리즘과 열전달 모델을 가속 실행하여 결과를 확인
    /// </summary>
    public class BakeoutSimulationForm : Form
    {
        #region PI 상수 (AutoRunService와 동일)

        private const double Kp = 1.5;
        private const double Ki_norm = 0.01;
        private const double Kd = 3.0;

        #endregion

        #region UI 컨트롤

        // 파라미터 입력
        private NumericUpDown numTargetTemp;
        private NumericUpDown numRampRate;
        private NumericUpDown numHoldTime;
        private NumericUpDown numHeaterMax;
        private NumericUpDown numTolerance;
        private NumericUpDown numInitialTemp;

        // 열 모델 파라미터
        private NumericUpDown numHeaterTau;
        private NumericUpDown numSampleTau;
        private NumericUpDown numHeatLoss;
        private NumericUpDown numHeaterInsulation;

        // 시뮬레이션 제어
        private ComboBox cmbControlMode;
        private NumericUpDown numSimSpeed;
        private Button btnStart;
        private Button btnStop;
        private Button btnReset;

        // 차트
        private FormsPlot chartTemp;
        private FormsPlot chartPI;

        // 결과 표시
        private Label lblStatus;
        private Label lblElapsed;
        private Label lblMonitorTemp;
        private Label lblCh1SV;
        private Label lblCh1PV;
        private Label lblOvershoot;
        private Label lblSettlingTime;
        private Label lblPhase;

        // 로그
        private TextBox txtLog;

        #endregion

        #region 시뮬레이션 상태

        private CancellationTokenSource _cts;
        private bool _isRunning;

        // 차트 데이터
        private ChartDataList _dataTime;
        private ChartDataList _dataCh1SV;
        private ChartDataList _dataCh1PV;
        private ChartDataList _dataMonitor;
        private ChartDataList _dataRampTarget;
        private ChartDataList _dataP;
        private ChartDataList _dataI;
        private ChartDataList _dataD;
        private ChartDataList _dataError;

        // 결과 메트릭
        private double _maxOvershoot;
        private double _settlingTimeMin;
        private bool _targetReached;

        #endregion

        public BakeoutSimulationForm()
        {
            InitializeUI();
        }

        private void InitializeUI()
        {
            Text = "베이크아웃 시뮬레이션 (PI 제어 테스트)";
            Size = new Size(1200, 850);
            MinimumSize = new Size(1100, 750);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            FormBorderStyle = FormBorderStyle.Sizable;

            // ── 왼쪽 패널: 파라미터 ──
            var panelLeft = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(280, 850),
                AutoScroll = true,
                Dock = DockStyle.Left,
                Padding = new Padding(10)
            };
            Controls.Add(panelLeft);

            int y = 10;

            // 베이크아웃 파라미터
            AddSectionLabel(panelLeft, "■ 베이크아웃 파라미터", ref y);

            numInitialTemp = AddNumericRow(panelLeft, "초기 온도 (°C):", 25, -10, 200, 1, 0, ref y);
            numTargetTemp = AddNumericRow(panelLeft, "목표 온도 (°C):", 200, 50, 400, 1, 0, ref y);
            numRampRate = AddNumericRow(panelLeft, "승온 속도 (°C/h):", 200, 1, 500, 1, 0, ref y);
            numHoldTime = AddNumericRow(panelLeft, "유지 시간 (분):", 180, 1, 600, 1, 0, ref y);
            numHeaterMax = AddNumericRow(panelLeft, "히터 최대 온도 (°C):", 250, 50, 500, 1, 0, ref y);
            numTolerance = AddNumericRow(panelLeft, "도달 허용오차 (°C):", 0.1M, 0.1M, 10, 0.1M, 1, ref y);

            y += 10;
            AddSectionLabel(panelLeft, "■ 열 모델 파라미터", ref y);

            numHeaterTau = AddNumericRow(panelLeft, "히터 시정수 (초):", 30, 5, 300, 5, 0, ref y);
            numSampleTau = AddNumericRow(panelLeft, "샘플 시정수 (초):", 2400, 30, 10000, 10, 0, ref y);
            numHeatLoss = AddNumericRow(panelLeft, "방열 계수 (%/100°C):", 1, 0, 50, 1, 0, ref y);
            numHeaterInsulation = AddNumericRow(panelLeft, "히터 단열률 (%):", 98, 0, 100, 1, 0, ref y);

            y += 10;
            AddSectionLabel(panelLeft, "■ 시뮬레이션", ref y);

            panelLeft.Controls.Add(new Label { Text = "제어 모드:", Location = new Point(10, y + 3), AutoSize = true, ForeColor = Color.LightGray });
            cmbControlMode = new ComboBox
            {
                Location = new Point(100, y), Size = new Size(150, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(45, 45, 45), ForeColor = Color.White
            };
            cmbControlMode.Items.AddRange(new object[] { "기존 PI", "Adaptive (자기학습)" });
            cmbControlMode.SelectedIndex = 1;
            panelLeft.Controls.Add(cmbControlMode);
            y += 30;

            numSimSpeed = AddNumericRow(panelLeft, "가속 배율:", 10000, 1, 100000, 1000, 0, ref y);

            y += 10;

            btnStart = new Button
            {
                Text = "▶ 시작",
                Location = new Point(10, y),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White
            };
            btnStart.Click += BtnStart_Click;
            panelLeft.Controls.Add(btnStart);

            btnStop = new Button
            {
                Text = "■ 정지",
                Location = new Point(95, y),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(180, 50, 50),
                ForeColor = Color.White,
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;
            panelLeft.Controls.Add(btnStop);

            btnReset = new Button
            {
                Text = "초기화",
                Location = new Point(180, y),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White
            };
            btnReset.Click += BtnReset_Click;
            panelLeft.Controls.Add(btnReset);

            y += 45;
            AddSectionLabel(panelLeft, "■ 결과", ref y);

            lblPhase = AddResultLabel(panelLeft, "단계:", "-", ref y);
            lblElapsed = AddResultLabel(panelLeft, "경과:", "-", ref y);
            lblMonitorTemp = AddResultLabel(panelLeft, "샘플 온도:", "-", ref y);
            lblCh1SV = AddResultLabel(panelLeft, "CH1 SV:", "-", ref y);
            lblCh1PV = AddResultLabel(panelLeft, "CH1 PV:", "-", ref y);
            lblOvershoot = AddResultLabel(panelLeft, "오버슈트:", "-", ref y);
            lblSettlingTime = AddResultLabel(panelLeft, "안정시간:", "-", ref y);
            lblStatus = AddResultLabel(panelLeft, "상태:", "대기", ref y);

            // ── 오른쪽: 차트 + 로그 ──
            var panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(5)
            };
            Controls.Add(panelRight);
            panelRight.BringToFront();

            // 온도 차트
            chartTemp = new FormsPlot
            {
                Dock = DockStyle.Top,
                Height = 320
            };
            panelRight.Controls.Add(chartTemp);

            // PI 차트
            chartPI = new FormsPlot
            {
                Dock = DockStyle.Top,
                Height = 260
            };
            panelRight.Controls.Add(chartPI);

            // 로그
            txtLog = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8.5f)
            };
            panelRight.Controls.Add(txtLog);
            txtLog.BringToFront();

            SetupCharts();
        }

        private void SetupCharts()
        {
            // 온도 차트
            chartTemp.Plot.Title("Temperature");
            chartTemp.Plot.YLabel("Temp (C)");
            chartTemp.Plot.XLabel("Time (min)");
            chartTemp.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1e1e1e");
            chartTemp.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#252525");
            chartTemp.Plot.Axes.Color(ScottPlot.Color.FromHex("#cccccc"));

            // PI 차트
            chartPI.Plot.Title("PID Output");
            chartPI.Plot.YLabel("Value (C)");
            chartPI.Plot.XLabel("Time (min)");
            chartPI.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#1e1e1e");
            chartPI.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#252525");
            chartPI.Plot.Axes.Color(ScottPlot.Color.FromHex("#cccccc"));
        }

        #region 파라미터 UI 헬퍼

        private void AddSectionLabel(Control parent, string text, ref int y)
        {
            parent.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(10, y),
                AutoSize = true,
                ForeColor = Color.LightSkyBlue,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            });
            y += 22;
        }

        private NumericUpDown AddNumericRow(Control parent, string label, decimal defaultVal,
            decimal min, decimal max, decimal increment, int decimalPlaces, ref int y)
        {
            parent.Controls.Add(new Label
            {
                Text = label,
                Location = new Point(10, y + 3),
                Size = new Size(140, 18),
                ForeColor = Color.LightGray
            });

            var nud = new NumericUpDown
            {
                Location = new Point(155, y),
                Size = new Size(100, 23),
                Minimum = min,
                Maximum = max,
                Value = defaultVal,
                Increment = increment,
                DecimalPlaces = decimalPlaces,
                BackColor = Color.FromArgb(45, 45, 45),
                ForeColor = Color.White
            };
            parent.Controls.Add(nud);

            y += 28;
            return nud;
        }

        private Label AddResultLabel(Control parent, string title, string defaultValue, ref int y)
        {
            parent.Controls.Add(new Label
            {
                Text = title,
                Location = new Point(10, y + 2),
                Size = new Size(80, 18),
                ForeColor = Color.Gray
            });

            var lbl = new Label
            {
                Text = defaultValue,
                Location = new Point(95, y + 2),
                Size = new Size(160, 18),
                ForeColor = Color.White,
                Font = new Font(Font.FontFamily, 9, FontStyle.Bold)
            };
            parent.Controls.Add(lbl);

            y += 22;
            return lbl;
        }

        #endregion

        #region 시뮬레이션 실행

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            _isRunning = true;
            _cts = new CancellationTokenSource();

            btnStart.Enabled = false;
            btnStop.Enabled = true;
            SetParamsEnabled(false);

            ResetData();
            lblStatus.Text = "시뮬레이션 중...";
            lblStatus.ForeColor = Color.Lime;

            try
            {
                await RunSimulationAsync(_cts.Token);
                if (!_cts.IsCancellationRequested)
                {
                    lblStatus.Text = "완료";
                    lblStatus.ForeColor = Color.Cyan;
                    AppendLog("=== 시뮬레이션 완료 ===");
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "중단됨";
                lblStatus.ForeColor = Color.Orange;
                AppendLog("=== 시뮬레이션 중단 ===");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "오류";
                lblStatus.ForeColor = Color.Red;
                AppendLog($"오류: {ex.Message}");
            }
            finally
            {
                _isRunning = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                SetParamsEnabled(true);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _cts?.Cancel();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (_isRunning) return;
            ResetData();
            UpdateCharts();
            txtLog.Clear();
            ResetResultLabels();
        }

        private void SetParamsEnabled(bool enabled)
        {
            numTargetTemp.Enabled = enabled;
            numRampRate.Enabled = enabled;
            numHoldTime.Enabled = enabled;
            numHeaterMax.Enabled = enabled;
            numTolerance.Enabled = enabled;
            numInitialTemp.Enabled = enabled;
            numHeaterTau.Enabled = enabled;
            numSampleTau.Enabled = enabled;
            numHeatLoss.Enabled = enabled;
            numHeaterInsulation.Enabled = enabled;
            numSimSpeed.Enabled = enabled;
        }

        private void ResetData()
        {
            _dataTime = new ChartDataList();
            _dataCh1SV = new ChartDataList();
            _dataCh1PV = new ChartDataList();
            _dataMonitor = new ChartDataList();
            _dataRampTarget = new ChartDataList();
            _dataP = new ChartDataList();
            _dataI = new ChartDataList();
            _dataD = new ChartDataList();
            _dataError = new ChartDataList();
            _maxOvershoot = 0;
            _settlingTimeMin = 0;
            _targetReached = false;
        }

        private void ResetResultLabels()
        {
            lblPhase.Text = "-";
            lblElapsed.Text = "-";
            lblMonitorTemp.Text = "-";
            lblCh1SV.Text = "-";
            lblCh1PV.Text = "-";
            lblOvershoot.Text = "-";
            lblSettlingTime.Text = "-";
            lblStatus.Text = "대기";
            lblStatus.ForeColor = Color.White;
        }

        private async Task RunSimulationAsync(CancellationToken ct)
        {
            // 파라미터 읽기
            double targetTemp = (double)numTargetTemp.Value;
            double rampRate = (double)numRampRate.Value;       // °C/h
            double holdMinutes = (double)numHoldTime.Value;
            double heaterMax = (double)numHeaterMax.Value;
            double tolerance = (double)numTolerance.Value;
            double initialTemp = (double)numInitialTemp.Value;
            double heaterTau = (double)numHeaterTau.Value;     // 초
            double sampleTau = (double)numSampleTau.Value;     // 초
            double heatLossPct = (double)numHeatLoss.Value;    // %/100°C
            double heaterInsulationPct = (double)numHeaterInsulation.Value; // %
            int simSpeed = (int)numSimSpeed.Value;
            bool useAdaptive = cmbControlMode.SelectedIndex == 1;

            double rampRatePerSec = rampRate / 3600.0;
            double effectiveMax = heaterMax;
            double maxIntegral = heaterMax - targetTemp;

            // 시뮬레이션 시간 단위: 5초 (PI 피드백 주기)
            const double dt = 5.0; // 실제 시간 5초

            // 방열: 샘플이 주변보다 100°C 높을 때 매 사이클(5초)마다 heatLossPct% 손실
            // lossFactor × (온도-실온) = 매 사이클 온도 하강분 (°C)
            double ambientTemp = initialTemp;
            double lossFactor = (heatLossPct / 100.0) * dt / 100.0;

            // 열 모델 초기 상태
            double ch1SV = initialTemp;
            double ch1PV = initialTemp;
            double monitorTemp = initialTemp;
            double prevMonitor = double.NaN;

            // PID 상태
            double integralTerm = 0;
            double lastSetpoint = initialTemp;
            double observedThermalLag = 0;
            double smoothedRate = 0; // 변화율 이동평균

            double simTimeSec = 0;
            bool temperatureReached = false;
            double holdStartSec = 0;

            int logInterval = Math.Max(1, (int)(60.0 / dt)); // 1분마다 로그
            int stepCount = 0;

            // ── 도달 가능성 사전 검증 ──
            // 히터 PV 평형: alphaH × (heaterMax - ch1PV_eq) = lossFactor × (ch1PV_eq - ambient) × (1 - insulation/100)
            double alphaH = 1.0 - Math.Exp(-dt / heaterTau);
            double heaterLossFrac = lossFactor * (1.0 - heaterInsulationPct / 100.0);
            double ch1PV_eq = (alphaH * heaterMax + heaterLossFrac * ambientTemp) / (alphaH + heaterLossFrac);
            // 샘플 평형: alphaS × (ch1PV_eq - sample_eq) = lossFactor × (sample_eq - ambient)
            double alphaS = 1.0 - Math.Exp(-dt / sampleTau);
            double sample_eq = (alphaS * ch1PV_eq + lossFactor * ambientTemp) / (alphaS + lossFactor);

            AppendLog($"=== 시뮬레이션 시작 ===");
            AppendLog($"목표: {targetTemp}°C, 승온: {rampRate}°C/h, 유지: {holdMinutes}분");
            AppendLog($"히터 최대: {heaterMax}°C, 감속구간: 자동 (열 지연 기반)");
            AppendLog($"열 모델: 히터τ={heaterTau}s, 샘플τ={sampleTau}s, 방열={heatLossPct}%/100°C, 히터 단열={heaterInsulationPct}%");
            AppendLog($"이론적 최대 도달 온도: 히터 PV={ch1PV_eq:F1}°C, 샘플={sample_eq:F1}°C");
            if (sample_eq < targetTemp - tolerance)
            {
                double shortage = targetTemp - sample_eq;
                AppendLog($"");
                AppendLog($"⚠ [경고] 현재 파라미터로 목표 온도 도달 불가!");
                AppendLog($"  샘플 이론 최대 {sample_eq:F1}°C < 목표 {targetTemp:F1}°C (부족: {shortage:F1}°C)");
                // 필요 히터 최대 온도 역산: target = (alphaS * ch1PV_needed + lossFactor * ambient) / (alphaS + lossFactor)
                double ch1PV_needed = (targetTemp * (alphaS + lossFactor) - lossFactor * ambientTemp) / alphaS;
                double heaterMax_needed = (ch1PV_needed * (alphaH + heaterLossFrac) - heaterLossFrac * ambientTemp) / alphaH;
                AppendLog($"  → 히터 최대 온도를 {heaterMax_needed:F0}°C 이상으로 설정 필요");
                AppendLog($"");
            }
            AppendLog("");

            if (useAdaptive)
            {
                await RunAdaptiveSimAsync(ct, targetTemp, rampRate, holdMinutes, heaterMax,
                    tolerance, initialTemp, heaterTau, sampleTau, heatLossPct,
                    heaterInsulationPct, simSpeed, ambientTemp, alphaH, alphaS, lossFactor);
                return;
            }

            // ── 기존 PI: 승온 + 유지 루프 ──
            while (true)
            {
                ct.ThrowIfCancellationRequested();

                double timeMin = simTimeSec / 60.0;
                string phase;

                if (!temperatureReached)
                {
                    // ── 승온 단계 ──
                    phase = "승온";

                    // 램프 타겟 계산
                    double rampedTarget = Math.Min(
                        initialTemp + rampRatePerSec * simTimeSec,
                        targetTemp);

                    // PI 에러
                    double error = rampedTarget - monitorTemp;

                    // 변화율: 이동평균으로 노이즈 필터링
                    double rawRate = double.IsNaN(prevMonitor) ? 0 : (monitorTemp - prevMonitor);
                    smoothedRate = smoothedRate * 0.7 + rawRate * 0.3;
                    double rateOfChange = smoothedRate;

                    double integralGain = Ki_norm;
                    if (rateOfChange > 0.5 && error > 0)
                        integralGain *= 0.3;

                    // 적응형 감속 구간: 현재 열 지연 추적 (빠른 감쇠)
                    double currentLag = Math.Max(0, ch1PV - monitorTemp);
                    // 빠른 추적: 증가 시 즉시, 감소 시 EMA(α=0.02)
                    if (currentLag > observedThermalLag && rateOfChange > 0.05)
                        observedThermalLag = currentLag;
                    else
                        observedThermalLag = observedThermalLag * 0.98 + currentLag * 0.02;
                    double decelZone = Math.Max(5.0, currentLag * 1.5); // 현재 지연 기반

                    // 축적 에너지 분석: 현재 열 지연 기반 (과도 상태 인플레이션 방지)
                    double distToTarget = targetTemp - monitorTemp;
                    double storedEnergy = ch1PV - (targetTemp + currentLag);
                    // 근접도: 목표에 가까울수록 1, 멀수록 0
                    double proximity = decelZone > 0
                        ? Math.Max(0, 1.0 - distToTarget / decelZone) : 0;

                    // 적응형 게인
                    double lagScale = observedThermalLag > 1 ? observedThermalLag / 20.0 : 0.5;
                    lagScale = Math.Max(0.3, Math.Min(lagScale, 2.0));
                    double effKp = Kp * Math.Sqrt(lagScale);
                    double effKd = Kd / Math.Sqrt(lagScale);

                    if (error > 0)
                    {
                        double growthRate = integralGain;
                        if (storedEnergy > 0 && decelZone > 0 && distToTarget > 0 && distToTarget < decelZone)
                            growthRate *= distToTarget / decelZone;
                        if (storedEnergy > 0 && proximity > 0.5)
                            growthRate *= (1.0 - proximity);
                        integralTerm += error * growthRate;
                    }
                    else
                    {
                        integralTerm += error * Ki_norm * 1.5;
                    }

                    // 적분 감쇠: storedEnergy > 2°C일 때만
                    if (storedEnergy > 2.0 && proximity > 0.3 && integralTerm > 0)
                    {
                        double decayRate = ((storedEnergy - 2.0) / Math.Max(1, observedThermalLag)) * proximity;
                        decayRate = Math.Min(decayRate, 0.05);
                        integralTerm *= (1.0 - decayRate);
                    }
                    double minIntegralForLag = proximity > 0.3 ? observedThermalLag * 0.85 : 0;
                    integralTerm = Math.Max(minIntegralForLag, Math.Min(integralTerm, maxIntegral));

                    // 모델 기반 적분 수렴 가속 (상향만)
                    if (proximity > 0.5 && observedThermalLag > 1)
                    {
                        double estI = observedThermalLag * 0.9;
                        if (integralTerm < estI - 1.0)
                            integralTerm += (estI - integralTerm) * 0.2;
                    }

                    // D항: 양방향
                    double dTerm = -effKd * rateOfChange;

                    // 에너지 보상
                    double energyComp = 0;
                    if (storedEnergy > 0 && proximity > 0.3)
                        energyComp = -storedEnergy * 0.15 * proximity;

                    // PID 출력 → CH1 SV
                    double newSV = rampedTarget + error * effKp + integralTerm + dTerm + energyComp;

                    // 상한: rampedTarget + 열지연 기반 vs 히터 최대
                    double svCeiling = rampedTarget + Math.Max(observedThermalLag, 5) * 1.5;
                    double upperLimit = Math.Min(effectiveMax, svCeiling);
                    // 하한
                    double estimatedSteadySV = rampedTarget + observedThermalLag * 0.85;
                    double svLowerBound = Math.Max(estimatedSteadySV, monitorTemp - currentLag * 0.3);
                    newSV = Math.Max(svLowerBound, Math.Min(newSV, upperLimit));

                    // SV 감소 속도 제한
                    if (newSV < lastSetpoint)
                    {
                        if (monitorTemp < targetTemp)
                            newSV = lastSetpoint;
                        else if (monitorTemp <= targetTemp + tolerance)
                            newSV = Math.Max(newSV, lastSetpoint - 0.1);
                        else
                            newSV = Math.Max(newSV, lastSetpoint - 0.3);
                    }

                    ch1SV = newSV;
                    lastSetpoint = newSV;

                    // 데이터 기록
                    _dataRampTarget.Add(rampedTarget);
                    _dataP.Add(error * Kp);
                    _dataI.Add(integralTerm);
                    _dataD.Add(dTerm);
                    _dataError.Add(error);

                    // 목표 도달 확인 (허용 오차 적용)
                    if (monitorTemp >= targetTemp - tolerance)
                    {
                        temperatureReached = true;
                        holdStartSec = simTimeSec;
                        AppendLog($"[{timeMin:F1}분] ★ 목표 온도 도달! 유지 단계 진입");
                        AppendLog($"  열 지연: {observedThermalLag:F1}°C → 감속구간: {decelZone:F1}°C (자동)");
                    }

                    if (stepCount % logInterval == 0)
                    {
                        AppendLog($"[{timeMin:F1}분] 승온 | 램프:{rampedTarget:F1}°C " +
                            $"샘플:{monitorTemp:F1}°C SV:{ch1SV:F1}°C PV:{ch1PV:F1}°C " +
                            $"Lag:{observedThermalLag:F1} Kp:{effKp:F2} I:{integralTerm:F1}");
                    }
                }
                else
                {
                    // ── 유지 단계 ──
                    phase = "유지";
                    double holdElapsed = simTimeSec - holdStartSec;
                    double holdRemain = holdMinutes * 60 - holdElapsed;

                    if (holdRemain <= 0)
                    {
                        AppendLog($"[{timeMin:F1}분] ★ 유지 시간 완료!");
                        break;
                    }

                    // 유지 PID
                    double holdError = targetTemp - monitorTemp;
                    double holdRateRaw = double.IsNaN(prevMonitor) ? 0 : (monitorTemp - prevMonitor);
                    smoothedRate = smoothedRate * 0.7 + holdRateRaw * 0.3;

                    if (holdError > 0)
                        integralTerm += holdError * Ki_norm;
                    else
                        integralTerm += holdError * Ki_norm * 3;

                    // 축적 에너지: 현재 열 지연 기반
                    double holdCurrentLag = Math.Max(0, ch1PV - monitorTemp);
                    double holdStoredEnergy = ch1PV - (targetTemp + holdCurrentLag);
                    if (holdStoredEnergy > 0 && smoothedRate > 0 && integralTerm > 0)
                    {
                        double decayRate = holdStoredEnergy / Math.Max(1, holdCurrentLag);
                        integralTerm *= Math.Max(0.0, 1.0 - decayRate);
                    }
                    integralTerm = Math.Max(0, Math.Min(integralTerm, maxIntegral));

                    // D항: 이동평균 기반
                    double holdDTerm = smoothedRate > 0 ? -Kd * smoothedRate : 0;

                    // 축적 에너지 보상: 온도 상승 중일 때만
                    double holdEnergyComp = 0;
                    if (holdStoredEnergy > 0 && smoothedRate > 0)
                        holdEnergyComp = -holdStoredEnergy * 0.5;

                    double newSV = targetTemp + holdError * Kp + integralTerm + holdDTerm + holdEnergyComp;

                    newSV = Math.Max(initialTemp, Math.Min(newSV, effectiveMax));

                    ch1SV = newSV;

                    _dataRampTarget.Add(targetTemp);
                    _dataP.Add(holdError * Kp);
                    _dataI.Add(integralTerm);
                    _dataD.Add(holdDTerm);
                    _dataError.Add(holdError);

                    // 오버슈트 측정
                    double overshoot = monitorTemp - targetTemp;
                    if (overshoot > _maxOvershoot)
                        _maxOvershoot = overshoot;

                    // 안정 판정 (±0.5°C 이내)
                    if (Math.Abs(monitorTemp - targetTemp) <= 0.5)
                    {
                        if (_settlingTimeMin == 0)
                            _settlingTimeMin = (simTimeSec - holdStartSec) / 60.0;
                    }
                    else
                    {
                        _settlingTimeMin = 0; // 아직 안정 안됨
                    }

                    if (stepCount % logInterval == 0)
                    {
                        AppendLog($"[{timeMin:F1}분] 유지 | 잔여:{holdRemain / 60:F1}분 " +
                            $"샘플:{monitorTemp:F1}°C CH1_SV:{ch1SV:F1}°C CH1_PV:{ch1PV:F1}°C " +
                            $"편차:{holdError:F2}°C I:{integralTerm:F2} D:{holdDTerm:F2}");
                    }
                }

                // ── 열 모델 업데이트 ──
                // CH1 PV: 1차 지연 응답 (히터 → 히터 표면) + 히터 방열
                double alphaHeater = 1.0 - Math.Exp(-dt / heaterTau);
                ch1PV += alphaHeater * (ch1SV - ch1PV);
                ch1PV -= lossFactor * (ch1PV - ambientTemp) * (1.0 - heaterInsulationPct / 100.0);

                // 모니터(샘플) 온도: 1차 지연 응답 (히터 표면 → 샘플) + 샘플 방열
                prevMonitor = monitorTemp;
                double alphaSample = 1.0 - Math.Exp(-dt / sampleTau);
                monitorTemp += alphaSample * (ch1PV - monitorTemp);
                monitorTemp -= lossFactor * (monitorTemp - ambientTemp); // 샘플 방열 (주변으로)

                // 데이터 저장
                _dataTime.Add(timeMin);
                _dataCh1SV.Add(ch1SV);
                _dataCh1PV.Add(ch1PV);
                _dataMonitor.Add(monitorTemp);

                // UI 업데이트 (고배율 시 빈도 감소)
                int uiInterval = Math.Max(10, simSpeed / 100);
                if (stepCount % uiInterval == 0)
                {
                    UpdateUI(phase, timeMin, monitorTemp, ch1SV, ch1PV);
                    UpdateCharts();
                }

                simTimeSec += dt;
                stepCount++;

                // 실제 대기 (가속 적용) — 고배율 시 여러 스텝을 묶어 처리
                int stepsPerDelay = Math.Max(1, simSpeed / 1000); // 1000배율 기준 1스텝, 10000이면 10스텝
                if (stepCount % stepsPerDelay == 0)
                {
                    await Task.Delay(1, ct); // UI 응답성 유지
                }

                // 안전 타임아웃: 시뮬레이션 24시간 초과 시 중단
                if (simTimeSec > 86400)
                {
                    AppendLog("시뮬레이션 시간 24시간 초과 — 중단");
                    break;
                }
            }

            // 최종 차트 업데이트
            UpdateCharts();
            UpdateUI("완료", simTimeSec / 60.0, monitorTemp, ch1SV, ch1PV);
        }

        #endregion

        #region UI 업데이트

        private void UpdateUI(string phase, double timeMin, double monitor, double sv, double pv)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateUI(phase, timeMin, monitor, sv, pv)));
                return;
            }

            lblPhase.Text = phase;
            lblPhase.ForeColor = phase == "승온" ? Color.Orange : phase == "유지" ? Color.Lime : Color.Cyan;
            lblElapsed.Text = $"{timeMin:F1} 분";
            lblMonitorTemp.Text = $"{monitor:F1} °C";
            lblCh1SV.Text = $"{sv:F1} °C";
            lblCh1PV.Text = $"{pv:F1} °C";
            lblOvershoot.Text = _maxOvershoot > 0.1 ? $"{_maxOvershoot:F2} °C" : "-";
            lblSettlingTime.Text = _settlingTimeMin > 0 ? $"{_settlingTimeMin:F1} 분" : "-";
        }

        private void UpdateCharts()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateCharts));
                return;
            }

            if (_dataTime == null || _dataTime.Count == 0) return;

            var timeArr = _dataTime.ToArray();

            // 온도 차트
            chartTemp.Plot.Clear();
            chartTemp.Plot.Title("Temperature");
            chartTemp.Plot.YLabel("Temp (C)");
            chartTemp.Plot.XLabel("Time (min)");

            var sigSV = chartTemp.Plot.Add.Scatter(timeArr, _dataCh1SV.ToArray());
            sigSV.LineWidth = 2; sigSV.MarkerSize = 0;
            sigSV.Color = ScottPlot.Color.FromHex("#FF6666");
            sigSV.LegendText = "CH1 SV";

            var sigPV = chartTemp.Plot.Add.Scatter(timeArr, _dataCh1PV.ToArray());
            sigPV.LineWidth = 2; sigPV.MarkerSize = 0;
            sigPV.Color = ScottPlot.Color.FromHex("#FF9999");
            sigPV.LegendText = "CH1 PV";

            var sigMon = chartTemp.Plot.Add.Scatter(timeArr, _dataMonitor.ToArray());
            sigMon.LineWidth = 2.5f; sigMon.MarkerSize = 0;
            sigMon.Color = ScottPlot.Color.FromHex("#66CCFF");
            sigMon.LegendText = "Sample";

            var sigRamp = chartTemp.Plot.Add.Scatter(timeArr, _dataRampTarget.ToArray());
            sigRamp.LineWidth = 1.5f; sigRamp.MarkerSize = 0;
            sigRamp.Color = ScottPlot.Color.FromHex("#88FF88");
            sigRamp.LineStyle.Pattern = ScottPlot.LinePattern.Dashed;
            sigRamp.LegendText = "Ramp Target";

            chartTemp.Plot.Legend.IsVisible = true;
            chartTemp.Plot.Legend.Alignment = ScottPlot.Alignment.UpperLeft;
            chartTemp.Refresh();

            // PI 차트
            chartPI.Plot.Clear();
            chartPI.Plot.Title("PID Output");
            chartPI.Plot.YLabel("Value (C)");
            chartPI.Plot.XLabel("Time (min)");

            var sigP = chartPI.Plot.Add.Scatter(timeArr, _dataP.ToArray());
            sigP.LineWidth = 1.5f; sigP.MarkerSize = 0;
            sigP.Color = ScottPlot.Color.FromHex("#FF8800");
            sigP.LegendText = "P";

            var sigI = chartPI.Plot.Add.Scatter(timeArr, _dataI.ToArray());
            sigI.LineWidth = 1.5f; sigI.MarkerSize = 0;
            sigI.Color = ScottPlot.Color.FromHex("#00CCFF");
            sigI.LegendText = "I";

            var sigD = chartPI.Plot.Add.Scatter(timeArr, _dataD.ToArray());
            sigD.LineWidth = 1.5f; sigD.MarkerSize = 0;
            sigD.Color = ScottPlot.Color.FromHex("#FF44FF");
            sigD.LegendText = "D";

            var sigErr = chartPI.Plot.Add.Scatter(timeArr, _dataError.ToArray());
            sigErr.LineWidth = 1; sigErr.MarkerSize = 0;
            sigErr.Color = ScottPlot.Color.FromHex("#AAAAAA");
            sigErr.LineStyle.Pattern = ScottPlot.LinePattern.Dotted;
            sigErr.LegendText = "Error";

            chartPI.Plot.Legend.IsVisible = true;
            chartPI.Plot.Legend.Alignment = ScottPlot.Alignment.UpperRight;
            chartPI.Refresh();
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLog(text)));
                return;
            }

            txtLog.AppendText(text + Environment.NewLine);
        }

        #endregion

        #region 차트 데이터 리스트

        /// <summary>
        /// 간단한 동적 배열 (시뮬레이션 데이터 수집용)
        /// </summary>
        private class ChartDataList
        {
            private double[] _data;
            private int _count;

            public ChartDataList(int initialCapacity = 4096)
            {
                _data = new double[initialCapacity];
                _count = 0;
            }

            public int Count => _count;

            public void Add(double value)
            {
                if (_count >= _data.Length)
                {
                    var newArr = new double[_data.Length * 2];
                    Array.Copy(_data, newArr, _count);
                    _data = newArr;
                }
                _data[_count++] = value;
            }

            public double[] ToArray()
            {
                var result = new double[_count];
                Array.Copy(_data, result, _count);
                return result;
            }
        }

        #endregion

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _cts?.Cancel();
            base.OnFormClosing(e);
        }

        #region Adaptive 시뮬레이션

        private async Task RunAdaptiveSimAsync(CancellationToken ct,
            double targetTemp, double rampRate, double holdMinutes, double heaterMax,
            double tolerance, double initialTemp, double heaterTau, double sampleTau,
            double heatLossPct, double heaterInsulationPct, int simSpeed,
            double ambientTemp, double alphaH, double alphaS, double lossFactor)
        {
            const double dt = 1.0; // 1초 주기 (Adaptive는 1초)

            double ch1SV = initialTemp;
            double ch1PV = initialTemp;
            double monitorTemp = initialTemp;

            // Adaptive 상태
            double offset = Math.Max(3.0, 0.05 * (targetTemp - initialTemp));
            double probeStep = offset;
            int probeEsc = 0;
            double equilibriumOffset = 0;
            double holdCorrection = 0;
            string phase = "Probe";
            bool temperatureReached = false;
            double holdStartSec = 0;

            // 변화율 버퍼
            var rateBuf = new Queue<(double time, double temp)>();
            double smoothRate = 0;

            double simTimeSec = 0;
            int logInterval = Math.Max(1, 60); // 1분마다
            int stepCount = 0;
            double probeCheckSec = 0;

            AppendLog($"=== Adaptive 시뮬레이션 시작 ===");
            AppendLog($"목표: {targetTemp}°C, 승온: {rampRate}°C/h, 히터 상한: {heaterMax}°C");

            while (true)
            {
                ct.ThrowIfCancellationRequested();

                double timeMin = simTimeSec / 60.0;

                // === 열 모델 시뮬레이션 ===
                double ch1Response = 1.0 - Math.Exp(-dt / heaterTau);
                ch1PV += ch1Response * (ch1SV - ch1PV);
                ch1PV -= lossFactor * (ch1PV - ambientTemp) * (1 - heaterInsulationPct / 100.0) * dt;

                double sampleResponse = 1.0 - Math.Exp(-dt / sampleTau);
                monitorTemp += sampleResponse * (ch1PV - monitorTemp) * dt;
                monitorTemp -= lossFactor * (monitorTemp - ambientTemp) * dt;

                // 변화율 추정
                rateBuf.Enqueue((simTimeSec, monitorTemp));
                while (rateBuf.Count > 1 && rateBuf.Last().time - rateBuf.First().time > 30)
                    rateBuf.Dequeue();
                double rate = CalcSimRate(rateBuf);

                // === 상태별 제어 ===
                if (phase == "Probe")
                {
                    ch1SV = Math.Min(monitorTemp + offset, heaterMax);

                    if (simTimeSec - probeCheckSec >= 20)
                    {
                        probeCheckSec = simTimeSec;
                        if (Math.Abs(rate) < 0.015)
                        {
                            probeEsc++;
                            offset += probeStep;
                            offset = Math.Min(offset, heaterMax - monitorTemp - 3);
                            AppendLog($"[{timeMin:F1}분] Probe #{probeEsc}: offset→{offset:F1}°C");
                            if (probeEsc > 10) { phase = "Ramp"; AppendLog($"→ Ramp (강제)"); }
                        }
                        else
                        {
                            // 응답 감지 → Ramp 전이
                            if (Math.Abs(rate) > 0.005)
                            {
                                double ratio = offset / rate;
                                offset = (rampRate / 60.0) * ratio;
                                offset = Math.Max(0.5, Math.Min(offset, heaterMax - monitorTemp - 3));
                            }
                            phase = "Ramp";
                            AppendLog($"[{timeMin:F1}분] → Ramp: offset={offset:F1}°C (rate={rate:F3}°C/min)");
                        }
                    }
                }
                else if (phase == "Ramp")
                {
                    double rateTarget = rampRate / 60.0;
                    double rampedTarget = Math.Min(initialTemp + rateTarget * timeMin, targetTemp);

                    double convBand = Math.Max(1.5, (targetTemp - initialTemp) * 0.05);
                    if (monitorTemp >= targetTemp - convBand)
                    {
                        phase = "Converge";
                        equilibriumOffset = offset * 0.3;
                        AppendLog($"[{timeMin:F1}분] → Converge: T_s={monitorTemp:F1}°C");
                    }
                    else
                    {
                        double rateError = rateTarget - rate;
                        double gain = 0.15 / Math.Max(1.0, offset * 0.5);
                        offset += gain * rateError;
                        offset = Math.Max(0.2, Math.Min(offset, heaterMax - monitorTemp - 3));

                        double trackErr = rampedTarget - monitorTemp;
                        double corr = Math.Max(-2.0, Math.Min(trackErr * 0.1, 3.0));
                        ch1SV = Math.Min(monitorTemp + offset + corr, heaterMax);
                    }
                }
                else if (phase == "Converge")
                {
                    double remaining = targetTemp - monitorTemp;
                    double convBand = Math.Max(1.5, (targetTemp - initialTemp) * 0.05);
                    double frac = Math.Max(0, Math.Min(1, remaining / convBand));
                    double rateTarget = (rampRate / 60.0) * frac;

                    double rateError = rateTarget - rate;
                    double gain = 0.1 / Math.Max(1.0, offset * 0.5);
                    offset += gain * rateError;
                    offset = Math.Max(0.2, Math.Min(offset, heaterMax - monitorTemp - 3));

                    double pull = remaining * 0.15;
                    ch1SV = Math.Min(monitorTemp + offset + pull, heaterMax);

                    if (frac < 0.2)
                        equilibriumOffset = 0.95 * equilibriumOffset + 0.05 * (ch1PV - targetTemp);

                    if (Math.Abs(remaining) < 0.5 && Math.Abs(rate) < 0.03 && simTimeSec > 60)
                    {
                        equilibriumOffset = ch1PV - targetTemp;
                        phase = "Hold";
                        holdStartSec = simTimeSec;
                        temperatureReached = true;
                        holdCorrection = 0;
                        AppendLog($"[{timeMin:F1}분] → Hold: eq_offset={equilibriumOffset:F1}°C");
                    }
                }
                else if (phase == "Hold")
                {
                    ch1SV = Math.Min(targetTemp + equilibriumOffset + holdCorrection, heaterMax);

                    double holdElapsed = simTimeSec - holdStartSec;
                    if (holdElapsed > holdMinutes * 60)
                    {
                        AppendLog($"[{timeMin:F1}분] ★ 유지 완료!");
                        break;
                    }

                    // 60초마다 보정
                    if ((int)holdElapsed % 60 == 0 && holdElapsed > 5)
                    {
                        double err = targetTemp - monitorTemp;
                        if (Math.Abs(err) > 0.3)
                        {
                            double adj = Math.Max(-1.0, Math.Min(err * 0.3, 1.0));
                            holdCorrection += adj;
                            equilibriumOffset += holdCorrection * 0.1;
                            holdCorrection *= 0.9;
                        }
                    }
                }

                // 오버슈트 추적
                if (monitorTemp > targetTemp + 0.1 && monitorTemp - targetTemp > _maxOvershoot)
                    _maxOvershoot = monitorTemp - targetTemp;

                // 데이터 기록
                _dataTime.Add(timeMin);
                _dataCh1SV.Add(ch1SV);
                _dataCh1PV.Add(ch1PV);
                _dataMonitor.Add(monitorTemp);
                _dataRampTarget.Add(phase == "Ramp" ?
                    Math.Min(initialTemp + (rampRate / 60.0) * timeMin, targetTemp) : targetTemp);
                _dataP.Add(offset);
                _dataI.Add(equilibriumOffset);
                _dataD.Add(rate);
                _dataError.Add(targetTemp - monitorTemp);

                // 로그
                if (stepCount % logInterval == 0)
                {
                    AppendLog($"[{timeMin:F1}분] {phase} | 샘플:{monitorTemp:F1}°C SV:{ch1SV:F1}°C PV:{ch1PV:F1}°C offset:{offset:F1} rate:{rate:F3}°C/min");
                }

                // UI 갱신
                if (stepCount % 10 == 0)
                {
                    UpdateUI(phase, timeMin, monitorTemp, ch1SV, ch1PV);
                    UpdateCharts();
                    await Task.Delay(Math.Max(1, 1000 / simSpeed), ct);
                }

                simTimeSec += dt;
                stepCount++;

                if (simTimeSec > 86400) { AppendLog("24시간 초과 — 종료"); break; }
            }

            UpdateCharts();
            AppendLog($"=== 시뮬레이션 완료 ===");
            AppendLog($"오버슈트: {_maxOvershoot:F2}°C");
            AppendLog($"평형 오프셋: {equilibriumOffset:F1}°C");
        }

        private double CalcSimRate(Queue<(double time, double temp)> buf)
        {
            if (buf.Count < 5) return 0;
            var data = buf.ToArray();
            double t0 = data[0].time; int n = data.Length;
            double sT = 0, sV = 0, sTT = 0, sTV = 0;
            for (int i = 0; i < n; i++)
            {
                double t = data[i].time - t0;
                sT += t; sV += data[i].temp; sTT += t * t; sTV += t * data[i].temp;
            }
            double d = n * sTT - sT * sT;
            return Math.Abs(d) < 1e-10 ? 0 : (n * sTV - sT * sV) / d * 60.0;
        }

        private double _targetTemp_sim; // for overshoot tracking

        #endregion
    }
}

