using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class QuestServiceTests
{
    private static (QuestService Sut, List<DailyQuest> Store, Mock<IXpService> Xp, Mock<IFastingProtocolRepository> Protocols) Build()
    {
        var store = new List<DailyQuest>();
        var repo = new Mock<IDailyQuestRepository>();
        repo.Setup(r => r.GetForDateAsync(It.IsAny<DateTime>())).ReturnsAsync((DateTime d) => store.Where(q => q.LocalDateUtc == d).ToList().AsReadOnly());
        repo.Setup(r => r.UpsertAsync(It.IsAny<DailyQuest>())).Callback<DailyQuest>(q =>
        {
            if (q.Id == 0) { q.Id = store.Count + 1; store.Add(q); }
        }).Returns(Task.CompletedTask);
        repo.Setup(r => r.UpsertManyAsync(It.IsAny<IEnumerable<DailyQuest>>())).Callback<IEnumerable<DailyQuest>>(qs =>
        {
            foreach (var q in qs)
            {
                if (q.Id == 0) { q.Id = store.Count + 1; store.Add(q); }
            }
        }).Returns(Task.CompletedTask);

        var xp = new Mock<IXpService>();
        xp.Setup(x => x.AwardAsync(It.IsAny<int>())).ReturnsAsync(new XpState(0, Level.All[0], Level.All[1], 0, 500));

        var protocols = new Mock<IFastingProtocolRepository>();
        return (new QuestService(repo.Object, xp.Object, protocols.Object), store, xp, protocols);
    }

    [Fact]
    public async Task GetTodayAsync_generates_three_quests_on_first_call()
    {
        var (sut, store, _, _) = Build();
        var today = await sut.GetTodayAsync();
        today.Should().HaveCount(3);
        store.Should().HaveCount(3);
        today.Select(t => t.Definition.Key).Distinct().Should().HaveCount(3);
    }

    [Fact]
    public async Task GetTodayAsync_is_deterministic_for_the_same_date()
    {
        var (sut, _, _, _) = Build();
        var first = await sut.GetTodayAsync();
        var second = await sut.GetTodayAsync();
        first.Select(t => t.Definition.Key).Should().Equal(second.Select(t => t.Definition.Key));
    }

    [Fact]
    public async Task OnFastCompletedAsync_credits_complete_one_quest_and_awards_xp()
    {
        var (sut, store, xp, _) = Build();
        // Force a known set of quests by pre-seeding.
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "complete_one", Target = 1, XpReward = 25 });

        var protocolId = Guid.NewGuid();
        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocolId,
            StartUtc = DateTime.UtcNow.AddHours(-16),
            EndUtc = DateTime.UtcNow,
            GoalHours = 16,
            EndReason = FastEndReason.Completed,
        };

        var updates = await sut.OnFastCompletedAsync(fast, new FastingProtocol { Id = protocolId }, new List<Fast> { fast });

        updates.Should().ContainSingle(u => u.Definition.Key == "complete_one" && u.NewlyCompleted);
        xp.Verify(x => x.AwardAsync(25), Times.Once);
        store[0].IsClaimed.Should().BeTrue();
    }

    [Fact]
    public async Task OnFastCompletedAsync_credits_fast_at_least_hours_when_threshold_met()
    {
        var (sut, store, xp, _) = Build();
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "fast_16h", Target = 16, XpReward = 50 });

        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddHours(-16),
            EndUtc = DateTime.UtcNow,
            GoalHours = 16,
            EndReason = FastEndReason.Completed,
        };
        var updates = await sut.OnFastCompletedAsync(fast, new FastingProtocol(), new List<Fast> { fast });
        updates.Should().ContainSingle(u => u.Definition.Key == "fast_16h" && u.NewlyCompleted);
        xp.Verify(x => x.AwardAsync(50), Times.Once);
    }

    [Fact]
    public async Task Beat_goal_by_1h_only_credits_when_actually_exceeded()
    {
        var (sut, store, xp, _) = Build();
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "beat_goal_1h", Target = 1, XpReward = 75 });

        // Beat by 30 min — should not credit.
        var almost = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16.5), EndUtc = DateTime.UtcNow, GoalHours = 16 };
        var updates = await sut.OnFastCompletedAsync(almost, new FastingProtocol(), new List<Fast> { almost });
        updates.Should().BeEmpty();

        // Beat by 90 min — credits.
        var won = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-17.5), EndUtc = DateTime.UtcNow, GoalHours = 16 };
        var updates2 = await sut.OnFastCompletedAsync(won, new FastingProtocol(), new List<Fast> { won });
        updates2.Should().ContainSingle(u => u.NewlyCompleted);
    }

    [Fact]
    public async Task Try_new_protocol_credits_only_on_first_use()
    {
        var (sut, store, _, _) = Build();
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "new_protocol", Target = 1, XpReward = 40 });

        var protocolId = Guid.NewGuid();
        var firstFast = new Fast { Id = Guid.NewGuid(), ProtocolId = protocolId, StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 };
        var updates = await sut.OnFastCompletedAsync(firstFast, new FastingProtocol(), new List<Fast> { firstFast });
        updates.Should().ContainSingle(u => u.NewlyCompleted);

        // Reset claimed for re-test; second use of same protocol should NOT credit.
        store[0].IsClaimed = false;
        store[0].Progress = 0;
        var secondFast = new Fast { Id = Guid.NewGuid(), ProtocolId = protocolId, StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 };
        var updates2 = await sut.OnFastCompletedAsync(secondFast, new FastingProtocol(), new List<Fast> { firstFast, secondFast });
        updates2.Should().BeEmpty();
    }

    [Fact]
    public async Task Dont_end_early_credits_only_on_completed_reason()
    {
        var (sut, store, _, _) = Build();
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "stay_committed", Target = 1, XpReward = 35 });

        var earlyFast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-8), EndUtc = DateTime.UtcNow, GoalHours = 16, EndReason = FastEndReason.Hungry };
        (await sut.OnFastCompletedAsync(earlyFast, new FastingProtocol(), new List<Fast> { earlyFast })).Should().BeEmpty();

        var onTime = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16, EndReason = FastEndReason.Completed };
        (await sut.OnFastCompletedAsync(onTime, new FastingProtocol(), new List<Fast> { onTime })).Should().ContainSingle();
    }

    [Fact]
    public async Task Already_claimed_quest_is_not_credited_again()
    {
        var (sut, store, xp, _) = Build();
        var today = DateTime.SpecifyKind(DateTime.Now.Date, DateTimeKind.Utc);
        store.Add(new DailyQuest { Id = 1, LocalDateUtc = today, QuestKey = "complete_one", Target = 1, XpReward = 25, IsClaimed = true, Progress = 1, ClaimedAtUtc = DateTime.UtcNow });

        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16, EndReason = FastEndReason.Completed };
        var updates = await sut.OnFastCompletedAsync(fast, new FastingProtocol(), new List<Fast> { fast });
        updates.Should().BeEmpty();
        xp.Verify(x => x.AwardAsync(It.IsAny<int>()), Times.Never);
    }
}
