using ITKamianets.Engine.Shell;
using UnityEngine;

namespace Temnorid.Master
{
	/// <summary>
	/// What the shared shell (splash, landing, lobby, top and bottom bars) needs to know about this game. The shell's own screens
	/// are in English; the places of the in-game bars are in Ukrainian like the rest of the game.
	/// </summary>
	public static class MasterShell
	{
		public const string Repo = "WebArtWork/master.temnorid.com";

		/// <summary>The place ids of the in-game bar. The first is the workshop itself (the hub, the order boards, the tasks and the stable).</summary>
		public const string PlaceWorkshop = "workshop";
		public const string PlaceOverview = "overview";

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Configure()
		{
			ShellConfig.Current = new ShellConfig
			{
				Title = "Temnorid Master",
				Tagline = "Seven trades, twelve orders, one shared workshop",
				GameId = "master",
				Repo = Repo,
				InstallerSuffix = "-Setup.exe",
				MaxPlayers = 4,
				// Single-player on the landing screen; the Profile screen comes later (the in-game Profile place stays).
				Multiplayer = false,
				Profile = false,
				// After sign-in the game opens in its own village scene, not on the shell's title screen.
				SkipLanding = true,
				GameScene = "Master",
				Accent = new Color(0.93f, 0.76f, 0.36f),
				Places =
				{
					new ShellPlace("village", Loc.T("village.tab"), "С"),
				},
			};
		}
	}
}
