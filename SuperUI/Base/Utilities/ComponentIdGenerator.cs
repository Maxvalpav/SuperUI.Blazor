// SuperUI/Base/Utilities/ComponentIdGenerator.cs
// ✅ УЛУЧШЕНИЯ:
//   - NextFor<T>() — читаемые имена по типу компонента
//   - Reset() помечен [Conditional("TESTING")] для тестов

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Генератор уникальных ID компонентов SuperUI.
/// Thread-safe. Формат: "{prefix}-{counter}".
/// </summary>
public static class ComponentIdGenerator
{
    private static int _counter;

    /// <summary>
    /// Сгенерировать уникальный ID.
    /// </summary>
    /// <param name="prefix">Префикс (например "btn", "input").</param>
    /// <returns>Строка вида "btn-42".</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Next(string prefix = "sg")
    {
        var id = Interlocked.Increment(ref _counter);
        return $"{prefix}-{id}";
    }

    /// <summary>
    /// Сгенерировать ID по типу компонента.
    /// Пример: NextFor&lt;SgButton&gt;() → "button-5"
    /// </summary>
    /// <typeparam name="T">Тип компонента.</typeparam>
    /// <param name="prefix">Явный префикс. Если null — выводится из имени типа.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string NextFor<T>(string? prefix = null)
    {
        var p = prefix ?? DerivePrefix(typeof(T).Name);
        return Next(p);
    }

    /// <summary>
    /// Сгенерировать ID по имени типа (runtime).
    /// </summary>
    public static string NextFor(Type type, string? prefix = null)
    {
        var p = prefix ?? DerivePrefix(type.Name);
        return Next(p);
    }

    // "SgButton" → "button", "SgDataGrid" → "datagrid"
    private static string DerivePrefix(string typeName)
    {
        var name = typeName.AsSpan();
        if (name.StartsWith("Sg", StringComparison.Ordinal))
            name = name[2..];
        return name.ToString().ToLowerInvariant();
    }

    /// <summary>Сброс счётчика. Только для тестов!</summary>
    [Conditional("TESTING")]
    internal static void Reset() => Interlocked.Exchange(ref _counter, 0);

    /// <summary>Текущее значение счётчика (для диагностики).</summary>
    public static int CurrentCount => Volatile.Read(ref _counter);
}
