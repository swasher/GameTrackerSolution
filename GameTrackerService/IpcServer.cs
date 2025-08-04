/*
Отвечает на команды от GUI
*/


using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;

namespace GameTrackerService;

public class IpcServer
{
    private const string PipeName = "GameTrackerPipe";
    private readonly ProcessTracker _processTracker;
    private const int PipeRestartDelayMs = 100;
    private const int MaxServerInstances = 4;

    public IpcServer(ProcessTracker processTracker)
    {
        _processTracker = processTracker;
    }

    // public async Task Start(CancellationToken token)
    // {
    //     Logger.Info("IPC Server starting...");
    //     while (!token.IsCancellationRequested)
    //     {
    //         try
    //         {
    //             await using var server = new NamedPipeServerStream(PipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
    //             Logger.Info($"Waiting for a client connection on pipe '{PipeName}'...");
    //             await server.WaitForConnectionAsync(token);
    //             Logger.Info("Client connected.");
    //
    //             // Указываем leaveOpen: true, чтобы StreamReader и StreamWriter не закрывали основной поток (пайп).
    //             // За закрытие теперь отвечает только главный блок "await using var server".
    //             using var reader = new StreamReader(server, Encoding.UTF8, leaveOpen: true);
    //             using var writer = new StreamWriter(server, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
    //
    //             var request = await reader.ReadLineAsync(token);
    //             if (request == null)
    //             {
    //                 Logger.Info("Client disconnected without sending a request.");
    //                 continue;
    //             }
    //             Logger.Info($"Received request: {request.Split(' ')[0]}");
    //
    //             var response = HandleRequest(request);
    //             Logger.Info($"Sending response: {response}");
    //             await writer.WriteLineAsync(response);
    //             // Явно отправляем буфер клиенту перед завершением цикла.
    //             await writer.FlushAsync();
    //         }
    //         catch (OperationCanceledException)
    //         {
    //             // Это ожидаемое исключение при остановке службы
    //             Logger.Info("Operation canceled. IPC server is shutting down.");
    //             break;
    //         }
    //         catch (Exception ex)
    //         {
    //             Logger.Error($"IPC Server loop error", ex);
    //             // Небольшая задержка перед новой попыткой, чтобы не зацикливаться при постоянной ошибке
    //             await Task.Delay(1000, token);
    //         }
    //         // Добавьте задержку после закрытия пайпа
    //         await Task.Delay(PipeRestartDelayMs, token);
    //     }
    //     Logger.Info("IPC Server stopped.");
    // }
    [SupportedOSPlatform("windows")]
    public async Task Start(CancellationToken token)
    {
        Logger.Info("IPC Server starting...");

        // Запускаем несколько обработчиков подключений параллельно
        var tasks = Enumerable.Range(0, MaxServerInstances)
            .Select(i => HandleConnectionsAsync(token))
            .ToList();

        // Ждем завершения всех обработчиков
        await Task.WhenAll(tasks);
        Logger.Info("IPC Server stopped.");
    }

    [SupportedOSPlatform("windows")]
    private async Task HandleConnectionsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    MaxServerInstances,
                    PipeTransmissionMode.Message,  // Изменено на Message
                    PipeOptions.Asynchronous);

                Logger.Info("Pipe instance waiting for connection...");
                await server.WaitForConnectionAsync(token);
                Logger.Info("Client connected.");

                using var reader = new StreamReader(server);
                using var writer = new StreamWriter(server) { AutoFlush = true };

                // Читаем запрос
                var request = await reader.ReadLineAsync(token);
                if (request == null)
                {
                    Logger.Info("Client disconnected without sending a request.");
                    continue;
                }

                Logger.Info($"Received request: {request}");
            
                // Обрабатываем запрос
                var response = HandleRequest(request);
                Logger.Info($"Prepared response: {response}");

                // Отправляем ответ
                await writer.WriteLineAsync(response);
                Logger.Info("Response sent successfully.");

                // Закрываем соединение
                server.Disconnect();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                Logger.Error($"Error in pipe handler: {ex.Message}", ex);
                await Task.Delay(1000, token);
            }
        }
    }

    private string HandleRequest(string request)
    {
        var parts = request.Split(new[] { ' ' }, 2);
        var command = parts[0];
        var payload = parts.Length > 1 ? parts[1] : string.Empty;

        return command switch
        {
            "GET_FULL_STATS" => JsonSerializer.Serialize(_processTracker.GetFullProcessStats()),
            "GET_WATCHED_DIRS" => JsonSerializer.Serialize(_processTracker.GetWatchedDirectories()),
            "SET_WATCHED_DIRS" => HandleSetWatchedDirs(payload),
            "SET_PROCESS_NAME" => HandleSetProcessName(payload),
            "SET_PROCESS_TRACKING" => HandleSetProcessTracking(payload),
            _ => "{\"error\":\"Unknown command\"}"
        };
    }
    
    private string HandleSetProcessName(string payload)
    {
        try
        {
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(payload);
            if (data == null || !data.TryGetValue("path", out var path) || !data.TryGetValue("name", out var name))
                return "{\"error\":\"Invalid payload format\"}";
            
            _processTracker.SetProcessDisplayName(path, name);
            return JsonSerializer.Serialize("OK");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
    
    private string HandleSetProcessTracking(string payload)
    {
        try
        {
            var data = JsonSerializer.Deserialize<JsonElement>(payload);
            if (!data.TryGetProperty("path", out var pathElement) || 
                !data.TryGetProperty("isTracked", out var isTrackedElement))
            {
                return "{\"error\":\"Invalid payload format\"}";
            }

            var path = pathElement.GetString();
            var isTracked = isTrackedElement.GetBoolean();

            if (path == null)
            {
                return "{\"error\":\"Path cannot be null\"}";
            }

            _processTracker.SetProcessTracking(path, isTracked);
            return JsonSerializer.Serialize("OK");
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }

    private string HandleSetWatchedDirs(string jsonPayload)
    {
        try
        {
            var dirs = JsonSerializer.Deserialize<List<string>>(jsonPayload);
            _processTracker.SetWatchedDirectories(dirs ?? new List<string>());
            return JsonSerializer.Serialize("OK");
        }
        catch (JsonException ex)
        {
            return $"{{\"error\":\"Invalid JSON payload: {ex.Message}\"}}";
        }
    }
    
   
}