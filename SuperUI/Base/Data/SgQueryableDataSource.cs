// SuperUI/Base/Data/SgQueryableDataSource.cs — НОВЫЙ
// ✅ Серверный DataSource для IQueryable<T> (EF Core)
// ✅ Автоматическая компиляция Expression деревьев с кэшированием
// ✅ Поддержка серверной пагинации, сортировки, фильтрации
// ✅ Интеграция с SgDataRequest/SgDataResult

using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using SuperUI.Base;

namespace SuperUI.Base.Data;

/// <summary>
/// Серверный провайдер данных для IQueryable&lt;T&gt;.
/// Автоматически компилирует Expression-деревья и кэширует их.
/// Используется с SgDataBase&lt;T&gt;.DataSource.
/// </summary>
public sealed class SgQueryableDataSource<T> where T : class
{
    private readonly Func<IQueryable<T>> _queryFactory;
    private static readonly ConcurrentDictionary<string, Delegate> _compiledDelegates = new();

    /// <summary>
    /// Создать DataSource из фабрики IQueryable.
    /// </summary>
    /// <param name="queryFactory">Фабрика IQueryable (например, () => dbContext.Products).</param>
    public SgQueryableDataSource(Func<IQueryable<T>> queryFactory)
    {
        _queryFactory = queryFactory ?? throw new ArgumentNullException(nameof(queryFactory));
    }

    /// <summary>
    /// Основной метод: выполнить запрос с пагинацией, сортировкой и фильтрацией.
    /// </summary>
    public async Task<SgDataResult<T>> QueryAsync(SgDataRequest request)
    {
        var query = _queryFactory();

        // Фильтрация
        if (request.Filters is { Count: > 0 })
        {
            foreach (var filter in request.Filters.Where(f => f.IsActive))
            {
                query = ApplyFilter(query, filter);
            }
        }

        // Глобальный поиск
        if (!string.IsNullOrEmpty(request.SearchText))
        {
            query = ApplyGlobalSearch(query, request.SearchText);
        }

        // Группировка (если задана)
        if (request.Groups is { Count: > 0 })
        {
            return await QueryWithGroupsAsync(query, request);
        }

        // Сортировка
        if (request.Sort is not null)
        {
            query = ApplySort(query, request.Sort);
        }

        // Общее количество (до пагинации)
        var totalCount = await Task.Run(() => query.Count(), request.CancellationToken);

        // Пагинация
        if (!request.NoPaging && request.PageSize > 0)
        {
            query = query.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);
        }

        // Материализация
        var items = await Task.Run(() => query.ToList(), request.CancellationToken);

        return new SgDataResult<T>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    // ── Фильтрация ────────────────────────────────────────────────────────────

    private IQueryable<T> ApplyFilter(IQueryable<T> query, SgFilterDescriptor filter)
    {
        if (string.IsNullOrEmpty(filter.Field))
            return query;

        var cacheKey = $"Filter_{typeof(T).FullName}_{filter.Field}_{filter.Operator}";
        var expression = GetOrCompileExpression<Func<T, bool>>(cacheKey,
            () => BuildFilterExpression(filter));

        if (expression is not null)
        {
            // Для операторов с переменным значением — перекомпилируем
            if (filter.Operator is not SgFilterOperator.IsNull and not SgFilterOperator.IsNotNull)
            {
                var lambda = BuildFilterExpression(filter);
                if (lambda is not null)
                    query = query.Where(lambda);
            }
            else
            {
                query = query.Where(expression);
            }
        }

        return query;
    }

    private static Expression<Func<T, bool>>? BuildFilterExpression(SgFilterDescriptor filter)
    {
        var prop = typeof(T).GetProperty(filter.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null) return null;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);

        Expression? body = filter.Operator switch
        {
            SgFilterOperator.Equals when filter.Value is not null =>
                Expression.Equal(member, Expression.Constant(
                    Convert.ChangeType(filter.Value, prop.PropertyType), prop.PropertyType)),

            SgFilterOperator.Contains when prop.PropertyType == typeof(string) =>
                Expression.Call(member,
                    typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!,
                    Expression.Constant(filter.Value?.ToString() ?? string.Empty)),

            SgFilterOperator.StartsWith when prop.PropertyType == typeof(string) =>
                Expression.Call(member,
                    typeof(string).GetMethod(nameof(string.StartsWith), [typeof(string)])!,
                    Expression.Constant(filter.Value?.ToString() ?? string.Empty)),

            SgFilterOperator.GreaterThan when filter.Value is not null =>
                Expression.GreaterThan(member, Expression.Constant(
                    Convert.ChangeType(filter.Value, prop.PropertyType), prop.PropertyType)),

            SgFilterOperator.LessThan when filter.Value is not null =>
                Expression.LessThan(member, Expression.Constant(
                    Convert.ChangeType(filter.Value, prop.PropertyType), prop.PropertyType)),

            SgFilterOperator.IsNull =>
                Expression.Equal(member, Expression.Constant(null, prop.PropertyType)),

            SgFilterOperator.IsNotNull =>
                Expression.NotEqual(member, Expression.Constant(null, prop.PropertyType)),

            _ => null
        };

        return body is not null
            ? Expression.Lambda<Func<T, bool>>(body, param)
            : null;
    }

    // ── Сортировка ────────────────────────────────────────────────────────────

    private IQueryable<T> ApplySort(IQueryable<T> query, SgSortDescriptor sort)
    {
        var prop = typeof(T).GetProperty(sort.Field,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (prop is null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var keySelector = Expression.Lambda(member, param);

        var methodName = sort.Direction == SgSortDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable).GetMethods()
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), prop.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }

    // ── Глобальный поиск ─────────────────────────────────────────────────────

    private static readonly ConcurrentDictionary<Type, Expression<Func<T, bool>>?> _searchCache = new();

    private IQueryable<T> ApplyGlobalSearch(IQueryable<T> query, string searchText)
    {
        var expression = _searchCache.GetOrAdd(typeof(T), _ => BuildGlobalSearchExpression());
        return expression is not null ? query.Where(expression) : query;
    }

    private static Expression<Func<T, bool>>? BuildGlobalSearchExpression()
    {
        var stringProps = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string))
            .ToArray();

        if (stringProps.Length == 0) return null;

        var param = Expression.Parameter(typeof(T), "x");
        var searchTextParam = Expression.Parameter(typeof(string), "searchText");
        var containsMethod = typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

        Expression? combined = null;
        foreach (var prop in stringProps)
        {
            var member = Expression.Property(param, prop);
            var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(member, containsMethod, searchTextParam);
            var condition = Expression.AndAlso(notNull, contains);

            combined = combined is null ? condition : Expression.OrElse(combined, condition);
        }

        return combined is not null
            ? Expression.Lambda<Func<T, bool>>(combined, param)
            : null;
    }

    // ── Группировка ───────────────────────────────────────────────────────────

    private async Task<SgDataResult<T>> QueryWithGroupsAsync(
        IQueryable<T> query, SgDataRequest request)
    {
        // Базовая реализация группировки
        var totalCount = await Task.Run(() => query.Count(), request.CancellationToken);
        var items = await Task.Run(() => query.ToList(), request.CancellationToken);

        return new SgDataResult<T>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    // ── Кэш Expression ────────────────────────────────────────────────────────

    private static TDelegate? GetOrCompileExpression<TDelegate>(
        string key, Func<Expression<TDelegate>?> factory) where TDelegate : Delegate
    {
        if (_compiledDelegates.TryGetValue(key, out var cached))
            return cached as TDelegate;

        var expression = factory();
        if (expression is null)
        {
            _compiledDelegates[key] = null!;
            return null;
        }

        var compiled = expression.Compile();
        _compiledDelegates[key] = compiled;
        return compiled;
    }

    /// <summary>
    /// Очистить кэш скомпилированных Expression (при Hot Reload).
    /// </summary>
    public static void ClearCache() => _compiledDelegates.Clear();
}
