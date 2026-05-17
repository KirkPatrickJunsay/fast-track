using FastTrack.Models;

namespace FastTrack.Data;

public sealed class WaterRepository : IWaterRepository
{
    private readonly IDatabaseInitializer _db;
    public WaterRepository(IDatabaseInitializer db) => _db = db;

    public async Task AddAsync(WaterEntry entry)
    {
        var conn = await _db.InitializeAsync();
        if (entry.Id == 0) await conn.InsertAsync(entry);
        else await conn.UpdateAsync(entry);
    }

    public async Task<IReadOnlyList<WaterEntry>> GetForDayAsync(DateTime dayStartUtc, DateTime dayEndUtc)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<WaterEntry>()
            .Where(w => w.TimestampUtc >= dayStartUtc && w.TimestampUtc < dayEndUtc)
            .OrderBy(w => w.TimestampUtc)
            .ToListAsync();
        return rows;
    }

    public async Task<int> GetTotalForDayAsync(DateTime dayStartUtc, DateTime dayEndUtc)
    {
        var entries = await GetForDayAsync(dayStartUtc, dayEndUtc);
        return entries.Sum(e => e.AmountMl);
    }

    public async Task<IReadOnlyList<WaterEntry>> GetRangeAsync(DateTime fromUtc, DateTime toUtc)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<WaterEntry>()
            .Where(w => w.TimestampUtc >= fromUtc && w.TimestampUtc <= toUtc)
            .OrderBy(w => w.TimestampUtc)
            .ToListAsync();
        return rows;
    }

    public async Task DeleteAsync(int id)
    {
        var conn = await _db.InitializeAsync();
        await conn.DeleteAsync<WaterEntry>(id);
    }
}
