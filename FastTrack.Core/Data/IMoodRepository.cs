using FastTrack.Models;

namespace FastTrack.Data;

public interface IMoodRepository
{
    Task AddAsync(MoodEntry entry);
    Task<IReadOnlyList<MoodEntry>> GetForFastAsync(Guid fastId);
    Task<IReadOnlyList<MoodEntry>> GetRecentAsync(int limit = 100);
    Task<int> GetTotalCountAsync();
}
