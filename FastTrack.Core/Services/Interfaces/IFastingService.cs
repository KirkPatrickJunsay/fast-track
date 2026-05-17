using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IFastingService
{
    Task<Fast?> GetActiveAsync();

    Task<Fast> StartAsync(Guid protocolId, DateTime? startUtc = null);

    Task<Fast> EndAsync(Guid fastId, FastEndReason reason, DateTime? endUtc = null);

    Task<Fast> EditTimesAsync(Guid fastId, DateTime newStartUtc, DateTime? newEndUtc);

    /// <summary>Raised whenever a fast is completed. Epic 02 listens here for XP / badge / streak hooks.</summary>
    event EventHandler<Fast>? FastCompleted;
}
