using System.Threading;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.IO;

namespace GameTrackerClient;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // 1. Настраиваем конфигурацию для чтения appsettings.json
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // 2. Настраиваем глобальный статический логгер Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        // 3. Оборачиваем запуск приложения в try/catch/finally для надежного логирования
        try
        {
            Log.Information("Application starting up");

            // Добавляем глобальные обработчики исключений
            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "A fatal error occurred during application startup.");
        }
        finally
        {
            Log.Information("Application shutting down.");
            // Крайне важно! Гарантирует, что все сообщения из буфера будут записаны в файл перед выходом.
            Log.CloseAndFlush();
        }
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        Log.Error(e.Exception, "Unhandled UI thread exception.");
        MessageBox.Show($"An unhandled UI error occurred: {e.Exception.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Error(e.ExceptionObject as Exception, "Unhandled non-UI thread exception.");
        MessageBox.Show($"A critical non-UI error occurred. See log for details.", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    
}