using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.izumisQOL.Scripts
{
	public static class LogParser
	{
		public static string Default<T>(T obj)
		{
			return obj.ToString();
		}

		public static string Vector2(Vector2 vector)
		{
			return "x: " + vector.X + "  y: " + vector.Y;
		}
	}
}
