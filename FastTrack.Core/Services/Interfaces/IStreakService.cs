using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record StreakResult(
    int Current,
    int Longest,
    bool IncrementedToday,
    bool FreezeConsumed,
    bool Broken);

public sealed record StreakSnapshot(int Current, int Longest, int FreezesAvailable);

public interface IStreakService
{
    Task<StreakSnapshot> GetSnapshotAsync();
    Task<StreakResult> RecordCompletedFastAsync(Fast completed, FastingProtocol protocol);
}
