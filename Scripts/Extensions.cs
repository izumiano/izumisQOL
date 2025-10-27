using System;
using System.Diagnostics;
using System.Reflection;
using Celeste.Mod.izumisQOL.Scripts;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.izumisQOL;

public static class Extensions
{
	public static string SanitizeFilePath(this string path)
	{
		return path.Replace("\\", "/");
	}

	public static string[] SanitizeFilePath(this string[] paths)
	{
		for( var i = 0; i < paths.Length; i++ )
		{
			paths[i] = paths[i].SanitizeFilePath();
		}

		return paths;
	}

	public static OuiJournalPage? Page(this OuiJournal journal)
	{
		if( journal.PageIndex > journal.Pages.Count - 1 || journal.PageIndex < 0 )
		{
			Log("Could not get the current journal page", null, LogLevel.Warn);
			return null;
		}

		return journal.Page;
	}

	public static string AsDialog(this string dialogID)
	{
		if( Dialog.Languages is null )
		{
			return "Dialog not yet loaded";
		}

		return Dialog.Clean(dialogID);
	}

	public static TextMenuExt.EaseInSubHeaderExt AddDescription(
		this TextMenu.Item subMenuItem,      TextMenuExt.SubMenu subMenu, TextMenu containingMenu, string description,
		Color?             textColor = null, bool                initiallyVisible = false
	)
	{
		var descriptionText = subMenuItem.CreateDescription(containingMenu, description, textColor);

		subMenu.Add(descriptionText);

		return descriptionText;
	}

	public static TextMenuExt.EaseInSubHeaderExt CreateDescription(
		this TextMenu.Item subMenuItem, TextMenu containingMenu, string description, Color? textColor = null,
		bool               initiallyVisible = false
	)
	{
		textColor ??= Color.Gray;
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, initiallyVisible, containingMenu)
		{
			TextColor   = textColor.Value,
			HeightExtra = 0f,
		};
		subMenuItem.OnEnter =
			(Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate { descriptionText.FadeVisible = true; });
		subMenuItem.OnLeave =
			(Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate { descriptionText.FadeVisible = false; });

		return descriptionText;
	}

	public static void InsertDescription(
		this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description
	)
	{
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, false, containingMenu)
		{
			TextColor   = Color.Gray,
			HeightExtra = 0f,
		};
		subMenu.Insert(subMenu.IndexOf(subMenuItem) + 1, descriptionText);
		subMenuItem.OnEnter =
			(Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate { descriptionText.FadeVisible = true; });
		subMenuItem.OnLeave =
			(Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate { descriptionText.FadeVisible = false; });
	}

	public static void NeedsRelaunch(this TextMenu.Item subMenuItem, TextMenuExt.SubMenu subMenu, TextMenu containingMenu)
	{
		TextMenuExt.EaseInSubHeaderExt needsRelaunchText =
			new("MODOPTIONS_NEEDSRELAUNCH".AsDialog(), false, containingMenu)
			{
				TextColor   = Color.OrangeRed,
				HeightExtra = 0f,
			};
		subMenu.Add(needsRelaunchText);
		subMenuItem.OnEnter =
			(Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate { needsRelaunchText.FadeVisible = true; });
		subMenuItem.OnLeave =
			(Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate { needsRelaunchText.FadeVisible = false; });
	}

	public static Oui? Goto(this Overworld overworld, Type? ouiType)
	{
		if( ouiType is not { } oui2 )
		{
			return null;
		}

		var uI = GetUI(oui2);
		if( uI != null )
		{
			overworld.routineEntity.Add(new Coroutine(overworld.GotoRoutine(uI)));
		}

		return uI;

		Oui? GetUI(Type ouiTypeIn)
		{
			Oui? ouiOut = null;
			foreach( var ui in overworld.UIs )
			{
				if( ui.GetType() == ouiTypeIn )
				{
					ouiOut = ui;
				}
			}

			return ouiOut;
		}
	}

	public static T Log<T>(
		this T           obj,
		string?          identifier = null,
		LogLevel         logLevel   = LogLevel.Verbose,
		Func<T, string>? logParser  = null,
		MethodBase?      methodInfo = null
	)
	{
		logParser ??= LogParser.Default;
		var text = obj is null ? "null" : logParser(obj);
		var log  = string.IsNullOrEmpty(identifier) ? text : identifier + ": " + text;

		if( string.IsNullOrEmpty(log) )
		{
			log = (string.IsNullOrEmpty(identifier) ? "value" : identifier) + " was null or empty";
		}

		try
		{
			methodInfo ??= new StackTrace().GetFrame(1)?.GetMethod();

			if( methodInfo is null )
			{
				throw new Exception("methodInfo was null");
			}

			var className = methodInfo.GetRealDeclaringType()?.Name;
			if( className is null )
			{
				throw new Exception("className was null");
			}

			var methodName = methodInfo.Name;

			log = "[" + className + "/" + methodName + "] " + log;
		}
		catch( Exception ex )
		{
			Logger.Log(LogLevel.Warn, nameof(izumisQOL), ex.ToString());
		}

		Logger.Log(logLevel, nameof(izumisQOL), log);
		return obj;
	}
}