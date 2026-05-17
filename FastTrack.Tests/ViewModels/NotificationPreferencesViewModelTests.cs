using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class NotificationPreferencesViewModelTests
{
    private static (NotificationPreferencesViewModel Vm,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<IDialogService> Dialogs,
                    Mock<IFastTrackNotificationService> Notifications,
                    UserProfile Profile)
        Build(string? prefsJson = null)
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), NotificationPrefsJson = prefsJson };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        var dialogs = new Mock<IDialogService>();
        var notifications = new Mock<IFastTrackNotificationService>();
        var vm = new NotificationPreferencesViewModel(profiles.Object, dialogs.Object, notifications.Object);
        return (vm, profiles, dialogs, notifications, profile);
    }

    [Fact]
    public async Task LoadAsync_uses_defaults_when_no_saved_prefs()
    {
        var (vm, _, _, _, _) = Build(prefsJson: null);
        await vm.LoadAsync();

        vm.FastLifecycle.Should().BeTrue();
        vm.StageTransitions.Should().BeTrue();
        vm.EatingWindow.Should().BeTrue();
        vm.StreakProtection.Should().BeTrue();
        vm.QuestReminders.Should().BeTrue();
        vm.QuietHoursEnabled.Should().BeTrue();
        vm.QuietHoursStart.Should().Be(new TimeSpan(22, 0, 0));
        vm.QuietHoursEnd.Should().Be(new TimeSpan(7, 0, 0));
    }

    [Fact]
    public async Task LoadAsync_hydrates_from_json()
    {
        var prefs = new NotificationPreferences
        {
            FastLifecycleEnabled = false,
            StageTransitionEnabled = false,
            QuietHoursEnabled = false,
            QuietHoursStart = new TimeSpan(23, 30, 0),
            QuietHoursEnd = new TimeSpan(6, 30, 0),
        };
        var (vm, _, _, _, _) = Build(prefsJson: JsonSerializer.Serialize(prefs));
        await vm.LoadAsync();

        vm.FastLifecycle.Should().BeFalse();
        vm.StageTransitions.Should().BeFalse();
        vm.QuietHoursEnabled.Should().BeFalse();
        vm.QuietHoursStart.Should().Be(new TimeSpan(23, 30, 0));
    }

    [Fact]
    public async Task LoadAsync_falls_back_to_defaults_when_json_is_invalid()
    {
        var (vm, _, _, _, _) = Build(prefsJson: "{not json}");
        await vm.LoadAsync();
        vm.FastLifecycle.Should().BeTrue();
    }

    [Fact]
    public async Task SaveAsync_persists_current_state_as_json()
    {
        var (vm, profiles, _, _, profile) = Build();
        await vm.LoadAsync();
        vm.FastLifecycle = false;
        vm.QuietHoursStart = new TimeSpan(21, 15, 0);

        await vm.SaveCommand.ExecuteAsync(null);

        profile.NotificationPrefsJson.Should().NotBeNullOrWhiteSpace();
        var roundTrip = JsonSerializer.Deserialize<NotificationPreferences>(profile.NotificationPrefsJson!)!;
        roundTrip.FastLifecycleEnabled.Should().BeFalse();
        roundTrip.QuietHoursStart.Should().Be(new TimeSpan(21, 15, 0));
        vm.StatusMessage.Should().Be("Saved.");
        vm.HasStatus.Should().BeTrue();
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task SendTestAsync_requests_permission_when_not_yet_granted()
    {
        var (vm, _, dialogs, notifications, _) = Build();
        notifications.Setup(n => n.IsPermittedAsync()).ReturnsAsync(false);
        notifications.Setup(n => n.RequestPermissionAsync()).ReturnsAsync(true);

        await vm.SendTestCommand.ExecuteAsync(null);

        notifications.Verify(n => n.RequestPermissionAsync(), Times.Once);
        notifications.Verify(n => n.ScheduleAsync(It.IsAny<ScheduledNotification>()), Times.Once);
        dialogs.Verify(d => d.ShowAlertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        vm.StatusMessage.Should().Contain("Test notification");
    }

    [Fact]
    public async Task SendTestAsync_shows_dialog_when_permission_denied()
    {
        var (vm, _, dialogs, notifications, _) = Build();
        notifications.Setup(n => n.IsPermittedAsync()).ReturnsAsync(false);
        notifications.Setup(n => n.RequestPermissionAsync()).ReturnsAsync(false);

        await vm.SendTestCommand.ExecuteAsync(null);

        dialogs.Verify(d => d.ShowAlertAsync("Permission needed", It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        notifications.Verify(n => n.ScheduleAsync(It.IsAny<ScheduledNotification>()), Times.Never);
    }

    [Fact]
    public async Task SendTestAsync_schedules_directly_when_already_permitted()
    {
        var (vm, _, _, notifications, _) = Build();
        notifications.Setup(n => n.IsPermittedAsync()).ReturnsAsync(true);

        await vm.SendTestCommand.ExecuteAsync(null);

        notifications.Verify(n => n.RequestPermissionAsync(), Times.Never);
        notifications.Verify(n => n.ScheduleAsync(It.Is<ScheduledNotification>(sn =>
            sn.Title == "Test notification" && sn.Body.Contains("working"))), Times.Once);
    }
}
