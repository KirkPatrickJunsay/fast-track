using CommunityToolkit.Mvvm.ComponentModel;

namespace FastTrack.ViewModels;

public partial class StageRowViewModel : ObservableObject
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Range { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string IconAsset { get; init; } = string.Empty;
    public string LongDescription { get; init; } = string.Empty;
    public string FeelingsBulleted { get; init; } = string.Empty;
    public int Index { get; init; }

    [ObservableProperty] private bool isCurrent;
    [ObservableProperty] private bool isPast;
    [ObservableProperty] private double opacity = 0.55;
    [ObservableProperty] private string statusLabel = "UPCOMING";
}
