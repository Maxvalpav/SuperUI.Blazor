// SuperUI/Base/Services/SgStreamingRenderingService.cs
// ИСПРАВЛЕНИЯ: добавлен интерфейс и полная реализация

using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс сервиса потокового рендеринга.
/// </summary>
public interface ISgStreamingRenderingService
{
    bool IsStreaming { get; }
    bool IsSupported { get; }
    event Action? StreamingCompleted;
    void NotifyStreamingCompleted();
}

/// <summary>
/// Server-side реализация потокового рендеринга (.NET 8+).
/// </summary>
public sealed class SgStreamingRenderingService : ISgStreamingRenderingService
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public bool IsStreaming => _httpContextAccessor?.HttpContext is not null;
    public bool IsSupported => true;
    public event Action? StreamingCompleted;

    public SgStreamingRenderingService(IHttpContextAccessor? httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void NotifyStreamingCompleted()
    {
        StreamingCompleted?.Invoke();
    }
}

/// <summary>
/// WASM-заглушка: потоковый рендеринг не поддерживается на WASM.
/// </summary>
public sealed class WasmStreamingRenderingService : ISgStreamingRenderingService
{
    public static readonly WasmStreamingRenderingService Instance = new();

    public bool IsStreaming => false;
    public bool IsSupported => false;
    public event Action? StreamingCompleted { add { } remove { } }

    public void NotifyStreamingCompleted() { }
}
