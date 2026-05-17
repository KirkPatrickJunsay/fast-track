using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

/// <summary>
/// XP rules per US-02.4:
///  base_xp = fast_hours * 10
///  difficulty multiplier: Beginner 1.0 / Intermediate 1.25 / Advanced 1.5
///  goal-exceeded bonus: +25% if elapsed > goal
///  streak multiplier: +1% per day of streak, capped at +50%
///  XP awarded only on completion; never decreases.
/// </summary>
public sealed class XpService : IXpService
{
    private readonly IUserProfileRepository _profiles;

    public XpService(IUserProfileRepository profiles) => _profiles = profiles;

    public XpAward CalculateForFast(Fast completed, FastingProtocol protocol, int currentStreak, bool comebackBonus)
    {
        if (completed.EndUtc is null) return new XpAward(0, 1.0, false, 1.0, false, 0);

        var elapsed = completed.EndUtc.Value - completed.StartUtc;
        var hours = Math.Max(0, elapsed.TotalHours);
        var baseXp = (int)Math.Round(hours * 10);

        var difficulty = protocol.Difficulty switch
        {
            Difficulty.Beginner => 1.0,
            Difficulty.Intermediate => 1.25,
            Difficulty.Advanced => 1.5,
            _ => 1.0,
        };

        var goalExceeded = hours > completed.GoalHours;
        var goalBonus = goalExceeded ? 1.25 : 1.0;

        var streakBonus = 1.0 + Math.Min(0.50, currentStreak * 0.01);
        var comeback = comebackBonus ? 1.5 : 1.0;

        var total = (int)Math.Round(baseXp * difficulty * goalBonus * streakBonus * comeback);

        return new XpAward(baseXp, difficulty, goalExceeded, streakBonus, comebackBonus, total);
    }

    public async Task<XpState> GetStateAsync()
    {
        var p = await _profiles.GetOrCreateAsync();
        return BuildState(p.TotalXp);
    }

    public async Task<XpState> AwardAsync(int xp)
    {
        if (xp <= 0) return await GetStateAsync();
        var profile = await _profiles.GetOrCreateAsync();
        profile.TotalXp += xp;
        await _profiles.UpdateAsync(profile);
        return BuildState(profile.TotalXp);
    }

    private static XpState BuildState(int totalXp)
    {
        var level = Level.ForXp(totalXp);
        var next = Level.Next(level);
        var xpIntoLevel = totalXp - level.XpThreshold;
        var xpForNext = next is null ? 0 : (next.XpThreshold - level.XpThreshold);
        return new XpState(totalXp, level, next, xpIntoLevel, xpForNext);
    }
}
