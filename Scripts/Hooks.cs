﻿using Microsoft.Xna.Framework;
using Monocle;
using Celeste.Mod.izumisQOL.ModIntegration;

using System.Collections.Generic;

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

			ModSettings.ButtonsSwapKeybinds.Clear();
		}

		private static void Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime)
		{
			orig(self, gameTime);

			KeybindModule.Update();
			GamepadPauser.Update();
		}
	}
}
