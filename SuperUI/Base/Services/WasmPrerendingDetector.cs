// SuperUI/Base/Services/WasmPrerendingDetector.cs
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace SuperUI.Base.Services;

/// <summary>
/// Detects pre-rendering state in WASM applications.
/// Pre-rendering in WASM means the component is being rendered
/// on the server during static site generation or SSR before
/// the WASM runtime takes over.
/// </summary>
public class WasmPrerendingDetector : IPrerenderingDetector
{
    private readonly Lazy<bool> _isPrerendering;

    /// <inheritdoc/>
    public bool IsPrerendering => _isPrerendering.Value;

    /// <inheritdoc/>
    public bool IsInteractive => !IsPrerendering;

    public WasmPrerendingDetector()
    {
        // In WASM, prerendering is detected by checking if we're running
        // in the browser context vs the static rendering context.
        _isPrerendering = new Lazy<bool>(() =>
        {
            try
            {
                // If we can access browser APIs, we're interactive
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
    /// In .NET 8+, RendererInfo can be used for more accurate detection.
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

/// <summary>Interface for prerendering detection.</summary>
public interface IPrerenderingDetector
{
    bool IsPrerendering { get; }

    bool IsInteractive { get; }
}
