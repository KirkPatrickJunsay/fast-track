namespace FastTrack.Services.Interfaces;

public sealed record PickedFile(string FileName, string Contents);

public interface IFilePickerService
{
    /// <summary>Opens the native file picker and returns the picked file's text contents, or null if cancelled.</summary>
    Task<PickedFile?> PickJsonAsync();
}
