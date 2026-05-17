using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class StreakServiceTests
{
    private static (StreakService Sut, UserProfile Profile, Mock<IUserProfileRepository> Repo) Build(
        int current = 0, int longest = 0, int freezes = 0, DateTime? lastDayUtc = null)
    {
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            CurrentStreak = current,
            LongestStreak = longest,
            StreakFreezesAvailable = freezes,
            LastStreakDayUtc = lastDayUtc,
        };
        var repo = new Mock<IUserProfileRepository>();
        repo.Setup(r => r.GetOrCreateAsync()).ReturnsAsync(profile);
        repo.Setup(r => r.UpdateAsync(It.IsAny<UserProfile>())).Returns(Task.CompletedTask);
        return (new StreakService(repo.Object), profile, repo);
    }

    private static Fast Completed(int goalHours, double actualHours, DateTime endUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            StartUtc = endUtc.AddHours(-actualHours),
            EndUtc = endUtc,
            GoalHours = goalHours,
        };

    [Fact]
    public async Task GetSnapshotAsync_returns_profile_state()
    {
        var (sut, _, _) = Build(current: 3, longest: 9, freezes: 2);
        var snap = await sut.GetSnapshotAsync();
        snap.Current.Should().Be(3);
        snap.Longest.Should().Be(9);
        snap.FreezesAvailable.Should().Be(2);
    }

    [Fact]
    public async Task First_qualifying_fast_starts_streak_at_one()
    {
        var (sut, profile, _) = Build();
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.IncrementedToday.Should().BeTrue();
        result.Current.Should().Be(1);
        profile.CurrentStreak.Should().Be(1);
        profile.LongestStreak.Should().Be(1);
    }

    [Fact]
    public async Task Fast_under_80_percent_does_not_qualify()
    {
        var (sut, profile, _) = Build();
        var result = await sut.RecordCompletedFastAsync(Completed(16, 12, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.IncrementedToday.Should().BeFalse();
        result.Current.Should().Be(0);
        profile.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public async Task Same_day_second_fast_is_idempotent()
    {
        var today = DateTime.UtcNow.Date;
        var (sut, profile, _) = Build(current: 5, lastDayUtc: today);
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, today.AddHours(12)), new FastingProtocol { FastHours = 16 });
        result.IncrementedToday.Should().BeFalse();
        result.Current.Should().Be(5);
        profile.CurrentStreak.Should().Be(5);
    }

    [Fact]
    public async Task Consecutive_day_increments_streak()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var (sut, profile, _) = Build(current: 4, lastDayUtc: yesterday);
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.Current.Should().Be(5);
        profile.CurrentStreak.Should().Be(5);
    }

    [Fact]
    public async Task Two_day_gap_with_freeze_consumes_it()
    {
        // Start at 5 → gap saved by 1 freeze → current = 6 (not a 7-day milestone, so no re-award).
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
        var (sut, profile, _) = Build(current: 5, freezes: 2, lastDayUtc: twoDaysAgo);
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.FreezeConsumed.Should().BeTrue();
        result.Current.Should().Be(6);
        profile.StreakFreezesAvailable.Should().Be(1);
    }

    [Fact]
    public async Task Two_day_gap_with_no_freeze_breaks_streak_and_sets_comeback_flag()
    {
        var twoDaysAgo = DateTime.UtcNow.Date.AddDays(-2);
        var (sut, profile, _) = Build(current: 6, freezes: 0, lastDayUtc: twoDaysAgo);
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.Broken.Should().BeTrue();
        result.Current.Should().Be(1);
        profile.ComebackBonusPending.Should().BeTrue();
    }

    [Fact]
    public async Task Three_day_gap_breaks_streak_even_with_freezes()
    {
        var threeDaysAgo = DateTime.UtcNow.Date.AddDays(-3);
        var (sut, profile, _) = Build(current: 10, freezes: 3, lastDayUtc: threeDaysAgo);
        var result = await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        result.Broken.Should().BeTrue();
        result.Current.Should().Be(1);
        profile.StreakFreezesAvailable.Should().Be(3); // freezes only cover 1-day gaps
    }

    [Fact]
    public async Task Seven_day_milestone_awards_a_freeze()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var (sut, profile, _) = Build(current: 6, freezes: 0, lastDayUtc: yesterday);
        await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        profile.CurrentStreak.Should().Be(7);
        profile.StreakFreezesAvailable.Should().Be(1);
    }

    [Fact]
    public async Task Freezes_cap_at_three()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var (sut, profile, _) = Build(current: 27, freezes: 3, lastDayUtc: yesterday);
        // Day 28 — not a 7-multiple yet
        await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow), new FastingProtocol { FastHours = 16 });
        profile.StreakFreezesAvailable.Should().Be(3);

        // Tomorrow → 29 → no milestone
        profile.LastStreakDayUtc = DateTime.UtcNow.Date;
        await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow.AddDays(1)), new FastingProtocol { FastHours = 16 });
        profile.StreakFreezesAvailable.Should().Be(3);
    }

    [Fact]
    public async Task LongestStreak_tracks_max_seen()
    {
        var (sut, profile, _) = Build(current: 9, longest: 10);
        await sut.RecordCompletedFastAsync(Completed(16, 16, DateTime.UtcNow.Date.AddDays(1)), new FastingProtocol { FastHours = 16 });
        // First-day path: current resets to 1. Longest stays 10.
        profile.LongestStreak.Should().Be(10);
    }
}
