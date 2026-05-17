using FastTrack.Data;
using FastTrack.Views;

namespace FastTrack;

public partial class AppShell : Shell
{
	private readonly IUserProfileRepository _profiles;
	private bool _onboardingChecked;

	public AppShell(IUserProfileRepository profiles)
	{
		InitializeComponent();
		_profiles = profiles;

		// Modal / detail routes (not in TabBar / not auto-created).
		Routing.RegisterRoute("CustomProtocolPage", typeof(CustomProtocolPage));
		Routing.RegisterRoute("EditFastPage", typeof(EditFastPage));
		Routing.RegisterRoute("NotificationPreferencesPage", typeof(NotificationPreferencesPage));
		Routing.RegisterRoute("TrophyCabinetPage", typeof(TrophyCabinetPage));
	}

	protected override async void OnNavigated(ShellNavigatedEventArgs args)
	{
		base.OnNavigated(args);

		if (_onboardingChecked) return;
		_onboardingChecked = true;

		try
		{
			var profile = await _profiles.GetOrCreateAsync();
			if (!profile.OnboardingCompleted)
			{
				await GoToAsync("//OnboardingPage");
			}
		}
		catch
		{
			// Don't block app launch on a profile read failure — onboarding will get another chance.
			_onboardingChecked = false;
		}
	}
}
