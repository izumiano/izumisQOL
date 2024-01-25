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
		public enum DisplayType
		{
			WhenRecording,
			WhenNotRecording,
			Either
		}

		private readonly MTexture recordingIcon;
		private readonly MTexture xIcon;

		private static bool IsRecordingOrStreaming => OBSIntegration.IsRecording || OBSIntegration.IsStreaming;
		private static bool ShowIndicator
		{
			get
			{
				return Global.ModSettings.ShowRecordingIndicatorWhen switch 
				{
					DisplayType.WhenRecording => IsRecordingOrStreaming,
					DisplayType.WhenNotRecording => !IsRecordingOrStreaming,
					DisplayType.Either => true,
					_ => false
				};
			}
		}
		private static bool ShouldDrawXIcon
		{
			get
			{
				return Global.ModSettings.ShowRecordingIndicatorWhen switch
				{
					DisplayType.WhenRecording => false,
					DisplayType.WhenNotRecording => true,
					DisplayType.Either => !IsRecordingOrStreaming,
					_ => false
				};
			}
		}

		public static void OnLevelLoad(Level level, Player.IntroTypes playerIntro, bool isFromLoader)
		{
			level.Add(new RecordingIndicator());
		}

		public RecordingIndicator()
		{
			recordingIcon = GFX.Gui["hud/recordingIndicator"];
			xIcon = GFX.Gui["hud/x"];
			Position = new Vector2(Engine.Width - 50f, Engine.Height - 50f);
			Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
		}

		public override void Render()
		{
			base.Render();

			if (ShowIndicator)
			{
				recordingIcon.DrawCentered(Position);
				if(ShouldDrawXIcon) xIcon.DrawCentered(Position);
			}
		}
	}
}
