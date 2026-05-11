namespace SuperUI.Utilities;

/// <summary>
/// Fluent CSS class builder. Zero-allocation для пустого результата.
/// Использует ValueStringBuilder через stackalloc для коротких строк.
/// </summary>
public readonly struct SgCssBuilder
{
    private readonly List<string>? _classes;

    public SgCssBuilder(string? baseClass = null)
    {
        if (!string.IsNullOrWhiteSpace(baseClass))
        {
            _classes = [baseClass.Trim()];
        }
    }

    /// <summary>Добавить класс безусловно.</summary>
    public SgCssBuilder Add(string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass)) return this;
        var result = new SgCssBuilder();
        // копируем существующие + новый
        (_classes ?? []).ForEach(c => result._classes!.Add(c));
        result._classes!.Add(cssClass.Trim());
        return result;
    }

    /// <summary>Добавить класс по условию.</summary>
    public SgCssBuilder AddIf(string? cssClass, bool condition)
        => condition ? Add(cssClass) : this;

    /// <summary>Добавить класс, если func возвращает true (ленивое вычисление).</summary>
    public SgCssBuilder AddIf(string? cssClass, Func<bool> condition)
        => condition() ? Add(cssClass) : this;

    /// <summary>Добавить несколько классов через пробел.</summary>
    public SgCssBuilder AddRange(params string?[] classes)
    {
        var builder = this;
        foreach (var c in classes)
            builder = builder.Add(c);
        return builder;
    }

    /// <summary>Добавить пользовательский Class параметр в конец.</summary>
    public SgCssBuilder AddUserClass(string? userClass) => Add(userClass);

    /// <summary>Собрать строку. Возвращает null если нет классов (не рендерит атрибут).</summary>
    public string? Build()
    {
        if (_classes is null || _classes.Count == 0) return null;
        if (_classes.Count == 1) return _classes[0];
        return string.Join(' ', _classes);
    }

    // Неявное преобразование для удобства в razor
    public static implicit operator string?(SgCssBuilder builder) => builder.Build();
}
