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
    private readonly List<SgDropdownSub> _subs = new();
    private int _focusedIndex = -1;
    private ElementReference _triggerRef;
    private ElementReference _menuRef;
    private bool _attached;
    private bool _lastRenderedOpen;
    private string _searchText = "";
    private bool _flipX;
    private bool _flipY;
    private int _contextX;
    private int _contextY;
    private ElementReference _searchRef;

    // ── Existing Parameters ──
    [Parameter] public string? Text { get; set; }
    [Parameter] public RenderFragment? Icon { get; set; }
    [Parameter] public SgButtonVariant Variant { get; set; } = SgButtonVariant.Default;
    [Parameter] public SgSize Size { get; set; } = SgSize.Md;
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public RenderFragment? TriggerContent { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public string? MenuCssClass { get; set; }
    [Parameter] public int? MaxHeight { get; set; }
    [Parameter] public int? MinWidth { get; set; }
    [Parameter] public SgDropdownTrigger Trigger { get; set; } = SgDropdownTrigger.Click;
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.BottomStart;
    [Parameter] public bool CloseOnSelect { get; set; } = true;
    [Parameter] public bool ShowCaret { get; set; }
    [Parameter] public int OpenDelay { get; set; } = 80;
    [Parameter] public int CloseDelay { get; set; } = 120;
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    [Parameter] public EventCallback OnOpen { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool Searchable { get; set; }
    [Parameter] public string? SearchPlaceholder { get; set; }
    [Parameter] public bool MatchWidth { get; set; }
    [Parameter] public string? Gap { get; set; }
    [Parameter] public int? MenuMaxWidth { get; set; }
    [Parameter] public bool Animation { get; set; } = true;

    // ── New Optional Parameters ──
    [Parameter] public bool UsePortal { get; set; }
    [Parameter] public string? EmptyText { get; set; }
    [Parameter] public bool AutoFocusSearch { get; set; }
    [Parameter] public bool ShowSearchClear { get; set; }
    [Parameter] public bool Compact { get; set; }
    [Parameter] public bool Flip { get; set; }
    [Parameter] public bool ShowArrow { get; set; }
    [Parameter] public SgDropdownTransition DropdownTransition { get; set; } = SgDropdownTransition.Scale;

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

    private string ComputedGap => Gap ?? "4px";

    private string MenuInlineStyle
    {
        get
        {
            var parts = new List<string>(6);
            parts.Add($"max-height:{(MaxHeight ?? 320)}px");
            if (MinWidth.HasValue) parts.Add($"min-width:{MinWidth.Value}px");
            if (MenuMaxWidth.HasValue) parts.Add($"max-width:{MenuMaxWidth.Value}px");
            if (MatchWidth) parts.Add("min-width:0");
            if (!string.IsNullOrEmpty(Gap))
            {
                var isTop = Placement is SgPlacement.TopStart or SgPlacement.TopEnd or SgPlacement.Top;
                parts.Add(isTop ? $"bottom:calc(100% + {Gap})" : $"top:calc(100% + {Gap})");
            }
            if (_flipX) parts.Add("left:auto;right:0");
            if (_flipY && Placement is SgPlacement.BottomStart or SgPlacement.BottomEnd or SgPlacement.Bottom)
                parts.Add("top:auto;bottom:calc(100% + 4px)");
            else if (_flipY && Placement is SgPlacement.TopStart or SgPlacement.TopEnd or SgPlacement.Top)
                parts.Add("top:calc(100% + 4px);bottom:auto");
            if (Trigger == SgDropdownTrigger.ContextMenu)
                parts.Add($"left:{_contextX}px;top:{_contextY}px;position:fixed");
            return string.Join(";", parts);
        }
    }



    private string MenuClasses
    {
        get
        {
            var c = $"sgc-dropdown-menu sgc-dropdown-menu-{PlacementClass}";
            if (!Animation) c += " sgc-no-anim";
            else if (DropdownTransition == SgDropdownTransition.Scale) c += " sgc-trans-scale";
            else if (DropdownTransition == SgDropdownTransition.Slide) c += " sgc-trans-slide";
            else if (DropdownTransition == SgDropdownTransition.Fade) c += " sgc-trans-fade";
            if (MatchWidth) c += " sgc-dropdown-match";
            if (Compact) c += " sgc-dropdown-compact";
            if (ShowArrow) c += " sgc-has-arrow";
            if (!string.IsNullOrEmpty(MenuCssClass)) c += " " + MenuCssClass;
            return c;
        }
    }

    private bool ShowSearch => Searchable && _open;
    private bool HasFilteredResults => _searchText.Length > 0 && _items.Any(i => !i.Divider && !ParentIsItemFiltered(i));
    private string ResolvedEmptyText => EmptyText ?? "Nothing found";

    internal bool ParentIsItemFiltered(SgDropdownItem item) => IsItemFiltered(item);

    internal void RegisterSub(SgDropdownSub sub)
    {
        if (!_subs.Contains(sub)) _subs.Add(sub);
    }

    internal void UnregisterSub(SgDropdownSub sub)
    {
        _subs.Remove(sub);
    }

    internal void CloseAllSubs()
    {
        foreach (var s in _subs) s.Close();
    }

    public Task ToggleAsync() => _open ? CloseAsync() : OpenAsync();

    public async Task OpenAsync()
    {
        if (Disabled || _open) return;
        _open = true;
        _focusedIndex = -1;
        _searchText = "";
        _flipX = false;
        _flipY = false;
        if (OpenChanged.HasDelegate) await OpenChanged.InvokeAsync(true);
        if (OnOpen.HasDelegate) await OnOpen.InvokeAsync();
        RefreshItems();
        await InvokeAsync(StateHasChanged);
    }

    public async Task CloseAsync()
    {
        if (!_open) return;
        _open = false;
        _focusedIndex = -1;
        CloseAllSubs();
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

    [JSInvokable]
    public void ApplyFlip(bool flipX, bool flipY)
    {
        _flipX = flipX;
        _flipY = flipY;
        InvokeAsync(StateHasChanged);
    }

    protected override string ModulePath => "./_content/SuperUI/superui-dropdown.js";

    private Task HandleTriggerClickAsync()
    {
        if (Trigger != SgDropdownTrigger.Click) return Task.CompletedTask;
        return ToggleAsync();
    }

    private Task HandleContextMenuAsync(MouseEventArgs e)
    {
        if (Trigger != SgDropdownTrigger.ContextMenu) return Task.CompletedTask;
        _contextX = (int)e.ClientX;
        _contextY = (int)e.ClientY;
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
                if (!_open) await OpenAsync();
                else if (_focusedIndex >= 0 && _focusedIndex < _items.Count)
                    await _items[_focusedIndex].ActivateAsync();
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
        RefreshItems();
    }

    private void ClearSearch()
    {
        _searchText = "";
        _focusedIndex = -1;
        RefreshItems();
    }

    private void RefreshItems()
    {
        foreach (var item in _items)
            item.Refresh();
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (_open && !_lastRenderedOpen)
        {
            _lastRenderedOpen = true;
            if (AutoFocusSearch && Searchable)
            {
                try { await _searchRef.FocusAsync(); } catch { }
            }
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
        await SafeInvokeVoidAsync("attach",
            RootRef, _triggerRef, _menuRef, SelfRef,
            true, true, Flip, UsePortal);
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
