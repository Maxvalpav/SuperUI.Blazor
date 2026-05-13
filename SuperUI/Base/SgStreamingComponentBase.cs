// SuperUI/Base/SgStreamingComponentBase.cs
// ✅ .NET 8+ Streaming Rendering из коробки
// ✅ Интеграция с PersistentComponentState через SgPersistentState
// ✅ DeferredContent — skeleton пока данные грузятся
// ✅ Обратная совместимость: LoadContentAsync, ReloadContentAsync, LoadingState, ErrorTemplate

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;
using SuperUI.Base.State;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с нативной поддержкой Streaming Rendering (.NET 8+).
/// Автоматически показывает skeleton при SSR Streaming и заменяет его на контент.
///
/// Использование в Razor:
/// <code>
/// @attribute [StreamRendering]
/// @inherits SgStreamingComponentBase
/// </code>
/// </summary>
public abstract class SgStreamingComponentBase : SgInteractiveBase
{
    [Inject] private PersistentComponentState? PersistentState { get; set; }
    [Inject] protected ISgStreamingRenderingService StreamingService { get; set; } = null!;

    // ── Параметры ──────────────────────────────────────────────────────────
    [Parameter] public RenderFragment? LoadingPlaceholder { get; set; }
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────
    /// <summary>Компонент в режиме Streaming Rendering (данные ещё не готовы).</summary>
    protected bool IsStreaming => StreamingService.IsStreamingRendering && !IsInteractive;

    /// <summary>Данные полностью загружены.</summary>
    protected bool IsContentReady { get; private set; }

    /// <summary>Ошибка загрузки данных.</summary>
    protected Exception? ContentError { get; private set; }

    /// <summary>Состояние загрузки.</summary>
    protected SgLoadingState LoadingState { get; private set; } = SgLoadingState.Idle;

    /// <summary>
    /// Абстракция над PersistentComponentState.
    /// Доступна после OnInitialized.
    /// </summary>
    protected SgPersistentState? AppState { get; private set; }

    // ── Lifecycle ───────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        AppState = new SgPersistentState(PersistentState, Logger);
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        LoadingState = SgLoadingState.Loading;
        ContentError = null;

        try
        {
            await LoadContentAsync();
            IsContentReady = true;
            LoadingState = SgLoadingState.Success;
        }
        catch (OperationCanceledException)
        {
            LoadingState = SgLoadingState.Idle;
        }
        catch (Exception ex)
        {
            ContentError = ex;
            LoadingState = SgLoadingState.Error;
            Logger.LogError(ex, "[{Id}] Streaming content load error", ComponentId);
        }
    }

    // ── Render ──────────────────────────────────────────────────────────────
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        switch (LoadingState)
        {
            case SgLoadingState.Loading:
                builder.AddContent(0, LoadingPlaceholder ?? StreamingSkeleton);
                break;

            case SgLoadingState.Error:
                builder.AddContent(0,
                    ErrorTemplate?.Invoke(ContentError!) ??
                    (RenderFragment)(b =>
                    {
                        b.OpenElement(0, "div");
                        b.AddAttribute(1, "class", "sg-error sg-error--content");
                        b.AddAttribute(2, "role", "alert");
                        b.AddContent(3, $"Error: {ContentError!.Message}");
                        b.CloseElement();
                    }));
                break;

            default:
                builder.AddContent(0, ChildContent);
                break;
        }
    }

    // ── Abstract / Virtual ──────────────────────────────────────────────────

    /// <summary>
    /// Загрузить данные асинхронно.
    /// В режиме Streaming SSR выполняется на сервере до отправки HTML клиенту.
    /// </summary>
    protected virtual Task LoadContentAsync() => Task.CompletedTask;

    /// <summary>
    /// Skeleton-плейсхолдер на время загрузки.
    /// Переопределите для кастомного вида.
    /// </summary>
    protected virtual RenderFragment StreamingSkeleton => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sg-streaming-skeleton");
        builder.AddAttribute(2, "aria-busy", "true");
        builder.AddAttribute(3, "role", "status");

        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "sg-skeleton-pulse");
        builder.AddAttribute(6, "style", "height:1rem;width:60%;margin:.5rem 0");
        builder.CloseElement();

        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "class", "sg-skeleton-pulse");
        builder.AddAttribute(9, "style", "height:1rem;width:80%;margin:.5rem 0");
        builder.CloseElement();

        builder.OpenElement(10, "div");
        builder.AddAttribute(11, "class", "sg-skeleton-pulse");
        builder.AddAttribute(12, "style", "height:1rem;width:40%;margin:.5rem 0");
        builder.CloseElement();

        builder.CloseElement();
    };

    // Обратная совместимость
    protected virtual RenderFragment DefaultPlaceholder() => StreamingSkeleton;

    /// <summary>
    /// Обёртка: показывает skeleton пока данные грузятся, контент — когда готовы.
    /// </summary>
    protected RenderFragment DeferredContent(RenderFragment content) => builder =>
        builder.AddContent(0, !IsContentReady && IsStreaming ? StreamingSkeleton : content);

    // ── Reload ──────────────────────────────────────────────────────────────

    /// <summary>Принудительно перезагрузить контент (например, по кнопке Retry).</summary>
    public async Task ReloadContentAsync()
    {
        if (IsDisposed) return;

        IsContentReady = false;
        ContentError = null;
        LoadingState = SgLoadingState.Loading;
        await InvokeAsync(StateHasChanged);

        try
        {
            await LoadContentAsync();
            IsContentReady = true;
            LoadingState = SgLoadingState.Success;
        }
        catch (OperationCanceledException)
        {
            LoadingState = SgLoadingState.Idle;
        }
        catch (Exception ex)
        {
            ContentError = ex;
            LoadingState = SgLoadingState.Error;
            Logger.LogError(ex, "[{Id}] Streaming content reload error", ComponentId);
        }

        await InvokeAsync(StateHasChanged);
    }
}
