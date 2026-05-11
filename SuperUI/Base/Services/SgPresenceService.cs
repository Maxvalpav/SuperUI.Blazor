// SuperUI/Base/Services/SgPresenceService.cs
// ИСПРАВЛЕНО:
// 1. Убран using System.Reactive.Subjects (CS0234 — пакет не подключён)
// 2. Заменён IObservable<T> на IAsyncEnumerable<T> (Channel-based, без зависимостей)
// 3. Добавлен минимальный IObservable<T> через собственный Subject<T> без внешних зависимостей
// Примечание: если System.Reactive нужен — добавьте NuGet: System.Reactive
namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для real-time присутствия — показывает кто сейчас редактирует что.
/// Интеграция с Blazor Server SignalR для мультипользовательских сценариев.
/// На WASM: работает через HTTP polling или WebSocket напрямую.
/// </summary>
public interface ISgPresenceService
{
    /// <summary>Получить список пользователей, просматривающих/редактирующих объект.</summary>
    Task<IReadOnlyList<SgPresenceUser>> GetPresenceAsync(string entityType, string entityId);

    /// <summary>Заявить о редактировании объекта.</summary>
    Task ClaimEditAsync(string entityType, string entityId);

    /// <summary>Освободить объект.</summary>
    Task ReleaseEditAsync(string entityType, string entityId);

    /// <summary>
    /// Подписка на изменения присутствия.
    /// ИСПРАВЛЕНО: собственный IObservable без зависимости от System.Reactive.
    /// </summary>
    IObservable<SgPresenceChangedEvent> PresenceChanged { get; }

    /// <summary>
    /// AsyncEnumerable вариант для Blazor Server (SignalR streaming).
    /// </summary>
    IAsyncEnumerable<SgPresenceChangedEvent> StreamPresenceChangesAsync(
        string entityType, string entityId, CancellationToken ct = default);
}

public record SgPresenceUser(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    bool IsEditing);

public record SgPresenceChangedEvent(
    string EntityType,
    string EntityId,
    IReadOnlyList<SgPresenceUser> Users);

/// <summary>
/// Минимальная реализация Subject без внешних зависимостей.
/// Для полноценного Rx — добавьте NuGet: System.Reactive
/// </summary>
public sealed class SgSubject<T> : IObservable<T>, IDisposable
{
    private readonly List<IObserver<T>> _observers = new();
    private readonly Lock _lock = new();
    private bool _completed;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_lock)
        {
            if (!_completed)
                _observers.Add(observer);
        }
        return new Subscription(this, observer);
    }

    public void OnNext(T value)
    {
        IObserver<T>[] snapshot;
        lock (_lock) { snapshot = _observers.ToArray(); }
        foreach (var o in snapshot)
        {
            try { o.OnNext(value); }
            catch { /* observer не должен бросать */ }
        }
    }

    public void OnCompleted()
    {
        IObserver<T>[] snapshot;
        lock (_lock) { _completed = true; snapshot = _observers.ToArray(); _observers.Clear(); }
        foreach (var o in snapshot) { try { o.OnCompleted(); } catch { } }
    }

    public void Dispose() => OnCompleted();

    private sealed class Subscription : IDisposable
    {
        private readonly SgSubject<T> _subject;
        private readonly IObserver<T> _observer;

        public Subscription(SgSubject<T> subject, IObserver<T> observer)
        {
            _subject = subject;
            _observer = observer;
        }

        public void Dispose()
        {
            lock (_subject._lock)
                _subject._observers.Remove(_observer);
        }
    }
}