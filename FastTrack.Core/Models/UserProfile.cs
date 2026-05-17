using SQLite;

namespace FastTrack.Models;

[Table("UserProfile")]
public class UserProfile
{
    [PrimaryKey]
    public Guid Id { get; set; }

    public string? DisplayName { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ExperienceLevel Level { get; set; }

    public bool OnboardingCompleted { get; set; }

    public Guid? LastUsedProtocolId { get; set; }

    /// <summary>Set true when the medical safety screening flags any contraindication.</summary>
    public bool IsEducationalMode { get; set; }

    // Gamification state (Epic 02)
    public int TotalXp { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastStreakDayUtc { get; set; }
    public int StreakFreezesAvailable { get; set; }
    public int CompletedFastsCount { get; set; }

    /// <summary>Set true when streak broke and consumes (with +50% XP) on the next qualifying fast.</summary>
    public bool ComebackBonusPending { get; set; }

    // Serialized blobs for early scaffold — proper relational tables come in later migrations.
    public string? GoalsJson { get; set; }
    public string? MedicalScreeningJson { get; set; }
    public string? NotificationPrefsJson { get; set; }
    public string? AppearancePrefsJson { get; set; }
    public string? DashboardWidgetsJson { get; set; }
}
