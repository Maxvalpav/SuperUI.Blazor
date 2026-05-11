// Файл: Utilities/AdditionalAttributesMerger.cs
// Зависимости: NONE (pure C#)
// GC: минимальный, EmptyDictionary singleton для пустых случаев

namespace SuperUI.Utilities;

/// <summary>
/// Интерфейс сервиса слияния дополнительных HTML-атрибутов.
/// </summary>
public interface IAdditionalAttributesMerger
{
    IReadOnlyDictionary<string, object?> Merge(
        IReadOnlyDictionary<string, object?>? componentAttributes,
        IReadOnlyDictionary<string, object?>? userAttributes);
}

/// <summary>
/// Сервис слияния дополнительных HTML-атрибутов.
/// Решает проблему: компонент принимает UserAttributes (CaptureUnmatchedValues),
/// но нужно корректно мержить их с внутренними атрибутами (class, style, aria-*).
/// 
/// ПРАВИЛА СЛИЯНИЯ:
/// - class: объединяются через пробел
/// - style: объединяются через "; " (пользовательские побеждают)
/// - id: пользовательский побеждает
/// - aria-*: пользовательские атрибуты имеют приоритет (allow override)
/// - остальные: пользовательские атрибуты имеют приоритет
/// </summary>
public sealed class AdditionalAttributesMerger : IAdditionalAttributesMerger
{
    /// <summary>Singleton instance для DI.</summary>
    public static readonly IAdditionalAttributesMerger Instance = new AdditionalAttributesMerger();

    /// <summary>
    /// Слить внутренние атрибуты компонента с пользовательскими.
    /// </summary>
    public IReadOnlyDictionary<string, object?> Merge(
        IReadOnlyDictionary<string, object?>? componentAttributes,
        IReadOnlyDictionary<string, object?>? userAttributes)
    {
        if (componentAttributes is null && userAttributes is null)
            return EmptyDictionary.Instance;
        if (userAttributes is null)
            return componentAttributes!;
        if (componentAttributes is null)
            return userAttributes;

        var result = new Dictionary<string, object?>(
            componentAttributes.Count + userAttributes.Count,
            StringComparer.OrdinalIgnoreCase);

        // Сначала добавляем внутренние
        foreach (var (key, value) in componentAttributes)
            result[key] = value;

        // Потом мержим пользовательские
        foreach (var (key, value) in userAttributes)
        {
            if (string.Equals(key, "class", StringComparison.OrdinalIgnoreCase))
            {
                // Объединяем классы
                var existing = result.TryGetValue("class", out var ec) ? ec?.ToString() : null;
                result["class"] = string.IsNullOrWhiteSpace(existing)
                    ? value?.ToString()
                    : $"{existing} {value}";
            }
            else if (string.Equals(key, "style", StringComparison.OrdinalIgnoreCase))
            {
                // Объединяем стили (пользовательские в конце = приоритет)
                var existing = result.TryGetValue("style", out var es) ? es?.ToString() : null;
                result["style"] = string.IsNullOrWhiteSpace(existing)
                    ? value?.ToString()
                    : $"{existing}; {value}";
            }
            else
            {
                // Пользовательские значения имеют приоритет
                result[key] = value;
            }
        }

        return result;
    }

    IReadOnlyDictionary<string, object?> IAdditionalAttributesMerger.Merge(
        IReadOnlyDictionary<string, object?>? componentAttributes,
        IReadOnlyDictionary<string, object?>? userAttributes)
        => Merge(componentAttributes, userAttributes);

    private static class EmptyDictionary
    {
        public static readonly IReadOnlyDictionary<string, object?> Instance =
            new Dictionary<string, object?>();
    }
}
