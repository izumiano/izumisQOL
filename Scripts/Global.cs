using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Celeste.Mod.izumisQOL;

public static class Global
{
	public static SettingsModule ModSettings => izumisQOL.ModSettings;

	public static T Log<T>(T obj, LogLevel logLevel = LogLevel.Verbose, Func<T, string>? logParser = null)
	{
		return obj.Log(logLevel: logLevel, logParser: logParser, methodInfo: new StackTrace().GetFrame(1)?.GetMethod());
	}

	public static bool WhitelistContains(string path, string name)
	{
		return File.ReadAllLines(path)
		           .Where(line => line[0] != '#')
		           .Any(line => line.StartsWith(name));
	}

	public static Task? RunAfter(
		Action func, int millisecondsDelay, Task? task, ref CancellationTokenSource cancellationTokenSource
	)
	{
		if( task is null )
		{
			return RunAfterAsync(func, millisecondsDelay, task, cancellationTokenSource);
		}

		if( !task.IsCompleted )
		{
			return task;
		}
		
		cancellationTokenSource = new CancellationTokenSource();
		return null;
	}

	private static async Task? RunAfterAsync(
		Action func, int millisecondsDelay, Task? task, CancellationTokenSource cancellationTokenSource
	)
	{
		if( task != null )
			return;

		await Task.Delay(millisecondsDelay, cancellationTokenSource.Token);

		func();
	}
}