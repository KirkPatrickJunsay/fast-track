using FastTrack.Services.Interfaces;
using Plugin.LocalNotification;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Cross-platform local notification service wrapping Plugin.LocalNotification.
/// All scheduling is on-device; nothing leaves the device.
/// </summary>
public sealed class NotificationService : IFastTrackNotificationService
{
    public Task<bool> RequestPermissionAsync()
        => LocalNotificationCenter.Current.RequestNotificationPermission();

    public Task<bool> IsPermittedAsync()
        => LocalNotificationCenter.Current.AreNotificationsEnabled();

    public async Task ScheduleAsync(ScheduledNotification req)
    {
        var notification = new NotificationRequest
        {
            NotificationId = req.Id,
            Title = req.Title,
            Description = req.Body,
            Group = req.Group,
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = req.DeliverAtLocal,
            },
        };
        await LocalNotificationCenter.Current.Show(notification);
    }

    public Task CancelAsync(int id)
    {
        LocalNotificationCenter.Current.Cancel(id);
        return Task.CompletedTask;
    }

    public Task CancelAllAsync()
    {
        LocalNotificationCenter.Current.CancelAll();
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<int>> GetPendingIdsAsync()
    {
        var pending = await LocalNotificationCenter.Current.GetPendingNotificationList();
        return pending.Select(n => n.NotificationId).ToList();
    }
}
