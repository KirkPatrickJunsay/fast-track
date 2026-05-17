namespace FastTrack.Models;

public sealed record FastingStage(
    string Key,
    string Name,
    double StartHour,
    double? EndHour,
    string Summary,
    string IconAsset,
    string LongDescription,
    IReadOnlyList<string> Feelings)
{
    public bool Contains(double hours) =>
        hours >= StartHour && (EndHour is null || hours < EndHour);
}
