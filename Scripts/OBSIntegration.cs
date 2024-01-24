using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;

namespace Celeste.Mod.izumisQOL.OBS
{
	public class OBSIntegration : Global
	{
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

		private static OBSWebsocket socket = new();

		private static Task CheckRecordingStatusTask;

		public static async void Update()
		{
			if (!ModSettings.OBSWebsocketsEnabled || !IsConnected)	return;
			if (CheckRecordingStatusTask != null)	return;

			CheckRecordingStatusTask = CheckRecordingStatusAsync();
			await CheckRecordingStatusTask;

			CheckRecordingStatusTask = null;
		}

		private static async Task CheckRecordingStatusAsync()
		{
			await Task.Delay(5000);
			IsRecording = GetRecordingState(socket);
		}

		public static void Connect()
		{
			socket ??= new();

			if (!socket.IsConnected)
			{
				try
				{
					socket.ConnectAsync("ws://127.0.0.1:4455", "08xQzVB6ZClMKgmK");
					socket.Connected += OnConnect;
					socket.Disconnected += OnDisconnect;
					socket.RecordStateChanged += OnRecordStateChange;
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
			Log("connected to obs websockets");
			IsConnected = true;
			IsRecording = GetRecordingState(socket);
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
			Log("disconnected from obs websockets");
			socket = null;
			IsConnected = false;
			IsRecording = false;
		}

		private static void OnRecordStateChange(object sender, EventArgs ev)
		{
			IsRecording = GetRecordingState(socket);
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
	}
}
