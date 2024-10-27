using System;
using System.Threading;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;
using OBSWebsocketDotNet.Types.Events;

namespace Celeste.Mod.izumisQOL.Obs;
public enum RecordingType
{
	Record,
	Stream,
	ReplayBuffer,
}

public static class OBSIntegration
{
	private static readonly OBSWebsocket _socket = new();

	private static bool isConnected;
	public static bool IsConnected
	{
		get => isConnected;
		private set
		{
			WaitingForConnection = false;
			isConnected = value.Log("connected");
		}
	}
	public static bool WaitingForConnection { get; set; }

	public static  bool IsRecording { get; private set; }

	public static  bool IsStreaming { get; private set; }

	public static  bool IsReplayBuffering { get; private set; }

	public static bool SuppressIndicators { get; set; }

	public static readonly string[] PollFrequencyText =
	[
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
	];

	private static readonly int[] PollFrequencyMilliseconds =
	[
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
	];

	public static string HostPort { get; set; } = "localhost:4455";
	public static string Password { get; set; } = "";

	private static bool isFromLaunch;

	private static Task? obsPollTask;

	public static void Update()
	{
		if (ModSettings.ButtonSuppressOBSIndicators.Pressed)
		{
			SuppressIndicators = !SuppressIndicators;
		}

		if (!ModSettings.OBSIntegrationEnabled || !IsConnected) return;

		if (obsPollTask is not null && !obsPollTask.IsCompleted) return;

		obsPollTask = PollObsForState();
	}

	public static void Connect(bool fromLaunch = false)
	{
		if(!ModSettings.OBSIntegrationEnabled) return;

		isFromLaunch = fromLaunch;

		if( _socket.IsConnected ) return;
		
		WaitingForConnection = true;
		try
		{
			if(!isFromLaunch) Tooltip.Show("Connecting...");
			_socket.ConnectAsync("ws://" + HostPort, Password);
			_socket.Connected                += OnConnect;
			_socket.Disconnected             += OnDisconnect;
			_socket.RecordStateChanged       += OnRecordStateChange;
			_socket.StreamStateChanged       += OnStreamStateChange;
			_socket.ReplayBufferStateChanged += OnReplayBufferStateChange;
		}
		catch (Exception ex)
		{
			if(!isFromLaunch) Tooltip.Show("Failed Connecting To OBS Websocket");
			Log(ex, LogLevel.Error);
		}
	}

	private static void OnConnect(object? sender, EventArgs ev)
	{
		if(!isFromLaunch) Tooltip.Show("Connected to OBS!");
		IsConnected = true;
	}

	public static void Disconnect()
	{
		IsConnected = false;
		IsRecording = false;

		try
		{
			_socket.Connected -= OnConnect;
			_socket.Disconnected -= OnDisconnect;
			_socket.RecordStateChanged -= OnRecordStateChange;
			_socket.Disconnect();
		}
		catch (Exception ex)
		{
			Tooltip.Show("Failed Disconnecting From OBS Websocket");
			Log(ex, LogLevel.Error);
		}
	}

	private static void OnDisconnect(object? sender, ObsDisconnectionInfo ev)
	{
		if (!IsConnected && !isFromLaunch) Tooltip.Show("Failed Connecting To OBS");
		IsConnected = false;
		IsRecording = false;
		CancelObsPoll();
	}

	private static Task?                    recordingStatusTask;
	private static CancellationTokenSource recordingStatusCancellationToken = new();
	private static Task?                    streamingStatusTask;
	private static CancellationTokenSource streamingStatusCancellationToken = new();
	private static Task?                    replayBufferStatusTask;
	private static CancellationTokenSource replayBufferStatusCancellationToken = new();
	private static async Task? PollObsForState()
	{
		if (ModSettings.CheckRecordingStatus)
		{
			recordingStatusTask = RunAfter(() =>
			{
				IsRecording = GetRecordingState(_socket);
			},
			GetPollObsFrequency(RecordingType.Record), recordingStatusTask, ref recordingStatusCancellationToken);
		}
		await Task.Delay(100);
		if (ModSettings.CheckStreamingStatus)
		{
			streamingStatusTask = RunAfter(() =>
			{
				IsStreaming = GetStreamingState(_socket);
			},
			GetPollObsFrequency(RecordingType.Stream), streamingStatusTask, ref streamingStatusCancellationToken);
		}
		await Task.Delay(100);
		if (ModSettings.CheckReplayBufferStatus)
		{
			replayBufferStatusTask = RunAfter(() =>
			{
				IsReplayBuffering = GetReplayBufferState(_socket);
			},
			GetPollObsFrequency(RecordingType.ReplayBuffer), replayBufferStatusTask, ref replayBufferStatusCancellationToken);
		}
	}

	#region RecordState
	private static void OnRecordStateChange(object? sender, RecordStateChangedEventArgs recordStateChangedEventArgs)
	{
		IsRecording = ModSettings.CheckRecordingStatus && GetRecordingState(_socket);
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
	private static void OnStreamStateChange(object? sender, StreamStateChangedEventArgs streamStateChangedEventArgs)
	{
		IsStreaming = ModSettings.CheckStreamingStatus && GetStreamingState(_socket);
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
	private static void OnReplayBufferStateChange(object? sender, ReplayBufferStateChangedEventArgs replayBufferStateChangedEventArgs)
	{
		IsReplayBuffering = ModSettings.CheckReplayBufferStatus && GetReplayBufferState(_socket);
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

	private static int GetPollObsFrequency(RecordingType recordingType)
	{
		return PollFrequencyMilliseconds[ModSettings.OBSPollFrequencyIndex[recordingType]];
	}

	public static void CancelObsPoll()
	{
		recordingStatusCancellationToken.Cancel();
		streamingStatusCancellationToken.Cancel();
		replayBufferStatusCancellationToken.Cancel();
	}

	public static void OnLevelBegin(On.Celeste.Level.orig_Begin orig, Level self)
	{
		orig(self);

		SuppressIndicators = false;
	}
}
