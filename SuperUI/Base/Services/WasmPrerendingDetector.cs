// ================================================================
// Файл: SuperUI/Base/Services/WasmPrerendingDetector.cs
// ИСПРАВЛЕНО:
// - Убран using Microsoft.AspNetCore.Components.WebAssembly.Hosting
// - Убран дубликат IPrerenderingDetector
// - Используется OperatingSystem.IsBrowser() вместо WebAssembly проверок
// ================================================================

namespace SuperUI.Base.Services;

/// <summary>
/// Detects pre-rendering state in WASM applications.
/// В WASM: prerendering = код выполняется НЕ в браузере (статическая генерация).
/// </summary>
public class WasmPrerendingDetector : IPrerenderingDetector
{
    private readonly Lazy<bool> _isPrerendering;

    /// <summary>
    /// Singleton instance для WASM (stateless).
    /// </summary>
    public static readonly WasmPrerendingDetector Instance = new();

    public bool IsPrerendering => _isPrerendering.Value;

    public bool IsInteractive => !IsPrerendering;

    public WasmPrerendingDetector()
    {
        _isPrerendering = new Lazy<bool>(() =>
        {
            try
            {
                return OperatingSystem.IsBrowser() == false;
            }
            catch
            {
                return true;
            }
        });
    }

    /// <summary>
    /// Determines if the application is currently prerendering.
    /// В .NET 8+ RendererInfo используется для более точного определения.
    /// </summary>
    public static bool DetectPrerendering(RendererInfo? rendererInfo = null)
    {
        if (rendererInfo != null)
            return !rendererInfo.IsInteractive;

        try
        {
            return !OperatingSystem.IsBrowser();
        }
        catch
        {
            return true;
        }
    }
}
