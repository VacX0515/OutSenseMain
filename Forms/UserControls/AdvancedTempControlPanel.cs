using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;
using VacX_OutSense.Core.Devices.TempController;
using VacX_OutSense.Core.Control;
using VacX_OutSense.Core.Extensions;
using static VacX_OutSense.Core.Control.ExpertParameterSystem;

namespace VacX_OutSense.Forms.UserControls
{
    /// <summary>
    /// 통합 온도 제어 패널 - UserControl 상속 수정 버전
    /// </summary>
    public partial class AdvancedTempControlPanel : UserControl
    {
        #region 필드

        private TempController _tempController;
        private IntelligentRampController _rampController;
        private ExperimentProfile _currentProfile;
        private System.Windows.Forms.Timer _updateTimer;
        private bool _isRunning;
        private string _profilesPath;

        // UI 컨트롤
        private TabControl tabMain;
        private TabPage tabControl;
        private TabPage tabProfile;

        // 제어 탭 컨트롤
        private GroupBox grpQuickSetup;
        private GroupBox grpRampControl;
        private GroupBox grpStatus;
        private ComboBox cboQuickProfile;
        private NumericUpDown nudTargetTemp;
        private NumericUpDown nudRampRate;
        private Button btnStart;
        private Button btnStop;
        private Button btnEmergencyStop;
        private Label lblCurrentProfile;
        private Label lblCurrentTemp;
        private Label lblHeaterTemp;
        private Label lblProgress;
        private ProgressBar prgProgress;
        private TextBox txtStatus;

        // 프로파일 탭 컨트롤
        private PropertyGrid propGrid;
        private ListBox lstProfiles;

        #endregion

        #region 생성자

        public AdvancedTempControlPanel()
        {
            InitializeComponents();
            InitializeProfilesPath();
            LoadDefaultProfile();
        }

        #endregion

        #region 공개 메서드

        /// <summary>
        /// 온도 컨트롤러 초기화
        /// </summary>
        public void Initialize(TempController controller)
        {
            _tempController = controller;
            _rampController = new IntelligentRampController(controller);

            // 이벤트 연결
            _rampController.StatusChanged += (s, e) =>
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => UpdateStatus(e)));
                }
                else
                {
                    UpdateStatus(e);
                }
            };

            _rampController.ErrorOccurred += (s, error) => LogStatus($"오류: {error}");
            _rampController.RampCompleted += (s, e) => OnRampCompleted(e);

            _updateTimer.Start();

            LogStatus("온도 제어 시스템 초기화 완료");
        }

        #endregion

        #region UI 초기화

        private void InitializeComponents()
        {
            this.Size = new Size(900, 650);
            this.Font = new Font("맑은 고딕", 9F);

            // 메인 탭 컨트롤
            tabMain = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // 탭 페이지 생성
            tabControl = new TabPage("제어");
            tabProfile = new TabPage("프로파일");

            CreateControlTab();
            CreateProfileTab();

            tabMain.TabPages.AddRange(new[] { tabControl, tabProfile });
            this.Controls.Add(tabMain);

            // 타이머 설정
            _updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            _updateTimer.Tick += UpdateTimer_Tick;
        }

        private void CreateControlTab()
        {
            // 빠른 설정 그룹
            grpQuickSetup = new GroupBox
            {
                Text = "빠른 설정",
                Location = new Point(10, 10),
                Size = new Size(860, 100)
            };

            Label lblProfile = new Label
            {
                Text = "프로파일:",
                Location = new Point(20, 30),
                Size = new Size(70, 25)
            };

            cboQuickProfile = new ComboBox
            {
                Location = new Point(90, 28),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboQuickProfile.SelectedIndexChanged += CboQuickProfile_SelectedIndexChanged;

            Button btnBeginner = new Button
            {
                Text = "초보자",
                Location = new Point(310, 27),
                Size = new Size(80, 28),
                BackColor = Color.LightGreen
            };
            btnBeginner.Click += (s, e) => LoadProfile("초보자_안전모드");

            Button btnStandard = new Button
            {
                Text = "표준",
                Location = new Point(395, 27),
                Size = new Size(80, 28),
                BackColor = Color.LightBlue
            };
            btnStandard.Click += (s, e) => LoadProfile("표준_균형모드");

            Button btnExpert = new Button
            {
                Text = "전문가",
                Location = new Point(480, 27),
                Size = new Size(80, 28),
                BackColor = Color.LightCoral
            };
            btnExpert.Click += (s, e) => LoadProfile("전문가_고속모드");

            lblCurrentProfile = new Label
            {
                Text = "현재: 프로파일 없음",
                Location = new Point(20, 65),
                Size = new Size(540, 20),
                Font = new Font("맑은 고딕", 9F, FontStyle.Bold),
                ForeColor = Color.DarkBlue
            };

            grpQuickSetup.Controls.AddRange(new Control[] {
                lblProfile, cboQuickProfile, btnBeginner, btnStandard, btnExpert, lblCurrentProfile
            });

            // Ramp 제어 그룹
            grpRampControl = new GroupBox
            {
                Text = "Ramp 제어",
                Location = new Point(10, 120),
                Size = new Size(860, 150)
            };

            Label lblTarget = new Label
            {
                Text = "목표 온도:",
                Location = new Point(20, 30),
                Size = new Size(80, 25)
            };

            nudTargetTemp = new NumericUpDown
            {
                Location = new Point(100, 28),
                Size = new Size(80, 25),
                Minimum = 0,
                Maximum = 200,
                Value = 85,
                DecimalPlaces = 1,
                Increment = 0.1M
            };

            Label lblTempUnit = new Label
            {
                Text = "°C",
                Location = new Point(185, 30),
                Size = new Size(30, 25)
            };

            Label lblRate = new Label
            {
                Text = "승온 속도:",
                Location = new Point(230, 30),
                Size = new Size(80, 25)
            };

            nudRampRate = new NumericUpDown
            {
                Location = new Point(310, 28),
                Size = new Size(80, 25),
                Minimum = 0.1M,
                Maximum = 50,
                Value = 10,
                DecimalPlaces = 1,
                Increment = 0.1M
            };

            Label lblRateUnit = new Label
            {
                Text = "°C/min",
                Location = new Point(395, 30),
                Size = new Size(50, 25)
            };

            btnStart = new Button
            {
                Text = "시작",
                Location = new Point(500, 27),
                Size = new Size(100, 35),
                BackColor = Color.LightGreen,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold)
            };
            btnStart.Click += BtnStart_Click;

            btnStop = new Button
            {
                Text = "정지",
                Location = new Point(610, 27),
                Size = new Size(100, 35),
                BackColor = Color.LightYellow,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                Enabled = false
            };
            btnStop.Click += BtnStop_Click;

            btnEmergencyStop = new Button
            {
                Text = "비상정지",
                Location = new Point(720, 27),
                Size = new Size(100, 35),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold)
            };
            btnEmergencyStop.Click += BtnEmergencyStop_Click;

            prgProgress = new ProgressBar
            {
                Location = new Point(20, 70),
                Size = new Size(820, 25),
                Maximum = 100
            };

            lblProgress = new Label
            {
                Text = "진행률: 0%",
                Location = new Point(20, 100),
                Size = new Size(200, 20)
            };

            lblCurrentTemp = new Label
            {
                Text = "샘플: 25.0°C",
                Location = new Point(250, 100),
                Size = new Size(150, 20),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.DarkGreen
            };

            lblHeaterTemp = new Label
            {
                Text = "히터: 25.0°C",
                Location = new Point(420, 100),
                Size = new Size(150, 20),
                Font = new Font("맑은 고딕", 10F, FontStyle.Bold),
                ForeColor = Color.DarkRed
            };

            grpRampControl.Controls.AddRange(new Control[] {
                lblTarget, nudTargetTemp, lblTempUnit,
                lblRate, nudRampRate, lblRateUnit,
                btnStart, btnStop, btnEmergencyStop,
                prgProgress, lblProgress, lblCurrentTemp, lblHeaterTemp
            });

            // 상태 그룹
            grpStatus = new GroupBox
            {
                Text = "상태 로그",
                Location = new Point(10, 280),
                Size = new Size(860, 270)
            };

            txtStatus = new TextBox
            {
                Location = new Point(10, 25),
                Size = new Size(840, 235),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.Black,
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9F)
            };

            grpStatus.Controls.Add(txtStatus);

            tabControl.Controls.AddRange(new Control[] { grpQuickSetup, grpRampControl, grpStatus });
        }

        private void CreateProfileTab()
        {
            // 프로파일 목록
            var grpList = new GroupBox
            {
                Text = "프로파일 목록",
                Location = new Point(10, 10),
                Size = new Size(250, 540)
            };

            lstProfiles = new ListBox
            {
                Location = new Point(10, 25),
                Size = new Size(230, 450)
            };
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;

            Button btnLoadProfile = new Button
            {
                Text = "불러오기",
                Location = new Point(10, 485),
                Size = new Size(110, 30)
            };
            btnLoadProfile.Click += BtnLoadProfile_Click;

            Button btnSaveProfile = new Button
            {
                Text = "저장",
                Location = new Point(130, 485),
                Size = new Size(110, 30)
            };
            btnSaveProfile.Click += BtnSaveProfile_Click;

            grpList.Controls.AddRange(new Control[] {
                lstProfiles, btnLoadProfile, btnSaveProfile
            });

            // 프로파일 편집
            var grpEdit = new GroupBox
            {
                Text = "프로파일 편집",
                Location = new Point(270, 10),
                Size = new Size(600, 540)
            };

            propGrid = new PropertyGrid
            {
                Location = new Point(10, 25),
                Size = new Size(580, 505),
                HelpVisible = true
            };

            grpEdit.Controls.Add(propGrid);

            tabProfile.Controls.AddRange(new Control[] { grpList, grpEdit });

            RefreshProfileList();
        }

        #endregion

        #region 이벤트 핸들러

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            if (_tempController == null || _currentProfile == null)
            {
                MessageBox.Show("컨트롤러 또는 프로파일이 설정되지 않았습니다.",
                    "오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _isRunning = true;
                btnStart.Enabled = false;
                btnStop.Enabled = true;

                double targetTemp = (double)nudTargetTemp.Value;
                double rampRate = (double)nudRampRate.Value;

                LogStatus($"Ramp 시작: {targetTemp}°C @ {rampRate}°C/min");
                LogStatus($"프로파일: {_currentProfile.ProfileName}");

                bool success = await _rampController.ExecuteRampWithProfile(
                    _currentProfile, targetTemp, rampRate);

                if (success)
                {
                    LogStatus("Ramp 완료!");
                }
                else
                {
                    LogStatus("Ramp 실패");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"실행 오류: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _isRunning = false;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _rampController?.EmergencyStop();
            _isRunning = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            LogStatus("Ramp 정지됨");
        }

        private void BtnEmergencyStop_Click(object sender, EventArgs e)
        {
            _rampController?.EmergencyStop();
            _tempController?.SetRunStop(1, false);
            _isRunning = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            LogStatus("!!! 비상 정지 !!!");
        }

        private void CboQuickProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboQuickProfile.SelectedItem != null)
            {
                string profileName = cboQuickProfile.SelectedItem.ToString();
                LoadProfile(profileName);
            }
        }

        private void LstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem != null)
            {
                string filename = Path.Combine(_profilesPath, lstProfiles.SelectedItem.ToString());
                try
                {
                    var profile = ExperimentProfile.LoadFromFile(filename);
                    propGrid.SelectedObject = profile;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"프로파일 로드 오류: {ex.Message}");
                }
            }
        }

        private void BtnLoadProfile_Click(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedItem != null)
            {
                LoadProfile(Path.GetFileNameWithoutExtension(lstProfiles.SelectedItem.ToString()));
            }
        }

        private void BtnSaveProfile_Click(object sender, EventArgs e)
        {
            if (propGrid.SelectedObject is ExperimentProfile profile)
            {
                var dlg = new SaveFileDialog
                {
                    Filter = "JSON 파일|*.json",
                    InitialDirectory = _profilesPath
                };

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    profile.SaveToFile(dlg.FileName);
                    RefreshProfileList();
                    MessageBox.Show("프로파일이 저장되었습니다.");
                }
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_tempController == null || !_isRunning) return;

            UpdateChannelStatus();
        }

        #endregion

        #region 헬퍼 메서드

        private void InitializeProfilesPath()
        {
            _profilesPath = Path.Combine(Application.StartupPath, "Profiles");
            if (!Directory.Exists(_profilesPath))
            {
                Directory.CreateDirectory(_profilesPath);
                SaveDefaultProfiles();
            }
        }

        private void LoadDefaultProfile()
        {
            _currentProfile = DefaultProfiles.StandardBalanced;
            lblCurrentProfile.Text = $"현재: {_currentProfile.ProfileName}";
            propGrid.SelectedObject = _currentProfile;
        }

        private void LoadProfile(string profileName)
        {
            try
            {
                string filepath = Path.Combine(_profilesPath, profileName + ".json");
                if (File.Exists(filepath))
                {
                    _currentProfile = ExperimentProfile.LoadFromFile(filepath);
                }
                else
                {
                    // 기본 프로파일 확인
                    switch (profileName)
                    {
                        case "초보자_안전모드":
                            _currentProfile = DefaultProfiles.BeginnerSafe;
                            break;
                        case "표준_균형모드":
                            _currentProfile = DefaultProfiles.StandardBalanced;
                            break;
                        case "전문가_고속모드":
                            _currentProfile = DefaultProfiles.ExpertFast;
                            break;
                    }
                }

                if (_currentProfile != null)
                {
                    lblCurrentProfile.Text = $"현재: {_currentProfile.ProfileName}";
                    propGrid.SelectedObject = _currentProfile;
                    LogStatus($"프로파일 로드: {_currentProfile.ProfileName}");
                }
            }
            catch (Exception ex)
            {
                LogStatus($"프로파일 로드 실패: {ex.Message}");
            }
        }

        private void RefreshProfileList()
        {
            lstProfiles.Items.Clear();
            cboQuickProfile.Items.Clear();

            if (Directory.Exists(_profilesPath))
            {
                var files = Directory.GetFiles(_profilesPath, "*.json");
                foreach (var file in files)
                {
                    string filename = Path.GetFileName(file);
                    lstProfiles.Items.Add(filename);
                    cboQuickProfile.Items.Add(Path.GetFileNameWithoutExtension(filename));
                }
            }
        }

        private void SaveDefaultProfiles()
        {
            try
            {
                DefaultProfiles.BeginnerSafe.SaveToFile(
                    Path.Combine(_profilesPath, "초보자_안전모드.json"));
                DefaultProfiles.StandardBalanced.SaveToFile(
                    Path.Combine(_profilesPath, "표준_균형모드.json"));
                DefaultProfiles.ExpertFast.SaveToFile(
                    Path.Combine(_profilesPath, "전문가_고속모드.json"));
            }
            catch { }
        }

        private void UpdateStatus(IntelligentRampController.RampStatusEventArgs e)
        {
            lblCurrentTemp.Text = $"샘플: {e.CurrentTemp:F1}°C";
            lblHeaterTemp.Text = $"히터: {e.HeaterTemp:F1}°C";
            lblProgress.Text = $"진행률: {e.Progress:F1}%";
            prgProgress.Value = (int)e.Progress;
            LogStatus(e.Status);
        }

        private void UpdateChannelStatus()
        {
            try
            {
                _tempController.UpdateAllChannelStatus();

                double heaterTemp = _tempController.GetChannelTemperature(1);
                double avgSampleTemp = _tempController.GetAverageTemperature(2, 3, 4, 5);

                lblHeaterTemp.Text = $"히터: {heaterTemp:F1}°C";
                lblCurrentTemp.Text = $"샘플: {avgSampleTemp:F1}°C";
            }
            catch { }
        }

        private void LogStatus(string message)
        {
            if (txtStatus.InvokeRequired)
            {
                txtStatus.Invoke(new Action(() => LogStatus(message)));
                return;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            txtStatus.AppendText($"[{timestamp}] {message}\r\n");
            txtStatus.SelectionStart = txtStatus.Text.Length;
            txtStatus.ScrollToCaret();

            // 최대 500줄 유지
            var lines = txtStatus.Lines;
            if (lines.Length > 500)
            {
                txtStatus.Lines = lines.Skip(lines.Length - 500).ToArray();
            }
        }

        private void OnRampCompleted(IntelligentRampController.RampCompletedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnRampCompleted(e)));
                return;
            }

            LogStatus($"===== Ramp 완료 =====");
            LogStatus($"결과: {(e.Success ? "성공" : "실패")}");
            LogStatus($"메시지: {e.Message}");

            if (e.Statistics != null)
            {
                LogStatus($"총 시간: {e.Statistics.RampTime:F1}분");
                LogStatus($"평균 오차: {e.Statistics.AverageError:F2}°C");
                LogStatus($"최대 오차: {e.Statistics.MaxError:F2}°C");
                LogStatus($"오버슈트: {e.Statistics.MaxOvershoot:F2}°C");
            }
        }

        #endregion
    }
}