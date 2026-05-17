namespace FastTrack.ViewModels;

public sealed class HistoryItemViewModel
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string DurationDisplay { get; init; } = string.Empty;
    public string GoalDisplay { get; init; } = string.Empty;
    public string EndReasonDisplay { get; init; } = string.Empty;
    public string StartedLocal { get; init; } = string.Empty;
    public string EndedLocal { get; init; } = string.Empty;
    public bool GoalMet { get; init; }
}
