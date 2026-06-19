using FastTrack.Data;
using FastTrack.Models;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class BootViewModelTests
{
    private static BootViewModel Build(UserProfile profile, Mock<IUserProfileRepository>? profilesOut = null)
    {
        var profiles = profilesOut ?? new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        return new BootViewModel(profiles.Object);
    }

    [Fact]
    public async Task DecideRoute_returns_main_when_onboarding_completed()
    {
        var vm = Build(new UserProfile { Id = Guid.NewGuid(), OnboardingCompleted = true });
        var route = await vm.DecideRouteAsync();
        route.Should().Be(BootViewModel.MainRoute);
    }

    [Fact]
    public async Task DecideRoute_returns_onboarding_when_onboarding_not_completed()
    {
        var vm = Build(new UserProfile { Id = Guid.NewGuid(), OnboardingCompleted = false });
        var route = await vm.DecideRouteAsync();
        route.Should().Be(BootViewModel.OnboardingRoute);
    }

    [Fact]
    public async Task DecideRoute_falls_back_to_onboarding_when_repository_throws()
    {
        // If the profile read blows up on cold launch we want to land in onboarding
        // so the user can re-establish a profile, not hang on the boot screen.
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ThrowsAsync(new InvalidOperationException("db missing"));
        var vm = new BootViewModel(profiles.Object);

        var route = await vm.DecideRouteAsync();

        route.Should().Be(BootViewModel.OnboardingRoute);
    }

    [Fact]
    public void Route_constants_are_absolute()
    {
        // Shell needs absolute routes ("//...") for top-level navigation away from BootPage.
        BootViewModel.MainRoute.Should().StartWith("//");
        BootViewModel.OnboardingRoute.Should().StartWith("//");
    }
}
