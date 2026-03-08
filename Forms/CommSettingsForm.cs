using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Windows.Forms;
using VacX_OutSense.Core.Communication;

namespace VacX_OutSense.Forms
{
    public class CommSettingsForm : Form
    {
        private CheckBox chkUseManual;
        private Panel devicePanel;

        // 장치 순서 (고정)
        private static readonly string[] DeviceKeys = {
            "IOModule", "DryPump", "TurboPump", "BathCirculator", "TempController"
        };

        private static readonly int[] BaudRates = { 9600, 19200, 38400, 57600, 115200 };
        private static readonly string[] Parities = { "None", "Even", "Odd" };

        // 장치별 컨트롤
        private readonly Dictionary<string, ComboBox> _cmbPorts = new();
        private readonly Dictionary<string, ComboBox> _cmbBaud = new();
        private readonly Dictionary<string, ComboBox> _cmbParity = new();
        private readonly Dictionary<string, NumericUpDown> _nudAddr = new();

        private CommPortSettings _settings;

        public CommSettingsForm(CommPortSettings settings)
        {
            _settings = settings ?? CommPortSettings.CreateDefault();
            InitializeUI();
            LoadFromSettings();
        }

        private void InitializeUI()
        {
            Text = "통신 포트 설정";
            Size = new Size(640, 480);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // ── 수동 모드 체크박스 ──
            chkUseManual = new CheckBox
            {
                Text = "수동 포트 지정 사용 (자동 감지 건너뜀)",
                Font = new Font(Font.FontFamily, 9f, FontStyle.Bold),
                Location = new Point(15, 12),
                AutoSize = true
            };
            chkUseManual.CheckedChanged += (s, e) => devicePanel.Enabled = chkUseManual.Checked;
            Controls.Add(chkUseManual);

            // ── 장치 설정 패널 ──
            devicePanel = new Panel
            {
                Location = new Point(0, 40),
                Size = new Size(625, 310),
                AutoScroll = true
            };
            Controls.Add(devicePanel);

            // 헤더
            int y = 5;
            AddHeaderRow(devicePanel, ref y);
            y += 5;

            // 장치 행
            string[] ports = GetAvailablePorts();
            foreach (var key in DeviceKeys)
            {
                AddDeviceRow(devicePanel, key, ports, ref y);
            }

            // ── 경고 라벨 ──
            var lblWarning = new Label
            {
                Text = "※ 변경 사항은 앱 재시작 후 적용됩니다.",
                ForeColor = Color.OrangeRed,
                Font = new Font(Font.FontFamily, 8.5f),
                Location = new Point(15, 360),
                AutoSize = true
            };
            Controls.Add(lblWarning);

            // ── 하단 버튼 ──
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
                Location = new Point(200, 10)
            };
            btnOk.Click += BtnOk_Click;

            var btnCancel = new Button
            {
                Text = "취소",
                DialogResult = DialogResult.Cancel,
                Size = new Size(80, 30),
                Location = new Point(290, 10)
            };

            var btnReset = new Button
            {
                Text = "기본값",
                Size = new Size(80, 30),
                Location = new Point(380, 10)
            };
            btnReset.Click += BtnReset_Click;

            var btnRefresh = new Button
            {
                Text = "포트 새로고침",
                Size = new Size(100, 30),
                Location = new Point(470, 10)
            };
            btnRefresh.Click += BtnRefresh_Click;

            btnPanel.Controls.AddRange(new Control[] { btnOk, btnCancel, btnReset, btnRefresh });
            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void AddHeaderRow(Panel panel, ref int y)
        {
            var headers = new[] {
                (text: "장치", x: 15, w: 90),
                (text: "COM 포트", x: 110, w: 90),
                (text: "BaudRate", x: 210, w: 80),
                (text: "Parity", x: 300, w: 70),
                (text: "주소", x: 380, w: 50)
            };

            foreach (var h in headers)
            {
                panel.Controls.Add(new Label
                {
                    Text = h.text,
                    Font = new Font(Font.FontFamily, 8.5f, FontStyle.Bold),
                    Location = new Point(h.x, y),
                    Size = new Size(h.w, 18)
                });
            }
            y += 22;
        }

        private void AddDeviceRow(Panel panel, string deviceKey, string[] ports, ref int y)
        {
            string displayName = CommPortSettings.GetDeviceDisplayName(deviceKey);

            var lbl = new Label
            {
                Text = displayName,
                Location = new Point(15, y + 3),
                Size = new Size(90, 20)
            };
            panel.Controls.Add(lbl);

            var cmbPort = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(110, y),
                Size = new Size(90, 22)
            };
            cmbPort.Items.Add("(미지정)");
            cmbPort.Items.AddRange(ports);
            cmbPort.SelectedIndex = 0;
            panel.Controls.Add(cmbPort);
            _cmbPorts[deviceKey] = cmbPort;

            var cmbBaud = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(210, y),
                Size = new Size(80, 22)
            };
            foreach (var br in BaudRates)
                cmbBaud.Items.Add(br.ToString());
            cmbBaud.SelectedIndex = 0;
            panel.Controls.Add(cmbBaud);
            _cmbBaud[deviceKey] = cmbBaud;

            var cmbParity = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(300, y),
                Size = new Size(70, 22)
            };
            cmbParity.Items.AddRange(Parities);
            cmbParity.SelectedIndex = 0;
            panel.Controls.Add(cmbParity);
            _cmbParity[deviceKey] = cmbParity;

            var nudAddr = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 247,
                Value = 1,
                Location = new Point(380, y),
                Size = new Size(55, 22)
            };
            panel.Controls.Add(nudAddr);
            _nudAddr[deviceKey] = nudAddr;

            y += 35;
        }

        private void LoadFromSettings()
        {
            chkUseManual.Checked = _settings.UseManualSettings;
            devicePanel.Enabled = _settings.UseManualSettings;

            foreach (var key in DeviceKeys)
            {
                if (!_settings.Devices.TryGetValue(key, out var cfg))
                    continue;

                // COM 포트
                if (!string.IsNullOrEmpty(cfg.PortName))
                {
                    int idx = _cmbPorts[key].Items.IndexOf(cfg.PortName);
                    if (idx >= 0)
                        _cmbPorts[key].SelectedIndex = idx;
                    else
                    {
                        // 저장된 포트가 현재 없으면 추가 표시
                        _cmbPorts[key].Items.Add(cfg.PortName + " (없음)");
                        _cmbPorts[key].SelectedIndex = _cmbPorts[key].Items.Count - 1;
                    }
                }

                // BaudRate
                int baudIdx = Array.IndexOf(BaudRates, cfg.BaudRate);
                if (baudIdx >= 0) _cmbBaud[key].SelectedIndex = baudIdx;

                // Parity
                int parIdx = Array.IndexOf(Parities, cfg.Parity);
                if (parIdx >= 0) _cmbParity[key].SelectedIndex = parIdx;

                // SlaveAddress
                _nudAddr[key].Value = Math.Clamp(cfg.SlaveAddress, 1, 247);
            }
        }

        private void SaveToSettings()
        {
            _settings.UseManualSettings = chkUseManual.Checked;

            foreach (var key in DeviceKeys)
            {
                if (!_settings.Devices.ContainsKey(key))
                    _settings.Devices[key] = new DevicePortConfig();

                var cfg = _settings.Devices[key];

                string selected = _cmbPorts[key].SelectedItem?.ToString() ?? "";
                cfg.PortName = selected == "(미지정)" || selected.EndsWith("(없음)")
                    ? ""
                    : selected;

                cfg.BaudRate = BaudRates[_cmbBaud[key].SelectedIndex];
                cfg.Parity = Parities[_cmbParity[key].SelectedIndex];
                cfg.DataBits = 8;
                cfg.StopBits = "One";
                cfg.SlaveAddress = (int)_nudAddr[key].Value;
            }
        }

        private string[] GetAvailablePorts()
        {
            try
            {
                return SerialPort.GetPortNames().OrderBy(p => p).ToArray();
            }
            catch
            {
                return Array.Empty<string>();
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            SaveToSettings();
            _settings.Save();
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "모든 통신 설정을 기본값으로 초기화하시겠습니까?",
                "기본값 복원",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _settings = CommPortSettings.CreateDefault();
                LoadFromSettings();
            }
        }

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            string[] ports = GetAvailablePorts();

            foreach (var key in DeviceKeys)
            {
                var cmb = _cmbPorts[key];
                string current = cmb.SelectedItem?.ToString() ?? "";

                cmb.Items.Clear();
                cmb.Items.Add("(미지정)");
                cmb.Items.AddRange(ports);

                int idx = cmb.Items.IndexOf(current);
                cmb.SelectedIndex = idx >= 0 ? idx : 0;
            }
        }
    }
}
