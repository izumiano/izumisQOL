using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Monocle;
using Microsoft.Xna.Framework;

using MonoMod.Utils;

namespace Celeste.Mod.izumisQOL
{
	public class SaveStateModule : Global
	{
		private static readonly List<SaveState> saveStates = new();
		private static int _currentState = 0;
		private static int currentState
		{
			get
			{
				return _currentState;
			}
			set
			{
				if (value < 0 || saveStates.Count < 1)
				{
					_currentState = 0;
				}
				else if (value >= saveStates.Count - 1)
				{
					_currentState = saveStates.Count - 1;
				}
				else
				{
					_currentState = value;
				}

				Tooltip.Show($"Selected slot {currentState}");
			}
		}

		private static bool saveStatePressed = false;
		private static bool loadStatePressed = false;

		private static bool frozen = false;

		private static readonly List<Binding> unfreezeInputs = new();
		private static List<Binding> UnfreezeInputs
		{
			get
			{
				Settings settings = Settings.Instance;

				if(unfreezeInputs.Count <= 0)
				{
					unfreezeInputs.Clear();
					unfreezeInputs.Add(settings.Dash);
					unfreezeInputs.Add(settings.DemoDash);
					unfreezeInputs.Add(settings.Down);
					unfreezeInputs.Add(settings.DownDashOnly);
					unfreezeInputs.Add(settings.DownMoveOnly);
					unfreezeInputs.Add(settings.Grab);
					unfreezeInputs.Add(settings.Jump);
					unfreezeInputs.Add(settings.Left);
					unfreezeInputs.Add(settings.LeftDashOnly);
					unfreezeInputs.Add(settings.LeftMoveOnly);
					unfreezeInputs.Add(settings.Pause);
					unfreezeInputs.Add(settings.Right);
					unfreezeInputs.Add(settings.RightDashOnly);
					unfreezeInputs.Add(settings.RightMoveOnly);
					unfreezeInputs.Add(settings.Talk);
					unfreezeInputs.Add(settings.Up);
					unfreezeInputs.Add(settings.UpDashOnly);
					unfreezeInputs.Add(settings.UpMoveOnly);
				}
				return unfreezeInputs;
			}
		}

		public static void Update(On.Celeste.Level.orig_Update orig, Level level)
		{
			if (frozen)
			{
				if (UnfreezeInputs.Any(binding => binding.Pressed(0, 0.5f)))
				{
					FreezeGameplay(level, false);
				}
			}

			orig(level);

			if (!ModSettings.SaveStatesEnabled)
				return;

			if(level is null)
			{
				level.Log("level");
				return;
			}
			if (level.Paused)
				return;

			CheckButtonPresses(level);
		}

		private static void CheckBufferButtonPresses()
		{
			if (ModSettings.ButtonSaveState.Pressed)
			{
				saveStatePressed = true;
			}
			if (ModSettings.ButtonLoadState.Pressed)
			{
				loadStatePressed = true;
			}
		}

		private static void CheckButtonPresses(Level level)
		{
			if (!ModSettings.EnableHotkeys)
				return;

			CheckBufferButtonPresses();

			Camera camera = level.Camera;
			if (camera is null)
			{
				camera.Log("camera");
				return;
			}
			Player player = Engine.Scene.Tracker.GetEntity<Player>();
			if (player is null)
			{
				return;
			}

			DynamicData playerDynData = DynamicData.For(player);

			if (saveStatePressed)
			{
				Tooltip.Add("Saved state to new slot.");
				saveStatePressed = false;

				saveStates.Add
				(
					new SaveState()
					{
						Position				= player.Position,
						PreviousPosition		= player.PreviousPosition,
						CameraPosition			= level.Camera.Position,
						Speed					= player.Speed,
						Stamina					= player.Stamina,
						Dashes					= player.Dashes,
						Facing					= player.Facing,
						DashDir					= player.DashDir,
						PlayerState				= player.StateMachine.State,
						Ducking					= player.Ducking,
						BeforeDashSpeed			= (Vector2)playerDynData.Get("beforeDashSpeed"),
						StarFlyLastDir			= (Vector2)playerDynData.Get("starFlyLastDir"),
						JumpGraceTimer			= (float)playerDynData.Get("jumpGraceTimer"),
						VarJumpSpeed			= (float)playerDynData.Get("varJumpSpeed"),
						VarJumpTimer			= (float)playerDynData.Get("varJumpTimer"),
						DashCooldownTimer		= (float)playerDynData.Get("dashCooldownTimer"),
						DashRefillCooldownTimer = (float)playerDynData.Get("dashRefillCooldownTimer"),
						WallSlideTimer			= (float)playerDynData.Get("wallSlideTimer"),
						ClimbNoMoveTimer		= (float)playerDynData.Get("climbNoMoveTimer"),
						WallSpeedRetentionTimer = (float)playerDynData.Get("wallSpeedRetentionTimer"),
						WallSpeedRetained		= (float)playerDynData.Get("wallSpeedRetained"),
						WallBoostTimer			= (float)playerDynData.Get("wallBoostTimer"),
						MaxFall					= (float)playerDynData.Get("maxFall"),
						DashAttackTimer			= (float)playerDynData.Get("dashAttackTimer"),
						GliderBoostTimer		= (float)playerDynData.Get("gliderBoostTimer"),
						HighestAirY				= (float)playerDynData.Get("highestAirY"),
						StarFlyTimer			= (float)playerDynData.Get("starFlyTimer"),
						StarFlySpeedLerp		= (float)playerDynData.Get("starFlySpeedLerp"),
						WallSlideDir			= (int)playerDynData.Get("wallSlideDir"),
						WallBoostDir			= (int)playerDynData.Get("wallBoostDir"),
						ClimbTriggerDir			= (int)playerDynData.Get("climbTriggerDir"),
						OnGround				= (bool)playerDynData.Get("onGround"),
						WasOnGround				= (bool)playerDynData.Get("wasOnGround"),
						WasDucking				= (bool)playerDynData.Get("wasDucking"),
						WasTired				= (bool)playerDynData.Get("wasTired"),
						DashStartedOnGround		= (bool)playerDynData.Get("dashStartedOnGround"),
						DemoDashed				= (bool)playerDynData.Get("demoDashed"),
						WallBoosting			= (bool)playerDynData.Get("wallBoosting"),
						WasDashB				= (bool)playerDynData.Get("wasDashB"),
						DreamJump				= (bool)playerDynData.Get("dreamJump"),
						StarFlyTransforming		= (bool)playerDynData.Get("starFlyTransforming"),
						StartedDashing			= (bool)playerDynData.Get("StartedDashing")
					}
				);
				currentState++;
			}
			if (loadStatePressed)
			{
				loadStatePressed = false;
				LoadState(player, level);
			}
			if (ModSettings.ButtonNextState.Pressed)
			{
				currentState++;
			}
			if (ModSettings.ButtonPreviousState.Pressed)
			{
				currentState--;
			}
			if (ModSettings.ButtonDeleteState.Pressed)
			{
				if (currentState < saveStates.Count)
				{
					Tooltip.Add($"Deleted save state slot {currentState}");
					saveStates.RemoveAt(currentState);
					currentState--;
				}
			}
		}

		private static void LoadState(Player player, Level level)
		{
			if (currentState < saveStates.Count)
			{
				Tooltip.Show($"Loaded state {currentState}.");
				SaveState saveState = saveStates[currentState];

				DynamicData playerDynData = DynamicData.For(player);

				#region Set Values
				player.Position = saveState.Position;
				player.PreviousPosition = saveState.PreviousPosition;
				level.Camera.Position = saveState.CameraPosition;
				player.Speed = saveState.Speed;
				player.Stamina = saveState.Stamina;
				player.Dashes = saveState.Dashes;
				player.Facing = saveState.Facing;
				playerDynData.Set("onGround", saveState.OnGround);
				playerDynData.Set("wasOnGround", saveState.WasOnGround);
				playerDynData.Set("jumpGraceTimer", saveState.JumpGraceTimer);
				playerDynData.Set("varJumpSpeed", saveState.VarJumpSpeed);
				playerDynData.Set("varJumpTimer", saveState.VarJumpTimer);
				playerDynData.Set("dashCooldownTimer", saveState.DashCooldownTimer);
				playerDynData.Set("dashRefillCooldownTimer", saveState.DashRefillCooldownTimer);
				player.DashDir = saveState.DashDir;
				playerDynData.Set("wallSlideDir", saveState.WallSlideDir);
				playerDynData.Set("wallSlideTimer", saveState.WallSlideTimer);
				playerDynData.Set("climbNoMoveTimer", saveState.ClimbNoMoveTimer);
				playerDynData.Set("wallSpeedRetentionTimer", saveState.WallSpeedRetentionTimer);
				playerDynData.Set("wallSpeedRetained", saveState.WallSpeedRetained);
				playerDynData.Set("wallBoostDir", saveState.WallBoostDir);
				playerDynData.Set("wallBoostTimer", saveState.WallBoostTimer);
				playerDynData.Set("wasDucking", saveState.WasDucking);
				playerDynData.Set("climbTriggerDir", saveState.ClimbTriggerDir);
				playerDynData.Set("maxFall", saveState.MaxFall);
				playerDynData.Set("dashAttackTimer", saveState.DashAttackTimer);
				playerDynData.Set("gliderBoostTimer", saveState.GliderBoostTimer);
				playerDynData.Set("wasTired", saveState.WasTired);
				playerDynData.Set("highestAirY", saveState.HighestAirY);
				playerDynData.Set("dashStartedOnGround", saveState.DashStartedOnGround);
				playerDynData.Set("demoDashed", saveState.DemoDashed);
				playerDynData.Set("wallBoosting", saveState.WallBoosting);
				playerDynData.Set("beforeDashSpeed", saveState.BeforeDashSpeed);
				playerDynData.Set("wasDashB", saveState.WasDashB);
				playerDynData.Set("dreamJump", saveState.DreamJump);
				playerDynData.Set("starFlyTimer", saveState.StarFlyTimer);
				playerDynData.Set("starFlyTransforming", saveState.StarFlyTransforming);
				playerDynData.Set("starFlySpeedLerp", saveState.StarFlySpeedLerp);
				playerDynData.Set("starFlyLastDir", saveState.StarFlyLastDir);
				player.Ducking = saveState.Ducking;
				playerDynData.Set("StartedDashing", saveState.StartedDashing);
				
				player.StateMachine.ForceState(saveState.PlayerState);
				#endregion

				FreezeGameplay(level);
			}
		}

		public static void OnSceneBegin(On.Monocle.Scene.orig_Begin orig, Scene self)
		{
			orig(self);

			ClearStates();
		}

		public static void OnScreenTransition(On.Celeste.Player.orig_OnTransition orig, Player self)
		{
			orig(self);

			ClearStates();
		}

		private static void ClearStates()
		{
			saveStates.Clear();
		}

		private static void FreezeGameplay(Level level, bool freeze = true)
		{
			level.Frozen = freeze;
			frozen = freeze;
		}

		private struct SaveState
		{
			public Vector2 Position, PreviousPosition, CameraPosition, Speed, DashDir, BeforeDashSpeed, StarFlyLastDir;
			public float Stamina, JumpGraceTimer, VarJumpSpeed, VarJumpTimer, DashCooldownTimer, DashRefillCooldownTimer,
				WallSlideTimer, ClimbNoMoveTimer, WallSpeedRetentionTimer, WallSpeedRetained, WallBoostTimer, MaxFall,
				DashAttackTimer, GliderBoostTimer, HighestAirY, StarFlyTimer, StarFlySpeedLerp;
			public int Dashes, WallSlideDir, WallBoostDir, PlayerState, ClimbTriggerDir;
			public bool OnGround, WasOnGround, WasDucking, WasTired, DashStartedOnGround, DemoDashed, WallBoosting,
				WasDashB, DreamJump, StarFlyTransforming, Ducking, StartedDashing;
			public Facings Facing;
		}
	}
}
