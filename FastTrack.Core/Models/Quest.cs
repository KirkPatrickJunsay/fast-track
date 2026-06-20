using SQLite;

namespace FastTrack.Models;

public enum QuestType
{
    CompleteAnyFast = 0,
    FastAtLeastHours = 1,
    BeatGoalByHours = 2,
    TryNewProtocol = 3,
    FinishBeforeNoon = 4,
    DontEndEarly = 5,
    /// <summary>End a fast after 6pm local — the opposite of FinishBeforeNoon.</summary>
    FinishAfter6pm = 6,
    /// <summary>Fast ended on Saturday or Sunday (local).</summary>
    WeekendFast = 7,
    /// <summary>This fast's start is within 24h of the previous completed fast's end (back-to-back consistency).</summary>
    BackToBackWithin24h = 8,
}

/// <summary>
/// Static template for a quest. Authored in code, bundled with the app.
/// </summary>
public sealed record QuestDefinition(
    string Key,
    string Title,
    string Description,
    QuestType Type,
    int Target,
    int XpReward);

/// <summary>
/// A quest active on a given local date. One row per quest per day.
/// </summary>
[Table("DailyQuests")]
public class DailyQuest
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>Local-date midnight (UTC). Stable identifier for the day.</summary>
    [Indexed]
    public DateTime LocalDateUtc { get; set; }

    public string QuestKey { get; set; } = string.Empty;
    public int Progress { get; set; }
    public int Target { get; set; }
    public int XpReward { get; set; }
    public bool IsClaimed { get; set; }
    public DateTime? ClaimedAtUtc { get; set; }
}
