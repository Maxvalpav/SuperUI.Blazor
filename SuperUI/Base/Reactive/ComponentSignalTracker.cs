// SuperUI/Base/Reactive/ComponentSignalTracker.cs
namespace SuperUI.Base.Reactive;

/// <summary>
/// Автоматический render batching для Blazor.
///
/// Проблема: в Blazor каждый StateHasChanged() триггерит рендер немедленно.
/// При изменении 10 свойств = 10 рендеров.
///
/// Решение: CollectSignals → batch → один StateHasChanged() за тик.
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    private volatile int _pendingCount;
    private Task? _batchTask;
    private readonly Lock _lock = new();

    public ComponentSignalTracker(SgComponentBase component)
    {
        _component = component;
    }

    /// <summary>
    /// Запланировать рендер в следующий микротаск.
    /// Несколько вызовов в одном тике = один рендер.
    /// </summary>
    public void ScheduleRender()
    {
        var count = Interlocked.Increment(ref _pendingCount);
        if (count == 1)
        {
            lock (_lock)
            {
                _batchTask ??= FlushAsync();
            }
        }
    }

    private async Task FlushAsync()
    {
        await Task.Yield();
        Interlocked.Exchange(ref _pendingCount, 0);
        lock (_lock) { _batchTask = null; }
        await _component.RefreshAsync();
    }

    public void Dispose() { }
}
