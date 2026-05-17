using SQLite;

namespace FastTrack.Models;

[Table("Fasts")]
public class Fast
{
    [PrimaryKey]
    public Guid Id { get; set; }

    [Indexed]
    public DateTime StartUtc { get; set; }

    public DateTime? EndUtc { get; set; }

    [Indexed]
    public Guid ProtocolId { get; set; }

    public int GoalHours { get; set; }

    public FastEndReason? EndReason { get; set; }

    public string? Notes { get; set; }

    public DateTime? OriginalStartUtc { get; set; }

    public DateTime? OriginalEndUtc { get; set; }

    [Ignore]
    public bool IsActive => EndUtc is null;
}
