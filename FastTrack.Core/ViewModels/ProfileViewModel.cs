using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

/// <summary>
/// Settings → Profile edit page. Two editable fields: display name and
/// experience level. Educational-mode toggle is intentionally NOT exposed
/// here — it's set by the medical safety screening during onboarding, and
/// flipping it casually would undermine the screening's purpose. Use Redo
/// onboarding for that path.
/// </summary>
public partial class ProfileViewModel : ObservableObject
{
    private readonly IUserProfileRepository _profiles;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private UserProfile? _profile;
    private bool _loaded;

    [ObservableProperty] private string displayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBeginnerSelected))]
    [NotifyPropertyChangedFor(nameof(IsIntermediateSelected))]
    [NotifyPropertyChangedFor(nameof(IsAdvancedSelected))]
    [NotifyPropertyChangedFor(nameof(LevelHint))]
    private ExperienceLevel experience = ExperienceLevel.Beginner;

    public bool IsBeginnerSelected => Experience == ExperienceLevel.Beginner;
    public bool IsIntermediateSelected => Experience == ExperienceLevel.Intermediate;
    public bool IsAdvancedSelected => Experience == ExperienceLevel.Advanced;

    public string LevelHint => Experience switch
    {
        ExperienceLevel.Beginner => "Gentle on-ramp. 16:8 and shorter fasts are well-tolerated and easy to sustain.",
        ExperienceLevel.Intermediate => "Comfortable with daily windows. 18:6 and the occasional 20:4 fit this range.",
        ExperienceLevel.Advanced => "Full library unlocked — OMAD, 5:2, and extended fasts are yours to choose from.",
        _ => string.Empty,
    };

    [ObservableProperty] private bool isSaving;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ProfileViewModel(
        IUserProfileRepository profiles,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _profiles = profiles;
        _dialogs = dialogs;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _profile = await _profiles.GetOrCreateAsync();
        DisplayName = _profile.DisplayName ?? string.Empty;
        Experience = _profile.Level;
        _loaded = true;
    }

    [RelayCommand] private void PickBeginner() => Experience = ExperienceLevel.Beginner;
    [RelayCommand] private void PickIntermediate() => Experience = ExperienceLevel.Intermediate;
    [RelayCommand] private void PickAdvanced() => Experience = ExperienceLevel.Advanced;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_profile is null || IsSaving) return;
        ErrorMessage = null;
        try
        {
            IsSaving = true;
            // Trim and normalize — blank name falls back to the friendly default
            // ("Faster") in the greeting code, so we just store null for blanks.
            var trimmed = DisplayName?.Trim();
            _profile.DisplayName = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
            _profile.Level = Experience;
            await _profiles.UpdateAsync(_profile);
            await _dialogs.ShowAlertAsync("Saved", "Profile updated.");
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync() => _navigation.GoBackAsync();
}
