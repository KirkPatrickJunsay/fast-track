using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class CustomizeHomeViewModelTests
{
    private static (CustomizeHomeViewModel Vm, Mock<IDashboardPreferencesService> Prefs, Mock<INavigationService> Nav) Build(DashboardPreferences? initial = null)
    {
        var prefs = new Mock<IDashboardPreferencesService>();
        prefs.Setup(p => p.GetAsync()).ReturnsAsync(initial ?? DashboardPreferences.Default);
        prefs.Setup(p => p.SaveAsync(It.IsAny<DashboardPreferences>())).Returns(Task.CompletedTask);
        var nav = new Mock<INavigationService>();
        return (new CustomizeHomeViewModel(prefs.Object, nav.Object), prefs, nav);
    }

    [Fact]
    public async Task LoadAsync_mirrors_persisted_prefs_onto_observable_properties()
    {
        var initial = new DashboardPreferences
        {
            ShowGamification = false,
            ShowDailyHealth = true,
            ShowQuests = false,
            ShowProgressCards = true,
            ShowStagesRoadmap = false,
        };
        var (vm, _, _) = Build(initial);
        await vm.LoadAsync();

        vm.ShowGamification.Should().BeFalse();
        vm.ShowDailyHealth.Should().BeTrue();
        vm.ShowQuests.Should().BeFalse();
        vm.ShowProgressCards.Should().BeTrue();
        vm.ShowStagesRoadmap.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_does_not_trigger_save()
    {
        // The initial mirror is gated by _suppressSave so opening the page doesn't
        // immediately re-write the same prefs back to disk.
        var (vm, prefs, _) = Build();
        await vm.LoadAsync();
        prefs.Verify(p => p.SaveAsync(It.IsAny<DashboardPreferences>()), Times.Never);
    }

    [Fact]
    public async Task Toggling_a_switch_after_load_saves_immediately()
    {
        var (vm, prefs, _) = Build();
        await vm.LoadAsync();

        vm.ShowQuests = false;

        // The save fires on the partial OnChanged callback. Give the fire-and-forget
        // Task a tick to complete.
        await Task.Delay(20);

        prefs.Verify(p => p.SaveAsync(It.Is<DashboardPreferences>(d => d.ShowQuests == false)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Reset_restores_defaults_and_saves()
    {
        var initial = new DashboardPreferences { ShowGamification = false, ShowQuests = false };
        var (vm, prefs, _) = Build(initial);
        await vm.LoadAsync();

        await vm.ResetCommand.ExecuteAsync(null);

        vm.ShowGamification.Should().BeTrue();
        vm.ShowQuests.Should().BeTrue();
        prefs.Verify(p => p.SaveAsync(It.Is<DashboardPreferences>(d =>
            d.ShowGamification && d.ShowDailyHealth && d.ShowQuests && d.ShowProgressCards && d.ShowStagesRoadmap)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Back_calls_go_back()
    {
        var (vm, _, nav) = Build();
        await vm.BackCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
