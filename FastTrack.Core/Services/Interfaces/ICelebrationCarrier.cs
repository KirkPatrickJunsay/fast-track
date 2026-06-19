using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record CelebrationData(
    bool GoalMet,
    TimeSpan Duration,
    string StageName,
    int XpEarned,
    bool ComebackBonus,
    bool GoalExceededBonus,
    int CurrentStreak,
    bool StreakIncremented,
    bool FreezeConsumed,
    bool LevelledUp,
    Level? PreviousLevel,
    Level? NewLevel,
    IReadOnlyList<BadgeDefinition> NewBadges,
    IReadOnlyList<string> ClaimedQuests);

/// <summary>
/// In-memory hand-off between HomeViewModel and CelebrationPage. Replaces a noisy alert dialog
/// with a full-screen moment, while keeping the navigation contract simple.
/// </summary>
public interface ICelebrationCarrier
{
    void Set(CelebrationData data);
    CelebrationData? Take();
}

public sealed class CelebrationCarrier : ICelebrationCarrier
{
    private CelebrationData? _pending;
    public void Set(CelebrationData data) => _pending = data;
    public CelebrationData? Take()
    {
        var d = _pending;
        _pending = null;
        return d;
    }
}
