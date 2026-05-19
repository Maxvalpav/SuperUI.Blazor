using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a single notification displayed by <see cref="SgNotificationPanel"/>.
/// </summary>
public sealed class NotificationItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the notification.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Gets or sets the notification title.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the notification body text.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets a short relative or absolute time label (e.g. "5 мин назад").
    /// </summary>
    public string? Time { get; set; }

    /// <summary>
    /// Gets or sets the absolute timestamp; used for default sorting when supplied.
    /// </summary>
    public DateTimeOffset? Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the variant. Default is <see cref="SgBadgeVariant.Default"/>.
    /// </summary>
    public SgBadgeVariant Variant { get; set; } = SgBadgeVariant.Default;

    /// <summary>
    /// Gets or sets a category/group name (e.g. "Система", "Сообщения").
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets whether the notification has been read.
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Gets or sets optional icon content rendered in the leading slot.
    /// </summary>
    public RenderFragment? IconContent { get; set; }

    /// <summary>
    /// Gets or sets optional extra content rendered below the message (e.g. action buttons).
    /// </summary>
    public RenderFragment? ExtraContent { get; set; }
}
