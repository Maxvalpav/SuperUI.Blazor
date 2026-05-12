// SuperUI/Base/Reactive/ComponentSignalTracker.cs
// Ключевые исправления:
// 1. FlushAsync — try/catch для ObjectDisposedException / OperationCanceledException + общий catch
// 2. InvokeStateHasChangedAsync — изолирован от circuit disconnection exceptions
// 3. _isFlushing сбрасывается в finally (гарантировано при любом исходе)
// 4. Race-window check после сброса _isFlushing
// 5. Dispose — идемпотентен через Interlocked.Exchange

using System.Runtime.CompilerServices;
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Render batching: несколько StateHasChanged за один тик = один рендер.
///
/// Алгоритм (drain loop):
/// 1. ScheduleRender() — атомарно устанавливает _scheduled=1
/// 2. Если _isFlushing=0 → запускает FlushAsync
/// 3. FlushAsync: Task.Yield() → сбросить _scheduled=0 → рендер
/// 4. После рендера: если _scheduled снова 1 → ещё итерация (drain)
/// 5. После выхода из цикла: проверяем финальный сигнал (race window защита)
///
/// Blazor Server: StateHasChanged() может синхронно вызвать ScheduleRender()
/// изнутри рендера → drain loop обработает это как следующую итерацию. ✅
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;

    // 0 = нет запланированной задачи, 1 = задача запланирована
    private int _scheduled;

    // 0 = не в процессе флаша, 1 = FlushAsync выполняется
    private int _isFlushing;

    // ИСПРАВЛЕНО: int для Interlocked.Exchange (atomic compare-and-swap)
    private int _disposedInt;

    public ComponentSignalTracker(SgComponentBase component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

     /// <summary>
     /// Запланировать рендер в следующий микротаск.
     /// Несколько вызовов за один тик = один рендер (batching).
     /// </summary>
      public void ScheduleRender()
      {
          if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed) return;

          // Атомарно устанавливаем флаг: если уже 1 — задача уже запланирована, выходим
          if (Interlocked.Exchange(ref _scheduled, 1) == 0)
          {
              // Запускаем FlushAsync только если не выполняется сейчас
              // Если FlushAsync выполняется — он увидит _scheduled=1 в drain loop
              if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
              {
                  // ИСПРАВЛЕНО: ContinueWith для логирования необработанных исключений
                  _ = FlushAsync().ContinueWith(
                      static t => System.Diagnostics.Debug.WriteLine($"[ComponentSignalTracker] FlushAsync faulted: {t.Exception}"),
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
                await Task.Yield();

                if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed)
                {
                    Interlocked.Exchange(ref _scheduled, 0);
                    return;
                }

                Interlocked.Exchange(ref _scheduled, 0);

                try
                {
                    await _component.InvokeStateHasChangedAsync();
                }
                catch (ObjectDisposedException) { return; }
                catch (OperationCanceledException) { return; }
                catch (Exception ex)
                {
                    // Логируем, но не останавливаем drain loop
                    System.Diagnostics.Debug.WriteLine($"[ComponentSignalTracker] StateHasChanged error: {ex}");
                }

                if (Volatile.Read(ref _scheduled) == 0) break;
            }
        }
        finally
        {
            // ИСПРАВЛЕНО: _isFlushing сбрасывается в finally → гарантировано при любом исходе
            Interlocked.Exchange(ref _isFlushing, 0);

            // Race-window: сигнал пришёл между выходом из while и сбросом _isFlushing
            if (Volatile.Read(ref _scheduled) == 1
                && Volatile.Read(ref _disposedInt) == 0
                && !_component.IsDisposed)
            {
                if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
                {
                    _ = FlushAsync().ContinueWith(
                        static t => System.Diagnostics.Debug.WriteLine($"[ComponentSignalTracker] FlushAsync faulted (race): {t.Exception}"),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                }
            }
        }
    }

    /// <summary>
    /// ИСПРАВЛЕНО: Interlocked.Exchange — атомарный compare-and-swap.
    /// Гарантирует, что Dispose выполняется ровно один раз даже при race.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
    }
}
