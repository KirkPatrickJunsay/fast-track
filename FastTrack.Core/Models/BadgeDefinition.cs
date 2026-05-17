namespace FastTrack.Models;

/// <summary>
/// Static, in-app-bundled badge metadata. Earning state lives in <see cref="EarnedBadge"/>.
/// </summary>
public sealed record BadgeDefinition(
    string Key,
    string Name,
    string Description,
    string IconAsset,
    bool IsHidden = false);
