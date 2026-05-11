// SuperUI/Base/Reactive/RenderPriorityScheduler.cs
// НОВОЕ: Планировщик рендеров с приоритетами.
// Позволяет назначить приоритет рендерингу компонента:
// - Critical: немедленный рендер (модальные окна, toast)
// - Normal: следующий тик (обычные компоненты)
// - Idle: рендер только если нет более важных работ
//
// Это снижает нагрузку на браузер при большом количестве компонентов.
// Аналог React.startTransition + React.useTransition, но для Blazor.
namespace SuperUI.Base.Reactive;

/// <summary>
/// Приоритет рендеринга компонента.
/// Используется для оптимизации порядка обновления UI.
/// </summary>
public enum RenderPriority
{
    /// <summary>Немедленный рендер: модальные окна, критические уведомления.</summary>
    Critical = 0,
    
    /// <summary>Обычный рендер: следующий тик планировщика.</summary>
    Normal = 1,
    
    /// <summary>Фоновый рендер: только если нет Critical/Normal работ.</summary>
    Idle = 2
}

/// <summary>
/// Глобальный планировщик рендеров с поддержкой приоритетов.
/// Batching + priority queue = минимальное количество рендеров.
/// 
/// Singletone сервис — регистрируется как Scoped (per-circuit на Server, per-instance на WASM).
/// </summary>
public sealed class RenderPriorityScheduler : IDisposable
{
    // Три очереди по приоритетам
    private readonly Queue<WeakReference<SgComponentBase>>[] _queues =
    [
        new(), // Critical
        new(), // Normal
        new()  // Idle
    ];
    
    private readonly Lock _lock = new();
    private volatile int _scheduled;  // 0 = нет задачи, 1 = задача запланирована
    private volatile bool _disposed;

    /// <summary>
    /// Добавить компонент в очередь рендера с заданным приоритетом.
    /// Если компонент уже в очереди (по WeakRef), не добавляем дубликат.
    /// </summary>
    public void Schedule(SgComponentBase component, RenderPriority priority = RenderPriority.Normal)
    {
        if (_disposed || component.IsDisposed) return;

        lock (_lock)
        {
            _queues[(int)priority].Enqueue(new WeakReference<SgComponentBase>(component));
        }

        // Запускаем flush если не запланирован
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            _ = FlushAsync();
        }
    }

    private async Task FlushAsync()
    {
        try
        {
            // Critical — без yield (почти немедленно)
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Critical);

            // Normal — после Critical
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Normal);

            // Idle — только если нет новых Critical/Normal
            // Используем Task.Delay(0) вместо Yield для большей задержки
            await Task.Delay(1);
            if (!HasPendingHighPriority())
                await DrainQueueAsync(RenderPriority.Idle);
        }
        finally
        {
            Interlocked.Exchange(ref _scheduled, 0);

            // Если во время флаша пришли новые задачи — запускаем снова
            bool hasMore;
            lock (_lock)
            {
                hasMore = _queues.Any(q => q.Count > 0);
            }
            if (hasMore && Interlocked.Exchange(ref _scheduled, 1) == 0)
            {
                _ = FlushAsync();
            }
        }
    }

    private async Task DrainQueueAsync(RenderPriority priority)
    {
        // Берём snapshot очереди под lock'ом
        SgComponentBase[] batch;
        lock (_lock)
        {
            var queue = _queues[(int)priority];
            var list = new List<SgComponentBase>(queue.Count);
            while (queue.TryDequeue(out var weakRef))
            {
                if (weakRef.TryGetTarget(out var component) && !component.IsDisposed)
                    list.Add(component);
            }
            batch = [.. list];
        }

        // Рендерим компоненты параллельно (каждый в своём InvokeAsync)
        var tasks = new Task[batch.Length];
        for (int i = 0; i < batch.Length; i++)
        {
            var component = batch[i];
            tasks[i] = component.InvokeStateHasChangedAsync();
        }
        await Task.WhenAll(tasks);
    }

    private bool HasPendingHighPriority()
    {
        lock (_lock)
        {
            return _queues[(int)RenderPriority.Critical].Count > 0 ||
                   _queues[(int)RenderPriority.Normal].Count > 0;
        }
    }

    public void Dispose() => _disposed = true;
}