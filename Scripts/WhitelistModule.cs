using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
		var files = Directory.GetFiles(whitelistsPath).SanitizeFilePath();

		if( files.Length <= 0 )
		{
			AddWhitelist();
			return;
		}

		foreach( var file in files )
		{
			var fileName = file.Replace(whitelistsPath + "/", "").Replace(".txt", "");

			ModSettings.AddWhitelistName(fileName);
			Log(fileName);
		}
	}

	public static bool RenameFile(string? origName, string newName)
	{
		if( origName is null ) return false;

		try
		{
			var newPath = whitelistsPath + "/" + newName + ".txt";
			if( File.Exists(newPath) )
			{
				Log(newPath + " already exists", LogLevel.Info);
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

		var whitelist       = GetCurrentEverestWhitelist();
		var whitelistString = "";
		foreach( var entry in whitelist )
		{
			whitelistString += entry + "\n";
		}

		File.WriteAllText(whitelistsPath + "/whitelist_" + id + ".txt", whitelistString);
		ModSettings.AddWhitelistName("whitelist_" + id);
	}

	public static void RemoveWhitelist(string? fileName)
	{
		if( fileName is null ) return;

		var path = whitelistsPath + "/" + fileName + ".txt";
		Log("Deleting: " + path);
		File.Delete(path);
	}

	public static void SaveCurrentWhitelist(string? fileName, int index)
	{
		if( fileName is null ) return;

		try
		{
			if( !File.Exists(whitelistsPath + "/" + fileName + ".txt") )
			{
				Tooltip.Show(fileName + " " + "MODOPTIONS_IZUMISQOL_WHITELISTERROR_DOESNOTEXIST".AsDialog());
				Log(whitelistsPath + "/" + fileName + ".txt" + " does not exist", LogLevel.Info);
				return;
			}

			var whitelist       = GetCurrentEverestWhitelist();
			var whitelistString = "";
			foreach( var entry in whitelist )
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

	private static string[]? LoadWhitelist(string? name)
	{
		if( !File.Exists(whitelistsPath + "/" + name + ".txt") )
		{
			Log(whitelistsPath + "/" + name + ".txt" + " does not exist", LogLevel.Info);
			Tooltip.Show(name + " " + "MODOPTIONS_IZUMISQOL_WHITELISTERROR_DOESNOTEXIST".AsDialog());
			return null;
		}

		return File.ReadAllLines(whitelistsPath + "/" + name + ".txt");
	}

	public static bool WriteToEverestBlacklist(string? name)
	{
		if( name is null ) return false;

		try
		{
			var whitelistLines = LoadWhitelist(name);
			if( whitelistLines is null ) return false;

			var everestBlacklistLines = File.ReadAllLines(Everest.Loader.PathBlacklist);
			var everestBlacklistText  = "";

			if( ModSettings.WhitelistIsExclusive )
			{
				var modFiles   = Directory.GetFiles(Everest.Loader.PathMods).SanitizeFilePath();
				var modFolders = Directory.GetDirectories(Everest.Loader.PathMods).SanitizeFilePath();
				foreach( var modPath in modFiles )
				{
					if( !modPath.EndsWith(".zip") )
						continue;

					AddBlacklistText(modPath);
				}

				foreach( var modPath in modFolders )
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
				foreach( var whitelistLine in whitelistLines )
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
		              .Select(module =>
			              module.Metadata.Name + (string.IsNullOrEmpty(module.Metadata.PathArchive) ? "" : ".zip"))
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
		var name = moduleName.Replace(".zip", "");
		return name is "Everest" or "Celeste" or "DialogCutscene" or "UpdateChecker" or "InfiniteSaves" or "DebugRebind"
			or "RebindPeriod" or "Cache";
	}

	private static EverestModuleMetadata[]? LoadZip(string? archive)
	{
		if( archive is null ) return null;

		try
		{
			using var zipArchive = ZipFile.OpenRead(archive);
			foreach( var entry in zipArchive.Entries )
			{
				if( entry.FullName == "metadata.yaml" )
				{
					using( var stream = entry.ExtractStream() )
					{
						using var input                 = new StreamReader(stream);
						var       everestModuleMetadata = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata>(input);
						everestModuleMetadata.PathArchive = archive;
						everestModuleMetadata.PostParse();
						return new EverestModuleMetadata[1] { everestModuleMetadata, };
					}
				}

				if( !(entry.FullName == "multimetadata.yaml") && !(entry.FullName == "everest.yaml") &&
					!(entry.FullName == "everest.yml") ) continue;
				using var stream2      = entry.ExtractStream();
				using var streamReader = new StreamReader(stream2);
				if( !streamReader.EndOfStream )
				{
					var array  = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata[]>(streamReader);
					var array2 = array;
					foreach( var obj in array2 )
					{
						obj.PathArchive = archive;
						obj.PostParse();
					}

					return array;
				}
			}
		}
		catch( Exception value )
		{
			Logger.Warn("loader", $"Failed loading everest.yaml in archive {archive}: {value}");
		}

		return null;
	}

	private static EverestModuleMetadata[]? LoadDir(string? dir)
	{
		if( dir is null ) return null;

		try
		{
			var path = Path.Combine(dir, "metadata.yaml");
			if( File.Exists(path) )
			{
				using var input                 = new StreamReader(path);
				var       everestModuleMetadata = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata>(input);
				everestModuleMetadata.PathDirectory = dir;
				everestModuleMetadata.PostParse();
				return new EverestModuleMetadata[1] { everestModuleMetadata, };
			}

			path = Path.Combine(dir, "multimetadata.yaml");
			if( !File.Exists(path) ) path = Path.Combine(dir, "everest.yaml");
			if( !File.Exists(path) ) path = Path.Combine(dir, "everest.yml");
			if( File.Exists(path) )
			{
				using var streamReader = new StreamReader(path);
				if( !streamReader.EndOfStream )
				{
					var array  = YamlHelper.Deserializer.Deserialize<EverestModuleMetadata[]>(streamReader);
					var array2 = array;
					foreach( var obj in array2 )
					{
						obj.PathDirectory = dir;
						obj.PostParse();
					}

					return array;
				}
			}
		}
		catch( Exception value )
		{
			Logger.Warn("loader", $"Failed loading everest.yaml in directory {dir}: {value}");
		}

		return null;
	}

	private static Dictionary<string, IEnumerable<EverestModuleMetadata>> LoadYamlsFromWhitelist(string[] whitelist)
	{
		var dictionary             = new Dictionary<string, IEnumerable<EverestModuleMetadata>>();
		var source                 = Everest.Modules.Select(module => module.Metadata);
		var everestModuleMetadatas = source as EverestModuleMetadata[] ?? source.ToArray();
		foreach( var fileName in whitelist )
		{
			var filePath = Path.Combine(Everest.Loader.PathMods, fileName);
			if( fileName.EndsWith(".zip") )
			{
				var array = everestModuleMetadatas.Where(meta => meta.PathArchive == filePath).ToArray();
				if( array.Length == 0 ) array = LoadZip(filePath);
				if( array != null ) dictionary[fileName] = array;
			}
			else
			{
				var array = everestModuleMetadatas.Where(meta => meta.PathDirectory == filePath).ToArray();
				if( array.Length == 0 ) array = LoadDir(filePath);
				if( array != null ) dictionary[fileName] = array;
			}
		}

		return dictionary;
	}

	private static string ModuleCollectionToExportString(IEnumerable<EverestModuleMetadata> moduleCollection)
	{
		var modules =
			moduleCollection
				.Where(module => module.Name != "Celeste")
				.Select(module =>
				{
					var name    = module.Name;
					var version = $"{module.Version.Major}.{module.Version.Minor}.{module.Version.Build}";

					return new ModuleExportInfo(name, version);
				});

		return YamlHelper.Serializer.Serialize(modules);
	}

	private static string ModuleCollectionToExportString(IEnumerable<EverestModule> moduleCollection)
	{
		return ModuleCollectionToExportString(moduleCollection.Select(module => module.Metadata));
	}

	public static string GetEnabledModsExport()
	{
		return ModuleCollectionToExportString(Everest.Modules);
	}

	public static string GetCurrentWhitelistModsExport(string? whitelistName)
	{
		var currentWhitelist = LoadWhitelist(whitelistName);
		if( currentWhitelist is null ) return "";

		var modules = LoadYamlsFromWhitelist(currentWhitelist)
		              .Select(module => module.Value)
		              .Aggregate(new List<EverestModuleMetadata>(),
			              (array, module) => array.Concat([ ..module, ]).ToList());

		return ModuleCollectionToExportString(modules);
	}

	private record ModuleExportInfo(string Name, string Version);
}