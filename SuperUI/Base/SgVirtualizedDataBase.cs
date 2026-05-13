// SuperUI/Base/SgVirtualizedDataBase.cs
// ✅ BUG-7 FIX: ConcurrentDictionary для _prefetchCache
// ✅ PERF-6: IAsyncEnumerable streaming
// ✅ FIX C4: CancelCurrentLoad() — атомарная отмена; IsDisposed проверки

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

public abstract class SgVirtualizedDataBase<TItem> : SgInteractiveBase
{
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public Func<SgDataRequest, Task<SgDataResult<TItem>?>>? LoadData { get; set; }
    [Parameter] public Func<SgDataRequest, IAsyncEnumerable<TItem>>? StreamData { get; set; }
    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public bool EnablePaging { get; set; } = true;
    [Parameter] public bool EnableVirtualization { get; set; } = false;
    [Parameter] public EventCallback<SgDataRequest> OnLoadData { get; set; }

    private readonly List<TItem> _items = [];
    private int _currentPage = 1;
    private int _totalCount;
    private bool _isLoading;
    private string? _loadError;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly ConcurrentDictionary<int, SgDataResult<TItem>> _prefetchCache = new();
    private CancellationTokenSource? _currentLoadCts;

    public IReadOnlyList<TItem> DisplayItems => _items;
    public int TotalCount => _totalCount;
    public int CurrentPage => _currentPage;
    public bool IsLoading => _isLoading;
    public string? LoadError => _loadError;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)_totalCount / PageSize) : 0;
    public bool HasPreviousPage => _currentPage > 1;
    public bool HasNextPage => _currentPage < TotalPages;
    public bool IsEmpty => !_isLoading && _loadError is null && _items.Count == 0;

    public Task GoToFirstPageAsync() => GoToPageAsync(1);
    public Task GoToLastPageAsync() => GoToPageAsync(TotalPages);
    public Task NextPageAsync() => HasNextPage ? GoToPageAsync(_currentPage + 1) : Task.CompletedTask;
    public Task PreviousPageAsync() => HasPreviousPage ? GoToPageAsync(_currentPage - 1) : Task.CompletedTask;

    public async Task GoToPageAsync(int page)
    {
        if (IsDisposed) return;
        if (page < 1 || (TotalPages > 0 && page > TotalPages)) return;

        // ✅ FIX C4: всегда отменяем предыдущую загрузку
        CancelCurrentLoad();

        _currentPage = page;
        await LoadPageAsync(page);
    }

    public async Task ReloadAsync()
    {
        if (IsDisposed) return;

        CancelCurrentLoad();
        _currentPage = 1;
        _items.Clear();
        _prefetchCache.Clear();

        if (LoadData is not null || StreamData is not null)
            await LoadPageAsync(1);
    }

    // ✅ FIX C4: выделенный метод — атомарная отмена с try/catch
    private void CancelCurrentLoad()
    {
        var cts = Interlocked.Exchange(ref _currentLoadCts, null);
        if (cts is not null)
        {
            try { cts.Cancel(); } catch { }
            try { cts.Dispose(); } catch { }
        }
    }

    protected async Task LoadPageAsync(int page)
    {
        if (_prefetchCache.TryRemove(page, out var cached))
        {
            if (IsDisposed) return; // ✅ FIX C4: проверка после кэша

            _items.Clear();
            _items.AddRange(cached.Items);
            _totalCount = cached.TotalCount;
            _currentPage = page;
            await InvokeAsync(StateHasChanged);
            _ = PrefetchNextPageAsync();
            return;
        }

        if (!await _loadLock.WaitAsync(0)) return;

        // ✅ FIX C4: атомарно заменяем CTS и отменяем старый
        var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        var oldCts = Interlocked.Exchange(ref _currentLoadCts, cts);
        if (oldCts is not null)
        {
            try { oldCts.Cancel(); } catch { }
            try { oldCts.Dispose(); } catch { }
        }

        try
        {
            if (IsDisposed) return;

            _isLoading = true;
            _loadError = null;
            await InvokeAsync(StateHasChanged);

            var request = new SgDataRequest
            {
                Page = page,
                PageSize = PageSize,
                CancellationToken = cts.Token
            };

            await OnLoadData.InvokeAsync(request);

            if (StreamData is not null)
            {
                await LoadStreamingAsync(request, cts.Token);
            }
            else if (LoadData is not null)
            {
                var result = await LoadData(request);
                if (result is not null && !cts.Token.IsCancellationRequested && !IsDisposed)
                {
                    _items.Clear();
                    _items.AddRange(result.Items);
                    _totalCount = result.TotalCount;
                    _currentPage = page;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            Logger.LogError(ex, "[{Id}] LoadData error page={Page}", ComponentId, page);
        }
        finally
        {
            _isLoading = false;

            // ✅ FIX C4: безопасный Release
            try { _loadLock.Release(); } catch (ObjectDisposedException) { }

            if (!IsDisposed && !cts.Token.IsCancellationRequested)
                await InvokeAsync(StateHasChanged);

            // ✅ FIX C4: dispose только если CTS всё ещё текущий
            if (Interlocked.CompareExchange(ref _currentLoadCts, null, cts) == cts)
            {
                try { cts.Dispose(); } catch { }
            }
        }
    }

    private async Task LoadStreamingAsync(SgDataRequest request, CancellationToken ct)
    {
        _items.Clear();
        var batchCount = 0;
        const int batchSize = 20;

        await foreach (var item in StreamData!(request).WithCancellation(ct))
        {
            if (IsDisposed || ct.IsCancellationRequested) break; // ✅ FIX C4
            _items.Add(item);
            if (++batchCount % batchSize == 0)
                await InvokeAsync(StateHasChanged);
        }

        _totalCount = _items.Count;
        _currentPage = request.Page;
    }

    protected Task PrefetchNextPageAsync()
    {
        if (LoadData is null || IsDisposed || !HasNextPage) return Task.CompletedTask;

        var nextPage = _currentPage + 1;
        if (_prefetchCache.ContainsKey(nextPage)) return Task.CompletedTask;

        _ = Task.Run(async () =>
        {
            try
            {
                if (IsDisposed) return; // ✅ FIX C4

                var result = await LoadData(new SgDataRequest
                {
                    Page = nextPage,
                    PageSize = PageSize,
                    CancellationToken = ComponentToken
                });

                if (result is not null && !IsDisposed)
                    _prefetchCache.TryAdd(nextPage, result);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "[{Id}] Prefetch page {Page} failed", ComponentId, nextPage);
            }
        }, ComponentToken);

        return Task.CompletedTask;
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        CancelCurrentLoad();
        _prefetchCache.Clear();
        try { _loadLock.Dispose(); } catch { }
        await base.DisposeComponentAsync();
    }
}
