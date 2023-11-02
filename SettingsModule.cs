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

		// ButtonBinds
		public ButtonBinding ButtonSaveJournal { get; set; } = new();

		public ButtonBinding ButtonLoadKeybind { get; set; } = new();

		public List<ButtonBinding> ButtonsSwapKeybinds { get; set; } = new();

		// KeybindModule settings
		private readonly List<string> keybindNames = new();

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

		// WhitelistModule settings
		private readonly List<string> whitelistNames = new();

		[SettingIgnore]
		private TextMenu.Slider CurrentWhitelistSlider { get; set; }
		private int currentWhitelistSlot = 0;
		public int CurrentWhitelistSlot 
		{
			get
			{
				if(currentWhitelistSlot > whitelistNames.Count - 1)
				{
					currentWhitelistSlot = whitelistNames.Count - 1;
				}
				return currentWhitelistSlot;
			} 
			set
			{
				currentWhitelistSlot = value;
			}
		}
		public bool WhitelistIsExclusive = true;
		private bool showRestartButton = false;

		// BetterJournal settings
		public bool BetterJournalEnabled { get; set; } = false;
		public bool ShowModTimeInJournal = false;
		public bool SeparateABCSideTimes = true;


		private bool verboseLogging = false;
		[SettingSubText("Enable to get more debug info.")]
		public bool VerboseLogging 
		{
			get
			{
				return verboseLogging;
			}
			set
			{
				verboseLogging = value;
#if !DEBUG
				Logger.SetLogLevel(nameof(izumisQOL), VerboseLogging ? LogLevel.Verbose : LogLevel.Info);
#endif
			}
		}

		public void CreateCurrentKeybindSlotEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new("Binding Settings", false);

			TextMenu.Item menuItem;

			Global.Log("keybindSettings.Count=" + KeybindModule.KeybindSettings.Count);

			subMenu.Add(CurrentKeybindSlider = new TextMenu.Slider("Current Keybind Slot", i => GetKeybindName(i), 0, keybindNames.Count - 1, CurrentKeybindSlot)
			{
				OnValueChange = delegate (int val)
				{
					CurrentKeybindSlot = val;
					if (AutoLoadKeybinds)
					{
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
			subMenu.AddDescription(menu, menuItem, "Whether the keybinds are loaded automatically when selecting a keybind\nor only when pressing the load button.");

			subMenu.Add(menuItem = new TextMenu.Button("Copy Current Keybinds Here"));
			menuItem.Pressed(
				delegate
				{
					Global.Log("Copying to: " + CurrentKeybindSlider.Index);
					Tooltip.Show("Copying current keybinds to " + GetKeybindName(CurrentKeybindSlider.Index));

					KeybindModule.SaveKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Copies the keybinds configured in settings to the current keybind slot.");

			subMenu.Add(menuItem = new TextMenu.Button("Load"));
			menuItem.Pressed(
				delegate
				{
					KeybindModule.ApplyKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Load the current keybind slot into your keybind-settings.");

			subMenu.Add(menuItem = new TextMenu.Button("Import Name From Clipboard"));
			menuItem.Pressed(
				delegate
				{
					string clipboardText = TextInput.GetClipboardText();
					if (!string.IsNullOrEmpty(clipboardText) && KeybindModule.RenameFile(CurrentKeybindSlot + "_" + GetKeybindName(CurrentKeybindSlot), clipboardText))
					{
						ChangeKeybindName(CurrentKeybindSlot, clipboardText);
						CurrentKeybindSlider.Values.Insert(CurrentKeybindSlot + 1, Tuple.Create(clipboardText, CurrentKeybindSlot));
						CurrentKeybindSlider.Values.RemoveAt(CurrentKeybindSlot);
						CurrentKeybindSlider.SelectWiggler.Start();
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, "Sets the name of the current keybind slot to the text in your clipboard.");

			subMenu.Add(menuItem = new TextMenu.Button("Add"));
			menuItem.Pressed(
				delegate
				{
					int val = CurrentKeybindSlider.Values.Count;

					KeybindModule.SaveKeybinds(val);

					CurrentKeybindSlider.Add(GetKeybindName(val), val);
					CurrentKeybindSlider.SelectWiggler.Start();

					Tooltip.Show("Adding new keybind slot");

					ButtonsSwapKeybinds.Add(new ButtonBinding());
					izumisQOL.InitializeButtonBinding(ButtonsSwapKeybinds[val]);
				}
			);
			subMenu.AddDescription(menu, menuItem, "Add another keybind slot.");

			subMenu.Add(menuItem = new TextMenu.Button("Remove"));
			menuItem.Pressed(
				delegate
				{
					if(keybindNames.Count > 1)
					{
						int keybindSlot = CurrentKeybindSlider.Index;
						Tooltip.Add(tooltips: new Tooltip.Info("Removed " + GetKeybindName(keybindSlot)), clearQueue: true);

						if (keybindSlot >= CurrentKeybindSlider.Values.Count - 1)
						{
							keybindSlot--;
						}

						if(keybindSlot < 0)
						{
							Global.Log("keybindSlot was negative", LogLevel.Warn);
							keybindSlot = 0;
						}

						KeybindModule.RemoveKeybindSlot(CurrentKeybindSlider.Index);
						keybindNames.RemoveAt(CurrentKeybindSlider.Index);

						CurrentKeybindSlider.Values.Clear();
						for(int i = 0; i < keybindNames.Count; i++)
						{
							CurrentKeybindSlider.Values.Add(new(GetKeybindName(i), i));
						}
						CurrentKeybindSlider.Index = keybindSlot;

						CurrentKeybindSlider.SelectWiggler.Start();

						ButtonsSwapKeybinds.RemoveAt(ButtonsSwapKeybinds.Count - 1);

						if (AutoLoadKeybinds)
						{
							KeybindModule.ApplyKeybinds(keybindSlot);
						}
						else
						{
							Tooltip.Show();
						}
					}
					Global.Log("only 1 item in slider");
				}
			);
			subMenu.AddDescription(menu, menuItem, "Remove this keybind slot.");

			menu.Add(subMenu);
		}

		public void CreateCurrentWhitelistSlotEntry(TextMenu menu, bool inGame)
		{
			if (inGame)
				return;

			WhitelistModule.Init();

			TextMenuExt.SubMenu subMenu = new("Whitelist Settings", false);

			TextMenu.Item menuItem;

			subMenu.Add(CurrentWhitelistSlider = new TextMenu.Slider("Current Whitelist", i => GetWhitelistName(i), 0, whitelistNames.Count - 1, CurrentWhitelistSlot)
			{
				OnValueChange = delegate(int val)
				{
					CurrentWhitelistSlot = val;
				}
			});
			subMenu.AddDescription(menu, CurrentWhitelistSlider, "The currently selected whitelist.");

			TextMenu.Button restartButton = new TextMenu.Button("Restart");
			restartButton.Pressed(
				delegate
				{
					Everest.QuickFullRestart();
				}
			);

			subMenu.Add(menuItem = new TextMenu.Button("Apply Current Whitelist"));
			menuItem.Pressed(
				delegate
				{
					if (WhitelistModule.WriteToEverestBlacklist(GetWhitelistName(CurrentWhitelistSlot)))
					{
						if (!showRestartButton)
						{
							subMenu.Insert(5, restartButton);
							subMenu.InsertDescription(menu, restartButton, "Restart Celeste");
							restartButton.SelectWiggler.Start();
						}
						showRestartButton = true;
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, "Apply the currently selected whitelist.");
			subMenu.NeedsRelaunch(menu, menuItem);

			if (showRestartButton)
			{
				subMenu.Add(restartButton);
				subMenu.AddDescription(menu, restartButton, "Restart Celeste");
			}

			subMenu.Add(menuItem = new TextMenu.Button("Save Current Whitelist"));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.SaveCurrentWhitelist(GetWhitelistName(CurrentWhitelistSlot), CurrentWhitelistSlot);
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

					if(!string.IsNullOrEmpty(clipboardText) && WhitelistModule.RenameFile(GetWhitelistName(CurrentWhitelistSlot), clipboardText))
					{
						ChangeWhitelistName(CurrentWhitelistSlot, clipboardText);
						CurrentWhitelistSlider.Values.Insert(CurrentWhitelistSlot + 1, Tuple.Create(clipboardText, CurrentWhitelistSlot));
						CurrentWhitelistSlider.Values.RemoveAt(CurrentWhitelistSlot);
						CurrentWhitelistSlider.SelectWiggler.Start();
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, "Sets the name of the current whitelist to the text in your clipboard.");

			subMenu.Add(menuItem = new TextMenu.Button("Add"));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.AddWhitelist();
					int val = whitelistNames.Count - 1;
					CurrentWhitelistSlider.Add(GetWhitelistName(val), val);
					CurrentWhitelistSlider.SelectWiggler.Start();

					Tooltip.Show("Added whitelist");
				}
			);
			subMenu.AddDescription(menu, menuItem, "Add another whitelist.");

			subMenu.Add(menuItem = new TextMenu.Button("Remove"));
			menuItem.Pressed(
				delegate
				{
					if (whitelistNames.Count > 1)
					{
						CurrentWhitelistSlot.Log("Current key bind slot");
						CurrentWhitelistSlider.Index.Log("Slider index");

						int whitelistSlot = CurrentWhitelistSlider.Index;
						Tooltip.Show("Removed " + GetWhitelistName(whitelistSlot));

						if (whitelistSlot >= CurrentWhitelistSlider.Values.Count - 1)
						{
							whitelistSlot--;
						}
						whitelistSlot.Log("whitelistSlot");
						CurrentWhitelistSlider.Values.Count.Log("count");

						if (whitelistSlot < 0)
						{
							Global.Log("whitelistSlot was negative", LogLevel.Warn);
							whitelistSlot = 0;
						}

						WhitelistModule.RemoveWhitelist(GetWhitelistName(CurrentWhitelistSlider.Index));
						whitelistNames.RemoveAt(CurrentWhitelistSlider.Index);

						CurrentWhitelistSlider.Values.Clear();
						for (int i = 0; i < whitelistNames.Count; i++)
						{
							CurrentWhitelistSlider.Values.Add(new(GetWhitelistName(i), i));
						}
						CurrentWhitelistSlider.Index = whitelistSlot;

						CurrentWhitelistSlider.SelectWiggler.Start();
					}
					Global.Log("only 1 item in slider");
				}
			);
			subMenu.AddDescription(menu, menuItem, "Remove this whitelist.");

			menu.Add(subMenu);
		}

		public void CreateBetterJournalEnabledEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new("Better Journal Settings", false);
			TextMenu.Item menuItem;

			subMenu.Add(menuItem = new TextMenu.OnOff("Enabled", BetterJournalEnabled)
			{
				OnValueChange = (val) => BetterJournalEnabled = val
			});

			subMenu.Add(menuItem = new TextMenu.OnOff("Show Mod Time In Journal", ShowModTimeInJournal)
			{
				OnValueChange = (val) => ShowModTimeInJournal = val
			});
			subMenu.AddDescription(menu, menuItem, "Enable to have the journal show the total time spent in a single mod in addition to the full total.");

			subMenu.Add(new TextMenu.OnOff("Separate ABC-Side Times", SeparateABCSideTimes)
			{
				OnValueChange = (val) => SeparateABCSideTimes = val
			});

			menu.Add(subMenu);
		}

		public TextMenu.Slider GetCurrentKeybindSlider()
		{
			return CurrentKeybindSlider;
		}

		private string GetWhitelistName(int index)
		{
			if(index > whitelistNames.Count - 1 || index < 0)
			{
				Global.Log("index: " + index + " out of bounds for whitelistNames");
				Tooltip.Show("Failed getting whitelist name.");
				return null;
			}
			return whitelistNames[index];
		}

		public void AddWhitelistName(string name)
		{
			if (!whitelistNames.Contains(name))
			{
				Global.Log(name);
				whitelistNames.Add(name);
				return;
			}
			Global.Log(name + " is already another whitelist's name", LogLevel.Info);
		}

		public void ChangeWhitelistName(int index, string name)
		{
			if (index > whitelistNames.Count - 1 || index < 0)
			{
				Global.Log("index: " + index + " out of bounds for whitelistNames");
				Tooltip.Show("Failed changing whitelist name.");
				return;
			}
			whitelistNames[index] = name;
		}

		public void ResetWhitelist()
		{
			whitelistNames.Clear();
		}

		public void AddKeybindName(string name)
		{
			keybindNames.Add(name);
		}

		private string GetKeybindName(int index)
		{
			if (index > keybindNames.Count - 1 || index < 0)
			{
				Global.Log("index: " + index + " out of bounds for keybindNames");
				Tooltip.Show("Failed getting keybind name.");
				return null;
			}
			return keybindNames[index];
		}

		public void ChangeKeybindName(int index, string name)
		{
			if (index > keybindNames.Count - 1 || index < 0)
			{
				Global.Log("index: " + index + " out of bounds for keybindNames");
				Tooltip.Show("Failed changing keybind name.");
				return;
			}
			keybindNames[index] = name;
		}

		public List<string> GetKeybindNames()
		{
			return keybindNames;
		}
	}
}
