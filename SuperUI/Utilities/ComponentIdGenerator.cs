// Файл: Utilities/ComponentIdGenerator.cs
// Зависимости: NONE
// Thread-safe через Interlocked (lock-free)

namespace SuperUI.Utilities;

/// <summary>
/// Thread-safe генератор уникальных ID для компонентов.
/// Использует Interlocked.Increment — lock-free, O(1), без аллокаций счётчика.
/// Избегает Guid.NewGuid() (16 байт + string allocation) для каждого компонента.
/// 
/// Формат: "sg-{prefix}-{counter}" — детерминированный в рамках сессии,
/// понятный в DevTools, компактный.
/// </summary>
public static class ComponentIdGenerator
{
    // Volatile не нужен — Interlocked сам обеспечивает memory barrier
    private static int _counter = 0;

    /// <summary>
    /// Генерирует уникальный ID с префиксом компонента.
    /// Пример: "sg-btn-42", "sg-input-43"
    /// </summary>
    public static string Next(string componentPrefix = "comp")
    {
        var id = Interlocked.Increment(ref _counter);
        // string.Create для минимальных аллокаций
        return string.Create(
            componentPrefix.Length + 4 + CountDigits(id), // "sg-" + prefix + "-" + digits
            (componentPrefix, id),
            static (span, state) =>
            {
                var (prefix, counter) = state;
                "sg-".CopyTo(span);
                int pos = 3;
                prefix.AsSpan().CopyTo(span[pos..]);
                pos += prefix.Length;
                span[pos++] = '-';
                counter.TryFormat(span[pos..], out _);
            });
    }

    /// <summary>Генерирует ID для aria атрибутов (без дефисов в начале).</summary>
    public static string NextAriaId(string context)
        => Next(context);

    private static int CountDigits(int n)
    {
        if (n < 10) return 1;
        if (n < 100) return 2;
        if (n < 1000) return 3;
        if (n < 10000) return 4;
        return 5; // int.MaxValue = 2147483647 = 10 digits, но нам достаточно
    }
}
