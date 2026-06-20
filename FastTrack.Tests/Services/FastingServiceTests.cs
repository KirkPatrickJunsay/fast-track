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
                    Mock<IFastingTickerService> Ticker,
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
        var ticker = new Mock<IFastingTickerService>();
        var sut = new FastingService(fasts.Object, protocols.Object, notifications.Object, ticker.Object);
        return (sut, fasts, protocols, notifications, ticker, store);
    }

    [Fact]
    public async Task StartAsync_creates_active_fast_with_protocol_goal()
    {
        var (sut, _, _, notifications, _, store) = Build();
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
        var (sut, _, _, _, _, _) = Build(active);
        var act = () => sut.StartAsync(Default.Id);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("A fast is already active*");
    }

    [Fact]
    public async Task StartAsync_throws_when_protocol_not_found()
    {
        var (sut, _, protocols, _, _, _) = Build();
        protocols.Setup(p => p.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((FastingProtocol?)null);
        await sut.Awaiting(s => s.StartAsync(Guid.NewGuid())).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartAsync_rejects_future_start_time()
    {
        var (sut, _, _, _, _, _) = Build();
        await sut.Awaiting(s => s.StartAsync(Default.Id, DateTime.UtcNow.AddHours(1)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task StartAsync_accepts_backdated_start_and_swallows_notification_errors()
    {
        var (sut, _, _, notifications, _, _) = Build();
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
        var (sut, _, _, notifications, _, _) = Build(active);
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
        var (sut, _, _, _, _, _) = Build();
        await sut.Awaiting(s => s.EndAsync(Guid.NewGuid(), FastEndReason.Other))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EndAsync_rejects_double_end()
    {
        var done = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _, _) = Build(done);
        await sut.Awaiting(s => s.EndAsync(done.Id, FastEndReason.Completed))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task EndAsync_rejects_end_before_start()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow, EndUtc = null, GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _, _) = Build(active);
        await sut.Awaiting(s => s.EndAsync(active.Id, FastEndReason.Other, DateTime.UtcNow.AddHours(-1)))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task EditTimesAsync_records_original_times_once_for_audit_trail()
    {
        var originalStart = DateTime.UtcNow.AddHours(-10);
        var originalEnd = DateTime.UtcNow.AddHours(-2);
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = originalStart, EndUtc = originalEnd, GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _, _) = Build(fast);

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
    public async Task StartAsync_starts_the_live_ticker_with_fast_start_time()
    {
        var (sut, _, _, _, ticker, _) = Build();
        var fast = await sut.StartAsync(Default.Id);
        ticker.Verify(t => t.StartAsync(
                It.Is<string>(s => s.Contains("Fasting")),
                fast.StartUtc,
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task StartAsync_swallows_ticker_failures()
    {
        var (sut, _, _, _, ticker, _) = Build();
        ticker.Setup(t => t.StartAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>()))
              .ThrowsAsync(new Exception("notification denied"));
        // A failed indicator must NOT prevent a fast from starting.
        var fast = await sut.StartAsync(Default.Id);
        fast.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task EndAsync_stops_the_live_ticker()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), ProtocolId = Default.Id, GoalHours = 16 };
        var (sut, _, _, _, ticker, _) = Build(active);
        await sut.EndAsync(active.Id, FastEndReason.Completed);
        ticker.Verify(t => t.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task EditTimesAsync_refreshes_ticker_when_still_active()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, _, _, ticker, _) = Build(active);

        var newStart = DateTime.UtcNow.AddHours(-4);
        await sut.EditTimesAsync(active.Id, newStart, null);

        ticker.Verify(t => t.StartAsync(
                It.IsAny<string>(),
                It.Is<DateTime>(d => Math.Abs((d - newStart).TotalSeconds) < 2),
                It.IsAny<string?>()),
            Times.Once);
    }

    [Fact]
    public async Task EditTimesAsync_stops_ticker_when_setting_end_time()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, _, _, ticker, _) = Build(active);
        await sut.EditTimesAsync(active.Id, active.StartUtc, DateTime.UtcNow);
        ticker.Verify(t => t.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task ChangeProtocolAsync_updates_goal_and_protocol_id_on_active_fast()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, protocols, _, _, _) = Build(active);
        var newProtocol = new FastingProtocol
        {
            Id = Guid.NewGuid(), Name = "OMAD", FastHours = 23, EatHours = 1, Difficulty = Difficulty.Advanced,
        };
        protocols.Setup(p => p.GetByIdAsync(newProtocol.Id)).ReturnsAsync(newProtocol);

        var result = await sut.ChangeProtocolAsync(active.Id, newProtocol.Id);

        result.ProtocolId.Should().Be(newProtocol.Id);
        result.GoalHours.Should().Be(23);
    }

    [Fact]
    public async Task ChangeProtocolAsync_reschedules_notifications_and_refreshes_ticker()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-1), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, protocols, notifications, ticker, _) = Build(active);
        var newProtocol = new FastingProtocol
        {
            Id = Guid.NewGuid(), Name = "20:4", FastHours = 20, EatHours = 4, Difficulty = Difficulty.Advanced,
        };
        protocols.Setup(p => p.GetByIdAsync(newProtocol.Id)).ReturnsAsync(newProtocol);

        await sut.ChangeProtocolAsync(active.Id, newProtocol.Id);

        notifications.Verify(n => n.CancelForFastAsync(active.Id), Times.Once);
        notifications.Verify(n => n.ScheduleForFastAsync(It.Is<Fast>(f => f.Id == active.Id), newProtocol), Times.Once);
        ticker.Verify(t => t.StartAsync(It.Is<string>(s => s.Contains("20:4")), active.StartUtc, It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ChangeProtocolAsync_throws_when_fast_already_ended()
    {
        var ended = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-16), EndUtc = DateTime.UtcNow, ProtocolId = Default.Id, GoalHours = 16 };
        var (sut, _, _, _, _, _) = Build(ended);
        await sut.Awaiting(s => s.ChangeProtocolAsync(ended.Id, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot change the protocol of an ended fast.");
    }

    [Fact]
    public async Task ChangeProtocolAsync_throws_when_protocol_not_found()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-1), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, protocols, _, _, _) = Build(active);
        protocols.Setup(p => p.GetByIdAsync(It.Is<Guid>(g => g != Default.Id)))
                 .ReturnsAsync((FastingProtocol?)null);
        await sut.Awaiting(s => s.ChangeProtocolAsync(active.Id, Guid.NewGuid()))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task AddPastFastAsync_creates_completed_record_without_raising_event()
    {
        var (sut, _, _, notifications, ticker, store) = Build();
        var raisedCount = 0;
        sut.FastCompleted += (_, _) => raisedCount++;

        var start = DateTime.UtcNow.AddDays(-2).AddHours(-16);
        var end = DateTime.UtcNow.AddDays(-2);
        var fast = await sut.AddPastFastAsync(Default.Id, start, end, FastEndReason.Completed);

        fast.Id.Should().NotBe(Guid.Empty);
        fast.ProtocolId.Should().Be(Default.Id);
        fast.EndUtc.Should().NotBeNull();
        fast.EndReason.Should().Be(FastEndReason.Completed);
        store[fast.Id].Should().BeSameAs(fast);

        // Critical: migration must NOT fire gamification or notifications.
        raisedCount.Should().Be(0);
        notifications.Verify(n => n.ScheduleForFastAsync(It.IsAny<Fast>(), It.IsAny<FastingProtocol>()), Times.Never);
        notifications.Verify(n => n.CancelForFastAsync(It.IsAny<Guid>()), Times.Never);
        ticker.Verify(t => t.StartAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>()), Times.Never);
        ticker.Verify(t => t.StopAsync(), Times.Never);
    }

    [Fact]
    public async Task AddPastFastAsync_throws_when_start_after_end()
    {
        var (sut, _, _, _, _, _) = Build();
        var act = () => sut.AddPastFastAsync(Default.Id, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-2), FastEndReason.Completed);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPastFastAsync_throws_when_end_in_future()
    {
        var (sut, _, _, _, _, _) = Build();
        var act = () => sut.AddPastFastAsync(Default.Id, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(2), FastEndReason.Completed);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AddPastFastAsync_throws_when_protocol_unknown()
    {
        var (sut, _, _, _, _, _) = Build();
        var act = () => sut.AddPastFastAsync(Guid.NewGuid(), DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(-1), FastEndReason.Completed);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ChangeProtocolAsync_swapping_to_same_protocol_is_noop()
    {
        var active = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-2), ProtocolId = Default.Id, GoalHours = 16, EndUtc = null };
        var (sut, _, _, notifications, ticker, _) = Build(active);

        var result = await sut.ChangeProtocolAsync(active.Id, Default.Id);

        result.ProtocolId.Should().Be(Default.Id);
        notifications.Verify(n => n.CancelForFastAsync(It.IsAny<Guid>()), Times.Never);
        ticker.Verify(t => t.StartAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task EditTimesAsync_validates_future_start_and_inverted_window()
    {
        var fast = new Fast { Id = Guid.NewGuid(), StartUtc = DateTime.UtcNow.AddHours(-8), EndUtc = DateTime.UtcNow.AddHours(-1), GoalHours = 16, ProtocolId = Default.Id };
        var (sut, _, _, _, _, _) = Build(fast);
        await sut.Awaiting(s => s.EditTimesAsync(fast.Id, DateTime.UtcNow.AddHours(1), null))
            .Should().ThrowAsync<ArgumentException>();
        await sut.Awaiting(s => s.EditTimesAsync(fast.Id, DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(-5)))
            .Should().ThrowAsync<ArgumentException>();
    }
}
