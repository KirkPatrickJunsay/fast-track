using FastTrack.Models;

namespace FastTrack.Services.Interfaces;

public sealed record EvaluatedBadge(BadgeDefinition Definition, bool IsEarned, DateTime? EarnedAtUtc);

public interface IBadgeService
{
    IReadOnlyList<BadgeDefinition> Definitions { get; }

    Task<IReadOnlyList<EvaluatedBadge>> GetAllAsync();

    /// <summary>Returns the badge keys newly earned by this event.</summary>
    Task<IReadOnlyList<BadgeDefinition>> EvaluateOnFastCompletedAsync(Fast fast, FastingProtocol protocol);
}
