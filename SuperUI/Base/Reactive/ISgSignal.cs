// SuperUI/Base/Reactive/ISgSignal.cs
// ✅ ARCH-1: Интерфейсы для мокирования в юнит-тестах и DI

namespace SuperUI.Base.Reactive;

/// <summary>
/// Интерфейс для чтения реактивного сигнала без отслеживания.
/// Используется в юнит-тестах с Mock&lt;IReadOnlySgSignal&lt;T&gt;&gt;.
/// </summary>
public interface IReadOnlySgSignal<out T>
{
    /// <summary>Текущее значение сигнала (с отслеживанием зависимости).</summary>
    T Value { get; }

    /// <summary>Прочитать значение без отслеживания зависимости.</summary>
    T Peek();

    /// <summary>Подписаться на изменения с callback.</summary>
    IDisposable Subscribe(Action<T> callback);
}

/// <summary>
/// Полный интерфейс реактивного сигнала (чтение + запись).
/// </summary>
public interface ISgSignal<T> : IReadOnlySgSignal<T>
{
    /// <summary>Установить новое значение (нотификация если изменилось).</summary>
    void Set(T newValue);

    /// <summary>Обновить значение через функцию.</summary>
    void Update(Func<T, T> updater);

    /// <summary>Сбросить значение без нотификации.</summary>
    void Reset(T value);

    /// <summary>Принудительно уведомить подписчиков.</summary>
    void ForceNotify();
}
