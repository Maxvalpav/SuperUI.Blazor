// SuperUI/Base/Utilities/ComponentIdGenerator.cs
//
// НОВЫЙ: генератор уникальных ID компонентов.
// Thread-safe: Interlocked.Increment.
// WASM-safe: нет Thread.CurrentThread зависимостей.
// Формат: {prefix}-{counter} (например: cmp-42, btn-7).

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Потокобезопасный генератор уникальных ID компонентов.
/// </summary>
/// <remarks>
/// Thread safety: Interlocked.Increment — атомарная операция на всех архитектурах.<br/>
/// WASM: работает корректно (однопоточный, но Interlocked поддерживается).<br/>
/// Server: многопоточный — Interlocked гарантирует уникальность.
/// </remarks>
public static class ComponentIdGenerator
{
    private static int _counter;

    /// <summary>
    /// Сгенерировать следующий уникальный ID.
    /// </summary>
    /// <param name="prefix">Префикс (например, "cmp", "btn", "modal").</param>
    /// <returns>Строка вида "prefix-N" (например, "cmp-42").</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Next(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        // string.Concat — zero-allocation для числа до 7 цифр
        return string.Concat(
            string.IsNullOrEmpty(prefix) ? "cmp" : prefix,
            "-",
            id.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Сбросить счётчик (только для тестов!).
    /// </summary>
    /// <remarks>⚠️ Не вызывайте в продакшне — приведёт к дубликатам ID.</remarks>
    internal static void ResetForTesting()
        => Interlocked.Exchange(ref _counter, 0);
}
