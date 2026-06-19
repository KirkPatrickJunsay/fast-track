namespace FastTrack.Services.Interfaces;

public interface IFileShareService
{
    /// <summary>
    /// Writes content to a temp file and invokes the native share sheet so the user
    /// chooses where to save / send. Caller owns nothing afterwards.
    /// </summary>
    Task ShareTextFileAsync(string fileName, string content, string title = "Share");

    /// <summary>
    /// Invokes the native share sheet with an in-memory text payload (no file written).
    /// </summary>
    Task ShareTextAsync(string title, string text);
}
