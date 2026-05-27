using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Cascaded selection component for hierarchical data such as locations or categories.
/// </summary>
public sealed partial class SgCascader : IAsyncDisposable
{
    // ── Injected Services ────────────────────────────────────────────────
    [Inject] private IJSRuntime JS { get; set; } = default!;

    // ── JS Module ────────────────────────────────────────────────────────
    private static Task<IJSObjectReference>? _moduleTask;
    private IJSObjectReference? _jsModule;
    private ElementReference _rootRef;
    private ElementReference _searchInputRef;
    private ElementReference _columnsRef;

    // ── State ────────────────────────────────────────────────────────────
    private bool _open;
    private bool _focusWithin;
    private bool _disposed;
    private List<string> _selectedPath = new();
    private string _filterText = string.Empty;
    private DotNetObjectReference<SgCascader>? _dotNetRef;

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

    /// <summary>When true shows a loading indicator.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>Prefix icon rendered before the value/placeholder.</summary>
    [Parameter]
    public RenderFragment? PrefixIcon { get; set; }

    /// <summary>Fires when the dropdown opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    // ── Icon Parameters (customizable) ───────────────────────────────────

    /// <summary>Custom caret (dropdown arrow) icon. Defaults to SVG chevron-down.</summary>
    [Parameter]
    public RenderFragment? CaretIconOverride { get; set; }

    /// <summary>Custom clear icon. Defaults to SVG X-mark.</summary>
    [Parameter]
    public RenderFragment? ClearIconOverride { get; set; }

    /// <summary>Custom loading icon. Defaults to SVG spinner.</summary>
    [Parameter]
    public RenderFragment? LoadingIconOverride { get; set; }

    /// <summary>Custom search icon. Defaults to SVG magnifying glass.</summary>
    [Parameter]
    public RenderFragment? SearchIconOverride { get; set; }

    /// <summary>Custom filter clear icon. Defaults to SVG X-mark.</summary>
    [Parameter]
    public RenderFragment? FilterClearIconOverride { get; set; }

    // ── Computed ─────────────────────────────────────────────────────────

    private string DisplayText => Value.Count > 0
        ? string.Join(Separator, Value.Select(v => GetLabelForValue(v)))
        : string.Empty;

    private bool NoDataFound => Filterable && !string.IsNullOrEmpty(_filterText) && !GetFilteredColumns().Any();

    private RenderFragment CaretIcon => CaretIconOverride ?? DefaultCaretIcon;
    private RenderFragment ClearIcon => ClearIconOverride ?? DefaultClearIcon;
    private RenderFragment LoadingIcon => LoadingIconOverride ?? DefaultLoadingIcon;
    private RenderFragment SearchIcon => SearchIconOverride ?? DefaultSearchIcon;
    private RenderFragment FilterClearIcon => FilterClearIconOverride ?? DefaultClearIcon;

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

    // ── Lifecycle ────────────────────────────────────────────────────────

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _moduleTask = JS.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/superui-cascader.js").AsTask();
                _jsModule = await _moduleTask;
                _dotNetRef = DotNetObjectReference.Create(this);
                await _jsModule.InvokeVoidAsync("attach", _rootRef, _dotNetRef);
            }
            catch
            {
                // JS module not available (e.g., during testing)
            }
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (!_open && !_selectedPath.SequenceEqual(Value))
        {
            _selectedPath = new List<string>(Value);
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
        if (!string.IsNullOrEmpty(Class)) sb.Add(Class);
        return string.Join(" ", sb);
    }

    private RenderFragment RenderCascaderContent() => __builder =>
    {
        if (Filterable && !string.IsNullOrEmpty(_filterText))
        {
            RenderFilteredResults(__builder);
        }
        else
        {
            RenderColumns(Options, 0)(__builder);
        }
    };

    private void RenderFilteredResults(RenderTreeBuilder __builder)
    {
        var results = GetFilteredColumns().ToList();
        if (results.Count == 0)
        {
            __builder.AddMarkupContent(0, "<div class=\"sgc-cascader-empty\">No matching options</div>");
            return;
        }

        __builder.OpenElement(1, "div");
        __builder.AddAttribute(2, "class", "sgc-cascader-column sgc-cascader-filter-column");
        foreach (var (option, path) in results)
        {
            var isSelected = Value.Count > 0 && path.SequenceEqual(Value);
            var pathText = string.Join(Separator, path.Select(p => GetLabelForValue(p)));
            __builder.OpenElement(3, "div");
            __builder.AddAttribute(4, "class", $"sgc-cascader-option {(isSelected ? "sgc-selected" : "")}");
            __builder.AddAttribute(5, "role", "option");
            __builder.AddAttribute(6, "aria-selected", isSelected ? "true" : "false");
            __builder.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, () => SelectPathAsync(path)));
            __builder.AddContent(8, pathText);
            __builder.CloseElement();
        }
        __builder.CloseElement();
    }

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
            var currentPath = new List<string>(parentPath) { opt.Value };
            if (opt.Label.Contains(filter, StringComparison.CurrentCultureIgnoreCase))
            {
                results.Add((opt, currentPath));
            }
            if (opt.Children.Count > 0)
            {
                SearchOptions(opt.Children, currentPath, filter, results);
            }
        }
    }

    private RenderFragment RenderColumns(List<SgCascaderOption> options, int level)
    {
        if (options.Count == 0 && level > 0) return EmptyFragment;

        return __builder =>
        {
            __builder.OpenElement(0, "div");
            __builder.AddAttribute(1, "class", "sgc-cascader-column");
            foreach (var opt in options)
            {
                var isSelected = _selectedPath.Count > level && _selectedPath[level] == opt.Value;
                var hasChildren = opt.Children.Count > 0 && !opt.IsLeaf;
                var optClasses = "sgc-cascader-option";
                if (isSelected) optClasses += " sgc-selected";
                if (opt.Disabled) optClasses += " sgc-disabled";

                __builder.OpenElement(2, "div");
                __builder.AddAttribute(3, "class", optClasses);
                __builder.AddAttribute(4, "role", "option");
                __builder.AddAttribute(5, "aria-selected", isSelected ? "true" : "false");
                __builder.AddAttribute(6, "aria-disabled", opt.Disabled ? "true" : "false");
                __builder.AddAttribute(7, "onclick", EventCallback.Factory.Create(this, () => SelectAsync(opt, level)));
                __builder.AddAttribute(8, "onmouseenter", EventCallback.Factory.Create(this, () => HoverOption(opt, level)));

                // Icon
                if (!string.IsNullOrEmpty(opt.Icon))
                {
                    __builder.OpenElement(9, "span");
                    __builder.AddAttribute(10, "class", "sgc-cascader-option-icon");
                    __builder.AddMarkupContent(11, opt.Icon);
                    __builder.CloseElement();
                }

                // Label
                __builder.OpenElement(12, "span");
                __builder.AddAttribute(13, "class", "sgc-cascader-option-label");
                __builder.AddContent(14, opt.Label);
                __builder.CloseElement();

                // Badge
                if (!string.IsNullOrEmpty(opt.BadgeText))
                {
                    __builder.OpenElement(15, "span");
                    __builder.AddAttribute(16, "class", $"sgc-cascader-option-badge sgc-badge-{opt.BadgeVariant.ToString().ToLowerInvariant()}");
                    __builder.AddContent(17, opt.BadgeText);
                    __builder.CloseElement();
                }

                // Arrow
                if (hasChildren)
                {
                    __builder.OpenElement(18, "span");
                    __builder.AddAttribute(19, "class", "sgc-cascader-option-arrow");
                    __builder.AddMarkupContent(20, @"<svg xmlns=""http://www.w3.org/2000/svg"" viewBox=""0 0 20 20"" fill=""currentColor"" width=""14"" height=""14"">
                        <path fill-rule=""evenodd"" d=""M7.21 14.77a.75.75 0 0 1 .02-1.06L11.168 10 7.23 6.29a.75.75 0 1 1 1.04-1.08l4.5 4.25a.75.75 0 0 1 0 1.08l-4.5 4.25a.75.75 0 0 1-1.06-.02Z"" clip-rule=""evenodd""/>
                    </svg>");
                    __builder.CloseElement();
                }

                __builder.CloseElement();
            }
            __builder.CloseElement();

            if (_selectedPath.Count > level)
            {
                var selected = options.FirstOrDefault(o => o.Value == _selectedPath[level]);
                if (selected?.Children.Count > 0 && !selected.IsLeaf)
                {
                    __builder.AddContent(21, RenderColumns(selected.Children, level + 1));
                }
            }
        };
    }

    private static readonly RenderFragment EmptyFragment = __builder => { };

    // ── Event Handlers ───────────────────────────────────────────────────

    private async Task SelectAsync(SgCascaderOption option, int level)
    {
        if (option.Disabled) return;

        while (_selectedPath.Count > level)
            _selectedPath.RemoveAt(_selectedPath.Count - 1);
        _selectedPath.Add(option.Value);

        if (option.Children.Count == 0 || option.IsLeaf)
        {
            Value = new List<string>(_selectedPath);
            _open = false;
            _filterText = string.Empty;
            await NotifyValueChangedAsync();
        }
        StateHasChanged();
    }

    private async Task SelectPathAsync(List<string> path)
    {
        Value = new List<string>(path);
        _open = false;
        _filterText = string.Empty;
        await NotifyValueChangedAsync();
        StateHasChanged();
    }

    private void HoverOption(SgCascaderOption option, int level)
    {
        if (option.Disabled) return;
        while (_selectedPath.Count > level)
            _selectedPath.RemoveAt(_selectedPath.Count - 1);
        _selectedPath.Add(option.Value);
        StateHasChanged();
    }

    private async Task ToggleAsync()
    {
        if (Disabled) return;
        _open = !_open;
        if (_open)
        {
            _selectedPath = new List<string>(Value);
            _filterText = string.Empty;
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

    private void ClearFilter()
    {
        _filterText = string.Empty;
        StateHasChanged();
    }

    private async Task NotifyValueChangedAsync()
    {
        if (ValueChanged.HasDelegate)
            await ValueChanged.InvokeAsync(Value);
    }

    private async Task NotifyOpenChangedAsync()
    {
        if (OpenChanged.HasDelegate)
            await OpenChanged.InvokeAsync(_open);
    }

    // ── JS Interop (called from JS) ──────────────────────────────────────

    /// <summary>
    /// Called from JS when a click outside is detected.
    /// </summary>
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
