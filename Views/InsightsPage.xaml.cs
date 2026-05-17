using FastTrack.Helpers;
using FastTrack.Models;
using FastTrack.ViewModels;
using Microcharts;
using SkiaSharp;

namespace FastTrack.Views;

public partial class InsightsPage : ContentPage
{
    private readonly InsightsViewModel _vm;
    private readonly HeatmapDrawable _drawable = new();

    // Design-system aligned chart colors.
    private static readonly SKColor PrimaryColor = SKColor.Parse("#C4C0FF");
    private static readonly SKColor SecondaryColor = SKColor.Parse("#A9D38B");
    private static readonly SKColor LabelColor = SKColor.Parse("#C8C4D6");
    private static readonly SKColor SurfaceColor = SKColor.Parse("#1C1B1B");

    public InsightsPage(InsightsViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        HeatmapView.Drawable = _drawable;
        _vm.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(InsightsViewModel.HeatmapDays):
                    _drawable.Days = _vm.HeatmapDays;
                    HeatmapView.Invalidate();
                    break;
                case nameof(InsightsViewModel.FastDurationSeries):
                    FastDurationChart.Chart = BuildLine(_vm.FastDurationSeries, PrimaryColor);
                    break;
                case nameof(InsightsViewModel.WeeklyHoursSeries):
                    WeeklyHoursChart.Chart = BuildBar(_vm.WeeklyHoursSeries, PrimaryColor);
                    break;
                case nameof(InsightsViewModel.WeightSeries):
                    WeightChart.Chart = BuildLine(_vm.WeightSeries, SecondaryColor);
                    break;
                case nameof(InsightsViewModel.WaterDailySeries):
                    WaterChart.Chart = BuildBar(_vm.WaterDailySeries, SecondaryColor);
                    break;
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadAsync();
        _drawable.Days = _vm.HeatmapDays;
        HeatmapView.Invalidate();
        FastDurationChart.Chart = BuildLine(_vm.FastDurationSeries, PrimaryColor);
        WeeklyHoursChart.Chart = BuildBar(_vm.WeeklyHoursSeries, PrimaryColor);
        WeightChart.Chart = BuildLine(_vm.WeightSeries, SecondaryColor);
        WaterChart.Chart = BuildBar(_vm.WaterDailySeries, SecondaryColor);
    }

    private static LineChart BuildLine(IReadOnlyList<ChartPoint> points, SKColor color) => new()
    {
        Entries = ToEntries(points, color),
        LabelTextSize = 22,
        LabelColor = LabelColor,
        BackgroundColor = SurfaceColor,
        LineMode = LineMode.Straight,
        LineSize = 4,
        PointSize = 8,
        PointMode = PointMode.Circle,
        Margin = 16,
    };

    private static BarChart BuildBar(IReadOnlyList<ChartPoint> points, SKColor color) => new()
    {
        Entries = ToEntries(points, color),
        LabelTextSize = 22,
        LabelColor = LabelColor,
        BackgroundColor = SurfaceColor,
        Margin = 16,
    };

    private static IEnumerable<ChartEntry> ToEntries(IReadOnlyList<ChartPoint> points, SKColor color)
    {
        if (points.Count == 0)
        {
            return new[] { new ChartEntry(0) { Color = color, ValueLabelColor = LabelColor } };
        }
        // Microcharts crowds labels for long series — only label first / mid / last.
        var n = points.Count;
        return points.Select((p, i) =>
        {
            var showLabel = n <= 7 || i == 0 || i == n - 1 || i == n / 2;
            return new ChartEntry((float)p.Value)
            {
                Color = color,
                Label = showLabel ? p.Label : string.Empty,
                ValueLabel = p.ValueLabel,
                ValueLabelColor = LabelColor,
                TextColor = LabelColor,
            };
        }).ToArray();
    }
}
