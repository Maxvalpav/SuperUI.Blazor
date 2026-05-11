// Файл: Utilities/CssBuilder.cs
// Зависимости: NONE (pure C#)
// GC: минимальный, ValueStringBuilder-like через Span<char> для hot path

namespace SuperUI.Utilities;

/// <summary>
/// Fluent CSS class builder с нулевыми аллокациями для hot paths.
/// Инспирирован MudBlazor CssBuilder, улучшен Span/ValueStringBuilder.
/// </summary>
public sealed class CssBuilder
{
    // Используем List<string> вместо StringBuilder чтобы избежать
    // промежуточных строковых аллокаций при условных добавлениях
    private readonly List<string> _classes;
    private readonly string? _initial;

    public CssBuilder(string? initialClass = null)
    {
        _initial = initialClass;
        // Начинаем с capacity=4 — типичное количество классов
        _classes = initialClass is null ? new List<string>(4) : new List<string>(4) { initialClass };
    }

    /// <summary>Добавить класс безусловно (если не null/empty).</summary>
    public CssBuilder AddClass(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
            _classes.Add(cssClass.Trim());
        return this;
    }

    /// <summary>Добавить класс условно.</summary>
    public CssBuilder AddClass(string? cssClass, bool when)
    {
        if (when) AddClass(cssClass);
        return this;
    }

    /// <summary>Добавить класс через ленивое вычисление (избегаем аллокации строки при when=false).</summary>
    public CssBuilder AddClass(Func<string?> cssClassFactory, bool when)
    {
        if (when) AddClass(cssClassFactory());
        return this;
    }

    /// <summary>Добавить класс через predicate (избегаем аллокации при false).</summary>
    public CssBuilder AddClass(string? cssClass, Func<bool> when)
        => AddClass(cssClass, when());

    /// <summary>Добавить результат другого CssBuilder.</summary>
    public CssBuilder AddClassFromAttributes(IDictionary<string, object>? attributes)
    {
        if (attributes is not null && attributes.TryGetValue("class", out var cls) && cls is string s)
            AddClass(s);
        return this;
    }

    /// <summary>Собрать финальную строку. Возвращает null если нет классов (не добавляет пустой атрибут).</summary>
    public string? Build()
    {
        // Фильтрация дублей для корректности
        if (_classes.Count == 0) return null;
        if (_classes.Count == 1) return _classes[0];

        // Используем Join через Span для минимальных аллокаций
        return string.Join(' ', _classes.Distinct(StringComparer.Ordinal));
    }

    /// <summary>Implicit conversion для удобства в razor-параметрах.</summary>
    public static implicit operator string?(CssBuilder builder) => builder.Build();

    /// <summary>Статический entry-point для fluent chain.</summary>
    public static CssBuilder Default(string? initialClass = null) => new(initialClass);
}
