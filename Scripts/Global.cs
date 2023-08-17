using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Celeste.Mod.izumisQOL
{
	public class Global
	{
		public static string BaseDirectory;

		public static SettingsModule izuSettings;

		//public static void Log<T>(T text)
		//{
		//	if(text == null)
		//	{
		//		Logger.Log(LogLevel.Info, "izumi keybind swapper", "value was null");
		//	}
		//	Logger.Log(LogLevel.Info, "izumi keybind swapper", text.ToString());
		//}
		public static void Log<T>(T text)
		{
			var methodInfo = new StackTrace().GetFrame(1).GetMethod();
			var className = methodInfo.ReflectedType.Name;
			if (text == null)
			{
				Logger.Log(LogLevel.Info, "izumisQOL/" + className, "value was null");
			}
			Logger.Log(LogLevel.Info, "izumisQOL/" + className, text.ToString());
		}
	}
}
