global using static Celeste.Mod.izumisQOL.Global;

using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Monocle;
using FMOD.Studio;
using Celeste.Mod.izumisQOL.Menu;
using Celeste.Mod.izumisQOL.ModIntegration;
using Celeste.Mod.izumisQOL.OBS;
using Celeste.Mod.izumisQOL.UI;

namespace Celeste.Mod.izumisQOL
{
	public class izumisQOL : EverestModule
	{
		// Only one alive module instance can exist at any given time.
		public static izumisQOL Instance;

		public izumisQOL()
		{
			Instance = this;

#if DEBUG
			// debug builds use verbose logging
			Logger.SetLogLevel(nameof(izumisQOL), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(izumisQOL), LogLevel.Info);
#endif
		}

		// Check the next section for more information about mod settings, save data and session.
		// Those are optional: if you don't need one of those, you can remove it from the module.

		// If you need to store settings:
		public override Type SettingsType => typeof(SettingsModule);
		public static SettingsModule ModSettings => (SettingsModule)Instance._Settings;

		// Load runs before Celeste itself has initialized properly.
		public override void Load()
		{
			Hooks.Load();

			if (ModSettings is not null && ModSettings.ConnectToOBSWebsocketsOnStartup)
			{
				OBSIntegration.Connect(true);
			}
		}

		// Optional, initialize anything after Celeste has initialized itself properly.
		public override void Initialize()
		{
			
		}

		// Optional, do anything requiring either the Celeste or mod content here.
		public override void LoadContent(bool firstLoad)
		{
			CollabUtils2Integration.Load();
			Indicator.Load();
		}

		// Unload the entirety of your mod's content. Free up any native resources.
		public override void Unload()
		{
			Hooks.Unload();
		}

		public override void OnInputInitialize()
		{
			base.OnInputInitialize();

			if (ModSettings.ButtonsSwapKeybinds == null)
				return;

			foreach (ButtonBinding buttonsConsoleCommand in ModSettings.ButtonsSwapKeybinds)
			{
				InitializeButtonBinding(buttonsConsoleCommand);
			}
		}

		public static void InitializeButtonBinding(ButtonBinding buttonBinding)
		{
			if (buttonBinding != null && buttonBinding.Button == null && buttonBinding.Binding != null)
			{
				buttonBinding.Button = new(buttonBinding.Binding, Input.Gamepad, 0.08f, 0.2f);
				buttonBinding.Button.AutoConsumeBuffer = true;
			}
		}

		public override void CreateModMenuSectionKeyBindings(TextMenu menu, bool inGame, EventInstance snapshot)
		{
			menu.Add(new TextMenu.Button("options_keyconfig".AsDialog()).Pressed(delegate
			{
				menu.Focused = false;
				Engine.Scene.Add(CreateCustomKeyboardConfigUI(menu));
				Engine.Scene.OnEndOfFrame += delegate
				{
					Engine.Scene.Entities.UpdateLists();
				};
			}));
			menu.Add(new TextMenu.Button("options_btnconfig".AsDialog()).Pressed(delegate
			{
				menu.Focused = false;
				Engine.Scene.Add(CreateCustomButtonConfigUI(menu));
				Engine.Scene.OnEndOfFrame += delegate
				{
					Engine.Scene.Entities.UpdateLists();
				};
			}));
		}

		private Entity CreateCustomKeyboardConfigUI(TextMenu menu)
		{
			return new CustomModuleSettingsKeyboardConfigUI(Instance)
			{
				OnClose = delegate
				{
					menu.Focused = true;
				}
			};
		}

		private Entity CreateCustomButtonConfigUI(TextMenu menu)
		{
			return new CustomModuleSettingsButtonConfigUI(Instance)
			{
				OnClose = delegate
				{
					menu.Focused = true;
				}
			};
		}
	}
}