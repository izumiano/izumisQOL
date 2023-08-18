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

		public static void Log<T>(T text, LogLevel logLevel = LogLevel.Verbose)
		{
			if(logLevel == LogLevel.Verbose && !izuSettings.VerboseLogging)
				return;

			string log = text.ToString();

			if (string.IsNullOrEmpty(log))
			{
				log = "value was null or empty";
			}

#if DEBUG
			var methodInfo = new StackTrace().GetFrame(1).GetMethod();
			var className = methodInfo.ReflectedType.Name;

			Logger.Log(LogLevel.Debug, "izumisQOL/" + className, log);
#else
			Logger.Log(logLevel, "izumiQOL", log);
#endif
		}
	}
}
