using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace SuperUI.Base.Data;

/// <summary>
/// Абстракция для выполнения async LINQ операций (EF Core и другие ORM).
/// Позволяет избежать прямой зависимости от EntityFrameworkCore.
/// </summary>
public interface IAsyncQueryExecutor
{
    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default);
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default);
}

/// <summary>
/// Синхронный fallback для non-EF сценариев.
/// </summary>
public sealed class SyncQueryExecutor : IAsyncQueryExecutor
{
    public static readonly SyncQueryExecutor Instance = new();

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default)
        => Task.FromResult(query.Count());

    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default)
        => Task.FromResult(query.ToList());
}

/// <summary>
/// Источник данных на основе IQueryable — поддерживает EF Core, LINQ to SQL и in-memory.
/// Выполняет фильтрацию, сортировку и пагинацию на стороне провайдера (SQL).
/// </summary>
public sealed class SgQueryableDataSource<T> where T : class
{
    private readonly Func<IQueryable<T>> _queryFactory;
    private readonly IAsyncQueryExecutor _executor;

    private static readonly ConcurrentDictionary<string, PropertyInfo?>
        _propertyCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly ConcurrentDictionary<Type, PropertyInfo[]>
        _searchPropsCache = new();

    public SgQueryableDataSource(
        Func<IQueryable<T>> queryFactory,
        IAsyncQueryExecutor? executor = null)
    {
        ArgumentNullException.ThrowIfNull(queryFactory);
        _queryFactory = queryFactory;
        _executor = executor ?? SyncQueryExecutor.Instance;
    }

    public async Task<SgDataResult<T>> ExecuteAsync(SgDataRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IQueryable<T> query = _queryFactory();

        if (request.Filters is { Count: > 0 })
        {
            foreach (var filter in request.Filters.Where(f => f.IsActive))
            {
                var predicate = BuildFilterExpression(filter);
                if (predicate is not null)
                    query = query.Where(predicate);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchPredicate = BuildSearchExpression(request.SearchText);
            if (searchPredicate is not null)
                query = query.Where(searchPredicate);
        }

        if (request.Sort is not null)
            query = ApplySortExpression(query, request.Sort);

        int totalCount = await _executor.CountAsync(query, request.CancellationToken);

        if (!request.NoPaging && request.PageSize > 0)
            query = query.Skip(request.SkipCount).Take(request.TakeCount);

        var items = await _executor.ToListAsync(query, request.CancellationToken);

        return new SgDataResult<T> { Items = items, TotalCount = totalCount };
    }

    private static Expression<Func<T, bool>>? BuildFilterExpression(SgFilterDescriptor filter)
    {
        if (string.IsNullOrEmpty(filter.Field)) return null;

        var prop = _propertyCache.GetOrAdd(filter.Field,
            f => typeof(T).GetProperty(f, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        if (prop is null) return null;

        var param = Expression.Parameter(typeof(T), "x");
        var member = Expression.Property(param, prop);
        var filterValue = filter.Value;

        try
        {
            Expression? body = filter.Operator switch
            {
                SgFilterOperator.Equals when filterValue is not null =>
                    Expression.Equal(member,
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.NotEquals when filterValue is not null =>
                    Expression.NotEqual(member,
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.Contains when prop.PropertyType == typeof(string) =>
                    BuildStringContains(member, filterValue?.ToString() ?? string.Empty),

                SgFilterOperator.NotContains when prop.PropertyType == typeof(string) =>
                    Expression.Not(BuildStringContains(member, filterValue?.ToString() ?? string.Empty)),

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
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.GreaterThanOrEqual when filterValue is not null =>
                    Expression.GreaterThanOrEqual(member,
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.LessThan when filterValue is not null =>
                    Expression.LessThan(member,
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.LessThanOrEqual when filterValue is not null =>
                    Expression.LessThanOrEqual(member,
                        Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),

                SgFilterOperator.Between when filterValue is not null && filter.Value2 is not null =>
                    Expression.AndAlso(
                        Expression.GreaterThanOrEqual(member,
                            Expression.Constant(SafeConvert(filterValue, prop.PropertyType), prop.PropertyType)),
                        Expression.LessThanOrEqual(member,
                            Expression.Constant(SafeConvert(filter.Value2, prop.PropertyType), prop.PropertyType))),

                SgFilterOperator.IsNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) is null
                        ? (Expression)Expression.Constant(false)
                        : Expression.Equal(member, Expression.Constant(null, prop.PropertyType)),

                SgFilterOperator.IsNotNull =>
                    prop.PropertyType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) is null
                        ? (Expression)Expression.Constant(true)
                        : Expression.NotEqual(member, Expression.Constant(null, prop.PropertyType)),

                _ => null
            };

            return body is null ? null : Expression.Lambda<Func<T, bool>>(body, param);
        }
        catch
        {
            return null;
        }
    }

    private static Expression BuildStringContains(MemberExpression member, string value)
    {
        var containsMethod = typeof(string).GetMethod(
            nameof(string.Contains), [typeof(string), typeof(StringComparison)])!;

        var notNull = Expression.NotEqual(member, Expression.Constant(null, typeof(string)));
        var call = Expression.Call(member, containsMethod,
            Expression.Constant(value),
            Expression.Constant(StringComparison.OrdinalIgnoreCase));

        return Expression.AndAlso(notNull, call);
    }

    private static Expression<Func<T, bool>>? BuildSearchExpression(string searchText)
    {
        var stringProps = _searchPropsCache.GetOrAdd(typeof(T), t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
             .Where(p => p.PropertyType == typeof(string))
             .ToArray());

        if (stringProps.Length == 0) return null;

        var param = Expression.Parameter(typeof(T), "x");
        Expression? combined = null;

        foreach (var prop in stringProps)
        {
            var member = Expression.Property(param, prop);
            var body = BuildStringContains(member, searchText);
            combined = combined is null ? body : Expression.OrElse(combined, body);
        }

        return combined is null ? null : Expression.Lambda<Func<T, bool>>(combined, param);
    }

    private static IQueryable<T> ApplySortExpression(IQueryable<T> query, SgSortDescriptor sort)
    {
        if (string.IsNullOrEmpty(sort.Field)) return query;

        var prop = _propertyCache.GetOrAdd(sort.Field,
            f => typeof(T).GetProperty(f, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));

        if (prop is null) return query;

        var param = Expression.Parameter(typeof(T), "x");
        var keySelector = Expression.Lambda(Expression.Property(param, prop), param);

        var methodName = sort.Direction == SgSortDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(T), prop.PropertyType);

        return (IQueryable<T>)method.Invoke(null, [query, keySelector])!;
    }

    private static object? SafeConvert(object? value, Type targetType)
    {
        if (value is null) return null;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Convert.ChangeType(value, underlying);
    }
}
