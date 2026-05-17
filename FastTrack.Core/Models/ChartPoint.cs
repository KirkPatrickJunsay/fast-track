namespace FastTrack.Models;

/// <summary>
/// One value in a time series. <paramref name="Date"/> is the local-time anchor for x-axis ordering.
/// </summary>
public sealed record ChartPoint(DateTime Date, double Value, string Label, string? ValueLabel = null);
