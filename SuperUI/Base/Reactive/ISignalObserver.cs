// SuperUI/Base/Reactive/ISignalObserver.cs
//
// ИСПРАВЛЕНИЕ CS0308:
// Вводим ДВА интерфейса:
//   ISignalObserver        — non-generic, используется в SignalTracker (object _currentObserver)
//   ISignalObserver<T>     — generic, реализуется вложенными классами Signal/Computed/Effect
//
// ПОЧЕМУ ДВА:
//   SignalTracker хранит _currentObserver как object (ThreadStatic),
//   не зная T. Поэтому нужен non-generic базовый интерфейс.
//   Typed методы OnSignalRead/OnComputedRead нужны только в typed-scope
//   (внутри SgSignal<T>, SgComputed<T>).

namespace SuperUI.Base.Reactive;

/// <summary>
/// Non-generic базовый интерфейс наблюдателя сигналов.
/// Реализуется всеми typed наблюдателями через ISignalObserver&lt;T&gt;.
/// Используется в SignalTracker для хранения без знания типа.
/// </summary>
public interface ISignalObserver
{
    /// <summary>Вызывается при изменении одного из отслеживаемых сигналов.</summary>
    void OnSignalChanged();
}

/// <summary>
/// Generic-интерфейс наблюдателя для typed сигналов и computed.
/// Реализуется вложенными observer-классами в SgSignal&lt;T&gt;, SgComputed&lt;T&gt;, SgEffect.
/// </summary>
/// <typeparam name="T">Тип значения сигнала.</typeparam>
public interface ISignalObserver<T> : ISignalObserver
{
    /// <summary>Вызывается при чтении сигнала в scope наблюдателя.</summary>
    void OnSignalRead(SgSignal<T> signal);

    /// <summary>Вызывается при чтении computed в scope наблюдателя.</summary>
    void OnComputedRead(SgComputed<T> computed);
}
