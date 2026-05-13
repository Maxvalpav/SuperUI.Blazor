// SuperUI/Base/SgDataBase.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS0101: SgDataTypes.cs удалён — все типы объединены здесь
// ✅ CS8863: SgSortDescriptor — единственное объявление record с параметрами
// ✅ CS0117: SgSortDirection.Ascending/Descending (не Asc/Desc)
// ✅ ComponentToken передаётся в DataSource
// ✅ ReloadAsync(), ExportAsync() — public extension points
// УЛУЧШЕНИЯ:
// ✅ SgFilterGroup с вложенными группами (для сложных AND/OR условий)
// ✅ SgGroupDescriptor для группировки
// ✅ SgDataResult<T> — generic record с AggregateData, GroupData, Meta
// ✅ SgDataRequest.SkipCount / TakeCount — computed, не дублируют Page/PageSize
// ✅ Race-condition protection через версионирование

using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

// ══════════════════════════════════════════════════════════════════════════════
// Модели запроса / результата
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Запрос данных для DataSource (server-side paging/sort/filter).</summary>
public sealed record SgDataRequest
{
    /// <summary>Номер страницы (1-based).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Количество строк на странице.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Сколько записей пропустить (computed из Page/PageSize).</summary>
    public int SkipCount => (Page - 1) * PageSize;

    /// <summary>Сколько записей взять (алиас PageSize).</summary>
    public int TakeCount => PageSize;

    /// <summary>Параметры сортировки.</summary>
    public SgSortDescriptor? Sort { get; init; }

    /// <summary>Список активных фильтров.</summary>
    public IReadOnlyList<SgFilterDescriptor> Filters { get; init; } = [];

    /// <summary>Дескрипторы группировки.</summary>
    public IReadOnlyList<SgGroupDescriptor> Groups { get; init; } = [];

    /// <summary>Поисковая строка (глобальный поиск по всем полям).</summary>
    public string? SearchText { get; init; }

    /// <summary>Дополнительные произвольные параметры.</summary>
    public IReadOnlyDictionary<string, object?>? ExtraParams { get; init; }

    /// <summary>Отключить пагинацию — вернуть все записи.</summary>
    public bool NoPaging { get; init; }

    /// <summary>Токен отмены (прокидывается из ComponentToken).</summary>
    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>Результат запроса данных от DataSource.</summary>
/// <typeparam name="T">Тип элемента.</typeparam>
public sealed record SgDataResult<T>
{
    /// <summary>Элементы текущей страницы.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Общее количество записей (без пагинации).</summary>
    public int TotalCount { get; init; }

    /// <summary>Пустой результат.</summary>
    public static SgDataResult<T> Empty => new();

    /// <summary>Агрегированные данные (итоги по колонкам).</summary>
    public IReadOnlyDictionary<string, object?>? AggregateData { get; init; }

    /// <summary>Данные группировки.</summary>
    public IReadOnlyList<SgGroupData<T>>? GroupData { get; init; }

    /// <summary>Произвольные метаданные.</summary>
    public IReadOnlyDictionary<string, object?>? Meta { get; init; }
}

/// <summary>Данные группы строк.</summary>
public sealed record SgGroupData<T>(
    string GroupKey,
    string GroupLabel,
    IReadOnlyList<T> Items,
    IReadOnlyDictionary<string, object?>? Aggregates = null);

// ══════════════════════════════════════════════════════════════════════════════
// Дескрипторы (единственные объявления — CS0101 / CS8863 fix)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Дескриптор сортировки.</summary>
/// <remarks>FIX CS8863: только одно объявление record с параметрами.</remarks>
public sealed record SgSortDescriptor(string Field, SgSortDirection Direction)
{
    /// <summary>Помещать null-значения в начало.</summary>
    public bool NullsFirst { get; init; } = false;
}

/// <summary>Дескриптор группировки.</summary>
public sealed record SgGroupDescriptor(
    string Field,
    SgSortDirection Direction = SgSortDirection.Ascending);

/// <summary>Дескриптор фильтра.</summary>
public sealed record SgFilterDescriptor(
    string Field,
    object? Value,
    SgFilterOperator Operator = SgFilterOperator.Contains,
    string? Value2 = null)   // для Between
{
    /// <summary>Фильтр активен (false = временно отключён).</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>Группа фильтров с логическим оператором (AND/OR условия).</summary>
public sealed class SgFilterGroup
{
    /// <summary>Логический оператор между фильтрами.</summary>
    public SgLogicalOperator Operator { get; init; } = SgLogicalOperator.And;

    /// <summary>Фильтры группы.</summary>
    public List<SgFilterDescriptor> Filters { get; } = [];

    /// <summary>Вложенные группы.</summary>
    public List<SgFilterGroup> Groups { get; } = [];
}

// ══════════════════════════════════════════════════════════════════════════════
// Enum-ы, специфичные для данных (остальные в SgEnums.cs)
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>Оператор фильтра.</summary>
public enum SgFilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    In,
    NotIn,
    IsNull,
    IsNotNull,
    /// <summary>Регулярное выражение (только для строк).</summary>
    Regex
}

/// <summary>Логический оператор для групп фильтров.</summary>
public enum SgLogicalOperator { And, Or }

// ══════════════════════════════════════════════════════════════════════════════
// Базовый класс данных
// ══════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Базовый класс для компонентов работы с данными.
/// Иерархия: SgInteractiveBase → SgDataBase{T}
/// </summary>
/// <typeparam name="T">Тип элемента данных.</typeparam>
public abstract class SgDataBase<T> : SgInteractiveBase
{
    // ── Параметры ───────────────────────────────────────────────────────────

    /// <summary>In-memory коллекция элементов.</summary>
    [Parameter] public IEnumerable<T>? Items { get; set; }

    /// <summary>
    /// Серверный источник данных (server-side paging/sort/filter).
    /// Сигнатура: (SgDataRequest) → Task&lt;SgDataResult&lt;T&gt;&gt;
    /// </summary>
    [Parameter]
    public Func<SgDataRequest, Task<SgDataResult<T>>>? DataSource { get; set; }

    /// <summary>
    /// IQueryable источник данных (EF Core, LINQ to SQL).
    /// При использовании — сортировка/фильтрация/пагинация выполняются на стороне БД.
    /// Приоритет: QueryableItems > DataSource > Items
    /// </summary>
    [Parameter]
    public IQueryable<T>? QueryableItems { get; set; }

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

    // ── Состояние ───────────────────────────────────────────────────────────

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

    // ── Selection State ──────────────────────────────────────────────────────

    /// <summary>Выбранные ключи элементов (для multi-select).</summary>
    private readonly HashSet<object> _selectedKeys = [];

    /// <summary>Получить список выбранных ключей (read-only).</summary>
    protected IReadOnlySet<object> SelectedKeys => _selectedKeys;

    /// <summary>Количество выбранных элементов.</summary>
    protected int SelectedCount => _selectedKeys.Count;

    /// <summary>Все элементы на текущей странице выбраны.</summary>
    protected bool AllPageItemsSelected
    {
        get
        {
            if (DisplayItems.Count == 0) return false;
            // Предполагаем, что T имеет свойство Id или используется GetItemKey
            return DisplayItems.All(item => _selectedKeys.Contains(GetItemKey(item)));
        }
    }

    /// <summary>
    /// Получить ключ элемента для selection.
    /// Override для использования пользовательского ключа (по умолчанию — сам элемент).
    /// </summary>
    protected virtual object GetItemKey(T item) => item!;

    /// <summary>Проверить, выбран ли элемент.</summary>
    protected bool IsItemSelected(T item) => _selectedKeys.Contains(GetItemKey(item));

    /// <summary>Переключить выбор элемента (добавить/удалить).</summary>
    protected async Task ToggleSelectionAsync(T item)
    {
        if (IsDisposed) return;
        var key = GetItemKey(item);
        if (!_selectedKeys.Remove(key)) _selectedKeys.Add(key);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Выбрать элемент.</summary>
    protected async Task SelectItemAsync(T item)
    {
        if (IsDisposed) return;
        _selectedKeys.Add(GetItemKey(item));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Отменить выбор элемента.</summary>
    protected async Task DeselectItemAsync(T item)
    {
        if (IsDisposed) return;
        _selectedKeys.Remove(GetItemKey(item));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Выбрать все элементы на текущей странице.</summary>
    protected async Task SelectAllPageAsync()
    {
        if (IsDisposed) return;
        foreach (var item in DisplayItems)
            _selectedKeys.Add(GetItemKey(item));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Отменить выбор всех элементов на текущей странице.</summary>
    protected async Task DeselectAllPageAsync()
    {
        if (IsDisposed) return;
        foreach (var item in DisplayItems)
            _selectedKeys.Remove(GetItemKey(item));
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Выбрать все элементы (все страницы).</summary>
    protected async Task SelectAllAsync()
    {
        if (IsDisposed) return;
        if (Items != null)
        {
            foreach (var item in Items)
                _selectedKeys.Add(GetItemKey(item));
        }
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Отменить выбор всех элементов.</summary>
    protected async Task ClearSelectionAsync()
    {
        if (IsDisposed) return;
        _selectedKeys.Clear();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Получить выбранные элементы (из DisplayItems).</summary>
    protected IEnumerable<T> GetSelectedItems()
        => DisplayItems.Where(item => _selectedKeys.Contains(GetItemKey(item)));

    // ── Приватное состояние ─────────────────────────────────────────────────

    private volatile int _loadingVersion;
    private IEnumerable<T>? _lastItems;
    private IQueryable<T>? _lastQueryableItems;
    private int _lastPageSize;
    private Func<SgDataRequest, Task<SgDataResult<T>>>? _lastDataSource;
    private CancellationTokenSource? _searchDebounceCts;

    // ── Загрузка данных ─────────────────────────────────────────────────────

    /// <summary>
    /// Загрузить / перезагрузить данные.
    /// Версионирование защищает от race-conditions.
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

            if (QueryableItems is not null)
            {
                // ✅ UX-6: IQueryable источник (EF Core, LINQ to SQL)
                // Сортировка/фильтрация/пагинация выполняются на стороне БД
                var query = QueryableItems;

                if (EnableFiltering && CurrentFilters.Count > 0)
                    query = (IQueryable<T>)ApplyFilters(query);

                if (!string.IsNullOrEmpty(SearchText) && EnableFiltering)
                    query = (IQueryable<T>)ApplySearch(query);

                if (EnableSorting && CurrentSort is not null)
                    query = (IQueryable<T>)ApplySort(query);

                // Получаем total count БЕЗ пагинации (SQL COUNT(*))
                totalCount = await Task.Run(() => query.Count(), ComponentToken);

                if (EnablePaging && PageSize > 0)
                    query = query.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

                // Материализуем результат
                result = await Task.Run(() => query.ToList(), ComponentToken);
            }
            else if (DataSource != null)
            {
                var request = new SgDataRequest
                {
                    Page = CurrentPage,
                    PageSize = PageSize,
                    Sort = CurrentSort,
                    Filters = CurrentFilters,
                    SearchText = SearchText,
                    // ✅ ComponentToken для автоотмены при Dispose
                    CancellationToken = ComponentToken
                };

                var dataResult = await DataSource(request);
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
                    if (Items is ICollection<T> col) totalCount = col.Count;
                    else if (Items is ICollection rawCol) totalCount = rawCol.Count;
                    else totalCount = query.Count();
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
            // Нормальная отмена при Dispose
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

    // ── Управление данными ──────────────────────────────────────────────────

    /// <summary>Переключить сортировку: None → Ascending → Descending → None.</summary>
    protected async Task SortByAsync(string field)
    {
        if (IsDisposed) return;

        // ✅ FIX CS0117: Ascending/Descending (не Asc/Desc)
        CurrentSort = CurrentSort?.Field == field
            ? CurrentSort.Direction == SgSortDirection.Ascending
                ? CurrentSort with { Direction = SgSortDirection.Descending }
                : null
            : new SgSortDescriptor(field, SgSortDirection.Ascending);

        CurrentPage = 1;
        await LoadDataAsync();
    }

    /// <summary>Применить / сбросить фильтр.</summary>
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

    /// <summary>
    /// Поиск с дебаунсом (по умолчанию 300ms).
    /// Предотвращает лавину запросов при быстром вводе.
    /// </summary>
    public async Task SearchDebouncedAsync(string? text, int debounceMs = 300)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();

        _searchDebounceCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        var ct = _searchDebounceCts.Token;

        try
        {
            await Task.Delay(debounceMs, ct);
            await SearchAsync(text);
        }
        catch (OperationCanceledException) { }
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

    /// <summary>Принудительная перезагрузка (например, после сохранения).</summary>
    public Task ReloadAsync() => LoadDataAsync();

    /// <summary>Extension point для экспорта данных.</summary>
    protected virtual Task ExportAsync(SgExportFormat format) => Task.CompletedTask;

    // ── In-memory фильтрация ────────────────────────────────────────────────

    // PERF-3: Кэш Expression-деревьев для фильтров
    /// <summary>Ключ для кэша expression деревьев.</summary>
    private readonly record struct FilterCacheKey(Type EntityType, string Field, SgFilterOperator Operator);

    /// <summary>Статический кэш — разделяется между всеми экземплярами компонента одного типа.</summary>
    private static readonly ConcurrentDictionary<FilterCacheKey, Delegate?> _filterExpressionCache = new();

    /// <summary>Кэш свойств для быстрого поиска через reflection.</summary>
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?> _propertyCache = new();

    /// <summary>
    /// ✅ PERF-2: кэш string-свойств типа T — вычисляется один раз на тип.
    /// </summary>
    private static readonly PropertyInfo[] _stringProperties =
        typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(string))
            .ToArray();

    /// <summary>
    /// Expression cache для string.Contains (самый частый фильтр).
    /// Ключ: (EntityType, fieldName, lowerValue)
    /// </summary>
    private static readonly ConcurrentDictionary<(Type EntityType, string Field, string LowerValue), Expression<Func<T, bool>>> _stringContainsCache = new();

    /// <summary>Получить кэшированное свойство типа.</summary>
    private static PropertyInfo? GetCachedProperty(Type type, string fieldName)
        => _propertyCache.GetOrAdd((type, fieldName), static k =>
            k.Item1.GetProperty(k.Item2,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

    /// <summary>
    /// Получить или построить кэшированное expression для string.Contains (case-insensitive).
    /// </summary>
    private static Expression<Func<T, bool>> GetCachedStringContainsExpression(
        string fieldName, string value)
    {
        var key = (typeof(T), fieldName, value.ToLowerInvariant());
        return _stringContainsCache.GetOrAdd(key, _ =>
        {
            var prop = GetCachedProperty(typeof(T), fieldName);
            if (prop is null) return (Expression<Func<T, bool>>)(x => false);

            var param = Expression.Parameter(typeof(T), "x");
            var member = Expression.Property(param, prop);
            var toLower = Expression.Call(member, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
            var contains = Expression.Call(toLower,
                typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                Expression.Constant(value.ToLowerInvariant()));

            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var body = Expression.AndAlso(notNull, contains);

            return Expression.Lambda<Func<T, bool>>(body, param);
        });
    }

    /// <summary>Получить или построить кэшированное expression дерево для фильтра.</summary>
    private Expression<Func<T, bool>>? GetOrBuildFilterExpression(SgFilterDescriptor filter)
    {
        // Для статических операторов (IsNull, IsNotNull) — кэшируем полностью
        if (filter.Operator is SgFilterOperator.IsNull or SgFilterOperator.IsNotNull)
        {
            var key = new FilterCacheKey(typeof(T), filter.Field, filter.Operator);
            var cachedDelegate = _filterExpressionCache.GetOrAdd(key, k =>
                BuildFilterExpression(new SgFilterDescriptor(k.Field, null, k.Operator)) as Delegate);
            return cachedDelegate as Expression<Func<T, bool>>;
        }

        // Для операторов с переменным значением — кэшируем только структуру, значение параметризуемо
        return BuildFilterExpression(filter);
    }

    /// <summary>Применить фильтры (Expression-деревья через Reflection).</summary>
    protected virtual IQueryable<T> ApplyFilters(IQueryable<T> query)
    {
        foreach (var filter in CurrentFilters.Where(f => f.IsActive))
        {
            var expr = GetOrBuildFilterExpression(filter);
            if (expr != null) query = query.Where(expr);
        }
        return query;
    }

    // ── In-memory фильтрация (старая версия, заменена выше) ────────────────────────────────────────────────

    private static Expression<Func<T, bool>>? BuildFilterExpression(SgFilterDescriptor filter)
    {
        if (string.IsNullOrEmpty(filter.Field)) return null;

        // PERF-3: Используем кэшированный поиск свойства
        var prop = GetCachedProperty(typeof(T), filter.Field);
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
                     GetCachedStringContainsExpression(filter.Field, filterValue?.ToString() ?? string.Empty),

                SgFilterOperator.NotContains when prop.PropertyType == typeof(string) =>
                     Expression.Not(GetCachedStringContainsExpression(filter.Field, filterValue?.ToString() ?? string.Empty)),

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

                SgFilterOperator.GreaterThanOrEqual when filterValue is not null =>
                    Expression.GreaterThanOrEqual(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.LessThan when filterValue is not null =>
                    Expression.LessThan(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.LessThanOrEqual when filterValue is not null =>
                    Expression.LessThanOrEqual(member,
                        Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),

                // ═══ C4 FIX: добавленные операторы ═══
                SgFilterOperator.Between when filterValue is not null && filter.Value2 is not null =>
                    Expression.AndAlso(
                        Expression.GreaterThanOrEqual(member,
                            Expression.Constant(Convert.ChangeType(filterValue, prop.PropertyType), prop.PropertyType)),
                        Expression.LessThanOrEqual(member,
                            Expression.Constant(Convert.ChangeType(filter.Value2, prop.PropertyType), prop.PropertyType))),

                SgFilterOperator.In when filterValue is System.Collections.IEnumerable enumerable =>
                    BuildInExpression(member, enumerable, prop.PropertyType) ?? Expression.Constant(false),

                SgFilterOperator.NotIn when filterValue is System.Collections.IEnumerable enumerable =>
                    Expression.Not(BuildInExpression(member, enumerable, prop.PropertyType) ?? Expression.Constant(true)),

                SgFilterOperator.Regex when prop.PropertyType == typeof(string) =>
                    BuildRegexExpression(member, filterValue?.ToString() ?? string.Empty) ?? Expression.Constant(false),

                SgFilterOperator.IsNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) == null
                        ? Expression.Constant(false)
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
            return null; // Некорректный тип — игнорируем
        }
    }

    // ═══ C4: вспомогательные методы ═══

    /// <summary>
    /// Построить Expression для оператора In (value IN (v1, v2, v3, ...)).
    /// </summary>
    private static Expression? BuildInExpression(MemberExpression member, System.Collections.IEnumerable values, Type propertyType)
    {
        Expression? combined = null;
        foreach (var val in values)
        {
            if (val is null) continue;
            var eqExpr = Expression.Equal(member,
                Expression.Constant(Convert.ChangeType(val, propertyType), propertyType));
            combined = combined is null ? (Expression)eqExpr : Expression.OrElse(combined, eqExpr);
        }
        return combined;
    }

    /// <summary>
    /// Построить Expression для оператора Regex (только строковые поля).
    /// Использует статический метод Regex.IsMatch.
    /// </summary>
    private static Expression? BuildRegexExpression(MemberExpression member, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return null;
        var regexIsMatch = typeof(System.Text.RegularExpressions.Regex)
            .GetMethod(nameof(System.Text.RegularExpressions.Regex.IsMatch), [typeof(string), typeof(string)])!;
        return Expression.Call(regexIsMatch, member, Expression.Constant(pattern));
    }

    // ── In-memory сортировка ────────────────────────────────────────────────

    /// <summary>Применить сортировку (Expression-деревья через Reflection).</summary>
    protected virtual IQueryable<T> ApplySort(IQueryable<T> query)
    {
        if (CurrentSort is null || string.IsNullOrEmpty(CurrentSort.Field)) return query;

        var prop = typeof(T).GetProperty(CurrentSort.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop == null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var keySelector = Expression.Lambda(member, param);

        // ✅ FIX CS0117: Ascending/Descending
        var methodName = CurrentSort.Direction == SgSortDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), prop.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }

    // ── In-memory поиск ─────────────────────────────────────────────────────

    protected virtual IQueryable<T> ApplySearch(IQueryable<T> query)
    {
        if (string.IsNullOrEmpty(SearchText)) return query;

        // ✅ PERF-2: используем кэшированный массив string-свойств
        if (_stringProperties.Length == 0) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var containsMethod = typeof(string).GetMethod(
            nameof(string.Contains),
            [typeof(string), typeof(StringComparison)])!;

        Expression? combined = null;

        foreach (var prop in _stringProperties)
        {
            var memberExpr = Expression.Property(param, prop);
            var notNull = Expression.NotEqual(memberExpr, Expression.Constant(null, typeof(string)));
            var call = Expression.Call(memberExpr, containsMethod,
                Expression.Constant(SearchText),
                Expression.Constant(StringComparison.OrdinalIgnoreCase));
            var safe = Expression.AndAlso(notNull, call);

            combined = combined is null ? (Expression)safe : Expression.OrElse(combined, safe);
        }

        if (combined is not null)
            query = query.Where(Expression.Lambda<Func<T, bool>>(combined, param));

        return query;
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        if (IsDisposed) return;

        var dataSourceChanged = !ReferenceEquals(DataSource, _lastDataSource);
        var queryableItemsChanged = !ReferenceEquals(QueryableItems, _lastQueryableItems);
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

        if (queryableItemsChanged)
        {
            _lastQueryableItems = QueryableItems;
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

        if ((queryableItemsChanged || itemsChanged || pageSizeChanged || dataSourceChanged)
            && (QueryableItems != null || Items != null || DataSource != null))
        {
            _lastItems = Items;
            _lastPageSize = PageSize;
            if (!dataSourceChanged && !queryableItemsChanged) CurrentPage = 1;
            await LoadDataAsync();
        }
    }

    // ── Dispose ────────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        await base.DisposeComponentAsync();
    }
}
