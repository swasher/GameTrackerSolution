using GameTrackerService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Reflection;
using Serilog;


Host.CreateDefaultBuilder(args)
    .UseContentRoot(AppContext.BaseDirectory)
    .UseWindowsService() // важно!
    .UseSerilog((context, services, loggerConfiguration) =>
    {
        // Определяем, находимся ли мы в режиме разработки
        var isDevelopment = context.HostingEnvironment.IsDevelopment();
        var config = context.Configuration;

        // Получаем настройки из конфигурации
        var appName = config["Settings:AppName"] ?? "GameTracker";
        var logSubDir = config["Logging:File:LogSubDir"] ?? "logs";
        var logFileName = config["Logging:File:LogFileName"] ?? "GameTrackerService-.log";
        var fileSizeLimit = config.GetValue<long?>("Logging:File:FileSizeLimitBytes") ?? 5 * 1024 * 1024;
        var retainedFileCount = config.GetValue<int?>("Logging:File:RetainedFileCountLimit") ?? 5;

        // Определяем базовую директорию в зависимости от среды выполнения
        string appDataBaseDirectory = isDevelopment
            ? AppContext.BaseDirectory // Папка с exe-файлом для DEV
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), appName); // C:\ProgramData\GameTracker для PROD

        // Настройка пути к файлу лога
        var logPath = Path.Combine(
            appDataBaseDirectory,
            logSubDir,
            logFileName);

        loggerConfiguration
            // Читаем базовые настройки уровня логирования из секции "Logging" в appsettings.json
            .ReadFrom.Configuration(config)
            .Enrich.FromLogContext()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day, // Ротация логов по дням
                fileSizeLimitBytes: fileSizeLimit,
                retainedFileCountLimit: retainedFileCount,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
            );

        // В режиме разработки добавляем консольный вывод
        if (isDevelopment)
        {
            loggerConfiguration.WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
            );
        }
    })
    .ConfigureServices((context, services) =>
    {
        // Регистрируем наш класс настроек в DI контейнере
        services.Configure<AppSettings>(context.Configuration.GetSection(AppSettings.SectionName));

        services.AddSingleton(provider =>
        {
            // Получаем экземпляр логгера из контейнера зависимостей.
            // Получаем доступ к окружению хоста, чтобы снова проверить IsDevelopment
            var hostEnvironment = provider.GetRequiredService<IHostEnvironment>();
            var logger = provider.GetRequiredService<ILogger<ProcessTracker>>();
            // Получаем экземпляр наших настроек через IOptions
            var settings = provider.GetRequiredService<IOptions<AppSettings>>().Value;

            try
            {
                var dbPath = Path.Combine(
                    // Повторяем ту же логику для определения пути к базе данных
                    hostEnvironment.IsDevelopment()
                        ? AppContext.BaseDirectory
                        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), settings.AppName),
                    settings.DbFileName);
                
                logger.LogInformation("Initializing DB at path: {DbPath}", dbPath);

                // Убедимся, что директория существует
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
                
                var tracker = new ProcessTracker(dbPath, logger);
                logger.LogInformation("ProcessTracker initialized successfully");
                return tracker;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize ProcessTracker (DB Location)");
                throw;
            }
        });
        // Регистрируем IPC сервер как Singleton. DI-контейнер сам подставит нужные зависимости (ProcessTracker и ILogger).
        services.AddSingleton<IpcServer>();
        services.AddHostedService<Worker>();
    })
    .Build()
    .Run();
