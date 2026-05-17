using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class FastRepositoryTests
{
    private static Fast Sample(DateTime startUtc, DateTime? endUtc = null, FastEndReason? reason = null, Guid? id = null) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            ProtocolId = Guid.NewGuid(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            GoalHours = 16,
            EndReason = reason,
        };

    [Fact]
    public async Task UpsertAsync_assigns_id_when_empty()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        var fast = new Fast { Id = Guid.Empty, ProtocolId = Guid.NewGuid(), StartUtc = DateTime.UtcNow, GoalHours = 16 };
        await repo.UpsertAsync(fast);
        fast.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetActiveAsync_returns_only_in_progress_fast()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);

        var ended = Sample(DateTime.UtcNow.AddHours(-20), DateTime.UtcNow.AddHours(-4), FastEndReason.Completed);
        var active = Sample(DateTime.UtcNow.AddHours(-2));
        await repo.UpsertAsync(ended);
        await repo.UpsertAsync(active);

        var result = await repo.GetActiveAsync();
        result.Should().NotBeNull();
        result!.Id.Should().Be(active.Id);
    }

    [Fact]
    public async Task GetActiveAsync_returns_null_when_none_active()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        await repo.UpsertAsync(Sample(DateTime.UtcNow.AddHours(-10), DateTime.UtcNow, FastEndReason.Completed));
        (await repo.GetActiveAsync()).Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_returns_match()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        var fast = Sample(DateTime.UtcNow);
        await repo.UpsertAsync(fast);

        var found = await repo.GetByIdAsync(fast.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(fast.Id);
    }

    [Fact]
    public async Task GetHistoryAsync_returns_only_completed_fasts_newest_first()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);

        var older = Sample(DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(-3).AddHours(16), FastEndReason.Completed);
        var newer = Sample(DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(16), FastEndReason.Completed);
        var active = Sample(DateTime.UtcNow.AddHours(-2));
        await repo.UpsertAsync(older);
        await repo.UpsertAsync(newer);
        await repo.UpsertAsync(active);

        var history = await repo.GetHistoryAsync(10);
        history.Should().HaveCount(2);
        history[0].Id.Should().Be(newer.Id);
        history[1].Id.Should().Be(older.Id);
    }

    [Fact]
    public async Task GetHistoryAsync_respects_limit()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        for (var i = 0; i < 5; i++)
        {
            var s = DateTime.UtcNow.AddDays(-i - 1);
            await repo.UpsertAsync(Sample(s, s.AddHours(16), FastEndReason.Completed));
        }
        (await repo.GetHistoryAsync(3)).Should().HaveCount(3);
    }

    [Fact]
    public async Task DeleteAsync_removes_a_fast()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        var fast = Sample(DateTime.UtcNow);
        await repo.UpsertAsync(fast);
        await repo.DeleteAsync(fast.Id);
        (await repo.GetByIdAsync(fast.Id)).Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_row()
    {
        using var db = new TestDb();
        var repo = new FastRepository(db.Initializer);
        var fast = Sample(DateTime.UtcNow.AddHours(-3));
        await repo.UpsertAsync(fast);

        fast.EndUtc = DateTime.UtcNow;
        fast.EndReason = FastEndReason.Completed;
        await repo.UpsertAsync(fast);

        var reread = await repo.GetByIdAsync(fast.Id);
        reread!.EndUtc.Should().NotBeNull();
        reread.EndReason.Should().Be(FastEndReason.Completed);
    }
}
