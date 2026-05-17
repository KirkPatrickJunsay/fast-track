namespace FastTrack.Models;

/// <summary>
/// User preferences for local notifications (US-06.6 + Epic 07).
/// Persisted as JSON on UserProfile. Epic 07 reads these at scheduling time.
/// </summary>
public sealed class NotificationPreferences
{
    public bool FastLifecycleEnabled { get; set; } = true;
    public bool StageTransitionEnabled { get; set; } = true;
    public bool EatingWindowEnabled { get; set; } = true;
    public bool StreakProtectionEnabled { get; set; } = true;
    public bool QuestRemindersEnabled { get; set; } = true;

    /// <summary>Local time when quiet hours begin. Default 22:00.</summary>
    public TimeSpan QuietHoursStart { get; set; } = new(22, 0, 0);

    /// <summary>Local time when quiet hours end. Default 07:00.</summary>
    public TimeSpan QuietHoursEnd { get; set; } = new(7, 0, 0);

    public bool QuietHoursEnabled { get; set; } = true;

    public static NotificationPreferences Default() => new();
}
