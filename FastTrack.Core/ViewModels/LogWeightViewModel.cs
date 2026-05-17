using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class LogWeightViewModel : ObservableObject
{
    private const double LbsPerKg = 2.20462262;

    private readonly IWeightService _weights;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UnitLabel))]
    [NotifyPropertyChangedFor(nameof(IsKgSelected))]
    [NotifyPropertyChangedFor(nameof(IsLbSelected))]
    private bool useLbs;

    public string UnitLabel => UseLbs ? "lb" : "kg";
    public bool IsKgSelected => !UseLbs;
    public bool IsLbSelected => UseLbs;

    [ObservableProperty] private string textValue = "70.0";
    [ObservableProperty] private DateTime entryDate = DateTime.Now.Date;
    [ObservableProperty] private TimeSpan entryTime = DateTime.Now.TimeOfDay;
    [ObservableProperty] private string note = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public LogWeightViewModel(IWeightService weights, INavigationService navigation)
    {
        _weights = weights;
        _navigation = navigation;
    }

    [RelayCommand]
    private void PickKg()
    {
        if (!UseLbs) return;
        if (TryParse(out var lbs)) TextValue = (lbs / LbsPerKg).ToString("0.0", CultureInfo.InvariantCulture);
        UseLbs = false;
    }

    [RelayCommand]
    private void PickLb()
    {
        if (UseLbs) return;
        if (TryParse(out var kg)) TextValue = (kg * LbsPerKg).ToString("0.0", CultureInfo.InvariantCulture);
        UseLbs = true;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        ErrorMessage = null;
        if (!TryParse(out var raw) || raw <= 0)
        {
            ErrorMessage = "Enter a positive number.";
            return;
        }
        var kg = UseLbs ? raw / LbsPerKg : raw;
        var localTs = EntryDate.Date + EntryTime;
        var utc = DateTime.SpecifyKind(localTs, DateTimeKind.Local).ToUniversalTime();
        try
        {
            await _weights.LogAsync(kg, utc, string.IsNullOrWhiteSpace(Note) ? null : Note);
            await _navigation.GoBackAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CancelAsync() => await _navigation.GoBackAsync();

    private bool TryParse(out double value) =>
        double.TryParse(TextValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
}
