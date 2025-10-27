using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Celeste.Mod.izumisQOL.Scripts;
using Monocle;

namespace Celeste.Mod.izumisQOL;

public static class KeybindModule
{
	public static readonly  List<Settings> KeybindSettings = [ ];
	private static readonly string         keybindsPath    = UserIO.SavePath.SanitizeFilePath() + "/izumisQOL/keybinds";

	public static void Load()
	{
		SetUpDirectory();

		LoadKeybindFiles();

		MatchKeybindButtonsToKeybindSettings();
	}

	public static void Update()
	{
		var buttonPressed = CheckButtonSwapBinds();
		if( buttonPressed > -1 )
		{
			ModSettings.CurrentKeybindSlot = buttonPressed;

			if( ModSettings.AutoLoadKeybinds )
			{
				ApplyKeybinds(ModSettings.CurrentKeybindSlot);
			}
			else
			{
				Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDS_SLOTSELECTED1".AsDialog() + " " + (buttonPressed + 1) + " " +
					"MODOPTIONS_IZUMISQOL_KEYBINDS_SLOTSELECTED2".AsDialog());
			}
		}

		if( ModSettings.ButtonLoadKeybind.Pressed )
		{
			ApplyKeybinds(ModSettings.CurrentKeybindSlot);
		}
	}

	private static void SetUpDirectory()
	{
		Directory.CreateDirectory(keybindsPath);

		if( !File.Exists(keybindsPath + "/whitelist.txt") )
		{
			File.WriteAllText(keybindsPath + "/whitelist.txt",
				"# Mods written here will have their settings copied by izumisQOL\n" +
				"# Lines starting with # are ignored"
			);
		}
	}

	private static int CheckButtonSwapBinds()
	{
		if( !ModSettings.EnableHotkeys )
		{
			return -1;
		}

		var buttonSwapBinds = ModSettings.ButtonsSwapKeybinds;

		var val = -1;
		for( var i = 0; i < buttonSwapBinds.Count; i++ )
		{
			if( !buttonSwapBinds[i].Pressed )
			{
				continue;
			}

			val = i;
			break;
		}

		if( val == -1 )
		{
			return -1;
		}

		var currentKeybindSlider = ModSettings.GetCurrentKeybindSlider();
		if( currentKeybindSlider is null )
		{
			return val;
		}

		currentKeybindSlider.Index = val;
		currentKeybindSlider.SelectWiggler.Start();
		return val;
	}

	public static void LoadKeybindFiles()
	{
		LoadVanillaKeybindFiles();
	}

	private static void LoadVanillaKeybindFiles()
	{
		var keybindFiles = GetKeybindFilesIDPathValuePair();

		var keybindIndex = -1;
		for( var i = 0; i < keybindFiles.Count; i++ )
		{
			var file = keybindFiles[i];
			var shortPath = file.path
			                    .Replace(UserIO.SavePath.SanitizeFilePath() + "/", "")
			                    .Replace(".celeste", "");
			var fileName = shortPath.Replace("izumisQOL/keybinds/", "");

			var s = fileName.Remove(0, fileName.IndexOf('_') + 1);

			keybindIndex++;

			Log("Loading: " + shortPath);

			if( keybindIndex >= KeybindSettings.Count )
			{
				Log("Add keybind setting " + keybindIndex);
				KeybindSettings.Add(UserIO.Load<Settings>(shortPath, false));
				ModSettings.AddKeybindName(s);
			}
			else
			{
				Log("Reload keybind setting " + keybindIndex);
				KeybindSettings[keybindIndex] = UserIO.Load<Settings>(shortPath, false);
				ModSettings.ChangeKeybindName(keybindIndex, s);
			}
		}

		if( keybindIndex == -1 )
		{
			SaveKeybinds(0);
		}
	}

	public static void SaveKeybinds(int keybindID)
	{
		Log("getting ready to save keybind " + keybindID);
		var fileStream =
			File.Open((UserIO.SavePath.SanitizeFilePath() + "/settings.celeste").Log("reading celeste settings at"),
				FileMode.Open);

		string celesteSettingsFile;
		using( var sr = new StreamReader(fileStream) )
		{
			celesteSettingsFile = sr.ReadToEnd();
		}

		fileStream.Dispose();

		string name;
		var    keybindNames = ModSettings.GetKeybindNames();
		if( keybindID < keybindNames.Count )
		{
			name = keybindNames[keybindID];
		}
		else
		{
			var index = 0;
			while( keybindNames.Contains("keybind" + index) )
			{
				index++;
			}

			name = "keybind" + index;
		}

		Log("saving keybind " + keybindID + " to path:");
		File.WriteAllText((keybindsPath + "/" + keybindID + "_" + name + ".celeste").Log(), celesteSettingsFile);

		SaveModKeybinds(keybindID);

		LoadKeybindFiles();
		CopyFromKeybindsToKeybindSwapper(keybindID);
	}

	public static void RemoveKeybindSlot(int keybindID)
	{
		var keybindNames = ModSettings.GetKeybindNames();

		var keybindFiles = GetKeybindFilesIDPathValuePair();

		var keybindDelPath = keybindsPath + "/" + keybindID + "_" + keybindNames[keybindID] + ".celeste";
		Log("Deleting: " + keybindDelPath, LogLevel.Info);
		File.Delete(keybindDelPath);

		var modKeybinds = GetIndexGroupedModKeybinds();

		if( modKeybinds.TryGetValue(keybindID, out var mainModKeybindsGroup) )
		{
			foreach( var modKeybind in mainModKeybindsGroup )
			{
				Log("Deleting Mod File: " + modKeybind.path, LogLevel.Info);
				File.Delete(modKeybind.path);
			}
		}

		for( var i = keybindID + 1; i < keybindFiles.Count; i++ )
		{
			var fileName = keybindFiles[i].name;
			RenameFile($"{i}_{fileName}", fileName, i - 1);

			if( modKeybinds.TryGetValue(i, out var modKeybindsGroup) )
			{
				foreach( var modKeybind in modKeybindsGroup )
				{
					RenameFile($"{i}_{modKeybind.name}", modKeybind.name, i - 1);
				}
			}
		}

		KeybindSettings.RemoveAt(keybindID);
	}

	private static List<(string path, string name, int index)> GetKeybindFilesIDPathValuePair()
	{
		var filePaths = Directory.GetFiles(keybindsPath).SanitizeFilePath();

		var keybindFiles       = new List<(int id, string path, string name)>();
		var failedKeybindFiles = new List<(string path, string name)>();
		foreach( var filePath in filePaths )
		{
			var fullFileName = filePath.Replace(keybindsPath + "/", "");
			if( !fullFileName.EndsWith(".celeste") )
			{
				continue;
			}

			var underscoreIndex = fullFileName.IndexOf('_');
			underscoreIndex = underscoreIndex == 0 ? -1 : underscoreIndex; // dont allow _ as the first character
			var keybindName =
				fullFileName[(underscoreIndex + 1)..fullFileName.IndexOf(".celeste", StringComparison.Ordinal)];
			Log(keybindName);
			if( fullFileName.Remove(0, underscoreIndex + 1).StartsWith("modsettings") )
			{
				continue;
			}

			var key = "";
			for( var i = 0; i < underscoreIndex; i++ )
			{
				key += fullFileName[i];
			}

			if( int.TryParse(key, out var index) )
			{
				keybindFiles.Add((id: index, path: filePath, name: keybindName));
			}
			else
			{
				failedKeybindFiles.Add((path: filePath, name: keybindName));
			}
		}

		return FixKeybindFileNames(keybindFiles, failedKeybindFiles);
	}

	private static List<(string path, string name, int index)> FixKeybindFileNames(
		List<(int id, string path, string name)> keybindFiles, List<(string path, string name)> failedKeybindFiles
	)
	{
		if( failedKeybindFiles.Count > 0 )
		{
			failedKeybindFiles.Select(file => file.name)
			                  .Log("Found invalid keybind file names", LogLevel.Warn, LogParser.Enumerable);
		}

		var fixedKeybindFiles = new List<(string path, string name, int index)>();

		var modKeybinds = GetIndexGroupedModKeybinds();

		var orderedKeybindFiles = keybindFiles.OrderBy(id => id).ToList();
		for( var i = 0; i < orderedKeybindFiles.Count; i++ )
		{
			var file = orderedKeybindFiles[i];
			if( file.id != i && RenameFile($"{file.id}_{file.name}", file.name, index: i,
				newPath: out var newPath) )
			{
				if( modKeybinds.TryGetValue(file.id, out var modKeybindsGroup) )
				{
					foreach( var modKeybind in modKeybindsGroup )
					{
						RenameFile($"{file.id}_{modKeybind.name}", modKeybind.name, i);
					}
				}

				file = file with { path = newPath, };
			}

			fixedKeybindFiles.Add((file.path, file.name, index: i));
		}

		for( var i = 0; i < failedKeybindFiles.Count; i++ )
		{
			var file = failedKeybindFiles[i];
			if( RenameFile(file.name, file.name, index: i + keybindFiles.Count,
				newPath: out var newPath) )
			{
				fixedKeybindFiles.Add((path: newPath, file.name, index: i + keybindFiles.Count));
			}
		}

		var oldIds = LogParser.Enumerable(keybindFiles.Select(file => file.id));
		oldIds.Log("Found keybind ids", LogLevel.Info);
		var newIds = LogParser.Enumerable(fixedKeybindFiles.Select(file => file.index));
		if( oldIds != newIds )
		{
			Log($"keybind ids changed from {oldIds} to {newIds}", LogLevel.Warn);
		}

		return fixedKeybindFiles;
	}

	private static Dictionary<int, IGrouping<int, (string name, string path, int index)>> GetIndexGroupedModKeybinds()
	{
		var isKeybindFileRegex = new Regex(@"^\d+_modsettings-.+\.celeste$");
		return Directory.GetFiles(keybindsPath)
		                .Select(file => (name: Path.GetFileName(file), path: file))
		                .Where(file => isKeybindFileRegex.IsMatch(file.name))
		                .Select(file =>
		                (
			                name: file.name[
				                (file.name.IndexOf('_', StringComparison.Ordinal) + 1)..file.name.IndexOf(".celeste",
					                StringComparison.Ordinal)],
			                file.path,
			                index: int.TryParse(file.name[..file.name.IndexOf('_')], out var index)
				                ? index
				                : -1))
		                .GroupBy(file => file.index)
		                .ToDictionary(group => group.Key);
	}

	public static bool RenameFile(string origName, string newName, int? index = null, bool showTooltip = false)
	{
		return RenameFile(origName, newName, out _, index, showTooltip);
	}

	public static bool RenameFile(
		string origName, string newName, [NotNullWhen(true)] out string? newPath, int? index = null,
		bool   showTooltip = false
	)
	{
		Log($"Renaming keybind: {origName} to {newName}{(index is null ? "" : $" at index {index}")}...", LogLevel.Info);

		newPath = null;
		try
		{
			var oldPath = keybindsPath + "/" + origName + ".celeste";
			if( !File.Exists(oldPath) )
			{
				Log(oldPath + " does not exist", LogLevel.Warn);
				return false;
			}

			var origNameIndex = index ?? int.Parse(origName[..origName.IndexOf('_')]);
			var _newPath      = keybindsPath + "/" + origNameIndex + "_" + newName + ".celeste";
			if( File.Exists(_newPath) )
			{
				Log(_newPath + " already exists", LogLevel.Info);
				if( showTooltip )
				{
					Tooltip.Show(newName + " " + "MODOPTIONS_IZUMISQOL_KEYBINDS_EXISTS".AsDialog());
				}

				return false;
			}

			File.Move(oldPath, _newPath);
			if( showTooltip )
			{
				Tooltip.Show("MODOPTIONS_IZUMISQOL_IMPORTED1".AsDialog() + " " + newName + " " +
					"MODOPTIONS_IZUMISQOL_IMPORTED2".AsDialog());
			}

			newPath = _newPath;
			return true;
		}
		catch( Exception ex )
		{
			Tooltip.Show("MODOPTIONS_IZUMISQOL_ERROR_INVALIDCLIPBOARD".AsDialog());
			Log(ex, LogLevel.Warn);
			return false;
		}
	}

	public static void ApplyKeybinds(int keybindID)
	{
		if( keybindID >= ModSettings.GetKeybindNames().Count )
		{
			Log(keybindID + " exceeded the size of keybindNames");
			Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDERROR_APPLYING".AsDialog() + " " + keybindID);
			return;
		}

		Log("Applying keybind: " + ModSettings.GetKeybindNames()[keybindID]);
		Tooltip.Show("MODOPTIONS_IZUMISQOL_KEYBINDS_APPLYING".AsDialog() + " " + ModSettings.GetKeybindNames()[keybindID]);

		CopyFromKeybindSwapperToKeybinds(keybindID);

		LoadModKeybinds(keybindID);
	}

	private static void SaveModKeybinds(int keybindID)
	{
		foreach( var module in Everest.Modules )
		{
			if( module.Metadata.Name == "Celeste" ||
				!WhitelistContains(keybindsPath + "/whitelist.txt", module.Metadata.Name) )
			{
				continue;
			}

			try
			{
				object settings = module._Settings;
				if( settings is null )
				{
					Log($"{module.Metadata.Name} does not have a settings module, skipping");
					continue;
				}

				Log(settings);

				var doSave = true;

				var                 properties   = module.SettingsType.GetProperties();
				ModKeybindsSaveType settingsSave = new();
				foreach( var prop in properties )
				{
					SettingInGameAttribute? attribInGame;
					if( ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) is not null &&
							attribInGame.InGame != Engine.Scene is Level) ||
						prop.GetCustomAttribute<SettingIgnoreAttribute>() is not null || !prop.CanRead || !prop.CanWrite )
					{
						continue;
					}

					if( typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType) )
					{
						if( prop.GetValue(settings) is ButtonBinding )
						{
							List<Binding> buttonBindingList = [ ];
							var           binding           = (ButtonBinding?)prop.GetValue(settings);
							if( binding?.Button?.Binding is null )
							{
								Log("binding was null");
								doSave = false;
								break;
							}

							buttonBindingList.Add(binding.Button.Binding);
							settingsSave.ButtonBindings.Add(prop.Name, buttonBindingList);
						}

						continue;
					}

					if( typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType) )
					{
						if( prop.GetValue(settings) is List<ButtonBinding> )
						{
							List<Binding> bindingList       = [ ];
							var           buttonBindingList = (List<ButtonBinding>?)prop.GetValue(settings);
							buttonBindingList?.ForEach(delegate(ButtonBinding buttonBinding)
							{
								if( buttonBinding.Button?.Binding is not null )
								{
									bindingList.Add(buttonBinding.Button.Binding);
								}
								else
								{
									Log("binding in list was null");
									doSave = false;
								}
							});
							settingsSave.ButtonBindings.Add(prop.Name, bindingList);
						}
					}
				}

				if( doSave )
				{
					var fileStream2 =
						File.Create(keybindsPath + "/" + keybindID + "_modsettings-" + module.Metadata.Name + ".celeste");
					using StreamWriter writer = new(fileStream2);
					YamlHelper.Serializer.Serialize(writer, settingsSave, typeof(ModKeybindsSaveType));
				}
				else
				{
					Log("Could not save " + module.Metadata.Name + " to id: " + keybindID, LogLevel.Warn);
				}
			}
			catch( Exception ex )
			{
				Log("Could not write modsettings to keybind file.\n" + ex, LogLevel.Warn);
			}
		}
	}

	private static void LoadModKeybinds(int keybindID)
	{
		Log("Reloading mod keybinds");
		foreach( var module in Everest.Modules )
		{
			var modName = module.Metadata.Name;

			var inWhitelist = WhitelistContains(keybindsPath + "/whitelist.txt", modName);
			Log(modName + " in whitelist: " + inWhitelist);

			var modPath = keybindsPath + "/" + keybindID + "_modsettings-" + modName + ".celeste";
			if( !inWhitelist )
			{
				continue;
			}

			if( !File.Exists(modPath) )
			{
				Log(modName + " does not have a keybind file saved");
				continue;
			}

			try
			{
				var       fileStream          = File.OpenRead(modPath);
				using var reader              = new StreamReader(fileStream);
				var       modKeybindsSaveType = YamlHelper.Deserializer.Deserialize<ModKeybindsSaveType>(reader);

				object settings   = module._Settings;
				var    properties = module.SettingsType.GetProperties();
				foreach( var prop in properties )
				{
					SettingInGameAttribute? attribInGame;
					if( ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) is not null &&
							attribInGame.InGame != Engine.Scene is Level) ||
						prop.GetCustomAttribute<SettingIgnoreAttribute>() is not null || !prop.CanRead || !prop.CanWrite )
					{
						continue;
					}

					if( typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType) )
					{
						if( prop.GetValue(settings) is ButtonBinding buttonBinding && modKeybindsSaveType is not null )
						{
							var modBinding    = buttonBinding.Button.Binding;
							var customBinding = modKeybindsSaveType.ButtonBindings[prop.Name][0];
							ClearKey(modBinding);
							modBinding.Add(customBinding.Keyboard.ToArray());
							modBinding.Add(customBinding.Controller.ToArray());
							modBinding.Add(customBinding.Mouse.ToArray());
						}

						continue;
					}

					if( typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType) )
					{
						if( prop.GetValue(settings) is List<ButtonBinding> propButtonBindings && modKeybindsSaveType is not null )
						{
							var savedBindings = modKeybindsSaveType.ButtonBindings[prop.Name];
							for( var i = 0; i < savedBindings.Count; i++ )
							{
								var modBinding = propButtonBindings[i].Button.Binding;
								ClearKey(modBinding);
								modBinding.Add(savedBindings[i].Keyboard.ToArray());
								modBinding.Add(savedBindings[i].Controller.ToArray());
								modBinding.Add(savedBindings[i].Mouse.ToArray());
							}
						}
					}
				}
			}
			catch( Exception ex )
			{
				Log("Could not read modsettings from keybind file.\n" + ex, LogLevel.Warn);
			}
		}
	}

	public static void CopyFromKeybindSwapperToKeybinds(int keybindID)
	{
		if( keybindID > KeybindSettings.Count )
		{
			return;
		}

		Log("Swapper to keybinds");
		ChangeKeybindsFromAToB(KeybindSettings[keybindID], Settings.Instance, keybindID);
		UserIO.SaveHandler(false, true);
	}

	public static void CopyFromKeybindsToKeybindSwapper(int keybindID)
	{
		if( KeybindSettings.Count < keybindID )
		{
			return;
		}

		Log("Keybinds to swapper");
		ChangeKeybindsFromAToB(Settings.Instance, KeybindSettings[keybindID], keybindID);
	}

	private static void ChangeKeybindsFromAToB(Settings a, Settings b, int keybindID)
	{
		ModSettings.CurrentKeybindSlot = keybindID;
		Log("Modifying Key Bind " + ModSettings.CurrentKeybindSlot);

		//Settings.Instance            KeybindSettings[keybindID]

		#region

		// adding the menu binds earlier because they always need atleast one button bound
		b.MenuLeft.Add(a.MenuLeft.Keyboard.ToArray());
		b.MenuRight.Add(a.MenuRight.Keyboard.ToArray());
		b.MenuUp.Add(a.MenuUp.Keyboard.ToArray());
		b.MenuDown.Add(a.MenuDown.Keyboard.ToArray());
		b.Confirm.Add(a.Confirm.Keyboard.ToArray());
		b.Cancel.Add(a.Cancel.Keyboard.ToArray());
		b.Journal.Add(a.Journal.Keyboard.ToArray());
		b.Pause.Add(a.Pause.Keyboard.ToArray());

		b.MenuLeft.Add(a.MenuLeft.Controller.ToArray());
		b.MenuRight.Add(a.MenuRight.Controller.ToArray());
		b.MenuUp.Add(a.MenuUp.Controller.ToArray());
		b.MenuDown.Add(a.MenuDown.Controller.ToArray());
		b.Confirm.Add(a.Confirm.Controller.ToArray());
		b.Cancel.Add(a.Cancel.Controller.ToArray());
		b.Journal.Add(a.Journal.Controller.ToArray());
		b.Pause.Add(a.Pause.Controller.ToArray());

		b.MenuLeft.Add(a.MenuLeft.Mouse.ToArray());
		b.MenuRight.Add(a.MenuRight.Mouse.ToArray());
		b.MenuUp.Add(a.MenuUp.Mouse.ToArray());
		b.MenuDown.Add(a.MenuDown.Mouse.ToArray());
		b.Confirm.Add(a.Confirm.Mouse.ToArray());
		b.Cancel.Add(a.Cancel.Mouse.ToArray());
		b.Journal.Add(a.Journal.Mouse.ToArray());
		b.Pause.Add(a.Pause.Mouse.ToArray());

		#endregion

		Binding[] bindings =
		[
			b.Cancel,
			b.Confirm,
			b.Dash,
			b.DemoDash,
			b.Down,
			b.DownDashOnly,
			b.DownMoveOnly,
			b.Grab,
			b.Journal,
			b.Jump,
			b.Left,
			b.LeftDashOnly,
			b.LeftMoveOnly,
			b.MenuDown,
			b.MenuLeft,
			b.MenuRight,
			b.MenuUp,
			b.Pause,
			b.QuickRestart,
			b.Right,
			b.RightDashOnly,
			b.RightMoveOnly,
			b.Talk,
			b.Up,
			b.UpDashOnly,
			b.UpMoveOnly,
		];
		Binding[] newBindings =
		[
			a.Cancel,
			a.Confirm,
			a.Dash,
			a.DemoDash,
			a.Down,
			a.DownDashOnly,
			a.DownMoveOnly,
			a.Grab,
			a.Journal,
			a.Jump,
			a.Left,
			a.LeftDashOnly,
			a.LeftMoveOnly,
			a.MenuDown,
			a.MenuLeft,
			a.MenuRight,
			a.MenuUp,
			a.Pause,
			a.QuickRestart,
			a.Right,
			a.RightDashOnly,
			a.RightMoveOnly,
			a.Talk,
			a.Up,
			a.UpDashOnly,
			a.UpMoveOnly,
		];

		for( var i = 0; i < bindings.Length; i++ )
		{
			ClearKey(bindings[i]);
			bindings[i].Add(newBindings[i].Keyboard.ToArray());
			bindings[i].Add(newBindings[i].Controller.ToArray());
			bindings[i].Add(newBindings[i].Mouse.ToArray());
		}
	}

	private static void ClearKey(Binding binding)
	{
		binding.ClearGamepad();
		binding.ClearKeyboard();
		binding.ClearMouse();
	}

	private static void MatchKeybindButtonsToKeybindSettings()
	{
		var swapKeybindsCount = ModSettings.ButtonsSwapKeybinds.Count;
		if( swapKeybindsCount > KeybindSettings.Count )
		{
			Log(ModSettings.ButtonsSwapKeybinds.Count);
			ModSettings.ButtonsSwapKeybinds.RemoveRange(KeybindSettings.Count, swapKeybindsCount - KeybindSettings.Count);
			Log(ModSettings.ButtonsSwapKeybinds.Count);
		}

		swapKeybindsCount = ModSettings.ButtonsSwapKeybinds.Count;
		for( var i = swapKeybindsCount; i < KeybindSettings.Count; i++ )
		{
			ModSettings.ButtonsSwapKeybinds.Add(new ButtonBinding());
		}
	}

	private class ModKeybindsSaveType
	{
		// ReSharper disable once FieldCanBeMadeReadOnly.Local
		public Dictionary<string, List<Binding>> ButtonBindings = new();
	}
}