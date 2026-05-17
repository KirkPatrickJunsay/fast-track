using FastTrack.Models;

namespace FastTrack.Data;

public sealed class FastRepository : IFastRepository
{
    private readonly IDatabaseInitializer _db;

    public FastRepository(IDatabaseInitializer db)
    {
        _db = db;
    }

    public async Task<Fast?> GetActiveAsync()
    {
        var conn = await _db.InitializeAsync();
        return await conn.Table<Fast>()
            .Where(f => f.EndUtc == null)
            .OrderByDescending(f => f.StartUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<Fast?> GetByIdAsync(Guid id)
    {
        var conn = await _db.InitializeAsync();
        return await conn.FindAsync<Fast>(id);
    }

    public async Task<IReadOnlyList<Fast>> GetHistoryAsync(int limit = 50)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<Fast>()
            .Where(f => f.EndUtc != null)
            .OrderByDescending(f => f.StartUtc)
            .Take(limit)
            .ToListAsync();
        return rows;
    }

    public async Task UpsertAsync(Fast fast)
    {
        var conn = await _db.InitializeAsync();
        if (fast.Id == Guid.Empty)
        {
            fast.Id = Guid.NewGuid();
        }
        await conn.InsertOrReplaceAsync(fast);
    }

    public async Task DeleteAsync(Guid id)
    {
        var conn = await _db.InitializeAsync();
        await conn.DeleteAsync<Fast>(id);
    }
}
