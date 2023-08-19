using Celeste;
using Celeste.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Monocle;
using Celeste.Mod.izumisQOL.Menu;
using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using FMOD.Studio;
using Monocle;
using On.Celeste;
using On.Monocle;

namespace Celeste.Mod.izumisQOL
{
	[SettingName("modoptions_izumisQOL_title")]
	public class SettingsModule : EverestModuleSettings
	{
		public bool EnableHotkeys { get; set; } = true;

		public ButtonBinding ButtonLoadKeybind { get; set; } = new ButtonBinding();

		public List<ButtonBinding> ButtonsSwapKeybinds { get; set; } = new();

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
					KeybindModule.SaveKeybinds(0);
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

		private List<string> BlacklistNames = new();

		private TextMenu.Slider CurrentBlacklistSlider;
		private int currentBlacklistSlot = 0;
		public int CurrentBlacklistSlot 
		{
			get
			{
				if(currentBlacklistSlot > BlacklistNames.Count - 1)
				{
					currentBlacklistSlot = BlacklistNames.Count - 1;
				}
				return currentBlacklistSlot;
			} 
			set
			{
				currentBlacklistSlot = value;
			}
		}

		[SettingSubText("Enable to get more debug info.")]
		public bool VerboseLogging { get; set; } = false;

		public void CreateCurrentKeybindSlotEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new("Binding Settings", false);

			TextMenu.Item menuItem;

			Global.Log("keybindSettings.Count=" + KeybindModule.KeybindSettings.Count);
			subMenu.Add(CurrentKeybindSlider = new TextMenu.Slider("Current Keybind Slot", i => (i + 1).ToString(), 0, KeybindModule.KeybindSettings.Count - 1, CurrentKeybindSlot)
			{
				OnValueChange = delegate (int val)
				{
					if (AutoLoadKeybinds)
					{
						Global.Log(val);
						KeybindModule.ApplyKeybinds(val);
					}
				}
			});
			subMenu.AddDescription(menu, CurrentKeybindSlider, "The currently selected keybinds. \n\nNote: Turn auto-load keybinds off if you want to edit an existing keybind slot.");

			subMenu.Add(menuItem = new TextMenu.OnOff("Auto-Load Keybinds", AutoLoadKeybinds)
			{
				OnValueChange = delegate(bool val)
				{
					AutoLoadKeybinds = val;
				}
			});
			subMenu.AddDescription(menu, menuItem, "Whether the keybinds are loaded automatically when selecting a keybind\nor when pressing the load button.");

			subMenu.Add(menuItem = new TextMenu.Button("Copy Current Keybinds Here"));
			menuItem.Pressed(
				delegate
				{
					Global.Log("Copying to: " + CurrentKeybindSlider.Index);
					Tooltip.Show("Copying current keybinds to slot " + (CurrentKeybindSlider.Index + 1));

					//Everest.SaveSettings();

					KeybindModule.SaveKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Copies the keybinds configured in settings to the current keybind slot.");

			subMenu.Add(menuItem = new TextMenu.Button("Load"));
			menuItem.Pressed(
				delegate
				{
					Global.Log(CurrentKeybindSlider.Index);
					KeybindModule.ApplyKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Load the current keybind slot into your keybind-settings.");

			subMenu.Add(menuItem = new TextMenu.Button("Add"));
			menuItem.Pressed(
				delegate
				{
					int val = CurrentKeybindSlider.Values.Count;

					KeybindModule.SaveKeybinds(val);

					CurrentKeybindSlider.Add((val + 1).ToString(), val);
					CurrentKeybindSlider.SelectWiggler.Start();

					Tooltip.Show("Adding new keybind slot");

					ButtonsSwapKeybinds.Add(new ButtonBinding());
					izumisQOL.InitializeButtonBinding(ButtonsSwapKeybinds[val]);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Add another keybind slot.");

			//subMenu.Remove(menuItem = new TextMenu.Button("Remove"));
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

			TextMenu.Item menuItem;

			subMenu.Add(CurrentBlacklistSlider = new TextMenu.Slider("Current Blacklist", i => BlacklistNames[i], 0, BlacklistNames.Count - 1, CurrentBlacklistSlot)
			{
				OnValueChange = delegate(int val)
				{
					CurrentBlacklistSlot = val;
					BlacklistModule.CopyCustomBlacklistToCeleste(BlacklistNames[val]);
				}
			});
			subMenu.NeedsRelaunch(menu, CurrentBlacklistSlider);

			subMenu.Add(menuItem = new TextMenu.Button("Import Name From Clipboard"));
			menuItem.Pressed(
				delegate
				{
					string clipboardText = TextInput.GetClipboardText();
					if(!string.IsNullOrEmpty(clipboardText))
					{
						BlacklistModule.ChangeFileName(BlacklistNames[CurrentBlacklistSlot], clipboardText);
						BlacklistNames[CurrentBlacklistSlot] = clipboardText;
						CurrentBlacklistSlider.Values.Insert(CurrentBlacklistSlot + 1, Tuple.Create(clipboardText, CurrentBlacklistSlot));
						CurrentBlacklistSlider.Values.RemoveAt(CurrentBlacklistSlot);
						CurrentBlacklistSlider.SelectWiggler.Start();
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, "Sets the name of the current blacklist to the text in your clipboard.");

			subMenu.Add(menuItem = new TextMenu.Button("Add"));
			menuItem.Pressed(
				delegate
				{
					BlacklistModule.CopyCelesteBlacklistToNewFile();
					CurrentBlacklistSlider.Add(BlacklistNames[BlacklistNames.Count - 1], BlacklistNames.Count - 1);
					CurrentBlacklistSlider.SelectWiggler.Start();
				}
			);
			subMenu.AddDescription(menu, menuItem, "Add another blacklist.");

			menu.Add(subMenu);
		}

		public TextMenu.Slider GetCurrentKeybindSlider()
		{
			return CurrentKeybindSlider;
		}

		public void AddBlackListName(string name)
		{
			if (!BlacklistNames.Contains(name))
			{
				Global.Log(name);
				BlacklistNames.Add(name);
			}
		}
	}
}
