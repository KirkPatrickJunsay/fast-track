using FastTrack.Models;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.ViewModels;

public class InsightsViewModelTests
{
    private static (InsightsViewModel Vm, Mock<IAnalyticsService> Analytics) Build()
    {
        var analytics = new Mock<IAnalyticsService>();
        analytics.Setup(a => a.GetHeatmapAsync(It.IsAny<int>())).ReturnsAsync(Array.Empty<HeatmapDay>());
        analytics.Setup(a => a.GetPersonalBestsAsync())
                 .ReturnsAsync(new PersonalBests(TimeSpan.Zero, 0, 0, 0, 0));
        analytics.Setup(a => a.GetWeeklySummaryAsync())
                 .ReturnsAsync(new WeeklySummary(0, 0, 0, 0, 0, 0, "No fasts this week yet. Whenever you're ready."));
        analytics.Setup(a => a.GetFastDurationSeriesAsync(It.IsAny<TimeRange>())).ReturnsAsync(Array.Empty<ChartPoint>());
        analytics.Setup(a => a.GetWeeklyFastingHoursAsync(It.IsAny<TimeRange>())).ReturnsAsync(Array.Empty<ChartPoint>());
        analytics.Setup(a => a.GetWeightSeriesAsync(It.IsAny<TimeRange>())).ReturnsAsync(Array.Empty<ChartPoint>());
        analytics.Setup(a => a.GetWaterDailyAsync(It.IsAny<TimeRange>())).ReturnsAsync(Array.Empty<ChartPoint>());
        return (new InsightsViewModel(analytics.Object), analytics);
    }

    [Fact]
    public async Task LoadAsync_empty_state_displays_dashes_and_zeros()
    {
        var (vm, _) = Build();
        await vm.LoadAsync();
        vm.LongestFastDisplay.Should().Be("0h");
        vm.WeeklyHoursDisplay.Should().Be("0h");
        vm.WeeklyHighlight.Should().Contain("No fasts");
        vm.HeatmapRangeText.Should().Contain("0 active");
    }

    [Fact]
    public async Task LoadAsync_populates_personal_bests_and_summary()
    {
        var (vm, analytics) = Build();
        analytics.Setup(a => a.GetPersonalBestsAsync())
                 .ReturnsAsync(new PersonalBests(TimeSpan.FromHours(48.5), 23, 6, 80.0, 41));
        analytics.Setup(a => a.GetWeeklySummaryAsync())
                 .ReturnsAsync(new WeeklySummary(28, 16, 12, 3, 5, 2, "Your week looks strong."));

        await vm.LoadAsync();

        vm.LongestFastDisplay.Should().Be("48h 30m");
        vm.LongestStreakDisplay.Should().Be("23 days");
        vm.MostFastsInMonthDisplay.Should().Be("6 fasts");
        vm.HighestWeeklyHoursDisplay.Should().Be("80h");
        vm.TotalCompletedDisplay.Should().Be("41 completed");

        vm.WeeklyHoursDisplay.Should().Be("28h");
        vm.WeeklyHoursLastDisplay.Should().Contain("16h last week");
        vm.WeeklyDeltaDisplay.Should().StartWith("+12");
        vm.WeeklyFastsDisplay.Should().Be("3 fasts");
        vm.WeeklyStreakDisplay.Should().Be("5 day streak");
        vm.WeeklyBadgesDisplay.Should().Be("2 badges");
        vm.WeeklyHighlight.Should().Be("Your week looks strong.");
    }

    [Fact]
    public async Task LoadAsync_negative_delta_renders_with_minus_sign()
    {
        var (vm, analytics) = Build();
        analytics.Setup(a => a.GetWeeklySummaryAsync())
                 .ReturnsAsync(new WeeklySummary(10, 16, -6, 2, 2, 0, "Lighter week."));
        await vm.LoadAsync();
        vm.WeeklyDeltaDisplay.Should().StartWith("-6");
    }

    [Fact]
    public async Task LoadAsync_zero_delta_renders_flat()
    {
        var (vm, analytics) = Build();
        analytics.Setup(a => a.GetWeeklySummaryAsync())
                 .ReturnsAsync(new WeeklySummary(12, 12, 0, 2, 2, 0, "Steady week."));
        await vm.LoadAsync();
        vm.WeeklyDeltaDisplay.Should().Contain("Flat");
    }

    [Fact]
    public async Task LoadAsync_singular_badge_label()
    {
        var (vm, analytics) = Build();
        analytics.Setup(a => a.GetWeeklySummaryAsync())
                 .ReturnsAsync(new WeeklySummary(8, 0, 8, 1, 1, 1, "."));
        await vm.LoadAsync();
        vm.WeeklyBadgesDisplay.Should().Be("1 badge");
    }

    [Fact]
    public async Task LoadAsync_heatmap_reports_active_count()
    {
        var (vm, analytics) = Build();
        var today = DateTime.Now.Date;
        analytics.Setup(a => a.GetHeatmapAsync(It.IsAny<int>())).ReturnsAsync(new HeatmapDay[]
        {
            new(today.AddDays(-2), 2),
            new(today.AddDays(-1), 0),
            new(today, 3),
        });
        await vm.LoadAsync();
        vm.HeatmapDays.Should().HaveCount(3);
        vm.HeatmapRangeText.Should().Contain("2 active");
    }

    [Fact]
    public async Task LoadAsync_defaults_to_30_day_range_and_pulls_charts()
    {
        var (vm, analytics) = Build();
        await vm.LoadAsync();
        vm.SelectedRange.Should().Be(TimeRange.Days30);
        vm.IsRange30.Should().BeTrue();
        analytics.Verify(a => a.GetFastDurationSeriesAsync(TimeRange.Days30), Times.Once);
        analytics.Verify(a => a.GetWeeklyFastingHoursAsync(TimeRange.Days30), Times.Once);
        analytics.Verify(a => a.GetWeightSeriesAsync(TimeRange.Days30), Times.Once);
        analytics.Verify(a => a.GetWaterDailyAsync(TimeRange.Days30), Times.Once);
    }

    [Theory]
    [InlineData("7", TimeRange.Days7)]
    [InlineData("30", TimeRange.Days30)]
    [InlineData("90", TimeRange.Days90)]
    [InlineData("365", TimeRange.Days365)]
    [InlineData("all", TimeRange.All)]
    public async Task SelectRange_parses_each_token(string token, TimeRange expected)
    {
        var (vm, analytics) = Build();
        await vm.LoadAsync();
        await vm.SelectRangeCommand.ExecuteAsync(token);
        vm.SelectedRange.Should().Be(expected);
        if (expected != TimeRange.Days30)
        {
            analytics.Verify(a => a.GetFastDurationSeriesAsync(expected), Times.Once);
            analytics.Verify(a => a.GetWaterDailyAsync(expected), Times.Once);
        }
    }

    [Fact]
    public async Task SelectRange_same_range_does_not_refresh()
    {
        var (vm, analytics) = Build();
        await vm.LoadAsync();
        await vm.SelectRangeCommand.ExecuteAsync("30");
        analytics.Verify(a => a.GetFastDurationSeriesAsync(It.IsAny<TimeRange>()), Times.Once); // only the initial load
    }

    [Fact]
    public async Task SelectRange_unknown_token_falls_back_to_30()
    {
        var (vm, _) = Build();
        await vm.LoadAsync();
        await vm.SelectRangeCommand.ExecuteAsync("nonsense");
        vm.SelectedRange.Should().Be(TimeRange.Days30); // unchanged
    }

    [Fact]
    public async Task Has_flags_reflect_series_count()
    {
        var (vm, analytics) = Build();
        analytics.Setup(a => a.GetFastDurationSeriesAsync(It.IsAny<TimeRange>()))
                 .ReturnsAsync(new[] { new ChartPoint(DateTime.Today, 16, "5/1", "16h") });
        analytics.Setup(a => a.GetWaterDailyAsync(It.IsAny<TimeRange>()))
                 .ReturnsAsync(new[] { new ChartPoint(DateTime.Today, 2.0, "5/1", "2L") });
        await vm.LoadAsync();
        vm.HasFastDuration.Should().BeTrue();
        vm.HasWater.Should().BeTrue();
        vm.HasWeekly.Should().BeFalse();
        vm.HasWeight.Should().BeFalse();
    }
}
