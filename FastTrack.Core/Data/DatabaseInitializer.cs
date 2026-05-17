using FastTrack.Data.Migrations;
using Microsoft.Extensions.Logging;
using SQLite;

namespace FastTrack.Data;

public interface IDatabaseInitializer
{
    Task<SQLiteAsyncConnection> InitializeAsync();
}

/// <summary>Provides the OS-specific app-data directory path. Implemented in the MAUI project.</summary>
public interface IDatabasePathProvider
{
    string GetDatabasePath(string fileName);
}

public sealed class DatabaseInitializer : IDatabaseInitializer
{
    private const string DatabaseFileName = "fasttrack.db3";

    private static readonly SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite |
        SQLiteOpenFlags.Create |
        SQLiteOpenFlags.SharedCache;

    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly IDatabasePathProvider _paths;
    private SQLiteAsyncConnection? _connection;

    public DatabaseInitializer(ILogger<DatabaseInitializer> logger, IDatabasePathProvider paths)
    {
        _logger = logger;
        _paths = paths;
    }

    public async Task<SQLiteAsyncConnection> InitializeAsync()
    {
        if (_connection is not null)
        {
            return _connection;
        }

        var dbPath = _paths.GetDatabasePath(DatabaseFileName);
        _logger.LogInformation("Opening SQLite database at {Path}", dbPath);

        _connection = new SQLiteAsyncConnection(dbPath, Flags);

        await RunMigrationsAsync(_connection);
        return _connection;
    }

    private async Task RunMigrationsAsync(SQLiteAsyncConnection db)
    {
        var current = await GetUserVersionAsync(db);
        _logger.LogInformation("Current DB user_version = {Version}", current);

        var migrations = GetMigrations().OrderBy(m => m.Version).ToList();

        foreach (var migration in migrations)
        {
            if (migration.Version <= current)
            {
                continue;
            }

            _logger.LogInformation("Applying migration {Version}: {Name}", migration.Version, migration.Name);
            try
            {
                await migration.UpAsync(db);
                await SetUserVersionAsync(db, migration.Version);
                _logger.LogInformation("Migration {Version} applied.", migration.Version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Migration {Version} ({Name}) failed. Existing data left intact.", migration.Version, migration.Name);
                throw;
            }
        }
    }

    private static IEnumerable<IMigration> GetMigrations() =>
        new IMigration[]
        {
            new V001_InitialSchema(),
            new V002_AddLastUsedProtocol(),
            new V003_AddEducationalMode(),
            new V004_AddGamification(),
            new V005_AddQuests(),
            new V006_AddHealthLogs(),
        };

    private static async Task<int> GetUserVersionAsync(SQLiteAsyncConnection db)
    {
        var raw = await db.ExecuteScalarAsync<int>("PRAGMA user_version;");
        return raw;
    }

    private static Task SetUserVersionAsync(SQLiteAsyncConnection db, int version)
    {
        // PRAGMA does not support parameter binding for user_version.
        return db.ExecuteAsync($"PRAGMA user_version = {version};");
    }
}
