namespace SuperUI.Services;

/// <summary>
/// Реализация для WASM.
/// В WASM prerendering невозможен — всегда интерактивный режим.
/// </summary>
public sealed class WasmPrerendingDetector : IPrerendingDetector
{
    public bool IsPrerendering => false;
    public bool IsInteractive => true;
}
