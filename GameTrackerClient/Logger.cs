using Serilog;

namespace GameTrackerClient;

/// <summary>
/// Простой статический логгер-обертка, использующий Serilog под капотом.
/// </summary>
public static class Logger
{
    public static void Info(string message)
    {
        // Делегируем вызов статическому логгеру Serilog
        Log.Information(message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        // Serilog умеет красиво форматировать исключения
        Log.Error(ex, message);
    }
}