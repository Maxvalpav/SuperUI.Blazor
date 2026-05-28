using Microsoft.AspNetCore.Components;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// A notification panel with filtering, grouping, read/dismiss actions, priority, pinning, and avatar support.
/// </summary>
public partial class SgNotificationPanel
{
    private string _filter = "all";
    private List<NotificationItem> _filteredItems = new();
    private int _unreadCount;
    private int _allCount;

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

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    protected override void OnParametersSet()
    {
        RebuildFilteredList();
    }

    private void SetFilter(string filter)
    {
        _filter = filter;
        RebuildFilteredList();
    }

    private void RebuildFilteredList()
    {
        _unreadCount = Items.Count(x => !x.IsRead);
        _allCount = Items.Count();

        var query = _filter == "unread"
            ? Items.Where(x => !x.IsRead)
            : Items;

        _filteredItems = query
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => (int)x.Priority)
            .ThenByDescending(x => x.Timestamp ?? DateTimeOffset.MinValue)
            .ToList();
    }

    private bool ShowGroup(string key, bool isPinned)
    {
        if (!ShowPinnedSeparately) return true;
        if (isPinned) return _filteredItems.Any(x => x.IsPinned);
        return _filteredItems.Any(x => !x.IsPinned);
    }

    private async Task HandleItemClickAsync(NotificationItem item)
    {
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

    private async Task HandleActionClickAsync(SgNotificationAction action)
    {
        if (action.OnClick is not null)
            await action.OnClick();
        if (OnActionClick.HasDelegate)
            await OnActionClick.InvokeAsync(action);
    }

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
}
