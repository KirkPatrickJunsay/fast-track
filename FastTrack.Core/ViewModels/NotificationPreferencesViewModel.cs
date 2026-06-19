using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class NotificationPreferencesViewModel : ObservableObject
{
    private const int TestNotificationId = 7777;

    private readonly IUserProfileRepository _profiles;
    private readonly IDialogService _dialogs;
    private readonly IFastTrackNotificationService _notifications;

    [ObservableProperty] private bool fastLifecycle = true;
    [ObservableProperty] private bool stageTransitions = true;
    [ObservableProperty] private bool eatingWindow = true;
    [ObservableProperty] private bool streakProtection = true;
    [ObservableProperty] private bool questReminders = true;

    [ObservableProperty] private bool quietHoursEnabled = true;
    [ObservableProperty] private TimeSpan quietHoursStart = new(22, 0, 0);
    [ObservableProperty] private TimeSpan quietHoursEnd = new(7, 0, 0);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? statusMessage;

    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    public NotificationPreferencesViewModel(
        IUserProfileRepository profiles,
        IDialogService dialogs,
        IFastTrackNotificationService notifications)
    {
        _profiles = profiles;
        _dialogs = dialogs;
        _notifications = notifications;
    }

    public async Task LoadAsync()
    {
        StatusMessage = null;
        var profile = await _profiles.GetOrCreateAsync();
        var prefs = Deserialize(profile.NotificationPrefsJson) ?? NotificationPreferences.Default();

        FastLifecycle = prefs.FastLifecycleEnabled;
        StageTransitions = prefs.StageTransitionEnabled;
        EatingWindow = prefs.EatingWindowEnabled;
        StreakProtection = prefs.StreakProtectionEnabled;
        QuestReminders = prefs.QuestRemindersEnabled;
        QuietHoursEnabled = prefs.QuietHoursEnabled;
        QuietHoursStart = prefs.QuietHoursStart;
        QuietHoursEnd = prefs.QuietHoursEnd;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var prefs = new NotificationPreferences
        {
            FastLifecycleEnabled = FastLifecycle,
            StageTransitionEnabled = StageTransitions,
            EatingWindowEnabled = EatingWindow,
            StreakProtectionEnabled = StreakProtection,
            QuestRemindersEnabled = QuestReminders,
            QuietHoursEnabled = QuietHoursEnabled,
            QuietHoursStart = QuietHoursStart,
            QuietHoursEnd = QuietHoursEnd,
        };

        var profile = await _profiles.GetOrCreateAsync();
        profile.NotificationPrefsJson = JsonSerializer.Serialize(prefs);
        await _profiles.UpdateAsync(profile);

        StatusMessage = "Saved.";
    }

    [RelayCommand]
    private async Task SendTestAsync()
    {
        if (!await _notifications.IsPermittedAsync())
        {
            var granted = await _notifications.RequestPermissionAsync();
            if (!granted)
            {
                await _dialogs.ShowAlertAsync(
                    "Permission needed",
                    "Allow notifications in system settings to receive reminders.");
                return;
            }
        }

        await _notifications.ScheduleAsync(new ScheduledNotification
        {
            Id = TestNotificationId,
            Title = "Test notification",
            Body = "If you see this, Fast Track notifications are working.",
            DeliverAtLocal = DateTime.Now.AddSeconds(5),
        });

        StatusMessage = "Test notification scheduled — should arrive in ~5 seconds.";
    }

    private static NotificationPreferences? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<NotificationPreferences>(json);
        }
        catch
        {
            return null;
        }
    }
}
