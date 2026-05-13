// SuperUI/Base/Reactive/SignalBatch.cs
// ИСПРАВЛЕНИЯ:
// ✅ DUPLICATE FIX: ISignalFlushable определён ТОЛЬКО здесь (убрать из SgSignal.cs и SgComputed.cs)
// ✅ BUG-2 FIX: [ThreadStatic] → ConcurrentQueue + AsyncLocal
// ✅ WASM: Task.Yield сохраняет SynchronizationContext
// ✅ AOT: нет dynamic
// ✅ NET8: совместим

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс для сигналов/computed, которые могут сбрасывать состояние dirty.
/// ЕДИНСТВЕННОЕ определение — только в этом файле.
/// Из SgSignal.cs и SgComputed.cs — УДАЛИТЬ дублирующее определение.
/// </summary>
internal interface ISignalFlushable
{
    void FlushIfDirty();
}

/// <summary>
/// Батчинг уведомлений компонентов и эффектов.
///
/// BUG-2 FIX: [ThreadStatic] → ConcurrentQueue + AsyncLocal.
/// [ThreadStatic] не работает корректно при Task.Run / thread-pool переключениях.
/// AsyncLocal корректно пробрасывается через async/await цепочки.
///
/// Blazor Server: один circuit = один поток = AsyncLocal работает.
/// Blazor WASM: однопоточный = нет race conditions.
/// </summary>
internal static class SignalBatch
{
    // Единая очередь работ, видимая из любого потока
    private static readonly ConcurrentQueue<Action> _workQueue = new();
    private static int _scheduled; // 0 = idle, 1 = scheduled

    // ── Signal batching ───────────────────────────────────────────────────────

    // AsyncLocal корректно работает с async/await (в отличие от [ThreadStatic])
    private static readonly AsyncLocal<int> _batchDepth = new();
    private static readonly ConcurrentDictionary<ISignalFlushable, bool> _dirtySignals = new();

    public static bool IsBatching => _batchDepth.Value > 0;

    /// <summary>Начать batch — все Set() откладываются до конца batch.</summary>
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

        if (depth == 1)
        {
            // Flush всех dirty сигналов
            var dirty = _dirtySignals.Keys.ToArray();
            _dirtySignals.Clear();

            foreach (var signal in dirty)
                signal.FlushIfDirty();
        }
    }

    public static void MarkDirty(ISignalFlushable signal)
        => _dirtySignals[signal] = true;

    private sealed class BatchScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                End();
            }
        }
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Schedule()
    {
        // Task.Yield сохраняет SynchronizationContext (Blazor Server circuit)
        // Task.Run потеряет контекст → SignalBatch не работал бы корректно
        if (Interlocked.CompareExchange(ref _scheduled, 1, 0) == 0)
            _ = ScheduleFlushAsync();
    }

    private static async Task ScheduleFlushAsync()
    {
        await Task.Yield();
        Flush();
    }

    private static void Flush()
    {
        int maxIterations = 1000; // защита от бесконечного цикла

        while (_workQueue.TryDequeue(out var work) && maxIterations-- > 0)
        {
            try
            {
                work();
            }
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

    // ── Scope для SignalTracker ────────────────────────────────────────────────

    private static readonly AsyncLocal<int> _nestCount = new();
    private static readonly AsyncLocal<HashSet<ISignalFlushable>?> _trackedItems = new();

    public static void EnterScope()
    {
        if (_nestCount.Value == 0)
            _trackedItems.Value = new HashSet<ISignalFlushable>();

        _nestCount.Value++;
    }

    public static void ExitScope()
    {
        var n = _nestCount.Value;
        if (n <= 0) return;

        _nestCount.Value = n - 1;
    }

    public static IDisposable TrackingScope() => new ScopeToken();

    private sealed class ScopeToken : IDisposable
    {
        public ScopeToken() => EnterScope();

        public void Dispose() => ExitScope();
    }

    internal static void DisposeAll()
    {
        while (_workQueue.TryDequeue(out _)) { }
        _dirtySignals.Clear();
        Volatile.Write(ref _scheduled, 0);
    }
}
