// SuperUI/Base/Services/ISgNotificationService.cs

// ИСПРАВЛЕНИЯ:
// ✅ CS0311: сигнатура совпадает с SgNotificationService
// ПОДХОД: интерфейс приведён к реализации

using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>Сервис уведомлений (Notification Center).</summary>
public interface ISgNotificationService
{
    // ── Данные ──────────────────────────────────────────────────────────────

    /// <summary>Все уведомления (новые первые).</summary>
    IReadOnlyList<SgNotification> Notifications { get; }

    /// <summary>Алиас для Notifications (для совместимости).</summary>
    IReadOnlyList<SgNotification> Items => Notifications;

    /// <summary>Количество непрочитанных.</summary>
    int UnreadCount { get; }

    // ── События ─────────────────────────────────────────────────────────────

    /// <summary>Событие изменения ленты.</summary>
    event Action? OnChange;

    /// <summary>Алиас для OnChange (для совместимости).</summary>
    event Action? Changed
    {
        add { OnChange += value; }
        remove { OnChange -= value; }
    }

    // ── API ─────────────────────────────────────────────────────────────────

    /// <summary>Добавить уведомление.</summary>
    SgNotification Add(string title,
        string? message = null,
        string? icon = null,
        string? href = null,
        SgAlertVariant variant = SgAlertVariant.Info);

    /// <summary>Отметить уведомление как прочитанное.</summary>
    void MarkAsRead(int id);

    /// <summary>Отметить все как прочитанные.</summary>
    void MarkAllAsRead();

    /// <summary>Удалить уведомление.</summary>
    void Remove(int id);

    /// <summary>Очистить все уведомления.</summary>
    void Clear();
}

/// <summary>Уведомление в ленте.</summary>
public sealed record SgNotification(
    int Id,
    string Title,
    string? Message,
    string? Icon,
    string? Href,
    SgAlertVariant Variant,
    DateTimeOffset CreatedAt,
    bool IsRead);
