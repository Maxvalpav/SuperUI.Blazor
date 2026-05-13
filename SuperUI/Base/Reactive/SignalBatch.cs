// SuperUI/Base/Reactive/SignalBatch.cs
// ✅ BUG-2 FIX: Убран [ThreadStatic] + Task.Run — ConcurrentQueue + AsyncLocal + Task.Yield
// ✅ THREAD: ConcurrentQueue + ConcurrentDictionary корректно работают между потоками
// ✅ PERF: Flush дренирует всю очередь за один проход
// ✅ Signal batching сохранён через AsyncLocal<int> + ConcurrentDictionary<ISignalFlushable>

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

internal interface ISignalFlushable
{
    void FlushIfDirty();
}

/// <summary>
/// Батчинг уведомлений компонентов и эффектов.
///
/// BUG-2 FIX: [ThreadStatic] поля не видны из Task.Run потоков.
/// Решение: ConcurrentQueue (разделяется между потоками) + AsyncLocal (async/await safe).
/// Schedule использует Task.Yield вместо Task.Run — сохраняет SynchronizationContext.
/// </summary>
internal static class SignalBatch
{
    // ✅ FIX BUG-2: единая очередь, видимая из любого потока
    private static readonly ConcurrentQueue<Action> _workQueue = new();
    private static int _scheduled; // 0 = idle, 1 = scheduled

    // ── Signal batching ───────────────────────────────────────────────────────

    // AsyncLocal корректно работает с async/await (в отличие от [ThreadStatic])
    private static readonly AsyncLocal<int> _batchDepth = new();
    private static readonly ConcurrentDictionary<object, bool> _dirtySignals = new();

    public static bool IsBatching => _batchDepth.Value > 0;

    public static IDisposable Begin()
    {
        _batchDepth.Value++;
        return new BatchScope();
    }

    public static void End()
    {
        var depth = _batchDepth.Value;
        if (depth <= 0) return;

        _batchDepth.Value = depth - 1;

        if (depth - 1 == 0)
        {
            foreach (var key in _dirtySignals.Keys.ToArray())
            {
                if (_dirtySignals.TryRemove(key, out _))
                {
                    try
                    {
                        if (key is ISignalFlushable flushable)
                            flushable.FlushIfDirty();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SignalBatch.End] Flush error: {ex.Message}");
                    }
                }
            }
        }
    }

    internal static void MarkDirty(object signal)
        => _dirtySignals[signal] = true;

    internal static void AddDirty(ISignalFlushable signal)
        => _dirtySignals[signal] = true;

    private sealed class BatchScope : IDisposable
    {
        private bool _disposed;
        public void Dispose() { if (!_disposed) { _disposed = true; End(); } }
    }

    // ── Enqueue ───────────────────────────────────────────────────────────────

    public static void EnqueueComponent(SgComponentBase component)
    {
        if (component.IsDisposed) return;
        _workQueue.Enqueue(component.RequestRender);
        Schedule();
    }

    public static void EnqueueEffect(SgEffect effect)
    {
        if (effect.IsDisposed) return;
        _workQueue.Enqueue(effect.Run);
        Schedule();
    }

    public static void NotifyComponent(SgComponentBase component)
        => EnqueueComponent(component);

    public static void NotifyComponent<T>(SgComputed<T> _) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Schedule()
    {
        // ✅ FIX BUG-2: Task.Yield сохраняет SynchronizationContext (Blazor Server)
        // Task.Run терял контекст и [ThreadStatic] поля были недоступны
        if (Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0)
            _ = ScheduleFlushAsync();
    }

    private static async Task ScheduleFlushAsync()
    {
        await Task.Yield(); // уступаем текущий стек, затем флашим в том же контексте
        Flush();
    }

    private static void Flush()
    {
        int maxIterations = 1000; // защита от бесконечного цикла
        while (_workQueue.TryDequeue(out var work) && maxIterations-- > 0)
        {
            try { work(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch.Flush] Error: {ex}");
            }
        }

        Volatile.Write(ref _scheduled, 0);

        // Если во время Flush добавились новые элементы — планируем ещё раз
        if (!_workQueue.IsEmpty)
            Schedule();
    }

    // ── Scope (для SignalTracker) ─────────────────────────────────────────────

    private static readonly AsyncLocal<int> _nestCount = new();
    private static readonly AsyncLocal<HashSet<object>?> _trackedItems = new();

    public static void EnterScope()
    {
        if (_nestCount.Value == 0)
            _trackedItems.Value = new HashSet<object>();
        _nestCount.Value++;
    }

    public static void ExitScope()
    {
        var n = _nestCount.Value;
        if (n <= 0) return;
        _nestCount.Value = n - 1;
        if (n - 1 == 0)
        {
            _trackedItems.Value?.Clear();
            _trackedItems.Value = null;
        }
    }

    public static IDisposable BlockScope()
    {
        EnterScope();
        return new ScopeHandle();
    }

    private sealed class ScopeHandle : IDisposable
    {
        public void Dispose() => ExitScope();
    }

    internal static void DisposeAll()
    {
        while (_workQueue.TryDequeue(out _)) { }
        _dirtySignals.Clear();
        Volatile.Write(ref _scheduled, 0);
    }
}
