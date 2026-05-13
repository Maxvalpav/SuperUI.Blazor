// SuperUI/Base/SgModelHasher.cs
// НОВОЕ: Быстрый хэш модели без JSON-сериализации
// Использует IEqualityComparer или GetHashCode для эффективного сравнения

namespace SuperUI.Base;

/// <summary>
/// Быстрый хэш модели для детекции изменений.
/// Использует <see cref="IEqualityComparer{T}"/> вместо JSON-сериализации.
/// </summary>
public static class SgModelHasher<TModel> where TModel : class
{
    private static IEqualityComparer<TModel>? _customComparer;

    /// <summary>Зарегистрировать кастомный компаратор для типа модели.</summary>
    public static void RegisterComparer(IEqualityComparer<TModel> comparer)
        => _customComparer = comparer;

    /// <summary>
    /// Быстрое вычисление хэша модели.
    /// Порядок: 1) кастомный компаратор, 2) IEquatable, 3) JSON fallback.
    /// </summary>
    public static string ComputeHash(TModel model)
    {
        if (_customComparer is not null)
            return _customComparer.GetHashCode(model).ToString("x8");

        if (model is IEquatable<TModel> equatable)
            return equatable.GetHashCode().ToString("x8");

        // Fallback: JSON (медленно, но надёжно для DTO)
        var json = System.Text.Json.JsonSerializer.Serialize(model);
        return ComputeFnv1a(json);
    }

    private static string ComputeFnv1a(string text)
    {
        unchecked
        {
            uint hash = 2166136261u;
            foreach (var b in System.Text.Encoding.UTF8.GetBytes(text))
                hash = (hash ^ b) * 16777619u;
            return hash.ToString("x8");
        }
    }
}
