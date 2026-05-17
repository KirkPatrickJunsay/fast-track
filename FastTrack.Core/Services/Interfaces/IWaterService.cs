using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record WaterTodaySnapshot(int TotalMl, int GoalMl, double GoalFraction, bool GoalHit);

public interface IWaterService
{
    /// <summary>Default daily goal in millilitres. 2L per the spec; configurable later.</summary>
    int DailyGoalMl { get; }

    Task<WaterEntry> AddAsync(int amountMl, DateTime? timestampUtc = null);

    Task<WaterTodaySnapshot> GetTodayAsync();

    /// <summary>Counts unique local-days in <paramref name="lookbackDays"/> where total intake hit the goal.</summary>
    Task<int> CountGoalHitDaysAsync(int lookbackDays = 30);
}
