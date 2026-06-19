using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class StageDetailViewModelTests
{
    private static (StageDetailViewModel Vm, Mock<INavigationService> Nav) Build()
    {
        var stages = new StageCalculator();
        var nav = new Mock<INavigationService>();
        return (new StageDetailViewModel(stages, nav.Object), nav);
    }

    [Fact]
    public void LoadFromKey_populates_known_stage()
    {
        var (vm, _) = Build();
        vm.LoadFromKey("ketosis");
        vm.Name.Should().Be("Ketosis");
        vm.IconAsset.Should().Be("stage_ketosis.svg");
        vm.RangeDisplay.Should().Be("18–24 hours");
        vm.StageIndexDisplay.Should().Be("Stage 4 of 7");
        vm.Feelings.Should().NotBeEmpty();
        vm.LongDescription.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void LoadFromKey_unknown_falls_back_to_first_stage()
    {
        var (vm, _) = Build();
        vm.LoadFromKey("does-not-exist");
        vm.Name.Should().Be("Anabolic");
        vm.StageIndexDisplay.Should().Be("Stage 1 of 7");
    }

    [Fact]
    public void LoadFromKey_null_falls_back_to_first_stage()
    {
        var (vm, _) = Build();
        vm.LoadFromKey(null);
        vm.Name.Should().Be("Anabolic");
    }

    [Fact]
    public void LoadFromKey_extended_stage_uses_open_ended_range()
    {
        var (vm, _) = Build();
        vm.LoadFromKey("extended");
        vm.RangeDisplay.Should().Contain("72");
        vm.RangeDisplay.Should().Contain("beyond");
    }

    [Fact]
    public async Task CloseAsync_navigates_back()
    {
        var (vm, nav) = Build();
        await vm.CloseCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
