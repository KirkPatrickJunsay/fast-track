using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class FastingProtocolRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_returns_seeded_presets_ordered_by_fast_hours()
    {
        using var db = new TestDb();
        var repo = new FastingProtocolRepository(db.Initializer);
        var all = await repo.GetAllAsync();
        all.Should().HaveCount(5);
        all.Select(p => p.FastHours).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetByIdAsync_returns_known_preset()
    {
        using var db = new TestDb();
        var repo = new FastingProtocolRepository(db.Initializer);
        var sixteenEight = new Guid("a1000000-0000-0000-0000-000000000001");
        var found = await repo.GetByIdAsync(sixteenEight);
        found.Should().NotBeNull();
        found!.Name.Should().Be("16:8");
        found.FastHours.Should().Be(16);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_for_unknown_id()
    {
        using var db = new TestDb();
        var repo = new FastingProtocolRepository(db.Initializer);
        (await repo.GetByIdAsync(Guid.NewGuid())).Should().BeNull();
    }

    [Fact]
    public async Task UpsertAsync_assigns_id_when_empty_and_persists()
    {
        using var db = new TestDb();
        var repo = new FastingProtocolRepository(db.Initializer);
        var custom = new FastingProtocol
        {
            Id = Guid.Empty,
            Name = "Custom 14:10",
            FastHours = 14,
            EatHours = 10,
            Difficulty = Difficulty.Beginner,
            IsCustom = true,
        };
        await repo.UpsertAsync(custom);
        custom.Id.Should().NotBe(Guid.Empty);

        var all = await repo.GetAllAsync();
        all.Should().Contain(p => p.Name == "Custom 14:10" && p.IsCustom);
    }

    [Fact]
    public async Task UpsertAsync_updates_existing_protocol()
    {
        using var db = new TestDb();
        var repo = new FastingProtocolRepository(db.Initializer);
        var id = new Guid("a1000000-0000-0000-0000-000000000001");
        var sixteenEight = (await repo.GetByIdAsync(id))!;
        sixteenEight.Description = "Tweaked";
        await repo.UpsertAsync(sixteenEight);

        var reread = await repo.GetByIdAsync(id);
        reread!.Description.Should().Be("Tweaked");
    }
}
