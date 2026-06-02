using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using SuperUI.Base.Utilities;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// In-memory store of <see cref="NotificationItem"/>s shared between producers
/// (any code that calls <see cref="Push(NotificationItem)"/>) and consumers
/// (e.g. a header bell icon and the <see cref="SgNotificationPanel"/>).
/// Also bridges transient toast notifications through <see cref="SgToastService"/>.
/// </summary>
/// <remarks>
/// <para>Thread-safe: все мутации внутри <c>lock</c>, чтения наружу выходят как
/// иммутабельный снимок (<see cref="ImmutableArray{T}"/>) — подписчики получают
/// стабильный список даже если поток-источник дальше его меняет.</para>
/// <para>Event <see cref="Changed"/> вызывается ВНЕ блокировки — подписчик не
/// может вызвать deadlock или повторный mutation через тот же сервис.</para>
/// <para>Toast notifications now delegate to <see cref="SgToastService"/> internally.
/// The legacy toast queue is kept for backward compatibility but
/// deprecated — use <see cref="SgToastHost"/> + <see cref="SgToastService"/> directly.</para>
/// </remarks>
public sealed class SgNotificationService
{
    private const string PersistenceKey = "superui.notifications.v1";

    private readonly object _gate = new();
    private readonly List<NotificationItem> _items = new();
    private readonly List<SgNotificationToastItem> _toasts = new();
    private readonly SgToastService? _toastService;
    private Services.SgStorageService? _storage;
    private int _maxItems = 200;
    private bool _persistenceEnabled;
    private bool _initialized;

    private static readonly JsonSerializerOptions s_json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>Initializes a new instance with optional toast service for bridging notifications to toasts.</summary>
    public SgNotificationService() : this(null) { }

    /// <summary>Initializes a new instance with toast service bridge.</summary>
    public SgNotificationService(SgToastService? toastService)
    {
        _toastService = toastService;
    }

    /// <summary>True, если уведомления сохраняются в localStorage между сессиями.</summary>
    public bool IsPersistenceEnabled => _persistenceEnabled;

    /// <summary>Включает/выключает persistence через SgStorageService. Загружает сохранённые при включении.</summary>
    public async Task EnablePersistenceAsync(Services.SgStorageService storage)
    {
        ArgumentNullException.ThrowIfNull(storage);
        _storage = storage;
        _persistenceEnabled = true;
        await RestoreFromStorageAsync().ConfigureAwait(false);
    }

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

    /// <summary>Pushes a transient toast notification. Delegates to SgToastService when available.</summary>
    public void PushToast(SgNotificationToastItem toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        if (_toastService is not null)
        {
            var variant = toast.Variant switch
            {
                SgBadgeVariant.Success => SgToastVariant.Success,
                SgBadgeVariant.Warn => SgToastVariant.Warn,
                SgBadgeVariant.Danger => SgToastVariant.Danger,
                SgBadgeVariant.Info => SgToastVariant.Default,
                _ => SgToastVariant.Default
            };
            _toastService.Show(t => { t.Title = toast.Title; t.Message = toast.Message; t.Variant = variant; t.DurationMs = toast.DurationMs; });
            return;
        }

        // Fallback: keep old behavior when no SgToastService available
        lock (_gate)
        {
            _toasts.Insert(0, toast);
        }
    }

    /// <summary>Creates and pushes a toast notification from parts. Delegates to SgToastService when available.</summary>
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

    /// <summary>Возвращает снимок уведомлений, отфильтрованных по <paramref name="predicate"/>.</summary>
    public List<NotificationItem> Where(Func<NotificationItem, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        lock (_gate) { return _items.Where(predicate).ToList(); }
    }

    /// <summary>Возвращает уведомления с указанной категорией.</summary>
    public List<NotificationItem> GetByCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) return GetActiveItems();
        return Where(x => string.Equals(x.Category, category, StringComparison.Ordinal));
    }

    /// <summary>Возвращает уведомления с указанным каналом.</summary>
    public List<NotificationItem> GetByChannel(string channel)
    {
        if (string.IsNullOrEmpty(channel)) return GetActiveItems();
        return Where(x => string.Equals(x.Channel, channel, StringComparison.Ordinal));
    }

    /// <summary>Возвращает набор уникальных категорий среди всех уведомлений.</summary>
    public IReadOnlyList<string> GetCategories()
    {
        lock (_gate)
        {
            return _items.Where(x => !string.IsNullOrEmpty(x.Category))
                         .Select(x => x.Category!)
                         .Distinct(StringComparer.Ordinal)
                         .ToImmutableArray();
        }
    }

    /// <summary>Возвращает набор уникальных каналов среди всех уведомлений.</summary>
    public IReadOnlyList<string> GetChannels()
    {
        lock (_gate)
        {
            return _items.Where(x => !string.IsNullOrEmpty(x.Channel))
                         .Select(x => x.Channel!)
                         .Distinct(StringComparer.Ordinal)
                         .ToImmutableArray();
        }
    }

    // ── Persistence ────────────────────────────────────────────────────────

    private async Task RestoreFromStorageAsync()
    {
        if (_storage is null) return;
        try
        {
            var items = await _storage.GetAsync<List<NotificationItem>>(PersistenceKey).ConfigureAwait(false);
            if (items is null || items.Count == 0) return;
            lock (_gate)
            {
                foreach (var item in items)
                {
                    // Skip items that already expired (older than 30 days) and were read.
                    if (item.IsRead && item.Timestamp.HasValue
                        && (DateTimeOffset.Now - item.Timestamp.Value).TotalDays > 30) continue;
                    _items.Add(item);
                }
            }
            RaiseChanged();
        }
        catch
        {
            // Corrupted storage — ignore and start fresh.
        }
    }

    private async Task PersistAsync()
    {
        if (!_persistenceEnabled || _storage is null) return;
        try
        {
            List<NotificationItem> snapshot;
            lock (_gate) { snapshot = _items.ToList(); }
            await _storage.SetAsync(PersistenceKey, snapshot).ConfigureAwait(false);
        }
        catch
        {
            // Best-effort: persistence failure must not break UI.
        }
    }

    /// <summary>Принудительно сохраняет текущий snapshot в localStorage.</summary>
    public Task FlushAsync() => PersistAsync();

    /// <summary>Принудительно загружает snapshot из localStorage, заменяя текущий список.</summary>
    public async Task ReloadAsync()
    {
        await RestoreFromStorageAsync().ConfigureAwait(false);
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

    private void RaiseChanged()
    {
        // Fire-and-forget persistence. If persistence is enabled, save asynchronously.
        if (_persistenceEnabled && _storage is not null)
        {
            _ = PersistAsync();
        }

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
