// ─────────────────────────────────────────────────────────────────
// FILE: Base/RenderBudget/RenderBudgetService.cs
// ИННОВАЦИЯ: Бюджет рендеров — приоритизация перерисовок.
// Предотвращает "рендер-шторм" при массовых обновлениях данных.
// ─────────────────────────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.RenderBudget;

/// <summary>
/// Уровень рендер-приоритета компонента.
/// </summary>
public enum RenderPriority { Critical = 0, High = 1, Normal = 2, Low = 3, Background = 4 }

/// <summary>
/// Интерфейс сервиса управления бюджетом рендеров.
/// </summary>
public interface IRenderBudgetService
{
    Task RequestRenderAsync(Func<Task> renderAction, RenderPriority priority = RenderPriority.Normal);
}

/// <summary>
/// ИННОВАЦИЯ: Сервис управления очередью рендеров.
/// При перегрузке рендеров — ставит низкоприоритетные в очередь.
/// Предотвращает jank и "render storm".
/// </summary>
public sealed class RenderBudgetService : IRenderBudgetService, IAsyncDisposable
{
    private const int MaxConcurrentRenders = 16;
    private const int FrameBudgetMs        = 16; // ~60fps

    private readonly SemaphoreSlim _semaphore = new(MaxConcurrentRenders);
    private readonly PriorityQueue<Func<Task>, int> _queue = new();
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cts = new();

    public RenderBudgetService()
    {
        // Фоновый процессор очереди
        _ = ProcessQueueAsync();
    }

    /// <summary>
    /// Запрашивает рендер с заданным приоритетом.
    /// Critical — немедленно; остальные — через очередь.
    /// </summary>
    public Task RequestRenderAsync(Func<Task> renderAction, RenderPriority priority = RenderPriority.Normal)
    {
        if (priority == RenderPriority.Critical)
            return renderAction();

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            _queue.Enqueue(async () =>
            {
                try
                {
                    await renderAction();
                    tcs.SetResult();
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }, (int)priority);
        }
        return tcs.Task; // caller может await реальный рендер
    }

    private async Task ProcessQueueAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            await Task.Delay(FrameBudgetMs, _cts.Token);

            int budget = MaxConcurrentRenders;
            while (budget-- > 0)
            {
                Func<Task>? action;
                lock (_lock)
                {
                    if (!_queue.TryDequeue(out action, out _)) break;
                }
                try { await action(); } catch { /* игнорируем */ }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _cts.CancelAsync();
        _cts.Dispose();
        _semaphore.Dispose();
    }
}
