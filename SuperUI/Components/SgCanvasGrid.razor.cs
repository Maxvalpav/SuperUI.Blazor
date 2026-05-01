namespace SuperUI.Components
{
    using Microsoft.AspNetCore.Components;
    using Microsoft.JSInterop;
    using SuperUI.Localization;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Reflection;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Linq.Expressions;
    using System.Text;
    using System.Globalization;
    using Microsoft.AspNetCore.Components.Web;

    public partial class SgCanvasGrid<TItem> : ComponentBase, IAsyncDisposable
    {
        // ── Property accessor cache (per TItem type, shared across all instances) ──
        private sealed class Accessor
        {
            public PropertyInfo Info = default!;
            public Func<TItem, object?> Get = default!;
            public Type UnderlyingType = default!;
        }

        private static readonly Dictionary<string, Accessor?> _accessorCache = new();
        private static readonly object _accessorLock = new();

        private static Accessor? GetAccessor(string name)
        {
            if (_accessorCache.TryGetValue(name, out var a)) return a;
            lock (_accessorLock)
            {
                if (_accessorCache.TryGetValue(name, out a)) return a;
                var pi = typeof(TItem).GetProperty(name);
                if (pi is null) { _accessorCache[name] = null; return null; }

                var p = Expression.Parameter(typeof(TItem), "x");
                Expression body = Expression.Property(p, pi);
                if (pi.PropertyType.IsValueType)
                    body = Expression.Convert(body, typeof(object));
                var lambda = Expression.Lambda<Func<TItem, object?>>(body, p).Compile();

                var acc = new Accessor
                {
                    Info = pi,
                    Get = lambda,
                    UnderlyingType = Nullable.GetUnderlyingType(pi.PropertyType) ?? pi.PropertyType
                };
                _accessorCache[name] = acc;
                return acc;
            }
        }

        private static PropertyInfo? GetProp(string name) => GetAccessor(name)?.Info;

        [Parameter] public IEnumerable<TItem>? Items { get; set; }
        [Parameter] public List<CanvasGridColumn<TItem>>? Columns { get; set; }
        [Parameter] public string Height { get; set; } = "400px";
        [Parameter] public string Width { get; set; } = "100%";
        [Parameter] public int RowHeight { get; set; } = 35;
        [Parameter] public int HeaderHeight { get; set; } = 40;
        [Parameter] public bool AutoGenerateColumns { get; set; } = false;
        [Parameter] public bool ShowStatus { get; set; } = true;
        [Parameter] public bool ShowSelectionColumn { get; set; } = true;
        [Parameter] public bool ShowToolbar { get; set; } = true;
        [Parameter] public bool ShowColumnChooser { get; set; } = true;
        [Parameter] public bool ShowExport { get; set; } = true;

        [Parameter] public bool AllowSelection { get; set; } = true;
        [Parameter] public bool MultiSelect { get; set; } = true;
        [Parameter] public EventCallback<TItem> OnRowClick { get; set; }
        [Parameter] public EventCallback<List<TItem>> SelectedItemsChanged { get; set; }

        [Parameter] public EventCallback<int> CurrentPageChanged { get; set; }
        [Inject] public IJSRuntime JSRuntime { get; set; } = default!;
        [Inject] public ISuperUILocalizer Localizer { get; set; } = default!;
        [Parameter] public bool Loading { get; set; }
        
        // Pagination parameters
        [Parameter] public bool EnablePaging { get; set; } = false;
        [Parameter] public int PageSize { get; set; } = 100;
        private int _currentPage = 1;

        public async Task ExportToCsvAsync(string? fileName = null)
        {
            if (_module is null || _isDisposed) return;
            var rows = _processedItems ?? (Items?.ToList() ?? new List<TItem>());
            var cols = _effectiveColumns.Where(c => !c.IsSystem).ToList();
            if (cols.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", cols.Select(c => CsvEscape(c.Title))));

            foreach (var item in rows)
            {
                var values = new List<string>(cols.Count);
                foreach (var col in cols)
                    values.Add(CsvEscape(GetDisplay(item, col)));
                sb.AppendLine(string.Join(",", values));
            }

            var name = fileName ?? $"export-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            try { await _module.InvokeVoidAsync("downloadCsv", name, sb.ToString()); }
            catch (JSException) { }
            catch (TaskCanceledException) { }
        }

        public async Task ExportExcelAsync(string? fileName = null)
        {
            if (_module is null || _isDisposed) return;
            var rows = _processedItems ?? (Items?.ToList() ?? new List<TItem>());
            var cols = _effectiveColumns.Where(c => !c.IsSystem).ToList();
            if (cols.Count == 0) return;

            var sb = new StringBuilder();
            sb.AppendLine("<table border='1'>");
            sb.AppendLine("  <thead><tr style='background-color: #f2f2f2;'>");
            foreach (var col in cols)
                sb.AppendLine($"    <th>{System.Net.WebUtility.HtmlEncode(col.Title)}</th>");
            sb.AppendLine("  </tr></thead><tbody>");

            foreach (var item in rows)
            {
                sb.AppendLine("  <tr>");
                foreach (var col in cols)
                    sb.AppendLine($"    <td>{System.Net.WebUtility.HtmlEncode(GetDisplay(item, col))}</td>");
                sb.AppendLine("  </tr>");
            }
            sb.AppendLine("</tbody></table>");

            var name = fileName ?? $"export-{DateTime.Now:yyyyMMdd-HHmmss}.xls";
            try { await _module.InvokeVoidAsync("downloadExcel", name, sb.ToString()); }
            catch (JSException) { }
            catch (TaskCanceledException) { }
        }

        /// <summary>
        /// Exports the current canvas view as a PNG image.
        /// </summary>
        public async Task ExportToImageAsync(string? fileName = null)
        {
            if (_module is null || _isDisposed) return;
            var name = fileName ?? $"export-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            try { await _module.InvokeVoidAsync("downloadImage", _canvas, name); }
            catch (JSException) { }
            catch (TaskCanceledException) { }
        }

        private static string GetDisplay(TItem item, CanvasGridColumn<TItem> col)
        {
            var acc = GetAccessor(col.Property);
            if (acc is null) return string.Empty;
            var val = acc.Get(item);
            if (val is null) return string.Empty;
            if (val is bool b) return b ? "✓" : "✗";
            if (!string.IsNullOrEmpty(col.Format))
            {
                // Check if format is a composite format string like "{0:N2} ₽"
                if (col.Format.Contains('{') && col.Format.Contains('}'))
                {
                    try
                    {
                        return string.Format(CultureInfo.CurrentCulture, col.Format, val);
                    }
                    catch
                    {
                        // Fallback to standard formatting
                    }
                }
                // Standard format specifier like "N2", "C2", etc.
                if (val is IFormattable f)
                {
                    try
                    {
                        return f.ToString(col.Format, CultureInfo.CurrentCulture);
                    }
                    catch
                    {
                        // Fallback to ToString
                    }
                }
            }
            return val.ToString() ?? string.Empty;
        }

        private static string CsvEscape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var needsQuote = s.Contains(',') || s.Contains('"') || s.Contains('\n') || s.Contains('\r');
            if (!needsQuote) return s;
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }

        private int _totalItems = 0;

        private int TotalPages => EnablePaging && _processedItems is not null
            ? (int)Math.Ceiling(_processedItems.Count / (double)PageSize)
            : 1;

        // Content-based version fields for cache invalidation
        private int _itemsVersion = 0;
        private int _filterVersion = 0;
        private int _sortVersion = 0;
        private int _columnsVersion = 0;
        
        // Cache for filtered/sorted results
        private List<TItem>? _filteredCache;
        private List<TItem>? _sortedCache;
        private int _filteredCacheItemsVersion = -1;
        private int _filteredCacheFilterVersion = -1;
        private int _sortedCacheFilterVersion = -1;
        private int _sortedCacheSortVersion = -1;
        private List<TItem> _selectedItems = new();
        private HashSet<TItem> _selectedItemsSet = new(); // fast O(1) lookup
        private Dictionary<string, HashSet<string>> _columnFiltersValues = new();
        private Dictionary<string, string> _columnSearchText = new();
        private Dictionary<string, ColumnFilter> _conditionFilters = new();
        private List<FilterRule> _pendingRules = new();
        private bool _pendingRulesAnd = true;
        private bool _showConditionFilter = false;
        private HashSet<string> _groupBy = new();
        private HashSet<string> _collapsedGroups = new();
        private string? _sortProperty;
        private bool _sortDescending;
        private string _filterText = "";
        private List<TItem>? _processedItems;
        private List<TItem?> _rowItemMap = new(); // index in JS data -> TItem (null for group rows)
        private List<CanvasGridColumn<TItem>> _effectiveColumns = new();
        private Dictionary<string, string> _aggregates = new();
        private (string Property, double X, double Y)? _filterPopup;
        private List<string> _currentFilterOptions = new();
        private int _lastClickedIndex = -1;
        private bool _showColumnChooser;
        private bool _showExportMenu;

        // ── Change-tracking for OnParametersSet ──────────────────────────────────
        private IEnumerable<TItem>? _prevItems;
        private List<CanvasGridColumn<TItem>>? _prevColumns;

        // ── Search debounce ───────────────────────────────────────────────────────
        private CancellationTokenSource? _searchDebounce;
        
        // ── Data update cancellation ───────────────────────────────────────────────
        private CancellationTokenSource? _updateCts;

        private (int Index, string Property, double X, double Y, int Width, int Height)? _editingCell;
        private string _editingValue = "";

        private ElementReference _canvas;
        private ElementReference _container;
        private ElementReference _scrollV;
        private ElementReference _scrollH;
        
        private IJSObjectReference? _module;
        private DotNetObjectReference<SgCanvasGrid<TItem>>? _objRef;
        private bool _isDisposed;
        private bool _isLoading = true;

        private double _totalWidth => _effectiveColumns?.Sum(c => c.Width) ?? 0;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;
            try
            {
                _module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-canvasgrid.js");
                if (_isDisposed) return;
                _objRef = DotNetObjectReference.Create(this);
                await _module.InvokeVoidAsync("init", _canvas, _container, _objRef);
                if (_isDisposed) return;
                // Fire-and-forget to avoid blocking initial paint
                _ = SafeUpdateDataAsync();
            }
            catch (JSException) { }
            catch (TaskCanceledException) { }
        }

        private async Task SafeUpdateDataAsync()
        {
            try { await UpdateData(); }
            catch (JSException) { }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SgCanvasGrid.UpdateData] {ex}");
            }
        }

        protected override void OnParametersSet()
        {
            var itemsChanged = !ReferenceEquals(_prevItems, Items);
            var columnsChanged = !ReferenceEquals(_prevColumns, Columns);
            _prevItems = Items;
            _prevColumns = Columns;

            if (itemsChanged)
                _itemsVersion++;
            if (columnsChanged)
                _columnsVersion++;

            if (AutoGenerateColumns && (Columns is null || Columns.Count == 0))
                BuildAutoColumns();

            RebuildEffectiveColumns();

            if (itemsChanged || columnsChanged)
                _ = SafeUpdateDataAsync();
        }

        private void RebuildEffectiveColumns()
        {
            _effectiveColumns = new List<CanvasGridColumn<TItem>>();
            if (ShowSelectionColumn)
            {
                _effectiveColumns.Add(new CanvasGridColumn<TItem>
                {
                    Title = " ",
                    Property = "__selection",
                    Width = 40,
                    IsSystem = true,
                    Pinned = true
                });
            }
            if (Columns is not null)
            {
                _effectiveColumns.AddRange(Columns
                    .Where(c => !c.Hidden)
                    .OrderByDescending(c => c.Pinned));
            }
        }

        private void BuildAutoColumns()
        {
            var type = typeof(TItem);
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<BrowsableAttribute>()?.Browsable != false)
                .Where(p => p.GetCustomAttribute<DisplayAttribute>()?.GetAutoGenerateField() != false)
                .Select(p => new
                {
                    Prop = p,
                    Display = p.GetCustomAttribute<DisplayAttribute>(),
                    Format = p.GetCustomAttribute<DisplayFormatAttribute>(),
                })
                .OrderBy(x => x.Display?.GetOrder() ?? int.MaxValue)
                .ThenBy(x => x.Prop.Name)
                .ToList();

            var autoCols = new List<CanvasGridColumn<TItem>>(props.Count);
            foreach (var x in props)
            {
                var p = x.Prop;
                var underlying = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType;
                var align = IsNumericType(underlying) ? "right" : "left";

                autoCols.Add(new CanvasGridColumn<TItem>
                {
                    Title = x.Display?.GetName() ?? SplitPascalCase(p.Name),
                    Property = p.Name,
                    Align = align,
                    Sortable = true,
                    Filterable = true,
                    Format = ExtractFormat(x.Format?.DataFormatString),
                    ValueType = underlying
                });
            }
            Columns = autoCols;
        }

        private static string? ExtractFormat(string? raw)
        {
            // Preserve full DataFormatString to support both simple (N2) and composite ({0:N2} ₽) formats
            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }

        private static string SplitPascalCase(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length + 4);
            sb.Append(s[0]);
            for (var i = 1; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]) && !char.IsUpper(s[i - 1])) sb.Append(' ');
                sb.Append(s[i]);
            }
            return sb.ToString();
        }

        private bool IsNumericColumn(string property)
        {
            var col = _effectiveColumns.FirstOrDefault(c => c.Property == property);
            if (col == null) return false;
            if (col.ValueType != null) return IsNumericType(col.ValueType);
            
            // Try to infer from TItem property
            var prop = GetProp(property);
            if (prop != null)
            {
                var t = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                return IsNumericType(t);
            }
            return false;
        }

        private static bool IsNumericType(Type? t) =>
            t == typeof(int) || t == typeof(long) || t == typeof(double) ||
            t == typeof(float) || t == typeof(decimal) || t == typeof(short) ||
            t == typeof(byte) || t == typeof(uint) || t == typeof(ulong);

        private readonly struct ColAcc
        {
            public ColAcc(CanvasGridColumn<TItem> col, Accessor? acc) { Col = col; Acc = acc; }
            public readonly CanvasGridColumn<TItem> Col;
            public readonly Accessor? Acc;
        }

        /// <summary>
        /// Result of background data processing - contains all data needed for JS rendering.
        /// Data: each row is either an object?[] (data row, last slot = _selected flag) or a group-row dictionary.
        /// </summary>
        private class ProcessedDataResult
        {
            public List<TItem> ProcessedItems { get; set; } = new();
            public List<object?> Data { get; set; } = new();
            public string[] DataColumnKeys { get; set; } = Array.Empty<string>();
            public List<TItem?> RowItemMap { get; set; } = new();
            public Dictionary<string, string> Aggregates { get; set; } = new();
            public Dictionary<string, string> ActiveFilters { get; set; } = new();
            public int TotalItems { get; set; }
        }

        /// <summary>
        /// Immutable snapshot of UI-thread state captured before background processing,
        /// to avoid races with concurrent UI mutations.
        /// </summary>
        private sealed class ProcessSnapshot
        {
            public List<CanvasGridColumn<TItem>> EffectiveColumns = new();
            public Dictionary<string, HashSet<string>> ColumnFilterValues = new();
            public Dictionary<string, ColumnFilter> ConditionFilters = new();
            public HashSet<string> GroupBy = new();
            public HashSet<string> CollapsedGroups = new();
            public string? SortProperty;
            public bool SortDescending;
            public string FilterText = "";
            public HashSet<TItem> SelectedItemsSet = new();
            public bool EnablePaging;
            public int PageSize;
            public int CurrentPage;
        }

        private static object?[] BuildDataRow(TItem item, List<ColAcc> dataCols, bool selected)
        {
            // Compact array: dataCols.Count values + 1 trailing slot for _selected flag.
            var arr = new object?[dataCols.Count + 1];
            for (var ci = 0; ci < dataCols.Count; ci++)
            {
                var ca = dataCols[ci];
                var val = ca.Acc?.Get(item);
                if (val is bool b) arr[ci] = b ? "✓" : "✗";
                else if (val is not null && !string.IsNullOrEmpty(ca.Col.Format))
                {
                    // Check if format is a composite format string like "{0:N2} ₽"
                    if (ca.Col.Format.Contains('{') && ca.Col.Format.Contains('}'))
                    {
                        try
                        {
                            arr[ci] = string.Format(CultureInfo.CurrentCulture, ca.Col.Format, val);
                        }
                        catch
                        {
                            arr[ci] = val?.ToString();
                        }
                    }
                    else if (val is IFormattable formattable)
                    {
                        try
                        {
                            arr[ci] = formattable.ToString(ca.Col.Format, CultureInfo.CurrentCulture);
                        }
                        catch
                        {
                            arr[ci] = val?.ToString();
                        }
                    }
                    else
                    {
                        arr[ci] = val;
                    }
                }
                else
                    arr[ci] = val;
            }
            arr[dataCols.Count] = selected;
            return arr;
        }

        private async Task UpdateData()
        {
            if (_module is null || _isDisposed) return;

            // Cancel previous update if still running and dispose its CTS to prevent leaks
            var prevCts = _updateCts;
            _updateCts = new CancellationTokenSource();
            var token = _updateCts.Token;
            if (prevCts is not null)
            {
                try { prevCts.Cancel(); } catch (ObjectDisposedException) { }
                prevCts.Dispose();
            }

            if (!Loading)
            {
                _isLoading = true;
                await InvokeAsync(StateHasChanged); // show spinner
            }

            // Snapshot UI-thread state so the background pass cannot race with
            // concurrent mutations (column resize, filter changes, etc.).
            var snap = new ProcessSnapshot
            {
                EffectiveColumns = new List<CanvasGridColumn<TItem>>(_effectiveColumns),
                ColumnFilterValues = _columnFiltersValues.Count == 0
                    ? new Dictionary<string, HashSet<string>>()
                    : _columnFiltersValues.ToDictionary(kv => kv.Key, kv => new HashSet<string>(kv.Value)),
                ConditionFilters = _conditionFilters.Count == 0
                    ? new Dictionary<string, ColumnFilter>()
                    : new Dictionary<string, ColumnFilter>(_conditionFilters),
                GroupBy = new HashSet<string>(_groupBy),
                CollapsedGroups = new HashSet<string>(_collapsedGroups),
                SortProperty = _sortProperty,
                SortDescending = _sortDescending,
                FilterText = _filterText,
                SelectedItemsSet = new HashSet<TItem>(_selectedItemsSet),
                EnablePaging = EnablePaging,
                PageSize = PageSize,
                CurrentPage = _currentPage,
            };

            // Yield twice so Blazor's render queue flushes and the browser paints
            // the spinner before we kick off heavy work.
            await Task.Yield();
            await Task.Delay(16).ConfigureAwait(true);

            try
            {
                // Run heavy data processing on background thread
                var result = await Task.Run(() => ProcessDataInBackground(snap, token), token);

                if (token.IsCancellationRequested) return;

                // Update state on UI thread
                _processedItems = result.ProcessedItems;
                _aggregates = result.Aggregates;
                _rowItemMap = result.RowItemMap;
                _totalItems = result.TotalItems;

                // Send data to JS on UI thread
                await InvokeAsync(async () =>
                {
                    var module = _module;
                    if (module is null || _isDisposed) return;
                    try
                    {
                        await module.InvokeVoidAsync(
                            "setData",
                            _canvas,
                            result.Data,
                            snap.EffectiveColumns,
                            RowHeight,
                            HeaderHeight,
                            result.ActiveFilters,
                            _aggregates,
                            snap.GroupBy,
                            result.DataColumnKeys,
                            snap.SortProperty,
                            snap.SortDescending);
                    }
                    catch (JSException) { }
                    catch (TaskCanceledException) { }
                    catch (ObjectDisposedException) { }

                    if (!Loading)
                    {
                        _isLoading = false;
                        StateHasChanged();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, ignore
            }
        }

        /// <summary>
        /// Processes data (filter, sort, group, build rows) on a background thread.
        /// Reads only from <paramref name="snap"/> and from <see cref="Items"/> (snapshotted locally),
        /// so it cannot race with concurrent UI mutations.
        /// </summary>
        private ProcessedDataResult ProcessDataInBackground(ProcessSnapshot snap, CancellationToken token)
        {
            var result = new ProcessedDataResult();

            // Materialize source once
            List<TItem> src;
            if (Items is List<TItem> asList) src = asList;
            else if (Items is null) src = new List<TItem>(0);
            else src = new List<TItem>(Items);

            // Pre-bind data columns + accessors
            var dataColCount = 0;
            for (var i = 0; i < snap.EffectiveColumns.Count; i++)
                if (!snap.EffectiveColumns[i].IsSystem) dataColCount++;
            var dataCols = new List<ColAcc>(dataColCount);
            for (var i = 0; i < snap.EffectiveColumns.Count; i++)
            {
                var c = snap.EffectiveColumns[i];
                if (!c.IsSystem) dataCols.Add(new ColAcc(c, GetAccessor(c.Property)));
            }

            // Build the column-key array that JS uses to index into row arrays.
            var dataKeys = new string[dataCols.Count];
            for (var i = 0; i < dataCols.Count; i++) dataKeys[i] = dataCols[i].Col.Property;
            result.DataColumnKeys = dataKeys;

            // ── Filter pipeline ───────────────────────────────────────────────────
            // Check if we can use cached filtered results
            List<TItem> filtered;
            if (_filteredCacheItemsVersion == _itemsVersion &&
                _filteredCacheFilterVersion == _filterVersion &&
                _filteredCache is not null)
            {
                filtered = _filteredCache;
            }
            else
            {
                var hasText = !string.IsNullOrWhiteSpace(snap.FilterText);
                var term = snap.FilterText;

                // Pre-bind value-filters
                List<(Accessor Acc, HashSet<string> Allowed)>? valueFilters = null;
                if (snap.ColumnFilterValues.Count > 0)
                {
                    valueFilters = new List<(Accessor, HashSet<string>)>(snap.ColumnFilterValues.Count);
                    foreach (var kv in snap.ColumnFilterValues)
                    {
                        if (kv.Value is null || kv.Value.Count == 0) continue;
                        var acc = GetAccessor(kv.Key);
                        if (acc is null) continue;
                        valueFilters.Add((acc, kv.Value));
                    }
                    if (valueFilters.Count == 0) valueFilters = null;
                }

                // Pre-bind condition filters
                List<(Accessor Acc, ColumnFilter Cf)>? condFilters = null;
                if (snap.ConditionFilters.Count > 0)
                {
                    condFilters = new List<(Accessor, ColumnFilter)>(snap.ConditionFilters.Count);
                    foreach (var kv in snap.ConditionFilters)
                    {
                        var acc = GetAccessor(kv.Key);
                        if (acc is null) continue;
                        condFilters.Add((acc, kv.Value));
                    }
                    if (condFilters.Count == 0) condFilters = null;
                }

                // Single-pass filter
                filtered = new List<TItem>(src.Count);
                for (var i = 0; i < src.Count; i++)
                {
                    if (token.IsCancellationRequested) break;
                    
                    var item = src[i];

                    if (hasText)
                    {
                        var match = false;
                        for (var ci = 0; ci < dataCols.Count; ci++)
                        {
                            var acc = dataCols[ci].Acc;
                            if (acc is null) continue;
                            var v = acc.Get(item);
                            if (v is null) continue;
                            var s = v.ToString();
                            if (s != null && s.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0) { match = true; break; }
                        }
                        if (!match) continue;
                    }

                    if (valueFilters != null)
                    {
                        var ok = true;
                        for (var fi = 0; fi < valueFilters.Count; fi++)
                        {
                            var (acc, allowed) = valueFilters[fi];
                            var val = acc.Get(item);
                            var s = val?.ToString() ?? "(Пусто)";
                            if (!allowed.Contains(s)) { ok = false; break; }
                        }
                        if (!ok) continue;
                    }

                    if (condFilters != null)
                    {
                        var ok = true;
                        for (var fi = 0; fi < condFilters.Count; fi++)
                        {
                            var (acc, cf) = condFilters[fi];
                            var cell = acc.Get(item);
                            bool pass;
                            if (cf.And)
                            {
                                pass = true;
                                for (var ri = 0; ri < cf.Rules.Count; ri++)
                                {
                                    if (!MatchesCondition(cell, cf.Rules[ri].Condition, cf.Rules[ri].Value)) { pass = false; break; }
                                }
                            }
                            else
                            {
                                pass = false;
                                for (var ri = 0; ri < cf.Rules.Count; ri++)
                                {
                                    if (MatchesCondition(cell, cf.Rules[ri].Condition, cf.Rules[ri].Value)) { pass = true; break; }
                                }
                            }
                            if (!pass) { ok = false; break; }
                        }
                        if (!ok) continue;
                    }

                    filtered.Add(item);
                }
                
                // Update cache
                _filteredCache = filtered;
                _filteredCacheItemsVersion = _itemsVersion;
                _filteredCacheFilterVersion = _filterVersion;
            }

            if (token.IsCancellationRequested) throw new OperationCanceledException(token);

            // ── Sort ─────────────────────────────────────────────────────────────
            // Check if we can use cached sorted results
            if (!string.IsNullOrEmpty(snap.SortProperty))
            {
                if (_sortedCacheFilterVersion == _filterVersion &&
                    _sortedCacheSortVersion == _sortVersion &&
                    _sortedCache is not null)
                {
                    filtered = _sortedCache;
                }
                else
                {
                    var sortAcc = GetAccessor(snap.SortProperty);
                    if (sortAcc is not null)
                    {
                        var get = sortAcc.Get;
                        var desc = snap.SortDescending;
                        filtered.Sort((a, b) =>
                        {
                            var cmp = NullableComparer.Instance.Compare(get(a), get(b));
                            return desc ? -cmp : cmp;
                        });
                    }

                    // Update cache
                    _sortedCache = filtered;
                    _sortedCacheFilterVersion = _filterVersion;
                    _sortedCacheSortVersion = _sortVersion;
                }
            }

            result.ProcessedItems = filtered;
            result.Aggregates = CalculateAggregates(filtered, snap.EffectiveColumns);

            var rowItemMap = new List<TItem?>();
            var data = new List<object?>();
            int totalItems;
            int startIndex = 0;
            int endIndex = filtered.Count;

            // Apply pagination if enabled
            if (snap.EnablePaging && filtered.Count > 0)
            {
                var totalPages = (int)Math.Ceiling(filtered.Count / (double)snap.PageSize);
                var page = Math.Clamp(snap.CurrentPage, 1, totalPages);
                startIndex = (page - 1) * snap.PageSize;
                endIndex = Math.Min(startIndex + snap.PageSize, filtered.Count);
            }

            if (snap.GroupBy.Count > 0)
            {
                var rowsToDisplay = new List<object>(filtered.Count + 16);
                var tree = BuildGroupTree(filtered, snap);
                FlattenGroupTree(tree, rowsToDisplay, snap.CollapsedGroups, snap.EffectiveColumns);
                totalItems = rowsToDisplay.Count;

                // For grouping, pagination is not supported - show all
                if (rowItemMap.Capacity < rowsToDisplay.Count) rowItemMap.Capacity = rowsToDisplay.Count;
                data.Capacity = rowsToDisplay.Count;

                for (var i = 0; i < rowsToDisplay.Count; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var rowObj = rowsToDisplay[i];
                    if (rowObj is Dictionary<string, object?> groupDict)
                    {
                        rowItemMap.Add(default);
                        data.Add(groupDict);
                    }
                    else if (rowObj is TItem item)
                    {
                        var arr = BuildDataRow(item, dataCols, snap.SelectedItemsSet.Contains(item));
                        rowItemMap.Add(item);
                        data.Add(arr);
                    }
                }
            }
            else
            {
                totalItems = filtered.Count;

                // Only process current page if pagination is enabled
                var itemsToProcess = snap.EnablePaging
                    ? filtered.GetRange(startIndex, endIndex - startIndex)
                    : filtered;

                if (rowItemMap.Capacity < itemsToProcess.Count) rowItemMap.Capacity = itemsToProcess.Count;
                data.Capacity = itemsToProcess.Count;

                for (var i = 0; i < itemsToProcess.Count; i++)
                {
                    if (token.IsCancellationRequested) break;

                    var item = itemsToProcess[i];
                    if (item is null) continue;

                    var arr = BuildDataRow(item, dataCols, snap.SelectedItemsSet.Contains(item));
                    rowItemMap.Add(item);
                    data.Add(arr);
                }
            }

            if (token.IsCancellationRequested) throw new OperationCanceledException(token);

            var activeFilters = new Dictionary<string, string>(dataCols.Count);
            for (var ci = 0; ci < dataCols.Count; ci++)
            {
                var prop = dataCols[ci].Col.Property;
                var active = (snap.ColumnFilterValues.TryGetValue(prop, out var v) && v.Count > 0)
                             || snap.ConditionFilters.ContainsKey(prop);
                activeFilters[prop] = active ? "active" : "";
            }

            result.Data = data;
            result.RowItemMap = rowItemMap;
            result.ActiveFilters = activeFilters;
            result.TotalItems = totalItems;

            return result;
        }

        private sealed class NullableComparer : IComparer<object?>
        {
            public static readonly NullableComparer Instance = new();
            public int Compare(object? x, object? y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (x is null) return -1;
                if (y is null) return 1;
                if (x.GetType() == y.GetType() && x is IComparable cx) return cx.CompareTo(y);
                if (IsNum(x) && IsNum(y))
                {
                    var dx = Convert.ToDouble(x, CultureInfo.InvariantCulture);
                    var dy = Convert.ToDouble(y, CultureInfo.InvariantCulture);
                    return dx.CompareTo(dy);
                }
                return string.Compare(x.ToString(), y.ToString(), StringComparison.CurrentCulture);
            }
            private static bool IsNum(object o) =>
                o is int or long or decimal or double or float or short or byte or sbyte or uint or ulong or ushort;
        }

        public async Task ClearSort()
        {
            _sortProperty = null;
            _sortDescending = false;
            _sortVersion++;
            await UpdateData();
        }

        [JSInvokable]
        public async Task OnHeaderClick(string property)
        {
            if (property == "__selection") return;
            var col = _effectiveColumns.FirstOrDefault(c => c.Property == property);
            if (col is null || !col.Sortable) return;

            if (_sortProperty == property)
            {
                if (_sortDescending) { _sortProperty = null; _sortDescending = false; }
                else _sortDescending = true;
            }
            else
            {
                _sortProperty = property;
                _sortDescending = false;
            }
            _sortVersion++;
            await UpdateData();
        }

        [JSInvokable]
        public async Task OnToggleSelectAll()
        {
            if (_processedItems is null || _processedItems.Count == 0)
            {
                if (_selectedItems.Count > 0)
                {
                    _selectedItems.Clear();
                    _selectedItemsSet.Clear();
                    await SelectedItemsChanged.InvokeAsync(_selectedItems);
                    await UpdateData();
                }
                return;
            }

            // Быстрая проверка через множество, не через .All(...)
            var allSelected = _selectedItemsSet.Count == _processedItems.Count
                              && _processedItems.All(i => i is not null && _selectedItemsSet.Contains(i));

            if (allSelected)
            {
                _selectedItems.Clear();
                _selectedItemsSet.Clear();
            }
            else
            {
                // Один проход без промежуточного ToList
                if (_selectedItemsSet.Count != 0) { _selectedItems.Clear(); _selectedItemsSet.Clear(); }
                _selectedItems.Capacity = _processedItems.Count;
                _selectedItemsSet.EnsureCapacity(_processedItems.Count);
                for (var i = 0; i < _processedItems.Count; i++)
                {
                    var it = _processedItems[i];
                    if (it is null) continue;
                    _selectedItems.Add(it);
                    _selectedItemsSet.Add(it);
                }
            }

            // Лёгкий путь: меняем только флаги в JS
            if (_groupBy.Count == 0 && _module is not null && !_isDisposed)
            {
                try { await _module.InvokeVoidAsync("setSelectionAll", _canvas, !allSelected); }
                catch (JSException) { } catch (TaskCanceledException) { }
                await SelectedItemsChanged.InvokeAsync(_selectedItems);
                StateHasChanged(); // обновить статус-строку
                return;
            }

            await SelectedItemsChanged.InvokeAsync(_selectedItems);
            await UpdateData(); // fallback для группировки
        }

        [JSInvokable]
        public async Task OnShowFilter(string property, double x, double y, double containerWidth = 0, double containerHeight = 0)
        {
            if (Items is null) return;

            var col = _effectiveColumns.FirstOrDefault(c => c.Property == property);
            if (col is null || !col.Filterable) return;

            var acc = GetAccessor(property);
            if (acc is null) return;

            // Ensure dictionary keys exist to avoid KeyNotFoundException during binding
            if (!_columnSearchText.ContainsKey(property))
            {
                _columnSearchText[property] = string.Empty;
            }

            // Snapshot Items so the background pass can't see a concurrently mutated source.
            var src = Items as IReadOnlyCollection<TItem> ?? Items.ToList();
            var get = acc.Get;

            // Compute distinct values on background thread to avoid blocking UI
            _currentFilterOptions = await Task.Run(() =>
            {
                var set = new HashSet<string>();
                foreach (var i in src)
                    set.Add(get(i)?.ToString() ?? "(Пусто)");
                var list = set.ToList();
                list.Sort(StringComparer.CurrentCulture);
                return list;
            });

            // Clamp popup position to grid container so it doesn't overflow.
            const double popupWidth = 280;
            const double popupHeight = 450;
            const double margin = 8;
            var clampedX = x;
            var clampedY = y;
            if (containerWidth > 0)
            {
                if (clampedX + popupWidth + margin > containerWidth)
                    clampedX = Math.Max(margin, containerWidth - popupWidth - margin);
                if (clampedX < margin) clampedX = margin;
            }
            if (containerHeight > 0)
            {
                if (clampedY + popupHeight + margin > containerHeight)
                    clampedY = Math.Max(margin, containerHeight - popupHeight - margin);
                if (clampedY < margin) clampedY = margin;
            }

            _filterPopup = (property, clampedX, clampedY);

            if (_conditionFilters.TryGetValue(property, out var cf))
            {
                _pendingRules = cf.Rules.Select(r => new FilterRule { Condition = r.Condition, Value = r.Value }).ToList();
                _pendingRulesAnd = cf.And;
                _showConditionFilter = true;
            }
            else
            {
                _pendingRules = new List<FilterRule> { new FilterRule() };
                _pendingRulesAnd = true;
                _showConditionFilter = false;
            }

            await InvokeAsync(StateHasChanged);
        }

        [JSInvokable]
        public async Task OnColumnResized(List<ColumnResizeInfo> columnInfos)
        {
            if (Columns == null) return;

            foreach (var info in columnInfos)
            {
                var col = Columns.FirstOrDefault(c => c.Property == info.Property);
                if (col != null)
                {
                    col.Width = (int)info.Width;
                }

                // Also update effective columns
                var effCol = _effectiveColumns.FirstOrDefault(c => c.Property == info.Property);
                if (effCol != null)
                {
                    effCol.Width = (int)info.Width;
                }
            }
            await InvokeAsync(StateHasChanged);
        }

        public class ColumnResizeInfo
        { 
            public string Property { get; set; } = "";
            public double Width { get; set; }
        }

        private void ToggleFilterValue(string property, string value)
        {
            if (!_columnFiltersValues.ContainsKey(property))
            {
                _columnFiltersValues[property] = new HashSet<string>();
            }

            if (_columnFiltersValues[property].Contains(value))
            {
                _columnFiltersValues[property].Remove(value);
            }
            else
            {
                _columnFiltersValues[property].Add(value);
            }
        }

        private bool IsFilterValueSelected(string property, string value)
        {
            return _columnFiltersValues.ContainsKey(property) && _columnFiltersValues[property].Contains(value);
        }

        private void SelectAllFilterValues(string property)
        {
            _columnFiltersValues.Remove(property);
        }

        private async Task ApplyCurrentFilter()
        {
            if (_filterPopup != null)
            {
                _conditionFilters.Remove(_filterPopup.Value.Property);
            }
            _filterPopup = null;
            _filterVersion++;
            _currentPage = 1;
            await UpdateData();
        }

        private async Task ClearFilter(string property)
        {
            _columnFiltersValues.Remove(property);
            _columnSearchText.Remove(property);
            _conditionFilters.Remove(property);
            _filterPopup = null;
            _filterVersion++;
            _currentPage = 1;
            await UpdateData();
        }

        private async Task ResetAll()
        {
            _columnFiltersValues.Clear();
            _columnSearchText.Clear();
            _conditionFilters.Clear();
            _sortProperty = null;
            _sortDescending = false;
            _filterText = "";
            _filterPopup = null;
            _currentPage = 1;
            _filterVersion++;
            _sortVersion++;
            await UpdateData();
        }

        // Pagination methods
        private async Task GoToPage(int page)
        {
            if (!EnablePaging) return;
            _currentPage = Math.Clamp(page, 1, TotalPages);
            await CurrentPageChanged.InvokeAsync(_currentPage);
            await UpdateData();
        }

        private async Task NextPage()
        {
            if (!EnablePaging || _currentPage >= TotalPages) return;
            await GoToPage(_currentPage + 1);
        }

        private async Task PreviousPage()
        {
            if (!EnablePaging || _currentPage <= 1) return;
            await GoToPage(_currentPage - 1);
        }

        private async Task FirstPage()
        {
            if (!EnablePaging || _currentPage == 1) return;
            await GoToPage(1);
        }

        private async Task LastPage()
        {
            if (!EnablePaging || _currentPage == TotalPages) return;
            await GoToPage(TotalPages);
        }

        private async Task ApplyConditionFilter()
        {
            if (_filterPopup == null) return;
            var property = _filterPopup.Value.Property;

            var activeRules = _pendingRules
                .Where(r => r.Condition == FilterCondition.IsEmpty || r.Condition == FilterCondition.IsNotEmpty || !string.IsNullOrEmpty(r.Value))
                .ToList();

            if (activeRules.Count > 0)
                _conditionFilters[property] = new ColumnFilter(activeRules, _pendingRulesAnd);
            else
                _conditionFilters.Remove(property);

            _columnFiltersValues.Remove(property);
            _filterPopup = null;
            _filterVersion++;
            _currentPage = 1;
            await UpdateData();
        }

        private static bool MatchesCondition(object? cellValue, FilterCondition condition, string? filterValue)
        {
            var cell = cellValue?.ToString() ?? string.Empty;
            var fv = filterValue ?? string.Empty;

            return condition switch
            {
                FilterCondition.Contains => cell.IndexOf(fv, StringComparison.CurrentCultureIgnoreCase) >= 0,
                FilterCondition.NotContains => cell.IndexOf(fv, StringComparison.CurrentCultureIgnoreCase) < 0,
                FilterCondition.Equals => string.Equals(cell, fv, StringComparison.CurrentCultureIgnoreCase),
                FilterCondition.NotEquals => !string.Equals(cell, fv, StringComparison.CurrentCultureIgnoreCase),
                FilterCondition.StartsWith => cell.StartsWith(fv, StringComparison.CurrentCultureIgnoreCase),
                FilterCondition.EndsWith => cell.EndsWith(fv, StringComparison.CurrentCultureIgnoreCase),
                FilterCondition.IsEmpty => string.IsNullOrEmpty(cell),
                FilterCondition.IsNotEmpty => !string.IsNullOrEmpty(cell),
                FilterCondition.GreaterThan => CompareNumeric(cellValue, fv) > 0,
                FilterCondition.LessThan => CompareNumeric(cellValue, fv) < 0,
                FilterCondition.GreaterOrEqual => CompareNumeric(cellValue, fv) >= 0,
                FilterCondition.LessOrEqual => CompareNumeric(cellValue, fv) <= 0,
                _ => true
            };
        }

        private static int CompareNumeric(object? cellValue, string filterValue)
        {
            if (!double.TryParse(filterValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var fv))
            {
                if (!double.TryParse(filterValue, NumberStyles.Any, CultureInfo.CurrentCulture, out fv))
                    return 0;
            }
            
            try
            {
                double cv;
                if (cellValue is IConvertible)
                    cv = Convert.ToDouble(cellValue, CultureInfo.InvariantCulture);
                else
                    cv = double.Parse(cellValue?.ToString() ?? "0", CultureInfo.InvariantCulture);
                
                return cv.CompareTo(fv);
            }
            catch 
            {
                try
                {
                    var cv = Convert.ToDouble(cellValue, CultureInfo.CurrentCulture);
                    return cv.CompareTo(fv);
                }
                catch { return 0; }
            }
        }

        private void AddPendingRule()
        {
            _pendingRules.Add(new FilterRule());
        }

        private void RemovePendingRule(int index)
        {
            if (_pendingRules.Count > 1)
                _pendingRules.RemoveAt(index);
        }

        private async Task SetSort(string property, bool descending)
        {
            _sortProperty = property;
            _sortDescending = descending;
            _filterPopup = null;
            await UpdateData();
        }

        private void CloseFilter()
        {
            _filterPopup = null;
        }

        private void ToggleColumnChooser()
        {
            _showColumnChooser = !_showColumnChooser;
            _showExportMenu = false;
        }

        private void ToggleExportMenu()
        {
            _showExportMenu = !_showExportMenu;
            _showColumnChooser = false;
        }

        private async Task ToggleColumnVisibility(CanvasGridColumn<TItem> col)
        {
            col.Hidden = !col.Hidden;
            RebuildEffectiveColumns();
            await UpdateData();
        }

        [JSInvokable]
        public async Task ToggleGroupBy(string property)
        {
            if (!_groupBy.Add(property)) _groupBy.Remove(property);
            _collapsedGroups.Clear();
            _filterPopup = null;
            await UpdateData();
        }

        [JSInvokable]
        public async Task ToggleGroupCollapsed(string path)
        {
            if (!_collapsedGroups.Add(path)) _collapsedGroups.Remove(path);
            await UpdateData();
        }

        private Dictionary<string, string> CalculateAggregates(List<TItem> items, List<CanvasGridColumn<TItem>> columns)
        {
            var resultDict = new Dictionary<string, string>();
            for (var ci = 0; ci < columns.Count; ci++)
            {
                var col = columns[ci];
                if (col.IsSystem || col.Aggregate == Aggregate.None) continue;

                var acc = GetAccessor(col.Property);
                if (acc is null) continue;
                var get = acc.Get;

var prefix = col.Aggregate switch
                 {
                     Aggregate.Sum => Localizer["DataGrid_AggregateSum"] + ": ",
                     Aggregate.Average => Localizer["DataGrid_AggregateAverage"] + ": ",
                     Aggregate.Count => Localizer["DataGrid_AggregateCount"] + ": ",
                     Aggregate.Min => Localizer["DataGrid_AggregateMin"] + ": ",
                     Aggregate.Max => Localizer["DataGrid_AggregateMax"] + ": ",
                     _ => ""
                 };

                if (col.Aggregate == Aggregate.Count)
                {
                    resultDict[col.Property] = prefix + items.Count.ToString(CultureInfo.CurrentCulture);
                    continue;
                }

                double sum = 0, min = double.PositiveInfinity, max = double.NegativeInfinity;
                int count = 0;
                bool failed = false;
                for (var i = 0; i < items.Count; i++)
                {
                    var v = get(items[i]);
                    if (v is null) continue;
                    double d;
                    try { d = Convert.ToDouble(v, CultureInfo.InvariantCulture); }
                    catch { failed = true; break; }
                    sum += d;
                    if (d < min) min = d;
                    if (d > max) max = d;
                    count++;
                }
                if (failed || count == 0) continue;

                double result = col.Aggregate switch
                {
                    Aggregate.Sum => sum,
                    Aggregate.Average => sum / count,
                    Aggregate.Min => min,
                    Aggregate.Max => max,
                    _ => 0
                };

                var fmt = string.IsNullOrEmpty(col.Format) ? "N2" : col.Format;
                resultDict[col.Property] = prefix + result.ToString(fmt, CultureInfo.CurrentCulture);
            }
            return resultDict;
        }

        private List<GroupNode> BuildGroupTree(List<TItem> items, ProcessSnapshot snap)
        {
            // Order group keys by their position in snap.GroupBy
            var groupByList = snap.GroupBy.ToList();
            var keys = new List<(CanvasGridColumn<TItem> Col, Accessor? Acc)>(groupByList.Count);
            for (var gi = 0; gi < groupByList.Count; gi++)
            {
                var prop = groupByList[gi];
                CanvasGridColumn<TItem>? found = null;
                for (var ci = 0; ci < snap.EffectiveColumns.Count; ci++)
                {
                    if (snap.EffectiveColumns[ci].Property == prop) { found = snap.EffectiveColumns[ci]; break; }
                }
                if (found is not null) keys.Add((found, GetAccessor(prop)));
            }

            if (keys.Count == 0) return new List<GroupNode>();

            var root = new List<GroupNode>();
            var rootIndex = new Dictionary<string, GroupNode>();
            var path = new StringBuilder(64);

            for (var ii = 0; ii < items.Count; ii++)
            {
                var item = items[ii];
                path.Clear();
                var level = root;
                var index = rootIndex;

                for (var i = 0; i < keys.Count; i++)
                {
                    var (col, acc) = keys[i];
                    var val = acc?.Get(item);
                    var label = val?.ToString() ?? "(Пусто)";

                    if (path.Length > 0) path.Append('\u001f');
                    path.Append(col.Property).Append('=').Append(label);
                    var pathStr = path.ToString();

                    if (!index.TryGetValue(pathStr, out var node))
                    {
                        node = new GroupNode
                        {
                            Property = col.Property,
                            Title = col.Title,
                            Label = label,
                            Path = pathStr,
                            Depth = i,
                        };
                        level.Add(node);
                        index[pathStr] = node;
                    }
                    if (i == keys.Count - 1) node.Items.Add(item);
                    level = node.Children;
                    index = node.ChildIndex;
                }
            }
            return root;
        }

        private void FlattenGroupTree(List<GroupNode> nodes, List<object> result, HashSet<string> collapsedGroups, List<CanvasGridColumn<TItem>> columns)
        {
            for (var ni = 0; ni < nodes.Count; ni++)
            {
                var node = nodes[ni];
                var allItems = node.GetAllItems();
                var groupAggregates = CalculateAggregates(allItems, columns);

                var groupRow = new Dictionary<string, object?>(6 + groupAggregates.Count)
                {
                    ["_isGroupRow"] = true,
                    ["_groupPath"] = node.Path,
                    ["_groupLabel"] = node.Title + ": " + node.Label,
                    ["_groupDepth"] = node.Depth,
                    ["_isCollapsed"] = collapsedGroups.Contains(node.Path),
                    ["_count"] = node.GetTotalCount()
                };

                foreach (var agg in groupAggregates)
                    groupRow[agg.Key] = agg.Value;

                result.Add(groupRow);

                if (!collapsedGroups.Contains(node.Path))
                {
                    if (node.Children.Count > 0)
                    {
                        FlattenGroupTree(node.Children, result, collapsedGroups, columns);
                    }
                    else
                    {
                        var items = node.Items;
                        for (var ii = 0; ii < items.Count; ii++)
                        {
                            var it = items[ii];
                            if (it is not null) result.Add(it);
                        }
                    }
                }
            }
        }

        private sealed class GroupNode
        {
            public string Property = "";
            public string Title = "";
            public string Label = "";
            public string Path = "";
            public int Depth;
            public List<GroupNode> Children { get; } = new();
            public Dictionary<string, GroupNode> ChildIndex { get; } = new();
            public List<TItem> Items { get; } = new();

            private int _totalCount = -1;
            private List<TItem>? _allItemsCache;

            public int GetTotalCount()
            {
                if (_totalCount >= 0) return _totalCount;
                var c = Items.Count;
                for (var i = 0; i < Children.Count; i++) c += Children[i].GetTotalCount();
                _totalCount = c;
                return c;
            }

            public List<TItem> GetAllItems()
            {
                if (_allItemsCache is not null) return _allItemsCache;
                if (Items.Count > 0 && Children.Count == 0) { _allItemsCache = Items; return Items; }
                var list = new List<TItem>(GetTotalCount());
                Collect(this, list);
                _allItemsCache = list;
                return list;
            }

            private static void Collect(GroupNode node, List<TItem> list)
            {
                var items = node.Items;
                for (var i = 0; i < items.Count; i++) list.Add(items[i]);
                var children = node.Children;
                for (var i = 0; i < children.Count; i++) Collect(children[i], list);
            }
        }

        [JSInvokable]
        public Task OnRowDoubleClickInternal(int index, string property, double x, double y, int width, int height)
        {
            if (index < 0 || index >= _rowItemMap.Count) return Task.CompletedTask;
            var item = _rowItemMap[index];
            if (item is null) return Task.CompletedTask;

            var col = _effectiveColumns.FirstOrDefault(c => c.Property == property);
            if (col is null || !col.Editable || col.IsSystem) return Task.CompletedTask;

            var acc = GetAccessor(property);
            if (acc is null) return Task.CompletedTask;

            _editingCell = (index, property, x, y, width, height);
            _editingValue = acc.Get(item)?.ToString() ?? string.Empty;
            StateHasChanged();
            return Task.CompletedTask;
        }

        private async Task EndEdit()
        {
            if (_editingCell is null) { return; }

            var (index, property, _, _, _, _) = _editingCell.Value;
            _editingCell = null;

            if (index < 0 || index >= _rowItemMap.Count) { await UpdateData(); return; }
            var item = _rowItemMap[index];
            if (item is null) { await UpdateData(); return; }

            var col = _effectiveColumns.FirstOrDefault(c => c.Property == property);
            var prop = GetProp(property);
            if (prop is not null && col is not null)
            {
                var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                object? val = null;
                var raw = _editingValue;
                try
                {
                    if (string.IsNullOrEmpty(raw))
                        val = targetType.IsValueType && Nullable.GetUnderlyingType(prop.PropertyType) is null
                            ? Activator.CreateInstance(targetType)
                            : null;
                    else if (targetType == typeof(string)) val = raw;
                    else if (targetType.IsEnum) val = Enum.Parse(targetType, raw, true);
                    else if (targetType == typeof(bool)) val = bool.Parse(raw);
                    else if (targetType == typeof(DateTime)) val = DateTime.Parse(raw, CultureInfo.CurrentCulture);
                    else val = Convert.ChangeType(raw, targetType, CultureInfo.CurrentCulture);

                    prop.SetValue(item, val);
                    col.OnValueChanged?.Invoke(item, val);
                }
                catch { /* keep old value */ }
            }
            await UpdateData();
        }

        private async Task OnEditorKeyDown(KeyboardEventArgs e)
        {
            if (e.Key == "Enter") await EndEdit();
            else if (e.Key == "Escape")
            {
                _editingCell = null;
                StateHasChanged();
            }
        }

        [JSInvokable]
        public async Task OnRowClickInternal(int index, bool shift, bool ctrl)
        {
            if (index < 0 || index >= _rowItemMap.Count) return;
            var item = _rowItemMap[index];
            if (item is null) return; // group row

            if (AllowSelection)
            {
                var changedSet = new HashSet<int>();
                var isSelectedNow = _selectedItemsSet.Contains(item);

                if (MultiSelect && shift && _lastClickedIndex != -1 && _lastClickedIndex < _rowItemMap.Count)
                {
                    var start = Math.Min(_lastClickedIndex, index);
                    var end = Math.Max(_lastClickedIndex, index);
                    if (!ctrl)
                    {
                        // Collect indices of currently selected items to unselect them in JS
                        for (int i = 0; i < _rowItemMap.Count; i++)
                        {
                            var r = _rowItemMap[i];
                            if (r is not null && _selectedItemsSet.Contains(r)) changedSet.Add(i);
                        }
                        _selectedItems.Clear();
                        _selectedItemsSet.Clear();
                    }
                    for (var i = start; i <= end; i++)
                    {
                        var r = _rowItemMap[i];
                        if (r is not null && !_selectedItemsSet.Contains(r))
                        {
                            _selectedItems.Add(r);
                            _selectedItemsSet.Add(r);
                            changedSet.Add(i);
                        }
                    }
                }
                else if (MultiSelect && (ctrl || ShowSelectionColumn))
                {
                    if (isSelectedNow)
                    {
                        _selectedItems.Remove(item);
                        _selectedItemsSet.Remove(item);
                    }
                    else
                    {
                        _selectedItems.Add(item);
                        _selectedItemsSet.Add(item);
                    }
                    changedSet.Add(index);
                }
                else
                {
                    // Clear previous
                    for (int i = 0; i < _rowItemMap.Count; i++)
                    {
                        var r = _rowItemMap[i];
                        if (r is not null && _selectedItemsSet.Contains(r)) changedSet.Add(i);
                    }
                    _selectedItems.Clear();
                    _selectedItemsSet.Clear();
                    _selectedItems.Add(item);
                    _selectedItemsSet.Add(item);
                    changedSet.Add(index);
                }

                _lastClickedIndex = index;
                await SelectedItemsChanged.InvokeAsync(_selectedItems);

                // Лёгкий путь: если нет группировки, шлём только индексы
                if (_groupBy.Count == 0 && _module is not null && !_isDisposed && changedSet.Count > 0)
                {
                    var selectedIndices = new List<int>();
                    var unselectedIndices = new List<int>();
                    foreach (var i in changedSet)
                    {
                        var r = _rowItemMap[i];
                        if (r is null) continue;
                        if (_selectedItemsSet.Contains(r)) selectedIndices.Add(i);
                        else unselectedIndices.Add(i);
                    }

                    try
                    {
                        if (selectedIndices.Count > 0)
                            await _module.InvokeVoidAsync("setSelectionAt", _canvas, selectedIndices, true);
                        if (unselectedIndices.Count > 0)
                            await _module.InvokeVoidAsync("setSelectionAt", _canvas, unselectedIndices, false);
                    }
                    catch (JSException) { } catch (TaskCanceledException) { } catch (ObjectDisposedException) { }
                    StateHasChanged();
                    return;
                }

                await UpdateData();
            }

            await OnRowClick.InvokeAsync(item);
        }

        // ── Search with 300ms debounce ────────────────────────────────────────────
        private async Task OnSearchInput(ChangeEventArgs e)
        {
            _filterText = e.Value?.ToString() ?? string.Empty;
            _filterVersion++;
            _currentPage = 1;
            var prev = _searchDebounce;
            _searchDebounce = new CancellationTokenSource();
            var token = _searchDebounce.Token;
            if (prev is not null)
            {
                try { prev.Cancel(); } catch (ObjectDisposedException) { }
                prev.Dispose();
            }
            try
            {
                await Task.Delay(300, token);
                await UpdateData();
            }
            catch (TaskCanceledException) { }
            catch (OperationCanceledException) { }
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            try { _searchDebounce?.Cancel(); } catch (ObjectDisposedException) { }
            _searchDebounce?.Dispose();
            try { _updateCts?.Cancel(); } catch (ObjectDisposedException) { }
            _updateCts?.Dispose();
            if (_module is not null)
            {
                try { await _module.InvokeVoidAsync("dispose", _canvas); }
                catch (JSException) { }
                catch (TaskCanceledException) { }
                catch (ObjectDisposedException) { }
                try { await _module.DisposeAsync(); }
                catch (JSException) { }
                catch (TaskCanceledException) { }
                catch (ObjectDisposedException) { }
            }
            _objRef?.Dispose();
        }
    }

    public class CanvasGridColumn<TItemCol>
    {
        public string Title { get; set; } = string.Empty;
        public string Property { get; set; } = string.Empty;
        public int Width { get; set; } = 150;
        public string? Format { get; set; }
        public bool IsSystem { get; set; } = false;
        public string Align { get; set; } = "left"; // "left", "center", "right"
        public bool Sortable { get; set; } = true;
        public bool Filterable { get; set; } = true;
        public bool Pinned { get; set; } = false;
        public bool Hidden { get; set; } = false;
        public bool Editable { get; set; } = false;
        [JsonIgnore]
        public Action<TItemCol, object?>? OnValueChanged { get; set; }
        public Aggregate Aggregate { get; set; } = Aggregate.None;
        [JsonIgnore]
        public Type? ValueType { get; set; }
    }
}
