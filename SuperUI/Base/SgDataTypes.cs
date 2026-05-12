namespace SuperUI.Base;

/// <summary>Запрос данных для DataSource.</summary>
public record SgDataRequest
{
    /// <summary>Номер страницы (1-based).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Количество строк на странице.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>
    /// Сколько записей пропустить (вычисляется из Page/PageSize).
    /// FIX CS0117: был отдельным полем, которое не инициализировалось → теперь computed.
    /// </summary>
    public int SkipCount => (Page - 1) * PageSize;

    /// <summary>
    /// Сколько записей взять (алиас PageSize для совместимости).
    /// FIX CS0117: был отдельным полем → теперь computed.
    /// </summary>
    public int TakeCount => PageSize;

    /// <summary>Параметры сортировки.</summary>
    public SgSortDescriptor? Sort { get; init; }

    /// <summary>Список активных фильтров.</summary>
    public IReadOnlyList<SgFilterDescriptor> Filters { get; init; } = [];

    /// <summary>Группировка (поле и направление).</summary>
    public IReadOnlyList<SgGroupDescriptor> Groups { get; init; } = [];

    /// <summary>Поисковая строка (глобальный поиск по всем полям).</summary>
    public string? SearchText { get; init; }

    /// <summary>Дополнительные произвольные параметры (передаются в DataSource).</summary>
    public IReadOnlyDictionary<string, object?>? ExtraParams { get; init; }

    /// <summary>Отключить пагинацию — вернуть все записи.</summary>
    public bool NoPaging { get; init; }

    /// <summary>Токен отмены для длительных операций DataSource.</summary>
    public CancellationToken CancellationToken { get; init; } = default;
}

/// <summary>Результат запроса данных от DataSource.</summary>
/// <typeparam name="T">Тип элемента.</typeparam>
public record SgDataResult<T>(IReadOnlyList<T> Items, int TotalCount)
{
    /// <summary>Пустой результат (нет элементов).</summary>
    public static SgDataResult<T> Empty => new([], 0);

    /// <summary>Агрегированные данные (итоги, суммы) для отображения в footer.</summary>
    public IReadOnlyDictionary<string, object?>? AggregateData { get; init; }

    /// <summary>Данные группировки (если включена группировка в DataGrid).</summary>
    public IReadOnlyList<SgGroupData<T>>? GroupData { get; init; }

    /// <summary>Дополнительные метаданные (для расширяемости).</summary>
    public IReadOnlyDictionary<string, object?>? Meta { get; init; }
}

/// <summary>Данные группы строк.</summary>
public record SgGroupData<T>(
    string GroupKey,
    string GroupLabel,
    IReadOnlyList<T> Items,
    IReadOnlyDictionary<string, object?>? Aggregates = null);

/// <summary>Дескриптор сортировки.</summary>
public record SgSortDescriptor(string Field, SgSortDirection Direction)
{
    /// <summary>Помещать null-значения в начало при сортировке.</summary>
    public bool NullsFirst { get; init; } = false;
}

/// <summary>Дескриптор группировки.</summary>
public record SgGroupDescriptor(string Field, SgSortDirection Direction = SgSortDirection.Ascending);

/// <summary>Дескриптор фильтра.</summary>
public record SgFilterDescriptor(
    string Field,
    object? Value,
    SgFilterOperator Operator = SgFilterOperator.Contains,
    string? Value2 = null   // для Between
)
{
    /// <summary>Фильтр активен. false — временно отключён без удаления.</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>Группа фильтров с логическим оператором (для сложных условий).</summary>
public sealed class SgFilterGroup
{
    /// <summary>Логический оператор между фильтрами группы.</summary>
    public SgLogicalOperator Operator { get; init; } = SgLogicalOperator.And;

    /// <summary>Фильтры группы.</summary>
    public List<SgFilterDescriptor> Filters { get; } = [];

    /// <summary>Вложенные группы (для сложных условий).</summary>
    public List<SgFilterGroup> Groups { get; } = [];
}

/// <summary>Логический оператор для групп фильтров.</summary>
public enum SgLogicalOperator { And, Or }

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
    /// <summary>Регулярное выражение (для строк).</summary>
    Regex
}
