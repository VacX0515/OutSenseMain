using System;
using System.Drawing;
using System.Windows.Forms;
using VacX_OutSense.Utils;

namespace VacX_OutSense.Forms
{
    public class PathSettingsForm : Form
    {
        private TextBox txtDataPath;
        private TextBox txtLogsPath;
        private TextBox txtConfigPath;
        private TextBox txtProfilesPath;

        public PathSettingsForm()
        {
            InitializeUI();
            LoadCurrentPaths();
        }

        private void InitializeUI()
        {
            Text = "파일 경로 설정";
            Size = new Size(520, 400);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15, 15, 15, 60)
            };
            Controls.Add(panel);

            int y = 10;

            // 경로 입력 행 4개
            txtDataPath = AddPathRow(panel, "데이터 (Data):", ref y);
            txtLogsPath = AddPathRow(panel, "로그 (Logs):", ref y);
            txtConfigPath = AddPathRow(panel, "설정 (Config):", ref y);
            txtProfilesPath = AddPathRow(panel, "프로파일 (Profiles):", ref y);

            // 경고 라벨
            y += 10;
            var lblWarning = new Label
            {
                Text = "※ 경로 변경 시 기존 데이터는 이동되지 않습니다.\n    변경 사항은 앱 재시작 후 적용됩니다.",
                ForeColor = Color.OrangeRed,
                Font = new Font(Font.FontFamily, 8.5f),
                Location = new Point(15, y),
                AutoSize = true
            };
            panel.Controls.Add(lblWarning);

            // 하단 버튼
            var btnPanel = new Panel
            {
                Height = 50,
                Dock = DockStyle.Bottom
            };
            Controls.Add(btnPanel);

            var btnOk = new Button
            {
                Text = "확인",
                DialogResult = DialogResult.OK,
                Size = new Size(80, 30),
                Location = new Point(170, 10)
            };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
                Location = new Point(260, 10)
            };

            var btnReset = new Button
            {
                Text = "기본값",
                Size = new Size(80, 30),
                Location = new Point(350, 10)
            };
            btnReset.Click += BtnReset_Click;

            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel, btnReset });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private TextBox AddPathRow(Panel panel, string labelText, ref int y)
        {
            var lbl = new Label
            {
                Text = labelText,
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Location = new Point(15, y),
                AutoSize = true
            };
            panel.Controls.Add(lbl);
            y += 20;

            var txt = new TextBox
            {
                ReadOnly = true,
                BackColor = SystemColors.Window,
                Location = new Point(15, y),
                Size = new Size(380, 22)
            };
            panel.Controls.Add(txt);

            var btn = new Button
            {
                Text = "찾아보기",
                Size = new Size(75, 22),
                Location = new Point(400, y)
            };
            btn.Click += (s, e) =>
            {
                using (var dlg = new FolderBrowserDialog())
                {
                    dlg.Description = labelText;
                    dlg.SelectedPath = txt.Text;

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                    {
                        txt.Text = dlg.SelectedPath;
                    }
                }
            };
            panel.Controls.Add(btn);

            y += 35;
            return txt;
        }

        private void LoadCurrentPaths()
        {
            var ps = PathSettings.Instance;
            txtDataPath.Text = ps.DataPath;
            txtLogsPath.Text = ps.LogsPath;
            txtConfigPath.Text = ps.ConfigPath;
            txtProfilesPath.Text = ps.ProfilesPath;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            var ps = PathSettings.Instance;
            ps.DataPath = txtDataPath.Text;
            ps.LogsPath = txtLogsPath.Text;
            ps.ConfigPath = txtConfigPath.Text;
            ps.ProfilesPath = txtProfilesPath.Text;
            ps.Save();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 경로를 기본값으로 초기화하시겠습니까?",
                "기본값 복원",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                txtDataPath.Text = PathSettings.DefaultDataPath;
                txtLogsPath.Text = PathSettings.DefaultLogsPath;
                txtConfigPath.Text = PathSettings.DefaultConfigPath;
                txtProfilesPath.Text = PathSettings.DefaultProfilesPath;
            }
        }
    }
}
