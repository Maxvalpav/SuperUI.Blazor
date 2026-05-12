namespace SuperUI.Base.Services;

/// <summary>
/// Сервис широковещательных сообщений между компонентами.
/// Singleton, thread-safe (ConcurrentDictionary + lock).
/// Аналог EventBus / MessageBus.
/// </summary>
public interface ISgBroadcastService
{
    /// <summary>Подписаться на сообщения типа T.</summary>
    IDisposable Subscribe<T>(Action<T> handler);

    /// <summary>Подписаться на сообщения типа T (async).</summary>
    IDisposable Subscribe<T>(Func<T, Task> handler);

    /// <summary>Опубликовать сообщение всем подписчикам.</summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default);

    /// <summary>Синхронная публикация (fire-and-forget).</summary>
    void Publish<T>(T message);

    /// <summary>Количество подписчиков для типа T (диагностика).</summary>
    int GetSubscriberCount<T>();
}

/// <summary>Сообщение о присутствии пользователя в поле формы.</summary>
public sealed record SgFieldPresenceMessage(
    string UserId,
    string DisplayName,
    string FormId,
    string FieldName,
    bool IsEditing,
    DateTimeOffset Timestamp);
