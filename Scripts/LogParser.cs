using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL.Scripts;

public static class LogParser
{
	public static string Default<T>(T obj)
	{
		return obj?.ToString() ?? "object was null";
	}

	public static string Vector2(Vector2 vector)
	{
		return "x: " + vector.X + "  y: " + vector.Y;
	}

	public static string Array<T>(T[] array)
	{
		return "[" + string.Join(", ", array) + "]";
	}
	
	public static string Array<T>(T[] array, Func<T, string> itemLogParser)
	{
		return "[" + string.Join(", ", array.Select(itemLogParser)) + "]";
	}

	public static string Enumerable<T>(IEnumerable<T> enumerable)
	{
		return "[" + string.Join(", ", enumerable) + "]";
	}

	public static string Enumerable<T>(IEnumerable<T> enumerable, Func<T, string> itemLogParser)
	{
		return "[" + string.Join(", ", enumerable.Select(itemLogParser)) + "]";
	}
}