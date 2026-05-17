using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class EditFastViewModelTests
{
    private static (EditFastViewModel Vm,
                    Mock<IFastingService> Fasting,
                    Mock<IFastRepository> Fasts,
                    Mock<IDialogService> Dialogs,
                    Mock<INavigationService> Nav)
        Build(Fast? fast = null)
    {
        var fasts = new Mock<IFastRepository>();
        if (fast is not null)
            fasts.Setup(r => r.GetByIdAsync(fast.Id)).ReturnsAsync(fast);
        var fasting = new Mock<IFastingService>();
        fasting.Setup(s => s.EditTimesAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime?>()))
               .ReturnsAsync((Guid id, DateTime s, DateTime? e) => new Fast { Id = id, StartUtc = s, EndUtc = e });
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        return (new EditFastViewModel(fasting.Object, fasts.Object, dialogs.Object, nav.Object),
                fasting, fasts, dialogs, nav);
    }

    [Fact]
    public async Task LoadAsync_with_invalid_id_sets_error()
    {
        var (vm, _, _, _, _) = Build();
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
        var dialogs = new Mock<IDialogService>();
        var nav = new Mock<INavigationService>();
        var vm = new EditFastViewModel(fasting.Object, fasts.Object, dialogs.Object, nav.Object)
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
        var (vm, _, _, _, _) = Build(fast);
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
        var (vm, _, _, _, _) = Build(fast);
        vm.FastId = fast.Id.ToString();

        await vm.LoadAsync();

        vm.IsPastFast.Should().BeTrue();
        vm.EndDate.Should().Be(endUtc.ToLocalTime().Date);
    }

    [Fact]
    public async Task SaveAsync_calls_service_with_utc_times_and_navigates_back()
    {
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-3), EndUtc = null, GoalHours = 16 };
        var (vm, fasting, _, dialogs, nav) = Build(fast);
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
        var (vm, fasting, _, _, nav) = Build(fast);
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
    public async Task CancelAsync_navigates_back()
    {
        var (vm, _, _, _, nav) = Build();
        await vm.CancelCommand.ExecuteAsync(null);
        nav.Verify(n => n.GoBackAsync(), Times.Once);
    }
}
