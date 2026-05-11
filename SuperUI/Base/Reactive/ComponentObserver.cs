// SuperUI/Base/Reactive/ComponentObserver.cs
// НОВОЕ: Интерфейс подписчика на Signal для legacy реактивности.
// Реализуется SgComponentBase через ComponentSignalTracker.
// Signal<T> и SignalTracker вынесены в отдельные файлы.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс подписчика на Signal.
/// Реализуется SgComponentBase через ComponentSignalTracker.
/// </summary>
public interface ISignalSubscriber
{
    void OnSignalChanged();
}