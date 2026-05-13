// SuperUI/Base/Reactive/ComponentSignalTracker.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS1061 FIX: InvokeStateHasChangedAsync() → component.RefreshAsync()
// ✅ NET8 COMPAT: нет зависимости от .NET 9+ API
// ✅ RACE: защита race-window в finally блоке
// ✅ DISPOSE: идемпотентный через Interlocked
// ✅ DIAGNOSTICS: счётчики для DEBUG builds

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Render batching на уровне компонента.
/// Несколько RequestRender()/StateHasChanged за один async тик → один рендер.
///
/// ИСПРАВЛЕНИЕ CS1061:
/// Вместо несуществующего _component.InvokeStateHasChangedAsync() используется
/// _component.RefreshAsync(), который:
/// - Проверяет IsDisposed внутри
/// - Проверяет IsStaticSSR (не рендерить вне интерактивного режима)
/// - Корректно работает и в Server (через circuit dispatcher) и в WASM
///
/// Алгоритм (drain loop):
/// 1. ScheduleRender() → _scheduled=1
/// 2. Если _isFlushing=0 → запускаем FlushAsync
/// 3. FlushAsync: Task.Yield() → сбросить _scheduled=0 → RefreshAsync()
/// 4. После рендера: если _scheduled=1 снова → следующая итерация
/// 5. Выход из цикла → сброс _isFlushing в finally
/// 6. Race-window: если _scheduled=1 между выходом и сбросом → новый FlushAsync
///
/// WASM: Task.Yield() = браузерный microtask — один кадр UI = один рендер. ✅
/// Server: Task.Yield() = следующий await point в circuit thread. ✅
/// SSR: ScheduleRender() не запускает FlushAsync (компонент IsStaticSSR). ✅
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    private int _scheduled;    // 0 = нет, 1 = запланировано
    private int _isFlushing;   // 0 = нет, 1 = FlushAsync выполняется
    private int _disposedInt;

#if DEBUG
    private int _totalRenders;
    private int _totalScheduled;

    /// <summary>Всего запланированных рендеров (до дедупликации).</summary>
    public int TotalScheduled => Volatile.Read(ref _totalScheduled);

    /// <summary>Всего выполненных рендеров (после дедупликации).</summary>
    public int TotalRendered => Volatile.Read(ref _totalRenders);

    /// <summary>Сколько рендеров сохранено благодаря батчингу.</summary>
    public int SavedRenders => TotalScheduled - TotalRendered;

    /// <summary>Последнее время рендера (мс).</summary>
    public double LastRenderMs { get; private set; }
#endif

    public ComponentSignalTracker(SgComponentBase component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

    /// <summary>
    /// Запланировать рендер в следующий microtask.
    /// Идемпотентен: несколько вызовов = один рендер.
    /// Thread-safe.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ScheduleRender()
    {
        if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed)
            return;

#if DEBUG
        Interlocked.Increment(ref _totalScheduled);
#endif

        // CAS: если был 0 → стал 1 → запускаем FlushAsync
        // Если уже 1 — FlushAsync увидит флаг в drain loop
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            // Запускаем FlushAsync только если не выполняется сейчас
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
                // Уступаем браузеру/circuit — батчим все синхронные Set() в один рендер
                await Task.Yield();

                if (Volatile.Read(ref _disposedInt) == 1 || _component.IsDisposed)
                {
                    Interlocked.Exchange(ref _scheduled, 0);
                    return;
                }

                // Сбрасываем флаг ДО рендера — если во время рендера придёт новый сигнал,
                // он установит _scheduled=1 и следующая итерация drain loop поймет его
                Interlocked.Exchange(ref _scheduled, 0);

#if DEBUG
                var sw = System.Diagnostics.Stopwatch.GetTimestamp();
#endif

                try
                {
                    // ✅ CS1061 FIX: было InvokeStateHasChangedAsync()
                    // RefreshAsync() — публичный метод SgComponentBase:
                    // - Проверяет IsDisposed (не бросает исключение)
                    // - Проверяет IsStaticSSR (возвращает Task.CompletedTask)
                    // - Вызывает InvokeAsync(StateHasChanged) в правильном SynchronizationContext
                    await _component.RefreshAsync();

#if DEBUG
                    LastRenderMs = System.Diagnostics.Stopwatch.GetElapsedTime(sw).TotalMilliseconds;
                    Interlocked.Increment(ref _totalRenders);
#endif
                }
                catch (ObjectDisposedException)
                {
                    // Компонент удалён во время рендера — нормальная ситуация
                    return;
                }
                catch (OperationCanceledException)
                {
                    // Circuit закрыт / CancellationToken сработал
                    return;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[ComponentSignalTracker] StateHasChanged error: {ex.Message}");
                    // Продолжаем drain loop — не прерываем из-за одной ошибки
                }

                // Drain loop: если пришёл новый сигнал пока рендерили → ещё итерация
                if (Volatile.Read(ref _scheduled) == 0)
                    break;
            }
        }
        finally
        {
            // ВАЖНО: сброс в finally — гарантирован при любом исходе (даже исключении)
            Interlocked.Exchange(ref _isFlushing, 0);

            // Race-window protection:
            // Сигнал мог прийти между выходом из while и сбросом _isFlushing
            // В этом случае: _scheduled=1, _isFlushing=0 → запускаем новый FlushAsync
            if (Volatile.Read(ref _scheduled) == 1 &&
                Volatile.Read(ref _disposedInt) == 0 &&
                !_component.IsDisposed)
            {
                if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
                {
                    _ = FlushAsync().ContinueWith(
                        static t => System.Diagnostics.Debug.WriteLine(
                            $"[ComponentSignalTracker] FlushAsync faulted (race): " +
                            $"{t.Exception?.InnerException?.Message}"),
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
        // Принудительно не прерываем — RefreshAsync() сам проверит IsDisposed компонента
    }
}
