using FastTrack.Models;

namespace FastTrack.Data;

public interface IWeightRepository
{
    Task AddAsync(WeightEntry entry);
    Task<WeightEntry?> GetLatestAsync();
    Task<IReadOnlyList<WeightEntry>> GetRecentAsync(int limit = 50);
    Task<IReadOnlyList<WeightEntry>> GetRangeAsync(DateTime fromUtc, DateTime toUtc);
    Task DeleteAsync(int id);
}
