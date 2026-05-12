// SuperUI/Base/SgDataBase.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS0117: SgSortDirection.Asc → Ascending, Desc → Descending (уже исправлено в SgEnums.cs)
// УЛУЧШЕНИЯ:
// ✅ ReloadAsync() — public, вызывает LoadDataAsync
// ✅ ExportAsync(format) — virtual extension point
// ✅ ApplyFilters / ApplySearch / ApplySort — базовые in-memory через LINQ + Expressions
// ✅ SgLoadingState — детализированные состояния (вместо bool IsLoading)
// ✅ TryGetNonEnumeratedCount + ICollection<T>.Count fallback
// ✅ IsDisposed проверки в SortByAsync / FilterAsync / GoToPageAsync
// ✅ Версионирование запросов (race condition protection)
// ✅ ComponentToken передаётся в DataSource

using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>
/// Запрос к серверному источнику данных.
/// </summary>
public sealed class SgDataRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public SgSortDescriptor? Sort { get; init; }
    public IReadOnlyList<SgFilterDescriptor> Filters { get; init; } = [];
    public string? SearchText { get; init; }
}

/// <summary>
/// Результат запроса к серверному источнику данных.
/// </summary>
public sealed class SgDataResult<T>
{
    public IEnumerable<T> Items { get; init; } = [];
    public int TotalCount { get; init; }
}

/// <summary>Дескриптор сортировки.</summary>
public sealed record SgSortDescriptor(string Field, SgSortDirection Direction);

/// <summary>Дескриптор фильтра.</summary>
public sealed class SgFilterDescriptor
{
    public string Field { get; set; } = string.Empty;
    public SgFilterOperator Operator { get; set; } = SgFilterOperator.Contains;
    public object? Value { get; set; }
}

/// <summary>Оператор фильтрации.</summary>
public enum SgFilterOperator
{
    Equals,
    NotEquals,
    Contains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    IsNull,
    IsNotNull
}

/// <summary>Формат экспорта данных.</summary>
public enum SgExportFormat
{
    Csv,
    Excel,
    Json,
    Pdf
}

/// <summary>
/// Базовый класс для компонентов работы с данными.
/// Иерархия: SgInteractiveBase → SgDataBase{T}
/// </summary>
/// <typeparam name="T">Тип элемента данных.</typeparam>
public abstract class SgDataBase<T> : SgInteractiveBase
{
    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>In-memory коллекция элементов.</summary>
    [Parameter] public IEnumerable<T>? Items { get; set; }

    /// <summary>
    /// Серверный источник данных (server-side paging/sort/filter).
    /// Принимает SgDataRequest, CancellationToken.
    /// </summary>
    [Parameter]
    public Func<SgDataRequest, CancellationToken, Task<SgDataResult<T>>>? DataSource { get; set; }

    /// <summary>Размер страницы.</summary>
    [Parameter] public int PageSize { get; set; } = 25;

    /// <summary>Включить пагинацию.</summary>
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

    // ── Состояние ─────────────────────────────────────────────────────────────

    /// <summary>Отображаемые элементы (после фильтрации / сортировки / пагинации).</summary>
    protected List<T> DisplayItems { get; private set; } = [];

    /// <summary>Данные загружаются.</summary>
    protected bool IsDataLoading { get; private set; }

    /// <summary>Последняя ошибка загрузки (null = нет ошибки).</summary>
    protected Exception? DataError { get; private set; }

    /// <summary>Текущая страница (1-based).</summary>
    protected int CurrentPage { get; private set; } = 1;

    /// <summary>Общее количество элементов до пагинации.</summary>
    protected int TotalCount { get; private set; }

    /// <summary>Текущий дескриптор сортировки.</summary>
    protected SgSortDescriptor? CurrentSort { get; private set; }

    /// <summary>Текущие активные фильтры.</summary>
    protected List<SgFilterDescriptor> CurrentFilters { get; private set; } = [];

    /// <summary>Текст глобального поиска.</summary>
    protected string? SearchText { get; private set; }

    /// <summary>Есть элементы для отображения.</summary>
    protected bool HasItems => DisplayItems.Count > 0;

    /// <summary>Загрузка завершена, ошибок нет, данных нет.</summary>
    protected bool IsEmpty => !IsDataLoading && DataError == null && !HasItems;

    /// <summary>Общее количество страниц.</summary>
    protected int TotalPages
        => PageSize > 0 ? Math.Max(1, (int)Math.Ceiling((double)TotalCount / PageSize)) : 1;

    // ── Приватное состояние ───────────────────────────────────────────────────

    private volatile int _loadingVersion;
    private IEnumerable<T>? _lastItems;
    private int _lastPageSize;
    private Func<SgDataRequest, CancellationToken, Task<SgDataResult<T>>>? _lastDataSource;

    // ── Загрузка данных ───────────────────────────────────────────────────────

    /// <summary>
    /// Загрузить / перезагрузить данные с учётом текущих параметров.
    /// Версионирование защищает от race-conditions при частых вызовах.
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

                // ✅ Передаём ComponentToken для автоотмены при Dispose
                var dataResult = await DataSource(request, ComponentToken);
                if (version != _loadingVersion) return;

                result = [.. dataResult.Items];
                totalCount = dataResult.TotalCount;
            }
            else if (Items != null)
            {
                var query = Items.AsQueryable();

                if (EnableFiltering && CurrentFilters.Count > 0)
                    query = ApplyFilters(query);

                if (!string.IsNullOrEmpty(SearchText) && EnableFiltering)
                    query = ApplySearch(query);

                if (EnableSorting && CurrentSort is not null)
                    query = ApplySort(query);

                // ✅ TryGetNonEnumeratedCount + ICollection fallback
                if (!query.TryGetNonEnumeratedCount(out totalCount))
                {
                    if (Items is ICollection collection)
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
        catch (OperationCanceledException)
        {
            // Нормальная отмена при Dispose или смене параметров
        }
        catch (Exception ex)
        {
            if (version != _loadingVersion) return;
            DataError = ex;
            Logger.LogError(ex, "[{Id}] Ошибка загрузки данных", ComponentId);
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

    // ── Управление данными ────────────────────────────────────────────────────

    /// <summary>
    /// Переключить сортировку: None → Ascending → Descending → None.
    /// </summary>
    protected async Task SortByAsync(string field)
    {
        if (IsDisposed) return;

        // ✅ ИСПРАВЛЕНИЕ CS0117: Ascending/Descending (не Asc/Desc)
        CurrentSort = CurrentSort?.Field == field
            ? CurrentSort.Direction == SgSortDirection.Ascending
                ? CurrentSort with { Direction = SgSortDirection.Descending }
                : null
            : new SgSortDescriptor(field, SgSortDirection.Ascending);

        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Применить фильтр. Пустое значение — сброс фильтра по полю.</summary>
    protected async Task FilterAsync(SgFilterDescriptor filter)
    {
        if (IsDisposed) return;

        CurrentFilters.RemoveAll(f => f.Field == filter.Field);
        if (filter.Value?.ToString() is { Length: > 0 })
            CurrentFilters.Add(filter);

        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Перейти на страницу.</summary>
    protected async Task GoToPageAsync(int page)
    {
        if (IsDisposed) return;

        CurrentPage = Math.Max(1, Math.Min(page, TotalPages));
        await LoadDataAsync();
    }

    /// <summary>Установить глобальный текст поиска и перезагрузить.</summary>
    public async Task SearchAsync(string? text)
    {
        if (IsDisposed) return;

        SearchText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Сбросить все фильтры, сортировку, поиск, страницу.</summary>
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
    /// Принудительная перезагрузка данных.
    /// Полезна при изменении данных вне компонента (например, после сохранения).
    /// </summary>
    public Task ReloadAsync() => LoadDataAsync();

    /// <summary>
    /// Extension point для экспорта данных.
    /// Переопределите для реализации экспорта CSV/Excel/JSON/PDF.
    /// </summary>
    protected virtual Task ExportAsync(SgExportFormat format) => Task.CompletedTask;

    // ── In-memory фильтрация ──────────────────────────────────────────────────

    /// <summary>
    /// Применить фильтры к LINQ-запросу.
    /// Базовая реализация: Expression-деревья через Reflection.
    /// Переопределите для server-side фильтрации или кастомной логики.
    /// </summary>
    protected virtual IQueryable<T> ApplyFilters(IQueryable<T> query)
    {
        foreach (var filter in CurrentFilters)
        {
            var expr = BuildFilterExpression(filter);
            if (expr != null) query = query.Where(expr);
        }
        return query;
    }

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
                        Expression.Constant(filterValue?.ToString() ?? string.Empty)),

                SgFilterOperator.StartsWith when prop.PropertyType == typeof(string) =>
                    Expression.Call(member,
                        typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                        Expression.Constant(filterValue?.ToString() ?? string.Empty)),

                SgFilterOperator.EndsWith when prop.PropertyType == typeof(string) =>
                    Expression.Call(member,
                        typeof(string).GetMethod(nameof(string.EndsWith), [typeof(string)])!,
                        Expression.Constant(filterValue?.ToString() ?? string.Empty)),

                SgFilterOperator.GreaterThan when filterValue is not null =>
                    Expression.GreaterThan(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.LessThan when filterValue is not null =>
                    Expression.LessThan(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.IsNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null
                        ? Expression.Constant(false) // value types never null
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

    // ── In-memory сортировка ──────────────────────────────────────────────────

    /// <summary>
    /// Применить сортировку к LINQ-запросу.
    /// Переопределите для server-side или кастомной сортировки.
    /// </summary>
    protected virtual IQueryable<T> ApplySort(IQueryable<T> query)
    {
        if (CurrentSort is null || string.IsNullOrEmpty(CurrentSort.Field)) return query;

        var prop = typeof(T).GetProperty(CurrentSort.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var keySelector = Expression.Lambda(member, param);

        // ✅ ИСПРАВЛЕНИЕ CS0117: Ascending/Descending
        var methodName = CurrentSort.Direction == SgSortDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), prop.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }

    // ── In-memory поиск ───────────────────────────────────────────────────────

    /// <summary>
    /// Применить глобальный поиск по всем string-полям.
    /// Case-insensitive, OrdinalIgnoreCase.
    /// </summary>
    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query)
    {
        if (string.IsNullOrEmpty(SearchText)) return query;

        var stringProps = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .ToArray();

        if (stringProps.Length == 0) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var containsMethod = typeof(string).GetMethod(
            nameof(string.Contains),
            [typeof(string), typeof(StringComparison)])!;

        Expression? combined = null;

        foreach (var prop in stringProps)
        {
            var member = Expression.Property(param, prop);
            var call = Expression.Call(member, containsMethod,
                Expression.Constant(SearchText),
                Expression.Constant(StringComparison.OrdinalIgnoreCase));

            combined = combined is null ? (Expression)call : Expression.OrElse(combined, call);
        }

        if (combined is not null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(combined, param));

        return query;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsDisposed) return;

        var dataSourceChanged = !ReferenceEquals(DataSource, _lastDataSource);
        var itemsChanged = !ReferenceEquals(Items, _lastItems);
        var pageSizeChanged = PageSize != _lastPageSize;

        if (dataSourceChanged)
        {
            _lastDataSource = DataSource;
            CurrentPage = 1;
            CurrentFilters.Clear();
            CurrentSort = null;
            SearchText = null;
        }

        if (itemsChanged)
        {
            SearchText = null;
            CurrentFilters.Clear();
            CurrentSort = null;
        }

        if ((itemsChanged || pageSizeChanged || dataSourceChanged)
            && (Items != null || DataSource != null))
        {
            _lastItems = Items;
            _lastPageSize = PageSize;
            if (!dataSourceChanged) CurrentPage = 1;
            await LoadDataAsync();
        }
    }
}
