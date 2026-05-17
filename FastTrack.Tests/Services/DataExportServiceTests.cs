using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Tests.Data;
using FluentAssertions;

namespace FastTrack.Tests.Services;

public class DataExportServiceTests
{
    [Fact]
    public async Task BuildExport_returns_default_seeded_state_for_fresh_db()
    {
        using var db = new TestDb();
        var sut = new DataExportService(db.Initializer);
        var export = await sut.BuildExportAsync();

        export.SchemaVersion.Should().Be(FastTrackExport.CurrentSchemaVersion);
        export.Protocols.Should().HaveCount(5); // 5 presets from V001
        export.Fasts.Should().BeEmpty();
        export.Weights.Should().BeEmpty();
        export.EarnedBadges.Should().BeEmpty();
        export.DailyQuests.Should().BeEmpty();
    }

    [Fact]
    public async Task BuildExport_captures_all_user_data()
    {
        using var db = new TestDb();
        var fastsRepo = new FastRepository(db.Initializer);
        var weightsRepo = new WeightRepository(db.Initializer);
        var moodsRepo = new MoodRepository(db.Initializer);
        var waterRepo = new WaterRepository(db.Initializer);
        var profilesRepo = new UserProfileRepository(db.Initializer);
        var badgesRepo = new EarnedBadgeRepository(db.Initializer);

        var profile = await profilesRepo.GetOrCreateAsync();
        profile.DisplayName = "Kirk";
        profile.TotalXp = 5_000;
        await profilesRepo.UpdateAsync(profile);

        await fastsRepo.UpsertAsync(new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = Guid.NewGuid(),
            StartUtc = DateTime.UtcNow.AddHours(-16),
            EndUtc = DateTime.UtcNow,
            GoalHours = 16,
            EndReason = FastEndReason.Completed,
        });
        await weightsRepo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow, WeightKg = 72.3 });
        await moodsRepo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow, MoodLevel = 4 });
        await waterRepo.AddAsync(new WaterEntry { TimestampUtc = DateTime.UtcNow, AmountMl = 500 });
        await badgesRepo.UpsertAsync(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = DateTime.UtcNow });

        var sut = new DataExportService(db.Initializer);
        var export = await sut.BuildExportAsync();

        export.Profile!.DisplayName.Should().Be("Kirk");
        export.Profile.TotalXp.Should().Be(5_000);
        export.Fasts.Should().HaveCount(1);
        export.Weights.Single().WeightKg.Should().Be(72.3);
        export.Moods.Single().MoodLevel.Should().Be(4);
        export.Water.Single().AmountMl.Should().Be(500);
        export.EarnedBadges.Single().BadgeKey.Should().Be("first_fast");
    }

    [Fact]
    public async Task BuildJson_round_trips_through_System_Text_Json()
    {
        using var db = new TestDb();
        var sut = new DataExportService(db.Initializer);
        var json = await sut.BuildJsonAsync();
        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"schemaVersion\""); // camelCase on the wire

        var parsed = JsonSerializer.Deserialize<FastTrackExport>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        parsed.Should().NotBeNull();
        parsed!.SchemaVersion.Should().Be(FastTrackExport.CurrentSchemaVersion);
        parsed.Protocols.Should().HaveCount(5);
    }
}
