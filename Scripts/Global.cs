using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Celeste.Mod;
using Celeste.Mod.izumisQOL;

public class Global
{
	public static string BaseDirectory;

	public static SettingsModule izuSettings;

	public static void Log<T>(T text, LogLevel logLevel = LogLevel.Verbose)
	{
#if !DEBUG
		if(logLevel == LogLevel.Verbose && !izuSettings.VerboseLogging)
			return;
#endif

		string log = text.ToString();

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
