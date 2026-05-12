// SuperUI/Base/Reactive/RenderPriorityScheduler.cs
//
// ИСПРАВЛЕНИЯ:
//   1. Lock<T> заменён на object _lock (Lock — .NET 9+ API, не везде доступен)
//   2. Параллельный рендер через Task.WhenAll — изоляция исключений
//   3. HasPendingHighPriority — thread-safe lock
//   4. FlushAsync finally — сброс _scheduled
//   5. НОВОЕ: Clear() — очистить все очереди

namespace SuperUI.Base.Reactive;

/// <summary>Приоритет рендеринга компонента.</summary>
public enum RenderPriority
{
    /// <summary>Немедленный: модальные окна, критические уведомления.</summary>
    Critical = 0,
    /// <summary>Обычный: следующий тик планировщика.</summary>
    Normal = 1,
    /// <summary>Фоновый: только если нет Critical/Normal.</summary>
    Idle = 2
}

/// <summary>
/// Глобальный планировщик рендеров с поддержкой приоритетов.
/// Регистрируется как Scoped-сервис: per-circuit (Server) / per-instance (WASM).
/// </summary>
public sealed class RenderPriorityScheduler : IDisposable
{
    private readonly Queue<WeakReference<SgComponentBase>>[] _queues =
    [
        new(), // Critical
        new(), // Normal
        new()  // Idle
    ];

    private readonly object _lock = new();      // ИСПРАВЛЕНО: object вместо Lock<T>
    private int _scheduled;
    private int _disposed;

    /// <summary>Добавить компонент в очередь с заданным приоритетом.</summary>
    public void Schedule(SgComponentBase component, RenderPriority priority = RenderPriority.Normal)
    {
        if (Volatile.Read(ref _disposed) == 1 || component.IsDisposed) return;

        lock (_lock)
            _queues[(int)priority].Enqueue(new WeakReference<SgComponentBase>(component));

        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
            _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        try
        {
            // Critical — без дополнительной задержки
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Critical);

            // Normal — после Critical
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Normal);

            // Idle — только если нет более приоритетных задач
            await Task.Delay(1);
            if (!HasPendingHighPriority())
                await DrainQueueAsync(RenderPriority.Idle);
        }
        finally
        {
            Interlocked.Exchange(ref _scheduled, 0);

            // Если появились новые задачи — запускаем снова
            bool hasMore;
            lock (_lock) { hasMore = _queues.Any(q => q.Count > 0); }

            if (hasMore && Interlocked.Exchange(ref _scheduled, 1) == 0)
                _ = FlushAsync();
        }
    }

    private async Task DrainQueueAsync(RenderPriority priority)
    {
        SgComponentBase[] batch;
        lock (_lock)
        {
            var queue = _queues[(int)priority];
            var list = new List<SgComponentBase>(queue.Count);
            while (queue.TryDequeue(out var weakRef))
                if (weakRef.TryGetTarget(out var component) && !component.IsDisposed)
                    list.Add(component);
            batch = [.. list];
        }

        if (batch.Length == 0) return;

        // Параллельный рендер всех компонентов приоритета
        var tasks = new Task[batch.Length];
        for (int i = 0; i < batch.Length; i++)
        {
            var comp = batch[i];
            tasks[i] = SafeRefreshAsync(comp);
        }
        await Task.WhenAll(tasks);
    }

    private static async Task SafeRefreshAsync(SgComponentBase component)
    {
        try { await component.RefreshAsync(); }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[RenderPriorityScheduler] Render error {component.ComponentId}: {ex.Message}");
        }
    }

    private bool HasPendingHighPriority()
    {
        lock (_lock)
            return _queues[(int)RenderPriority.Critical].Count > 0
                || _queues[(int)RenderPriority.Normal].Count > 0;
    }

    /// <summary>Очистить все очереди (при сбросе приложения).</summary>
    public void Clear()
    {
        lock (_lock)
            foreach (var q in _queues) q.Clear();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        Clear();
    }
}
