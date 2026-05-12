using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация IPrerenderingDetector для Blazor Server / Web App Server.
/// Определяет prerendering по IHttpContextAccessor:
/// - prerendering = HTTP запрос БЕЗ WebSocket соединения
/// - interactive = WebSocket или Blazor circuit
/// </summary>
/// <remarks>
/// Требует регистрации IHttpContextAccessor:
/// services.AddHttpContextAccessor();
/// </remarks>
public sealed class ServerPrerenderingDetector : IPrerenderingDetector, IPrerendingDetector
{
    private readonly IHttpContextAccessor _accessor;

    public ServerPrerenderingDetector(IHttpContextAccessor accessor)
        => _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    /// <inheritdoc/>
    /// <remarks>
    /// Prerendering = есть HttpContext И нет WebSocket И не Blazor SignalR путь.
    /// </remarks>
    public bool IsPrerendering =>
        _accessor.HttpContext is { } ctx
        && !ctx.WebSockets.IsWebSocketRequest
        && !ctx.Request.Path.StartsWithSegments("/_blazor");

    /// <inheritdoc/>
    public bool IsInteractive => !IsPrerendering;
}
