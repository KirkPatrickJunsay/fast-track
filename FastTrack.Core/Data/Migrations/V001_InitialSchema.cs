using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V001_InitialSchema : IMigration
{
    public int Version => 1;
    public string Name => nameof(V001_InitialSchema);

    public async Task UpAsync(SQLiteAsyncConnection db)
    {
        await db.CreateTableAsync<UserProfile>();
        await db.CreateTableAsync<FastingProtocol>();
        await db.CreateTableAsync<Fast>();

        foreach (var p in PresetProtocols.Build())
        {
            await db.InsertOrReplaceAsync(p);
        }
    }
}
