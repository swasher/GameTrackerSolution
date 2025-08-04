namespace GameTrackerService.Models;

public class TrackedProcess
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime LastSeen { get; set; }
    public TimeSpan TotalRunTime { get; set; } = TimeSpan.Zero;
}