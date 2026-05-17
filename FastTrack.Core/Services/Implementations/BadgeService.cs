using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class BadgeService : IBadgeService
{
    private readonly IEarnedBadgeRepository _earned;
    private readonly IFastRepository _fasts;
    private readonly IUserProfileRepository _profiles;
    private readonly IFastingProtocolRepository _protocols;

    // 15 MVP milestone badges. Hidden + Epic-03-dependent ones marked accordingly.
    private static readonly IReadOnlyList<BadgeDefinition> _definitions = new[]
    {
        new BadgeDefinition("first_fast",     "First Fast",          "Complete your first fast.",                        "badge_first_fast.svg"),
        new BadgeDefinition("day_one",        "Day One",             "Complete a 24-hour fast.",                          "badge_day_one.svg"),
        new BadgeDefinition("week_warrior",   "Week Warrior",        "Maintain a 7-day streak.",                          "badge_week_warrior.svg"),
        new BadgeDefinition("month_master",   "Month Master",        "Maintain a 30-day streak.",                         "badge_month_master.svg"),
        new BadgeDefinition("century",        "Century",             "Complete 100 fasts.",                               "badge_century.svg"),
        new BadgeDefinition("marathon",       "Marathon",            "Complete a single fast of at least 48 hours.",      "badge_marathon.svg"),
        new BadgeDefinition("iron_will",      "Iron Will",           "Exceed your fasting goal 10 times.",                "badge_iron_will.svg"),
        new BadgeDefinition("early_bird",     "Early Bird",          "Start 5 fasts before 6 AM.",                        "badge_early_bird.svg"),
        new BadgeDefinition("night_owl",      "Night Owl",           "End 5 fasts after midnight.",                       "badge_night_owl.svg"),
        new BadgeDefinition("hydration_hero", "Hydration Hero",      "Hit your water goal 10 days. (Epic 03)",            "badge_hydration_hero.svg"),
        new BadgeDefinition("mood_tracker",   "Mood Tracker",        "Log mood 30 times. (Epic 03)",                       "badge_mood_tracker.svg"),
        new BadgeDefinition("comeback_kid",   "Comeback Kid",        "Return after a 7+ day gap and complete a fast.",    "badge_comeback_kid.svg"),
        new BadgeDefinition("protocol_explorer","Protocol Explorer", "Try 5 different protocols.",                        "badge_protocol_explorer.svg"),
        new BadgeDefinition("autophagy_achieved","Autophagy Achieved","Complete a 24-hour+ fast 10 times.",               "badge_autophagy.svg"),
        new BadgeDefinition("level_five",     "Level 5 — Monk",      "Reach the Monk level.",                              "badge_level_five.svg"),

        // Hidden badges — criteria intentionally not surfaced in-app until earned.
        new BadgeDefinition("weekend_warrior",   "Weekend Warrior",   "Complete fasts on both Saturday and Sunday in the same week.", "badge_hidden.svg", IsHidden: true),
        new BadgeDefinition("disciplined_soul",  "Disciplined Soul",  "Complete 20 fasts in a row without ending early.",             "badge_hidden.svg", IsHidden: true),
        new BadgeDefinition("triple_crown",      "Triple Crown",      "Reach ketosis (18h+) three times within a 7-day window.",       "badge_hidden.svg", IsHidden: true),
        new BadgeDefinition("cycle_breaker",     "Cycle Breaker",     "Return after a 14+ day gap and complete a fast.",               "badge_hidden.svg", IsHidden: true),
        new BadgeDefinition("trophy_hunter",     "Trophy Hunter",     "Earn 10 badges.",                                                "badge_hidden.svg", IsHidden: true),
    };

    public BadgeService(
        IEarnedBadgeRepository earned,
        IFastRepository fasts,
        IUserProfileRepository profiles,
        IFastingProtocolRepository protocols)
    {
        _earned = earned;
        _fasts = fasts;
        _profiles = profiles;
        _protocols = protocols;
    }

    public IReadOnlyList<BadgeDefinition> Definitions => _definitions;

    public async Task<IReadOnlyList<EvaluatedBadge>> GetAllAsync()
    {
        var earned = await _earned.GetAllAsync();
        var byKey = earned.ToDictionary(e => e.BadgeKey, e => e.EarnedAtUtc);
        return _definitions
            .Select(d => new EvaluatedBadge(d, byKey.ContainsKey(d.Key), byKey.TryGetValue(d.Key, out var t) ? t : null))
            .ToList();
    }

    public async Task<IReadOnlyList<BadgeDefinition>> EvaluateOnFastCompletedAsync(Fast fast, FastingProtocol protocol)
    {
        var history = await _fasts.GetHistoryAsync(int.MaxValue);
        var profile = await _profiles.GetOrCreateAsync();
        var newlyEarned = new List<BadgeDefinition>();

        var elapsed = (fast.EndUtc ?? DateTime.UtcNow) - fast.StartUtc;
        var hoursElapsed = elapsed.TotalHours;
        var goalExceededCount = history.Count(f => f.EndUtc is not null && (f.EndUtc.Value - f.StartUtc).TotalHours > f.GoalHours);
        var fastsAtLeast24h = history.Count(f => f.EndUtc is not null && (f.EndUtc.Value - f.StartUtc).TotalHours >= 24);
        var earlyStarts = history.Count(f => f.StartUtc.ToLocalTime().Hour < 6);
        var lateEnds = history.Count(f => f.EndUtc is not null && f.EndUtc.Value.ToLocalTime().Hour < 6);
        var distinctProtocols = history.Select(f => f.ProtocolId).Distinct().Count();

        // gap detection for Comeback Kid: a previous fast more than 7 days before this one ended.
        var hadComebackGap = history
            .Where(f => f.EndUtc.HasValue && f.Id != fast.Id)
            .OrderByDescending(f => f.EndUtc!.Value)
            .Take(2)
            .Skip(0)
            .Any(prev => (fast.StartUtc - prev.EndUtc!.Value).TotalDays >= 7);

        var level = Level.ForXp(profile.TotalXp);

        await TryAward("first_fast", profile.CompletedFastsCount + 1 >= 1, newlyEarned);
        await TryAward("day_one", hoursElapsed >= 24, newlyEarned);
        await TryAward("week_warrior", profile.CurrentStreak >= 7, newlyEarned);
        await TryAward("month_master", profile.CurrentStreak >= 30, newlyEarned);
        await TryAward("century", (profile.CompletedFastsCount + 1) >= 100, newlyEarned);
        await TryAward("marathon", hoursElapsed >= 48, newlyEarned);
        await TryAward("iron_will", goalExceededCount >= 10, newlyEarned);
        await TryAward("early_bird", earlyStarts >= 5, newlyEarned);
        await TryAward("night_owl", lateEnds >= 5, newlyEarned);
        await TryAward("comeback_kid", hadComebackGap, newlyEarned);
        await TryAward("protocol_explorer", distinctProtocols >= 5, newlyEarned);
        await TryAward("autophagy_achieved", fastsAtLeast24h >= 10, newlyEarned);
        await TryAward("level_five", level.Number >= 5, newlyEarned);
        // hydration_hero + mood_tracker stay locked until Epic 03 logs exist.

        // HIDDEN BADGE EVALUATION
        var localEnd = (fast.EndUtc ?? DateTime.UtcNow).ToLocalTime();

        // Weekend Warrior: this fast ended Sat or Sun, AND another fast ended on the other weekend day in the same ISO week.
        if (localEnd.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            var wantedOther = localEnd.DayOfWeek == DayOfWeek.Saturday ? DayOfWeek.Sunday : DayOfWeek.Saturday;
            var weekHasOther = history.Any(f =>
                f.Id != fast.Id &&
                f.EndUtc.HasValue &&
                IsoWeekOf(f.EndUtc.Value.ToLocalTime()) == IsoWeekOf(localEnd) &&
                f.EndUtc.Value.ToLocalTime().DayOfWeek == wantedOther);
            await TryAward("weekend_warrior", weekHasOther, newlyEarned);
        }

        // Disciplined Soul: 20 consecutive completed-on-time fasts (most recent 20 in history).
        var recent20 = history
            .Where(f => f.EndUtc.HasValue)
            .OrderByDescending(f => f.EndUtc!.Value)
            .Take(20)
            .ToList();
        var disciplined = recent20.Count >= 20 && recent20.All(f => f.EndReason == FastEndReason.Completed);
        await TryAward("disciplined_soul", disciplined, newlyEarned);

        // Triple Crown: 3+ fasts of 18h+ within any rolling 7-day window ending at this fast.
        var windowStart = localEnd.AddDays(-7);
        var deepInWindow = history.Count(f =>
            f.EndUtc.HasValue &&
            f.EndUtc.Value.ToLocalTime() >= windowStart &&
            (f.EndUtc.Value - f.StartUtc).TotalHours >= 18);
        await TryAward("triple_crown", deepInWindow >= 3, newlyEarned);

        // Cycle Breaker: previous fast ended 14+ days before this one started.
        var previous = history
            .Where(f => f.Id != fast.Id && f.EndUtc.HasValue)
            .OrderByDescending(f => f.EndUtc!.Value)
            .FirstOrDefault();
        if (previous is not null && (fast.StartUtc - previous.EndUtc!.Value).TotalDays >= 14)
        {
            await TryAward("cycle_breaker", true, newlyEarned);
        }

        // Trophy Hunter: 10 total earned badges (count includes ones earned this evaluation).
        var earnedCountAfter = (await _earned.GetAllAsync()).Count;
        await TryAward("trophy_hunter", earnedCountAfter >= 10, newlyEarned);

        return newlyEarned;
    }

    private static int IsoWeekOf(DateTime dt) =>
        System.Globalization.ISOWeek.GetWeekOfYear(dt);

    private async Task TryAward(string key, bool criterion, List<BadgeDefinition> earnedSink)
    {
        if (!criterion) return;
        if (await _earned.HasAsync(key)) return;

        var def = _definitions.FirstOrDefault(d => d.Key == key);
        if (def is null) return;

        await _earned.UpsertAsync(new EarnedBadge
        {
            BadgeKey = key,
            EarnedAtUtc = DateTime.UtcNow,
        });
        earnedSink.Add(def);
    }
}
