using System;
using System.Diagnostics;
using System.Reflection;
using Celeste.Mod.izumisQOL.Scripts;
using Microsoft.Xna.Framework;
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
		return Dialog.Clean(dialogID);
	}

	public static void AddDescription(
		this TextMenu.Item subMenuItem, TextMenuExt.SubMenu subMenu, TextMenu containingMenu, string description
	)
	{
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, initiallyVisible: false, containingMenu)
		{
			TextColor   = Color.Gray,
			HeightExtra = 0f,
		};
		subMenu.Add(descriptionText);
		subMenuItem.OnEnter =
			(Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate { descriptionText.FadeVisible = true; });
		subMenuItem.OnLeave =
			(Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate { descriptionText.FadeVisible = false; });
	}

	public static void InsertDescription(
		this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description
	)
	{
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, initiallyVisible: false, containingMenu)
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
			new("MODOPTIONS_NEEDSRELAUNCH".AsDialog(), initiallyVisible: false, containingMenu)
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

	public static T Log<T>(
		this T           obj,
		string?          identifier = null,
		LogLevel         logLevel   = LogLevel.Verbose,
		Func<T, string>? logParser  = null,
		MethodBase?      methodInfo = null
	)
	{
		logParser ??= LogParser.Default;
		string text = obj is null ? "null" : logParser(obj);
		string log  = string.IsNullOrEmpty(identifier) ? text : identifier + ": " + text;

		if (string.IsNullOrEmpty(log))
		{
			log = (string.IsNullOrEmpty(identifier) ? "value" : identifier) + " was null or empty";
		}

		try
		{
			methodInfo ??= new StackTrace().GetFrame(1)?.GetMethod();

			if (methodInfo is null) throw new Exception("methodInfo was null");

			string? className = methodInfo.GetRealDeclaringType()?.Name;
			if(className is null) throw new Exception("className was null");
			
			string methodName = methodInfo.Name;

			log = "[" + className + "/" + methodName + "] " + log;
		}
		catch(Exception ex)
		{
			Logger.Log(LogLevel.Warn, nameof(izumisQOL), ex.ToString());
		}

		Logger.Log(logLevel, nameof(izumisQOL), log);
		return obj;
	}
}