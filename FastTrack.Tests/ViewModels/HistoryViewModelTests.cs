using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class HistoryViewModelTests
{
    private static (HistoryViewModel Vm, Mock<IFastRepository> Fasts, Mock<INavigationService> Nav) Build(
        IReadOnlyList<Fast>? history = null)
    {
        history ??= new List<Fast>();
        var fasts = new Mock<IFastRepository>();
        fasts.Setup(f => f.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(history);

        var protocols = new Mock<IFastingProtocolRepository>();
        protocols.Setup(p => p.GetAllAsync()).ReturnsAsync(new List<FastingProtocol>
        {
            new() { Id = Guid.Parse("a1000000-0000-0000-0000-000000000001"), Name = "16:8", FastHours = 16, EatHours = 8 },
        });

        var nav = new Mock<INavigationService>();
        var vm = new HistoryViewModel(fasts.Object, protocols.Object, nav.Object);
        return (vm, fasts, nav);
    }

    [Fact]
    public async Task LoadAsync_with_no_history_shows_empty_state()
    {
        var (vm, _, _) = Build();
        await vm.LoadAsync();
        vm.IsEmpty.Should().BeTrue();
        vm.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_maps_fasts_to_history_items()
    {
        var start = DateTime.UtcNow.AddDays(-1);
        var end = start.AddHours(16);
        var history = new List<Fast>
        {
            new() { Id = Guid.NewGuid(), StartUtc = start, EndUtc = end, GoalHours = 16,
                    ProtocolId = Guid.Parse("a1000000-0000-0000-0000-000000000001"), EndReason = FastEndReason.Completed },
            new() { Id = Guid.NewGuid(), StartUtc = start.AddDays(-1), EndUtc = start.AddDays(-1).AddHours(8),
                    GoalHours = 16, ProtocolId = Guid.Parse("a1000000-0000-0000-0000-000000000001"), EndReason = FastEndReason.Hungry },
        };
        var (vm, _, _) = Build(history);
        await vm.LoadAsync();

        vm.IsEmpty.Should().BeFalse();
        vm.Items.Should().HaveCount(2);
        vm.Items[0].GoalMet.Should().BeTrue();
        vm.Items[0].EndReasonDisplay.Should().Be("completed");
        vm.Items[1].GoalMet.Should().BeFalse();
        vm.Items[1].EndReasonDisplay.Should().Contain("hungry");
    }

    [Fact]
    public async Task OpenAsync_navigates_with_fastId()
    {
        var (vm, _, nav) = Build();
        var item = new HistoryItemViewModel { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), Title = "16:8" };
        await vm.OpenCommand.ExecuteAsync(item);
        nav.Verify(n => n.GoToAsync($"EditFastPage?fastId={item.Id}"), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_with_null_is_safe()
    {
        var (vm, _, nav) = Build();
        await vm.OpenCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }
}
