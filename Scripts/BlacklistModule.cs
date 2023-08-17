using System;
using System.IO;

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
			blacklistsPath = BaseDirectory + "Saves/izuMod/blacklists";
			Directory.CreateDirectory(blacklistsPath);
		}

		private static void LoadBlacklistFiles()
		{

		}
	}
}
