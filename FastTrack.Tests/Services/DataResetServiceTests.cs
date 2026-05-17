using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Tests.Data;
using FluentAssertions;

namespace FastTrack.Tests.Services;

public class DataResetServiceTests
{
    [Fact]
    public async Task ResetAllAsync_wipes_user_data_and_keeps_presets()
    {
        using var db = new TestDb();
        var fastsRepo = new FastRepository(db.Initializer);
        var weightsRepo = new WeightRepository(db.Initializer);
        var moodsRepo = new MoodRepository(db.Initializer);
        var waterRepo = new WaterRepository(db.Initializer);
        var profilesRepo = new UserProfileRepository(db.Initializer);
        var badgesRepo = new EarnedBadgeRepository(db.Initializer);
        var protocolsRepo = new FastingProtocolRepository(db.Initializer);

        var profile = await profilesRepo.GetOrCreateAsync();
        profile.DisplayName = "Kirk";
        profile.OnboardingCompleted = true;
        await profilesRepo.UpdateAsync(profile);

        await fastsRepo.UpsertAsync(new Fast { Id = Guid.NewGuid(), ProtocolId = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, GoalHours = 16 });
        await weightsRepo.AddAsync(new WeightEntry { TimestampUtc = DateTime.UtcNow, WeightKg = 72 });
        await moodsRepo.AddAsync(new MoodEntry { TimestampUtc = DateTime.UtcNow, MoodLevel = 3 });
        await waterRepo.AddAsync(new WaterEntry { TimestampUtc = DateTime.UtcNow, AmountMl = 500 });
        await badgesRepo.UpsertAsync(new EarnedBadge { BadgeKey = "first_fast", EarnedAtUtc = DateTime.UtcNow });

        var sut = new DataResetService(db.Initializer);
        await sut.ResetAllAsync();

        // Profile gone — GetOrCreate creates a fresh one with OnboardingCompleted = false.
        var fresh = await profilesRepo.GetOrCreateAsync();
        fresh.OnboardingCompleted.Should().BeFalse();
        fresh.DisplayName.Should().BeNullOrEmpty();

        (await fastsRepo.GetHistoryAsync(100)).Should().BeEmpty();
        (await weightsRepo.GetLatestAsync()).Should().BeNull();
        (await moodsRepo.GetTotalCountAsync()).Should().Be(0);
        (await badgesRepo.GetAllAsync()).Should().BeEmpty();

        // Presets back to 5.
        (await protocolsRepo.GetAllAsync()).Should().HaveCount(5);
    }
}
