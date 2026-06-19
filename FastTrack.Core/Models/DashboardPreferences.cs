using System.Text.Json;

namespace FastTrack.Models;

/// <summary>
/// Which Home cards the user has chosen to show. Persisted as JSON in
/// <see cref="UserProfile.DashboardWidgetsJson"/>. Defaults are "show everything"
/// so an empty/legacy profile sees the original Home page.
///
/// Always-on chrome (active-fast hero, action buttons, error banner,
/// educational-mode notice) is not toggleable — those are essential signal.
/// </summary>
public sealed record DashboardPreferences
{
    public bool ShowGamification { get; init; } = true;
    public bool ShowDailyHealth { get; init; } = true;
    public bool ShowQuests { get; init; } = true;
    public bool ShowProgressCards { get; init; } = true;
    public bool ShowStagesRoadmap { get; init; } = true;

    public static DashboardPreferences Default => new();

    public static DashboardPreferences FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Default;
        try
        {
            var parsed = JsonSerializer.Deserialize<DashboardPreferences>(json);
            return parsed ?? Default;
        }
        catch
        {
            // Corrupt or unknown-shape JSON falls back to default rather than crashing the page.
            return Default;
        }
    }

    public string ToJson() => JsonSerializer.Serialize(this);
}
