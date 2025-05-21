public class LoadingForm : Form
{
    private Label lblStatus;
    private ProgressBar progressBar;

    public LoadingForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "시스템 초기화 중...";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Size = new Size(400, 150);
        this.ControlBox = false;

        // 항상 다른 창 위에 표시되도록 설정
        this.TopMost = true;

        // 작업 표시줄에 표시되지 않도록 설정 (선택 사항)
        this.ShowInTaskbar = false;

        this.lblStatus = new Label
        {
            Location = new Point(20, 20),
            Size = new Size(350, 20),
            Text = "시스템을 초기화하는 중입니다..."
        };

        this.progressBar = new ProgressBar
        {
            Location = new Point(20, 50),
            Size = new Size(350, 30),
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 30
        };

        this.Controls.Add(lblStatus);
        this.Controls.Add(progressBar);
    }

    public void UpdateStatus(string message)
    {
        if (this.InvokeRequired)
        {
            this.BeginInvoke(new Action<string>(UpdateStatus), message);
            return;
        }

        lblStatus.Text = message;
        // 로딩 폼을 최상위로 가져오기
        this.BringToFront();
        Application.DoEvents();
    }

    // 메인 폼이 로딩 폼 앞으로 오는 것을 방지하기 위한 이벤트 핸들러
    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        // 활성화될 때마다 최상위로 설정
        this.TopMost = true;
    }
}