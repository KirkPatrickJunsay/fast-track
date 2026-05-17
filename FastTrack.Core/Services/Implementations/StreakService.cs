using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Streak rules per US-02.1 / 02.2:
///  - A day counts when the completed fast hits ≥80% of its goal.
///  - Calendar gaps consume one streak freeze if available; otherwise the streak resets.
///  - Earn 1 freeze every 7-day milestone, capped at 3.
/// </summary>
public sealed class StreakService : IStreakService
{
    private const double DayCountThreshold = 0.80;
    private const int MaxFreezes = 3;
    private const int FreezeMilestoneDays = 7;

    private readonly IUserProfileRepository _profiles;

    public StreakService(IUserProfileRepository profiles) => _profiles = profiles;

    public async Task<StreakSnapshot> GetSnapshotAsync()
    {
        var p = await _profiles.GetOrCreateAsync();
        return new StreakSnapshot(p.CurrentStreak, p.LongestStreak, p.StreakFreezesAvailable);
    }

    public async Task<StreakResult> RecordCompletedFastAsync(Fast completed, FastingProtocol protocol)
    {
        var profile = await _profiles.GetOrCreateAsync();

        // Does this fast meet the ≥80% threshold to qualify the day?
        var elapsed = (completed.EndUtc ?? DateTime.UtcNow) - completed.StartUtc;
        var qualified = completed.GoalHours <= 0
            || elapsed.TotalHours >= (DayCountThreshold * completed.GoalHours);

        var today = (completed.EndUtc ?? DateTime.UtcNow).Date;
        var last = profile.LastStreakDayUtc?.Date;

        var freezeConsumed = false;
        var broken = false;
        var incrementedToday = false;

        if (!qualified)
        {
            // Didn't qualify — streak unchanged but record no progression.
            return new StreakResult(profile.CurrentStreak, profile.LongestStreak, false, false, false);
        }

        if (last == today)
        {
            // Already credited today; idempotent.
            return new StreakResult(profile.CurrentStreak, profile.LongestStreak, false, false, false);
        }

        if (last is null)
        {
            // First-ever qualifying day.
            profile.CurrentStreak = 1;
            incrementedToday = true;
        }
        else
        {
            var gap = (today - last.Value).Days;
            if (gap == 1)
            {
                profile.CurrentStreak += 1;
                incrementedToday = true;
            }
            else if (gap > 1)
            {
                // Try to consume a freeze for each missed day, max one fast can't recover all gaps —
                // simplification: a single freeze covers the entire gap when gap == 2 (one missed day).
                if (gap == 2 && profile.StreakFreezesAvailable > 0)
                {
                    profile.StreakFreezesAvailable -= 1;
                    profile.CurrentStreak += 1;
                    freezeConsumed = true;
                    incrementedToday = true;
                }
                else
                {
                    profile.CurrentStreak = 1;
                    broken = true;
                    incrementedToday = true;
                    // Comeback bonus applies to the NEXT fast after this break,
                    // not the one that just broke it.
                    profile.ComebackBonusPending = true;
                }
            }
            else
            {
                // gap == 0 handled above; negative shouldn't happen but guard anyway.
                profile.CurrentStreak = Math.Max(profile.CurrentStreak, 1);
                incrementedToday = true;
            }
        }

        profile.LastStreakDayUtc = today;
        if (profile.CurrentStreak > profile.LongestStreak)
            profile.LongestStreak = profile.CurrentStreak;

        // Award a freeze on every 7-day milestone.
        if (profile.CurrentStreak > 0 && profile.CurrentStreak % FreezeMilestoneDays == 0)
        {
            profile.StreakFreezesAvailable = Math.Min(MaxFreezes, profile.StreakFreezesAvailable + 1);
        }

        await _profiles.UpdateAsync(profile);
        return new StreakResult(profile.CurrentStreak, profile.LongestStreak, incrementedToday, freezeConsumed, broken);
    }
}
