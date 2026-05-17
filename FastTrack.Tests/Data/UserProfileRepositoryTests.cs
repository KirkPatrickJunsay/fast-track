using FastTrack.Data;
using FastTrack.Models;
using FluentAssertions;

namespace FastTrack.Tests.Data;

public class UserProfileRepositoryTests
{
    [Fact]
    public async Task GetOrCreateAsync_creates_profile_on_first_call()
    {
        using var db = new TestDb();
        var repo = new UserProfileRepository(db.Initializer);
        var profile = await repo.GetOrCreateAsync();
        profile.Id.Should().NotBe(Guid.Empty);
        profile.OnboardingCompleted.Should().BeFalse();
        profile.Level.Should().Be(ExperienceLevel.Beginner);
        profile.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task GetOrCreateAsync_returns_existing_profile_on_subsequent_calls()
    {
        using var db = new TestDb();
        var repo = new UserProfileRepository(db.Initializer);
        var first = await repo.GetOrCreateAsync();
        var second = await repo.GetOrCreateAsync();
        second.Id.Should().Be(first.Id);
        second.CreatedAtUtc.Should().Be(first.CreatedAtUtc);
    }

    [Fact]
    public async Task UpdateAsync_persists_changes()
    {
        using var db = new TestDb();
        var repo = new UserProfileRepository(db.Initializer);
        var p = await repo.GetOrCreateAsync();
        p.DisplayName = "Kirk";
        p.Level = ExperienceLevel.Advanced;
        p.OnboardingCompleted = true;
        p.TotalXp = 1234;
        p.CurrentStreak = 5;
        p.IsEducationalMode = true;
        await repo.UpdateAsync(p);

        var reread = await repo.GetOrCreateAsync();
        reread.DisplayName.Should().Be("Kirk");
        reread.Level.Should().Be(ExperienceLevel.Advanced);
        reread.OnboardingCompleted.Should().BeTrue();
        reread.TotalXp.Should().Be(1234);
        reread.CurrentStreak.Should().Be(5);
        reread.IsEducationalMode.Should().BeTrue();
    }
}
