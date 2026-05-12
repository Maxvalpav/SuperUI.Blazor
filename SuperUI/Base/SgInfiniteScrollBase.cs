// SuperUI/Base/SgInfiniteScrollBase.cs — НОВЫЙ (П13)
//
// НОВОЕ:
// ✅ Бесконечная прокрутка через Intersection Observer (JS)
// ✅ Lazy-loading данных по мере скролла
// ✅ Поддержка шаблонов для загрузки/конца/ошибки
// ✅ Сброс и перезагрузка
// ✅ JS Interop callback для триггера загрузки

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с бесконечной прокруткой.
/// Загружает данные по мере скролла (Intersection Observer через JS).
/// </summary>
/// <typeparam name="T">Тип элемента данных.</typeparam>
public abstract class SgInfiniteScrollBase<T> : SgJsComponentBase
{
    // ── Параметры ────────────────────────────────────────────────────────────

    /// <summary>Асинхронный провайдер данных. page — 0-based.</summary>
    [Parameter]
    public Func<int, Task<SgInfiniteScrollResult<T>>>? LoadPage { get; set; }

    /// <summary>Количество элементов на страницу.</summary>
    [Parameter] public int BatchSize { get; set; } = 50;

    /// <summary>Расстояние от нижней границы (px) для триггера загрузки.</summary>
    [Parameter] public double Threshold { get; set; } = 200;

    /// <summary>Контент во время загрузки.</summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Контент когда данные закончились.</summary>
    [Parameter] public RenderFragment? EndTemplate { get; set; }

    /// <summary>Контент при ошибке.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorTemplate { get; set; }

    // ── Состояние ────────────────────────────────────────────────────────────

    private readonly List<T> _items = [];
    private int _currentPage;
    private bool _hasMore = true;
    private bool _isLoading;
    private Exception? _error;
    private bool _observerInitialized;
    private DotNetObjectReference<SgInfiniteScrollBase<T>>? _dotNetRef;

    // ── Публичные свойства ──────────────────────────────────────────────────

    /// <summary>Все загруженные элементы.</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>Есть ещё данные для загрузки.</summary>
    public bool HasMore => _hasMore;

    /// <summary>Идёт загрузка.</summary>
    public bool IsLoading => _isLoading;

    /// <summary>Ошибка последней загрузки.</summary>
    public Exception? Error => _error;

    /// <summary>Данные ещё не загружались.</summary>
    public bool IsInitial => _items.Count == 0 && !_isLoading && _error is null;

    /// <summary>Общее количество загруженных элементов.</summary>
    public int LoadedCount => _items.Count;

    // ── Жизненный цикл ──────────────────────────────────────────────────────

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (IsPrerendering) return;

        // Инициализируем Intersection Observer через JS
        _dotNetRef = DotNetObjectReference.Create(this);
        await SafeInvokeVoidAsync("superui.initInfiniteScroll", ComponentId, _dotNetRef, Threshold);
        _observerInitialized = true;

        // Загружаем первую страницу
        await LoadNextPageAsync();
    }

    // ── Публичные методы ────────────────────────────────────────────────────

    /// <summary>Загрузить следующую страницу.</summary>
    public async Task LoadNextPageAsync()
    {
        if (_isLoading || !_hasMore || IsDisposed || LoadPage is null) return;

        _isLoading = true;
        _error = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            var result = await LoadPage(_currentPage);
            if (IsDisposed) return;

            if (result is not null)
            {
                _items.AddRange(result.Items);
                _hasMore = result.HasMore;
                _currentPage++;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] InfiniteScroll load error page={Page}",
                ComponentId, _currentPage);
            _error = ex;
        }
        finally
        {
            _isLoading = false;
            if (!IsDisposed) await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Сбросить и перезагрузить.</summary>
    public async Task ResetAsync()
    {
        if (IsDisposed) return;

        _items.Clear();
        _currentPage = 0;
        _hasMore = true;
        _error = null;
        await InvokeAsync(StateHasChanged);
        await LoadNextPageAsync();
    }

    // ── JS Callback ──────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из JS когда пользователь доскроллил до порога.
    /// </summary>
    [JSInvokable]
    public async Task OnScrollNearEndAsync()
    {
        await LoadNextPageAsync();
    }

    // ── Dispose ──────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        if (IsBrowser && _observerInitialized)
        {
            try
            {
                await SafeInvokeVoidAsync("superui.destroyInfiniteScroll", ComponentId);
            }
            catch { }
        }

        _dotNetRef?.Dispose();
        _dotNetRef = null;

        await base.DisposeComponentAsync();
    }
}

/// <summary>
/// Результат страницы для бесконечной прокрутки.
/// </summary>
public sealed record SgInfiniteScrollResult<T>(
    IReadOnlyList<T> Items,
    bool HasMore);
