// SuperUI/Base/Services/ISgBroadcastService.cs
// Cross-tab / cross-circuit messaging
// WASM: BroadcastChannel API (через JS interop)
// Server: SignalR Hub (через IHubContext)
namespace SuperUI.Base.Services;

/// <summary>
/// Широковещательный сервис для inter-tab (WASM) и inter-circuit (Server) коммуникации.
/// На WASM использует BroadcastChannel Web API.
/// На Server использует SignalR IHubContext.
/// </summary>
public interface ISgBroadcastService : IAsyncDisposable
{
    /// <summary>Отправить сообщение всем подписчикам того же канала.</summary>
    Task PublishAsync<T>(string channel, T message) where T : notnull;

    /// <summary>Подписаться на канал.</summary>
    IAsyncDisposable Subscribe<T>(string channel, Func<T, Task> handler) where T : notnull;

    /// <summary>Подписаться на канал (синхронный handler).</summary>
    IAsyncDisposable Subscribe<T>(string channel, Action<T> handler) where T : notnull;
}

/// <summary>
/// Сообщение о присутствии пользователя в поле формы.
/// Используется для Conflict-free Editing Indicators.
/// </summary>
public sealed record SgFieldPresenceMessage(
    string UserId,
    string DisplayName,
    string FormId,
    string FieldName,
    bool IsEditing,
    DateTimeOffset Timestamp);