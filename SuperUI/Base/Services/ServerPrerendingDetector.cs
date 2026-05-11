using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация для Blazor Server / Web App.
/// Определяет prerendering по наличию активного HTTP запроса.
/// В интерактивном режиме HttpContext = null или это WebSocket.
/// </summary>
public sealed class ServerPrerendingDetector : IPrerendingDetector
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ServerPrerendingDetector(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public bool IsPrerendering
        => _httpContextAccessor.HttpContext is not null
           && !_httpContextAccessor.HttpContext.WebSockets.IsWebSocketRequest;

    public bool IsInteractive => !IsPrerendering;
}
