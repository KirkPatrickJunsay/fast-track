namespace FastTrack.Models;

/// <summary>
/// One cell of the fasting heatmap. Intensity:
///   0 = no completed fast that day
///   1 = attempted but ended early (EndReason != Completed)
///   2 = goal met (hours within ±20% of goal)
///   3 = goal exceeded (hours ≥ 1.2× goal)
/// </summary>
public sealed record HeatmapDay(DateTime LocalDate, int Intensity);
