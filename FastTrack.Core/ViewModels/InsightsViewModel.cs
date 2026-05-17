using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.ViewModels;

public partial class InsightsViewModel : ObservableObject
{
    private readonly IAnalyticsService _analytics;

    [ObservableProperty] private bool isLoading;

    // Personal bests
    [ObservableProperty] private string longestFastDisplay = "—";
    [ObservableProperty] private string longestStreakDisplay = "—";
    [ObservableProperty] private string mostFastsInMonthDisplay = "—";
    [ObservableProperty] private string highestWeeklyHoursDisplay = "—";
    [ObservableProperty] private string totalCompletedDisplay = "—";

    // Weekly summary
    [ObservableProperty] private string weeklyHoursDisplay = "0h";
    [ObservableProperty] private string weeklyHoursLastDisplay = "0h";
    [ObservableProperty] private string weeklyDeltaDisplay = "—";
    [ObservableProperty] private string weeklyFastsDisplay = "—";
    [ObservableProperty] private string weeklyStreakDisplay = "—";
    [ObservableProperty] private string weeklyBadgesDisplay = "—";
    [ObservableProperty] private string weeklyHighlight = "No data yet.";

    // Heatmap raw data
    [ObservableProperty] private IReadOnlyList<HeatmapDay> heatmapDays = Array.Empty<HeatmapDay>();
    [ObservableProperty] private string heatmapRangeText = "Last 365 days";

    // Range selector (Slice 2)
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsRange7))]
    [NotifyPropertyChangedFor(nameof(IsRange30))]
    [NotifyPropertyChangedFor(nameof(IsRange90))]
    [NotifyPropertyChangedFor(nameof(IsRange365))]
    [NotifyPropertyChangedFor(nameof(IsRangeAll))]
    private TimeRange selectedRange = TimeRange.Days30;

    public bool IsRange7 => SelectedRange == TimeRange.Days7;
    public bool IsRange30 => SelectedRange == TimeRange.Days30;
    public bool IsRange90 => SelectedRange == TimeRange.Days90;
    public bool IsRange365 => SelectedRange == TimeRange.Days365;
    public bool IsRangeAll => SelectedRange == TimeRange.All;

    // Series data — consumed by the InsightsPage code-behind to build Microcharts entries.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasFastDuration))]
    private IReadOnlyList<ChartPoint> fastDurationSeries = Array.Empty<ChartPoint>();
    public bool HasFastDuration => FastDurationSeries.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWeekly))]
    private IReadOnlyList<ChartPoint> weeklyHoursSeries = Array.Empty<ChartPoint>();
    public bool HasWeekly => WeeklyHoursSeries.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWeight))]
    private IReadOnlyList<ChartPoint> weightSeries = Array.Empty<ChartPoint>();
    public bool HasWeight => WeightSeries.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasWater))]
    private IReadOnlyList<ChartPoint> waterDailySeries = Array.Empty<ChartPoint>();
    public bool HasWater => WaterDailySeries.Count > 0;

    public InsightsViewModel(IAnalyticsService analytics) => _analytics = analytics;

    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var bests = await _analytics.GetPersonalBestsAsync();
            LongestFastDisplay = FormatHours(bests.LongestFast.TotalHours);
            LongestStreakDisplay = $"{bests.LongestStreak} days";
            MostFastsInMonthDisplay = $"{bests.MostFastsInMonth} fasts";
            HighestWeeklyHoursDisplay = $"{bests.HighestWeeklyHours:0.#}h";
            TotalCompletedDisplay = $"{bests.TotalCompletedFasts} completed";

            var summary = await _analytics.GetWeeklySummaryAsync();
            WeeklyHoursDisplay = $"{summary.HoursThisWeek:0.#}h";
            WeeklyHoursLastDisplay = $"{summary.HoursLastWeek:0.#}h last week";
            WeeklyDeltaDisplay = summary.DeltaHours switch
            {
                > 0 => $"+{summary.DeltaHours:0.#}h vs last week",
                < 0 => $"{summary.DeltaHours:0.#}h vs last week",
                _ => "Flat vs last week",
            };
            WeeklyFastsDisplay = $"{summary.FastsThisWeek} fasts";
            WeeklyStreakDisplay = $"{summary.CurrentStreak} day streak";
            WeeklyBadgesDisplay = summary.BadgesThisWeek == 1 ? "1 badge" : $"{summary.BadgesThisWeek} badges";
            WeeklyHighlight = summary.Highlight;

            HeatmapDays = await _analytics.GetHeatmapAsync(365);
            HeatmapRangeText = $"Last 365 days · {HeatmapDays.Count(d => d.Intensity > 0)} active";

            await RefreshChartsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SelectRangeAsync(string range)
    {
        var parsed = range switch
        {
            "7" => TimeRange.Days7,
            "30" => TimeRange.Days30,
            "90" => TimeRange.Days90,
            "365" => TimeRange.Days365,
            "all" => TimeRange.All,
            _ => TimeRange.Days30,
        };
        if (parsed == SelectedRange) return;
        SelectedRange = parsed;
        await RefreshChartsAsync();
    }

    private async Task RefreshChartsAsync()
    {
        FastDurationSeries = await _analytics.GetFastDurationSeriesAsync(SelectedRange);
        WeeklyHoursSeries = await _analytics.GetWeeklyFastingHoursAsync(SelectedRange);
        WeightSeries = await _analytics.GetWeightSeriesAsync(SelectedRange);
        WaterDailySeries = await _analytics.GetWaterDailyAsync(SelectedRange);
    }

    private static string FormatHours(double hours)
    {
        var h = (int)Math.Floor(hours);
        var m = (int)Math.Round((hours - h) * 60);
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }
}
