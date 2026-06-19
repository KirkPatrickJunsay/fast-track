namespace FastTrack.Models;

/// <summary>
/// A read-only education article. Sourced from the in-memory catalog —
/// no user state, so it's safe to keep as a record with init-only members.
/// </summary>
public sealed record Article(
    string Id,
    string Title,
    string Summary,
    string HeroAsset,
    int ReadingMinutes,
    IReadOnlyList<ArticleSection> Sections);

/// <summary>
/// A single sub-section of an article. Heading is optional; when null the
/// section is rendered as a continuation paragraph.
/// </summary>
public sealed record ArticleSection(string? Heading, string Body);
