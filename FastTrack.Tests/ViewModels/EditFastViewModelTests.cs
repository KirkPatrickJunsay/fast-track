using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class EditFastViewModelTests
{
    private static readonly FastingProtocol[] DefaultProtocols = new[]
    {
        new FastingProtocol { Id = Guid.NewGuid(), Name = "16:8", FastHours = 16, EatHours = 8, Difficulty = Difficulty.Beginner },
        new FastingProtocol { Id = Guid.NewGuid(), Name = "18:6", FastHours = 18, EatHours = 6, Difficulty = Difficulty.Intermediate },
        new FastingProtocol { Id = Guid.NewGuid(), Name = "OMAD", FastHours = 23, EatHours = 1, Difficulty = Difficulty.Advanced },
    };

    private static (EditFastViewModel Vm,
                    Mock<IFastingService> Fasting,
                    Mock<IFastRepository> Fasts,
                    Mock<IFastingProtocolRepository> Protocols,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav)
        Build(Fast? fast = null, IReadOnlyList<FastingProtocol>? protocols = null)
    {
        var fasts = new Mock<IFastRepository>();
        if (fast is not null)
            fasts.Setup(r => r.GetByIdAsync(fast.Id)).ReturnsAsync(fast);
        var fasting = new Mock<IFastingService>();
        fasting.Setup(s => s.EditTimesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
               .ReturnsAsync((Guid id, DateTime s, DateTime? e) => new Fast { Id = id, StartUtc = s, EndUtc = e });
        var protoRepo = new Mock<IFastingProtocolRepository>();
        protoRepo.Setup(p => p.GetAllAsync()).ReturnsAsync(protocols ?? DefaultProtocols);
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        return (new EditFastViewModel(fasting.Object, fasts.Object, protoRepo.Object, dialogs.Object, nav.Object),
                fasting, fasts, protoRepo, dialogs, nav);
    }

    [Fact]
    public async Task LoadAsync_with_invalid_id_sets_error()
    {
        var (vm, _, _, _, _, _) = Build();
        vm.FastId = "not-a-guid";
        await vm.LoadAsync();
        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Be("Invalid fast id.");
    }

    [Fact]
    public async Task LoadAsync_with_missing_fast_sets_error()
    {
        var fasts = new Mock<IFastRepository>();
        fasts.Setup(f => f.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Fast?)null);
        var fasting = new Mock<IFastingService>();
        var protoRepo = new Mock<IFastingProtocolRepository>();
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        var vm = new EditFastViewModel(fasting.Object, fasts.Object, protoRepo.Object, dialogs.Object, nav.Object)
        {
            FastId = Guid.NewGuid().ToString(),
        };
        await vm.LoadAsync();
        vm.ErrorMessage.Should().Be("Fast not found.");
    }

    [Fact]
    public async Task LoadAsync_hydrates_active_fast_in_local_time()
    {
        var startUtc = DateTime.UtcNow.AddHours(-5);
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = startUtc, EndUtc = null, GoalHours = 16 };
        var (vm, _, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.IsPastFast.Should().BeFalse();
        var local = startUtc.ToLocalTime();
        vm.StartDate.Should().Be(local.Date);
        vm.StartTime.Should().BeCloseTo(local.TimeOfDay, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task LoadAsync_hydrates_past_fast_with_end_picker_visible()
    {
        var startUtc = DateTime.UtcNow.AddDays(-1);
        var endUtc = startUtc.AddHours(16);
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = startUtc, EndUtc = endUtc, GoalHours = 16 };
        var (vm, _, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.IsPastFast.Should().BeTrue();
        vm.EndDate.Should().Be(endUtc.ToLocalTime().Date);
    }

    [Fact]
    public async Task SaveAsync_calls_service_with_utc_times_and_navigates_back()
    {
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-3), EndUtc = null, GoalHours = 16 };
        var (vm, fasting, _, _, dialogs, nav) = Build(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();

        vm.StartDate = DateTime.Today.AddDays(-1);
        vm.StartTime = new TimeSpan(8, 0, 0);

        await vm.SaveCommand.ExecuteAsync(null);

        fasting.Verify(s => s.EditTimesAsync(fast.Id, It.IsAny<DateTime>(), null), Times.Once);
        dialogs.Verify(d => d.ShowAlertAsync("Saved", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_surfaces_service_error()
    {
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), EndUtc = null, GoalHours = 16 };
        var (vm, fasting, _, _, _, nav) = Build(fast);
        fasting.Setup(s => s.EditTimesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
               .ThrowsAsync(new InvalidOperationException("nope"));
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        vm.HasError.Should().BeTrue();
        vm.ErrorMessage.Should().Be("nope");
        nav.Verify(n => n.GoBackAsync(), Times.Never);
    }

    [Fact]
    public async Task LoadAsync_active_fast_populates_protocol_picker_with_current_selection()
    {
        var current = DefaultProtocols[1]; // 18:6
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), EndUtc = null, GoalHours = 18, ProtocolId = current.Id };
        var (vm, _, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.Protocols.Should().HaveCount(DefaultProtocols.Length);
        vm.SelectedProtocol.Should().NotBeNull();
        vm.SelectedProtocol!.Id.Should().Be(current.Id);
        vm.IsActiveFast.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_past_fast_does_not_populate_protocol_picker()
    {
        // Protocol swap is an active-fast-only feature; for a completed fast
        // there's nothing live to update so we skip the catalog fetch entirely.
        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(-1).AddHours(16),
            GoalHours = 16,
            ProtocolId = DefaultProtocols[0].Id,
        };
        var (vm, _, _, protoRepo, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.Protocols.Should().BeEmpty();
        protoRepo.Verify(p => p.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_changes_protocol_when_picker_selection_differs_from_current()
    {
        var original = DefaultProtocols[0]; // 16:8
        var target = DefaultProtocols[2];   // OMAD
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-3), EndUtc = null, GoalHours = 16, ProtocolId = original.Id };
        var (vm, fasting, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();

        vm.SelectedProtocol = vm.Protocols.First(p => p.Id == target.Id);
        await vm.SaveCommand.ExecuteAsync(null);

        fasting.Verify(s => s.ChangeProtocolAsync(fast.Id, target.Id), Times.Once);
    }

    [Fact]
    public async Task SaveAsync_does_not_call_change_protocol_when_selection_unchanged()
    {
        var current = DefaultProtocols[1];
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-3), EndUtc = null, GoalHours = 18, ProtocolId = current.Id };
        var (vm, fasting, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();

        await vm.SaveCommand.ExecuteAsync(null);

        fasting.Verify(s => s.ChangeProtocolAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task SaveAsync_on_past_fast_never_calls_change_protocol()
    {
        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddDays(-1),
            EndUtc = DateTime.UtcNow.AddDays(-1).AddHours(16),
            GoalHours = 16,
            ProtocolId = DefaultProtocols[0].Id,
        };
        var (vm, fasting, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();
        await vm.LoadAsync();

        // Even if SelectedProtocol were set (it isn't, since the picker isn't populated),
        // the protocol-swap branch is gated on IsActiveFast.
        await vm.SaveCommand.ExecuteAsync(null);

        fasting.Verify(s => s.ChangeProtocolAsync(It.IsAny<Guid>(), It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CancelAsync_navigates_back()
    {
        var (vm, _, _, _, _, nav) = Build();
        await vm.CancelCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
