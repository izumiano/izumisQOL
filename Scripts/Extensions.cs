using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Celeste;
using Celeste.Mod;
using Celeste.Mod.izumisQOL.UI;

using Monocle;

public static class Extensions
{
	public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description)
	{
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, initiallyVisible: false, containingMenu)
		{
			TextColor = Color.Gray,
			HeightExtra = 0f
		};
		subMenu.Add(descriptionText);
		subMenuItem.OnEnter = (Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate
		{
			descriptionText.FadeVisible = true;
		});
		subMenuItem.OnLeave = (Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate
		{
			descriptionText.FadeVisible = false;
		});
	}

	public static void InsertDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description)
	{
		TextMenuExt.EaseInSubHeaderExt descriptionText = new(description, initiallyVisible: false, containingMenu)
		{
			TextColor = Color.Gray,
			HeightExtra = 0f
		};
		subMenu.Insert(subMenu.IndexOf(subMenuItem) + 1, descriptionText);
		subMenuItem.OnEnter = (Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate
		{
			descriptionText.FadeVisible = true;
		});
		subMenuItem.OnLeave = (Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate
		{
			descriptionText.FadeVisible = false;
		});
	}

	public static void NeedsRelaunch(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem)
		{
			TextMenuExt.EaseInSubHeaderExt needsRelaunchText = new(Dialog.Clean("MODOPTIONS_NEEDSRELAUNCH"), initiallyVisible: false, containingMenu)
			{
				TextColor = Color.OrangeRed,
				HeightExtra = 0f
			};
			subMenu.Add(needsRelaunchText);
			subMenuItem.OnEnter = (Action)Delegate.Combine(subMenuItem.OnEnter, (Action)delegate
			{
				needsRelaunchText.FadeVisible = true;
			});
			subMenuItem.OnLeave = (Action)Delegate.Combine(subMenuItem.OnLeave, (Action)delegate
			{
				needsRelaunchText.FadeVisible = false;
			});
		}

	public static T1 Log<T1, T2>(this T1 obj, T2 identifier, LogLevel logLevel = LogLevel.Verbose)
	{
		return Log(obj, identifier.ToString(), logLevel);
	}

	public static T Log<T>(this T obj, string identifier = null, LogLevel logLevel = LogLevel.Verbose)
	{
		string text = obj is null ? "null" : obj.ToString();
		string log = string.IsNullOrEmpty(identifier) ? text : identifier + ": " + text;

		if (string.IsNullOrEmpty(log))
		{
			log = (string.IsNullOrEmpty(identifier) ? "value" : identifier) + " was null or empty";
		}

#if DEBUG
		var methodInfo = new StackTrace().GetFrame(1).GetMethod();
		var className = methodInfo.ReflectedType.Name;
		var methodName = methodInfo.Name;

		Logger.Log(logLevel == LogLevel.Error ? LogLevel.Error : LogLevel.Debug, "izumisQOL/" + className + "/" + methodName, log);
#else
		Logger.Log(logLevel, "izumisQOL", log);
#endif
		return obj;
	}
}
