// SuperUI/Base/Services/SgStreamingRenderingService.cs
// ✅ SSR-3 FIX: надёжное определение Streaming Rendering через HttpContext
// ✅ Убрана нестабильная CascadingParameter(Name = "IsStreamingRendering") зависимость
// ✅ WasmStreamingRenderingService — заглушка для WASM (всегда false)

using Microsoft.AspNetCore.Http;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для определения режима Streaming Rendering (.NET 8+).
/// </summary>
public interface ISgStreamingRenderingService
{
    /// <summary>true если текущий запрос выполняется в режиме Streaming Rendering.</summary>
    bool IsStreamingRendering { get; }
}

/// <summary>
/// Server-side реализация: определяет Streaming через Response.HasStarted.
/// Ответ уже начат до завершения рендеринга = streaming mode.
/// </summary>
public sealed class SgStreamingRenderingService : ISgStreamingRenderingService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SgStreamingRenderingService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsStreamingRendering
    {
        get
        {
            var ctx = _httpContextAccessor.HttpContext;
            return ctx is not null && ctx.Response.HasStarted;
        }
    }
}

/// <summary>
/// WASM реализация — никогда не streaming.
/// </summary>
public sealed class WasmStreamingRenderingService : ISgStreamingRenderingService
{
    public static readonly WasmStreamingRenderingService Instance = new();
    public bool IsStreamingRendering => false;
}
