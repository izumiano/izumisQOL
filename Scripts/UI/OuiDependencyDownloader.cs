#nullable disable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Celeste;
using Celeste.Mod;
using Celeste.Mod.Helpers;
using Celeste.Mod.izumisQOL;
using Celeste.Mod.UI;
using IL.Celeste.Pico8;

public class OuiDependencyDownloader : OuiLoggedProgress
{
	public static List<EverestModuleMetadata> MissingDependencies = [ ];
	public static List<EverestModuleMetadata> ModulesToDisable    = [ ];

	private bool shouldAutoExit;

	private bool shouldRestart;

	private Everest.Updater.Entry everestVersionToInstall;

	private Task task;

	public override IEnumerator Enter(Oui from)
	{
		string.Join(", ", MissingDependencies.Select(module => $"{module.Name}, (v{module.Version})"))
		      .Log("Missing Dependencies");
		string.Join(", ", ModulesToDisable.Select(module => $"{module.Name}, (v{module.Version})"))
		      .Log("Modules To Disable");

		// Everest.Loader.AutoLoadNewMods = false;
		typeof(Everest.Loader).GetProperty("AutoLoadNewMods", BindingFlags.Static | BindingFlags.Public)!
		                      .GetSetMethod(true)!.Invoke(null, [ false, ]);
		Title = "IMPORTING MODS";
		task = new Task(() =>
		{
			if( MissingDependencies.Count > 0 || ModulesToDisable.Count > 0 )
				downloadAllDependencies();
			else
			{
				LogLine("Imported mods list is the same as your enabled mods.");

				Thread.Sleep(500);

				Progress       = 1;
				ProgressMax    = 1;
				shouldAutoExit = false;
				shouldRestart  = false;
				LogLine("\n" + Dialog.Clean("DEPENDENCYDOWNLOADER_PRESS_BACK_TO_GO_BACK"));
			}
		});
		Lines            = [ ];
		Progress         = 0;
		ProgressMax      = 0;
		shouldAutoExit   = true;
		shouldRestart    = false;
		task.Start();
		return base.Enter(from);
	}

	public override IEnumerator Leave(Oui next)
	{
		// Everest.Loader.AutoLoadNewMods = true;
		typeof(Everest.Loader).GetProperty("AutoLoadNewMods", BindingFlags.Static | BindingFlags.Public)!
		                      .GetSetMethod(true)!.Invoke(null, [ true, ]);
		return base.Leave(next);
	}

	private void downloadAllDependencies()
	{
		LogLine((ModSettings.WhitelistIsExclusive ? "Exlusively" : "Non-Exclusively") + " applying imported whitelist.");
		LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_DOWNLOADING_DATABASE"));
		Everest.Updater.Entry entry      = null;
		var                   dictionary = ModUpdaterHelper.DownloadModUpdateList();
		if( dictionary == null )
		{
			shouldAutoExit = false;
			shouldRestart  = false;
			LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_DOWNLOAD_DATABASE_FAILED"));
		}
		else
		{
			var dictionary2 = ModUpdaterHelper.DownloadModDependencyGraph();
			if( dictionary2 != null ) addTransitiveDependencies(dictionary2);
			LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_LOADING_INSTALLED_MODS"));
			Progress    = 0;
			ProgressMax = 100;
			var dictionary3 = WhitelistModule.LoadModuleYamls(progressCallback: progress =>
			{
				Lines[^1] =
					$"{Dialog.Clean("DEPENDENCYDOWNLOADER_LOADING_INSTALLED_MODS")} ({(int)(progress * 100f)}%)";
				Progress = (int)(progress * 100f);
			});
			ProgressMax = 0;
			var dictionary4 = new Dictionary<EverestModuleMetadata, string>();
			foreach( var (modPath, modules) in dictionary3 )
			{
				if( modules is null ) continue;

				foreach( var key in modules )
				{
					dictionary4[key] = modPath;
				}
			}

			Lines[^1] = Dialog.Clean("DEPENDENCYDOWNLOADER_LOADING_INSTALLED_MODS") + " " +
				Dialog.Clean("DEPENDENCYDOWNLOADER_DONE");
			Log( "Computing dependencies to download...");
			var dictionary5           = new Dictionary<string, ModUpdateInfo>();
			var dictionary6           = new Dictionary<string, ModUpdateInfo>();
			var dictionary7           = new Dictionary<string, EverestModuleMetadata>();
			var flag                  = false;
			var hashSet               = new HashSet<string>();
			var hashSet2              = new HashSet<string>();
			var modFileNamesToEnable  = new HashSet<string>();
			var modFileNamesToDisable = new HashSet<string>();
			var dictionary8           = new Dictionary<string, HashSet<Version>>();
			var dictionary9           = new Dictionary<string, string>();
			
			foreach( var dependency in MissingDependencies )
			{
				if( typeof(Everest.Loader).GetField("Delayed", BindingFlags.Static | BindingFlags.Public)
				                          ?.GetValue(null) is List<Tuple<EverestModuleMetadata, Action>> delayed &&
					delayed.Any(delayedMod => dependency.Name == delayedMod.Item1.Name) )
				{
					Log( dependency.Name + " is installed but load is delayed, skipping");
					continue;
				}

				if( dependency.Name == "Everest" || dependency.Name == "EverestCore" )
				{
					Log( "Everest should be updated");
					shouldAutoExit = false;
					if( dependency.Version.Major != 1 || dependency.Version.Build > 0 || dependency.Version.Revision > 0 )
						flag = true;
					else if( !flag && (entry == null || entry.Build < dependency.Version.Minor) )
					{
						entry = findEverestVersionToInstall(dependency.Version.Minor);
						if( entry == null ) flag = true;
					}

					continue;
				}

				if( tryUnblacklist(dependency, dictionary4, modFileNamesToEnable) )
				{
					Log(
						dependency.Name + " is blacklisted, and should be unblacklisted instead");
					continue;
				}

				if( !dictionary.TryGetValue(dependency.Name, out var value) )
				{
					Log( dependency.Name + " was not found in the database");
					hashSet.Add(dependency.Name);
					shouldAutoExit = false;
					continue;
				}

				if( value.xxHash.Count > 1 )
				{
					Log(
						dependency.Name + " has multiple versions and cannot be installed automatically");
					hashSet2.Add(dependency.Name);
					shouldAutoExit = false;
					continue;
				}

				if( !isVersionCompatible(dependency.Version, dictionary[dependency.Name].Version) )
				{
					Log(
						$"{dependency.Name} has a version in database ({dictionary[dependency.Name].Version}) that would not satisfy dependency ({dependency.Version})");
					HashSet<Version> value2;
					var hashSet4 = dictionary8.TryGetValue(dependency.Name, out value2) ? value2 : new HashSet<Version>();
					hashSet4.Add(dependency.Version);
					dictionary8[dependency.Name] = hashSet4;
					dictionary9[dependency.Name] = dictionary[dependency.Name].Version;
					shouldAutoExit               = false;
					continue;
				}

				EverestModuleMetadata everestModuleMetadata = null;
				foreach( var module in Everest.Modules )
				{
					if( module.Metadata.PathArchive != null && module.Metadata.Name == dependency.Name )
					{
						everestModuleMetadata = module.Metadata;
						break;
					}
				}

				if( everestModuleMetadata != null )
				{
					Log( dependency.Name + " is already installed and will be updated");
					dictionary6[dependency.Name] = dictionary[dependency.Name];
					dictionary7[dependency.Name] = everestModuleMetadata;
				}
				else
				{
					Log( dependency.Name + " will be installed");
					dictionary5[dependency.Name] = dictionary[dependency.Name];
				}
			}

			foreach( var moduleToDisable in ModulesToDisable )
			{
				if( tryBlacklist(moduleToDisable, dictionary4, modFileNamesToDisable, modFileNamesToEnable) )
				{
					Log(
						moduleToDisable.Name + " is not blacklisted, and should be blacklisted instead");
				}
			}
			
			foreach( var value3 in dictionary5.Values )
			{
				downloadDependency(value3, null);
			}

			foreach( var value4 in dictionary6.Values )
			{
				downloadDependency(value4, dictionary7[value4.Name]);
			}

			if( modFileNamesToEnable.Count > 0 )
			{
				shouldAutoExit = true;
				shouldRestart  = true;
				unblacklistMods(modFileNamesToEnable);
			}
			
			if( modFileNamesToDisable.Count > 0 )
			{
				shouldAutoExit = true;
				shouldRestart  = true;
				blacklistMods(modFileNamesToDisable);
			}

			if( flag ) LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_MUST_UPDATE_EVEREST"));
			foreach( var item2 in hashSet )
			{
				if( item2 == "Celeste" )
					LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_UPDATE_CELESTE"));
				else
					LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_MOD_NOT_FOUND"), item2));
			}

			foreach( var item3 in hashSet2 )
			{
				LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_MOD_NOT_AUTO_INSTALLABLE"), item3));
			}

			foreach( var key2 in dictionary8.Keys )
			{
				LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_MOD_WRONG_VERSION"), key2,
					string.Join(", ", dictionary8[key2]), dictionary9[key2]));
			}
		}

		Progress    = 1;
		ProgressMax = 1;
		if( shouldAutoExit )
		{
			if( shouldRestart )
			{
				LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_RESTARTING"));
				for( var num = 3; num > 0; num-- )
				{
					Lines[Lines.Count - 1] = string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_RESTARTING_IN"), num);
					Thread.Sleep(1000);
				}

				Lines[Lines.Count - 1] = Dialog.Clean("DEPENDENCYDOWNLOADER_RESTARTING");
				Everest.QuickFullRestart();
			}
			else
				Exit();
		}
		else if( entry != null )
		{
			LogLine("\n" + string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_EVEREST_UPDATE"), entry.Build));
			everestVersionToInstall = entry;
		}
		else if( shouldRestart )
			LogLine("\n" + Dialog.Clean("DEPENDENCYDOWNLOADER_PRESS_BACK_TO_RESTART"));
		else
			LogLine("\n" + Dialog.Clean("DEPENDENCYDOWNLOADER_PRESS_BACK_TO_GO_BACK"));
	}

	private static void addTransitiveDependencies(Dictionary<string, EverestModuleMetadata> modDependencyGraph)
	{
		var list = new List<EverestModuleMetadata>();
		do
		{
			Log( "Checking for transitive dependencies...");
			list.Clear();
			foreach( var missingDependency in MissingDependencies )
			{
				if( !modDependencyGraph.TryGetValue(missingDependency.Name, out var value) )
				{
					Log( missingDependency.Name + " was not found in the graph");
					continue;
				}

				foreach( var dependency in value.Dependencies )
				{
					if( Everest.Loader.DependencyLoaded(dependency) )
					{
						Log( dependency.Name + " is loaded");
						continue;
					}

					if( MissingDependencies.Any(dep => dep.Name == dependency.Name) ||
						list.Any(dep => dep.Name == dependency.Name) )
					{
						Log( dependency.Name + " is already missing");
						continue;
					}

					Log( dependency.Name + " was added to the missing dependencies!");
					list.Add(dependency);
				}
			}

			MissingDependencies.AddRange(list);
		} while( list.Count > 0 );
	}

	private static bool tryUnblacklist(
		EverestModuleMetadata dependency, Dictionary<EverestModuleMetadata, string> allModsInformation,
		HashSet<string>       modsToUnblacklist
	)
	{
		var keyValuePair = default(KeyValuePair<EverestModuleMetadata, string>);
		foreach( var item in allModsInformation )
		{
			if( dependency.Name == item.Key.Name &&
				(keyValuePair.Key == null || keyValuePair.Key.Version < item.Key.Version) ) keyValuePair = item;
		}

		if( keyValuePair.Key == null ) return false;
		if( modsToUnblacklist.Contains(keyValuePair.Value) ) return true;

		if( dependency.Version > keyValuePair.Key.Version )
		{
			Log($"{dependency.Name} needs updating");
			return false;
		}
		
		modsToUnblacklist.Add(keyValuePair.Value);
		foreach( var dependency2 in keyValuePair.Key.Dependencies )
		{
			tryUnblacklist(dependency2, allModsInformation, modsToUnblacklist);
		}

		return true;
	}

	private static bool tryBlacklist(
		EverestModuleMetadata dependency, Dictionary<EverestModuleMetadata, string> allModsInformation,
		HashSet<string>       modsToBlacklist, HashSet<string> modsToUnblacklist
	)
	{
		KeyValuePair<EverestModuleMetadata, string> keyValuePair = default;
		foreach( var item in allModsInformation )
		{
			if( dependency.Name == item.Key.Name &&
				(keyValuePair.Key == null || keyValuePair.Key.Version < item.Key.Version) )

				keyValuePair = item;
		}

		if( keyValuePair.Key == null || Everest.Loader.Blacklist.Contains(keyValuePair.Value) || modsToUnblacklist.Contains(keyValuePair.Value) ) return false;
		if( modsToBlacklist.Contains(keyValuePair.Value) ) return true;
		modsToBlacklist.Add(keyValuePair.Value);

		return true;
	}

	private bool unblacklistMods(HashSet<string> modFilenamesToUnblacklist)
	{
		try
		{
			var       list         = File.ReadAllLines(Everest.Loader.PathBlacklist).Select(l => l.Trim()).ToList();
			var       hashSet      = new HashSet<string>(modFilenamesToUnblacklist);
			using var streamWriter = File.CreateText(Everest.Loader.PathBlacklist);
			foreach( var item in list )
			{
				if( modFilenamesToUnblacklist.Contains(item) )
				{
					streamWriter.WriteLine("# " + item);
					hashSet.Remove(item);
					LogLine($"Whitelisted {item}");
					Log( "Whitelisted " + item);
				}
				else
					streamWriter.WriteLine(item);
			}
			foreach( var modFileToBlacklist in hashSet )
			{
				streamWriter.WriteLine("# " + modFileToBlacklist);
				LogLine($"Whitelisted {modFileToBlacklist}");
				Log( "Whitelisted " + modFileToBlacklist);
			}


			return true;
		}
		catch( Exception e )
		{
			LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_UNBLACKLIST_FAILED"));
			Logger.LogDetailed(e);
			return false;
		}
	}

	private bool blacklistMods(HashSet<string> modFilenamesToBlacklist)
	{
		try
		{
			var       originalBlacklistLines = File.ReadAllLines(Everest.Loader.PathBlacklist).Select(l => l.Trim());
			var       hashSet                = new HashSet<string>(modFilenamesToBlacklist);
			using var streamWriter           = File.CreateText(Everest.Loader.PathBlacklist);
			foreach( var origBlacklistLine in originalBlacklistLines )
			{
				if( modFilenamesToBlacklist.Contains(origBlacklistLine) )
				{
					streamWriter.WriteLine(origBlacklistLine);
					hashSet.Remove(origBlacklistLine);
					LogLine($"Blacklisted {origBlacklistLine}");
					Log( "Blacklisted " + origBlacklistLine);
				}
				else
					streamWriter.WriteLine(origBlacklistLine);
			}

			foreach( var modFileToBlacklist in hashSet )
			{
				streamWriter.WriteLine(modFileToBlacklist);
				LogLine($"Blacklisted {modFileToBlacklist}");
				Log( "Blacklisted " + modFileToBlacklist);
			}

			return true;
		}
		catch( Exception e )
		{
			LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_UNBLACKLIST_FAILED"));
			Logger.LogDetailed(e);
			return false;
		}
	}

	private bool isVersionCompatible(Version requiredVersion, string databaseVersionString)
	{
		Version installedVersion;
		try
		{
			installedVersion = new Version(databaseVersionString);
		}
		catch( Exception e )
		{
			Log("Could not parse version number: " + databaseVersionString, LogLevel.Warn);
			Logger.LogDetailed(e);
			return false;
		}

		return Everest.Loader.VersionSatisfiesDependency(requiredVersion, installedVersion);
	}

	private Everest.Updater.Entry findEverestVersionToInstall(int requestedBuild)
	{
		var                   updatePriority = Everest.Updater.UpdatePriority.None;
		Everest.Updater.Entry entry          = null;
		foreach( var source in Everest.Updater.Sources )
		{
			if( source?.Entries == null || (source?.UpdatePriority ?? Everest.Updater.UpdatePriority.Low) ==
				Everest.Updater.UpdatePriority.None ) continue;
			foreach( var entry2 in source.Entries )
			{
				if( entry2.Build >= requestedBuild && source.UpdatePriority >= updatePriority &&
					(source.UpdatePriority != updatePriority || !(entry2.Build < entry?.Build)) )
				{
					updatePriority = source.UpdatePriority;
					entry          = entry2;
				}
			}
		}

		return entry;
	}

	private void downloadDependency(ModUpdateInfo mod, EverestModuleMetadata installedVersion)
	{
		var text = Path.Combine(Everest.PathTmp, "dependency-download.zip");
		try
		{
			var progressCallback = delegate(int position, long length, int speed)
			{
				if( length > 0 )
				{
					Lines[Lines.Count - 1] = $"{(int)Math.Floor(100.0 * (position / (double)length))}% @ {speed} KiB/s";
					Progress               = position;
					ProgressMax            = (int)length;
				}
				else
				{
					Lines[Lines.Count - 1] = $"{(int)Math.Floor(position / 1000.0)}KiB @ {speed} KiB/s";
					ProgressMax            = 0;
				}

				return true;
			};
			Exception ex = null;
			foreach( string allMirrorUrl in ModUpdaterHelper.GetAllMirrorUrls(mod.URL) )
			{
				try
				{
					ex = null;
					LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_DOWNLOADING"), mod.Name, allMirrorUrl));
					LogLine("", false);
					Everest.Updater.DownloadFileWithProgress(allMirrorUrl, text, progressCallback);
					ProgressMax            = 0;
					Lines[Lines.Count - 1] = Dialog.Clean("DEPENDENCYDOWNLOADER_DOWNLOAD_FINISHED");
					LogLine(Dialog.Clean("DEPENDENCYDOWNLOADER_VERIFYING_CHECKSUM"));
					ModUpdaterHelper.VerifyChecksum(mod, text);
				}
				catch( Exception ex2 ) when( (ex2 is WebException || ex2 is TimeoutException || ex2 is IOException ? 1 : 0) !=
					0 )
				{
					ex = ex2;
					Log("Download failed, trying another mirror", LogLevel.Warn);
					Logger.LogDetailed(ex2);
					Lines[Lines.Count - 1] = string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_DOWNLOADING_MIRROR"), "");
					continue;
				}

				break;
			}

			if( ex != null )
			{
				ModUpdaterHelper.TryDelete(text);
				throw ex;
			}

			if( installedVersion != null )
			{
				shouldRestart = true;
				LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_UPDATING"), mod.Name, installedVersion.Version,
					mod.Version, installedVersion.PathArchive));
				ModUpdaterHelper.InstallModUpdate(mod, installedVersion, text);
				return;
			}

			var text3 = Path.Combine(Everest.Loader.PathMods, mod.Name + ".zip");
			LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_INSTALLING"), mod.Name, mod.Version, text3));
			if( File.Exists(text3) ) File.Delete(text3);
			File.Move(text, text3);
			shouldRestart = true;
		}
		catch( Exception e2 )
		{
			LogLine(string.Format(Dialog.Get("DEPENDENCYDOWNLOADER_INSTALL_FAILED"), mod.Name));
			Logger.LogDetailed(e2);
			shouldAutoExit = false;
			if( File.Exists(text) )
			{
				try
				{
					Log( "Deleting temp file " + text);
					File.Delete(text);
				}
				catch( Exception )
				{
					Log("Removing " + text + " failed", LogLevel.Warn);
				}
			}
		}
	}

	public override void Update()
	{
		if( everestVersionToInstall != null )
		{
			if( Input.MenuConfirm.Pressed && Focused )
				Everest.Updater.Update(OuiModOptions.Instance.Overworld.Goto<OuiLoggedProgress>(), everestVersionToInstall);
		}
		else if( task != null && !shouldAutoExit && (task.IsCompleted || task.IsCanceled || task.IsFaulted) &&
			Input.MenuCancel.Pressed && Focused )
		{
			if( shouldRestart )
				Everest.QuickFullRestart();
			else
				Exit();
		}

		base.Update();
	}

	public void Exit()
	{
		task = null;
		Lines.Clear();
		MainThreadHelper.Schedule(delegate { Overworld.GetUI<OuiMainMenu>()?.NeedsRebuild(); });
		Audio.Play("event:/ui/main/button_back");
		Overworld.Goto<OuiModOptions>();
	}
}