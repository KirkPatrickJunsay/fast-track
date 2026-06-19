using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

/// <summary>
/// Read/write wrapper over the DashboardWidgetsJson field on UserProfile.
/// Encapsulates the JSON round-trip so callers don't need to know the storage shape.
/// </summary>
public interface IDashboardPreferencesService
{
    Task<DashboardPreferences> GetAsync();
    Task SaveAsync(DashboardPreferences prefs);
}
