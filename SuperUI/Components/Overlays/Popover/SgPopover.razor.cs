using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.Builders;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

/// <summary>
/// A popover component that displays overlay content relative to a trigger element.
/// Supports click, hover, focus, and manual trigger modes; 12 placements; rich content;
/// arrows; size variants; color customization; interactive content; and configurable offset.
/// </summary>
public partial class SgPopover : SgJsComponentBase
{
    private ElementReference _triggerRef;
    private ElementReference _popoverRef;
    private readonly SgDebouncer _hoverDebouncer = new();
    private bool _open;
    private bool _isClosing;
    private bool _lastSyncedOpen;
    private bool _lastRenderedOpen;
    private bool _attached;
    private int _zIndex;

    [Inject] private SgZIndexService ZIndexService { get; set; } = default!;

    private const int CloseAnimationMs = 200;
    private const int HoverGraceMs = 120;

    private static readonly Dictionary<SgPlacement, (string Css, string Js)> PlacementMap = new()
    {
        [SgPlacement.Top] = ("sgc-pop-top", "top"),
        [SgPlacement.TopStart] = ("sgc-pop-top-start", "top-start"),
        [SgPlacement.TopEnd] = ("sgc-pop-top-end", "top-end"),
        [SgPlacement.Bottom] = ("sgc-pop-bottom", "bottom"),
        [SgPlacement.BottomStart] = ("sgc-pop-bottom-start", "bottom-start"),
        [SgPlacement.BottomEnd] = ("sgc-pop-bottom-end", "bottom-end"),
        [SgPlacement.Left] = ("sgc-pop-left", "left"),
        [SgPlacement.LeftStart] = ("sgc-pop-left-start", "left-start"),
        [SgPlacement.LeftEnd] = ("sgc-pop-left-end", "left-end"),
        [SgPlacement.Right] = ("sgc-pop-right", "right"),
        [SgPlacement.RightStart] = ("sgc-pop-right-start", "right-start"),
        [SgPlacement.RightEnd] = ("sgc-pop-right-end", "right-end"),
    };

    private readonly string _titleId = SgIdGenerator.NewId("popover-title");

    /// <summary>
    /// Gets or sets whether the popover is currently open.
    /// </summary>
    [Parameter] public bool Open { get; set; }

    /// <summary>
    /// Gets or sets the callback invoked when the open state changes.
    /// </summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>
    /// Gets or sets the placement of the popover relative to the trigger. Default is <see cref="SgPlacement.BottomStart"/>.
    /// </summary>
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.BottomStart;

    /// <summary>
    /// Gets or sets the ARIA role attribute for the popover. Default is "dialog".
    /// </summary>
    [Parameter] public string Role { get; set; } = "dialog";

    /// <summary>
    /// Gets or sets the title text displayed in the popover header.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Gets or sets whether to display an arrow pointing to the trigger element.
    /// </summary>
    [Parameter] public bool ShowArrow { get; set; }

    /// <summary>
    /// Gets or sets whether the popover closes when clicking outside of it. Default is true.
    /// </summary>
    [Parameter] public bool CloseOnOutsideClick { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the popover closes when pressing the Escape key. Default is true.
    /// </summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>
    /// Gets or sets the trigger behavior for the popover. Default is <see cref="SgTrigger.Click"/>.
    /// When <see cref="SgTrigger.Manual"/>, use <see cref="OpenAsync"/>/<see cref="CloseAsync"/> or the <see cref="Open"/> parameter.
    /// </summary>
    [Parameter] public SgTrigger Trigger { get; set; } = SgTrigger.Click;

    /// <summary>
    /// Gets or sets whether the popover is disabled.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the text for the default trigger button. Default is "Open".
    /// </summary>
    [Parameter] public string ButtonText { get; set; } = "Open";

    /// <summary>
    /// Gets or sets the variant for the default trigger button. Default is <see cref="SgButtonVariant.Default"/>.
    /// </summary>
    [Parameter] public SgButtonVariant ButtonVariant { get; set; } = SgButtonVariant.Default;

    /// <summary>
    /// Gets or sets the size for the default trigger button. Default is <see cref="SgSize.Md"/>.
    /// </summary>
    [Parameter] public SgSize ButtonSize { get; set; } = SgSize.Md;

    /// <summary>
    /// Gets or sets the custom trigger content.
    /// When provided, replaces the default button trigger.
    /// </summary>
    [Parameter] public RenderFragment? TriggerContent { get; set; }

    /// <summary>
    /// Gets or sets the content to display inside the popover. Required.
    /// </summary>
    [Parameter, EditorRequired] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets whether to use a Portal to render the popover content.
    /// Recommended to stay true to avoid overflow clipping and stacking context issues.
    /// </summary>
    [Parameter] public bool UsePortal { get; set; } = true;

    /// <summary>
    /// Gets or sets the target element selector where the popover should be rendered via Portal.
    /// Default is "body". Set to null to render in-place (not recommended for nested overlays).
    /// </summary>
    [Parameter] public string? PortalTarget { get; set; } = "body";

    /// <summary>
    /// Gets or sets a fixed z-index for the popover.
    /// If not provided, it will be automatically allocated from <see cref="SgZIndexService.PopoverBase"/>.
    /// </summary>
    [Parameter] public int? ZIndex { get; set; }

    /// <summary>
    /// Gets or sets the popover size. When null, uses the default size.
    /// </summary>
    [Parameter] public SgSize? Size { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the popover. Example: "360px", "50vw".
    /// </summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets a custom CSS color value for the popover background.
    /// Example: "var(--sg-color-info)", "#3b82f6", etc.
    /// </summary>
    [Parameter] public string? Color { get; set; }

    /// <summary>
    /// Gets or sets whether the popover content is interactive.
    /// When true and <see cref="Trigger"/> is <see cref="SgTrigger.Hover"/>,
    /// the popover stays open when the mouse moves over the popover itself,
    /// allowing interaction with rich content.
    /// </summary>
    [Parameter] public bool Interactive { get; set; }

    /// <summary>
    /// Gets or sets the offset distance in pixels between the popover and the trigger element. Default is 6.
    /// </summary>
    [Parameter] public int Offset { get; set; } = 6;

    protected override string ModulePath => "./_content/SuperUI/superui-popover.js";
    protected override string IdPrefix => "sg-popover";

    private string PlacementCssClass => PlacementMap.TryGetValue(Placement, out var p) ? p.Css : "sgc-pop-bottom-start";
    private string PlacementString => PlacementMap.TryGetValue(Placement, out var p) ? p.Js : "bottom-start";

    private string PopoverCssClass
    {
        get
        {
            var css = CssBuilder.Default("sgc-pop")
                .AddClass(PlacementCssClass)
                .AddClass("sgc-pop-arrow", ShowArrow)
                .AddClass("sgc-closing", _isClosing)
                .AddClass("sgc-pop-interactive", Interactive && Trigger == SgTrigger.Hover)
                .AddClass(Size switch
                {
                    SgSize.Sm => "sgc-pop-sm",
                    SgSize.Md => "sgc-pop-md",
                    SgSize.Lg => "sgc-pop-lg",
                    SgSize.Xl => "sgc-pop-xl",
                    _ => null
                })
                .Build();
            return css;
        }
    }

    private string PopoverInlineStyle
    {
        get
        {
            var parts = new List<string>();
            if (_zIndex > 0)
                parts.Add($"z-index:{_zIndex}");
            if (!string.IsNullOrEmpty(MaxWidth))
                parts.Add($"max-width:{MaxWidth}");
            if (!string.IsNullOrEmpty(Color))
            {
                parts.Add($"--sgc-pop-bg:{Color}");
                parts.Add($"background:{Color}");
                parts.Add("color:#fff");
            }
            return parts.Count > 0 ? string.Join(";", parts) + ";" : "";
        }
    }

    protected override void OnParametersSet()
    {
        if (Open != _lastSyncedOpen)
        {
            _lastSyncedOpen = Open;
            _open = Open;
        }
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (_open && !_lastRenderedOpen)
        {
            _lastRenderedOpen = true;
            if (Module is not null && !_attached) // Only attach if not already attached via OnInteractiveAsync
                await AttachAsync();
        }
        else if (!_open && _lastRenderedOpen)
        {
            _lastRenderedOpen = false;
            await DetachAsync();
            _ = ScheduleClosingResetAsync();
        }
    }

    protected override async ValueTask OnInteractiveAsync()
    {
        if (_open)
            await AttachAsync();
    }

    protected override async ValueTask OnDisposingAsync()
    {
        _hoverDebouncer.Dispose();
        await DetachAsync();
    }

    private async Task AttachAsync()
    {
        _zIndex = ZIndex ?? ZIndexService.Allocate(this, SgZIndexService.PopoverBase);
        _isClosing = false;
        // Ensure state is synced before calling JS
        StateHasChanged();
        
        await SafeInvokeVoidAsync("attach",
            RootRef, _popoverRef, _triggerRef, SelfRef,
            CloseOnOutsideClick, CloseOnEscape, Offset, Interactive && Trigger == SgTrigger.Hover);
        _attached = true;
    }

    private async Task DetachAsync()
    {
        if (!_attached) return;
        if (!ZIndex.HasValue)
            ZIndexService.Release(this);
        
        _zIndex = 0;
        await SafeInvokeVoidAsync("detach", RootRef);
        _attached = false;
    }

    private async Task ScheduleClosingResetAsync()
    {
        _isClosing = true;
        StateHasChanged();
        try
        {
            await Task.Delay(CloseAnimationMs, ComponentLifetime);
        }
        catch (OperationCanceledException) { return; }

        if (!_open) // Only reset if it didn't reopen in the meantime
        {
            _isClosing = false;
            StateHasChanged();
        }
    }

    private async Task OnTriggerClickAsync()
    {
        if (Disabled || Trigger != SgTrigger.Click) return;
        await SetOpenAsync(!_open);
    }

    private async Task OnTriggerMouseEnterAsync()
    {
        if (Disabled) return;
        if (Trigger == SgTrigger.Hover)
        {
            _hoverDebouncer.Cancel();
            await SetOpenAsync(true);
        }
    }

    private async Task OnTriggerMouseLeaveAsync()
    {
        if (Trigger == SgTrigger.Hover && !Interactive)
        {
            await _hoverDebouncer.RunAsync(_ => InvokeAsync(() => SetOpenAsync(false)),
                TimeSpan.FromMilliseconds(HoverGraceMs));
        }
    }

    private Task OnPopoverMouseEnterAsync()
    {
        if (Trigger == SgTrigger.Hover && Interactive)
            _hoverDebouncer.Cancel();
        return Task.CompletedTask;
    }

    private async Task OnPopoverMouseLeaveAsync()
    {
        if (Trigger == SgTrigger.Hover && Interactive)
        {
            await _hoverDebouncer.RunAsync(_ => InvokeAsync(() => SetOpenAsync(false)),
                TimeSpan.FromMilliseconds(HoverGraceMs));
        }
    }

    /// <summary>
    /// Manually opens the popover. For use when Trigger is <see cref="SgTrigger.Manual"/>.
    /// </summary>
    public Task OpenAsync() => SetOpenAsync(true);

    /// <summary>
    /// Manually closes the popover. For use when Trigger is <see cref="SgTrigger.Manual"/>.
    /// </summary>
    public Task CloseAsync() => SetOpenAsync(false);

    [JSInvokable]
    public Task CloseFromJsAsync() => SetOpenAsync(false);

    private async Task SetOpenAsync(bool value)
    {
        if (_open == value) return;
        _open = value;
        if (OpenChanged.HasDelegate)
            await OpenChanged.InvokeAsync(value);
        StateHasChanged();
    }
}
