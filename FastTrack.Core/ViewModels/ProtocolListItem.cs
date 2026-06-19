using CommunityToolkit.Mvvm.ComponentModel;

namespace FastTrack.ViewModels;

public partial class ProtocolListItem : ObservableObject
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Difficulty { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconAsset { get; init; } = "protocol_custom.svg";

    [ObservableProperty] private bool isDefault;
}
