using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Localization;
using SuperUI.Base.Utilities;
using System.Collections;
using SuperUI.Enums;
using SortDirection = SuperUI.Enums.SgDataGridSortDirection;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SuperUI.Components;

public partial class SgDataGrid<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private TItem? _detailItem;
    private bool _detailDrawerVisible;
    private bool _detailWindowVisible;

    // Tree mode
    private readonly HashSet<TItem> _expandedTreeNodes = new();

    internal string EffectiveDetailDrawerTitle => string.IsNullOrWhiteSpace(DetailDrawerTitle) ? Localizer["DataGrid_DetailDrawerTitle"] : DetailDrawerTitle!;
    internal string EffectiveDetailWindowTitle => string.IsNullOrWhiteSpace(DetailWindowTitle) ? Localizer["DataGrid_DetailWindowTitle"] : DetailWindowTitle!;

    // ── Detail ──────────────────────────────────────────────────────────────────

    internal IReadOnlyList<AutoDetailProperty> GetAutoDetailProperties()
    {
        var filter = AutoDetailFields?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return typeof(TItem)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p => filter is null || filter.Contains(p.Name))
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
            .Select(p =>
            {
                var t = p.PropertyType;
                var display = p.GetCustomAttribute<DisplayAttribute>();
                var label = display?.GetName() ?? p.Name;

                var collectionItemType = GetCollectionItemType(t);
                if (collectionItemType is not null)
                    return new AutoDetailProperty(p, label, AutoDetailKind.Collection, collectionItemType);

                var underlying = Nullable.GetUnderlyingType(t) ?? t;
                if (underlying.IsClass && underlying != typeof(string) && !underlying.IsPrimitive)
                    return new AutoDetailProperty(p, label, AutoDetailKind.Object, underlying);

                return null;
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
    }

    private static Type? GetCollectionItemType(Type t)
    {
        if (t == typeof(string)) return null;
        foreach (var iface in t.GetInterfaces().Prepend(t))
        {
            if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                var itemType = iface.GetGenericArguments()[0];
                if (itemType.IsClass && itemType != typeof(string))
                    return itemType;
            }
        }
        return null;
    }

    // ── Tree mode ───────────────────────────────────────────────────────────────

    internal bool IsTreeNodeExpanded(TItem item) => 
        ExpandedItems.Contains(item) || _expandedTreeNodes.Contains(item);

    internal bool IsLastChild(TItem item)
    {
        if (Items == null || TreeChildren == null) return false;
        return false; 
    }

    internal async Task ToggleTreeNodeExpandedAsync(TItem item)
    {
        if (ExpandedItems.Contains(item))
        {
            ExpandedItems.Remove(item);
        }
        else if (_expandedTreeNodes.Contains(item))
        {
            _expandedTreeNodes.Remove(item);
        }
        else
        {
            _expandedTreeNodes.Add(item);
        }

        if (ExpandedItemsChanged.HasDelegate)
            await ExpandedItemsChanged.InvokeAsync(ExpandedItems);

        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ExpandAllTreeNodesAsync()
    {
        if (Items == null || TreeChildren == null) return;

        void ExpandRecursive(TItem item)
        {
            var children = TreeChildren.Invoke(item);
            if (children != null && children.Any())
            {
                if (!ExpandedItems.Contains(item) && !_expandedTreeNodes.Contains(item))
                {
                    _expandedTreeNodes.Add(item);
                }

                foreach (var child in children)
                {
                    ExpandRecursive(child);
                }
            }
        }

        foreach (var item in Items)
        {
            ExpandRecursive(item);
        }

        if (ExpandedItemsChanged.HasDelegate)
            await ExpandedItemsChanged.InvokeAsync(ExpandedItems);

        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    public async Task CollapseAllTreeNodesAsync()
    {
        ExpandedItems.Clear();
        _expandedTreeNodes.Clear();

        if (ExpandedItemsChanged.HasDelegate)
            await ExpandedItemsChanged.InvokeAsync(ExpandedItems);

        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }
}
