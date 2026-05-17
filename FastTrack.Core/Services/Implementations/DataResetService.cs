using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class DataResetService : IDataResetService
{
    private readonly IDatabaseInitializer _db;

    public DataResetService(IDatabaseInitializer db) => _db = db;

    public async Task ResetAllAsync()
    {
        var conn = await _db.InitializeAsync();
        await conn.RunInTransactionAsync(c =>
        {
            c.DeleteAll<Fast>();
            c.DeleteAll<EarnedBadge>();
            c.DeleteAll<DailyQuest>();
            c.DeleteAll<WeightEntry>();
            c.DeleteAll<MoodEntry>();
            c.DeleteAll<WaterEntry>();
            c.DeleteAll<UserProfile>();
            c.DeleteAll<FastingProtocol>();

            foreach (var p in PresetProtocols.Build()) c.Insert(p);
        });
    }
}
