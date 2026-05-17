using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class WeightServiceTests
{
    private static (WeightService Sut, List<WeightEntry> Store) Build()
    {
        var store = new List<WeightEntry>();
        var repo = new Mock<IWeightRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<WeightEntry>())).Callback<WeightEntry>(e =>
        {
            if (e.Id == 0) e.Id = store.Count + 1;
            store.Add(e);
        }).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetRecentAsync(It.IsAny<int>())).ReturnsAsync((int limit) =>
            store.OrderByDescending(w => w.TimestampUtc).Take(limit).ToList());
        return (new WeightService(repo.Object), store);
    }

    [Fact]
    public async Task LogAsync_persists_with_defaults()
    {
        var (sut, store) = Build();
        var entry = await sut.LogAsync(72.5);
        entry.Source.Should().Be("Manual");
        entry.WeightKg.Should().Be(72.5);
        entry.Note.Should().BeNull();
        store.Should().ContainSingle();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(501)]
    public async Task LogAsync_rejects_out_of_range_weight(double bad)
    {
        var (sut, _) = Build();
        await sut.Awaiting(s => s.LogAsync(bad)).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task LogAsync_trims_and_nullifies_blank_notes()
    {
        var (sut, _) = Build();
        var e1 = await sut.LogAsync(70, note: "   ");
        e1.Note.Should().BeNull();
        var e2 = await sut.LogAsync(70, note: "  hello  ");
        e2.Note.Should().Be("hello");
    }

    [Fact]
    public async Task GetTrendAsync_no_samples_returns_nulls()
    {
        var (sut, _) = Build();
        var t = await sut.GetTrendAsync();
        t.Samples.Should().Be(0);
        t.LatestKg.Should().BeNull();
        t.ChangeKg.Should().BeNull();
    }

    [Fact]
    public async Task GetTrendAsync_two_samples_reports_change()
    {
        var (sut, _) = Build();
        await sut.LogAsync(73, timestampUtc: DateTime.UtcNow.AddDays(-3));
        await sut.LogAsync(72, timestampUtc: DateTime.UtcNow);
        var t = await sut.GetTrendAsync();
        t.LatestKg.Should().Be(72);
        t.PreviousKg.Should().Be(73);
        t.ChangeKg.Should().Be(-1);
        t.Samples.Should().Be(2);
    }

    [Fact]
    public async Task GetTrendAsync_with_lookback_uses_oldest_in_window()
    {
        var (sut, _) = Build();
        await sut.LogAsync(74, timestampUtc: DateTime.UtcNow.AddDays(-30));
        await sut.LogAsync(72.5, timestampUtc: DateTime.UtcNow.AddDays(-5));
        await sut.LogAsync(72, timestampUtc: DateTime.UtcNow);

        var t = await sut.GetTrendAsync(TimeSpan.FromDays(7));
        // In-window samples: 72.5 (5 days ago) + 72 (today). Oldest = 72.5, latest = 72.
        t.LatestKg.Should().Be(72);
        t.PreviousKg.Should().Be(72.5);
        t.ChangeKg.Should().BeApproximately(-0.5, 0.001);
    }
}
