// SuperUI/Base/Reactive/SignalBatch.cs
// ИСПРАВЛЕНО:
// ✅ CS0121: ISignalFlushable объявлен ТОЛЬКО ЗДЕСЬ — убран из SgSignal.cs
// ✅ Добавлено предупреждение при достижении maxIterations (защита от потери уведомлений)
// ✅ Добавлен MarkDirty как публичный для ISignalFlushable реализаций

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Маркер для batch-flush.
/// ✅ ЕДИНСТВЕННОЕ объявление — убрать из SgSignal.cs!
/// </summary>
internal interface ISignalFlushable
{
    void FlushIfDirty();
}

/// <summary>
/// Батчинг уведомлений компонентов и эффектов.
/// AsyncLocal + ConcurrentQueue + Task.Yield для корректной работы в async/await.
/// </summary>
internal static class SignalBatch
{
    // Единая очередь, видимая из любого потока
    private static readonly ConcurrentQueue<Action> _workQueue = new();
    private static int _scheduled; // 0 = idle, 1 = scheduled

    // ── Signal batching ──────────────────────────────────────────────────────
    // AsyncLocal корректно работает с async/await (в отличие от [ThreadStatic])
    private static readonly AsyncLocal<int> _batchDepth = new();
    private static readonly ConcurrentDictionary<ISignalFlushable, bool> _dirtySignals = new();

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

        if (_batchDepth.Value == 0)
            FlushDirtySignals();
    }

    private static void FlushDirtySignals()
    {
        // Снимаем snapshot и очищаем
        var dirty = _dirtySignals.Keys.ToArray();
        _dirtySignals.Clear();

        foreach (var signal in dirty)
        {
            try { signal.FlushIfDirty(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch.FlushDirty] Error: {ex}");
            }
        }
    }

    /// <summary>
    /// ✅ FIX CS0121: единственная версия MarkDirty, принимает ISignalFlushable
    /// </summary>
    public static void MarkDirty(ISignalFlushable signal)
        => _dirtySignals[signal] = true;

    /// <summary>Алиас для совместимости с AddDirty.</summary>
    internal static void AddDirty(ISignalFlushable signal)
        => _dirtySignals[signal] = true;

    private sealed class BatchScope : IDisposable
    {
        private bool _disposed;

        public void Dispose() { if (!_disposed) { _disposed = true; End(); } }
    }

    // ── Enqueue ──────────────────────────────────────────────────────────────

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

    public static void NotifyComponent<T>(SgComputed<T> _) { } // computed не нужно notify

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Schedule()
    {
        // Task.Yield сохраняет SynchronizationContext (Blazor Server)
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
        const int MaxIterations = 1000;
        int processed = 0;

        while (_workQueue.TryDequeue(out var work) && processed < MaxIterations)
        {
            try { work(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch.Flush] Error: {ex}");
            }

            processed++;
        }

        // ✅ FIX: предупреждение при потере уведомлений
        if (processed >= MaxIterations && !_workQueue.IsEmpty)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalBatch.Flush] WARNING: MaxIterations ({MaxIterations}) reached. " +
                $"Remaining items: {_workQueue.Count}. Possible signal loop.");
        }

        Volatile.Write(ref _scheduled, 0);

        // Если во время Flush добавились новые элементы — планируем ещё раз
        if (!_workQueue.IsEmpty)
            Schedule();
    }

    // ── Scope (для SignalTracker) ──────────────────────────────────────────

    private static readonly AsyncLocal<int> _nestCount = new();
    private static readonly AsyncLocal<HashSet<ISgSignal>?> _trackedItems = new();

    public static void EnterScope()
    {
        if (_nestCount.Value == 0)
            _trackedItems.Value = new HashSet<ISgSignal>();

        _nestCount.Value++;
    }

    public static void ExitScope()
    {
        var n = _nestCount.Value;
        if (n <= 0) return;

        _nestCount.Value = n - 1;

        if (n == 1) _trackedItems.Value = null;
    }

    public static void TrackSignal(ISgSignal signal)
    {
        if (_nestCount.Value > 0)
            _trackedItems.Value?.Add(signal);
    }

    public static IReadOnlyCollection<ISgSignal> GetTracked()
        => _trackedItems.Value ?? (IReadOnlyCollection<ISgSignal>)Array.Empty<ISgSignal>();

    internal static void DisposeAll()
    {
        while (_workQueue.TryDequeue(out _)) { }
        _dirtySignals.Clear();
        Volatile.Write(ref _scheduled, 0);
    }
}
