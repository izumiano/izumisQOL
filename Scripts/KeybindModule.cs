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
			izuSettings = izumisQOL.ModSettings;

			SetUpDirectory();

			LoadKeybindFiles();

			MatchKeybindButtonsToKeybindSettings();
		}

		public static void Update()
		{
			int buttonPressed = CheckButtonSwapBinds();
			if(buttonPressed > -1)
			{
				izuSettings.CurrentKeybindSlot = buttonPressed;

				if (izuSettings.AutoLoadKeybinds)
				{
					ApplyKeybinds(izuSettings.CurrentKeybindSlot);
				}
				else
				{
					Tooltip.Show("Slot " + (buttonPressed + 1) + " selected");
				}
			}

			if (izuSettings.ButtonLoadKeybind.Pressed)
			{
				ApplyKeybinds(izuSettings.CurrentKeybindSlot);
			}
		}

		private static void SetUpDirectory()
		{
			keybindsPath = BaseDirectory + "Saves\\izuMod\\keybinds";
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
			if (!izuSettings.EnableHotkeys)
				return -1;

			List<ButtonBinding> buttonSwapBinds = izuSettings.ButtonsSwapKeybinds;
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

			TextMenu.Slider currentKeybindSlider = izuSettings.GetCurrentKeybindSlider();
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
			string[] files = Directory.GetFiles(keybindsPath);

			int keybindIndex = -1;
			for(int i = 0; i < files.Length; i++)
			{
				string shortPath = files[i].Replace(BaseDirectory + "Saves/", "").Replace(".celeste", ""); // izuMod\0_
				string fileName = shortPath.Replace("izuMod\\keybinds\\", "");

				string s = fileName.Remove(0, fileName.IndexOf('_') + 1);
				if (!s.StartsWith("keybind"))
					continue;

				keybindIndex++;

				Log("Loading: " + fileName);

				if (keybindIndex >= KeybindSettings.Count)
				{
					Log("Add keybind setting " + keybindIndex);
					KeybindSettings.Add(UserIO.Load<Settings>(shortPath, backup: false));
				}
				else
				{
					Log("Reload keybind setting " + keybindIndex);
					KeybindSettings[keybindIndex] = UserIO.Load<Settings>(shortPath, backup: false);
				}
			}

			if (keybindIndex == -1)
			{
				SaveKeybinds(0);
				return;
			}
		}

		private static void CopyFromKeybindSwapperToModSaves()
		{
			string[] keybindFiles = Directory.GetFiles(keybindsPath);

			foreach(string keybindFile in keybindFiles)
			{
				string shortPath = keybindFile.Replace(BaseDirectory + "Saves\\", "").Replace(".celeste", ""); // izuMod\0_
				string fileName = shortPath.Replace("izuMod\\keybinds\\", "");

				if (!fileName.StartsWith(izuSettings.CurrentKeybindSlot.ToString()))
					continue;

				string s = fileName.Remove(0, fileName.IndexOf('_') + 1);
				if (!s.StartsWith("modsettings"))
					continue;

				string t = File.ReadAllText(keybindFile);

				string p = BaseDirectory + "Saves/" + s + ".celeste";
				FileStream fileStream = File.Create(p);

				using (var sw = new StreamWriter(fileStream))
				{
					sw.Write(t);
				}
				fileStream.Dispose();

				//File.WriteAllText(p, t);

				Log("Applying: " + fileName);
			}
		}

		public static void SaveKeybinds(int keybindID)
		{
			FileStream fileStream = File.Open("Saves/settings.celeste", FileMode.Open);

			string celesteSettingsFile;
			using(var sr = new StreamReader(fileStream))
			{
				celesteSettingsFile = sr.ReadToEnd();
			}
			fileStream.Dispose();

			File.WriteAllText(keybindsPath + "/" + keybindID + "_keybind.celeste", celesteSettingsFile);

			SaveModKeybinds(keybindID);

			LoadKeybindFiles();
			CopyFromKeybindsToKeybindSwapper(keybindID);
		}

		public static void ApplyKeybinds(int keybindID)
		{
			Tooltip.Show("Applying keybind " + (keybindID + 1));

			CopyFromKeybindSwapperToKeybinds(keybindID);

			CopyFromKeybindSwapperToModSaves();
			LoadModKeybinds(keybindID);
		}

		private static void SaveModKeybinds(int keybindID)
		{
			foreach (EverestModule module in Everest.Modules)
			{
				if (!WhitelistContains(module.Metadata.Name))
					continue;

				try
				{
					object settings = module._Settings;
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
							if (prop.GetValue(settings) is ButtonBinding)
							{
								List<Binding> buttonBindingList = new();
								buttonBindingList.Add(((ButtonBinding)prop.GetValue(settings)).Button.Binding);
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
									bindingList.Add(buttonBinding.Binding);
								});
								settingsSave.buttonBindings.Add(prop.Name, bindingList);
							}
							continue;
						}
					}
					FileStream fileStream2 = File.Create(keybindsPath + "\\" + keybindID + "_modsettings-" + module.Metadata.Name + ".celeste");
					using StreamWriter writer = new StreamWriter(fileStream2);
					YamlHelper.Serializer.Serialize(writer, settingsSave, typeof(ModKeybindsSaveType));
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
				bool inWhitelist = WhitelistContains(module.Metadata.Name);
				Log(module.Metadata.Name + " in whitelist: " + inWhitelist);

				if (!inWhitelist)
				{
					continue;
				}

				try
				{
					FileStream fileStream = File.OpenRead(keybindsPath + "\\" + keybindID + "_modsettings-" + module.Metadata.Name + ".celeste");
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
			ChangeKeybindsFromAToB(KeybindSettings[keybindID], Settings.Instance, keybindID);
		}

		public static void CopyFromKeybindsToKeybindSwapper(int keybindID)
		{
			if (KeybindSettings.Count < keybindID)
				return;
			ChangeKeybindsFromAToB(Settings.Instance, KeybindSettings[keybindID], keybindID);
		}

		private static void ChangeKeybindsFromAToB(Settings a, Settings b, int keybindID)
		{
			izuSettings.CurrentKeybindSlot = keybindID;
			Log("Modifying Key Bind " + izuSettings.CurrentKeybindSlot);

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
			if (izuSettings.ButtonsSwapKeybinds == null)
				return;

			int swapKeybindsCount = izuSettings.ButtonsSwapKeybinds.Count;
			if (swapKeybindsCount > KeybindSettings.Count)
			{
				Log(izuSettings.ButtonsSwapKeybinds.Count);
				izuSettings.ButtonsSwapKeybinds.RemoveRange(KeybindSettings.Count, swapKeybindsCount - KeybindSettings.Count);
				Log(izuSettings.ButtonsSwapKeybinds.Count);
			}
			swapKeybindsCount = izuSettings.ButtonsSwapKeybinds.Count;
			for (int i = swapKeybindsCount; i < KeybindSettings.Count; i++)
			{
				izuSettings.ButtonsSwapKeybinds.Add(new ButtonBinding());
			}
		}

		private static bool WhitelistContains(string name)
		{
			foreach (string line in File.ReadAllLines(keybindsPath + "/whitelist.txt"))
			{
				if (name[0] == '#')
				{
					continue;
				}
				if (name.StartsWith(line))
				{
					return true;
				}
			}
			return false;
		}

		private class ModKeybindsSaveType
		{
			public Dictionary<string, List<Binding>> buttonBindings = new();
		}
	}
}
