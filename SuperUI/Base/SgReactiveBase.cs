// SuperUI/Base/SgReactiveBase.cs
// НОВЫЙ ФАЙЛ: Convenience base class для компонентов активно использующих реактивность.
// Предоставляет готовые фабричные методы прямо в шаблоне компонента.
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
}
