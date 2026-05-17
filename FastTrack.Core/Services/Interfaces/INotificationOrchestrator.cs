using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface INotificationOrchestrator
{
    Task ScheduleForFastAsync(Fast fast, FastingProtocol protocol);
    Task CancelForFastAsync(Guid fastId);

    /// <summary>Pre-schedules "we miss you" reminders at +3 / +7 / +14 days from now.</summary>
    Task RescheduleReengagementAsync();

    /// <summary>Cancels any pending re-engagement reminders — called when the user opens the app.</summary>
    Task CancelReengagementAsync();
}
