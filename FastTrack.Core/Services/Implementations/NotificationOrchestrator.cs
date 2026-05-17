using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class NotificationOrchestrator : INotificationOrchestrator
{
    private const int FastIdSlots = 100_000;
    private const int LifecycleHalfwayOffset = 0;
    private const int LifecycleGoalOffset = 1;
    private const int EatingWindowOpenOffset = 2;
    private const int StageBaseOffset = 100;          // +stage index
    private const int ReengageDay3Id = 9001;
    private const int ReengageDay7Id = 9002;
    private const int ReengageDay14Id = 9003;

    private readonly IFastTrackNotificationService _notifications;
    private readonly IStageCalculator _stages;
    private readonly IUserProfileRepository _profiles;

    public NotificationOrchestrator(
        IFastTrackNotificationService notifications,
        IStageCalculator stages,
        IUserProfileRepository profiles)
    {
        _notifications = notifications;
        _stages = stages;
        _profiles = profiles;
    }

    public async Task ScheduleForFastAsync(Fast fast, FastingProtocol protocol)
    {
        if (!await _notifications.IsPermittedAsync()) return;

        var prefs = await GetPreferencesAsync();
        var baseId = SlotForFast(fast.Id);
        var startLocal = fast.StartUtc.ToLocalTime();
        var goalLocal = startLocal.AddHours(fast.GoalHours);
        var eatingOpenLocal = goalLocal; // eating window opens at goal end
        var now = DateTime.Now;

        // FAST LIFECYCLE — halfway + goal
        if (prefs.FastLifecycleEnabled)
        {
            var halfway = startLocal.AddHours(fast.GoalHours / 2.0);
            await TrySchedule(baseId + LifecycleHalfwayOffset, halfway, prefs, now,
                "Halfway there",
                $"You've fasted {fast.GoalHours / 2} hours. Keep the momentum.",
                group: GroupForFast(fast.Id));

            await TrySchedule(baseId + LifecycleGoalOffset, goalLocal, prefs, now,
                "Goal reached 🎉",
                $"You've hit your {fast.GoalHours}-hour goal. Decide how to wrap up.",
                group: GroupForFast(fast.Id));
        }

        // EATING WINDOW OPENS
        if (prefs.EatingWindowEnabled && protocol.EatHours > 0)
        {
            await TrySchedule(baseId + EatingWindowOpenOffset, eatingOpenLocal, prefs, now,
                "Eating window open",
                $"You can break your fast. Window closes in {protocol.EatHours}h.",
                group: GroupForFast(fast.Id));
        }

        // STAGE TRANSITIONS — only those that fall before goal
        if (prefs.StageTransitionEnabled)
        {
            for (var i = 0; i < _stages.Stages.Count; i++)
            {
                var s = _stages.Stages[i];
                if (s.StartHour <= 0) continue;                 // Anabolic is the starting state
                if (s.StartHour > fast.GoalHours) continue;     // skip post-goal stages

                var stageLocal = startLocal.AddHours(s.StartHour);
                await TrySchedule(baseId + StageBaseOffset + i, stageLocal, prefs, now,
                    $"Entering {s.Name}",
                    s.Summary,
                    group: GroupForFast(fast.Id));
            }
        }
    }

    public async Task CancelForFastAsync(Guid fastId)
    {
        var baseId = SlotForFast(fastId);

        var ids = new List<int> { baseId + LifecycleHalfwayOffset, baseId + LifecycleGoalOffset, baseId + EatingWindowOpenOffset };
        for (var i = 0; i < _stages.Stages.Count; i++) ids.Add(baseId + StageBaseOffset + i);

        foreach (var id in ids) await _notifications.CancelAsync(id);
    }

    public async Task RescheduleReengagementAsync()
    {
        if (!await _notifications.IsPermittedAsync()) return;

        var prefs = await GetPreferencesAsync();
        var now = DateTime.Now;

        await TrySchedule(ReengageDay3Id, now.AddDays(3), prefs, now,
            "Ready for a fresh fast?",
            "Tap to start. Whatever streak you had is fine — today's a new start.");

        await TrySchedule(ReengageDay7Id, now.AddDays(7), prefs, now,
            "Your data is here when you're ready",
            "Take a peek at what's changed since you last fasted.");

        await TrySchedule(ReengageDay14Id, now.AddDays(14), prefs, now,
            "We miss you",
            "No pressure — fasting is a long game. We'll be here.");
    }

    public async Task CancelReengagementAsync()
    {
        await _notifications.CancelAsync(ReengageDay3Id);
        await _notifications.CancelAsync(ReengageDay7Id);
        await _notifications.CancelAsync(ReengageDay14Id);
    }

    private async Task TrySchedule(int id, DateTime deliverLocal, NotificationPreferences prefs, DateTime now,
        string title, string body, string? group = null)
    {
        var adjusted = QuietHours.Adjust(deliverLocal, prefs);
        if (adjusted <= now.AddSeconds(5)) return; // skip if it's already in the past or imminent

        await _notifications.ScheduleAsync(new ScheduledNotification
        {
            Id = id,
            Title = title,
            Body = body,
            DeliverAtLocal = adjusted,
            Group = group,
        });
    }

    private async Task<NotificationPreferences> GetPreferencesAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();
        if (string.IsNullOrWhiteSpace(profile.NotificationPrefsJson))
            return NotificationPreferences.Default();
        try
        {
            return JsonSerializer.Deserialize<NotificationPreferences>(profile.NotificationPrefsJson)
                ?? NotificationPreferences.Default();
        }
        catch
        {
            return NotificationPreferences.Default();
        }
    }

    /// <summary>Deterministic 5-digit slot per fast id, so cancellation lines up reliably.</summary>
    private static int SlotForFast(Guid fastId)
    {
        var hash = fastId.GetHashCode();
        return (Math.Abs(hash) % FastIdSlots) * 1000; // multiply so per-fast IDs don't collide with each other
    }

    private static string GroupForFast(Guid fastId) => $"fast-{fastId}";
}
