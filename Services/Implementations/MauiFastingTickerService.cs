#if ANDROID
using Android.Content;
using FastTrack.Platforms.Android;
using FastTrack.Services.Interfaces;
using Plugin.LocalNotification;

namespace FastTrack.Services.Implementations;

/// <summary>
/// Android bridge from the Core IFastingTickerService contract to the
/// FastingForegroundService. Just translates Start/Stop into the right
/// service-start intents — all the notification work lives in the Service.
/// </summary>
public sealed class MauiFastingTickerService : IFastingTickerService
{
    public async Task StartAsync(string title, DateTime startUtc, string? subtitle = null)
    {
        // Android 13+ silently drops the foreground-service notification when
        // POST_NOTIFICATIONS hasn't been granted. Trigger the system prompt now
        // (Plugin.LocalNotification handles the platform plumbing) so the user
        // sees the live chronometer the moment they start their first fast.
        try
        {
            var allowed = await LocalNotificationCenter.Current.AreNotificationsEnabled();
            if (!allowed)
            {
                await LocalNotificationCenter.Current.RequestNotificationPermission();
            }
        }
        catch { /* permission flow is best-effort — the FGS still keeps the process alive */ }

        var context = global::Android.App.Application.Context;

        var intent = new Intent(context, typeof(FastingForegroundService));
        intent.SetAction(FastingForegroundService.ActionStart);
        intent.PutExtra(FastingForegroundService.ExtraStartMillis,
            new DateTimeOffset(startUtc.ToUniversalTime()).ToUnixTimeMilliseconds());
        intent.PutExtra(FastingForegroundService.ExtraTitle, title);
        if (subtitle is not null)
        {
            intent.PutExtra(FastingForegroundService.ExtraSubtitle, subtitle);
        }

        // Android 8+ requires the foreground-service start variant.
        context.StartForegroundService(intent);
    }

    public Task StopAsync()
    {
        var context = global::Android.App.Application.Context;
        var intent = new Intent(context, typeof(FastingForegroundService));
        intent.SetAction(FastingForegroundService.ActionStop);
        context.StartService(intent);
        return Task.CompletedTask;
    }
}
#endif
