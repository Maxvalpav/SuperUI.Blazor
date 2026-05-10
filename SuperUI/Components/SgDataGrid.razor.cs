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

public partial class SgDataGrid<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private static readonly JsonSerializerOptions StateJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Sample size for AutoFit column width calculation (PERF-06).
    /// Limits the number of rows examined when measuring column widths to improve performance.
    /// </summary>
    private const int AutoFitSampleSize = 200;

    private readonly List<SgDataGridColumn<TItem>> _columns = new();
    private readonly Dictionary<string, HashSet<string>> _filters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ColumnFilter> _conditionFilters = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _quickFilters = new(StringComparer.Ordinal);
    private readonly List<QueryRule> _queryRules = new();
    private readonly List<PersistedSortRule> _sort = new();
    private readonly List<RowHighlightRule> _rowHighlightRules = new();
    private readonly HashSet<string> _hiddenColumns = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pinnedColumns = new(StringComparer.Ordinal); // runtime pin overrides
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
    private SortDirection? _pendingSort;
    private string? _search;
    private string _filterMenuSearchText = string.Empty;
    private string? _openFilterColumn;
    private bool _showChooser;
    private bool _showExportMenu;
    private bool _showSortBuilder;
    // Working copy of sort rules while sort builder is open
    private List<PersistedSortRule> _sortBuilderRules = new();
    private bool _showGroupBuilder;
    // Working copies for group builder
    private List<string> _groupBuilderKeys = new();
    private Dictionary<string, Aggregate> _groupBuilderAggregates = new(StringComparer.Ordinal);
    private bool _showSavedViewsPanel;
    // Working copies for column chooser
    private HashSet<string> _chooserHiddenColumns = new(StringComparer.Ordinal);
    private HashSet<string> _chooserPinnedColumns = new(StringComparer.Ordinal);
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
    private bool _rowHighlighterModalOpen;
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

    /// <summary>Tracks if component has been rendered at least once (for ShouldRender optimization)</summary>
    private bool _hasRendered = false;

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

    private readonly Dictionary<string, string> _columnFilterTypeCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, bool> _numericColumnCache = new(StringComparer.Ordinal);
    private int _columnFilterTypeCacheColumnsVersion = -1;
    private int _columnFilterTypeCacheItemsVersion = -1;

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

    // Scroll debounce — skip re-renders that arrive faster than one animation frame
    private CancellationTokenSource? _scrollDebounceCts;
    private const int ScrollDebounceMs = 16; // ~1 frame at 60fps

    // Group building progress
    private bool _isGroupBuilding;
    private CancellationTokenSource? _groupBuildCts;

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
    /// Gets or sets the height of the grid. Can be a CSS value like "400px" or "100%".
    /// </summary>
    [Parameter] public string? Height { get; set; }

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
    /// Gets or sets whether to allow selecting a single row with a radio button.
    /// When enabled, selecting a new row automatically deselects the previously selected row.
    /// </summary>
    [Parameter] public bool AllowSingleSelect { get; set; }

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
    /// When true, automatically generates a detail panel from TItem properties:
    /// object properties are shown as a read-only form, collection properties as nested grids.
    /// Requires no DetailTemplate — the panel is built via reflection.
    /// </summary>
    [Parameter] public bool AutoDetail { get; set; }

    /// <summary>
    /// Explicit list of TItem property names to include in the auto-detail panel.
    /// When null or empty, all eligible properties are shown.
    /// </summary>
    [Parameter] public IEnumerable<string>? AutoDetailFields { get; set; }

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
    /// Gets or sets the callback invoked when a cell is clicked.
    /// Provides item, column, raw value and formatted value.
    /// </summary>
    [Parameter] public EventCallback<SgDataGridCellClickEventArgs<TItem>> OnCellClick { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when a cell is double-clicked.
    /// </summary>
    [Parameter] public EventCallback<SgDataGridCellClickEventArgs<TItem>> OnCellDoubleClick { get; set; }

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

    /// <summary>
    /// Gets or sets a function that returns additional CSS class(es) for a row.
    /// Return null or empty string to apply no extra class.
    /// </summary>
    [Parameter] public Func<TItem, string?>? RowCssClass { get; set; }

    /// <summary>
    /// Gets or sets a function that returns programmatic row styling.
    /// Takes precedence over rules defined in the visual row-highlighter constructor.
    /// Return <c>null</c> to fall back to the visual highlighter rules.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;SgDataGrid Items="orders" RowStyle="@(o => o.IsOverdue ? new RowHighlightStyle { BackgroundColor = "#ffe0e0", TextColor = "#c00" } : null)" /&gt;
    /// </code>
    /// </example>
    [Parameter] public Func<TItem, RowHighlightStyle?>? RowStyle { get; set; }

    /// <summary>
    /// Gets or sets a function that determines whether a row is disabled.
    /// Disabled rows receive the <c>sg-row-disabled</c> CSS class and cannot be selected or edited.
    /// </summary>
    [Parameter] public Func<TItem, bool>? RowDisabled { get; set; }

    /// <summary>
    /// Gets or sets a function that provides a tooltip (HTML <c>title</c> attribute) for a row.
    /// </summary>
    [Parameter] public Func<TItem, string?>? RowTooltip { get; set; }

    /// <summary>
    /// Gets or sets a function that returns a unique key for each row.
    /// Used for stable row identification across sorting, filtering, and pagination.
    /// If not provided, row identity is based on object reference.
    /// </summary>
    [Parameter] public Func<TItem, string>? RowKeySelector { get; set; }

    /// <summary>
    /// Gets or sets whether to show a row-number column as the first column. Default is false.
    /// </summary>
    [Parameter] public bool ShowRowNumbers { get; set; }

    /// <summary>
    /// Gets or sets a function that returns children for a tree-mode grid.
    /// When set, the grid operates in tree mode.
    /// </summary>
    [Parameter] public Func<TItem, IEnumerable<TItem>?>? TreeChildren { get; set; }

    /// <summary>
    /// Gets or sets whether to show the expand/collapse button in the first data column for tree mode.
    /// </summary>
    [Parameter] public bool IsTree { get; set; }

    /// <summary>
    /// Gets or sets the set of expanded items in tree mode.
    /// </summary>
    [Parameter] public HashSet<TItem> ExpandedItems { get; set; } = new();

    /// <summary>
    /// Callback invoked when the set of expanded items changes.
    /// </summary>
    [Parameter] public EventCallback<HashSet<TItem>> ExpandedItemsChanged { get; set; }

    private readonly HashSet<TItem> _expandedTreeNodes = new();

    internal bool IsTreeNodeExpanded(TItem item) => 
        ExpandedItems.Contains(item) || _expandedTreeNodes.Contains(item);

    internal bool IsLastChild(TItem item)
    {
        if (Items == null || TreeChildren == null) return false;
        
        // This is a simplified check. For a robust solution, we'd need parent context.
        // For now, let's focus on the visual indent and expanders.
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

    /// <summary>
    /// Callback invoked after an item is successfully saved via the edit modal (for both add and edit operations).
    /// </summary>
    [Parameter] public EventCallback<TItem> RowSaved { get; set; }

    /// <summary>
    /// Callback invoked whenever filter, sort, or pagination state changes.
    /// Useful for server-side data loading or saving grid state externally.
    /// </summary>
    [Parameter] public EventCallback OnStateChanged { get; set; }

    /// <summary>
    /// Gets or sets whether to show the row highlighter button in the toolbar.
    /// </summary>
    [Parameter] public bool ShowRowHighlighter { get; set; } = true;

    /// <summary>
    /// When true, shows a "Виды" (Saved Views) button in the toolbar.
    /// Requires <see cref="SavedViewsStorageKey"/> to be set for localStorage persistence.
    /// </summary>
    [Parameter] public bool ShowSavedViews { get; set; }

    /// <summary>
    /// localStorage key for saved views. Required when <see cref="ShowSavedViews"/> is true.
    /// </summary>
    [Parameter] public string? SavedViewsStorageKey { get; set; }

    /// <summary>
    /// Callback invoked when the user saves a view — use this to persist to a database.
    /// Receives the view item with its name and serialized state JSON.
    /// </summary>
    [Parameter] public EventCallback<SgSavedViews<TItem>.SavedViewItem> OnSaveViewToDb { get; set; }

    /// <summary>
    /// Callback invoked when the user deletes a view — use this to remove from a database.
    /// Receives the view id.
    /// </summary>
    [Parameter] public EventCallback<string> OnDeleteViewFromDb { get; set; }

    /// <summary>
    /// Optional list of saved views loaded from a database.
    /// When set, these views are merged with localStorage views.
    /// </summary>
    [Parameter] public IEnumerable<SgSavedViews<TItem>.SavedViewItem>? DbViews { get; set; }

    /// <summary>
    /// Gets or sets the initial row highlight rules.
    /// </summary>
    [Parameter] public IEnumerable<RowHighlightRule>? RowHighlightRules { get; set; }

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
    internal bool SelectionEnabled => AllowMultiSelect || AllowSingleSelect || SelectedItemsChanged.HasDelegate;
    internal int ColumnSpan
    {
        get
        {
            if (_columnSpanCacheVersion == _columnsVersion)
                return _columnSpanCacheValue;

            _columnSpanCacheValue = VisibleColumns.Count
                + ((DetailTemplate is not null || AutoDetail) && DetailPlacement == DetailPlacement.Inline ? 1 : 0)
                + (IsTree ? 1 : 0)
                + (SelectionEnabled ? 1 : 0)
                + (AllowEdit || AllowDelete ? 1 : 0)
                + (ShowRowNumbers ? 1 : 0);
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

    private bool _rowHighlightRulesInitialized;

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
            // Re-detect numeric columns when Items changes
            DetectNumericColumns();
            // Re-schedule group build if grouping is active
            if (_groupByKeys.Count > 0)
                ScheduleGroupBuild();
        }

        if (!_rowHighlightRulesInitialized && RowHighlightRules is not null)
        {
            _rowHighlightRules.Clear();
            foreach (var rule in RowHighlightRules)
            {
                _rowHighlightRules.Add(new RowHighlightRule
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Rules = rule.Rules.Where(IsValidQueryRule).Select(CloneQueryRule).ToList(),
                    RulesAnd = rule.RulesAnd,
                    BackgroundColor = rule.BackgroundColor,
                    TextColor = rule.TextColor,
                    IsEnabled = rule.IsEnabled
                });
            }
            _rowHighlightRulesInitialized = true;
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposing)
            return;

        if (firstRender)
        {
            _hasRendered = true;
            try
            {
                var module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui.js");
                if (_disposing)
                {
                    // Component was disposed while we awaited the import — clean up locally.
                    try { await module.DisposeAsync(); } catch (Exception) { }
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

    /// <summary>
    /// Prevents unnecessary re-renders by checking if content has actually changed (PERF-05).
    /// Only re-renders when items, filters, sort, columns, or grouping change.
    /// </summary>
    protected override bool ShouldRender()
    {
        // Always render on first render
        if (!_hasRendered)
            return true;

        // Check if any content-based version has changed
        // These versions track actual data changes, not render cycles
        var itemsChanged = _itemsVersion != _visibleRowsCacheItemsVersion;
        var filterChanged = _filterVersion != _visibleRowsCacheFilterVersion;
        var sortChanged = _sortVersion != _visibleRowsCacheSortVersion;
        var columnsChanged = _columnsVersion != _visibleRowsCacheColumnsVersion;
        var groupChanged = _groupVersion != _groupTreeCacheGroupVersion;

        // Only render if content has actually changed
        return itemsChanged || filterChanged || sortChanged || columnsChanged || groupChanged;
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
        _columnsVersion++;
        _numericColumnCache.Clear();

        // Detect numeric type for this column as soon as it registers
        DetectNumericColumn(column);
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

        // Debounce scroll re-renders to ~1 frame (16ms) to avoid flooding Blazor
        _scrollDebounceCts?.Cancel();
        _scrollDebounceCts?.Dispose();
        var cts = new CancellationTokenSource();
        _scrollDebounceCts = cts;

        try
        {
            await Task.Delay(ScrollDebounceMs, cts.Token);
            if (!cts.IsCancellationRequested && !_disposing)
                await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException) { }
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
            PinnedColumns = _pinnedColumns.ToList(),
            ColumnWidths = new Dictionary<string, int>(_columnWidths, StringComparer.Ordinal),
            ColumnOrder = new Dictionary<string, int>(_columnOrder, StringComparer.Ordinal),
            GroupBy = _groupByKeys.ToList(),
            PageSize = PageSize,
            ColumnAggregates = _columns
                .Where(c => c.Aggregate != Aggregate.None)
                .ToDictionary(c => c.Key, c => c.Aggregate.ToString(), StringComparer.Ordinal),
            RowHighlightRules = _rowHighlightRules.Select(r => new PersistedRowHighlightRule
            {
                Id = r.Id,
                Name = r.Name,
                Rules = r.Rules.Select(CloneQueryRule).ToList(),
                RulesAnd = r.RulesAnd,
                BackgroundColor = r.BackgroundColor,
                TextColor = r.TextColor,
                IsEnabled = r.IsEnabled,
                TargetColumnKey = r.TargetColumnKey
            }).ToList()
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

        _pinnedColumns.Clear();
        foreach (var key in state.PinnedColumns)
            _pinnedColumns.Add(key);
        System.Diagnostics.Debug.WriteLine($"[DataGrid] ImportStateAsync: Loaded pinned columns: {string.Join(",", _pinnedColumns)}");
        _pinnedLeftOffsetsCache = null;
        _pinnedLeftOffsetsCacheVersion = -1;
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

        // Restore column aggregates
        if (state.ColumnAggregates is { Count: > 0 })
        {
            foreach (var col in _columns)
            {
                if (state.ColumnAggregates.TryGetValue(col.Key, out var aggStr)
                    && Enum.TryParse<Aggregate>(aggStr, out var agg))
                    col.SetAggregate(agg);
                else
                    col.SetAggregate(Aggregate.None);
            }
            _aggregateCache.Clear();
            _aggregateCacheItemsVersion = -1;
            _aggregateCacheFilterVersion = -1;
        }
        else
        {
            // Clear all aggregates if not in state
            foreach (var col in _columns)
                col.SetAggregate(Aggregate.None);
        }

        _rowHighlightRules.Clear();
        foreach (var persistedRule in state.RowHighlightRules ?? new List<PersistedRowHighlightRule>())
        {
            _rowHighlightRules.Add(new RowHighlightRule
            {
                Id = persistedRule.Id,
                Name = persistedRule.Name,
                Rules = (persistedRule.Rules ?? new List<QueryRule>())
                    .Where(IsValidQueryRule)
                    .Select(CloneQueryRule)
                    .ToList(),
                RulesAnd = persistedRule.RulesAnd,
                BackgroundColor = persistedRule.BackgroundColor,
                TextColor = persistedRule.TextColor,
                IsEnabled = persistedRule.IsEnabled,
                TargetColumnKey = persistedRule.TargetColumnKey
            });
        }
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

    /// <summary>Forces a full re-render of the grid. Useful after external data mutations.</summary>
    public async Task RefreshAsync()
    {
        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Returns the current filtered (and sorted) items as a read-only list.</summary>
    public IReadOnlyList<TItem> GetFilteredItems() => GetFilteredSortedRows();

    /// <summary>Selects all currently filtered rows programmatically.</summary>
    public async Task SelectAllAsync()
    {
        var rows = GetFilteredRows();
        var changed = false;
        foreach (var row in rows)
        {
            if (RowDisabled?.Invoke(row) == true) continue;
            if (SelectedItems.Add(row)) changed = true;
        }
        if (changed)
        {
            _selectionVersion++;
            if (SelectedItemsChanged.HasDelegate)
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>Deselects all selected rows programmatically.</summary>
    public async Task DeselectAllAsync()
    {
        if (SelectedItems.Count == 0) return;
        SelectedItems.Clear();
        _selectionVersion++;
        if (SelectedItemsChanged.HasDelegate)
            await SelectedItemsChanged.InvokeAsync(SelectedItems);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Selects specific items programmatically.</summary>
    public async Task SelectItemsAsync(IEnumerable<TItem> items, bool clearExisting = false)
    {
        if (clearExisting) SelectedItems.Clear();
        var changed = false;
        foreach (var item in items)
        {
            if (RowDisabled?.Invoke(item) == true) continue;
            if (SelectedItems.Add(item)) changed = true;
        }
        if (changed)
        {
            _selectionVersion++;
            if (SelectedItemsChanged.HasDelegate)
                await SelectedItemsChanged.InvokeAsync(SelectedItems);
            await InvokeAsync(StateHasChanged);
        }
    }

    public IReadOnlyList<QueryField> GetQueryFields()
    {
        EnsureAutoGeneratedColumns();

        return GetOrderedColumns()
            .Select(col =>
            {
                var type = ResolveColumnType(col);
                IReadOnlyList<QueryFieldEnumOption>? enumOptions = null;
                if (type.IsEnum)
                {
                    enumOptions = SgEnumHelper.GetItems(type)
                        .Select(ei => new QueryFieldEnumOption(ei.Name, ei.Label))
                        .ToList();
                }
                return new QueryField
                {
                    Name = col.Key,
                    Label = col.Title,
                    Type = type,
                    ShowTime = col.ShowTime,
                    EnumOptions = enumOptions
                };
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

    internal bool MatchesRowHighlightRule(TItem item, RowHighlightRule rule)
    {
        if (!rule.IsEnabled || rule.Rules.Count == 0)
            return false;

        if (rule.RulesAnd)
        {
            foreach (var queryRule in rule.Rules)
            {
                if (!MatchesQueryRule(item, queryRule))
                    return false;
            }
            return true;
        }
        else
        {
            foreach (var queryRule in rule.Rules)
            {
                if (MatchesQueryRule(item, queryRule))
                    return true;
            }
            return false;
        }
    }

    internal RowHighlightRule? GetMatchingRowHighlightRule(TItem item)
    {
        foreach (var rule in _rowHighlightRules)
        {
            // Skip cell-specific rules — they are applied per-cell, not per-row
            if (!string.IsNullOrEmpty(rule.TargetColumnKey)) continue;
            if (MatchesRowHighlightRule(item, rule))
                return rule;
        }
        return null;
    }

    /// <summary>
    /// Returns the first matching rule that targets a specific column cell (TargetColumnKey set).
    /// </summary>
    internal RowHighlightRule? GetMatchingCellHighlightRule(TItem item, string columnKey)
    {
        foreach (var rule in _rowHighlightRules)
        {
            if (rule.TargetColumnKey == columnKey && MatchesRowHighlightRule(item, rule))
                return rule;
        }
        return null;
    }

    /// <summary>Returns true if any enabled rule targets specific columns (not whole rows).</summary>
    internal bool HasCellHighlightRules =>
        _rowHighlightRules.Any(r => r.IsEnabled && !string.IsNullOrEmpty(r.TargetColumnKey));

    public IReadOnlyList<RowHighlightRule> GetRowHighlightRules() =>
        _rowHighlightRules.Select(r => new RowHighlightRule
        {
            Id = r.Id,
            Name = r.Name,
            Rules = r.Rules.Select(CloneQueryRule).ToList(),
            RulesAnd = r.RulesAnd,
            BackgroundColor = r.BackgroundColor,
            TextColor = r.TextColor,
            IsEnabled = r.IsEnabled,
            TargetColumnKey = r.TargetColumnKey
        }).ToList();

    public async Task ApplyRowHighlightRulesAsync(IReadOnlyList<RowHighlightRule>? rules)
    {
        EnsureAutoGeneratedColumns();

        _rowHighlightRules.Clear();
        if (rules is not null)
        {
            foreach (var rule in rules)
            {
                _rowHighlightRules.Add(new RowHighlightRule
                {
                    Id = rule.Id,
                    Name = rule.Name,
                    Rules = rule.Rules.Where(IsValidQueryRule).Select(CloneQueryRule).ToList(),
                    RulesAnd = rule.RulesAnd,
                    BackgroundColor = rule.BackgroundColor,
                    TextColor = rule.TextColor,
                    IsEnabled = rule.IsEnabled,
                    TargetColumnKey = rule.TargetColumnKey
                });
            }
        }

        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    public async Task ClearRowHighlightRulesAsync()
    {
        if (_rowHighlightRules.Count == 0)
            return;

        _rowHighlightRules.Clear();
        await SaveStateAsync();
        await InvokeAsync(StateHasChanged);
    }

    private void OpenRowHighlighterAsync()
    {
        _rowHighlighterModalOpen = true;
        StateHasChanged();
    }

    private void CloseRowHighlighterAsync()
    {
        _rowHighlighterModalOpen = false;
        StateHasChanged();
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

    /// <summary>
    /// Returns properties of TItem eligible for auto-detail rendering.
    /// Splits into object properties (→ form) and collection properties (→ nested grid).
    /// </summary>
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

                // Collection: IEnumerable<T> where T is a class (not string)
                var collectionItemType = GetCollectionItemType(t);
                if (collectionItemType is not null)
                    return new AutoDetailProperty(p, label, AutoDetailKind.Collection, collectionItemType);

                // Object: class, not primitive/string/value-type
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
        // IEnumerable<T>
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

    private IReadOnlyList<SgDataGridColumn<TItem>> GetOrderedColumns()
    {
        // Check if cache is valid for current columns version
        if (_orderedColumnsCacheColumnsVersion == _columnsVersion && _orderedColumnsCache is not null)
            return _orderedColumnsCache;

        _orderedColumnsCache = _columns
            .Select((column, index) => (column, index))
            // Pinned columns must come first so they visually appear at the left edge
            .OrderBy(x => IsColumnPinned(x.column.Key) ? 0 : 1)
            .ThenBy(x => _columnOrder.TryGetValue(x.column.Key, out var order) ? order : int.MaxValue)
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

    private readonly Dictionary<TItem, int> _rowLevels = new();

    private List<TItem> GetFilteredRows()
    {
        // Check if cache is valid for current items and filter versions
        if (_filteredRowsCacheItemsVersion == _itemsVersion &&
            _filteredRowsCacheFilterVersion == _filterVersion &&
            _filteredRowsCache is not null)
            return _filteredRowsCache;

        EnsureAutoGeneratedColumns();
        _rowLevels.Clear();

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
        }

        _filteredRowsCache = result;
        _filteredRowsCacheItemsVersion = _itemsVersion;
        _filteredRowsCacheFilterVersion = _filterVersion;
        return _filteredRowsCache;
    }

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
                // We always recurse if filtering is active to find matches deep in the tree.
                // If not filtering, we only recurse if the current node is expanded.
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
            
            // If filtering is active and the node itself doesn't match but we are including it because children match, 
            // we force it to be expanded so the matching children are visible.
            if (hasActiveFilter && !selfMatches && anyChildMatches)
            {
                _expandedTreeNodes.Add(item);
            }

            // Add children results (already filtered/processed)
            result.AddRange(childrenResults);
        }

        return shouldInclude;
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
        // Invalidate cache when columns structure or data changes
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

        // Prefer explicit ValueType, then sample first non-null value from data
        var type = col.ValueType;
        if (type is null)
        {
            // Use IList<T> fast-path to avoid LINQ allocation
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

    private bool HasColumnFilter(string key) =>
        _filters.ContainsKey(key) || _conditionFilters.ContainsKey(key);

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

        // Initialize pending sort from current sort
        _pendingSort = GetSort(key);

        await InvokeAsync(StateHasChanged);
    }

    private Task CloseFilterMenuAsync()
    {
        _openFilterColumn = null;
        return Task.CompletedTask;
    }

    private Task SetPendingSortAsync(SortDirection dir)
    {
        _pendingSort = dir;
        return Task.CompletedTask;
    }

    private Task ClearPendingSortAsync()
    {
        _pendingSort = null;
        return Task.CompletedTask;
    }

    private Task SetPendingRulesAndAsync(bool and)
    {
        _pendingRulesAnd = and;
        // No StateHasChanged - will be called when filter is applied
        return Task.CompletedTask;
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
        if (selected)
        {
            // Get all distinct values for the column
            var allValues = GetDistinctValuesForColumn(key);
            
            // Filter by search text if present
            var filteredValues = allValues
                .Where(v => string.IsNullOrEmpty(_filterMenuSearchText) || 
                           (GetDisplayLabelForFilterValue(key, v)?.Contains(_filterMenuSearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            
            // Normalize and add to pending selection
            var normalized = new HashSet<string>(StringComparer.Ordinal);
            foreach (var val in filteredValues)
            {
                var norm = NormalizeFilterValue(val);
                if (norm is not null)
                    normalized.Add(norm);
            }
            _pendingSelectedValues = normalized;
        }
        else
        {
            _pendingSelectedValues = new HashSet<string>(StringComparer.Ordinal);
        }
        
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

        var itemsToIterate = IsTree && TreeChildren != null ? GetAllTreeItems() : (Items ?? Enumerable.Empty<TItem>());

        foreach (var item in itemsToIterate)
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
        _pendingSort = null;
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

        // Apply pending sort
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

        _filterVersion++;  // Increment filter version when filter changes
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
        _filterVersion++;  // Increment filter version when filters change
        _openFilterColumn = null;
        _currentPage = 1;
        await SaveStateAsync();
        await RaiseStateChangedAsync();
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

    private List<GroupNode> GetGroupTree() => BuildGroupTree();

    /// <summary>
    /// Starts async group tree build in background, showing progress indicator.
    /// Called whenever grouping or data changes.
    /// </summary>
    private void ScheduleGroupBuild()
    {
        if (_groupByKeys.Count == 0) return;

        // Cancel any in-flight build
        _groupBuildCts?.Cancel();
        _groupBuildCts?.Dispose();
        var cts = new CancellationTokenSource();
        _groupBuildCts = cts;

        _isGroupBuilding = true;
        // Invalidate cache so next render triggers rebuild
        _groupTreeCacheItemsVersion = -1;
        _groupTreeCacheGroupVersion = -1;

        _ = Task.Run(() =>
        {
            if (cts.IsCancellationRequested) return;
            // Build on thread pool — CPU-bound work
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

        // Group rows by key — use GetGroupKey (no formatting) for performance
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

        // Sort group keys — Ordinal is ~3x faster than CurrentCulture for typical data
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

            // Display label: for dates/numbers show formatted value of first item
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
            var shouldSelect = AllowSingleSelect ? true : !SelectedItems.Contains(_activeRow);
            await ToggleRowAsync(_activeRow, shouldSelect);
            _anchorRow = _activeRow;
        }
        else if (e.Key == "c" && e.CtrlKey)
        {
            await CopySelectionToClipboardAsync();
        }
    }

    /// <summary>
    /// Copies selected rows (or active row) to clipboard as TSV — pastes cleanly into Excel/Sheets.
    /// </summary>
    public async Task CopySelectionToClipboardAsync()
    {
        if (_module is null) return;

        var cols = VisibleColumns;
        var rows = SelectedItems.Count > 0
            ? GetFilteredSortedRows().Where(r => SelectedItems.Contains(r)).ToList()
            : _activeRow is not null ? new List<TItem> { _activeRow } : new List<TItem>();

        if (rows.Count == 0) return;

        var sb = new StringBuilder();
        // Header row
        sb.AppendLine(string.Join('\t', cols.Select(c => c.Title)));
        // Data rows
        foreach (var row in rows)
            sb.AppendLine(string.Join('\t', cols.Select(c => c.GetDisplay(row))));

        try
        {
            await _module.InvokeVoidAsync("copyToClipboard", sb.ToString().TrimEnd());
        }
        catch (JSException) { }
        catch (TaskCanceledException) { }
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
            await SelectedItemsChanged.InvokeAsync(SelectedItems);

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
        {
            var w = col.Width;
            // Ensure width has units
            if (!w.EndsWith("px", StringComparison.OrdinalIgnoreCase) && 
                !w.EndsWith("%", StringComparison.OrdinalIgnoreCase) &&
                !w.EndsWith("em", StringComparison.OrdinalIgnoreCase) &&
                !w.EndsWith("rem", StringComparison.OrdinalIgnoreCase))
            {
                // If it's just a number, add px
                if (int.TryParse(w, out _))
                    w = w + "px";
            }
            return $"width:{w};min-width:{w};";
        }

        return string.Empty;
    }

    private string GetColumnPinStyle(SgDataGridColumn<TItem> col)
    {
        if (!IsColumnPinned(col.Key))
            return string.Empty;

        EnsurePinnedLeftOffsets();
        if (_pinnedLeftOffsetsCache is not null && _pinnedLeftOffsetsCache.TryGetValue(col.Key, out var left))
        {
            // Inline position:sticky bypasses Blazor CSS isolation issues with RenderTreeBuilder.
            // No background here — CSS rules handle it per row state (even/odd/hover/selected).
            return $"position:sticky;left:{left}px;z-index:3;";
        }

        return string.Empty;
    }

    private string GetColumnPinStyleForHeader(SgDataGridColumn<TItem> col)
    {
        if (!IsColumnPinned(col.Key))
            return string.Empty;

        EnsurePinnedLeftOffsets();
        if (_pinnedLeftOffsetsCache is not null && _pinnedLeftOffsetsCache.TryGetValue(col.Key, out var left))
        {
            // For thead: add position:sticky with both top:0 (vertical) and left:X (horizontal)
            // z-index:4 to be above tbody cells (z-index:3)
            return $"position:sticky;top:0;left:{left}px;z-index:4;background:var(--sg-header-bg);";
        }

        return string.Empty;
    }

    private string GetColumnPinStyleForQuickFilter(SgDataGridColumn<TItem> col)
    {
        if (!IsColumnPinned(col.Key))
            return string.Empty;

        EnsurePinnedLeftOffsets();
        if (_pinnedLeftOffsetsCache is not null && _pinnedLeftOffsetsCache.TryGetValue(col.Key, out var left))
        {
            // Quick filter row: position:sticky with top:30px (below main header) and left:X (horizontal)
            // z-index:3 to be above non-pinned quick filter cells (z-index:1) and above tbody (z-index:3 when pinned)
            return $"position:sticky;top:30px;left:{left}px;z-index:3;background:var(--sg-header-bg);";
        }

        return string.Empty;
    }

    /// <summary>
    /// Calculates the left offset for technical columns (row numbers, checkboxes, expand, tree).
    /// Technical columns are always pinned and their order is fixed.
    /// </summary>
    private int GetTechnicalColumnLeft(string columnType)
    {
        var left = 0;
        
        // Order: rowNum (36px) → expand (32px) → tree (32px) → check (28px)
        // Each column adds its width to left for columns that come after it
        
        if (columnType == "expand")
        {
            if (ShowRowNumbers) left += 36;
            return left;
        }
        
        if (columnType == "tree")
        {
            if (ShowRowNumbers) left += 36;
            if ((DetailTemplate is not null || AutoDetail) && DetailPlacement == DetailPlacement.Inline) left += 32;
            return left;
        }
        
        if (columnType == "check")
        {
            if (ShowRowNumbers) left += 36;
            if ((DetailTemplate is not null || AutoDetail) && DetailPlacement == DetailPlacement.Inline) left += 32;
            if (IsTree) left += 32;
            return left;
        }
        
        return 0;
    }

    /// <summary>
    /// Returns inline sticky style for technical columns in tbody.
    /// Includes position:sticky, left offset, z-index, and background color.
    /// Background is set to transparent so CSS rules for row states (hover, selected, even/odd) work correctly.
    /// </summary>
    private string GetTechnicalColumnPinStyle(string columnType)
    {
        var left = GetTechnicalColumnLeft(columnType);
        // Use transparent background so row background colors (even/odd, selected, hover) show through
        // The CSS rules will handle the actual background via .sg-pinned class
        return $"position:sticky;left:{left}px;z-index:3;";
    }

    /// <summary>
    /// Returns the type of the last technical column that is enabled.
    /// Used to add shadow to visually separate pinned columns from scrollable content.
    /// </summary>
    private string GetLastTechnicalColumnType()
    {
        if (SelectionEnabled) return "check";
        if (IsTree) return "tree";
        if ((DetailTemplate is not null || AutoDetail) && DetailPlacement == DetailPlacement.Inline) return "expand";
        if (ShowRowNumbers) return "rownum";
        return "";
    }

    /// <summary>
    /// Returns CSS class for technical column, including sg-last-pinned-tech for the last one.
    /// </summary>
    private string GetTechnicalColumnClass(string baseClass, string columnType)
    {
        var lastType = GetLastTechnicalColumnType();
        var isLast = columnType == lastType;
        return isLast ? $"{baseClass} sg-last-pinned-tech" : baseClass;
    }

    private void EnsurePinnedLeftOffsets()
    {
        if (_pinnedLeftOffsetsCacheVersion == _columnsVersion && _pinnedLeftOffsetsCache is not null)
            return;

        _pinnedLeftOffsetsCache ??= new Dictionary<string, int>(StringComparer.Ordinal);
        _pinnedLeftOffsetsCache.Clear();

        var left = 0;
        
        // 1. Add row numbers column width if enabled
        if (ShowRowNumbers) left += 36;
        
        // 2. Add selection column width (checkbox/radio) if enabled
        if (SelectionEnabled) left += 28;
        
        // 3. Add expand column width if enabled
        if ((DetailTemplate is not null || AutoDetail) && DetailPlacement == DetailPlacement.Inline)
            left += 32;
        
        // 4. Add tree expand column width if enabled
        if (IsTree) left += 32;

        System.Diagnostics.Debug.WriteLine($"[DataGrid] EnsurePinnedLeftOffsets: Building cache, pinnedColumns={string.Join(",", _pinnedColumns)}, initial left={left}px");

        var visibleColumns = VisibleColumns;
        
        for (var i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];
            
            // Only process pinned columns
            if (!IsColumnPinned(column.Key))
                continue;

            var width = EstimateWidth(column);
            // Store the current left position for this pinned column
            _pinnedLeftOffsetsCache[column.Key] = left;
            System.Diagnostics.Debug.WriteLine($"[DataGrid]   Pinned: {column.Key}, left={left}px, width={width}px");
            // Add this column's width to left for the next pinned column
            left += width;
        }

        System.Diagnostics.Debug.WriteLine($"[DataGrid] EnsurePinnedLeftOffsets: Complete, {_pinnedLeftOffsetsCache.Count} pinned columns");
        _pinnedLeftOffsetsCacheVersion = _columnsVersion;
    }

    private int EstimateWidth(SgDataGridColumn<TItem> col)
    {
        if (_columnWidths.TryGetValue(col.Key, out var width))
            return width;

        if (!string.IsNullOrWhiteSpace(col.Width))
        {
            var s = col.Width.AsSpan();
            var i = 0;
            while (i < s.Length && !char.IsAsciiDigit(s[i])) i++;
            var start = i;
            while (i < s.Length && char.IsAsciiDigit(s[i])) i++;
            if (i > start && int.TryParse(s[start..i], out var parsed))
                return parsed;
        }

        return 140;
    }

    private string GetColumnTdClass(SgDataGridColumn<TItem> col, bool isNumeric = false)
    {
        var css = "sg-td";
        if (IsColumnPinned(col.Key))
            css += " sg-pinned";
        if (col.Editable && col.OnValueChanged is not null)
            css += " sg-editable";

        if (isNumeric)
            css += " sg-td-numeric";

        // Horizontal alignment — numeric columns default to right, others to left
        var hAlign = col.HAlign;
        if (hAlign == SgHAlign.Default && isNumeric)
            hAlign = SgHAlign.Right;

        css += hAlign switch
        {
            SgHAlign.Left   => " sg-align-left",
            SgHAlign.Center => " sg-align-center",
            SgHAlign.Right  => " sg-align-right",
            _               => string.Empty
        };

        // Vertical alignment
        css += col.VAlign switch
        {
            SgVAlign.Top    => " sg-valign-top",
            SgVAlign.Middle => " sg-valign-middle",
            SgVAlign.Bottom => " sg-valign-bottom",
            _               => string.Empty
        };

        return css;
    }

    /// <summary>
    /// Resolves whether a column should use numeric rendering.
    /// Calls <see cref="SgDataGridColumn{TItem}.TryDetectNumericType"/> with the first
    /// available value so auto-detection works even without <c>ValueType</c>.
    /// </summary>
    /// <summary>
    /// Detects numeric type for a single column by sampling Items.
    /// Falls back to TItem property reflection when col.Value is not set.
    /// </summary>
    private void DetectNumericColumn(SgDataGridColumn<TItem> col)
    {
        if (col.IsNumericResolved) return;

        // Try via Value delegate first
        if (col.Value is not null && Items is not null)
        {
            foreach (var row in Items)
            {
                var v = col.Value(row);
                if (v is null) continue;
                col.TryDetectNumericType(v);
                return;
            }
        }

        // Fallback: detect from TItem property type via reflection using the column key
        if (!col.IsNumericResolved)
        {
            var prop = typeof(TItem).GetProperty(col.Key,
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop is not null)
            {
                var underlying = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                col.TryDetectNumericType(
                    underlying == typeof(int)     ? (object)0 :
                    underlying == typeof(long)    ? (object)0L :
                    underlying == typeof(double)  ? (object)0.0 :
                    underlying == typeof(float)   ? (object)0f :
                    underlying == typeof(decimal) ? (object)0m :
                    underlying == typeof(short)   ? (object)(short)0 :
                    underlying == typeof(byte)    ? (object)(byte)0 :
                    underlying == typeof(uint)    ? (object)0u :
                    underlying == typeof(ulong)   ? (object)0ul :
                    underlying == typeof(sbyte)   ? (object)(sbyte)0 :
                    underlying == typeof(ushort)  ? (object)(ushort)0 : null);
            }
        }
    }

    /// <summary>
    /// Detects numeric type for all registered columns. Called when Items changes.
    /// </summary>
    private void DetectNumericColumns()
    {
        foreach (var col in _columns)
            DetectNumericColumn(col);
    }

    private bool ResolveIsNumericColumn(SgDataGridColumn<TItem> col)
    {
        // Explicit override — never cache, always re-evaluate
        if (col.NumericStyle.HasValue) return col.NumericStyle.Value;

        if (_numericColumnCache.TryGetValue(col.Key, out var cached))
            return cached;

        bool result;

        // ValueType parameter
        if (col.ValueType is not null)
        {
            var t = Nullable.GetUnderlyingType(col.ValueType) ?? col.ValueType;
            result = SgDataGridColumn<TItem>.IsNumericTypeStatic(t);
        }
        // Cached detection from sampled value
        else if (col.IsNumericColumn)
        {
            result = true;
        }
        else
        {
            // Last resort: reflect TItem property by column key
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

    private bool IsColumnHidden(string key) => _showChooser ? _chooserHiddenColumns.Contains(key) : _hiddenColumns.Contains(key);

    private bool IsColumnPinned(string key)
    {
        // Runtime override takes priority over column parameter
        var pinnedSet = _showChooser ? _chooserPinnedColumns : _pinnedColumns;
        var result = pinnedSet.Contains(key) || (GetColumnByKey(key)?.Pinned == true);
        return result;
    }

    private async Task ToggleColumnHiddenAsync(string key, bool hidden)
    {
        if (hidden)
            _chooserHiddenColumns.Add(key);
        else
            _chooserHiddenColumns.Remove(key);
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleColumnPinnedAsync(string key, bool pin)
    {
        if (pin)
            _chooserPinnedColumns.Add(key);
        else
            _chooserPinnedColumns.Remove(key);
        await InvokeAsync(StateHasChanged);
    }

    private async Task ToggleChooserAsync()
    {
        if (!_showChooser)
        {
            // Opening chooser - copy current state to working copies
            _chooserHiddenColumns = new HashSet<string>(_hiddenColumns, StringComparer.Ordinal);
            _chooserPinnedColumns = new HashSet<string>(_pinnedColumns, StringComparer.Ordinal);
        }
        _showChooser = !_showChooser;
        _showExportMenu = false;
        _showSortBuilder = false;
        _showGroupBuilder = false;
        _showSavedViewsPanel = false;
        await InvokeAsync(StateHasChanged);
    }

    private Task ToggleExportMenuAsync()
    {
        _showExportMenu = !_showExportMenu;
        _showChooser = false;
        _showSortBuilder = false;
        _showGroupBuilder = false;
        _showSavedViewsPanel = false;
        return Task.CompletedTask;
    }

    private async Task HandleChooserFocusOutAsync(FocusEventArgs _)
    {
        // Don't close on focus out - only close on explicit button clicks
        await Task.CompletedTask;
    }

    private async Task ApplyChooserChangesAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DataGrid] ========== APPLY CHOOSER CHANGES START ==========");
            System.Diagnostics.Debug.WriteLine($"[DataGrid] _chooserPinnedColumns before: {string.Join(",", _chooserPinnedColumns)}");
            System.Diagnostics.Debug.WriteLine($"[DataGrid] _pinnedColumns before: {string.Join(",", _pinnedColumns)}");
            
            // Close menu FIRST before applying changes
            _showChooser = false;
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Chooser closed, _showChooser = false");
            
            // Apply changes
            _hiddenColumns.Clear();
            foreach (var key in _chooserHiddenColumns)
                _hiddenColumns.Add(key);

            _pinnedColumns.Clear();
            foreach (var key in _chooserPinnedColumns)
                _pinnedColumns.Add(key);

            System.Diagnostics.Debug.WriteLine($"[DataGrid] Hidden columns: {string.Join(", ", _hiddenColumns)}");
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Pinned columns after: {string.Join(", ", _pinnedColumns)}");

            // Invalidate all caches
            _columnsVersion++;
            System.Diagnostics.Debug.WriteLine($"[DataGrid] _columnsVersion incremented to: {_columnsVersion}");
            
            _pinnedLeftOffsetsCache = null;
            _pinnedLeftOffsetsCacheVersion = -1;
            _visibleColumnsCache = null;
            _visibleColumnsCacheVersion = -1;
            _columnSpanCacheVersion = -1;
            _orderedColumnsCache = null;
            _orderedColumnsCacheColumnsVersion = -1;
            InvalidateComputedRowsCache();
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] All caches invalidated");
            
            // Force rebuild of pinned offsets cache
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Forcing rebuild of pinned offsets cache...");
            EnsurePinnedLeftOffsets();
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Pinned offsets cache rebuilt");
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Calling StateHasChanged...");
            await InvokeAsync(StateHasChanged);
            System.Diagnostics.Debug.WriteLine($"[DataGrid] StateHasChanged completed");
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Saving state...");
            await SaveStateAsync();
            System.Diagnostics.Debug.WriteLine($"[DataGrid] State saved");
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] ========== APPLY CHOOSER CHANGES END ==========");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataGrid] ERROR in ApplyChooserChangesAsync: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task ApplyChooserChangesWithoutClosingAsync()
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Apply button clicked!");
            
            // Apply changes but keep menu open
            _hiddenColumns.Clear();
            foreach (var key in _chooserHiddenColumns)
                _hiddenColumns.Add(key);

            _pinnedColumns.Clear();
            foreach (var key in _chooserPinnedColumns)
                _pinnedColumns.Add(key);

            System.Diagnostics.Debug.WriteLine($"[DataGrid] Hidden columns: {string.Join(", ", _hiddenColumns)}");
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Pinned columns: {string.Join(", ", _pinnedColumns)}");

            // Invalidate all caches
            _columnsVersion++;
            _pinnedLeftOffsetsCache = null;
            _pinnedLeftOffsetsCacheVersion = -1;
            _visibleColumnsCache = null;
            _visibleColumnsCacheVersion = -1;
            _columnSpanCacheVersion = -1;
            _orderedColumnsCache = null;
            _orderedColumnsCacheColumnsVersion = -1;
            InvalidateComputedRowsCache();
            
            // Force rebuild of pinned offsets cache
            EnsurePinnedLeftOffsets();
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Changes applied, calling StateHasChanged");
            await InvokeAsync(StateHasChanged);
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Saving state");
            await SaveStateAsync();
            
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Apply completed successfully");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DataGrid] Error in ApplyChooserChangesWithoutClosingAsync: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private async Task CancelChooserChangesAsync()
    {
        // Restore original values from actual collections
        _chooserHiddenColumns = new HashSet<string>(_hiddenColumns, StringComparer.Ordinal);
        _chooserPinnedColumns = new HashSet<string>(_pinnedColumns, StringComparer.Ordinal);
        _showChooser = false;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleExportFocusOutAsync(FocusEventArgs _)
    {
        // Small delay to allow click events to process before closing
        await Task.Delay(150);
        _showExportMenu = false;
    }

    private Task OpenSortBuilderAsync()
    {
        if (_showSortBuilder)
        {
            _showSortBuilder = false;
            return Task.CompletedTask;
        }
        // Clone current sort rules into working copy
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

    // ── Group Builder ─────────────────────────────────────────────────────────

    private Task OpenGroupBuilderAsync()
    {
        if (_showGroupBuilder)
        {
            _showGroupBuilder = false;
            return Task.CompletedTask;
        }
        // Clone current state into working copies
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
        // Apply grouping
        _groupByKeys.Clear();
        _groupByKeys.AddRange(_groupBuilderKeys);

        // Apply aggregates to columns
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

    // ── Saved Views Panel ─────────────────────────────────────────────────────

    private Task ToggleSavedViewsPanelAsync()
    {
        _showSavedViewsPanel = !_showSavedViewsPanel;
        if (_showSavedViewsPanel)
        {
            _showChooser = false;
            _showExportMenu = false;
            _showSortBuilder = false;
            _showGroupBuilder = false;
        }
        return Task.CompletedTask;
    }

    private async Task ExportCsvAsync()
    {
        if (_module is null)
            return;

        var cols = VisibleColumns;
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', cols.Select(c => EscapeCsv(c.Title))));
        foreach (var row in GetFilteredSortedRows())
            sb.AppendLine(string.Join(',', cols.Select(c => EscapeCsv(GetExportValue(c, row)))));

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
                    catch (Exception) { /* fall through */ }
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
        if (!text.Contains('"') && !text.Contains(',') && !text.Contains('\n') && !text.Contains('\r'))
            return text;
        return "\"" + text.Replace("\"", "\"\"") + "\"";
    }

    private async Task AutoFitAsync()
    {
        if (_module is null)
            return;

        var rows = GetFilteredSortedRows();
        var sampleCount = Math.Min(rows.Count, AutoFitSampleSize);
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
            
            // Check if component was disposed while we awaited
            if (_disposing)
                return;
            
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
        if (RowDisabled?.Invoke(item) == true) return;
        if (AllowSingleSelect && !AllowMultiSelect)
        {
            // Single-select: clear all previous selections, then select the clicked item
            SelectedItems.Clear();
            if (selected)
            {
                SelectedItems.Add(item);
                _lastSelectedItem = item;
            }
            else
            {
                _lastSelectedItem = default;
            }
        }
        else
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
        }

        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();

        // For single-select, clicking the radio button doesn't bubble to the <tr> onclick
        // (the <td> has stopPropagation), so we fire RowClicked here directly.
        if (AllowSingleSelect && selected)
            await OnRowClickAsync(item);
    }

    private async Task HandleRowSelectionClickAsync(MouseEventArgs args, TItem item)
    {
        if (!AllowSingleSelect && args.ShiftKey && _lastSelectedItem != null && !ReferenceEquals(_lastSelectedItem, item))
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

    public async Task ClearSelectionAsync()
    {
        SelectedItems.Clear();
        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
    }

    /// <summary>
    /// Programmatically selects a single item.
    /// Does nothing if <see cref="RowDisabled"/> returns true for the item.
    /// </summary>
    public async Task SelectItemAsync(TItem item)
    {
        if (RowDisabled?.Invoke(item) == true) return;
        await ToggleRowAsync(item, true);
    }

    /// <summary>
    /// Programmatically selects multiple items.
    /// Items for which <see cref="RowDisabled"/> returns true are skipped.
    /// </summary>
    public async Task SelectItemsAsync(IEnumerable<TItem> items)
    {
        var changed = false;
        foreach (var item in items)
        {
            if (RowDisabled?.Invoke(item) == true) continue;
            if (SelectedItems.Add(item)) changed = true;
        }
        if (!changed) return;
        _selectionVersion++;
        _selectionChangedPending = true;
        InvalidateComputedRowsCache();
        await FlushSelectedItemsChangedAsync();
        await InvokeAsync(StateHasChanged);
    }

    private async Task GoToPageAsync(int page)
    {
        _currentPage = Math.Clamp(page, 1, TotalPages);
        InvalidateComputedRowsCache();
        await SaveStateAsync();
        await RaiseStateChangedAsync();
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
        if (RowSaved.HasDelegate)
            await RowSaved.InvokeAsync(item);
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
        catch (Exception) { /* ignore conversion errors */ }
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
        if ((DetailTemplate is not null || AutoDetail) && DetailPlacement != DetailPlacement.Inline)
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

        var rowKey = _activeRow != null && RowKeySelector != null ? RowKeySelector(_activeRow) : null;

        try
        {
            await _module.InvokeVoidAsync("setActiveRow", _gridRootRef, rowKey);
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
        if (DetailTemplate is null && !AutoDetail || DetailPlacement != DetailPlacement.Inline)
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

    private async Task OnCellClickInternal(MouseEventArgs args, TItem item, SgDataGridColumn<TItem> column)
    {
        if (!OnCellClick.HasDelegate) return;
        await OnCellClick.InvokeAsync(new SgDataGridCellClickEventArgs<TItem>
        {
            Item = item,
            Column = column,
            ColumnKey = column.Key,
            ColumnTitle = column.Title,
            CellValue = column.GetValue(item),
            FormattedValue = column.GetDisplay(item),
            MouseArgs = args
        });
    }

    private async Task OnCellDoubleClickInternal(MouseEventArgs args, TItem item, SgDataGridColumn<TItem> column)
    {
        if (!OnCellDoubleClick.HasDelegate) return;
        await OnCellDoubleClick.InvokeAsync(new SgDataGridCellClickEventArgs<TItem>
        {
            Item = item,
            Column = column,
            ColumnKey = column.Key,
            ColumnTitle = column.Title,
            CellValue = column.GetValue(item),
            FormattedValue = column.GetDisplay(item),
            MouseArgs = args
        });
    }

    private Task RaiseStateChangedAsync() =>
        OnStateChanged.HasDelegate ? OnStateChanged.InvokeAsync() : Task.CompletedTask;

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

        // Lazy — only one of Items or Children is set, never both
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

public sealed class SgDataGridContextMenuEventArgs<TItem> where TItem : notnull
{
    public required TItem Item { get; init; }
    public SgDataGridColumn<TItem>? Column { get; init; }
    public string? ColumnKey { get; init; }
    public string? ColumnTitle { get; init; }
    public object? CellValue { get; init; }
    public string? FormattedValue { get; init; }
    public required MouseEventArgs MouseArgs { get; init; }
}

/// <summary>Event args for cell click and double-click events.</summary>
public sealed class SgDataGridCellClickEventArgs<TItem> where TItem : notnull
{
    public required TItem Item { get; init; }
    public required SgDataGridColumn<TItem> Column { get; init; }
    public string? ColumnKey { get; init; }
    public string? ColumnTitle { get; init; }
    public object? CellValue { get; init; }
    public string? FormattedValue { get; init; }
    public required MouseEventArgs MouseArgs { get; init; }
}

public enum AutoDetailKind { Object, Collection }

public sealed class AutoDetailProperty
{
    public AutoDetailProperty(System.Reflection.PropertyInfo prop, string label, AutoDetailKind kind, Type itemType)
    {
        Property = prop;
        Label = label;
        Kind = kind;
        ItemType = itemType;
    }
    public System.Reflection.PropertyInfo Property { get; }
    public string Label { get; }
    public AutoDetailKind Kind { get; }
    /// <summary>For Object: the object type. For Collection: the element type.</summary>
    public Type ItemType { get; }
}
