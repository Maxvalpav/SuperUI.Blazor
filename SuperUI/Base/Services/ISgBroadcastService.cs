// SuperUI/Base/Services/ISgBroadcastService.cs

namespace SuperUI.Base.Services;

/// <summary>
/// Внутрипроцессная шина сообщений между компонентами SuperUI.
/// Singleton. Thread-safe. In-process (не cross-server).
///
/// Паттерн: type-based pub/sub (без string channel).
/// Для cross-server: переопределите реализацию на SignalR Hub или Redis.
/// </summary>
public interface ISgBroadcastService : IAsyncDisposable
{
    /// <summary>
    /// Подписаться на сообщения типа T (sync-обработчик).
    /// </summary>
    /// <typeparam name="T">Тип сообщения.</typeparam>
    /// <param name="handler">Обработчик.</param>
    /// <returns>IDisposable — вызовите Dispose() для отписки.</returns>
    IDisposable Subscribe<T>(Action<T> handler) where T : notnull;

    /// <summary>
    /// Подписаться на сообщения типа T (async-обработчик).
    /// </summary>
    IDisposable Subscribe<T>(Func<T, Task> handler) where T : notnull;

    /// <summary>
    /// Опубликовать сообщение всем подписчикам типа T (async).
    /// Ошибки в подписчиках не прерывают остальных.
    /// </summary>
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : notnull;

    /// <summary>
    /// Синхронная публикация (fire-and-forget).
    /// Async-обработчики запускаются через Task.Run.
    /// </summary>
    void Publish<T>(T message) where T : notnull;

    /// <summary>
    /// Количество активных подписчиков для типа T.
    /// Используется в диагностике/тестах.
    /// </summary>
    int GetSubscriberCount<T>() where T : notnull;
}

/// <summary>
/// Сообщение о присутствии пользователя в конкретном поле формы.
/// Используется для real-time collaboration в SgDataForm.
/// </summary>
public sealed record SgFieldPresenceMessage(
    string UserId,
    string DisplayName,
    string FormId,
    string FieldName,
    bool IsEditing,
    DateTimeOffset Timestamp);

/// <summary>
/// Сообщение для глобального обновления темы.
/// </summary>
public sealed record SgThemeChangedMessage(
    SgTheme NewTheme,
    bool IsRtl);

/// <summary>
/// Сообщение для обновления данных компонента по ключу.
/// </summary>
public sealed record SgDataRefreshMessage(
    string ComponentId,
    object? Payload = null);
