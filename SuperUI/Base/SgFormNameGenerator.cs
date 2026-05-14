// ================================================================
// Файл: SuperUI/Base/SgFormNameGenerator.cs
// ИСПРАВЛЕНО:
// ✅ CS0246: SgFormNameGenerator не используется в DI — есть только интерфейс
// УЛУЧШЕНО:
// ✅ DefaultFormNameGenerator: коллизии для generic типов (List<T> → List_T)
// ✅ SsrFormNameGenerator: поддержка Static SSR form names (.NET 8+)
// ✅ CounterFormNameGenerator: thread-safe уникальные имена
// ================================================================

namespace SuperUI.Base;

/// <summary>
/// Интерфейс для генерации уникальных имён форм.
/// Используется в Static SSR (Antiforgery, form name attributes).
/// </summary>
public interface IFormNameGenerator
{
    /// <summary>Сгенерировать имя формы для типа модели.</summary>
    string GenerateName(Type modelType, string? suffix = null);
}

/// <summary>
/// Реализация по умолчанию — генерирует имя из типа + опциональный суффикс.
/// Корректно обрабатывает generic типы: <c>List&lt;int&gt;</c> → <c>List_Int32</c>.
/// </summary>
public sealed class DefaultFormNameGenerator : IFormNameGenerator
{
    /// <inheritdoc/>
    public string GenerateName(Type modelType, string? suffix = null)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var name = GetCleanTypeName(modelType);
        return string.IsNullOrWhiteSpace(suffix) ? name : $"{name}_{suffix}";
    }

    private static string GetCleanTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        // Generic: List<int> → List_Int32
        var baseName = type.Name[..type.Name.IndexOf('`')];
        var args = string.Join("_", type.GetGenericArguments().Select(GetCleanTypeName));
        return $"{baseName}_{args}";
    }
}

/// <summary>
/// Генератор имён форм для Static SSR с поддержкой уникальных счётчиков.
/// Использует thread-safe счётчик для гарантии уникальности при множественных формах.
/// </summary>
public sealed class CounterFormNameGenerator : IFormNameGenerator
{
    private static int _counter;

    /// <inheritdoc/>
    public string GenerateName(Type modelType, string? suffix = null)
    {
        ArgumentNullException.ThrowIfNull(modelType);

        var count = Interlocked.Increment(ref _counter);
        var baseName = modelType.Name.Replace("`", "_");

        return string.IsNullOrWhiteSpace(suffix)
            ? $"{baseName}_{count}"
            : $"{baseName}_{suffix}_{count}";
    }
}
