using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;

namespace FastTrack.Platforms.Android;

/// <summary>
/// Foreground service that hosts the persistent fasting timer notification.
/// The OS draws the elapsed time itself via Chronometer mode — we just give
/// it the reference start time and never touch the notification again.
///
/// Why ForegroundServiceType.SpecialUse rather than .Health:
/// Android 15 (targetSDK=35) requires that an FGS of type Health also hold
/// one of ACTIVITY_RECOGNITION / BODY_SENSORS / HIGH_SAMPLING_RATE_SENSORS.
/// Fast Track is a stopwatch, not a sensor app — so we declare SpecialUse with
/// a subtype property in AndroidManifest.xml that documents the use case for
/// the Play Console review.
/// </summary>
[Service(
    Exported = false,
    Name = "com.codesandchips.fasttrack.FastingForegroundService",
    ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeSpecialUse)]
public sealed class FastingForegroundService : Service
{
    public const string ActionStart = "com.codesandchips.fasttrack.action.START";
    public const string ActionStop  = "com.codesandchips.fasttrack.action.STOP";
    public const string ExtraStartMillis = "startMillis";
    public const string ExtraTitle = "title";
    public const string ExtraSubtitle = "subtitle";

    public const int NotificationId = 4711;
    // Channel id bumped to _v2 to apply a new IMPORTANCE level on devices that
    // already had the old v1 channel (Android freezes channel importance after
    // first creation — only the user can change it). MIUI/HyperOS hides LOW
    // notifications when the shade is collapsed, so we now create the channel
    // at DEFAULT importance to stay visible.
    public const string ChannelId = "fasttrack_timer_v2";
    private const string ChannelName = "Fasting timer";
    private const string ChannelDescription = "Persistent indicator while a fast is in progress.";

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == ActionStop)
        {
            StopForeground(StopForegroundFlags.Remove);
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        var startMillis = intent?.GetLongExtra(ExtraStartMillis, 0L) ?? 0L;
        var title = intent?.GetStringExtra(ExtraTitle) ?? "Fasting";
        var subtitle = intent?.GetStringExtra(ExtraSubtitle);

        EnsureChannel();
        var notification = BuildNotification(startMillis, title, subtitle);

        // Android 14+ refuses startForeground without the matching service type bit.
        if (Build.VERSION.SdkInt >= BuildVersionCodes.UpsideDownCake)
        {
            StartForeground(NotificationId, notification,
                global::Android.Content.PM.ForegroundService.TypeSpecialUse);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        // Sticky so the OS restores the timer if the process is killed and respawned.
        return StartCommandResult.Sticky;
    }

    private Notification BuildNotification(long startMillis, string title, string? subtitle)
    {
        var tapIntent = PackageManager?.GetLaunchIntentForPackage(PackageName!);
        var pendingFlags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
        var contentIntent = tapIntent is null
            ? null
            : PendingIntent.GetActivity(this, 0, tapIntent, pendingFlags);

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetSmallIcon(global::Android.Resource.Drawable.IcDialogInfo)  // fallback; replaced below if our drawable is available
            .SetContentTitle(title)
            .SetUsesChronometer(true)        // <-- the magic: OS animates elapsed time
            .SetShowWhen(true)
            .SetWhen(startMillis)
            .SetOngoing(true)                // user can't swipe it away
            .SetSilent(true)                 // no sound on first post
            .SetCategory(NotificationCompat.CategoryStopwatch)
            .SetPriority(NotificationCompat.PriorityDefault)
            // Lock-screen visibility so the user can see the timer without unlocking.
            .SetVisibility(NotificationCompat.VisibilityPublic)
            // Android 12+ normally delays FGS notifications up to 10s. Immediate
            // tells the OS to show it as soon as startForeground is called.
            .SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);

        if (!string.IsNullOrWhiteSpace(subtitle))
        {
            builder.SetContentText(subtitle);
        }

        if (contentIntent is not null)
        {
            builder.SetContentIntent(contentIntent);
        }

        // Prefer our themed app icon if MAUI generated one (xamarinandroid resizetizer
        // produces appicon under @drawable too); fall back gracefully if it isn't present.
        var iconId = Resources?.GetIdentifier("appiconfg", "drawable", PackageName)
                  ?? 0;
        if (iconId != 0)
        {
            builder.SetSmallIcon(iconId);
        }

        return builder.Build()!;
    }

    private void EnsureChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
        var manager = (NotificationManager?)GetSystemService(NotificationService);
        if (manager is null) return;
        if (manager.GetNotificationChannel(ChannelId) is not null) return;

        // IMPORTANCE_DEFAULT — sticky indicator that stays visible in the
        // collapsed shade on OEMs (MIUI, One UI) that suppress LOW notifications.
        // Still silent + no vibration because it's an ambient indicator.
        var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Default)
        {
            Description = ChannelDescription,
        };
        channel.SetShowBadge(false);
        channel.EnableVibration(false);
        channel.SetSound(null, null);
        channel.LockscreenVisibility = NotificationVisibility.Public;
        manager.CreateNotificationChannel(channel);
    }
}
