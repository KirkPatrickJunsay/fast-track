using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IUserProfileRepository _profiles;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    [ObservableProperty] private string profileSummary = "—";
    [ObservableProperty] private string versionText = "Fast Track 0.1.0 · local-only";

    public SettingsViewModel(IUserProfileRepository profiles, IDialogService dialogs, INavigationService navigation)
    {
        _profiles = profiles;
        _dialogs = dialogs;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        var p = await _profiles.GetOrCreateAsync();
        var name = string.IsNullOrWhiteSpace(p.DisplayName) ? "Faster" : p.DisplayName;
        var level = p.Level.ToString();
        var mode = p.IsEducationalMode ? "Educational mode" : "Full timer access";
        ProfileSummary = $"{name} · {level} · {mode}";
    }

    [RelayCommand]
    private async Task OpenProfileAsync() =>
        await _navigation.GoToAsync("ProfilePage");

    [RelayCommand]
    private async Task OpenNotificationsAsync() =>
        await _navigation.GoToAsync("NotificationPreferencesPage");

    [RelayCommand]
    private async Task OpenTrophiesAsync() =>
        await _navigation.GoToAsync("TrophyCabinetPage");

    [RelayCommand]
    private async Task OpenDataAsync() =>
        await _navigation.GoToAsync("DataManagementPage");

    [RelayCommand]
    private async Task OpenPrivacyAsync() =>
        await _navigation.GoToAsync("PrivacyPage");

    [RelayCommand]
    private async Task OpenCustomizeHomeAsync() =>
        await _navigation.GoToAsync("CustomizeHomePage");

    [RelayCommand]
    private async Task RedoOnboardingAsync()
    {
        var confirm = await _dialogs.ConfirmAsync(
            "Redo onboarding?",
            "This will reopen the onboarding flow. Your fasts and data are kept.",
            "Yes, redo",
            "Cancel");
        if (!confirm) return;

        var p = await _profiles.GetOrCreateAsync();
        p.OnboardingCompleted = false;
        await _profiles.UpdateAsync(p);

        await _navigation.GoToAsync("//OnboardingPage");
    }
}
