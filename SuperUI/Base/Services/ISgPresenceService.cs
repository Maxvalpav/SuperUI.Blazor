// SuperUI/Base/Services/ISgPresenceService.cs

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис присутствия пользователей (online/offline).
/// Scoped: per-user на Server, per-tab на WASM.
/// </summary>
public interface ISgPresenceService : IAsyncDisposable
{
    /// <summary>Текущий пользователь онлайн.</summary>
    bool IsOnline { get; }

    /// <summary>Статус пользователя.</summary>
    string? Status { get; }

    /// <summary>Список онлайн пользователей (для collaboration).</summary>
    IReadOnlyList<SgPresenceUser> OnlineUsers { get; }

    /// <summary>Событие изменения присутствия.</summary>
    event Action<SgPresenceUser>? PresenceChanged;

    /// <summary>Обновить статус текущего пользователя.</summary>
    Task UpdateStatusAsync(string status, CancellationToken ct = default);

    /// <summary>Установить пользователя как онлайн.</summary>
    Task SetOnlineAsync(string userId, string? displayName = null, CancellationToken ct = default);

    /// <summary>Установить пользователя как оффлайн.</summary>
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
