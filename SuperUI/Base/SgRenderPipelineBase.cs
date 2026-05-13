// SuperUI/Base/SgRenderPipelineBase.cs
// NEW: Базовый класс для Server Streaming Rendering pipeline
// Аналог: FluentUI streaming pipeline
// Поддерживает: IAsyncEnumerable<T> streaming, SSR, InteractiveAuto

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс компонента с полным pipeline для Server Streaming Rendering.
/// Поддерживает <see cref="IAsyncEnumerable{T}"/> источники данных.
/// </summary>
/// <typeparam name="TItem">Тип элемента потока.</typeparam>
public abstract class SgRenderPipelineBase<TItem> : SgInteractiveBase
{
    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Источник данных как IAsyncEnumerable.</summary>
    [Parameter] public IAsyncEnumerable<TItem>? StreamSource { get; set; }

    /// <summary>Шаблон для каждого элемента.</summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>Шаблон загрузки.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Шаблон пустого списка.</summary>
    [Parameter] public RenderFragment? EmptyTemplate { get; set; }

    /// <summary>Шаблон ошибки.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }

    /// <summary>Максимальное количество элементов (0 = без ограничений).</summary>
    [Parameter] public int MaxItems { get; set; } = 0;

    // ── Состояние ─────────────────────────────────────────────────────────────

    protected List<TItem> Items { get; } = new();
    protected bool IsLoading { get; private set; }
    protected bool IsComplete { get; private set; }
    protected Exception? StreamError { get; private set; }
    protected int LoadedCount => Items.Count;

    private CancellationTokenSource? _streamCts;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsInteractive && StreamSource is not null)
            await StartStreamingAsync();
    }

    // ── Streaming ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Запустить потоковую загрузку данных.
    /// В SSR режиме — вызывается сервером до отправки HTML.
    /// В интерактивном режиме — запускается после первого рендера.
    /// </summary>
    protected async Task StartStreamingAsync()
    {
        if (IsDisposed) return;

        // Отменяем предыдущий поток
        _streamCts?.Cancel();
        _streamCts?.Dispose();
        _streamCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);

        Items.Clear();
        IsLoading = true;
        IsComplete = false;
        StreamError = null;

        await InvokeAsync(StateHasChanged);

        try
        {
            var source = StreamSource ?? GetStreamSourceAsync(_streamCts.Token);

            await foreach (var item in source.WithCancellation(_streamCts.Token))
            {
                if (IsDisposed || _streamCts.IsCancellationRequested) break;
                if (MaxItems > 0 && Items.Count >= MaxItems) break;

                Items.Add(item);
                await OnItemReceivedAsync(item);

                // Рендерим каждый новый элемент (Streaming Rendering)
                await InvokeAsync(StateHasChanged);
            }

            IsComplete = true;
            await OnStreamCompleteAsync();
        }
        catch (OperationCanceledException)
        {
            // Нормальная отмена
        }
        catch (Exception ex)
        {
            StreamError = ex;
            Logger.LogError(ex, "[{Id}] Stream error", ComponentId);
            await OnStreamErrorAsync(ex);
        }
        finally
        {
            IsLoading = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Переопределите для предоставления IAsyncEnumerable источника данных.
    /// Используется если <see cref="StreamSource"/> не задан.
    /// </summary>
    protected virtual IAsyncEnumerable<TItem> GetStreamSourceAsync(CancellationToken ct)
        => AsyncEnumerable.Empty<TItem>();

    /// <summary>Вызывается для каждого полученного элемента.</summary>
    protected virtual Task OnItemReceivedAsync(TItem item) => Task.CompletedTask;

    /// <summary>Вызывается по завершению потока.</summary>
    protected virtual Task OnStreamCompleteAsync() => Task.CompletedTask;

    /// <summary>Вызывается при ошибке потока.</summary>
    protected virtual Task OnStreamErrorAsync(Exception ex) => Task.CompletedTask;

    // ── Render ────────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var seq = 0;

        if (StreamError is not null && ErrorTemplate is not null)
        {
            builder.AddContent(seq++, ErrorTemplate(StreamError));
            return;
        }

        if (IsLoading && Items.Count == 0)
        {
            builder.AddContent(seq++, LoadingTemplate ?? DefaultLoadingTemplate);
            return;
        }

        if (IsComplete && Items.Count == 0)
        {
            builder.AddContent(seq++, EmptyTemplate ?? DefaultEmptyTemplate);
            return;
        }

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sg-pipeline");

        foreach (var item in Items)
        {
            builder.AddContent(seq++, ItemTemplate?.Invoke(item) ?? DefaultItemTemplate(item));
        }

        if (IsLoading)
            builder.AddContent(seq++, LoadingTemplate ?? DefaultLoadingTemplate);

        builder.CloseElement();
    }

    protected virtual RenderFragment DefaultLoadingTemplate => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sg-pipeline__loading");
        builder.AddAttribute(2, "aria-busy", "true");
        builder.AddMarkupContent(3, "<span class=\"sg-spinner\" aria-hidden=\"true\"></span>");
        builder.CloseElement();
    };

    protected virtual RenderFragment DefaultEmptyTemplate => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sg-pipeline__empty");
        builder.AddContent(2, "No items.");
        builder.CloseElement();
    };

    protected virtual RenderFragment DefaultItemTemplate(TItem item) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "class", "sg-pipeline__item");
        builder.AddContent(2, item?.ToString());
        builder.CloseElement();
    };

    // ── Dispose ───────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        _streamCts?.Cancel();
        _streamCts?.Dispose();
        await base.DisposeComponentAsync();
    }
}

// Вспомогательный класс для пустого IAsyncEnumerable
internal static class AsyncEnumerable
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
