using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record WeightTrend(
    double? LatestKg,
    double? PreviousKg,
    double? ChangeKg,
    int Samples);

public interface IWeightService
{
    Task<WeightEntry> LogAsync(double weightKg, DateTime? timestampUtc = null, string? note = null);
    Task<WeightTrend> GetTrendAsync(TimeSpan? lookback = null);
    Task<IReadOnlyList<WeightEntry>> GetRecentAsync(int limit = 30);
}
