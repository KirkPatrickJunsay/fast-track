using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class AnalyticsServiceTests
{
    private static Fast Completed(DateTime startUtc, double actualHours, int goalHours,
        FastEndReason reason = FastEndReason.Completed) => new()
    {
        Id = Guid.NewGuid(),
        ProtocolId = Guid.NewGuid(),
        StartUtc = startUtc,
        EndUtc = startUtc.AddHours(actualHours),
        GoalHours = goalHours,
        EndReason = reason,
    };

    private static (AnalyticsService Sut,
                    Mock<IFastRepository> Fasts,
                    Mock<IUserProfileRepository> Profiles,
                    Mock<IEarnedBadgeRepository> Badges,
                    UserProfile Profile,
                    List<Fast> History,
                    List<EarnedBadge> EarnedList,
                    List<WeightEntry> Weights,
                    List<WaterEntry> Water) Build()
    {
        var history = new List<Fast>();
        var earnedList = new List<EarnedBadge>();
        var weights = new List<WeightEntry>();
        var water = new List<WaterEntry>();

        var fasts = new Mock<IFastRepository>();
        fasts.Setup(r => r.GetHistoryAsync(It.IsAny<int>())).ReturnsAsync(() => history.AsReadOnly());

        var profile = new UserProfile { Id = Guid.NewGuid(), CurrentStreak = 0, LongestStreak = 0 };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);

        var badges = new Mock<IEarnedBadgeRepository>();
        badges.Setup(b => b.GetAllAsync()).ReturnsAsync(() => earnedList.AsReadOnly());

        var weightsRepo = new Mock<IWeightRepository>();
        weightsRepo.Setup(r => r.GetRecentAsync(It.IsAny<int>()))
            .ReturnsAsync(() => weights.OrderByDescending(w => w.TimestampUtc).ToList());

        var waterRepo = new Mock<IWaterRepository>();
        waterRepo.Setup(r => r.GetRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime a, DateTime b) =>
                water.Where(w => w.TimestampUtc >= a && w.TimestampUtc <= b).ToList());

        return (new AnalyticsService(fasts.Object, profiles.Object, badges.Object, weightsRepo.Object, waterRepo.Object),
                fasts, profiles, badges, profile, history, earnedList, weights, water);
    }

    [Fact]
    public async Task GetHeatmapAsync_returns_requested_day_count_with_zeros_when_empty()
    {
        var (sut, _, _, _, _, _, _, _, _) = Build();
        var days = await sut.GetHeatmapAsync(30);
        days.Should().HaveCount(30);
        days.Should().OnlyContain(d => d.Intensity == 0);
        days[^1].LocalDate.Should().Be(DateTime.Now.Date);
    }

    [Fact]
    public async Task GetHeatmapAsync_classifies_intensity_by_outcome()
    {
        var (sut, _, _, _, _, history, _, _, _) = Build();
        var today = DateTime.Now.Date;

        // Each fast ends within a single local day — service buckets by EndUtc.ToLocalTime().Date.
        // 3 days ago: 10h, started 08:00 → ended 18:00 same day, early reason
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-3).AddHours(8), DateTimeKind.Local).ToUniversalTime(),
            actualHours: 10, goalHours: 16, reason: FastEndReason.Hungry));
        // 2 days ago: 16h, started 02:00 → ended 18:00 same day
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-2).AddHours(2), DateTimeKind.Local).ToUniversalTime(),
            actualHours: 16, goalHours: 16));
        // 1 day ago: 20h, started 01:00 → ended 21:00 same day (1.25× goal)
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-1).AddHours(1), DateTimeKind.Local).ToUniversalTime(),
            actualHours: 20, goalHours: 16));

        var days = await sut.GetHeatmapAsync(7);
        days.Single(d => d.LocalDate == today.AddDays(-3)).Intensity.Should().Be(1);
        days.Single(d => d.LocalDate == today.AddDays(-2)).Intensity.Should().Be(2);
        days.Single(d => d.LocalDate == today.AddDays(-1)).Intensity.Should().Be(3);
        days.Single(d => d.LocalDate == today).Intensity.Should().Be(0);
    }

    [Fact]
    public async Task GetHeatmapAsync_clamps_days_below_one_to_one()
    {
        var (sut, _, _, _, _, _, _, _, _) = Build();
        (await sut.GetHeatmapAsync(0)).Should().HaveCount(1);
        (await sut.GetHeatmapAsync(-5)).Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPersonalBestsAsync_aggregates_correctly()
    {
        var (sut, _, _, _, profile, history, _, _, _) = Build();
        profile.LongestStreak = 23;

        var march = new DateTime(2026, 3, 5, 8, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 4; i++) history.Add(Completed(march.AddDays(i), 16, 16));
        var april = new DateTime(2026, 4, 7, 8, 0, 0, DateTimeKind.Utc);
        for (var i = 0; i < 6; i++) history.Add(Completed(april.AddDays(i), 16, 16));
        // Marathon
        history.Add(Completed(new DateTime(2026, 5, 1, 6, 0, 0, DateTimeKind.Utc), 49, 48));

        var bests = await sut.GetPersonalBestsAsync();
        bests.LongestFast.TotalHours.Should().BeApproximately(49, 0.01);
        bests.LongestStreak.Should().Be(23);
        bests.MostFastsInMonth.Should().Be(6);
        bests.HighestWeeklyHours.Should().BeGreaterThan(48); // marathon week is the highest
        bests.TotalCompletedFasts.Should().Be(11);
    }

    [Fact]
    public async Task GetPersonalBestsAsync_with_no_history_returns_zeros()
    {
        var (sut, _, _, _, _, _, _, _, _) = Build();
        var bests = await sut.GetPersonalBestsAsync();
        bests.LongestFast.Should().Be(TimeSpan.Zero);
        bests.LongestStreak.Should().Be(0);
        bests.MostFastsInMonth.Should().Be(0);
        bests.HighestWeeklyHours.Should().Be(0);
        bests.TotalCompletedFasts.Should().Be(0);
    }

    [Fact]
    public async Task GetWeeklySummaryAsync_empty_history_returns_neutral_message()
    {
        var (sut, _, _, _, _, _, _, _, _) = Build();
        var summary = await sut.GetWeeklySummaryAsync();
        summary.HoursThisWeek.Should().Be(0);
        summary.FastsThisWeek.Should().Be(0);
        summary.Highlight.Should().Contain("No fasts");
    }

    [Fact]
    public async Task GetWeeklySummaryAsync_counts_fasts_and_badges_within_local_week()
    {
        var (sut, _, _, _, profile, history, badges, _, _) = Build();
        profile.CurrentStreak = 4;

        // This-week local Monday (start of current week).
        var nowLocal = DateTime.Now;
        var monday = nowLocal.Date.AddDays(-(((int)nowLocal.DayOfWeek + 6) % 7));
        var mondayUtc = DateTime.SpecifyKind(monday.AddHours(7), DateTimeKind.Local).ToUniversalTime();

        history.Add(Completed(mondayUtc, 16, 16));
        history.Add(Completed(mondayUtc.AddDays(1), 18, 16));
        // Last week: one fast at 12h
        history.Add(Completed(mondayUtc.AddDays(-7).AddHours(2), 12, 16));

        badges.Add(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = mondayUtc });

        var summary = await sut.GetWeeklySummaryAsync();
        summary.FastsThisWeek.Should().Be(2);
        summary.HoursThisWeek.Should().BeApproximately(34, 0.01);
        summary.HoursLastWeek.Should().BeApproximately(12, 0.01);
        summary.DeltaHours.Should().BeApproximately(22, 0.01);
        summary.CurrentStreak.Should().Be(4);
        summary.BadgesThisWeek.Should().Be(1);
        summary.Highlight.Should().NotContain("No fasts");
    }

    // ----- Slice 2: trend series -----

    [Fact]
    public async Task GetFastDurationSeriesAsync_emits_one_point_per_completed_fast_in_range()
    {
        var (sut, _, _, _, _, history, _, _, _) = Build();
        var today = DateTime.Now.Date;
        // 40 days ago — outside 30d range
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-40).AddHours(1), DateTimeKind.Local).ToUniversalTime(), 16, 16));
        // 5 and 2 days ago — inside range
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-5).AddHours(1), DateTimeKind.Local).ToUniversalTime(), 18, 16));
        history.Add(Completed(DateTime.SpecifyKind(today.AddDays(-2).AddHours(1), DateTimeKind.Local).ToUniversalTime(), 20, 16));

        var points = await sut.GetFastDurationSeriesAsync(TimeRange.Days30);

        points.Should().HaveCount(2);
        points.Select(p => p.Value).Should().Equal(new[] { 18.0, 20.0 });
        points[0].ValueLabel.Should().Be("18h");
        points[1].ValueLabel.Should().Be("20h");
    }

    [Fact]
    public async Task GetWeeklyFastingHoursAsync_buckets_by_monday()
    {
        var (sut, _, _, _, _, history, _, _, _) = Build();

        var nowLocal = DateTime.Now;
        var monday = nowLocal.Date.AddDays(-(((int)nowLocal.DayOfWeek + 6) % 7));
        var mondayUtc = DateTime.SpecifyKind(monday.AddHours(2), DateTimeKind.Local).ToUniversalTime();
        history.Add(Completed(mondayUtc, 16, 16));
        history.Add(Completed(mondayUtc.AddDays(1), 18, 16));
        history.Add(Completed(mondayUtc.AddDays(-7), 12, 16));

        var points = await sut.GetWeeklyFastingHoursAsync(TimeRange.Days30);
        points.Should().HaveCount(2);
        points[0].Value.Should().BeApproximately(12, 0.01);
        points[1].Value.Should().BeApproximately(34, 0.01);
    }

    [Fact]
    public async Task GetWeightSeriesAsync_returns_in_chronological_order_within_range()
    {
        var (sut, _, _, _, _, _, _, weights, _) = Build();
        var today = DateTime.Now;
        weights.Add(new WeightEntry { TimestampUtc = today.AddDays(-100).ToUniversalTime(), WeightKg = 80 });
        weights.Add(new WeightEntry { TimestampUtc = today.AddDays(-5).ToUniversalTime(), WeightKg = 75 });
        weights.Add(new WeightEntry { TimestampUtc = today.AddDays(-1).ToUniversalTime(), WeightKg = 74.5 });

        var points = await sut.GetWeightSeriesAsync(TimeRange.Days30);
        points.Should().HaveCount(2);
        points.Select(p => p.Value).Should().Equal(new[] { 75.0, 74.5 });
        points.Select(p => p.Date).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetWaterDailyAsync_returns_one_point_per_day_in_window_with_zero_fill()
    {
        var (sut, _, _, _, _, _, _, _, water) = Build();
        var today = DateTime.Now.Date;
        water.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddHours(8), DateTimeKind.Local).ToUniversalTime(), AmountMl = 1500 });
        water.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddDays(-1).AddHours(20), DateTimeKind.Local).ToUniversalTime(), AmountMl = 500 });

        var points = await sut.GetWaterDailyAsync(TimeRange.Days7);
        points.Should().HaveCount(7);
        points[^1].Value.Should().BeApproximately(1.5, 0.01);
        points[^2].Value.Should().BeApproximately(0.5, 0.01);
        points[0].Value.Should().Be(0); // zero-fill for empty days
        points.Last().ValueLabel.Should().Be("1.5L");
    }

    [Fact]
    public async Task GetFastDurationSeriesAsync_with_all_returns_full_history()
    {
        var (sut, _, _, _, _, history, _, _, _) = Build();
        history.Add(Completed(DateTime.UtcNow.AddDays(-400), 16, 16));
        history.Add(Completed(DateTime.UtcNow.AddDays(-5), 18, 16));

        var points = await sut.GetFastDurationSeriesAsync(TimeRange.All);
        points.Should().HaveCount(2);
    }
}
