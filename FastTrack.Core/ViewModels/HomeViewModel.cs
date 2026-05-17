using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class HomeViewModel : ObservableObject, IDisposable
{
    private readonly IFastingService _fasting;
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly IUserProfileRepository _profiles;
    private readonly IStageCalculator _stages;
    private readonly IDialogService _dialogs;
    private readonly IStreakService _streaks;
    private readonly IXpService _xp;
    private readonly IGamificationOrchestrator _gamification;
    private readonly IQuestService _quests;
    private readonly INavigationService _navigation;
    private readonly ITicker _ticker;
    private readonly IWaterService _water;
    private readonly IMoodService _moods;
    private readonly IWeightService _weights;

    private GamificationResult? _pendingRewards;

    private Fast? _activeFast;
    private Fast? _lastCompletedFast;
    private FastingProtocol? _lastCompletedProtocol;
    private FastingProtocol? _defaultProtocol;
    private DateTime _eatingWindowEndUtc;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(ShowStartButton))]
    [NotifyPropertyChangedFor(nameof(StartButtonVisible))]
    private bool isActive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    [NotifyPropertyChangedFor(nameof(ShowStartButton))]
    [NotifyPropertyChangedFor(nameof(StartButtonVisible))]
    [NotifyPropertyChangedFor(nameof(EatingStartButtonVisible))]
    private bool isEatingWindow;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StartButtonVisible))]
    [NotifyPropertyChangedFor(nameof(EatingStartButtonVisible))]
    private bool isEducationalMode;

    public bool IsIdle => !IsActive && !IsEatingWindow;
    public bool ShowStartButton => !IsActive;
    public bool StartButtonVisible => IsIdle && !IsEducationalMode;
    public bool EatingStartButtonVisible => IsEatingWindow && !IsEducationalMode;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty] private string elapsedDisplay = "00:00:00";
    [ObservableProperty] private string goalDisplay = "—";
    [ObservableProperty] private double progress;
    [ObservableProperty] private string progressPercent = "0%";
    [ObservableProperty] private string stageName = "—";
    [ObservableProperty] private string stageSummary = "Tap Start to begin a fast.";
    [ObservableProperty] private string startedAtDisplay = string.Empty;
    [ObservableProperty] private string defaultProtocolName = "—";

    [ObservableProperty] private string eatingWindowRemaining = "00:00:00";
    [ObservableProperty] private string eatingWindowEndsAt = string.Empty;

    // Gamification (Epic 02)
    [ObservableProperty] private int currentStreak;
    [ObservableProperty] private string streakChipText = "0 day streak";
    [ObservableProperty] private int freezesAvailable;
    [ObservableProperty] private string freezesText = string.Empty;
    [ObservableProperty] private bool hasFreezes;
    [ObservableProperty] private string levelName = "Novice Faster";
    [ObservableProperty] private int totalXp;
    [ObservableProperty] private string xpProgressText = "0 / 500 XP";
    [ObservableProperty] private double xpProgressFraction;

    [ObservableProperty] private bool hasQuests;

    // Today health summary (Epic 03)
    [ObservableProperty] private int waterTodayMl;
    [ObservableProperty] private int waterGoalMl = 2000;
    [ObservableProperty] private double waterFraction;
    [ObservableProperty] private string waterDisplay = "0 / 2000 ml";
    [ObservableProperty] private bool waterGoalHit;
    [ObservableProperty] private string latestMoodEmoji = "—";
    [ObservableProperty] private string latestMoodAgo = "Tap to log";
    [ObservableProperty] private string latestWeightDisplay = "—";
    [ObservableProperty] private string weightDeltaDisplay = "Tap to log";

    public ObservableCollection<StageRowViewModel> Stages { get; } = new();
    public ObservableCollection<QuestRowViewModel> Quests { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public HomeViewModel(
        IFastingService fasting,
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        IUserProfileRepository profiles,
        IStageCalculator stages,
        IDialogService dialogs,
        IStreakService streaks,
        IXpService xp,
        IGamificationOrchestrator gamification,
        IQuestService quests,
        INavigationService navigation,
        ITicker ticker,
        IWaterService water,
        IMoodService moods,
        IWeightService weights)
    {
        _fasting = fasting;
        _fasts = fasts;
        _protocols = protocols;
        _profiles = profiles;
        _stages = stages;
        _dialogs = dialogs;
        _streaks = streaks;
        _xp = xp;
        _gamification = gamification;
        _gamification.RewardsGranted += (_, r) => _pendingRewards = r;
        _quests = quests;
        _navigation = navigation;
        _ticker = ticker;
        _water = water;
        _moods = moods;
        _weights = weights;

        for (var i = 0; i < _stages.Stages.Count; i++)
        {
            var s = _stages.Stages[i];
            var range = s.EndHour is null ? $"{s.StartHour:F0}h+" : $"{s.StartHour:F0}–{s.EndHour:F0}h";
            Stages.Add(new StageRowViewModel
            {
                Index = i + 1,
                Key = s.Key,
                Name = s.Name,
                Range = range,
                Summary = s.Summary,
                IconAsset = s.IconAsset,
                LongDescription = s.LongDescription,
                FeelingsBulleted = string.Join("\n", s.Feelings.Select(f => $"•  {f}")),
            });
        }
    }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        try
        {
            var profile = await _profiles.GetOrCreateAsync();
            IsEducationalMode = profile.IsEducationalMode;

            await RefreshGamificationAsync();
            await RefreshHealthAsync();
            await ResolveDefaultProtocolAsync();
            DefaultProtocolName = _defaultProtocol?.Name ?? "(none)";

            _activeFast = await _fasting.GetActiveAsync();
            if (_activeFast is not null)
            {
                EnterActive();
                return;
            }

            await TryEnterEatingWindowAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    private async Task RefreshGamificationAsync()
    {
        var streak = await _streaks.GetSnapshotAsync();
        CurrentStreak = streak.Current;
        StreakChipText = streak.Current == 1 ? "1 day streak" : $"{streak.Current} day streak";
        FreezesAvailable = streak.FreezesAvailable;
        HasFreezes = streak.FreezesAvailable > 0;
        FreezesText = streak.FreezesAvailable == 1 ? "1 freeze" : $"{streak.FreezesAvailable} freezes";

        var xp = await _xp.GetStateAsync();
        TotalXp = xp.TotalXp;
        LevelName = $"Lv {xp.Level.Number} · {xp.Level.Name}";
        if (xp.NextLevel is null || xp.XpForNextLevel == 0)
        {
            XpProgressText = $"{xp.TotalXp:N0} XP · MAX";
            XpProgressFraction = 1;
        }
        else
        {
            XpProgressText = $"{xp.XpIntoLevel:N0} / {xp.XpForNextLevel:N0} XP";
            XpProgressFraction = (double)xp.XpIntoLevel / xp.XpForNextLevel;
        }

        await RefreshQuestsAsync();
    }

    private async Task RefreshQuestsAsync()
    {
        Quests.Clear();
        var today = await _quests.GetTodayAsync();
        foreach (var (q, def) in today)
        {
            Quests.Add(new QuestRowViewModel
            {
                Title = def.Title,
                Description = def.Description,
                XpReward = def.XpReward,
                Progress = q.Target == 0 ? 0 : Math.Min(1.0, (double)q.Progress / q.Target),
                ProgressText = q.IsClaimed ? "Claimed" : $"{q.Progress} / {q.Target}",
                IsClaimed = q.IsClaimed,
            });
        }
        HasQuests = Quests.Count > 0;
    }

    private async Task RefreshHealthAsync()
    {
        try
        {
            // Water
            var snap = await _water.GetTodayAsync();
            WaterTodayMl = snap.TotalMl;
            WaterGoalMl = snap.GoalMl;
            WaterFraction = snap.GoalFraction;
            WaterGoalHit = snap.GoalHit;
            WaterDisplay = FormatWater(snap.TotalMl, snap.GoalMl);

            // Latest mood — newest one of any kind.
            var recentMoods = await _moods.GetRecentAsync(1);
            if (recentMoods.Count > 0)
            {
                var latest = recentMoods[0];
                LatestMoodEmoji = MoodEmoji(latest.MoodLevel);
                LatestMoodAgo = FormatAgo(latest.TimestampUtc);
            }
            else
            {
                LatestMoodEmoji = "—";
                LatestMoodAgo = "Tap to log";
            }

            // Latest weight + delta
            var trend = await _weights.GetTrendAsync(TimeSpan.FromDays(30));
            if (trend.LatestKg is double kg)
            {
                LatestWeightDisplay = $"{kg:0.0} kg";
                if (trend.ChangeKg is double delta)
                {
                    var sign = delta > 0 ? "+" : (delta < 0 ? "−" : "");
                    WeightDeltaDisplay = $"{sign}{Math.Abs(delta):0.0} kg vs prior";
                }
                else
                {
                    WeightDeltaDisplay = "First entry";
                }
            }
            else
            {
                LatestWeightDisplay = "—";
                WeightDeltaDisplay = "Tap to log";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AddWaterAsync(string amount)
    {
        if (!int.TryParse(amount, out var ml) || ml <= 0) return;
        try
        {
            await _water.AddAsync(ml);
            await RefreshHealthAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task OpenLogMoodAsync() =>
        await _navigation.GoToAsync(_activeFast is null
            ? "LogMoodPage"
            : $"LogMoodPage?fastId={_activeFast.Id}");

    [RelayCommand]
    private async Task OpenLogWeightAsync() =>
        await _navigation.GoToAsync("LogWeightPage");

    private static string FormatWater(int totalMl, int goalMl)
    {
        // Show in L if goal is ≥ 1L; otherwise ml.
        if (goalMl >= 1000)
        {
            return $"{totalMl / 1000.0:0.#}L / {goalMl / 1000.0:0.#}L";
        }
        return $"{totalMl} / {goalMl} ml";
    }

    private static string MoodEmoji(int level) => level switch
    {
        1 => "😞",
        2 => "😐",
        3 => "🙂",
        4 => "😊",
        5 => "🤩",
        _ => "—",
    };

    private static string FormatAgo(DateTime utc)
    {
        var delta = DateTime.UtcNow - utc;
        if (delta.TotalSeconds < 60) return "just now";
        if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
        if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
        return $"{(int)delta.TotalDays}d ago";
    }

    private async Task ResolveDefaultProtocolAsync()
    {
        var profile = await _profiles.GetOrCreateAsync();
        var all = await _protocols.GetAllAsync();

        FastingProtocol? chosen = null;
        if (profile.LastUsedProtocolId is { } pid)
        {
            chosen = all.FirstOrDefault(p => p.Id == pid);
        }
        chosen ??= all.FirstOrDefault(p => p.IsPreset) ?? all.FirstOrDefault();
        _defaultProtocol = chosen;
    }

    private async Task TryEnterEatingWindowAsync()
    {
        var history = await _fasts.GetHistoryAsync(1);
        var last = history.FirstOrDefault();
        if (last is null || last.EndUtc is null)
        {
            EnterIdle();
            return;
        }

        var protocol = await _protocols.GetByIdAsync(last.ProtocolId);
        if (protocol is null || protocol.EatHours <= 0)
        {
            EnterIdle();
            return;
        }

        var windowEnds = last.EndUtc.Value.AddHours(protocol.EatHours);
        if (DateTime.UtcNow >= windowEnds)
        {
            EnterIdle();
            return;
        }

        _lastCompletedFast = last;
        _lastCompletedProtocol = protocol;
        _eatingWindowEndUtc = windowEnds;
        EnterEatingWindow();
    }

    [RelayCommand]
    private async Task StartFastAsync()
    {
        ErrorMessage = null;
        try
        {
            if (_defaultProtocol is null) return;
            _activeFast = await _fasting.StartAsync(_defaultProtocol.Id);

            // Persist as last-used.
            var profile = await _profiles.GetOrCreateAsync();
            profile.LastUsedProtocolId = _defaultProtocol.Id;
            await _profiles.UpdateAsync(profile);

            EnterActive();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EndFastAsync()
    {
        if (_activeFast is null) return;

        var elapsed = DateTime.UtcNow - _activeFast.StartUtc;
        var goalMet = elapsed.TotalHours >= _activeFast.GoalHours;
        var stage = _stages.GetStage(elapsed.TotalHours);

        FastEndReason reason;
        if (goalMet)
        {
            reason = FastEndReason.Completed;
        }
        else
        {
            var choice = await _dialogs.ShowActionSheetAsync(
                "End fast early?",
                "Cancel",
                "I'm hungry",
                "Social event",
                "Feeling unwell",
                "Other");
            if (choice is null) return; // user cancelled

            reason = choice switch
            {
                "I'm hungry" => FastEndReason.Hungry,
                "Social event" => FastEndReason.SocialEvent,
                "Feeling unwell" => FastEndReason.Illness,
                _ => FastEndReason.Other,
            };
        }

        try
        {
            var ended = await _fasting.EndAsync(_activeFast.Id, reason);
            _activeFast = null;

            // Give the gamification orchestrator a moment to process the FastCompleted event.
            await Task.Delay(150);

            var hh = (int)elapsed.TotalHours;
            var rewards = _pendingRewards;
            _pendingRewards = null;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(goalMet
                ? $"🎉 Goal met! {hh}h {elapsed.Minutes}m fasted."
                : $"{hh}h {elapsed.Minutes}m fasted.");
            sb.AppendLine($"Furthest stage: {stage.Name}.");

            if (rewards is not null)
            {
                if (rewards.Xp.Total > 0)
                {
                    var bonusBits = new List<string>();
                    if (rewards.Xp.ComebackBonus) bonusBits.Add("comeback +50%");
                    if (rewards.Xp.GoalExceededBonus) bonusBits.Add("goal beat +25%");
                    var bonusText = bonusBits.Count > 0 ? $" ({string.Join(", ", bonusBits)})" : "";
                    sb.AppendLine($"+{rewards.Xp.Total} XP{bonusText}");
                }
                if (rewards.LevelledUp && rewards.PreviousLevel is not null)
                    sb.AppendLine($"⭐ Level up! {rewards.PreviousLevel.Name} → {rewards.XpState.Level.Name}");
                if (rewards.Streak.IncrementedToday)
                    sb.AppendLine(rewards.Streak.FreezeConsumed
                        ? $"🔥 Streak saved with a freeze · {rewards.Streak.Current} days"
                        : $"🔥 Streak: {rewards.Streak.Current} days");
                if (rewards.QuestUpdates.Any(u => u.NewlyCompleted))
                {
                    sb.AppendLine();
                    sb.AppendLine("Quests completed:");
                    foreach (var u in rewards.QuestUpdates.Where(x => x.NewlyCompleted))
                        sb.AppendLine($"✅ {u.Definition.Title}  +{u.Quest.XpReward} XP");
                }
                if (rewards.NewBadges.Count > 0)
                {
                    sb.AppendLine();
                    sb.AppendLine("New badges:");
                    foreach (var b in rewards.NewBadges) sb.AppendLine($"🏅 {b.Name}");
                }
            }

            await _dialogs.ShowAlertAsync(goalMet ? "Fast complete" : "Fast ended", sb.ToString().TrimEnd());

            // Refresh state — likely moves into eating window.
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task StartFastNowAsync()
    {
        // Override eating window and begin next fast immediately.
        await StartFastAsync();
    }

    [RelayCommand]
    private async Task EditActiveFastAsync()
    {
        if (_activeFast is null) return;
        await _navigation.GoToAsync($"EditFastPage?fastId={_activeFast.Id}");
    }

    private void EnterIdle()
    {
        StopTimer();
        IsActive = false;
        IsEatingWindow = false;
        ElapsedDisplay = "00:00:00";
        Progress = 0;
        ProgressPercent = "0%";
        StageName = "—";
        StageSummary = "Tap Start to begin a fast.";
        StartedAtDisplay = string.Empty;
        GoalDisplay = _defaultProtocol is null ? "—" : $"Goal: {_defaultProtocol.FastHours}h";
    }

    private void EnterActive()
    {
        if (_activeFast is null) { EnterIdle(); return; }
        StopTimer();
        IsActive = true;
        IsEatingWindow = false;
        StartedAtDisplay = $"Started {_activeFast.StartUtc.ToLocalTime():g}";
        GoalDisplay = $"Goal: {_activeFast.GoalHours}h";
        ActiveTick();
        StartTimer(ActiveTick);
    }

    private void EnterEatingWindow()
    {
        StopTimer();
        IsActive = false;
        IsEatingWindow = true;
        EatingWindowEndsAt = $"Next fast at {_eatingWindowEndUtc.ToLocalTime():g}";
        EatingTick();
        StartTimer(EatingTick);
    }

    private void StartTimer(Action tickAction)
    {
        _ticker.Start(TimeSpan.FromSeconds(1), tickAction);
    }

    private void StopTimer()
    {
        _ticker.Stop();
    }

    private void ActiveTick()
    {
        if (_activeFast is null) return;
        var elapsed = DateTime.UtcNow - _activeFast.StartUtc;
        if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;

        ElapsedDisplay = FormatDuration(elapsed);

        var goalSeconds = Math.Max(1, _activeFast.GoalHours * 3600);
        var pct = elapsed.TotalSeconds / goalSeconds;
        Progress = Math.Min(1.0, pct);
        ProgressPercent = $"{pct * 100:0}%";

        var stage = _stages.GetStage(elapsed.TotalHours);
        StageName = stage.Name;
        StageSummary = stage.Summary;

        UpdateStageRows(elapsed.TotalHours, stage.Key);
    }

    private void UpdateStageRows(double elapsedHours, string currentKey)
    {
        foreach (var row in Stages)
        {
            var spec = _stages.Stages.First(s => s.Key == row.Key);
            var inPast = spec.EndHour is not null && elapsedHours >= spec.EndHour;
            var isCurrent = row.Key == currentKey;
            row.IsCurrent = isCurrent;
            row.IsPast = inPast;
            row.Opacity = isCurrent ? 1.0 : inPast ? 0.7 : 0.55;
            row.StatusLabel = isCurrent ? "NOW" : inPast ? "REACHED" : "UPCOMING";
        }
    }

    private void EatingTick()
    {
        var remaining = _eatingWindowEndUtc - DateTime.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            EnterIdle();
            return;
        }
        EatingWindowRemaining = FormatDuration(remaining);
    }

    private static string FormatDuration(TimeSpan t)
    {
        var totalH = (int)t.TotalHours;
        return totalH >= 100
            ? $"{totalH}:{t.Minutes:D2}:{t.Seconds:D2}"
            : $"{totalH:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }

    public void Dispose() => StopTimer();
}
