using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с виртуализацией данных.
/// Управляет pagination, lazy-loading, scroll-based loading.
/// </summary>
/// <typeparam name="TItem">Тип элемента данных.</typeparam>
public abstract class SgVirtualizedDataBase<TItem> : SgInteractiveBase
{
    // ── Параметры ──────────────────────────────────────────────────────────────
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public Func<SgDataRequest, Task<SgDataResult<TItem>>>? LoadData { get; set; }
    [Parameter] public int PageSize { get; set; } = 50;
    [Parameter] public bool EnablePaging { get; set; } = true;
    [Parameter] public bool EnableVirtualization { get; set; } = false;
    [Parameter] public EventCallback<SgDataRequest> OnLoadData { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────────
    private readonly List<TItem> _items = [];
    private int _currentPage = 1;
    private int _totalCount;
    private bool _isLoading;
    private string? _loadError;
    private SgDataRequest? _lastRequest;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    // ── Публичные свойства ─────────────────────────────────────────────────────
    public IReadOnlyList<TItem> DisplayItems => _items;
    public int TotalCount => _totalCount;
    public int CurrentPage => _currentPage;
    public bool IsLoading => _isLoading;
    public string? LoadError => _loadError;
    public int TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)_totalCount / PageSize) : 0;
    public bool HasPreviousPage => _currentPage > 1;
    public bool HasNextPage => _currentPage < TotalPages;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (Items is not null)
        {
            // Локальные данные — без загрузки
            _items.Clear();
            _items.AddRange(Items);
            _totalCount = _items.Count;
        }
    }

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (LoadData is not null && Items is null)
            await LoadPageAsync(_currentPage);
    }

    // ── Публичные методы ───────────────────────────────────────────────────────
    public Task GoToFirstPageAsync() => GoToPageAsync(1);
    public Task GoToLastPageAsync()  => GoToPageAsync(TotalPages);
    public Task NextPageAsync()      => HasNextPage  ? GoToPageAsync(_currentPage + 1) : Task.CompletedTask;
    public Task PreviousPageAsync()  => HasPreviousPage ? GoToPageAsync(_currentPage - 1) : Task.CompletedTask;

    public async Task GoToPageAsync(int page)
    {
        if (page < 1 || (TotalPages > 0 && page > TotalPages)) return;
        _currentPage = page;
        await LoadPageAsync(page);
    }

    public async Task ReloadAsync()
    {
        _currentPage = 1;
        _items.Clear();
        if (LoadData is not null)
            await LoadPageAsync(1);
    }

    // ── Внутренние методы ──────────────────────────────────────────────────────
    protected async Task LoadPageAsync(int page)
    {
        if (LoadData is null || IsDisposed) return;
        if (!await _loadLock.WaitAsync(0)) return; // пропустить если уже грузим

        try
        {
            _isLoading = true;
            _loadError = null;
            StateHasChanged();

            var request = new SgDataRequest
            {
                Page = page,
                PageSize = PageSize,
                Skip = (page - 1) * PageSize,
                Take = PageSize
            };
            _lastRequest = request;
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
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _loadError = ex.Message;
            Logger.LogError(ex, "[{Id}] LoadData error page={Page}", ComponentId, page);
        }
        finally
        {
            _isLoading = false;
            _loadLock.Release();
            if (!IsDisposed) StateHasChanged();
        }
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        _loadLock.Dispose();
        await base.DisposeComponentAsync();
    }
}

