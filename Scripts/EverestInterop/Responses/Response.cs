using Newtonsoft.Json;
using System.Net;
using System;

namespace Celeste.Mod.izumisQOL.EverestInterop;

public enum StatusCode
{
	Ok = 200,
}

public enum CelesteStatusCode{
	Ok = 200,
	Accepted = 202,
	BadRequest = 400,
	InternalServerError = 500,
}

public class Response(StatusCode statusCode, CelesteStatusCode celesteStatusCode)
{
	[JsonIgnore]
	public StatusCode StatusCode { get; } = statusCode;
	public CelesteStatusCode CelesteStatusCode { get; } = celesteStatusCode;
	public string Status { get; } = Enum.GetName(typeof(CelesteStatusCode), celesteStatusCode) ?? "Invalid status code";

	public void Write(HttpListenerContext httpListener)
	{
		DebugRC.Write(this, httpListener);
	}
	
	public void WriteJson(HttpListenerContext httpListener)
	{
		DebugRC.WriteJson(this, httpListener);
	}
	
	public virtual string ToJson()
	{
		return JsonConvert.SerializeObject(this);
	}
}