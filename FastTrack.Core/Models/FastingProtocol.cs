using SQLite;

namespace FastTrack.Models;

[Table("FastingProtocols")]
public class FastingProtocol
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int FastHours { get; set; }

    public int EatHours { get; set; }

    public Difficulty Difficulty { get; set; }

    public bool IsCustom { get; set; }

    public bool IsPreset { get; set; }

    public string? Description { get; set; }
}
