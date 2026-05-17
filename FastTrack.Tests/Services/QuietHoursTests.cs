using FastTrack.Models;
using FastTrack.Services.Implementations;
using FluentAssertions;

namespace FastTrack.Tests.Services;

public class QuietHoursTests
{
    private static NotificationPreferences Prefs(
        bool enabled = true,
        int startHour = 22, int startMin = 0,
        int endHour = 7, int endMin = 0)
        => new()
        {
            QuietHoursEnabled = enabled,
            QuietHoursStart = new TimeSpan(startHour, startMin, 0),
            QuietHoursEnd = new TimeSpan(endHour, endMin, 0),
        };

    [Fact]
    public void Adjust_returns_input_when_disabled()
    {
        var when = new DateTime(2026, 5, 18, 2, 0, 0);
        QuietHours.Adjust(when, Prefs(enabled: false)).Should().Be(when);
    }

    [Fact]
    public void Adjust_returns_input_when_start_equals_end()
    {
        var when = new DateTime(2026, 5, 18, 2, 0, 0);
        QuietHours.Adjust(when, Prefs(startHour: 7, endHour: 7)).Should().Be(when);
    }

    [Fact]
    public void Adjust_shifts_overnight_window_after_midnight_to_morning_end()
    {
        // 22:00 → 07:00 overnight; 02:00 is inside it
        var when = new DateTime(2026, 5, 18, 2, 30, 0);
        var adjusted = QuietHours.Adjust(when, Prefs());
        adjusted.Should().Be(new DateTime(2026, 5, 18, 7, 0, 0));
    }

    [Fact]
    public void Adjust_shifts_overnight_window_before_midnight_to_next_morning()
    {
        // 23:30 is inside 22:00–07:00, window ends at 07:00 the next day
        var when = new DateTime(2026, 5, 18, 23, 30, 0);
        var adjusted = QuietHours.Adjust(when, Prefs());
        adjusted.Should().Be(new DateTime(2026, 5, 19, 7, 0, 0));
    }

    [Fact]
    public void Adjust_leaves_outside_overnight_window_unchanged()
    {
        var when = new DateTime(2026, 5, 18, 12, 0, 0);
        QuietHours.Adjust(when, Prefs()).Should().Be(when);
    }

    [Fact]
    public void Adjust_handles_same_day_window()
    {
        // 13:00–14:00 — same-day window
        var inside = new DateTime(2026, 5, 18, 13, 30, 0);
        QuietHours.Adjust(inside, Prefs(startHour: 13, endHour: 14))
            .Should().Be(new DateTime(2026, 5, 18, 14, 0, 0));

        var outside = new DateTime(2026, 5, 18, 9, 0, 0);
        QuietHours.Adjust(outside, Prefs(startHour: 13, endHour: 14)).Should().Be(outside);
    }

    [Fact]
    public void Adjust_returns_window_end_when_exactly_on_start()
    {
        var when = new DateTime(2026, 5, 18, 22, 0, 0);
        var adjusted = QuietHours.Adjust(when, Prefs());
        adjusted.Should().Be(new DateTime(2026, 5, 19, 7, 0, 0));
    }
}
