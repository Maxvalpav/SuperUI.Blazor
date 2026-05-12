using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления лентой уведомлений (notification feed).
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgNotificationService
{
    private readonly List<SgNotification> _notifications = [];
    private readonly Lock _lock = new();
    private int _nextId;

    /// <summary>Все уведомления (snapshot, новые первые).</summary>
    public IReadOnlyList<SgNotification> Notifications
    {
        get { lock (_lock) return [.. _notifications]; }
    }

    /// <summary>Количество непрочитанных уведомлений.</summary>
    public int UnreadCount
    {
        get { lock (_lock) return _notifications.Count(n => !n.IsRead); }
    }

    /// <summary>Событие изменения ленты.</summary>
    public event Action? OnChange;

    /// <summary>Добавить уведомление.</summary>
    public SgNotification Add(
        string title,
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
            _notifications.Insert(0, notification); // новые первые
            // Ограничение: максимум 200 уведомлений
            if (_notifications.Count > 200)
                _notifications.RemoveAt(_notifications.Count - 1);
        }

        OnChange?.Invoke();
        return notification;
    }

    /// <summary>Отметить уведомление как прочитанное.</summary>
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

    /// <summary>Отметить все как прочитанные.</summary>
    public void MarkAllAsRead()
    {
        lock (_lock)
            for (int i = 0; i < _notifications.Count; i++)
                if (!_notifications[i].IsRead)
                    _notifications[i] = _notifications[i] with { IsRead = true };
        OnChange?.Invoke();
    }

    /// <summary>Удалить уведомление.</summary>
    public void Remove(int id)
    {
        bool removed;
        lock (_lock) removed = _notifications.RemoveAll(n => n.Id == id) > 0;
        if (removed) OnChange?.Invoke();
    }

    /// <summary>Очистить все уведомления.</summary>
    public void Clear()
    {
        lock (_lock) _notifications.Clear();
        OnChange?.Invoke();
    }
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
