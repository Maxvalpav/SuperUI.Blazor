// SuperUI/Base/SgStreamingComponentBase.cs — НОВЫЙ
// ✅ Поддержка Streaming Rendering (.NET 8+)
// ✅ Автоматический placeholder при SSR Streaming
// ✅ Адаптивный рендеринг: SSR → Interactive без мерцания

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с нативной поддержкой Streaming Rendering.
/// Автоматически показывает placeholder при SSR Streaming и заменяет
/// его на реальный контент когда данные готовы.
///
/// Иерархия: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase → SgStreamingComponentBase
/// </summary>
public abstract class SgStreamingComponentBase : SgInteractiveBase
{
    // ── Состояние ──────────────────────────────────────────────────────────────

    /// <summary>Компонент находится в процессе Streaming Rendering.</summary>
    protected bool IsStreaming => IsStreamingRendering && !IsInteractive;

    /// <summary>Данные полностью загружены (streaming завершён).</summary>
    protected bool IsContentReady { get; private set; }

    /// <summary>Ошибка загрузки данных.</summary>
    protected Exception? ContentError { get; private set; }

    /// <summary>Состояние загрузки данных.</summary>
    protected SgLoadingState LoadingState { get; private set; } = SgLoadingState.Idle;

    // ── Параметры ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Шаблон-заглушка на время загрузки (SSR Streaming).
    /// Если не задан — используется стандартный скелетон.
    /// </summary>
    [Parameter] public RenderFragment? LoadingPlaceholder { get; set; }

    /// <summary>Шаблон при ошибке загрузки.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>Контент, отображаемый когда данные готовы.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

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

    // ── Render ─────────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        switch (LoadingState)
        {
            case SgLoadingState.Loading:
                // В Streaming SSR placeholder будет заменён сервером по готовности.
                // В интерактивном режиме — показываем спиннер до завершения загрузки.
                builder.AddContent(seq, LoadingPlaceholder ?? DefaultPlaceholder());
                break;

            case SgLoadingState.Error:
                builder.AddContent(seq,
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

            case SgLoadingState.Success:
            default:
                builder.AddContent(seq, ChildContent);
                break;
        }
    }

    // ── Abstract ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Загрузить данные асинхронно.
    /// Вызывается в <see cref="OnInitializedAsync"/> и <see cref="ReloadContentAsync"/>.
    /// В режиме Streaming SSR выполняется на сервере до отправки HTML клиенту.
    /// </summary>
    protected abstract Task LoadContentAsync();

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Стандартный placeholder — скелетон-строка.
    /// Переопределите для кастомного вида.
    /// </summary>
    protected virtual RenderFragment DefaultPlaceholder() =>
        builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "sg-skeleton sg-skeleton--text");
            builder.AddAttribute(2, "style", "height: 1.5rem; width: 100%; margin: 0.5rem 0;");
            builder.AddAttribute(3, "aria-busy", "true");
            builder.AddAttribute(4, "aria-label", "Loading...");
            builder.CloseElement();
        };

    /// <summary>
    /// Принудительно перезагрузить контент (например, по кнопке Retry).
    /// </summary>
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
