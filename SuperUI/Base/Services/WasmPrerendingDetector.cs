// SuperUI/Base/Services/WasmPrerendingDetector.cs

namespace SuperUI.Base.Services;

public sealed class WasmPrerenderingDetector : IPrerenderingDetector, IPrerendingDetector
{
    public static readonly WasmPrerenderingDetector Instance = new();
    private WasmPrerenderingDetector() { }

    public bool IsPrerendering => false;
    public bool IsInteractive => true;
}
