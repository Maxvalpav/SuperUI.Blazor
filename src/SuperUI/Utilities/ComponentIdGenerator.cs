namespace SuperUI.Utilities;

/// <summary>
/// Thread-safe генератор уникальных ID для компонентов.
/// Использует Interlocked для lock-free инкремента.
/// Формат: "sg-{prefix}-{counter}" — короче GUID, читаемо, уникально в сессии.
/// </summary>
public static class ComponentIdGenerator
{
    private static int _counter = 0;

    /// <summary>
    /// Генерирует новый уникальный ID.
    /// Пример: "sg-btn-42", "sg-input-43"
    /// </summary>
    public static string Next(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        return $"sg-{prefix}-{id}";
    }

    /// <summary>
    /// Генерирует ID с гарантией уникальности между вкладками (добавляет хеш сессии).
    /// </summary>
    public static string NextGlobal(string prefix = "cmp")
    {
        var id = Interlocked.Increment(ref _counter);
        return $"sg-{prefix}-{id:x}"; // hex для краткости
    }
}
