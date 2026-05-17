using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V002_AddLastUsedProtocol : IMigration
{
    public int Version => 2;
    public string Name => nameof(V002_AddLastUsedProtocol);

    public Task UpAsync(SQLiteAsyncConnection db)
    {
        // sqlite-net-pcl's CreateTableAsync detects new columns on existing tables and emits ALTER TABLE ADD COLUMN.
        return db.CreateTableAsync<UserProfile>();
    }
}
