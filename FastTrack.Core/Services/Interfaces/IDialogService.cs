namespace FastTrack.Services.Interfaces;

public interface IDialogService
{
    Task<string?> ShowActionSheetAsync(string title, string cancel, params string[] options);
    Task ShowAlertAsync(string title, string message, string ok = "OK");
    Task<bool> ConfirmAsync(string title, string message, string ok = "Yes", string cancel = "Cancel");
}
