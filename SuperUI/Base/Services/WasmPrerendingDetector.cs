namespace SuperUI.Base.Services;

/// <summary>
/// WASM-реализация: никогда не является prerendering.
/// ИСПРАВЛЕНО: переименован из WasmPrerendingDetector.
/// </summary>
public sealed class WasmPrerenderingDetector : IPrerenderingDetector, IPrerendingDetector
{
    public static readonly WasmPrerenderingDetector Instance = new();

    /// <summary>WASM не имеет SSR prerendering → всегда false.</summary>
    public bool IsPrerendering => false;

    public bool IsInteractive => true;
}
