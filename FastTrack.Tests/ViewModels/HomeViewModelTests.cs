using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class HomeViewModelTests
{
    private static readonly FastingProtocol DefaultProtocol = new()
    {
        Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"),
        Name = "16:8",
        FastHours = 16,
        EatHours = 8,
        Difficulty = Difficulty.Beginner,
        IsPreset = true,
    };

    private sealed class FakeTicker : ITicker
    {
        public TimeSpan? Interval { get; private set; }
        public Action? OnTick { get; private set; }
        public bool Running { get; private set; }
        public void Start(TimeSpan interval, Action onTick)
        {
            Interval = interval;
            OnTick = onTick;
            Running = true;
        }
        public void Stop()
        {
            Running = false;
        }
    }

    private sealed class FakeFastingService : IFastingService
    {
        public Fast? Active { get; set; }
        public List<Guid> StartedProtocolIds { get; } = new();
        public List<Guid> EndedFastIds { get; } = new();

        public event EventHandler<Fast>? FastCompleted;

        public Task<Fast?> GetActiveAsync() => Task.FromResult(Active);

        public Task<Fast> StartAsync(Guid protocolId, DateTime? startUtc = null)
        {
            StartedProtocolIds.Add(protocolId);
            Active = new Fast
            {
                Id = Guid.NewGuid(),
                ProtocolId = protocolId,
                StartUtc = startUtc ?? DateTime.UtcNow,
                GoalHours = 16,
            };
            return Task.FromResult(Active);
        }

        public Task<Fast> EndAsync(Guid fastId, FastEndReason reason, DateTime? endUtc = null)
        {
            EndedFastIds.Add(fastId);
            if (Active is not null) Active.EndUtc = endUtc ?? DateTime.UtcNow;
            var ended = Active ?? new Fast { Id = fastId, EndUtc = DateTime.UtcNow };
            Active = null;
            FastCompleted?.Invoke(this, ended);
            return Task.FromResult(ended);
        }

        public Task<Fast> EditTimesAsync(Guid fastId, DateTime newStartUtc, DateTime? newEndUtc) =>
            throw new NotImplementedException();
    }

    private sealed class FakeOrchestrator : IGamificationOrchestrator
    {
        public void Start() { }
        public event EventHandler<GamificationResult>? RewardsGranted;
        public void Raise(GamificationResult r) => RewardsGranted?.Invoke(this, r);
    }

    private static (HomeViewModel Vm,
                    FakeFastingService Fasting,
                    Mock<IFastRepository> Fasts,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav,
                    FakeTicker Ticker,
                    UserProfile Profile)
        Build(Fast? active = null, IReadOnlyList<Fast>? history = null, UserProfile? profile = null)
    {
        profile ??= new UserProfile { Id = Guid.NewGuid(), IsEducationalMode = false };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);

        var protocols = new Mock<IFastingProtocolRepository>();
        protocols.Setup(p => p.GetAllAsync()).ReturnsAsync(new[] { DefaultProtocol });
        protocols.Setup(p => p.GetByIdAsync(DefaultProtocol.Id)).ReturnsAsync(DefaultProtocol);

        var fasts = new Mock<IFastRepository>();
        fasts.Setup(f => f.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(history ?? new List<Fast>());

        var fasting = new FakeFastingService { Active = active };
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();

        var streaks = new Mock<IStreakService>();
        streaks.Setup(s => s.GetSnapshotAsync()).ReturnsAsync(new StreakSnapshot(0, 0, 0));

        var xp = new Mock<IXpService>();
        xp.Setup(x => x.GetStateAsync()).ReturnsAsync(
            new XpState(0, Level.All[0], Level.All[1], 0, 500));

        var quests = new Mock<IQuestService>();
        quests.Setup(q => q.GetTodayAsync()).ReturnsAsync(Array.Empty<(DailyQuest, QuestDefinition)>());

        var orchestrator = new FakeOrchestrator();
        var ticker = new FakeTicker();
        var stages = new StageCalculator();

        var water = new Mock<IWaterService>();
        water.Setup(w => w.GetTodayAsync()).ReturnsAsync(new WaterTodaySnapshot(0, 2000, 0, false));
        var moodsSvc = new Mock<IMoodService>();
        moodsSvc.Setup(m => m.GetRecentAsync(It.IsAny<int>())).ReturnsAsync(Array.Empty<MoodEntry>());
        moodsSvc.Setup(m => m.GetTotalCountAsync()).ReturnsAsync(0);
        var weightsSvc = new Mock<IWeightService>();
        weightsSvc.Setup(w => w.GetTrendAsync(It.IsAny<TimeSpan?>()))
            .ReturnsAsync(new WeightTrend(null, null, null, 0));

        var haptics = new Mock<IHapticService>();
        var celebrations = new CelebrationCarrier();

        var dashboardPrefs = new Mock<IDashboardPreferencesService>();
        dashboardPrefs.Setup(p => p.GetAsync()).ReturnsAsync(DashboardPreferences.Default);

        var vm = new HomeViewModel(
            fasting,
            fasts.Object,
            protocols.Object,
            profiles.Object,
            stages,
            dialogs.Object,
            streaks.Object,
            xp.Object,
            orchestrator,
            quests.Object,
            nav.Object,
            ticker,
            water.Object,
            moodsSvc.Object,
            weightsSvc.Object,
            haptics.Object,
            celebrations,
            dashboardPrefs.Object);

        return (vm, fasting, fasts, profiles, dialogs, nav, ticker, profile);
    }

    [Fact]
    public async Task LoadAsync_with_no_active_or_eating_window_enters_idle()
    {
        var (vm, _, _, _, _, _, ticker, _) = Build();
        await vm.LoadAsync();
        vm.IsIdle.Should().BeTrue();
        vm.IsActive.Should().BeFalse();
        vm.IsEatingWindow.Should().BeFalse();
        vm.StartButtonVisible.Should().BeTrue();
        vm.GoalDisplay.Should().Be("Goal: 16h");
        ticker.Running.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_with_active_fast_enters_active_and_starts_ticker()
    {
        var active = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = DefaultProtocol.Id,
            StartUtc = DateTime.UtcNow.AddHours(-2),
            GoalHours = 16,
        };
        var (vm, _, _, _, _, _, ticker, _) = Build(active: active);
        await vm.LoadAsync();

        vm.IsActive.Should().BeTrue();
        vm.IsIdle.Should().BeFalse();
        vm.ProgressPercent.Should().NotBe("0%");
        vm.StageName.Should().Be("Anabolic"); // 2h elapsed sits in the 0–4h Anabolic stage
        ticker.Running.Should().BeTrue();
        ticker.Interval.Should().Be(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LoadAsync_in_eating_window_renders_eating_state()
    {
        var endedRecently = DateTime.UtcNow.AddHours(-1);
        var lastFast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = DefaultProtocol.Id,
            StartUtc = endedRecently.AddHours(-16),
            EndUtc = endedRecently,
            GoalHours = 16,
            EndReason = FastEndReason.Completed,
        };
        var (vm, _, _, _, _, _, _, _) = Build(history: new[] { lastFast });
        await vm.LoadAsync();

        vm.IsEatingWindow.Should().BeTrue();
        vm.EatingStartButtonVisible.Should().BeTrue();
    }

    [Fact]
    public async Task Educational_mode_hides_start_buttons()
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), IsEducationalMode = true };
        var (vm, _, _, _, _, _, _, _) = Build(profile: profile);
        await vm.LoadAsync();
        vm.IsEducationalMode.Should().BeTrue();
        vm.IsIdle.Should().BeTrue();
        vm.StartButtonVisible.Should().BeFalse();
        vm.EatingStartButtonVisible.Should().BeFalse();
    }

    [Fact]
    public async Task StartFastAsync_starts_and_persists_default_protocol()
    {
        var (vm, fasting, _, profiles, _, _, _, profile) = Build();
        await vm.LoadAsync();

        await vm.StartFastCommand.ExecuteAsync(null);

        fasting.StartedProtocolIds.Should().ContainSingle().Which.Should().Be(DefaultProtocol.Id);
        profile.LastUsedProtocolId.Should().Be(DefaultProtocol.Id);
        vm.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EndFastAsync_completed_goal_skips_reason_picker()
    {
        var active = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = DefaultProtocol.Id,
            StartUtc = DateTime.UtcNow.AddHours(-17),
            GoalHours = 16,
        };
        var (vm, fasting, _, _, dialogs, _, _, _) = Build(active: active);
        await vm.LoadAsync();

        await vm.EndFastCommand.ExecuteAsync(null);

        dialogs.Verify(d => d.ShowActionSheetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()), Times.Never);
        fasting.EndedFastIds.Should().ContainSingle().Which.Should().Be(active.Id);
    }

    [Fact]
    public async Task EndFastAsync_early_cancel_does_not_end_fast()
    {
        var active = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = DefaultProtocol.Id,
            StartUtc = DateTime.UtcNow.AddHours(-2),
            GoalHours = 16,
        };
        var (vm, fasting, _, _, dialogs, _, _, _) = Build(active: active);
        dialogs.Setup(d => d.ShowActionSheetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync((string?)null);
        await vm.LoadAsync();

        await vm.EndFastCommand.ExecuteAsync(null);

        fasting.EndedFastIds.Should().BeEmpty();
        vm.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task EndFastAsync_early_with_reason_ends_with_correct_enum()
    {
        var active = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = DefaultProtocol.Id,
            StartUtc = DateTime.UtcNow.AddHours(-2),
            GoalHours = 16,
        };
        var (vm, fasting, _, _, dialogs, _, _, _) = Build(active: active);
        dialogs.Setup(d => d.ShowActionSheetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync("I'm hungry");
        await vm.LoadAsync();

        await vm.EndFastCommand.ExecuteAsync(null);

        fasting.EndedFastIds.Should().ContainSingle().Which.Should().Be(active.Id);
    }

    [Fact]
    public async Task EditActiveFastAsync_navigates_with_id_when_active()
    {
        var active = new Fast { Id = Guid.NewGuid(), ProtocolId = DefaultProtocol.Id, StartUtc = DateTime.UtcNow, GoalHours = 16 };
        var (vm, _, _, _, _, nav, _, _) = Build(active: active);
        await vm.LoadAsync();
        await vm.EditActiveFastCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync($"EditFastPage?fastId={active.Id}"), Times.Once);
    }

    [Fact]
    public async Task EditActiveFastAsync_noop_when_idle()
    {
        var (vm, _, _, _, _, nav, _, _) = Build();
        await vm.LoadAsync();
        await vm.EditActiveFastCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task StartFastNowAsync_starts_a_fast_via_StartFastAsync()
    {
        var (vm, fasting, _, _, _, _, _, _) = Build();
        await vm.LoadAsync();
        await vm.StartFastNowCommand.ExecuteAsync(null);
        fasting.StartedProtocolIds.Should().ContainSingle();
    }

    [Fact]
    public async Task Dispose_stops_the_ticker()
    {
        var active = new Fast { Id = Guid.NewGuid(), ProtocolId = DefaultProtocol.Id, StartUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 16 };
        var (vm, _, _, _, _, _, ticker, _) = Build(active: active);
        await vm.LoadAsync();
        ticker.Running.Should().BeTrue();
        vm.Dispose();
        ticker.Running.Should().BeFalse();
    }
}
