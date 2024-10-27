using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using SDL2;

namespace Celeste.Mod.izumisQOL;

public static class GamepadPauser
{
	private static int sameCount;

	private static int SameCount
	{
		get => sameCount;
		set
		{
			if( value > ModSettings.PauseAfterFramesGamepadInactive )
			{
				sameCount = ModSettings.PauseAfterFramesGamepadInactive + 1;
			}
			else
			{
				sameCount = value;
			}
		}
	}

	private static Vector2 prevLeftJoy;
	private static Vector2 prevRightJoy;

	private static bool gamepadInitialized;
	private static bool hasSetInitGamepadState;

	public static void Update()
	{
		if( !ModSettings.GamepadPauserEnabled )
			return;

		GamePadState gamepadState     = MInput.GamePads[Input.Gamepad].CurrentState;
		GamePadState prevGamepadState = MInput.GamePads[Input.Gamepad].PreviousState;

		bool controllerDisconnected = !gamepadState.IsConnected && prevGamepadState.IsConnected;
		if( controllerDisconnected )
		{
			Log("un-init");
			hasSetInitGamepadState = false;
		}

		if( !gamepadState.IsConnected )
		{
			gamepadInitialized = false;
		}

		if( Engine.Scene is not Level { CanPause: true, } level || BetterJournalModule.InJournal )
		{
			return;
		}
		
		if( controllerDisconnected )
		{
			Tooltip.Show("MODOPTIONS_IZUMISQOL_GAMEAPADPAUSE_DISCONNECT_TOOLTIP".AsDialog(), 2);
			SameCount = 0;
			level.Pause();
		}

		if( !gamepadState.IsConnected )
		{
			return;
		}
		
		CheckGamepadInitialization(gamepadState);
		if( !gamepadInitialized )
		{
			return;
		}
		
		if( !HasControllerChanged(gamepadState, prevGamepadState, ref prevLeftJoy, ref prevRightJoy,
			   out bool gotSDLJoystick) )
		{
			SameCount++;
		}
		else
		{
			SameCount = 0;
		}

		if( !gotSDLJoystick )
		{
			gamepadInitialized     = false;
			hasSetInitGamepadState = false;
		}

		if( SameCount > ModSettings.PauseAfterFramesGamepadInactive )
		{
			Tooltip.Show("MODOPTIONS_IZUMISQOL_GAMEAPADPAUSE_FREEZE_TOOLTIP".AsDialog(), 2);
			SameCount = 0;
			level.Pause();
		}
	}

	private static Vector2 initLeftJoystick;
	private static Vector2 initRightJoystick;

	private static void CheckGamepadInitialization(GamePadState gamepadState)
	{
		if( gamepadInitialized )
		{
			return;
		}
		
		if( !hasSetInitGamepadState )
		{
			hasSetInitGamepadState = GetSDLJoysticks(out initLeftJoystick, out initRightJoystick);
			Log("nowa");
		}
		else if( GetSDLJoysticks(out Vector2 l, out Vector2 r) &&
		         HasJoystickChanged(l, r, initLeftJoystick, initRightJoystick) )
		{
			Log("inits");
			gamepadInitialized = true;
		}
	}

	private static bool GetSDLJoysticks(out Vector2 left, out Vector2 right)
	{
		IntPtr intPtr = IntPtr.Zero;
		var    i      = 0;
		while( intPtr == IntPtr.Zero && i < 100 )
		{
			intPtr = SDL.SDL_GameControllerFromInstanceID(i);
			i++;
		}

		if( intPtr != IntPtr.Zero )
		{
			left = new Vector2(
				SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) / 32767f,
				SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) / -32767f);
			right = new Vector2(
				SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) / 32767f,
				SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) / -32767f);
			return left != Vector2.Zero || right != Vector2.Zero;
		}

		Log("Couldn't get SDL gamepad pointer", LogLevel.Warn);
		left  = Vector2.Zero;
		right = Vector2.Zero;
		return false;
	}

	private static bool HasJoystickChanged(
		Vector2 leftJoystickPosition, Vector2 rightJoystickPosition, Vector2 prevLeftJoystick, Vector2 prevRightJoystick
	)
	{
		return leftJoystickPosition != prevLeftJoystick || rightJoystickPosition != prevRightJoystick;
	}

	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	private static bool HasControllerChanged(
		GamePadState gamepadState,      GamePadState prevGamepadState, ref Vector2 prevLeftJoystick,
		ref Vector2  prevRightJoystick, out bool     gotSDLJoystick
	)
	{
		gotSDLJoystick = GetSDLJoysticks(out Vector2 leftJoystickPosition, out Vector2 rightJoystickPosition);
		if( !gotSDLJoystick )
		{
			Log("wrong");
			leftJoystickPosition  = gamepadState.ThumbSticks.Left;
			rightJoystickPosition = gamepadState.ThumbSticks.Right;
		}

		if( HasJoystickChanged(leftJoystickPosition, rightJoystickPosition, prevLeftJoystick, prevRightJoystick) )
		{
			prevLeftJoystick  = leftJoystickPosition;
			prevRightJoystick = rightJoystickPosition;
			return true;
		}

		prevLeftJoystick  = leftJoystickPosition;
		prevRightJoystick = rightJoystickPosition;
		if( prevGamepadState.Buttons != gamepadState.Buttons )
		{
			Log("buttons");
			return true;
		}

		if( prevGamepadState.DPad != gamepadState.DPad )
		{
			Log("DPad");
			return true;
		}

		if( gamepadState.Triggers.Left  != prevGamepadState.Triggers.Left ||
		    gamepadState.Triggers.Right != prevGamepadState.Triggers.Right )
		{
			Log("triggers");
			return true;
		}

		if( prevGamepadState.IsConnected != gamepadState.IsConnected )
		{
			Log("connection");
			return true;
		}

		if( Vector2.DistanceSquared(leftJoystickPosition,  Vector2.Zero) < 1.8627588E-08 &&
		    Vector2.DistanceSquared(rightJoystickPosition, Vector2.Zero) < 1.8627588E-08 )
		{
			Log("joystick at zero");
			return true;
		}

		return false;
	}
}