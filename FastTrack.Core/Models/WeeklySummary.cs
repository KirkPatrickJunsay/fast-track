namespace FastTrack.Models;

public sealed record WeeklySummary(
    double HoursThisWeek,
    double HoursLastWeek,
    double DeltaHours,
    int FastsThisWeek,
    int CurrentStreak,
    int BadgesThisWeek,
    string Highlight);
