using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using MonoMod.Utils;

namespace Celeste.Mod.izumisQOL.ModIntegration;

public static class CollabUtils2Integration
{
	public static bool Loaded;

	private static Type? progressPageType;

	private static MethodInfo? isHeartSide_MethodInfo;
	private static FieldInfo? collabInGameForcedArea_FieldInfo;

	public static void Load()
	{
		if (!IntegrationUtils.TryGetModule(new EverestModuleMetadata
		{
			Name = "CollabUtils2",
			VersionString = "1.12.5",
		}, out var collabUtils2EverestModule))
		{
			return;
		}

		var collabUtils2Module = collabUtils2EverestModule.GetType().Module;

		progressPageType = collabUtils2Module.GetType("Celeste.Mod.CollabUtils2.UI.OuiJournalCollabProgressInLobby");
		isHeartSide_MethodInfo = collabUtils2Module
			.GetType("Celeste.Mod.CollabUtils2.LobbyHelper")
			?.GetMethod("IsHeartSide", BindingFlags.Static | BindingFlags.Public);
		collabInGameForcedArea_FieldInfo = collabUtils2Module
			.GetType("Celeste.Mod.CollabUtils2.UI.InGameOverworldHelper")
			?.GetField("collabInGameForcedArea", BindingFlags.NonPublic | BindingFlags.Static);

		if (progressPageType is null)
		{
			Log("progressPageType not found", logLevel: LogLevel.Warn);
			return;
		}
		if (isHeartSide_MethodInfo is null)
		{
			Log("isHeartSide_MethodInfo not found", logLevel: LogLevel.Warn);
			return;
		}
		if (collabInGameForcedArea_FieldInfo is null)
		{
			Log("collabInGameForcedArea_FieldInfo not found", logLevel: LogLevel.Warn);
			return;
		}

		Log($"Loaded CollabUtils2 v{collabUtils2EverestModule.Metadata.VersionString}");
		Loaded = true;
	}

	/// <summary>
	/// Return true if it is a collabutils2 progress page.
	/// </summary>
	/// <param name="page"></param>
	/// <returns></returns>
	// ReSharper disable once InconsistentNaming
	public static bool IsCU2ProgressPage(OuiJournalPage? page)
	{
		if (!Loaded)
		{
			return false;
		}
		return page?.GetType() == progressPageType;
	}

	public static bool IsHeartSide(string sid)
	{
		object? isHeartSide = isHeartSide_MethodInfo?.Invoke(null, [sid,]);
		return (bool)(isHeartSide ?? false);
	}

	public static string? GetCurrentLevelSet()
	{
		return (collabInGameForcedArea_FieldInfo?.GetValue(null) as AreaData)?.LevelSet;
	}

	private static List<AreaStats> GetUnsortedCollabStats(SaveData instance, OuiJournal journal)
	{
		string? journalLevelSet = GetCurrentLevelSet();

		LevelSetStats levelSet = instance.GetLevelSetStatsFor(journalLevelSet);

		if (levelSet.Areas.TrueForAll(area => area.SID != "SpringCollab2020/5-Grandmaster/ZZ-NewHeartSide"))
		{
			return levelSet.Areas;
		}
		return levelSet.Areas.Where(area => area.SID != "SpringCollab2020/5-Grandmaster/ZZ-HeartSide").ToList();
	}

	public static List<AreaStats> GetSortedCollabAreaStats(SaveData instance, OuiJournal journal)
	{
		List<AreaStats> areaStats = GetUnsortedCollabStats(instance, journal);
		var areaStatsArray = new AreaStats[areaStats.Count];
		areaStats.CopyTo(areaStatsArray);
		List<AreaStats> areaStatsCopy = areaStatsArray.ToList();

		Regex startsWithNumber = new(".*/[0-9]+-.*");
		if (areaStats.Select(map => AreaData.Get(map).Icon ?? "").All(icon => startsWithNumber.IsMatch(icon)))
		{
			areaStatsCopy.Sort(delegate (AreaStats a, AreaStats b)
			{
				AreaData aAreaData = AreaData.Get(a);
				AreaData bAreaData = AreaData.Get(b);
				bool aIsHeartSide = IsHeartSide(a.SID);
				bool bIsHeartSide = IsHeartSide(b.SID);
				if (aIsHeartSide && !bIsHeartSide)
				{
					return 1;
				}
				if (!aIsHeartSide && bIsHeartSide)
				{
					return -1;
				}
				return aAreaData.Icon != bAreaData.Icon ? aAreaData.Icon.CompareTo(bAreaData.Icon) : aAreaData.Name.CompareTo(bAreaData.Name);
			});
		}

		return areaStatsCopy;
	}

	public static int ProgressPageAmount(OuiJournal journal)
	{
		return journal.Pages.Count(page => page.GetType() == progressPageType);
	}

	private static int FirstProgressPage(OuiJournal journal)
	{
		var i = 0;
		while (journal.Pages[i].GetType() != progressPageType)
		{
			if (i + 1 > journal.Pages.Count - 1)
			{
				return -1;
			}
			i++;
		}
		return i;
	}

	private const int MAPS_PER_PAGE = 12;
	public static int MapsOnPage(OuiJournalPage page, OuiJournal journal, SaveData instance, out int firstIndexOnPage)
	{
		int firstProgressPage = FirstProgressPage(journal);
		if (firstProgressPage == -1)
		{
			firstIndexOnPage = -1;
			return -1;
		}

		int i = page.PageIndex - firstProgressPage;
		firstIndexOnPage = MAPS_PER_PAGE * i;
		int val = GetUnsortedCollabStats(instance, journal).Count - MAPS_PER_PAGE * i;
		return val > MAPS_PER_PAGE ? MAPS_PER_PAGE : val;
	}

	public static int FirstMapIndexOnPage(OuiJournalPage page, OuiJournal journal)
	{
		int firstProgressPage = FirstProgressPage(journal);
		if (firstProgressPage == -1)
		{
			return -1;
		}
		int i = page.PageIndex - firstProgressPage;
		return MAPS_PER_PAGE * i;
	}
}
