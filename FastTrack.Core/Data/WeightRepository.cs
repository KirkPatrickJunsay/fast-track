using FastTrack.Models;

namespace FastTrack.Data;

public sealed class WeightRepository : IWeightRepository
{
    private readonly IDatabaseInitializer _db;
    public WeightRepository(IDatabaseInitializer db) => _db = db;

    public async Task AddAsync(WeightEntry entry)
    {
        var conn = await _db.InitializeAsync();
        if (entry.Id == 0) await conn.InsertAsync(entry);
        else await conn.UpdateAsync(entry);
    }

    public async Task<WeightEntry?> GetLatestAsync()
    {
        var conn = await _db.InitializeAsync();
        return await conn.Table<WeightEntry>()
            .OrderByDescending(w => w.TimestampUtc)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<WeightEntry>> GetRecentAsync(int limit = 50)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<WeightEntry>()
            .OrderByDescending(w => w.TimestampUtc)
            .Take(limit)
            .ToListAsync();
        return rows;
    }

    public async Task<IReadOnlyList<WeightEntry>> GetRangeAsync(DateTime fromUtc, DateTime toUtc)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<WeightEntry>()
            .Where(w => w.TimestampUtc >= fromUtc && w.TimestampUtc <= toUtc)
            .OrderBy(w => w.TimestampUtc)
            .ToListAsync();
        return rows;
    }

    public async Task DeleteAsync(int id)
    {
        var conn = await _db.InitializeAsync();
        await conn.DeleteAsync<WeightEntry>(id);
    }
}
