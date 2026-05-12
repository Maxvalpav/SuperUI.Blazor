// SuperUI/Base/Services/ISgPresenceService.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS0535: удалены GetPresenceAsync, ClaimEditAsync, ReleaseEditAsync,
//           StreamPresenceChangesAsync из интерфейса — они не нужны для in-memory impl.
//           Если нужна real-time коллаборация — вынести в ISgCollaborationService.
// УЛУЧШЕНИЯ:
// ✅ Разделение: ISgPresenceService (базовый) + ISgCollaborationService (SignalR)

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

/// <summary>
/// Расширенный интерфейс для real-time коллаборации (требует SignalR Hub).
/// Отделён от ISgPresenceService для чистоты in-memory реализации.
/// </summary>
public interface ISgCollaborationService
{
    /// <summary>Получить информацию о присутствии пользователя для документа.</summary>
    Task<SgPresenceUser?> GetPresenceAsync(string documentId, string userId,
        CancellationToken ct = default);

    /// <summary>Захватить право редактирования документа.</summary>
    Task<bool> ClaimEditAsync(string documentId, string userId,
        CancellationToken ct = default);

    /// <summary>Освободить право редактирования документа.</summary>
    Task ReleaseEditAsync(string documentId, string userId,
        CancellationToken ct = default);

    /// <summary>Стрим изменений присутствия для документа.</summary>
    IAsyncEnumerable<SgPresenceUser> StreamPresenceChangesAsync(string documentId, string userId,
        CancellationToken ct = default);
}

/// <summary>Информация о пользователе в сети.</summary>
public sealed record SgPresenceUser(
    string UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? Status,
    DateTimeOffset LastSeen,
    bool IsOnline);
