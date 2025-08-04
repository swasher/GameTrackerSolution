using System.Runtime.Versioning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameTrackerService;

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
        _logger.LogInformation("GameTracker Service starting up.");

        // Запускаем IPC сервер в отдельной задаче
        var ipcTask = _ipcServer.Start(stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _processTracker.UpdateProcessTimes();
                _logger.LogDebug("Updated process stats at {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // This is the expected exception when stopping the service.
            _logger.LogInformation("Service is stopping due to cancellation request.");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "An unhandled exception occurred in the worker loop.");
        }
        finally
        {
            _logger.LogInformation("Service is shutting down. Saving final process times...");
            _processTracker.SaveAllActiveProcessTimes();
            
            _logger.LogInformation("Waiting for IPC server to shut down.");
            await ipcTask; // Ожидаем корректного завершения IPC сервера
            _logger.LogInformation("GameTracker Service has stopped.");
        }
    }
}