using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Default no-op implementation used by tests and platforms that don't
/// have a native live-indicator yet (iOS Live Activities are TODO).
/// </summary>
public sealed class NullFastingTickerService : IFastingTickerService
{
    public Task StartAsync(string title, DateTime startUtc, string? subtitle = null) => Task.CompletedTask;
    public Task StopAsync() => Task.CompletedTask;
}
