using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IMoodService
{
    Task<MoodEntry> LogAsync(int moodLevel, DateTime? timestampUtc = null, Guid? fastId = null, string? note = null);
    Task<IReadOnlyList<MoodEntry>> GetForFastAsync(Guid fastId);
    Task<IReadOnlyList<MoodEntry>> GetRecentAsync(int limit = 50);
    Task<int> GetTotalCountAsync();
}
