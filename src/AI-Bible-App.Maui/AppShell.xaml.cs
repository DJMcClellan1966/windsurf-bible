using AI_Bible_App.Maui.Views;

namespace AI_Bible_App.Maui;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register routes
		Routing.RegisterRoute("chat", typeof(ChatPage));
		Routing.RegisterRoute("prayer", typeof(PrayerPage));
		Routing.RegisterRoute("userselection", typeof(UserSelectionPage));
		
		// Multi-character routes
		Routing.RegisterRoute("MultiCharacterSelectionPage", typeof(MultiCharacterSelectionPage));
		Routing.RegisterRoute("RoundtableChatPage", typeof(RoundtableChatPage));
		Routing.RegisterRoute("WisdomCouncilPage", typeof(WisdomCouncilPage));
		Routing.RegisterRoute("PrayerChainPage", typeof(PrayerChainPage));
	}
}
