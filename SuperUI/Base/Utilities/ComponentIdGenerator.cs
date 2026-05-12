// SuperUI/Base/Utilities/ComponentIdGenerator.cs
//
// Генератор уникальных ID для компонентов.
// Thread-safe через Interlocked.Increment.
// WASM + Server совместимо.

namespace SuperUI.Base.Utilities;

/// <summary>
/// Генератор уникальных ID компонентов.
/// </summary>
/// <remarks>
/// Thread-safe: <see cref="Interlocked.Increment"/> атомарен на всех платформах.
/// Формат: "{prefix}-{counter}" — детерминированный, предсказуемый для тестов.
/// Overflow: int.MaxValue → переполнение в отрицательные (крайне маловероятно при ~2млрд компонентах).
/// </remarks>
public static class ComponentIdGenerator
{
    private static int _counter;

    /// <summary>Сгенерировать следующий уникальный ID с заданным префиксом.</summary>
    public static string Next(string prefix = "cmp")
        => string.IsNullOrWhiteSpace(prefix)
            ? $"cmp-{Interlocked.Increment(ref _counter)}"
            : $"{prefix}-{Interlocked.Increment(ref _counter)}";

    /// <summary>Сбросить счётчик (только для unit-тестов).</summary>
    internal static void Reset() => Interlocked.Exchange(ref _counter, 0);
}
