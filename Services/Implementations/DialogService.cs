using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class DialogService : IDialogService
{
    private static Page? CurrentPage =>
        Application.Current?.Windows?.FirstOrDefault()?.Page;

    public async Task<string?> ShowActionSheetAsync(string title, string cancel, params string[] options)
    {
        var page = CurrentPage;
        if (page is null) return null;
        var result = await page.DisplayActionSheet(title, cancel, null, options);
        return result == cancel ? null : result;
    }

    public Task ShowAlertAsync(string title, string message, string ok = "OK") =>
        CurrentPage?.DisplayAlert(title, message, ok) ?? Task.CompletedTask;

    public async Task<bool> ConfirmAsync(string title, string message, string ok = "Yes", string cancel = "Cancel")
    {
        var page = CurrentPage;
        if (page is null) return false;
        return await page.DisplayAlert(title, message, ok, cancel);
    }
}
