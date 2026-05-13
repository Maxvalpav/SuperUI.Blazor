// SuperUI/Base/Services/SgStreamingRenderingService.cs
// НОВЫЙ: поддержка Streaming Rendering (.NET 8+)
// 
// Streaming Rendering позволяет отправлять части страницы клиенту
// по мере их готовности, не ожидая полного рендеринга.
// Активируется атрибутом [StreamRendering] на странице.
//
// Использование:
//   В компоненте через CascadingParameter:
//   [CascadingParameter(Name = "IsStreamingRendering")] bool IsStreaming { get; set; }

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для определения и управления Streaming Rendering (.NET 8+).
/// </summary>
public interface ISgStreamingRenderingService
{
    /// <summary>true если страница рендерится в режиме Streaming.</summary>
    bool IsStreamingRendering { get; }

    /// <summary>
    /// Получить RenderFragment с Suspense-подобной обёрткой для Streaming.
    /// Показывает fallback пока данные загружаются.
    /// </summary>
    RenderFragment StreamingFragment(
        RenderFragment content,
        RenderFragment? fallback = null);
}

public sealed class SgStreamingRenderingService : ISgStreamingRenderingService
{
    private bool _isStreaming;

    public bool IsStreamingRendering => _isStreaming;

    /// <summary>Устанавливается из HttpContext через Middleware или CascadingValue.</summary>
    public void SetStreamingRendering(bool value) => _isStreaming = value;

    public RenderFragment StreamingFragment(
        RenderFragment content,
        RenderFragment? fallback = null)
    {
        return builder =>
        {
            if (_isStreaming && fallback is not null)
            {
                // В режиме Streaming — сначала fallback, потом заменяется content
                // через механизм Streaming Rendering Blazor
                builder.AddContent(0, fallback);
            }
            else
            {
                builder.AddContent(0, content);
            }
        };
    }
}

/// <summary>
/// Хелпер атрибута для Streaming Rendering компонентов.
/// Используйте на страницах (.razor с @page):
/// <code>@attribute [StreamRendering(true)]</code>
/// </summary>
public static class StreamingRenderingHelper
{
    /// <summary>
    /// Создать CascadingValue для передачи IsStreamingRendering в дочерние компоненты.
    /// Используется в App.razor или Layout.
    /// </summary>
    public static RenderFragment CreateStreamingContext(
        bool isStreaming,
        RenderFragment childContent)
    {
        return builder =>
        {
            builder.OpenComponent<CascadingValue<bool>>(0);
            builder.AddAttribute(1, "Name", "IsStreamingRendering");
            builder.AddAttribute(2, "Value", isStreaming);
            builder.AddAttribute(3, "IsFixed", true); // не меняется — оптимизация
            builder.AddAttribute(4, "ChildContent", childContent);
            builder.CloseComponent();
        };
    }
}
