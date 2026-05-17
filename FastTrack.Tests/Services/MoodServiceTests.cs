using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class MoodServiceTests
{
    private static (MoodService Sut, List<MoodEntry> Store) Build()
    {
        var store = new List<MoodEntry>();
        var repo = new Mock<IMoodRepository>();
        repo.Setup(r => r.AddAsync(It.IsAny<MoodEntry>())).Callback<MoodEntry>(e =>
        {
            if (e.Id == 0) e.Id = store.Count + 1;
            store.Add(e);
        }).Returns(Task.CompletedTask);
        repo.Setup(r => r.GetTotalCountAsync()).ReturnsAsync(() => store.Count);
        repo.Setup(r => r.GetForFastAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) =>
            store.Where(m => m.FastId == id).OrderBy(m => m.TimestampUtc).ToList());
        return (new MoodService(repo.Object), store);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(6)]
    public async Task LogAsync_rejects_out_of_range_levels(int level)
    {
        var (sut, _) = Build();
        await sut.Awaiting(s => s.LogAsync(level)).Should().ThrowAsync<ArgumentException>();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task LogAsync_persists_valid_levels(int level)
    {
        var (sut, store) = Build();
        var entry = await sut.LogAsync(level);
        entry.MoodLevel.Should().Be(level);
        store.Should().ContainSingle();
    }

    [Fact]
    public async Task LogAsync_attaches_fast_id_when_provided()
    {
        var (sut, _) = Build();
        var fastId = Guid.NewGuid();
        var entry = await sut.LogAsync(4, fastId: fastId, note: " feels good ");
        entry.FastId.Should().Be(fastId);
        entry.Note.Should().Be("feels good");
    }

    [Fact]
    public async Task GetForFastAsync_returns_only_entries_for_that_fast()
    {
        var (sut, _) = Build();
        var f1 = Guid.NewGuid();
        var f2 = Guid.NewGuid();
        await sut.LogAsync(3, fastId: f1);
        await sut.LogAsync(4, fastId: f1);
        await sut.LogAsync(2, fastId: f2);

        var rows = await sut.GetForFastAsync(f1);
        rows.Should().HaveCount(2);
        rows.Should().OnlyContain(m => m.FastId == f1);
    }

    [Fact]
    public async Task GetTotalCountAsync_reflects_inserts()
    {
        var (sut, _) = Build();
        await sut.LogAsync(3);
        await sut.LogAsync(4);
        (await sut.GetTotalCountAsync()).Should().Be(2);
    }
}
