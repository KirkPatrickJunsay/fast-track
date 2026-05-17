using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly INavigationService _navigation;

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool isEmpty = true;

    public HistoryViewModel(IFastRepository fasts, IFastingProtocolRepository protocols, INavigationService navigation)
    {
        _fasts = fasts;
        _protocols = protocols;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        Items.Clear();
        var rows = await _fasts.GetHistoryAsync(100);
        var protocols = await _protocols.GetAllAsync();
        var byId = protocols.ToDictionary(p => p.Id);

        foreach (var f in rows)
        {
            byId.TryGetValue(f.ProtocolId, out var protocol);
            var duration = (f.EndUtc!.Value - f.StartUtc);
            var hh = (int)duration.TotalHours;
            var goalMet = duration.TotalHours >= f.GoalHours;
            Items.Add(new HistoryItemViewModel
            {
                Id = f.Id,
                Title = protocol?.Name ?? "Fast",
                DurationDisplay = $"{hh}h {duration.Minutes}m",
                GoalDisplay = $"goal {f.GoalHours}h",
                EndReasonDisplay = FormatReason(f.EndReason),
                StartedLocal = f.StartUtc.ToLocalTime().ToString("g"),
                EndedLocal = f.EndUtc.Value.ToLocalTime().ToString("g"),
                GoalMet = goalMet,
            });
        }
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task OpenAsync(HistoryItemViewModel? item)
    {
        if (item is null) return;
        await _navigation.GoToAsync($"EditFastPage?fastId={item.Id}");
    }

    private static string FormatReason(FastEndReason? r) => r switch
    {
        FastEndReason.Completed => "completed",
        FastEndReason.Hungry => "ended early — hungry",
        FastEndReason.SocialEvent => "ended early — social event",
        FastEndReason.Illness => "ended early — unwell",
        FastEndReason.Other => "ended early",
        _ => "",
    };
}
