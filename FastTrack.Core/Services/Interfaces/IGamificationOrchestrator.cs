using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record GamificationResult(
    StreakResult Streak,
    XpAward Xp,
    XpState XpState,
    bool LevelledUp,
    Level? PreviousLevel,
    IReadOnlyList<BadgeDefinition> NewBadges,
    IReadOnlyList<QuestUpdate> QuestUpdates);

public interface IGamificationOrchestrator
{
    /// <summary>Begin listening to FastingService events. Idempotent.</summary>
    void Start();

    event EventHandler<GamificationResult>? RewardsGranted;
}
