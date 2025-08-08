using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace GameTrackerClient;

public class ProcessStats
{
    public string Path { get; set; } = "";
    public string? DisplayName { get; set; }
    public int TotalSeconds { get; set; }
    public bool IsTracked { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastStartTime { get; set; }

    public string DisplayNameOrPath => DisplayName ?? Path;
}

public class IpcClient
{
    private const string PipeName = "GameTrackerPipe";

    private async Task<string> SendRequestAsync(string request)
    {
        Logger.Info($"Sending request: {request.Split(' ')[0]}");
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            // Устанавливаем таймаут подключения, чтобы не ждать вечно, если служба не запущена
            Logger.Info($"Attempting to connect to pipe '{PipeName}' with 2s timeout...");
            await client.ConnectAsync(2000); 
            Logger.Info("Connection successful.");

            // Указываем leaveOpen: true, чтобы ридер и райтер не закрывали основной поток (пайп).
            // За закрытие теперь отвечает только главный блок "using var client".
            using var writer = new StreamWriter(client, Encoding.UTF8, leaveOpen: true) { AutoFlush = true };
            using var reader = new StreamReader(client, Encoding.UTF8, leaveOpen: true);

            await writer.WriteLineAsync(request);
            // Явно отправляем буфер на сервер, что бы избежать дедлока. Это критически важно.
            await writer.FlushAsync();
            Logger.Info("Request sent, waiting for response...");
            var response = await reader.ReadLineAsync();
            Logger.Info($"Raw response received: {response ?? "NULL"}");

            return response ?? "{\"error\":\"empty response from server\"}";
        }
        catch (TimeoutException ex)
        {
            var errorMsg = "{\"error\":\"connection timeout. Is the service running?\"}";
            Logger.Error("Connection timed out.", ex);
            return errorMsg;
        }
        catch (Exception ex)
        {
            var errorMsg = JsonSerializer.Serialize(new { error = $"IPC client error: {ex.Message}" });
            Logger.Error("An unexpected error occurred in SendRequestAsync.", ex);
            return errorMsg;
        }
    }

    
    public async Task<Dictionary<string, ProcessStats>> GetFullProcessStatsAsync()
    {
        var responseJson = await SendRequestAsync("GET_FULL_STATS");
        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, ProcessStats>>(responseJson);
            return result ?? new Dictionary<string, ProcessStats>();
        }
        catch (JsonException ex)
        {
            throw new Exception($"Failed to parse process stats: {responseJson}", ex);
        }
    }
    
 
    
    public async Task<List<string>> GetWatchedDirectoriesAsync()
    {
        var responseJson = await SendRequestAsync("GET_WATCHED_DIRS");
        try
        {
            var result = JsonSerializer.Deserialize<List<string>>(responseJson);
            return result ?? new List<string>();
        }
        catch (JsonException ex)
        {
            // Если десериализация не удалась, возможно, сервер вернул JSON с ошибкой.
            // Попробуем извлечь это сообщение.
            try
            {
                var error = JsonSerializer.Deserialize<Dictionary<string, string>>(responseJson);
                if (error != null && error.TryGetValue("error", out var errorMessage))
                {
                    // Нашли конкретную ошибку, пробрасываем ее.
                    throw new Exception(errorMessage);
                }
            }
            catch (JsonException) { /* Игнорируем, если это не объект ошибки, и бросаем общее исключение ниже. */ }

            // Если мы здесь, значит, пришел некорректный JSON, который не является ни ожидаемым результатом, ни объектом ошибки.
            throw new Exception($"Failed to parse directory list from server: {responseJson}", ex);
        }
    }

    public async Task<bool> SetWatchedDirectoriesAsync(List<string> directories)
    {
        var payload = JsonSerializer.Serialize(directories);
        var command = $"SET_WATCHED_DIRS {payload}";
        var response = await SendRequestAsync(command);
        // Просто проверяем, что сервер ответил "OK"
        return response == "\"OK\"";
    }
    
    
    public async Task<bool> SetProcessNameAsync(string path, string displayName)
    {
        var payload = JsonSerializer.Serialize(new { path, name = displayName });
        var response = await SendRequestAsync($"SET_PROCESS_NAME {payload}");
        return response == "\"OK\"";
    }

    public async Task<bool> SetProcessTrackingAsync(string path, bool isTracked)
    {
        var payload = JsonSerializer.Serialize(new { path, isTracked });
        var response = await SendRequestAsync($"SET_PROCESS_TRACKING {payload}");
        return response == "\"OK\"";
    }
    
}