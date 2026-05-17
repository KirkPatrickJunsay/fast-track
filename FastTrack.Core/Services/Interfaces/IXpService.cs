using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record XpAward(
    int Base,
    double DifficultyMultiplier,
    bool GoalExceededBonus,
    double StreakMultiplier,
    bool ComebackBonus,
    int Total);

public sealed record XpState(int TotalXp, Level Level, Level? NextLevel, int XpIntoLevel, int XpForNextLevel);

public interface IXpService
{
    XpAward CalculateForFast(Fast completed, FastingProtocol protocol, int currentStreak, bool comebackBonus);

    Task<XpState> GetStateAsync();

    Task<XpState> AwardAsync(int xp);
}
