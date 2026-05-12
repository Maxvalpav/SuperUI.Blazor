// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНО:
// 1. RunAsync — отслеживает зависимости через SignalTracker.EnterScopeForObserver
// 2. _disposed — Interlocked для Server thread-safety
// 3. Console.Error → ILogger (через callback)
// 4. EffectObserver._dependents — убрано (не используется)
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Reactive side-effect: выполняет функцию при изменении зависимых сигналов.
/// Автоматически отслеживает SgSignal, прочитанные во время выполнения.
/// </summary>
public sealed class SgEffect : IDisposable
{
    private readonly Func<Task> _action;
    private readonly Action<Exception>? _onError;
    private readonly EffectObserver _observer;
    private int _disposed; // ИСПРАВЛЕНО: Interlocked для Server

    public SgEffect(Action action, Action<Exception>? onError = null)
    {
        _action = () => { action(); return Task.CompletedTask; };
        _onError = onError;
        _observer = new EffectObserver(RunAsync);
        _ = RunAsync();
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null)
    {
        _action = action;
        _onError = onError;
        _observer = new EffectObserver(RunAsync);
        _ = RunAsync();
    }

    private async Task RunAsync()
    {
        // ИСПРАВЛЕНО: Interlocked.Read
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;

        try
        {
            // ИСПРАВЛЕНО: отслеживаем зависимости через scope
            using (SignalTracker.EnterScopeForObserver(_observer))
            {
                await _action();
            }
        }
        catch (Exception ex)
        {
            // ИСПРАВЛЕНО: callback вместо Console.Error
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"SgEffect error: {ex}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _observer.Dispose();
    }

    private sealed class EffectObserver : ISignalObserver, IDisposable
    {
        private readonly Func<Task> _invalidate;
        private int _disposed;

        public EffectObserver(Func<Task> invalidate) => _invalidate = invalidate;

        public void OnSignalChanged()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;
            _ = _invalidate();
        }

        public void OnSignalRead<T>(SgSignal<T> signal) { }
        public void OnComputedRead<T>(SgComputed<T> computed) { }

        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }
}