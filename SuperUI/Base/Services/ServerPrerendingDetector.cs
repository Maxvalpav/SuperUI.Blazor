// SuperUI/Base/Services/ServerPrerendingDetector.cs

using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

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
