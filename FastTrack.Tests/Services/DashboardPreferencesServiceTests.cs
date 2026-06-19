using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class DashboardPreferencesServiceTests
{
    [Fact]
    public async Task GetAsync_with_no_persisted_json_returns_default()
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), DashboardWidgetsJson = null };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        var svc = new DashboardPreferencesService(profiles.Object);

        var prefs = await svc.GetAsync();

        prefs.Should().Be(DashboardPreferences.Default);
    }

    [Fact]
    public async Task SaveAsync_persists_json_and_calls_update()
    {
        var profile = new UserProfile { Id = Guid.NewGuid() };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        var svc = new DashboardPreferencesService(profiles.Object);

        var toSave = DashboardPreferences.Default with { ShowQuests = false };
        await svc.SaveAsync(toSave);

        profile.DashboardWidgetsJson.Should().NotBeNullOrEmpty();
        DashboardPreferences.FromJson(profile.DashboardWidgetsJson).Should().Be(toSave);
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task GetAsync_round_trips_with_SaveAsync()
    {
        var profile = new UserProfile { Id = Guid.NewGuid() };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        var svc = new DashboardPreferencesService(profiles.Object);

        var toSave = new DashboardPreferences { ShowGamification = false, ShowDailyHealth = false };
        await svc.SaveAsync(toSave);
        var loaded = await svc.GetAsync();

        loaded.Should().Be(toSave);
    }
}
