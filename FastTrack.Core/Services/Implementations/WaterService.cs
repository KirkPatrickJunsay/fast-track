using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class WaterService : IWaterService
{
    public int DailyGoalMl => 2000; // 2L default per US-03.3

    private readonly IWaterRepository _water;

    public WaterService(IWaterRepository water) => _water = water;

    public async Task<WaterEntry> AddAsync(int amountMl, DateTime? timestampUtc = null)
    {
        if (amountMl <= 0)
            throw new ArgumentException("Water amount must be positive.", nameof(amountMl));
        if (amountMl > 5_000)
            throw new ArgumentException("Single entry over 5L looks unreasonable.", nameof(amountMl));

        var entry = new WaterEntry
        {
            TimestampUtc = (timestampUtc ?? DateTime.UtcNow).ToUniversalTime(),
            AmountMl = amountMl,
            Source = "Manual",
        };
        await _water.AddAsync(entry);
        return entry;
    }

    public async Task<WaterTodaySnapshot> GetTodayAsync()
    {
        var (start, end) = LocalDayBoundsUtc(DateTime.Now.Date);
        var total = await _water.GetTotalForDayAsync(start, end);
        var fraction = Math.Min(1.0, (double)total / DailyGoalMl);
        return new WaterTodaySnapshot(total, DailyGoalMl, fraction, total >= DailyGoalMl);
    }

    public async Task<int> CountGoalHitDaysAsync(int lookbackDays = 30)
    {
        if (lookbackDays < 1) return 0;
        var todayLocal = DateTime.Now.Date;
        var earliestLocal = todayLocal.AddDays(-(lookbackDays - 1));
        var (rangeStart, rangeEnd) = (
            DateTime.SpecifyKind(earliestLocal, DateTimeKind.Local).ToUniversalTime(),
            DateTime.SpecifyKind(todayLocal.AddDays(1), DateTimeKind.Local).ToUniversalTime());

        var entries = await _water.GetRangeAsync(rangeStart, rangeEnd);
        if (entries.Count == 0) return 0;

        // Group by local date.
        var totalsByDay = entries
            .GroupBy(e => e.TimestampUtc.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Sum(e => e.AmountMl));

        return totalsByDay.Count(kvp => kvp.Value >= DailyGoalMl);
    }

    private static (DateTime startUtc, DateTime endUtc) LocalDayBoundsUtc(DateTime localDate)
    {
        var start = DateTime.SpecifyKind(localDate, DateTimeKind.Local);
        var end = start.AddDays(1);
        return (start.ToUniversalTime(), end.ToUniversalTime());
    }
}
