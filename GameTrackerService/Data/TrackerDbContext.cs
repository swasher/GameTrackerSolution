using Microsoft.EntityFrameworkCore;
using GameTrackerService.Models;

namespace GameTrackerService.Data;

public class TrackerDbContext : DbContext
{
    public DbSet<TrackedProcess> TrackedProcesses => Set<TrackedProcess>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=tracker.db");
    }
}