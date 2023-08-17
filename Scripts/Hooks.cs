using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Celeste;
using Celeste.Mod.Core;
using Monocle;
using System.IO;

namespace Celeste.Mod.izumisQOL
{
	public class Hooks : Global
	{
		internal static void Load()
		{
			//On.Celeste.Player.Update += Update;
			On.Monocle.Engine.Update += Update;
			On.Celeste.GameplayRenderer.Render += TestClassDraw;

			BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

			KeybindModule.Init();
			BlacklistModule.Init();
		}

		internal static void Unload()
		{
			//On.Celeste.Player.Update -= Update;
			On.Monocle.Engine.Update -= Update;
			On.Celeste.GameplayRenderer.Render -= TestClassDraw;

			izuSettings.ButtonsSwapKeybinds.Clear();
			Log("unload");
		}

		private static void Update(On.Monocle.Engine.orig_Update orig, Engine self, GameTime gameTime)
		{
			orig(self, gameTime);

			KeybindModule.Update();
		}

		private static void TestClassDraw(On.Celeste.GameplayRenderer.orig_Render orig, GameplayRenderer self, Scene scene)
		{
			orig(self, scene);

			Draw.SpriteBatch.Begin();

			
			
			Draw.SpriteBatch.End();
		}
	}
}
