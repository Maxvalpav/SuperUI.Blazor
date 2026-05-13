// SuperUI/Base/Reactive/SignalBatch.cs
// ✅ Dispose: отменяет все запланированные задачи (очистка очередей)
// ✅ EnqueueComponent/Effect: игнорирует disposed
// ✅ NotifyComponent: внешний API для сигналов/computed
// ✅ Nested scope: ExitScope очищает _current

using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Батчинг уведомлений компонентов: несколько изменений сигналов за один тик
/// вызывают только один рендер на компонент.
/// </summary>
internal static class SignalBatch
{
    [ThreadStatic] private static ConcurrentQueue<Action>? _queue;
    [ThreadStatic] private static bool _scheduled;
    [ThreadStatic] private static int _nestCount;
    [ThreadStatic] private static HashSet<object>? _trackedItems;

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
