// SuperUI/Base/Utilities/SgIdGenerator.cs
// Генератор уникальных и стабильных HTML-идентификаторов.
// Решает проблему hydration mismatch: GUID в поле-инициализаторе
// генерируется отдельно на сервере (prerender) и клиенте.

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Генератор HTML-идентификаторов для компонентов SuperUI.
/// </summary>
/// <remarks>
/// <para><see cref="NewId"/> использует <see cref="Interlocked.Increment"/>
/// — монотонно возрастающий счётчик. Гарантирует уникальность в пределах процесса,
/// но НЕ гарантирует одинаковый ID на сервере и клиенте при prerender.
/// Используйте только для ID, не участвующих в HTML-гидрации.</para>
/// <para><see cref="StableIdFor"/> через <see cref="ConditionalWeakTable{TKey,TValue}"/>
/// возвращает один и тот же ID для одного owner-объекта на протяжении его жизни.
/// Используйте для ARIA-связей между родителем и детьми.</para>
/// </remarks>
public static class SgIdGenerator
{
    private static long _counter;

    // ConditionalWeakTable не удерживает key от GC — безопасно для компонентов.
    private static readonly ConditionalWeakTable<object, StableIdHolder> _stableIds = new();

    /// <summary>
    /// Создаёт новый уникальный ID вида <c>{prefix}-{base36}</c>.
    /// </summary>
    /// <param name="prefix">Префикс. По умолчанию <c>"sg"</c>.</param>
    /// <returns>Строка вида <c>"sg-a1b2c3"</c>.</returns>
    public static string NewId(string prefix = "sg")
    {
        var n = Interlocked.Increment(ref _counter);
        return $"{prefix}-{ToBase36(n)}";
    }

    /// <summary>
    /// Возвращает стабильный ID для данного <paramref name="owner"/>.
    /// Повторные вызовы с одним объектом возвращают одинаковую строку.
    /// </summary>
    /// <param name="owner">Объект-владелец (обычно <c>this</c> в компоненте).</param>
    /// <param name="prefix">Префикс. По умолчанию <c>"sg"</c>.</param>
    public static string StableIdFor(object owner, string prefix = "sg")
    {
        ArgumentNullException.ThrowIfNull(owner);
        return _stableIds
            .GetOrCreateValue(owner)
            .GetOrCreate(prefix);
    }

    // Base-36 кодирование: цифры + строчные буквы латиницы.
    private static string ToBase36(long value)
    {
        const string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
        if (value == 0) return "0";

        Span<char> buffer = stackalloc char[16];
        int pos = buffer.Length;
        long v = Math.Abs(value);

        while (v > 0)
        {
            buffer[--pos] = chars[(int)(v % 36)];
            v /= 36;
        }

        return new string(buffer[pos..]);
    }

    // Вспомогательный класс для ConditionalWeakTable (должен быть reference type).
    private sealed class StableIdHolder
    {
        private string? _id;

        public string GetOrCreate(string prefix)
            => _id ??= NewId(prefix);
    }
}