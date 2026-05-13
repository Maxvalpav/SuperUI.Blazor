// SuperUI/Base/Reactive/SgSubscription.cs
// Единое место для Subscription и CompositeSubscription.
// ИСПРАВЛЯЕТ: CS0101 + CS0111 в SgSignalPersistence.cs и SgEffect.cs

namespace SuperUI.Base.Reactive;

/// <summary>
/// Подписка с действием на Dispose.
/// Потокобезопасна: Dispose идемпотентен.
/// </summary>
internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose)
        => _onDispose = onDispose ?? throw new ArgumentNullException(nameof(onDispose));

    public void Dispose()
    {
        // Атомарно забираем действие → идемпотентный dispose
        var action = Interlocked.Exchange(ref _onDispose, null);
        action?.Invoke();
    }
}

/// <summary>
/// Композитная подписка — dispose всех при dispose.
/// Потокобезопасна.
/// </summary>
internal sealed class CompositeSubscription : IDisposable
{
    private List<IDisposable>? _subscriptions;

    public CompositeSubscription(List<IDisposable> subscriptions)
        => _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));

    public void Dispose()
    {
        var list = Interlocked.Exchange(ref _subscriptions, null);
        if (list is null) return;

        foreach (var sub in list)
        {
            try { sub.Dispose(); }
            catch { /* игнорируем — dispose не должен бросать */ }
        }

        list.Clear();
    }
}
