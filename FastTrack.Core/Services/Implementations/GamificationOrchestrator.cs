using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class GamificationOrchestrator : IGamificationOrchestrator
{
    private readonly IFastingService _fasting;
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly IUserProfileRepository _profiles;
    private readonly IStreakService _streaks;
    private readonly IXpService _xp;
    private readonly IBadgeService _badges;
    private readonly IQuestService _quests;

    private bool _started;

    public event EventHandler<GamificationResult>? RewardsGranted;

    public GamificationOrchestrator(
        IFastingService fasting,
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        IUserProfileRepository profiles,
        IStreakService streaks,
        IXpService xp,
        IBadgeService badges,
        IQuestService quests)
    {
        _fasting = fasting;
        _fasts = fasts;
        _protocols = protocols;
        _profiles = profiles;
        _streaks = streaks;
        _xp = xp;
        _badges = badges;
        _quests = quests;
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        _fasting.FastCompleted += OnFastCompleted;
    }

    private async void OnFastCompleted(object? sender, Fast fast)
    {
        try { await ProcessAsync(fast); }
        catch { /* gamification must never crash the fasting flow */ }
    }

    private async Task ProcessAsync(Fast fast)
    {
        var protocol = await _protocols.GetByIdAsync(fast.ProtocolId);
        if (protocol is null) return;

        // Snapshot pre-state.
        var beforeProfile = await _profiles.GetOrCreateAsync();
        var beforeLevel = Level.ForXp(beforeProfile.TotalXp);
        var comebackPending = beforeProfile.ComebackBonusPending;

        // 1. Streak (may set ComebackBonusPending on break).
        var streak = await _streaks.RecordCompletedFastAsync(fast, protocol);

        // 2. Bump fasts-completed counter + consume comeback flag if it was pending coming IN.
        var profile = await _profiles.GetOrCreateAsync();
        profile.CompletedFastsCount += 1;
        if (comebackPending) profile.ComebackBonusPending = false;
        await _profiles.UpdateAsync(profile);

        // 3. XP (comebackPending was the state when this fast STARTED; the break that just happened doesn't count toward itself).
        var award = _xp.CalculateForFast(fast, protocol, streak.Current, comebackPending);
        var xpState = await _xp.AwardAsync(award.Total);
        var leveledUp = xpState.Level.Number > beforeLevel.Number;

        // 4. Badges (sees updated state).
        var newBadges = await _badges.EvaluateOnFastCompletedAsync(fast, protocol);

        // 5. Daily quests.
        var history = await _fasts.GetHistoryAsync(int.MaxValue);
        var questUpdates = await _quests.OnFastCompletedAsync(fast, protocol, history);

        // 6. If any quest paid out XP, refresh the state so the UI reads the latest total.
        if (questUpdates.Any(u => u.NewlyCompleted))
        {
            xpState = await _xp.GetStateAsync();
            // A quest could have triggered another level-up too.
            leveledUp = xpState.Level.Number > beforeLevel.Number;
        }

        RewardsGranted?.Invoke(this, new GamificationResult(
            streak,
            award,
            xpState,
            leveledUp,
            leveledUp ? beforeLevel : null,
            newBadges,
            questUpdates));
    }
}
