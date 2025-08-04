using System.Threading;

namespace GameTrackerClient;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Добавляем глобальные обработчики исключений
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        Logger.Error("Unhandled UI thread exception.", e.Exception);
        MessageBox.Show($"An unhandled UI error occurred: {e.Exception.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Exit();
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Error("Unhandled non-UI thread exception.", e.ExceptionObject as Exception);
        MessageBox.Show($"A critical non-UI error occurred. See log for details.", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Exit();
    }
    
}