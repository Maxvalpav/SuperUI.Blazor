// ================================================================
// Файл: SuperUI/Base/Services/ServerPrerenderingDetector.cs
// ИСПРАВЛЕНО: добавлен using Microsoft.AspNetCore.Http
// ================================================================

using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация IPrerenderingDetector для Blazor Server / Web App Server.
/// Определяет prerendering по IHttpContextAccessor.
/// Требует регистрации: services.AddHttpContextAccessor();
/// </summary>
public sealed class ServerPrerenderingDetector : IPrerenderingDetector, IPrerendingDetector
{
    private readonly IHttpContextAccessor _accessor;

    public ServerPrerenderingDetector(IHttpContextAccessor accessor)
        => _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));

    public bool IsPrerendering =>
        _accessor.HttpContext is { } ctx
        && !ctx.WebSockets.IsWebSocketRequest
        && !ctx.Request.Path.StartsWithSegments("/_blazor");

    public bool IsInteractive => !IsPrerendering;
}
