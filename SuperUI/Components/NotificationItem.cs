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
    /// Gets or sets a short relative or absolute time label (e.g. "5 min ago").
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
    /// Gets or sets a category/group name (e.g. "System", "Messages").
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

    /// <summary>
    /// Gets or sets the notification priority level. Higher priority items are visually emphasized.
    /// </summary>
    public SgNotificationPriority Priority { get; set; } = SgNotificationPriority.Default;

    /// <summary>
    /// Gets or sets whether the notification is pinned (persistent).
    /// Pinned items appear at the top of the list.
    /// </summary>
    public bool IsPinned { get; set; }

    /// <summary>
    /// Gets or sets avatar image URL. When set, the leading dot/icon is replaced with the avatar.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets avatar initials displayed when <see cref="AvatarUrl"/> is null.
    /// </summary>
    public string? AvatarName { get; set; }

    /// <summary>
    /// Gets or sets optional action buttons displayed in the notification item.
    /// </summary>
    public List<SgNotificationAction>? Actions { get; set; }

    /// <summary>
    /// Gets or sets a navigation URL. When set, clicking the item navigates to this URL.
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Gets or sets a small tag label (e.g. "NEW", "BETA").
    /// </summary>
    public string? Tag { get; set; }

    /// <summary>
    /// Gets or sets the notification channel (e.g. "email", "system", "billing").
    /// Used for channel filtering and channel badge display.
    /// </summary>
    public string? Channel { get; set; }

    /// <summary>
    /// Gets or sets the absolute start time. Shown in snooze picker as the time the
    /// notification will reappear. If null, the item was not snoozed.
    /// </summary>
    public DateTimeOffset? SnoozeUntil { get; set; }

    /// <summary>
    /// Gets or sets whether this notification has been snoozed and is waiting to reappear.
    /// </summary>
    public bool IsSnoozed => SnoozeUntil.HasValue && SnoozeUntil > DateTimeOffset.Now;
}
