using System;
using System.IO;
using System.Collections.Generic;

namespace Celeste.Mod.izumisQOL
{
	public class BlacklistModule : Global
	{
		private static string blacklistsPath;

		public static void Init()
		{
			SetUpDirectory();

			LoadBlacklistFiles();
		}

		private static void SetUpDirectory()
		{
			blacklistsPath = BaseDirectory + "Saves\\izuMod\\blacklists";
			Directory.CreateDirectory(blacklistsPath);
		}

		private static void LoadBlacklistFiles()
		{
			string[] files = Directory.GetFiles(blacklistsPath);

			if(files.Length <= 0)
			{
				CopyCelesteBlacklistToNewFile();
				return;
			}

			for (int i = 0; i < files.Length; i++)
			{
				string fileName = files[i].Replace(blacklistsPath + "\\", "").Replace(".txt", "");

				izuSettings.AddBlackListName(fileName);
				Log(fileName);
			}
		}

		public static void ChangeFileName(string origName, string newName)
		{
			File.Move(blacklistsPath + "/" + origName + ".txt", blacklistsPath + "/" + newName + ".txt");
		}

		public static void CopyCelesteBlacklistToNewFile()
		{
			int id = 0;
			while (File.Exists(blacklistsPath + "/blacklist " + id + ".txt"))
			{
				id++;
			}
			File.Copy(BaseDirectory + "Mods/blacklist.txt", blacklistsPath + "/blacklist " + id + ".txt");

			izuSettings.AddBlackListName("blacklist " + id);
		}

		public static void CopyCustomBlacklistToCeleste(string name)
		{
			string text = File.ReadAllText(blacklistsPath + "/" + name + ".txt");
			File.WriteAllText(BaseDirectory + "Mods/blacklist.txt", text);
		}
	}
}
