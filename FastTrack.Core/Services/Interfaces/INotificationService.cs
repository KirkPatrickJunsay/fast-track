namespace FastTrack.Services.Interfaces;

public sealed class ScheduledNotification
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Body { get; init; } = string.Empty;
    public DateTime DeliverAtLocal { get; init; }
    public string? Group { get; init; }
}

public interface IFastTrackNotificationService
{
    Task<bool> RequestPermissionAsync();
    Task<bool> IsPermittedAsync();
    Task ScheduleAsync(ScheduledNotification request);
    Task CancelAsync(int id);
    Task CancelAllAsync();
    Task<IReadOnlyList<int>> GetPendingIdsAsync();
}
