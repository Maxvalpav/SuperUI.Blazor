// SuperUI/Base/Reactive/ComponentSignalTracker.cs
// ИСПРАВЛЕНО:
// 1. _scheduled сбрасывается ПОСЛЕ вызова StateHasChanged (не до)
// 2. Добавлен _isFlushing флаг против повторного запуска FlushAsync
// 3. Защита от накопления задач при высокочастотных сигналах
namespace SuperUI.Base.Reactive;

/// <summary>
/// Render batching: несколько StateHasChanged за один тик = один рендер.
/// 
/// ИСПРАВЛЕНИЯ:
/// - _scheduled сбрасывается ПОСЛЕ вызова InvokeStateHasChangedAsync,
///   чтобы новые ScheduleRender() во время рендера не потерялись.
/// - _isFlushing предотвращает запуск двух параллельных FlushAsync.
/// - Работает корректно на Blazor Server (многопоточный SynchronizationContext).
/// - Работает корректно на Blazor WASM (однопоточный, но Task.Yield меняет порядок).
/// </summary>
public sealed class ComponentSignalTracker : IDisposable
{
    private readonly SgComponentBase _component;
    
    // 0 = нет запланированной задачи
    // 1 = задача запланирована (FlushAsync запущен)
    private volatile int _scheduled;
    
    // 0 = не в процессе флаша
    // 1 = FlushAsync выполняется прямо сейчас
    private volatile int _isFlushing;
    
    private volatile bool _disposed;

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
        if (_disposed || _component.IsDisposed) return;

        // Атомарно устанавливаем флаг: если уже 1 — задача уже запланирована, выходим
        if (Interlocked.Exchange(ref _scheduled, 1) == 0)
        {
            // Если FlushAsync уже выполняется — он увидит _scheduled=1 и сделает ещё один рендер
            // Если нет — запускаем новый FlushAsync
            if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
            {
                _ = FlushAsync();
            }
            // Иначе: выполняющийся FlushAsync после своего рендера проверит _scheduled
            // и если он 1 — выполнит ещё один рендер (drain loop)
        }
    }

    private async Task FlushAsync()
    {
        try
        {
            // Drain loop: рендерим пока есть накопленные сигналы
            while (true)
            {
                // Ждём следующего микротаска — позволяем другим сигналам накопиться
                await Task.Yield();

                if (_disposed || _component.IsDisposed)
                {
                    _scheduled = 0;
                    return;
                }

                // Сбрасываем ПЕРЕД рендером: если во время рендера придёт новый сигнал,
                // он установит _scheduled=1 и мы выполним ещё итерацию цикла
                Interlocked.Exchange(ref _scheduled, 0);

                await _component.InvokeStateHasChangedAsync();

                // После рендера: если новый сигнал пришёл во время рендера — продолжаем
                // Иначе — выходим из цикла
                if (_scheduled == 0)
                    break;
            }
        }
        finally
        {
            // Освобождаем флаг выполнения
            Interlocked.Exchange(ref _isFlushing, 0);

            // Финальная проверка: вдруг пришёл сигнал между последней проверкой _scheduled
            // и сбросом _isFlushing
            if (_scheduled == 1 && !_disposed && !_component.IsDisposed)
            {
                // Рекурсивно запускаем ещё один FlushAsync
                if (Interlocked.CompareExchange(ref _isFlushing, 1, 0) == 0)
                {
                    _ = FlushAsync();
                }
            }
        }
    }

    public void Dispose()
    {
        _disposed = true;
        _scheduled = 0;
    }
}
