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

/// <summary>A full-featured data grid component supporting sorting, filtering, grouping, pagination, tree mode, inline editing, row selection, row highlighting, state import/export, and keyboard navigation.</summary>
/// <typeparam name="TItem">The type of data items displayed in the grid.</typeparam>
public partial class SgDataGrid<TItem> : ComponentBase, IAsyncDisposable where TItem : notnull
{
    private readonly List<SgDataGridColumn<TItem>> _columns = new();

    private bool _isSyntheticColumnsInitialized;
    private IEnumerable<TItem>? _prevItems;

    [Inject] private IJSRuntime JS { get; set; } = default!;
    [Inject] private ISuperUILocalizer Localizer { get; set; } = default!;
    [Inject] private ILogger<SgDataGrid<TItem>> Logger { get; set; } = default!;
    [Inject] private SgJsModuleCache ModuleCache { get; set; } = default!;

    private Action? _localeChangedHandler;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _localeChangedHandler = () =>
        {
            if (_disposing) return;
            try { InvokeAsync(StateHasChanged); }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
            catch (TaskCanceledException) { }
        };
        Localizer.OnLocaleChanged += _localeChangedHandler;
    }

    [Parameter, EditorRequired] public IEnumerable<TItem> Items { get; set; } = default!;

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

    [Parameter] public EventCallback<int> PageSizeChanged { get; set; }

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

    internal string EffectiveEmptyText => string.IsNullOrWhiteSpace(EmptyText) ? Localizer["DataGrid_EmptyText"] : EmptyText!;
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
        if (_lastPageSize != PageSize)
        {
            _pageSize = PageSize > 0 ? PageSize : 25;
            _lastPageSize = PageSize;
        }

        _estimatedRowHeight = EstimatedRowHeight > 0 ? EstimatedRowHeight : 32;

        var currentCount = Items is ICollection col ? col.Count : -1;
        if (!ReferenceEquals(_prevItems, Items) || currentCount != _prevItemsCount)
        {
            _prevItems = Items;
            _prevItemsCount = currentCount;
            _itemsVersion++;
            DetectNumericColumns();
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
        _renderVersion++;
    }

    /// <summary>
    /// Gets or sets whether to enable row virtualization for large datasets. Default is false.
    /// </summary>
    [Parameter] public bool EnableVirtualization { get; set; }

    /// <summary>
    /// Gets or sets the estimated height of each row in pixels. Default is 32.
    /// </summary>
    [Parameter] public int EstimatedRowHeight { get; set; } = 32;

    /// <summary>
    /// Когда <c>true</c>, содержимое ячеек переносится по словам, и высота строки растёт
    /// до размера самой высокой ячейки. По умолчанию <c>false</c> (одна строка + ellipsis).
    /// </summary>
    /// <remarks>
    /// При включённом wrap виртуализация (<see cref="EnableVirtualization"/>) автоматически
    /// отключается через <see cref="ShouldUseVirtualization"/>, потому что её алгоритм
    /// рассчитывает позиции по фиксированной высоте строки и при переменной высоте даст
    /// «дёрганый» скролл и неточные паддинги. Используйте wrap для умеренных наборов данных.
    /// </remarks>
    [Parameter] public bool WrapCells { get; set; }

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
        return string.IsNullOrWhiteSpace(format) ? null : format;
    }

    private void DetectNumericColumn(SgDataGridColumn<TItem> col)
    {
        if (col.IsNumericResolved) return;

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

    private void DetectNumericColumns()
    {
        foreach (var col in _columns)
            DetectNumericColumn(col);
    }
}

/// <summary>Event arguments for the <see cref="SgDataGrid{TItem}.OnRowContextMenu"/> event.</summary>
/// <typeparam name="TItem">The type of data item.</typeparam>
public sealed class SgDataGridContextMenuEventArgs<TItem> where TItem : notnull
{
    /// <summary>The data item associated with the context menu event.</summary>
    public required TItem Item { get; init; }
    /// <summary>The column on which the context menu was invoked, if applicable.</summary>
    public SgDataGridColumn<TItem>? Column { get; init; }
    /// <summary>The key of the column on which the context menu was invoked.</summary>
    public string? ColumnKey { get; init; }
    /// <summary>The display title of the column on which the context menu was invoked.</summary>
    public string? ColumnTitle { get; init; }
    /// <summary>The raw cell value at the context menu location.</summary>
    public object? CellValue { get; init; }
    /// <summary>The formatted cell value at the context menu location.</summary>
    public string? FormattedValue { get; init; }
    /// <summary>The mouse event arguments describing the context menu trigger.</summary>
    public required MouseEventArgs MouseArgs { get; init; }
}

/// <summary>Event args for cell click and double-click events.</summary>
/// <typeparam name="TItem">The type of data item.</typeparam>
public sealed class SgDataGridCellClickEventArgs<TItem> where TItem : notnull
{
    /// <summary>The data item associated with the cell event.</summary>
    public required TItem Item { get; init; }
    /// <summary>The column that was clicked.</summary>
    public required SgDataGridColumn<TItem> Column { get; init; }
    /// <summary>The key of the column that was clicked.</summary>
    public string? ColumnKey { get; init; }
    /// <summary>The display title of the column that was clicked.</summary>
    public string? ColumnTitle { get; init; }
    /// <summary>The raw cell value.</summary>
    public object? CellValue { get; init; }
    /// <summary>The formatted cell value.</summary>
    public string? FormattedValue { get; init; }
    /// <summary>The mouse event arguments describing the click location.</summary>
    public required MouseEventArgs MouseArgs { get; init; }
}

/// <summary>Describes a property of <typeparamref name="TItem"/> used by the auto-detail panel.</summary>
public sealed class AutoDetailProperty
{
    /// <summary>Initializes a new <see cref="AutoDetailProperty"/> with property metadata.</summary>
    public AutoDetailProperty(System.Reflection.PropertyInfo prop, string label, AutoDetailKind kind, Type itemType)
    {
        Property = prop;
        Label = label;
        Kind = kind;
        ItemType = itemType;
    }
    /// <summary>The reflection property info.</summary>
    public System.Reflection.PropertyInfo Property { get; }
    /// <summary>The display label for the detail field.</summary>
    public string Label { get; }
    /// <summary>Whether this property is an object, collection, or value type.</summary>
    public AutoDetailKind Kind { get; }
    /// <summary>For Object: the object type. For Collection: the element type.</summary>
    public Type ItemType { get; }
}
