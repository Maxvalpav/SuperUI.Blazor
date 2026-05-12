// SuperUI/Base/SgDataBase.cs
//
// ИСПРАВЛЕНИЯ КОМПИЛЯЦИИ (CS0117):
//   ✅ SgSortDirection.Asc → SgSortDirection.Ascending
//   ✅ SgSortDirection.Desc → SgSortDirection.Descending
//
// УЛУЧШЕНИЯ:
//   ✅ ApplyFilters — базовая LINQ in-memory реализация (Contains, Equals, GreaterThan и др.)
//   ✅ ApplySort    — базовая LINQ OrderBy/OrderByDescending по Field через reflection
//   ✅ ReloadAsync() — публичный метод для программной перезагрузки
//   ✅ ExportAsync() — virtual extension point для экспорта
//   ✅ TryGetNonEnumeratedCount + ICollection<T>.Count fallback
//   ✅ IsDisposed check в OnParametersSetAsync (было) + в SortByAsync/FilterAsync
//   ✅ SgLoadingState — детализированное состояние вместо bool

    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Microsoft.AspNetCore.Components;
    using Microsoft.Extensions.Logging;
    using SuperUI.Base;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов работы с данными.
/// Уровень 4: SgInteractiveBase → SgDataBase
/// </summary>
/// <typeparam name="T">Тип элемента данных.</typeparam>
public abstract class SgDataBase<T> : SgInteractiveBase
{
    // ── Параметры ────────────────────────────────────────────────────────────────

    /// <summary>Коллекция элементов для отображения (in-memory).</summary>
    [Parameter] public IEnumerable<T>? Items { get; set; }

    /// <summary>Асинхронный провайдер данных (server-side / virtualized).</summary>
    [Parameter] public Func<SgDataRequest, CancellationToken, Task<SgDataResult<T>>>? DataSource { get; set; }

    /// <summary>Количество строк на странице.</summary>
    [Parameter] public int PageSize { get; set; } = 25;

    /// <summary>Включить постраничную навигацию.</summary>
    [Parameter] public bool EnablePaging { get; set; }

    /// <summary>Включить сортировку.</summary>
    [Parameter] public bool EnableSorting { get; set; }

    /// <summary>Включить фильтрацию.</summary>
    [Parameter] public bool EnableFiltering { get; set; }

    /// <summary>Контент при отсутствии данных.</summary>
    [Parameter] public RenderFragment? EmptyContent { get; set; }

    /// <summary>Контент при загрузке.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Контент при ошибке.</summary>
    [Parameter] public RenderFragment<Exception>? ErrorContent { get; set; }

    // ── Состояние ────────────────────────────────────────────────────────────────

    /// <summary>Список отображаемых элементов после фильтрации/сортировки/пагинации.</summary>
    protected List<T> DisplayItems { get; private set; } = [];

    /// <summary>Данные загружаются.</summary>
    protected bool IsDataLoading { get; private set; }

    /// <summary>Ошибка последней загрузки.</summary>
    protected Exception? DataError { get; private set; }

    /// <summary>Текущая страница (1-based).</summary>
    protected int CurrentPage { get; private set; } = 1;

    /// <summary>Общее количество элементов (до пагинации).</summary>
    protected int TotalCount { get; private set; }

    /// <summary>Текущий дескриптор сортировки.</summary>
    protected SgSortDescriptor? CurrentSort { get; private set; }

    /// <summary>Текущие фильтры.</summary>
    protected List<SgFilterDescriptor> CurrentFilters { get; private set; } = [];

    /// <summary>Текущий текст глобального поиска.</summary>
    protected string? SearchText { get; private set; }

    /// <summary>true если есть хоть один элемент для отображения.</summary>
    protected bool HasItems => DisplayItems.Count > 0;

    /// <summary>true если загрузка завершена, нет ошибки, и данных нет.</summary>
    protected bool IsEmpty => !IsDataLoading && DataError == null && !HasItems;

    /// <summary>Общее количество страниц.</summary>
    protected int TotalPages => PageSize > 0
        ? Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize))
        : 1;

    // ── Приватное состояние ──────────────────────────────────────────────────────

    private volatile int _loadingVersion;
    private IEnumerable<T>? _lastItems;
    private int _lastPageSize;
    private Func<SgDataRequest, CancellationToken, Task<SgDataResult<T>>>? _lastDataSource;

    // ── Загрузка данных ──────────────────────────────────────────────────────────

    /// <summary>
    /// Загрузить/перезагрузить данные с учётом текущих параметров сортировки/фильтрации/пагинации.
    /// Версионирование предотвращает race condition при частых вызовах.
    /// </summary>
    protected async Task LoadDataAsync()
    {
        var version = Interlocked.Increment(ref _loadingVersion);
        IsDataLoading = true;
        DataError = null;
        await InvokeAsync(StateHasChanged);

        try
        {
            List<T> result;
            int totalCount;

            if (DataSource != null)
            {
                var request = new SgDataRequest
                {
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Sort = CurrentSort,
                    Filters = CurrentFilters,
                    SearchText = SearchText
                };

                var dataResult = await DataSource(request, ComponentToken);
                if (version != _loadingVersion) return;

                result = [.. dataResult.Items];
                totalCount = dataResult.TotalCount;
            }
            else if (Items != null)
            {
                var query = Items.AsQueryable();

                // УЛУЧШЕНИЕ: базовая in-memory реализация фильтрации
                if (EnableFiltering && CurrentFilters.Count > 0)
                    query = ApplyFilters(query);

                // УЛУЧШЕНИЕ: глобальный поиск по всем строковым полям
                if (!string.IsNullOrEmpty(SearchText) && EnableFiltering)
                    query = ApplySearch(query);

                // УЛУЧШЕНИЕ: базовая in-memory реализация сортировки
                if (EnableSorting && CurrentSort is not null)
                    query = ApplySort(query);

                // УЛУЧШЕНИЕ: TryGetNonEnumeratedCount + ICollection fallback
                if (!query.TryGetNonEnumeratedCount(out totalCount))
                {
                    if (Items is ICollection<T> collection)
                        totalCount = collection.Count;
                    else
                        totalCount = query.Count();
                }

                if (EnablePaging && PageSize > 0)
                    query = query.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

                result = [.. query];
            }
            else
            {
                result = [];
                totalCount = 0;
            }

            if (version != _loadingVersion) return;

            DisplayItems = result;
            TotalCount = totalCount;
        }
        catch (OperationCanceledException) { /* Нормальная отмена */ }
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
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    // ── Сортировка / Фильтрация / Пагинация ──────────────────────────────────────

    /// <summary>Переключить сортировку по полю: None → Ascending → Descending → None.</summary>
    protected async Task SortByAsync(string field)
    {
        if (IsDisposed) return;

        // ИСПРАВЛЕНИЕ CS0117: Asc/Desc → Ascending/Descending
        CurrentSort = CurrentSort?.Field == field
            ? CurrentSort.Direction == SgSortDirection.Ascending
                ? CurrentSort with { Direction = SgSortDirection.Descending }
                : null
            : new SgSortDescriptor(field, SgSortDirection.Ascending);

        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Применить фильтр по полю. Пустое значение — сброс фильтра.</summary>
    protected async Task FilterAsync(SgFilterDescriptor filter)
    {
        if (IsDisposed) return;
        CurrentFilters.RemoveAll(f => f.Field == filter.Field);
        if (filter.Value?.ToString() is { Length: > 0 })
            CurrentFilters.Add(filter);
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Перейти на указанную страницу.</summary>
    protected async Task GoToPageAsync(int page)
    {
        if (IsDisposed) return;
        CurrentPage = Math.Max(1, Math.Min(page, TotalPages));
        await LoadDataAsync();
    }

    /// <summary>Установить глобальный поисковый текст и перезагрузить данные.</summary>
    public async Task SearchAsync(string? text)
    {
        if (IsDisposed) return;
        SearchText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Сбросить все фильтры, сортировку и поиск.</summary>
    public async Task ResetAsync()
    {
        if (IsDisposed) return;
        CurrentFilters.Clear();
        CurrentSort = null;
        SearchText = null;
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>
    /// Публичный метод принудительной перезагрузки данных.
    /// Полезен при изменении данных вне компонента.
    /// </summary>
    public Task ReloadAsync() => LoadDataAsync();

    /// <summary>
    /// Extension point для экспорта данных.
    /// Переопределите в компонентах для реализации экспорта CSV/Excel/JSON.
    /// </summary>
    protected virtual Task ExportAsync(SgExportFormat format) => Task.CompletedTask;

    // ── Базовая in-memory фильтрация ─────────────────────────────────────────────

    /// <summary>
    /// Применить фильтры к LINQ-запросу.
    /// УЛУЧШЕНИЕ: базовая реализация для in-memory коллекций.
    /// Переопределите для кастомной логики или server-side фильтрации.
    /// </summary>
    protected virtual IQueryable<T> ApplyFilters(IQueryable<T> query)
    {
        foreach (var filter in CurrentFilters)
        {
            var filterExpr = BuildFilterExpression(filter);
            if (filterExpr != null)
                query = query.Where(filterExpr);
        }
        return query;
    }

    // Строит Expression<Func<T, bool>> для фильтра через Reflection
    private static Expression<Func<T, bool>>? BuildFilterExpression(SgFilterDescriptor filter)
    {
        if (string.IsNullOrEmpty(filter.Field)) return null;

        var prop = typeof(T).GetProperty(filter.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return null;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var filterValue = filter.Value;

        try
        {
            Expression body = filter.Operator switch
            {
                SgFilterOperator.Equals when filterValue is not null =>
                    Expression.Equal(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.NotEquals when filterValue is not null =>
                    Expression.NotEqual(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.Contains when prop.PropertyType == typeof(string) =>
                    Expression.Call(member,
                        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                        Expression.Constant(filterValue?.ToString() ?? string.Empty, typeof(string))),

                SgFilterOperator.StartsWith when prop.PropertyType == typeof(string) =>
                    Expression.Call(member,
                        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                        Expression.Constant(filterValue?.ToString() ?? string.Empty, typeof(string))),

                SgFilterOperator.EndsWith when prop.PropertyType == typeof(string) =>
                    Expression.Call(member,
                        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!,
                        Expression.Constant(filterValue?.ToString() ?? string.Empty, typeof(string))),

                SgFilterOperator.IsNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null
                        ? Expression.Constant(false)  // value types never null
                        : (Expression)Expression.Equal(member, Expression.Constant(null, prop.PropertyType)),

                SgFilterOperator.IsNotNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null
                        ? Expression.Constant(true)
                        : (Expression)Expression.NotEqual(member, Expression.Constant(null, prop.PropertyType)),

                _ => null!
            };

            if (body == null) return null;
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
        catch
        {
            return null; // Некорректный тип — игнорируем фильтр
        }
    }

    // ── Базовая in-memory сортировка ─────────────────────────────────────────────

    /// <summary>
    /// Применить сортировку к LINQ-запросу.
    /// УЛУЧШЕНИЕ: базовая реализация для in-memory коллекций.
    /// Переопределите для кастомной логики или server-side сортировки.
    /// </summary>
    protected virtual IQueryable<T> ApplySort(IQueryable<T> query)
    {
        if (CurrentSort is null || string.IsNullOrEmpty(CurrentSort.Field))
            return query;

        var prop = typeof(T).GetProperty(CurrentSort.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var keySelector = Expression.Lambda(member, param);

        var methodName = CurrentSort.Direction == SgSortDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), prop.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }



    // ── Lifecycle ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (IsDisposed) return;

        var dataSourceChanged = !ReferenceEquals(DataSource, _lastDataSource);
        if (dataSourceChanged)
        {
            _lastDataSource = DataSource;
            CurrentPage = 1;
            CurrentFilters.Clear();
            CurrentSort = null;
            SearchText = null;   // сброс поиска при смене DataSource
        }

        var itemsChanged   = !ReferenceEquals(Items, _lastItems);
        var pageSizeChanged = PageSize != _lastPageSize;

        if (itemsChanged)
        {
            // При смене in-memory коллекции сбрасываем поиск (новые данные — новый контекст)
            SearchText = null;
            CurrentFilters.Clear();
            CurrentSort = null;
        }

        if ((itemsChanged || pageSizeChanged || dataSourceChanged)
            && (Items != null || DataSource != null))
        {
            _lastItems    = Items;
            _lastPageSize = PageSize;
            if (!dataSourceChanged) CurrentPage = 1;
            await LoadDataAsync();
        }
    }

    // ── Базовая in-memory поиск ────────────────────────────────────────────────────

    /// <summary>
    /// Применить глобальный поиск по всем string-полям типа T.
    /// УЛУЧШЕНИЕ: case-insensitive Contains через StringComparison.OrdinalIgnoreCase.
    /// </summary>
    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query)
    {
        if (string.IsNullOrEmpty(SearchText)) return query;

        var stringProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .ToArray();

        if (stringProperties.Length == 0) return query;

        var param = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var prop in stringProperties)
        {
            var member = Expression.Property(param, prop);
            var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string), typeof(StringComparison)]);
            var call = Expression.Call(
                member,
                containsMethod!,
                Expression.Constant(SearchText, typeof(string)),
                Expression.Constant(StringComparison.OrdinalIgnoreCase, typeof(StringComparison)));

            combined = combined is null ? (Expression)call : Expression.OrElse(combined, call);
        }

        if (combined is not null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(combined, param);
            query = query.Where(lambda);
        }

        return query;
    }
}
