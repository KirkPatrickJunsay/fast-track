using FastTrack.Services.Interfaces;

namespace FastTrack.Services.Implementations;

public sealed class MauiHapticService : IHapticService
{
    public void Tick(HapticIntensity intensity = HapticIntensity.Light)
    {
        try
        {
            var feedback = intensity switch
            {
                HapticIntensity.Heavy => HapticFeedbackType.LongPress,
                _ => HapticFeedbackType.Click,
            };
            HapticFeedback.Default.Perform(feedback);
        }
        catch
        {
            // Some devices / iOS Simulator lack the haptic engine — silently ignore.
        }
    }
}
