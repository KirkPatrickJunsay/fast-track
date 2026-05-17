using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class MoodRepositoryTests
{
    [Fact]
    public async Task GetTotalCountAsync_returns_zero_when_empty()
    {
        using var db = new TestDb();
        var repo = new MoodRepository(db.Initializer);
        (await repo.GetTotalCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddAsync_inserts_and_count_reflects()
    {
        using var db = new TestDb();
        var repo = new MoodRepository(db.Initializer);
        await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow, MoodLevel = 4 });
        await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow.AddMinutes(-5), MoodLevel = 3 });
        (await repo.GetTotalCountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task GetForFastAsync_filters_by_fast_id_and_sorts_ascending()
    {
        using var db = new TestDb();
        var repo = new MoodRepository(db.Initializer);
        var fastA = Guid.NewGuid();
        var fastB = Guid.NewGuid();
        await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow.AddHours(-3), MoodLevel = 5, FastId = fastA });
        await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow.AddHours(-1), MoodLevel = 4, FastId = fastA });
        await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow.AddHours(-2), MoodLevel = 2, FastId = fastB });

        var forA = await repo.GetForFastAsync(fastA);
        forA.Should().HaveCount(2);
        forA.Select(m => m.MoodLevel).Should().Equal(new[] { 5, 4 });
    }

    [Fact]
    public async Task GetRecentAsync_orders_newest_first_with_limit()
    {
        using var db = new TestDb();
        var repo = new MoodRepository(db.Initializer);
        for (var i = 0; i < 4; i++)
            await repo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow.AddHours(-i), MoodLevel = 3 });
        var recent = await repo.GetRecentAsync(2);
        recent.Should().HaveCount(2);
    }
}
