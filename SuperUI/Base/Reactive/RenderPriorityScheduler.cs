// SuperUI/Base/Reactive/RenderPriorityScheduler.cs
//
// ИСПРАВЛЕНИЯ v3:
// ✅ CS0019 FIX: RenderPriority — ЕДИНСТВЕННОЕ определение здесь.
//    SgRenderPriority удалён из SgEnums.cs. Все файлы используют этот enum.
// ✅ Lock заменён на object _lock (Lock — .NET 9+ API, не везде доступен)
// ✅ Параллельный рендер через Task.WhenAll — изоляция исключений
// ✅ HasPendingHighPriority — thread-safe lock
// ✅ FlushAsync finally — сброс _scheduled
// ✅ НОВОЕ: Clear() — очистить все очереди
// ✅ НОВОЕ: PendingCount/PendingCriticalCount/PendingNormalCount/PendingIdleCount
//           — количество ожидающих рендеров (для диагностики)

namespace SuperUI.Base.Reactive;

/// <summary>
/// Приоритет рендеринга компонента.
/// ЕДИНСТВЕННОЕ определение в проекте (удалён SgRenderPriority из SgEnums.cs).
/// </summary>
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
    private readonly Queue<WeakReference<SgComponentBase>>[] _queues = [
        new(), // Critical
        new(), // Normal
        new()  // Idle
    ];

    private readonly object _lock = new();
    private int _scheduled;
    private int _disposed;

    /// <summary>
    /// Количество компонентов, ожидающих рендера (все приоритеты).
    /// Для UI диагностики.
    /// </summary>
    public int PendingCount
    {
        get
        {
            lock (_lock)
                return _queues.Sum(q => q.Count);
        }
    }

    /// <summary>Количество Critical-компонентов в очереди.</summary>
    public int PendingCriticalCount
    {
        get
        {
            lock (_lock)
                return _queues[(int)RenderPriority.Critical].Count;
        }
    }

    /// <summary>Количество Normal-компонентов в очереди.</summary>
    public int PendingNormalCount
    {
        get
        {
            lock (_lock)
                return _queues[(int)RenderPriority.Normal].Count;
        }
    }

    /// <summary>Количество Idle-компонентов в очереди.</summary>
    public int PendingIdleCount
    {
        get
        {
            lock (_lock)
                return _queues[(int)RenderPriority.Idle].Count;
        }
    }

    /// <summary>
    /// Добавить компонент в очередь с заданным приоритетом.
    /// </summary>
    public void Schedule(SgComponentBase component, RenderPriority priority = RenderPriority.Normal)
    {
        if (Volatile.Read(ref _disposed) == 1 || component.IsDisposed)
            return;

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
            lock (_lock)
            {
                hasMore = _queues.Any(q => q.Count > 0);
            }

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

        if (batch.Length == 0)
            return;

        // Параллельный рендер всех компонентов приоритета
        var tasks = new Task[batch.Length];
        for (int i = 0; i < batch.Length; i++)
        {
            var component = batch[i];
            tasks[i] = Task.Run(async () =>
            {
                try
                {
                    await component.InvokeStateHasChangedAsync();
                }
                catch (ObjectDisposedException) { }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    // Логируем, но не прерываем остальные рендеры
                    System.Diagnostics.Debug.WriteLine(
                        $"[RenderPriorityScheduler] Render error: {ex.Message}");
                }
            });
        }

        await Task.WhenAll(tasks);
    }

    private bool HasPendingHighPriority()
    {
        lock (_lock)
            return _queues[(int)RenderPriority.Critical].Count > 0 ||
                   _queues[(int)RenderPriority.Normal].Count > 0;
    }

    /// <summary>Очистить все очереди (при сбросе приложения).</summary>
    public void Clear()
    {
        lock (_lock)
            foreach (var q in _queues)
                q.Clear();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;
        Clear();
    }
}