// SuperUI/Base/Reactive/ISignalObserver.cs
// Интерфейс для наблюдателей сигналов (computed, effects)
namespace SuperUI.Base.Reactive;

/// <summary>
/// Наблюдатель изменений сигналов.
/// Реализуется SgComputed и SgEffect для получения уведомлений
/// об изменении зависимых сигналов.
/// </summary>
public interface ISignalObserver
{
    /// <summary>Вызывается при изменении одного из отслеживаемых сигналов.</summary>
    void OnSignalChanged();

    /// <summary>Вызывается при чтении сигнала в scope наблюдателя.</summary>
    void OnSignalRead<T>(SgSignal<T> signal);

    /// <summary>Вызывается при чтении computed в scope наблюдателя.</summary>
    void OnComputedRead<T>(SgComputed<T> computed);
}