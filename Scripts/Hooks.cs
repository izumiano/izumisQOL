using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod.Core;
using Monocle;
using System.IO;

namespace Celeste.Mod.izumisQOL
{
	public class Hooks : Global
	{
		internal static void Load()
		{
			On.Monocle.Engine.Update += Update;
			On.Celeste.OuiJournal.Update += BetterJournalModule.OnJournalUpdate;
			On.Celeste.OuiJournalProgress.ctor += BetterJournalModule.OuiJournalProgressCtor;
			On.Celeste.OuiJournalProgress.Redraw += BetterJournalModule.OnJournalProgressRedraw;
			On.Celeste.OuiJournal.Close += BetterJournalModule.OnJournalClose;

			KeybindModule.Init();
			WhitelistModule.Init();
			BetterJournalModule.Init();
		}

		internal static void Unload()
		{
			On.Monocle.Engine.Update -= Update;
			On.Celeste.OuiJournal.Update -= BetterJournalModule.OnJournalUpdate;
			On.Celeste.OuiJournalProgress.ctor -= BetterJournalModule.OuiJournalProgressCtor;
			On.Celeste.OuiJournalProgress.Redraw -= BetterJournalModule.OnJournalProgressRedraw;
			On.Celeste.OuiJournal.Close -= BetterJournalModule.OnJournalClose;

			ModSettings.ButtonsSwapKeybinds.Clear();
		}

		private static void Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime)
		{
			orig(self, gameTime);

			KeybindModule.Update();
			//BetterJournalModule.Update();
		}
	}
}
