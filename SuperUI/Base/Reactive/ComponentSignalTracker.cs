// SuperUI/Base/Reactive/ComponentSignalTracker.cs
//
// УЛУЧШЕНИЯ:
//   1. Drain loop с защитой от ObjectDisposedException и OperationCanceledException
//   2. _isFlushing сбрасывается в finally (гарантировано)
//   3. Race-window защита после выхода из while
//   4. Dispose идемпотентен (Interlocked.Exchange)
//   5. НОВОЕ: MaxPendingRenders счётчик для диагностики

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Render batching на уровне компонента.
/// Несколько StateHasChanged за один async тик → один рендер.
/// </summary>
/// <remarks>
/// Алгоритм (drain loop):
///   1. ScheduleRender() → _scheduled=1
///   2. Если _isFlushing=0 → запускаем FlushAsync
///   3. FlushAsync: Task.Yield() → сбросить _scheduled=0 → рендер
///   4. После рендера: если _scheduled=1 снова → следующая итерация
///   5. Выход из цикла → сброс _isFlushing в finally
///   6. Race-window: если _scheduled=1 между выходом и сбросом → новый FlushAsync
///
/// WASM: Task.Yield() = браузерный microtask — один кадр UI = один рендер. ✅
/// Server: Task.Yield() = следующий await point в circuit thread. ✅
/// </remarks>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    private int _scheduled;     // 0 = нет, 1 = запланирован
    private int _isFlushing;    // 0 = нет, 1 = FlushAsync выполняется
    private int _disposedInt;

#if DEBUG
    private int _totalRenders;
    private int _totalScheduled;
    /// <summary>Всего запланированных рендеров (до дедупликации).</summary>
    public int TotalScheduled => Volatile.Read(ref _totalScheduled);
    /// <summary>Всего выполненных рендеров (после дедупликации).</summary>
    public int TotalRendered => Volatile.Read(ref _totalRenders);
    /// <summary>Эффективность batch: сколько рендеров сохранено.</summary>
    public int SavedRenders => TotalScheduled - TotalRendered;
#endif

    public ComponentSignalTracker(SgComponentBase component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

    /// <summary>
    /// Запланировать рендер в следующий microtask.
    /// Идемпотентен: несколько вызовов = один рендер.
    /// </summary>
    public void ScheduleRender()
    {
        if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed) return;

#if DEBUG
        Interlocked.Increment(ref _totalScheduled);
#endif

        // CAS: если был 0 → стал 1 → запускаем FlushAsync
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            // Запускаем только если не выполняется сейчас
            // Если FlushAsync активен → он увидит _scheduled=1 в drain loop
            if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
            {
                _ = FlushAsync().ContinueWith(
                    static t => System.Diagnostics.Debug.WriteLine(
                        $"[ComponentSignalTracker] FlushAsync faulted: {t.Exception?.InnerException?.Message}"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
        }
    }

    private async Task FlushAsync()
    {
        try
        {
            while (true)
            {
                await Task.Yield();     // уступаем браузеру/circuit

                if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed)
                {
                    Interlocked.Exchange(ref _scheduled, 0);
                    return;
                }

                Interlocked.Exchange(ref _scheduled, 0);

                try
                {
                    await _component.InvokeStateHasChangedAsync();
#if DEBUG
                    Interlocked.Increment(ref _totalRenders);
#endif
                }
                catch (ObjectDisposedException) { return; }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ComponentSignalTracker] StateHasChanged error: {ex.Message}");
                }

                // Drain loop: если пришёл новый сигнал пока рендерили → ещё итерация
                if (Volatile.Read(ref _scheduled) == 0) break;
            }
        }
        finally
        {
            // ВАЖНО: сброс в finally — гарантирован при любом исходе
            Interlocked.Exchange(ref _isFlushing, 0);

            // Race-window: сигнал между выходом из while и сбросом _isFlushing
            if (Volatile.Read(ref _scheduled) == 1
                && Volatile.Read(ref _disposedInt) == 0
                && !_component.IsDisposed)
            {
                if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
                {
                    _ = FlushAsync().ContinueWith(
                        static t => System.Diagnostics.Debug.WriteLine(
                            $"[ComponentSignalTracker] FlushAsync faulted (race): {t.Exception?.InnerException?.Message}"),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                }
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        // _isFlushing FlushAsync завершится сам при проверке _disposedInt
    }
}
