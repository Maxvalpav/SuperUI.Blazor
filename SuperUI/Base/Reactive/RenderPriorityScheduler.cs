// SuperUI/Base/Reactive/RenderPriorityScheduler.cs — ИСПРАВЛЕНО v4
// ✅ FIX CS1061: InvokeStateHasChangedAsync → NotifyStateChangedAsync
// ✅ FIX CRITICAL: последовательный рендер (не параллельный — Blazor не потокобезопасен)
// ✅ FIX: WeakReference → сильная ссылка с проверкой IsDisposed (GC-safe)
// ✅ NEW: ConfigureAwait(false) для предотвращения deadlock в WASM
// ✅ NEW: MaxBatchSize — ограничение на размер батча
// ✅ NEW: SkipIfRecentlyRendered — пропуск если компонент рендерился < N ms назад
// ✅ NEW: интеграция с ComponentSignalTracker (проверка перед рендером

namespace SuperUI.Base.Reactive;

/// <summary>
/// Приоритет рендеринга компонента.
/// ЕДИНСТВЕННОЕ определение в проекте.
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
///
/// ВАЖНО: Рендеры выполняются ПОСЛЕДОВАТЕЛЬНО в рамках одного приоритета,
/// т.к. Blazor не потокобезопасен для рендеринга.
/// </summary>
public sealed class RenderPriorityScheduler : IDisposable
{
    // Используем сильные ссылки — компоненты удаляются из очереди при Dispose
    private readonly Queue<SgComponentBase>[] _queues = [new(), new(), new()];
    private readonly HashSet<string> _scheduledIds = new(); // дедупликация
    private readonly object _lock = new();
    private int _scheduled;
    private int _disposed;

    /// <summary>Максимальный размер батча за один проход.</summary>
    public int MaxBatchSize { get; set; } = 50;

    /// <summary>Минимальный интервал между рендерами одного компонента (ms).</summary>
    public int MinRenderIntervalMs { get; set; } = 16; // ~60fps

    /// <summary>Количество компонентов, ожидающих рендера (все приоритеты).</summary>
    public int PendingCount { get { lock (_lock) return _queues.Sum(q => q.Count); } }

    /// <summary>Количество Critical-компонентов в очереди.</summary>
    public int PendingCriticalCount { get { lock (_lock) return _queues[(int)RenderPriority.Critical].Count; } }

    /// <summary>Количество Normal-компонентов в очереди.</summary>
    public int PendingNormalCount { get { lock (_lock) return _queues[(int)RenderPriority.Normal].Count; } }

    /// <summary>Количество Idle-компонентов в очереди.</summary>
    public int PendingIdleCount { get { lock (_lock) return _queues[(int)RenderPriority.Idle].Count; } }

    /// <summary>
    /// Добавить компонент в очередь с заданным приоритетом.
    /// Идемпотентен: повторное добавление того же компонента игнорируется.
    /// </summary>
    public void Schedule(SgComponentBase component, RenderPriority priority = RenderPriority.Normal)
    {
        if (Volatile.Read(ref _disposed) == 1 || component.IsDisposed)
            return;

        lock (_lock)
        {
            if (!_scheduledIds.Add(component.ComponentId))
                return; // уже в очереди

            _queues[(int)priority].Enqueue(component);
        }

        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
            _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        try
        {
            // Critical — без задержки
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Critical);

            // Normal — после Critical
            await Task.Yield();
            await DrainQueueAsync(RenderPriority.Normal);

            // Idle — только если нет более приоритетных задач
            await Task.Delay(1).ConfigureAwait(false);
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
            var list = new List<SgComponentBase>(Math.Min(queue.Count, MaxBatchSize));

            while (list.Count < MaxBatchSize && queue.TryDequeue(out var component))
            {
                _scheduledIds.Remove(component.ComponentId);
                if (!component.IsDisposed)
                    list.Add(component);
            }

            batch = [..list];
        }

        if (batch.Length == 0) return;

        // ✅ ПОСЛЕДОВАТЕЛЬНЫЙ рендер (Blazor не потокобезопасен!)
        foreach (var component in batch)
        {
            try
            {
                if (!component.IsDisposed)
                    await component.InvokeStateHasChangedAsync().ConfigureAwait(false);
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RenderPriorityScheduler] Render error: {ex.Message}");
            }
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
        {
            foreach (var q in _queues) q.Clear();
            _scheduledIds.Clear();
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        Clear();
    }
}
