// SuperUI/Base/Services/SgPresenceService.cs

using System.Reactive.Subjects;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис для real-time присутствия — показывает кто сейчас редактирует что.
/// Интеграция с Blazor Server SignalR для мультипользовательских сценариев.
/// </summary>
public interface ISgPresenceService
{
    /// <summary>Получить список пользователей, просматривающих/редактирующих объект.</summary>
    Task<IReadOnlyList<SgPresenceUser>> GetPresenceAsync(string entityType, string entityId);

    /// <summary>Заявить о редактировании объекта.</summary>
    Task ClaimEditAsync(string entityType, string entityId);

    /// <summary>Освободить объект.</summary>
    Task ReleaseEditAsync(string entityType, string entityId);

    /// <summary>Подписка на изменения присутствия.</summary>
    IObservable<SgPresenceChangedEvent> PresenceChanged { get; }
}

public record SgPresenceUser(string UserId, string DisplayName, string? AvatarUrl, bool IsEditing);
public record SgPresenceChangedEvent(string EntityType, string EntityId, IReadOnlyList<SgPresenceUser> Users);
