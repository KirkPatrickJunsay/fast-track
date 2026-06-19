using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class StageDetailViewModel : ObservableObject
{
    private readonly IStageCalculator _stages;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string name = string.Empty;
    [ObservableProperty] private string rangeDisplay = string.Empty;
    [ObservableProperty] private string summary = string.Empty;
    [ObservableProperty] private string longDescription = string.Empty;
    [ObservableProperty] private string iconAsset = "stage_anabolic.svg";
    [ObservableProperty] private string stageIndexDisplay = string.Empty;

    public ObservableCollection<string> Feelings { get; } = new();

    public StageDetailViewModel(IStageCalculator stages, INavigationService navigation)
    {
        _stages = stages;
        _navigation = navigation;
    }

    public void LoadFromKey(string? stageKey)
    {
        var key = stageKey ?? _stages.Stages[0].Key;
        var index = _stages.Stages.ToList().FindIndex(s => s.Key == key);
        var stage = index >= 0 ? _stages.Stages[index] : _stages.Stages[0];
        if (index < 0) index = 0;

        Name = stage.Name;
        Summary = stage.Summary;
        LongDescription = stage.LongDescription;
        IconAsset = stage.IconAsset;
        RangeDisplay = FormatRange(stage);
        StageIndexDisplay = $"Stage {index + 1} of {_stages.Stages.Count}";

        Feelings.Clear();
        foreach (var f in stage.Feelings) Feelings.Add(f);
    }

    [RelayCommand]
    private Task CloseAsync() => _navigation.GoBackAsync();

    private static string FormatRange(FastingStage stage) =>
        stage.EndHour is null
            ? $"{stage.StartHour:F0} hours and beyond"
            : $"{stage.StartHour:F0}–{stage.EndHour:F0} hours";
}
