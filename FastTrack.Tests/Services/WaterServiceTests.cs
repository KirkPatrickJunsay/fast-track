using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class WaterServiceTests
{
    private static (WaterService Sut, List<WaterEntry> Store, Mock<IWaterRepository> Repo) Build()
    {
        var store = new List<WaterEntry>();
        var repo = new Mock<IWaterRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<WaterEntry>())).Callback<WaterEntry>(e =>
        {
            if (e.Id == 0) e.Id = store.Count + 1;
            store.Add(e);
        }).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetForDayAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime a, DateTime b) =>
                store.Where(w => w.TimestampUtc >= a && w.TimestampUtc < b).ToList());
        repo.Setup(r => r.GetTotalForDayAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime a, DateTime b) =>
                store.Where(w => w.TimestampUtc >= a && w.TimestampUtc < b).Sum(w => w.AmountMl));
        repo.Setup(r => r.GetRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .ReturnsAsync((DateTime a, DateTime b) =>
                store.Where(w => w.TimestampUtc >= a && w.TimestampUtc <= b).OrderBy(w => w.TimestampUtc).ToList());
        return (new WaterService(repo.Object), store, repo);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    [InlineData(5_001)]
    public async Task AddAsync_rejects_unreasonable_amounts(int bad)
    {
        var (sut, _, _) = Build();
        await sut.Awaiting(s => s.AddAsync(bad)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddAsync_persists_with_defaults()
    {
        var (sut, store, _) = Build();
        await sut.AddAsync(250);
        store.Should().ContainSingle();
        store[0].AmountMl.Should().Be(250);
        store[0].Source.Should().Be("Manual");
    }

    [Fact]
    public async Task GetTodayAsync_returns_progress_and_goal_state()
    {
        var (sut, _, _) = Build();
        await sut.AddAsync(500);
        await sut.AddAsync(500);
        var snap = await sut.GetTodayAsync();
        snap.GoalMl.Should().Be(2000);
        snap.TotalMl.Should().Be(1000);
        snap.GoalFraction.Should().BeApproximately(0.5, 0.001);
        snap.GoalHit.Should().BeFalse();
    }

    [Fact]
    public async Task GetTodayAsync_flags_goal_hit_at_or_above_2L()
    {
        var (sut, _, _) = Build();
        await sut.AddAsync(1000);
        await sut.AddAsync(1000);
        var snap = await sut.GetTodayAsync();
        snap.GoalHit.Should().BeTrue();
        snap.GoalFraction.Should().Be(1.0);
    }

    [Fact]
    public async Task CountGoalHitDaysAsync_counts_unique_days_meeting_goal()
    {
        var (sut, store, _) = Build();
        var today = DateTime.Now.Date;

        // Day -2: 2L (hit)
        store.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddDays(-2).AddHours(8),  DateTimeKind.Local).ToUniversalTime(), AmountMl = 1000 });
        store.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddDays(-2).AddHours(14), DateTimeKind.Local).ToUniversalTime(), AmountMl = 1000 });
        // Day -1: 1.5L (miss)
        store.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddDays(-1).AddHours(9),  DateTimeKind.Local).ToUniversalTime(), AmountMl = 1500 });
        // Today: 2L (hit)
        store.Add(new WaterEntry { TimestampUtc = DateTime.SpecifyKind(today.AddHours(7),  DateTimeKind.Local).ToUniversalTime(), AmountMl = 2000 });

        var hits = await sut.CountGoalHitDaysAsync(lookbackDays: 7);
        hits.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public async Task CountGoalHitDaysAsync_with_non_positive_lookback_returns_zero(int days)
    {
        var (sut, _, _) = Build();
        (await sut.CountGoalHitDaysAsync(days)).Should().Be(0);
    }
}
