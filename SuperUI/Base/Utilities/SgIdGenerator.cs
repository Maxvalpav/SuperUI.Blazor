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
/// <para><see cref="StableIdFor{T}(T, string)"/> дополнительно принимает generic
/// тип-параметр и добавляет его имя в префикс — предотвращает коллизии между
/// SgModal и SgDrawer при одинаковом префиксе "sg".</para>
/// </remarks>
public static class SgIdGenerator
{
    private static long _counter;

    // ConditionalWeakTable не удерживает key от GC — безопасно для компонентов.
    private static readonly ConditionalWeakTable<object, StableIdHolder> _stableIds = new();

    /// <summary>
    /// Создаёт новый уникальный ID вида <c>{prefix}-{base36}</c>.
    /// </summary>
    public static string NewId(string prefix = "sg")
    {
        var n = Interlocked.Increment(ref _counter);
        return $"{prefix}-{ToBase36(n)}";
    }

    /// <summary>
    /// Возвращает стабильный ID для <paramref name="owner"/>.
    /// </summary>
    public static string StableIdFor(object owner, string prefix = "sg")
    {
        ArgumentNullException.ThrowIfNull(owner);
        return _stableIds
            .GetOrCreateValue(owner)
            .GetOrCreate(prefix);
    }

    /// <summary>
    /// Возвращает стабильный ID для <paramref name="owner"/>, включающий
    /// имя типа <typeparamref name="T"/> в префикс (например, <c>sg-modal-a1b2c</c>).
    /// </summary>
    /// <remarks>
    /// Полезно когда у нескольких компонентов одинаковый логический префикс
    /// (например, "title") и хочется избежать коллизий в одном DOM-дереве.
    /// </remarks>
    public static string StableIdFor<T>(T owner, string suffix = "", string typePrefix = "")
        where T : class
    {
        ArgumentNullException.ThrowIfNull(owner);
        var prefix = string.IsNullOrEmpty(typePrefix)
            ? DefaultPrefixForType<T>()
            : typePrefix;
        return StableIdFor((object)owner, prefix + suffix);
    }

    private static string DefaultPrefixForType<T>()
    {
        var t = typeof(T);
        // Strip "Sg" prefix and "Component" suffix for shorter IDs.
        var name = t.Name;
        if (name.StartsWith("Sg", StringComparison.Ordinal)) name = name[2..];
        if (name.EndsWith("Component", StringComparison.Ordinal)) name = name[..^9];
        if (name.EndsWith("Base", StringComparison.Ordinal)) name = name[..^4];
        return "sg-" + name.ToLowerInvariant();
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
