using SQLite;

namespace FastTrack.Models;

[Table("EarnedBadges")]
public class EarnedBadge
{
    /// <summary>Stable string key matching a <see cref="BadgeDefinition"/>.</summary>
    [PrimaryKey]
    public string BadgeKey { get; set; } = string.Empty;

    public DateTime EarnedAtUtc { get; set; }
}
