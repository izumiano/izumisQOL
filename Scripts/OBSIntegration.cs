using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using Celeste.Mod.izumisQOL.UI;
using Monocle;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;

namespace Celeste.Mod.izumisQOL.OBS
{
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
				_IsConnected = value.Log("connected");
			}
		}

		private static bool _IsRecording = false;
		public static bool IsRecording
		{
			get
			{
				return _IsRecording;
			}
			private set
			{
				_IsRecording = value.Log("recording");
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
				_IsStreaming = value.Log("streaming");
			}
		}

		public static int PollOBSFrequency => PollFrequencyMilliseconds[ModSettings.PollFrequencyIndex] / 2;

		public static readonly string[] PollFrequencyText =
		{
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

		public static string HostPort = "localhost:4455";
		public static string Password = "";

		private static bool isFromLaunch = false;

		private static Task ObsPollTask;

		public static void Update()
		{
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
				try
				{
					Tooltip.Show("Connecting...");
					socket.ConnectAsync("ws://" + HostPort, Password);
					socket.Connected += OnConnect;
					socket.Disconnected += OnDisconnect;
					socket.RecordStateChanged += OnRecordStateChange;
					socket.StreamStateChanged += OnStreamStateChange;
				}
				catch (Exception ex)
				{
					Tooltip.Show("Failed Connecting To OBS Websocket");
					Log(ex);
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
				Log(ex);
			}
		}

		private static void OnDisconnect(object sender, ObsDisconnectionInfo ev)
		{
			if (!IsConnected) Tooltip.Show("Failed Connecting To OBS");
			socket = null;
			IsConnected = false;
			IsRecording = false;
		}

		private static Task recordingStatusTask;
		private static CancellationTokenSource recordingStatusCancellationToken = new();
		private static Task streamingStatusTask;
		private static CancellationTokenSource streamingStatusCancellationToken = new();
		private static async Task PollOBSForState()
		{
			if (ModSettings.CheckRecordingStatus)
			{
				await CheckRecordingStatus();
				if (!recordingStatusTask.IsCanceled) IsRecording = GetRecordingState(socket);

				recordingStatusTask = null;
				recordingStatusCancellationToken = new();
			}
			if (ModSettings.CheckStreamingStatus)
			{
				await CheckStreamingStatus();
				if(!streamingStatusTask.IsCanceled) IsStreaming = GetStreamingState(socket);

				streamingStatusTask = null;
				streamingStatusCancellationToken = new();
			}
		}

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
				Log(ex);
				return false;
			}
		}

		private static async Task CheckRecordingStatus()
		{
			if (recordingStatusTask != null)
				return;

			await (recordingStatusTask = Task.Delay(PollOBSFrequency, recordingStatusCancellationToken.Token));
		}

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
				Log(ex);
				return false;
			}
		}

		private static async Task CheckStreamingStatus()
		{
			if (streamingStatusTask != null)
				return;

			await (streamingStatusTask = Task.Delay(PollOBSFrequency, streamingStatusCancellationToken.Token));
		}

		public static void CancelOBSPoll()
		{
			recordingStatusCancellationToken?.Cancel();
			streamingStatusCancellationToken?.Cancel();
		}
	}
}
