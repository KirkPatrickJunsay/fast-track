using FastTrack.Data;

namespace FastTrack.ViewModels;

/// <summary>
/// Cold-launch traffic controller. Lives behind a visually neutral BootPage that
/// matches the system splash, reads the persisted UserProfile, and decides whether
/// the user belongs in the onboarding wizard or on the main tab bar.
///
/// Pulling this out of AppShell.OnNavigated means the first frame the user sees
/// after the splash is *always* the right page — no Home flash for first-run users,
/// no Onboarding flash for returning users.
/// </summary>
public sealed class BootViewModel
{
    public const string OnboardingRoute = "//OnboardingPage";
    public const string MainRoute = "//MainPage";

    private readonly IUserProfileRepository _profiles;

    public BootViewModel(IUserProfileRepository profiles)
    {
        _profiles = profiles;
    }

    /// <summary>
    /// Returns the Shell route the boot page should navigate to. Defensive against
    /// repository failures — if the profile read throws, we send the user into
    /// onboarding so they can re-establish the profile rather than hanging on the boot screen.
    /// </summary>
    public async Task<string> DecideRouteAsync()
    {
        try
        {
            var profile = await _profiles.GetOrCreateAsync();
            return profile.OnboardingCompleted ? MainRoute : OnboardingRoute;
        }
        catch
        {
            return OnboardingRoute;
        }
    }
}
