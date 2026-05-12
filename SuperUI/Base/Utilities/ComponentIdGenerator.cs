// SuperUI/Base/Utilities/ComponentIdGenerator.cs

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Генератор уникальных ID для компонентов SuperUI.
/// Thread-safe: Interlocked.Increment.
/// WASM-safe: однопоточный, но Interlocked корректен на ARM.
/// </summary>
public static class ComponentIdGenerator
{
    private static int _counter = 0;

    /// <summary>
    /// Сгенерировать уникальный ID вида "prefix-N".
    /// </summary>
    /// <param name="prefix">Префикс компонента (напр. "btn", "inp", "cmp").</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Next(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        return string.Concat(prefix, "-", id.ToString());
    }

    /// <summary>Сброс счётчика (только для тестов!).</summary>
    internal static void Reset() => _counter = 0;
}
