using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Daily quests (US-02.6). 3 deterministic quests per local date, chosen via a hash of the date string.
/// Quest progress updates when relevant events fire. Completion auto-awards XP.
/// </summary>
public sealed class QuestService : IQuestService
{
    private const int QuestsPerDay = 3;

    private static readonly IReadOnlyList<QuestDefinition> _definitions = new[]
    {
        new QuestDefinition("complete_one", "Finish a fast", "Complete any fast today.", QuestType.CompleteAnyFast, 1, 25),

        // Length ladder — beginner (12h) → marathon (48h). Random pick gives variety.
        new QuestDefinition("fast_12h", "Warm up", "Complete a 12-hour fast.", QuestType.FastAtLeastHours, 12, 30),
        new QuestDefinition("fast_16h", "Stack 16 hours", "Complete a 16-hour fast.", QuestType.FastAtLeastHours, 16, 50),
        new QuestDefinition("fast_18h", "Reach ketosis", "Complete an 18-hour fast.", QuestType.FastAtLeastHours, 18, 60),
        new QuestDefinition("fast_20h", "Twenty-hour push", "Complete a 20-hour fast.", QuestType.FastAtLeastHours, 20, 70),
        new QuestDefinition("fast_24h", "Day-long discipline", "Complete a 24-hour fast.", QuestType.FastAtLeastHours, 24, 90),
        new QuestDefinition("fast_36h", "Marathon mode", "Complete a 36-hour fast.", QuestType.FastAtLeastHours, 36, 130),
        new QuestDefinition("fast_48h", "Two-day fast", "Complete a 48-hour fast.", QuestType.FastAtLeastHours, 48, 180),

        // Beat-goal ladder.
        new QuestDefinition("beat_goal_1h", "Beat your goal", "Exceed your goal by 1+ hour.", QuestType.BeatGoalByHours, 1, 75),
        new QuestDefinition("beat_goal_2h", "Past the line", "Exceed your goal by 2+ hours.", QuestType.BeatGoalByHours, 2, 110),
        new QuestDefinition("beat_goal_4h", "Way past the line", "Exceed your goal by 4+ hours.", QuestType.BeatGoalByHours, 4, 160),

        // Variety + lifestyle.
        new QuestDefinition("new_protocol", "Try something new", "Use a protocol you haven't picked before.", QuestType.TryNewProtocol, 1, 40),
        new QuestDefinition("finish_morning", "Morning finisher", "End a fast before noon.", QuestType.FinishBeforeNoon, 1, 30),
        new QuestDefinition("finish_evening", "Evening close", "End a fast after 6pm.", QuestType.FinishAfter6pm, 1, 30),
        new QuestDefinition("weekend_warrior", "Weekend warrior", "Complete a fast on Saturday or Sunday.", QuestType.WeekendFast, 1, 45),
        new QuestDefinition("back_to_back", "Back-to-back", "Start within 24h of your last fast's end.", QuestType.BackToBackWithin24h, 1, 55),
        new QuestDefinition("stay_committed", "Stay committed", "Complete a fast without ending early.", QuestType.DontEndEarly, 1, 35),
    };

    private readonly IDailyQuestRepository _repo;
    private readonly IXpService _xp;
    private readonly IFastingProtocolRepository _protocols;

    public QuestService(IDailyQuestRepository repo, IXpService xp, IFastingProtocolRepository protocols)
    {
        _repo = repo;
        _xp = xp;
        _protocols = protocols;
    }

    public async Task<IReadOnlyList<(DailyQuest Quest, QuestDefinition Definition)>> GetTodayAsync()
    {
        var key = TodayKey();
        var rows = await _repo.GetForDateAsync(key);
        if (rows.Count == 0)
        {
            rows = await GenerateForDateAsync(key);
        }
        return rows
            .Select(r => (r, _definitions.FirstOrDefault(d => d.Key == r.QuestKey)))
            .Where(t => t.Item2 is not null)
            .Select(t => (t.r, t.Item2!))
            .ToList();
    }

    public async Task<IReadOnlyList<QuestUpdate>> OnFastCompletedAsync(Fast fast, FastingProtocol protocol, IReadOnlyList<Fast> history)
    {
        var today = await GetTodayAsync();
        if (today.Count == 0) return Array.Empty<QuestUpdate>();

        var updates = new List<QuestUpdate>();
        foreach (var (quest, def) in today)
        {
            if (quest.IsClaimed) continue;

            var progressAdd = ScoreFast(fast, protocol, def, history);
            if (progressAdd <= 0) continue;

            quest.Progress = Math.Min(quest.Target, quest.Progress + progressAdd);
            var nowCompleted = quest.Progress >= quest.Target;
            if (nowCompleted && !quest.IsClaimed)
            {
                quest.IsClaimed = true;
                quest.ClaimedAtUtc = DateTime.UtcNow;
                await _xp.AwardAsync(quest.XpReward);
                updates.Add(new QuestUpdate(quest, def, NewlyCompleted: true));
            }
            else
            {
                updates.Add(new QuestUpdate(quest, def, NewlyCompleted: false));
            }
            await _repo.UpsertAsync(quest);
        }
        return updates;
    }

    private static int ScoreFast(Fast fast, FastingProtocol protocol, QuestDefinition def, IReadOnlyList<Fast> history)
    {
        if (fast.EndUtc is null) return 0;
        var elapsed = (fast.EndUtc.Value - fast.StartUtc).TotalHours;

        var endLocal = fast.EndUtc.Value.ToLocalTime();

        return def.Type switch
        {
            QuestType.CompleteAnyFast => 1,
            QuestType.FastAtLeastHours => elapsed >= def.Target ? def.Target : 0,
            QuestType.BeatGoalByHours => (elapsed - fast.GoalHours) >= def.Target ? def.Target : 0,
            QuestType.TryNewProtocol => IsFirstUseOfProtocol(fast.ProtocolId, history) ? 1 : 0,
            QuestType.FinishBeforeNoon => endLocal.Hour < 12 ? 1 : 0,
            QuestType.FinishAfter6pm => endLocal.Hour >= 18 ? 1 : 0,
            QuestType.WeekendFast => endLocal.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? 1 : 0,
            QuestType.BackToBackWithin24h => IsBackToBack(fast, history) ? 1 : 0,
            QuestType.DontEndEarly => fast.EndReason == FastEndReason.Completed ? 1 : 0,
            _ => 0,
        };
    }

    /// <summary>
    /// True if there is a previous completed fast (not this one) whose end was within
    /// 24h before this fast's start. Used by the "Back-to-back" quest to reward
    /// consistency without rewarding people who just chained two fasts back-to-back
    /// with no break.
    /// </summary>
    private static bool IsBackToBack(Fast fast, IReadOnlyList<Fast> history)
    {
        var previous = history
            .Where(f => f.Id != fast.Id && f.EndUtc is not null)
            .OrderByDescending(f => f.EndUtc!.Value)
            .FirstOrDefault();
        if (previous is null) return false;
        var gap = fast.StartUtc - previous.EndUtc!.Value;
        return gap.TotalHours is >= 0 and <= 24;
    }

    private static bool IsFirstUseOfProtocol(Guid protocolId, IReadOnlyList<Fast> history)
        => history.Count(f => f.ProtocolId == protocolId) <= 1;

    private async Task<IReadOnlyList<DailyQuest>> GenerateForDateAsync(DateTime localDateUtc)
    {
        var rnd = new Random(SeedForDate(localDateUtc));
        var picks = _definitions.OrderBy(_ => rnd.Next()).Take(QuestsPerDay).ToList();

        var rows = picks.Select(def => new DailyQuest
        {
            LocalDateUtc = localDateUtc,
            QuestKey = def.Key,
            Progress = 0,
            Target = def.Target,
            XpReward = def.XpReward,
        }).ToList();

        await _repo.UpsertManyAsync(rows);
        // Re-read to get assigned Ids.
        return (await _repo.GetForDateAsync(localDateUtc)).ToList();
    }

    private static int SeedForDate(DateTime localDateUtc)
        => (int)(localDateUtc.Ticks % int.MaxValue);

    private static DateTime TodayKey()
    {
        var nowLocal = DateTime.Now;
        return DateTime.SpecifyKind(nowLocal.Date, DateTimeKind.Utc);
    }
}
