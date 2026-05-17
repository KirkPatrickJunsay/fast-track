using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V006_AddHealthLogs : IMigration
{
    public int Version => 6;
    public string Name => nameof(V006_AddHealthLogs);

    public async Task UpAsync(SQLiteAsyncConnection db)
    {
        await db.CreateTableAsync<WeightEntry>();
        await db.CreateTableAsync<MoodEntry>();
        await db.CreateTableAsync<WaterEntry>();
    }
}
