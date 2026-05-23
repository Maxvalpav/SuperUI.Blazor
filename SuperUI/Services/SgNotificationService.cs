using System.Collections.Immutable;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// In-memory store of <see cref="NotificationItem"/>s shared between producers
/// (any code that calls <see cref="Push(NotificationItem)"/>) and consumers
/// (e.g. a header bell icon and the <see cref="SgNotificationPanel"/>).
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
    private int _maxItems = 200;

    /// <summary>Raised whenever the notification list changes (added, removed, marked read).</summary>
    public event Action? Changed;

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

    private bool TrimLocked()
    {
        if (_maxItems <= 0 || _items.Count <= _maxItems) return false;
        _items.RemoveRange(_maxItems, _items.Count - _maxItems);
        return true;
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
