using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.izumisQOL
{
	public class Global
	{
		public static string BaseDirectory;

		public static SettingsModule izuSettings;

		public static void Log<T>(T text)
		{
			if(text == null)
			{
				Logger.Log(LogLevel.Info, "izumi keybind swapper", "value was null");
			}
			Logger.Log(LogLevel.Info, "izumi keybind swapper", text.ToString());
		}
	}
}
