// SuperUI/Base/Services/ISgPresenceService.cs

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис присутствия пользователей (online/offline/status).
/// Scoped: per-user на Blazor Server, per-tab на WASM.
///
/// Для реального multi-user real-time: реализуйте через SignalR Hub.
/// </summary>
public interface ISgPresenceService : IAsyncDisposable
{
    /// <summary>Текущий пользователь находится онлайн.</summary>
    bool IsOnline { get; }

    /// <summary>Статус текущего пользователя (null = не установлен).</summary>
    string? Status { get; }

    /// <summary>
    /// Список известных онлайн-пользователей.
    /// На WASM обычно содержит только текущего пользователя.
    /// </summary>
    IReadOnlyList<SgPresenceUser> OnlineUsers { get; }

    /// <summary>
    /// Событие изменения присутствия любого пользователя.
    /// Вызывается при SetOnlineAsync / SetOfflineAsync / UpdateStatusAsync.
    /// </summary>
    event Action<SgPresenceUser>? PresenceChanged;

    /// <summary>Обновить статус текущего пользователя.</summary>
    Task UpdateStatusAsync(string status, CancellationToken ct = default);

    /// <summary>
    /// Установить текущего пользователя как онлайн.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя.</param>
    /// <param name="displayName">Отображаемое имя (необязательно).</param>
    /// <param name="ct">Токен отмены.</param>
    Task SetOnlineAsync(string userId, string? displayName = null, CancellationToken ct = default);

    /// <summary>Установить текущего пользователя как оффлайн.</summary>
    Task SetOfflineAsync(CancellationToken ct = default);
}

/// <summary>Информация о пользователе в сети.</summary>
public sealed record SgPresenceUser(
    string UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Status,
    DateTimeOffset LastSeen,
    bool IsOnline);
