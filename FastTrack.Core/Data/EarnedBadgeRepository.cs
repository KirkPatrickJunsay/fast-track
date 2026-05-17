using FastTrack.Models;

namespace FastTrack.Data;

public sealed class EarnedBadgeRepository : IEarnedBadgeRepository
{
    private readonly IDatabaseInitializer _db;

    public EarnedBadgeRepository(IDatabaseInitializer db) => _db = db;

    public async Task<IReadOnlyList<EarnedBadge>> GetAllAsync()
    {
        var conn = await _db.InitializeAsync();
        return await conn.Table<EarnedBadge>().OrderByDescending(b => b.EarnedAtUtc).ToListAsync();
    }

    public async Task<bool> HasAsync(string key)
    {
        var conn = await _db.InitializeAsync();
        var hit = await conn.FindAsync<EarnedBadge>(key);
        return hit is not null;
    }

    public async Task UpsertAsync(EarnedBadge badge)
    {
        var conn = await _db.InitializeAsync();
        await conn.InsertOrReplaceAsync(badge);
    }
}
