using Celeste;
using Celeste.Mod;
using System;
using System.Collections;
using System.Collections.Generic;
using YamlDotNet.Serialization;
using Monocle;
using Celeste.Mod.izumisQOL.Menu;
using Microsoft.Xna.Framework;
using Celeste.Mod.izumisQOL.UI;

namespace Celeste.Mod.izumisQOL
{
	[SettingName("MODOPTIONS_IZUMISQOL_TITLE")]
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
				return currentWhitelistSlot;
			} 
			set
			{
				if(value < 0)
				{
					Global.Log("tried to set currentWhitelistSlot to value outside array size, setting to 0", LogLevel.Warn);
					currentWhitelistSlot = 0;
				}
				else if(value > whitelistNames.Count - 1)
				{
					Global.Log("tried to set currentWhitelistSlot to value outside array size, setting to .Count", LogLevel.Warn);
					currentWhitelistSlot = whitelistNames.Count > 0 ? whitelistNames.Count - 1 : 0;
				}
				else
				{
					currentWhitelistSlot = value;
				}
			}
		}
		public bool WhitelistIsExclusive = true;

		// BetterJournal settings
		public bool BetterJournalEnabled { get; set; } = false;
		public bool ShowModTimeInJournal = false;
		public bool SeparateABCSideTimes = true;

		public bool ShowRestartButtonInMainMenu { get; set; } = false;

		private bool verboseLogging = false;
		[SettingIgnore]
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
			TextMenuExt.SubMenu subMenu = new(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_BINDINGSETTINGS"), false);

			TextMenu.Item menuItem;

			Global.Log("keybindSettings.Count=" + KeybindModule.KeybindSettings.Count);

			subMenu.Add(CurrentKeybindSlider = new TextMenu.Slider(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT"), i => GetKeybindName(i), 0, keybindNames.Count - 1, CurrentKeybindSlot)
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
			subMenu.AddDescription(menu, CurrentKeybindSlider, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT_DESC") + "\n\n" + Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT_DESCNOTE"));

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_AUTOLOADKEYBINDS"), AutoLoadKeybinds)
			{
				OnValueChange = delegate(bool val)
				{
					AutoLoadKeybinds = val;
				}
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_AUTOLOADKEYBINDS_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS")));
			menuItem.Pressed(
				delegate
				{
					Global.Log("Copying to: " + CurrentKeybindSlider.Index);
					Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS_TOOLTIP") + " " + GetKeybindName(CurrentKeybindSlider.Index));

					KeybindModule.SaveKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_LOAD")));
			menuItem.Pressed(
				delegate
				{
					KeybindModule.ApplyKeybinds(CurrentKeybindSlider.Index);
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_LOAD_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_IMPORTCLIPBOARD")));
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
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_IMPORTCLIPBOARD_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD")));
			menuItem.Pressed(
				delegate
				{
					int val = CurrentKeybindSlider.Values.Count;

					KeybindModule.SaveKeybinds(val);

					CurrentKeybindSlider.Add(GetKeybindName(val), val);
					CurrentKeybindSlider.SelectWiggler.Start();

					Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD_TOOLTIP"));

					ButtonsSwapKeybinds.Add(new ButtonBinding());
					izumisQOL.InitializeButtonBinding(ButtonsSwapKeybinds[val]);
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE")));
			menuItem.Pressed(
				delegate
				{
					if(keybindNames.Count > 1)
					{
						int keybindSlot = CurrentKeybindSlider.Index;
						Tooltip.Add(tooltips: new Tooltip.Info(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE_TOOLTIP") + " " + GetKeybindName(keybindSlot)), clearQueue: true);

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
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE_DESC"));

			menu.Add(subMenu);
		}

		public void CreateCurrentWhitelistSlotEntry(TextMenu menu, bool inGame)
		{
			if (inGame)
				return;

			WhitelistModule.Init();

			TextMenuExt.SubMenu subMenu = new(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_WHITELISTSETTINGS"), false);

			TextMenu.Item menuItem;

			subMenu.Add(CurrentWhitelistSlider = new TextMenu.Slider(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_CURRENTWHITELIST"), i => GetWhitelistName(i), 0, whitelistNames.Count - 1, CurrentWhitelistSlot)
			{
				OnValueChange = delegate(int val)
				{
					CurrentWhitelistSlot = val;
				}
			});
			subMenu.AddDescription(menu, CurrentWhitelistSlider, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_CURRENTWHITELIST_DESC"));

			ToggleableRestartButton restartButton = ToggleableRestartButton.New("restartForApplyWhitelist");

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_APPLY")));
			menuItem.Pressed(
				delegate
				{
					if (WhitelistModule.WriteToEverestBlacklist(GetWhitelistName(CurrentWhitelistSlot)))
					{
						restartButton.Show(5, menu, subMenu);
					}
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_APPLY_DESC"));
			subMenu.NeedsRelaunch(menu, menuItem);

			restartButton.AddToMenuIfIsShown(menu, subMenu);

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_SAVE")));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.SaveCurrentWhitelist(GetWhitelistName(CurrentWhitelistSlot), CurrentWhitelistSlot);
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_SAVE_DESC"));

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_EXCLUSIVE"), WhitelistIsExclusive)
			{
				OnValueChange = delegate(bool val)
				{
					WhitelistIsExclusive = val;
				}
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_EXCLUSIVE_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_IMPORTCLIPBOARD")));
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
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_IMPORTCLIPBOARD_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD")));
			menuItem.Pressed(
				delegate
				{
					WhitelistModule.AddWhitelist();
					int val = whitelistNames.Count - 1;
					CurrentWhitelistSlider.Add(GetWhitelistName(val), val);
					CurrentWhitelistSlider.SelectWiggler.Start();

					Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD_TOOLTIP"));
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD_DESC"));

			subMenu.Add(menuItem = new TextMenu.Button(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE")));
			menuItem.Pressed(
				delegate
				{
					if (whitelistNames.Count > 1)
					{
						int whitelistSlot = CurrentWhitelistSlider.Index;
						Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE_TOOLTIP") + " " + GetWhitelistName(whitelistSlot));

						if (whitelistSlot >= CurrentWhitelistSlider.Values.Count - 1)
						{
							whitelistSlot--;
						}
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
				}
			);
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE_DESC"));

			menu.Add(subMenu);
		}

		public void CreateBetterJournalEnabledEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new(Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SETTINGS"), false);
			TextMenu.Item menuItem;

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_ENABLED"), BetterJournalEnabled)
			{
				OnValueChange = (val) => BetterJournalEnabled = val
			});

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_MODTIMEINJOURNAL"), ShowModTimeInJournal)
			{
				OnValueChange = (val) => ShowModTimeInJournal = val
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_MODTIMEINJOURNAL_DESC"));

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SEPARATEABCSIDE"), SeparateABCSideTimes)
			{
				OnValueChange = (val) => SeparateABCSideTimes = val
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SEPARATEABCSIDE_DESC"));

			menu.Add(subMenu);
		}

		public void CreateShowRestartButtonInMainMenuEntry(TextMenu menu, bool inGame)
		{
			TextMenuExt.SubMenu subMenu = new(Dialog.Clean("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_OTHERSETTINGS"), false);
			TextMenu.Item menuItem;

			ToggleableRestartButton restartButton = ToggleableRestartButton.New("restartForMenuRestartButton");

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_SHOWRESTARTINMAINMENU"), ShowRestartButtonInMainMenu)
			{
				OnValueChange = 
				delegate(bool val)
				{
					restartButton.Show(3, menu, subMenu);
					ShowRestartButtonInMainMenu = val;
				}
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_SHOWRESTARTINMAINMENU_DESC"));
			subMenu.NeedsRelaunch(menu, menuItem);

			restartButton.AddToMenuIfIsShown(menu, subMenu);

			subMenu.Add(menuItem = new TextMenu.OnOff(Dialog.Clean("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_VERBOSELOGGING"), VerboseLogging)
			{
				OnValueChange = (val) => VerboseLogging = val
			});
			subMenu.AddDescription(menu, menuItem, Dialog.Clean("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_VERBOSELOGGING_DESC"));

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
				Tooltip.Add(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTERROR_GETNAME"));
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
				Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_WHITELISTERROR_NAMECHANGE"));
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
				Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDERROR_GETNAME"));
				return null;
			}
			return keybindNames[index];
		}

		public void ChangeKeybindName(int index, string name)
		{
			if (index > keybindNames.Count - 1 || index < 0)
			{
				Global.Log("index: " + index + " out of bounds for keybindNames");
				Tooltip.Show(Dialog.Clean("MODOPTIONS_IZUMISQOL_KEYBINDERROR_NAMECHANGE"));
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
