using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class LogMoodViewModel : ObservableObject
{
    private readonly IMoodService _moods;
    private readonly INavigationService _navigation;

    /// <summary>Optional Guid string of the linked fast (Shell query param).</summary>
    public string? FastId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLevel1Selected))]
    [NotifyPropertyChangedFor(nameof(IsLevel2Selected))]
    [NotifyPropertyChangedFor(nameof(IsLevel3Selected))]
    [NotifyPropertyChangedFor(nameof(IsLevel4Selected))]
    [NotifyPropertyChangedFor(nameof(IsLevel5Selected))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private int selectedLevel; // 0 = none

    public bool IsLevel1Selected => SelectedLevel == 1;
    public bool IsLevel2Selected => SelectedLevel == 2;
    public bool IsLevel3Selected => SelectedLevel == 3;
    public bool IsLevel4Selected => SelectedLevel == 4;
    public bool IsLevel5Selected => SelectedLevel == 5;

    public bool CanSave => SelectedLevel is >= 1 and <= 5;

    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public LogMoodViewModel(IMoodService moods, INavigationService navigation)
    {
        _moods = moods;
        _navigation = navigation;
    }

    [RelayCommand]
    private void PickLevel(string level)
    {
        if (int.TryParse(level, out var n) && n is >= 1 and <= 5) SelectedLevel = n;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (!CanSave) { ErrorMessage = "Pick a mood first."; return; }
        try
        {
            Guid? linked = null;
            if (Guid.TryParse(FastId, out var g)) linked = g;
            await _moods.LogAsync(SelectedLevel, fastId: linked, note: string.IsNullOrWhiteSpace(Note) ? null : Note);
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await _navigation.GoBackAsync();
}
