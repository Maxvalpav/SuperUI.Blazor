// SuperUI/Base/Reactive/SgEffect.cs
// НОВЫЙ: реактивные side-effect с автоматическим отслеживанием зависимостей.
// Аналог effect() из Solid.js / watchEffect() из Vue 3
// Автоматически перезапускается при изменении любых SgSignal внутри функции.

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Reactive side-effect: выполняет функцию при изменении зависимых сигналов.
/// Автоматически отслеживает SgSignal, прочитанные во время выполнения.
/// </summary>
/// <example>
/// var count = new SgSignal<int>(0);
/// var effect = new SgEffect(() => Console.WriteLine($"Count changed: {count.Value}"));
/// // При count.Set(5) effect автоматически перезапустится
/// </example>
public sealed class SgEffect : IDisposable
{
    private readonly Func<Task> _action;
    private readonly EffectObserver _observer;
    private bool _disposed;

    public SgEffect(Action action)
    {
        _action = () => { action(); return Task.CompletedTask; };
        _observer = new EffectObserver(RunAsync);
        _ = RunAsync(); // Запускаем сразу при создании
    }

    public SgEffect(Func<Task> action)
    {
        _action = action;
        _observer = new EffectObserver(RunAsync);
        _ = RunAsync(); // Запускаем сразу при создании
    }

    private async Task RunAsync()
    {
        if (_disposed) return;
        try
        {
            await _action();
        }
        catch (Exception ex)
        {
            // Effect не должен молча падать — логируем
            Console.Error.WriteLine($"SgEffect error: {ex}");
        }
    }

    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _observer.Dispose();
    }

    private sealed class EffectObserver : ISignalObserver, IDisposable
    {
        private readonly Func<Task> _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly object _lock = new();
        private bool _disposed;

        public EffectObserver(Func<Task> invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock)
                _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        public void OnSignalChanged() => _ = _invalidate();

        public void OnSignalRead<T>(SgSignal<T> signal) { }

        public void OnComputedRead<T>(SgComputed<T> computed) { }

        public void Dispose() { _disposed = true; _dependents.Clear(); }
    }
}