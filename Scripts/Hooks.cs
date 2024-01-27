using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.izumisQOL.OBS;

using System.Collections.Generic;
using Celeste.Mod.izumisQOL.UI;

namespace Celeste.Mod.izumisQOL
{
	public class Hooks : Global
	{
		internal static void Load()
		{
			On.Monocle.Engine.Update += Update;
			On.Celeste.OuiJournal.Update += BetterJournalModule.Update;
			On.Celeste.OuiJournalProgress.ctor += BetterJournalModule.OuiJournalProgressCtor;
			On.Celeste.OuiJournalPage.Redraw += BetterJournalModule.OnJournalPageRedraw;
			Everest.Events.Journal.OnEnter += BetterJournalModule.OnJournalEnter;
			On.Celeste.OuiJournal.Close += BetterJournalModule.OnJournalClose;
			Everest.Events.MainMenu.OnCreateButtons += UI.General.OnCreateButtons;
			On.Monocle.Scene.Begin += Indicator.OnSceneBegin;
			On.Monocle.Scene.End += Indicator.OnSceneEnd;

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
			Everest.Events.MainMenu.OnCreateButtons -= UI.General.OnCreateButtons;
			On.Monocle.Scene.Begin -= Indicator.OnSceneBegin;
			On.Monocle.Scene.End -= Indicator.OnSceneEnd;

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
