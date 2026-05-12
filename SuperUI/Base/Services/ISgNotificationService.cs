// SuperUI/Base/Services/ISgNotificationService.cs

namespace SuperUI.Base.Services;

/// <summary>Сервис уведомлений (Notification Center).</summary>
public interface ISgNotificationService
{
    /// <summary>Добавить уведомление.</summary>
    void Add(SgNotificationItem notification);

    /// <summary>Пометить уведомление как прочитанное.</summary>
    void MarkAsRead(string notificationId);

    /// <summary>Пометить все как прочитанные.</summary>
    void MarkAllAsRead();

    /// <summary>Удалить уведомление.</summary>
    void Remove(string notificationId);

    /// <summary>Очистить все уведомления.</summary>
    void Clear();

    /// <summary>Все уведомления.</summary>
    IReadOnlyList<SgNotificationItem> Notifications { get; }

    /// <summary>Количество непрочитанных.</summary>
    int UnreadCount { get; }

    /// <summary>Событие изменения списка.</summary>
    event Action? OnChange;
}

/// <summary>Элемент уведомления.</summary>
public sealed class SgNotificationItem
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public SgNotificationType Type { get; set; } = SgNotificationType.Info;
    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public bool IsRead { get; set; }
    public int? DurationMs { get; set; } = 5000; // null = не автоудаляется
    public Action? OnClick { get; set; }
}

/// <summary>Тип уведомления.</summary>
public enum SgNotificationType
{
    Info,
    Success,
    Warning,
    Error
}
