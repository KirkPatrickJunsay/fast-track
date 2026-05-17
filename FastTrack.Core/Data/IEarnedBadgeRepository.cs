using FastTrack.Models;

namespace FastTrack.Data;

public interface IEarnedBadgeRepository
{
    Task<IReadOnlyList<EarnedBadge>> GetAllAsync();
    Task<bool> HasAsync(string key);
    Task UpsertAsync(EarnedBadge badge);
}
