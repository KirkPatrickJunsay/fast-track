using CommunityToolkit.Mvvm.ComponentModel;
using FastTrack.Models;

namespace FastTrack.ViewModels;

public partial class GoalChipViewModel : ObservableObject
{
    public FastingGoal Goal { get; init; }
    public string Label { get; init; } = string.Empty;

    [ObservableProperty] private bool isSelected;
}
