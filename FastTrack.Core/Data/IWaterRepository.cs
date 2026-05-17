using FastTrack.Models;

namespace FastTrack.Data;

public interface IWaterRepository
{
    Task AddAsync(WaterEntry entry);
    Task<IReadOnlyList<WaterEntry>> GetForDayAsync(DateTime dayStartUtc, DateTime dayEndUtc);
    Task<int> GetTotalForDayAsync(DateTime dayStartUtc, DateTime dayEndUtc);
    Task<IReadOnlyList<WaterEntry>> GetRangeAsync(DateTime fromUtc, DateTime toUtc);
    Task DeleteAsync(int id);
}
