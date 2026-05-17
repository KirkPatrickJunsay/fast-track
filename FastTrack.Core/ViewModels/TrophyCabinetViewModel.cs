using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class TrophyCabinetViewModel : ObservableObject
{
    private readonly IBadgeService _badges;

    public ObservableCollection<TrophyItemViewModel> Items { get; } = new();

    [ObservableProperty] private string summary = "—";

    public TrophyCabinetViewModel(IBadgeService badges) => _badges = badges;

    public async Task LoadAsync()
    {
        var evaluated = await _badges.GetAllAsync();
        var earnedCount = evaluated.Count(b => b.IsEarned);

        Items.Clear();
        foreach (var b in evaluated)
        {
            var concealed = b.Definition.IsHidden && !b.IsEarned;
            Items.Add(new TrophyItemViewModel
            {
                Key = b.Definition.Key,
                Name = concealed ? "???" : b.Definition.Name,
                Description = concealed ? "Hidden — keep fasting to discover this one." : b.Definition.Description,
                IconAsset = b.Definition.IconAsset,
                IsEarned = b.IsEarned,
                EarnedText = b.IsEarned ? $"Earned {b.EarnedAtUtc!.Value.ToLocalTime():d}" : (b.Definition.IsHidden ? "Hidden" : "Locked"),
            });
        }

        Summary = $"{earnedCount} of {evaluated.Count} badges earned";
    }
}
