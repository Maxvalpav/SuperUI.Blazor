// SuperUI/Base/SgComponentContext.cs
// НОВЫЙ КЛАСС
// Аналог: React Context API, Angular Injection Tokens
// Поддержка: .NET 8/9/10, SSR + Interactive
//
// Проблема: CascadingValue/CascadingParameter нетипобезопасны при рефакторинге
// Решение: строго типизированный wrapper с compile-time safety

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SuperUI.Base;

/// <summary>
/// Типобезопасный каскадный контекст.
/// Обёртка над CascadingValue с именованным параметром.
///
/// Вместо:
/// <code>
/// [CascadingParameter(Name = "MyContext")]
/// public MyContext? Ctx { get; set; }
/// </code>
///
/// Используйте:
/// <code>
/// [CascadingParameter]
/// public SgComponentContext&lt;MyContext&gt;? Ctx { get; set; }
///
/// var value = Ctx?.Value; // типобезопасно
/// </code>
///
/// Provider:
/// <code>
/// &lt;SgContextProvider TValue="MyContext" Value="@myContext"&gt;
///     &lt;ChildContent /&gt;
/// &lt;/SgContextProvider&gt;
/// </code>
/// </summary>
public sealed class SgComponentContext<T>
{
    private T _value;
    private readonly List<Action<T>> _listeners = [];
    private readonly object _lock = new();

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            NotifyListeners(value);
        }
    }

    public SgComponentContext(T initialValue)
    {
        _value = initialValue;
    }

    /// <summary>Подписаться на изменения контекста.</summary>
    public IDisposable Subscribe(Action<T> listener)
    {
        lock (_lock) _listeners.Add(listener);
        return new Subscription(() =>
        {
            lock (_lock) _listeners.Remove(listener);
        });
    }

    private void NotifyListeners(T value)
    {
        Action<T>[] snapshot;
        lock (_lock) snapshot = [.._listeners];

        foreach (var listener in snapshot)
            listener(value);
    }

    public static implicit operator T(SgComponentContext<T> ctx) => ctx.Value;
}

/// <summary>
/// Подписка с действием на Dispose.
/// </summary>
internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        var action = _onDispose;
        _onDispose = null;
        action?.Invoke();
    }
}

/// <summary>
/// Провайдер каскадного контекста.
/// Рендерит CascadingValue с вашим типом.
/// </summary>
public sealed class SgContextProvider<TValue> : ComponentBase
{
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool IsFixed { get; set; }

    private SgComponentContext<TValue>? _context;

    protected override void OnParametersSet()
    {
        if (_context is null)
            _context = new SgComponentContext<TValue>(Value);
        else
            _context.Value = Value;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<SgComponentContext<TValue>>>(0);
        builder.AddComponentParameter(1, nameof(CascadingValue<TValue>.Value), _context!);
        builder.AddComponentParameter(2, nameof(CascadingValue<TValue>.IsFixed), IsFixed);
        builder.AddComponentParameter(3, nameof(CascadingValue<TValue>.ChildContent), ChildContent);
        builder.CloseComponent();
    }
}
