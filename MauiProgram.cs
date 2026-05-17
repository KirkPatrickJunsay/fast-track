using FastTrack.Data;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FastTrack.Views;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;

namespace FastTrack;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseLocalNotification()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		RegisterData(builder.Services);
		RegisterServices(builder.Services);
		RegisterViewModels(builder.Services);
		RegisterViews(builder.Services);

		builder.Services.AddSingleton<App>();
		builder.Services.AddSingleton<AppShell>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static void RegisterData(IServiceCollection services)
	{
		services.AddSingleton<IDatabasePathProvider, MauiDatabasePathProvider>();
		services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
		services.AddSingleton<IFastRepository, FastRepository>();
		services.AddSingleton<IFastingProtocolRepository, FastingProtocolRepository>();
		services.AddSingleton<IUserProfileRepository, UserProfileRepository>();
		services.AddSingleton<IEarnedBadgeRepository, EarnedBadgeRepository>();
		services.AddSingleton<IDailyQuestRepository, DailyQuestRepository>();
	}

	private static void RegisterServices(IServiceCollection services)
	{
		services.AddSingleton<IStageCalculator, StageCalculator>();
		services.AddSingleton<IFastTrackNotificationService, NotificationService>();
		services.AddSingleton<INotificationOrchestrator, NotificationOrchestrator>();
		services.AddSingleton<IFastingService, FastingService>();
		services.AddSingleton<IDialogService, DialogService>();
		services.AddSingleton<INavigationService, ShellNavigationService>();
		services.AddSingleton<ITicker, MauiDispatcherTicker>();
		services.AddSingleton<IStreakService, StreakService>();
		services.AddSingleton<IXpService, XpService>();
		services.AddSingleton<IBadgeService, BadgeService>();
		services.AddSingleton<IQuestService, QuestService>();
		services.AddSingleton<IGamificationOrchestrator, GamificationOrchestrator>();
	}

	private static void RegisterViewModels(IServiceCollection services)
	{
		services.AddTransient<HomeViewModel>();
		services.AddTransient<ProtocolsViewModel>();
		services.AddTransient<CustomProtocolViewModel>();
		services.AddTransient<HistoryViewModel>();
		services.AddTransient<EditFastViewModel>();
		services.AddTransient<OnboardingViewModel>();
		services.AddTransient<SettingsViewModel>();
		services.AddTransient<NotificationPreferencesViewModel>();
		services.AddTransient<TrophyCabinetViewModel>();
	}

	private static void RegisterViews(IServiceCollection services)
	{
		services.AddTransient<MainPage>();
		services.AddTransient<ProtocolsPage>();
		services.AddTransient<CustomProtocolPage>();
		services.AddTransient<HistoryPage>();
		services.AddTransient<EditFastPage>();
		services.AddTransient<InsightsPage>();
		services.AddTransient<SettingsPage>();
		services.AddTransient<OnboardingPage>();
		services.AddTransient<NotificationPreferencesPage>();
		services.AddTransient<TrophyCabinetPage>();
	}
}
