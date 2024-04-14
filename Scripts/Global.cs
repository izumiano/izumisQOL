﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using Celeste.Mod;
using Celeste.Mod.izumisQOL;
using System.Threading;

public class Global
{
	public static readonly string BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

	public static SettingsModule ModSettings => izumisQOL.ModSettings;

	public static void Log<T>(T obj, LogLevel logLevel = LogLevel.Verbose)
	{
		string text = obj is null ? "null" : obj.ToString();
		string log = text.ToString();

		if (string.IsNullOrEmpty(log))
		{
			log = "value was null or empty";
		}

		var methodInfo = new StackTrace().GetFrame(1).GetMethod();
		var className = methodInfo.ReflectedType.Name;
		var methodName = methodInfo.Name;

		Logger.Log(logLevel, "izumisQOL/" + className + "/" + methodName, log);
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

	public static Task RunAfter(Action func, int millisecondsDelay, Task task, ref CancellationTokenSource cancellationTokenSource)
	{
		if (task is null)
		{
			return RunAfterAsync(func, millisecondsDelay, task, cancellationTokenSource);
		}
		else if (task.IsCompleted)
		{
			cancellationTokenSource = new();
			return null;
		}
		return task;
	}

	private static async Task RunAfterAsync(Action func, int millisecondsDelay, Task task, CancellationTokenSource cancellationTokenSource)
	{
		if (task != null)
			return;

		await Task.Delay(millisecondsDelay, cancellationTokenSource.Token);

		func();
	}
}
