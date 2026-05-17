using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FluentAssertions;
using Moq;

namespace FastTrack.Tests.Services;

public class FastingServiceTests
{
    private static readonly FastingProtocol Default = new()
    {
        Id = Guid.NewGuid(), Name = "16:8", FastHours = 16, EatHours = 8, Difficulty = Difficulty.Beginner,
    };

    private static (FastingService Sut,
                    Mock<IFastRepository> Fasts,
                    Mock<IFastingProtocolRepository> Protocols,
                    Mock<INotificationOrchestrator> Notifications,
                    Dictionary<Guid, Fast> Store)
        Build(Fast? active = null)
    {
        var store = new Dictionary<Guid, Fast>();
        if (active is not null) store[active.Id] = active;

        var fasts = new Mock<IFastRepository>();
        fasts.Setup(r => r.GetActiveAsync()).ReturnsAsync(() => store.Values.FirstOrDefault(f => f.EndUtc is null));
        fasts.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Guid id) => store.GetValueOrDefault(id));
        fasts.Setup(r => r.UpsertAsync(It.IsAny<Fast>())).Callback<Fast>(f =>
        {
            if (f.Id == Guid.Empty) f.Id = Guid.NewGuid();
            store[f.Id] = f;
        }).Returns(Task.CompletedTask);

        var protocols = new Mock<IFastingProtocolRepository>();
        protocols.Setup(p => p.GetByIdAsync(Default.Id)).ReturnsAsync(Default);

        var notifications = new Mock<INotificationOrchestrator>();
        var sut = new FastingService(fasts.Object, protocols.Object, notifications.Object);
        return (sut, fasts, protocols, notifications, store);
    }

    [Fact]
    public async Task StartAsync_creates_active_fast_with_protocol_goal()
    {
        var (sut, _, _, notifications, store) = Build();
        var fast = await sut.StartAsync(Default.Id);
        fast.ProtocolId.Should().Be(Default.Id);
        fast.GoalHours.Should().Be(16);
        fast.EndUtc.Should().BeNull();
        store[fast.Id].Should().BeSameAs(fast);
        notifications.Verify(n => n.ScheduleForFastAsync(fast, Default), Times.Once);
    }

    [Fact]
    public async Task StartAsync_throws_if_another_fast_is_already_active()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), ProtocolId = Default.Id, GoalHours = 16 };
        var (sut, _, _, _, _) = Build(active);
        var act = () => sut.StartAsync(Default.Id);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("A fast is already active*");
    }

    [Fact]
    public async Task StartAsync_throws_when_protocol_not_found()
    {
        var (sut, _, protocols, _, _) = Build();
        protocols.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FastingProtocol?)null);
        await sut.Awaiting(s => s.StartAsync(Guid.NewGuid())).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_rejects_future_start_time()
    {
        var (sut, _, _, _, _) = Build();
        await sut.Awaiting(s => s.StartAsync(Default.Id, DateTime.UtcNow.AddHours(1)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartAsync_accepts_backdated_start_and_swallows_notification_errors()
    {
        var (sut, _, _, notifications, _) = Build();
        notifications.Setup(n => n.ScheduleForFastAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()))
                     .ThrowsAsync(new Exception("boom"));
        var backdated = DateTime.UtcNow.AddHours(-3);
        var fast = await sut.StartAsync(Default.Id, backdated);
        fast.StartUtc.Should().BeCloseTo(backdated, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task EndAsync_marks_completion_cancels_notifications_and_raises_event()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), ProtocolId = Default.Id, GoalHours = 16 };
        var (sut, _, _, notifications, _) = Build(active);
        Fast? raised = null;
        sut.FastCompleted += (_, f) => raised = f;

        var ended = await sut.EndAsync(active.Id, FastEndReason.Completed);

        ended.EndUtc.Should().NotBeNull();
        ended.EndReason.Should().Be(FastEndReason.Completed);
        notifications.Verify(n => n.CancelForFastAsync(active.Id), Times.Once);
        raised.Should().BeSameAs(ended);
    }

    [Fact]
    public async Task EndAsync_throws_on_unknown_id()
    {
        var (sut, _, _, _, _) = Build();
        await sut.Awaiting(s => s.EndAsync(Guid.NewGuid(), FastEndReason.Other))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EndAsync_rejects_double_end()
    {
        var done = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _) = Build(done);
        await sut.Awaiting(s => s.EndAsync(done.Id, FastEndReason.Completed))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EndAsync_rejects_end_before_start()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow, EndUtc = null, GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _) = Build(active);
        await sut.Awaiting(s => s.EndAsync(active.Id, FastEndReason.Other, DateTime.UtcNow.AddHours(-1)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EditTimesAsync_records_original_times_once_for_audit_trail()
    {
        var originalStart = DateTime.UtcNow.AddHours(-10);
        var originalEnd = DateTime.UtcNow.AddHours(-2);
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = originalStart, EndUtc = originalEnd, GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _) = Build(fast);

        var newStart = originalStart.AddHours(-1);
        var newEnd = originalEnd.AddHours(1);
        var updated = await sut.EditTimesAsync(fast.Id, newStart, newEnd);

        updated.OriginalStartUtc.Should().Be(originalStart);
        updated.OriginalEndUtc.Should().Be(originalEnd);
        updated.StartUtc.Should().BeCloseTo(newStart, TimeSpan.FromSeconds(1));
        updated.EndUtc.Should().BeCloseTo(newEnd, TimeSpan.FromSeconds(1));

        // Editing again must NOT overwrite OriginalStartUtc.
        var newerStart = newStart.AddHours(-1);
        var twice = await sut.EditTimesAsync(fast.Id, newerStart, newEnd);
        twice.OriginalStartUtc.Should().Be(originalStart);
    }

    [Fact]
    public async Task EditTimesAsync_validates_future_start_and_inverted_window()
    {
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-8), EndUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _) = Build(fast);
        await sut.Awaiting(s => s.EditTimesAsync(fast.Id, DateTime.UtcNow.AddHours(1), null))
            .Should().ThrowAsync<ArgumentException>();
        await sut.Awaiting(s => s.EditTimesAsync(fast.Id, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-5)))
            .Should().ThrowAsync<ArgumentException>();
    }
}
