using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V004_AddGamification : IMigration
{
    public int Version => 4;
    public string Name => nameof(V004_AddGamification);

    public async Task UpAsync(SQLiteAsyncConnection db)
    {
        // sqlite-net-pcl ALTER TABLE picks up new UserProfile columns.
        await db.CreateTableAsync<UserProfile>();
        await db.CreateTableAsync<EarnedBadge>();
    }
}
