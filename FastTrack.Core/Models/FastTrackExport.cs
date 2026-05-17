namespace FastTrack.Models;

/// <summary>
/// Root JSON shape for the data-export file (US-10.4 / 10.5).
/// Document format owned by /docs/data-format.md.
/// </summary>
public sealed class FastTrackExport
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTime ExportedAtUtc { get; set; }
    public string AppVersion { get; set; } = "0.1.0";

    public UserProfile? Profile { get; set; }
    public List<FastingProtocol> Protocols { get; set; } = new();
    public List<Fast> Fasts { get; set; } = new();
    public List<WeightEntry> Weights { get; set; } = new();
    public List<MoodEntry> Moods { get; set; } = new();
    public List<WaterEntry> Water { get; set; } = new();
    public List<EarnedBadge> EarnedBadges { get; set; } = new();
    public List<DailyQuest> DailyQuests { get; set; } = new();
}
