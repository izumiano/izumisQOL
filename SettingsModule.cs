using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Mod.izumisQOL.Obs;
using Celeste.Mod.izumisQOL.UI;
using YamlDotNet.Serialization;

namespace Celeste.Mod.izumisQOL;

[SettingName("MODOPTIONS_IZUMISQOL_TITLE")]
public class SettingsModule : EverestModuleSettings
{
	public bool EnableHotkeys { get; set; } = true;

	// ButtonBinds
	public ButtonBinding ButtonSaveJournal { get; set; } = new();

	[SettingName("Suppress OBS-Indicators")]
	public ButtonBinding ButtonSuppressOBSIndicators { get; set; } = new();

	public ButtonBinding ButtonEnableNoClip { get; set; } = new();

	public ButtonBinding ButtonLoadKeybind { get; set; } = new();

	public List<ButtonBinding> ButtonsSwapKeybinds { get; set; } = [];

	// KeybindModule settings
	private readonly List<string> keybindNames = [];

	[SettingIgnore] private TextMenu.Slider? CurrentKeybindSlider { get; set; }

	public bool AutoLoadKeybinds = true;

	private int currentKeybindSlot = -1;

	public int CurrentKeybindSlot
	{
		get
		{
			if( currentKeybindSlot == -1 )
			{
				currentKeybindSlot = 0;
				KeybindModule.LoadKeybindFiles();
			}

			if( currentKeybindSlot > KeybindModule.KeybindSettings.Count - 1 )
			{
				currentKeybindSlot = KeybindModule.KeybindSettings.Count - 1;
			}

			return currentKeybindSlot;
		}
		set { currentKeybindSlot = value; }
	}

	// WhitelistModule settings
	private readonly List<string> whitelistNames = [ ];

	[SettingIgnore] private TextMenu.Slider? CurrentWhitelistSlider { get; set; }
	private                 int              currentWhitelistSlot;

	public int CurrentWhitelistSlot
	{
		get => currentWhitelistSlot;
		set
		{
			if( value < 0 )
			{
				Log("tried to set currentWhitelistSlot to value outside array size, setting to 0", LogLevel.Warn);
				currentWhitelistSlot = 0;
			}
			else
			{
				currentWhitelistSlot = value;
			}
		}
	}

	public bool WhitelistIsExclusive = true;

	// BetterJournal settings
	public bool BetterJournalEnabled { get; set; }
	public bool ShowModTimeInJournal;
	public bool SeparateABCSideTimes = true;

	// GamepadPauser settings
	public bool GamepadPauserEnabled { get; set; }
	public int  PauseAfterFramesGamepadInactive = 10;

	// OBS Websocket settings
	public bool OBSIntegrationEnabled { get; set; }
	public bool ConnectToOBSWebsocketsOnStartup;

	public OBSRecordingIndicator.DisplayType ShowRecordingIndicatorWhen =
		OBSRecordingIndicator.DisplayType.WhenNotRecording;

	[SettingIgnore] // we're not using YamlIgnore here to support older versions
	public bool CheckRecordingStatus
	{
		get => OBSPollFrequencyIndex[RecordingType.Record] != 0;
		[Obsolete]
		set
		{
			if( value )
			{
				setRecordingPollFrequencyIndex = true;
			}
		}
	}

	[SettingIgnore] // we're not using YamlIgnore here to support older versions
	public bool CheckStreamingStatus
	{
		get => OBSPollFrequencyIndex[RecordingType.Stream] != 0;
		[Obsolete]
		set
		{
			if( value )
			{
				setStreamingPollFrequencyIndex = true;
			}
		}
	}

	[YamlIgnore] public bool CheckReplayBufferStatus => OBSPollFrequencyIndex[RecordingType.ReplayBuffer] != 0;

	public Dictionary<RecordingType, int> OBSPollFrequencyIndex = new()
	{
		{ RecordingType.Record, 0 },
		{ RecordingType.Stream, 0 },
		{ RecordingType.ReplayBuffer, 0 },
	};


	[Obsolete] private bool setRecordingPollFrequencyIndex;
	[Obsolete] private bool setStreamingPollFrequencyIndex;

	[Obsolete("Use OBSPollFrequency instead.")]
	public int PollFrequencyIndex
	{
		get => -1;
		set
		{
			if( value < 0 ) return;

			if( setRecordingPollFrequencyIndex )
			{
				OBSPollFrequencyIndex[RecordingType.Record] = value > 0 ? value - 1 : 0;
			}

			if( setStreamingPollFrequencyIndex )
			{
				OBSPollFrequencyIndex[RecordingType.Stream] = value > 0 ? value - 1 : 0;
			}
		}
	}


	[SettingIgnore]
	public string OBSHostPort
	{
		get => OBSIntegration.HostPort;
		set => OBSIntegration.HostPort = value.Replace("ws://", "");
	}

	[SettingIgnore]
	public string OBSPassword
	{
		get => OBSIntegration.Password;
		set => OBSIntegration.Password = value;
	}

	// NoClip Settings
	[YamlIgnore]    public bool NoClipEnabled     { get; set; }
	[SettingIgnore] public int  NoClipNormalSpeed { get; set; } = 8;
	[SettingIgnore] public int  NoClipFastSpeed   { get; set; } = 8;
	[SettingIgnore] public int  NoClipSlowSpeed   { get; set; } = 8;

	// Other settings
	public bool ShowRestartButtonInMainMenu { get; set; }

	private bool verboseLogging;

	[SettingIgnore]
	public bool VerboseLogging
	{
		get { return verboseLogging; }
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
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_BINDINGSETTINGS".AsDialog(), false);

		TextMenu.Item menuItem;

		Log("keybindSettings.Count=" + KeybindModule.KeybindSettings.Count);

		subMenu.Add(CurrentKeybindSlider =
			new TextMenu.Slider("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT".AsDialog(), i => GetKeybindName(i),
				0, keybindNames.Count - 1, CurrentKeybindSlot)
			{
				OnValueChange = delegate(int val)
				{
					CurrentKeybindSlot = val;
					if( AutoLoadKeybinds )
					{
						KeybindModule.ApplyKeybinds(val);
					}
				},
			});
		subMenu.AddDescription(menu, CurrentKeybindSlider,
			"MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT_DESC".AsDialog() + "\n\n" +
			"MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_CURRENTKEYBINDSLOT_DESCNOTE".AsDialog());

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_AUTOLOADKEYBINDS".AsDialog(), AutoLoadKeybinds)
			{
				OnValueChange = delegate(bool val) { AutoLoadKeybinds = val; },
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_AUTOLOADKEYBINDS_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				Log("Copying to: " + CurrentKeybindSlider.Index);
				Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS_TOOLTIP".AsDialog() + " " +
				             GetKeybindName(CurrentKeybindSlider.Index));

				KeybindModule.SaveKeybinds(CurrentKeybindSlider.Index);
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_COPYKEYBINDS_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_LOAD".AsDialog()));
		menuItem.Pressed(
			delegate { KeybindModule.ApplyKeybinds(CurrentKeybindSlider.Index); }
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_LOAD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_IMPORTCLIPBOARD".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				string clipboardText = TextInput.GetClipboardText();
				if( !string.IsNullOrEmpty(clipboardText) &&
				    KeybindModule.RenameFile(CurrentKeybindSlot + "_" + GetKeybindName(CurrentKeybindSlot), clipboardText) )
				{
					ChangeKeybindName(CurrentKeybindSlot, clipboardText);
					CurrentKeybindSlider.Values.Insert(CurrentKeybindSlot + 1, Tuple.Create(clipboardText, CurrentKeybindSlot));
					CurrentKeybindSlider.Values.RemoveAt(CurrentKeybindSlot);
					CurrentKeybindSlider.SelectWiggler.Start();
				}
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_IMPORTCLIPBOARD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				int val = CurrentKeybindSlider.Values.Count;

				KeybindModule.SaveKeybinds(val);

				CurrentKeybindSlider.Add(GetKeybindName(val), val);
				CurrentKeybindSlider.SelectWiggler.Start();

				Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD_TOOLTIP".AsDialog());

				ButtonsSwapKeybinds.Add(new ButtonBinding());
				izumisQOL.InitializeButtonBinding(ButtonsSwapKeybinds[val]);
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_ADD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				if( keybindNames.Count > 1 )
				{
					int keybindSlot = CurrentKeybindSlider.Index;
					Tooltip.Add(
						tooltips: new Tooltip.Info("MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE_TOOLTIP".AsDialog() + " " +
						                           GetKeybindName(keybindSlot)), clearQueue: true);

					if( keybindSlot >= CurrentKeybindSlider.Values.Count - 1 )
					{
						keybindSlot--;
					}

					if( keybindSlot < 0 )
					{
						Log("keybindSlot was negative", LogLevel.Warn);
						keybindSlot = 0;
					}

					KeybindModule.RemoveKeybindSlot(CurrentKeybindSlider.Index);
					keybindNames.RemoveAt(CurrentKeybindSlider.Index);

					CurrentKeybindSlider.Values.Clear();
					int offset = 0;
					for( var i = 0; i < keybindNames.Count; i++ )
					{
						string? keybindName = GetKeybindName(i);
						if( keybindName is null )
						{
							offset++;
							continue;
						}
						
						CurrentKeybindSlider.Values.Add(new Tuple<string, int>(keybindName, i - offset));
					}

					CurrentKeybindSlider.Index = keybindSlot;

					CurrentKeybindSlider.SelectWiggler.Start();

					ButtonsSwapKeybinds.RemoveAt(ButtonsSwapKeybinds.Count - 1);

					if( AutoLoadKeybinds )
					{
						KeybindModule.ApplyKeybinds(keybindSlot);
					}
					else
					{
						Tooltip.Show();
					}
				}

				Log("only 1 item in slider");
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_KEYBINDSETTINGS_REMOVE_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public void CreateCurrentWhitelistSlotEntry(TextMenu menu, bool inGame)
	{
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_WHITELISTSETTINGS".AsDialog(), false);
		TextMenu.Item       menuItem;

		if( inGame )
		{
			menuItem = new TextMenu.SubHeader("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ONLYINMAINMENU".AsDialog(),
				topPadding: false);
			subMenu.Add(menuItem);
			menu.Add(subMenu);
			return;
		}

		WhitelistModule.Init();

		if( CurrentWhitelistSlot > whitelistNames.Count - 1 )
		{
			Log("CurrentWhitelistSlot > whitelistNames.Count - 1");
			CurrentWhitelistSlot = whitelistNames.Count - 1;
		}

		subMenu.Add(CurrentWhitelistSlider =
			new TextMenu.Slider("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_CURRENTWHITELIST".AsDialog(), GetWhitelistName, 0,
				whitelistNames.Count - 1, CurrentWhitelistSlot)
			{
				OnValueChange = delegate(int val) { CurrentWhitelistSlot = val; },
			});
		subMenu.AddDescription(menu, CurrentWhitelistSlider,
			"MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_CURRENTWHITELIST_DESC".AsDialog());

		var restartButton = ToggleableRestartButton.New("restartForApplyWhitelist");

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_APPLY".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				if( WhitelistModule.WriteToEverestBlacklist(GetWhitelistName(CurrentWhitelistSlot)) )
				{
					restartButton.Show(5, menu, subMenu);
				}
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_APPLY_DESC".AsDialog());
		subMenu.NeedsRelaunch(menu, menuItem);

		restartButton.AddToMenuIfIsShown(menu, subMenu);

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_SAVE".AsDialog()));
		menuItem.Pressed(
			delegate { WhitelistModule.SaveCurrentWhitelist(GetWhitelistName(CurrentWhitelistSlot), CurrentWhitelistSlot); }
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_SAVE_DESC".AsDialog());

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_EXCLUSIVE".AsDialog(), WhitelistIsExclusive)
			{
				OnValueChange = delegate(bool val) { WhitelistIsExclusive = val; },
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_EXCLUSIVE_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_IMPORTCLIPBOARD".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				string clipboardText = TextInput.GetClipboardText();

				if( !string.IsNullOrEmpty(clipboardText) &&
				    WhitelistModule.RenameFile(GetWhitelistName(CurrentWhitelistSlot), clipboardText) )
				{
					ChangeWhitelistName(CurrentWhitelistSlot, clipboardText);
					CurrentWhitelistSlider.Values.Insert(CurrentWhitelistSlot + 1,
						Tuple.Create(clipboardText, CurrentWhitelistSlot));
					CurrentWhitelistSlider.Values.RemoveAt(CurrentWhitelistSlot);
					CurrentWhitelistSlider.SelectWiggler.Start();
				}
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_IMPORTCLIPBOARD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				WhitelistModule.AddWhitelist();
				int val = whitelistNames.Count - 1;
				CurrentWhitelistSlider.Add(GetWhitelistName(val), val);
				CurrentWhitelistSlider.SelectWiggler.Start();

				Tooltip.Show("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD_TOOLTIP".AsDialog());
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_ADD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE".AsDialog()));
		menuItem.Pressed(
			delegate
			{
				if( whitelistNames.Count <= 1 )
				{
					return;
				}
				
				int whitelistSlot = CurrentWhitelistSlider.Index;
				Tooltip.Show("MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE_TOOLTIP".AsDialog() + " " +
				             GetWhitelistName(whitelistSlot));

				if( whitelistSlot >= CurrentWhitelistSlider.Values.Count - 1 )
				{
					whitelistSlot--;
				}

				CurrentWhitelistSlider.Values.Count.Log("count");

				if( whitelistSlot < 0 )
				{
					Log("whitelistSlot was negative", LogLevel.Warn);
					whitelistSlot = 0;
				}

				WhitelistModule.RemoveWhitelist(GetWhitelistName(CurrentWhitelistSlider.Index));
				whitelistNames.RemoveAt(CurrentWhitelistSlider.Index);

				var offset = 0;
				CurrentWhitelistSlider.Values.Clear();
				for( var i = 0; i < whitelistNames.Count; i++ )
				{
					string? whitelistName = GetWhitelistName(i);
					if( whitelistName is null )
					{
						offset++;
						continue;
					}
					
					CurrentWhitelistSlider.Values.Add(new Tuple<string, int>(whitelistName, i - offset));
				}

				CurrentWhitelistSlider.Index = whitelistSlot;

				CurrentWhitelistSlider.SelectWiggler.Start();
			}
		);
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_WHITELISTSETTINGS_REMOVE_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public void CreateBetterJournalEnabledEntry(TextMenu menu, bool inGame)
	{
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SETTINGS".AsDialog(), false);
		TextMenu.Item       menuItem;

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_ENABLED".AsDialog(), BetterJournalEnabled)
			{
				OnValueChange = val => BetterJournalEnabled = val,
			});

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_MODTIMEINJOURNAL".AsDialog(), ShowModTimeInJournal)
			{
				OnValueChange = val => ShowModTimeInJournal = val,
			});
		subMenu.AddDescription(menu, menuItem,
			"MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_MODTIMEINJOURNAL_DESC".AsDialog());

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SEPARATEABCSIDE".AsDialog(), SeparateABCSideTimes)
			{
				OnValueChange = val => SeparateABCSideTimes = val,
			});
		subMenu.AddDescription(menu, menuItem,
			"MODOPTIONS_IZUMISQOL_BETTERJOURNALSETTINGS_SEPARATEABCSIDE_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public void CreateGamepadPauserEnabledEntry(TextMenu menu, bool inGame)
	{
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_GAMEAPADPAUSESETTINGS_GAMEPADPASUESETTINGS".AsDialog(),
			false);
		TextMenu.Item menuItem;

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_GAMEAPADPAUSESETTINGS_ENABLE".AsDialog(), GamepadPauserEnabled)
			{
				OnValueChange = val => GamepadPauserEnabled = val,
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_GAMEAPADPAUSESETTINGS_ENABLE_DESC".AsDialog());

		subMenu.Add(menuItem =
			new TextMenu.Slider("MODOPTIONS_IZUMISQOL_GAMEAPADPAUSESETTINGS_PAUSEFRAMESINACTIVE".AsDialog(),
				i => i.ToString(), 3, 30, PauseAfterFramesGamepadInactive)
			{
				OnValueChange = val => PauseAfterFramesGamepadInactive = val,
			});
		subMenu.AddDescription(menu, menuItem,
			"MODOPTIONS_IZUMISQOL_GAMEAPADPAUSESETTINGS_PAUSEFRAMESINACTIVE_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public void CreateOBSIntegrationEnabledEntry(TextMenu menu, bool inGame)
	{
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_OBSSETTINGS_OBSSETTINGS".AsDialog(), false);
		TextMenu.Item       menuItem;

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_OBSSETTINGS_INTEGRATIONENABLED".AsDialog(), OBSIntegrationEnabled)
			{
				OnValueChange = val =>
				{
					OBSIntegrationEnabled = val;
					if( !OBSIntegrationEnabled )
					{
						OBSIntegration.Disconnect();
					}
				},
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_INTEGRATIONENABLED_DESC".AsDialog());

		subMenu.Add(menuItem = new DisableableButton("MODOPTIONS_IZUMISQOL_OBSSETTINGS_CONNECT".AsDialog(),
			() => OBSIntegration.IsConnected || OBSIntegration.WaitingForConnection)
		{
			OnPressed = () =>
			{
				if( OBSIntegrationEnabled )
				{
					OBSIntegration.Connect();
				}
				else
				{
					Tooltip.Show("MODOPTIONS_IZUMISQOL_OBSSETTINGS_INTEGRATIONDISABLED_TOOLTIP".AsDialog());
				}
			},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_CONNECT_DESC".AsDialog());

		subMenu.Add(menuItem =
			new DisableableButton("MODOPTIONS_IZUMISQOL_OBSSETTINGS_DISCONNECT".AsDialog(), () => !OBSIntegration.IsConnected)
			{
				OnPressed = OBSIntegration.Disconnect,
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_DISCONNECT_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_OBSSETTINGS_IMPORTHOSTPORT".AsDialog())
		{
			OnPressed = () =>
			{
				string clipboardText = TextInput.GetClipboardText();
				if( string.IsNullOrEmpty(clipboardText) )
				{
					Tooltip.Show("MODOPTIONS_IZUMISQOL_OBSSETTINGS_INVALIDCLIPBOARD_TOOLTIP".AsDialog());
				}
				else
				{
					OBSHostPort = clipboardText;
				}
			},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_IMPORTHOSTPORT_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Button("MODOPTIONS_IZUMISQOL_OBSSETTINGS_IMPORTPASSWORD".AsDialog())
		{
			OnPressed = () => OBSPassword = TextInput.GetClipboardText(),
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_IMPORTPASSWORD_DESC".AsDialog());

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_OBSSETTINGS_CONNECTSTARTUP".AsDialog(), ConnectToOBSWebsocketsOnStartup)
			{
				OnValueChange = val => ConnectToOBSWebsocketsOnStartup = val,
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_CONNECTSTARTUP_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Slider("MODOPTIONS_IZUMISQOL_OBSSETTINGS_SHOWINDICATOR".AsDialog(),
			index =>
				new[]
				{
					"MODOPTIONS_IZUMISQOL_OBSSETTINGS_SHOWINDICATOR_1".AsDialog(),
					"MODOPTIONS_IZUMISQOL_OBSSETTINGS_SHOWINDICATOR_2".AsDialog(),
					"MODOPTIONS_IZUMISQOL_OBSSETTINGS_SHOWINDICATOR_3".AsDialog(),
				}[index],
			0, 2, (int)ShowRecordingIndicatorWhen)
		{
			OnValueChange = val => ShowRecordingIndicatorWhen = (OBSRecordingIndicator.DisplayType)val,
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_SHOWINDICATOR_DESC".AsDialog());
		
		subMenu.Add(menuItem = new TextMenu.Slider("MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKRECORD".AsDialog(),
			index => OBSIntegration.PollFrequencyText[index],
			0, OBSIntegration.PollFrequencyText.Length - 1, OBSPollFrequencyIndex[RecordingType.Record])
		{
			OnValueChange = val =>
			{
				OBSPollFrequencyIndex[RecordingType.Record] = val;
				OBSIntegration.CancelObsPoll();
			},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKRECORD_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Slider("MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKSTREAM".AsDialog(),
			index => OBSIntegration.PollFrequencyText[index],
			0, OBSIntegration.PollFrequencyText.Length - 1, OBSPollFrequencyIndex[RecordingType.Stream])
		{
			OnValueChange = val =>
			{
				OBSPollFrequencyIndex[RecordingType.Stream] = val;
				OBSIntegration.CancelObsPoll();
			},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKSTREAM_DESC".AsDialog());

		subMenu.Add(menuItem = new TextMenu.Slider("MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKREPLAYBUFFER".AsDialog(),
			index => OBSIntegration.PollFrequencyText[index],
			0, OBSIntegration.PollFrequencyText.Length - 1, OBSPollFrequencyIndex[RecordingType.ReplayBuffer])
		{
			OnValueChange = val =>
			{
				OBSPollFrequencyIndex[RecordingType.ReplayBuffer] = val;
				OBSIntegration.CancelObsPoll();
			},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OBSSETTINGS_CHECKREPLAYBUFFER_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public void CreateNoClipEnabledEntry(TextMenu menu, bool inGame)
	{
		if( !inGame )
		{
			NoClipEnabled = false;
		}

		TextMenuExt.SubMenu subMenu = new("NoClip Settings", false);

		TextMenu.Item menuItem;
		subMenu.Add(menuItem = new TextMenu.OnOff("NoClip Enabled", NoClipEnabled)
		{
			OnValueChange = val => NoClipEnabled = val,
			Disabled      = !inGame,
		});
		subMenu.AddDescription(menu, menuItem, "Whether NoClip is enabled or not.");

		subMenu.Add(menuItem =
			new TextMenu.Slider("Movement Speed", index => (index * 0.25f).ToString(), 1, 16, NoClipNormalSpeed)
			{
				OnValueChange = val => NoClipNormalSpeed = val,
			});
		subMenu.AddDescription(menu, menuItem, "How fast regular movement speed is while NoClip is enabled.");

		subMenu.Add(menuItem =
			new TextMenu.Slider("Fast Movement Speed", index => (index * 0.5f).ToString(), 1, 16, NoClipFastSpeed)
			{
				OnValueChange = val => NoClipFastSpeed = val,
			});
		subMenu.AddDescription(menu, menuItem, "How fast movement speed is while holding the grab button.");

		subMenu.Add(menuItem =
			new TextMenu.Slider("Slow Movement Speed", index => (index * 0.125f).ToString(), 1, 16, NoClipSlowSpeed)
			{
				OnValueChange = val => NoClipSlowSpeed = val,
			});
		subMenu.AddDescription(menu, menuItem, "How fast movement speed is while holding the dash button.");

		menu.Add(subMenu);
	}

	public void CreateShowRestartButtonInMainMenuEntry(TextMenu menu, bool inGame)
	{
		TextMenuExt.SubMenu subMenu = new("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_OTHERSETTINGS".AsDialog(), false);
		TextMenu.Item       menuItem;

		var restartButton = ToggleableRestartButton.New("restartForMenuRestartButton");

		subMenu.Add(menuItem = new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_SHOWRESTARTINMAINMENU".AsDialog(),
			ShowRestartButtonInMainMenu)
		{
			OnValueChange =
				delegate(bool val)
				{
					restartButton.Show(3, menu, subMenu);
					ShowRestartButtonInMainMenu = val;
				},
		});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OTHERSETTINGS_SHOWRESTARTINMAINMENU_DESC".AsDialog());
		subMenu.NeedsRelaunch(menu, menuItem);

		restartButton.AddToMenuIfIsShown(menu, subMenu);

		subMenu.Add(menuItem =
			new TextMenu.OnOff("MODOPTIONS_IZUMISQOL_OTHERSETTINGS_VERBOSELOGGING".AsDialog(), VerboseLogging)
			{
				OnValueChange = val => VerboseLogging = val,
			});
		subMenu.AddDescription(menu, menuItem, "MODOPTIONS_IZUMISQOL_OTHERSETTINGS_VERBOSELOGGING_DESC".AsDialog());

		menu.Add(subMenu);
	}

	public TextMenu.Slider? GetCurrentKeybindSlider()
	{
		return CurrentKeybindSlider;
	}

	private string? GetWhitelistName(int index)
	{
		if( index > whitelistNames.Count - 1 || index < 0 )
		{
			Log("index: " + index + " out of bounds for whitelistNames");
			Tooltip.Add("MODOPTIONS_IZUMISQOL_WHITELISTERROR_GETNAME".AsDialog());
			return null;
		}

		return whitelistNames[index];
	}

	public void AddWhitelistName(string name)
	{
		if( !whitelistNames.Contains(name) )
		{
			Log(name);
			whitelistNames.Add(name);
			return;
		}

		Log(name + " is already another whitelist's name", LogLevel.Info);
	}

	public void ChangeWhitelistName(int index, string name)
	{
		if( index > whitelistNames.Count - 1 || index < 0 )
		{
			Log("index: " + index + " out of bounds for whitelistNames");
			Tooltip.Show("MODOPTIONS_IZUMISQOL_WHITELISTERROR_NAMECHANGE".AsDialog());
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

	private string? GetKeybindName(int index)
	{
		if( index > keybindNames.Count - 1 || index < 0 )
		{
			Log("index: " + index + " out of bounds for keybindNames");
			Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDERROR_GETNAME".AsDialog());
			return null;
		}

		return keybindNames[index];
	}

	public void ChangeKeybindName(int index, string name)
	{
		if( index > keybindNames.Count - 1 || index < 0 )
		{
			Log("index: " + index + " out of bounds for keybindNames");
			Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDERROR_NAMECHANGE".AsDialog());
			return;
		}

		keybindNames[index] = name;
	}

	public List<string> GetKeybindNames()
	{
		return keybindNames;
	}
}