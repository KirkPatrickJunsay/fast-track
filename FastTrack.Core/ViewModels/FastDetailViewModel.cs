using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class FastDetailViewModel : ObservableObject
{
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly IMoodService _moods;
    private readonly IStageCalculator _stages;
    private readonly INavigationService _navigation;

    public string FastId { get; set; } = string.Empty;

    [ObservableProperty] private string title = "Fast";
    [ObservableProperty] private string startedLocal = "—";
    [ObservableProperty] private string endedLocal = "—";
    [ObservableProperty] private string durationDisplay = "—";
    [ObservableProperty] private string goalDisplay = "—";
    [ObservableProperty] private string stageReachedDisplay = "—";
    [ObservableProperty] private string endReasonDisplay = string.Empty;
    [ObservableProperty] private string protocolDisplay = "—";
    [ObservableProperty] private bool goalMet;
    [ObservableProperty] private bool isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMoods))]
    private int moodCount;
    public bool HasMoods => MoodCount > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<MoodTimelineItem> MoodTimeline { get; } = new();

    public FastDetailViewModel(
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        IMoodService moods,
        IStageCalculator stages,
        INavigationService navigation)
    {
        _fasts = fasts;
        _protocols = protocols;
        _moods = moods;
        _stages = stages;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        ErrorMessage = null;
        if (!Guid.TryParse(FastId, out var id))
        {
            ErrorMessage = "Invalid fast id.";
            return;
        }

        var fast = await _fasts.GetByIdAsync(id);
        if (fast is null)
        {
            ErrorMessage = "Fast not found.";
            return;
        }

        var protocol = await _protocols.GetByIdAsync(fast.ProtocolId);
        ProtocolDisplay = protocol?.Name ?? "Custom";
        Title = $"Fast · {ProtocolDisplay}";

        var startLocal = fast.StartUtc.ToLocalTime();
        StartedLocal = startLocal.ToString("g");

        var endUtc = fast.EndUtc ?? DateTime.UtcNow;
        EndedLocal = fast.EndUtc is null ? "In progress" : endUtc.ToLocalTime().ToString("g");

        var duration = endUtc - fast.StartUtc;
        DurationDisplay = FormatDuration(duration);
        GoalDisplay = $"Goal {fast.GoalHours}h";
        GoalMet = duration.TotalHours >= fast.GoalHours;

        var stage = _stages.GetStage(duration.TotalHours);
        StageReachedDisplay = stage.Name;

        EndReasonDisplay = FormatReason(fast.EndReason);

        MoodTimeline.Clear();
        var moods = await _moods.GetForFastAsync(fast.Id);
        foreach (var m in moods.OrderBy(m => m.TimestampUtc))
        {
            var hoursIn = (m.TimestampUtc - fast.StartUtc).TotalHours;
            MoodTimeline.Add(new MoodTimelineItem
            {
                Emoji = MoodEmoji(m.MoodLevel),
                Label = $"{hoursIn:0.#}h in",
                Note = m.Note ?? string.Empty,
            });
        }
        MoodCount = MoodTimeline.Count;

        IsLoaded = true;
    }

    [RelayCommand]
    private async Task EditAsync()
    {
        if (!Guid.TryParse(FastId, out _)) return;
        await _navigation.GoToAsync($"EditFastPage?fastId={FastId}");
    }

    [RelayCommand]
    private async Task BackAsync() => await _navigation.GoBackAsync();

    private static string FormatDuration(TimeSpan t)
    {
        var h = (int)Math.Floor(t.TotalHours);
        var m = t.Minutes;
        return $"{h}h {m}m";
    }

    private static string FormatReason(FastEndReason? r) => r switch
    {
        FastEndReason.Completed => "Completed",
        FastEndReason.Hungry => "Ended early — hungry",
        FastEndReason.SocialEvent => "Ended early — social event",
        FastEndReason.Illness => "Ended early — feeling unwell",
        FastEndReason.Other => "Ended early",
        _ => "In progress",
    };

    private static string MoodEmoji(int level) => level switch
    {
        1 => "😞", 2 => "😐", 3 => "🙂", 4 => "😊", 5 => "🤩", _ => "·",
    };
}

public sealed class MoodTimelineItem
{
    public string Emoji { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public bool HasNote => !string.IsNullOrWhiteSpace(Note);
}
