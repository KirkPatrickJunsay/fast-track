using SQLite;

namespace FastTrack.Models;

[Table("WeightEntries")]
public class WeightEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    /// <summary>Always stored in kilograms; UI converts for lb display.</summary>
    public double WeightKg { get; set; }

    public string? Note { get; set; }

    /// <summary>"Manual" or "HealthKit" / "HealthConnect" when sourced from a platform store.</summary>
    public string Source { get; set; } = "Manual";
}
