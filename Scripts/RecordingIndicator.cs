using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.izumisQOL.OBS
{
	public class RecordingIndicator : Entity
	{
		private static MTexture texture;
		private static RecordingIndicator instance;

		public static void OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
		{
			instance?.RemoveSelf();

			level.Add(instance = new());
		}

		public RecordingIndicator()
		{
			texture = GFX.Gui["hud/recordingIndicator"];
			Position = new Vector2(Engine.Width - 75f, Engine.Height - 75f);
			Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
			instance = this;
		}

		public override void Render()
		{
			base.Render();

			if (OBSIntegration.IsRecording || OBSIntegration.IsStreaming)
			{
				texture.Draw(Position);
			}
		}
	}
}
