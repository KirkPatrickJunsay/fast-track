using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class WaterRepositoryTests
{
    [Fact]
    public async Task GetTotalForDayAsync_returns_zero_when_empty()
    {
        using var db = new TestDb();
        var repo = new WaterRepository(db.Initializer);
        var d = DateTime.UtcNow.Date;
        (await repo.GetTotalForDayAsync(d, d.AddDays(1))).Should().Be(0);
    }

    [Fact]
    public async Task GetTotalForDayAsync_sums_amounts_in_window()
    {
        using var db = new TestDb();
        var repo = new WaterRepository(db.Initializer);
        var dayStart = DateTime.UtcNow.Date;
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddHours(8),  AmountMl = 250 });
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddHours(12), AmountMl = 500 });
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddHours(20), AmountMl = 250 });
        // Yesterday entry that must be excluded:
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddDays(-1).AddHours(10), AmountMl = 1000 });

        var total = await repo.GetTotalForDayAsync(dayStart, dayStart.AddDays(1));
        total.Should().Be(1000);
    }

    [Fact]
    public async Task GetForDayAsync_returns_entries_in_order()
    {
        using var db = new TestDb();
        var repo = new WaterRepository(db.Initializer);
        var dayStart = DateTime.UtcNow.Date;
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddHours(18), AmountMl = 250 });
        await repo.AddAsync(new WaterEntry { TimestampUtc = dayStart.AddHours(8),  AmountMl = 500 });
        var rows = await repo.GetForDayAsync(dayStart, dayStart.AddDays(1));
        rows.Select(r => r.AmountMl).Should().Equal(new[] { 500, 250 });
    }

    [Fact]
    public async Task DeleteAsync_removes_entry()
    {
        using var db = new TestDb();
        var repo = new WaterRepository(db.Initializer);
        var entry = new WaterEntry { TimestampUtc = DateTime.UtcNow, AmountMl = 500 };
        await repo.AddAsync(entry);
        await repo.DeleteAsync(entry.Id);
        (await repo.GetForDayAsync(DateTime.UtcNow.Date, DateTime.UtcNow.Date.AddDays(1))).Should().BeEmpty();
    }

    [Fact]
    public async Task GetRangeAsync_filters_by_window()
    {
        using var db = new TestDb();
        var repo = new WaterRepository(db.Initializer);
        var now = DateTime.UtcNow;
        await repo.AddAsync(new WaterEntry { TimestampUtc = now.AddDays(-10), AmountMl = 250 });
        await repo.AddAsync(new WaterEntry { TimestampUtc = now.AddDays(-2),  AmountMl = 250 });
        await repo.AddAsync(new WaterEntry { TimestampUtc = now, AmountMl = 250 });
        var range = await repo.GetRangeAsync(now.AddDays(-5), now);
        range.Should().HaveCount(2);
    }
}
