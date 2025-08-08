/*
Отвечает на команды от GUI
*/


using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace GameTrackerService;

public class IpcServer
{
    private const string PipeName = "GameTrackerPipe";
    private readonly ProcessTracker _processTracker;
    private readonly ILogger<IpcServer> _logger;
    private readonly PipeSecurity _pipeSecurity;
    private const int PipeRestartDelayMs = 100;
    private const int MaxServerInstances = 4;

    [SupportedOSPlatform("windows")]
    public IpcServer(ProcessTracker processTracker, ILogger<IpcServer> logger)
    {
        _processTracker = processTracker;
        _logger = logger;
         
        // Создаем объект безопасности ОДИН РАЗ при инициализации сервера.
        // Это более эффективно, чем создавать его в цикле при каждом новом подключении.
        _pipeSecurity = new PipeSecurity();
        // Разрешаем всем аутентифицированным пользователям (включая того, кто запустил GUI)
        // подключаться к пайпу, созданному службой (которая работает от имени Local System).
        var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
        _pipeSecurity.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));
    }

    [SupportedOSPlatform("windows")]
    public async Task Start(CancellationToken token)
    {
        _logger.LogInformation("IPC Server starting...");

        // Запускаем несколько обработчиков подключений параллельно
        var tasks = Enumerable.Range(0, MaxServerInstances)
            .Select(i => HandleConnectionsAsync(token))
            .ToList();

        // Ждем завершения всех обработчиков
        await Task.WhenAll(tasks);
        _logger.LogInformation("IPC Server stopped.");
    }

    [SupportedOSPlatform("windows")]
    private async Task HandleConnectionsAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                // Используем правильную перегрузку конструктора, которая принимает PipeSecurity.
                // Для этого нужно указать оба размера буфера: inBufferSize и outBufferSize.
                using var server = new NamedPipeServerStream(
                    PipeName,
                    PipeDirection.InOut,
                    MaxServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly,
                    0,  
                    0
                    );

                _logger.LogInformation("Pipe instance waiting for connection...");
                await server.WaitForConnectionAsync(token);
                _logger.LogInformation("Client connected.");

                using var reader = new StreamReader(server);
                using var writer = new StreamWriter(server) { AutoFlush = true };

                // Читаем запрос
                var request = await reader.ReadLineAsync(token);
                if (request == null)
                {
                    _logger.LogInformation("Client disconnected without sending a request.");
                    continue;
                }

                _logger.LogInformation("Received request: {Request}", request);
            
                // Обрабатываем запрос
                var response = HandleRequest(request);
                _logger.LogDebug("Prepared response (first 100 chars): {Response}", response.Length > 100 ? response[..100] : response);

                // Отправляем ответ
                await writer.WriteLineAsync(response);
                _logger.LogInformation("Response sent successfully.");

                // Закрываем соединение
                // server.Disconnect();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in pipe handler: {ErrorMessage}", ex.Message);
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