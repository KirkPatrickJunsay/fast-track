using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class CelebrationViewModel : ObservableObject
{
    private readonly ICelebrationCarrier _carrier;
    private readonly INavigationService _navigation;
    private readonly IHapticService _haptics;

    [ObservableProperty] private string headline = "Fast complete";
    [ObservableProperty] private string emoji = "🎉";
    [ObservableProperty] private string durationDisplay = "—";
    [ObservableProperty] private string stageDisplay = "—";

    [ObservableProperty] private bool hasXp;
    [ObservableProperty] private string xpDisplay = string.Empty;
    [ObservableProperty] private int xpAmount;
    [ObservableProperty] private string xpBonusDisplay = string.Empty;

    /// <summary>True if the celebration is for a successfully completed fast — drives confetti.</summary>
    [ObservableProperty] private bool isGoalMet;

    [ObservableProperty] private bool hasStreak;
    [ObservableProperty] private string streakDisplay = string.Empty;
    [ObservableProperty] private string streakSubtext = string.Empty;

    [ObservableProperty] private bool hasLevelUp;
    [ObservableProperty] private string levelUpDisplay = string.Empty;

    [ObservableProperty] private bool hasBadges;
    public ObservableCollection<string> Badges { get; } = new();

    [ObservableProperty] private bool hasQuests;
    public ObservableCollection<string> Quests { get; } = new();

    public CelebrationViewModel(ICelebrationCarrier carrier, INavigationService navigation, IHapticService haptics)
    {
        _carrier = carrier;
        _navigation = navigation;
        _haptics = haptics;
    }

    public Task LoadAsync()
    {
        var data = _carrier.Take();
        if (data is null)
        {
            // Nothing to celebrate — render a neutral state, user can close.
            Headline = "Fast ended";
            Emoji = "·";
            return Task.CompletedTask;
        }

        IsGoalMet = data.GoalMet;
        Headline = data.GoalMet ? "Fast complete" : "Fast ended";
        Emoji = data.GoalMet ? "🎉" : "·";
        DurationDisplay = FormatDuration(data.Duration);
        StageDisplay = $"Furthest stage · {data.StageName}";

        if (data.XpEarned > 0)
        {
            HasXp = true;
            XpAmount = data.XpEarned;
            XpDisplay = $"+{data.XpEarned} XP";
            var bonuses = new List<string>();
            if (data.ComebackBonus) bonuses.Add("comeback +50%");
            if (data.GoalExceededBonus) bonuses.Add("goal beat +25%");
            XpBonusDisplay = bonuses.Count > 0 ? string.Join(" · ", bonuses) : string.Empty;
        }

        if (data.StreakIncremented)
        {
            HasStreak = true;
            StreakDisplay = data.CurrentStreak == 1 ? "1 day streak" : $"{data.CurrentStreak} day streak";
            StreakSubtext = data.FreezeConsumed ? "Saved by a freeze." : "Keep it going.";
        }

        if (data.LevelledUp && data.PreviousLevel is not null && data.NewLevel is not null)
        {
            HasLevelUp = true;
            LevelUpDisplay = $"Level up! {data.PreviousLevel.Name} → {data.NewLevel.Name}";
        }

        if (data.NewBadges.Count > 0)
        {
            HasBadges = true;
            foreach (var b in data.NewBadges) Badges.Add(b.Name);
        }

        if (data.ClaimedQuests.Count > 0)
        {
            HasQuests = true;
            foreach (var q in data.ClaimedQuests) Quests.Add(q);
        }

        // Soft success haptic on appearance.
        _haptics.Tick(data.GoalMet ? HapticIntensity.Heavy : HapticIntensity.Light);
        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ContinueAsync()
    {
        _haptics.Tick();
        await _navigation.GoBackAsync();
    }

    private static string FormatDuration(TimeSpan t)
    {
        var h = (int)Math.Floor(t.TotalHours);
        var m = t.Minutes;
        return $"{h}h {m}m";
    }
}
