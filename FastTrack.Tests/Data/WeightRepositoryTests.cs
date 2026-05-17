using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class WeightRepositoryTests
{
    [Fact]
    public async Task GetLatestAsync_returns_null_when_empty()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        (await repo.GetLatestAsync()).Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_inserts_and_assigns_id()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        var entry = new WeightEntry { TimestampUtc = DateTime.UtcNow, WeightKg = 72.5 };
        await repo.AddAsync(entry);
        entry.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetLatestAsync_returns_newest_by_timestamp()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        await repo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow.AddDays(-2), WeightKg = 73 });
        await repo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow.AddHours(-1), WeightKg = 72 });
        var latest = await repo.GetLatestAsync();
        latest!.WeightKg.Should().Be(72);
    }

    [Fact]
    public async Task GetRecentAsync_orders_newest_first_with_limit()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        for (var i = 0; i < 5; i++)
            await repo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow.AddDays(-i), WeightKg = 70 + i });
        var recent = await repo.GetRecentAsync(3);
        recent.Should().HaveCount(3);
        recent.Select(w => w.TimestampUtc).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetRangeAsync_returns_entries_in_window_sorted_ascending()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        var now = DateTime.UtcNow;
        await repo.AddAsync(new WeightEntry { TimestampUtc = now.AddDays(-10), WeightKg = 70 });
        await repo.AddAsync(new WeightEntry { TimestampUtc = now.AddDays(-3), WeightKg = 71 });
        await repo.AddAsync(new WeightEntry { TimestampUtc = now, WeightKg = 72 });
        var range = await repo.GetRangeAsync(now.AddDays(-5), now);
        range.Should().HaveCount(2);
        range.Select(w => w.WeightKg).Should().Equal(new[] { 71.0, 72.0 });
    }

    [Fact]
    public async Task DeleteAsync_removes_entry()
    {
        using var db = new TestDb();
        var repo = new WeightRepository(db.Initializer);
        var entry = new WeightEntry { TimestampUtc = DateTime.UtcNow, WeightKg = 72 };
        await repo.AddAsync(entry);
        await repo.DeleteAsync(entry.Id);
        (await repo.GetLatestAsync()).Should().BeNull();
    }
}
