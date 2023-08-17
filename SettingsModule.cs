using Celeste;
using Celeste.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Monocle;
using Celeste.Mod.izumisQOL.Menu;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL
{
	[SettingName("modoptions_izumisQOL_title")]
	public class SettingsModule : EverestModuleSettings
	{
		public bool EnableHotkeys { get; set; } = true;

		public ButtonBinding ButtonLoadKeybind { get; set; } = new ButtonBinding();

		public List<ButtonBinding> ButtonsSwapKeybinds { get; set; } = new();

		[SettingIgnore]
		private TextMenu.Button CopyButton { get; set; }
		[SettingIgnore]
		private TextMenu.Button LoadButton { get; set; }
		[SettingIgnore]
		private TextMenu.Button AddKeyBindingsButton { get; set; }
		[SettingIgnore]
		private TextMenu.Button RemoveKeyBindingsButton { get; set; }

		[SettingIgnore]
		private TextMenu.Slider CurrentKeybindSlider { get; set; }

		public bool AutoLoadKeybinds = true;

		private int currentKeybindSlot = -1;
		public int CurrentKeybindSlot
		{
			get 
			{
				if(currentKeybindSlot == -1)
				{
					currentKeybindSlot = 0;
					KeybindModule.CopyCelesteSettingsToKeybindIDFile(0);
				}
				if(currentKeybindSlot > KeybindModule.KeybindSettings.Count - 1)
				{
					currentKeybindSlot = KeybindModule.KeybindSettings.Count - 1;
				}
				return currentKeybindSlot;
			}
			set
			{
				currentKeybindSlot = value;
			}
		}

		public List<string> BlackListNames = new List<string>();

		public int CurrentBlacklistSlot { get; set; }

		public void CreateCurrentKeybindSlotEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new("Binding Settings", false);

			Global.Log("keybindSettings.Count=" + KeybindModule.KeybindSettings.Count);
			subMenu.Add(CurrentKeybindSlider = new TextMenu.Slider("Current Key Bind Slot", i => (i + 1).ToString(), 0, KeybindModule.KeybindSettings.Count - 1, CurrentKeybindSlot));

			subMenu.Add(new TextMenu.OnOff("Auto-Load Keybinds", AutoLoadKeybinds));

			subMenu.Add(CopyButton = new TextMenu.Button("Copy Current Keybinds Here"));
			CopyButton.Pressed(
				delegate
				{
					Global.Log("Copying to: " + CurrentKeybindSlider.Index);
					KeybindModule.CopyCelesteSettingsToKeybindIDFile(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, CopyButton, "Copies the keybinds configured in settings to the current keybind slot.");

			subMenu.Add(LoadButton = new TextMenu.Button("Load"));
			LoadButton.Pressed(
				delegate
				{
					Global.Log(CurrentKeybindSlider.Index);
					KeybindModule.ApplyKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, LoadButton, "Load the current keybind slot into your keybind-settings.");

			subMenu.Add(AddKeyBindingsButton = new TextMenu.Button("Add"));
			AddKeyBindingsButton.Pressed(
				delegate
				{
					int val = CurrentKeybindSlider.Values.Count;

					KeybindModule.CopyCelesteSettingsToKeybindIDFile(val);

					CurrentKeybindSlider.Add((val + 1).ToString(), val);
					CurrentKeybindSlider.SelectWiggler.Start();

					ButtonsSwapKeybinds.Add(new ButtonBinding());
					izumisQOL.InitializeButtonBinding(ButtonsSwapKeybinds[val]);
				}
			);
			subMenu.AddDescription(menu, AddKeyBindingsButton, "Add another keybind slot.");

			//subMenu.Remove(RemoveKeyBindingsButton = new TextMenu.Button("Remove"));
			//RemoveKeyBindingsButton.Pressed(
			//	delegate
			//	{

			//	}
			//);

			menu.Add(subMenu);
		}

		public void CreateCurrentBlacklistSlotEntry(TextMenu menu, bool inGame)
		{
			if (inGame)
				return;

			TextMenuExt.SubMenu subMenu = new("Blacklist Settings", false);

			subMenu.Add(new TextMenu.Slider("Current Blacklist", i => BlackListNames[i], 0, BlackListNames.Count));

			menu.Add(subMenu);
		}

		public TextMenu.Slider GetCurrentKeybindSlider()
		{
			return CurrentKeybindSlider;
		}
	}
}
