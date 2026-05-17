using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class DatabaseInitializerTests
{
    [Fact]
    public async Task InitializeAsync_creates_the_database_file_on_disk()
    {
        using var db = new TestDb();
        File.Exists(db.DatabasePath).Should().BeFalse();
        await db.Initializer.InitializeAsync();
        File.Exists(db.DatabasePath).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_runs_all_migrations_and_bumps_user_version_to_latest()
    {
        using var db = new TestDb();
        var conn = await db.Initializer.InitializeAsync();
        var version = await conn.ExecuteScalarAsync<int>("PRAGMA user_version;");
        version.Should().Be(6); // V001..V006
    }

    [Fact]
    public async Task InitializeAsync_is_idempotent_on_repeated_calls()
    {
        using var db = new TestDb();
        var first = await db.Initializer.InitializeAsync();
        var second = await db.Initializer.InitializeAsync();
        second.Should().BeSameAs(first);
    }

    [Fact]
    public async Task V001_seeds_five_preset_protocols()
    {
        using var db = new TestDb();
        var conn = await db.Initializer.InitializeAsync();
        var protocols = await conn.Table<FastingProtocol>().ToListAsync();
        protocols.Should().HaveCount(5);
        protocols.Select(p => p.Name).Should().BeEquivalentTo(new[] { "16:8", "18:6", "20:4", "OMAD", "5:2" });
        protocols.Should().OnlyContain(p => p.IsPreset);
    }

    [Fact]
    public async Task Schema_includes_all_expected_tables()
    {
        using var db = new TestDb();
        var conn = await db.Initializer.InitializeAsync();
        var tables = await conn.QueryAsync<NameRow>("SELECT name FROM sqlite_master WHERE type='table';");
        var names = tables.Select(t => t.Name).ToList();
        names.Should().Contain(new[]
        {
            "UserProfile", "FastingProtocols", "Fasts", "EarnedBadges", "DailyQuests",
            "WeightEntries", "MoodEntries", "WaterEntries",
        });
    }

    [Fact]
    public async Task Migrations_already_applied_are_skipped_on_reopen()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fasttrack-{Guid.NewGuid():N}.db3");
        try
        {
            // First initializer instance creates everything.
            var first = new DatabaseInitializer(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseInitializer>.Instance,
                new FixedPathProviderForReopen(path));
            await first.InitializeAsync();

            // Second instance opens the same file — version should already be 5 and not roll back.
            var second = new DatabaseInitializer(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<DatabaseInitializer>.Instance,
                new FixedPathProviderForReopen(path));
            var conn = await second.InitializeAsync();
            var version = await conn.ExecuteScalarAsync<int>("PRAGMA user_version;");
            version.Should().Be(6);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class FixedPathProviderForReopen : FastTrack.Data.IDatabasePathProvider
    {
        private readonly string _path;
        public FixedPathProviderForReopen(string p) => _path = p;
        public string GetDatabasePath(string _) => _path;
    }

    private sealed class NameRow { public string Name { get; set; } = string.Empty; }
}
