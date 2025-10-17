using System;
using System.Collections.Generic;
using Celeste.Mod.izumisQOL.Obs;
using Microsoft.Xna.Framework;
using Monocle;
using Scene = On.Monocle.Scene;

namespace Celeste.Mod.izumisQOL.UI;

public class Indicator : Entity
{
	protected static readonly List<Indicator> Indicators = [ ];

	public bool IsVisible
	{
		get
		{
			if( ParentIcon is null ) return _shouldBeVisible();

			return ParentIcon.IsVisible && _shouldBeVisible();
		}
	}

	private readonly Func<bool> _shouldBeVisible;

	private readonly MTexture iconTexture;

	private readonly int rowIndex;

	public Indicator? ParentIcon;
	public Indicator? ChildIcon;

	public static void Load()
	{
		_ = new OBSRecordingIndicator();
		_ = new OBSDisconnectedIndicator();
	}

	protected Indicator(string iconName, Func<bool> shouldBeVisible)
	{
		rowIndex = Indicators.Count;
		Indicators.Add(this);

		Position         = new Vector2(Engine.Width - 50f, Engine.Height - 50f);
		_shouldBeVisible = shouldBeVisible;
		iconTexture      = GFX.Gui["hud/" + iconName];
	}


	public void AddChild(Indicator child)
	{
		ChildIcon            = child;
		ChildIcon.ParentIcon = this;
	}

	public override void Update()
	{
		base.Update();

		if( IsVisible )
			Position.X = GetIndicatorXPosition();
	}

	public override void Render()
	{
		base.Render();

		if( IsVisible ) iconTexture.DrawCentered(Position);
	}

	private float GetIndicatorXPosition()
	{
		if( ParentIcon is not null )
			return ParentIcon.Position.X;

		if( rowIndex > Indicators.Count || rowIndex < 0 )
		{
			Log("indexInIndicatorsList was out of out of bounds in indicators", LogLevel.Error);
			return Engine.Width - 50f;
		}

		var visibleIcons = 0;
		try
		{
			for( var i = 0; i < rowIndex; i++ )
			{
				var indicator = Indicators[i];

				if( indicator.ParentIcon is null && indicator.IsVisible )
					visibleIcons++;
			}
		}
		catch( Exception ex )
		{
			Log(ex, LogLevel.Error);
		}

		return Engine.Width - 50f - visibleIcons * 80;
	}

	public static void OnSceneBegin(Scene.orig_Begin orig, Monocle.Scene scene)
	{
		orig(scene);

		Log("Adding indicators");
		Indicators.ForEach(i => i.Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate |
			(int)Tags.TransitionUpdate);
		MainThreadHelper.Schedule(() => { scene.Add(Indicators); });
	}
}

public class OBSRecordingIndicator : Indicator
{
	public enum DisplayType
	{
		WhenRecording,
		WhenNotRecording,
		Either,
	}

	private static bool IsRecordingOrStreamingOrReplayBuffering => OBSIntegration.IsRecording ||
		OBSIntegration.IsStreaming ||
		OBSIntegration.IsReplayBuffering;

	private static bool ShowIndicator
	{
		get
		{
			if( !ModSettings.OBSIntegrationEnabled || OBSIntegration.SuppressIndicators ) return false;

			return ModSettings.ShowRecordingIndicatorWhen switch
			{
				DisplayType.WhenRecording    => IsRecordingOrStreamingOrReplayBuffering,
				DisplayType.WhenNotRecording => !IsRecordingOrStreamingOrReplayBuffering,
				DisplayType.Either           => true,
				_                            => false,
			};
		}
	}

	private static bool ShouldShowXIcon
	{
		get
		{
			return ModSettings.ShowRecordingIndicatorWhen switch
			{
				DisplayType.WhenRecording    => false,
				DisplayType.WhenNotRecording => !IsRecordingOrStreamingOrReplayBuffering,
				DisplayType.Either           => !IsRecordingOrStreamingOrReplayBuffering,
				_                            => false,
			};
		}
	}

	public OBSRecordingIndicator() : base("recordingIndicator", () => ShowIndicator)
	{
		AddChild(new XIndicator(() => ShouldShowXIcon));
	}
}

public class XIndicator(Func<bool> shouldBeVisible) : Indicator("x", shouldBeVisible);

public class OBSDisconnectedIndicator : Indicator
{
	public OBSDisconnectedIndicator() : base("disconnectedIndicator",
		() => !OBSIntegration.IsConnected && ModSettings.OBSIntegrationEnabled && !OBSIntegration.SuppressIndicators) { }
}