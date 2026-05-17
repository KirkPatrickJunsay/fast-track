namespace FastTrack.Services.Interfaces;

public sealed record ImportResult(
    bool Success,
    string Message,
    int Fasts,
    int Weights,
    int Moods,
    int Water,
    int Badges,
    int Quests);

public interface IDataImportService
{
    /// <summary>
    /// Validates JSON, then transactionally replaces all data in the DB.
    /// Returns success + counts, or failure with a user-friendly message.
    /// </summary>
    Task<ImportResult> ApplyAsync(string json);
}
