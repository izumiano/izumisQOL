using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Celeste.Mod.izumisQOL.OBS;

namespace Celeste.Mod.izumisQOL.UI
{
	public class Indicator : Entity
	{
		private static readonly List<Indicator> indicators = new();
		public bool IsVisible
		{
			get
			{
				if(ParentIcon is null) return shouldBeVisible();

				return ParentIcon.IsVisible && shouldBeVisible();
			}
		}
		private readonly Func<bool> shouldBeVisible;

		private readonly MTexture iconTexture;

		private readonly int indexInIndicatorList;

		public Indicator ParentIcon = null;
		public Indicator ChildIcon = null;

		public Indicator(string iconName, Func<bool> ShouldBeVisible) : base()
		{
			indexInIndicatorList = indicators.Count;
			indicators.Add(this);

			Position = new Vector2(Engine.Width - 50f, Engine.Height - 50f);
			shouldBeVisible = ShouldBeVisible;
			iconTexture = GFX.Gui["hud/" + iconName];
			Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
		}

		public void AddChild(Indicator child)
		{
			ChildIcon = child;
			ChildIcon.ParentIcon = this;
		}

		public override void Update()
		{
			base.Update();

			if (IsVisible)
				Position.X = GetIndicatorXPosition();
		}

		public override void Render()
		{
			base.Render();

			if(IsVisible) iconTexture.DrawCentered(Position);
		}

		private float GetIndicatorXPosition()
		{
			if (ParentIcon is not null)
				return ParentIcon.Position.X;

			int visibleIcons = 0;
			for (int i = 0; i < indexInIndicatorList; i++)
			{
				if (indicators[i].ParentIcon is null && indicators[i].IsVisible)
					visibleIcons++;
			}

			return Engine.Width - 50f - visibleIcons * 80;
		}

		public static void OnSceneBegin(On.Monocle.Scene.orig_Begin orig, Scene scene)
		{
			orig(scene);

			indicators.Clear();

			new OBSRecordingIndicator();
			new OBSDisconnectedIndicator();

			scene.Add(indicators);
		}

		public static void OnSceneEnd(On.Monocle.Scene.orig_End orig, Scene self)
		{
			orig(self);

			indicators.Clear();
		}
	}

	public class OBSRecordingIndicator : Indicator
	{
		public enum DisplayType
		{
			WhenRecording,
			WhenNotRecording,
			Either
		}

		private static bool IsRecordingOrStreamingOrReplayBuffering => OBSIntegration.IsRecording || OBSIntegration.IsStreaming || OBSIntegration.IsReplayBuffering;
		private static bool ShowIndicator
		{
			get
			{
				if(!Global.ModSettings.OBSIntegrationEnabled || OBSIntegration.SuppressIndicators) return false;

				return Global.ModSettings.ShowRecordingIndicatorWhen switch
				{
					DisplayType.WhenRecording => IsRecordingOrStreamingOrReplayBuffering,
					DisplayType.WhenNotRecording => !IsRecordingOrStreamingOrReplayBuffering,
					DisplayType.Either => true,
					_ => false
				};
			}
		}

		private static bool ShouldShowXIcon
		{
			get
			{
				return Global.ModSettings.ShowRecordingIndicatorWhen switch
				{
					DisplayType.WhenRecording => false,
					DisplayType.WhenNotRecording => !IsRecordingOrStreamingOrReplayBuffering,
					DisplayType.Either => !IsRecordingOrStreamingOrReplayBuffering,
					_ => false
				};
			}
		}

		public OBSRecordingIndicator() : base("recordingIndicator", () => ShowIndicator)
		{
			AddChild(new XIndicator(() => ShouldShowXIcon));
		}
	}

	public class XIndicator : Indicator
	{
		public XIndicator(Func<bool> shouldBeVisible) : base("x", shouldBeVisible) { }
	}

	public class OBSDisconnectedIndicator : Indicator
	{
		public OBSDisconnectedIndicator() : base("disconnectedIndicator", () => !OBSIntegration.IsConnected && Global.ModSettings.OBSIntegrationEnabled || OBSIntegration.SuppressIndicators) { }
	}
}
