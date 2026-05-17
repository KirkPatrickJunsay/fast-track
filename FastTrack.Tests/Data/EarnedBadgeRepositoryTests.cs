using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class EarnedBadgeRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_returns_empty_for_fresh_db()
    {
        using var db = new TestDb();
        var repo = new EarnedBadgeRepository(db.Initializer);
        (await repo.GetAllAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task HasAsync_returns_false_before_award_and_true_after()
    {
        using var db = new TestDb();
        var repo = new EarnedBadgeRepository(db.Initializer);
        (await repo.HasAsync("first_fast")).Should().BeFalse();
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = DateTime.UtcNow });
        (await repo.HasAsync("first_fast")).Should().BeTrue();
    }

    [Fact]
    public async Task UpsertAsync_is_idempotent_on_same_key()
    {
        using var db = new TestDb();
        var repo = new EarnedBadgeRepository(db.Initializer);
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "day_one", EarnedAtUtc = DateTime.UtcNow.AddDays(-2) });
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "day_one", EarnedAtUtc = DateTime.UtcNow });
        (await repo.GetAllAsync()).Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAllAsync_returns_newest_first()
    {
        using var db = new TestDb();
        var repo = new EarnedBadgeRepository(db.Initializer);
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = DateTime.UtcNow.AddDays(-3) });
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "day_one",    EarnedAtUtc = DateTime.UtcNow.AddDays(-1) });
        await repo.UpsertAsync(new EarnedBadge { BadgeKey = "week_warrior", EarnedAtUtc = DateTime.UtcNow });

        var all = await repo.GetAllAsync();
        all.Select(b => b.BadgeKey).Should().Equal(new[] { "week_warrior", "day_one", "first_fast" });
    }
}
