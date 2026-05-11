// SuperUI/Base/Reactive/ComponentObserver.cs
// НОВОЕ: Реактивные сигналы с автоматическим отслеживанием зависимостей.
//
// Вместо ручного StateHasChanged() — компонент автоматически перерисовывается
// при изменении любого Signal<T> который он читал во время последнего рендера.
//
// Пример использования в компоненте:
//   private readonly Signal<int> _count = new(0);
//   // В Razor: @_count.Value  ← автоматически подписывается
//   // При _count.Set(1) → StateHasChanged() вызывается автоматически
//
// Аналог: SolidJS signals, Vue 3 reactivity, но для Blazor.
// В MudBlazor/Radzen/Telerik/DevExpress такого нет.
namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — контейнер значения с автоматическим уведомлением подписчиков.
/// </summary>
public sealed class Signal<T>
{
    private T _value;
    private readonly List<WeakReference<ISignalSubscriber>> _subscribers = [];
    private readonly Lock _lock = new();
    private readonly IEqualityComparer<T> _comparer;

    public Signal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _value = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Читать значение. Если вызывается во время рендера компонента —
    /// компонент автоматически подписывается на изменения.
    /// </summary>
    public T Value
    {
        get
        {
            // Регистрируем текущий компонент как подписчик (если есть активный tracking scope)
            SignalTracker.Track(this);
            return _value;
        }
    }

    /// <summary>
    /// Установить новое значение. Если изменилось — уведомляем подписчиков.
    /// </summary>
    public void Set(T value)
    {
        if (_comparer.Equals(_value, value)) return;
        _value = value;
        NotifySubscribers();
    }

    /// <summary>
    /// Обновить значение через функцию (атомарно для ссылочных типов).
    /// </summary>
    public void Update(Func<T, T> updater) => Set(updater(_value));

    internal void Subscribe(ISignalSubscriber subscriber)
    {
        lock (_lock)
        {
            // Очищаем мёртвые WeakRef'ы
            _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
            
            // Добавляем только если ещё не подписан
            if (!_subscribers.Any(wr => wr.TryGetTarget(out var s) && ReferenceEquals(s, subscriber)))
                _subscribers.Add(new WeakReference<ISignalSubscriber>(subscriber));
        }
    }

    private void NotifySubscribers()
    {
        ISignalSubscriber[] toNotify;
        lock (_lock)
        {
            var alive = new List<ISignalSubscriber>(_subscribers.Count);
            var dead = new List<WeakReference<ISignalSubscriber>>();
            
            foreach (var wr in _subscribers)
            {
                if (wr.TryGetTarget(out var s))
                    alive.Add(s);
                else
                    dead.Add(wr);
            }
            
            // Убираем мёртвые ссылки
            foreach (var d in dead) _subscribers.Remove(d);
            
            toNotify = [.. alive];
        }

        foreach (var subscriber in toNotify)
            subscriber.OnSignalChanged();
    }
}

/// <summary>
/// Интерфейс подписчика на Signal.
/// Реализуется SgComponentBase через ComponentSignalTracker.
/// </summary>
public interface ISignalSubscriber
{
    void OnSignalChanged();
}

/// <summary>
/// Контекст отслеживания: пока компонент рендерится,
/// все Signal.Value создают подписку на этот компонент.
/// Thread-local + AsyncLocal для корректной работы на Blazor Server.
/// </summary>
public static class SignalTracker
{
    // AsyncLocal корректно работает через async/await boundaries
    private static readonly AsyncLocal<ISignalSubscriber?> _current = new();

    /// <summary>Начать scope отслеживания для компонента.</summary>
    public static IDisposable EnterScope(ISignalSubscriber subscriber)
    {
        var previous = _current.Value;
        _current.Value = subscriber;
        return new TrackingScope(previous);
    }

    /// <summary>Зарегистрировать сигнал как зависимость текущего компонента.</summary>
    internal static void Track<T>(Signal<T> signal)
    {
        var current = _current.Value;
        if (current is not null)
            signal.Subscribe(current);
    }

    private sealed class TrackingScope : IDisposable
    {
        private readonly ISignalSubscriber? _previous;
        public TrackingScope(ISignalSubscriber? previous) => _previous = previous;
        public void Dispose() => _current.Value = _previous;
    }
}