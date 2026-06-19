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
    private readonly IDialogService _dialogs;
    private readonly IFileShareService _share;
    private readonly IHapticService _haptics;

    public ObservableCollection<HistoryItemViewModel> Items { get; } = new();

    [ObservableProperty] private bool isEmpty = true;
    [ObservableProperty] private bool isRefreshing;

    public HistoryViewModel(
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        INavigationService navigation,
        IDialogService dialogs,
        IFileShareService share,
        IHapticService haptics)
    {
        _fasts = fasts;
        _protocols = protocols;
        _navigation = navigation;
        _dialogs = dialogs;
        _share = share;
        _haptics = haptics;
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
                IconAsset = protocol is null ? "protocol_custom.svg" : ProtocolsViewModel.ResolveIcon(protocol),
            });
        }
        IsEmpty = Items.Count == 0;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _haptics.Tick(HapticIntensity.Light);
        try { await LoadAsync(); }
        finally { IsRefreshing = false; }
    }

    [RelayCommand]
    private async Task SwipeDeleteAsync(HistoryItemViewModel? item)
    {
        if (item is null) return;
        _haptics.Tick(HapticIntensity.Medium);
        var confirmed = await _dialogs.ConfirmAsync(
            "Delete this fast?",
            "This can't be undone.",
            "Delete",
            "Cancel");
        if (!confirmed) return;
        await _fasts.DeleteAsync(item.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task OpenAsync(HistoryItemViewModel? item)
    {
        if (item is null) return;
        await _navigation.GoToAsync($"FastDetailPage?fastId={item.Id}");
    }

    [RelayCommand]
    private async Task OptionsAsync(HistoryItemViewModel? item)
    {
        if (item is null) return;
        var choice = await _dialogs.ShowActionSheetAsync(
            item.Title,
            "Cancel",
            "Edit",
            "Share",
            "Delete");
        if (choice is null) return;
        switch (choice)
        {
            case "Edit":
                await _navigation.GoToAsync($"EditFastPage?fastId={item.Id}");
                break;
            case "Share":
                await _share.ShareTextAsync(
                    "Fast Track — fast summary",
                    $"{item.Title}\n{item.DurationDisplay} ({item.GoalDisplay})\nStarted {item.StartedLocal}\nEnded {item.EndedLocal}\n{item.EndReasonDisplay}");
                break;
            case "Delete":
                var confirmed = await _dialogs.ConfirmAsync(
                    "Delete this fast?",
                    "This can't be undone.",
                    "Delete",
                    "Cancel");
                if (confirmed)
                {
                    await _fasts.DeleteAsync(item.Id);
                    await LoadAsync();
                }
                break;
        }
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
