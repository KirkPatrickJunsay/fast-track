namespace FastTrack.Services.Interfaces;

/// <summary>
/// Abstraction over a one-shot or repeating dispatcher timer so HomeViewModel is testable.
/// </summary>
public interface ITicker
{
    void Start(TimeSpan interval, Action onTick);
    void Stop();
}
