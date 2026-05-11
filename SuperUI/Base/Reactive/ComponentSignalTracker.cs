using System.Threading.Tasks;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Render batching: несколько StateHasChanged за один тик = один рендер.
/// Использует lock-free Interlocked для планирования.
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    private volatile int _scheduled; // 0 = нет задачи, 1 = задача запланирована
    private volatile bool _disposed;

    public ComponentSignalTracker(SgComponentBase component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

    /// <summary>
    /// Запланировать рендер в следующий микротаск.
    /// Несколько вызовов за один тик = один рендер.
    /// </summary>
    public void ScheduleRender()
    {
        if (_disposed) return;

        // Если уже запланировано (1) — ничего не делаем
        // Если не запланировано (0) — ставим 1 и запускаем задачу
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            _ = FlushAsync();
        }
    }

    private async Task FlushAsync()
    {
        await Task.Yield();

        // Сбрасываем флаг ПЕРЕД вызовом рендера
        Interlocked.Exchange(ref _scheduled, 0);

        if (_disposed || _component.IsDisposed)
            return;

        await _component.InvokeStateHasChangedAsync();
    }

    public void Dispose()
    {
        _disposed = true;
    }
}
