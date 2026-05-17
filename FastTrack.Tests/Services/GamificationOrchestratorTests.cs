using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class GamificationOrchestratorTests
{
    private sealed class FakeFastingService : IFastingService
    {
        public event EventHandler<Fast>? FastCompleted;
        public Task<Fast?> GetActiveAsync() => Task.FromResult<Fast?>(null);
        public Task<Fast> StartAsync(Guid p, DateTime? s = null) => throw new NotImplementedException();
        public Task<Fast> EndAsync(Guid id, FastEndReason r, DateTime? e = null) => throw new NotImplementedException();
        public Task<Fast> EditTimesAsync(Guid id, DateTime s, DateTime? e) => throw new NotImplementedException();
        public void Raise(Fast f) => FastCompleted?.Invoke(this, f);
    }

    private static (GamificationOrchestrator Sut,
                    FakeFastingService Fasting,
                    Mock<IXpService> Xp,
                    Mock<IStreakService> Streaks,
                    Mock<IBadgeService> Badges,
                    Mock<IQuestService> Quests,
                    UserProfile Profile)
        Build(UserProfile? profile = null)
    {
        profile ??= new UserProfile { Id = Guid.NewGuid(), TotalXp = 0 };
        var fasting = new FakeFastingService();
        var fastsRepo = new Mock<IFastRepository>();
        fastsRepo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(new List<Fast>());

        var protocols = new Mock<IFastingProtocolRepository>();
        protocols.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new FastingProtocol { Difficulty = Difficulty.Beginner });

        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        var streaks = new Mock<IStreakService>();
        streaks.Setup(s => s.RecordCompletedFastAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()))
               .ReturnsAsync(new StreakResult(1, 1, true, false, false));

        var xp = new Mock<IXpService>();
        xp.Setup(x => x.CalculateForFast(It.IsAny<Fast>(), It.IsAny<FastingProtocol>(), It.IsAny<int>(), It.IsAny<bool>()))
          .Returns(new XpAward(160, 1.0, false, 1.0, false, 160));
        xp.Setup(x => x.AwardAsync(It.IsAny<int>()))
          .ReturnsAsync(new XpState(160, Level.All[0], Level.All[1], 160, 500));
        xp.Setup(x => x.GetStateAsync())
          .ReturnsAsync(new XpState(160, Level.All[0], Level.All[1], 160, 500));

        var badges = new Mock<IBadgeService>();
        badges.Setup(b => b.EvaluateOnFastCompletedAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()))
              .ReturnsAsync(new List<BadgeDefinition>());

        var quests = new Mock<IQuestService>();
        quests.Setup(q => q.OnFastCompletedAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>(), It.IsAny<IReadOnlyList<Fast>>()))
              .ReturnsAsync(new List<QuestUpdate>());

        var sut = new GamificationOrchestrator(fasting, fastsRepo.Object, protocols.Object, profiles.Object, streaks.Object, xp.Object, badges.Object, quests.Object);
        return (sut, fasting, xp, streaks, badges, quests, profile);
    }

    private static Fast SampleFast() =>
        new() { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 };

    private static async Task<GamificationResult?> RaiseAndCapture(GamificationOrchestrator sut, FakeFastingService fasting, Fast fast)
    {
        GamificationResult? captured = null;
        sut.RewardsGranted += (_, r) => captured = r;
        sut.Start();
        fasting.Raise(fast);
        // ProcessAsync runs async-void; give it a moment.
        await Task.Delay(50);
        return captured;
    }

    [Fact]
    public async Task Start_is_idempotent_subscribing_only_once()
    {
        var (sut, fasting, _, streaks, _, _, _) = Build();
        sut.Start();
        sut.Start();
        fasting.Raise(SampleFast());
        await Task.Delay(50);
        streaks.Verify(s => s.RecordCompletedFastAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()), Times.Once);
    }

    [Fact]
    public async Task Processing_increments_completed_count_and_clears_comeback()
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), CompletedFastsCount = 7, ComebackBonusPending = true };
        var (sut, fasting, _, _, _, _, _) = Build(profile);
        await RaiseAndCapture(sut, fasting, SampleFast());
        profile.CompletedFastsCount.Should().Be(8);
        profile.ComebackBonusPending.Should().BeFalse();
    }

    [Fact]
    public async Task Comeback_pending_at_entry_flows_into_xp_calculation()
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), ComebackBonusPending = true };
        var (sut, fasting, xp, _, _, _, _) = Build(profile);
        await RaiseAndCapture(sut, fasting, SampleFast());
        xp.Verify(x => x.CalculateForFast(It.IsAny<Fast>(), It.IsAny<FastingProtocol>(), It.IsAny<int>(), true), Times.Once);
    }

    [Fact]
    public async Task LevelledUp_is_set_when_level_increases()
    {
        // Profile starts at 400 (Novice), XP service returns post-award state at Apprentice (level 2).
        var profile = new UserProfile { Id = Guid.NewGuid(), TotalXp = 400 };
        var (sut, fasting, xp, _, _, _, _) = Build(profile);
        xp.Setup(x => x.AwardAsync(It.IsAny<int>()))
          .ReturnsAsync(new XpState(560, Level.All[1], Level.All[2], 60, 1000));

        var result = await RaiseAndCapture(sut, fasting, SampleFast());
        result.Should().NotBeNull();
        result!.LevelledUp.Should().BeTrue();
        result.PreviousLevel!.Number.Should().Be(1);
        result.XpState.Level.Number.Should().Be(2);
    }

    [Fact]
    public async Task Quest_completion_refreshes_xp_state()
    {
        var (sut, fasting, xp, _, _, quests, _) = Build();
        var doneQuest = new DailyQuest { Id = 1, QuestKey = "complete_one", Target = 1, Progress = 1, XpReward = 25, IsClaimed = true };
        var def = new QuestDefinition("complete_one", "Finish a fast", "Any fast.", QuestType.CompleteAnyFast, 1, 25);
        quests.Setup(q => q.OnFastCompletedAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>(), It.IsAny<IReadOnlyList<Fast>>()))
              .ReturnsAsync(new List<QuestUpdate> { new(doneQuest, def, NewlyCompleted: true) });

        await RaiseAndCapture(sut, fasting, SampleFast());

        // Once for the fast XP, once for the quest-refresh GetStateAsync.
        xp.Verify(x => x.GetStateAsync(), Times.Once);
    }

    [Fact]
    public async Task Missing_protocol_swallows_and_skips_processing()
    {
        var profile = new UserProfile { Id = Guid.NewGuid() };
        var fasting = new FakeFastingService();
        var fastsRepo = new Mock<IFastRepository>();
        fastsRepo.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(new List<Fast>());

        var protocols = new Mock<IFastingProtocolRepository>();
        protocols.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FastingProtocol?)null);

        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);

        var streaks = new Mock<IStreakService>();
        var xp = new Mock<IXpService>();
        var badges = new Mock<IBadgeService>();
        var quests = new Mock<IQuestService>();

        var sut = new GamificationOrchestrator(fasting, fastsRepo.Object, protocols.Object, profiles.Object, streaks.Object, xp.Object, badges.Object, quests.Object);
        sut.Start();
        fasting.Raise(SampleFast());
        await Task.Delay(50);

        streaks.Verify(s => s.RecordCompletedFastAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()), Times.Never);
    }
}
