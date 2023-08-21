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
					KeybindModule.LoadKeybindFiles();
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

		private List<string> WhitelistNames = new();

		private TextMenu.Slider CurrentWhitelistSlider;
		private int currentWhitelistSlot = 0;
		public int CurrentWhitelistSlot 
		{
			get
			{
				if(currentWhitelistSlot > WhitelistNames.Count - 1)
				{
					currentWhitelistSlot = WhitelistNames.Count - 1;
				}
				return currentWhitelistSlot;
			} 
			set
			{
				currentWhitelistSlot = value;
			}
		}
		public bool WhitelistIsExclusive = false;

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
			subMenu.AddDescription(menu, CurrentKeybindSlider, "The currently selected keybinds. \n\nNote: You may want to turn auto-load keybinds off if you want to edit an existing keybind slot.");

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

		public void CreateCurrentWhitelistSlotEntry(TextMenu menu, bool inGame)
		{
			if (inGame)
				return;

			TextMenuExt.SubMenu subMenu = new("Whitelist Settings", false);

			TextMenu.Item menuItem;

			subMenu.Add(CurrentWhitelistSlider = new TextMenu.Slider("Current Whitelist", i => WhitelistNames[i], 0, WhitelistNames.Count - 1, CurrentWhitelistSlot)
			{
				OnValueChange = delegate(int val)
				{
					CurrentWhitelistSlot = val;
					
				}
			});
			subMenu.AddDescription(menu, CurrentWhitelistSlider, "The currently selected whitelist.");

			subMenu.Add(menuItem = new TextMenu.Button("Apply Current Whitelist"));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.WriteToEverestBlacklist(WhitelistNames[CurrentWhitelistSlot]);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Apply the currently selected whitelist.");
			subMenu.NeedsRelaunch(menu, menuItem);

			subMenu.Add(menuItem = new TextMenu.Button("Save Current Whitelist"));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.SaveCurrentWhitelist(WhitelistNames[CurrentWhitelistSlot], CurrentWhitelistSlot);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Save the currently enabled mods to this whitelist.");

			subMenu.Add(menuItem = new TextMenu.OnOff("Is Exclusive", WhitelistIsExclusive)
			{
				OnValueChange = delegate(bool val)
				{
					WhitelistIsExclusive = val;
				}
			});
			subMenu.AddDescription(menu, menuItem, "Whether everything not in the whitelist is disabled or not");

			subMenu.Add(menuItem = new TextMenu.Button("Import Name From Clipboard"));
			menuItem.Pressed(
				delegate
				{
					string clipboardText = TextInput.GetClipboardText();
					if(!string.IsNullOrEmpty(clipboardText) && WhitelistModule.RenameFile(WhitelistNames[CurrentWhitelistSlot], clipboardText))
					{
						WhitelistNames[CurrentWhitelistSlot] = clipboardText;
						CurrentWhitelistSlider.Values.Insert(CurrentWhitelistSlot + 1, Tuple.Create(clipboardText, CurrentWhitelistSlot));
						CurrentWhitelistSlider.Values.RemoveAt(CurrentWhitelistSlot);
						CurrentWhitelistSlider.SelectWiggler.Start();
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, "Sets the name of the current blacklist to the text in your clipboard.");

			subMenu.Add(menuItem = new TextMenu.Button("Add"));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.AddWhitelist();
					int val = WhitelistNames.Count - 1;
					CurrentWhitelistSlider.Add(WhitelistNames[val], val);
					CurrentWhitelistSlider.SelectWiggler.Start();

					Tooltip.Show("Added whitelist");
				}
			);
			subMenu.AddDescription(menu, menuItem, "Add another whitelist.");

			menu.Add(subMenu);
		}

		public TextMenu.Slider GetCurrentKeybindSlider()
		{
			return CurrentKeybindSlider;
		}

		public void AddWhitelistName(string name)
		{
			if (!WhitelistNames.Contains(name))
			{
				Global.Log(name);
				WhitelistNames.Add(name);
				return;
			}
			Global.Log(name + " is already another whitelist's name", LogLevel.Info);
		}

		public void ChangeWhitelistName(int index, string name)
		{
			WhitelistNames[index] = name;
		}
	}
}
