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
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ColumnFilter> _conditionFilters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _quickFilters = new(StringComparer.Ordinal);
    private int _quickFilterUiVersion;
    private int _lastRenderedQuickFilterUiVersion = -1;

    private int _filterMenuUiVersion;
    private int _lastRenderedFilterMenuUiVersion = -1;
    private int _renderVersion;
    private int _lastRenderedRenderVersion = -1;
    private readonly List<QueryRule> _queryRules = new();
    internal readonly List<PersistedSortRule> _sort = new();
    internal readonly List<string> _groupByKeys = new();
    internal readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);
    private HashSet<string> _pendingSelectedValues = new(StringComparer.Ordinal);
    internal List<FilterRule> _pendingRules = [new()];
    internal bool _pendingRulesAnd = true;
    private string? _pendingFilterKey;
    private SortDirection? _pendingSort;
    private string? _search;
    private string _filterMenuSearchText = string.Empty;

    private List<SgFilterTreeNode>? _filterTree;
    private string? _openFilterColumn;

    private bool _filterMenuLoading;
    private List<string?> _filterMenuAllValues = new();
    private List<string?> _filterMenuVisibleValues = new();
    private int _filterMenuLoadToken;
    private CancellationTokenSource? _filterMenuSearchDebounceCts;
    private const int FilterMenuSearchDebounceMs = 180;
    private CancellationTokenSource? _pendingRuleValueDebounceCts;
    private const int PendingRuleValueDebounceMs = 150;

    private bool _showSortBuilder;
    internal List<PersistedSortRule> _sortBuilderRules = new();
    private bool _showGroupBuilder;
    internal List<string> _groupBuilderKeys = new();
    internal Dictionary<string, Aggregate> _groupBuilderAggregates = new(StringComparer.Ordinal);

    internal int _currentPage = 1;
    internal int _pageSize = 25;
    private int _lastPageSize = -1;

    // Content-based version fields for cache invalidation
    private int _itemsVersion = 0;
    private int _filterVersion = 0;
    private int _sortVersion = 0;
    private int _groupVersion = 0;

    // Cache version tracking fields
    private int _filteredRowsCacheItemsVersion = -1;
    private int _filteredRowsCacheFilterVersion = -1;
    private int _filteredSortedRowsCacheFilterVersion = -1;
    private int _filteredSortedRowsCacheSortVersion = -1;
    private int _visibleRowsCacheItemsVersion = -1;
    private int _visibleRowsCacheFilterVersion = -1;
    private int _visibleRowsCacheSortVersion = -1;
    private int _visibleRowsCacheColumnsVersion = -1;
    private int _orderedColumnsCacheColumnsVersion = -1;
    private int _distinctValuesCacheItemsVersion = -1;
    private int _groupTreeCacheItemsVersion = -1;
    private int _groupTreeCacheGroupVersion = -1;
    private int _aggregateCacheItemsVersion = -1;
    private int _aggregateCacheFilterVersion = -1;

    private readonly Dictionary<string, string> _columnFilterTypeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _numericColumnCache = new(StringComparer.Ordinal);
    private int _columnFilterTypeCacheColumnsVersion = -1;
    private int _columnFilterTypeCacheItemsVersion = -1;

    private readonly Dictionary<string, List<SgEnumItem>> _columnEnumItemsCache = new(StringComparer.Ordinal);
    private int _columnEnumItemsCacheColumnsVersion = -1;
    private int _columnEnumItemsCacheItemsVersion = -1;

    private int _preparedSortsCacheSortVersion = -1;
    private int _preparedSortsCacheColumnsVersion = -1;
    private List<(Func<TItem, object?> Selector, bool Descending)>? _preparedSortsCache;

    private (int items, int filter, int selection, int count)? _allFilteredSelectedCacheKey;
    private bool _allFilteredSelectedCacheValue;
    private int _visibleColumnsCacheVersion = -1;
    private List<TItem>? _filteredRowsCache;
    private List<TItem>? _filteredSortedRowsCache;
    private List<TItem>? _visibleRowsCache;
    private int _columnSpanCacheVersion = -1;
    private int _columnSpanCacheValue;
    private List<SgDataGridColumn<TItem>>? _orderedColumnsCache;
    private List<SgDataGridColumn<TItem>>? _visibleColumnsCache;
    private List<GroupNode>? _groupTreeCache;
    private readonly Dictionary<string, List<string?>> _distinctValuesCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _distinctNormalizedValuesCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, string>> _displayToRawKeyCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _aggregateCache = new(StringComparer.Ordinal);

    // Virtualization state fields
    private int _scrollTop = 0;
    private int _viewportHeight = 0;
    private int _estimatedRowHeight = 40;
    private int _virtualizationBufferRows = 10;
    private const int VirtualizationThreshold = 1000;
    private const int MinColumnWidth = 40;

    // Scroll debounce
    private CancellationTokenSource? _scrollDebounceCts;
    private const int ScrollDebounceMs = 16;

    // Group building progress
    private bool _isGroupBuilding;
    private CancellationTokenSource? _groupBuildCts;

    // Debounce state for text inputs
    private CancellationTokenSource? _searchDebounceCts;
    private readonly Dictionary<string, CancellationTokenSource> _quickFilterDebounceCts = new(StringComparer.Ordinal);
    private const int InputDebounceMs = 250;

    internal readonly Dictionary<TItem, int> _rowLevels = new();

    internal bool HasActiveSort => _sort.Count > 0;
    internal bool HasActiveFilters => !string.IsNullOrWhiteSpace(_search) || _filters.Count > 0 || _conditionFilters.Count > 0 || _quickFilters.Count > 0;
    internal int CurrentPage => _currentPage;
    internal int TotalFilteredCount => GetFilteredRows().Count;
    internal int TotalPages => !EnablePaging ? 1 : Math.Max(1, (int)Math.Ceiling(TotalFilteredCount / (double)Math.Max(1, _pageSize)));
    internal IReadOnlyList<string> GroupByKeys => _groupByKeys;
    internal IReadOnlyList<PersistedSortRule> SortRules => _sort;
    internal List<FilterRule> PendingRules => _pendingRules;
    internal bool PendingRulesAnd => _pendingRulesAnd;

    private async Task HandleFilterMenuSearchInputAsync(string? value)
    {
        _filterMenuSearchText = value ?? string.Empty;

        _filterMenuSearchDebounceCts?.Cancel();
        _filterMenuSearchDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _filterMenuSearchDebounceCts = cts;

        try
        {
            await Task.Delay(FilterMenuSearchDebounceMs, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested || _openFilterColumn is null)
            return;

        ApplyFilterMenuSearch();
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private void ApplyFilterMenuSearch()
    {
        if (_openFilterColumn is null) return;

        var key = _openFilterColumn;
        var filterType = GetColumnFilterType(key);
        var hasSearch = !string.IsNullOrEmpty(_filterMenuSearchText);

        if (filterType == "date" || filterType == "datetime")
        {
            _filterTree = BuildFilterTree(key, _filterMenuAllValues);
            _filterMenuVisibleValues = _filterMenuAllValues;
            return;
        }

        if (!hasSearch)
        {
            _filterMenuVisibleValues = _filterMenuAllValues;
            return;
        }

        var search = _filterMenuSearchText;
        var visible = new List<string?>(Math.Min(_filterMenuAllValues.Count, 256));
        for (int i = 0; i < _filterMenuAllValues.Count; i++)
        {
            var v = _filterMenuAllValues[i];
            var display = GetDisplayLabelForFilterValue(key, v);
            if (display?.Contains(search, StringComparison.OrdinalIgnoreCase) == true)
                visible.Add(v);
        }
        _filterMenuVisibleValues = visible;
    }

    private void OnSearchInput(ChangeEventArgs args)
    {
        _search = args.Value?.ToString();
        _filterVersion++;
        _currentPage = 1;
        StateHasChanged();

        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;

        _ = DebounceApplyAsync(cts.Token, static () => { });
    }

    private void OnQuickFilterInputAsync(string key, ChangeEventArgs args)
    {
        var value = args.Value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            _quickFilters.Remove(key);
        else
            _quickFilters[key] = value;

        _quickFilterUiVersion++;
        StateHasChanged();

        if (_quickFilterDebounceCts.TryGetValue(key, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
        }
        var cts = new CancellationTokenSource();
        _quickFilterDebounceCts[key] = cts;

        _ = DebounceApplyAsync(cts.Token, () =>
        {
            _filterVersion++;
            _currentPage = 1;
            InvalidateComputedRowsCache();
            StateHasChanged();
        });
    }

    private async Task DebounceApplyAsync(CancellationToken ct, Action applyState)
    {
        try
        {
            try
            {
                await Task.Delay(InputDebounceMs, ct);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (_disposing || ct.IsCancellationRequested)
                return;

            applyState();
            try
            {
                await SaveStateAsync();
            }
            catch (Exception ex) when (ex is JSException or TaskCanceledException or ObjectDisposedException)
            {
            }

            if (_disposing || ct.IsCancellationRequested)
                return;

            await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[SgDataGrid.DebounceApplyAsync] {ex}");
        }
    }

    private async Task ToggleFilterMenuAsync(string key)
    {
        if (_openFilterColumn == key)
        {
            _openFilterColumn = null;
            _filterMenuLoading = false;
            _filterMenuAllValues = new List<string?>();
            _filterMenuVisibleValues = new List<string?>();
            _filterTree = null;
            _filterMenuUiVersion++;
            await InvokeAsync(StateHasChanged);
            return;
        }

        _openFilterColumn = key;
        _pendingFilterKey = key;
        _filterMenuSearchText = string.Empty;
        _filterMenuLoading = true;
        _filterMenuAllValues = new List<string?>();
        _filterMenuVisibleValues = new List<string?>();
        _filterTree = null;

        if (_conditionFilters.TryGetValue(key, out var condition))
        {
            _pendingRules = condition.Rules.Select(x => x with { }).ToList();
            _pendingRulesAnd = condition.And;
        }
        else
        {
            _pendingRules = [new()];
            _pendingRulesAnd = true;
        }
        _pendingSort = GetSort(key);

        if (_filters.TryGetValue(key, out var current))
            _pendingSelectedValues = current.ToHashSet(StringComparer.Ordinal);
        else
            _pendingSelectedValues = new HashSet<string>(StringComparer.Ordinal);

        var token = ++_filterMenuLoadToken;

        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);

        List<string?> narrowedValues;
        List<SgFilterTreeNode>? tree = null;
        var filterType = GetColumnFilterType(key);
        try
        {
            narrowedValues = await Task.Run(() =>
            {
                var narrowedItems = GetFilteredRowsExcept(key);
                return GetDistinctValuesForColumn(key, narrowedItems);
            });
        }
        catch
        {
            narrowedValues = new List<string?>();
        }

        if (token != _filterMenuLoadToken || _openFilterColumn != key)
            return;

        try
        {
            if (filterType == "date" || filterType == "datetime")
                tree = BuildFilterTree(key, narrowedValues);
        }
        catch (ObjectDisposedException) { tree = null; }
        catch (InvalidOperationException) { tree = null; }

        _filterMenuAllValues = narrowedValues;
        _filterMenuVisibleValues = narrowedValues;
        _filterTree = tree;

        if (!_filters.ContainsKey(key))
        {
            var narrowedNormalized = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < narrowedValues.Count; i++)
                narrowedNormalized.Add(NormalizeFilterValue(narrowedValues[i]));
            _pendingSelectedValues = narrowedNormalized;
        }

        _filterMenuLoading = false;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private List<SgFilterTreeNode>? BuildFilterTree(string key, List<string?> rawValues)
    {
        var filterType = GetColumnFilterType(key);
        if (filterType != "date" && filterType != "datetime") return null;

        var root = new List<SgFilterTreeNode>();
        var years = new Dictionary<int, SgFilterTreeNode>();
        var hasSearch = !string.IsNullOrWhiteSpace(_filterMenuSearchText);
        var currentFilter = _filters.TryGetValue(key, out var f) ? f : new HashSet<string>();

        foreach (var raw in rawValues)
        {
            if (raw == null) continue;
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                if (hasSearch)
                {
                    var display = dt.ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.CurrentCulture);
                    if (!display.Contains(_filterMenuSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                if (!years.TryGetValue(dt.Year, out var yearNode))
                {
                    yearNode = new SgFilterTreeNode { Label = dt.Year.ToString(), Year = dt.Year, Children = new List<SgFilterTreeNode>(), IsExpanded = false };
                    years[dt.Year] = yearNode;
                    root.Add(yearNode);
                }

                var monthName = dt.ToString("MMMM", CultureInfo.CurrentCulture);
                var monthNode = yearNode.Children!.FirstOrDefault(m => m.Month == dt.Month);
                if (monthNode == null)
                {
                    monthNode = new SgFilterTreeNode { Label = char.ToUpper(monthName[0]) + monthName.Substring(1), Month = dt.Month, Children = new List<SgFilterTreeNode>(), Year = dt.Year, IsExpanded = false };
                    yearNode.Children!.Add(monthNode);
                }

                var isSelected = currentFilter.Contains(NormalizeFilterValue(raw));
                if (isSelected)
                {
                    yearNode.IsExpanded = true;
                    monthNode.IsExpanded = true;
                }

                monthNode.Children!.Add(new SgFilterTreeNode 
                { 
                    Label = dt.Day.ToString("D2"), 
                    Value = raw, 
                    Day = dt.Day,
                    Month = dt.Month,
                    Year = dt.Year,
                    IsSelected = isSelected
                });
            }
        }

        root.Sort((a, b) => b.Year!.Value.CompareTo(a.Year!.Value));
        foreach (var y in root)
        {
            y.Children!.Sort((a, b) => a.Month!.Value.CompareTo(b.Month!.Value));
            foreach (var m in y.Children)
            {
                m.Children!.Sort((a, b) => a.Day!.Value.CompareTo(b.Day!.Value));
            }
        }

        SyncFilterTreeSelectionState(root);
        return root;
    }

    private void SyncFilterTreeSelectionState(List<SgFilterTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.Children != null && node.Children.Count > 0)
            {
                SyncFilterTreeSelectionState(node.Children);
                var allChecked = node.Children.All(c => c.IsSelected == true);
                var allUnchecked = node.Children.All(c => c.IsSelected == false);
                
                if (allChecked) node.IsSelected = true;
                else if (allUnchecked) node.IsSelected = false;
                else node.IsSelected = null;
            }
            else
            {
                node.IsSelected = _pendingSelectedValues.Contains(NormalizeFilterValue(node.Value));
            }
        }
    }

    private void ToggleFilterTreeNode(SgFilterTreeNode node, bool isChecked)
    {
        node.IsSelected = isChecked;
        SetChildrenSelection(node, isChecked);

        UpdatePendingFromTree(_filterTree);

        if (_filterTree != null) SyncFilterTreeSelectionState(_filterTree);

        _filterMenuUiVersion++;
    }

    private void SetChildrenSelection(SgFilterTreeNode node, bool isChecked)
    {
        if (node.Children == null) return;
        foreach (var child in node.Children)
        {
            child.IsSelected = isChecked;
            SetChildrenSelection(child, isChecked);
        }
    }

    private void UpdatePendingFromTree(List<SgFilterTreeNode>? nodes)
    {
        if (nodes == null) return;
        foreach (var node in nodes)
        {
            if (node.Children != null && node.Children.Count > 0)
            {
                UpdatePendingFromTree(node.Children);
            }
            else if (node.Value != null)
            {
                var norm = NormalizeFilterValue(node.Value);
                if (node.IsSelected == true) _pendingSelectedValues.Add(norm);
                else _pendingSelectedValues.Remove(norm);
            }
        }
    }

    private async Task CloseFilterMenuAsync()
    {
        _openFilterColumn = null;
        _filterMenuLoading = false;
        _filterMenuAllValues = new List<string?>();
        _filterMenuVisibleValues = new List<string?>();
        _filterTree = null;
        _filterMenuLoadToken++;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetPendingSortAsync(SortDirection dir)
    {
        _pendingSort = dir;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearPendingSortAsync()
    {
        _pendingSort = null;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetPendingRulesAndAsync(bool and)
    {
        _pendingRulesAnd = and;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetPendingRuleConditionAsync(int index, FilterCondition condition)
    {
        if (index < 0 || index >= _pendingRules.Count)
            return;

        _pendingRules[index] = _pendingRules[index] with { Condition = condition };
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task SetPendingRuleValueAsync(int index, string? value)
    {
        if (index < 0 || index >= _pendingRules.Count)
            return;

        _pendingRules[index] = _pendingRules[index] with { Value = value };

        _pendingRuleValueDebounceCts?.Cancel();
        _pendingRuleValueDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _pendingRuleValueDebounceCts = cts;

        try
        {
            await Task.Delay(PendingRuleValueDebounceMs, cts.Token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (cts.IsCancellationRequested) return;

        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task RemovePendingRuleAsync(int index)
    {
        if (_pendingRules.Count <= 1 || index < 0 || index >= _pendingRules.Count)
            return;

        _pendingRules.RemoveAt(index);
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task AddPendingRuleAsync()
    {
        _pendingRules.Add(new FilterRule());
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ApplyConditionFilterAsync(string key)
    {
        ApplyPendingConditionFilter(key);
        _filterVersion++;
        _currentPage = 1;
        await InvokeAsync(StateHasChanged);
    }

    private void ApplyPendingConditionFilter(string key)
    {
        var validRules = _pendingRules
            .Where(rule => rule.Condition is FilterCondition.IsEmpty or FilterCondition.IsNotEmpty || !string.IsNullOrWhiteSpace(rule.Value))
            .Select(rule => rule with { })
            .ToList();

        if (validRules.Count == 0)
            _conditionFilters.Remove(key);
        else
            _conditionFilters[key] = new ColumnFilter(validRules, _pendingRulesAnd);
    }

    private bool IsPendingAllSelected(string key)
    {
        if (_openFilterColumn == key && !_filterMenuLoading)
        {
            var visible = _filterMenuVisibleValues;
            if (visible.Count == 0) return false;
            for (int i = 0; i < visible.Count; i++)
            {
                if (!_pendingSelectedValues.Contains(NormalizeFilterValue(visible[i])))
                    return false;
            }
            return true;
        }

        var distinct = GetDistinctNormalizedValuesForColumn(key);
        return distinct.SetEquals(_pendingSelectedValues);
    }

    private async Task TogglePendingAllAsync(string key, bool selected)
    {
        if (selected)
        {
            if (_openFilterColumn == key && !_filterMenuLoading)
            {
                var visible = _filterMenuVisibleValues;
                var set = new HashSet<string>(StringComparer.Ordinal);
                for (int i = 0; i < visible.Count; i++)
                    set.Add(NormalizeFilterValue(visible[i]));
                _pendingSelectedValues = set;
            }
            else
            {
                _pendingSelectedValues = GetDistinctNormalizedValuesForColumn(key);
            }
        }
        else
        {
            _pendingSelectedValues = new HashSet<string>(StringComparer.Ordinal);
        }

        if (_filterTree != null) SyncFilterTreeSelectionState(_filterTree);

        await InvokeAsync(StateHasChanged);
    }

    private List<string?> GetDistinctValuesForColumn(string key, IEnumerable<TItem>? customItems = null)
    {
        if (customItems == null && _distinctValuesCacheItemsVersion != _itemsVersion)
        {
            _distinctValuesCache.Clear();
            _distinctNormalizedValuesCache.Clear();
            _displayToRawKeyCache.Clear();
            _distinctValuesCacheItemsVersion = _itemsVersion;
        }

        if (customItems == null && _distinctValuesCache.TryGetValue(key, out var cachedValues))
            return cachedValues;

        var col = GetColumnByKey(key);
        if (col is null)
            return new List<string?>();

        var filterType = GetColumnFilterType(key);
        var useRaw = filterType == "number" || filterType == "date" || filterType == "datetime" || filterType == "enum";

        var displayToRaw = new Dictionary<string, string>(StringComparer.Ordinal);

        var seen = new HashSet<string?>(StringComparer.Ordinal);
        var values = new List<string?>();

        var itemsToIterate = customItems ?? (IsTree && TreeChildren != null ? GetAllTreeItems() : (Items ?? Enumerable.Empty<TItem>()));

        foreach (var item in itemsToIterate)
        {
            string rawKey;
            string displayLabel;

            if (useRaw)
            {
                var raw = col.GetValue(item);
                rawKey = raw switch
                {
                    null => string.Empty,
                    DateTime dt => dt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Enum e => e.ToString(),
                    _ => raw.ToString() ?? string.Empty
                };
                displayLabel = col.GetDisplay(item);
            }
            else
            {
                displayLabel = col.GetDisplay(item);
                rawKey = displayLabel;
            }

            var normalized = string.IsNullOrEmpty(rawKey) ? null : rawKey;
            if (seen.Add(normalized))
            {
                values.Add(normalized);
                if (useRaw && normalized is not null)
                    displayToRaw[normalized] = displayLabel;
            }
        }

        values.Sort((a, b) =>
        {
            if (filterType == "number")
            {
                var aIsNum = decimal.TryParse(a ?? string.Empty,
                    System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var aNum);
                var bIsNum = decimal.TryParse(b ?? string.Empty,
                    System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out var bNum);
                if (aIsNum && bIsNum) return aNum.CompareTo(bNum);
                if (aIsNum) return -1;
                if (bIsNum) return 1;
            }
            return string.Compare(a, b, StringComparison.CurrentCulture);
        });

        _distinctValuesCache[key] = values;
        if (useRaw)
            _displayToRawKeyCache[key] = displayToRaw;

        return values;
    }

    private string GetDisplayLabelForFilterValue(string key, string? rawKey)
    {
        if (rawKey is null) return Localizer["DataGrid_FilterEmpty"];
        if (_displayToRawKeyCache.TryGetValue(key, out var map) &&
            map.TryGetValue(rawKey, out var label))
            return label;
        return rawKey;
    }

    private HashSet<string> GetDistinctNormalizedValuesForColumn(string key)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(_filterMenuSearchText);
        if (_openFilterColumn == key && !_filterMenuLoading)
        {
            var source = hasSearch ? _filterMenuVisibleValues : _filterMenuAllValues;
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < source.Count; i++)
                set.Add(NormalizeFilterValue(source[i]));
            return set;
        }

        var narrowedItems = GetFilteredRowsExcept(key);
        var values = GetDistinctValuesForColumn(key, narrowedItems);
        var normalized = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < values.Count; i++)
        {
            var val = values[i];
            if (hasSearch)
            {
                var display = GetDisplayLabelForFilterValue(key, val);
                if (!(display?.Contains(_filterMenuSearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                    continue;
            }
            normalized.Add(NormalizeFilterValue(val));
        }
        return normalized;
    }

    private bool IsPendingValueSelected(string? value) =>
        _pendingSelectedValues.Contains(NormalizeFilterValue(value));

    private async Task TogglePendingValueAsync(string? value, bool selected)
    {
        var normalized = NormalizeFilterValue(value);
        if (selected)
            _pendingSelectedValues.Add(normalized);
        else
            _pendingSelectedValues.Remove(normalized);

        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearPendingAsync(string key)
    {
        _pendingSelectedValues = GetDistinctNormalizedValuesForColumn(key);
        _pendingRules = [new()];
        _pendingRulesAnd = true;
        _pendingSort = null;
        _filterMenuUiVersion++;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ApplyFilterAsync()
    {
        if (string.IsNullOrWhiteSpace(_pendingFilterKey))
            return;

        var key = _pendingFilterKey;
        var distinctValues = GetDistinctNormalizedValuesForColumn(key);
        if (_pendingSelectedValues.SetEquals(distinctValues))
            _filters.Remove(key);
        else
            _filters[key] = _pendingSelectedValues.ToHashSet(StringComparer.Ordinal);

        ApplyPendingConditionFilter(key);

        if (_pendingSort.HasValue)
        {
            if (_pendingSort.Value == SortDirection.None)
            {
                await RemoveSortAsync(key);
            }
            else
            {
                await SetSortAsync(key, _pendingSort.Value, false);
            }
        }

        _filterVersion++;
        _openFilterColumn = null;
        _currentPage = 1;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearAllFiltersAsync()
    {
        _search = null;
        _quickFilters.Clear();
        _filters.Clear();
        _conditionFilters.Clear();
        _filterVersion++;
        _openFilterColumn = null;
        _currentPage = 1;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnFilterOpenChanged(string key, bool open)
    {
        if (open)
        {
            if (_openFilterColumn != key)
            {
                await ToggleFilterMenuAsync(key);
            }
        }
        else
        {
            if (_openFilterColumn == key)
            {
                _openFilterColumn = null;
                _filterMenuLoading = false;
                _filterMenuAllValues = new List<string?>();
                _filterMenuVisibleValues = new List<string?>();
                _filterTree = null;
                _filterMenuUiVersion++;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private bool HasColumnFilter(string key) =>
        _filters.ContainsKey(key) || _conditionFilters.ContainsKey(key);

    // ── Sorting ─────────────────────────────────────────────────────────────────

    private async Task SetSortAsync(string key, SortDirection dir, bool multi)
    {
        if (!multi)
            _sort.Clear();
        else
            _sort.RemoveAll(x => x.Key == key);

        _sort.Add(new PersistedSortRule { Key = key, Dir = dir });
        _sortVersion++;
        _currentPage = 1;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleSortAsync(string key, bool multi)
    {
        var existing = _sort.FirstOrDefault(x => x.Key == key);
        if (!multi)
            _sort.Clear();
        else if (existing is not null)
            _sort.RemoveAll(x => x.Key == key);

        if (existing is null)
        {
            _sort.Add(new PersistedSortRule { Key = key, Dir = SortDirection.Ascending });
        }
        else if (existing.Dir == SortDirection.Ascending)
        {
            _sort.Add(new PersistedSortRule { Key = key, Dir = SortDirection.Descending });
        }

        _sortVersion++;
        _currentPage = 1;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private SortDirection GetSort(string key) =>
        _sort.FirstOrDefault(x => x.Key == key)?.Dir ?? SortDirection.None;

    private int GetSortIndex(string key)
    {
        for (var i = 0; i < _sort.Count; i++)
        {
            if (_sort[i].Key == key)
                return i + 1;
        }
        return 0;
    }

    private async Task RemoveSortAsync(string key)
    {
        _sort.RemoveAll(x => x.Key == key);
        _sortVersion++;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearSortAsync()
    {
        _sort.Clear();
        _sortVersion++;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Task OpenSortBuilderAsync()
    {
        if (_showSortBuilder)
        {
            _showSortBuilder = false;
            return Task.CompletedTask;
        }
        _sortBuilderRules = _sort.Select(r => new PersistedSortRule { Key = r.Key, Dir = r.Dir }).ToList();
        _showSortBuilder = true;
        _showChooser = false;
        _showExportMenu = false;
        _showGroupBuilder = false;
        _showSavedViewsPanel = false;
        return Task.CompletedTask;
    }

    private async Task HandleSortBuilderFocusOutAsync(FocusEventArgs _)
    {
        await Task.Delay(150);
        _showSortBuilder = false;
    }

    private void SortBuilderAddColumn(string key)
    {
        if (_sortBuilderRules.Any(r => r.Key == key)) return;
        _sortBuilderRules.Add(new PersistedSortRule { Key = key, Dir = SortDirection.Ascending });
    }

    private void SortBuilderRemoveRule(string key)
    {
        _sortBuilderRules.RemoveAll(r => r.Key == key);
    }

    private void SortBuilderToggleDir(string key)
    {
        var rule = _sortBuilderRules.FirstOrDefault(r => r.Key == key);
        if (rule is null) return;
        rule.Dir = rule.Dir == SortDirection.Ascending ? SortDirection.Descending : SortDirection.Ascending;
    }

    private void SortBuilderMoveUp(string key)
    {
        var idx = _sortBuilderRules.FindIndex(r => r.Key == key);
        if (idx <= 0) return;
        (_sortBuilderRules[idx], _sortBuilderRules[idx - 1]) = (_sortBuilderRules[idx - 1], _sortBuilderRules[idx]);
    }

    private void SortBuilderMoveDown(string key)
    {
        var idx = _sortBuilderRules.FindIndex(r => r.Key == key);
        if (idx < 0 || idx >= _sortBuilderRules.Count - 1) return;
        (_sortBuilderRules[idx], _sortBuilderRules[idx + 1]) = (_sortBuilderRules[idx + 1], _sortBuilderRules[idx]);
    }

    private async Task ApplySortBuilderAsync()
    {
        _sort.Clear();
        _sort.AddRange(_sortBuilderRules.Select(r => new PersistedSortRule { Key = r.Key, Dir = r.Dir }));
        _sortVersion++;
        _currentPage = 1;
        _showSortBuilder = false;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetSortBuilderAsync()
    {
        _sortBuilderRules.Clear();
        _sort.Clear();
        _sortVersion++;
        _currentPage = 1;
        _showSortBuilder = false;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    // ── Grouping ─────────────────────────────────────────────────────────────────

    internal void InitGroupBy(string key)
    {
        if (!_groupByKeys.Contains(key))
            _groupByKeys.Add(key);
    }

    private bool IsGroupedBy(string key) => _groupByKeys.Contains(key);

    public async Task ExpandAllGroupsAsync()
    {
        _collapsedGroups.Clear();
        await InvokeAsync(StateHasChanged);
    }

    public async Task CollapseAllGroupsAsync()
    {
        var nodes = GetGroupTree();
        var allPaths = new List<string>();

        void Traverse(List<GroupNode> level)
        {
            foreach (var node in level)
            {
                allPaths.Add(node.Path);
                if (node._children is { Count: > 0 } children)
                    Traverse(children);
            }
        }

        Traverse(nodes);
        foreach (var path in allPaths)
            _collapsedGroups.Add(path);

        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleGroupByAsync(string key)
    {
        if (_groupByKeys.Contains(key))
            _groupByKeys.Remove(key);
        else
            _groupByKeys.Add(key);

        _groupVersion++;
        _collapsedGroups.Clear();
        ScheduleGroupBuild();
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private bool IsGroupCollapsed(string path) => _collapsedGroups.Contains(path);

    private async Task ToggleGroupCollapsedAsync(string path)
    {
        if (!_collapsedGroups.Add(path))
            _collapsedGroups.Remove(path);
        await InvokeAsync(StateHasChanged);
    }

    private List<GroupNode> BuildGroupTree()
    {
        if (_groupTreeCacheItemsVersion == _itemsVersion &&
            _groupTreeCacheGroupVersion == _groupVersion &&
            _groupTreeCache is not null)
            return _groupTreeCache;

        _groupTreeCache = BuildGroupLevel(GetFilteredSortedRows(), 0, string.Empty);
        _groupTreeCacheItemsVersion = _itemsVersion;
        _groupTreeCacheGroupVersion = _groupVersion;
        return _groupTreeCache;
    }

    private List<GroupNode> GetGroupTree() => BuildGroupTree();

    private void ScheduleGroupBuild()
    {
        if (_groupByKeys.Count == 0) return;

        _groupBuildCts?.Cancel();
        _groupBuildCts?.Dispose();
        var cts = new CancellationTokenSource();
        _groupBuildCts = cts;

        _isGroupBuilding = true;
        _groupTreeCacheItemsVersion = -1;
        _groupTreeCacheGroupVersion = -1;

        _ = Task.Run(() =>
        {
            if (cts.IsCancellationRequested) return;
            var tree = BuildGroupLevel(GetFilteredSortedRows(), 0, string.Empty);
            if (cts.IsCancellationRequested) return;

            InvokeAsync(() =>
            {
                if (cts.IsCancellationRequested || _disposing) return;
                _groupTreeCache = tree;
                _groupTreeCacheItemsVersion = _itemsVersion;
                _groupTreeCacheGroupVersion = _groupVersion;
                _isGroupBuilding = false;
                StateHasChanged();
            });
        });
    }

    private List<GroupNode> BuildGroupLevel(List<TItem> rows, int depth, string pathPrefix)
    {
        if (depth >= _groupByKeys.Count || rows.Count == 0)
            return new List<GroupNode>();

        var key = _groupByKeys[depth];
        var column = GetColumnByKey(key);
        if (column is null)
            return new List<GroupNode>();

        var groupedRows = new Dictionary<string, List<TItem>>(rows.Count / 4 + 4, StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var groupKey = column.GetGroupKey(item);
            if (!groupedRows.TryGetValue(groupKey, out var bucket))
            {
                bucket = new List<TItem>();
                groupedRows[groupKey] = bucket;
            }
            bucket.Add(item);
        }

        var sortedKeys = new List<string>(groupedRows.Count);
        sortedKeys.AddRange(groupedRows.Keys);
        sortedKeys.Sort(StringComparer.OrdinalIgnoreCase);

        var nodes = new List<GroupNode>(sortedKeys.Count);
        var leaf = depth == _groupByKeys.Count - 1;
        var emptyLabel = Localizer["DataGrid_FilterEmpty"].ToString() ?? string.Empty;

        for (var i = 0; i < sortedKeys.Count; i++)
        {
            var groupKey = sortedKeys[i];
            var groupRows = groupedRows[groupKey];
            var path = pathPrefix.Length == 0
                ? string.Concat(key, ":", groupKey)
                : string.Concat(pathPrefix, "|", key, ":", groupKey);

            var displayLabel = groupKey.Length == 0
                ? emptyLabel
                : column.GetDisplayFromValue(column.GetValue(groupRows[0])) is { Length: > 0 } dl ? dl : groupKey;

            var node = new GroupNode(path, column, depth, displayLabel, groupRows.Count);

            if (leaf)
                node.SetItems(groupRows);
            else
                node.SetChildren(BuildGroupLevel(groupRows, depth + 1, path));

            nodes.Add(node);
        }

        return nodes;
    }

    private Task OpenGroupBuilderAsync()
    {
        if (_showGroupBuilder)
        {
            _showGroupBuilder = false;
            return Task.CompletedTask;
        }
        _groupBuilderKeys = _groupByKeys.ToList();
        _groupBuilderAggregates.Clear();
        foreach (var col in _columns)
            _groupBuilderAggregates[col.Key] = col.Aggregate;
        _showGroupBuilder = true;
        _showSortBuilder = false;
        _showChooser = false;
        _showExportMenu = false;
        _showSavedViewsPanel = false;
        return Task.CompletedTask;
    }

    private void GroupBuilderAddKey(string key)
    {
        if (!_groupBuilderKeys.Contains(key))
            _groupBuilderKeys.Add(key);
    }

    private void GroupBuilderRemoveKey(string key)
    {
        _groupBuilderKeys.Remove(key);
    }

    private void GroupBuilderMoveUp(string key)
    {
        var idx = _groupBuilderKeys.IndexOf(key);
        if (idx <= 0) return;
        (_groupBuilderKeys[idx], _groupBuilderKeys[idx - 1]) = (_groupBuilderKeys[idx - 1], _groupBuilderKeys[idx]);
    }

    private void GroupBuilderMoveDown(string key)
    {
        var idx = _groupBuilderKeys.IndexOf(key);
        if (idx < 0 || idx >= _groupBuilderKeys.Count - 1) return;
        (_groupBuilderKeys[idx], _groupBuilderKeys[idx + 1]) = (_groupBuilderKeys[idx + 1], _groupBuilderKeys[idx]);
    }

    private void GroupBuilderSetAggregate(string key, Aggregate agg)
    {
        _groupBuilderAggregates[key] = agg;
    }

    private async Task ApplyGroupBuilderAsync()
    {
        _groupByKeys.Clear();
        _groupByKeys.AddRange(_groupBuilderKeys);

        foreach (var col in _columns)
        {
            if (_groupBuilderAggregates.TryGetValue(col.Key, out var agg))
                col.SetAggregate(agg);
        }

        _groupVersion++;
        _collapsedGroups.Clear();
        _aggregateCache.Clear();
        _aggregateCacheItemsVersion = -1;
        _aggregateCacheFilterVersion = -1;
        _showGroupBuilder = false;
        ScheduleGroupBuild();
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ResetGroupBuilderAsync()
    {
        _groupBuilderKeys.Clear();
        _groupByKeys.Clear();
        _groupBuilderAggregates.Clear();
        foreach (var col in _columns)
            col.SetAggregate(Aggregate.None);
        _groupVersion++;
        _collapsedGroups.Clear();
        _aggregateCache.Clear();
        _aggregateCacheItemsVersion = -1;
        _aggregateCacheFilterVersion = -1;
        _showGroupBuilder = false;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    // ── Pagination ──────────────────────────────────────────────────────────────

    private async Task GoToPageAsync(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
        InvalidateComputedRowsCache();
        await SaveStateAsync();
        await RaiseStateChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnPageSizeChange(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var pageSize) && pageSize > 0)
        {
            _pageSize = pageSize;
            _currentPage = 1;
            
            if (PageSizeChanged.HasDelegate)
            {
                await PageSizeChanged.InvokeAsync(pageSize);
            }
        }

        InvalidateComputedRowsCache();
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private IEnumerable<int> GetPageWindow()
    {
        var total = TotalPages;
        if (total <= 7)
            return Enumerable.Range(1, total);

        var pages = new List<int> { 1 };
        var start = Math.Max(2, _currentPage - 1);
        var end = Math.Min(total - 1, _currentPage + 1);

        if (start > 2)
            pages.Add(-1);

        for (var i = start; i <= end; i++)
            pages.Add(i);

        if (end < total - 1)
            pages.Add(-1);

        pages.Add(total);
        return pages;
    }

    // ── Virtualization ──────────────────────────────────────────────────────────

    private bool ShouldUseVirtualization()
    {
        if (!EnableVirtualization)
            return false;

        if (_groupByKeys.Count > 0)
            return false;

        if (WrapCells)
            return false;

        var totalRows = GetFilteredSortedRows().Count;
        return totalRows >= VirtualizationThreshold;
    }

    private (int startIndex, int endIndex, int topPadding, int bottomPadding) CalculateVirtualWindow()
    {
        var totalRows = GetFilteredSortedRows().Count;

        if (totalRows == 0 || _viewportHeight == 0)
        {
            return (0, 0, 0, 0);
        }

        var firstVisibleRow = (int)Math.Floor((double)_scrollTop / _estimatedRowHeight);
        var rowsInViewport = (int)Math.Ceiling((double)_viewportHeight / _estimatedRowHeight);
        var startIndex = Math.Max(0, firstVisibleRow - _virtualizationBufferRows);
        var endIndex = Math.Min(totalRows - 1, firstVisibleRow + rowsInViewport + _virtualizationBufferRows);
        var topPadding = startIndex * _estimatedRowHeight;
        var bottomPadding = (totalRows - 1 - endIndex) * _estimatedRowHeight;

        return (startIndex, endIndex, topPadding, bottomPadding);
    }

    // ── Row computation ─────────────────────────────────────────────────────────

    internal void InvalidateComputedRowsCache()
    {
        _filteredRowsCacheItemsVersion = -1;
        _filteredRowsCacheFilterVersion = -1;
        _filteredSortedRowsCacheFilterVersion = -1;
        _filteredSortedRowsCacheSortVersion = -1;
        _visibleRowsCacheItemsVersion = -1;
        _visibleRowsCacheFilterVersion = -1;
        _visibleRowsCacheSortVersion = -1;
        _visibleRowsCacheColumnsVersion = -1;
        _orderedColumnsCacheColumnsVersion = -1;
        _distinctValuesCacheItemsVersion = -1;
        _groupTreeCacheItemsVersion = -1;
        _groupTreeCacheGroupVersion = -1;
        _aggregateCacheItemsVersion = -1;
        _aggregateCacheFilterVersion = -1;
    }

    private List<TItem> GetFilteredRowsExcept(string? skipKey)
    {
        var hasSearch = !string.IsNullOrWhiteSpace(_search);

        var preparedQuickFilters = new List<(SgDataGridColumn<TItem> Column, string Value)>();
        foreach (var quickFilter in _quickFilters)
        {
            if (quickFilter.Key == skipKey) continue;
            var col = GetColumnByKey(quickFilter.Key);
            if (col != null) preparedQuickFilters.Add((col, quickFilter.Value));
        }

        var preparedValueFilters = new List<(SgDataGridColumn<TItem> Column, HashSet<string> Values)>();
        foreach (var valueFilter in _filters)
        {
            if (valueFilter.Key == skipKey) continue;
            var col = GetColumnByKey(valueFilter.Key);
            if (col != null) preparedValueFilters.Add((col, valueFilter.Value));
        }

        var preparedConditionFilters = new List<(SgDataGridColumn<TItem> Column, ColumnFilter Filter)>();
        foreach (var conditionFilter in _conditionFilters)
        {
            if (conditionFilter.Key == skipKey) continue;
            var col = GetColumnByKey(conditionFilter.Key);
            if (col != null) preparedConditionFilters.Add((col, conditionFilter.Value));
        }

        var preparedQueryRules = new List<(SgDataGridColumn<TItem> Column, QueryRule Rule, Type TargetType)>();
        foreach (var queryRule in _queryRules)
        {
            if (queryRule.FieldName == skipKey) continue;
            var col = GetColumnByKey(queryRule.FieldName ?? "");
            if (col != null) preparedQueryRules.Add((col, queryRule, ResolveColumnType(col)));
        }

        var result = new List<TItem>();
        var source = IsTree && TreeChildren != null ? GetAllTreeItems() : (Items ?? Enumerable.Empty<TItem>());

        foreach (var item in source)
        {
            if (hasSearch && !MatchesSearch(item)) continue;
            if (!ItemPassesFilters(item, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules)) continue;
            result.Add(item);
        }

        return result;
    }

    private List<TItem> GetFilteredRows()
    {
        if (_filteredRowsCacheItemsVersion == _itemsVersion &&
            _filteredRowsCacheFilterVersion == _filterVersion &&
            _filteredRowsCache is not null)
            return _filteredRowsCache;

        EnsureAutoGeneratedColumns();
        _rowLevels.Clear();

        var result = new List<TItem>();
        var hasSearch = !string.IsNullOrWhiteSpace(_search);

        var preparedQuickFilters = new List<(SgDataGridColumn<TItem> Column, string Value)>(_quickFilters.Count);
        foreach (var quickFilter in _quickFilters)
        {
            var col = GetColumnByKey(quickFilter.Key);
            if (col is null)
                continue;
            preparedQuickFilters.Add((col, quickFilter.Value));
        }

        var preparedValueFilters = new List<(SgDataGridColumn<TItem> Column, HashSet<string> Values)>(_filters.Count);
        foreach (var valueFilter in _filters)
        {
            var col = GetColumnByKey(valueFilter.Key);
            if (col is null)
                continue;
            preparedValueFilters.Add((col, valueFilter.Value));
        }

        var preparedConditionFilters = new List<(SgDataGridColumn<TItem> Column, ColumnFilter Filter)>(_conditionFilters.Count);
        foreach (var conditionFilter in _conditionFilters)
        {
            var col = GetColumnByKey(conditionFilter.Key);
            if (col is null)
                continue;
            preparedConditionFilters.Add((col, conditionFilter.Value));
        }

        var preparedQueryRules = new List<(SgDataGridColumn<TItem> Column, QueryRule Rule, Type TargetType)>(_queryRules.Count);
        for (var i = 0; i < _queryRules.Count; i++)
        {
            var queryRule = _queryRules[i];
            if (string.IsNullOrWhiteSpace(queryRule.FieldName))
                continue;
            var col = GetColumnByKey(queryRule.FieldName);
            if (col is null)
                continue;
            preparedQueryRules.Add((col, queryRule, ResolveColumnType(col)));
        }

        if (IsTree && TreeChildren != null)
        {
            if (Items != null)
            {
                foreach (var item in Items)
                {
                    AddTreeItemRecursive(item, 0, result, hasSearch, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules);
                }
            }
        }
        else
        {
            if (Items is IList<TItem> itemsList)
            {
                for (var i = 0; i < itemsList.Count; i++)
                {
                    var item = itemsList[i];
                    if (hasSearch && !MatchesSearch(item))
                        continue;
                    if (!ItemPassesFilters(item, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules))
                        continue;
                    result.Add(item);
                }
            }
            else if (Items is not null)
            {
                foreach (var item in Items)
                {
                    if (hasSearch && !MatchesSearch(item))
                        continue;
                    if (!ItemPassesFilters(item, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules))
                        continue;
                    result.Add(item);
                }
            }
        }

        _filteredRowsCache = result;
        _filteredRowsCacheItemsVersion = _itemsVersion;
        _filteredRowsCacheFilterVersion = _filterVersion;
        return _filteredRowsCache;
    }

    private List<TItem> GetFilteredSortedRows()
    {
        if (_filteredSortedRowsCacheFilterVersion == _filterVersion &&
            _filteredSortedRowsCacheSortVersion == _sortVersion &&
            _filteredSortedRowsCache is not null)
            return _filteredSortedRowsCache;

        IEnumerable<TItem> query = GetFilteredRows();
        if (_sort.Count == 0)
        {
            _filteredSortedRowsCache = query.ToList();
            _filteredSortedRowsCacheFilterVersion = _filterVersion;
            _filteredSortedRowsCacheSortVersion = _sortVersion;
            return _filteredSortedRowsCache;
        }

        List<(Func<TItem, object?> Selector, bool Descending)> preparedSorts;
        if (_preparedSortsCache is not null
            && _preparedSortsCacheSortVersion == _sortVersion
            && _preparedSortsCacheColumnsVersion == _columnsVersion)
        {
            preparedSorts = _preparedSortsCache;
        }
        else
        {
            preparedSorts = new List<(Func<TItem, object?> Selector, bool Descending)>(_sort.Count);
            for (var i = 0; i < _sort.Count; i++)
            {
                var sortRule = _sort[i];
                var col = GetColumnByKey(sortRule.Key);
                if (col is null)
                    continue;
                preparedSorts.Add((col.GetValue, sortRule.Dir == SortDirection.Descending));
            }
            _preparedSortsCache = preparedSorts;
            _preparedSortsCacheSortVersion = _sortVersion;
            _preparedSortsCacheColumnsVersion = _columnsVersion;
        }

        if (preparedSorts.Count == 0)
        {
            _filteredSortedRowsCache = query.ToList();
            _filteredSortedRowsCacheFilterVersion = _filterVersion;
            _filteredSortedRowsCacheSortVersion = _sortVersion;
            return _filteredSortedRowsCache;
        }

        IOrderedEnumerable<TItem>? ordered = null;
        for (var i = 0; i < preparedSorts.Count; i++)
        {
            var sort = preparedSorts[i];
            ordered = ordered is null
                ? (sort.Descending
                    ? query.OrderByDescending(sort.Selector, GridObjectComparer.Instance)
                    : query.OrderBy(sort.Selector, GridObjectComparer.Instance))
                : (sort.Descending
                    ? ordered.ThenByDescending(sort.Selector, GridObjectComparer.Instance)
                    : ordered.ThenBy(sort.Selector, GridObjectComparer.Instance));
        }

        _filteredSortedRowsCache = (ordered ?? query.OrderBy(static _ => 0)).ToList();
        _filteredSortedRowsCacheFilterVersion = _filterVersion;
        _filteredSortedRowsCacheSortVersion = _sortVersion;
        return _filteredSortedRowsCache;
    }

    private List<TItem> GetVisibleRows()
    {
        if (_visibleRowsCacheItemsVersion == _itemsVersion &&
            _visibleRowsCacheFilterVersion == _filterVersion &&
            _visibleRowsCacheSortVersion == _sortVersion &&
            _visibleRowsCacheColumnsVersion == _columnsVersion &&
            _visibleRowsCache is not null)
            return _visibleRowsCache;

        var rows = GetFilteredSortedRows();

        if (ShouldUseVirtualization())
        {
            var (startIndex, endIndex, _, _) = CalculateVirtualWindow();
            var count = Math.Min(endIndex - startIndex + 1, rows.Count - startIndex);
            _visibleRowsCache = rows.GetRange(startIndex, count);
            _visibleRowsCacheItemsVersion = _itemsVersion;
            _visibleRowsCacheFilterVersion = _filterVersion;
            _visibleRowsCacheSortVersion = _sortVersion;
            _visibleRowsCacheColumnsVersion = _columnsVersion;
            return _visibleRowsCache;
        }

        if (!EnablePaging)
        {
            _visibleRowsCache = rows;
            _visibleRowsCacheItemsVersion = _itemsVersion;
            _visibleRowsCacheFilterVersion = _filterVersion;
            _visibleRowsCacheSortVersion = _sortVersion;
            _visibleRowsCacheColumnsVersion = _columnsVersion;
            return rows;
        }

        var pageSize = Math.Max(1, _pageSize);
        var skip = Math.Max(0, (_currentPage - 1) * pageSize);
        if (skip >= rows.Count)
        {
            _visibleRowsCache = new List<TItem>();
            _visibleRowsCacheItemsVersion = _itemsVersion;
            _visibleRowsCacheFilterVersion = _filterVersion;
            _visibleRowsCacheSortVersion = _sortVersion;
            _visibleRowsCacheColumnsVersion = _columnsVersion;
            return _visibleRowsCache;
        }
        var take = Math.Min(pageSize, rows.Count - skip);
        _visibleRowsCache = rows.GetRange(skip, take);
        _visibleRowsCacheItemsVersion = _itemsVersion;
        _visibleRowsCacheFilterVersion = _filterVersion;
        _visibleRowsCacheSortVersion = _sortVersion;
        _visibleRowsCacheColumnsVersion = _columnsVersion;
        return _visibleRowsCache;
    }

    // ── Tree helpers for filtering ──────────────────────────────────────────────

    private IEnumerable<TItem> GetAllTreeItems()
    {
        if (Items == null || TreeChildren == null) yield break;
        foreach (var item in Items)
        {
            foreach (var subItem in TraverseTree(item))
            {
                yield return subItem;
            }
        }
    }

    private IEnumerable<TItem> TraverseTree(TItem item)
    {
        yield return item;
        var children = TreeChildren?.Invoke(item);
        if (children != null)
        {
            foreach (var child in children)
            {
                foreach (var subChild in TraverseTree(child))
                {
                    yield return subChild;
                }
            }
        }
    }

    private bool AddTreeItemRecursive(
        TItem item, 
        int level, 
        List<TItem> result, 
        bool hasSearch,
        List<(SgDataGridColumn<TItem> Column, string Value)> preparedQuickFilters,
        List<(SgDataGridColumn<TItem> Column, HashSet<string> Values)> preparedValueFilters,
        List<(SgDataGridColumn<TItem> Column, ColumnFilter Filter)> preparedConditionFilters,
        List<(SgDataGridColumn<TItem> Column, QueryRule Rule, Type TargetType)> preparedQueryRules)
    {
        bool selfMatches = true;
        if (hasSearch && !MatchesSearch(item)) selfMatches = false;
        if (selfMatches && !ItemPassesFilters(item, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules)) selfMatches = false;

        var children = TreeChildren?.Invoke(item);
        var childrenResults = new List<TItem>();
        bool anyChildMatches = false;
        bool hasActiveFilter = hasSearch || preparedQuickFilters.Count > 0 || preparedValueFilters.Count > 0 || preparedConditionFilters.Count > 0 || preparedQueryRules.Count > 0;

        if (children != null)
        {
            foreach (var child in children)
            {
                if (hasActiveFilter || IsTreeNodeExpanded(item))
                {
                    var childSubList = new List<TItem>();
                    if (AddTreeItemRecursive(child, level + 1, childSubList, hasSearch, preparedQuickFilters, preparedValueFilters, preparedConditionFilters, preparedQueryRules))
                    {
                        anyChildMatches = true;
                        childrenResults.AddRange(childSubList);
                    }
                }
            }
        }

        bool shouldInclude = selfMatches || anyChildMatches;

        if (shouldInclude)
        {
            result.Add(item);
            _rowLevels[item] = level;
            
            if (hasActiveFilter && !selfMatches && anyChildMatches)
            {
                _expandedTreeNodes.Add(item);
            }

            result.AddRange(childrenResults);
        }

        return shouldInclude;
    }

    // ── Filter matching ─────────────────────────────────────────────────────────

    private bool MatchesSearch(TItem item)
    {
        if (string.IsNullOrWhiteSpace(_search))
            return true;

        for (var i = 0; i < _columns.Count; i++)
        {
            var display = _columns[i].GetDisplay(item);
            if (display.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private bool ItemPassesFilters(
        TItem item,
        List<(SgDataGridColumn<TItem> Column, string Value)> preparedQuickFilters,
        List<(SgDataGridColumn<TItem> Column, HashSet<string> Values)> preparedValueFilters,
        List<(SgDataGridColumn<TItem> Column, ColumnFilter Filter)> preparedConditionFilters,
        List<(SgDataGridColumn<TItem> Column, QueryRule Rule, Type TargetType)> preparedQueryRules)
    {
        for (var q = 0; q < preparedQuickFilters.Count; q++)
        {
            var quickFilter = preparedQuickFilters[q];
            var col = quickFilter.Column;
            var val = quickFilter.Value;

            var type = col.ValueType ?? typeof(string);
            type = Nullable.GetUnderlyingType(type) ?? type;

            if (col.ValueType is null)
            {
                var sample = col.GetValue(item);
                if (sample is not null)
                {
                    var sampleType = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
                    if (sampleType != typeof(string))
                        type = sampleType;
                }
            }

            if (type == typeof(bool))
            {
                var rawValue = col.GetValue(item);
                if (rawValue is bool b)
                {
                    var filterBool = bool.Parse(val);
                    if (b != filterBool) return false;
                }
                else
                {
                    return false;
                }
            }
            else if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            {
                var rawValue = col.GetValue(item);

                DateTime? itemDate = rawValue switch
                {
                    DateTime dt => dt,
                    DateTimeOffset dto => dto.DateTime,
                    DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                    _ => null
                };

                if (itemDate is null) return false;

                if (DateTime.TryParseExact(val, "yyyy-MM-ddTHH:mm",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDateTime))
                {
                    var itemTrunc = new DateTime(itemDate.Value.Year, itemDate.Value.Month, itemDate.Value.Day,
                        itemDate.Value.Hour, itemDate.Value.Minute, 0);
                    if (itemTrunc != isoDateTime) return false;
                }
                else if (DateTime.TryParseExact(val, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDate))
                {
                    if (itemDate.Value.Date != isoDate.Date) return false;
                }
                else if (DateTime.TryParse(val, CultureInfo.CurrentCulture,
                        System.Globalization.DateTimeStyles.None, out var locDate))
                {
                    if (locDate.TimeOfDay == TimeSpan.Zero)
                    {
                        if (itemDate.Value.Date != locDate.Date) return false;
                    }
                    else
                    {
                        var itemTrunc = new DateTime(itemDate.Value.Year, itemDate.Value.Month, itemDate.Value.Day,
                            itemDate.Value.Hour, itemDate.Value.Minute, 0);
                        var filterTrunc = new DateTime(locDate.Year, locDate.Month, locDate.Day,
                            locDate.Hour, locDate.Minute, 0);
                        if (itemTrunc != filterTrunc) return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else if (type.IsEnum)
            {
                var rawValue = col.GetValue(item);
                var enumDisplay = col.GetDisplay(item);
                var enumName = rawValue?.ToString() ?? string.Empty;
                if (enumDisplay.IndexOf(val, StringComparison.CurrentCultureIgnoreCase) < 0 &&
                    enumName.IndexOf(val, StringComparison.CurrentCultureIgnoreCase) < 0)
                    return false;
            }
            else
            {
                var display = col.GetDisplay(item);
                if (display.IndexOf(val, StringComparison.CurrentCultureIgnoreCase) < 0)
                    return false;
            }
        }

        for (var v = 0; v < preparedValueFilters.Count; v++)
        {
            var valueFilter = preparedValueFilters[v];
            var col = valueFilter.Column;
            var filterType = GetColumnFilterType(col.Key);
            var useRaw = filterType == "number" || filterType == "date" || filterType == "datetime" || filterType == "enum";

            string rawKey;
            if (useRaw)
            {
                var raw = col.GetValue(item);
                rawKey = raw switch
                {
                    null => string.Empty,
                    DateTime dt => dt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Enum e => e.ToString(),
                    _ => raw.ToString() ?? string.Empty
                };
            }
            else
            {
                rawKey = NormalizeFilterValue(col.GetDisplay(item));
            }

            if (!valueFilter.Values.Contains(rawKey))
                return false;
        }

        for (var c = 0; c < preparedConditionFilters.Count; c++)
        {
            var conditionFilter = preparedConditionFilters[c];
            if (!MatchesConditionFilter(item, conditionFilter.Column, conditionFilter.Filter))
                return false;
        }

        for (var r = 0; r < preparedQueryRules.Count; r++)
        {
            var queryRule = preparedQueryRules[r];
            var rawValue = queryRule.Column.GetValue(item);
            var display = queryRule.Column.GetDisplay(item);
            if (!MatchesQueryRulePrepared(rawValue, display, queryRule.TargetType, queryRule.Rule))
                return false;
        }

        return true;
    }

    private bool MatchesConditionFilter(TItem item, SgDataGridColumn<TItem> col, ColumnFilter filter)
    {
        var rawValue = col.GetValue(item);
        var targetType = ResolveColumnType(col, rawValue);

        if (filter.And)
        {
            for (var i = 0; i < filter.Rules.Count; i++)
            {
                if (!MatchesRule(rawValue, targetType, filter.Rules[i]))
                    return false;
            }
            return true;
        }

        for (var i = 0; i < filter.Rules.Count; i++)
        {
            if (MatchesRule(rawValue, targetType, filter.Rules[i]))
                return true;
        }

        return false;
    }

    private bool MatchesQueryRule(TItem item, QueryRule rule)
    {
        if (string.IsNullOrWhiteSpace(rule.FieldName))
            return true;

        var col = GetColumnByKey(rule.FieldName);
        if (col is null)
            return true;

        var rawValue = col.GetValue(item);
        var targetType = ResolveColumnType(col, rawValue);
        var display = col.GetDisplay(item);
        return MatchesQueryRulePrepared(rawValue, display, targetType, rule);
    }

    private static bool MatchesQueryRulePrepared(object? rawValue, string display, Type targetType, QueryRule rule)
    {
        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (target.IsEnum && rawValue is not null)
        {
            var enumItems = SgEnumHelper.GetItems(target);
            var enumName = rawValue.ToString() ?? string.Empty;
            var enumLabel = enumItems.FirstOrDefault(ei =>
                ei.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase))?.Label ?? enumName;
            display = enumLabel;
        }

        return rule.Operator switch
        {
            QueryFieldOperator.Equals => CompareQueryValue(rawValue, rule.Value, targetType) == 0,
            QueryFieldOperator.NotEquals => CompareQueryValue(rawValue, rule.Value, targetType) != 0,
            QueryFieldOperator.Contains => display.IndexOf(rule.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) >= 0,
            QueryFieldOperator.NotContains => display.IndexOf(rule.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) < 0,
            QueryFieldOperator.StartsWith => display.StartsWith(rule.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase),
            QueryFieldOperator.EndsWith => display.EndsWith(rule.Value?.ToString() ?? string.Empty, StringComparison.CurrentCultureIgnoreCase),
            QueryFieldOperator.GreaterThan => CompareQueryValue(rawValue, rule.Value, targetType) > 0,
            QueryFieldOperator.GreaterOrEqual => CompareQueryValue(rawValue, rule.Value, targetType) >= 0,
            QueryFieldOperator.LessThan => CompareQueryValue(rawValue, rule.Value, targetType) < 0,
            QueryFieldOperator.LessOrEqual => CompareQueryValue(rawValue, rule.Value, targetType) <= 0,
            QueryFieldOperator.In => MatchesQuerySet(rawValue, rule.Value, targetType, true),
            QueryFieldOperator.NotIn => MatchesQuerySet(rawValue, rule.Value, targetType, false),
            QueryFieldOperator.IsNull => rawValue is null || string.IsNullOrWhiteSpace(display),
            QueryFieldOperator.IsNotNull => rawValue is not null && !string.IsNullOrWhiteSpace(display),
            _ => true
        };
    }

    private static bool MatchesRule(object? rawValue, Type targetType, FilterRule rule)
    {
        var target = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var display = rawValue?.ToString() ?? string.Empty;

        if (target.IsEnum && rawValue is not null)
        {
            var enumItems = SgEnumHelper.GetItems(target);
            var enumName = rawValue.ToString() ?? string.Empty;
            var enumLabel = enumItems.FirstOrDefault(ei =>
                ei.Name.Equals(enumName, StringComparison.OrdinalIgnoreCase))?.Label ?? enumName;
            display = enumLabel;
        }

        return rule.Condition switch
        {
            FilterCondition.Contains => display.IndexOf(rule.Value ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) >= 0,
            FilterCondition.NotContains => display.IndexOf(rule.Value ?? string.Empty, StringComparison.CurrentCultureIgnoreCase) < 0,
            FilterCondition.Equals => CompareForCondition(rawValue, rule.Value, targetType) == 0,
            FilterCondition.NotEquals => CompareForCondition(rawValue, rule.Value, targetType) != 0,
            FilterCondition.StartsWith => display.StartsWith(rule.Value ?? string.Empty, StringComparison.CurrentCultureIgnoreCase),
            FilterCondition.EndsWith => display.EndsWith(rule.Value ?? string.Empty, StringComparison.CurrentCultureIgnoreCase),
            FilterCondition.GreaterThan => CompareForCondition(rawValue, rule.Value, targetType) > 0,
            FilterCondition.LessThan => CompareForCondition(rawValue, rule.Value, targetType) < 0,
            FilterCondition.GreaterOrEqual => CompareForCondition(rawValue, rule.Value, targetType) >= 0,
            FilterCondition.LessOrEqual => CompareForCondition(rawValue, rule.Value, targetType) <= 0,
            FilterCondition.IsEmpty => string.IsNullOrWhiteSpace(display),
            FilterCondition.IsNotEmpty => !string.IsNullOrWhiteSpace(display),
            _ => true
        };
    }

    private static int CompareForCondition(object? rawValue, string? textValue, Type targetType)
    {
        var left = ConvertForComparison(rawValue, targetType);
        var right = ConvertFromString(textValue, targetType);
        return GridObjectComparer.Instance.Compare(left, right);
    }

    private static int CompareQueryValue(object? rawValue, object? queryValue, Type targetType)
    {
        var left = ConvertForComparison(rawValue, targetType);
        var right = ConvertQueryValue(queryValue, targetType);
        return GridObjectComparer.Instance.Compare(left, right);
    }

    private static object? ConvertForComparison(object? value, Type type)
    {
        if (value is null)
            return null;

        var target = Nullable.GetUnderlyingType(type) ?? type;

        if (target.IsEnum)
        {
            if (value is Enum)
                return Convert.ToInt32(value);
            if (value is string s && Enum.TryParse(target, s, true, out var parsed))
                return Convert.ToInt32(parsed);
            return value.ToString();
        }

        if (target == typeof(DateTime))
        {
            if (value is DateTime dt) return dt.Date;
            if (value is DateTimeOffset dto) return dto.Date;
        }
        if (target == typeof(DateTimeOffset))
        {
            if (value is DateTimeOffset dto2) return dto2.Date;
            if (value is DateTime dt2) return dt2.Date;
        }
        if (target == typeof(DateOnly))
        {
            if (value is DateOnly d) return d;
            if (value is DateTime dt3) return DateOnly.FromDateTime(dt3);
        }

        var valueType = Nullable.GetUnderlyingType(value.GetType()) ?? value.GetType();
        if (valueType != target && target != typeof(object))
        {
            if (IsNumericType(target) && IsNumericValue(value))
            {
                try
                {
                    return Convert.ChangeType(value, target, CultureInfo.CurrentCulture);
                }
                catch
                {
                    try
                    {
                        var asDecimal = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
                        return Convert.ChangeType(asDecimal, target, CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                    }
                }
            }
        }
        return value;
    }

    private static object? ConvertFromString(string? text, Type type)
    {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (string.IsNullOrWhiteSpace(text))
        {
            if (target == typeof(string))
                return string.Empty;
            return null;
        }

        try
        {
            if (target == typeof(string))
                return text;
            if (target == typeof(bool))
                return text is "true" or "True" or "1" or "✓" || bool.Parse(text);

            if (IsNumericType(target))
            {
                text = CleanNumericString(text);
                var expectedSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                if (expectedSeparator == ',' && text.Contains('.'))
                {
                    text = text.Replace('.', ',');
                }
            }

            if (target == typeof(int))
                return int.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(long))
                return long.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(decimal))
                return decimal.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(double))
                return double.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(float))
                return float.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(short))
                return short.Parse(text, CultureInfo.CurrentCulture);
            if (target == typeof(byte))
                return byte.Parse(text, CultureInfo.CurrentCulture);

            if (target == typeof(DateTime))
            {
                if (DateTime.TryParseExact(text, "yyyy-MM-ddTHH:mm",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDateTime))
                    return isoDateTime;
                if (DateTime.TryParseExact(text, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDate))
                    return isoDate.Date;
                if (DateTime.TryParse(text, CultureInfo.CurrentCulture,
                        System.Globalization.DateTimeStyles.None, out var locDate))
                    return locDate;
                return null;
            }
            if (target == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(text, CultureInfo.CurrentCulture, System.Globalization.DateTimeStyles.None, out var dto))
                    return dto;
                return null;
            }
            if (target == typeof(DateOnly))
            {
                if (DateOnly.TryParse(text, CultureInfo.CurrentCulture, out var d))
                    return d;
                return null;
            }

            if (target == typeof(Guid))
                return Guid.Parse(text);

            if (target.IsEnum)
            {
                if (Enum.TryParse(target, text, true, out var enumVal))
                    return enumVal;
                var items = SgEnumHelper.GetItems(target);
                var match = items.FirstOrDefault(ei =>
                    ei.Label.Equals(text, StringComparison.CurrentCultureIgnoreCase) ||
                    ei.Name.Equals(text, StringComparison.CurrentCultureIgnoreCase));
                if (match is not null)
                    return Enum.Parse(target, match.Name, true);
                return text;
            }

            return Convert.ChangeType(text, target, CultureInfo.CurrentCulture);
        }
        catch
        {
            return text;
        }
    }

    private static object? ConvertQueryValue(object? value, Type type)
    {
        if (value is null)
            return null;

        return value switch
        {
            string text => ConvertFromString(text, type),
            _ => ConvertForComparison(value, type)
        };
    }

    private static bool MatchesQuerySet(object? rawValue, object? queryValue, Type targetType, bool shouldContain)
    {
        var values = ParseQueryValues(queryValue, targetType);
        if (values.Count == 0)
            return !shouldContain;

        var left = ConvertForComparison(rawValue, targetType);
        var contains = values.Any(candidate => GridObjectComparer.Instance.Compare(left, candidate) == 0);
        return shouldContain ? contains : !contains;
    }

    private static List<object?> ParseQueryValues(object? value, Type targetType)
    {
        if (value is null)
            return new List<object?>();

        if (value is string text)
        {
            return text
                .Split(new[] { ',', ';', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(item => ConvertFromString(item, targetType))
                .ToList();
        }

        if (value is IEnumerable enumerable and not string)
        {
            var result = new List<object?>();
            foreach (var item in enumerable)
                result.Add(ConvertQueryValue(item, targetType));
            return result;
        }

        return new List<object?> { ConvertQueryValue(value, targetType) };
    }

    private static QueryRule CloneQueryRule(QueryRule rule) =>
        new()
        {
            FieldName = rule.FieldName,
            Operator = rule.Operator,
            Value = CloneQueryValue(rule.Value)
        };

    private static object? CloneQueryValue(object? value)
    {
        if (value is null)
            return null;

        if (value is string)
            return value;

        if (value is IEnumerable enumerable and not string)
        {
            var result = new List<object?>();
            foreach (var item in enumerable)
                result.Add(item);
            return result;
        }

        return value;
    }

    private static bool IsValidQueryRule(QueryRule? rule)
    {
        if (rule is null || string.IsNullOrWhiteSpace(rule.FieldName))
            return false;

        return rule.Operator is QueryFieldOperator.IsNull or QueryFieldOperator.IsNotNull ||
               rule.Value is not null && !string.IsNullOrWhiteSpace(rule.Value.ToString());
    }

    private static string NormalizeFilterValue(string? value) => value ?? string.Empty;

    private string GetColumnFilterType(string key)
    {
        if (_columnFilterTypeCacheColumnsVersion != _columnsVersion ||
            _columnFilterTypeCacheItemsVersion != _itemsVersion)
        {
            _columnFilterTypeCache.Clear();
            _columnFilterTypeCacheColumnsVersion = _columnsVersion;
            _columnFilterTypeCacheItemsVersion = _itemsVersion;
        }

        if (_columnFilterTypeCache.TryGetValue(key, out var cached))
            return cached;

        var result = ComputeColumnFilterType(key);
        _columnFilterTypeCache[key] = result;
        return result;
    }

    private string ComputeColumnFilterType(string key)
    {
        var col = GetColumnByKey(key);
        if (col is null) return "string";

        var type = col.ValueType;
        if (type is null)
        {
            if (Items is IList<TItem> list)
            {
                var limit = Math.Min(20, list.Count);
                for (var i = 0; i < limit; i++)
                {
                    var v = col.GetValue(list[i]);
                    if (v is null) continue;
                    type = Nullable.GetUnderlyingType(v.GetType()) ?? v.GetType();
                    break;
                }
            }
            else if (Items is not null)
            {
                foreach (var item in Items)
                {
                    var v = col.GetValue(item);
                    if (v is null) continue;
                    type = Nullable.GetUnderlyingType(v.GetType()) ?? v.GetType();
                    break;
                }
            }
        }

        if (type is null) return "string";
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(bool)) return "bool";
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(DateOnly))
            return col.ShowTime ? "datetime" : "date";
        if (type.IsEnum) return "enum";
        if (IsNumericType(type)) return "number";

        return "string";
    }

    private List<SgEnumItem> GetColumnEnumItems(string key)
    {
        if (_columnEnumItemsCacheColumnsVersion != _columnsVersion ||
            _columnEnumItemsCacheItemsVersion != _itemsVersion)
        {
            _columnEnumItemsCache.Clear();
            _columnEnumItemsCacheColumnsVersion = _columnsVersion;
            _columnEnumItemsCacheItemsVersion = _itemsVersion;
        }

        if (_columnEnumItemsCache.TryGetValue(key, out var cached))
            return cached;

        var col = GetColumnByKey(key);
        if (col is null)
        {
            _columnEnumItemsCache[key] = new();
            return _columnEnumItemsCache[key];
        }
        var type = col.ValueType;
        if (type is null)
        {
            var sample = Items?.Take(20)
                .Select(i => col.GetValue(i))
                .FirstOrDefault(v => v is not null);
            if (sample is not null)
                type = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
        }
        List<SgEnumItem> result;
        if (type is null)
        {
            result = new();
        }
        else
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            result = type.IsEnum ? SgEnumHelper.GetItems(type) : new();
        }
        _columnEnumItemsCache[key] = result;
        return result;
    }

    private bool IsNumericColumn(string key)
    {
        var col = GetColumnByKey(key);
        if (col?.ValueType is not null)
        {
            var type = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
            return IsNumericType(type);
        }

        var values = Items?.Take(20).Select(item => col?.GetValue(item)) ?? Enumerable.Empty<object?>();
        var numericCount = values
            .Where(v => v is not null)
            .Take(10)
            .Count(IsNumericValue);

        return numericCount >= 3;
    }

    private bool IsDateColumn(string key)
    {
        var ft = GetColumnFilterType(key);
        return ft == "date" || ft == "datetime";
    }

    private bool IsBoolColumn(string key)
        => GetColumnFilterType(key) == "bool";

    private bool IsEnumColumn(string key)
        => GetColumnFilterType(key) == "enum";

    private static bool IsNumericType(Type type) =>
        type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
        type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong) ||
        type == typeof(float) || type == typeof(double) || type == typeof(decimal);

    private static bool IsNumericValue(object? value)
    {
        return value switch
        {
            sbyte => true,
            byte => true,
            short => true,
            ushort => true,
            int => true,
            uint => true,
            long => true,
            ulong => true,
            float => true,
            double => true,
            decimal => true,
            _ => false
        };
    }

    private static string CleanNumericString(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var cleaned = new StringBuilder();

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (char.IsDigit(c) || c == ',' || c == '.' || c == '-' || c == '+' || c == 'E' || c == 'e')
            {
                cleaned.Append(c);
            }
        }

        return cleaned.ToString();
    }

    private static Type ResolveColumnType(SgDataGridColumn<TItem>? col, object? rawValue = null)
    {
        if (col?.ValueType is not null)
        {
            var t = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
            return t;
        }

        if (rawValue is not null)
        {
            var valueType = Nullable.GetUnderlyingType(rawValue.GetType()) ?? rawValue.GetType();
            if (IsNumericType(valueType) || valueType == typeof(DateTime) ||
                valueType == typeof(DateTimeOffset) || valueType == typeof(DateOnly) ||
                valueType == typeof(bool) || valueType.IsEnum)
                return valueType;
        }

        return typeof(string);
    }

    // ── Query rules ─────────────────────────────────────────────────────────────

    public IReadOnlyList<QueryRule> GetQueryRules() =>
        _queryRules.Select(CloneQueryRule).ToList();

    public async Task ApplyQueryRulesAsync(IReadOnlyList<QueryRule>? rules)
    {
        EnsureAutoGeneratedColumns();

        _queryRules.Clear();
        if (rules is not null)
        {
            _queryRules.AddRange(rules
                .Where(IsValidQueryRule)
                .Select(CloneQueryRule));
        }

        _currentPage = 1;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ClearQueryRulesAsync()
    {
        if (_queryRules.Count == 0)
            return;

        _queryRules.Clear();
        _currentPage = 1;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    // ── Aggregates ──────────────────────────────────────────────────────────────

    private object? ComputeAggregate(SgDataGridColumn<TItem> col)
    {
        if (col.Aggregate == Aggregate.None)
            return null;

        EnsureAggregateCache();
        return _aggregateCache.TryGetValue(col.Key, out var value) ? value : null;
    }

    private void EnsureAggregateCache()
    {
        if (_aggregateCacheItemsVersion == _itemsVersion &&
            _aggregateCacheFilterVersion == _filterVersion)
            return;

        _aggregateCache.Clear();
        var rows = GetFilteredRows();
        if (rows.Count == 0)
        {
            _aggregateCacheItemsVersion = _itemsVersion;
            _aggregateCacheFilterVersion = _filterVersion;
            return;
        }

        var visible = VisibleColumns;
        List<SgDataGridColumn<TItem>>? aggregateColumns = null;
        for (var i = 0; i < visible.Count; i++)
        {
            if (visible[i].Aggregate == Aggregate.None)
                continue;
            aggregateColumns ??= new List<SgDataGridColumn<TItem>>();
            aggregateColumns.Add(visible[i]);
        }
        if (aggregateColumns is null)
        {
            _aggregateCacheItemsVersion = _itemsVersion;
            _aggregateCacheFilterVersion = _filterVersion;
            return;
        }

        var states = new AggregateState[aggregateColumns.Count];
        for (var i = 0; i < aggregateColumns.Count; i++)
        {
            states[i] = new AggregateState
            {
                Count = rows.Count
            };
        }

        for (var rowIndex = 0; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            for (var colIndex = 0; colIndex < aggregateColumns.Count; colIndex++)
            {
                var column = aggregateColumns[colIndex];
                var value = column.GetValue(row);
                if (value is null)
                    continue;

                ref var state = ref states[colIndex];
                switch (column.Aggregate)
                {
                    case Aggregate.Sum:
                        state.Sum += ToDecimal(value);
                        state.NonNullCount++;
                        break;
                    case Aggregate.Average:
                        state.Sum += ToDecimal(value);
                        state.NonNullCount++;
                        break;
                    case Aggregate.Min:
                        if (!state.HasMin || GridObjectComparer.Instance.Compare(value, state.Min) < 0)
                        {
                            state.Min = value;
                            state.HasMin = true;
                        }
                        break;
                    case Aggregate.Max:
                        if (!state.HasMax || GridObjectComparer.Instance.Compare(value, state.Max) > 0)
                        {
                            state.Max = value;
                            state.HasMax = true;
                        }
                        break;
                }
            }
        }

        for (var i = 0; i < aggregateColumns.Count; i++)
        {
            var column = aggregateColumns[i];
            var state = states[i];
            _aggregateCache[column.Key] = column.Aggregate switch
            {
                Aggregate.Count => state.Count.ToString(CultureInfo.CurrentCulture),
                Aggregate.Sum => state.NonNullCount > 0 ? FormatAggregate(state.Sum) : null,
                Aggregate.Average => state.NonNullCount > 0 ? FormatAggregate(state.Sum / state.NonNullCount) : null,
                Aggregate.Min => state.HasMin ? state.Min : null,
                Aggregate.Max => state.HasMax ? state.Max : null,
                _ => null
            };
        }

        _aggregateCacheItemsVersion = _itemsVersion;
        _aggregateCacheFilterVersion = _filterVersion;
    }

    private static string FormatAggregate(decimal value) =>
        value.ToString("0.##", CultureInfo.CurrentCulture);

    private static decimal ToDecimal(object? value) =>
        value is null ? 0m : Convert.ToDecimal(value, CultureInfo.CurrentCulture);

    // ── Numeric detection ───────────────────────────────────────────────────────

    private bool ResolveIsNumericColumn(SgDataGridColumn<TItem> col)
    {
        if (col.NumericStyle.HasValue) return col.NumericStyle.Value;

        if (_numericColumnCache.TryGetValue(col.Key, out var cached))
            return cached;

        bool result;

        if (col.ValueType is not null)
        {
            var t = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
            result = SgDataGridColumn<TItem>.IsNumericTypeStatic(t);
        }
        else if (col.IsNumericColumn)
        {
            result = true;
        }
        else
        {
            var prop = typeof(TItem).GetProperty(col.Key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop is not null)
            {
                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                result = SgDataGridColumn<TItem>.IsNumericTypeStatic(underlying);
            }
            else
            {
                result = false;
            }
        }

        _numericColumnCache[col.Key] = result;
        return result;
    }

    // ── Internal types ──────────────────────────────────────────────────────────

    private sealed record DeletedRowEntry(TItem Item, int Index);

    private struct AggregateState
    {
        public int Count { get; set; }
        public int NonNullCount { get; set; }
        public decimal Sum { get; set; }
        public object? Min { get; set; }
        public object? Max { get; set; }
        public bool HasMin { get; set; }
        public bool HasMax { get; set; }
    }

    private sealed class GroupNode
    {
        public GroupNode(string path, SgDataGridColumn<TItem> column, int depth, string label, int totalCount)
        {
            Path = path;
            Column = column;
            Depth = depth;
            Label = label;
            TotalCount = totalCount;
        }

        public string Path { get; }
        public SgDataGridColumn<TItem> Column { get; }
        public int Depth { get; }
        public string Label { get; }
        public int TotalCount { get; }

        internal List<TItem>? _items;
        internal List<GroupNode>? _children;

        public List<TItem> Items => _items ??= new List<TItem>(0);
        public List<GroupNode> Children => _children ??= new List<GroupNode>(0);

        public void SetItems(List<TItem> items) => _items = items;
        public void SetChildren(List<GroupNode> children) => _children = children;
    }

    private sealed class GridObjectComparer : IComparer<object?>
    {
        public static readonly GridObjectComparer Instance = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return -1;
            if (y is null)
                return 1;

            var xType = Nullable.GetUnderlyingType(x.GetType()) ?? x.GetType();
            var yType = Nullable.GetUnderlyingType(y.GetType()) ?? y.GetType();

            if (xType.IsEnum && yType.IsEnum && xType == yType)
            {
                var xi = Convert.ToInt32(x);
                var yi = Convert.ToInt32(y);
                return xi.CompareTo(yi);
            }
            if (xType == typeof(int) && yType == typeof(int))
                return ((int)x).CompareTo((int)y);

            if (IsNumericType(xType) && IsNumericType(yType))
            {
                try
                {
                    var xDec = Convert.ToDecimal(x, CultureInfo.CurrentCulture);
                    var yDec = Convert.ToDecimal(y, CultureInfo.CurrentCulture);
                    return xDec.CompareTo(yDec);
                }
                catch
                {
                }
            }

            if (xType == typeof(DateTime) && yType == typeof(DateTime))
                return ((DateTime)x).CompareTo((DateTime)y);
            if (xType == typeof(DateTimeOffset) && yType == typeof(DateTimeOffset))
                return ((DateTimeOffset)x).CompareTo((DateTimeOffset)y);
            if (xType == typeof(DateOnly) && yType == typeof(DateOnly))
                return ((DateOnly)x).CompareTo((DateOnly)y);

            if (xType == yType && x is IComparable comparable)
                return comparable.CompareTo(y);

            if (x is string xs && y is string ys)
            {
                if (decimal.TryParse(xs, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out var xd) &&
                    decimal.TryParse(ys, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out var yd))
                    return xd.CompareTo(yd);
            }

            return string.Compare(x.ToString(), y.ToString(), true, CultureInfo.CurrentCulture);
        }
    }
}
