using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// A full-featured notification panel with filtering, grouping, priority, pinning,
/// avatars, action buttons, bulk operations, search, collapsible groups, importance
/// threshold, channel indicators, snooze, and swipe-to-dismiss.
/// </summary>
public partial class SgNotificationPanel : IDisposable
{
    private string _filter = "all";
    private string _searchText = "";
    private List<NotificationItem> _filteredItems = new();
    private int _unreadCount;
    private int _allCount;
    private HashSet<string> _selectedIds = new();
    private HashSet<string> _collapsedGroups = new();
    private string? _snoozingItemId;
    private bool _showBulkBar;
    private int _lowPriorityHiddenCount;
    private CancellationTokenSource? _searchCts;

    // ── Core parameters ───────────────────────────────────────────────────

    /// <summary>Panel header title.</summary>
    [Parameter] public string Title { get; set; } = "Notifications";

    /// <summary>Collection of notifications to render.</summary>
    [Parameter] public IEnumerable<NotificationItem> Items { get; set; } = Array.Empty<NotificationItem>();

    /// <summary>Empty-state text when there are no notifications.</summary>
    [Parameter] public string EmptyText { get; set; } = "No notifications";

    /// <summary>Label for the "All" filter tab.</summary>
    [Parameter] public string AllText { get; set; } = "All";

    /// <summary>Label for the "Unread" filter tab.</summary>
    [Parameter] public string UnreadText { get; set; } = "Unread";

    /// <summary>Mark all as read action text.</summary>
    [Parameter] public string MarkAllReadText { get; set; } = "Mark all read";

    /// <summary>Clear all action text.</summary>
    [Parameter] public string ClearAllText { get; set; } = "Clear all";

    /// <summary>Tooltip for the mark-read button.</summary>
    [Parameter] public string MarkReadText { get; set; } = "Mark as read";

    /// <summary>Tooltip for the mark-unread button.</summary>
    [Parameter] public string MarkUnreadText { get; set; } = "Mark as unread";

    /// <summary>Tooltip for the dismiss button.</summary>
    [Parameter] public string DismissText { get; set; } = "Dismiss";

    /// <summary>Heading text for pinned items group.</summary>
    [Parameter] public string PinnedText { get; set; } = "Pinned";

    /// <summary>Heading text for other items when pinned group is shown.</summary>
    [Parameter] public string OtherText { get; set; } = "Others";

    // ── Feature toggles ───────────────────────────────────────────────────

    /// <summary>Whether the all/unread filter tabs are shown.</summary>
    [Parameter] public bool ShowFilter { get; set; } = true;

    /// <summary>Whether built-in "mark all read" / "clear all" actions are shown.</summary>
    [Parameter] public bool ShowDefaultActions { get; set; } = true;

    /// <summary>Whether the per-item dismiss button is shown.</summary>
    [Parameter] public bool AllowDismiss { get; set; } = true;

    /// <summary>Whether "clear all" action is allowed.</summary>
    [Parameter] public bool AllowClear { get; set; } = true;

    /// <summary>Whether to show the panel header.</summary>
    [Parameter] public bool ShowHeader { get; set; } = true;

    /// <summary>Optional max-height of the scrollable list (e.g. "420px").</summary>
    [Parameter] public string? MaxHeight { get; set; } = "420px";

    /// <summary>Grouping selector. When set, items are grouped under the returned key.</summary>
    [Parameter] public Func<NotificationItem, string>? GroupBy { get; set; }

    /// <summary>Whether pinned items appear in a separate group at the top.</summary>
    [Parameter] public bool ShowPinnedSeparately { get; set; } = true;

    /// <summary>Custom header action content. Replaces built-in actions when supplied.</summary>
    [Parameter] public RenderFragment? ActionsContent { get; set; }

    /// <summary>Custom footer content (e.g. "View all" link).</summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }

    // ── New feature parameters ────────────────────────────────────────────

    /// <summary>Enables checkboxes for multi-select and a bulk action toolbar.</summary>
    [Parameter] public bool AllowBulkActions { get; set; }

    /// <summary>Shows a search input that filters items by title/message.</summary>
    [Parameter] public bool AllowSearch { get; set; }

    /// <summary>Makes group headers clickable to expand/collapse their items.</summary>
    [Parameter] public bool CollapsibleGroups { get; set; }

    /// <summary>Items at or below this priority are collapsed under a "Show N more" link. Default is <see cref="SgNotificationPriority.Default"/> (no collapsing).</summary>
    [Parameter] public SgNotificationPriority ImportanceThreshold { get; set; } = SgNotificationPriority.Default;

    /// <summary>Shows a snooze button on each item with preset durations.</summary>
    [Parameter] public bool AllowSnooze { get; set; }

    /// <summary>Enables touch-swipe-to-dismiss on items.</summary>
    [Parameter] public bool AllowSwipeDismiss { get; set; }

    /// <summary>Shows channel badge on items that have a <see cref="NotificationItem.Channel"/>.</summary>
    [Parameter] public bool ShowChannel { get; set; }

    // ── Localization strings ──────────────────────────────────────────────

    /// <summary>Search input placeholder.</summary>
    [Parameter] public string SearchPlaceholder { get; set; } = "Search notifications...";

    /// <summary>Text for the select-all checkbox.</summary>
    [Parameter] public string SelectAllText { get; set; } = "Select all";

    /// <summary>Text showing selected count (e.g. "{0} selected").</summary>
    [Parameter] public string SelectedCountText { get; set; } = "{0} selected";

    /// <summary>Tooltip for the snooze button.</summary>
    [Parameter] public string SnoozeText { get; set; } = "Snooze";

    /// <summary>Snooze preset: 1 hour.</summary>
    [Parameter] public string Snooze1hText { get; set; } = "1 hour";

    /// <summary>Snooze preset: 3 hours.</summary>
    [Parameter] public string Snooze3hText { get; set; } = "3 hours";

    /// <summary>Snooze preset: tomorrow.</summary>
    [Parameter] public string SnoozeTomorrowText { get; set; } = "Tomorrow";

    /// <summary>Snooze preset: next week.</summary>
    [Parameter] public string SnoozeWeekText { get; set; } = "Next week";

    /// <summary>Text for the "Show N more" link when low-priority items are hidden.</summary>
    [Parameter] public string ShowMoreText { get; set; } = "Show {0} more";

    /// <summary>Channel filter label.</summary>
    [Parameter] public string ChannelText { get; set; } = "Channel";

    /// <summary>No results text when search yields no matches.</summary>
    [Parameter] public string NoResultsText { get; set; } = "No results found";

    /// <summary>Bulk mark-read button text.</summary>
    [Parameter] public string BulkMarkReadText { get; set; } = "Mark read";

    /// <summary>Bulk dismiss button text.</summary>
    [Parameter] public string BulkDismissText { get; set; } = "Dismiss";

    /// <summary>Bulk mark-unread button text.</summary>
    [Parameter] public string BulkMarkUnreadText { get; set; } = "Mark unread";

    /// <summary>Text for the clear-read button.</summary>
    [Parameter] public string ClearReadText { get; set; } = "Clear read";

    /// <summary>Unsnooze button text.</summary>
    [Parameter] public string UnsnoozeText { get; set; } = "Unsnooze";

    // ── Events ────────────────────────────────────────────────────────────

    /// <summary>Raised when an item is clicked.</summary>
    [Parameter] public EventCallback<NotificationItem> OnItemClick { get; set; }

    /// <summary>Raised when read state is toggled.</summary>
    [Parameter] public EventCallback<NotificationItem> OnReadToggle { get; set; }

    /// <summary>Raised when an item is dismissed.</summary>
    [Parameter] public EventCallback<NotificationItem> OnDismiss { get; set; }

    /// <summary>Raised when "mark all as read" is requested.</summary>
    [Parameter] public EventCallback OnMarkAllRead { get; set; }

    /// <summary>Raised when "clear all" is requested.</summary>
    [Parameter] public EventCallback OnClearAll { get; set; }

    /// <summary>Raised when an action button inside a notification item is clicked.</summary>
    [Parameter] public EventCallback<SgNotificationAction> OnActionClick { get; set; }

    /// <summary>Raised when bulk mark-read is triggered.</summary>
    [Parameter] public EventCallback<IEnumerable<string>> OnBulkMarkRead { get; set; }

    /// <summary>Raised when bulk dismiss is triggered.</summary>
    [Parameter] public EventCallback<IEnumerable<string>> OnBulkDismiss { get; set; }

    /// <summary>Raised when bulk mark-unread is triggered.</summary>
    [Parameter] public EventCallback<IEnumerable<string>> OnBulkMarkUnread { get; set; }

    /// <summary>Raised when an item is snoozed (id + until).</summary>
    [Parameter] public EventCallback<(string Id, DateTimeOffset Until)> OnSnooze { get; set; }

    /// <summary>Raised when search text changes.</summary>
    [Parameter] public EventCallback<string> OnSearch { get; set; }

    /// <summary>Raised when selection changes.</summary>
    [Parameter] public EventCallback<HashSet<string>> OnSelectionChanged { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    // ── Lifecycle ─────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        RebuildFilteredList();
    }

    // ── Filter & search ───────────────────────────────────────────────────

    private void SetFilter(string filter)
    {
        _filter = filter;
        RebuildFilteredList();
    }

    private async Task OnSearchInputAsync(ChangeEventArgs e)
    {
        _searchText = (e.Value as string) ?? "";
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        try
        {
            await Task.Delay(250, token);
            if (!token.IsCancellationRequested)
            {
                RebuildFilteredList();
                if (OnSearch.HasDelegate)
                    await OnSearch.InvokeAsync(_searchText);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void ClearSearch()
    {
        _searchText = "";
        RebuildFilteredList();
        if (OnSearch.HasDelegate)
            OnSearch.InvokeAsync(_searchText);
    }

    private bool MatchesSearch(NotificationItem item)
    {
        if (string.IsNullOrWhiteSpace(_searchText)) return true;
        var q = _searchText.AsSpan();
        return (item.Title?.AsSpan().Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Message?.AsSpan().Contains(q, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    // ── Data rebuilding ──────────────────────────────────────────────────

    private void RebuildFilteredList()
    {
        _unreadCount = Items.Count(x => !x.IsRead);
        _allCount = Items.Count();

        var query = _filter == "unread"
            ? Items.Where(x => !x.IsRead)
            : Items;

        if (!string.IsNullOrWhiteSpace(_searchText))
            query = query.Where(MatchesSearch);

        query = query
            .Where(x => !x.IsSnoozed)
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => (int)x.Priority)
            .ThenByDescending(x => x.Timestamp ?? DateTimeOffset.MinValue);

        var all = query.ToList();

        // Importance threshold: hide low-priority items
        if ((int)ImportanceThreshold > 0)
        {
            var threshold = (int)ImportanceThreshold;
            var lowPriority = all.Where(x => (int)x.Priority <= threshold).ToList();
            _lowPriorityHiddenCount = lowPriority.Count;
            _filteredItems = all.Where(x => (int)x.Priority > threshold).ToList();
        }
        else
        {
            _lowPriorityHiddenCount = 0;
            _filteredItems = all;
        }

        _selectedIds.IntersectWith(_filteredItems.Select(x => x.Id));
        _showBulkBar = AllowBulkActions && _selectedIds.Count > 0;
    }

    private void RevealLowPriority()
    {
        _lowPriorityHiddenCount = 0;
        RebuildFilteredList();
    }

    // ── Selection ──────────────────────────────────────────────────────────

    private bool IsSelected(NotificationItem item) => _selectedIds.Contains(item.Id);

    private void ToggleSelection(NotificationItem item)
    {
        if (!_selectedIds.Remove(item.Id))
            _selectedIds.Add(item.Id);
        _showBulkBar = _selectedIds.Count > 0;
        if (OnSelectionChanged.HasDelegate)
            OnSelectionChanged.InvokeAsync(_selectedIds);
    }

    private bool AllSelected => _filteredItems.Count > 0 && _selectedIds.Count == _filteredItems.Count;

    private void ToggleSelectAll()
    {
        if (AllSelected)
            _selectedIds.Clear();
        else
            _selectedIds = new HashSet<string>(_filteredItems.Select(x => x.Id));
        _showBulkBar = _selectedIds.Count > 0;
        if (OnSelectionChanged.HasDelegate)
            OnSelectionChanged.InvokeAsync(_selectedIds);
    }

    // ── Collapsible groups ────────────────────────────────────────────────

    private bool IsGroupCollapsed(string key) => _collapsedGroups.Contains(key);

    private void ToggleGroup(string key)
    {
        if (!_collapsedGroups.Remove(key))
            _collapsedGroups.Add(key);
    }

    // ── Item interaction ──────────────────────────────────────────────────

    private async Task HandleItemClickAsync(NotificationItem item)
    {
        if (AllowBulkActions)
        {
            ToggleSelection(item);
            return;
        }
        if (OnItemClick.HasDelegate)
            await OnItemClick.InvokeAsync(item);
    }

    private async Task HandleReadToggleAsync(NotificationItem item)
    {
        item.IsRead = !item.IsRead;
        if (OnReadToggle.HasDelegate)
            await OnReadToggle.InvokeAsync(item);
    }

    private async Task HandleDismissAsync(NotificationItem item)
    {
        if (OnDismiss.HasDelegate)
            await OnDismiss.InvokeAsync(item);
    }

    private async Task MarkAllAsReadInternalAsync()
    {
        foreach (var item in Items)
            item.IsRead = true;
        if (OnMarkAllRead.HasDelegate)
            await OnMarkAllRead.InvokeAsync();
    }

    private async Task ClearAllInternalAsync()
    {
        if (OnClearAll.HasDelegate)
            await OnClearAll.InvokeAsync();
    }

    private async Task ClearReadInternalAsync()
    {
        if (OnBulkDismiss.HasDelegate)
            await OnBulkDismiss.InvokeAsync(Items.Where(x => x.IsRead).Select(x => x.Id));
    }

    private async Task HandleActionClickAsync(SgNotificationAction action)
    {
        if (action.OnClick is not null)
            await action.OnClick();
        if (OnActionClick.HasDelegate)
            await OnActionClick.InvokeAsync(action);
    }

    // ── Bulk actions ─────────────────────────────────────────────────────

    private async Task HandleBulkMarkReadAsync()
    {
        if (OnBulkMarkRead.HasDelegate)
            await OnBulkMarkRead.InvokeAsync(_selectedIds);
        _selectedIds.Clear();
        _showBulkBar = false;
    }

    private async Task HandleBulkMarkUnreadAsync()
    {
        if (OnBulkMarkUnread.HasDelegate)
            await OnBulkMarkUnread.InvokeAsync(_selectedIds);
        _selectedIds.Clear();
        _showBulkBar = false;
    }

    private async Task HandleBulkDismissAsync()
    {
        if (OnBulkDismiss.HasDelegate)
            await OnBulkDismiss.InvokeAsync(_selectedIds);
        _selectedIds.Clear();
        _showBulkBar = false;
    }

    // ── Snooze ────────────────────────────────────────────────────────────

    private void ShowSnoozePicker(string itemId) => _snoozingItemId = _snoozingItemId == itemId ? null : itemId;

    private async Task SnoozeAsync(string itemId, int hours)
    {
        _snoozingItemId = null;
        var until = DateTimeOffset.Now.AddHours(hours);
        if (OnSnooze.HasDelegate)
            await OnSnooze.InvokeAsync((itemId, until));
    }

    private async Task SnoozeUntilAsync(string itemId, DateTimeOffset until)
    {
        _snoozingItemId = null;
        if (OnSnooze.HasDelegate)
            await OnSnooze.InvokeAsync((itemId, until));
    }

    // ── Swipe (touch) ─────────────────────────────────────────────────────

    // Per-item swipe origin (start X). The current delta is derived against this on every
    // move — we must NOT overwrite the origin with the delta, or each move measures relative
    // to the previous delta and the dismiss threshold never triggers correctly.
    private readonly Dictionary<string, double> _swipeStartX = new();
    private readonly Dictionary<string, double> _swipeDx = new();

    private void OnTouchStart(NotificationItem item, TouchEventArgs e)
    {
        if (!AllowSwipeDismiss || e.Touches.Length == 0) return;
        _swipeStartX[item.Id] = e.Touches[0].ClientX;
        _swipeDx[item.Id] = 0;
    }

    private void OnTouchMove(NotificationItem item, TouchEventArgs e, ElementReference el)
    {
        if (!AllowSwipeDismiss || e.Touches.Length == 0 || !_swipeStartX.TryGetValue(item.Id, out var startX)) return;
        _swipeDx[item.Id] = e.Touches[0].ClientX - startX;
    }

    private async Task OnTouchEndAsync(NotificationItem item, TouchEventArgs e)
    {
        if (!AllowSwipeDismiss) return;
        _swipeStartX.Remove(item.Id);
        if (_swipeDx.Remove(item.Id, out var dx) && dx < -80)
            await HandleDismissAsync(item);
    }

    // ── CSS helpers ────────────────────────────────────────────────────────

    private static string VariantClass(SgBadgeVariant variant) => variant switch
    {
        SgBadgeVariant.Info => "sgc-info",
        SgBadgeVariant.Success => "sgc-success",
        SgBadgeVariant.Warn => "sgc-warn",
        SgBadgeVariant.Danger => "sgc-danger",
        _ => ""
    };

    private static string PriorityClass(SgNotificationPriority priority) => priority switch
    {
        SgNotificationPriority.Urgent => "sgc-priority-urgent",
        SgNotificationPriority.High => "sgc-priority-high",
        SgNotificationPriority.Low => "sgc-priority-low",
        _ => ""
    };

    private IEnumerable<NotificationItem> ApplyThreshold(IEnumerable<NotificationItem> items)
    {
        if ((int)ImportanceThreshold == 0 || _lowPriorityHiddenCount == 0)
            return items;
        var threshold = (int)ImportanceThreshold;
        return items.Where(x => (int)x.Priority > threshold);
    }

    // ── Disposal ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Dispose()
    {
        // The search debounce CTS is replaced on every keystroke; the last one must be
        // cancelled + disposed on teardown or it leaks a timer registration.
        _searchCts?.Cancel();
        _searchCts?.Dispose();
    }
}
