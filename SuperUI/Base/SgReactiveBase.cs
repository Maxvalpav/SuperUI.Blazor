using System.Collections.Generic;
using SuperUI.Base.Reactive;
namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, активно использующих реактивные сигналы.
/// Расширяет SgComponentBase удобными фабриками и lifecycle-интеграцией.
/// </summary>
/// <remarks>
/// Пример использования:
/// <code>
/// public class MyCounter : SgReactiveBase
/// {
///     private SgSignal&lt;int&gt; _count = null!;
///
///     protected override void OnInitialized()
///     {
///         base.OnInitialized();
///         _count = Signal(0);
///         Effect(() => Console.WriteLine($"Count: {_count.Value}"));
///     }
/// }
/// </code>
/// </remarks>
public abstract class SgReactiveBase : SgComponentBase
{
    private readonly List<IParameterSyncEffect> _parameterSyncEffects = new();

    /// <summary>Создать и зарегистрировать реактивный сигнал.</summary>
    protected SgSignal<T> Signal<T>(T initial, IEqualityComparer<T>? comparer = null)
        => CreateSignal(initial, comparer);

    /// <summary>Создать и зарегистрировать computed-сигнал.</summary>
    protected SgComputed<T> Computed<T>(Func<T> compute)
        => RegisterComputed(compute);

    /// <summary>Создать и зарегистрировать side-effect.</summary>
    protected SgEffect Effect(Action action, Action<Exception>? onError = null)
    {
        var effect = new SgEffect(action, onError);
        effect.Subscribe(this);
        RegisterEffectInternal(effect);
        return effect;
    }

    /// <summary>Создать и зарегистрировать async side-effect.</summary>
    protected SgEffect Effect(Func<Task> action, Action<Exception>? onError = null)
    {
        var effect = new SgEffect(action, onError);
        effect.Subscribe(this);
        RegisterEffectInternal(effect);
        return effect;
    }

    /// <summary>
    /// Создать сигнал, привязанный к параметру компонента.
    /// Автоматически обновляется при изменении параметра в OnParametersChangedAsync.
    /// </summary>
    protected SgSignal<T> SignalFromParameter<T>(Func<T> getter, IEqualityComparer<T>? comparer = null)
    {
        var signal = CreateSignal(getter(), comparer);
        var effect = new ParameterSyncEffect<T>(signal, getter, this);
        RegisterEffectInternal(effect);
        _parameterSyncEffects.Add(effect);
        return signal;
    }

    /// <summary>
    /// Вызывается при каждом изменении параметров после базовых хуков.
    /// Переопределите для кастомной логики, но вызывайте base.
    /// </summary>
    protected override Task OnParametersChangedAsync()
    {
        foreach (var effect in _parameterSyncEffects)
            effect.Sync();
        return Task.CompletedTask;
    }

    /// <summary>
    /// ИСПРАВЛЕНИЕ: освобождаем список parameter sync effects.
    /// </summary>
    protected override async ValueTask DisposeComponentAsync()
    {
        _parameterSyncEffects.Clear();
        await base.DisposeComponentAsync();
    }

    // ── Helper types ─────────────────────────────────────────────────────────────

    private interface IParameterSyncEffect
    {
        void Sync();
    }

    private sealed class ParameterSyncEffect<T> : IDisposable, IParameterSyncEffect
    {
        private readonly SgSignal<T> _signal;
        private readonly Func<T> _getter;
        private readonly SgComponentBase _component;
        private bool _disposed;

        public ParameterSyncEffect(SgSignal<T> signal, Func<T> getter, SgComponentBase component)
        {
            _signal = signal;
            _getter = getter;
            _component = component;
        }

        // Вызывается из OnParametersChangedAsync
        public void Sync() => _signal.Set(_getter());

        public void Dispose() => _disposed = true;
    }
}
