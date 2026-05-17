using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class TrophyCabinetViewModelTests
{
    [Fact]
    public async Task LoadAsync_maps_evaluated_badges_with_conceal_logic()
    {
        var badges = new Mock<IBadgeService>();
        var earnedAt = DateTime.UtcNow.AddDays(-1);
        badges.Setup(b => b.GetAllAsync()).ReturnsAsync(new List<EvaluatedBadge>
        {
            new(new BadgeDefinition("first_fast", "First Fast", "Complete your first fast.", "x.svg"), true, earnedAt),
            new(new BadgeDefinition("week_warrior", "Week Warrior", "7-day streak.", "x.svg"), false, null),
            new(new BadgeDefinition("hidden_a", "Hidden A", "Secret stuff.", "x.svg", IsHidden: true), false, null),
            new(new BadgeDefinition("hidden_b", "Hidden B", "Also secret.", "x.svg", IsHidden: true), true, earnedAt),
        });

        var vm = new TrophyCabinetViewModel(badges.Object);
        await vm.LoadAsync();

        vm.Items.Should().HaveCount(4);
        vm.Summary.Should().Be("2 of 4 badges earned");

        var first = vm.Items[0];
        first.IsEarned.Should().BeTrue();
        first.EarnedText.Should().StartWith("Earned");

        var locked = vm.Items[1];
        locked.IsEarned.Should().BeFalse();
        locked.EarnedText.Should().Be("Locked");

        // Hidden-and-unearned conceals name and description.
        var concealed = vm.Items[2];
        concealed.Name.Should().Be("???");
        concealed.Description.Should().Contain("Hidden");
        concealed.EarnedText.Should().Be("Hidden");

        // Hidden-and-earned reveals everything.
        var revealed = vm.Items[3];
        revealed.Name.Should().Be("Hidden B");
        revealed.EarnedText.Should().StartWith("Earned");
    }
}
