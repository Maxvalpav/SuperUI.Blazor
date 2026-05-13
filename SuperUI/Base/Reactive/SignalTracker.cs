using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер реактивных зависимостей (Legacy wrapper).
/// Рекомендуется использовать SgReactiveComponentBase для новых компонентов.
/// </summary>
public static class SignalTracker
{
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
