// SuperUI/Base/Utilities/ComponentIdGenerator.cs

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Генератор уникальных ID компонентов.
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

    /// <summary>Сброс счётчика (только для тестов!).</summary>
    internal static void Reset() => _counter = 0;
}
