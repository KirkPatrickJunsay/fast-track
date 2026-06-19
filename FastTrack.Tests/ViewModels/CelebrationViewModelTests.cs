using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class CelebrationViewModelTests
{
    private static (CelebrationViewModel Vm,
                    CelebrationCarrier Carrier,
                    Mock<INavigationService> Nav,
                    Mock<IHapticService> Haptics) Build()
    {
        var carrier = new CelebrationCarrier();
        var nav = new Mock<INavigationService>();
        var haptics = new Mock<IHapticService>();
        return (new CelebrationViewModel(carrier, nav.Object, haptics.Object), carrier, nav, haptics);
    }

    private static CelebrationData Sample(
        bool goalMet = true,
        int xp = 160,
        bool levelUp = false,
        int streak = 0,
        bool incremented = false,
        IReadOnlyList<BadgeDefinition>? badges = null,
        IReadOnlyList<string>? quests = null)
        => new(
            GoalMet: goalMet,
            Duration: TimeSpan.FromHours(16),
            StageName: "Fat-burning",
            XpEarned: xp,
            ComebackBonus: false,
            GoalExceededBonus: false,
            CurrentStreak: streak,
            StreakIncremented: incremented,
            FreezeConsumed: false,
            LevelledUp: levelUp,
            PreviousLevel: levelUp ? Level.All[0] : null,
            NewLevel: levelUp ? Level.All[1] : null,
            NewBadges: badges ?? Array.Empty<BadgeDefinition>(),
            ClaimedQuests: quests ?? Array.Empty<string>());

    [Fact]
    public async Task LoadAsync_renders_neutral_state_when_carrier_empty()
    {
        var (vm, _, _, _) = Build();
        await vm.LoadAsync();
        vm.Headline.Should().Be("Fast ended");
        vm.Emoji.Should().Be("·");
        vm.HasXp.Should().BeFalse();
        vm.HasStreak.Should().BeFalse();
        vm.HasLevelUp.Should().BeFalse();
        vm.HasBadges.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_uses_goal_complete_headline_when_goal_met()
    {
        var (vm, carrier, _, _) = Build();
        carrier.Set(Sample(goalMet: true));
        await vm.LoadAsync();
        vm.Headline.Should().Be("Fast complete");
        vm.Emoji.Should().Be("🎉");
        vm.DurationDisplay.Should().Be("16h 0m");
        vm.StageDisplay.Should().Contain("Fat-burning");
    }

    [Fact]
    public async Task LoadAsync_xp_card_renders_with_bonuses()
    {
        var (vm, carrier, _, _) = Build();
        carrier.Set(Sample(goalMet: true, xp: 240) with { ComebackBonus = true, GoalExceededBonus = true });
        await vm.LoadAsync();
        vm.HasXp.Should().BeTrue();
        vm.XpDisplay.Should().Be("+240 XP");
        vm.XpBonusDisplay.Should().Contain("comeback");
        vm.XpBonusDisplay.Should().Contain("goal beat");
    }

    [Fact]
    public async Task LoadAsync_streak_card_with_freeze_consumed()
    {
        var (vm, carrier, _, _) = Build();
        carrier.Set(Sample(streak: 7, incremented: true) with { FreezeConsumed = true });
        await vm.LoadAsync();
        vm.HasStreak.Should().BeTrue();
        vm.StreakDisplay.Should().Be("7 day streak");
        vm.StreakSubtext.Should().Contain("freeze");
    }

    [Fact]
    public async Task LoadAsync_level_up_card()
    {
        var (vm, carrier, _, _) = Build();
        carrier.Set(Sample(levelUp: true));
        await vm.LoadAsync();
        vm.HasLevelUp.Should().BeTrue();
        vm.LevelUpDisplay.Should().Contain("Novice Faster");
        vm.LevelUpDisplay.Should().Contain("Apprentice");
    }

    [Fact]
    public async Task LoadAsync_lists_new_badges_and_quests()
    {
        var (vm, carrier, _, _) = Build();
        carrier.Set(Sample(
            badges: new[] { new BadgeDefinition("first_fast", "First Fast", "x", "x.svg") },
            quests: new[] { "Finish a fast" }));
        await vm.LoadAsync();
        vm.HasBadges.Should().BeTrue();
        vm.Badges.Should().ContainSingle().Which.Should().Be("First Fast");
        vm.HasQuests.Should().BeTrue();
        vm.Quests.Should().ContainSingle().Which.Should().Be("Finish a fast");
    }

    [Fact]
    public async Task LoadAsync_fires_haptic_heavy_when_goal_met_and_light_otherwise()
    {
        var (vm, carrier, _, haptics) = Build();
        carrier.Set(Sample(goalMet: true));
        await vm.LoadAsync();
        haptics.Verify(h => h.Tick(HapticIntensity.Heavy), Times.Once);

        var (vm2, carrier2, _, haptics2) = Build();
        carrier2.Set(Sample(goalMet: false));
        await vm2.LoadAsync();
        haptics2.Verify(h => h.Tick(HapticIntensity.Light), Times.Once);
    }

    [Fact]
    public async Task ContinueAsync_navigates_back()
    {
        var (vm, _, nav, _) = Build();
        await vm.ContinueCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public void CelebrationCarrier_returns_then_clears()
    {
        var carrier = new CelebrationCarrier();
        carrier.Take().Should().BeNull();
        carrier.Set(Sample());
        carrier.Take().Should().NotBeNull();
        carrier.Take().Should().BeNull();
    }
}
