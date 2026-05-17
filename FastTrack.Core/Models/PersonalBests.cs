namespace FastTrack.Models;

public sealed record PersonalBests(
    TimeSpan LongestFast,
    int LongestStreak,
    int MostFastsInMonth,
    double HighestWeeklyHours,
    int TotalCompletedFasts);
