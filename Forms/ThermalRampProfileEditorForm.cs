using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using VacX_OutSense.Core.Control;

namespace VacX_OutSense.Forms
{
    /// <summary>
    /// 열 램프 프로파일 편집 폼
    /// 프로파일 추가, 수정, 삭제 기능 제공
    /// </summary>
    public partial class ThermalRampProfileEditorForm : Form
    {
        #region 필드

        private readonly ThermalRampProfileManager _profileManager;
        private ThermalRampProfile _selectedProfile;
        private bool _isModified = false;

        #endregion

        #region 생성자

        public ThermalRampProfileEditorForm(ThermalRampProfileManager profileManager)
        {
            _profileManager = profileManager ?? throw new ArgumentNullException(nameof(profileManager));
            InitializeComponent();
            LoadProfileList();
        }

        #endregion

        #region 컨트롤 생성

        private ListBox lstProfiles;
        private PropertyGrid propGrid;
        private TextBox txtPropertyDescription;
        private Button btnNew;
        private Button btnDuplicate;
        private Button btnDelete;
        private Button btnImport;
        private Button btnExport;
        private Button btnSave;
        private Button btnClose;
        private Label lblInfo;
        private Panel panelButtons;
        private SplitContainer splitContainer;

        private void InitializeComponent()
        {
            this.Text = "프로파일 관리";
            this.Size = new Size(900, 650);
            this.MinimumSize = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.Sizable;

            // 메인 SplitContainer
            splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical
            };
            
            // MinSize와 SplitterDistance는 Load 이벤트에서 설정
            this.Load += (s, ev) =>
            {
                try
                {
                    splitContainer.Panel1MinSize = 150;
                    splitContainer.Panel2MinSize = 300;
                    if (splitContainer.Width > 460)
                    {
                        splitContainer.SplitterDistance = 250;
                    }
                }
                catch { }
            };

            // === 왼쪽 패널: 프로파일 목록 ===
            var panelLeft = new Panel { Dock = DockStyle.Fill };

            var lblTitle = new Label
            {
                Text = "프로파일 목록",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(this.Font, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            lstProfiles = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false
            };
            lstProfiles.SelectedIndexChanged += LstProfiles_SelectedIndexChanged;

            // 목록 버튼 패널
            var panelListButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 80,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(5)
            };

            btnNew = new Button { Text = "새 프로파일", Width = 100, Height = 30 };
            btnNew.Click += BtnNew_Click;

            btnDuplicate = new Button { Text = "복제", Width = 70, Height = 30 };
            btnDuplicate.Click += BtnDuplicate_Click;

            btnDelete = new Button { Text = "삭제", Width = 70, Height = 30 };
            btnDelete.Click += BtnDelete_Click;

            btnImport = new Button { Text = "가져오기...", Width = 90, Height = 30 };
            btnImport.Click += BtnImport_Click;

            btnExport = new Button { Text = "내보내기...", Width = 90, Height = 30 };
            btnExport.Click += BtnExport_Click;

            panelListButtons.Controls.AddRange(new Control[] { btnNew, btnDuplicate, btnDelete, btnImport, btnExport });

            panelLeft.Controls.Add(lstProfiles);
            panelLeft.Controls.Add(panelListButtons);
            panelLeft.Controls.Add(lblTitle);

            // === 오른쪽 패널: PropertyGrid ===
            var panelRight = new Panel { Dock = DockStyle.Fill };

            var lblPropTitle = new Label
            {
                Text = "프로파일 설정",
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font(this.Font, FontStyle.Bold),
                Padding = new Padding(5, 5, 0, 0)
            };

            propGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                PropertySort = PropertySort.Categorized,
                ToolbarVisible = false,
                HelpVisible = false  // 기본 도움말 숨김
            };
            propGrid.PropertyValueChanged += PropGrid_PropertyValueChanged;
            propGrid.SelectedGridItemChanged += PropGrid_SelectedGridItemChanged;

            // 스크롤 가능한 설명 패널
            var panelDescription = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblDescTitle = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = Color.Gray,
                ForeColor = Color.White,
                Text = "  속성 설명",
                Font = new Font(this.Font, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };

            txtPropertyDescription = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.None,
                Font = new Font("맑은 고딕", 9)
            };

            panelDescription.Controls.Add(txtPropertyDescription);
            panelDescription.Controls.Add(lblDescTitle);

            // 정보 라벨
            lblInfo = new Label
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.LightYellow,
                Padding = new Padding(10, 5, 10, 5),
                Text = "프로파일을 선택하여 설정을 확인하고 수정하세요. 시스템 기본 프로파일은 수정할 수 있지만 삭제할 수 없습니다."
            };

            panelRight.Controls.Add(propGrid);
            panelRight.Controls.Add(panelDescription);
            panelRight.Controls.Add(lblInfo);
            panelRight.Controls.Add(lblPropTitle);

            splitContainer.Panel1.Controls.Add(panelLeft);
            splitContainer.Panel2.Controls.Add(panelRight);

            // === 하단 버튼 패널 ===
            panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            btnSave = new Button
            {
                Text = "변경사항 저장",
                Width = 120,
                Height = 35,
                Location = new Point(this.ClientSize.Width - 260, 8),
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnSave.Click += BtnSave_Click;

            btnClose = new Button
            {
                Text = "닫기",
                Width = 80,
                Height = 35,
                Location = new Point(this.ClientSize.Width - 130, 8),
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                DialogResult = DialogResult.Cancel
            };
            btnClose.Click += BtnClose_Click;

            panelButtons.Controls.AddRange(new Control[] { btnSave, btnClose });

            // 폼에 추가
            this.Controls.Add(splitContainer);
            this.Controls.Add(panelButtons);

            this.AcceptButton = btnSave;
            this.CancelButton = btnClose;
        }

        #endregion

        #region 프로파일 목록

        private void LoadProfileList()
        {
            lstProfiles.Items.Clear();
            
            foreach (var profile in _profileManager.Profiles)
            {
                string displayName = profile.IsSystemDefault 
                    ? $"★ {profile.Name}" 
                    : profile.Name;
                lstProfiles.Items.Add(displayName);
            }

            if (lstProfiles.Items.Count > 0)
            {
                lstProfiles.SelectedIndex = 0;
            }

            UpdateButtonStates();
        }

        private void LstProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstProfiles.SelectedIndex < 0)
            {
                _selectedProfile = null;
                propGrid.SelectedObject = null;
                return;
            }

            // 수정 사항 확인
            if (_isModified)
            {
                var result = MessageBox.Show(
                    "변경사항이 저장되지 않았습니다. 저장하시겠습니까?",
                    "확인",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveCurrentProfile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            _selectedProfile = _profileManager.Profiles[lstProfiles.SelectedIndex];
            propGrid.SelectedObject = _selectedProfile;
            _isModified = false;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool hasSelection = _selectedProfile != null;
            bool isSystemDefault = hasSelection && _selectedProfile.IsSystemDefault;

            btnDuplicate.Enabled = hasSelection;
            btnDelete.Enabled = hasSelection && !isSystemDefault;
            btnExport.Enabled = hasSelection;
            btnSave.Enabled = _isModified;

            if (isSystemDefault)
            {
                lblInfo.Text = "시스템 기본 프로파일입니다. 수정은 가능하지만 삭제할 수 없습니다.";
                lblInfo.BackColor = Color.LightBlue;
            }
            else if (hasSelection)
            {
                lblInfo.Text = $"'{_selectedProfile.Name}' 프로파일을 편집 중입니다.";
                lblInfo.BackColor = Color.LightGreen;
            }
            else
            {
                lblInfo.Text = "프로파일을 선택하여 설정을 확인하고 수정하세요.";
                lblInfo.BackColor = Color.LightYellow;
            }
        }

        #endregion

        #region 버튼 이벤트

        private void BtnNew_Click(object sender, EventArgs e)
        {
            var newProfile = new ThermalRampProfile
            {
                Name = "새 프로파일",
                Description = "새로 생성된 프로파일입니다."
            };

            // 고유 이름 생성
            int counter = 1;
            while (_profileManager.Profiles.Any(p => p.Name == newProfile.Name))
            {
                newProfile.Name = $"새 프로파일 ({counter++})";
            }

            try
            {
                _profileManager.AddProfile(newProfile);
                LoadProfileList();
                
                // 새 프로파일 선택
                lstProfiles.SelectedIndex = lstProfiles.Items.Count - 1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로파일 생성 실패: {ex.Message}", "오류", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDuplicate_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;

            try
            {
                var duplicate = _profileManager.DuplicateProfile(_selectedProfile.Name);
                LoadProfileList();

                // 복제된 프로파일 선택
                int index = _profileManager.Profiles.IndexOf(duplicate);
                if (index >= 0)
                {
                    lstProfiles.SelectedIndex = index;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로파일 복제 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;

            if (_selectedProfile.IsSystemDefault)
            {
                MessageBox.Show("시스템 기본 프로파일은 삭제할 수 없습니다.", "알림",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show(
                $"'{_selectedProfile.Name}' 프로파일을 삭제하시겠습니까?\n이 작업은 되돌릴 수 없습니다.",
                "삭제 확인",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes) return;

            try
            {
                _profileManager.DeleteProfile(_selectedProfile.Name);
                _selectedProfile = null;
                _isModified = false;
                LoadProfileList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"프로파일 삭제 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnImport_Click(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "JSON 파일|*.json|모든 파일|*.*";
                dlg.Title = "프로파일 가져오기";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        var imported = _profileManager.ImportProfile(dlg.FileName);
                        LoadProfileList();

                        // 가져온 프로파일 선택
                        int index = _profileManager.Profiles.IndexOf(imported);
                        if (index >= 0)
                        {
                            lstProfiles.SelectedIndex = index;
                        }

                        MessageBox.Show($"'{imported.Name}' 프로파일을 가져왔습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"프로파일 가져오기 실패: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, EventArgs e)
        {
            if (_selectedProfile == null) return;

            using (var dlg = new SaveFileDialog())
            {
                dlg.Filter = "JSON 파일|*.json";
                dlg.Title = "프로파일 내보내기";
                dlg.FileName = $"{_selectedProfile.Name}.json";

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _profileManager.ExportProfile(_selectedProfile.Name, dlg.FileName);
                        MessageBox.Show("프로파일을 내보냈습니다.", "완료",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"프로파일 내보내기 실패: {ex.Message}", "오류",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            SaveCurrentProfile();
        }

        private void BtnClose_Click(object sender, EventArgs e)
        {
            if (_isModified)
            {
                var result = MessageBox.Show(
                    "변경사항이 저장되지 않았습니다. 저장하시겠습니까?",
                    "확인",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveCurrentProfile();
                }
                else if (result == DialogResult.Cancel)
                {
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        #endregion

        #region PropertyGrid 이벤트

        private void PropGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            _isModified = true;
            UpdateButtonStates();

            // 이름 변경 시 목록 업데이트
            if (e.ChangedItem.Label == "이름")
            {
                int selectedIndex = lstProfiles.SelectedIndex;
                LoadProfileList();
                lstProfiles.SelectedIndex = selectedIndex;
                _isModified = true; // LoadProfileList에서 리셋되므로 다시 설정
                UpdateButtonStates();
            }
        }

        private void PropGrid_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            // 선택된 속성의 설명을 표시 (범위 정보 포함)
            if (e.NewSelection != null && txtPropertyDescription != null)
            {
                string description = e.NewSelection.PropertyDescriptor?.Description ?? "";
                txtPropertyDescription.Text = description;
            }
        }

        #endregion

        #region 저장

        private void SaveCurrentProfile()
        {
            if (_selectedProfile == null || !_isModified) return;

            try
            {
                // PropertyGrid에서 수정된 내용은 이미 _selectedProfile에 반영됨
                // 매니저에 저장
                _profileManager.Save();
                _isModified = false;
                UpdateButtonStates();

                MessageBox.Show("프로파일이 저장되었습니다.", "완료",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"저장 실패: {ex.Message}", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 폼 이벤트

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isModified && e.CloseReason == CloseReason.UserClosing)
            {
                var result = MessageBox.Show(
                    "변경사항이 저장되지 않았습니다. 저장하시겠습니까?",
                    "확인",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SaveCurrentProfile();
                }
                else if (result == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        #endregion
    }
}
