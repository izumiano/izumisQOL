using System.Collections.Generic;
using System.Net;

namespace Celeste.Mod.izumisQOL.EverestInterop;

public static class DebugRC
{
	private const string PathPrefix = "/izumisQOL";

	private static readonly RCEndPoint getKeybindsEndPoint = new()
	{
		Path = $"{PathPrefix}/getKeybinds",
		Name = "Get Keybind Info",
		Handle = httpListener =>
		{
			httpListener.Response.AddHeader("Access-Control-Allow-Origin", "*");
			
			new KeybindsResponse(statusCode: StatusCode.Ok, celesteStatusCode: CelesteStatusCode.Ok, bindings: KeybindViewer.GetBindInfo())
				.WriteJson(httpListener);
		}
	};

	private static readonly List<RCEndPoint> endPoints =
		[ getKeybindsEndPoint, ];

	public static void Load()
	{
		foreach (RCEndPoint endPoint in endPoints)
		{
			Everest.DebugRC.EndPoints.Add(endPoint);
		}
	}

	public static void Unload() 
	{
		foreach (RCEndPoint endPoint in endPoints)
		{
			Everest.DebugRC.EndPoints.Remove(endPoint);
		}
	}

	public static void WriteJson(Response response, HttpListenerContext httpListener)
	{
		httpListener.Response.ContentType = "application/json";
		
		Write(response, httpListener);
	}

	public static void Write(Response response, HttpListenerContext httpListener)
	{
		httpListener.Response.StatusCode = (int)response.StatusCode;
		string responseString = response.ToJson();
			
		Everest.DebugRC.Write(httpListener, responseString);
	}
}