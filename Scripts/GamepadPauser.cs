using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Celeste;
using Monocle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SDL2;

namespace Celeste.Mod.izumisQOL
{
	public class GamepadPauser : Global
	{
		private static int sameCount = 0;

		public static void Update()
		{
			if (!ModSettings.GamepadPauserEnabled)
				return;

			if(Engine.Scene is Level level && level.CanPause)
			{
				if(!HasControllerChanged(out bool controllerConnected))
				{
					sameCount++.Log();
				}
				else
				{
					sameCount = 0;
				}

				if(!controllerConnected || sameCount > ModSettings.PauseAfterFramesGamepadInactive)
				{
					sameCount = 0;
					level.Pause();
				}
			}
		}

		private static Vector2 prevLeftJoy = new();
		private static Vector2 prevRightJoy = new();
		private static bool HasControllerChanged(out bool controllerConnected)
		{
			GamePadState gamepadState = MInput.GamePads[Input.Gamepad].CurrentState;
			GamePadState prevGamepadState = MInput.GamePads[Input.Gamepad].PreviousState;

			controllerConnected = gamepadState.IsConnected;

			Vector2 leftJoystickPosition;
			Vector2 rightJoystickPosition;

			IntPtr intPtr = IntPtr.Zero;
			int i = 0;
			while(intPtr == IntPtr.Zero && i < 500)
			{
				intPtr = SDL.SDL_GameControllerFromInstanceID(i);
				i++;
			}
			//i.Log("i");

			if (intPtr != IntPtr.Zero)
			{
				leftJoystickPosition = new Vector2(SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) / 32767f, SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) / -32767f);
				rightJoystickPosition = new Vector2(SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) / 32767f, SDL.SDL_GameControllerGetAxis(intPtr, SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) / -32767f);
			}
			else
			{
				Log("Couldn't get SDL controller pointer", LogLevel.Warn);
				leftJoystickPosition = gamepadState.ThumbSticks.Left;
				rightJoystickPosition = gamepadState.ThumbSticks.Right;
			}

			if (leftJoystickPosition != prevLeftJoy || rightJoystickPosition != prevRightJoy)
			{
				prevLeftJoy = leftJoystickPosition.Log();
				prevRightJoy = rightJoystickPosition;
				return true;
			}
			prevLeftJoy = leftJoystickPosition.Log();
			prevRightJoy = rightJoystickPosition;

			if (prevGamepadState.Buttons != gamepadState.Buttons)
			{
				Log("buttons");
				return true;
			}
			if (prevGamepadState.DPad != gamepadState.DPad)
			{
				Log("DPad");
				return true;
			}
			if (gamepadState.Triggers.Left != prevGamepadState.Triggers.Left || gamepadState.Triggers.Right != prevGamepadState.Triggers.Right)
			{
				Log("triggers");
				return true;
			}
			if (prevGamepadState.IsConnected != gamepadState.IsConnected)
			{
				Log("connection");
				return true;
			}
			if(leftJoystickPosition == Vector2.Zero && rightJoystickPosition == Vector2.Zero)
			{
				return true;
			}
			return false;
		}
	}
}
