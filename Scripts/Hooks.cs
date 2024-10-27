using Celeste.Mod.izumisQOL.Obs;
using Celeste.Mod.izumisQOL.UI;
using Microsoft.Xna.Framework;
using On.Monocle;

namespace Celeste.Mod.izumisQOL;

public static class Hooks
{
	internal static void Load()
	{
		Engine.Update                           += Update;
		On.Celeste.OuiJournal.Update            += BetterJournalModule.Update;
		On.Celeste.OuiJournalProgress.ctor      += BetterJournalModule.OuiJournalProgressCtor;
		On.Celeste.OuiJournalPage.Redraw        += BetterJournalModule.OnJournalPageRedraw;
		Everest.Events.Journal.OnEnter          += BetterJournalModule.OnJournalEnter;
		On.Celeste.OuiJournal.Close             += BetterJournalModule.OnJournalClose;
		Everest.Events.MainMenu.OnCreateButtons += General.OnCreateButtons;
		Scene.Begin                             += Indicator.OnSceneBegin;
		On.Celeste.Level.Begin                  += OBSIntegration.OnLevelBegin;

		NoClipModule.Load();
		KeybindModule.Load();
		BetterJournalModule.Load();
	}

	internal static void Unload()
	{
		Engine.Update                           -= Update;
		On.Celeste.OuiJournal.Update            -= BetterJournalModule.Update;
		On.Celeste.OuiJournalProgress.ctor      -= BetterJournalModule.OuiJournalProgressCtor;
		On.Celeste.OuiJournalPage.Redraw        -= BetterJournalModule.OnJournalPageRedraw;
		Everest.Events.Journal.OnEnter          -= BetterJournalModule.OnJournalEnter;
		On.Celeste.OuiJournal.Close             -= BetterJournalModule.OnJournalClose;
		Everest.Events.MainMenu.OnCreateButtons -= General.OnCreateButtons;
		Scene.Begin                             -= Indicator.OnSceneBegin;
		On.Celeste.Level.Begin                  -= OBSIntegration.OnLevelBegin;

		NoClipModule.Unload();

		ModSettings.ButtonsSwapKeybinds.Clear();
	}

	private static void Update(Engine.orig_Update orig, Monocle.Engine self, GameTime gameTime)
	{
		orig(self, gameTime);

		KeybindModule.Update();
		GamepadPauser.Update();
		OBSIntegration.Update();
	}
}