using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class FastingService : IFastingService
{
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly INotificationOrchestrator _notifications;
    private readonly IFastingTickerService _ticker;

    public FastingService(
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        INotificationOrchestrator notifications,
        IFastingTickerService ticker)
    {
        _fasts = fasts;
        _protocols = protocols;
        _notifications = notifications;
        _ticker = ticker;
    }

    public event EventHandler<Fast>? FastCompleted;

    public Task<Fast?> GetActiveAsync() => _fasts.GetActiveAsync();

    public async Task<Fast> StartAsync(Guid protocolId, DateTime? startUtc = null)
    {
        var existing = await _fasts.GetActiveAsync();
        if (existing is not null)
        {
            throw new InvalidOperationException("A fast is already active. End it before starting a new one.");
        }

        var protocol = await _protocols.GetByIdAsync(protocolId)
            ?? throw new InvalidOperationException($"Protocol {protocolId} not found.");

        var start = (startUtc ?? DateTime.UtcNow).ToUniversalTime();
        if (start > DateTime.UtcNow)
        {
            throw new ArgumentException("Start time cannot be in the future.", nameof(startUtc));
        }

        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            GoalHours = protocol.FastHours,
            StartUtc = start,
            EndUtc = null,
        };

        await _fasts.UpsertAsync(fast);

        // Fire-and-await notification scheduling; failures here mustn't block start.
        try { await _notifications.ScheduleForFastAsync(fast, protocol); }
        catch { /* swallow — notifications are non-critical for correctness */ }

        // Live indicator (Android foreground service + chronometer notification).
        // A missing notification permission or denied foreground-service grant must
        // never prevent the user from starting a fast.
        try
        {
            await _ticker.StartAsync(
                title: $"Fasting · {protocol.Name}",
                startUtc: fast.StartUtc,
                subtitle: $"Goal {protocol.FastHours:0.#}h");
        }
        catch { /* indicator is non-critical */ }

        return fast;
    }

    public async Task<Fast> EndAsync(Guid fastId, FastEndReason reason, DateTime? endUtc = null)
    {
        var fast = await _fasts.GetByIdAsync(fastId)
            ?? throw new InvalidOperationException($"Fast {fastId} not found.");

        if (fast.EndUtc is not null)
        {
            throw new InvalidOperationException("Fast already ended.");
        }

        var end = (endUtc ?? DateTime.UtcNow).ToUniversalTime();
        if (end < fast.StartUtc)
        {
            throw new ArgumentException("End time cannot be before start time.", nameof(endUtc));
        }

        fast.EndUtc = end;
        fast.EndReason = reason;

        await _fasts.UpsertAsync(fast);

        try { await _notifications.CancelForFastAsync(fast.Id); }
        catch { /* notifications are non-critical */ }

        try { await _ticker.StopAsync(); }
        catch { /* indicator is non-critical */ }

        FastCompleted?.Invoke(this, fast);
        return fast;
    }

    public async Task<Fast> EditTimesAsync(Guid fastId, DateTime newStartUtc, DateTime? newEndUtc)
    {
        var fast = await _fasts.GetByIdAsync(fastId)
            ?? throw new InvalidOperationException($"Fast {fastId} not found.");

        newStartUtc = newStartUtc.ToUniversalTime();
        var newEnd = newEndUtc?.ToUniversalTime();

        if (newStartUtc > DateTime.UtcNow)
        {
            throw new ArgumentException("Start time cannot be in the future.", nameof(newStartUtc));
        }
        if (newEnd is not null && newEnd < newStartUtc)
        {
            throw new ArgumentException("End time cannot be before start time.", nameof(newEndUtc));
        }

        // Preserve original times once for the audit trail (US-01.7).
        fast.OriginalStartUtc ??= fast.StartUtc;
        if (fast.EndUtc is not null) fast.OriginalEndUtc ??= fast.EndUtc;

        fast.StartUtc = newStartUtc;
        fast.EndUtc = newEnd;

        await _fasts.UpsertAsync(fast);

        // If they edited an active fast's start time, refresh the live indicator
        // so the chronometer reflects the new reference. If the fast is now over,
        // tear it down.
        try
        {
            if (newEnd is null)
            {
                var protocol = await _protocols.GetByIdAsync(fast.ProtocolId);
                var name = protocol?.Name ?? "Custom";
                var goalHours = protocol?.FastHours ?? fast.GoalHours;
                await _ticker.StartAsync(
                    title: $"Fasting · {name}",
                    startUtc: fast.StartUtc,
                    subtitle: $"Goal {goalHours:0.#}h");
            }
            else
            {
                await _ticker.StopAsync();
            }
        }
        catch { /* indicator is non-critical */ }

        return fast;
    }

    public async Task<Fast> AddPastFastAsync(
        Guid protocolId, DateTime startUtc, DateTime endUtc, FastEndReason reason)
    {
        var protocol = await _protocols.GetByIdAsync(protocolId)
            ?? throw new InvalidOperationException($"Protocol {protocolId} not found.");

        var startUtcN = startUtc.ToUniversalTime();
        var endUtcN = endUtc.ToUniversalTime();
        var now = DateTime.UtcNow;
        if (startUtcN >= endUtcN)
        {
            throw new ArgumentException("Start time must be before end time.", nameof(startUtc));
        }
        if (startUtcN > now)
        {
            throw new ArgumentException("Start time cannot be in the future.", nameof(startUtc));
        }
        if (endUtcN > now)
        {
            throw new ArgumentException("End time cannot be in the future.", nameof(endUtc));
        }

        var fast = new Fast
        {
            Id = Guid.NewGuid(),
            ProtocolId = protocol.Id,
            GoalHours = protocol.FastHours,
            StartUtc = startUtcN,
            EndUtc = endUtcN,
            EndReason = reason,
        };

        await _fasts.UpsertAsync(fast);

        // Deliberately NOT raising FastCompleted — migrating a back-catalog of
        // fasts must not spam the user with XP grants, badge unlocks, or the
        // celebration page. Streaks recompute from the DB on read so the user's
        // streak history will reflect the new rows on next query.
        return fast;
    }

    public async Task<Fast> ChangeProtocolAsync(Guid fastId, Guid newProtocolId)
    {
        var fast = await _fasts.GetByIdAsync(fastId)
            ?? throw new InvalidOperationException($"Fast {fastId} not found.");

        if (fast.EndUtc is not null)
        {
            throw new InvalidOperationException("Cannot change the protocol of an ended fast.");
        }

        var newProtocol = await _protocols.GetByIdAsync(newProtocolId)
            ?? throw new InvalidOperationException($"Protocol {newProtocolId} not found.");

        if (fast.ProtocolId == newProtocolId)
        {
            return fast; // no-op — swap to the same protocol
        }

        fast.ProtocolId = newProtocol.Id;
        fast.GoalHours = newProtocol.FastHours;
        await _fasts.UpsertAsync(fast);

        // Stage milestones are tied to goal hours — reschedule with the new protocol.
        try { await _notifications.CancelForFastAsync(fast.Id); } catch { /* non-critical */ }
        try { await _notifications.ScheduleForFastAsync(fast, newProtocol); } catch { /* non-critical */ }

        // Live indicator title / subtitle now references the new protocol.
        try
        {
            await _ticker.StartAsync(
                title: $"Fasting · {newProtocol.Name}",
                startUtc: fast.StartUtc,
                subtitle: $"Goal {newProtocol.FastHours:0.#}h");
        }
        catch { /* indicator is non-critical */ }

        return fast;
    }
}
