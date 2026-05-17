using FastTrack.Models;

namespace FastTrack.Data;

public interface IFastRepository
{
    Task<Fast?> GetActiveAsync();
    Task<Fast?> GetByIdAsync(Guid id);
    Task<IReadOnlyList<Fast>> GetHistoryAsync(int limit = 50);
    Task UpsertAsync(Fast fast);
    Task DeleteAsync(Guid id);
}
