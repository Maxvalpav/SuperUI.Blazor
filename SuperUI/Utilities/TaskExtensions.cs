// Файл: Utilities/TaskExtensions.cs
// Зависимости: NONE

namespace SuperUI.Utilities;

/// <summary>
/// Extension-методы для работы с Task/ValueTask.
/// </summary>
internal static class TaskExtensions
{
    /// <summary>Конвертирует Task в ValueTask без аллокации.</summary>
    public static ValueTask AsValueTask(this Task task) => new(task);

    /// <summary>Конвертирует Task&lt;T&gt; в ValueTask&lt;T&gt; без аллокации.</summary>
    public static ValueTask<T> AsValueTask<T>(this Task<T> task) => new(task);
}
