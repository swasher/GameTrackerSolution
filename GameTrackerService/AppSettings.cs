namespace GameTrackerService;

/// <summary>
/// Представляет секцию "Settings" из файла appsettings.json
/// </summary>
public class AppSettings
{
    public const string SectionName = "Settings";

    public string AppName { get; set; } = "GameTracker";
    public string DbFileName { get; set; } = "tracker.db";
}