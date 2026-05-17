using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Tests.Data;
using FluentAssertions;

namespace FastTrack.Tests.Services;

public class DataImportServiceTests
{
    [Fact]
    public async Task Apply_rejects_empty_string()
    {
        using var db = new TestDb();
        var sut = new DataImportService(db.Initializer);
        var result = await sut.ApplyAsync(string.Empty);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("empty");
    }

    [Fact]
    public async Task Apply_rejects_invalid_json()
    {
        using var db = new TestDb();
        var sut = new DataImportService(db.Initializer);
        var result = await sut.ApplyAsync("{ not valid }");
        result.Success.Should().BeFalse();
        result.Message.Should().StartWith("Invalid JSON");
    }

    [Fact]
    public async Task Apply_rejects_missing_schema_version()
    {
        using var db = new TestDb();
        var sut = new DataImportService(db.Initializer);
        var result = await sut.ApplyAsync("{ \"schemaVersion\": 0 }");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Apply_rejects_future_schema_version()
    {
        using var db = new TestDb();
        var sut = new DataImportService(db.Initializer);
        var future = new FastTrackExport { SchemaVersion = FastTrackExport.CurrentSchemaVersion + 1 };
        var result = await sut.ApplyAsync(JsonSerializer.Serialize(future));
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("newer version");
    }

    [Fact]
    public async Task Apply_replaces_existing_data_atomically_with_imported_payload()
    {
        using var db = new TestDb();
        var fastsRepo = new FastRepository(db.Initializer);
        var weightsRepo = new WeightRepository(db.Initializer);
        var moodsRepo = new MoodRepository(db.Initializer);
        var profilesRepo = new UserProfileRepository(db.Initializer);

        // Seed pre-import data we expect to be wiped.
        var oldProfile = await profilesRepo.GetOrCreateAsync();
        oldProfile.DisplayName = "Old";
        await profilesRepo.UpdateAsync(oldProfile);
        await fastsRepo.UpsertAsync(new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 });
        await weightsRepo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow, WeightKg = 80 });

        // Build a fresh import payload with different data.
        var importedFastId = Guid.NewGuid();
        var protocolId = Guid.NewGuid();
        var payload = new FastTrackExport
        {
            SchemaVersion = 1,
            ExportedAtUtc = DateTime.UtcNow,
            Profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                DisplayName = "Imported",
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
                Level = ExperienceLevel.Intermediate,
                TotalXp = 9_999,
                OnboardingCompleted = true,
            },
            Protocols = new List<FastingProtocol>
            {
                new() { Id = protocolId, Name = "Imported 12:12", FastHours = 12, EatHours = 12, IsCustom = true },
            },
            Fasts = new List<Fast>
            {
                new() { Id = importedFastId, ProtocolId = protocolId, StartUtc = DateTime.UtcNow.AddDays(-1), EndUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 12, EndReason = FastEndReason.Completed },
            },
            Weights = new List<WeightEntry>
            {
                new() { TimestampUtc = DateTime.UtcNow.AddHours(-1), WeightKg = 71.5 },
            },
            EarnedBadges = new List<EarnedBadge>
            {
                new() { BadgeKey = "day_one", EarnedAtUtc = DateTime.UtcNow },
            },
        };

        var sut = new DataImportService(db.Initializer);
        var result = await sut.ApplyAsync(JsonSerializer.Serialize(payload));

        result.Success.Should().BeTrue();
        result.Fasts.Should().Be(1);
        result.Weights.Should().Be(1);
        result.Badges.Should().Be(1);

        // Verify state via independent repo reads.
        var profileNow = await profilesRepo.GetOrCreateAsync();
        profileNow.DisplayName.Should().Be("Imported");
        profileNow.TotalXp.Should().Be(9_999);

        var fastsNow = await fastsRepo.GetHistoryAsync(100);
        fastsNow.Should().ContainSingle().Which.Id.Should().Be(importedFastId);

        (await weightsRepo.GetLatestAsync())!.WeightKg.Should().Be(71.5);
        (await moodsRepo.GetTotalCountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Apply_with_empty_protocols_restores_presets_as_safety_net()
    {
        using var db = new TestDb();
        var sut = new DataImportService(db.Initializer);
        var payload = new FastTrackExport { SchemaVersion = 1 };
        var result = await sut.ApplyAsync(JsonSerializer.Serialize(payload));
        result.Success.Should().BeTrue();

        var protocolsRepo = new FastingProtocolRepository(db.Initializer);
        (await protocolsRepo.GetAllAsync()).Should().HaveCount(5);
    }
}
