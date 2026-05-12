// SuperUI/Base/Services/IPrerenderingDetector.cs
//
// Детектор статического prerendering (SSR).
// На Server-side prerender JS недоступен — это нужно проверять перед JS Interop.
//
// WASM: IsPrerendering всегда false (нет SSR).
// Server: IsPrerendering = true во время первого статического рендера.
// Blazor Web App (Auto): IsPrerendering = true на server-prerender фазе.

namespace SuperUI.Base.Services;

/// <summary>
/// Детектор режима prerendering (статический SSR без JS).
/// </summary>
public interface IPrerendingDetector
{
    /// <summary>
    /// true — компонент рендерится в режиме статического SSR (JS недоступен).
    /// false — компонент работает в интерактивном режиме (JS доступен).
    /// </summary>
    bool IsPrerendering { get; }
}

/// <summary>
/// Реализация для Blazor Server / Web App через IHttpContextAccessor.
/// </summary>
internal sealed class ServerPrerendingDetector : IPrerendingDetector
{
    private readonly Microsoft.AspNetCore.Http.IHttpContextAccessor _httpContextAccessor;

    public ServerPrerendingDetector(
        Microsoft.AspNetCore.Http.IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public bool IsPrerendering
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx is null) return false;
            // В SignalR-circuit HttpContext есть, но WebSocket уже установлен
            return !ctx.WebSockets.IsWebSocketRequest
                && !ctx.Request.Headers.ContainsKey("Upgrade");
        }
    }
}

/// <summary>
/// Реализация для Blazor WebAssembly — prerendering всегда false.
/// </summary>
internal sealed class WasmPrerendingDetector : IPrerendingDetector
{
    /// <inheritdoc />
    public bool IsPrerendering => false;
}
