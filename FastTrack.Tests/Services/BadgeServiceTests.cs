using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class BadgeServiceTests
{
    private static (BadgeService Sut,
                    List<EarnedBadge> EarnedStore,
                    UserProfile Profile,
                    List<Fast> History,
                    List<FastingProtocol> Protocols) Build()
    {
        var earnedStore = new List<EarnedBadge>();
        var earned = new Mock<IEarnedBadgeRepository>();
        earned.Setup(r => r.GetAllAsync()).ReturnsAsync(() => earnedStore.AsReadOnly());
        earned.Setup(r => r.HasAsync(It.IsAny<string>())).ReturnsAsync((string k) => earnedStore.Any(e => e.BadgeKey == k));
        earned.Setup(r => r.UpsertAsync(It.IsAny<EarnedBadge>())).Callback<EarnedBadge>(b =>
        {
            earnedStore.RemoveAll(e => e.BadgeKey == b.BadgeKey);
            earnedStore.Add(b);
        }).Returns(Task.CompletedTask);

        var history = new List<Fast>();
        var fastsRepo = new Mock<IFastRepository>();
        fastsRepo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(() => history.AsReadOnly());

        var profile = new UserProfile { Id = Guid.NewGuid() };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);

        var protocols = new List<FastingProtocol>();
        var protocolsRepo = new Mock<IFastingProtocolRepository>();
        protocolsRepo.Setup(p => p.GetAllAsync()).ReturnsAsync(protocols);

        var water = new Mock<IWaterService>();
        water.Setup(w => w.CountGoalHitDaysAsync(It.IsAny<int>())).ReturnsAsync(0);
        var moods = new Mock<IMoodService>();
        moods.Setup(m => m.GetTotalCountAsync()).ReturnsAsync(0);

        var sut = new BadgeService(earned.Object, fastsRepo.Object, profiles.Object, protocolsRepo.Object, water.Object, moods.Object);
        return (sut, earnedStore, profile, history, protocols);
    }

    private static Fast Completed(DateTime startUtc, DateTime endUtc, int goalHours = 16, Guid? protocolId = null, FastEndReason reason = FastEndReason.Completed) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocolId ?? Guid.NewGuid(),
            StartUtc = startUtc,
            EndUtc = endUtc,
            GoalHours = goalHours,
            EndReason = reason,
        };

    [Fact]
    public void Exposes_twenty_definitions_with_five_hidden()
    {
        var (sut, _, _, _, _) = Build();
        sut.Definitions.Should().HaveCount(20);
        sut.Definitions.Count(d => d.IsHidden).Should().Be(5);
    }

    [Fact]
    public async Task GetAllAsync_marks_earned_with_dates()
    {
        var (sut, store, _, _, _) = Build();
        var earnedAt = DateTime.UtcNow.AddDays(-2);
        store.Add(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = earnedAt });

        var all = await sut.GetAllAsync();
        all.Should().HaveCount(20);
        var firstFast = all.Single(e => e.Definition.Key == "first_fast");
        firstFast.IsEarned.Should().BeTrue();
        firstFast.EarnedAtUtc.Should().Be(earnedAt);
    }

    [Fact]
    public async Task First_fast_unlocks_on_initial_completion()
    {
        var (sut, store, _, history, _) = Build();
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        history.Add(fast);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "first_fast");
        store.Should().Contain(e => e.BadgeKey == "first_fast");
    }

    [Fact]
    public async Task Day_one_unlocks_at_24_hours()
    {
        var (sut, _, _, history, _) = Build();
        var start = DateTime.UtcNow.AddHours(-25);
        var fast = Completed(start, start.AddHours(25));
        history.Add(fast);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "day_one");
    }

    [Fact]
    public async Task Marathon_unlocks_at_48_hours()
    {
        var (sut, _, _, history, _) = Build();
        var start = DateTime.UtcNow.AddHours(-49);
        var fast = Completed(start, start.AddHours(49));
        history.Add(fast);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "marathon");
    }

    [Fact]
    public async Task Week_and_month_badges_track_current_streak()
    {
        var (sut, _, profile, history, _) = Build();
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        history.Add(fast);

        profile.CurrentStreak = 7;
        (await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol())).Should().Contain(b => b.Key == "week_warrior");

        profile.CurrentStreak = 30;
        (await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol())).Should().Contain(b => b.Key == "month_master");
    }

    [Fact]
    public async Task Earned_badge_does_not_re_grant()
    {
        var (sut, store, _, history, _) = Build();
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        history.Add(fast);
        await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        var second = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        second.Should().NotContain(b => b.Key == "first_fast");
        store.Count(e => e.BadgeKey == "first_fast").Should().Be(1);
    }

    [Fact]
    public async Task Protocol_explorer_requires_five_distinct_protocols()
    {
        var (sut, _, _, history, _) = Build();
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToArray();
        for (var i = 0; i < 5; i++)
        {
            history.Add(Completed(DateTime.UtcNow.AddHours(-16 - i), DateTime.UtcNow.AddHours(-i), protocolId: ids[i]));
        }
        var newly = await sut.EvaluateOnFastCompletedAsync(history[^1], new FastingProtocol());
        newly.Should().Contain(b => b.Key == "protocol_explorer");
    }

    [Fact]
    public async Task Cycle_breaker_unlocks_after_14_day_gap()
    {
        var (sut, _, _, history, _) = Build();
        var oldFast = Completed(DateTime.UtcNow.AddDays(-30).AddHours(-16), DateTime.UtcNow.AddDays(-30));
        history.Add(oldFast);
        var current = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        history.Add(current);
        var newly = await sut.EvaluateOnFastCompletedAsync(current, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "cycle_breaker");
    }

    [Fact]
    public async Task Disciplined_soul_requires_20_completed_on_time_fasts()
    {
        var (sut, _, _, history, _) = Build();
        for (var i = 0; i < 20; i++)
        {
            history.Add(Completed(DateTime.UtcNow.AddHours(-16 - i), DateTime.UtcNow.AddHours(-i), reason: FastEndReason.Completed));
        }
        var newly = await sut.EvaluateOnFastCompletedAsync(history[^1], new FastingProtocol());
        newly.Should().Contain(b => b.Key == "disciplined_soul");
    }

    [Fact]
    public async Task Disciplined_soul_locked_if_any_recent_ended_early()
    {
        var (sut, _, _, history, _) = Build();
        for (var i = 0; i < 19; i++)
        {
            history.Add(Completed(DateTime.UtcNow.AddHours(-16 - i), DateTime.UtcNow.AddHours(-i), reason: FastEndReason.Completed));
        }
        history.Add(Completed(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow, reason: FastEndReason.Hungry));
        var newly = await sut.EvaluateOnFastCompletedAsync(history[^1], new FastingProtocol());
        newly.Should().NotContain(b => b.Key == "disciplined_soul");
    }

    [Fact]
    public async Task Hydration_hero_unlocks_after_10_goal_hit_days()
    {
        var earnedStore = new List<EarnedBadge>();
        var earned = new Mock<IEarnedBadgeRepository>();
        earned.Setup(r => r.GetAllAsync()).ReturnsAsync(() => earnedStore.AsReadOnly());
        earned.Setup(r => r.HasAsync(It.IsAny<string>())).ReturnsAsync((string k) => earnedStore.Any(e => e.BadgeKey == k));
        earned.Setup(r => r.UpsertAsync(It.IsAny<EarnedBadge>())).Callback<EarnedBadge>(b =>
        {
            earnedStore.RemoveAll(e => e.BadgeKey == b.BadgeKey);
            earnedStore.Add(b);
        }).Returns(Task.CompletedTask);

        var fastsRepo = new Mock<IFastRepository>();
        fastsRepo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(new List<Fast>().AsReadOnly());

        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(new UserProfile { Id = Guid.NewGuid() });
        var protocolsRepo = new Mock<IFastingProtocolRepository>();
        protocolsRepo.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<FastingProtocol>());

        var water = new Mock<IWaterService>();
        water.Setup(w => w.CountGoalHitDaysAsync(It.IsAny<int>())).ReturnsAsync(10);
        var moods = new Mock<IMoodService>();
        moods.Setup(m => m.GetTotalCountAsync()).ReturnsAsync(0);

        var sut = new BadgeService(earned.Object, fastsRepo.Object, profiles.Object, protocolsRepo.Object, water.Object, moods.Object);
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "hydration_hero");
    }

    [Fact]
    public async Task Mood_tracker_unlocks_after_30_mood_entries()
    {
        var earnedStore = new List<EarnedBadge>();
        var earned = new Mock<IEarnedBadgeRepository>();
        earned.Setup(r => r.GetAllAsync()).ReturnsAsync(() => earnedStore.AsReadOnly());
        earned.Setup(r => r.HasAsync(It.IsAny<string>())).ReturnsAsync((string k) => earnedStore.Any(e => e.BadgeKey == k));
        earned.Setup(r => r.UpsertAsync(It.IsAny<EarnedBadge>())).Callback<EarnedBadge>(b =>
        {
            earnedStore.RemoveAll(e => e.BadgeKey == b.BadgeKey);
            earnedStore.Add(b);
        }).Returns(Task.CompletedTask);

        var fastsRepo = new Mock<IFastRepository>();
        fastsRepo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(new List<Fast>().AsReadOnly());
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(new UserProfile { Id = Guid.NewGuid() });
        var protocolsRepo = new Mock<IFastingProtocolRepository>();
        protocolsRepo.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<FastingProtocol>());

        var water = new Mock<IWaterService>();
        water.Setup(w => w.CountGoalHitDaysAsync(It.IsAny<int>())).ReturnsAsync(0);
        var moods = new Mock<IMoodService>();
        moods.Setup(m => m.GetTotalCountAsync()).ReturnsAsync(30);

        var sut = new BadgeService(earned.Object, fastsRepo.Object, profiles.Object, protocolsRepo.Object, water.Object, moods.Object);
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());
        newly.Should().Contain(b => b.Key == "mood_tracker");
    }

    [Fact]
    public async Task Trophy_hunter_unlocks_after_ten_earned()
    {
        var (sut, store, _, _, _) = Build();
        // Pre-seed 9 earned, then evaluate to add first_fast as the 10th.
        var nine = new[] { "day_one", "week_warrior", "month_master", "century", "marathon", "iron_will", "early_bird", "night_owl", "comeback_kid" };
        foreach (var k in nine) store.Add(new EarnedBadge { BadgeKey = k, EarnedAtUtc = DateTime.UtcNow });

        // First fast triggers first_fast, which makes count 10 → trophy_hunter
        var fast = Completed(DateTime.UtcNow.AddHours(-16), DateTime.UtcNow);
        var newly = await sut.EvaluateOnFastCompletedAsync(fast, new FastingProtocol());

        newly.Should().Contain(b => b.Key == "first_fast");
        newly.Should().Contain(b => b.Key == "trophy_hunter");
    }
}
