using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Celeste.Mod.izumisQOL;

public static class WhitelistModule
{
	private static readonly string whitelistsPath = UserIO.SavePath.SanitizeFilePath() + "/izumisQOL/whitelists";

	public static void Init()
	{
		ModSettings.ResetWhitelist();
		if( !SetUpDirectory() )
		{
			Log("Failed setting up whitelist directory", LogLevel.Error);
			return;
		}

		LoadWhitelistFiles();
	}

	private static bool SetUpDirectory()
	{
		Directory.CreateDirectory(whitelistsPath);
		return Directory.Exists(whitelistsPath);
	}

	private static void LoadWhitelistFiles()
	{
		string[] files = Directory.GetFiles(whitelistsPath).SanitizeFilePath();

		if( files.Length <= 0 )
		{
			AddWhitelist();
			return;
		}

		foreach( string file in files )
		{
			string fileName = file.Replace(whitelistsPath + "/", "").Replace(".txt", "");

			ModSettings.AddWhitelistName(fileName);
			Log(fileName);
		}
	}

	public static bool RenameFile(string? origName, string newName)
	{
		if( origName is null )
		{
			return false;
		}
		
		try
		{
			string newPath = whitelistsPath + "/" + newName + ".txt";
			if( File.Exists(newPath) )
			{
				Log(newPath          + " already exists", LogLevel.Info);
				Tooltip.Show(newName + " " + "MODOPTIONS_IZUMISQOL_WHITELIST_EXISTS".AsDialog());
				return false;
			}

			File.Move(whitelistsPath + "/" + origName + ".txt", newPath);
			Tooltip.Show("MODOPTIONS_IZUMISQOL_IMPORTED1".AsDialog() + " " + newName + " " +
			             "MODOPTIONS_IZUMISQOL_IMPORTED2".AsDialog());
			return true;
		}
		catch( Exception ex )
		{
			Tooltip.Show("MODOPTIONS_IZUMISQOL_ERROR_INVALIDCLIPBOARD".AsDialog());
			Log(ex, LogLevel.Warn);
			return false;
		}
	}

	public static void AddWhitelist()
	{
		var id = 0;
		while( File.Exists(whitelistsPath + "/whitelist_" + id + ".txt") )
		{
			id++;
		}

		List<string> whitelist       = GetCurrentEverestWhitelist();
		var          whitelistString = "";
		foreach( string entry in whitelist )
		{
			whitelistString += entry + "\n";
		}

		File.WriteAllText(whitelistsPath          + "/whitelist_" + id + ".txt", whitelistString);
		ModSettings.AddWhitelistName("whitelist_" + id);
	}

	public static void RemoveWhitelist(string? fileName)
	{
		if( fileName is null )
		{
			return;
		}
		
		string path = whitelistsPath + "/" + fileName + ".txt";
		Log("Deleting: "             + path);
		File.Delete(path);
	}

	public static void SaveCurrentWhitelist(string? fileName, int index)
	{
		if( fileName is null )
		{
			return;
		}
		
		try
		{
			if( !File.Exists(whitelistsPath + "/" + fileName + ".txt") )
			{
				Tooltip.Show(fileName + " " + "MODOPTIONS_IZUMISQOL_WHITELISTERROR_DOESNOTEXIST".AsDialog());
				Log(whitelistsPath    + "/" + fileName + ".txt" + " does not exist", LogLevel.Info);
				return;
			}

			List<string> whitelist       = GetCurrentEverestWhitelist();
			var          whitelistString = "";
			foreach( string entry in whitelist )
			{
				whitelistString += entry + "\n";
			}

			File.WriteAllText(whitelistsPath + "/" + fileName + ".txt", whitelistString);
			ModSettings.ChangeWhitelistName(index, fileName);

			Tooltip.Show("MODOPTIONS_IZUMISQOL_WHITELIST_SAVEDTO".AsDialog() + " " + fileName);
		}
		catch( Exception ex )
		{
			Log(ex, LogLevel.Warn);
		}
	}

	public static bool WriteToEverestBlacklist(string? name)
	{
		if( name is null )
		{
			return false;
		}
		
		try
		{
			if( !File.Exists(whitelistsPath + "/" + name + ".txt") )
			{
				Log(whitelistsPath + "/" + name + ".txt" + " does not exist", LogLevel.Info);
				Tooltip.Show(name  + " " + "MODOPTIONS_IZUMISQOL_WHITELISTERROR_DOESNOTEXIST".AsDialog());
				return false;
			}

			string[] whitelistLines        = File.ReadAllLines(whitelistsPath + "/" + name + ".txt");
			string[] everestBlacklistLines = File.ReadAllLines(Everest.Loader.PathBlacklist);
			var      everestBlacklistText  = "";

			if( ModSettings.WhitelistIsExclusive )
			{
				string[] modFiles   = Directory.GetFiles(Everest.Loader.PathMods).SanitizeFilePath();
				string[] modFolders = Directory.GetDirectories(Everest.Loader.PathMods).SanitizeFilePath();
				foreach( string modPath in modFiles )
				{
					if( !modPath.EndsWith(".zip") )
						continue;

					AddBlacklistText(modPath);
				}

				foreach( string modPath in modFolders )
				{
					AddBlacklistText(modPath);
				}

				void AddBlacklistText(string modPath)
				{
					if( string.IsNullOrEmpty(modPath) )
						return;
					modPath = modPath.Replace(Everest.Loader.PathMods.SanitizeFilePath() + "/", "");
					if( IsEssentialModule(modPath) )
						return;

					everestBlacklistText += GetBlacklistLineToWrite(modPath);
				}
			}
			else
			{
				everestBlacklistText = everestBlacklistLines
				                       .Where(blacklistline => !string.IsNullOrEmpty(blacklistline))
				                       .Aggregate(everestBlacklistText,
					                       (current, blacklistline) => current + GetBlacklistLineToWrite(blacklistline));
			}

			File.WriteAllText(Everest.Loader.PathBlacklist, everestBlacklistText);

			Tooltip.Show((ModSettings.WhitelistIsExclusive ? "Exclusively " : "Non-exclusively ") + "applied " + name +
			             " to blacklist");

			return true;

			string GetBlacklistLineToWrite(string blacklistLine)
			{
				foreach( string whitelistLine in whitelistLines )
				{
					if( string.IsNullOrEmpty(whitelistLine) || whitelistLine[0] == '#' )
						continue;

					if( blacklistLine.StartsWith(whitelistLine) )
					{
						blacklistLine = "# " + whitelistLine;
						break;
					}
				}

				return blacklistLine + "\n";
			}
		}
		catch( Exception ex )
		{
			Log(ex, LogLevel.Warn);
			Tooltip.Show("MODOPTIONS_IZUMISQOL_WHITELISTERROR_FAILEDWRITE".AsDialog());
			return false;
		}
	}

	private static List<string> GetCurrentEverestWhitelist()
	{
		return Everest.Modules
		       .Where(module => !IsEssentialModule(module.Metadata.Name))
		       .Select(module => module.Metadata.Name + (string.IsNullOrEmpty(module.Metadata.PathArchive) ? "" : ".zip"))
		       .ToList();

		// foreach( EverestModule module in Everest.Modules )
		// {
		// 	string entry = module.Metadata.Name;
		// 	if( !IsEssentialModule(entry) )
		// 	{
		// 		whitelist.Add(entry + (string.IsNullOrEmpty(module.Metadata.PathArchive) ? "" : ".zip"));
		// 	}
		// }
	}

	private static bool IsEssentialModule(string moduleName)
	{
		string name = moduleName.Replace(".zip", "");
		return name is "Everest" or "Celeste" or "DialogCutscene" or "UpdateChecker" or "InfiniteSaves" or "DebugRebind"
			or "RebindPeriod" or "Cache";
	}
}