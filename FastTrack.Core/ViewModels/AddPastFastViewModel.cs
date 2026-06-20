using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

/// <summary>
/// "Add past fast" entry form used to migrate historical records from another
/// fasting app. Defaults are conservative: yesterday's window with the user's
/// most recent protocol selected and reason=Completed, so the typical "I fasted
/// 16:8 yesterday" entry takes two taps (open the page, tap Save).
/// </summary>
public partial class AddPastFastViewModel : ObservableObject
{
    private readonly IFastingService _fasting;
    private readonly IFastingProtocolRepository _protocolsRepo;
    private readonly IUserProfileRepository _profiles;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    public ObservableCollection<FastingProtocol> Protocols { get; } = new();

    public IReadOnlyList<FastReasonChoice> ReasonChoices { get; } = new[]
    {
        new FastReasonChoice("Completed (reached goal)", FastEndReason.Completed),
        new FastReasonChoice("Ended early — hungry", FastEndReason.Hungry),
        new FastReasonChoice("Ended early — social event", FastEndReason.SocialEvent),
        new FastReasonChoice("Ended early — felt unwell", FastEndReason.Illness),
        new FastReasonChoice("Ended early — other", FastEndReason.Other),
    };

    [ObservableProperty] private DateTime startDate = DateTime.Today.AddDays(-1);
    [ObservableProperty] private TimeSpan startTime = new(20, 0, 0); // 8pm previous day
    [ObservableProperty] private DateTime endDate = DateTime.Today;
    [ObservableProperty] private TimeSpan endTime = new(12, 0, 0);   // noon today
    [ObservableProperty] private FastingProtocol? selectedProtocol;
    [ObservableProperty] private FastReasonChoice? selectedReason;
    [ObservableProperty] private bool isSaving;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public AddPastFastViewModel(
        IFastingService fasting,
        IFastingProtocolRepository protocolsRepo,
        IUserProfileRepository profiles,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _fasting = fasting;
        _protocolsRepo = protocolsRepo;
        _profiles = profiles;
        _dialogs = dialogs;
        _navigation = navigation;
        SelectedReason = ReasonChoices[0];
    }

    public async Task LoadAsync()
    {
        if (Protocols.Count > 0) return; // idempotent — protocols don't change while page is open
        foreach (var p in await _protocolsRepo.GetAllAsync())
        {
            Protocols.Add(p);
        }

        // Preselect the user's most-recently-used protocol if we can find it;
        // otherwise just take the first in the list.
        try
        {
            var profile = await _profiles.GetOrCreateAsync();
            SelectedProtocol = Protocols.FirstOrDefault(p => p.Id == profile.LastUsedProtocolId)
                            ?? Protocols.FirstOrDefault();
        }
        catch
        {
            SelectedProtocol = Protocols.FirstOrDefault();
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsSaving) return;
        ErrorMessage = null;

        if (SelectedProtocol is null)
        {
            ErrorMessage = "Pick a protocol.";
            return;
        }
        if (SelectedReason is null)
        {
            ErrorMessage = "Pick a reason.";
            return;
        }

        try
        {
            IsSaving = true;
            var startLocal = StartDate.Date + StartTime;
            var endLocal = EndDate.Date + EndTime;
            var startUtc = DateTime.SpecifyKind(startLocal, DateTimeKind.Local).ToUniversalTime();
            var endUtc = DateTime.SpecifyKind(endLocal, DateTimeKind.Local).ToUniversalTime();

            await _fasting.AddPastFastAsync(SelectedProtocol.Id, startUtc, endUtc, SelectedReason.Reason);

            await _dialogs.ShowAlertAsync("Saved", "Past fast added to your history.");
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

/// <summary>Lightweight pairing of display label + the enum value the service expects.</summary>
public sealed record FastReasonChoice(string Label, FastEndReason Reason);
