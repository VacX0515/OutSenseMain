using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Core.Safety;

namespace VacX_OutSense.Forms
{
    public class InterlockSettingsForm : Form
    {
        private InterlockConfiguration _config;
        public InterlockConfiguration Configuration => _config;

        // 밸브
        private CheckBox chkVV_TurboBlock;
        private CheckBox chkEV_TurboBlock;
        private CheckBox chkGV_TurboBlock;

        // 드라이펌프
        private CheckBox chkDP_RequireGV;
        private CheckBox chkDP_RequireVVEVClosed;
        private CheckBox chkDPStop_TurboBlock;

        // 터보펌프
        private CheckBox chkTP_RequireDP;
        private CheckBox chkTP_RequirePressure;
        private CheckBox chkTP_RequireChiller;
        private CheckBox chkTP_RequireGV;

        // 이온게이지
        private CheckBox chkIG_RequireLowPressure;

        // 히터
        private CheckBox chkHeater_WarnVacuum;

        // 칠러
        private CheckBox chkChillerStop_TurboBlock;

        // 오토런
        private CheckBox chkAR_BlockValve;
        private CheckBox chkAR_BlockPump;
        private CheckBox chkAR_BlockIG;
        private CheckBox chkAR_BlockHeater;

        public InterlockSettingsForm(InterlockConfiguration config)
        {
            _config = config ?? new InterlockConfiguration();
            InitializeUI();
            LoadFromConfig();
        }

        private void InitializeUI()
        {
            Text = "인터락 설정";
            Size = new Size(460, 620);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                AutoScroll = true,
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 10, 10, 50)
            };
            Controls.Add(panel);

            int y = 10;

            // ── 밸브 ──
            y = AddGroupLabel(panel, "밸브", y);
            chkVV_TurboBlock = AddCheckBox(panel, "벤트밸브: 터보펌프 작동 시 차단", ref y);
            chkEV_TurboBlock = AddCheckBox(panel, "배기밸브: 터보펌프 작동 시 차단", ref y);
            chkGV_TurboBlock = AddCheckBox(panel, "게이트밸브: 터보펌프 작동 시 닫기 차단", ref y);
            y += 8;

            // ── 드라이펌프 ──
            y = AddGroupLabel(panel, "드라이펌프", y);
            chkDP_RequireGV = AddCheckBox(panel, "시작: 게이트밸브 열림 필요", ref y);
            chkDP_RequireVVEVClosed = AddCheckBox(panel, "시작: 벤트/배기밸브 닫힘 필요", ref y);
            chkDPStop_TurboBlock = AddCheckBox(panel, "정지: 터보펌프 작동 시 차단", ref y);
            y += 8;

            // ── 터보펌프 ──
            y = AddGroupLabel(panel, "터보펌프", y);
            chkTP_RequireDP = AddCheckBox(panel, "시작: 드라이펌프 작동 필요", ref y);
            chkTP_RequirePressure = AddCheckBox(panel, "시작: 챔버 압력 ≤ 1 Torr", ref y);
            chkTP_RequireChiller = AddCheckBox(panel, "시작: 칠러 작동 필요", ref y);
            chkTP_RequireGV = AddCheckBox(panel, "시작: 게이트밸브 열림 필요", ref y);
            y += 8;

            // ── 이온게이지 ──
            y = AddGroupLabel(panel, "이온게이지", y);
            chkIG_RequireLowPressure = AddCheckBox(panel, "HV ON: 피라니 압력 ≤ 1E-3 Torr", ref y);
            y += 8;

            // ── 히터 ──
            y = AddGroupLabel(panel, "히터", y);
            chkHeater_WarnVacuum = AddCheckBox(panel, "시작: 진공 미달 시 경고", ref y);
            y += 8;

            // ── 칠러 ──
            y = AddGroupLabel(panel, "칠러", y);
            chkChillerStop_TurboBlock = AddCheckBox(panel, "정지: 터보펌프 작동 시 차단", ref y);
            y += 8;

            // ── 오토런 보호 ──
            y = AddGroupLabel(panel, "오토런 보호", y);
            chkAR_BlockValve = AddCheckBox(panel, "오토런 중 밸브 조작 차단", ref y);
            chkAR_BlockPump = AddCheckBox(panel, "오토런 중 펌프 조작 차단", ref y);
            chkAR_BlockIG = AddCheckBox(panel, "오토런 중 이온게이지 조작 차단", ref y);
            chkAR_BlockHeater = AddCheckBox(panel, "오토런 중 히터 조작 차단", ref y);

            // ── 하단 버튼 ──
            var btnPanel = new Panel
            {
                Height = 45,
                Dock = DockStyle.Bottom
            };
            Controls.Add(btnPanel);

            var btnOk = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Location = new Point(140, 8)
            };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
                Location = new Point(230, 8)
            };

            var btnReset = new Button
            {
                Text = "기본값",
                Size = new Size(80, 30),
                Location = new Point(320, 8)
            };
            btnReset.Click += BtnReset_Click;

            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel, btnReset });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private int AddGroupLabel(Panel panel, string text, int y)
        {
            var lbl = new Label
            {
                Text = $"■ {text}",
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Location = new Point(15, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl);
            return y + 22;
        }

        private CheckBox AddCheckBox(Panel panel, string text, ref int y)
        {
            var chk = new CheckBox
            {
                Text = text,
                Location = new Point(30, y),
                AutoSize = true
            };
            panel.Controls.Add(chk);
            y += 24;
            return chk;
        }

        private void LoadFromConfig()
        {
            chkVV_TurboBlock.Checked = _config.VentValve_BlockIfTurboRunning;
            chkEV_TurboBlock.Checked = _config.ExhaustValve_BlockIfTurboRunning;
            chkGV_TurboBlock.Checked = _config.GateValveClose_BlockIfTurboRunning;

            chkDP_RequireGV.Checked = _config.DryPump_RequireGateValveOpen;
            chkDP_RequireVVEVClosed.Checked = _config.DryPump_RequireVentExhaustClosed;
            chkDPStop_TurboBlock.Checked = _config.DryPumpStop_BlockIfTurboRunning;

            chkTP_RequireDP.Checked = _config.TurboPump_RequireDryPumpRunning;
            chkTP_RequirePressure.Checked = _config.TurboPump_RequirePressureBelow1Torr;
            chkTP_RequireChiller.Checked = _config.TurboPump_RequireChillerRunning;
            chkTP_RequireGV.Checked = _config.TurboPump_RequireGateValveOpen;

            chkIG_RequireLowPressure.Checked = _config.IonGaugeHV_RequireLowPressure;
            chkHeater_WarnVacuum.Checked = _config.HeaterStart_WarnIfNoVacuum;
            chkChillerStop_TurboBlock.Checked = _config.ChillerStop_BlockIfTurboRunning;

            chkAR_BlockValve.Checked = _config.AutoRun_BlockManualValveControl;
            chkAR_BlockPump.Checked = _config.AutoRun_BlockManualPumpControl;
            chkAR_BlockIG.Checked = _config.AutoRun_BlockManualIonGaugeControl;
            chkAR_BlockHeater.Checked = _config.AutoRun_BlockManualHeaterControl;
        }

        private void SaveToConfig()
        {
            _config.VentValve_BlockIfTurboRunning = chkVV_TurboBlock.Checked;
            _config.ExhaustValve_BlockIfTurboRunning = chkEV_TurboBlock.Checked;
            _config.GateValveClose_BlockIfTurboRunning = chkGV_TurboBlock.Checked;

            _config.DryPump_RequireGateValveOpen = chkDP_RequireGV.Checked;
            _config.DryPump_RequireVentExhaustClosed = chkDP_RequireVVEVClosed.Checked;
            _config.DryPumpStop_BlockIfTurboRunning = chkDPStop_TurboBlock.Checked;

            _config.TurboPump_RequireDryPumpRunning = chkTP_RequireDP.Checked;
            _config.TurboPump_RequirePressureBelow1Torr = chkTP_RequirePressure.Checked;
            _config.TurboPump_RequireChillerRunning = chkTP_RequireChiller.Checked;
            _config.TurboPump_RequireGateValveOpen = chkTP_RequireGV.Checked;

            _config.IonGaugeHV_RequireLowPressure = chkIG_RequireLowPressure.Checked;
            _config.HeaterStart_WarnIfNoVacuum = chkHeater_WarnVacuum.Checked;
            _config.ChillerStop_BlockIfTurboRunning = chkChillerStop_TurboBlock.Checked;

            _config.AutoRun_BlockManualValveControl = chkAR_BlockValve.Checked;
            _config.AutoRun_BlockManualPumpControl = chkAR_BlockPump.Checked;
            _config.AutoRun_BlockManualIonGaugeControl = chkAR_BlockIG.Checked;
            _config.AutoRun_BlockManualHeaterControl = chkAR_BlockHeater.Checked;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            SaveToConfig();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 인터락 설정을 기본값으로 초기화하시겠습니까?",
                "기본값 복원",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _config.ResetToDefaults();
                LoadFromConfig();
            }
        }
    }
}
