using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class XpServiceTests
{
    private static (XpService Sut, Mock<IUserProfileRepository> Profiles, UserProfile Profile) Build(int totalXp = 0)
    {
        var profile = new UserProfile { Id = Guid.NewGuid(), TotalXp = totalXp };
        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);
        profiles.Setup(p => p.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        return (new XpService(profiles.Object), profiles, profile);
    }

    private static Fast Completed(int goalHours, double actualHours)
    {
        var start = DateTime.UtcNow.AddHours(-actualHours);
        return new Fast
        {
            Id = Guid.NewGuid(),
            StartUtc = start,
            EndUtc = start.AddHours(actualHours),
            GoalHours = goalHours,
        };
    }

    [Fact]
    public void Calculate_returns_zero_for_unfinished_fast()
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Id = Guid.NewGuid(), FastHours = 16, Difficulty = Difficulty.Beginner };
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow, EndUtc = null, GoalHours = 16 };
        var award = sut.CalculateForFast(fast, protocol, currentStreak: 0, comebackBonus: false);
        award.Total.Should().Be(0);
    }

    [Fact]
    public void Beginner_base_formula_is_hours_times_ten()
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Difficulty = Difficulty.Beginner, FastHours = 16 };
        var award = sut.CalculateForFast(Completed(16, 16), protocol, 0, false);
        // 16h * 10 = 160 base, x 1.0 difficulty, no bonus, no streak
        award.Base.Should().Be(160);
        award.DifficultyMultiplier.Should().Be(1.0);
        award.GoalExceededBonus.Should().BeFalse();
        award.StreakMultiplier.Should().Be(1.0);
        award.ComebackBonus.Should().BeFalse();
        award.Total.Should().Be(160);
    }

    [Theory]
    [InlineData(Difficulty.Intermediate, 1.25)]
    [InlineData(Difficulty.Advanced, 1.5)]
    public void Difficulty_multiplier_applies(Difficulty diff, double mult)
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Difficulty = diff };
        var award = sut.CalculateForFast(Completed(10, 10), protocol, 0, false);
        award.Total.Should().Be((int)Math.Round(100 * mult));
    }

    [Fact]
    public void Goal_exceeded_bonus_adds_25_percent()
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Difficulty = Difficulty.Beginner };
        var award = sut.CalculateForFast(Completed(16, 18), protocol, 0, false);
        award.GoalExceededBonus.Should().BeTrue();
        // 18h*10=180 base, x1.25 goal bonus = 225
        award.Total.Should().Be(225);
    }

    [Fact]
    public void Streak_multiplier_caps_at_50_percent()
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Difficulty = Difficulty.Beginner };
        // 16h*10*1.0*1.0*1.5 = 240 at streak >= 50
        sut.CalculateForFast(Completed(16, 16), protocol, currentStreak: 50, false).Total.Should().Be(240);
        sut.CalculateForFast(Completed(16, 16), protocol, currentStreak: 200, false).Total.Should().Be(240);
    }

    [Fact]
    public void Comeback_bonus_multiplies_by_1_5()
    {
        var (sut, _, _) = Build();
        var protocol = new FastingProtocol { Difficulty = Difficulty.Beginner };
        var award = sut.CalculateForFast(Completed(16, 16), protocol, 0, comebackBonus: true);
        award.ComebackBonus.Should().BeTrue();
        award.Total.Should().Be(240);
    }

    [Fact]
    public async Task AwardAsync_persists_and_returns_new_state()
    {
        var (sut, profiles, profile) = Build(totalXp: 400);
        var state = await sut.AwardAsync(200);
        profile.TotalXp.Should().Be(600);
        state.TotalXp.Should().Be(600);
        state.Level.Number.Should().Be(2); // Apprentice at 500+
        profiles.Verify(p => p.UpdateAsync(profile), Times.Once);
    }

    [Fact]
    public async Task AwardAsync_ignores_non_positive_xp()
    {
        var (sut, profiles, profile) = Build(totalXp: 100);
        await sut.AwardAsync(0);
        await sut.AwardAsync(-50);
        profile.TotalXp.Should().Be(100);
        profiles.Verify(p => p.UpdateAsync(It.IsAny<UserProfile>()), Times.Never);
    }

    [Fact]
    public async Task GetStateAsync_computes_progress_within_current_level()
    {
        var (sut, _, _) = Build(totalXp: 750); // mid-Apprentice (500..1500)
        var state = await sut.GetStateAsync();
        state.Level.Name.Should().Be("Apprentice");
        state.NextLevel!.Name.Should().Be("Practitioner");
        state.XpIntoLevel.Should().Be(250);
        state.XpForNextLevel.Should().Be(1000);
    }

    [Fact]
    public async Task GetStateAsync_returns_zero_progress_at_max_level()
    {
        var (sut, _, _) = Build(totalXp: 250_000);
        var state = await sut.GetStateAsync();
        state.Level.Name.Should().Be("Legendary");
        state.NextLevel.Should().BeNull();
    }
}
