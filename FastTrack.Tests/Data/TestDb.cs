using FastTrack.Data;
using Microsoft.Extensions.Logging.Abstractions;

namespace FastTrack.Tests.Data;

/// <summary>
/// Creates a real on-disk SQLite database in the system temp folder for a single test,
/// runs all migrations, and cleans up on dispose. Each instance is isolated.
/// </summary>
public sealed class TestDb : IDisposable
{
    public string DatabasePath { get; }
    public IDatabasePathProvider PathProvider { get; }
    public DatabaseInitializer Initializer { get; }

    public TestDb()
    {
        DatabasePath = Path.Combine(Path.GetTempPath(), $"fasttrack-{Guid.NewGuid():N}.db3");
        PathProvider = new FixedPathProvider(DatabasePath);
        Initializer = new DatabaseInitializer(NullLogger<DatabaseInitializer>.Instance, PathProvider);
    }

    public void Dispose()
    {
        try { if (File.Exists(DatabasePath)) File.Delete(DatabasePath); }
        catch { /* test cleanup, ignore */ }
    }

    private sealed class FixedPathProvider : IDatabasePathProvider
    {
        private readonly string _path;
        public FixedPathProvider(string path) => _path = path;
        public string GetDatabasePath(string _) => _path;
    }
}
