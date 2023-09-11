﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using Celeste.Mod;
using Celeste.Mod.izumisQOL;

public class Global
{
	public static string BaseDirectory;

	public static SettingsModule ModSettings;

	public static void Log<T>(T obj, LogLevel logLevel = LogLevel.Verbose)
	{
//#if !DEBUG
//		Logger.Log(LogLevel.Info, "izumisQOL", "is not log verbose: " + (!ModSettings.VerboseLogging).ToString());
//		if (logLevel == LogLevel.Verbose && !ModSettings.VerboseLogging)
//			return;
//#endif

		string text = obj is null ? "null" : obj.ToString();
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

	public static bool WhitelistContains(string path, string name)
	{
		foreach (string line in File.ReadAllLines(path))
		{
			if (line[0] == '#')
				continue;
			if (line.StartsWith(name))
				return true;
		}
		return false;
	}
}
