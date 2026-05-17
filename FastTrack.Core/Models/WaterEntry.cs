using SQLite;

namespace FastTrack.Models;

[Table("WaterEntries")]
public class WaterEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime TimestampUtc { get; set; }

    public int AmountMl { get; set; }

    public string Source { get; set; } = "Manual";
}
