using FastTrack.Data;
using FastTrack.Services.Implementations;
using FastTrack.Services.Interfaces;
using FastTrack.ViewModels;
using FastTrack.Views;
using Microcharts.Maui;
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
			.UseMicrocharts()
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
		services.AddSingleton<IWeightRepository, WeightRepository>();
		services.AddSingleton<IMoodRepository, MoodRepository>();
		services.AddSingleton<IWaterRepository, WaterRepository>();
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
		services.AddSingleton<IWeightService, WeightService>();
		services.AddSingleton<IMoodService, MoodService>();
		services.AddSingleton<IWaterService, WaterService>();
		services.AddSingleton<IDataExportService, DataExportService>();
		services.AddSingleton<IDataImportService, DataImportService>();
		services.AddSingleton<IDataResetService, DataResetService>();
		services.AddSingleton<IFileShareService, MauiFileShareService>();
		services.AddSingleton<IFilePickerService, MauiFilePickerService>();
		services.AddSingleton<IBadgeService, BadgeService>();
		services.AddSingleton<IQuestService, QuestService>();
		services.AddSingleton<IGamificationOrchestrator, GamificationOrchestrator>();
		services.AddSingleton<IAnalyticsService, AnalyticsService>();
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
		services.AddTransient<LogWeightViewModel>();
		services.AddTransient<LogMoodViewModel>();
		services.AddTransient<DataManagementViewModel>();
		services.AddTransient<InsightsViewModel>();
		services.AddTransient<FastDetailViewModel>();
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
		services.AddTransient<LogWeightPage>();
		services.AddTransient<LogMoodPage>();
		services.AddTransient<DataManagementPage>();
		services.AddTransient<FastDetailPage>();
	}
}
