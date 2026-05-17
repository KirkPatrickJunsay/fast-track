using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class LogMoodViewModelTests
{
    private static (LogMoodViewModel Vm, Mock<IMoodService> Moods, Mock<INavigationService> Nav) Build()
    {
        var moods = new Mock<IMoodService>();
        moods.Setup(m => m.LogAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<string?>()))
             .ReturnsAsync(new MoodEntry());
        var nav = new Mock<INavigationService>();
        return (new LogMoodViewModel(moods.Object, nav.Object), moods, nav);
    }

    [Fact]
    public void CanSave_only_when_level_picked()
    {
        var (vm, _, _) = Build();
        vm.CanSave.Should().BeFalse();
        vm.PickLevelCommand.Execute("3");
        vm.CanSave.Should().BeTrue();
        vm.IsLevel3Selected.Should().BeTrue();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    [InlineData("nope")]
    public void Invalid_level_is_ignored(string param)
    {
        var (vm, _, _) = Build();
        vm.PickLevelCommand.Execute(param);
        vm.SelectedLevel.Should().Be(0);
        vm.CanSave.Should().BeFalse();
    }

    [Fact]
    public async Task Save_without_level_sets_error()
    {
        var (vm, moods, _) = Build();
        await vm.SaveCommand.ExecuteAsync(null);
        vm.HasError.Should().BeTrue();
        moods.Verify(m => m.LogAsync(It.IsAny<int>(), It.IsAny<DateTime?>(), It.IsAny<Guid?>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task Save_logs_with_picked_level_and_no_fast()
    {
        var (vm, moods, nav) = Build();
        vm.PickLevelCommand.Execute("4");
        vm.Note = "feels light";
        await vm.SaveCommand.ExecuteAsync(null);
        moods.Verify(m => m.LogAsync(4, It.IsAny<DateTime?>(), null, "feels light"), Times.Once);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task Save_parses_FastId_query_param()
    {
        var (vm, moods, _) = Build();
        var id = Guid.NewGuid();
        vm.FastId = id.ToString();
        vm.PickLevelCommand.Execute("5");
        await vm.SaveCommand.ExecuteAsync(null);
        moods.Verify(m => m.LogAsync(5, It.IsAny<DateTime?>(), id, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Save_with_bogus_FastId_logs_without_link()
    {
        var (vm, moods, _) = Build();
        vm.FastId = "not-a-guid";
        vm.PickLevelCommand.Execute("3");
        await vm.SaveCommand.ExecuteAsync(null);
        moods.Verify(m => m.LogAsync(3, It.IsAny<DateTime?>(), null, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_navigates_back()
    {
        var (vm, _, nav) = Build();
        await vm.CancelCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
