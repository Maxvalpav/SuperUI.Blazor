// SuperUI/Base/SgDataTypes.cs
//
// УЛУЧШЕНИЯ:
//   ✅ SgDataRequest: добавлен ExtraParams с non-nullable default
//   ✅ SgDataResult<T>: добавлены поля AggregateData, GroupData для grouping
//   ✅ SgSortDescriptor: добавлен NullsFirst для сортировки null-значений
//   ✅ SgFilterDescriptor: добавлен IsActive флаг для временного отключения фильтра
//   ✅ SgFilterGroup: комбинирование нескольких фильтров через AND/OR
//   ✅ Все типы — record (immutable, value-equality)

namespace SuperUI.Base;

/// <summary>Запрос данных для DataSource.</summary>
public record SgDataRequest
{
    /// <summary>Номер страницы (1-based).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Количество строк на странице.</summary>
    public int PageSize { get; init; } = 25;

    /// <summary>Параметры сортировки.</summary>
    public SgSortDescriptor? Sort { get; init; }

    /// <summary>Список активных фильтров.</summary>
    public IReadOnlyList<SgFilterDescriptor> Filters { get; init; } = [];

    /// <summary>Дополнительные произвольные параметры (передаются в DataSource).</summary>
    public IReadOnlyDictionary<string, object?>? ExtraParams { get; init; } = null;

    /// <summary>Отключить пагинацию — вернуть все записи.</summary>
    public bool NoPaging { get; init; }
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
}

/// <summary>Данные группы строк.</summary>
public record SgGroupData<T>(string GroupKey, string GroupLabel, IReadOnlyList<T> Items);

/// <summary>Дескриптор сортировки.</summary>
public record SgSortDescriptor(string Field, SgSortDirection Direction)
{
    /// <summary>Помещать null-значения в начало при сортировке.</summary>
    public bool NullsFirst { get; init; } = false;
}

/// <summary>Дескриптор фильтра.</summary>
public record SgFilterDescriptor(
    string Field,
    object? Value,
    SgFilterOperator Operator = SgFilterOperator.Contains,
    string? Value2 = null   // для Between
)
{
    /// <summary>Фильтр активен. false — фильтр временно отключён без удаления.</summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>Группа фильтров с логическим оператором.</summary>
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
    IsNotNull
}
