using CommunityToolkit.Mvvm.ComponentModel;

namespace FastTrack.ViewModels;

/// <summary>
/// Lightweight row used by LearnPage. Keeping it separate from Article keeps the
/// view layer from binding to the heavyweight Article record with all its sections.
/// </summary>
public partial class ArticleSummaryViewModel : ObservableObject
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string HeroAsset { get; init; } = string.Empty;
    public string ReadingTimeDisplay { get; init; } = string.Empty;
}
