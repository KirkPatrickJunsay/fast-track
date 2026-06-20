using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public interface IFastingService
{
    Task<Fast?> GetActiveAsync();

    Task<Fast> StartAsync(Guid protocolId, DateTime? startUtc = null);

    Task<Fast> EndAsync(Guid fastId, FastEndReason reason, DateTime? endUtc = null);

    Task<Fast> EditTimesAsync(Guid fastId, DateTime newStartUtc, DateTime? newEndUtc);

    /// <summary>
    /// Swap the protocol attached to an in-progress fast. Updates the GoalHours
    /// from the new protocol, reschedules stage notifications, and refreshes the
    /// live ticker. Throws if the fast is already ended or the protocol is unknown.
    /// </summary>
    Task<Fast> ChangeProtocolAsync(Guid fastId, Guid newProtocolId);

    /// <summary>
    /// Create a completed historical fast record — used for one-time migration
    /// from another fasting app. Does NOT raise FastCompleted (so gamification
    /// does not award XP/badges or fire the celebration page), but the record
    /// IS visible to the streak calculator so the user's streak history reflects
    /// reality. Validates startUtc &lt; endUtc and both times are in the past.
    /// </summary>
    Task<Fast> AddPastFastAsync(Guid protocolId, DateTime startUtc, DateTime endUtc, FastEndReason reason);

    /// <summary>Raised whenever a fast is completed. Epic 02 listens here for XP / badge / streak hooks.</summary>
    event EventHandler<Fast>? FastCompleted;
}
