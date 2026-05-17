using FastTrack.Models;

namespace FastTrack.Data;

public sealed class MoodRepository : IMoodRepository
{
    private readonly IDatabaseInitializer _db;
    public MoodRepository(IDatabaseInitializer db) => _db = db;

    public async Task AddAsync(MoodEntry entry)
    {
        var conn = await _db.InitializeAsync();
        if (entry.Id == 0) await conn.InsertAsync(entry);
        else await conn.UpdateAsync(entry);
    }

    public async Task<IReadOnlyList<MoodEntry>> GetForFastAsync(Guid fastId)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<MoodEntry>()
            .Where(m => m.FastId == fastId)
            .OrderBy(m => m.TimestampUtc)
            .ToListAsync();
        return rows;
    }

    public async Task<IReadOnlyList<MoodEntry>> GetRecentAsync(int limit = 100)
    {
        var conn = await _db.InitializeAsync();
        var rows = await conn.Table<MoodEntry>()
            .OrderByDescending(m => m.TimestampUtc)
            .Take(limit)
            .ToListAsync();
        return rows;
    }

    public async Task<int> GetTotalCountAsync()
    {
        var conn = await _db.InitializeAsync();
        return await conn.Table<MoodEntry>().CountAsync();
    }
}
