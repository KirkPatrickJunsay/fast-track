using System.Text.Json;
using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class NotificationOrchestratorTests
{
    private static (NotificationOrchestrator Sut,
                    Mock<IFastTrackNotificationService> Notifications,
                    List<ScheduledNotification> Scheduled,
                    List<int> Cancelled,
                    UserProfile Profile)
        Build(bool permitted = true, NotificationPreferences? prefs = null)
    {
        prefs ??= NotificationPreferences.Default();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            NotificationPrefsJson = JsonSerializer.Serialize(prefs),
        };

        var scheduled = new List<ScheduledNotification>();
        var cancelled = new List<int>();

        var notifications = new Mock<IFastTrackNotificationService>();
        notifications.Setup(n => n.IsPermittedAsync()).ReturnsAsync(permitted);
        notifications.Setup(n => n.ScheduleAsync(It.IsAny<ScheduledNotification>()))
                     .Callback<ScheduledNotification>(s => scheduled.Add(s))
                     .Returns(Task.CompletedTask);
        notifications.Setup(n => n.CancelAsync(It.IsAny<int>()))
                     .Callback<int>(i => cancelled.Add(i))
                     .Returns(Task.CompletedTask);

        var profiles = new Mock<IUserProfileRepository>();
        profiles.Setup(p => p.GetOrCreateAsync()).ReturnsAsync(profile);

        var stages = new StageCalculator();
        var sut = new NotificationOrchestrator(notifications.Object, stages, profiles.Object);
        return (sut, notifications, scheduled, cancelled, profile);
    }

    private static Fast ActiveFast(DateTime startUtc, int goal) =>
        new() { Id = Guid.NewGuid(), StartUtc = startUtc, GoalHours = goal, ProtocolId = Guid.NewGuid() };

    private static FastingProtocol Protocol16x8 =>
        new() { FastHours = 16, EatHours = 8, Difficulty = Difficulty.Beginner };

    [Fact]
    public async Task ScheduleForFastAsync_does_nothing_when_permission_denied()
    {
        var (sut, _, scheduled, _, _) = Build(permitted: false);
        await sut.ScheduleForFastAsync(ActiveFast(DateTime.UtcNow, 16), Protocol16x8);
        scheduled.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleForFastAsync_schedules_lifecycle_eating_and_stage_notifications_only_in_future()
    {
        // Start a fast right now with a 24h goal so several stage boundaries fall in the future.
        var (sut, _, scheduled, _, _) = Build(prefs: new NotificationPreferences { QuietHoursEnabled = false });
        var fast = ActiveFast(DateTime.UtcNow.AddSeconds(-1), 24);
        await sut.ScheduleForFastAsync(fast, Protocol16x8);

        scheduled.Should().Contain(s => s.Title == "Halfway there");
        scheduled.Should().Contain(s => s.Title == "Goal reached 🎉");
        scheduled.Should().Contain(s => s.Title == "Eating window open");
        scheduled.Should().Contain(s => s.Title.StartsWith("Entering "));
        scheduled.Should().AllSatisfy(s => s.DeliverAtLocal.Should().BeAfter(DateTime.Now));
    }

    [Fact]
    public async Task ScheduleForFastAsync_respects_lifecycle_pref_off()
    {
        var prefs = new NotificationPreferences
        {
            FastLifecycleEnabled = false,
            StageTransitionEnabled = false,
            EatingWindowEnabled = false,
            QuietHoursEnabled = false,
        };
        var (sut, _, scheduled, _, _) = Build(prefs: prefs);
        await sut.ScheduleForFastAsync(ActiveFast(DateTime.UtcNow, 16), Protocol16x8);
        scheduled.Should().BeEmpty();
    }

    [Fact]
    public async Task ScheduleForFastAsync_groups_by_fast_id()
    {
        var (sut, _, scheduled, _, _) = Build(prefs: new NotificationPreferences { QuietHoursEnabled = false });
        var fast = ActiveFast(DateTime.UtcNow, 24);
        await sut.ScheduleForFastAsync(fast, Protocol16x8);
        scheduled.Should().OnlyContain(s => s.Group == $"fast-{fast.Id}");
    }

    [Fact]
    public async Task ScheduleForFastAsync_skips_stages_past_goal()
    {
        var (sut, _, scheduled, _, _) = Build(prefs: new NotificationPreferences { QuietHoursEnabled = false });
        // A 12h goal means only stages whose StartHour ≤ 12 should be considered (catabolic @ 4, fat-burning @ 12).
        var fast = ActiveFast(DateTime.UtcNow, 12);
        await sut.ScheduleForFastAsync(fast, Protocol16x8);
        scheduled.Where(s => s.Title.StartsWith("Entering ")).Select(s => s.Title)
            .Should().NotContain(t => t.Contains("Ketosis", StringComparison.Ordinal))
            .And.NotContain(t => t.Contains("Autophagy", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CancelForFastAsync_cancels_all_slot_ids_for_the_fast()
    {
        var (sut, _, _, cancelled, _) = Build();
        var fastId = Guid.NewGuid();
        await sut.CancelForFastAsync(fastId);
        cancelled.Should().HaveCount(10); // halfway + goal + eatingOpen + 7 stage slots
    }

    [Fact]
    public async Task RescheduleReengagementAsync_schedules_three_reminders()
    {
        var (sut, _, scheduled, _, _) = Build(prefs: new NotificationPreferences { QuietHoursEnabled = false });
        await sut.RescheduleReengagementAsync();
        scheduled.Select(s => s.Title).Should().BeEquivalentTo(new[]
        {
            "Ready for a fresh fast?",
            "Your data is here when you're ready",
            "We miss you",
        });
    }

    [Fact]
    public async Task CancelReengagementAsync_cancels_three_reserved_ids()
    {
        var (sut, _, _, cancelled, _) = Build();
        await sut.CancelReengagementAsync();
        cancelled.Should().BeEquivalentTo(new[] { 9001, 9002, 9003 });
    }
}
