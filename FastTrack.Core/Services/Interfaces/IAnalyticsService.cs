using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IAnalyticsService
{
    /// <summary>Returns one HeatmapDay per local day for the last <paramref name="days"/> days (ending today).</summary>
    Task<IReadOnlyList<HeatmapDay>> GetHeatmapAsync(int days = 365);

    Task<PersonalBests> GetPersonalBestsAsync();

    Task<WeeklySummary> GetWeeklySummaryAsync();

    // Trend series (Slice 2). Each returns time-ordered points within the requested range.
    Task<IReadOnlyList<ChartPoint>> GetFastDurationSeriesAsync(TimeRange range);
    Task<IReadOnlyList<ChartPoint>> GetWeeklyFastingHoursAsync(TimeRange range);
    Task<IReadOnlyList<ChartPoint>> GetWeightSeriesAsync(TimeRange range);
    Task<IReadOnlyList<ChartPoint>> GetWaterDailyAsync(TimeRange range);
}
