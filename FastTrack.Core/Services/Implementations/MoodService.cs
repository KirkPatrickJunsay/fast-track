using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class MoodService : IMoodService
{
    private readonly IMoodRepository _moods;

    public MoodService(IMoodRepository moods) => _moods = moods;

    public async Task<MoodEntry> LogAsync(int moodLevel, DateTime? timestampUtc = null, Guid? fastId = null, string? note = null)
    {
        if (moodLevel < 1 || moodLevel > 5)
            throw new ArgumentException("Mood level must be between 1 and 5.", nameof(moodLevel));

        var entry = new MoodEntry
        {
            TimestampUtc = (timestampUtc ?? DateTime.UtcNow).ToUniversalTime(),
            MoodLevel = moodLevel,
            FastId = fastId,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            Source = "Manual",
        };
        await _moods.AddAsync(entry);
        return entry;
    }

    public Task<IReadOnlyList<MoodEntry>> GetForFastAsync(Guid fastId) => _moods.GetForFastAsync(fastId);

    public Task<IReadOnlyList<MoodEntry>> GetRecentAsync(int limit = 50) => _moods.GetRecentAsync(limit);

    public Task<int> GetTotalCountAsync() => _moods.GetTotalCountAsync();
}
