namespace Celeste.Mod.izumisQOL.EverestInterop;

public class KeybindsResponse(StatusCode statusCode, CelesteStatusCode celesteStatusCode, BindingCollection bindings) : Response(statusCode, celesteStatusCode)
{
	public BindingCollection Bindings { get; } = bindings;
}