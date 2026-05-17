using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class DailyQuestRepositoryTests
{
    private static DateTime TodayKey() =>
        DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);

    [Fact]
    public async Task GetForDateAsync_returns_empty_when_none_exist()
    {
        using var db = new TestDb();
        var repo = new DailyQuestRepository(db.Initializer);
        (await repo.GetForDateAsync(TodayKey())).Should().BeEmpty();
    }

    [Fact]
    public async Task UpsertAsync_inserts_and_assigns_id_for_new_rows()
    {
        using var db = new TestDb();
        var repo = new DailyQuestRepository(db.Initializer);
        var quest = new DailyQuest
        {
            LocalDateUtc = TodayKey(),
            QuestKey = "complete_one",
            Target = 1, XpReward = 25,
        };
        await repo.UpsertAsync(quest);
        quest.Id.Should().BeGreaterThan(0);

        var rows = await repo.GetForDateAsync(TodayKey());
        rows.Should().HaveCount(1);
        rows[0].QuestKey.Should().Be("complete_one");
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_row_progress_and_claim()
    {
        using var db = new TestDb();
        var repo = new DailyQuestRepository(db.Initializer);
        var quest = new DailyQuest { LocalDateUtc = TodayKey(), QuestKey = "fast_16h", Target = 16, XpReward = 50 };
        await repo.UpsertAsync(quest);

        quest.Progress = 16;
        quest.IsClaimed = true;
        quest.ClaimedAtUtc = DateTime.UtcNow;
        await repo.UpsertAsync(quest);

        var reread = (await repo.GetForDateAsync(TodayKey())).Single();
        reread.IsClaimed.Should().BeTrue();
        reread.Progress.Should().Be(16);
        reread.ClaimedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task UpsertManyAsync_batches_inserts_and_preserves_order()
    {
        using var db = new TestDb();
        var repo = new DailyQuestRepository(db.Initializer);
        var today = TodayKey();
        var quests = new[]
        {
            new DailyQuest { LocalDateUtc = today, QuestKey = "complete_one", Target = 1, XpReward = 25 },
            new DailyQuest { LocalDateUtc = today, QuestKey = "fast_16h",    Target = 16, XpReward = 50 },
            new DailyQuest { LocalDateUtc = today, QuestKey = "beat_goal_1h", Target = 1, XpReward = 75 },
        };
        await repo.UpsertManyAsync(quests);

        var rows = await repo.GetForDateAsync(today);
        rows.Should().HaveCount(3);
        rows.Select(q => q.QuestKey).Should().Equal(new[] { "complete_one", "fast_16h", "beat_goal_1h" });
        rows.Should().OnlyContain(q => q.Id > 0);
    }

    [Fact]
    public async Task GetForDateAsync_isolates_rows_by_date()
    {
        using var db = new TestDb();
        var repo = new DailyQuestRepository(db.Initializer);
        var today = TodayKey();
        var yesterday = today.AddDays(-1);

        await repo.UpsertAsync(new DailyQuest { LocalDateUtc = today, QuestKey = "a", Target = 1, XpReward = 1 });
        await repo.UpsertAsync(new DailyQuest { LocalDateUtc = yesterday, QuestKey = "b", Target = 1, XpReward = 1 });

        (await repo.GetForDateAsync(today)).Should().HaveCount(1).And.OnlyContain(q => q.QuestKey == "a");
        (await repo.GetForDateAsync(yesterday)).Should().HaveCount(1).And.OnlyContain(q => q.QuestKey == "b");
    }
}
