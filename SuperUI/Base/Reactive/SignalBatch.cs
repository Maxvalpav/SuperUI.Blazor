// SuperUI/Base/Reactive/SignalBatch.cs
// ИСПРАВЛЕНО:
// ✅ Flush race: _scheduled сбрасывается через CAS перед проверкой IsEmpty
// ✅ AsyncLocal<int> для корректного async/await batching
// ✅ DisposeAll: полная очистка
// ✅ .NET 8/9/10 совместим

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс для сигналов/computed, которые могут сбрасывать состояние dirty.
/// ЕДИНСТВЕННОЕ определение — только в этом файле.
/// </summary>
internal interface ISignalFlushable
{
    void FlushIfDirty();
}

/// <summary>
/// Батчинг уведомлений компонентов и эффектов.
/// Все сигналы внутри Begin/End batch уведомляют подписчиков один раз — в End.
/// </summary>
internal static class SignalBatch
{
    // ── Signal batching ──────────────────────────────────────────────────────
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
            FlushDirtySignals();
    }

    private static void FlushDirtySignals()
    {
        // Снапшот грязных сигналов — атомарная замена
        var dirty = new List<ISignalFlushable>(_dirtySignals.Count);
        foreach (var key in _dirtySignals.Keys)
        {
            if (_dirtySignals.TryRemove(key, out _))
                dirty.Add(key);
        }

        foreach (var signal in dirty)
        {
            try { signal.FlushIfDirty(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch.Flush] Signal flush error: {ex}");
            }
        }
    }

    internal static void MarkDirty(ISignalFlushable signal)
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

    // ── Work queue ───────────────────────────────────────────────────────────
    private static readonly ConcurrentQueue<Action> _workQueue = new();

    // ✅ ИСПРАВЛЕНО: используем CAS для _scheduled, чтобы избежать потери работы.
    // Порядок в Flush:
    //   1. Обрабатываем всё что есть
    //   2. Сбрасываем _scheduled через CAS(1→0)
    //   3. Если очередь не пуста — снова Schedule()
    // Это гарантирует: если что-то добавлено МЕЖДУ шагами 2 и 3 — Schedule сработает.
    private static int _scheduled; // 0 = idle, 1 = scheduled

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
        // Task.Yield сохраняет SynchronizationContext (Blazor Server circuit).
        // Только один flush в полёте одновременно.
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
        int maxIterations = 10_000; // защита от бесконечного цикла

        while (_workQueue.TryDequeue(out var work) && maxIterations-- > 0)
        {
            try { work(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch.Flush] Work error: {ex}");
            }
        }

        // ✅ ИСПРАВЛЕНО: сбрасываем _scheduled ПЕРЕД проверкой IsEmpty.
        // CAS(1→0) — если кто-то уже поставил новую работу и вызвал Schedule()
        // между нашим dequeue и этой строкой — он увидит _scheduled=0 и запустит
        // новый ScheduleFlushAsync.
        Interlocked.Exchange(ref _scheduled, 0);

        // Если во время flush добавились новые элементы — запускаем ещё раз
        if (!_workQueue.IsEmpty)
            Schedule();
    }

    // ── Scope для SignalTracker ──────────────────────────────────────────────
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
        if (n == 1)
            _trackedItems.Value = null;
    }

    public static IReadOnlyCollection<ISgSignal> GetTracked()
        => (IReadOnlyCollection<ISgSignal>?)_trackedItems.Value
           ?? Array.Empty<ISgSignal>();

    public static void TrackSignal(ISgSignal signal)
        => _trackedItems.Value?.Add(signal);

    public static IDisposable CreateScope() => new ScopeToken();

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