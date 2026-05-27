namespace SuperUI.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Enums;

public partial class SgDropdown : SgJsComponentBase
{
    private bool _open;
    private CancellationTokenSource? _hoverCts;
    private readonly List<SgDropdownItem> _items = new();
    private int _focusedIndex = -1;
    private ElementReference _triggerRef;
    private bool _attached;
    private bool _lastRenderedOpen;
    private string _searchText = "";

    // ── Existing Parameters ──

    /// <summary>Text shown on the default button trigger when <see cref="TriggerContent"/> is null.</summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>Icon rendered inside the default button trigger.</summary>
    [Parameter] public RenderFragment? Icon { get; set; }

    /// <summary>Variant of the default button trigger.</summary>
    [Parameter] public SgButtonVariant Variant { get; set; } = SgButtonVariant.Default;

    /// <summary>Size of the default button trigger.</summary>
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>When true, all interaction is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Custom trigger content; replaces the default button.</summary>
    [Parameter] public RenderFragment? TriggerContent { get; set; }

    /// <summary>Menu items (typically <see cref="SgDropdownItem"/>s).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional header rendered above the items.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>Optional footer rendered below the items.</summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }

    /// <summary>Additional class on the menu panel.</summary>
    [Parameter] public string? MenuCssClass { get; set; }

    /// <summary>Maximum menu height; falls back to 320px if unset.</summary>
    [Parameter] public int? MaxHeight { get; set; }

    /// <summary>Minimum menu width in pixels.</summary>
    [Parameter] public int? MinWidth { get; set; }

    /// <summary>How the trigger opens the menu. Default is <see cref="SgDropdownTrigger.Click"/>.</summary>
    [Parameter] public SgDropdownTrigger Trigger { get; set; } = SgDropdownTrigger.Click;

    /// <summary>Menu placement relative to the trigger. Supports BottomStart/BottomEnd/TopStart/TopEnd.</summary>
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.BottomStart;

    /// <summary>When true, the menu closes after an item click. Default true.</summary>
    [Parameter] public bool CloseOnSelect { get; set; } = true;

    /// <summary>Show a caret indicator beside the default button trigger.</summary>
    [Parameter] public bool ShowCaret { get; set; }

    /// <summary>Hover open delay in milliseconds (used when <see cref="Trigger"/> is Hover).</summary>
    [Parameter] public int OpenDelay { get; set; } = 80;

    /// <summary>Hover close delay in milliseconds (used when <see cref="Trigger"/> is Hover).</summary>
    [Parameter] public int CloseDelay { get; set; } = 120;

    /// <summary>Raised whenever the open state changes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Raised when the menu opens.</summary>
    [Parameter] public EventCallback OnOpen { get; set; }

    /// <summary>Raised when the menu closes.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    // ── New Parameters (all optional) ──

    /// <summary>When true, shows a loading spinner inside the menu instead of items.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>When true, shows a search box at the top of the menu to filter items by text.</summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>Placeholder text for the search input when <see cref="Searchable"/> is true.</summary>
    [Parameter] public string? SearchPlaceholder { get; set; }

    /// <summary>When true, the menu width matches the trigger element width.</summary>
    [Parameter] public bool MatchWidth { get; set; }

    /// <summary>Custom gap between trigger and menu (CSS value, e.g. "8px" or "0.5rem"). Default is "4px".</summary>
    [Parameter] public string? Gap { get; set; }

    /// <summary>Maximum width of the menu in pixels.</summary>
    [Parameter] public int? MenuMaxWidth { get; set; }

    /// <summary>When false, disables the open/close animation.</summary>
    [Parameter] public bool Animation { get; set; } = true;

    /// <summary>Whether the menu is currently open.</summary>
    public bool IsOpen => _open;

    private string PlacementClass => Placement switch
    {
        SgPlacement.BottomEnd => "be",
        SgPlacement.Bottom or SgPlacement.BottomStart => "bs",
        SgPlacement.TopStart => "ts",
        SgPlacement.TopEnd => "te",
        SgPlacement.Top => "ts",
        _ => "bs"
    };

    private string MenuInlineStyle
    {
        get
        {
            var parts = new List<string>(4);
            parts.Add($"max-height:{(MaxHeight ?? 320)}px");
            if (MinWidth.HasValue) parts.Add($"min-width:{MinWidth.Value}px");
            if (MenuMaxWidth.HasValue) parts.Add($"max-width:{MenuMaxWidth.Value}px");
            if (MatchWidth) parts.Add("min-width:0");
            if (Gap is not null)
            {
                var isTop = Placement is SgPlacement.TopStart or SgPlacement.TopEnd or SgPlacement.Top;
                parts.Add(isTop ? $"bottom:calc(100% + {Gap})" : $"top:calc(100% + {Gap})");
            }
            return string.Join(";", parts);
        }
    }

    private string MenuClasses
    {
        get
        {
            var c = $"sgc-dropdown-menu sgc-dropdown-menu-{PlacementClass}";
            if (!Animation) c += " sgc-no-anim";
            if (MatchWidth) c += " sgc-dropdown-match";
            if (!string.IsNullOrEmpty(MenuCssClass)) c += " " + MenuCssClass;
            return c;
        }
    }

    private bool ShowSearch => Searchable && _open;

    /// <summary>Toggle the menu open/closed.</summary>
    public Task ToggleAsync() => _open ? CloseAsync() : OpenAsync();

    /// <summary>Open the menu programmatically.</summary>
    public async Task OpenAsync()
    {
        if (Disabled || _open) return;
        _open = true;
        _focusedIndex = -1;
        _searchText = "";
        if (OpenChanged.HasDelegate) await OpenChanged.InvokeAsync(true);
        if (OnOpen.HasDelegate) await OnOpen.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Close the menu programmatically.</summary>
    public async Task CloseAsync()
    {
        if (!_open) return;
        _open = false;
        _focusedIndex = -1;
        if (OpenChanged.HasDelegate) await OpenChanged.InvokeAsync(false);
        if (OnClose.HasDelegate) await OnClose.InvokeAsync();
        await InvokeAsync(StateHasChanged);
    }

    internal async Task NotifyItemSelectedAsync()
    {
        if (CloseOnSelect) await CloseAsync();
    }

    internal void RegisterItem(SgDropdownItem item)
    {
        if (!_items.Contains(item)) _items.Add(item);
    }

    internal void UnregisterItem(SgDropdownItem item)
    {
        _items.Remove(item);
    }

    internal bool IsItemFiltered(SgDropdownItem item)
    {
        if (!Searchable || string.IsNullOrWhiteSpace(_searchText)) return false;
        if (item.Divider) return false;
        var q = _searchText.Trim();
        if (string.IsNullOrEmpty(q)) return false;
        var match = item.Text.Contains(q, StringComparison.OrdinalIgnoreCase);
        if (!match && item.Subtext is not null)
            match = item.Subtext.Contains(q, StringComparison.OrdinalIgnoreCase);
        return !match;
    }

    [JSInvokable]
    public Task CloseFromJsAsync() => CloseAsync();

    protected override string ModulePath => "./_content/SuperUI/superui-dropdown.js";

    // ── Event Handlers ──

    private Task HandleTriggerClickAsync()
    {
        if (Trigger != SgDropdownTrigger.Click) return Task.CompletedTask;
        return ToggleAsync();
    }

    private Task HandleContextMenuAsync(MouseEventArgs e)
    {
        if (Trigger != SgDropdownTrigger.ContextMenu) return Task.CompletedTask;
        return OpenAsync();
    }

    private async Task HandleMouseEnterAsync()
    {
        if (Trigger != SgDropdownTrigger.Hover || Disabled) return;
        _hoverCts?.Cancel();
        _hoverCts = new CancellationTokenSource();
        var token = _hoverCts.Token;
        try
        {
            if (OpenDelay > 0) await Task.Delay(OpenDelay, token);
            if (!token.IsCancellationRequested) await OpenAsync();
        }
        catch (TaskCanceledException) { }
    }

    private async Task HandleMouseLeaveAsync()
    {
        if (Trigger != SgDropdownTrigger.Hover) return;
        _hoverCts?.Cancel();
        _hoverCts = new CancellationTokenSource();
        var token = _hoverCts.Token;
        try
        {
            if (CloseDelay > 0) await Task.Delay(CloseDelay, token);
            if (!token.IsCancellationRequested) await CloseAsync();
        }
        catch (TaskCanceledException) { }
    }

    private Task HandleMenuMouseEnterAsync()
    {
        _hoverCts?.Cancel();
        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (Disabled) return;
        switch (e.Key)
        {
            case "Enter":
            case " ":
                if (!_open)
                {
                    await OpenAsync();
                }
                else if (_focusedIndex >= 0 && _focusedIndex < _items.Count)
                {
                    await _items[_focusedIndex].ActivateAsync();
                }
                break;
            case "Escape":
                if (_open) await CloseAsync();
                break;
            case "ArrowDown":
                if (!_open) { await OpenAsync(); break; }
                await MoveFocusAsync(1);
                break;
            case "ArrowUp":
                if (!_open) { await OpenAsync(); break; }
                await MoveFocusAsync(-1);
                break;
            case "Home":
                if (_open) await FocusIndexAsync(0);
                break;
            case "End":
                if (_open) await FocusIndexAsync(_items.Count - 1);
                break;
        }
    }

    private async Task MoveFocusAsync(int delta)
    {
        if (_items.Count == 0) return;
        var start = _focusedIndex < 0 ? (delta > 0 ? -1 : _items.Count) : _focusedIndex;
        for (var step = 0; step < _items.Count; step++)
        {
            start += delta;
            if (start < 0) start = _items.Count - 1;
            if (start >= _items.Count) start = 0;
            var candidate = _items[start];
            if (!candidate.Disabled && !candidate.Divider)
            {
                _focusedIndex = start;
                await candidate.FocusAsync();
                return;
            }
        }
    }

    private async Task FocusIndexAsync(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        for (var i = 0; i < _items.Count; i++)
        {
            var idx = (index + i) % _items.Count;
            var c = _items[idx];
            if (!c.Disabled && !c.Divider)
            {
                _focusedIndex = idx;
                await c.FocusAsync();
                return;
            }
        }
    }

    private void HandleSearchInput(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString() ?? "";
        _focusedIndex = -1;
    }

    // ── JS Interop ──

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (_open && !_lastRenderedOpen)
        {
            _lastRenderedOpen = true;
            await AttachAsync();
        }
        else if (!_open && _lastRenderedOpen)
        {
            _lastRenderedOpen = false;
            await DetachAsync();
        }
    }

    private async Task AttachAsync()
    {
        if (_attached) return;
        await SafeInvokeVoidAsync("attach", RootRef, _triggerRef, SelfRef, true, true);
        _attached = true;
    }

    private async Task DetachAsync()
    {
        if (!_attached) return;
        await SafeInvokeVoidAsync("detach", RootRef);
        _attached = false;
    }

    protected override async ValueTask OnDisposingAsync()
    {
        _hoverCts?.Cancel();
        _hoverCts?.Dispose();
        await DetachAsync();
    }
}
