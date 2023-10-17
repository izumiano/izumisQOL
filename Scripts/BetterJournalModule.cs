using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using MonoMod.Utils;
using Microsoft.Xna.Framework;
using System.IO;

namespace Celeste.Mod.izumisQOL
{
	public class BetterJournalModule : Global
	{
		private static readonly string journalStatsPath = BaseDirectory + "Saves\\izumisQOL\\journalStats\\";

		private static bool inJournal = false;

		enum JournalDataType
		{
			Default,
			Saved,
			Difference
		}

		private static JournalDataType journalDataType = JournalDataType.Default;

		private static OuiJournalProgress journalProgressPage;
		private static VirtualRenderTarget renderTarget;

		public static void Init()
		{
			SetUpDirectory();
		}

		private static void SetUpDirectory()
		{
			Directory.CreateDirectory(journalStatsPath);
		}

		public static void Update()
		{
			if (!inJournal)
				return;

			if (ModSettings.ButtonSaveJournal.Pressed)
			{
				SaveJournalSnapshot();
			}
			if (ModSettings.ButtonLoadJournal.Pressed)
			{
				LoadJournalSnapshot();
			}
			if(journalProgressPage != null)
			{
				if (Input.MenuUp.Pressed)
				{
					ChangeCurrentJournalDataType(1);
					UpdateJournalData();
				}
				if (Input.MenuDown.Pressed)
				{
					ChangeCurrentJournalDataType(-1);
					UpdateJournalData();
				}
			}
		}

		private static void ChangeCurrentJournalDataType(int dir)
		{
			SaveData instance = SaveData.Instance;
			if (instance == null)
				return;

			string path = journalStatsPath + instance.FileSlot + "_" + instance.Areas_Safe[0].LevelSet + ".txt";

			if (!File.Exists(path))
			{
				return;
			}

			int newLocation = (int)journalDataType + dir;
			if (newLocation < 0)
			{
				journalDataType = JournalDataType.Difference;
				return;
			}
			if(newLocation > 2)
			{
				journalDataType = JournalDataType.Default;
				return;
			}
			journalDataType = (JournalDataType)newLocation;
		}

		private static void UpdateJournalData()
		{
			SaveData instance = SaveData.Instance;
			if (instance == null)
				return;

			Log("now");
			DynamicData journalProgressDynData = DynamicData.For(journalProgressPage);
			OuiJournalPage.Table table = journalProgressDynData.Get<OuiJournalPage.Table>("table");
			DynamicData tableDynData = DynamicData.For(table);

			List<OuiJournalPage.Row> rows = tableDynData.Get<List<OuiJournalPage.Row>>("rows");
			for (int i = 1; i < rows.Count - 5; i++)
			{
				OuiJournalPage.Row row1 = rows[i];
				List<OuiJournalPage.Cell> entries = row1.Entries;
				try
				{
					long time = GetTimeAtIndexFromDataType(i - 1, instance);
					if (time > 0)
					{
						string timeDialog = Dialog.Time(time);
						entries[entries.Count - 1] = new OuiJournalPage.TextCell(journalDataType == JournalDataType.Difference ? "+" + timeDialog : timeDialog, journalProgressPage.TextJustify, 0.5f, GetColorFromDataType());
					}
					else
					{
						entries[entries.Count - 1] = new OuiJournalPage.IconCell("dot");
					}
				}
				catch (Exception ex)
				{
					Log(ex);
				}

				try
				{
					int unlocked = instance.UnlockedModes;
					ReplaceDeathCount(0);

					void ReplaceDeathCount(int mode)
					{
						int unlockedCopy = unlocked;
						string deathDialog = Dialog.Deaths(GetDeathsAtIndexFromDataType(i - 1, mode, ref unlocked, instance));

						entries[entries.Count + mode - unlockedCopy - 1] = new OuiJournalPage.TextCell(journalDataType == JournalDataType.Difference ? "+" + deathDialog : deathDialog, journalProgressPage.TextJustify, 0.5f, GetColorFromDataType())
						{
							SpreadOverColumns = unlocked == unlockedCopy ? 1 : unlockedCopy
						};

						if(unlocked > mode + 1)
						{
							ReplaceDeathCount(mode + 1);
						}
					}
				}
				catch (Exception ex)
				{
					Log(ex);
				}
			}

			if (renderTarget != null)
			{
				journalProgressPage.Redraw(renderTarget);
			}
			else
			{
				Log("Could not find render target for journal progress page", LogLevel.Warn);
			}
		}

		private static int GetInterludeOffset(int index, SaveData instance, out AreaStats area)
		{
			int offset = 0;
			List<AreaStats> areas = instance.GetLevelSetStats().AreasIncludingCeleste;
			for (int i = 0; i < areas.Count - 1; i++)
			{
				if (AreaData.Get(areas[i]).Interlude_Safe)
				{
					offset++;
					if (offset + index >= areas.Count)
					{
						Log("weh");
						area = null;
						return 0;
					}
				}
				else if (i >= index + offset)
				{
					break;
				}
			}
			area = areas[index + offset];
			return offset;
		}

		private static long GetTimeAtIndexFromDataType(int index, SaveData instance)
		{
			int offset = GetInterludeOffset(index, instance, out AreaStats area);

			if (area == null)
				return 0;

			long newTimePlayed;
			newTimePlayed = area.TotalTimePlayed;

			if (journalDataType == JournalDataType.Default)
			{
				return newTimePlayed;
			}

			long oldTimePlayed = 0;
			List<CustomAreaStats> customAreaStats = LoadJournalSnapshot();
			if (customAreaStats == null)
				return 0;

			CustomAreaStats areaStats = customAreaStats[index + offset];
			foreach (long modeTime in areaStats.TimePlayed)
			{
				oldTimePlayed += modeTime;
			}
			
			if(journalDataType == JournalDataType.Saved)
			{
				return oldTimePlayed;	
			}

			if(journalDataType == JournalDataType.Difference)
			{
				return newTimePlayed - oldTimePlayed;
			}
			return 0;
		}

		private static int GetDeathsAtIndexFromDataType(int index, int mode, ref int unlockedModes, SaveData instance)
		{
			int offset = GetInterludeOffset(index, instance, out AreaStats area);

			if(area == null)
				return 0;

			int newDeaths;
			newDeaths = area.Modes[mode].Deaths;
			int modesForThisArea = AreaData.Get(area).Mode.Length;
			if (modesForThisArea < unlockedModes)
			{
				unlockedModes = modesForThisArea;
			}

			if (journalDataType == JournalDataType.Default)
			{
				return newDeaths;
			}

			List<CustomAreaStats> customAreaStats = LoadJournalSnapshot();
			if (customAreaStats == null)
				return 0;

			CustomAreaStats areaStats = customAreaStats[index + offset];
			int oldDeaths = areaStats.Deaths[mode];

			if (journalDataType == JournalDataType.Saved)
			{
				return oldDeaths;
			}

			if (journalDataType == JournalDataType.Difference)
			{
				return newDeaths - oldDeaths;
			}
			return 0;
		}

		private static Color GetColorFromDataType()
		{
			if (journalDataType == JournalDataType.Saved) return Color.Green;
			if (journalDataType == JournalDataType.Difference) return Color.Red;
			if (journalDataType == JournalDataType.Default) return journalProgressPage != null ? journalProgressPage.TextColor : Color.White;
			return Color.White;
		}

		private static long GetTotalModTime()
		{
			SaveData instance = SaveData.Instance;
			if (instance == null)
				return 0;

			long totalTime = 0;
			instance.GetLevelSetStats().AreasIncludingCeleste.ForEach(area => totalTime += area.TotalTimePlayed);
			return totalTime;
		}

		private static int GetTotalModDeaths()
		{
			SaveData Instance = SaveData.Instance;
			if (Instance == null)
				return 0;

			int totalDeaths = 0;
			Instance.GetLevelSetStats().AreasIncludingCeleste.ForEach(area => totalDeaths += area.TotalDeaths);
			return totalDeaths;
		}

		public static void OuiJournalProgressCtor(On.Celeste.OuiJournalProgress.orig_ctor orig, OuiJournalProgress self, OuiJournal journal)
		{
			orig(self, journal);

			if (!ModSettings.ShowModTimeInJournal)
				return;

			inJournal = true;

			journalProgressPage = self;

			DynamicData journalProgressDynData = DynamicData.For(self);
			OuiJournalPage.Table table = journalProgressDynData.Get<OuiJournalPage.Table>("table");
			

			OuiJournalPage.Row row = table.AddRow().Add(new OuiJournalPage.TextCell("Mod Totals", new Vector2(1f, 0.5f), 0.7f, self.TextColor)).Add(null)
				.Add(null)
				.Add(null)
				.Add(null)
				.Add(null);
			row.Add(new OuiJournalPage.TextCell(Dialog.Deaths(GetTotalModDeaths()), self.TextJustify, 0.6f, self.TextColor)
			{
				SpreadOverColumns = SaveData.Instance.UnlockedModes
			});
			for (int l = 1; l < SaveData.Instance.UnlockedModes; l++)
			{
				row.Add(null);
			}
			row.Add(new OuiJournalPage.TextCell(Dialog.Time(GetTotalModTime()), self.TextJustify, 0.6f, self.TextColor));
			table.AddRow();
		}

		public static void OnJournalRedraw(On.Celeste.OuiJournalProgress.orig_Redraw orig, OuiJournalProgress self, VirtualRenderTarget buffer)
		{
			renderTarget = buffer;
			orig(self, buffer);
		}

		public static void OnJournalClose(On.Celeste.OuiJournal.orig_Close orig, OuiJournal self)
		{
			journalDataType = JournalDataType.Default;
			inJournal = false;

			orig(self);
		}

		private static void SaveJournalSnapshot()
		{
			SaveData instance = SaveData.Instance;
			if (instance == null)
				return;

			List<CustomAreaStats> customAreaStats = new();
			List<AreaStats> areaStats = instance.GetLevelSetStats().AreasIncludingCeleste;
			areaStats.ForEach(area => customAreaStats.Add(new CustomAreaStats(area.Modes)));

			string levelSet = areaStats[0].LevelSet.Replace("/", "").Replace("\n", "");
			string path = journalStatsPath + instance.FileSlot + "_" + levelSet + ".txt";
			Log("Attempting to save journal to: " + path);
			try
			{
				FileStream fileStream = File.Create(path);
				using StreamWriter writer = new(fileStream);
				YamlHelper.Serializer.Serialize(writer, customAreaStats, typeof(List<CustomAreaStats>));
			}
			catch (Exception ex)
			{
				Log(ex);
			}
		}

		private static List<CustomAreaStats> LoadJournalSnapshot()
		{
			SaveData instance = SaveData.Instance;
			if (instance == null)
				return null;

			List<CustomAreaStats> customAreaStats = null;

			string levelSet = instance.GetLevelSetStats().AreasIncludingCeleste[0].LevelSet.Replace("/", "").Replace("\n", "");
			string path = journalStatsPath + instance.FileSlot + "_" + levelSet + ".txt";
			Log("Reading journal snapshot from: " + path);

			try
			{
				if (!File.Exists(path))
				{
					return null;
				}
				FileStream fileStream = File.OpenRead(path);
				using StreamReader reader = new(fileStream);
				customAreaStats = (List<CustomAreaStats>)YamlHelper.Deserializer.Deserialize(reader, typeof(List<CustomAreaStats>));
			}
			catch (Exception ex)
			{
				ex.Log();
			}
			return customAreaStats;
		}

		private struct CustomAreaStats
		{
			public long[] TimePlayed = new long[3];
			public int[] Deaths = new int[3];

			public CustomAreaStats(AreaModeStats[] areaModes)
			{
				TimePlayed[0] = areaModes[0].TimePlayed;
				TimePlayed[1] = areaModes[1].TimePlayed;
				TimePlayed[2] = areaModes[2].TimePlayed;

				Deaths[0] = areaModes[0].Deaths;
				Deaths[1] = areaModes[1].Deaths;
				Deaths[2] = areaModes[2].Deaths;
			}

			public CustomAreaStats(long[] timePlayed, int[] deaths)
			{
				TimePlayed = timePlayed;
				Deaths = deaths;
			}

			public CustomAreaStats()
			{

			}
		}
	}
}
