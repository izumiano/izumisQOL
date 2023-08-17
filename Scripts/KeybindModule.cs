using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Celeste;
using Monocle;

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
			}

			if (izuSettings.ButtonLoadKeybind.Pressed)
			{
				ApplyKeybinds(izuSettings.CurrentKeybindSlot);
			}
		}

		private static void SetUpDirectory()
		{
			keybindsPath = BaseDirectory + "Saves/izuMod/keybinds";
			Directory.CreateDirectory(keybindsPath);

			if (!File.Exists(keybindsPath + "/blacklist.txt"))
			{
				File.WriteAllText(keybindsPath + "/blacklist.txt", "# Mods written here will not have their settings copied by izumi's keybind swapper\n# Lines starting with # are ignored\nizumisQOL");
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
				CopyCelesteSettingsToKeybindIDFile(0);
				return;
			}
		}

		private static void CopyFromKeybindSwapperToModSaves()
		{
			string[] keybindFiles = Directory.GetFiles(keybindsPath);

			foreach(string keybindFile in keybindFiles)
			{
				string shortPath = keybindFile.Replace(BaseDirectory + "Saves/", "").Replace(".celeste", ""); // izuMod\0_
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

		public static void CopyCelesteSettingsToKeybindIDFile(int id)
		{
			FileStream fileStream = File.Open("Saves/settings.celeste", FileMode.Open);

			string celesteSettingsFile;
			using(var sr = new StreamReader(fileStream))
			{
				celesteSettingsFile = sr.ReadToEnd();
			}
			fileStream.Dispose();

			//string celesteSettingsFile = File.ReadAllText("Saves/settings.celeste");
			File.WriteAllText(keybindsPath + "/" + id + "_keybind.celeste", celesteSettingsFile);

			//string[] files = Directory.GetFiles(BaseDirectory + "Saves");
			//foreach (string file in files)
			//{
			//	string name = file.Replace(BaseDirectory + "Saves\\", "");

			//	if(!IsValidSettingsFile(name))
			//		continue;

			//	Log(keybindsPath + "/" + id + "_" + name);
			//	string s = File.ReadAllText(file);
			//	File.WriteAllText(keybindsPath + "/" + id + "_" + name, s);
			//}

			LoadKeybindFiles();
			CopyFromKeybindsToKeybindSwapper(id);
		}

		private static bool IsValidSettingsFile(string shortPath)
		{
			if (!shortPath.StartsWith("modsettings-"))
			{
				return false;
			}

			string name = shortPath.Replace("modsettings-", "");
			foreach (string line in File.ReadAllLines(keybindsPath + "/blacklist.txt"))
			{
				if(name[0] == '#')
				{
					continue;
				}
				if (name.StartsWith(line))
				{
					return false;
				}
			}
			return true;
		}

		public static void ApplyKeybinds(int keybindID)
		{
			Log("Applying keybind " + keybindID);

			CopyFromKeybindSwapperToKeybinds(keybindID);

			//CopyFromKeybindSwapperToModSaves();
			//Log("Reloading all mod settings");
			//foreach (EverestModule module in Everest.Modules)
			//{
			//	module.LoadSettings();
			//	//Log(module.)
			//}
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
			b.MenuLeft.Add(a.MenuLeft.Keyboard.ToArray());
			b.MenuRight.Add(a.MenuRight.Keyboard.ToArray());
			b.MenuUp.Add(a.MenuUp.Keyboard.ToArray());
			b.MenuDown.Add(a.MenuDown.Keyboard.ToArray());
			b.Confirm.Add(a.Confirm.Keyboard.ToArray());
			b.Cancel.Add(a.Cancel.Keyboard.ToArray());
			b.Journal.Add(a.Journal.Keyboard.ToArray());
			b.Pause.Add(a.Pause.Keyboard.ToArray());
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
	}
}
