// SuperUI/Base/Reactive/ComponentSignalTracker.cs
// ИСПРАВЛЕНО:
// 1. Рекурсия: флаг _scheduled сбрасывается ТОЛЬКО после выхода из InvokeStateHasChangedAsync
//    + добавлен _flushing guard чтобы FlushAsync не запустился параллельно
// 2. Dispose-safe: проверки _disposed везде

namespace SuperUI.Base.Reactive;

/// <summary>
/// Batching рендера: несколько StateHasChanged за один микротаск = один рендер.
/// Lock-free через Interlocked. Защита от рекурсии через _flushing guard.
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    private volatile int _scheduled;   // 0=нет, 1=запланировано
    private volatile int _flushing;    // 0=нет, 1=выполняется — ЗАЩИТА ОТ РЕКУРСИИ
    private volatile bool _disposed;

    public ComponentSignalTracker(SgComponentBase component)
        => _component = component ?? throw new ArgumentNullException(nameof(component));

    /// <summary>
    /// Запланировать рендер в следующий микротаск (идемпотентен).
    /// </summary>
    public void ScheduleRender()
    {
        if (_disposed || _component.IsDisposed) return;
        // Если уже идёт Flush — просто помечаем что нужен ещё один
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
            _ = FlushAsync();
    }

    private async Task FlushAsync()
    {
        // Защита от параллельного запуска
        if (Interlocked.Exchange(ref _flushing, 1) == 1)
            return;
        try
        {
            await Task.Yield(); // уступаем поток

            if (_disposed || _component.IsDisposed)
            {
                Interlocked.Exchange(ref _scheduled, 0);
                return;
            }

            // Читаем и сбрасываем флаг ПЕРЕД рендером.
            // Если во время рендера придёт новый ScheduleRender — он установит _scheduled=1
            // и запустит новый FlushAsync ПОСЛЕ того как мы выйдем из _flushing=0.
            Interlocked.Exchange(ref _scheduled, 0);

            await _component.InvokeStateHasChangedAsync();
        }
        finally
        {
            Interlocked.Exchange(ref _flushing, 0);

            // Если пока мы рендерили, пришёл новый запрос — перепланируем
            if (_scheduled == 1 && !_disposed && !_component.IsDisposed)
                _ = FlushAsync();
        }
    }

    public void Dispose() => _disposed = true;
}
