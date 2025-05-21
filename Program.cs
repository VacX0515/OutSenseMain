namespace VacX_OutSense
{
    internal static class Program
    {
        // ���ø����̼� ���ؽ� - �ߺ� ���� ������ ���
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

            // �ߺ� ���� üũ
            if (!createdNew)
            {
                // �̹� ���� ���� �ν��Ͻ��� ����
                MessageBox.Show("VacX OutSense ���ø����̼��� �̹� ���� ���Դϴ�.",
                               "�ߺ� ���� ����",
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
                // ���ø����̼� ���� �� ���ؽ� ����
                _mutex.ReleaseMutex();
            }

        }
    }
}