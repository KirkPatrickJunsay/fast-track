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
    private readonly IHapticService _haptics;
    private readonly ICelebrationCarrier _celebrations;
    private readonly IDashboardPreferencesService _dashboardPrefs;

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
    [NotifyPropertyChangedFor(nameof(ShowProgressGrid))]
    [NotifyPropertyChangedFor(nameof(ShowStagesRoadmapSection))]
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

    [ObservableProperty] private string greeting = "Today";
    [ObservableProperty] private string subGreeting = string.Empty;

    [ObservableProperty] private int currentStageIndex = -1;
    /// <summary>Raised when the active fast crosses into a new stage. The page plays a glow animation in response.</summary>
    public event EventHandler? StageJustChanged;

    [ObservableProperty] private string elapsedDisplay = "00:00:00";
    [ObservableProperty] private string goalDisplay = "—";
    [ObservableProperty] private double progress;
    /// <summary>Un-clamped progress (can exceed 1.0 when goal is beaten). Drives the ring beyond 100%.</summary>
    [ObservableProperty] private double rawProgress;
    [ObservableProperty] private bool isGoalMet;
    [ObservableProperty] private string progressPercent = "0%";
    [ObservableProperty] private string stageName = "—";
    [ObservableProperty] private string stageSummary = "Tap Start to begin a fast.";
    [ObservableProperty] private string startedAtDisplay = string.Empty;
    [ObservableProperty] private string defaultProtocolName = "—";

    [ObservableProperty] private string eatingWindowRemaining = "00:00:00";
    [ObservableProperty] private string eatingWindowEndsAt = string.Empty;

    // Gamification (Epic 02)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StreakFlameAsset))]
    private int currentStreak;
    [ObservableProperty] private string streakChipText = "0 day streak";

    /// <summary>Streak chip flame illustration — escalates with streak length.</summary>
    public string StreakFlameAsset => CurrentStreak switch
    {
        <= 0 => "streak_spark.svg",
        < 3 => "streak_spark.svg",
        < 7 => "streak_flame_small.svg",
        < 14 => "streak_flame_large.svg",
        < 30 => "streak_bonfire.svg",
        _ => "streak_inferno.svg",
    };

    [ObservableProperty] private int freezesAvailable;
    [ObservableProperty] private string freezesText = string.Empty;
    [ObservableProperty] private bool hasFreezes;
    [ObservableProperty] private string levelName = "Novice Faster";
    [ObservableProperty] private int totalXp;
    [ObservableProperty] private string xpProgressText = "0 / 500 XP";
    [ObservableProperty] private double xpProgressFraction;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQuestsCard))]
    private bool hasQuests;

    // Dashboard preferences (Epic 10) — these gate which sections of Home render.
    // Default to "show everything" so empty profiles see the original layout.
    [ObservableProperty] private bool showGamification = true;
    [ObservableProperty] private bool showDailyHealth = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQuestsCard))]
    private bool showQuestsPref = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProgressGrid))]
    [NotifyPropertyChangedFor(nameof(ShowStagesRoadmapSection))]
    private bool showProgressCardsPref = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowStagesRoadmapSection))]
    private bool showStagesRoadmapPref = true;

    public bool ShowQuestsCard => ShowQuestsPref && HasQuests;
    public bool ShowProgressGrid => ShowProgressCardsPref && IsActive;
    public bool ShowStagesRoadmapSection => ShowStagesRoadmapPref && IsActive;

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
        IWeightService weights,
        IHapticService haptics,
        ICelebrationCarrier celebrations,
        IDashboardPreferencesService dashboardPrefs)
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
        _haptics = haptics;
        _celebrations = celebrations;
        _dashboardPrefs = dashboardPrefs;

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

            UpdateGreeting(profile.DisplayName);

            var prefs = await _dashboardPrefs.GetAsync();
            ShowGamification = prefs.ShowGamification;
            ShowDailyHealth = prefs.ShowDailyHealth;
            ShowQuestsPref = prefs.ShowQuests;
            ShowProgressCardsPref = prefs.ShowProgressCards;
            ShowStagesRoadmapPref = prefs.ShowStagesRoadmap;

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
            _haptics.Tick();
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

    [RelayCommand]
    private async Task OpenStageAsync(StageRowViewModel? row)
    {
        if (row is null) return;
        _haptics.Tick(HapticIntensity.Light);
        await _navigation.GoToAsync($"StageDetailPage?stageKey={row.Key}");
    }

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
            _haptics.Tick(HapticIntensity.Medium);
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

        // Upfront confirmation guards against accidental taps on the "End fast"
        // button — especially important when a fast is mid-run and an early
        // end would lose hours of progress. Wording adapts to goal status so
        // the celebratory case still feels positive instead of cautionary.
        var elapsedH = (int)elapsed.TotalHours;
        var elapsedM = elapsed.Minutes;
        var confirmMessage = goalMet
            ? $"You've reached your goal at {elapsedH}h {elapsedM}m. Ready to wrap this fast?"
            : $"You're {elapsedH}h {elapsedM}m into a {_activeFast.GoalHours:0.#}h fast. Ending now will be logged as a partial fast.";
        var confirmed = await _dialogs.ConfirmAsync(
            title: "End fast?",
            message: confirmMessage,
            ok: "End fast",
            cancel: "Keep going");
        if (!confirmed) return;

        FastEndReason reason;
        if (goalMet)
        {
            reason = FastEndReason.Completed;
        }
        else
        {
            var choice = await _dialogs.ShowActionSheetAsync(
                "Reason for ending early?",
                "Cancel",
                "I'm hungry",
                "Social event",
                "Feeling unwell",
                "Other");
            if (choice is null) return; // user cancelled the reason picker

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
            _haptics.Tick(goalMet ? HapticIntensity.Heavy : HapticIntensity.Medium);
            var ended = await _fasting.EndAsync(_activeFast.Id, reason);
            _activeFast = null;

            // Give the gamification orchestrator a moment to process the FastCompleted event.
            await Task.Delay(150);
            var rewards = _pendingRewards;
            _pendingRewards = null;

            _celebrations.Set(new CelebrationData(
                GoalMet: goalMet,
                Duration: elapsed,
                StageName: stage.Name,
                XpEarned: rewards?.Xp.Total ?? 0,
                ComebackBonus: rewards?.Xp.ComebackBonus ?? false,
                GoalExceededBonus: rewards?.Xp.GoalExceededBonus ?? false,
                CurrentStreak: rewards?.Streak.Current ?? 0,
                StreakIncremented: rewards?.Streak.IncrementedToday ?? false,
                FreezeConsumed: rewards?.Streak.FreezeConsumed ?? false,
                LevelledUp: rewards?.LevelledUp ?? false,
                PreviousLevel: rewards?.PreviousLevel,
                NewLevel: rewards?.XpState.Level,
                NewBadges: rewards?.NewBadges ?? Array.Empty<BadgeDefinition>(),
                ClaimedQuests: rewards?.QuestUpdates.Where(q => q.NewlyCompleted).Select(q => q.Definition.Title).ToList() ?? new List<string>()));

            await _navigation.GoToAsync("CelebrationPage");

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
        RawProgress = 0;
        IsGoalMet = false;
        CurrentStageIndex = -1;
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
        RawProgress = pct;
        IsGoalMet = pct >= 1.0;
        ProgressPercent = $"{pct * 100:0}%";

        var stage = _stages.GetStage(elapsed.TotalHours);
        StageName = stage.Name;
        StageSummary = stage.Summary;

        UpdateStageRows(elapsed.TotalHours, stage.Key);

        // Detect a stage change for the glow animation + auto-snap of the roadmap pager.
        var newIndex = _stages.Stages.ToList().FindIndex(s => s.Key == stage.Key);
        if (newIndex >= 0 && newIndex != CurrentStageIndex)
        {
            var wasInitial = CurrentStageIndex < 0;
            CurrentStageIndex = newIndex;
            if (!wasInitial) StageJustChanged?.Invoke(this, EventArgs.Empty);
        }
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
            // Stronger fade-out on past + future so the current card visibly dominates.
            // Current = full strength; past = clearly done; future = quietly waiting.
            row.Opacity = isCurrent ? 1.0 : inPast ? 0.55 : 0.45;
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

    /// <summary>
    /// Lightweight refresh used by the page's Window.Resumed handler so the
    /// greeting (and date sub-line) stay current when the user returns from
    /// background. Cheaper than a full LoadAsync: re-reads the profile and
    /// rebuilds two labels, nothing else.
    /// </summary>
    public async Task RefreshGreetingAsync()
    {
        try
        {
            var profile = await _profiles.GetOrCreateAsync();
            UpdateGreeting(profile.DisplayName);
        }
        catch { /* greeting is non-critical chrome */ }
    }

    private void UpdateGreeting(string? displayName)
    {
        var hour = DateTime.Now.Hour;
        var period = hour switch
        {
            >= 5 and < 12 => "Good morning",
            >= 12 and < 17 => "Good afternoon",
            >= 17 and < 22 => "Good evening",
            _ => "Hi there",
        };
        var name = string.IsNullOrWhiteSpace(displayName) ? "Faster" : displayName.Trim();
        Greeting = $"{period}, {name}";
        SubGreeting = DateTime.Now.ToString("dddd, MMMM d");
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
