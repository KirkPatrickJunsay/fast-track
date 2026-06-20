namespace FastTrack.Services.Interfaces;

/// <summary>
/// Drives the platform's live "ongoing fast" indicator — the persistent
/// notification on Android (foreground service + chronometer) and Live
/// Activities on iOS. Implementations stay platform-specific; Core only
/// knows the start/stop contract.
///
/// Failures here must never propagate: a missing notification permission
/// or a denied foreground-service grant cannot block the user from starting
/// a fast. FastingService will swallow exceptions from these calls.
/// </summary>
public interface IFastingTickerService
{
    /// <summary>
    /// Start (or refresh) the platform indicator. Idempotent — calling
    /// twice with the same parameters is a no-op; calling with a different
    /// start time updates the chronometer reference (used after EditTimes).
    /// </summary>
    Task StartAsync(string title, DateTime startUtc, string? subtitle = null);

    /// <summary>Tear down the indicator. Safe to call when nothing is running.</summary>
    Task StopAsync();
}
