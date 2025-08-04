using System.Diagnostics;
using System.Data.SQLite;
using System.Collections.Concurrent;
using System.IO;

namespace GameTrackerService;

public class ProcessStats
{
    public string Path { get; set; } = "";
    public string? DisplayName { get; set; }
    public int TotalSeconds { get; set; }
    public bool IsTracked { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? LastStartTime { get; set; }
}

public class ProcessTracker
{
    private readonly string _dbPath;
    private readonly string _connStr;
    private readonly ConcurrentDictionary<string, DateTime> _activeProcesses = new();
    // Начальные директории для отслеживания. Они будут заменяться настройками из GUI.
    private List<string> _watchedDirectories = new() { @"G:\GOG Games\", @"G:\Battle.net\" };
    
    // Добавляем логику для периодического сохранения
    private static readonly TimeSpan PeriodicSaveInterval = TimeSpan.FromSeconds(30);
    private DateTime _lastPeriodicSave = DateTime.UtcNow;

    public ProcessTracker(string dbPath)
    {
        _dbPath = dbPath;
        _connStr = $"Data Source={_dbPath};Version=3;";
        InitDb();
    }

    private void InitDb()
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        CREATE TABLE IF NOT EXISTS TrackedProcesses (
            Path TEXT NOT NULL PRIMARY KEY,
            TotalSeconds INTEGER NOT NULL DEFAULT 0,
            DisplayName TEXT,
            IsTracked INTEGER NOT NULL DEFAULT 1
        );
    ";
        cmd.ExecuteNonQuery();
    }
    
    private bool IsWatchedPath(string path)
    {
        return _watchedDirectories.Any(dir => path.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
    }
    
    public void SetProcessDisplayName(string path, string displayName)
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO TrackedProcesses (Path, DisplayName, TotalSeconds)
        VALUES (@Path, @DisplayName, 0)
        ON CONFLICT(Path) DO UPDATE SET
        DisplayName = @DisplayName;
    ";
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Parameters.AddWithValue("@DisplayName", displayName);
        cmd.ExecuteNonQuery();
    }
    
    public void SetProcessTracking(string path, bool isTracked)
    {
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
        INSERT INTO TrackedProcesses (Path, IsTracked, TotalSeconds)
        VALUES (@Path, @IsTracked, 0)
        ON CONFLICT(Path) DO UPDATE SET
        IsTracked = @IsTracked;
    ";
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Parameters.AddWithValue("@IsTracked", isTracked ? 1 : 0);
        cmd.ExecuteNonQuery();
    }
    
    public void UpdateProcessTimes()
    {
        var current = Process.GetProcesses();
        var now = DateTime.UtcNow;

        // Периодически сохраняем время для активных сессий
        if (now - _lastPeriodicSave > PeriodicSaveInterval)
        {
            SaveActiveSessionChunks(now);
        }
        
        var runningPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var process in current)
        {
            string? path = null;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch
            {
                continue; // доступ запрещён
            }

            if (string.IsNullOrWhiteSpace(path))
                continue;

            if (!IsWatchedPath(path))
                continue;

            Console.WriteLine($"[TRACE] Found process: {path}");
            
            runningPaths.Add(path);

            // Используем потокобезопасный метод TryAdd
            if (_activeProcesses.TryAdd(path, now))
                Console.WriteLine($"[TRACE] New tracked process: {path} at {now}");
        }

        var completed = _activeProcesses.Keys.Except(runningPaths).ToList();

        foreach (var path in completed)
        {
            var start = _activeProcesses[path];
            var seconds = (int)(now - start).TotalSeconds;
            Console.WriteLine($"[TRACE] Process exited: {path}, duration: {seconds} sec");
            
            _activeProcesses.TryRemove(path, out _);
            SaveTime(path, seconds);
        }
    }

    private void SaveTime(string path, int seconds)
    {
        Console.WriteLine($"[TRACE] Saving to DB: {path} -> +{seconds} sec");
        
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();

        // Используем атомарный UPSERT (INSERT ... ON CONFLICT ...)
        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TrackedProcesses (Path, TotalSeconds)
            VALUES (@Path, @Seconds)
            ON CONFLICT(Path) DO UPDATE SET
            TotalSeconds = TotalSeconds + @Seconds;
        ";
        cmd.Parameters.AddWithValue("@Path", path);
        cmd.Parameters.AddWithValue("@Seconds", seconds);
        cmd.ExecuteNonQuery();
    }
    
    public void SaveAllActiveProcessTimes()
    {
        var now = DateTime.UtcNow;
        Console.WriteLine($"[INFO] Service shutting down. Saving session time for active processes.");
 
        // Сначала сохраняем оставшиеся "куски" времени
        SaveActiveSessionChunks(now);
        _activeProcesses.Clear();
    }
 
    private void SaveActiveSessionChunks(DateTime now)
    {
        Console.WriteLine($"[INFO] Performing periodic save for active processes.");
        foreach (var (path, startTime) in _activeProcesses)
        {
            var seconds = (int)(now - startTime).TotalSeconds;
            if (seconds > 0) SaveTime(path, seconds);
            _activeProcesses[path] = now; // Сбрасываем таймер на текущее время
        }
        _lastPeriodicSave = now;
    }
    
    public void SetWatchedDirectories(List<string> directories)
    {
        // Нормализуем пути, чтобы они всегда заканчивались разделителем.
        // Это делает сравнение StartsWith более надежным.
        _watchedDirectories = directories
            .Select(d => d.EndsWith(Path.DirectorySeparatorChar.ToString()) ? d : d + Path.DirectorySeparatorChar)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        
        Console.WriteLine($"[INFO] Watched directories updated: {string.Join(", ", _watchedDirectories)}");
    }
    
    public List<string> GetWatchedDirectories()
    {
        // Возвращаем копию, чтобы предотвратить внешнее изменение
        return new List<string>(_watchedDirectories);
    }

    public Dictionary<string, ProcessStats> GetFullProcessStats()
    {
        var result = new Dictionary<string, ProcessStats>(StringComparer.OrdinalIgnoreCase);
    
        // Получаем информацию из БД
        using var conn = new SQLiteConnection(_connStr);
        conn.Open();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Path, TotalSeconds, DisplayName, IsTracked FROM TrackedProcesses";
    
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var path = reader.GetString(0);
                result[path] = new ProcessStats
                {
                    Path = path,
                    TotalSeconds = reader.GetInt32(1),
                    DisplayName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    IsTracked = reader.GetInt32(3) == 1,
                    IsRunning = false // По умолчанию не запущен
                };
            }
        }

        // Добавляем информацию о текущих запущенных процессах
        foreach (var (path, startTime) in _activeProcesses)
        {
            if (!result.TryGetValue(path, out var stats))
            {
                result[path] = new ProcessStats 
                { 
                    Path = path,
                    IsTracked = true, // Новые процессы по умолчанию отслеживаются
                    TotalSeconds = 0
                };
            }
        
            result[path].IsRunning = true;
            result[path].LastStartTime = startTime;
        }

        return result;
    }
}
