// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;
// using System;
// using System.Diagnostics;
// using System.Threading;
// using System.Threading.Tasks;
//
// public class Worker : BackgroundService
// {
//     private readonly ILogger<Worker> _logger;
//
//     public Worker(ILogger<Worker> logger)
//     {
//         _logger = logger;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("Service started at: {time}", DateTimeOffset.Now);
//
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             var processes = Process.GetProcesses();
//             foreach (var process in processes)
//             {
//                 try
//                 {
//                     _logger.LogInformation("Running process: {0}", process.ProcessName);
//                 }
//                 catch { }
//             }
//
//             await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//         }
//
//         _logger.LogInformation("Service stopped at: {time}", DateTimeOffset.Now);
//     }
// }

using System.Runtime.Versioning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameTrackerService;

// public class Worker : BackgroundService
// {
//     private readonly ILogger<Worker> _logger;
//     private readonly ProcessTracker _tracker = new();
//
//     public Worker(ILogger<Worker> logger)
//     {
//         _logger = logger;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             _tracker.UpdateProcessTimes();
//             _logger.LogInformation("Updated process stats at {time}", DateTimeOffset.Now);
//             await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
//         }
//     }
//     
// }


// public class Worker : BackgroundService
// {
//     private readonly ILogger<Worker> _logger;
//     private readonly ProcessTracker _tracker;
//     // private readonly List<string> _watchedDirectories; // Добавляем поле для директорий
//
//     public Worker(ILogger<Worker> logger, ProcessTracker tracker) 
//     {
//         _logger = logger;
//         _tracker = tracker;
//     }
//
//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//         
//         // Устанавливаем наблюдаемые директории
//         // _tracker.SetWatchedDirectories(_watchedDirectories);
//
//         // Запуск IPC-сервера
//         var ipc = new IpcServer(_tracker, stoppingToken);
//         ipc.Start();
//
//         while (!stoppingToken.IsCancellationRequested)
//         {
//             _tracker.UpdateProcessTimes();
//             _logger.LogInformation("Updated process stats at {time}", DateTimeOffset.Now);
//             await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // Изменил задержку на 1 секунду как во втором примере
//         }
//     }
// }


public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly ProcessTracker _processTracker;
    private readonly IpcServer _ipcServer;

    public Worker(ILogger<Worker> logger, ProcessTracker processTracker) 
    {
        _logger = logger;
        _processTracker = processTracker;
        _ipcServer = new IpcServer(_processTracker);
    }

    [SupportedOSPlatform("windows")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        
        // Запускаем IPC сервер в отдельной задаче
        var ipcTask = _ipcServer.Start(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            _processTracker.UpdateProcessTimes();
            _logger.LogInformation("Updated process stats at {time}", DateTimeOffset.Now);
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // Изменил задержку на 1 секунду как во втором примере
        }
        
        await ipcTask; // Ожидаем корректного завершения IPC сервера
        Console.WriteLine("[INFO] Service stopped.");
        
    }
}