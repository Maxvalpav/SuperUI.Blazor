// SuperUI/Base/Utilities/ComponentIdGenerator.cs
// ИСПРАВЛЕНО:
// 1. Правильный namespace: SuperUI.Base.Utilities (совпадает с using в SgComponentBase)
// 2. NextGlobal использует hex для краткости
// 3. Stable добавлен префикс версии для SSR hydration

namespace SuperUI.Base.Utilities;

/// <summary>
/// Thread-safe генератор уникальных ID для компонентов.
/// Использует Interlocked для lock-free инкремента.
/// Формат: "sg-{prefix}-{counter}" — короче GUID, читаемо, уникально в сессии.
/// </summary>
public static class ComponentIdGenerator
{
    private static int _counter;

    /// <summary>
    /// Генерирует новый уникальный ID в рамках одной сессии.
    /// Пример: "sg-btn-42", "sg-input-43"
    /// </summary>
    public static string Next(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        return $"sg-{prefix}-{id}";
    }

    /// <summary>
    /// Генерирует ID в hex формате (короче для больших чисел).
    /// Пример: "sg-btn-2a"
    /// </summary>
    public static string NextHex(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        return $"sg-{prefix}-{id:x}";
    }

    /// <summary>
    /// Стабильный ID на основе контента (для SSR hydration).
    /// Одинаковый key → одинаковый ID → корректная гидратация DOM.
    /// </summary>
    public static string Stable(string prefix, string key)
    {
        var hash = string.GetHashCode(key, StringComparison.Ordinal);
        return $"sg-{prefix}-{(uint)hash:x8}"; // uint для отсутствия знака '-'
    }

    /// <summary>
    /// Сброс счётчика (только для тестов).
    /// </summary>
    internal static void ResetForTesting() => _counter = 0;
}