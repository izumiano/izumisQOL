using System.Collections;

using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.izumisQOL
{
	public class NoClipModule : Global
	{
		public static bool Enabled
		{
			get
			{
				return ModSettings.NoClipEnabled;
			}
			set
			{
				ModSettings.NoClipEnabled = value;
			}
		}

		private static float normalSpeed => ModSettings.NoClipNormalSpeed * 0.25f;
		private static float fastSpeed => ModSettings.NoClipFastSpeed * 0.5f;
		private static float slowSpeed => ModSettings.NoClipSlowSpeed * 0.125f;

		private static int noClipState;

		public static void Load()
		{
			On.Celeste.Player.ctor += OnPlayerCtor;
			On.Celeste.Player.Update += PlayerUpdate;
			On.Celeste.Player.OnTransition += OnPlayerTransition;
			On.Monocle.Collide.Check_Entity_Entity += CheckEntity;
		}

		public static void Unload()
		{
			On.Celeste.Player.ctor -= OnPlayerCtor;
			On.Celeste.Player.Update -= PlayerUpdate;
			On.Monocle.Collide.Check_Entity_Entity -= CheckEntity;
		}

		private static void OnPlayerCtor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
		{
			orig(self, position, spriteMode);

			noClipState = self.StateMachine.AddState<Player>("StNoClip", StateUpdate, StateCoroutine, StateBegin, StateEnd);
		}

		private static void OnPlayerTransition(On.Celeste.Player.orig_OnTransition orig, Player self)
		{
			orig(self);

			if (Engine.Scene is Level level)
			{
				Camera cam = level.Camera;
				camPos = cam.Position;
			}
		}

		private static void PlayerUpdate(On.Celeste.Player.orig_Update orig, Player self)
		{
			orig(self);

			StateMachine stateMachine = self.StateMachine;

			bool isNoClipState = stateMachine.state == noClipState;
			if (Enabled ^ isNoClipState)
			{
				stateMachine.ForceState(isNoClipState ? 0 : noClipState);
			}

			if (ModSettings.ButtonEnableNoClip.Pressed)
			{
				Enabled = !Enabled;
			}
		}

		private static Vector2 camPos;
		private static int StateUpdate(Player player)
		{
			Vector2 aim = Input.Aim.Value * (Input.Grab.Check ? fastSpeed : (Input.Dash.Check ? slowSpeed : normalSpeed));

			player.MoveH(aim.X);
			player.MoveV(aim.Y);

			if (Engine.Scene is Level level)
			{
				Camera cam = level.Camera;

				Vector2 target = player.Position - new Vector2(cam.Viewport.Width / 2f, cam.Viewport.Height / 2f) + aim * 20;
				target.X = MathHelper.Clamp(target.X, level.Bounds.Left, level.Bounds.Right - 320);
				target.Y = MathHelper.Clamp(target.Y, level.Bounds.Top, level.Bounds.Bottom - 180);
				camPos += (target - camPos) * 0.1f;
				cam.Position = camPos;
			}

			return noClipState;
		}

		private static IEnumerator StateCoroutine(Player player)
		{
			yield return 0;
		}

		private static int origDepth;
		private static void StateBegin(Player player)
		{
			Log("StateBegin");
			player.Speed = Vector2.Zero;
			origDepth = player.Depth;
			player.Depth = -1000000000;

			if(Engine.Scene is Level level)
			{
				camPos = level.Camera.Position;
			}
		}

		private static void StateEnd(Player player)
		{
			player.Depth = origDepth;
			Log("StateEnd");
		}

		private static bool CheckEntity(On.Monocle.Collide.orig_Check_Entity_Entity orig, Entity a, Entity b)
		{
			if (!Enabled) return orig(a, b);

			if (a is Player playerA)
			{
				if(playerA.StateMachine.State == noClipState)
				{
					return false;
				}
			}
			if (b is Player playerB)
			{
				if (playerB.StateMachine.State == noClipState)
				{
					return false;
				}
			}
			return orig(a, b);
		}
	}
}
