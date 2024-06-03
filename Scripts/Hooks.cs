using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.izumisQOL.OBS;
using Celeste.Mod.izumisQOL.UI;

namespace Celeste.Mod.izumisQOL
{
	public static class Hooks
	{
		internal static void Load()
		{
			On.Monocle.Engine.Update += Update;
			On.Celeste.OuiJournal.Update += BetterJournalModule.Update;
			On.Celeste.OuiJournalProgress.ctor += BetterJournalModule.OuiJournalProgressCtor;
			On.Celeste.OuiJournalPage.Redraw += BetterJournalModule.OnJournalPageRedraw;
			Everest.Events.Journal.OnEnter += BetterJournalModule.OnJournalEnter;
			On.Celeste.OuiJournal.Close += BetterJournalModule.OnJournalClose;
			Everest.Events.MainMenu.OnCreateButtons += General.OnCreateButtons;
			On.Monocle.Scene.Begin += Indicator.OnSceneBegin;
			On.Celeste.Level.Begin += OBSIntegration.OnLevelBegin;

			NoClipModule.Load();
			KeybindModule.Load();
			BetterJournalModule.Load();
		}

		internal static void Unload()
		{
			On.Monocle.Engine.Update -= Update;
			On.Celeste.OuiJournal.Update -= BetterJournalModule.Update;
			On.Celeste.OuiJournalProgress.ctor -= BetterJournalModule.OuiJournalProgressCtor;
			On.Celeste.OuiJournalPage.Redraw -= BetterJournalModule.OnJournalPageRedraw;
			Everest.Events.Journal.OnEnter -= BetterJournalModule.OnJournalEnter;
			On.Celeste.OuiJournal.Close -= BetterJournalModule.OnJournalClose;
			Everest.Events.MainMenu.OnCreateButtons -= General.OnCreateButtons;
			On.Monocle.Scene.Begin -= Indicator.OnSceneBegin;
			On.Celeste.Level.Begin -= OBSIntegration.OnLevelBegin;

			NoClipModule.Unload();

			ModSettings.ButtonsSwapKeybinds.Clear();
		}

		private static void Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime)
		{
			orig(self, gameTime);

			KeybindModule.Update();
			GamepadPauser.Update();
			OBSIntegration.Update();
		}
	}
}
