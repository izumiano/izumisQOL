using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Celeste.Mod.izumisQOL.ModIntegration;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.izumisQOL;

public static class BetterJournalModule
{
	private static readonly string journalStatsPath = UserIO.SavePath.SanitizeFilePath() + "/izumisQOL/journalStats/";

	private enum JournalDataType
	{
		Default,
		SeparateTimes,
		Saved,
		Difference,
	}

	private static JournalDataType CurrJournalDataType
	{
		get
		{
			if( JournalProgressPage is null )
			{
				return JournalDataType.Default;
			}

			return journalDataTypes.GetValueOrDefault(JournalProgressPage, JournalDataType.Default);
		}
		set
		{
			if( JournalProgressPage != null )
			{
				journalDataTypes[JournalProgressPage] = value;
			}
			else
			{
				Log("Journal Progress Page was null", LogLevel.Warn);
			}
		}
	}

	private static readonly Dictionary<OuiJournalPage, JournalDataType> journalDataTypes = new();

	private static OuiJournal? _journal;

	private static OuiJournalPage? JournalProgressPage
	{
		get
		{
			if( _journal is null )
			{
				return null;
			}

			if( _journal.PageIndex > _journal.Pages.Count - 1 || _journal.PageIndex < 0 )
			{
				return null;
			}

			return _journal.Page();
		}
	}

	private static readonly Dictionary<OuiJournalPage, VirtualRenderTarget> renderTargets = new();

	private static VirtualRenderTarget? RenderTarget
	{
		get
		{
			if( JournalProgressPage is null )
			{
				return null;
			}

			return renderTargets.GetValueOrDefault(JournalProgressPage);
		}
	}

	private static OuiJournalPage.TextCell? modTotalsTimeCell;

	private static List<CustomAreaStats>? journalSnapshot;

	private static List<CustomAreaStats>? JournalSnapshot
	{
		get
		{
			if( journalSnapshot == null )
			{
				if( _journal is null ) return null;
				journalSnapshot = LoadJournalSnapshot(_journal);
			}

			return journalSnapshot;
		}
		set => journalSnapshot = value;
	}

	private static int TimeColumnSpread
	{
		get
		{
			var save = SaveData.Instance;
			if( save == null )
				return 1;
			if( SeparateABCSideTimes(save) <= 0 || CurrJournalDataType == JournalDataType.Default )
			{
				return 1;
			}

			return save.UnlockedModes;
		}
	}

	public static  bool InJournal                  => JournalProgressPage != null;
	private static bool ProgressPageIsCollabUtils2 => CollabUtils2Integration.IsCU2ProgressPage(JournalProgressPage);


	public static void Load()
	{
		SetUpDirectory();
	}

	private static void SetUpDirectory()
	{
		Directory.CreateDirectory(journalStatsPath);
	}

	public static void Update(On.Celeste.OuiJournal.orig_Update orig, OuiJournal self)
	{
		orig(self);

		if( !ModSettings.BetterJournalEnabled )
			return;

		InputHandler(self);
	}

	private static void InputHandler(OuiJournal journal)
	{
		if( !ModSettings.EnableHotkeys )
			return;

		if( ModSettings.ButtonSaveJournal.Pressed )
		{
			if( SaveJournalSnapshot(journal) )
			{
				Tooltip.Show("MODOPTIONS_IZUMISQOL_BETTERJOURNAL_SAVED_TOOLTIP".AsDialog(),
					position: Tooltip.DisplayPosition.TopLeft);
				JournalSnapshot = null;
				if( ChangeCurrentJournalDataType(JournalDataType.Default) )
				{
					UpdateJournalData(journal);
				}
			}
		}

		int input = JournalDataTypeInput(journal);
		if( input != 0 )
		{
			if( ChangeCurrentJournalDataType(input) )
			{
				UpdateJournalData(journal);
			}
		}
	}

	private static void UpdateJournalData(OuiJournal journal)
	{
		var instance = SaveData.Instance;
		if( instance is null || JournalProgressPage is null )
			return;

		if( JournalSnapshot == null && (int)CurrJournalDataType > 1 )
		{
			return;
		}

		try
		{
			var isCollabUtils2Page         = false;
			var firstIndexOnPage           = 0;
			int mapsOnThisCollabUtils2Page = -1;
			if( CollabUtils2Integration.IsCU2ProgressPage(JournalProgressPage) )
			{
				isCollabUtils2Page = true;
				mapsOnThisCollabUtils2Page =
					CollabUtils2Integration.MapsOnPage(JournalProgressPage, journal, instance, out firstIndexOnPage);
			}
			else if( JournalProgressPage.GetType() != typeof(OuiJournalProgress) )
			{
				return;
			}

			DynamicData journalProgressDynData = DynamicData.For(JournalProgressPage);
			var         table                  = journalProgressDynData.Get<OuiJournalPage.Table>("table");
			if( table is null )
			{
				return;
			}
			DynamicData tableDynData = DynamicData.For(table);

			var rows = tableDynData.Get<List<OuiJournalPage.Row>>("rows");
			if( rows is null )
			{
				return;
			}

			SetupDefaultTableData();

			int lastUnmoddedRow = isCollabUtils2Page ? mapsOnThisCollabUtils2Page + 2 : GetLastUnmoddedRowIndex(rows);
			int abcSeparation   = SeparateABCSideTimes(instance);
			var heartsideOffset = 0;
			for( var i = 1; i < lastUnmoddedRow - 1; i++ )
			{
				if( isCollabUtils2Page )
				{
					GetInterludeOffset(firstIndexOnPage + i - 1, instance, journal, out AreaStats? area, true);
					if( area is null )
					{
						return;
					}
					heartsideOffset = CollabUtils2Integration.IsHeartSide(AreaData.Get(area).SID) ? 1 : 0;
				}

				OuiJournalPage.Row
					row = rows[i + heartsideOffset]; // go up an additional row if were on the heartside to skip the ui line between regular levels and the heartside
				InitializeColumns(row);
				List<OuiJournalPage.Cell> entries = row.Entries;
				try
				{
					ReplaceTime(0);

					void ReplaceTime(int mode)
					{
						OuiJournalPage.Cell cell = new OuiJournalPage.EmptyCell(100f);

						int entriesIndex = entries.Count + mode + instance.UnlockedModes - 6;
						if( entriesIndex > entries.Count - 1 || entriesIndex < 0 )
						{
							return;
						}

						long time = GetTimeAtIndexFromDataType(firstIndexOnPage + i - 1, journal, CurrJournalDataType, mode,
							out int modesForThisArea, instance, JournalSnapshot, isCollabUtils2Page);

						if( modesForThisArea < 1 )
						{
							return;
						}
						
						if( !ProgressPageIsCollabUtils2 && mode > abcSeparation )
						{
							GetInterludeOffset(i - 1, instance, journal, out AreaStats? area);
							if( area is null )
							{
								return;
							}
							AreaData areaData = AreaData.Get(area);
							if( !areaData.HasMode((AreaMode)mode) )
							{
								cell = new OuiJournalPage.EmptyCell(0);
							}
						}
						else if( mode <= abcSeparation || CurrJournalDataType != JournalDataType.Default )
						{
							if( time == 0 )
							{
								cell = new OuiJournalPage.IconCell("dot");
							}
							else
							{
								string timeDialog = time > 0 ? Dialog.Time(time) : "-";
								timeDialog = CurrJournalDataType == JournalDataType.Difference && time > 0
									? "+" + timeDialog
									: timeDialog;
								cell = new OuiJournalPage.TextCell(timeDialog, JournalProgressPage?.TextJustify ?? Vector2.Zero, 0.5f,
									GetColorFromDataType(CurrJournalDataType));
							}
						}

						cell.SpreadOverColumns =
							(int)Math.Round(TimeColumnSpread / (float)modesForThisArea, MidpointRounding.AwayFromZero);

						entries[entriesIndex] = cell;

						if( modesForThisArea > mode + 1 )
						{
							ReplaceTime(mode + 1);
						}
					}
				}
				catch( Exception ex )
				{
					Log(ex, LogLevel.Error);
				}

				try
				{
					int unlocked = ProgressPageIsCollabUtils2 ? 1 : instance.UnlockedModes;
					ReplaceDeathCount(0);

					void ReplaceDeathCount(int mode)
					{
						int unlockedCopy = unlocked;
						int entriesIndex = entries.Count + mode + instance.UnlockedModes - unlockedCopy - 6 +
						                   (ProgressPageIsCollabUtils2 ? -1 : 0);
						if( entriesIndex > entries.Count - 1 || entriesIndex < 0 )
						{
							return;
						}

						int deaths = GetDeathsAtIndexFromDataType(firstIndexOnPage + i - 1, journal, CurrJournalDataType, mode, ref unlocked,
							instance,                                                         JournalSnapshot, isCollabUtils2Page);

						OuiJournalPage.Cell cell;
						if( deaths <= 0 && ProgressPageIsCollabUtils2 )
						{
							cell = new OuiJournalPage.IconCell("dot")
							{
								SpreadOverColumns = unlocked == unlockedCopy ? 1 : unlockedCopy,
							};
						}
						else
						{
							string deathDialog = deaths > -1 ? Dialog.Deaths(deaths) : "-";
							deathDialog = CurrJournalDataType == JournalDataType.Difference && deaths > -1
								? "+" + deathDialog
								: deathDialog;
							cell = new OuiJournalPage.TextCell(deathDialog, JournalProgressPage?.TextJustify ?? Vector2.Zero, 0.5f,
								GetColorFromDataType(CurrJournalDataType))
							{
								SpreadOverColumns = unlocked == unlockedCopy ? 1 : unlockedCopy,
							};
						}

						entries[entriesIndex] = cell;

						if( unlocked > mode + 1 )
						{
							ReplaceDeathCount(mode + 1);
						}
					}
				}
				catch( Exception ex )
				{
					Log(ex, LogLevel.Error);
				}
			}

			if( RenderTarget != null )
			{
				JournalProgressPage.Redraw(RenderTarget);
			}
			else
			{
				Log("Could not find render target for journal progress page", LogLevel.Warn);
			}

			void InitializeColumns(OuiJournalPage.Row row)
			{
				if( isCollabUtils2Page )
					return;
				while( row.Count < 12 )
				{
					row.Add(new OuiJournalPage.EmptyCell(0f));
				}
			}

			// Sets up the time column, the header and the default total time cell in the vanilla journal
			void SetupDefaultTableData()
			{
				#region Header

				float width = 0;
				if( ProgressPageIsCollabUtils2 )
				{
					bool displayIcons = (from area in AreaData.Areas
					                     where !area.Interlude_Safe
					                     select area.Icon).Distinct().Count() > 1;
					width = displayIcons ? 360f : 420f;
				}

				rows[0].Entries[0] = new OuiJournalPage.TextCell(GetDataTypeText(CurrJournalDataType), new Vector2(0f, 0.5f),
					GetHeaderTextSizeFromDataType(CurrJournalDataType, isCollabUtils2Page), Color.Black * 0.7f, width);

				#endregion

				if( !isCollabUtils2Page )
				{
					#region Top Row Time Columns

					List<OuiJournalPage.Cell> entries = table.Header.Entries;
					if( CurrJournalDataType == JournalDataType.Default || SeparateABCSideTimes(instance) <= 0 )
					{
						entries[^3] = new OuiJournalPage.IconCell("time", 220f);
						entries[^2] = new OuiJournalPage.EmptyCell(100f);
					}
					else if( instance.UnlockedModes > 2 )
					{
						entries[^3] = new OuiJournalPage.EmptyCell(100f);
						entries[^2] = new OuiJournalPage.IconCell("time", 100f);
					}

					#endregion

					if( modTotalsTimeCell is not null )
					{
						modTotalsTimeCell.SpreadOverColumns = TimeColumnSpread;
						DynamicData.For(modTotalsTimeCell).Set("forceWidth", CurrJournalDataType != JournalDataType.Default);
					}

					#region Default Total Time Cell

					int totalTimeRowIndex = GetLastUnmoddedRowIndex(rows);
					if( totalTimeRowIndex < rows.Count - 1 && totalTimeRowIndex >= 0 )
					{
						OuiJournalPage.Row  defaultTotalTimeRow = rows[totalTimeRowIndex];
						OuiJournalPage.Cell totalTimeCell       = defaultTotalTimeRow.Entries[^1];
						totalTimeCell.SpreadOverColumns = TimeColumnSpread;
						DynamicData.For(totalTimeCell).Set("forceWidth", CurrJournalDataType != JournalDataType.Default);
					}

					#endregion
				}
			}
		}
		catch( Exception ex )
		{
			Log(ex);
		}
	}

	private static int JournalDataTypeInput(OuiJournal self)
	{
		if( !InJournal || (self.Page() is not OuiJournalProgress &&
		                   !CollabUtils2Integration.IsCU2ProgressPage(self.Page())) )
			return 0;

		int input = Input.MenuUp.Pressed
			? 1
			: Input.MenuDown.Pressed ? -1 : 0; // 1 if up is pressed, -1 if down is pressed, 0 if nothing
		return input;
	}

	private static bool ChangeCurrentJournalDataType(int dir)
	{
		var instance = SaveData.Instance;
		if( instance == null )
		{
			return false;
		}

		int             newLocation = (int)CurrJournalDataType + dir;
		var newType     = (JournalDataType)newLocation;
		if( JournalSnapshot == null )
		{
			int abcSeparation = SeparateABCSideTimes(instance, newType);
			switch( newLocation )
			{
				case > 1:
				case 0:
					CurrJournalDataType = JournalDataType.Default;
					return true;
				case < 0:
					newType = JournalDataType.SeparateTimes;
					break;
			}

			if( newType != JournalDataType.SeparateTimes || abcSeparation <= 0 ) 
				return false;
			
			Log(newType);
			CurrJournalDataType = newType;
			return true;

		}

		switch( newLocation )
		{
			case < 0:
				CurrJournalDataType = JournalDataType.Difference;
				return true;
			case > 3:
				CurrJournalDataType = JournalDataType.Default;
				return true;
		}

		if( newType == JournalDataType.SeparateTimes && SeparateABCSideTimes(instance, newType) <= 0 )
		{
			newType = (JournalDataType)(newLocation + dir);
		}

		CurrJournalDataType = newType;
		return true;
	}

	private static bool ChangeCurrentJournalDataType(JournalDataType journalDataType)
	{
		var instance = SaveData.Instance;
		if( instance == null )
		{
			return false;
		}

		if( JournalSnapshot == null )
		{
			return false;
		}

		var             newLocation = (int)journalDataType;
		var newType     = (JournalDataType)newLocation;
		if( newType == JournalDataType.SeparateTimes && SeparateABCSideTimes(instance) <= 0 )
		{
			newType = (JournalDataType)(newLocation + 1);
		}

		CurrJournalDataType = newType;
		return true;
	}

	/// <summary>
	/// Gives back a zero indexed integer telling you how many sides should currently be available in the journal
	/// </summary>
	// ReSharper disable once InconsistentNaming
	private static int SeparateABCSideTimes(SaveData instance, JournalDataType? journalDataType = null)
	{
		if( ProgressPageIsCollabUtils2 || !ModSettings.SeparateABCSideTimes )
			return 0;

		journalDataType ??= CurrJournalDataType;

		if( journalDataType.Value == JournalDataType.Default )
			return 0;

		List<AreaStats> areas                = instance.LevelSetStats.AreasIncludingCeleste;
		var             highestSideAvailable = 0;
		foreach( AreaData areaData in areas.Select(AreaData.Get) )
		{
			if( areaData.HasMode((AreaMode)2) )
			{
				highestSideAvailable = 2;
				break;
			}

			if( areaData.HasMode((AreaMode)1) )
			{
				highestSideAvailable = 1;
			}
		}

		return Math.Min(instance.UnlockedModes - 1, highestSideAvailable);
	}

	private static int GetLastUnmoddedRowIndex(List<OuiJournalPage.Row> rows)
	{
		var offset = 0;
		if( modTotalsTimeCell != null && ModSettings.ShowModTimeInJournal )
		{
			offset = 2;
		}

		int val = rows.Count - 2 - offset;
		if( val >= rows.Count - 1 )
		{
			val = rows.Count - 1;
		}
		else if( val < 0 )
		{
			val = 0;
		}

		return val;
	}

	private static int GetInterludeOffset(int index, SaveData instance, OuiJournal journal, out AreaStats? area, bool isCollabUtils2 = false)
	{
		List<AreaStats> areas;
		if( isCollabUtils2 )
		{
			areas = CollabUtils2Integration.GetSortedCollabAreaStats(instance, journal);
			area  = areas[index];
			return 0;
		}

		var offset = 0;
		areas = instance.LevelSetStats.AreasIncludingCeleste;
		for( var i = 0; i < areas.Count - 1; i++ )
		{
			if( AreaData.Get(areas[i]).Interlude_Safe )
			{
				offset++;
				if( offset + index >= areas.Count )
				{
					Log("weh");
					area = null;
					return 0;
				}
			}
			else if( i >= index + offset )
			{
				break;
			}
		}

		area = areas[index + offset];
		return offset;
	}

	private static long GetTimeAtIndexFromDataType(
		int                   index, OuiJournal journal, JournalDataType journalDataType, int mode, out int modesForThisArea, SaveData instance,
		List<CustomAreaStats>? customAreaStats, bool isCollabUtils2
	)
	{
		int offset = GetInterludeOffset(index, instance, journal, out AreaStats? area, isCollabUtils2);
		if( area is null )
		{
			modesForThisArea = 0;
			return -1;
		}

		AreaData areaData = AreaData.Get(area);
		modesForThisArea = isCollabUtils2 ? 1 : areaData.Mode.Length;
		if( !areaData.HasMode((AreaMode)mode) )
		{
			return -1;
		}

		long newTimePlayed = area.Modes[mode].TimePlayed;

		long totalTime = area.TotalTimePlayed;
		if( SeparateABCSideTimes(instance) <= 0 )
		{
			newTimePlayed = totalTime;
		}

		if( journalDataType == JournalDataType.Default )
		{
			return totalTime;
		}

		if( journalDataType == JournalDataType.SeparateTimes )
		{
			return newTimePlayed;
		}

		if( customAreaStats is null )
			return -1;

		CustomAreaStats areaStats     = customAreaStats[index + offset];
		long            oldTimePlayed = areaStats.TimePlayed[mode];

		if( SeparateABCSideTimes(instance) <= 0 )
		{
			oldTimePlayed = 0;
			foreach( long time in areaStats.TimePlayed )
			{
				oldTimePlayed += time;
			}
		}

		if( journalDataType == JournalDataType.Saved )
		{
			return oldTimePlayed;
		}

		if( journalDataType == JournalDataType.Difference )
		{
			return newTimePlayed - oldTimePlayed;
		}

		return -1;
	}

	private static int GetDeathsAtIndexFromDataType(
		int                    index, OuiJournal journal, JournalDataType journalDataType, int mode, ref int unlockedModes, SaveData instance,
		List<CustomAreaStats>? customAreaStats, bool isCollabUtils2
	)
	{
		int offset = GetInterludeOffset(index, instance, journal, out AreaStats? area, isCollabUtils2);
		if( area is null )
		{
			return -1;
		}
		
		AreaData areaData         = AreaData.Get(area);
		int      modesForThisArea = isCollabUtils2 ? 1 : areaData.Mode.Length;
		if( modesForThisArea < unlockedModes )
		{
			unlockedModes = modesForThisArea;
		}

		if( !areaData.HasMode((AreaMode)mode) )
		{
			return -1;
		}

		int newDeaths = area.Modes[mode].Deaths;

		if( journalDataType is JournalDataType.Default or JournalDataType.SeparateTimes )
		{
			return newDeaths;
		}

		if( customAreaStats == null )
			return -1;

		CustomAreaStats areaStats = customAreaStats[index + offset];
		int             oldDeaths = areaStats.Deaths[mode];

		return journalDataType switch
		{
			JournalDataType.Saved      => oldDeaths,
			JournalDataType.Difference => newDeaths - oldDeaths,
			_                          => -1,
		};
	}

	private static Color GetColorFromDataType(JournalDataType journalDataType)
	{
		return journalDataType switch
		{
			JournalDataType.Saved                                    => Color.Green,
			JournalDataType.Difference                               => Color.Red,
			JournalDataType.Default or JournalDataType.SeparateTimes => JournalProgressPage?.TextColor ?? Color.White,
			_                                                        => Color.White,
		};
	}

	private static string GetDataTypeText(JournalDataType journalDataType)
	{
		return journalDataType switch
		{
			JournalDataType.Default       => "journal_progress".AsDialog(),
			JournalDataType.SeparateTimes => "MODOPTIONS_IZUMISQOL_BETTERJOURNAL_SEPARATEABCSIDE".AsDialog(),
			JournalDataType.Saved         => "MODOPTIONS_IZUMISQOL_BETTERJOURNAL_SAVED".AsDialog(),
			JournalDataType.Difference    => "MODOPTIONS_IZUMISQOL_BETTERJOURNAL_DIFFERENCE".AsDialog(),
			_                             => "_ERROR_",
		};
	}

	private static float GetHeaderTextSizeFromDataType(JournalDataType journalDataType, bool isCollabUtils2)
	{
		if( isCollabUtils2 )
			return 1f;

		return journalDataType switch
		{
			JournalDataType.Default       => 1f,
			JournalDataType.SeparateTimes => 0.6f,
			JournalDataType.Saved         => 1.4f,
			JournalDataType.Difference    => 0.85f,
			_                             => 1f,
		};
	}

	private static long GetTotalModTime()
	{
		var instance = SaveData.Instance;
		if( instance == null )
			return 0;

		long totalTime = 0;
		instance.LevelSetStats.AreasIncludingCeleste.ForEach(area => totalTime += area.TotalTimePlayed);
		return totalTime;
	}

	private static int GetTotalModDeaths()
	{
		var instance = SaveData.Instance;
		if( instance == null )
			return 0;

		var totalDeaths = 0;
		instance.LevelSetStats.AreasIncludingCeleste.ForEach(area => totalDeaths += area.TotalDeaths);
		return totalDeaths;
	}

	private static void AddModTotalToJournal(OuiJournalProgress journalProgressPage, OuiJournalPage.Table table)
	{
		if( !ModSettings.ShowModTimeInJournal )
			return;

		OuiJournalPage.Row row = table
		                         .AddRow().Add(new OuiJournalPage.TextCell(
			                         "MODOPTIONS_IZUMISQOL_BETTERJOURNAL_MODTOTALS".AsDialog(), new Vector2(1f, 0.5f), 0.7f,
			                         journalProgressPage.TextColor))
		                         .Add(null)
		                         .Add(null)
		                         .Add(null)
		                         .Add(null)
		                         .Add(null);
		row.Add(new OuiJournalPage.TextCell(Dialog.Deaths(GetTotalModDeaths()), journalProgressPage.TextJustify, 0.6f,
			journalProgressPage.TextColor)
		{
			SpreadOverColumns = SaveData.Instance.UnlockedModes,
		});
		for( var i = 1; i < SaveData.Instance.UnlockedModes; i++ )
		{
			row.Add(null);
		}

		row.Add(modTotalsTimeCell = new OuiJournalPage.TextCell(Dialog.Time(GetTotalModTime()),
			journalProgressPage.TextJustify, 0.6f, journalProgressPage.TextColor));
		table.AddRow();
	}

	public static void OuiJournalProgressCtor(
		On.Celeste.OuiJournalProgress.orig_ctor orig, OuiJournalProgress self, OuiJournal journal
	)
	{
		orig(self, journal);

		if( !ModSettings.BetterJournalEnabled )
			return;

		DynamicData          journalProgressDynData = DynamicData.For(self);
		var table                  = journalProgressDynData.Get<OuiJournalPage.Table>("table");
		if( table is null )
		{
			return;
		}

		table.AddColumn(new OuiJournalPage.EmptyCell(100f))
		     .AddColumn(new OuiJournalPage.EmptyCell(100f));

		AddModTotalToJournal(self, table);
	}

	public static void OnJournalPageRedraw(
		On.Celeste.OuiJournalPage.orig_Redraw orig, OuiJournalPage self, VirtualRenderTarget buffer
	)
	{
		orig(self, buffer);
		if( self.GetType() != typeof(OuiJournalProgress) && !CollabUtils2Integration.IsCU2ProgressPage(self) )
		{
			return;
		}

		renderTargets[self] = buffer;
	}

	public static void OnJournalEnter(OuiJournal journal, Oui from)
	{
		_journal = journal;
	}

	public static void OnJournalClose(On.Celeste.OuiJournal.orig_Close orig, OuiJournal self)
	{
		CurrJournalDataType = JournalDataType.Default;
		JournalSnapshot     = null;
		_journal             = null;
		renderTargets.Clear();

		orig(self);
	}

	private static bool SaveJournalSnapshot(OuiJournal journal)
	{
		var instance = SaveData.Instance;
		if( instance == null )
			return false;

		List<CustomAreaStats> customAreaStats = [];
		List<AreaStats>       areaStats;
		// areaStats = !ProgressPageIsCollabUtils2 ? instance.LevelSetStats.AreasIncludingCeleste : CollabUtils2Integration.GetSortedCollabAreaStats(instance, journal);
		areaStats = ProgressPageIsCollabUtils2 ? CollabUtils2Integration.GetSortedCollabAreaStats(instance, journal) : instance.LevelSetStats.AreasIncludingCeleste;

		areaStats.ForEach(area => customAreaStats.Add(new CustomAreaStats(area.Modes)));
		areaStats.ForEach(area => area.SID.Log());

		string levelSet = areaStats[0].LevelSet.Replace("/", "").Replace("\n", "");
		string path     = journalStatsPath + instance.FileSlot + "_" + levelSet + ".txt";
		try
		{
			FileStream         fileStream = File.Create(path);
			using StreamWriter writer     = new(fileStream);
			YamlHelper.Serializer.Serialize(writer, customAreaStats, typeof(List<CustomAreaStats>));
			Log("Saved journal to: " + path);
			return true;
		}
		catch( Exception ex )
		{
			Log(ex);
			return false;
		}
	}

	private static List<CustomAreaStats>? LoadJournalSnapshot(OuiJournal journal)
	{
		var instance = SaveData.Instance;
		if( instance == null )
			return null;

		List<CustomAreaStats>? customAreaStats = null;

		try
		{
			if( !JournalStatFileExists(journal, instance, out string path) )
			{
				Log("Could not read from: " + path);
				return null;
			}

			FileStream         fileStream = File.OpenRead(path);
			using StreamReader reader     = new(fileStream);
			customAreaStats =
				(List<CustomAreaStats>?)YamlHelper.Deserializer.Deserialize(reader, typeof(List<CustomAreaStats>));
			Log("Successfully read journal snapshot from: " + path);
		}
		catch( Exception ex )
		{
			Log(ex, LogLevel.Error);
		}

		return customAreaStats;
	}

	private static bool JournalStatFileExists(OuiJournal journal, SaveData instance, out string path)
	{
		string? levelSet;
		if( ProgressPageIsCollabUtils2 )
		{
			levelSet = journal.Overworld == null
				? null
				: new DynData<Overworld>(journal.Overworld).Get<AreaData>("collabInGameForcedArea")?.LevelSet;
		}
		else
		{
			levelSet = instance.LevelSetStats.AreasIncludingCeleste[0].LevelSet;
		}

		levelSet = levelSet?.Replace("/", "").Replace("\n", "");
		path     = journalStatsPath + instance.FileSlot + "_" + levelSet + ".txt";
		return File.Exists(path);
	}

	private struct CustomAreaStats
	{
		public readonly long[] TimePlayed = new long[3];
		public readonly int[]  Deaths     = new int[3];

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
			Deaths     = deaths;
		}

		public CustomAreaStats() { }
	}
}