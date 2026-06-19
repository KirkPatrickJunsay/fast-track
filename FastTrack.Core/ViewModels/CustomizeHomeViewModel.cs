using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

/// <summary>
/// Settings → Customize home. Switch each card on/off; saves on every toggle so
/// the user never has to press a "Save" button — closing the page is enough.
/// </summary>
public partial class CustomizeHomeViewModel : ObservableObject
{
    private readonly IDashboardPreferencesService _prefs;
    private readonly INavigationService _navigation;
    private bool _suppressSave;

    [ObservableProperty] private bool showGamification;
    [ObservableProperty] private bool showDailyHealth;
    [ObservableProperty] private bool showQuests;
    [ObservableProperty] private bool showProgressCards;
    [ObservableProperty] private bool showStagesRoadmap;

    public CustomizeHomeViewModel(IDashboardPreferencesService prefs, INavigationService navigation)
    {
        _prefs = prefs;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        var current = await _prefs.GetAsync();
        _suppressSave = true;
        ShowGamification = current.ShowGamification;
        ShowDailyHealth = current.ShowDailyHealth;
        ShowQuests = current.ShowQuests;
        ShowProgressCards = current.ShowProgressCards;
        ShowStagesRoadmap = current.ShowStagesRoadmap;
        _suppressSave = false;
    }

    partial void OnShowGamificationChanged(bool value) => _ = SaveAsync();
    partial void OnShowDailyHealthChanged(bool value) => _ = SaveAsync();
    partial void OnShowQuestsChanged(bool value) => _ = SaveAsync();
    partial void OnShowProgressCardsChanged(bool value) => _ = SaveAsync();
    partial void OnShowStagesRoadmapChanged(bool value) => _ = SaveAsync();

    private async Task SaveAsync()
    {
        if (_suppressSave) return;
        await _prefs.SaveAsync(new DashboardPreferences
        {
            ShowGamification = ShowGamification,
            ShowDailyHealth = ShowDailyHealth,
            ShowQuests = ShowQuests,
            ShowProgressCards = ShowProgressCards,
            ShowStagesRoadmap = ShowStagesRoadmap,
        });
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        _suppressSave = true;
        var defaults = DashboardPreferences.Default;
        ShowGamification = defaults.ShowGamification;
        ShowDailyHealth = defaults.ShowDailyHealth;
        ShowQuests = defaults.ShowQuests;
        ShowProgressCards = defaults.ShowProgressCards;
        ShowStagesRoadmap = defaults.ShowStagesRoadmap;
        _suppressSave = false;
        await _prefs.SaveAsync(defaults);
    }

    [RelayCommand]
    private Task BackAsync() => _navigation.GoBackAsync();
}
