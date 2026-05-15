using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// In-memory store of <see cref="NotificationItem"/>s shared between producers (any code that calls <see cref="Push(NotificationItem)"/>)
/// and consumers (e.g. a header bell icon and the <see cref="SgNotificationPanel"/>).
/// Register via <see cref="ServiceCollectionExtensions.AddSuperUI(Microsoft.Extensions.DependencyInjection.IServiceCollection)"/>.
/// </summary>
public sealed class SgNotificationService
{
    private readonly List<NotificationItem> _items = new();

    /// <summary>
    /// Raised whenever the notification list changes (added, removed, marked read).
    /// </summary>
    public event Action? Changed;

    /// <summary>
    /// Gets the current list of notifications (newest first).
    /// </summary>
    public IReadOnlyList<NotificationItem> Items => _items;

    /// <summary>
    /// Gets the number of unread notifications.
    /// </summary>
    public int UnreadCount => _items.Count(x => !x.IsRead);

    /// <summary>
    /// Adds a notification to the store.
    /// </summary>
    public void Push(NotificationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (item.Timestamp is null) item.Timestamp = DateTimeOffset.Now;
        _items.Insert(0, item);
        Changed?.Invoke();
    }

    /// <summary>
    /// Adds a notification using the supplied parts.
    /// </summary>
    public NotificationItem Push(string? title, string? message, SgBadgeVariant variant = SgBadgeVariant.Default, string? category = null, string? time = null)
    {
        var item = new NotificationItem
        {
            Title = title,
            Message = message,
            Variant = variant,
            Category = category,
            Time = time,
            Timestamp = DateTimeOffset.Now
        };
        Push(item);
        return item;
    }

    /// <summary>
    /// Marks the notification with the given <paramref name="id"/> as read.
    /// </summary>
    public void MarkAsRead(string id)
    {
        var item = _items.FirstOrDefault(x => x.Id == id);
        if (item is null || item.IsRead) return;
        item.IsRead = true;
        Changed?.Invoke();
    }

    /// <summary>
    /// Marks all notifications as read.
    /// </summary>
    public void MarkAllAsRead()
    {
        var changed = false;
        foreach (var item in _items)
        {
            if (!item.IsRead) { item.IsRead = true; changed = true; }
        }
        if (changed) Changed?.Invoke();
    }

    /// <summary>
    /// Removes a notification from the store.
    /// </summary>
    public void Remove(string id)
    {
        if (_items.RemoveAll(x => x.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Removes all notifications.
    /// </summary>
    public void Clear()
    {
        if (_items.Count == 0) return;
        _items.Clear();
        Changed?.Invoke();
    }
}
