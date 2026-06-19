using FastTrack.Views;

namespace FastTrack;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Modal / detail routes (not in TabBar / not auto-created).
		Routing.RegisterRoute("CustomProtocolPage", typeof(CustomProtocolPage));
		Routing.RegisterRoute("EditFastPage", typeof(EditFastPage));
		Routing.RegisterRoute("NotificationPreferencesPage", typeof(NotificationPreferencesPage));
		Routing.RegisterRoute("TrophyCabinetPage", typeof(TrophyCabinetPage));
		Routing.RegisterRoute("LogWeightPage", typeof(LogWeightPage));
		Routing.RegisterRoute("LogMoodPage", typeof(LogMoodPage));
		Routing.RegisterRoute("DataManagementPage", typeof(DataManagementPage));
		Routing.RegisterRoute("FastDetailPage", typeof(FastDetailPage));
		Routing.RegisterRoute("CelebrationPage", typeof(CelebrationPage));
		Routing.RegisterRoute("StageDetailPage", typeof(StageDetailPage));
		Routing.RegisterRoute("PrivacyPage", typeof(PrivacyPage));
		Routing.RegisterRoute("ArticleDetailPage", typeof(ArticleDetailPage));
		Routing.RegisterRoute("CustomizeHomePage", typeof(CustomizeHomePage));
		// Cold-launch routing is owned by BootPage now (the default ShellContent).
	}
}
