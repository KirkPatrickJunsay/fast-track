using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class MauiDispatcherTicker : ITicker
{
    private IDispatcherTimer? _timer;

    public void Start(TimeSpan interval, Action onTick)
    {
        Stop();
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null) return;
        _timer = dispatcher.CreateTimer();
        _timer.Interval = interval;
        _timer.IsRepeating = true;
        _timer.Tick += (_, _) => onTick();
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer is null) return;
        _timer.Stop();
        _timer = null;
    }
}
