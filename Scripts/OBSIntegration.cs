using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Mod.izumisQOL.UI;
using Monocle;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;

namespace Celeste.Mod.izumisQOL.OBS
{
	public enum RecordingType
	{
		Record,
		Stream,
		ReplayBuffer
	}

	public class OBSIntegration : Global
	{
		private static OBSWebsocket socket = new();

		private static bool _IsConnected = false;
		public static bool IsConnected
		{
			get
			{
				return _IsConnected;
			}
			private set
			{
				WaitingForConnection = false;
				_IsConnected = value.Log("connected");
			}
		}
		public static bool WaitingForConnection { get; set; } = false;

		private static bool _IsRecording = false;
		public static bool IsRecording
		{
			get
			{
				return _IsRecording;
			}
			private set
			{
				_IsRecording = value;
			}
		}

		private static bool _IsStreaming = false;
		public static bool IsStreaming
		{
			get
			{
				return _IsStreaming;
			}
			private set
			{
				_IsStreaming = value;
			}
		}

		private static bool _IsReplayBuffering = false;
		public static bool IsReplayBuffering
		{
			get
			{
				return _IsReplayBuffering;
			}
			private set
			{
				_IsReplayBuffering = value;
			}
		}

		public static bool SuppressIndicators { get; set; } = false;

		public static readonly string[] PollFrequencyText =
		{
			"Never",
			"1 Second",
			"2 Seconds",
			"3 Seconds",
			"4 Seconds",
			"5 Seconds",
			"7 Seconds",
			"10 Seconds",
			"15 Seconds",
			"30 Seconds",
			"1 Minute",
			"2 Minutes",
			"3 Minutes",
			"5 Minutes",
			"7 Minutes",
			"10 Minutes",
			"20 Minutes",
		};

		private static readonly int[] PollFrequencyMilliseconds =
		{
			1_000,
			1_000,
			2_000,
			3_000,
			4_000,
			5_000,
			7_000,
			10_000,
			15_000,
			30_000,
			60_000,
			120_000,
			180_000,
			300_000,
			420_000,
			600_000,
			1_200_000,
		};

		public static string HostPort { get; set; } = "localhost:4455";
		public static string Password { get; set; } = "";

		private static bool isFromLaunch = false;

		private static Task ObsPollTask;

		public static void Update()
		{
			if (ModSettings.ButtonSuppressOBSIndicators.Pressed)
			{
				SuppressIndicators = !SuppressIndicators;
			}

			if (!ModSettings.OBSIntegrationEnabled || !IsConnected) return;

			if (ObsPollTask is not null && !ObsPollTask.IsCompleted) return;

			ObsPollTask = PollOBSForState();
		}

		public static void Connect(bool fromLaunch = false)
		{
			if(!ModSettings.OBSIntegrationEnabled) return;

			socket ??= new();

			isFromLaunch = fromLaunch;

			if (!socket.IsConnected)
			{
				WaitingForConnection = true;
				try
				{
					if(!isFromLaunch) Tooltip.Show("Connecting...");
					socket.ConnectAsync("ws://" + HostPort, Password);
					socket.Connected += OnConnect;
					socket.Disconnected += OnDisconnect;
					socket.RecordStateChanged += OnRecordStateChange;
					socket.StreamStateChanged += OnStreamStateChange;
					socket.ReplayBufferStateChanged += OnReplayBufferStateChange;
				}
				catch (Exception ex)
				{
					if(!isFromLaunch) Tooltip.Show("Failed Connecting To OBS Websocket");
					Log(ex, LogLevel.Error);
				}
			}
		}

		private static void OnConnect(object sender, EventArgs ev)
		{
			if(!isFromLaunch) Tooltip.Show("Connected to OBS!");
			IsConnected = true;
		}

		public static void Disconnect()
		{
			IsConnected = false;
			IsRecording = false;

			if (socket is null) return;

			try
			{
				socket.Connected -= OnConnect;
				socket.Disconnected -= OnDisconnect;
				socket.RecordStateChanged -= OnRecordStateChange;
				socket.Disconnect();
			}
			catch (Exception ex)
			{
				Tooltip.Show("Failed Disconnecting From OBS Websocket");
				Log(ex, LogLevel.Error);
			}
		}

		private static void OnDisconnect(object sender, ObsDisconnectionInfo ev)
		{
			if (!IsConnected && !isFromLaunch) Tooltip.Show("Failed Connecting To OBS");
			socket = null;
			IsConnected = false;
			IsRecording = false;
		}

		private static Task recordingStatusTask;
		private static CancellationTokenSource recordingStatusCancellationToken = new();
		private static Task streamingStatusTask;
		private static CancellationTokenSource streamingStatusCancellationToken = new();
		private static Task replayBufferStatusTask;
		private static CancellationTokenSource replayBufferStatusCancellationToken = new();
		private static async Task PollOBSForState()
		{
			if (ModSettings.CheckRecordingStatus)
			{
				recordingStatusTask = RunAfter(() =>
				{
					IsRecording = GetRecordingState(socket);
				},
				GetPollOBSFrequency(RecordingType.Record), recordingStatusTask, ref recordingStatusCancellationToken);
			}
			await Task.Delay(100);
			if (ModSettings.CheckStreamingStatus)
			{
				streamingStatusTask = RunAfter(() =>
				{
					IsStreaming = GetStreamingState(socket);
				},
				GetPollOBSFrequency(RecordingType.Stream), streamingStatusTask, ref streamingStatusCancellationToken);
			}
			await Task.Delay(100);
			if (ModSettings.CheckReplayBufferStatus)
			{
				replayBufferStatusTask = RunAfter(() =>
				{
					IsReplayBuffering = GetReplayBufferState(socket);
				},
				GetPollOBSFrequency(RecordingType.ReplayBuffer), replayBufferStatusTask, ref replayBufferStatusCancellationToken);
			}
		}

		#region RecordState
		private static void OnRecordStateChange(object sender, EventArgs ev)
		{
			IsRecording = ModSettings.CheckRecordingStatus && GetRecordingState(socket);
		}

		private static bool GetRecordingState(OBSWebsocket socket)
		{
			if (!IsConnected) return false;

			try
			{
				RecordingStatus recStatus = socket.GetRecordStatus();
				return recStatus.IsRecording && !recStatus.IsRecordingPaused;
			}
			catch (Exception ex)
			{
				Log(ex, LogLevel.Error);
				return false;
			}
		}
		#endregion

		#region StreamState
		private static void OnStreamStateChange(object sender, EventArgs ev)
		{
			IsStreaming = ModSettings.CheckStreamingStatus && GetStreamingState(socket);
		}

		private static bool GetStreamingState(OBSWebsocket socket)
		{
			if (!IsConnected)
				return false;

			try
			{
				OutputStatus streamStatus = socket.GetStreamStatus();
				return streamStatus.IsActive;
			}
			catch (Exception ex)
			{
				Log(ex, LogLevel.Error);
				return false;
			}
		}
		#endregion

		#region ReplayBufferState
		private static void OnReplayBufferStateChange(object sender, EventArgs ev)
		{
			IsReplayBuffering = ModSettings.CheckReplayBufferStatus && GetReplayBufferState(socket);
		}

		private static bool GetReplayBufferState(OBSWebsocket socket)
		{
			if (!IsConnected)
				return false;

			try
			{
				return socket.GetReplayBufferStatus();
			}
			catch (Exception ex)
			{
				Log(ex, LogLevel.Error);
				return false;
			}
		}
		#endregion

		private static int GetPollOBSFrequency(RecordingType recordingType)
		{
			return PollFrequencyMilliseconds[ModSettings.OBSPollFrequencyIndex[recordingType]];
		}

		public static void CancelOBSPoll()
		{
			recordingStatusCancellationToken?.Cancel();
			streamingStatusCancellationToken?.Cancel();
			replayBufferStatusCancellationToken?.Cancel();
		}

		public static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self)
		{
			orig(self);

			SuppressIndicators = false;
		}
	}
}
