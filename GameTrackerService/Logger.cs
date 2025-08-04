using System.IO;

namespace GameTrackerService;

/// <summary>
/// Простой статический логгер для записи сообщений в файл.
/// </summary>
public static class Logger
{
    // Файл лога будет создаваться рядом с exe-файлом клиента.
    private static readonly string LogFilePath = Path.Combine(AppContext.BaseDirectory, "GameTrackerServer.log");
    private static readonly object Lock = new();

    public static void Info(string message)
    {
        Write($"[INFO] {message}");
    }

    public static void Error(string message, Exception? ex = null)
    {
        var fullMessage = ex != null ? $"{message}\n{ex}" : message;
        Write($"[ERROR] {fullMessage}");
    }

    private static void Write(string message)
    {
        try
        {
            lock (Lock)
            {
                File.AppendAllText(LogFilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}");
            }
        }
        catch { /* Игнорируем ошибки записи в лог, чтобы не уронить приложение */ }
    }
}