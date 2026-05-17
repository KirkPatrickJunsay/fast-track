using SQLite;

namespace FastTrack.Data.Migrations;

public interface IMigration
{
    int Version { get; }
    string Name { get; }
    Task UpAsync(SQLiteAsyncConnection db);
}
