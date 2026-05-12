using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация для Blazor Server / Web App.
/// Определяет prerendering по наличию активного HTTP-запроса (не WebSocket).
///
/// ИСПРАВЛЕНО: переименован из ServerPrerendingDetector (опечатка устранена).
/// </summary>
public sealed class ServerPrerenderingDetector : IPrerendingDetector, IPrerenderingDetector
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerPrerenderingDetector(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    /// <summary>
    /// true — активный HTTP-запрос без WebSocket = prerendering.
    /// false — WebSocket (интерактивный SignalR) или нет контекста (WASM).
    /// </summary>
    public bool IsPrerendering
        => _httpContextAccessor.HttpContext is { } ctx
           && !ctx.WebSockets.IsWebSocketRequest;

    public bool IsInteractive => !IsPrerendering;
}
