using GameTrackerService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



Host.CreateDefaultBuilder(args)
    .UseWindowsService() // важно!
    .ConfigureServices(services =>
    {
        services.AddSingleton(provider =>
        {
            // Указываем путь к файлу базы данных.
            // В будущем это можно будет вынести в конфигурацию (appsettings.json).
            return new ProcessTracker("tracker.db");
        });
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
