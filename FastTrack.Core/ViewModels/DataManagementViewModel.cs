using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class DataManagementViewModel : ObservableObject
{
    private readonly IDataExportService _export;
    private readonly IDataImportService _import;
    private readonly IDataResetService _reset;
    private readonly IFileShareService _share;
    private readonly IFilePickerService _picker;
    private readonly IDialogService _dialogs;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatus))]
    private string? statusMessage;
    public bool HasStatus => !string.IsNullOrEmpty(StatusMessage);

    [ObservableProperty] private bool isBusy;

    public DataManagementViewModel(
        IDataExportService export,
        IDataImportService import,
        IDataResetService reset,
        IFileShareService share,
        IFilePickerService picker,
        IDialogService dialogs,
        INavigationService navigation)
    {
        _export = export;
        _import = import;
        _reset = reset;
        _share = share;
        _picker = picker;
        _dialogs = dialogs;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = null;
        try
        {
            var json = await _export.BuildJsonAsync();
            var fileName = $"fasttrack-export-{DateTime.Now:yyyy-MM-dd-HHmm}.json";
            await _share.ShareTextFileAsync(fileName, json, "Export FastTrack data");
            StatusMessage = $"Built {json.Length:N0} bytes. Pick a destination in the share sheet.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ImportAsync()
    {
        if (IsBusy) return;

        var picked = await _picker.PickJsonAsync();
        if (picked is null) return;

        var confirm = await _dialogs.ConfirmAsync(
            "Replace your data?",
            $"This will replace ALL data on this device with the contents of {picked.FileName}. This cannot be undone.",
            "Replace",
            "Cancel");
        if (!confirm) return;

        IsBusy = true;
        StatusMessage = null;
        try
        {
            var result = await _import.ApplyAsync(picked.Contents);
            if (result.Success)
            {
                StatusMessage = $"Imported {result.Fasts} fasts · {result.Weights} weights · {result.Moods} moods · {result.Water} water · {result.Badges} badges · {result.Quests} quests.";
            }
            else
            {
                StatusMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResetAsync()
    {
        if (IsBusy) return;

        var first = await _dialogs.ConfirmAsync(
            "Reset all data?",
            "Wipes fasts, gamification, health logs and your profile. Preset protocols remain. This cannot be undone.",
            "Continue",
            "Cancel");
        if (!first) return;

        var second = await _dialogs.ConfirmAsync(
            "Are you sure?",
            "Last chance — tap Reset to permanently erase your data on this device.",
            "Reset",
            "Cancel");
        if (!second) return;

        IsBusy = true;
        StatusMessage = null;
        try
        {
            await _reset.ResetAllAsync();
            StatusMessage = "All data has been reset. Reopen the app to start fresh.";
            // Routing back to onboarding will trigger naturally once the profile is gone.
            await _navigation.GoToAsync("//OnboardingPage");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Reset failed: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task BackAsync() => await _navigation.GoBackAsync();
}
