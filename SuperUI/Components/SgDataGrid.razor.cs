using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Localization;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace SuperUI.Components;

public partial class SgDataGrid<TItem> : ComponentBase, IAsyncDisposable
{
    private static readonly JsonSerializerOptions StateJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<SgDataGridColumn<TItem>> _columns = new();
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ColumnFilter> _conditionFilters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _quickFilters = new(StringComparer.Ordinal);
    private readonly List<QueryRule> _queryRules = new();
    private readonly List<PersistedSortRule> _sort = new();
    private readonly HashSet<string> _hiddenColumns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _columnWidths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _columnOrder = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SgDataGridColumn<TItem>> _columnLookup = new(StringComparer.Ordinal);
    private readonly List<string> _groupByKeys = new();
    private readonly HashSet<string> _collapsedGroups = new(StringComparer.Ordinal);
    private readonly Stack<DeletedRowEntry> _deletedRows = new();
    private readonly HashSet<TItem> _expandedRows = new();
    private HashSet<string> _pendingSelectedValues = new(StringComparer.Ordinal);
    private List<FilterRule> _pendingRules = [new()];
    private bool _pendingRulesAnd = true;
    private string? _pendingFilterKey;
    private string? _search;
    private string _filterMenuSearchText = string.Empty;
    private string? _openFilterColumn;
    private bool _showChooser;
    private bool _showExportMenu;
    private int _currentPage = 1;
    private TItem? _activeRow = default;
    private TItem? _lastSelectedItem = default;
    private TItem? _detailItem;

    // ── Bulk edit state ───────────────────────────────────────────────────────
    private bool _bulkEditPickerOpen;
    private bool _bulkEditModalOpen;
    private readonly HashSet<string> _bulkEditSelectedColumns = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string?> _bulkEditValues = new(StringComparer.Ordinal);

    // ── Edit form validation ──────────────────────────────────────────────────
    private readonly Dictionary<string, string> _editFormErrors = new(StringComparer.Ordinal);
    private bool _detailDrawerVisible;
    private bool _detailWindowVisible;
    private bool _editModalVisible;
    private string? _editModalTitle;
    private List<SgDataGridColumn<TItem>>? _editFormColumns;
    private readonly Dictionary<string, string?> _editFormValues = new(StringComparer.Ordinal);
    private TItem? _editingCellItem;
    private string? _editingCellColumnKey;
    private string? _editingCellValue;
    private bool _isEditMode; // true = editing existing item, false = adding new item
    private TItem? _editModalItem;
    private bool _isSyntheticColumnsInitialized;
    private IEnumerable<TItem>? _prevItems;
    private DotNetObjectReference<SgDataGrid<TItem>>? _selfRef;
    private IJSObjectReference? _module;
    private bool _disposing;
    private int _selectionVersion;
    
    // Content-based version fields for cache invalidation
    // These track changes to actual data content, not render cycles
    /// <summary>Incremented when Items collection changes (reference or count)</summary>
    private int _itemsVersion = 0;
    
    /// <summary>Incremented when search text or any filter changes</summary>
    private int _filterVersion = 0;
    
    /// <summary>Incremented when sort rules change</summary>
    private int _sortVersion = 0;
    
    /// <summary>Incremented when columns are added/removed/reordered/hidden</summary>
    private int _columnsVersion = 0;
    
    /// <summary>Incremented when grouping changes</summary>
    private int _groupVersion = 0;
    
    // Cache version tracking fields - track which content version each cache was built for
    // -1 means "never calculated", otherwise matches the content version it was built for
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
    private int _pinnedLeftOffsetsCacheVersion = -1;
    private Dictionary<string, int>? _pinnedLeftOffsetsCache;
    private readonly Dictionary<string, List<string?>> _distinctValuesCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, HashSet<string>> _distinctNormalizedValuesCache = new(StringComparer.Ordinal);
    // Maps display label -> raw filter key (for numeric/date/enum columns)
    private readonly Dictionary<string, Dictionary<string, string>> _displayToRawKeyCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _aggregateCache = new(StringComparer.Ordinal);
    private bool _selectionChangedPending;

    // Virtualization state fields
    private int _scrollTop = 0;
    private int _viewportHeight = 0;
    private int _estimatedRowHeight = 40;
    private int _virtualizationBufferRows = 10;
    private const int VirtualizationThreshold = 1000;
    private const int MinColumnWidth = 40;

    // Debounce state for text inputs (search + quick filters)
    private CancellationTokenSource? _searchDebounceCts;
    private readonly Dictionary<string, CancellationTokenSource> _quickFilterDebounceCts = new(StringComparer.Ordinal);
    private const int InputDebounceMs = 250;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;
    [Inject] private ILogger<SgDataGrid<TItem>> Logger { get; set; } = default!;

    private ElementReference _gridRootRef;
    private ElementReference _chooserRef;
    private ElementReference _exportRef;
    private readonly string _gridId = $"sg_{Guid.NewGuid():N}";

    /// <summary>
    /// Gets or sets the collection of items to display in the grid.
    /// </summary>
    [Parameter] public IEnumerable<TItem>? Items { get; set; }
    
    /// <summary>
    /// Gets or sets the child content containing <see cref="SgDataGridColumn{TItem}"/> definitions.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    
    /// <summary>
    /// Gets or sets the template for rendering row details.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? DetailTemplate { get; set; }

    /// <summary>
    /// Gets or sets the template for the toolbar content.
    /// </summary>
    [Parameter] public RenderFragment? ToolbarContent { get; set; }

    /// <summary>
    /// Gets or sets the template displayed when the grid has no data.
    /// </summary>
    [Parameter] public RenderFragment? EmptyDataTemplate { get; set; }
    
    /// <summary>
    /// Gets or sets the grid title displayed in the toolbar.
    /// </summary>
    [Parameter] public string? Title { get; set; }
    
    /// <summary>
    /// Gets or sets additional CSS class names to apply to the grid container.
    /// </summary>
    [Parameter] public string? CssClass { get; set; }
    
    /// <summary>
    /// Gets or sets the text displayed when the grid has no data.
    /// </summary>
    [Parameter] public string? EmptyText { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show borders between columns.
    /// </summary>
    [Parameter] public bool ShowColumnBorders { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show the search box in the toolbar. Default is true.
    /// </summary>
    [Parameter] public bool ShowSearch { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to show quick filter inputs below column headers.
    /// </summary>
    [Parameter] public bool ShowQuickFilters { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show the column chooser button in the toolbar. Default is true.
    /// </summary>
    [Parameter] public bool ShowColumnChooser { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether to show the CSV export button in the toolbar.
    /// </summary>
    [Parameter] public bool ShowExportCsv { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show the Excel export button in the toolbar.
    /// </summary>
    [Parameter] public bool ShowExportExcel { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show the status bar with row count and selection info.
    /// </summary>
    [Parameter] public bool ShowStatusBar { get; set; }
    
    /// <summary>
    /// Gets or sets whether to allow auto-fitting column widths to content.
    /// </summary>
    [Parameter] public bool AllowAutoFit { get; set; }
    
    /// <summary>
    /// Gets or sets whether to allow selecting multiple rows with checkboxes.
    /// </summary>
    [Parameter] public bool AllowMultiSelect { get; set; }
    
    /// <summary>
    /// Gets or sets whether to allow inline editing of cells.
    /// </summary>
    [Parameter] public bool AllowEdit { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show delete buttons for rows.
    /// </summary>
    [Parameter] public bool AllowDelete { get; set; }
    
    /// <summary>
    /// Gets or sets whether the grid should take full width of its container.
    /// </summary>
    [Parameter] public bool FullWidth { get; set; }
    
    /// <summary>
    /// Gets or sets whether to automatically generate columns from TItem properties.
    /// </summary>
    [Parameter] public bool AutoGenerateColumns { get; set; }
    
    /// <summary>
    /// Gets or sets whether to enable pagination. Default is true.
    /// </summary>
    [Parameter] public bool EnablePaging { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the number of rows per page. Default is 25.
    /// </summary>
    [Parameter] public int PageSize { get; set; } = 25;
    
    /// <summary>
    /// Gets or sets the available page size options. Default is [10, 25, 50, 100].
    /// </summary>
    [Parameter] public IReadOnlyList<int> PageSizeOptions { get; set; } = new[] { 10, 25, 50, 100 };
    
    /// <summary>
    /// Gets or sets where to display row details.
    /// Supported values: Inline, Drawer, Window. Default is Inline.
    /// </summary>
    [Parameter] public DetailPlacement DetailPlacement { get; set; } = DetailPlacement.Inline;
    
    /// <summary>
    /// Gets or sets the title for the detail drawer when <see cref="DetailPlacement"/> is Drawer.
    /// </summary>
    [Parameter] public string? DetailDrawerTitle { get; set; }
    
    /// <summary>
    /// Gets or sets the title for the detail window when <see cref="DetailPlacement"/> is Window.
    /// </summary>
    [Parameter] public string? DetailWindowTitle { get; set; }
    
    /// <summary>
    /// Gets or sets the width of the detail window in pixels. Default is 640.
    /// </summary>
    [Parameter] public int DetailWindowWidth { get; set; } = 640;
    
    /// <summary>
    /// Gets or sets the height of the detail window in pixels. Default is 360.
    /// </summary>
    [Parameter] public int DetailWindowHeight { get; set; } = 360;
    
    /// <summary>
    /// Gets or sets the factory function for creating new items when adding rows.
    /// </summary>
    [Parameter] public Func<TItem>? CreateItemFactory { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when a row is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> RowClicked { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when a row is double-clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> RowDoubleClicked { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when the selected items collection changes.
    /// </summary>
    [Parameter] public EventCallback<IReadOnlyCollection<TItem>> SelectedItemsChanged { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when a row is deleted.
    /// </summary>
    [Parameter] public EventCallback<TItem> RowDeleted { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when a new row is created.
    /// </summary>
    [Parameter] public EventCallback<TItem> RowCreated { get; set; }
    
    /// <summary>
    /// Gets or sets the callback invoked when a row context menu is requested.
    /// </summary>
    [Parameter] public EventCallback<SgDataGridContextMenuEventArgs<TItem>> OnRowContextMenu { get; set; }
    
    /// <summary>
    /// Gets or sets whether the grid is in loading state (shows loading indicator).
    /// </summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>
    /// Gets or sets whether to use modal dialog for add/edit operations instead of inline editing.
    /// </summary>
    [Parameter] public bool UseModalForEdit { get; set; }

    /// <summary>
    /// Gets or sets the width of the edit modal in pixels. Default is 500.
    /// </summary>
    [Parameter] public int EditModalWidth { get; set; } = 500;

    /// <summary>
    /// Gets or sets the title for the edit modal when adding a new item.
    /// </summary>
    [Parameter] public string? EditModalAddTitle { get; set; }

    /// <summary>
    /// Gets or sets the title for the edit modal when editing an existing item.
    /// </summary>
    [Parameter] public string? EditModalEditTitle { get; set; }

    /// <summary>
    /// When true, shows a "Bulk Edit" button in the toolbar when rows are selected.
    /// Allows editing multiple rows at once via a modal dialog.
    /// </summary>
    [Parameter] public bool AllowBulkEdit { get; set; }

    /// <summary>
    /// Callback invoked when the user confirms a bulk edit.
    /// Receives the list of affected items and a dictionary of column key → new value.
    /// </summary>
    [Parameter] public EventCallback<SgBulkEditEventArgs<TItem>> OnBulkSave { get; set; }

    public HashSet<TItem> SelectedItems { get; } = new();

    internal IReadOnlyList<string> GroupByKeys => _groupByKeys;
    internal IReadOnlyList<PersistedSortRule> SortRules => _sort;
    internal List<FilterRule> PendingRules => _pendingRules;
    internal bool PendingRulesAnd => _pendingRulesAnd;
    internal bool HasUndoDelete => _deletedRows.Count > 0;
    
    private string _editModalWidth => $"{EditModalWidth}px";
    internal bool HasActiveSort => _sort.Count > 0;
    internal bool HasActiveFilters => !string.IsNullOrWhiteSpace(_search) || _filters.Count > 0 || _conditionFilters.Count > 0 || _quickFilters.Count > 0;
    internal bool CanCreateInlineItem => CreateItemFactory is not null || typeof(TItem).GetConstructor(Type.EmptyTypes) is not null;
    internal int CurrentPage => _currentPage;
    internal int TotalFilteredCount => GetFilteredRows().Count;
    internal int TotalPages => !EnablePaging ? 1 : Math.Max(1, (int)Math.Ceiling(TotalFilteredCount / (double)Math.Max(1, PageSize)));
    internal string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localizer["DataGrid_EmptyText"] : EmptyText!;
    internal string EffectiveDetailDrawerTitle => string.IsNullOrWhiteSpace(DetailDrawerTitle) ? Localizer["DataGrid_DetailDrawerTitle"] : DetailDrawerTitle!;
    internal string EffectiveDetailWindowTitle => string.IsNullOrWhiteSpace(DetailWindowTitle) ? Localizer["DataGrid_DetailWindowTitle"] : DetailWindowTitle!;
    internal bool SelectionEnabled => AllowMultiSelect || SelectedItemsChanged.HasDelegate;
    internal int ColumnSpan
    {
        get
        {
            if (_columnSpanCacheVersion == _columnsVersion)
                return _columnSpanCacheValue;
            
            _columnSpanCacheValue = VisibleColumns.Count + (DetailTemplate is not null && DetailPlacement == DetailPlacement.Inline ? 1 : 0) + (SelectionEnabled ? 1 : 0) + (AllowEdit || AllowDelete ? 1 : 0);
            _columnSpanCacheVersion = _columnsVersion;
            return _columnSpanCacheValue;
        }
    }
    internal bool AllFilteredSelected
    {
        get
        {
            var selectedCount = SelectedItems.Count;
            var currentKey = (_itemsVersion, _filterVersion, _selectionVersion, selectedCount);
            if (_allFilteredSelectedCacheKey == currentKey)
            {
                return _allFilteredSelectedCacheValue;
            }

            var rows = GetFilteredRows();
            _allFilteredSelectedCacheValue = rows.Count > 0 && rows.All(SelectedItems.Contains);
            _allFilteredSelectedCacheKey = currentKey;
            return _allFilteredSelectedCacheValue;
        }
    }

    internal IReadOnlyList<SgDataGridColumn<TItem>> VisibleColumns
    {
        get
        {
            EnsureAutoGeneratedColumns();
            // Check if cache is valid for current columns version
            if (_visibleColumnsCacheVersion == _columnsVersion && _visibleColumnsCache is not null)
                return _visibleColumnsCache;

            _visibleColumnsCache = GetOrderedColumns()
                .Where(c => !_hiddenColumns.Contains(c.Key))
                .ToList();
            _visibleColumnsCacheVersion = _columnsVersion;
            return _visibleColumnsCache;
        }
    }

    private int _prevItemsCount = -1;

    /// <summary>
    /// Gets or sets the key for persisting grid state (filters, sort, columns) to localStorage.
    /// </summary>
    [Parameter] public string? PersistStateKey { get; set; }
    
    /// <summary>
    /// Gets or sets whether to show the add new row button in the toolbar.
    /// </summary>
    [Parameter] public bool AllowAdd { get; set; }

    protected override void OnParametersSet()
    {
        PageSize = Math.Max(1, PageSize);
        _estimatedRowHeight = EstimatedRowHeight > 0 ? EstimatedRowHeight : 32;

        var currentCount = Items is ICollection col ? col.Count : -1;
        if (!ReferenceEquals(_prevItems, Items) || currentCount != _prevItemsCount)
        {
            _prevItems = Items;
            _prevItemsCount = currentCount;
            _itemsVersion++;
        }

        _currentPage = Math.Clamp(_currentPage, 1, TotalPages);
    }

    private async Task LoadStateAsync()
    {
        if (_module is null || string.IsNullOrEmpty(PersistStateKey)) return;
        try
        {
            var stateJson = await _module.InvokeAsync<string?>("getLocalStorage", PersistStateKey);
            if (!string.IsNullOrEmpty(stateJson))
            {
                await ImportStateJsonAsync(stateJson);
            }
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgDataGrid: failed to load persisted state for key {Key}", PersistStateKey);
        }
    }

    private async Task SaveStateAsync()
    {
        if (_module is null || string.IsNullOrEmpty(PersistStateKey)) return;
        try
        {
            var stateJson = ExportStateJson();
            await _module.InvokeVoidAsync("setLocalStorage", PersistStateKey, stateJson);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgDataGrid: failed to persist state for key {Key}", PersistStateKey);
        }
    }

    protected override bool ShouldRender()
    {
        return true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposing)
            return;

        if (firstRender)
        {
            try
            {
                var module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
                if (_disposing)
                {
                    // Component was disposed while we awaited the import — clean up locally.
                    try { await module.DisposeAsync(); } catch { }
                    return;
                }
                _module = module;
                _selfRef = DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("init", _selfRef, _gridRootRef);

                if (_disposing)
                    return;

                // Initialize virtualization for large datasets
                if (ShouldUseVirtualization())
                {
                    await _module.InvokeVoidAsync("initDataGridVirtualization", _selfRef, _gridRootRef);
                    if (_disposing) return;
                }

                if (!string.IsNullOrEmpty(PersistStateKey))
                {
                    await LoadStateAsync();
                    if (_disposing) return;
                    StateHasChanged();
                }
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "SgDataGrid: failed to initialize JS interop module");
            }
        }
    }

    internal void RegisterColumn(SgDataGridColumn<TItem> column)
    {
        if (_columns.Contains(column))
            return;

        if (_columns.Any(c => !c.IsSynthetic))
        {
            _columns.RemoveAll(c => c.IsSynthetic);
        }

        _columns.Add(column);
        RebuildColumnLookup();
        if (!_columnOrder.ContainsKey(column.Key))
            _columnOrder[column.Key] = _columnOrder.Count;
        if (column.Hidden)
            _hiddenColumns.Add(column.Key);
        _visibleColumnsCache = null;
        _columnsVersion++;  // Increment columns version when column is registered
    }

    internal void UnregisterColumn(SgDataGridColumn<TItem> column)
    {
        _columns.Remove(column);
        RebuildColumnLookup();
        _columnsVersion++;  // Increment columns version when column is unregistered
    }

    internal void InitGroupBy(string key)
    {
        if (!_groupByKeys.Contains(key))
            _groupByKeys.Add(key);
    }

    [JSInvokable]
    public async Task AutoSizeColumnAsync(string key)
    {
        var col = GetColumnByKey(key);
        if (col is null) return;

        var rows = GetVisibleRows();
        var sampleRows = rows.Take(100).ToList(); // Sample first 100 rows for performance
        var values = sampleRows.Select(r => col.GetDisplay(r)).ToList();

        var widths = await JS.InvokeAsync<Dictionary<string, int>>("measureColumnWidths", 
            new[] { new { key = col.Key, title = col.Title, values } }, _gridId);

        if (widths.TryGetValue(key, out var width))
        {
            await SetColumnWidthAsync(key, width);
        }
    }

    /// <summary>
    /// Gets or sets whether to enable row virtualization for large datasets. Default is false.
    /// </summary>
    [Parameter] public bool EnableVirtualization { get; set; }
    
    /// <summary>
    /// Gets or sets the estimated height of each row in pixels. Default is 32.
    /// </summary>
    [Parameter] public int EstimatedRowHeight { get; set; } = 32;

    [JSInvokable]
    public async Task OnScrollAsync(int scrollTop, int viewportHeight)
    {
        if (_disposing) return;
        if (_scrollTop == scrollTop && _viewportHeight == viewportHeight)
            return;

        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;
        
        // Invalidate visible rows cache to trigger recalculation if virtualized
        if (ShouldUseVirtualization())
            _visibleRowsCacheItemsVersion = -1;

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task SetColumnWidthAsync(string key, int width)
    {
        if (_disposing) return;
        if (!string.IsNullOrWhiteSpace(key) && width > 0)
            _columnWidths[key] = Math.Max(MinColumnWidth, width);
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task ReorderColumnAsync(string dragKey, string targetKey, bool before)
    {
        if (_disposing) return;
        var ordered = GetOrderedColumns().ToList();
        var drag = ordered.FirstOrDefault(c => c.Key == dragKey);
        var target = ordered.FirstOrDefault(c => c.Key == targetKey);
        if (drag is null || target is null || drag == target)
            return;

        ordered.Remove(drag);
        var targetIndex = ordered.IndexOf(target);
        if (!before)
            targetIndex++;
        ordered.Insert(Math.Clamp(targetIndex, 0, ordered.Count), drag);

        for (var i = 0; i < ordered.Count; i++)
            _columnOrder[ordered[i].Key] = i;

        _columnsVersion++;  // Increment columns version when column order changes
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnRowHeightMeasuredAsync(int measuredHeight)
    {
        if (_disposing)
            return;
        if (measuredHeight > 0 && _estimatedRowHeight != measuredHeight)
        {
            _estimatedRowHeight = measuredHeight;
            _visibleRowsCacheItemsVersion = -1; // Invalidate cache
            await InvokeAsync(StateHasChanged);
        }
    }

    public SgGridState ExportState()
    {
        EnsureAutoGeneratedColumns();

        return new SgGridState
        {
            Search = _search,
            QuickFilters = new Dictionary<string, string>(_quickFilters, StringComparer.Ordinal),
            ValueFilters = _filters.ToDictionary(x => x.Key, x => x.Value.ToList(), StringComparer.Ordinal),
            ConditionFilters = _conditionFilters.ToDictionary(
                x => x.Key,
                x => new PersistedConditionFilter
                {
                    And = x.Value.And,
                    Rules = x.Value.Rules.Select(rule => rule with { }).ToList()
                },
                StringComparer.Ordinal),
            QueryRules = _queryRules.Select(CloneQueryRule).ToList(),
            Sort = _sort.Select(x => new PersistedSortRule { Key = x.Key, Dir = x.Dir }).ToList(),
            HiddenColumns = _hiddenColumns.ToList(),
            ColumnWidths = new Dictionary<string, int>(_columnWidths, StringComparer.Ordinal),
            ColumnOrder = new Dictionary<string, int>(_columnOrder, StringComparer.Ordinal),
            GroupBy = _groupByKeys.ToList(),
            PageSize = PageSize
        };
    }

    public string ExportStateJson() =>
        JsonSerializer.Serialize(ExportState(), StateJsonOptions);

    public async Task<bool> ImportStateAsync(SgGridState? state)
    {
        if (state is null)
            return false;

        EnsureAutoGeneratedColumns();

        _search = state.Search;

        _quickFilters.Clear();
        foreach (var pair in state.QuickFilters)
        {
            if (!string.IsNullOrWhiteSpace(pair.Value))
                _quickFilters[pair.Key] = pair.Value;
        }

        _filters.Clear();
        foreach (var pair in state.ValueFilters)
        {
            _filters[pair.Key] = pair.Value
                .Select(NormalizeFilterValue)
                .ToHashSet(StringComparer.Ordinal);
        }

        _conditionFilters.Clear();
        foreach (var pair in state.ConditionFilters)
        {
            var rules = pair.Value.Rules ?? new List<FilterRule>();
            if (rules.Count > 0)
                _conditionFilters[pair.Key] = new ColumnFilter(rules.Select(x => x with { }), pair.Value.And);
        }

        _queryRules.Clear();
        _queryRules.AddRange((state.QueryRules ?? new List<QueryRule>())
            .Where(IsValidQueryRule)
            .Select(CloneQueryRule));

        _sort.Clear();
        _sort.AddRange(state.Sort.Select(x => new PersistedSortRule { Key = x.Key, Dir = x.Dir }));

        _hiddenColumns.Clear();
        foreach (var key in state.HiddenColumns)
            _hiddenColumns.Add(key);
        _columnsVersion++;

        _columnWidths.Clear();
        foreach (var pair in state.ColumnWidths)
            _columnWidths[pair.Key] = pair.Value;

        _columnOrder.Clear();
        foreach (var pair in state.ColumnOrder)
            _columnOrder[pair.Key] = pair.Value;

        _groupByKeys.Clear();
        foreach (var key in state.GroupBy.Where(k => !string.IsNullOrWhiteSpace(k)))
            _groupByKeys.Add(key);

        _collapsedGroups.Clear();
        PageSize = state.PageSize > 0 ? state.PageSize : PageSize;
        _currentPage = 1;
        _openFilterColumn = null;

        await InvokeAsync(StateHasChanged);
        return true;
    }

    public async Task<bool> ImportStateJsonAsync(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var state = JsonSerializer.Deserialize<SgGridState>(json, StateJsonOptions);
            return await ImportStateAsync(state);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public async Task ResetStateAsync()
    {
        EnsureAutoGeneratedColumns();

        _search = null;
        _quickFilters.Clear();
        _filters.Clear();
        _conditionFilters.Clear();
        _queryRules.Clear();
        _sort.Clear();
        _openFilterColumn = null;
        _collapsedGroups.Clear();
        _columnWidths.Clear();
        _groupByKeys.Clear();
        _hiddenColumns.Clear();

        foreach (var col in _columns)
        {
            if (col.Hidden)
                _hiddenColumns.Add(col.Key);
            if (col.GroupBy && !_groupByKeys.Contains(col.Key))
                _groupByKeys.Add(col.Key);
        }
        _columnsVersion++;

        _columnOrder.Clear();
        for (var i = 0; i < _columns.Count; i++)
            _columnOrder[_columns[i].Key] = i;

        _currentPage = 1;
        await InvokeAsync(StateHasChanged);
    }

    public IReadOnlyList<QueryField> GetQueryFields()
    {
        EnsureAutoGeneratedColumns();

        return GetOrderedColumns()
            .Select(col => new QueryField
            {
                Name = col.Key,
                Label = col.Title,
                Type = ResolveColumnType(col)
            })
            .ToList();
    }

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

    public void Refresh()
    {
        EnsureAutoGeneratedColumns();
        _itemsVersion++;
        _prevItemsCount = Items is ICollection ic ? ic.Count : -1;
        InvalidateComputedRowsCache();
        _currentPage = Math.Clamp(_currentPage, 1, TotalPages);
        _ = InvokeAsync(StateHasChanged).ContinueWith(
            t => Logger.LogWarning(t.Exception, "SgDataGrid.Refresh: StateHasChanged failed"),
            System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
    }

    private void EnsureAutoGeneratedColumns()
    {
        if (!AutoGenerateColumns || _columns.Count > 0 || _isSyntheticColumnsInitialized)
            return;

        var props = typeof(TItem)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
            .Select(p => new
            {
                Property = p,
                Display = p.GetCustomAttribute<DisplayAttribute>(),
                Format = p.GetCustomAttribute<DisplayFormatAttribute>()
            })
            .OrderBy(x => x.Display?.GetOrder() ?? int.MaxValue)
            .ThenBy(x => x.Property.MetadataToken)
            .ToList();

        foreach (var item in props)
        {
            var property = item.Property;
            var column = SgDataGridColumn<TItem>.CreateSynthetic(
                key: property.Name,
                title: item.Display?.GetName() ?? property.Name,
                value: BuildPropertyAccessor(property),
                valueType: Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                format: item.Format?.DataFormatString is { Length: > 0 } fmt ? NormalizeFormat(fmt) : null);

            _columns.Add(column);
            _columnOrder[column.Key] = _columnOrder.Count;
        }

        RebuildColumnLookup();
        _isSyntheticColumnsInitialized = true;
    }

    private static Func<TItem, object?> BuildPropertyAccessor(PropertyInfo property) =>
        item => property.GetValue(item);

    private static string? NormalizeFormat(string format)
    {
        // Return the format as-is to support both simple formats (N2, C2)
        // and composite formats ({0:N2} ₽)
        return string.IsNullOrWhiteSpace(format) ? null : format;
    }

    private IReadOnlyList<SgDataGridColumn<TItem>> GetOrderedColumns()
    {
        // Check if cache is valid for current columns version
        if (_orderedColumnsCacheColumnsVersion == _columnsVersion && _orderedColumnsCache is not null)
            return _orderedColumnsCache;

        _orderedColumnsCache = _columns
            .Select((column, index) => (column, index))
            .OrderBy(x => _columnOrder.TryGetValue(x.column.Key, out var order) ? order : int.MaxValue)
            .ThenBy(x => x.index)
            .Select(x => x.column)
            .ToList();

        _orderedColumnsCacheColumnsVersion = _columnsVersion;
        return _orderedColumnsCache;
    }

    private List<TItem> GetVisibleRows()
    {
        // Check if cache is valid for current versions
        if (_visibleRowsCacheItemsVersion == _itemsVersion &&
            _visibleRowsCacheFilterVersion == _filterVersion &&
            _visibleRowsCacheSortVersion == _sortVersion &&
            _visibleRowsCacheColumnsVersion == _columnsVersion &&
            _visibleRowsCache is not null)
            return _visibleRowsCache;

        var rows = GetFilteredSortedRows();
        
        // Use virtualization for large datasets without pagination
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
        
        // Original pagination logic
        if (!EnablePaging)
        {
            _visibleRowsCache = rows;
            _visibleRowsCacheItemsVersion = _itemsVersion;
            _visibleRowsCacheFilterVersion = _filterVersion;
            _visibleRowsCacheSortVersion = _sortVersion;
            _visibleRowsCacheColumnsVersion = _columnsVersion;
            return rows;
        }

        var pageSize = Math.Max(1, PageSize);
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

    private bool ShouldUseVirtualization()
    {
        if (!EnableVirtualization)
            return false;

        // Don't virtualize if grouping is active - grouping has complex rendering
        if (_groupByKeys.Count > 0)
            return false;

        // Enable virtualization for large datasets (>= threshold)
        var totalRows = GetFilteredSortedRows().Count;
        return totalRows >= VirtualizationThreshold;
    }

    private (int startIndex, int endIndex, int topPadding, int bottomPadding) CalculateVirtualWindow()
    {
        var totalRows = GetFilteredSortedRows().Count;

        // Handle edge cases: no rows or no viewport height
        if (totalRows == 0 || _viewportHeight == 0)
        {
            return (0, 0, 0, 0);
        }

        // Calculate first visible row index based on scroll position
        var firstVisibleRow = (int)Math.Floor((double)_scrollTop / _estimatedRowHeight);

        // Calculate how many rows fit in the viewport
        var rowsInViewport = (int)Math.Ceiling((double)_viewportHeight / _estimatedRowHeight);

        // Add buffer rows above and below the viewport
        var startIndex = Math.Max(0, firstVisibleRow - _virtualizationBufferRows);
        var endIndex = Math.Min(totalRows - 1, firstVisibleRow + rowsInViewport + _virtualizationBufferRows);

        // Calculate padding to maintain scroll position
        var topPadding = startIndex * _estimatedRowHeight;
        var bottomPadding = (totalRows - 1 - endIndex) * _estimatedRowHeight;

        return (startIndex, endIndex, topPadding, bottomPadding);
    }

    private List<TItem> GetFilteredSortedRows()
    {
        // Check if cache is valid for current filter and sort versions
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

    private List<TItem> GetFilteredRows()
    {
        // Check if cache is valid for current items and filter versions
        if (_filteredRowsCacheItemsVersion == _itemsVersion &&
            _filteredRowsCacheFilterVersion == _filterVersion &&
            _filteredRowsCache is not null)
            return _filteredRowsCache;

        EnsureAutoGeneratedColumns();
        
        // Direct iteration without ToList() to avoid unnecessary copy
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

        // Direct iteration over Items without ToList() copy
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

        _filteredRowsCache = result;
        _filteredRowsCacheItemsVersion = _itemsVersion;
        _filteredRowsCacheFilterVersion = _filterVersion;
        return _filteredRowsCache;
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

            // If ValueType not set, try to infer from actual value
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
                // <input type="date"> returns "yyyy-MM-dd"
                // <input type="datetime-local"> returns "yyyy-MM-ddTHH:mm"
                var rawValue = col.GetValue(item);

                DateTime? itemDate = rawValue switch
                {
                    DateTime dt => dt,
                    DateTimeOffset dto => dto.DateTime,
                    DateOnly d => d.ToDateTime(TimeOnly.MinValue),
                    _ => null
                };

                if (itemDate is null) return false;

                // Try datetime-local ISO format first ("yyyy-MM-ddTHH:mm")
                if (DateTime.TryParseExact(val, "yyyy-MM-ddTHH:mm",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDateTime))
                {
                    // ShowTime=true: compare to the minute
                    var itemTrunc = new DateTime(itemDate.Value.Year, itemDate.Value.Month, itemDate.Value.Day,
                        itemDate.Value.Hour, itemDate.Value.Minute, 0);
                    if (itemTrunc != isoDateTime) return false;
                }
                // Try date-only ISO format ("yyyy-MM-dd")
                else if (DateTime.TryParseExact(val, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDate))
                {
                    if (itemDate.Value.Date != isoDate.Date) return false;
                }
                // Fallback: current culture parse
                else if (DateTime.TryParse(val, CultureInfo.CurrentCulture,
                        System.Globalization.DateTimeStyles.None, out var locDate))
                {
                    // If filter has time component, compare to the minute; otherwise date only
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
                // Match against display label (from [Display]/[Description]) or enum name
                var rawValue = col.GetValue(item);
                var enumDisplay = col.GetDisplay(item);
                // Also try matching against the enum member name directly
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

    internal void InvalidateComputedRowsCache()
    {
        // This method is called when we need to invalidate caches due to content changes
        // Individual version increments happen at mutation points (OnSearchInput, ApplyFilterAsync, etc.)
        // This method is kept for backward compatibility but most invalidation happens at mutation points
        
        // Invalidate caches that depend on filter/sort/columns/groups
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

        // For enum columns, use the display label for text-based operators
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
            QueryFieldOperator.GreaterThanOrEqual => CompareQueryValue(rawValue, rule.Value, targetType) >= 0,
            QueryFieldOperator.LessThan => CompareQueryValue(rawValue, rule.Value, targetType) < 0,
            QueryFieldOperator.LessThanOrEqual => CompareQueryValue(rawValue, rule.Value, targetType) <= 0,
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

        // For enum columns, use the display label (from [Display]/[Description]) for text ops
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

        // Enum: use integer value for numeric ordering (GreaterThan/LessThan work correctly)
        if (target.IsEnum)
        {
            if (value is Enum)
                return Convert.ToInt32(value);
            // If value is already a string (e.g. from display), try to parse
            if (value is string s && Enum.TryParse(target, s, true, out var parsed))
                return Convert.ToInt32(parsed);
            return value.ToString();
        }

        // DateTime/DateTimeOffset: strip time component so date-only comparisons work
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

        // Normalize value to target type for proper numeric comparison
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
                    // If conversion fails, try converting to decimal first as intermediate
                    try
                    {
                        var asDecimal = Convert.ToDecimal(value, CultureInfo.CurrentCulture);
                        return Convert.ChangeType(asDecimal, target, CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        // Fall through to return original value
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

            // Clean numeric strings: remove thousand separators (spaces), currency symbols, etc.
            // For formats like "92 731,00 ₽" -> "92731,00"
            if (IsNumericType(target))
            {
                text = CleanNumericString(text);
                // Normalize decimal separator to current culture format
                // In Russian locale, decimal separator is comma
                var expectedSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];
                // If we have a dot in the cleaned string but expecting comma, replace it
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
                // Handle datetime-local ISO format "yyyy-MM-ddTHH:mm" from <input type="datetime-local">
                if (DateTime.TryParseExact(text, "yyyy-MM-ddTHH:mm",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDateTime))
                    return isoDateTime;
                // Handle date-only ISO format "yyyy-MM-dd" from <input type="date">
                if (DateTime.TryParseExact(text, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var isoDate))
                    return isoDate.Date;
                // Fallback: current culture
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
                // Try by name first (case-insensitive), then by int value
                if (Enum.TryParse(target, text, true, out var enumVal))
                    return enumVal;
                // Try matching display label via SgEnumHelper
                var items = SgEnumHelper.GetItems(target);
                var match = items.FirstOrDefault(ei =>
                    ei.Label.Equals(text, StringComparison.CurrentCultureIgnoreCase) ||
                    ei.Name.Equals(text, StringComparison.CurrentCultureIgnoreCase));
                if (match is not null)
                    return Enum.Parse(target, match.Name, true);
                return text; // fallback to string comparison
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

    private SgDataGridColumn<TItem>? GetColumnByKey(string key)
    {
        if (_columnLookup.Count != _columns.Count)
            RebuildColumnLookup();

        return _columnLookup.TryGetValue(key, out var col) ? col : null;
    }

    private string GetColumnFilterType(string key)
    {
        var col = GetColumnByKey(key);
        if (col is null) return "string";

        // Prefer explicit ValueType, then sample first non-null value
        var type = col.ValueType;
        if (type is null)
        {
            // Try to infer from data
            var items = Items?.Take(20) ?? Enumerable.Empty<TItem>();
            foreach (var item in items)
            {
                var v = col.GetValue(item);
                if (v is null) continue;
                type = Nullable.GetUnderlyingType(v.GetType()) ?? v.GetType();
                break;
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

    private void OnSearchInput(ChangeEventArgs args)
    {
        _search = args.Value?.ToString();
        _filterVersion++;
        _currentPage = 1;
        InvalidateComputedRowsCache();
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
                // SaveStateAsync already swallows JS errors, but guard the await itself.
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

        _sortVersion++;  // Increment sort version when sort changes
        _currentPage = 1;
        await SaveStateAsync();
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
        _sortVersion++;  // Increment sort version when sort changes
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearSortAsync()
    {
        _sort.Clear();
        _sortVersion++;  // Increment sort version when sort changes
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleFilterMenuAsync(string key)
    {
        if (_openFilterColumn == key)
        {
            _openFilterColumn = null;
            return;
        }

        _openFilterColumn = key;
        _pendingFilterKey = key;
        _filterMenuSearchText = string.Empty;
        _pendingSelectedValues = _filters.TryGetValue(key, out var current)
            ? current.ToHashSet(StringComparer.Ordinal)
            : GetDistinctNormalizedValuesForColumn(key);

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

        await InvokeAsync(StateHasChanged);
    }

    private Task CloseFilterMenuAsync()
    {
        _openFilterColumn = null;
        return Task.CompletedTask;
    }

    private async Task SetPendingRulesAndAsync(bool and)
    {
        _pendingRulesAnd = and;
        // No StateHasChanged - will be called when filter is applied
    }

    private Task SetPendingRuleConditionAsync(int index, FilterCondition condition)
    {
        if (index < 0 || index >= _pendingRules.Count)
            return Task.CompletedTask;

        _pendingRules[index] = _pendingRules[index] with { Condition = condition };
        // No StateHasChanged during typing
        return Task.CompletedTask;
    }

    private Task SetPendingRuleValueAsync(int index, string? value)
    {
        if (index < 0 || index >= _pendingRules.Count)
            return Task.CompletedTask;

        _pendingRules[index] = _pendingRules[index] with { Value = value };
        // No StateHasChanged during typing - prevents UI lag
        return Task.CompletedTask;
    }

    private Task RemovePendingRuleAsync(int index)
    {
        if (_pendingRules.Count <= 1 || index < 0 || index >= _pendingRules.Count)
            return Task.CompletedTask;

        _pendingRules.RemoveAt(index);
        // No StateHasChanged - will be called when filter is applied
        return Task.CompletedTask;
    }

    private Task AddPendingRuleAsync()
    {
        _pendingRules.Add(new FilterRule());
        return Task.CompletedTask;
    }

    private async Task ApplyConditionFilterAsync(string key)
    {
        ApplyPendingConditionFilter(key);
        _filterVersion++;  // Increment filter version when condition filter changes
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
        var distinct = GetDistinctNormalizedValuesForColumn(key);
        return distinct.SetEquals(_pendingSelectedValues);
    }

    private async Task TogglePendingAllAsync(string key, bool selected)
    {
        _pendingSelectedValues = selected
            ? GetDistinctNormalizedValuesForColumn(key)
            : new HashSet<string>(StringComparer.Ordinal);
        await InvokeAsync(StateHasChanged);
    }

    private List<string?> GetDistinctValuesForColumn(string key)
    {
        if (_distinctValuesCacheItemsVersion != _itemsVersion)
        {
            _distinctValuesCache.Clear();
            _distinctNormalizedValuesCache.Clear();
            _displayToRawKeyCache.Clear();
            _distinctValuesCacheItemsVersion = _itemsVersion;
        }

        if (_distinctValuesCache.TryGetValue(key, out var cachedValues))
            return cachedValues;

        var col = GetColumnByKey(key);
        if (col is null)
            return new List<string?>();

        var filterType = GetColumnFilterType(key);
        var useRaw = filterType == "number" || filterType == "date" || filterType == "datetime" || filterType == "enum";

        // displayToRaw: rawKey -> displayLabel (for showing in checkboxes)
        var displayToRaw = new Dictionary<string, string>(StringComparer.Ordinal);

        var seen = new HashSet<string?>(StringComparer.Ordinal);
        var values = new List<string?>();

        if (Items is not null)
        {
            foreach (var item in Items)
            {
                string rawKey;
                string displayLabel;

                if (useRaw)
                {
                    var raw = col.GetValue(item);
                    // Build rawKey from the actual value — no formatting
                    rawKey = raw switch
                    {
                        null => string.Empty,
                        DateTime dt => dt.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        DateTimeOffset dto => dto.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        DateOnly d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        Enum e => e.ToString(),          // enum name
                        _ => raw.ToString() ?? string.Empty  // numeric: plain number string
                    };
                    // Display label: formatted for UI
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
        }

        values.Sort((a, b) =>
        {
            if (filterType == "number")
            {
                // Sort numerically by rawKey (plain number string)
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

    /// <summary>Returns the display label for a rawKey in the filter checkbox list.</summary>
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
        if (_distinctValuesCacheItemsVersion == _itemsVersion &&
            _distinctNormalizedValuesCache.TryGetValue(key, out var cachedSet))
        {
            return new HashSet<string>(cachedSet, StringComparer.Ordinal);
        }

        var values = GetDistinctValuesForColumn(key);
        var normalized = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < values.Count; i++)
            normalized.Add(NormalizeFilterValue(values[i]));

        _distinctNormalizedValuesCache[key] = normalized;
        return new HashSet<string>(normalized, StringComparer.Ordinal);
    }

    private bool IsPendingValueSelected(string? value) =>
        _pendingSelectedValues.Contains(NormalizeFilterValue(value));

    private Task TogglePendingValueAsync(string? value, bool selected)
    {
        var normalized = NormalizeFilterValue(value);
        if (selected)
            _pendingSelectedValues.Add(normalized);
        else
            _pendingSelectedValues.Remove(normalized);

        // No StateHasChanged - will be called when filter is applied
        return Task.CompletedTask;
    }

    private Task ClearPendingAsync(string key)
    {
        _pendingSelectedValues = GetDistinctNormalizedValuesForColumn(key);
        _pendingRules = [new()];
        _pendingRulesAnd = true;
        // No StateHasChanged - will be called when filter is applied
        return Task.CompletedTask;
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
        _filterVersion++;  // Increment filter version when filter changes
        _openFilterColumn = null;
        _currentPage = 1;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task ClearAllFiltersAsync()
    {
        _search = null;
        _quickFilters.Clear();
        _filters.Clear();
        _conditionFilters.Clear();
        _filterVersion++;  // Increment filter version when filters change
        _openFilterColumn = null;
        _currentPage = 1;
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    // ── Bulk edit ─────────────────────────────────────────────────────────────

    private void OpenBulkEditPicker()
    {
        _bulkEditPickerOpen = !_bulkEditPickerOpen;
    }

    private void ToggleBulkEditColumn(string key, bool selected)
    {
        if (selected) _bulkEditSelectedColumns.Add(key);
        else _bulkEditSelectedColumns.Remove(key);
    }

    private void OpenBulkEditModal()
    {
        if (_bulkEditSelectedColumns.Count == 0) return;
        _bulkEditValues.Clear();
        foreach (var key in _bulkEditSelectedColumns)
            _bulkEditValues[key] = null;
        _bulkEditPickerOpen = false;
        _bulkEditModalOpen = true;
        StateHasChanged();
    }

    private void CloseBulkEditModal()
    {
        _bulkEditModalOpen = false;
        StateHasChanged();
    }

    private async Task ConfirmBulkEditAsync()
    {
        if (!OnBulkSave.HasDelegate) return;

        var items = SelectedItems.ToList();
        var changes = new Dictionary<string, string?>(
            _bulkEditValues, StringComparer.Ordinal);

        await OnBulkSave.InvokeAsync(new SgBulkEditEventArgs<TItem>
        {
            Items = items,
            Changes = changes
        });

        _bulkEditModalOpen = false;
        _bulkEditSelectedColumns.Clear();
        _bulkEditValues.Clear();
        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Returns editable columns for bulk edit (Editable=true and has Value selector).</summary>
    private List<SgDataGridColumn<TItem>> GetBulkEditableColumns()
        => _columns.Where(c => c.Editable && c.Value is not null && !c.Hidden).ToList();

    private bool IsNumericColumn(string key)
    {
        var col = GetColumnByKey(key);
        if (col?.ValueType is not null)
        {
            var type = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
            return IsNumericType(type);
        }

        // Auto-detect numeric column by sampling first few non-null values
        var values = Items?.Take(20).Select(item => col?.GetValue(item)) ?? Enumerable.Empty<object?>();
        var numericCount = values
            .Where(v => v is not null)
            .Take(10)
            .Count(IsNumericValue);
        
        return numericCount >= 3; // If 3+ of first 10 values are numeric, treat as numeric column
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

    /// <summary>Returns enum items for a column, or empty list if not an enum column.</summary>
    private List<SgEnumItem> GetColumnEnumItems(string key)
    {
        var col = GetColumnByKey(key);
        if (col is null) return new();
        var type = col.ValueType;
        if (type is null)
        {
            var sample = Items?.Take(20)
                .Select(i => col.GetValue(i))
                .FirstOrDefault(v => v is not null);
            if (sample is not null)
                type = Nullable.GetUnderlyingType(sample.GetType()) ?? sample.GetType();
        }
        if (type is null) return new();
        type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsEnum ? SgEnumHelper.GetItems(type) : new();
    }

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

        // Remove thousand separators (spaces, commas depending on culture) and currency symbols
        // For "92 731,00 ₽" -> "92731,00" or for "92,731.00" -> "92731.00"
        // Support both comma and dot as decimal separators since input can be in any format
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

        // Try to infer type from the actual value
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
                if (node.Children.Count > 0)
                    Traverse(node.Children);
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

        _groupVersion++;  // Increment group version when grouping changes
        _collapsedGroups.Clear();
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
        // Check if cache is valid for current items and group versions
        if (_groupTreeCacheItemsVersion == _itemsVersion &&
            _groupTreeCacheGroupVersion == _groupVersion &&
            _groupTreeCache is not null)
            return _groupTreeCache;

        _groupTreeCache = BuildGroupLevel(GetFilteredSortedRows(), 0, string.Empty);
        _groupTreeCacheItemsVersion = _itemsVersion;
        _groupTreeCacheGroupVersion = _groupVersion;
        return _groupTreeCache;
    }

    /// <summary>
    /// Gets the group tree cache, using content-based version validation.
    /// Cache is invalidated when items or grouping changes.
    /// </summary>
    private List<GroupNode> GetGroupTree()
    {
        return BuildGroupTree();
    }

    private List<GroupNode> BuildGroupLevel(List<TItem> rows, int depth, string pathPrefix)
    {
        if (depth >= _groupByKeys.Count || rows.Count == 0)
            return new List<GroupNode>();

        var key = _groupByKeys[depth];
        var column = GetColumnByKey(key);
        if (column is null)
            return new List<GroupNode>();

        var groupedRows = new Dictionary<string, List<TItem>>(StringComparer.Ordinal);
        for (var i = 0; i < rows.Count; i++)
        {
            var item = rows[i];
            var groupKey = NormalizeFilterValue(column.GetDisplay(item));
            if (!groupedRows.TryGetValue(groupKey, out var bucket))
            {
                bucket = new List<TItem>();
                groupedRows[groupKey] = bucket;
            }
            bucket.Add(item);
        }

        var sortedKeys = groupedRows.Keys.OrderBy(static x => x, StringComparer.CurrentCulture).ToList();
        var nodes = new List<GroupNode>(sortedKeys.Count);
        var leaf = depth == _groupByKeys.Count - 1;
        for (var i = 0; i < sortedKeys.Count; i++)
        {
            var groupKey = sortedKeys[i];
            var groupRows = groupedRows[groupKey];
            var path = string.IsNullOrEmpty(pathPrefix) ? $"{key}:{groupKey}" : $"{pathPrefix}|{key}:{groupKey}";
            var node = new GroupNode
            {
                Path = path,
                Column = column,
                Depth = depth,
                Label = string.IsNullOrEmpty(groupKey) ? Localizer["DataGrid_FilterEmpty"] : groupKey,
                TotalCount = groupRows.Count
            };

            if (leaf)
            {
                node.Items.AddRange(groupRows);
            }
            else
            {
                node.Children.AddRange(BuildGroupLevel(groupRows, depth + 1, path));
            }

            nodes.Add(node);
        }

        return nodes;
    }

    private TItem? _anchorRow;

    public async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        var rows = GetVisibleRows();
        if (rows.Count == 0) return;

        var currentIndex = _activeRow != null ? rows.IndexOf(_activeRow) : -1;

        if (e.Key == "ArrowDown")
        {
            var nextIndex = currentIndex < rows.Count - 1 ? currentIndex + 1 : 0;
            var nextRow = rows[nextIndex];
            
            if (e.ShiftKey && SelectionEnabled)
            {
                await HandleRangeSelectionAsync(nextRow, rows);
            }
            else
            {
                _activeRow = nextRow;
                _anchorRow = nextRow;
                await OnRowClickAsync(_activeRow);
            }
        }
        else if (e.Key == "ArrowUp")
        {
            var prevIndex = currentIndex > 0 ? currentIndex - 1 : rows.Count - 1;
            var prevRow = rows[prevIndex];

            if (e.ShiftKey && SelectionEnabled)
            {
                await HandleRangeSelectionAsync(prevRow, rows);
            }
            else
            {
                _activeRow = prevRow;
                _anchorRow = prevRow;
                await OnRowClickAsync(_activeRow);
            }
        }
        else if (e.Key == "Enter" && _activeRow != null)
        {
            await OnRowDoubleClickAsync(_activeRow);
        }
        else if (e.Key == " " && _activeRow != null && SelectionEnabled)
        {
            await ToggleRowAsync(_activeRow, !SelectedItems.Contains(_activeRow));
            _anchorRow = _activeRow;
        }
    }

    private async Task HandleRangeSelectionAsync(TItem targetRow, List<TItem> visibleRows)
    {
        _anchorRow ??= _activeRow ?? targetRow;
        var startIdx = visibleRows.IndexOf(_anchorRow);
        var endIdx = visibleRows.IndexOf(targetRow);

        if (startIdx == -1 || endIdx == -1) return;

        var min = Math.Min(startIdx, endIdx);
        var max = Math.Max(startIdx, endIdx);

        SelectedItems.Clear();
        for (var i = min; i <= max; i++)
        {
            SelectedItems.Add(visibleRows[i]);
        }

        _activeRow = targetRow;

        if (SelectedItemsChanged.HasDelegate)
            await SelectedItemsChanged.InvokeAsync(SelectedItems.ToList());

        await InvokeAsync(StateHasChanged);
    }

    private void RebuildColumnLookup()
    {
        _columnLookup.Clear();
        for (var i = 0; i < _columns.Count; i++)
        {
            var column = _columns[i];
            _columnLookup[column.Key] = column;
        }
    }

    private string GetColumnWidthStyle(SgDataGridColumn<TItem> col)
    {
        if (_columnWidths.TryGetValue(col.Key, out var width) && width > 0)
            return $"width:{width}px;min-width:{width}px;";

        if (!string.IsNullOrWhiteSpace(col.Width))
            return $"width:{col.Width};min-width:{col.Width};";

        return string.Empty;
    }

    private string GetColumnPinStyle(SgDataGridColumn<TItem> col)
    {
        if (!col.Pinned)
            return string.Empty;

        EnsurePinnedLeftOffsets();
        if (_pinnedLeftOffsetsCache is not null && _pinnedLeftOffsetsCache.TryGetValue(col.Key, out var left))
            return $"left:{left}px;";

        return string.Empty;
    }

    private void EnsurePinnedLeftOffsets()
    {
        // Check if cache is valid for current columns version
        if (_pinnedLeftOffsetsCacheVersion == _columnsVersion && _pinnedLeftOffsetsCache is not null)
            return;

        _pinnedLeftOffsetsCache ??= new Dictionary<string, int>(StringComparer.Ordinal);
        _pinnedLeftOffsetsCache.Clear();

        var left = SelectionEnabled ? 28 : 0;
        var visibleColumns = VisibleColumns;
        for (var i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];
            if (!column.Pinned)
                continue;

            _pinnedLeftOffsetsCache[column.Key] = left;
            left += EstimateWidth(column);
        }

        _pinnedLeftOffsetsCacheVersion = _columnsVersion;
    }

    private int EstimateWidth(SgDataGridColumn<TItem> col)
    {
        if (_columnWidths.TryGetValue(col.Key, out var width))
            return width;

        if (!string.IsNullOrWhiteSpace(col.Width))
        {
            var digits = new string(col.Width.Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, out var parsed))
                return parsed;
        }

        return 140;
    }

    private string GetColumnTdClass(SgDataGridColumn<TItem> col)
    {
        var css = "sg-td";
        if (col.Pinned)
            css += " sg-pinned";
        if (col.Editable && col.OnValueChanged is not null)
            css += " sg-editable";
        return css;
    }

    private bool IsColumnHidden(string key) => _hiddenColumns.Contains(key);

    private async Task ToggleColumnHiddenAsync(string key, bool hidden)
    {
        if (hidden)
            _hiddenColumns.Add(key);
        else
            _hiddenColumns.Remove(key);
        _columnsVersion++;  // Increment columns version when column visibility changes
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Task ToggleChooserAsync()
    {
        _showChooser = !_showChooser;
        _showExportMenu = false;
        return Task.CompletedTask;
    }

    private Task ToggleExportMenuAsync()
    {
        _showExportMenu = !_showExportMenu;
        _showChooser = false;
        return Task.CompletedTask;
    }

    private async Task HandleChooserFocusOutAsync(FocusEventArgs _)
    {
        // Small delay to allow click events to process before closing
        await Task.Delay(150);
        _showChooser = false;
    }

    private async Task HandleExportFocusOutAsync(FocusEventArgs _)
    {
        // Small delay to allow click events to process before closing
        await Task.Delay(150);
        _showExportMenu = false;
    }

    private async Task ExportCsvAsync()
    {
        if (_module is null)
            return;

        var cols = VisibleColumns;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(';', cols.Select(c => EscapeCsv(c.Title))));
        foreach (var row in GetFilteredSortedRows())
            sb.AppendLine(string.Join(';', cols.Select(c => EscapeCsv(GetExportValue(c, row)))));

        try
        {
            await _module.InvokeVoidAsync("downloadCsv", "grid.csv", sb.ToString());
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgDataGrid: CSV export failed");
        }
    }

    private async Task ExportExcelAsync()
    {
        if (_module is null)
            return;

        var cols = VisibleColumns;
        var sb = new StringBuilder();
        sb.Append("<table><thead><tr>");
        foreach (var col in cols)
            sb.Append("<th>").Append(HtmlEncoder.Default.Encode(col.Title)).Append("</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var row in GetFilteredSortedRows())
        {
            sb.Append("<tr>");
            foreach (var col in cols)
            {
                var raw = col.GetValue(row);
                var colType = GetColumnFilterType(col.Key);

                if (colType == "number" && raw is not null && IsNumericValue(raw))
                {
                    // Numeric cell — no formatting, Excel will treat as number
                    var numStr = Convert.ToDecimal(raw, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture);
                    sb.Append("<td style=\"mso-number-format:'0\\.##########'\">")
                      .Append(HtmlEncoder.Default.Encode(numStr))
                      .Append("</td>");
                }
                else if ((colType == "date" || colType == "datetime") && raw is not null)
                {
                    var dateStr = raw switch
                    {
                        DateTime dt => col.ShowTime
                            ? dt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                            : dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                        DateTimeOffset dto => col.ShowTime
                            ? dto.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)
                            : dto.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                        DateOnly d => d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                        _ => col.GetDisplay(row)
                    };
                    sb.Append("<td>").Append(HtmlEncoder.Default.Encode(dateStr)).Append("</td>");
                }
                else
                {
                    sb.Append("<td>").Append(HtmlEncoder.Default.Encode(col.GetDisplay(row))).Append("</td>");
                }
            }
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");

        try
        {
            await _module.InvokeVoidAsync("downloadExcel", "grid.xls", sb.ToString());
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgDataGrid: Excel export failed");
        }
    }

    /// <summary>
    /// Returns a clean export value for a cell — no currency symbols, no thousand separators.
    /// Numbers → invariant decimal string. Dates → ISO or locale date. Enum → display label.
    /// </summary>
    private string GetExportValue(SgDataGridColumn<TItem> col, TItem item)
    {
        var raw = col.GetValue(item);
        if (raw is null) return string.Empty;

        var colType = GetColumnFilterType(col.Key);

        switch (colType)
        {
            case "number":
                if (IsNumericValue(raw))
                {
                    try
                    {
                        return Convert.ToDecimal(raw, CultureInfo.InvariantCulture)
                            .ToString(CultureInfo.InvariantCulture);
                    }
                    catch { /* fall through */ }
                }
                return raw.ToString() ?? string.Empty;

            case "date":
                return raw switch
                {
                    DateTime dt => dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    DateOnly d => d.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
                    _ => col.GetDisplay(item)
                };

            case "datetime":
                return raw switch
                {
                    DateTime dt => dt.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                    DateTimeOffset dto => dto.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
                    _ => col.GetDisplay(item)
                };

            case "enum":
            {
                // Use display label from [Display]/[Description]
                var type = Nullable.GetUnderlyingType(raw.GetType()) ?? raw.GetType();
                if (type.IsEnum)
                {
                    var items = SgEnumHelper.GetItems(type);
                    var name = raw.ToString() ?? string.Empty;
                    var ei = items.FirstOrDefault(x =>
                        x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    return ei?.Label ?? name;
                }
                return col.GetDisplay(item);
            }

            default:
                return col.GetDisplay(item);
        }
    }

    private static string EscapeCsv(string? value)
    {
        var text = value ?? string.Empty;
        if (!text.Contains('"') && !text.Contains(';') && !text.Contains('\n') && !text.Contains('\r'))
            return text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private async Task AutoFitAsync()
    {
        if (_module is null)
            return;

        var rows = GetFilteredSortedRows();
        var sampleCount = Math.Min(rows.Count, 200);
        var visible = VisibleColumns;
        var payload = new List<object>(visible.Count);
        for (var c = 0; c < visible.Count; c++)
        {
            var col = visible[c];
            var values = new List<string>(sampleCount);
            for (var r = 0; r < sampleCount; r++)
                values.Add(col.GetDisplay(rows[r]));
            payload.Add(new { key = col.Key, title = col.Title, values });
        }

        try
        {
            var widths = await _module.InvokeAsync<Dictionary<string, int>>("measureColumnWidths", payload, _gridId);
            foreach (var pair in widths)
                _columnWidths[pair.Key] = pair.Value;
            await InvokeAsync(StateHasChanged);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgDataGrid: AutoFit measure failed");
        }
    }

    private async Task ToggleRowAsync(TItem item, bool selected)
    {
        if (selected)
        {
            SelectedItems.Add(item);
            _lastSelectedItem = item;
        }
        else
        {
            SelectedItems.Remove(item);
            if (ReferenceEquals(_lastSelectedItem, item))
                _lastSelectedItem = default;
        }

        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
    }

    private async Task HandleRowSelectionClickAsync(MouseEventArgs args, TItem item)
    {
        if (args.ShiftKey && _lastSelectedItem != null && !ReferenceEquals(_lastSelectedItem, item))
        {
            var rows = GetVisibleRows();
            var lastIdx = rows.IndexOf(_lastSelectedItem);
            var currIdx = rows.IndexOf(item);

            if (lastIdx != -1 && currIdx != -1)
            {
                var start = Math.Min(lastIdx, currIdx);
                var end = Math.Max(lastIdx, currIdx);
                
                var shouldSelect = SelectedItems.Contains(_lastSelectedItem);
                for (var i = start; i <= end; i++)
                {
                    if (shouldSelect)
                        SelectedItems.Add(rows[i]);
                    else
                        SelectedItems.Remove(rows[i]);
                }

                _selectionVersion++;
                _selectionChangedPending = true;
                InvalidateComputedRowsCache();
                await FlushSelectedItemsChangedAsync();
                return;
            }
        }
        
        // Regular click (checkbox change will handle it)
    }

    private async Task OnToggleAllAsync(ChangeEventArgs args)
    {
        var selected = args.Value is true;
        var rows = GetFilteredRows();

        if (selected)
        {
            SelectedItems.UnionWith(rows);
        }
        else
        {
            SelectedItems.ExceptWith(rows);
        }

        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
    }

    private async Task ClearSelectionAsync()
    {
        SelectedItems.Clear();
        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
    }

    private async Task GoToPageAsync(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
        InvalidateComputedRowsCache();
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private bool IsEditingCell(TItem item, SgDataGridColumn<TItem> column) =>
        _editingCellColumnKey == column.Key &&
        _editingCellItem is not null &&
        EqualityComparer<TItem>.Default.Equals(_editingCellItem, item);

    private void StartCellEdit(TItem item, SgDataGridColumn<TItem> column)
    {
        _editingCellItem = item;
        _editingCellColumnKey = column.Key;
        _editingCellValue = column.GetValue(item)?.ToString() ?? string.Empty;
    }

    private void OnCellEditInput(ChangeEventArgs args)
    {
        _editingCellValue = args.Value?.ToString() ?? string.Empty;
    }

    private async Task OnCellEditKeyDownAsync(KeyboardEventArgs args, TItem item, SgDataGridColumn<TItem> column)
    {
        if (args.Key == "Enter")
        {
            await CommitCellEditAsync(item, column);
        }
        else if (args.Key == "Escape")
        {
            CancelCellEdit();
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task CommitCellEditAsync(TItem item, SgDataGridColumn<TItem> column)
    {
        column.OnValueChanged?.Invoke(item, ConvertCellEditValue(_editingCellValue, column, item));
        CancelCellEdit();
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    private void CancelCellEdit()
    {
        _editingCellItem = default;
        _editingCellColumnKey = null;
        _editingCellValue = null;
    }

    private static object? ConvertCellEditValue(string? value, SgDataGridColumn<TItem> column, TItem item)
    {
        var targetType = column.ValueType ?? column.GetValue(item)?.GetType();
        if (targetType is null || targetType == typeof(string))
            return value;

        targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        if (string.IsNullOrWhiteSpace(value))
            return targetType.IsValueType ? Activator.CreateInstance(targetType) : null;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, ignoreCase: true);

        return Convert.ChangeType(value, targetType, CultureInfo.CurrentCulture);
    }

    private async Task OnPageSizeChange(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var pageSize) && pageSize > 0)
        {
            PageSize = pageSize;
            _currentPage = 1;
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

    private object? ComputeAggregate(SgDataGridColumn<TItem> col)
    {
        if (col.Aggregate == Aggregate.None)
            return null;

        EnsureAggregateCache();
        return _aggregateCache.TryGetValue(col.Key, out var value) ? value : null;
    }

    private void EnsureAggregateCache()
    {
        // Check if cache is valid for current items and filter versions
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

    // Modal edit methods
    private async Task StartEditModalAsync(TItem item)
    {
        _editModalItem = item;
        _isEditMode = true;
        _editModalTitle = !string.IsNullOrEmpty(EditModalEditTitle) ? EditModalEditTitle : Localizer["DataGrid_Edit"];
        
        // Get editable columns, or if none are marked editable, use all visible columns except ID
        var editableColumns = VisibleColumns.Where(c => c.Editable).ToList();
        if (editableColumns.Count == 0)
        {
            editableColumns = VisibleColumns
                .Where(c => !c.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        _editFormColumns = editableColumns;
        _editFormValues.Clear();
        _editFormErrors.Clear();

        foreach (var col in _editFormColumns)
        {
            var raw = col.GetValue(item);
            var filterType = GetColumnFilterType(col.Key);
            _editFormValues[col.Key] = filterType switch
            {
                "date" => raw is DateTime dt ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : raw is DateTimeOffset dto ? dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : raw?.ToString() ?? string.Empty,
                "datetime" => raw is DateTime dt2 ? dt2.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                            : raw is DateTimeOffset dto2 ? dto2.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                            : raw?.ToString() ?? string.Empty,
                "bool" => raw is bool b ? (b ? "true" : "false") : raw?.ToString() ?? "false",
                "enum" => raw?.ToString() ?? string.Empty,
                "number" => raw is not null ? Convert.ToDecimal(raw, CultureInfo.InvariantCulture)
                                .ToString(CultureInfo.InvariantCulture) : string.Empty,
                _ => raw?.ToString() ?? string.Empty
            };
        }

        _editModalVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private async Task StartCreateModalAsync()
    {
        _editModalItem = CreateItemFactory is not null ? CreateItemFactory() : Activator.CreateInstance<TItem>();
        _isEditMode = false;
        _editModalTitle = !string.IsNullOrEmpty(EditModalAddTitle) ? EditModalAddTitle : Localizer["DataGrid_Add"];
        
        var editableColumns = VisibleColumns.Where(c => c.Editable).ToList();
        if (editableColumns.Count == 0)
        {
            editableColumns = VisibleColumns
                .Where(c => !c.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        
        _editFormColumns = editableColumns;
        _editFormValues.Clear();
        _editFormErrors.Clear();

        // Initialize values from the new item's defaults (same logic as edit mode)
        foreach (var col in _editFormColumns)
        {
            var raw = col.GetValue(_editModalItem!);
            var filterType = GetColumnFilterType(col.Key);
            _editFormValues[col.Key] = filterType switch
            {
                "date" => raw is DateTime dt ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : raw is DateTimeOffset dto ? dto.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : string.Empty,
                "datetime" => raw is DateTime dt2 ? dt2.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                            : raw is DateTimeOffset dto2 ? dto2.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                            : string.Empty,
                "bool" => raw is bool b ? (b ? "true" : "false") : "false",
                "enum" => raw?.ToString() ?? string.Empty,
                "number" => raw is not null && IsNumericValue(raw)
                    ? Convert.ToDecimal(raw, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                    : string.Empty,
                _ => raw?.ToString() ?? string.Empty
            };
        }

        _editModalVisible = true;
        await InvokeAsync(StateHasChanged);
    }

    private Task OnEditFormInputAsync(string key, string? value)
    {
        _editFormValues[key] = value;
        return Task.CompletedTask;
    }

    private Task OnEditFormInputAsync(string key, bool value)
    {
        _editFormValues[key] = value ? "true" : "false";
        return Task.CompletedTask;
    }

    private Task OnEditFormInputAsync(string key, decimal? value)
    {
        _editFormValues[key] = value?.ToString();
        return Task.CompletedTask;
    }

    private Task OnEditFormInputAsync(string key, DateTime? value)
    {
        _editFormValues[key] = value?.ToString("yyyy-MM-dd");
        return Task.CompletedTask;
    }

    private async Task SaveEditModal(TItem item)
    {
        if (item is null)
            return;

        if (!_isEditMode)
        {
            // Adding new item
            if (Items is IList<TItem> list)
            {
                list.Insert(0, item);
                _itemsVersion++;
                _prevItemsCount = list.Count;
                InvalidateComputedRowsCache();
            }

            if (RowCreated.HasDelegate)
                await RowCreated.InvokeAsync(item);
        }
        else
        {
            // Editing existing item - invalidate cache to refresh display
            InvalidateComputedRowsCache();
            
            if (RowDoubleClicked.HasDelegate)
                await RowDoubleClicked.InvokeAsync(item);
        }

        _editModalVisible = false;
        _editModalItem = default;
        _editFormColumns = null;
        _editFormValues.Clear();
        await InvokeAsync(StateHasChanged);
    }

    private Task CloseEditModal()
    {
        _editModalVisible = false;
        _editModalItem = default;
        _editFormColumns = null;
        _editFormValues.Clear();
        _editFormErrors.Clear();
        return Task.CompletedTask;
    }

    private RenderFragment RenderEditForm() => builder =>
    {
        if (_editModalItem is null || _editFormColumns is null)
            return;

        var seq = 0;

        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sg-edit-form");

        // Fields grid
        builder.OpenElement(seq++, "div");
        builder.AddAttribute(seq++, "class", "sg-edit-form-grid");

        foreach (var col in _editFormColumns)
        {
            var colKey = col.Key;
            var filterType = GetColumnFilterType(colKey);
            var currentVal = _editFormValues.TryGetValue(colKey, out var v) ? v : string.Empty;
            var hasError = _editFormErrors.TryGetValue(colKey, out var errMsg);

            // Full-width for textarea-like fields (long text)
            var isFullWidth = filterType == "string" && col.ValueType == typeof(string);

            builder.OpenElement(seq++, "div");
            builder.AddAttribute(seq++, "class",
                "sg-edit-form-group" + (isFullWidth ? " sg-edit-full" : ""));

            // Label
            builder.OpenElement(seq++, "label");
            builder.AddAttribute(seq++, "class", "sg-edit-form-label");
            builder.AddContent(seq++, col.Title);
            builder.CloseElement();

            // Input by type
            switch (filterType)
            {
                case "bool":
                {
                    var boolVal = currentVal is "true" or "True" or "1" or "✓";
                    builder.OpenElement(seq++, "label");
                    builder.AddAttribute(seq++, "class", "sg-checkbox-label");
                    builder.OpenElement(seq++, "input");
                    builder.AddAttribute(seq++, "type", "checkbox");
                    builder.AddAttribute(seq++, "checked", boolVal);
                    builder.AddAttribute(seq++, "onchange",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value is true ? "true" : "false";
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    builder.CloseElement(); // input
                    builder.AddContent(seq++, boolVal
                        ? Localizer["DataGrid_FilterTrue"]
                        : Localizer["DataGrid_FilterFalse"]);
                    builder.CloseElement(); // label
                    break;
                }

                case "enum":
                {
                    var enumItems = GetColumnEnumItems(colKey);
                    builder.OpenElement(seq++, "select");
                    builder.AddAttribute(seq++, "class",
                        "sg-edit-form-select" + (hasError ? " sg-edit-invalid" : ""));
                    builder.AddAttribute(seq++, "value", currentVal ?? string.Empty);
                    builder.AddAttribute(seq++, "onchange",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value?.ToString();
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    // Empty option
                    builder.OpenElement(seq++, "option");
                    builder.AddAttribute(seq++, "value", "");
                    builder.AddContent(seq++, $"— {Localizer["DataGrid_FilterAll"]} —");
                    builder.CloseElement();
                    foreach (var ei in enumItems)
                    {
                        builder.OpenElement(seq++, "option");
                        builder.AddAttribute(seq++, "value", ei.Name);
                        if (currentVal == ei.Name)
                            builder.AddAttribute(seq++, "selected", true);
                        builder.AddContent(seq++, ei.Label);
                        builder.CloseElement();
                    }
                    builder.CloseElement(); // select
                    break;
                }

                case "date":
                {
                    // Normalize to yyyy-MM-dd for input[type=date]
                    var dateVal = NormalizeDateForInput(currentVal, false);
                    builder.OpenElement(seq++, "input");
                    builder.AddAttribute(seq++, "type", "date");
                    builder.AddAttribute(seq++, "class",
                        "sg-edit-form-input" + (hasError ? " sg-edit-invalid" : ""));
                    builder.AddAttribute(seq++, "value", dateVal);
                    builder.AddAttribute(seq++, "oninput",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value?.ToString();
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    builder.CloseElement();
                    break;
                }

                case "datetime":
                {
                    var dtVal = NormalizeDateForInput(currentVal, true);
                    builder.OpenElement(seq++, "input");
                    builder.AddAttribute(seq++, "type", "datetime-local");
                    builder.AddAttribute(seq++, "class",
                        "sg-edit-form-input" + (hasError ? " sg-edit-invalid" : ""));
                    builder.AddAttribute(seq++, "value", dtVal);
                    builder.AddAttribute(seq++, "oninput",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value?.ToString();
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    builder.CloseElement();
                    break;
                }

                case "number":
                {
                    builder.OpenElement(seq++, "input");
                    builder.AddAttribute(seq++, "type", "text");
                    builder.AddAttribute(seq++, "inputmode", "decimal");
                    builder.AddAttribute(seq++, "class",
                        "sg-edit-form-input" + (hasError ? " sg-edit-invalid" : ""));
                    builder.AddAttribute(seq++, "value", currentVal ?? string.Empty);
                    builder.AddAttribute(seq++, "placeholder", "0");
                    builder.AddAttribute(seq++, "oninput",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value?.ToString();
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    builder.CloseElement();
                    break;
                }

                default: // string
                {
                    builder.OpenElement(seq++, "input");
                    builder.AddAttribute(seq++, "type", "text");
                    builder.AddAttribute(seq++, "class",
                        "sg-edit-form-input" + (hasError ? " sg-edit-invalid" : ""));
                    builder.AddAttribute(seq++, "value", currentVal ?? string.Empty);
                    builder.AddAttribute(seq++, "oninput",
                        EventCallback.Factory.Create<ChangeEventArgs>(this, e =>
                        {
                            var val = e.Value?.ToString();
                            _editFormValues[colKey] = val;
                            ApplyEditValueToItem(colKey, val);
                            ValidateEditField(colKey);
                            StateHasChanged();
                        }));
                    builder.CloseElement();
                    break;
                }
            }

            // Error message
            if (hasError)
            {
                builder.OpenElement(seq++, "span");
                builder.AddAttribute(seq++, "class", "sg-edit-form-error");
                builder.AddContent(seq++, errMsg);
                builder.CloseElement();
            }

            builder.CloseElement(); // sg-edit-form-group
        }

        builder.CloseElement(); // sg-edit-form-grid
        builder.CloseElement(); // sg-edit-form
    };

    /// <summary>Normalizes a date string to ISO format for input[type=date/datetime-local].</summary>
    private static string NormalizeDateForInput(string? value, bool includeTime)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        // Already ISO
        if (!includeTime && value.Length == 10 && value[4] == '-') return value;
        if (includeTime && value.Length >= 16 && value[4] == '-') return value[..16];
        // Try parse
        if (DateTime.TryParse(value, CultureInfo.CurrentCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
        {
            return includeTime
                ? dt.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture)
                : dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        return value;
    }

    /// <summary>Applies the string value from the edit form back to the item via reflection.</summary>
    private void ApplyEditValueToItem(string colKey, string? value)
    {
        if (_editModalItem is null) return;
        var col = GetColumnByKey(colKey);
        if (col?.OnValueChanged is not null)
        {
            // Use the column's own handler if provided
            var parsed = ParseEditValue(colKey, value);
            col.OnValueChanged(_editModalItem, parsed);
            return;
        }
        // Fallback: reflection on the item's property matching the column key
        var prop = typeof(TItem).GetProperty(colKey,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.IgnoreCase);
        if (prop is null || !prop.CanWrite) return;
        try
        {
            var parsed = ParseEditValue(colKey, value);
            if (parsed is null && Nullable.GetUnderlyingType(prop.PropertyType) is null &&
                prop.PropertyType.IsValueType)
                return; // can't set null on non-nullable value type
            prop.SetValue(_editModalItem, parsed);
        }
        catch { /* ignore conversion errors */ }
    }

    /// <summary>Parses a string edit value to the column's target type.</summary>
    private object? ParseEditValue(string colKey, string? value)
    {
        var col = GetColumnByKey(colKey);
        var type = col?.ValueType;
        if (type is null)
        {
            // Infer from current item value
            var raw = col?.GetValue(_editModalItem!);
            if (raw is not null)
                type = Nullable.GetUnderlyingType(raw.GetType()) ?? raw.GetType();
        }
        if (type is null) return value;
        return ConvertFromString(value, type);
    }

    /// <summary>Validates a single edit field and updates _editFormErrors.</summary>
    private void ValidateEditField(string colKey)
    {
        var filterType = GetColumnFilterType(colKey);
        var val = _editFormValues.TryGetValue(colKey, out var v) ? v : null;
        _editFormErrors.Remove(colKey);

        if (filterType == "number" && !string.IsNullOrWhiteSpace(val))
        {
            var cleaned = CleanNumericString(val);
            if (!decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                    CultureInfo.CurrentCulture, out _) &&
                !decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any,
                    CultureInfo.InvariantCulture, out _))
            {
                _editFormErrors[colKey] = Localizer["DataGrid_EditInvalidNumber"];
            }
        }
        else if (filterType == "date" && !string.IsNullOrWhiteSpace(val))
        {
            if (!DateTime.TryParse(val, CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out _))
                _editFormErrors[colKey] = Localizer["DataGrid_EditInvalidDate"];
        }
    }

    /// <summary>Validates all edit fields and saves if valid.</summary>
    private async Task TrySaveEditModalAsync()
    {
        _editFormErrors.Clear();
        if (_editFormColumns is not null)
        {
            foreach (var col in _editFormColumns)
                ValidateEditField(col.Key);
        }
        if (_editFormErrors.Count > 0)
        {
            await InvokeAsync(StateHasChanged);
            return;
        }
        if (_editModalItem is not null)
            await SaveEditModal(_editModalItem);
    }

    private EventCallback<bool> CreateBooleanHandler(string key)
    {
        return EventCallback.Factory.Create<bool>(this, async (bool value) =>
        {
            await OnEditFormInputAsync(key, value);
        });
    }

    private EventCallback<DateTime?> CreateDateTimeHandler(string key)
    {
        return EventCallback.Factory.Create<DateTime?>(this, async (DateTime? value) =>
        {
            var formatted = value?.ToString("yyyy-MM-dd");
            await OnEditFormInputAsync(key, formatted);
        });
    }

    private EventCallback<decimal?> CreateDecimalHandler(string key)
    {
        return EventCallback.Factory.Create<decimal?>(this, async (decimal? value) =>
        {
            await OnEditFormInputAsync(key, value);
        });
    }

    private EventCallback<string> CreateStringHandler(string key)
    {
        return EventCallback.Factory.Create<string>(this, async (string value) =>
        {
            await OnEditFormInputAsync(key, value);
        });
    }

    private bool IsBooleanColumn(string key)
    {
        var col = GetColumnByKey(key);
        if (col?.ValueType is null)
            return false;

        var type = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
        return type == typeof(bool);
    }

    private bool GetBooleanValue(string? value) => value is "true" or "True" or "1" or "✓";

    private async Task DeleteRowAsync(TItem item)
    {
        var removed = false;
        if (Items is IList<TItem> list)
        {
            var index = list.IndexOf(item);
            if (index >= 0)
            {
                list.RemoveAt(index);
                _deletedRows.Push(new DeletedRowEntry(item, index));
                removed = true;
            }
        }

        SelectedItems.Remove(item);
        _expandedRows.Remove(item);
        if (_detailItem is not null && EqualityComparer<TItem>.Default.Equals(_detailItem, item))
            CloseDetail();

        if (removed)
        {
            _itemsVersion++;
            _prevItemsCount = Items is ICollection ic ? ic.Count : _prevItemsCount;
        }

        if (RowDeleted.HasDelegate)
            await RowDeleted.InvokeAsync(item);

        _selectionVersion++;
        _selectionChangedPending = true;
        _currentPage = Math.Clamp(_currentPage, 1, TotalPages);
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private Task FlushSelectedItemsChangedAsync()
    {
        if (!_selectionChangedPending)
            return Task.CompletedTask;

        _selectionChangedPending = false;
        return SelectedItemsChanged.HasDelegate
            ? SelectedItemsChanged.InvokeAsync(SelectedItems)
            : Task.CompletedTask;
    }

    private async Task UndoDeleteAsync()
    {
        if (_deletedRows.Count == 0 || Items is not IList<TItem> list)
            return;

        var entry = _deletedRows.Pop();
        list.Insert(Math.Clamp(entry.Index, 0, list.Count), entry.Item);
        _itemsVersion++;
        _prevItemsCount = list.Count;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    private bool IsActiveRow(TItem item) =>
        _activeRow is not null && EqualityComparer<TItem>.Default.Equals(_activeRow, item);

    private bool IsRowExpanded(TItem item) => _expandedRows.Contains(item);

    private async Task OnRowClickAsync(TItem item)
    {
        _activeRow = item;
        await UpdateActiveRowInJsAsync();

        // Inline expand is handled by the dedicated chevron handler so a row click
        // doesn't toggle the detail panel. Drawer/Window placements still surface
        // their detail on row click.
        if (DetailTemplate is not null && DetailPlacement != DetailPlacement.Inline)
        {
            switch (DetailPlacement)
            {
                case DetailPlacement.Drawer:
                    _detailItem = item;
                    _detailDrawerVisible = true;
                    _detailWindowVisible = false;
                    break;
                case DetailPlacement.Window:
                    _detailItem = item;
                    _detailWindowVisible = true;
                    _detailDrawerVisible = false;
                    break;
            }
            StateHasChanged();
        }

        if (RowClicked.HasDelegate)
            await RowClicked.InvokeAsync(item);
    }

    private async Task UpdateActiveRowInJsAsync()
    {
        if (_module is null || _gridRootRef.Context is null) return;

        var rows = GetVisibleRows();
        var index = _activeRow != null ? rows.IndexOf(_activeRow) : -1;
        
        try
        {
            await _module.InvokeVoidAsync("setActiveRow", _gridRootRef, index);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException) { }
        catch (JSException ex)
        {
            Logger.LogDebug(ex, "SgDataGrid: setActiveRow JS call failed");
        }
    }

    private void OnRowExpandToggle(TItem item)
    {
        if (DetailTemplate is null || DetailPlacement != DetailPlacement.Inline)
            return;

        if (!_expandedRows.Add(item))
            _expandedRows.Remove(item);

        StateHasChanged();
    }

    private async Task OnRowDoubleClickAsync(TItem item)
    {
        if (AllowEdit)
        {
            await StartEditModalAsync(item);
        }
        else if (RowDoubleClicked.HasDelegate)
        {
            await RowDoubleClicked.InvokeAsync(item);
        }
    }

    private void CloseDetail()
    {
        _detailDrawerVisible = false;
        _detailWindowVisible = false;
        _detailItem = default;
    }

    private async Task OnRowContextMenuInternal(MouseEventArgs args, TItem item, SgDataGridColumn<TItem>? column)
    {
        if (!OnRowContextMenu.HasDelegate)
            return;

        var payload = new SgDataGridContextMenuEventArgs<TItem>
        {
            Item = item,
            Column = column,
            ColumnKey = column?.Key,
            ColumnTitle = column?.Title,
            CellValue = column?.GetValue(item),
            FormattedValue = column?.GetDisplay(item),
            MouseArgs = args
        };

        await OnRowContextMenu.InvokeAsync(payload);
    }

    public async ValueTask DisposeAsync()
    {
        _disposing = true;

        // Cancel any pending debounce timers so they don't fire after disposal.
        try
        {
            _searchDebounceCts?.Cancel();
            _searchDebounceCts?.Dispose();
            _searchDebounceCts = null;
            foreach (var cts in _quickFilterDebounceCts.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _quickFilterDebounceCts.Clear();
        }
        catch (ObjectDisposedException) { }

        if (_module is not null && _selfRef is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("disposeDataGridVirtualization", _selfRef);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "SgDataGrid: virtualization dispose JS call failed");
            }

            try
            {
                await _module.InvokeVoidAsync("dispose", _selfRef);
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "SgDataGrid: dispose JS call failed");
            }
        }

        // Null out _selfRef before disposing the JS module so that any in-flight JS
        // callbacks cannot invoke methods on a freed DotNetObjectReference.
        var selfRef = _selfRef;
        _selfRef = null;
        selfRef?.Dispose();

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "SgDataGrid: JS module DisposeAsync failed");
            }
        }

        // Release all accumulated state so it doesn't survive the component on a long-lived
        // Blazor Server circuit. The fields are readonly, so we Clear() instead of reassigning.
        _columns.Clear();
        _filters.Clear();
        _conditionFilters.Clear();
        _quickFilters.Clear();
        _queryRules.Clear();
        _sort.Clear();
        _hiddenColumns.Clear();
        _columnWidths.Clear();
        _columnOrder.Clear();
        _columnLookup.Clear();
        _groupByKeys.Clear();
        _collapsedGroups.Clear();
        _deletedRows.Clear();
        _expandedRows.Clear();
        _pendingSelectedValues.Clear();
        _pendingRules.Clear();

        _distinctValuesCache.Clear();
        _distinctNormalizedValuesCache.Clear();
        _aggregateCache.Clear();
        _filteredRowsCache = null;
        _filteredSortedRowsCache = null;
        _visibleRowsCache = null;
        _orderedColumnsCache = null;
        _visibleColumnsCache = null;
        _groupTreeCache = null;
        _pinnedLeftOffsetsCache = null;
    }

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
        public string Path { get; set; } = string.Empty;
        public SgDataGridColumn<TItem> Column { get; set; } = default!;
        public int Depth { get; set; }
        public string Label { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public List<TItem> Items { get; } = new();
        public List<GroupNode> Children { get; } = new();
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

             // Enum: compare by underlying integer value so ordering is correct
             if (xType.IsEnum && yType.IsEnum && xType == yType)
             {
                 var xi = Convert.ToInt32(x);
                 var yi = Convert.ToInt32(y);
                 return xi.CompareTo(yi);
             }
             // Enum vs int (after ConvertForComparison returns int)
             if (xType == typeof(int) && yType == typeof(int))
                 return ((int)x).CompareTo((int)y);

             // Handle comparison of numeric types with different types (int vs double, etc.)
             if (IsNumericType(xType) && IsNumericType(yType))
             {
                 // Convert both to decimal for comparison
                 try
                 {
                     var xDec = Convert.ToDecimal(x, CultureInfo.CurrentCulture);
                     var yDec = Convert.ToDecimal(y, CultureInfo.CurrentCulture);
                     return xDec.CompareTo(yDec);
                 }
                 catch
                 {
                     // Fall through to string comparison
                 }
             }

             // DateTime: compare as dates (time already stripped by ConvertForComparison)
             if (xType == typeof(DateTime) && yType == typeof(DateTime))
                 return ((DateTime)x).CompareTo((DateTime)y);
             if (xType == typeof(DateTimeOffset) && yType == typeof(DateTimeOffset))
                 return ((DateTimeOffset)x).CompareTo((DateTimeOffset)y);
             if (xType == typeof(DateOnly) && yType == typeof(DateOnly))
                 return ((DateOnly)x).CompareTo((DateOnly)y);

             if (xType == yType && x is IComparable comparable)
                 return comparable.CompareTo(y);

             // Numeric string fallback: if both strings look like numbers, compare numerically
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

public sealed class SgDataGridContextMenuEventArgs<TItem>
{
    public required TItem Item { get; init; }
    public SgDataGridColumn<TItem>? Column { get; init; }
    public string? ColumnKey { get; init; }
    public string? ColumnTitle { get; init; }
    public object? CellValue { get; init; }
    public string? FormattedValue { get; init; }
    public required MouseEventArgs MouseArgs { get; init; }
}
