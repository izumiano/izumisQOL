using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using System;

[Tracked(false)]
public class Tooltip : Entity
{
	private readonly string message;

	private float alpha;

	private float unEasedAlpha;

	private readonly float duration;

	private static readonly Queue<TooltipInfo> tooltipQueue = new();

	private Tooltip(string message, float duration = 1f)
	{
		this.message = message;
		this.duration = duration;
		Vector2 messageSize = ActiveFont.Measure(message);
		Position = new Vector2(25f, Engine.Height - messageSize.Y - 12.5f);
		Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
		Add(new Coroutine(Show()));
	}

	private IEnumerator Show()
	{
		while (alpha < 1f)
		{
			unEasedAlpha = Calc.Approach(unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
			alpha = Ease.SineOut(unEasedAlpha);
			yield return null;
		}
		yield return Dismiss();
	}

	private IEnumerator Dismiss()
	{
		yield return duration;
		while (alpha > 0f)
		{
			unEasedAlpha = Calc.Approach(unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
			alpha = Ease.SineIn(unEasedAlpha);
			yield return null;
		}

		if(tooltipQueue.Count > 0)
		{
			Display();
		}

		RemoveSelf();
	}

	public override void Render()
	{
		base.Render();
		ActiveFont.DrawOutline(message, Position, Vector2.Zero, Vector2.One, Color.White * alpha, 2f, Color.Black * alpha * alpha * alpha);
	}

	private static void Display()
	{
		Scene scene = Engine.Scene;
		if (scene != null)
		{
			if (!scene.Tracker.Entities.TryGetValue(typeof(Tooltip), out var tooltips))
			{
				tooltips = scene.Entities.FindAll<Tooltip>().Cast<Entity>().ToList();
			}
			tooltips.ForEach(delegate (Entity entity)
			{
				entity.RemoveSelf();
			});
			TooltipInfo info = tooltipQueue.Dequeue();
			scene.Add(new Tooltip(info.message, info.duration));
		}
	}

	public static void Show(params TooltipInfo[] tooltips)
	{
		if(tooltips.Length == 1)
		{
			Show(tooltips[0].message, tooltips[0].duration);
			return;
		}

		if(tooltips.Length > 0)
		{
			Add(tooltips);
		}
		Display();
	}

	public static void Show(string message, float duration = 1f)
	{
		tooltipQueue.Enqueue(new TooltipInfo(message, duration));
		Display();
	}

	public static void Add(params TooltipInfo[] tooltips)
	{
		foreach (TooltipInfo tooltip in tooltips)
		{
			tooltipQueue.Enqueue(tooltip);
		}
	}

	public static void Add(bool clearQueue = true, params TooltipInfo[] tooltips)
	{
		if (clearQueue)
		{
			tooltipQueue.Clear();
		}
		Add(tooltips);
	}

	public static void Add(string message, bool clearQueue = true, float duration = 1f)
	{
		if (clearQueue)
		{
			tooltipQueue.Clear();
		}
		tooltipQueue.Enqueue(new TooltipInfo(message, duration));
	}
}

public struct TooltipInfo
{
	public string message;
	public float duration;

	public TooltipInfo(string message, float duration = 1f)
	{
		this.message = message;
		this.duration = duration;
	}
}

