using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V005_AddQuests : IMigration
{
    public int Version => 5;
    public string Name => nameof(V005_AddQuests);

    public async Task UpAsync(SQLiteAsyncConnection db)
    {
        await db.CreateTableAsync<UserProfile>(); // adds ComebackBonusPending column
        await db.CreateTableAsync<DailyQuest>();
    }
}
