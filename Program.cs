namespace VacX_OutSense
{
    internal static class Program
    {
        private static Mutex _mutex = null;
        private const string MutexName = "VacX_OutSense_Application_Mutex";

        [STAThread]
        static void Main()
        {
            // 1. 윈도우 생성 전에 반드시 먼저 호출
            ApplicationConfiguration.Initialize();

            // 2. 중복 실행 체크 (MessageBox는 Initialize 이후에 안전)
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                MessageBox.Show("VacX OutSense 애플리케이션이 이미 실행 중입니다.",
                               "중복 실행 감지",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Information);
                return;
            }

            // 3. 메인 폼 실행 (한 번만)
            try
            {
                Application.Run(new MainForm());
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }
    }
}