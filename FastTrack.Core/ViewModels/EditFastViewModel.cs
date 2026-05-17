using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class EditFastViewModel : ObservableObject
{
    private readonly IFastingService _fasting;
    private readonly IFastRepository _fasts;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private Fast? _fast;
    private bool _loaded;

    public string FastId { get; set; } = string.Empty;

    [ObservableProperty] private bool isPastFast;
    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private TimeSpan startTime = TimeSpan.Zero;
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    [ObservableProperty] private TimeSpan endTime = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public EditFastViewModel(
        IFastingService fasting,
        IFastRepository fasts,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _fasting = fasting;
        _fasts = fasts;
        _dialogs = dialogs;
        _navigation = navigation;
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        if (!Guid.TryParse(FastId, out var id))
        {
            ErrorMessage = "Invalid fast id.";
            return;
        }

        _fast = await _fasts.GetByIdAsync(id);
        if (_fast is null)
        {
            ErrorMessage = "Fast not found.";
            return;
        }

        var startLocal = _fast.StartUtc.ToLocalTime();
        StartDate = startLocal.Date;
        StartTime = startLocal.TimeOfDay;

        IsPastFast = _fast.EndUtc is not null;
        if (_fast.EndUtc is { } endUtc)
        {
            var endLocal = endUtc.ToLocalTime();
            EndDate = endLocal.Date;
            EndTime = endLocal.TimeOfDay;
        }
        _loaded = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_fast is null) return;
        ErrorMessage = null;
        try
        {
            var newStartLocal = StartDate.Date + StartTime;
            var newStartUtc = DateTime.SpecifyKind(newStartLocal, DateTimeKind.Local).ToUniversalTime();

            DateTime? newEndUtc = null;
            if (IsPastFast)
            {
                var newEndLocal = EndDate.Date + EndTime;
                newEndUtc = DateTime.SpecifyKind(newEndLocal, DateTimeKind.Local).ToUniversalTime();
            }

            await _fasting.EditTimesAsync(_fast.Id, newStartUtc, newEndUtc);
            await _dialogs.ShowAlertAsync("Saved", "Fast times updated.");
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
