using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

using System.Threading;

namespace Celeste.Mod.izumisQOL;
public class Global
{
	public static SettingsModule ModSettings => izumisQOL.ModSettings;

	public static T Log<T>(T obj, LogLevel logLevel = LogLevel.Verbose, Func<T, string> logParser = null)
	{
		return obj.Log(logLevel: logLevel, logParser: logParser, methodInfo: new StackTrace()?.GetFrame(1)?.GetMethod());
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
