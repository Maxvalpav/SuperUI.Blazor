using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер реактивных зависимостей (Legacy wrapper).
/// Рекомендуется использовать SgReactiveComponentBase для новых компонентов.
/// </summary>
public static class SignalTracker
{
    [ThreadStatic]
    private static ISignalObserver? _currentObserver;

    /// <summary>
    /// Begin tracking signal dependencies for an observer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BeginTracking(ISignalObserver observer)
    {
        _currentObserver = observer;
    }

    /// <summary>
    /// End tracking signal dependencies.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EndTracking()
    {
        _currentObserver = null;
    }

    /// <summary>
    /// Get the current observer being tracked.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ISignalObserver? GetCurrentObserver()
    {
        return _currentObserver;
    }

    /// <summary>
    /// Регистрация зависимости. 
    /// Теперь просто перенаправляет в SgReactiveComponentBase.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Track(ISgSignal signal)
    {
        SgReactiveComponentBase.TrackSignalImplicitly(signal);
    }

    /// <summary>
    /// Регистрация вычисляемой зависимости.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TrackComputed<T>(SgComputed<T> computed)
    {
        SgReactiveComponentBase.TrackSignalImplicitly(computed);
    }

    // Остальные методы можно оставить как заглушки или реализовать через SgReactiveComponentBase
    public static IDisposable EnterScope(ISignalObserver observer)
    {
        return SgReactiveComponentBase.EnterScope(observer);
    }
}
