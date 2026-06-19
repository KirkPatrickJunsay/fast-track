using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Models;

public class DashboardPreferencesTests
{
    [Fact]
    public void Default_shows_every_card()
    {
        var d = DashboardPreferences.Default;
        d.ShowGamification.Should().BeTrue();
        d.ShowDailyHealth.Should().BeTrue();
        d.ShowQuests.Should().BeTrue();
        d.ShowProgressCards.Should().BeTrue();
        d.ShowStagesRoadmap.Should().BeTrue();
    }

    [Fact]
    public void FromJson_null_returns_default()
    {
        DashboardPreferences.FromJson(null).Should().Be(DashboardPreferences.Default);
    }

    [Fact]
    public void FromJson_empty_returns_default()
    {
        DashboardPreferences.FromJson(string.Empty).Should().Be(DashboardPreferences.Default);
    }

    [Fact]
    public void FromJson_garbage_returns_default()
    {
        DashboardPreferences.FromJson("{ this isn't valid").Should().Be(DashboardPreferences.Default);
    }

    [Fact]
    public void ToJson_then_FromJson_round_trips()
    {
        var custom = DashboardPreferences.Default with { ShowQuests = false, ShowStagesRoadmap = false };
        var roundTripped = DashboardPreferences.FromJson(custom.ToJson());
        roundTripped.Should().Be(custom);
    }
}
