namespace SuperUI.Base.Services;

/// <summary>
/// Широковещательный сервис для межкомпонентной коммуникации.
/// In-process реализация через Channel (WASM: single-tab, Server: cross-circuit).
/// Для cross-tab/cross-server требуется внешний транспорт (SignalR/Redis).
/// </summary>
public interface ISgBroadcastService : IAsyncDisposable
{
    /// <summary>Опубликовать сообщение всем подписчикам того же канала.</summary>
    Task PublishAsync<T>(string channel, T message) where T : notnull;

    /// <summary>Подписаться на канал (async handler).</summary>
    IAsyncDisposable Subscribe<T>(string channel, Func<T, Task> handler) where T : notnull;

    /// <summary>Подписаться на канал (sync handler).</summary>
    IAsyncDisposable Subscribe<T>(string channel, Action<T> handler) where T : notnull;
}

/// <summary>Сообщение о присутствии пользователя в поле формы.</summary>
public sealed record SgFieldPresenceMessage(
    string UserId,
    string DisplayName,
    string FormId,
    string FieldName,
    bool IsEditing,
    DateTimeOffset Timestamp);
