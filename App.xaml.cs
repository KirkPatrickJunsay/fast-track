using FastTrack.Services.Interfaces;

namespace FastTrack;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		InitializeComponent();
		_services = services;

		// Gamification listens for FastCompleted events for the lifetime of the app.
		try
		{
			_services.GetRequiredService<IGamificationOrchestrator>().Start();
		}
		catch { /* gamification is non-critical for app startup */ }
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var shell = _services.GetRequiredService<AppShell>();
		var window = new Window(shell);

		// US-07.5 Re-engagement: cancel pending reminders on app resume,
		// reschedule on sleep so we only ping a user who's actually gone quiet.
		window.Resumed += async (_, _) =>
		{
			try
			{
				var orchestrator = _services.GetRequiredService<INotificationOrchestrator>();
				await orchestrator.CancelReengagementAsync();
			}
			catch { /* notifications are non-critical for app startup */ }
		};

		window.Stopped += async (_, _) =>
		{
			try
			{
				var orchestrator = _services.GetRequiredService<INotificationOrchestrator>();
				await orchestrator.RescheduleReengagementAsync();
			}
			catch { }
		};

		return window;
	}
}
