using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Cascaded selection component for hierarchical data such as locations or categories.
/// Supports filtering, lazy loading, checkable multi-select, icons, badges, keyboard nav, and more.
/// </summary>
public sealed partial class SgCascader : IAsyncDisposable
{
    // ── Injected Services ────────────────────────────────────────────────
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ── JS Module ────────────────────────────────────────────────────────
    private IJSObjectReference? _jsModule;
    private ElementReference _rootRef;
    private ElementReference _searchInputRef;
    private readonly string _menuId = $"sgc-cascader-menu-{Guid.NewGuid():N}";

    // ── State ────────────────────────────────────────────────────────────
    private bool _open;
    private bool _focusWithin;
    private bool _disposed;
    private bool _isLoadingChildren;
    private CancellationTokenSource? _hoverCts;
    private List<string> _selectedPath = new();
    private string _filterText = string.Empty;
    private DotNetObjectReference<SgCascader>? _dotNetRef;
    private HashSet<string> _checkedValues = new();
    private bool _checkInitialized;

    // ── Parameters ───────────────────────────────────────────────────────

    /// <summary>Root options for the cascader.</summary>
    [Parameter, EditorRequired]
    public List<SgCascaderOption> Options { get; set; } = new();

    /// <summary>Selected value path (list of values from each level).</summary>
    [Parameter]
    public List<string> Value { get; set; } = new();

    /// <summary>Fires when the selected value changes.</summary>
    [Parameter]
    public EventCallback<List<string>> ValueChanged { get; set; }

    /// <summary>
    /// Fires on every selection change with the full list of selected option objects.
    /// The list contains one <see cref="SgCascaderOption"/> per level of the selected path.
    /// </summary>
    [Parameter]
    public EventCallback<List<SgCascaderOption>> OnChange { get; set; }

    /// <summary>Placeholder text when nothing is selected.</summary>
    [Parameter]
    public string Placeholder { get; set; } = "Select...";

    /// <summary>Separator for displaying the selected path.</summary>
    [Parameter]
    public string Separator { get; set; } = " / ";

    /// <summary>Makes the cascader take full width.</summary>
    [Parameter]
    public bool Block { get; set; } = true;

    /// <summary>Disables the cascader.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Additional CSS class for the root element.</summary>
    [Parameter]
    public string? Class { get; set; }

    /// <summary>Size variant.</summary>
    [Parameter]
    public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>When true shows a clear button when a value is selected.</summary>
    [Parameter]
    public bool ShowClear { get; set; } = true;

    /// <summary>When true shows a search box at the top of the menu.</summary>
    [Parameter]
    public bool Filterable { get; set; }

    /// <summary>Placeholder for the search input.</summary>
    [Parameter]
    public string FilterPlaceholder { get; set; } = "Search...";

    /// <summary>Text shown when no results match the filter.</summary>
    [Parameter]
    public string EmptyText { get; set; } = "No matching options";

    /// <summary>Alternate content shown when no results match the filter (overrides EmptyText).</summary>
    [Parameter]
    public RenderFragment? EmptyContent { get; set; }

    /// <summary>When true shows a loading indicator.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Prefix icon rendered before the value/placeholder.</summary>
    [Parameter]
    public RenderFragment? PrefixIcon { get; set; }

    /// <summary>Fires when the dropdown opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Placement of the dropdown menu relative to the trigger.</summary>
    [Parameter]
    public SgPlacement Placement { get; set; } = SgPlacement.BottomStart;

    /// <summary>Trigger for expanding sub-levels.</summary>
    [Parameter]
    public SgCascaderExpandTrigger ExpandTrigger { get; set; } = SgCascaderExpandTrigger.Click;

    /// <summary>How the selected value is displayed in the trigger.</summary>
    [Parameter]
    public SgCascaderValueDisplay ValueDisplay { get; set; } = SgCascaderValueDisplay.FullPath;

    /// <summary>Visual variant of the cascader trigger.</summary>
    [Parameter]
    public SgCascaderVariant Variant { get; set; } = SgCascaderVariant.Outlined;

    /// <summary>Max height of the dropdown menu (CSS value like "300px"). Defaults to "280px".</summary>
    [Parameter]
    public string? MaxHeight { get; set; }

    /// <summary>
    /// Async callback to load children on demand when a node is expanded.
    /// Receives the clicked option and returns its children.
    /// When set, <see cref="SgCascaderOption.Children"/> is treated as pre-loaded if non-empty,
    /// otherwise lazy-loaded via this callback.
    /// </summary>
    [Parameter]
    public Func<SgCascaderOption, Task<List<SgCascaderOption>>>? OnLoadChildren { get; set; }

    /// <summary>
    /// Async callback for server-side search. Receives the search text and returns matching options with their paths.
    /// When set, client-side filtering is disabled and this callback is used instead.
    /// </summary>
    [Parameter]
    public Func<string, Task<List<(SgCascaderOption Option, List<string> Path)>>>? OnSearch { get; set; }

    /// <summary>
    /// Callback to dynamically determine if an option is disabled.
    /// Receives the option and returns true if it should be disabled.
    /// </summary>
    [Parameter]
    public Func<SgCascaderOption, bool>? DisabledOption { get; set; }

    /// <summary>Custom option template. If set, overrides the default rendering for each option row.</summary>
    [Parameter]
    public RenderFragment<SgCascaderOptionContext>? OptionTemplate { get; set; }

    /// <summary>When true enables checkbox multi-selection mode (checkable).</summary>
    [Parameter]
    public bool Checkable { get; set; }

    /// <summary>The checked values (multi-select). Two-way bindable.</summary>
    [Parameter]
    public List<string> CheckedValues { get; set; } = new();

    /// <summary>Fires when checked values change (multi-select).</summary>
    [Parameter]
    public EventCallback<List<string>> CheckedValuesChanged { get; set; }

    /// <summary>When true, shows the full search path in filter results with match highlighting.</summary>
    [Parameter]
    public bool ShowSearchPath { get; set; } = true;

    // ── Icon Parameters ──────────────────────────────────────────────────

    /// <summary>Custom caret icon.</summary>
    [Parameter]
    public RenderFragment? CaretIconOverride { get; set; }

    /// <summary>Custom clear icon.</summary>
    [Parameter]
    public RenderFragment? ClearIconOverride { get; set; }

    /// <summary>Custom loading icon.</summary>
    [Parameter]
    public RenderFragment? LoadingIconOverride { get; set; }

    /// <summary>Custom search icon.</summary>
    [Parameter]
    public RenderFragment? SearchIconOverride { get; set; }

    /// <summary>Custom filter clear icon.</summary>
    [Parameter]
    public RenderFragment? FilterClearIconOverride { get; set; }

    /// <summary>Custom checked icon for checkable mode.</summary>
    [Parameter]
    public RenderFragment? CheckedIconOverride { get; set; }

    /// <summary>Custom unchecked icon for checkable mode.</summary>
    [Parameter]
    public RenderFragment? UncheckedIconOverride { get; set; }

    // ── Computed ─────────────────────────────────────────────────────────

    private string DisplayText
    {
        get
        {
            if (Value.Count == 0) return string.Empty;
            if (ValueDisplay == SgCascaderValueDisplay.Leaf)
                return GetLabelForValue(Value[^1]);
            return string.Join(Separator, Value.Select(v => GetLabelForValue(v)));
        }
    }

    private bool NoDataFound => Filterable && !string.IsNullOrEmpty(_filterText) && !HasFilterResults();

    private string CurrentMaxHeight => MaxHeight ?? "280px";

    private RenderFragment CaretIcon => CaretIconOverride ?? DefaultCaretIcon;
    private RenderFragment ClearIcon => ClearIconOverride ?? DefaultClearIcon;
    private RenderFragment LoadingIcon => LoadingIconOverride ?? DefaultLoadingIcon;
    private RenderFragment SearchIcon => SearchIconOverride ?? DefaultSearchIcon;
    private RenderFragment FilterClearIcon => FilterClearIconOverride ?? DefaultClearIcon;
    private RenderFragment CheckedIcon => CheckedIconOverride ?? DefaultCheckedIcon;
    private RenderFragment UncheckedIcon => UncheckedIconOverride ?? DefaultUncheckedIcon;

    // ── Default Icons ────────────────────────────────────────────────────

    private static RenderFragment DefaultCaretIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""16"" height=""16"">
            <path fill-rule=""evenodd"" d=""M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"" clip-rule=""evenodd""/>
        </svg>");
    };

    private static RenderFragment DefaultClearIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"">
            <path d=""M6.28 5.22a.75.75 0 0 0-1.06 1.06L8.94 10l-3.72 3.72a.75.75 0 1 0 1.06 1.06L10 11.06l3.72 3.72a.75.75 0 1 0 1.06-1.06L11.06 10l3.72-3.72a.75.75 0 0 0-1.06-1.06L10 8.94 6.28 5.22Z""/>
        </svg>");
    };

    private static RenderFragment DefaultLoadingIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"" class=""sgc-cascader-spinner"">
            <path d=""M15.655 4.344a8 8 0 1 0-.028 11.345.75.75 0 1 1 1.06 1.06 9.5 9.5 0 1 1 .03-13.487.75.75 0 0 1-1.062 1.082Z""/>
        </svg>");
    };

    private static RenderFragment DefaultSearchIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"">
            <path fill-rule=""evenodd"" d=""M9 3.5a5.5 5.5 0 1 0 0 11 5.5 5.5 0 0 0 0-11ZM2 9a7 7 0 1 1 12.452 4.391l3.328 3.329a.75.75 0 1 1-1.06 1.06l-3.329-3.328A7 7 0 0 1 2 9Z"" clip-rule=""evenodd""/>
        </svg>");
    };

    private static RenderFragment DefaultCheckedIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"">
            <path fill-rule=""evenodd"" d=""M16.704 4.153a.75.75 0 0 1 .143 1.052l-8 10.5a.75.75 0 0 1-1.127.075l-4.5-4.5a.75.75 0 0 1 1.06-1.06l3.894 3.893 7.48-9.817a.75.75 0 0 1 1.05-.143Z"" clip-rule=""evenodd""/>
        </svg>");
    };

    private static RenderFragment DefaultUncheckedIcon => __builder =>
    {
        __builder.AddMarkupContent(0, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""none"" stroke=""currentColor"" stroke-width=""1.5"" width=""14"" height=""14"">
            <rect x=""2.75"" y=""2.75"" width=""14.5"" height=""14.5"" rx=""2"" />
        </svg>");
    };

    // ── Lifecycle ────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _jsModule = await JS.InvokeAsync<IJSObjectReference>("import",
                    "./_content/SuperUI/superui-cascader.js");
                _dotNetRef = DotNetObjectReference.Create(this);
                var placementStr = GetPlacementString();
                await _jsModule.InvokeVoidAsync("attach", _rootRef, _dotNetRef, placementStr, _menuId);
            }
            catch
            {
                // JS module not available (e.g., during testing)
            }
        }

        if (_open)
        {
            try
            {
                await _jsModule!.InvokeVoidAsync("repositionMenu", _rootRef);
            }
            catch { }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!_open && !_selectedPath.SequenceEqual(Value))
        {
            _selectedPath = new List<string>(Value);
        }

        if (Checkable && !_checkInitialized)
        {
            _checkedValues = new HashSet<string>(CheckedValues);
            _checkInitialized = true;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────

    private string GetRootClasses()
    {
        var sb = new List<string> { "sgc-cascader" };
        if (Block) sb.Add("sgc-block");
        if (_open) sb.Add("sgc-open");
        if (Disabled) sb.Add("sgc-disabled");
        if (_focusWithin) sb.Add("sgc-focus");
        if (Size != SgSize.Md) sb.Add($"sgc-cascader-{Size.ToString().ToLowerInvariant()}");
        if (Variant != SgCascaderVariant.Outlined) sb.Add($"sgc-cascader-{Variant.ToString().ToLowerInvariant()}");
        if (!string.IsNullOrEmpty(Class)) sb.Add(Class);
        return string.Join(" ", sb);
    }

    private string GetPlacementString() => Placement switch
    {
        SgPlacement.BottomStart => "BottomStart",
        SgPlacement.BottomEnd => "BottomEnd",
        SgPlacement.TopStart => "TopStart",
        SgPlacement.TopEnd => "TopEnd",
        _ => "BottomStart"
    };

    private RenderFragment RenderCascaderContent() => __builder =>
    {
        if (OnSearch != null)
        {
            // Server-side search - handled by OnSearch callback
            if (!string.IsNullOrEmpty(_filterText))
            {
                var results = _remoteResults;
                if (results.Count == 0)
                {
                    RenderEmpty(__builder);
                    return;
                }
                RenderFilteredResults(__builder, results);
            }
            else
            {
                RenderColumns(Options, 0)(__builder);
            }
        }
        else if (Filterable && !string.IsNullOrEmpty(_filterText))
        {
            var results = GetFilteredColumns().ToList();
            if (results.Count == 0)
            {
                RenderEmpty(__builder);
                return;
            }
            RenderFilteredResults(__builder, results);
        }
        else
        {
            RenderColumns(Options, 0)(__builder);
        }
    };

    private void RenderEmpty(RenderTreeBuilder __builder)
    {
        if (EmptyContent != null)
        {
            __builder.AddContent(0, EmptyContent);
        }
        else
        {
            __builder.AddMarkupContent(0, $"<div class=\"sgc-cascader-empty\">{EmptyText}</div>");
        }
    }

    // ── Remote search state ──────────────────────────────────────────────
    private List<(SgCascaderOption Option, List<string> Path)> _remoteResults = new();

    private bool HasFilterResults()
    {
        if (OnSearch != null) return _remoteResults.Count > 0;
        return GetFilteredColumns().Any();
    }

    // ── Filtered results rendering ───────────────────────────────────────

    private void RenderFilteredResults(RenderTreeBuilder __builder,
        List<(SgCascaderOption Option, List<string> Path)> results)
    {
        __builder.OpenElement(1, "div");
        __builder.AddAttribute(2, "class", "sgc-cascader-column sgc-cascader-filter-column");

        foreach (var (option, path) in results)
        {
            var isSelected = Value.Count > 0 && path.SequenceEqual(Value);
            var pathText = ShowSearchPath
                ? string.Join(Separator, path.Select(p => GetLabelForValue(p)))
                : option.Label;

            var optClasses = "sgc-cascader-option";
            if (isSelected) optClasses += " sgc-selected";

            __builder.OpenElement(3, "div");
            __builder.AddAttribute(4, "class", optClasses);
            __builder.AddAttribute(5, "role", "option");
            __builder.AddAttribute(6, "aria-selected", isSelected ? "true" : "false");
            __builder.AddAttribute(7, "onclick",
                EventCallback.Factory.Create(this, () => SelectPathAsync(path)));

            if (Checkable)
            {
                var isChecked = path.Any(v => _checkedValues.Contains(v));
                __builder.AddContent(8, isChecked ? CheckedIcon : UncheckedIcon);
            }

            if (ShowSearchPath)
            {
                __builder.AddMarkupContent(9, HighlightMatch(pathText, _filterText));
            }
            else
            {
                __builder.AddContent(10, option.Label);
            }

            __builder.CloseElement();
        }
        __builder.CloseElement();
    }

    private static string HighlightMatch(string text, string filter)
    {
        if (string.IsNullOrEmpty(filter)) return text;
        var idx = text.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase);
        if (idx < 0) return text;
        var before = text[..idx];
        var match = text.Substring(idx, filter.Length);
        var after = text[(idx + filter.Length)..];
        return $"{before}<mark>{match}</mark>{after}";
    }

    // ── Client-side filter search ────────────────────────────────────────

    private IEnumerable<(SgCascaderOption Option, List<string> Path)> GetFilteredColumns()
    {
        var filter = _filterText?.Trim().ToLowerInvariant() ?? "";
        if (string.IsNullOrEmpty(filter)) yield break;

        var results = new List<(SgCascaderOption, List<string>)>();
        SearchOptions(Options, new List<string>(), filter, results);
        foreach (var r in results) yield return r;
    }

    private static void SearchOptions(
        List<SgCascaderOption> options,
        List<string> parentPath,
        string filter,
        List<(SgCascaderOption, List<string>)> results)
    {
        foreach (var opt in options)
        {
            if (opt.IsGroup) continue;
            var currentPath = new List<string>(parentPath) { opt.Value };
            if (opt.Label.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            {
                results.Add((opt, currentPath));
            }
            if (opt.Children.Count > 0 && !opt.IsLeaf)
            {
                SearchOptions(opt.Children, currentPath, filter, results);
            }
        }
    }

    // ── Column rendering ─────────────────────────────────────────────────

    private RenderFragment RenderColumns(List<SgCascaderOption> options, int level)
    {
        if (options.Count == 0 && level > 0) return EmptyFragment;

        return __builder =>
        {
            __builder.OpenElement(0, "div");
            __builder.AddAttribute(1, "class", "sgc-cascader-column");

            foreach (var opt in options)
            {
                if (opt.IsGroup)
                {
                    RenderGroupHeader(__builder, opt);
                    continue;
                }

                var isSelected = _selectedPath.Count > level && _selectedPath[level] == opt.Value;
                var hasChildren = (opt.Children.Count > 0 || OnLoadChildren != null) && !opt.IsLeaf;
                var isDisabled = opt.Disabled || (DisabledOption?.Invoke(opt) ?? false);
                var isChecked = _checkedValues.Contains(opt.Value);

                var optClasses = "sgc-cascader-option";
                if (isSelected) optClasses += " sgc-selected";
                if (isDisabled) optClasses += " sgc-disabled";
                if (isChecked) optClasses += " sgc-checked";

                var context = new SgCascaderOptionContext(opt, level, isSelected, isDisabled, hasChildren, isChecked);

                __builder.OpenElement(2, "div");
                __builder.AddAttribute(3, "class", optClasses);
                __builder.AddAttribute(4, "role", "option");
                __builder.AddAttribute(5, "aria-selected", isSelected ? "true" : "false");
                __builder.AddAttribute(6, "aria-disabled", isDisabled ? "true" : "false");

                if (!isDisabled)
                {
                    if (ExpandTrigger == SgCascaderExpandTrigger.Hover)
                    {
                        __builder.AddAttribute(7, "onmouseenter",
                            EventCallback.Factory.Create(this, () => HoverExpandAsync(opt, level)));
                        __builder.AddAttribute(8, "onclick",
                            EventCallback.Factory.Create(this, () => SelectOrCheckAsync(opt, level)));
                    }
                    else
                    {
                        var clickHandler = Checkable
                            ? EventCallback.Factory.Create(this, () => ToggleCheckAsync(opt))
                            : EventCallback.Factory.Create(this, () => SelectAsync(opt, level));
                        __builder.AddAttribute(7, "onclick", clickHandler);
                    }
                }

                if (OptionTemplate != null)
                {
                    __builder.AddContent(8, OptionTemplate(context));
                }
                else
                {
                    // Checkbox
                    if (Checkable)
                    {
                        __builder.AddContent(9, isChecked ? CheckedIcon : UncheckedIcon);
                    }

                    // Icon
                    if (!string.IsNullOrEmpty(opt.Icon))
                    {
                        __builder.OpenElement(10, "span");
                        __builder.AddAttribute(11, "class", "sgc-cascader-option-icon");
                        __builder.AddMarkupContent(12, opt.Icon);
                        __builder.CloseElement();
                    }

                    // Label
                    __builder.OpenElement(13, "span");
                    __builder.AddAttribute(14, "class", "sgc-cascader-option-label");
                    __builder.AddContent(15, opt.Label);
                    __builder.CloseElement();

                    // Badge
                    if (!string.IsNullOrEmpty(opt.BadgeText))
                    {
                        __builder.OpenElement(16, "span");
                        __builder.AddAttribute(17, "class",
                            $"sgc-cascader-option-badge sgc-badge-{opt.BadgeVariant.ToString().ToLowerInvariant()}");
                        __builder.AddContent(18, opt.BadgeText);
                        __builder.CloseElement();
                    }

                    // Arrow (expand indicator)
                    if (hasChildren)
                    {
                        __builder.OpenElement(19, "span");
                        __builder.AddAttribute(20, "class", "sgc-cascader-option-arrow");
                        __builder.AddMarkupContent(21, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"">
                            <path fill-rule=""evenodd"" d=""M7.21 14.77a.75.75 0 0 1 .02-1.06L11.168 10 7.23 6.29a.75.75 0 1 1 1.04-1.08l4.5 4.25a.75.75 0 0 1 0 1.08l-4.5 4.25a.75.75 0 0 1-1.06-.02Z"" clip-rule=""evenodd""/>
                        </svg>");
                        __builder.CloseElement();
                    }
                }

                __builder.CloseElement();
            }

            __builder.CloseElement();

            // Render next level if selected
            if (_selectedPath.Count > level)
            {
                var selected = options.FirstOrDefault(o => o.Value == _selectedPath[level]);
                if (selected != null)
                {
                    var hasChildren = (selected.Children.Count > 0 || _lazyLoadedChildren.ContainsKey(selected.Value)) && !selected.IsLeaf;
                    if (hasChildren)
                    {
                        var children = selected.Children.Count > 0
                            ? selected.Children
                            : (_lazyLoadedChildren.TryGetValue(selected.Value, out var lazy) ? lazy : new List<SgCascaderOption>());
                        if (children.Count > 0)
                        {
                            __builder.AddContent(22, RenderColumns(children, level + 1));
                        }
                    }
                }
            }
        };
    }

    private static void RenderGroupHeader(RenderTreeBuilder __builder, SgCascaderOption opt)
    {
        __builder.OpenElement(0, "div");
        __builder.AddAttribute(1, "class", "sgc-cascader-group-header");

        if (!string.IsNullOrEmpty(opt.Icon))
        {
            __builder.OpenElement(2, "span");
            __builder.AddAttribute(3, "class", "sgc-cascader-option-icon");
            __builder.AddMarkupContent(4, opt.Icon);
            __builder.CloseElement();
        }

        __builder.OpenElement(5, "span");
        __builder.AddAttribute(6, "class", "sgc-cascader-group-label");
        __builder.AddContent(7, opt.Label);
        __builder.CloseElement();

        __builder.CloseElement();
    }

    private static readonly RenderFragment EmptyFragment = __builder => { };

    // ── Lazy loading ─────────────────────────────────────────────────────
    private readonly Dictionary<string, List<SgCascaderOption>> _lazyLoadedChildren = new();

    private async Task EnsureChildrenLoadedAsync(SgCascaderOption option)
    {
        if (OnLoadChildren == null) return;
        if (option.Children.Count > 0) return;
        if (_lazyLoadedChildren.ContainsKey(option.Value)) return;

        _isLoadingChildren = true;
        StateHasChanged();

        try
        {
            var children = await OnLoadChildren(option);
            _lazyLoadedChildren[option.Value] = children;
        }
        finally
        {
            _isLoadingChildren = false;
        }
    }

    // ── Event Handlers ───────────────────────────────────────────────────

    private async Task HoverExpandAsync(SgCascaderOption option, int level)
    {
        if (option.Disabled || (DisabledOption?.Invoke(option) ?? false)) return;

        // Debounce hover
        _hoverCts?.Cancel();
        _hoverCts = new CancellationTokenSource();
        var token = _hoverCts.Token;
        try
        {
            await Task.Delay(150, token);
        }
        catch (TaskCanceledException) { return; }

        if (token.IsCancellationRequested) return;

        while (_selectedPath.Count > level)
            _selectedPath.RemoveAt(_selectedPath.Count - 1);
        _selectedPath.Add(option.Value);

        if (OnLoadChildren != null)
        {
            await EnsureChildrenLoadedAsync(option);
        }

        StateHasChanged();
    }

    private async Task SelectAsync(SgCascaderOption option, int level)
    {
        if (option.Disabled || (DisabledOption?.Invoke(option) ?? false)) return;

        while (_selectedPath.Count > level)
            _selectedPath.RemoveAt(_selectedPath.Count - 1);
        _selectedPath.Add(option.Value);

        if (OnLoadChildren != null)
        {
            await EnsureChildrenLoadedAsync(option);
        }

        var hasChildren = option.Children.Count > 0 ||
            (OnLoadChildren != null && _lazyLoadedChildren.ContainsKey(option.Value) && _lazyLoadedChildren[option.Value].Count > 0);

        if (!hasChildren || option.IsLeaf)
        {
            Value = new List<string>(_selectedPath);
            _open = false;
            _filterText = string.Empty;
            await NotifyValueChangedAsync();
            await NotifyOnChangeAsync(GetSelectedOptions());
        }
        StateHasChanged();
    }

    private async Task SelectOrCheckAsync(SgCascaderOption option, int level)
    {
        if (Checkable)
        {
            await ToggleCheckAsync(option);
        }
        else
        {
            await SelectAsync(option, level);
        }
    }

    private async Task ToggleCheckAsync(SgCascaderOption option)
    {
        if (option.Disabled || (DisabledOption?.Invoke(option) ?? false)) return;

        if (_checkedValues.Contains(option.Value))
            _checkedValues.Remove(option.Value);
        else
            _checkedValues.Add(option.Value);

        CheckedValues = _checkedValues.ToList();
        if (CheckedValuesChanged.HasDelegate)
            await CheckedValuesChanged.InvokeAsync(CheckedValues);

        StateHasChanged();
    }

    private async Task SelectPathAsync(List<string> path)
    {
        Value = new List<string>(path);
        _open = false;
        _filterText = string.Empty;
        await NotifyValueChangedAsync();
        await NotifyOnChangeAsync(GetOptionsForPath(path));
        StateHasChanged();
    }

    private List<SgCascaderOption> GetSelectedOptions()
    {
        return GetOptionsForPath(_selectedPath);
    }

    private List<SgCascaderOption> GetOptionsForPath(List<string> path)
    {
        var result = new List<SgCascaderOption>();
        var current = Options;
        foreach (var val in path)
        {
            var opt = current.FirstOrDefault(o => o.Value == val);
            if (opt == null) break;
            result.Add(opt);
            current = opt.Children.Count > 0
                ? opt.Children
                : (_lazyLoadedChildren.TryGetValue(val, out var lazy) ? lazy : new());
        }
        return result;
    }

    private async Task ToggleAsync()
    {
        if (Disabled) return;
        _open = !_open;
        if (_open)
        {
            _selectedPath = new List<string>(Value);
            _filterText = string.Empty;
            _hoverCts?.Cancel();
        }
        await NotifyOpenChangedAsync();
        if (_open && Filterable)
        {
            await Task.Delay(50);
            await _searchInputRef.FocusAsync();
        }
        StateHasChanged();
    }

    private async Task ClearAsync()
    {
        Value = new List<string>();
        _selectedPath = new List<string>();
        _open = false;
        await NotifyValueChangedAsync();
        if (OnChange.HasDelegate)
            await OnChange.InvokeAsync(new List<SgCascaderOption>());
        StateHasChanged();
    }

    private async Task HandleFocusOutAsync(FocusEventArgs e)
    {
        await Task.Delay(160);
        if (_open && !_focusWithin)
        {
            _open = false;
            await NotifyOpenChangedAsync();
            StateHasChanged();
        }
        _focusWithin = false;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled) return;
        switch (e.Key)
        {
            case "Enter":
            case " ":
                await ToggleAsync();
                break;
            case "Escape":
                if (_open)
                {
                    _open = false;
                    await NotifyOpenChangedAsync();
                    StateHasChanged();
                }
                break;
        }
    }

    private async Task HandleSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            _open = false;
            await NotifyOpenChangedAsync();
            StateHasChanged();
        }
    }

    private async Task OnSearchInputChangedAsync(ChangeEventArgs e)
    {
        _filterText = (string?)e.Value ?? "";
        if (OnSearch != null && !string.IsNullOrEmpty(_filterText))
        {
            try
            {
                _remoteResults = await OnSearch(_filterText);
            }
            catch
            {
                _remoteResults = new();
            }
        }
        StateHasChanged();
    }

    private void ClearFilter()
    {
        _filterText = string.Empty;
        _remoteResults = new();
        StateHasChanged();
    }

    private async Task NotifyValueChangedAsync()
    {
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(Value);
    }

    private async Task NotifyOnChangeAsync(List<SgCascaderOption> options)
    {
        if (OnChange.HasDelegate)
            await OnChange.InvokeAsync(options);
    }

    private async Task NotifyOpenChangedAsync()
    {
        if (OpenChanged.HasDelegate)
            await OpenChanged.InvokeAsync(_open);
    }

    // ── JS Interop (invokable from JS) ───────────────────────────────────

    [JSInvokable]
    public async Task CloseFromJsAsync()
    {
        if (_open)
        {
            _open = false;
            _focusWithin = false;
            await NotifyOpenChangedAsync();
            StateHasChanged();
        }
    }

    /// <summary>
    /// Called from JS for hover expand.
    /// </summary>
    [JSInvokable]
    public async Task HoverFromJsAsync(double clientX, double clientY)
    {
        // Handled via mouseenter events in Razor - this is a no-op
        await Task.CompletedTask;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private string GetLabelForValue(string val)
    {
        var opt = FindOption(Options, val);
        return opt?.Label ?? val;
    }

    private static SgCascaderOption? FindOption(List<SgCascaderOption> list, string val)
    {
        foreach (var o in list)
        {
            if (o.Value == val) return o;
            if (o.Children.Count > 0)
            {
                var child = FindOption(o.Children, val);
                if (child != null) return child;
            }
        }
        return null;
    }

    // ── Dispose ──────────────────────────────────────────────────────────

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        _hoverCts?.Cancel();
        _hoverCts?.Dispose();
        _dotNetRef?.Dispose();
        if (_jsModule != null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("detach", _rootRef);
                await _jsModule.DisposeAsync();
            }
            catch { }
        }
    }
}

/// <summary>
/// Context passed to the <see cref="SgCascader.OptionTemplate"/> render fragment.
/// </summary>
public sealed class SgCascaderOptionContext
{
    /// <summary>The option being rendered.</summary>
    public SgCascaderOption Option { get; }

    /// <summary>The hierarchy level (0 = root).</summary>
    public int Level { get; }

    /// <summary>Whether this option is currently selected in the navigation path.</summary>
    public bool IsSelected { get; }

    /// <summary>Whether this option is disabled.</summary>
    public bool IsDisabled { get; }

    /// <summary>Whether this option has children (expandable).</summary>
    public bool HasChildren { get; }

    /// <summary>Whether this option is checked (checkable mode).</summary>
    public bool IsChecked { get; }

    public SgCascaderOptionContext(SgCascaderOption option, int level,
        bool isSelected, bool isDisabled, bool hasChildren, bool isChecked)
    {
        Option = option;
        Level = level;
        IsSelected = isSelected;
        IsDisabled = isDisabled;
        HasChildren = hasChildren;
        IsChecked = isChecked;
    }
}
