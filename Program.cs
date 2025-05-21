namespace VacX_OutSense
{
    internal static class Program
    {
        // 애플리케이션 뮤텍스 - 중복 실행 방지에 사용
        private static Mutex _mutex = null;
        private const string MutexName = "VacX_OutSense_Application_Mutex";

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());

            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            // 중복 실행 체크
            if (!createdNew)
            {
                // 이미 실행 중인 인스턴스가 있음
                MessageBox.Show("VacX OutSense 애플리케이션이 이미 실행 중입니다.",
                               "중복 실행 감지",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
                return;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            finally
            {
                // 애플리케이션 종료 시 뮤텍스 해제
                _mutex.ReleaseMutex();
            }

        }
    }
}