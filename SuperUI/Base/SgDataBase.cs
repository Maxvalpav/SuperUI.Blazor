// SuperUI/Base/SgDataBase.cs
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов работы с данными.
///
/// ИСПРАВЛЕНИЯ:
/// 1. _loadingVersion: volatile для видимости между потоками
/// 2. OnParametersSetAsync: не перезагружает при каждом изменении параметра
/// 3. Items сравнение: отслеживание по ReferenceEquals
/// 4. LoadDataAsync: finally блок корректно обновляет флаги
/// </summary>
public abstract class SgDataBase<TItem> : SgInteractiveBase
{
    // ── Параметры ─────────────────────────────────────────────────────────────
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    [Parameter] public Func<SgDataRequest, CancellationToken, ValueTask<SgDataResult<TItem>>>? DataSource { get; set; }
    [Parameter] public int  PageSize         { get; set; } = 25;
    [Parameter] public bool EnablePaging     { get; set; }
    [Parameter] public bool EnableSorting    { get; set; }
    [Parameter] public bool EnableFiltering  { get; set; }
    [Parameter] public RenderFragment?              EmptyContent   { get; set; }
    [Parameter] public RenderFragment?              LoadingContent { get; set; }
    [Parameter] public RenderFragment<Exception>?   ErrorContent   { get; set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    protected List<TItem>        DisplayItems    { get; private set; } = [];
    protected bool               IsDataLoading   { get; private set; }
    protected Exception?         DataError       { get; private set; }
    protected int                CurrentPage     { get; private set; } = 1;
    protected int                TotalCount      { get; private set; }
    protected SgSortDescriptor?  CurrentSort     { get; private set; }
    protected List<SgFilterDescriptor> CurrentFilters { get; private set; } = [];

    protected bool HasItems  => DisplayItems.Count > 0;
    protected bool IsEmpty   => !IsDataLoading && DataError == null && !HasItems;
    protected int  TotalPages => PageSize > 0
        ? (int)Math.Ceiling((double)TotalCount / PageSize)
        : 1;

    // ИСПРАВЛЕНО: volatile для видимости между потоками
    private volatile int _loadingVersion;

    // ИСПРАВЛЕНО: отслеживание предыдущего Items для предотвращения лишних загрузок
    private IEnumerable<TItem>? _lastItems;
    private int _lastPageSize;

    // ── Race-safe загрузка ────────────────────────────────────────────────────
    protected async Task LoadDataAsync()
    {
        var version = Interlocked.Increment(ref _loadingVersion);
        IsDataLoading = true;
        DataError     = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            List<TItem> result;
            int totalCount;

            if (DataSource != null)
            {
                var request = new SgDataRequest
                {
                    Page     = CurrentPage,
                    PageSize = PageSize,
                    Sort     = CurrentSort,
                    Filters  = CurrentFilters
                };

                var dataResult = await DataSource(request, ComponentToken);

                if (version != _loadingVersion) return;

                result     = dataResult.Items.ToList();
                totalCount = dataResult.TotalCount;
            }
            else if (Items != null)
            {
                var query = Items.AsQueryable();
                query      = ApplyFilters(query);
                query      = ApplySort(query);
                totalCount = query.Count();
                if (EnablePaging)
                    query  = query.Skip((CurrentPage - 1) * PageSize).Take(PageSize);
                result     = query.ToList();
            }
            else
            {
                result     = [];
                totalCount = 0;
            }

            if (version != _loadingVersion) return;

            DisplayItems = result;
            TotalCount   = totalCount;
        }
        catch (OperationCanceledException)
        {
            // Нормально: dispose или перезагрузка
        }
        catch (Exception ex)
        {
            if (version != _loadingVersion) return;
            DataError = ex;
            Logger.LogError(ex, "[{Id}] Data loading error", ComponentId);
        }
        finally
        {
            // ИСПРАВЛЕНО: обновляем IsDataLoading только для актуальной версии
            if (version == _loadingVersion)
            {
                IsDataLoading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // ── Sorting / Filtering ───────────────────────────────────────────────────
    protected async Task SortByAsync(string field)
    {
        CurrentSort = CurrentSort?.Field == field
            ? (CurrentSort.Direction == SgSortDirection.Asc
                ? CurrentSort with { Direction = SgSortDirection.Desc }
                : null)
            : new SgSortDescriptor(field, SgSortDirection.Asc);
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

    protected virtual IQueryable<TItem> ApplyFilters(IQueryable<TItem> query) => query;
    protected virtual IQueryable<TItem> ApplySort(IQueryable<TItem>    query) => query;

    // ── Lifecycle — ИСПРАВЛЕНО ────────────────────────────────────────────────
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        // ИСПРАВЛЕНО: перезагружаем только при реальном изменении Items или PageSize
        var itemsChanged  = !ReferenceEquals(Items, _lastItems);
        var pageSizeChanged = PageSize != _lastPageSize;

        if ((itemsChanged || pageSizeChanged) && Items != null && DataSource == null)
        {
            _lastItems    = Items;
            _lastPageSize = PageSize;
            await LoadDataAsync();
        }
    }
}
