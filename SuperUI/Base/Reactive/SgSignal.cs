// SuperUI/Base/Reactive/SgSignal.cs
using System.Collections.Concurrent;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Signal-based state management для Blazor.
/// </summary>
public sealed class SgSignal<T> : IDisposable
{
    private T _value;
    private readonly List<WeakReference<Action>> _subscribers = [];
    private readonly Lock _lock = new();
    private readonly IEqualityComparer<T> _comparer;

    public SgSignal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _value    = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        get
        {
            // Трекинг текущего computation context
            SignalTracker.Track(this);
            return _value;
        }
        set
        {
            if (_comparer.Equals(_value, value)) return;
            _value = value;
            NotifySubscribers();
        }
    }

    internal void Subscribe(Action callback)
    {
        lock (_lock)
        {
            // Очищаем мёртвые WeakReference
            _subscribers.RemoveAll(r => !r.TryGetTarget(out _));
            _subscribers.Add(new WeakReference<Action>(callback));
        }
    }

    private void NotifySubscribers()
    {
        List<Action> toNotify;
        lock (_lock)
        {
            toNotify = _subscribers
                .Select(r => { r.TryGetTarget(out var t); return t; })
                .Where(t => t != null)
                .ToList()!;
        }
        foreach (var sub in toNotify) sub();
    }

    public void Dispose()
    {
        lock (_lock) _subscribers.Clear();
    }

    public static implicit operator T(SgSignal<T> signal) => signal.Value;
}

/// <summary>Computed Signal — вычисляемое значение с мемоизацией.</summary>
public sealed class SgComputed<T> : IDisposable
{
    private readonly Func<T> _compute;
    private T _cached;
    private bool _isDirty = true;
    private readonly IEqualityComparer<T> _comparer;
    private readonly List<Action> _notifiers = [];

    public SgComputed(Func<T> compute, IEqualityComparer<T>? comparer = null)
    {
        _compute  = compute;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _cached   = default!;
    }

    public T Value
    {
        get
        {
            if (_isDirty)
            {
                var newVal = _compute();
                if (!_comparer.Equals(_cached, newVal))
                {
                    _cached = newVal;
                    NotifySubscribers();
                }
                _isDirty = false;
            }
            return _cached;
        }
    }

    internal void Invalidate()
    {
        _isDirty = true;
        NotifySubscribers();
    }

    private void NotifySubscribers() => _notifiers.ForEach(n => n());

    public void Dispose() => _notifiers.Clear();
}

/// <summary>Трекер активного контекста для Signal подписок.</summary>
internal static class SignalTracker
{
    private static readonly System.Threading.AsyncLocal<SgComponentBase?> _currentComponent = new();

    internal static IDisposable EnterScope(SgComponentBase component)
    {
        var prev = _currentComponent.Value;
        _currentComponent.Value = component;
        return new ScopeHandle(prev);
    }

    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent.Value is not null)
            signal.Subscribe(() => _currentComponent.Value!.RefreshAsync());
    }

    private sealed class ScopeHandle : IDisposable
    {
        private readonly SgComponentBase? _prev;
        public ScopeHandle(SgComponentBase? prev) => _prev = prev;
        public void Dispose() => _currentComponent.Value = _prev;
    }
}
