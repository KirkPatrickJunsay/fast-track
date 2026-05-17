using CommunityToolkit.Mvvm.ComponentModel;

namespace FastTrack.ViewModels;

public partial class QuestRowViewModel : ObservableObject
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int XpReward { get; init; }
    public double Progress { get; init; }
    public string ProgressText { get; init; } = string.Empty;
    public bool IsClaimed { get; init; }
    public double Opacity => IsClaimed ? 0.5 : 1.0;
}
