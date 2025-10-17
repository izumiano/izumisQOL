using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.izumisQOL;

[Tracked]
public class Tooltip : Entity
{
	public enum DisplayPosition
	{
		BottomLeft,
		TopLeft,
		BottomRight,
		TopRight,
	}

	private readonly string message;

	private float alpha;

	private float unEasedAlpha;

	private readonly float duration;

	private static readonly Queue<Info> tooltipQueue = new();

	private Tooltip(string message, float duration, DisplayPosition position)
	{
		this.message  = message;
		this.duration = duration;
		var messageSize = ActiveFont.Measure(message);
		Position = GetScreenPositionFromDisplayEnum(position, messageSize);
		Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate |
			(int)Tags.TransitionUpdate;
		Add(new Coroutine(Show()));
	}

	private Vector2 GetScreenPositionFromDisplayEnum(DisplayPosition position, Vector2 messageSize)
	{
		return position switch
		{
			DisplayPosition.BottomLeft => new Vector2(25f, Engine.Height - messageSize.Y - 12.5f),
			DisplayPosition.TopLeft    => new Vector2(25f, 12.5f),
			DisplayPosition.BottomRight => new Vector2(Engine.Width - messageSize.X - 25f,
				Engine.Height - messageSize.Y - 12.5f),
			DisplayPosition.TopRight => new Vector2(Engine.Width - messageSize.X - 25f, 12.5f),
			_                        => Vector2.Zero,
		};
	}

	private IEnumerator Show()
	{
		while( alpha < 1f )
		{
			unEasedAlpha = Calc.Approach(unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
			alpha        = Ease.SineOut(unEasedAlpha);
			yield return null;
		}

		yield return Dismiss();
	}

	private IEnumerator Dismiss()
	{
		yield return duration;
		while( alpha > 0f )
		{
			unEasedAlpha = Calc.Approach(unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
			alpha        = Ease.SineIn(unEasedAlpha);
			yield return null;
		}

		if( tooltipQueue.Count > 0 ) Display();

		RemoveSelf();
	}

	public override void Render()
	{
		base.Render();
		ActiveFont.DrawOutline(message, Position, Vector2.Zero, Vector2.One, Color.White * alpha, 2f,
			Color.Black * alpha * alpha * alpha);
	}

	public static void Display()
	{
		MainThreadHelper.Schedule(() =>
		{
			var scene = Engine.Scene;
			if( scene != null )
			{
				if( !scene.Tracker.Entities.TryGetValue(typeof(Tooltip), out var tooltips) )
					tooltips = scene.Entities.FindAll<Tooltip>().Cast<Entity>().ToList();

				tooltips.ForEach(delegate(Entity entity) { entity.RemoveSelf(); });
				var info = tooltipQueue.Dequeue();
				scene.Add(new Tooltip(info.Message, info.Duration, info.Position));
			}
		});
	}

	public static void Show(params Info[] tooltips)
	{
		switch( tooltips.Length )
		{
			case 1:
				Show(tooltips[0].Message, tooltips[0].Duration, tooltips[0].Position);
				return;
			case > 0:
				Add(tooltips);
				break;
		}

		Display();
	}

	public static void Show(string message, float duration = 1f, DisplayPosition position = DisplayPosition.BottomLeft)
	{
		tooltipQueue.Enqueue(new Info(message, duration, position));
		Display();
	}

	public static void Add(params Info[] tooltips)
	{
		foreach( var tooltip in tooltips )
		{
			tooltipQueue.Enqueue(tooltip);
		}
	}

	public static void Add(bool clearQueue = true, params Info[] tooltips)
	{
		if( clearQueue ) tooltipQueue.Clear();
		Add(tooltips);
	}

	public static void Add(
		string message, bool clearQueue = true, float duration = 1f, DisplayPosition position = DisplayPosition.BottomLeft
	)
	{
		if( clearQueue ) tooltipQueue.Clear();
		tooltipQueue.Enqueue(new Info(message, duration, position));
	}

	public struct Info(string message, float duration = 1f, DisplayPosition position = DisplayPosition.BottomLeft)
	{
		public readonly string          Message  = message.Log();
		public readonly DisplayPosition Position = position;
		public readonly float           Duration = duration;
	}
}