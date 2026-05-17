using System.Globalization;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IFastRepository _fasts;
    private readonly IUserProfileRepository _profiles;
    private readonly IEarnedBadgeRepository _badges;
    private readonly IWeightRepository _weights;
    private readonly IWaterRepository _water;

    public AnalyticsService(
        IFastRepository fasts,
        IUserProfileRepository profiles,
        IEarnedBadgeRepository badges,
        IWeightRepository weights,
        IWaterRepository water)
    {
        _fasts = fasts;
        _profiles = profiles;
        _badges = badges;
        _weights = weights;
        _water = water;
    }

    public async Task<IReadOnlyList<HeatmapDay>> GetHeatmapAsync(int days = 365)
    {
        if (days < 1) days = 1;

        var history = await _fasts.GetHistoryAsync(int.MaxValue);
        var todayLocal = DateTime.Now.Date;
        var earliestLocal = todayLocal.AddDays(-(days - 1));

        // Bucket completed fasts by the local date they ended on.
        var byEndDay = history
            .Where(f => f.EndUtc.HasValue)
            .GroupBy(f => f.EndUtc!.Value.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<HeatmapDay>(days);
        for (var d = earliestLocal; d <= todayLocal; d = d.AddDays(1))
        {
            int intensity = 0;
            if (byEndDay.TryGetValue(d, out var fastsOnDay))
            {
                intensity = fastsOnDay.Max(f => IntensityFor(f));
            }
            result.Add(new HeatmapDay(d, intensity));
        }
        return result;
    }

    public async Task<PersonalBests> GetPersonalBestsAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();
        var history = (await _fasts.GetHistoryAsync(int.MaxValue))
            .Where(f => f.EndUtc.HasValue)
            .ToList();

        var longestFast = history.Count == 0
            ? TimeSpan.Zero
            : history.Max(f => f.EndUtc!.Value - f.StartUtc);

        var mostFastsInMonth = history
            .GroupBy(f => new { f.EndUtc!.Value.ToLocalTime().Year, f.EndUtc.Value.ToLocalTime().Month })
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .Max();

        var highestWeeklyHours = history
            .GroupBy(f => IsoWeekKey(f.EndUtc!.Value.ToLocalTime()))
            .Select(g => g.Sum(f => (f.EndUtc!.Value - f.StartUtc).TotalHours))
            .DefaultIfEmpty(0)
            .Max();

        return new PersonalBests(
            LongestFast: longestFast,
            LongestStreak: profile.LongestStreak,
            MostFastsInMonth: mostFastsInMonth,
            HighestWeeklyHours: highestWeeklyHours,
            TotalCompletedFasts: history.Count);
    }

    public async Task<WeeklySummary> GetWeeklySummaryAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();
        var history = (await _fasts.GetHistoryAsync(int.MaxValue))
            .Where(f => f.EndUtc.HasValue)
            .ToList();
        var badges = await _badges.GetAllAsync();

        var (thisStart, thisEnd) = LocalWeekBounds(DateTime.Now);
        var (lastStart, lastEnd) = LocalWeekBounds(DateTime.Now.AddDays(-7));

        double SumHours(DateTime start, DateTime end) =>
            history.Where(f =>
                f.EndUtc!.Value.ToLocalTime() >= start && f.EndUtc.Value.ToLocalTime() < end)
            .Sum(f => (f.EndUtc!.Value - f.StartUtc).TotalHours);

        var hoursThis = SumHours(thisStart, thisEnd);
        var hoursLast = SumHours(lastStart, lastEnd);
        var delta = hoursThis - hoursLast;

        var fastsThis = history.Count(f =>
            f.EndUtc!.Value.ToLocalTime() >= thisStart && f.EndUtc.Value.ToLocalTime() < thisEnd);

        var badgesThis = badges.Count(b => b.EarnedAtUtc.ToLocalTime() >= thisStart && b.EarnedAtUtc.ToLocalTime() < thisEnd);

        var highlight = BuildHighlight(hoursThis, hoursLast, fastsThis, history, thisStart, thisEnd);

        return new WeeklySummary(
            HoursThisWeek: hoursThis,
            HoursLastWeek: hoursLast,
            DeltaHours: delta,
            FastsThisWeek: fastsThis,
            CurrentStreak: profile.CurrentStreak,
            BadgesThisWeek: badgesThis,
            Highlight: highlight);
    }

    private static int IntensityFor(Fast f)
    {
        if (!f.EndUtc.HasValue) return 0;
        if (f.EndReason is not null && f.EndReason != FastEndReason.Completed) return 1;
        var hours = (f.EndUtc.Value - f.StartUtc).TotalHours;
        if (f.GoalHours <= 0) return 2;
        if (hours >= f.GoalHours * 1.2) return 3;
        if (hours >= f.GoalHours * 0.8) return 2;
        return 1;
    }

    private static (DateTime start, DateTime end) LocalWeekBounds(DateTime when)
    {
        // ISO week: Monday = start. Convert DayOfWeek (Sun=0..Sat=6) → offset from Monday.
        var date = when.Date;
        var dow = (int)date.DayOfWeek; // Sun=0..Sat=6
        var offsetFromMonday = (dow + 6) % 7; // Mon=0, Tue=1, ..., Sun=6
        var start = date.AddDays(-offsetFromMonday);
        return (start, start.AddDays(7));
    }

    private static (int Year, int Week) IsoWeekKey(DateTime localDt)
    {
        var year = ISOWeek.GetYear(localDt);
        var week = ISOWeek.GetWeekOfYear(localDt);
        return (year, week);
    }

    private static string BuildHighlight(double hoursThis, double hoursLast, int fastsThis,
        IReadOnlyList<Fast> history, DateTime weekStart, DateTime weekEnd)
    {
        if (fastsThis == 0)
        {
            return "No fasts this week yet. Whenever you're ready.";
        }
        if (hoursThis > hoursLast && hoursLast > 0)
        {
            return $"You fasted {hoursThis - hoursLast:0.#}h more than last week.";
        }
        // Was this week's longest fast also the longest in 4 weeks?
        var fourWeeksAgo = weekStart.AddDays(-28);
        var longestThis = history
            .Where(f => f.EndUtc!.Value.ToLocalTime() >= weekStart && f.EndUtc.Value.ToLocalTime() < weekEnd)
            .Select(f => (f.EndUtc!.Value - f.StartUtc).TotalHours)
            .DefaultIfEmpty(0).Max();
        var longestPriorMonth = history
            .Where(f => f.EndUtc!.Value.ToLocalTime() >= fourWeeksAgo && f.EndUtc.Value.ToLocalTime() < weekStart)
            .Select(f => (f.EndUtc!.Value - f.StartUtc).TotalHours)
            .DefaultIfEmpty(0).Max();
        if (longestThis > 0 && longestThis >= longestPriorMonth)
        {
            return $"Your longest fast this week was {longestThis:0.#}h — your best in a month.";
        }
        return $"{fastsThis} fasts logged this week.";
    }

    // ----- Trend series (Slice 2) -----

    public async Task<IReadOnlyList<ChartPoint>> GetFastDurationSeriesAsync(TimeRange range)
    {
        var history = (await _fasts.GetHistoryAsync(int.MaxValue))
            .Where(f => f.EndUtc.HasValue)
            .ToList();
        var (start, _) = RangeBoundsLocal(range, history.Select(f => f.EndUtc!.Value.ToLocalTime()).DefaultIfEmpty(DateTime.Now).Min());
        return history
            .Where(f => f.EndUtc!.Value.ToLocalTime() >= start)
            .OrderBy(f => f.EndUtc!.Value)
            .Select(f =>
            {
                var hours = (f.EndUtc!.Value - f.StartUtc).TotalHours;
                var local = f.EndUtc.Value.ToLocalTime();
                return new ChartPoint(local, hours, local.ToString("M/d"), $"{hours:0.#}h");
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ChartPoint>> GetWeeklyFastingHoursAsync(TimeRange range)
    {
        var history = (await _fasts.GetHistoryAsync(int.MaxValue))
            .Where(f => f.EndUtc.HasValue)
            .ToList();
        var (start, _) = RangeBoundsLocal(range, history.Select(f => f.EndUtc!.Value.ToLocalTime()).DefaultIfEmpty(DateTime.Now).Min());

        // Bucket by ISO week start (Monday).
        var entries = history.Where(f => f.EndUtc!.Value.ToLocalTime() >= start)
            .GroupBy(f =>
            {
                var local = f.EndUtc!.Value.ToLocalTime().Date;
                var offsetFromMonday = ((int)local.DayOfWeek + 6) % 7;
                return local.AddDays(-offsetFromMonday);
            })
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var hours = g.Sum(f => (f.EndUtc!.Value - f.StartUtc).TotalHours);
                return new ChartPoint(g.Key, hours, g.Key.ToString("M/d"), $"{hours:0.#}h");
            })
            .ToList();
        return entries;
    }

    public async Task<IReadOnlyList<ChartPoint>> GetWeightSeriesAsync(TimeRange range)
    {
        var rows = await _weights.GetRecentAsync(limit: int.MaxValue);
        var anchor = rows.Select(w => w.TimestampUtc.ToLocalTime()).DefaultIfEmpty(DateTime.Now).Min();
        var (start, _) = RangeBoundsLocal(range, anchor);
        return rows
            .Where(w => w.TimestampUtc.ToLocalTime() >= start)
            .OrderBy(w => w.TimestampUtc)
            .Select(w =>
            {
                var local = w.TimestampUtc.ToLocalTime();
                return new ChartPoint(local, w.WeightKg, local.ToString("M/d"), $"{w.WeightKg:0.0}");
            })
            .ToList();
    }

    public async Task<IReadOnlyList<ChartPoint>> GetWaterDailyAsync(TimeRange range)
    {
        // Earliest local date we need.
        var todayLocal = DateTime.Now.Date;
        var fromLocal = range == TimeRange.All ? todayLocal.AddDays(-364) : todayLocal.AddDays(-(range.Days() - 1));
        var fromUtc = DateTime.SpecifyKind(fromLocal, DateTimeKind.Local).ToUniversalTime();
        var toUtc = DateTime.SpecifyKind(todayLocal.AddDays(1), DateTimeKind.Local).ToUniversalTime();

        var rows = await _water.GetRangeAsync(fromUtc, toUtc);
        var byDay = rows
            .GroupBy(w => w.TimestampUtc.ToLocalTime().Date)
            .ToDictionary(g => g.Key, g => g.Sum(w => w.AmountMl));

        var points = new List<ChartPoint>();
        for (var d = fromLocal; d <= todayLocal; d = d.AddDays(1))
        {
            byDay.TryGetValue(d, out var ml);
            var liters = ml / 1000.0;
            points.Add(new ChartPoint(d, liters, d.ToString("M/d"), $"{liters:0.#}L"));
        }
        return points;
    }

    /// <summary>
    /// For <see cref="TimeRange.All"/> we anchor to <paramref name="firstDataLocal"/>; otherwise N days back from today.
    /// </summary>
    private static (DateTime StartLocal, DateTime EndLocal) RangeBoundsLocal(TimeRange range, DateTime firstDataLocal)
    {
        var todayLocal = DateTime.Now;
        var endLocal = todayLocal.AddDays(1).Date;
        if (range == TimeRange.All)
        {
            return (firstDataLocal.Date, endLocal);
        }
        var start = todayLocal.Date.AddDays(-(range.Days() - 1));
        return (start, endLocal);
    }
}
