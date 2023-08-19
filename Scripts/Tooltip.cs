﻿using System.Collections;
using System.Linq;
using Celeste;
//using Celeste.Mod.SpeedrunTool.Message;
//using Celeste.Mod.SpeedrunTool.SaveLoad;
using Microsoft.Xna.Framework;
using Monocle;
using System;

[Tracked(false)]
public class Tooltip : Entity
{
	private const int Padding = 25;

	private readonly string message;

	private float alpha;

	private float unEasedAlpha;

	private readonly float duration;

	private Tooltip(string message, float duration = 1f)
	{
		this.message = message;
		this.duration = duration;
		Vector2 messageSize = ActiveFont.Measure(message);
		Position = new Vector2(25f, Engine.Height - messageSize.Y - 12.5f);
		Tag = (int)Tags.HUD | (int)Tags.Global | (int)Tags.FrozenUpdate | (int)Tags.PauseUpdate | (int)Tags.TransitionUpdate;
		Convert.ToString((int)Tags.HUD,				 2).Log("HUD");
		Convert.ToString((int)Tags.Global,			 2).Log("Global");
		Convert.ToString((int)Tags.FrozenUpdate,	 2).Log("FrozenUpdate");
		Convert.ToString((int)Tags.PauseUpdate,		 2).Log("PauseUpdate");
		Convert.ToString((int)Tags.TransitionUpdate, 2).Log("TransitionUpdate");
		Add(new Coroutine(Show()));
		//Add(new IgnoreSaveLoadComponent());
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
		RemoveSelf();
	}

	public override void Render()
	{
		base.Render();
		ActiveFont.DrawOutline(message, Position, Vector2.Zero, Vector2.One, Color.White * alpha, 2f, Color.Black * alpha * alpha * alpha);
	}

	public static void Show(string message, float duration = 1f)
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
			scene.Add(new Tooltip(message, duration));
		}
	}
}

