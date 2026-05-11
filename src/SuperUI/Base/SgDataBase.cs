using Microsoft.AspNetCore.Components;
using SuperUI.Base;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов работы с данными: DataGrid, TreeView, List, Select.
/// 
/// Обеспечивает:
/// - Загрузка данных (Items или DataSource callback)
/// - Paging встроенный
/// - Sorting встроенный
/// - Filtering встроенный
/// - Loading state
/// - Error state
/// - Empty state
/// - Race-safe загрузка (LifecycleToken)
/// </summary>
public abstract class SgDataBase<TItem> : SgInteractiveBase
{
    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Синхронный источник данных.</summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }

    /// <summary>Асинхронный источник данных с поддержкой paging/sort/filter.</summary>
    [Parameter] public Func<SgDataRequest, CancellationToken, Task<SgDataResult<TItem>>>? DataSource { get; set; }

    [Parameter] public int PageSize { get; set; } = 25;
    [Parameter] public bool EnablePaging { get; set; }
    [Parameter] public bool EnableSorting { get; set; }
    [Parameter] public bool EnableFiltering { get; set; }

    [Parameter] public RenderFragment? EmptyContent { get; set; }
    [Parameter] public RenderFragment? LoadingContent { get; set; }
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────

    protected List<TItem> DisplayItems { get; private set; } = [];
    protected bool IsDataLoading { get; private set; }
    protected Exception? DataError { get; private set; }
    protected int CurrentPage { get; private set; } = 1;
    protected int TotalCount { get; private set; }
    protected SgSortDescriptor? CurrentSort { get; private set; }
    protected List<SgFilterDescriptor> CurrentFilters { get; private set; } = [];

    protected bool HasItems => DisplayItems.Count > 0;
    protected bool IsEmpty => !IsDataLoading && DataError == null && !HasItems;
    protected int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;

    // ── Race-safe загрузка ────────────────────────────────────────────────────

    private int _loadingVersion = 0; // версия загрузки для предотвращения гонок

    /// <summary>
    /// Загрузить данные. Race-safe: отменяет предыдущую загрузку.
    /// </summary>
    protected async Task LoadDataAsync()
    {
        // Инкрементируем версию — это отменяет "устаревшие" ответы
        var version = Interlocked.Increment(ref _loadingVersion);

        IsDataLoading = true;
        DataError = null;
        StateHasChanged();

        try
        {
            List<TItem> result;
            int totalCount;

            if (DataSource != null)
            {
                var request = new SgDataRequest
                {
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Sort = CurrentSort,
                    Filters = CurrentFilters
                };

                var dataResult = await DataSource(request, ComponentToken);

                // Проверяем: не устарел ли ответ?
                if (version != _loadingVersion) return;

                result = dataResult.Items.ToList();
                totalCount = dataResult.TotalCount;
            }
            else if (Items != null)
            {
                // Локальная обработка
                var query = Items.AsQueryable();
                query = ApplyFilters(query);
                query = ApplySort(query);

                totalCount = query.Count();

                if (EnablePaging)
                    query = query.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

                result = query.ToList();
            }
            else
            {
                result = [];
                totalCount = 0;
            }

            if (version != _loadingVersion) return; // гонка

            DisplayItems = result;
            TotalCount = totalCount;
        }
        catch (OperationCanceledException)
        {
            // Нормально: компонент удалён или перезагружен
        }
        catch (Exception ex)
        {
            if (version != _loadingVersion) return;
            DataError = ex;
            Logger.LogError(ex, "[{Id}] Data loading error", ComponentId);
        }
        finally
        {
            if (version == _loadingVersion)
            {
                IsDataLoading = false;
                StateHasChanged();
            }
        }
    }

    // ── Sorting / Filtering ───────────────────────────────────────────────────

    protected async Task SortByAsync(string field)
    {
        if (CurrentSort?.Field == field)
        {
            CurrentSort = CurrentSort.Direction == SgSortDirection.Asc
                ? CurrentSort with { Direction = SgSortDirection.Desc }
                : null;
        }
        else
        {
            CurrentSort = new SgSortDescriptor(field, SgSortDirection.Asc);
        }

        CurrentPage = 1;
        await LoadDataAsync();
    }

    protected async Task FilterAsync(SgFilterDescriptor filter)
    {
        CurrentFilters.RemoveAll(f => f.Field == filter.Field);
        if (!string.IsNullOrEmpty(filter.Value?.ToString()))
            CurrentFilters.Add(filter);

        CurrentPage = 1;
        await LoadDataAsync();
    }

    protected async Task GoToPageAsync(int page)
    {
        CurrentPage = Math.Max(1, Math.Min(page, TotalPages));
        await LoadDataAsync();
    }

    // ── Локальная обработка ───────────────────────────────────────────────────

    protected virtual IQueryable<TItem> ApplyFilters(IQueryable<TItem> query)
    {
        // Базовая реализация — переопределить для конкретной логики
        return query;
    }

    protected virtual IQueryable<TItem> ApplySort(IQueryable<TItem> query)
    {
        if (CurrentSort is null) return query;
        // Переопределить для конкретной логики
        return query;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // Перезагрузить если Items изменился (только для синхронного источника)
        if (Items != null && DataSource == null)
            await LoadDataAsync();
    }
}
