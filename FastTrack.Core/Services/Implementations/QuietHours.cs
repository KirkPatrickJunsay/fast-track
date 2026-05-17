using FastTrack.Models;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Shifts notification delivery times to respect quiet hours (US-07.3).
/// </summary>
public static class QuietHours
{
    /// <summary>
    /// If <paramref name="deliverLocal"/> falls inside the configured quiet window,
    /// returns the next instant after the window ends. Otherwise returns it unchanged.
    /// </summary>
    public static DateTime Adjust(DateTime deliverLocal, NotificationPreferences prefs)
    {
        if (!prefs.QuietHoursEnabled) return deliverLocal;

        var start = prefs.QuietHoursStart;
        var end = prefs.QuietHoursEnd;

        // Inactive window (start == end) — treat as disabled.
        if (start == end) return deliverLocal;

        var tod = deliverLocal.TimeOfDay;

        bool insideWindow;
        DateTime endOfWindow;

        if (start < end)
        {
            // Same-day window, e.g. 13:00 → 14:00.
            insideWindow = tod >= start && tod < end;
            endOfWindow = deliverLocal.Date + end;
        }
        else
        {
            // Overnight window, e.g. 22:00 → 07:00.
            insideWindow = tod >= start || tod < end;
            // If currently past midnight (tod < end) the window ends today; otherwise tomorrow.
            endOfWindow = (tod < end)
                ? deliverLocal.Date + end
                : deliverLocal.Date.AddDays(1) + end;
        }

        return insideWindow ? endOfWindow : deliverLocal;
    }
}
