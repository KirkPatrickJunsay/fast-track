using FastTrack.Models;

namespace FastTrack.Data;

public sealed class DailyQuestRepository : IDailyQuestRepository
{
    private readonly IDatabaseInitializer _db;

    public DailyQuestRepository(IDatabaseInitializer db) => _db = db;

    public async Task<IReadOnlyList<DailyQuest>> GetForDateAsync(DateTime localDateUtc)
    {
        var conn = await _db.InitializeAsync();
        return await conn.Table<DailyQuest>()
            .Where(q => q.LocalDateUtc == localDateUtc)
            .OrderBy(q => q.Id)
            .ToListAsync();
    }

    public async Task UpsertAsync(DailyQuest quest)
    {
        var conn = await _db.InitializeAsync();
        if (quest.Id == 0) await conn.InsertAsync(quest);
        else await conn.UpdateAsync(quest);
    }

    public async Task UpsertManyAsync(IEnumerable<DailyQuest> quests)
    {
        var conn = await _db.InitializeAsync();
        foreach (var q in quests)
        {
            if (q.Id == 0) await conn.InsertAsync(q);
            else await conn.UpdateAsync(q);
        }
    }
}
