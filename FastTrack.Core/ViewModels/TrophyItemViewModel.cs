namespace FastTrack.ViewModels;

public sealed class TrophyItemViewModel
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconAsset { get; init; } = string.Empty;
    public bool IsEarned { get; init; }
    public double Opacity => IsEarned ? 1.0 : 0.4;
    public string EarnedText { get; init; } = string.Empty;
}
