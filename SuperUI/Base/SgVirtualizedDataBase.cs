using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с виртуализацией данных.
/// Управляет pagination, lazy-loading, scroll-based loading.
/// </summary>
/// <typeparam name="TItem">Тип элемента данных.</typeparam>
public abstract class SgVirtualizedDataBase<TItem> : SgInteractiveBase
{
    // ── Параметры ────────────────────────────────────────────────────────────
    /// <summary>Статические данные (in-memory).</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Асинхронный провайдер данных (server-side / lazy).</summary>
    [Parameter] public Func<SgDataRequest, Task<SgDataResult<TItem>>>? LoadData { get; set; }

    /// <summary>Количество строк на странице.</summary>
    [Parameter] public int PageSize { get; set; } = 50;

    /// <summary>Включить постраничную навигацию.</summary>
    [Parameter] public bool EnablePaging { get; set; } = true;

    /// <summary>Включить виртуализацию (scroll-based).</summary>
    [Parameter] public bool EnableVirtualization { get; set; } = false;

    /// <summary>Callback при старте загрузки данных.</summary>
    [Parameter] public EventCallback<SgDataRequest> OnLoadData { get; set; }

    // ── Состояние ────────────────────────────────────────────────────────────
    private readonly List<TItem> _items = [];
    private int _currentPage = 1;
    private int _totalCount;
    private bool _isLoading;
    private string? _loadError;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    // П5: Кэш предзагруженных страниц
    private readonly Dictionary<int, SgDataResult<TItem>> _prefetchCache = new();

    // ── Публичные свойства ───────────────────────────────────────────────────
    /// <summary>Отображаемые элементы (после загрузки/фильтрации).</summary>
    public IReadOnlyList<TItem> DisplayItems => _items;

    /// <summary>Общее количество элементов (до пагинации).</summary>
    public int TotalCount => _totalCount;

    /// <summary>Текущая страница (1-based).</summary>
    public int CurrentPage => _currentPage;

    /// <summary>Данные загружаются.</summary>
    public bool IsLoading => _isLoading;

    /// <summary>Текст ошибки последней загрузки (null = нет ошибки).</summary>
    public string? LoadError => _loadError;

    /// <summary>Общее количество страниц.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)_totalCount / PageSize) : 0;

    /// <summary>Есть предыдущая страница.</summary>
    public bool HasPreviousPage => _currentPage > 1;

    /// <summary>Есть следующая страница.</summary>
    public bool HasNextPage => _currentPage < TotalPages;

    /// <summary>Нет данных и нет ошибки.</summary>
    public bool IsEmpty => !_isLoading && _loadError is null && _items.Count == 0;

    // ── Навигация по страницам ───────────────────────────────────────────────
    /// <summary>Перейти на первую страницу.</summary>
    public Task GoToFirstPageAsync() => GoToPageAsync(1);

    /// <summary>Перейти на последнюю страницу.</summary>
    public Task GoToLastPageAsync() => GoToPageAsync(TotalPages);

    /// <summary>Перейти на следующую страницу.</summary>
    public Task NextPageAsync() => HasNextPage
        ? GoToPageAsync(_currentPage + 1)
        : Task.CompletedTask;

    /// <summary>Перейти на предыдущую страницу.</summary>
    public Task PreviousPageAsync() => HasPreviousPage
        ? GoToPageAsync(_currentPage - 1)
        : Task.CompletedTask;

    /// <summary>Перейти на указанную страницу.</summary>
    public async Task GoToPageAsync(int page)
    {
        if (IsDisposed) return;
        if (page < 1 || (TotalPages > 0 && page > TotalPages)) return;
        _currentPage = page;
        await LoadPageAsync(page);
    }

    /// <summary>Перезагрузить с первой страницы.</summary>
    public async Task ReloadAsync()
    {
        if (IsDisposed) return;
        _currentPage = 1;
        _items.Clear();
        if (LoadData is not null)
            await LoadPageAsync(1);
    }

    // ── Внутренняя загрузка ─────────────────────────────────────────────────
    /// <summary>
    /// Загрузить указанную страницу данных.
    /// </summary>
    protected async Task LoadPageAsync(int page)
    {
        if (LoadData is null || IsDisposed) return;

        // П5: Проверить кэш предзагруженных данных
        if (_prefetchCache.TryGetValue(page, out var cached))
        {
            _items.Clear();
            _items.AddRange(cached.Items);
            _totalCount = cached.TotalCount;
            _currentPage = page;
            _prefetchCache.Remove(page);
            await InvokeAsync(StateHasChanged);
            return;
        }

        // Пропустить если уже идёт загрузка (fire-and-forget protection)
        if (!await _loadLock.WaitAsync(0)) return;

        try
        {
            _isLoading = true;
            _loadError = null;
            await InvokeAsync(StateHasChanged);

            // FIX CS0117: убраны поля Skip/Take — они не существуют в SgDataRequest.
            // Используем Page/PageSize; вычисляемые SkipCount/TakeCount доступны как computed.
            var request = new SgDataRequest
            {
                Page = page,
                PageSize = PageSize
                // SkipCount и TakeCount — computed свойства, не нужно задавать
            };

            await OnLoadData.InvokeAsync(request);

            var result = await LoadData(request);

            if (result is not null && !IsDisposed)
            {
                _items.Clear();
                _items.AddRange(result.Items);
                _totalCount = result.TotalCount;
                _currentPage = page;
            }
        }
        catch (OperationCanceledException) { /* нормальная отмена */ }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            // FIX CS1061: Logger доступен из SgComponentBase (унаследован через SgInteractiveBase)
            Logger.LogError(ex, "[{Id}] LoadData error page={Page}", ComponentId, page);
        }
        finally
        {
            _isLoading = false;
            _loadLock.Release();
            if (!IsDisposed)
                await InvokeAsync(StateHasChanged);
        }
    }

    // ── Prefetch оптимизация ─────────────────────────────────────────────────

    /// <summary>
    /// П5: Предзагрузить следующую страницу в фоне.
    /// Вызывайте после загрузки текущей страницы для мгновенного переключения.
    /// </summary>
    protected async Task PrefetchNextPageAsync()
    {
        if (LoadData is null || IsDisposed || !HasNextPage) return;

        var nextPage = _currentPage + 1;

        // Fire-and-forget: не ждём, не меняем UI
        _ = Task.Run(async () =>
        {
            try
            {
                var request = new SgDataRequest
                {
                    Page = nextPage,
                    PageSize = PageSize,
                    CancellationToken = ComponentToken
                };

                // Предзагружаем данные в фоне
                var result = await LoadData(request);

                // Сохраняем для мгновенного показа при переходе
                if (result is not null && !IsDisposed)
                {
                    _prefetchCache[nextPage] = result;
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "[{Id}] Prefetch page {Page} failed", ComponentId, nextPage);
            }
        }, ComponentToken);
    }

    // ── Dispose ──────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        _prefetchCache.Clear();
        _loadLock.Dispose();
        await base.DisposeComponentAsync();
    }
}
