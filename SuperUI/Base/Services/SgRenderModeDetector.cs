// SuperUI/Base/Services/SgRenderModeDetector.cs
// ИСПРАВЛЕНИЯ: полная поддержка .NET 8/9/10

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс детектора режима рендеринга.
/// </summary>
public interface ISgRenderModeDetector
{
    SgRenderMode CurrentMode { get; }
    bool IsPreRendering { get; }
    bool IsInteractive { get; }
    bool IsServer { get; }
    bool IsWebAssembly { get; }
    bool IsAuto { get; }
    bool IsStaticSSR { get; }
    bool SupportsStreaming { get; }
    event Action<SgRenderMode>? RenderModeChanged;
}

/// <summary>
/// Определитель режима рендеринга с поддержкой .NET 8/9/10.
/// </summary>
public sealed class SgRenderModeDetector : ISgRenderModeDetector, IDisposable
{
    private readonly IComponentRenderMode? _assignedMode;
    private SgRenderMode _currentMode;
    private bool _isPreRendering;

    public SgRenderModeDetector(
#if NET9_0_OR_GREATER
        RendererInfo? rendererInfo = null
#else
        IComponentRenderMode? assignedMode = null
#endif
    )
    {
#if NET9_0_OR_GREATER
        _isPreRendering = rendererInfo?.IsInteractive == false;
        _currentMode = DetermineMode(rendererInfo);
#else
        _assignedMode = assignedMode;
        _isPreRendering = assignedMode is null;
        _currentMode = DetermineModeLegacy(assignedMode);
#endif
    }

    public SgRenderMode CurrentMode => _currentMode;
    public bool IsPreRendering => _isPreRendering;

    public bool IsInteractive => _currentMode is SgRenderMode.InteractiveServer
        or SgRenderMode.InteractiveWebAssembly
        or SgRenderMode.InteractiveAuto;

    public bool IsServer => _currentMode == SgRenderMode.InteractiveServer;
    public bool IsWebAssembly => _currentMode == SgRenderMode.InteractiveWebAssembly;
    public bool IsAuto => _currentMode == SgRenderMode.InteractiveAuto;
    public bool IsStaticSSR => _currentMode == SgRenderMode.StaticSSR;

    public bool SupportsStreaming =>
#if NET8_0_OR_GREATER
        _currentMode == SgRenderMode.StaticSSR || _currentMode == SgRenderMode.InteractiveServer;
#else
        false;
#endif

    public event Action<SgRenderMode>? RenderModeChanged;

    /// <summary>
    /// Вызывается при смене режима (InteractiveAuto: Server → WASM).
    /// </summary>
    public void OnRenderModeChanged(SgRenderMode newMode)
    {
        if (_currentMode != newMode)
        {
            var old = _currentMode;
            _currentMode = newMode;
            _isPreRendering = false;
            RenderModeChanged?.Invoke(newMode);
        }
    }

#if NET9_0_OR_GREATER
    private static SgRenderMode DetermineMode(RendererInfo? info)
    {
        if (info is null) return SgRenderMode.StaticSSR;

        return info.Name switch
        {
            "Static" => SgRenderMode.StaticSSR,
            "Server" => SgRenderMode.InteractiveServer,
            "WebAssembly" => SgRenderMode.InteractiveWebAssembly,
            "InteractiveAuto" => SgRenderMode.InteractiveAuto,
            _ => SgRenderMode.Unknown
        };
    }
#else
    private static SgRenderMode DetermineModeLegacy(IComponentRenderMode? mode)
    {
        return mode switch
        {
            InteractiveServerRenderMode => SgRenderMode.InteractiveServer,
            InteractiveWebAssemblyRenderMode => SgRenderMode.InteractiveWebAssembly,
            InteractiveAutoRenderMode => SgRenderMode.InteractiveAuto,
            null => SgRenderMode.StaticSSR,
            _ => SgRenderMode.Unknown
        };
    }
#endif

    public void Dispose()
    {
        RenderModeChanged = null;
    }
}
