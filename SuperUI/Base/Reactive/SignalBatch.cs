// SuperUI/Base/Reactive/SignalBatch.cs
//
// УЛУЧШЕНИЯ:
//   1. Async-Begin() с автоматическим await dispose
//   2. IsBatching — публичное свойство
//   3. PendingCount — для диагностики
//   4. Обработка исключений в каждом компоненте независимо

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Батчинг уведомлений компонентов: несколько изменений сигналов за один тик
/// вызывают только один рендер на компонент.
/// </summary>
/// <remarks>
/// [ThreadStatic] — каждый circuit/поток имеет независимый batch.
/// WASM: один поток — batch всегда корректен.
/// Server: каждый circuit = отдельный поток → изоляция гарантирована.
/// </remarks>
public static class SignalBatch
{
    [ThreadStatic] private static int _depth;
    [ThreadStatic] private static HashSet<SgComponentBase>? _pending;

    /// <summary>true — активен batch scope (уведомления накапливаются).</summary>
    public static bool IsBatching => _depth > 0;

    /// <summary>Количество компонентов ожидающих уведомления (для диагностики).</summary>
    public static int PendingCount => _pending?.Count ?? 0;

    /// <summary>
    /// Начать batch scope. Все уведомления внутри будут накоплены
    /// и выполнены при Dispose.
    /// </summary>
    public static IDisposable Begin()
    {
        _depth++;
        return new BatchScope();
    }

    /// <summary>
    /// Async-версия batch scope.
    /// Используйте: await using var _ = SignalBatch.BeginAsync();
    /// </summary>
    public static IAsyncDisposable BeginAsync() => new AsyncBatchScope();

    /// <summary>
    /// Уведомить компонент об изменении.
    /// Если внутри batch — накапливается, иначе — немедленный рендер.
    /// </summary>
    internal static void NotifyComponent(SgComponentBase component)
    {
        if (component.IsDisposed) return;

        if (_depth > 0)
        {
            (_pending ??= new()).Add(component);
            return;
        }

        // Вне batch — немедленно, изолируем исключения
        _ = SafeRefreshAsync(component);
    }

    private static async Task SafeRefreshAsync(SgComponentBase component)
    {
        try { await component.RefreshAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[SignalBatch] RefreshAsync error for {component.ComponentId}: {ex.Message}");
        }
    }

    private static void Flush()
    {
        if (_pending is not { Count: > 0 }) return;

        var snapshot = new List<SgComponentBase>(_pending);
        _pending.Clear();

        foreach (var c in snapshot)
        {
            if (c.IsDisposed) continue;
            try
            {
                _ = c.RefreshAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SignalBatch] RefreshAsync error: {ex.Message}");
            }
        }
    }

    // ── BatchScope ────────────────────────────────────────────────────────────

    private sealed class BatchScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (--_depth > 0) return;   // вложенный scope — не флашим

            Flush();
        }
    }

    // ── AsyncBatchScope ───────────────────────────────────────────────────────

    private sealed class AsyncBatchScope : IAsyncDisposable
    {
        private bool _disposed;

        public ValueTask DisposeAsync()
        {
            if (_disposed) return ValueTask.CompletedTask;
            _disposed = true;

            if (--_depth > 0) return ValueTask.CompletedTask;

            Flush();
            return ValueTask.CompletedTask;
        }
    }
}
