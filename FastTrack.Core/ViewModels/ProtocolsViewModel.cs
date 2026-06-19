using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class ProtocolsViewModel : ObservableObject
{
    private readonly IFastingProtocolRepository _protocols;
    private readonly IUserProfileRepository _profiles;
    private readonly INavigationService _navigation;

    public ObservableCollection<ProtocolListItem> Items { get; } = new();

    public ProtocolsViewModel(IFastingProtocolRepository protocols, IUserProfileRepository profiles, INavigationService navigation)
    {
        _protocols = protocols;
        _profiles = profiles;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        Items.Clear();
        var profile = await _profiles.GetOrCreateAsync();
        var all = await _protocols.GetAllAsync();
        foreach (var p in all)
        {
            Items.Add(ToItem(p, profile.LastUsedProtocolId));
        }
    }

    [RelayCommand]
    private async Task NewCustomAsync() =>
        await _navigation.GoToAsync("CustomProtocolPage");

    [RelayCommand]
    private async Task SetDefaultAsync(ProtocolListItem? item)
    {
        if (item is null) return;
        var profile = await _profiles.GetOrCreateAsync();
        profile.LastUsedProtocolId = item.Id;
        await _profiles.UpdateAsync(profile);
        foreach (var i in Items) i.IsDefault = i.Id == item.Id;
    }

    private static ProtocolListItem ToItem(FastingProtocol p, Guid? defaultId) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Subtitle = p.EatHours > 0 ? $"{p.FastHours}h fast · {p.EatHours}h eat" : $"{p.FastHours}h fast",
        Difficulty = p.Difficulty.ToString(),
        Description = p.Description ?? string.Empty,
        IconAsset = ResolveIcon(p),
        IsDefault = defaultId == p.Id,
    };

    public static string ResolveIcon(FastingProtocol p)
    {
        // Match by canonical preset name first; custom protocols fall through to the slider icon.
        return p.Name switch
        {
            "16:8" => "protocol_16_8.svg",
            "18:6" => "protocol_18_6.svg",
            "20:4" => "protocol_20_4.svg",
            "OMAD" => "protocol_omad.svg",
            "5:2"  => "protocol_5_2.svg",
            _ => "protocol_custom.svg",
        };
    }
}
