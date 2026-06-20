using System.Collections.ObjectModel;
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
    private readonly IFastingProtocolRepository _protocolsRepo;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    private Fast? _fast;
    private Guid _initialProtocolId;
    private bool _loaded;

    public string FastId { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsActiveFast))]
    private bool isPastFast;
    public bool IsActiveFast => !IsPastFast;

    [ObservableProperty] private DateTime startDate = DateTime.Today;
    [ObservableProperty] private TimeSpan startTime = TimeSpan.Zero;
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    [ObservableProperty] private TimeSpan endTime = TimeSpan.Zero;

    /// <summary>
    /// All protocols available for swap on an in-progress fast. Populated from
    /// the catalog on Load; the UI binds a Picker to this list.
    /// </summary>
    public ObservableCollection<FastingProtocol> Protocols { get; } = new();

    [ObservableProperty] private FastingProtocol? selectedProtocol;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public EditFastViewModel(
        IFastingService fasting,
        IFastRepository fasts,
        IFastingProtocolRepository protocolsRepo,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _fasting = fasting;
        _fasts = fasts;
        _protocolsRepo = protocolsRepo;
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

        // Populate the protocol picker. Only meaningful for active fasts —
        // protocol-swap is intentionally a live-fast feature.
        if (!IsPastFast)
        {
            Protocols.Clear();
            foreach (var p in await _protocolsRepo.GetAllAsync())
            {
                Protocols.Add(p);
            }
            _initialProtocolId = _fast.ProtocolId;
            SelectedProtocol = Protocols.FirstOrDefault(p => p.Id == _fast.ProtocolId);
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

            // If the user picked a different protocol on an active fast, apply it.
            // Times save first so audit trail (OriginalStartUtc) is captured against
            // the original protocol; protocol swap then updates GoalHours + reschedules
            // notifications + refreshes the live ticker.
            if (!IsPastFast
                && SelectedProtocol is not null
                && SelectedProtocol.Id != _initialProtocolId)
            {
                await _fasting.ChangeProtocolAsync(_fast.Id, SelectedProtocol.Id);
                _initialProtocolId = SelectedProtocol.Id;
            }

            await _dialogs.ShowAlertAsync("Saved", IsPastFast ? "Fast times updated." : "Fast updated.");
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
