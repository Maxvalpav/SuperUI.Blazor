// Файл: Utilities/RenderBatch.cs
// ИННОВАЦИЯ: Нет ни у одной библиотеки!
// Объединяет множество StateHasChanged в один через microtask queue

namespace SuperUI.Utilities;

/// <summary>
/// Батчер вызовов StateHasChanged.
/// 
/// ПРОБЛЕМА: в event handler вызывается 5 StateHasChanged подряд
/// = 5 синхронных render cycles = плохая производительность.
/// 
/// РЕШЕНИЕ: батчинг через Task.Yield() — все вызовы в одном task
/// объединяются в один рендер.
/// 
/// АНАЛОГИЯ: React batching в event handlers.
/// 
/// ИСПОЛЬЗОВАНИЕ (в SgComponentBase):
/// protected Task RequestStateUpdateAsync() => _renderBatch.RequestAsync();
/// </summary>
public sealed class RenderBatch
{
    private readonly Func<Task> _stateHasChanged;
    private Task? _pendingRender;
    private bool _disposed;

    public RenderBatch(Func<Task> stateHasChanged)
    {
        _stateHasChanged = stateHasChanged;
    }

    /// <summary>Запросить рендер. Если уже запрошен — не дублирует.</summary>
    public Task RequestAsync()
    {
        if (_disposed) return Task.CompletedTask;

        if (_pendingRender is { IsCompleted: false })
            return _pendingRender; // уже ожидает рендер

        _pendingRender = ExecuteBatchAsync();
        return _pendingRender;
    }

    private async Task ExecuteBatchAsync()
    {
        // Task.Yield() — уступаем поток, позволяем другим RequestAsync добавиться
        await Task.Yield();
        if (!_disposed)
            await _stateHasChanged();
        _pendingRender = null;
    }

    public void Dispose() => _disposed = true;
}
