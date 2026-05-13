// SuperUI/Base/Services/SgRenderModeResolver.cs
// NEW: Определение режима рендеринга через сервис (не только CascadingParameter)
// Аналог: FluentUI использует HttpContext для определения среды

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс для определения текущего режима рендеринга компонента.
/// </summary>
public interface ISgRenderModeResolver
{
    /// <summary>true — работаем в браузере (WASM или Blazor Server с JS).</summary>
    bool IsBrowser { get; }

    /// <summary>true — сервер (Server-side или SSR).</summary>
    bool IsServer { get; }

    /// <summary>true — статический SSR (нет интерактивности).</summary>
    bool IsStaticSSR { get; }

    /// <summary>true — InteractiveServer (SignalR).</summary>
    bool IsInteractiveServer { get; }

    /// <summary>true — InteractiveWebAssembly.</summary>
    bool IsInteractiveWebAssembly { get; }

    /// <summary>true — InteractiveAuto.</summary>
    bool IsInteractiveAuto { get; }

    /// <summary>true — идёт prerendering.</summary>
    bool IsPrerendering { get; }

    /// <summary>true — включён Streaming Rendering.</summary>
    bool IsStreamingRendering { get; }

    /// <summary>Имя текущего режима для логов/диагностики.</summary>
    string ModeName { get; }
}

/// <summary>
/// Реализация для WebAssembly.
/// Регистрируется в Program.cs на клиенте.
/// </summary>
public sealed class WasmRenderModeResolver : ISgRenderModeResolver
{
    public bool IsBrowser => true;
    public bool IsServer => false;
    public bool IsStaticSSR => false;
    public bool IsInteractiveServer => false;
    public bool IsInteractiveWebAssembly => true;
    public bool IsInteractiveAuto => false;
    public bool IsPrerendering => false;
    public bool IsStreamingRendering => false;
    public string ModeName => "InteractiveWebAssembly";
}

/// <summary>
/// Реализация для Server-side (определяет prerendering через HttpContext).
/// </summary>
public sealed class ServerRenderModeResolver : ISgRenderModeResolver
{
    private readonly IPrerenderingDetector _prerenderingDetector;
    private readonly IStreamingRenderingService? _streamingService;

    public ServerRenderModeResolver(
        IPrerenderingDetector prerenderingDetector,
        IStreamingRenderingService? streamingService = null)
    {
        _prerenderingDetector = prerenderingDetector;
        _streamingService = streamingService;
    }

    public bool IsBrowser => false;
    public bool IsServer => true;
    public bool IsStaticSSR => !IsPrerendering && _streamingService?.IsStreaming != true;
    public bool IsInteractiveServer => !IsPrerendering;
    public bool IsInteractiveWebAssembly => false;
    public bool IsInteractiveAuto => false;
    public bool IsPrerendering => _prerenderingDetector.IsPrerendering;
    public bool IsStreamingRendering => _streamingService?.IsStreaming ?? false;
    public string ModeName => IsPrerendering ? "Prerendering"
        : IsStreamingRendering ? "StreamingSSR"
        : "InteractiveServer";
}

/// <summary>
/// Реализация для InteractiveAuto (определяет среду динамически).
/// </summary>
public sealed class AutoRenderModeResolver : ISgRenderModeResolver
{
    private readonly IPrerenderingDetector _prerenderingDetector;

    public AutoRenderModeResolver(IPrerenderingDetector prerenderingDetector)
        => _prerenderingDetector = prerenderingDetector;

    public bool IsBrowser => OperatingSystem.IsBrowser();
    public bool IsServer => !IsBrowser;
    public bool IsStaticSSR => false;
    public bool IsInteractiveServer => IsServer && !IsPrerendering;
    public bool IsInteractiveWebAssembly => IsBrowser;
    public bool IsInteractiveAuto => true;
    public bool IsPrerendering => _prerenderingDetector.IsPrerendering;
    public bool IsStreamingRendering => false;
    public string ModeName => IsBrowser ? "InteractiveAuto(WASM)" : "InteractiveAuto(Server)";
}

/// <summary>
/// Вспомогательный интерфейс для Streaming Rendering.
/// </summary>
public interface IStreamingRenderingService
{
    bool IsStreaming { get; }
}
