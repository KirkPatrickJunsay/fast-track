using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class HistoryViewModelTests
{
    private static (HistoryViewModel Vm,
                    Mock<IFastRepository> Fasts,
                    Mock<INavigationService> Nav,
                    Mock<IDialogService> Dialogs,
                    Mock<IFileShareService> Share) Build(
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
        var dialogs = new Mock<IDialogService>();
        var share = new Mock<IFileShareService>();
        var haptics = new Mock<IHapticService>();
        var vm = new HistoryViewModel(fasts.Object, protocols.Object, nav.Object, dialogs.Object, share.Object, haptics.Object);
        return (vm, fasts, nav, dialogs, share);
    }

    [Fact]
    public async Task LoadAsync_with_no_history_shows_empty_state()
    {
        var (vm, _, _, _, _) = Build();
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
        var (vm, _, _, _, _) = Build(history);
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
        var (vm, _, nav, _, _) = Build();
        var item = new HistoryItemViewModel { Id = Guid.Parse("11111111-2222-3333-4444-555555555555"), Title = "16:8" };
        await vm.OpenCommand.ExecuteAsync(item);
        nav.Verify(n => n.GoToAsync($"FastDetailPage?fastId={item.Id}"), Times.Once);
    }

    [Fact]
    public async Task OpenAsync_with_null_is_safe()
    {
        var (vm, _, nav, _, _) = Build();
        await vm.OpenCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task OptionsAsync_edit_navigates_to_edit_page()
    {
        var (vm, _, nav, dialogs, _) = Build();
        dialogs.Setup(d => d.ShowActionSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync("Edit");
        var item = new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "16:8" };
        await vm.OptionsCommand.ExecuteAsync(item);
        nav.Verify(n => n.GoToAsync($"EditFastPage?fastId={item.Id}"), Times.Once);
    }

    [Fact]
    public async Task OptionsAsync_share_invokes_text_share()
    {
        var (vm, _, _, dialogs, share) = Build();
        dialogs.Setup(d => d.ShowActionSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync("Share");
        var item = new HistoryItemViewModel
        {
            Id = Guid.NewGuid(), Title = "16:8",
            DurationDisplay = "16h 0m", GoalDisplay = "goal 16h",
            StartedLocal = "x", EndedLocal = "y", EndReasonDisplay = "completed",
        };
        await vm.OptionsCommand.ExecuteAsync(item);
        share.Verify(s => s.ShareTextAsync(It.IsAny<string>(), It.Is<string>(t => t.Contains("16:8"))), Times.Once);
    }

    [Fact]
    public async Task OptionsAsync_delete_confirms_then_deletes_and_reloads()
    {
        var (vm, fasts, _, dialogs, _) = Build();
        dialogs.Setup(d => d.ShowActionSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync("Delete");
        dialogs.Setup(d => d.ConfirmAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);
        var item = new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "16:8" };
        await vm.OptionsCommand.ExecuteAsync(item);
        fasts.Verify(f => f.DeleteAsync(item.Id), Times.Once);
        fasts.Verify(f => f.GetHistoryAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task OptionsAsync_delete_cancelled_does_not_delete()
    {
        var (vm, fasts, _, dialogs, _) = Build();
        dialogs.Setup(d => d.ShowActionSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync("Delete");
        dialogs.Setup(d => d.ConfirmAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(false);
        await vm.OptionsCommand.ExecuteAsync(new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "x" });
        fasts.Verify(f => f.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SwipeDeleteAsync_confirms_then_deletes_and_reloads()
    {
        var (vm, fasts, _, dialogs, _) = Build();
        dialogs.Setup(d => d.ConfirmAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(true);
        var item = new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "16:8" };
        await vm.SwipeDeleteCommand.ExecuteAsync(item);
        fasts.Verify(f => f.DeleteAsync(item.Id), Times.Once);
        fasts.Verify(f => f.GetHistoryAsync(It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task SwipeDeleteAsync_cancelled_does_not_delete()
    {
        var (vm, fasts, _, dialogs, _) = Build();
        dialogs.Setup(d => d.ConfirmAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync(false);
        await vm.SwipeDeleteCommand.ExecuteAsync(new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "x" });
        fasts.Verify(f => f.DeleteAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task OptionsAsync_cancel_does_nothing()
    {
        var (vm, fasts, nav, dialogs, share) = Build();
        dialogs.Setup(d => d.ShowActionSheetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string[]>()))
               .ReturnsAsync((string?)null);
        await vm.OptionsCommand.ExecuteAsync(new HistoryItemViewModel { Id = Guid.NewGuid(), Title = "x" });
        fasts.Verify(f => f.DeleteAsync(It.IsAny<Guid>()), Times.Never);
        nav.Verify(n => n.GoToAsync(It.IsAny<string>()), Times.Never);
        share.Verify(s => s.ShareTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
