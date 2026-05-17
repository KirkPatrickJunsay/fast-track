using FastTrack.Models;
using SQLite;

namespace FastTrack.Data.Migrations;

public sealed class V003_AddEducationalMode : IMigration
{
    public int Version => 3;
    public string Name => nameof(V003_AddEducationalMode);

    public Task UpAsync(SQLiteAsyncConnection db)
    {
        // sqlite-net-pcl detects the new column and emits ALTER TABLE ADD COLUMN.
        return db.CreateTableAsync<UserProfile>();
    }
}
