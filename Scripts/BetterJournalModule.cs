using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Celeste;
using Monocle;
using MonoMod.Utils;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL
{
	public class BetterJournalModule : Global
	{
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
	}
}
