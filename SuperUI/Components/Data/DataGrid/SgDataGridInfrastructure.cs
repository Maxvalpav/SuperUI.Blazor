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
    private static readonly JsonSerializerOptions StateJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private const int AutoFitSampleSize = 200;

    private DotNetObjectReference<SgDataGrid<TItem>>? _selfRef;
    private IJSObjectReference? _module;
    private CancellationTokenSource? _lifetimeCts;
    private bool _disposing;

    private bool _hasRendered = false;
    private ElementReference _gridRootRef;
    private ElementReference _chooserRef;
    private ElementReference _exportRef;
    private readonly string _gridId = $"sg_{Guid.NewGuid():N}";

    // Saved views
    private bool _showSavedViewsPanel;
    private bool _rowHighlightRulesInitialized;

    // ── Lifecycle ───────────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposing)
            return;

        _lastRenderedQuickFilterUiVersion = _quickFilterUiVersion;
        _lastRenderedFilterMenuUiVersion = _filterMenuUiVersion;
        _lastRenderedRenderVersion = _renderVersion;

        if (firstRender)
        {
            _hasRendered = true;
            try
            {
                var module = await ModuleCache.GetAsync(JS, "./_content/SuperUI/superui.js", GetLifetimeToken());
                if (_disposing)
                {
                    try { await module.DisposeAsync(); } catch (JSDisconnectedException) { } catch (ObjectDisposedException) { }
                    return;
                }
                _module = module;
                _selfRef = DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("init", _selfRef, _gridRootRef);

                if (_disposing)
                    return;

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

    protected override bool ShouldRender()
    {
        if (!_hasRendered)
            return true;

        var itemsChanged = _itemsVersion != _visibleRowsCacheItemsVersion;
        var filterChanged = _filterVersion != _visibleRowsCacheFilterVersion;
        var sortChanged = _sortVersion != _visibleRowsCacheSortVersion;
        var columnsChanged = _columnsVersion != _visibleRowsCacheColumnsVersion;
        var groupChanged = _groupVersion != _groupTreeCacheGroupVersion;
        var quickFilterTyping = _quickFilterUiVersion != _lastRenderedQuickFilterUiVersion;
        var filterMenuChanged = _filterMenuUiVersion != _lastRenderedFilterMenuUiVersion;

        var renderChanged = _renderVersion != _lastRenderedRenderVersion;

        return itemsChanged || filterChanged || sortChanged || columnsChanged || groupChanged
            || quickFilterTyping || filterMenuChanged || renderChanged;
    }

    // ── State persistence ──────────────────────────────────────────────────────

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
            PageSize = _pageSize,
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
        Logger.LogDebug($"[DataGrid] ImportStateAsync: Loaded pinned columns: {string.Join(",", _pinnedColumns)}");
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
        _pageSize = state.PageSize > 0 ? state.PageSize : _pageSize;

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

    public async Task RefreshAsync()
    {
        _itemsVersion++;
        InvalidateComputedRowsCache();
        await InvokeAsync(StateHasChanged);
    }

    public IReadOnlyList<TItem> GetFilteredItems() => GetFilteredSortedRows();

    public IReadOnlyList<QueryField> GetQueryFields()
    {
        EnsureAutoGeneratedColumns();

        return GetOrderedColumns()
            .Select(col =>
            {
                var type = ResolveColumnType(col);
                IReadOnlyList<SuperUI.Enums.QueryFieldEnumOption>? enumOptions = null;
                if (type.IsEnum)
                {
                    enumOptions = SgEnumHelper.GetItems(type)
                        .Select(ei => new SuperUI.Enums.QueryFieldEnumOption(ei.Name, ei.Label))
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

    // ── Saved Views ─────────────────────────────────────────────────────────────

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

    // ── Export ──────────────────────────────────────────────────────────────────

    private async Task ExportCsvAsync()
    {
        if (_module is null) return;

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
        if (_module is null) return;

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
                    catch (OverflowException) { }
                    catch (InvalidCastException) { }
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

    // ── AutoFit ─────────────────────────────────────────────────────────────────

    private async Task AutoFitAsync()
    {
        if (_module is null) return;

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

    // ── JSInvokable ─────────────────────────────────────────────────────────────

    [JSInvokable]
    public async Task AutoSizeColumnAsync(string key)
    {
        var col = GetColumnByKey(key);
        if (col is null) return;

        var rows = GetVisibleRows();
        var sampleRows = rows.Take(100).ToList();
        var values = sampleRows.Select(r => col.GetDisplay(r)).ToList();

        var widths = await JS.InvokeAsync<Dictionary<string, int>>("measureColumnWidths",
            new[] { new { key = col.Key, title = col.Title, values } }, _gridId);

        if (widths.TryGetValue(key, out var width))
        {
            await SetColumnWidthAsync(key, width);
        }
    }

    [JSInvokable]
    public async Task OnScrollAsync(int scrollTop, int viewportHeight)
    {
        if (_disposing) return;
        if (_scrollTop == scrollTop && _viewportHeight == viewportHeight)
            return;

        _scrollTop = scrollTop;
        _viewportHeight = viewportHeight;

        if (ShouldUseVirtualization())
            _visibleRowsCacheItemsVersion = -1;

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

        _columnsVersion++;
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
            _visibleRowsCacheItemsVersion = -1;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── Clipboard / misc JS interop helpers ─────────────────────────────────────

    public async Task CopySelectionToClipboardAsync()
    {
        if (_module is null) return;

        var cols = VisibleColumns;
        var rows = SelectedItems.Count > 0
            ? GetFilteredSortedRows().Where(r => SelectedItems.Contains(r)).ToList()
            : _activeRow is not null ? new List<TItem> { _activeRow } : new List<TItem>();

        if (rows.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine(string.Join('\t', cols.Select(c => c.Title)));
        foreach (var row in rows)
            sb.AppendLine(string.Join('\t', cols.Select(c => c.GetDisplay(row))));

        try
        {
            await _module.InvokeVoidAsync("copyToClipboard", sb.ToString().TrimEnd());
        }
        catch (JSException) { }
        catch (TaskCanceledException) { }
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

    // ── Lifetime ────────────────────────────────────────────────────────────────

    private CancellationToken GetLifetimeToken()
    {
        _lifetimeCts ??= new CancellationTokenSource();
        return _lifetimeCts.Token;
    }

    public async ValueTask DisposeAsync()
    {
        _disposing = true;

        if (_localeChangedHandler is not null)
            Localizer.OnLocaleChanged -= _localeChangedHandler;

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

            _filterMenuSearchDebounceCts?.Cancel();
            _filterMenuSearchDebounceCts?.Dispose();
            _filterMenuSearchDebounceCts = null;

            _pendingRuleValueDebounceCts?.Cancel();
            _pendingRuleValueDebounceCts?.Dispose();
            _pendingRuleValueDebounceCts = null;

            _scrollDebounceCts?.Cancel();
            _scrollDebounceCts?.Dispose();
            _scrollDebounceCts = null;

            _groupBuildCts?.Cancel();
            _groupBuildCts?.Dispose();
            _groupBuildCts = null;
        }
        catch (ObjectDisposedException) { }

        _lifetimeCts?.Cancel();
        _lifetimeCts?.Dispose();

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

        var selfRef = _selfRef;
        _selfRef = null;
        selfRef?.Dispose();

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

    internal Task RaiseStateChangedAsync() =>
        OnStateChanged.HasDelegate ? OnStateChanged.InvokeAsync() : Task.CompletedTask;
}
