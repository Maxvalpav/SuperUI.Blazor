// SuperUI/Base/Reactive/SignalBatch.cs
// ✅ Dispose: отменяет все запланированные задачи (очистка очередей)
// ✅ EnqueueComponent/Effect: игнорирует disposed
// ✅ NotifyComponent: внешний API для сигналов/computed
// ✅ Nested scope: ExitScope очищает _current
// ✅ Signal batching: Begin/End/IsBatching/AddDirty — атомарное применение изменений

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>Внутренний интерфейс для сигналов, поддерживающих батчинг.</summary>
internal interface ISignalFlushable
{
    void FlushIfDirty();
}

/// <summary>
/// Батчинг уведомлений компонентов: несколько изменений сигналов за один тик
/// вызывают только один рендер на компонент.
///
/// Также поддерживает батчинг самих сигналов через Begin()/End():
/// все изменения внутри блока накапливаются и применяются атомарно в End().
///
/// Пример:
/// <code>
/// using (SignalBatch.Begin())
/// {
///     _count.Value++;
///     _name.Value = "New";
///     _items.Value = _items.Value.Add(newItem);
/// } // здесь произойдёт одна нотификация
/// </code>
/// </summary>
internal static class SignalBatch
{
    [ThreadStatic] private static ConcurrentQueue<Action>? _queue;
    [ThreadStatic] private static bool _scheduled;
    [ThreadStatic] private static int _nestCount;
    [ThreadStatic] private static HashSet<object>? _trackedItems;

    // ── Signal batching ───────────────────────────────────────────────────────

    [ThreadStatic] private static int _batchDepth;
    [ThreadStatic] private static HashSet<ISignalFlushable>? _dirtySignals;

    /// <summary>Находимся ли внутри батча сигналов.</summary>
    public static bool IsBatching => _batchDepth > 0;

    /// <summary>
    /// Начать батч сигналов. Все изменения накапливаются до End().
    /// Возвращает IDisposable для using-синтаксиса.
    /// </summary>
    public static IDisposable Begin()
    {
        _batchDepth++;
        return new BatchScope();
    }

    /// <summary>Завершить батч и применить все накопленные изменения.</summary>
    public static void End()
    {
        if (_batchDepth <= 0)
            throw new InvalidOperationException("SignalBatch.End() called without Begin()");

        _batchDepth--;

        if (_batchDepth == 0 && _dirtySignals is not null)
        {
            var signals = _dirtySignals;
            _dirtySignals = null;
            foreach (var signal in signals)
                signal.FlushIfDirty();
        }
    }

    /// <summary>Зарегистрировать сигнал как «грязный» внутри батча.</summary>
    internal static void AddDirty(ISignalFlushable signal)
    {
        _dirtySignals ??= new HashSet<ISignalFlushable>();
        _dirtySignals.Add(signal);
    }

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

    public static void EnqueueComponent(SgComponentBase component)
    {
        if (component.IsDisposed) return;
        _queue ??= new();
        _queue.Enqueue(component.RequestRender);
        Schedule();
    }

    public static void EnqueueEffect(SgEffect effect)
    {
        if (effect.IsDisposed) return;
        _queue ??= new();
        _queue.Enqueue(effect.Run);
        Schedule();
    }

    /// <summary>Сообщить о необходимости перерисовать компонент (через batch).</summary>
    public static void NotifyComponent(SgComponentBase component)
        => EnqueueComponent(component);

    /// <summary>Сообщить о необходимости перерисовать всех подписчиков computed.</summary>
    public static void NotifyComponent<T>(SgComputed<T> _)
    {
        // Computed внутри уже уведомил своих подписчиков (компоненты)
        // через SubscribeToTracked. Здесь — точка расширения.
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Schedule()
    {
        if (_scheduled) return;
        _scheduled = true;
        _ = Task.Run(Flush);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Flush()
    {
        if (_queue is null) { _scheduled = false; return; }

        while (_queue.TryDequeue(out var work))
        {
            try { work(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalBatch] Work item error: {ex}");
            }
        }

        _scheduled = false;
    }

    // ── Scope management ──────────────────────────────────────────────────────

    public static void EnterScope()
    {
        if (_nestCount == 0) _trackedItems = new();
        _nestCount++;
    }

    public static void ExitScope()
    {
        if (_nestCount == 0) return;
        _nestCount--;
        if (_nestCount == 0)
        {
            _trackedItems?.Clear();
            _trackedItems = null;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        _queue?.Clear();
        _trackedItems?.Clear();
        _trackedItems = null;
        _scheduled = false;
        _nestCount = 0;
    }
}
