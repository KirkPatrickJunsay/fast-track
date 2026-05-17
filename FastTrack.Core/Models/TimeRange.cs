namespace FastTrack.Models;

public enum TimeRange
{
    Days7,
    Days30,
    Days90,
    Days365,
    All,
}

public static class TimeRangeExtensions
{
    /// <summary>Days back from now. <c>All</c> returns a sentinel of <see cref="int.MaxValue"/>.</summary>
    public static int Days(this TimeRange r) => r switch
    {
        TimeRange.Days7 => 7,
        TimeRange.Days30 => 30,
        TimeRange.Days90 => 90,
        TimeRange.Days365 => 365,
        TimeRange.All => int.MaxValue,
        _ => 30,
    };

    public static string ShortLabel(this TimeRange r) => r switch
    {
        TimeRange.Days7 => "7d",
        TimeRange.Days30 => "30d",
        TimeRange.Days90 => "90d",
        TimeRange.Days365 => "1y",
        TimeRange.All => "All",
        _ => "30d",
    };
}
