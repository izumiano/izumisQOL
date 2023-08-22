using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using System.Diagnostics;

using Celeste;
using Celeste.Mod;

public static class Extensions
{
	public static void AddDescription(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem, string description)
		{
			TextMenuExt.EaseInSubHeaderExt descriptionText = new TextMenuExt.EaseInSubHeaderExt(description, initiallyVisible: false, containingMenu)
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

	public static void NeedsRelaunch(this TextMenuExt.SubMenu subMenu, TextMenu containingMenu, TextMenu.Item subMenuItem)
		{
			TextMenuExt.EaseInSubHeaderExt needsRelaunchText = new TextMenuExt.EaseInSubHeaderExt(Dialog.Clean("MODOPTIONS_NEEDSRELAUNCH"), initiallyVisible: false, containingMenu)
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

	public static void Log<T>(this T text, string identifier = null, LogLevel logLevel = LogLevel.Verbose)
	{
//#if !DEBUG
//		if (logLevel == LogLevel.Verbose && !Global.ModSettings.VerboseLogging)
//			return;
//#endif

		string log = string.IsNullOrEmpty(identifier) ? text.ToString() : identifier + ": " + text.ToString();

		if (string.IsNullOrEmpty(log))
		{
			log = "value was null or empty";
		}

#if DEBUG
		var methodInfo = new StackTrace().GetFrame(1).GetMethod();
		var className = methodInfo.ReflectedType.Name;
		var methodName = methodInfo.Name;

		Logger.Log(LogLevel.Debug, "izumisQOL/" + className + "/" + methodName, log);
#else
		Logger.Log(logLevel, "izumisQOL", log);
#endif
	}
}
