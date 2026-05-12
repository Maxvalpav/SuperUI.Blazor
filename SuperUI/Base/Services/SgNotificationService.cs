// SuperUI/Base/Services/SgNotificationService.cs

// ИСПРАВЛЕНИЯ:
// ✅ CS0311: реализует ISgNotificationService (все члены)

using SuperUI.Components;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления лентой уведомлений (Notification Center).
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgNotificationService : ISgNotificationService
{
    private readonly List<SgNotification> _notifications = [];
    private readonly Lock _lock = new();
    private int _nextId;

    // ── ISgNotificationService ───────────────────────────────────────────────

    /// <inheritdoc/>
    public IReadOnlyList<SgNotification> Notifications
    {
        get { lock (_lock) return [.. _notifications]; }
    }

    /// <inheritdoc/>
    public int UnreadCount
    {
        get { lock (_lock) return _notifications.Count(n => !n.IsRead); }
    }

    /// <inheritdoc/>
    public event Action? OnChange;

    /// <inheritdoc/>
    public SgNotification Add(string title,
        string? message = null,
        string? icon = null,
        string? href = null,
        SgAlertVariant variant = SgAlertVariant.Info)
    {
        var notification = new SgNotification(
            Id: Interlocked.Increment(ref _nextId),
            Title: title,
            Message: message,
            Icon: icon,
            Href: href,
            Variant: variant,
            CreatedAt: DateTimeOffset.UtcNow,
            IsRead: false);

        lock (_lock)
        {
            _notifications.Insert(0, notification);
            if (_notifications.Count > 200)
                _notifications.RemoveAt(_notifications.Count - 1);
        }

        OnChange?.Invoke();
        return notification;
    }

    /// <inheritdoc/>
    public void MarkAsRead(int id)
    {
        bool changed = false;

        lock (_lock)
        {
            var idx = _notifications.FindIndex(n => n.Id == id);
            if (idx >= 0 && !_notifications[idx].IsRead)
            {
                _notifications[idx] = _notifications[idx] with { IsRead = true };
                changed = true;
            }
        }

        if (changed) OnChange?.Invoke();
    }

    /// <inheritdoc/>
    public void MarkAllAsRead()
    {
        lock (_lock)
            for (int i = 0; i < _notifications.Count; i++)
                if (!_notifications[i].IsRead)
                    _notifications[i] = _notifications[i] with { IsRead = true };

        OnChange?.Invoke();
    }

    /// <inheritdoc/>
    public void Remove(int id)
    {
        bool removed;

        lock (_lock) removed = _notifications.RemoveAll(n => n.Id == id) > 0;

        if (removed) OnChange?.Invoke();
    }

    /// <inheritdoc/>
    public void Clear()
    {
        lock (_lock) _notifications.Clear();

        OnChange?.Invoke();
    }
}
