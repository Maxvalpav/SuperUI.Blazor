using System.Collections.Immutable;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// In-memory store of <see cref="NotificationItem"/>s shared between producers
/// (any code that calls <see cref="Push(NotificationItem)"/>) and consumers
/// (e.g. a header bell icon and the <see cref="SgNotificationPanel"/>).
/// Also manages transient toast notifications via <see cref="SgNotificationToastItem"/>.
/// </summary>
/// <remarks>
/// <para>Thread-safe: все мутации внутри <c>lock</c>, чтения наружу выходят как
/// иммутабельный снимок (<see cref="ImmutableArray{T}"/>) — подписчики получают
/// стабильный список даже если поток-источник дальше его меняет.</para>
/// <para>Event <see cref="Changed"/> вызывается ВНЕ блокировки — подписчик не
/// может вызвать deadlock или повторный mutation через тот же сервис.</para>
/// </remarks>
public sealed class SgNotificationService
{
    private readonly object _gate = new();
    private readonly List<NotificationItem> _items = new();
    private readonly List<SgNotificationToastItem> _toasts = new();
    private int _maxItems = 200;

    /// <summary>Raised whenever the notification list changes (added, removed, marked read).</summary>
    public event Action? Changed;

    /// <summary>Raised when the toast queue changes.</summary>
    public event Action? ToastsChanged;

    /// <summary>Иммутабельный снимок текущих уведомлений (самые новые первые).</summary>
    public IReadOnlyList<NotificationItem> Items
    {
        get
        {
            lock (_gate) return _items.ToImmutableArray();
        }
    }

    /// <summary>Количество непрочитанных уведомлений.</summary>
    public int UnreadCount
    {
        get
        {
            lock (_gate)
            {
                var count = 0;
                foreach (var it in _items) if (!it.IsRead) count++;
                return count;
            }
        }
    }

    /// <summary>
    /// Максимальное количество уведомлений в хранилище (FIFO-усечение хвоста).
    /// По умолчанию 200. Установите 0 чтобы отключить лимит.
    /// </summary>
    public int MaxItems
    {
        get { lock (_gate) return _maxItems; }
        set
        {
            bool changed;
            lock (_gate)
            {
                _maxItems = Math.Max(0, value);
                changed = TrimLocked();
            }
            if (changed) RaiseChanged();
        }
    }

    /// <summary>Добавляет уведомление в хранилище.</summary>
    public void Push(NotificationItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        lock (_gate)
        {
            item.Timestamp ??= DateTimeOffset.Now;
            _items.Insert(0, item);
            TrimLocked();
        }
        RaiseChanged();
    }

    /// <summary>Создаёт и добавляет уведомление из частей.</summary>
    public NotificationItem Push(
        string? title,
        string? message,
        SgBadgeVariant variant = SgBadgeVariant.Default,
        string? category = null,
        string? time = null)
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

    /// <summary>Помечает уведомление с указанным <paramref name="id"/> как прочитанное.</summary>
    public void MarkAsRead(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed = false;
        lock (_gate)
        {
            foreach (var item in _items)
            {
                if (item.Id == id)
                {
                    if (!item.IsRead) { item.IsRead = true; changed = true; }
                    break;
                }
            }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Переключает флаг прочитанности.</summary>
    public void ToggleRead(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed = false;
        lock (_gate)
        {
            foreach (var item in _items)
            {
                if (item.Id == id)
                {
                    item.IsRead = !item.IsRead;
                    changed = true;
                    break;
                }
            }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Помечает все уведомления как прочитанные.</summary>
    public void MarkAllAsRead()
    {
        bool changed = false;
        lock (_gate)
        {
            foreach (var item in _items)
            {
                if (!item.IsRead) { item.IsRead = true; changed = true; }
            }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Удаляет уведомление из хранилища.</summary>
    public void Remove(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed;
        lock (_gate)
        {
            changed = _items.RemoveAll(x => x.Id == id) > 0;
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Удаляет все уведомления.</summary>
    public void Clear()
    {
        bool changed;
        lock (_gate)
        {
            changed = _items.Count > 0;
            _items.Clear();
        }
        if (changed) RaiseChanged();
    }

    // ── Bulk operations ────────────────────────────────────────────────────

    /// <summary>Removes multiple notifications by id.</summary>
    public void RemoveMany(IEnumerable<string> ids)
    {
        var idSet = new HashSet<string>(ids);
        bool changed;
        lock (_gate)
        {
            changed = _items.RemoveAll(x => idSet.Contains(x.Id)) > 0;
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Marks multiple notifications as read.</summary>
    public void MarkManyAsRead(IEnumerable<string> ids)
    {
        var idSet = new HashSet<string>(ids);
        bool changed = false;
        lock (_gate)
        {
            foreach (var item in _items)
                if (idSet.Contains(item.Id) && !item.IsRead) { item.IsRead = true; changed = true; }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Marks multiple notifications as unread.</summary>
    public void MarkManyAsUnread(IEnumerable<string> ids)
    {
        var idSet = new HashSet<string>(ids);
        bool changed = false;
        lock (_gate)
        {
            foreach (var item in _items)
                if (idSet.Contains(item.Id) && item.IsRead) { item.IsRead = false; changed = true; }
        }
        if (changed) RaiseChanged();
    }

    /// <summary>Removes all read notifications.</summary>
    public void ClearRead()
    {
        bool changed;
        lock (_gate)
        {
            changed = _items.RemoveAll(x => x.IsRead) > 0;
        }
        if (changed) RaiseChanged();
    }

    // ── Snooze ─────────────────────────────────────────────────────────────

    /// <summary>Snoozes a notification until the specified time. Removes it from the active list until then.</summary>
    public void Snooze(string id, DateTimeOffset until)
    {
        if (string.IsNullOrEmpty(id)) return;
        lock (_gate)
        {
            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null) return;
            item.SnoozeUntil = until;
        }
        RaiseChanged();
    }

    /// <summary>Unsnoozes a notification — moves it back to the active list immediately.</summary>
    public void Unsnooze(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        lock (_gate)
        {
            var item = _items.FirstOrDefault(x => x.Id == id);
            if (item is null) return;
            item.SnoozeUntil = null;
        }
        RaiseChanged();
    }

    // ── Toast queue ────────────────────────────────────────────────────────

    /// <summary>Current snapshot of active toast notifications.</summary>
    public IReadOnlyList<SgNotificationToastItem> Toasts
    {
        get { lock (_gate) return _toasts.ToImmutableArray(); }
    }

    /// <summary>Pushes a transient toast notification.</summary>
    public void PushToast(SgNotificationToastItem toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        lock (_gate)
        {
            _toasts.Insert(0, toast);
        }
        RaiseToastsChanged();
    }

    /// <summary>Creates and pushes a toast notification from parts.</summary>
    public SgNotificationToastItem PushToast(string? title, string? message, SgBadgeVariant variant = SgBadgeVariant.Default, int durationMs = 4000)
    {
        var toast = new SgNotificationToastItem
        {
            Title = title,
            Message = message,
            Variant = variant,
            DurationMs = durationMs
        };
        PushToast(toast);
        return toast;
    }

    /// <summary>Removes a toast notification by id.</summary>
    public void RemoveToast(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        bool changed;
        lock (_gate)
        {
            changed = _toasts.RemoveAll(x => x.Id == id) > 0;
        }
        if (changed) RaiseToastsChanged();
    }

    /// <summary>Marks a toast as closing (for exit animation).</summary>
    public void CloseToast(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        lock (_gate)
        {
            var toast = _toasts.FirstOrDefault(x => x.Id == id);
            if (toast is not null) toast.IsClosing = true;
        }
        RaiseToastsChanged();
    }

    /// <summary>Clears all toasts.</summary>
    public void ClearToasts()
    {
        bool changed;
        lock (_gate)
        {
            changed = _toasts.Count > 0;
            _toasts.Clear();
        }
        if (changed) RaiseToastsChanged();
    }

    // ── Snapshot helpers for filtering ─────────────────────────────────────

    /// <summary>Returns only active (non-snoozed) items.</summary>
    public List<NotificationItem> GetActiveItems()
    {
        lock (_gate)
        {
            return _items.Where(x => !x.IsSnoozed).ToList();
        }
    }

    /// <summary>Returns snoozed items that should reappear now.</summary>
    public List<NotificationItem> GetDueSnoozedItems()
    {
        lock (_gate)
        {
            return _items.Where(x => x.SnoozeUntil.HasValue && x.SnoozeUntil <= DateTimeOffset.Now).ToList();
        }
    }

    /// <summary>Returns currently snoozed items.</summary>
    public List<NotificationItem> GetSnoozedItems()
    {
        lock (_gate)
        {
            return _items.Where(x => x.IsSnoozed).ToList();
        }
    }

    /// <summary>Resurrects snoozed items whose time has come — moves them back to the active set.</summary>
    public int UnsnoozeDueItems()
    {
        var due = new List<NotificationItem>();
        lock (_gate)
        {
            foreach (var item in _items)
                if (item.SnoozeUntil.HasValue && item.SnoozeUntil <= DateTimeOffset.Now)
                {
                    item.SnoozeUntil = null;
                    due.Add(item);
                }
        }
        if (due.Count > 0) RaiseChanged();
        return due.Count;
    }

    private bool TrimLocked()
    {
        if (_maxItems <= 0 || _items.Count <= _maxItems) return false;
        _items.RemoveRange(_maxItems, _items.Count - _maxItems);
        return true;
    }

    private void RaiseToastsChanged()
    {
        var handler = ToastsChanged;
        if (handler is null) return;
        foreach (var d in handler.GetInvocationList())
        {
            try { ((Action)d).Invoke(); }
            catch { }
        }
    }

    private void RaiseChanged()
    {
        // snapshot — подписчик может отписаться/мутировать ивент.
        var handler = Changed;
        if (handler is null) return;

        foreach (var d in handler.GetInvocationList())
        {
            try { ((Action)d).Invoke(); }
            catch { /* один подписчик не должен срывать остальных */ }
        }
    }
}
