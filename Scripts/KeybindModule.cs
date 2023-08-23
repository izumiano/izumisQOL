using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Celeste;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.izumisQOL
{
	public class KeybindModule : Global
	{
		public static List<Settings> KeybindSettings = new();
		private static string keybindsPath;

		public static void Init()
		{
			ModSettings = izumisQOL.ModSettings;

			SetUpDirectory();

			LoadKeybindFiles();

			MatchKeybindButtonsToKeybindSettings();
		}

		public static void Update()
		{
			int buttonPressed = CheckButtonSwapBinds();
			if(buttonPressed > -1)
			{
				ModSettings.CurrentKeybindSlot = buttonPressed;

				if (ModSettings.AutoLoadKeybinds)
				{
					ApplyKeybinds(ModSettings.CurrentKeybindSlot);
				}
				else
				{
					Tooltip.Show("Slot " + (buttonPressed + 1) + " selected");
				}
			}

			if (ModSettings.ButtonLoadKeybind.Pressed)
			{
				ApplyKeybinds(ModSettings.CurrentKeybindSlot);
			}
		}

		private static void SetUpDirectory()
		{
			keybindsPath = BaseDirectory + "Saves\\izumisQOL\\keybinds";
			Directory.CreateDirectory(keybindsPath);

			if (!File.Exists(keybindsPath + "/whitelist.txt"))
			{
				File.WriteAllText(keybindsPath + "/whitelist.txt", 
					"# Mods written here will have their settings copied by izumisQOL\n" +
					"# Lines starting with # are ignored"
					);
			}
		}

		private static int CheckButtonSwapBinds()
		{
			if (!ModSettings.EnableHotkeys)
				return -1;

			List<ButtonBinding> buttonSwapBinds = ModSettings.ButtonsSwapKeybinds;
			if (buttonSwapBinds == null)
				return -1;

			int val = -1;
			for (int i = 0; i < buttonSwapBinds.Count; i++)
			{
				if (buttonSwapBinds[i].Pressed)
				{
					val = i;
					break;
				}
			}
			if (val == -1)
				return -1;

			TextMenu.Slider currentKeybindSlider = ModSettings.GetCurrentKeybindSlider();
			if (currentKeybindSlider != null)
			{
				currentKeybindSlider.Index = val;
				currentKeybindSlider.SelectWiggler.Start();
			}
			return val;
		}

		public static void LoadKeybindFiles()
		{
			LoadVanillaKeybindFiles();
		}

		private static void LoadVanillaKeybindFiles()
		{
			Dictionary<string, string> keybindFiles = GetKeybindFilesIDPathValuePair(Directory.GetFiles(keybindsPath));

			int keybindIndex = -1;
			for(int i = 0; i < keybindFiles.Count; i++)
			{
				if (!keybindFiles.ContainsKey(i.ToString()))
					continue;

				string shortPath = keybindFiles[i.ToString()].Replace(BaseDirectory + "Saves\\", "").Replace(".celeste", ""); // izuMod\0_
				string fileName = shortPath.Replace("izumisQOL\\keybinds\\", "");

				string s = fileName.Remove(0, fileName.IndexOf('_') + 1);

				keybindIndex++;

				Log("Loading: " + shortPath);

				if (keybindIndex >= KeybindSettings.Count)
				{
					Log("Add keybind setting " + keybindIndex);
					KeybindSettings.Add(UserIO.Load<Settings>(shortPath, backup: false));
					ModSettings.AddKeybindName(s);
				}
				else
				{
					Log("Reload keybind setting " + keybindIndex);
					KeybindSettings[keybindIndex] = UserIO.Load<Settings>(shortPath, backup: false);
					ModSettings.ChangeKeybindName(keybindIndex, s);
				}
			}

			if (keybindIndex == -1)
			{
				SaveKeybinds(0);
				return;
			}
		}

		public static void SaveKeybinds(int keybindID)
		{
			Log("saving keybind " + keybindID);

			FileStream fileStream = File.Open("Saves/settings.celeste", FileMode.Open);

			string celesteSettingsFile;
			using(var sr = new StreamReader(fileStream))
			{
				celesteSettingsFile = sr.ReadToEnd();
			}
			fileStream.Dispose();

			string name;
			List<string> keybindNames = ModSettings.GetKeybindNames();
			if(keybindID < keybindNames.Count)
			{
				name = keybindNames[keybindID];
			}
			else
			{
				int index = 0;
				while(keybindNames.Contains("keybind" + index))
				{
					index++;
				}
				name = "keybind" + index;
			}
			File.WriteAllText(keybindsPath + "/" + keybindID + "_" + name + ".celeste", celesteSettingsFile);

			SaveModKeybinds(keybindID);

			LoadKeybindFiles();
			CopyFromKeybindsToKeybindSwapper(keybindID);
		}

		public static void RemoveKeybindSlot(int keybindID)
		{
			List<string> keybindNames = ModSettings.GetKeybindNames();

			Dictionary<string, string> keybindFiles = GetKeybindFilesIDPathValuePair(Directory.GetFiles(keybindsPath));

			string keybindDelPath = keybindsPath + "\\" + keybindID + "_" + keybindNames[keybindID] + ".celeste";
			Log("Deleting: " + keybindDelPath);
			File.Delete(keybindDelPath);

			foreach(EverestModule module in Everest.Modules)
			{
				string modName = module.Metadata.Name;
				if (!WhitelistContains(keybindsPath + "\\whitelist.txt", modName))
					continue;

				string modFileDelPath = keybindsPath + "\\" + keybindID + "_modsettings-" + modName + ".celeste";
				Log("Deleting Mod File: " + modFileDelPath);
				File.Delete(modFileDelPath);
			}

			for (int i = keybindID + 1; i < keybindFiles.Count; i++)
			{
				if (!keybindFiles.TryGetValue(i.ToString(), out string filePath))
					continue;

				MoveKeybindFileIndexDown(i, filePath);
				foreach(EverestModule module in Everest.Modules)
				{
					string modName = module.Metadata.Name;
					keybindID.Log("keybindID");
					if (!WhitelistContains(keybindsPath + "\\whitelist.txt", modName))
						continue;
					
					MoveKeybindFileIndexDown(i, keybindsPath + "\\" + i + "_modsettings-" + modName + ".celeste");
				}
			}

			KeybindSettings.RemoveAt(keybindID);

			static void MoveKeybindFileIndexDown(int keybindID, string filePath)
			{
				if (!File.Exists(filePath))
				{
					Log(filePath + " does not exist");
					return;
				}
				string fileName = Path.GetFileName(filePath);
				string newName = (keybindID - 1).ToString() + fileName.Remove(0, fileName.IndexOf('_'));

				Log("Renaming: " + fileName + " to: " + newName);
				File.Move(filePath, keybindsPath + "\\" + newName);
			}
		}

		private static Dictionary<string, string> GetKeybindFilesIDPathValuePair(string[] filePaths)
		{
			Dictionary<string, string> keybindFiles = new();
			foreach (string filePath in filePaths)
			{
				string fileName = filePath.Replace(keybindsPath + "\\", "");
				if (!fileName.Contains(".celeste"))
				{
					continue;
				}
				int underscoreIndex = fileName.IndexOf('_');
				if (fileName.Remove(0, underscoreIndex + 1).StartsWith("modsettings"))
					continue;

				string key = "";
				for (int i = 0; i < underscoreIndex; i++)
				{
					key += fileName[i];
				}
				if (keybindFiles.ContainsKey(key))
					continue;
				keybindFiles.Add(key, filePath);
			}
			return keybindFiles;
		}

		public static bool RenameFile(string origName, string newName)
		{
			try
			{
				if (!origName.Contains('_'))
				{
					Log("invalid name");
					return false;
				}

				string origNameIndex = "";
				for(int i = 0; i < origName.Length; i++)
				{
					if (origName[i] == '_')
						break;
					origNameIndex += origName[i];
				}
				string newPath = keybindsPath + "/" + origNameIndex + "_" + newName + ".celeste";
				if (File.Exists(newPath))
				{
					Log(newPath + " already exists", LogLevel.Info);
					Tooltip.Show(newName + " already exists");
					return false;
				}
				File.Move(keybindsPath + "/" + origName + ".celeste", newPath);
				Tooltip.Show("Imported: " + newName + " from clipboard");
				return true;
			}
			catch (Exception ex)
			{
				Tooltip.Show("Invalid text in clipboard");
				Log(ex, LogLevel.Warn);
				return false;
			}
		}

		public static void ApplyKeybinds(int keybindID)
		{
			if(keybindID >= ModSettings.GetKeybindNames().Count)
			{
				Log(keybindID + " exceeded the size of keybindNames");
				Tooltip.Show("Failed applying keybind id: " + keybindID);
				return;
			}
			Log("Applying keybind " + ModSettings.GetKeybindNames()[keybindID]);
			Tooltip.Show("Applying keybind " + ModSettings.GetKeybindNames()[keybindID]);

			CopyFromKeybindSwapperToKeybinds(keybindID);

			LoadModKeybinds(keybindID);
		}

		private static void SaveModKeybinds(int keybindID)
		{
			foreach (EverestModule module in Everest.Modules)
			{
				if (!WhitelistContains(keybindsPath + "/whitelist.txt", module.Metadata.Name))
					continue;

				try
				{
					object settings = module._Settings;
					if (settings is null)
					{
						Log("mod settings was null");
						Log("Could not write modsettings to keybind file.", LogLevel.Warn);
						continue;
					}
					Log(settings);

					bool doSave = true;

					PropertyInfo[] properties = module.SettingsType.GetProperties();
					ModKeybindsSaveType settingsSave = new ModKeybindsSaveType();
					foreach (PropertyInfo prop in properties)
					{
						SettingInGameAttribute attribInGame;
						if (((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null && attribInGame.InGame != Engine.Scene is Level) || prop.GetCustomAttribute<SettingIgnoreAttribute>() != null || !prop.CanRead || !prop.CanWrite)
						{
							continue;
						}
						if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType))
						{
							//if (prop.GetValue(settings) is null)
							//{
							//	Log("property was null");
							//	doSave = false;
							//	break;
							//}

							if (prop.GetValue(settings) is ButtonBinding)
							{
								List<Binding> buttonBindingList = new();
								ButtonBinding binding = (ButtonBinding)prop.GetValue(settings);
								if(binding is null || binding.Button is null || binding.Button.Binding is null)
								{
									Log("binding was null");
									doSave = false;
									break;
								}

								buttonBindingList.Add(binding.Button.Binding);
								settingsSave.buttonBindings.Add(prop.Name, buttonBindingList);
							}
							continue;
						}
						if (typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType))
						{
							if (prop.GetValue(settings) is List<ButtonBinding>)
							{
								List<Binding> bindingList = new();
								List<ButtonBinding> buttonBindingList = (List<ButtonBinding>)prop.GetValue(settings);
								buttonBindingList.ForEach(delegate (ButtonBinding buttonBinding)
								{
									if(buttonBinding is not null && buttonBinding.Button is not null && buttonBinding.Button.Binding is not null)
									{
										bindingList.Add(buttonBinding.Button.Binding);
									}
									else
									{
										Log("binding in list was null");
										doSave = false;
									}

								});
								settingsSave.buttonBindings.Add(prop.Name, bindingList);
							}
							continue;
						}
					}
					if (doSave)
					{
						FileStream fileStream2 = File.Create(keybindsPath + "\\" + keybindID + "_modsettings-" + module.Metadata.Name + ".celeste");
						using StreamWriter writer = new StreamWriter(fileStream2);
						YamlHelper.Serializer.Serialize(writer, settingsSave, typeof(ModKeybindsSaveType));
					}
					else
					{
						Log("Could not save " + module.Metadata.Name + " to id: " + keybindID, LogLevel.Warn);
					}
				}
				catch
				{
					Log("Could not write modsettings to keybind file.", LogLevel.Warn);
				}
			}
		}

		private static void LoadModKeybinds(int keybindID)
		{
			Log("Reloading mod keybinds");
			foreach (EverestModule module in Everest.Modules)
			{
				string modName = module.Metadata.Name;

				bool inWhitelist = WhitelistContains(keybindsPath + "/whitelist.txt", modName);
				Log(modName + " in whitelist: " + inWhitelist);

				string modPath = keybindsPath + "\\" + keybindID + "_modsettings-" + modName + ".celeste";
				if (!inWhitelist)
				{
					continue;
				}
				if (!File.Exists(modPath))
				{
					Log(modName + " does not have a keybind file saved");
					continue;
				}

				try
				{
					FileStream fileStream = File.OpenRead(modPath);
					using StreamReader reader = new StreamReader(fileStream);
					ModKeybindsSaveType modKeybindsSaveType = (ModKeybindsSaveType)YamlHelper.Deserializer.Deserialize(reader, typeof(ModKeybindsSaveType));

					object settings = module._Settings;
					PropertyInfo[] properties = module.SettingsType.GetProperties();
					foreach (PropertyInfo prop in properties)
					{
						SettingInGameAttribute attribInGame;
						if (((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null && attribInGame.InGame != Engine.Scene is Level) || prop.GetCustomAttribute<SettingIgnoreAttribute>() != null || !prop.CanRead || !prop.CanWrite)
						{
							continue;
						}
						if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType))
						{
							if (prop.GetValue(settings) is ButtonBinding buttonBinding)
							{
								Binding modBinding = buttonBinding.Button.Binding;
								Binding customBinding = modKeybindsSaveType.buttonBindings[prop.Name][0];
								ClearKey(modBinding);
								modBinding.Add(customBinding.Keyboard.ToArray());
								modBinding.Add(customBinding.Controller.ToArray());
								modBinding.Add(customBinding.Mouse.ToArray());
							}
							continue;
						}
						if (typeof(List<ButtonBinding>).IsAssignableFrom(prop.PropertyType))
						{
							if(prop.GetValue(settings) is List<ButtonBinding> propButtonBindings)
							{
								List<Binding> savedBindings = modKeybindsSaveType.buttonBindings[prop.Name];
								for(int i = 0; i< savedBindings.Count; i++)
								{
									Binding modBinding = propButtonBindings[i].Button.Binding;
									ClearKey(modBinding);
									modBinding.Add(savedBindings[i].Keyboard.ToArray());
									modBinding.Add(savedBindings[i].Controller.ToArray());
									modBinding.Add(savedBindings[i].Mouse.ToArray());
								}
							}
							continue;
						}
					}
				}
				catch
				{
					Log("Could not read modsettings from keybind file.", LogLevel.Warn);
				}
			}
		}

		public static void CopyFromKeybindSwapperToKeybinds(int keybindID)
		{
			if(keybindID > KeybindSettings.Count)
				return;
			Log("Swapper to keybinds");
			ChangeKeybindsFromAToB(KeybindSettings[keybindID], Settings.Instance, keybindID);
		}

		public static void CopyFromKeybindsToKeybindSwapper(int keybindID)
		{
			if (KeybindSettings.Count < keybindID)
				return;
			Log("Keybinds to swapper");
			ChangeKeybindsFromAToB(Settings.Instance, KeybindSettings[keybindID], keybindID);
		}

		private static void ChangeKeybindsFromAToB(Settings a, Settings b, int keybindID)
		{
			ModSettings.CurrentKeybindSlot = keybindID;
			Log("Modifying Key Bind " + ModSettings.CurrentKeybindSlot);

			if (a == null)
			{
				Log("setting 'a' was null", LogLevel.Warn);
				return;
			}
			if (b == null)
			{
				Log("setting 'b' was null", LogLevel.Warn);
				return;
			}

			//Settings.Instance            KeybindSettings[keybindID]
			#region
			// adding the menu binds earlier because they always need atleast one button bound
			b.MenuLeft.	Add(a.MenuLeft.	Keyboard.ToArray());
			b.MenuRight.Add(a.MenuRight.Keyboard.ToArray());
			b.MenuUp.	Add(a.MenuUp.	Keyboard.ToArray());
			b.MenuDown.	Add(a.MenuDown.	Keyboard.ToArray());
			b.Confirm.  Add(a.Confirm.	Keyboard.ToArray());
			b.Cancel.   Add(a.Cancel.	Keyboard.ToArray());
			b.Journal.  Add(a.Journal.	Keyboard.ToArray());
			b.Pause.    Add(a.Pause.	Keyboard.ToArray());

			b.MenuLeft. Add(a.MenuLeft.	Controller.ToArray());
			b.MenuRight.Add(a.MenuRight.Controller.ToArray());
			b.MenuUp.   Add(a.MenuUp.	Controller.ToArray());
			b.MenuDown. Add(a.MenuDown.	Controller.ToArray());
			b.Confirm.  Add(a.Confirm.	Controller.ToArray());
			b.Cancel.   Add(a.Cancel.	Controller.ToArray());
			b.Journal.  Add(a.Journal.	Controller.ToArray());
			b.Pause.    Add(a.Pause.	Controller.ToArray());

			b.MenuLeft.	Add(a.MenuLeft.	Mouse.ToArray());
			b.MenuRight.Add(a.MenuRight.Mouse.ToArray());
			b.MenuUp.	Add(a.MenuUp.	Mouse.ToArray());
			b.MenuDown.	Add(a.MenuDown.	Mouse.ToArray());
			b.Confirm.	Add(a.Confirm.	Mouse.ToArray());
			b.Cancel.	Add(a.Cancel.	Mouse.ToArray());
			b.Journal.	Add(a.Journal.	Mouse.ToArray());
			b.Pause.	Add(a.Pause.	Mouse.ToArray());
			#endregion

			Binding[] bindings =
			{
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
				b.UpMoveOnly
			};
			Binding[] newBindings =
			{
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
				a.UpMoveOnly
			};

			for (int i = 0; i < bindings.Length; i++)
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
			if (ModSettings.ButtonsSwapKeybinds == null)
				return;

			int swapKeybindsCount = ModSettings.ButtonsSwapKeybinds.Count;
			if (swapKeybindsCount > KeybindSettings.Count)
			{
				Log(ModSettings.ButtonsSwapKeybinds.Count);
				ModSettings.ButtonsSwapKeybinds.RemoveRange(KeybindSettings.Count, swapKeybindsCount - KeybindSettings.Count);
				Log(ModSettings.ButtonsSwapKeybinds.Count);
			}
			swapKeybindsCount = ModSettings.ButtonsSwapKeybinds.Count;
			for (int i = swapKeybindsCount; i < KeybindSettings.Count; i++)
			{
				ModSettings.ButtonsSwapKeybinds.Add(new ButtonBinding());
			}
		}

		private class ModKeybindsSaveType
		{
			public Dictionary<string, List<Binding>> buttonBindings = new();
		}
	}
}
