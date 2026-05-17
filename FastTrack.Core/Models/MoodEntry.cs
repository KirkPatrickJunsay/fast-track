using SQLite;

namespace FastTrack.Models;

[Table("MoodEntries")]
public class MoodEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    /// <summary>1–5 corresponding to 😞 😐 🙂 😊 🤩.</summary>
    public int MoodLevel { get; set; }

    public string? Note { get; set; }

    /// <summary>Optional linkage to the fast this mood was logged during.</summary>
    public Guid? FastId { get; set; }

    public string Source { get; set; } = "Manual";
}
