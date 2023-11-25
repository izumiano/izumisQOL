using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using MonoMod.Utils;
using System.Text.RegularExpressions;

namespace Celeste.Mod.izumisQOL.ModIntegration
{
	public class CollabUtils2Integration : Global
	{
		//public static bool InJournal => Loaded && Journal != null;

		public static bool Loaded = false;

		private static Type ProgressPageType;

		private static EverestModule collabUtils2Module;
		private static MethodInfo isHeartSide_MethodInfo;

		public static void Load()
		{
			if (IntegrationUtils.TryGetModule(new EverestModuleMetadata
			{
				Name = "CollabUtils2",
				VersionString = "1.8.11"
			}, out collabUtils2Module))
			{
				Log("loaded");
				Loaded = true;

				ProgressPageType = collabUtils2Module.GetType().Module.GetType("Celeste.Mod.CollabUtils2.UI.OuiJournalCollabProgressInLobby");
				isHeartSide_MethodInfo = collabUtils2Module.GetType().Module.GetType("Celeste.Mod.CollabUtils2.LobbyHelper").GetMethod("IsHeartSide", BindingFlags.Static | BindingFlags.Public);
			}
		}

		/// <summary>
		/// Return true if it is a collabutils2 progress page.
		/// </summary>
		/// <param name="page"></param>
		/// <returns></returns>
		public static bool IsCU2ProgressPage(OuiJournalPage page)
		{
			if (!Loaded)
			{
				return false;
			}
			return page?.GetType() == ProgressPageType;
		}

		public static bool IsHeartSide(string sid)
		{
			return (bool)isHeartSide_MethodInfo.Invoke(null, new object[] { sid });
		}

		private static List<AreaStats> GetUnsortedCollabStats(SaveData instance, OuiJournal journal)
		{
			List<AreaStats> areaStats = new();
			string journalLevelSet = (journal.Overworld == null) ? null : new DynData<Overworld>(journal.Overworld).Get<AreaData>("collabInGameForcedArea").LevelSet;

			foreach (LevelSetStats levelSet1 in instance.LevelSets)
			{
				foreach (AreaStats areaStat in levelSet1.Areas)
				{
					if (areaStat.LevelSet == journalLevelSet)
					{
						areaStats.Add(areaStat);
					}
				}
			}
			return areaStats;
		}

		public static List<AreaStats> GetSortedCollabAreaStats(SaveData instance, OuiJournal journal)
		{
			List<AreaStats> areaStats = GetUnsortedCollabStats(instance, journal);

			Regex startsWithNumber = new(".*/[0-9]+-.*");
			if (areaStats.Select((AreaStats map) => AreaData.Get(map).Icon ?? "").All((string icon) => startsWithNumber.IsMatch(icon)))
			{
				areaStats.Sort(delegate (AreaStats a, AreaStats b)
				{
					AreaData areaData2 = AreaData.Get(a);
					AreaData areaData3 = AreaData.Get(b);
					bool flag = IsHeartSide(a.SID);
					bool flag2 = IsHeartSide(b.SID);
					if (flag && !flag2)
					{
						return 1;
					}
					if (!flag && flag2)
					{
						return -1;
					}
					return (!(areaData2.Icon == areaData3.Icon)) ? areaData2.Icon.CompareTo(areaData3.Icon) : areaData2.Name.CompareTo(areaData3.Name);
				});
			}

			return areaStats;
		}

		public static int ProgressPageAmount(OuiJournal journal)
		{
			int count = 0;
			foreach(OuiJournalPage page in journal.Pages)
			{
				if(page.GetType() == ProgressPageType)
				{
					count++;
				}
			}
			return count;
		}

		private static int FirstProgressPage(OuiJournal journal)
		{
			int i = 0;
			while(journal.Pages[i].GetType() != ProgressPageType)
			{
				if(i + 1 > journal.Pages.Count - 1)
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
			return val > MAPS_PER_PAGE ? 12 : val;
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
}
