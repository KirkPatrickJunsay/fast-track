using FastTrack.Data;
using FastTrack.Models;
using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class FastingService : IFastingService
{
    private readonly IFastRepository _fasts;
    private readonly IFastingProtocolRepository _protocols;
    private readonly INotificationOrchestrator _notifications;

    public FastingService(
        IFastRepository fasts,
        IFastingProtocolRepository protocols,
        INotificationOrchestrator notifications)
    {
        _fasts = fasts;
        _protocols = protocols;
        _notifications = notifications;
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
        return fast;
    }
}
