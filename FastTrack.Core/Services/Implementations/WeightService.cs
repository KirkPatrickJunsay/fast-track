using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class WeightService : IWeightService
{
    private readonly IWeightRepository _weights;

    public WeightService(IWeightRepository weights) => _weights = weights;

    public async Task<WeightEntry> LogAsync(double weightKg, DateTime? timestampUtc = null, string? note = null)
    {
        if (weightKg <= 0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(weightKg));
        if (weightKg > 500)
            throw new ArgumentException("Weight value looks unreasonable. Please double-check.", nameof(weightKg));

        var entry = new WeightEntry
        {
            TimestampUtc = (timestampUtc ?? DateTime.UtcNow).ToUniversalTime(),
            WeightKg = weightKg,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Source = "Manual",
        };
        await _weights.AddAsync(entry);
        return entry;
    }

    public async Task<WeightTrend> GetTrendAsync(TimeSpan? lookback = null)
    {
        var samples = await _weights.GetRecentAsync(limit: 100);
        if (samples.Count == 0)
            return new WeightTrend(null, null, null, 0);

        var latest = samples[0];
        if (lookback is not null)
        {
            var cutoff = DateTime.UtcNow - lookback.Value;
            // Compare against the oldest sample inside the window (or the next-oldest if none).
            var inWindow = samples.Where(w => w.TimestampUtc >= cutoff).ToList();
            var reference = inWindow.Count >= 2
                ? inWindow[^1]
                : samples.Count >= 2 ? samples[1] : null;

            return new WeightTrend(latest.WeightKg, reference?.WeightKg,
                reference is null ? null : latest.WeightKg - reference.WeightKg, samples.Count);
        }

        var prev = samples.Count >= 2 ? samples[1] : null;
        return new WeightTrend(latest.WeightKg, prev?.WeightKg,
            prev is null ? null : latest.WeightKg - prev.WeightKg, samples.Count);
    }

    public Task<IReadOnlyList<WeightEntry>> GetRecentAsync(int limit = 30) =>
        _weights.GetRecentAsync(limit);
}
