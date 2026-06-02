namespace SuperUI.Components;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Localization;
using SuperUI.Services;

/// <summary>
/// A fully-featured modal dialog component with animations, sizing, positioning, drag, resize, and more.
/// </summary>
public partial class SgModal : SgOverlayComponentBase
{
    private string _titleId => SgIdGenerator.StableIdFor(this, "modal-title");
    private string _bodyId => SgIdGenerator.StableIdFor(this, "modal-body");
    private ElementReference _headerRef;
    private ElementReference _backdropRef;
    private bool _openedFired;
    private bool _isMaximized;
    private bool _responsiveFull;

    // ── Core parameters ─────────────────────────────────────────────────

    /// <summary>Title text shown in the modal header.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>Body content rendered inside the modal.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Optional footer content (buttons, etc.).</summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }

    /// <summary>Custom header content — replaces the default title+icon layout.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }

    /// <summary>SVG path data or icon name for the title icon.</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Custom SVG path data for the close button icon.</summary>
    [Parameter] public string? CloseIcon { get; set; }

    // ── Behavior ────────────────────────────────────────────────────────

    /// <summary>Whether to show the close (X) button in the header. Default: true.</summary>
    [Parameter] public bool ShowClose { get; set; } = true;

    /// <summary>Whether clicking the backdrop closes the modal. Default: true.</summary>
    [Parameter] public bool CloseOnBackdrop { get; set; } = true;

    /// <summary>Whether pressing Escape closes the modal. Default: true.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;

    /// <summary>The open/close animation type. Default: Scale.</summary>
    [Parameter] public SgModalAnimation Animation { get; set; } = SgModalAnimation.Scale;

    /// <summary>Whether the modal can be dragged by its header.</summary>
    [Parameter] public bool Draggable { get; set; }

    /// <summary>Whether to auto-focus the first focusable element inside the modal. Default: true.</summary>
    [Parameter] public bool AutoFocus { get; set; } = true;

    /// <summary>Whether to trap focus inside the modal. Default: true.</summary>
    [Parameter] public bool TrapFocus { get; set; } = true;

    /// <summary>Whether to lock body scroll when the modal is open. Default: true.</summary>
    [Parameter] public bool ScrollLock { get; set; } = true;

    // ── Sizing ──────────────────────────────────────────────────────────

    /// <summary>Custom CSS width (e.g. "600px", "50vw").</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Custom CSS max-width.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Custom CSS min-width.</summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>Custom CSS height (e.g. "400px", "60vh").</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Custom CSS max-height (e.g. "90vh").</summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>Custom CSS min-height (e.g. "200px").</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Preset size (Sm/Md/Lg/Xl). Default: Md.</summary>
    [Parameter] public SgModalSize Size { get; set; } = SgModalSize.Md;

    /// <summary>Whether the modal fills the entire screen. Overrides Size.</summary>
    [Parameter] public bool FullScreen { get; set; }

    // ── Position ────────────────────────────────────────────────────────

    /// <summary>Vertical position on screen: Center, Top, or Bottom. Default: Center.</summary>
    [Parameter] public SgModalPosition Position { get; set; } = SgModalPosition.Center;

    // ── Backdrop ────────────────────────────────────────────────────────

    /// <summary>Content rendered inside the backdrop layer.</summary>
    [Parameter] public RenderFragment? BackdropContent { get; set; }

    /// <summary>Whether backdrop dismiss is allowed. Default: true.</summary>
    [Parameter] public bool BackdropDismiss { get; set; } = true;

    /// <summary>CSS blur value applied to the backdrop (e.g. "8px").</summary>
    [Parameter] public string? BackdropBlur { get; set; }

    // ── Advanced ────────────────────────────────────────────────────────

    /// <summary>Whether to show the minimize button in the header.</summary>
    [Parameter] public bool ShowMinimize { get; set; }

    /// <summary>Whether to show the maximize/restore button in the header.</summary>
    [Parameter] public bool ShowMaximize { get; set; }

    /// <summary>Whether the modal can be resized by dragging edges/corners.</summary>
    [Parameter] public bool Resizable { get; set; }

    /// <summary>Whether to auto-switch to fullscreen on narrow viewports (&lt;= 768px).</summary>
    [Parameter] public bool ResponsiveMode { get; set; }

    /// <summary>Whether to show a loading overlay inside the modal.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Custom loading overlay content.</summary>
    [Parameter] public RenderFragment? LoadingContent { get; set; }

    /// <summary>Additional CSS class for the modal body.</summary>
    [Parameter] public string? BodyClass { get; set; }

    /// <summary>Additional CSS class for the modal header.</summary>
    [Parameter] public string? HeaderClass { get; set; }

    /// <summary>Additional CSS class for the modal footer.</summary>
    [Parameter] public string? FooterClass { get; set; }

    /// <summary>Removes body padding for edge-to-edge content.</summary>
    [Parameter] public bool NoPadding { get; set; }

    /// <summary>Enables glassmorphism effect (semi-transparent background with blur).</summary>
    [Parameter] public bool Glass { get; set; }

    /// <summary>Custom z-index value. Overrides the default modal z-index stack.</summary>
    [Parameter] public int? CustomZIndex { get; set; }

    /// <summary>Keyboard shortcut to submit (e.g. "ctrl+enter"). Triggers OnSubmit.</summary>
    [Parameter] public string? ShortcutSubmit { get; set; }

    /// <summary>Fired when the shortcut key combination is pressed.</summary>
    [Parameter] public EventCallback OnSubmit { get; set; }

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Fired after the modal has fully closed.</summary>
    [Parameter] public EventCallback OnClose { get; set; }

    /// <summary>Fired while the modal is closing (before animation completes).</summary>
    [Parameter] public EventCallback OnClosing { get; set; }

    /// <summary>Fired after the modal has fully opened and animation completed.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }

    /// <summary>Fired when the maximized state changes. Receives the new maximized value.</summary>
    [Parameter] public EventCallback<bool> OnMaximizedChanged { get; set; }

    /// <summary>Fired when the minimize button is clicked.</summary>
    [Parameter] public EventCallback OnMinimized { get; set; }

    protected override string ModulePath => "./_content/SuperUI/superui-modal.js";
    protected override int ZIndexBase => CustomZIndex ?? SgZIndexService.ModalBase;
    protected override string IdPrefix => "sg-modal";
    protected override int ClosingAnimationMs => Animation == SgModalAnimation.None ? 0 : 200;

    // ── Lifecycle ────────────────────────────────────────────────────────

    protected override async ValueTask OnOpeningAsync()
    {
        _openedFired = false;
        _responsiveFull = false;

        if (ResponsiveMode)
        {
            await SafeInvokeVoidAsync("watchResponsive", RootRef, SelfRef);
        }

        await SafeInvokeVoidAsync("attach",
            RootRef, SelfRef,
            CloseOnEscape, FullScreen || _responsiveFull,
            AutoFocus, TrapFocus, ScrollLock);

        if (Draggable && !FullScreen && !_responsiveFull)
            await SafeInvokeVoidAsync("initDrag", RootRef, _headerRef);

        if (Resizable)
            await SafeInvokeVoidAsync("initResize", RootRef);

        if (!string.IsNullOrEmpty(ShortcutSubmit))
            await SafeInvokeVoidAsync("initShortcuts", RootRef, SelfRef, ShortcutSubmit);
    }

    protected override async ValueTask OnOpenedAsync()
    {
        if (_openedFired) return;
        _openedFired = true;
        if (OnOpened.HasDelegate)
            await OnOpened.InvokeAsync();
    }

    protected override async ValueTask OnClosingAsync()
    {
        _responsiveFull = false;

        if (ResponsiveMode)
            await SafeInvokeVoidAsync("unwatchResponsive", RootRef);

        if (OnClosing.HasDelegate)
            await OnClosing.InvokeAsync();

        await SafeInvokeVoidAsync("detach", RootRef);
    }

    protected override async ValueTask OnClosedAsync()
    {
        _responsiveFull = false;

        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
    }

    protected override async ValueTask OnDisposingAsync()
    {
        if (Visible)
        {
            if (ResponsiveMode)
                await SafeInvokeVoidAsync("unwatchResponsive", RootRef);
            await SafeInvokeVoidAsync("detach", RootRef);
        }
        await base.OnDisposingAsync();
    }

    // ── JS-invokable ────────────────────────────────────────────────────

    [JSInvokable]
    public override Task RequestCloseAsync() => CloseAsync();

    [JSInvokable]
    public async Task OnSubmitAsync()
    {
        if (OnSubmit.HasDelegate)
            await OnSubmit.InvokeAsync();
    }

    [JSInvokable]
    public async Task OnResponsiveChangeAsync(bool isMobile)
    {
        if (_responsiveFull != isMobile)
        {
            _responsiveFull = isMobile;
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>Toggles the maximized state of the modal.</summary>
    public async Task ToggleMaximized()
    {
        _isMaximized = !_isMaximized;
        if (OnMaximizedChanged.HasDelegate)
            await OnMaximizedChanged.InvokeAsync(_isMaximized);
    }

    /// <summary>Minimizes (closes) the modal. Fires <see cref="OnMinimized"/> first.</summary>
    public async Task MinimizeAsync()
    {
        if (OnMinimized.HasDelegate)
            await OnMinimized.InvokeAsync();
        await CloseAsync();
    }

    // ── CSS class builders ──────────────────────────────────────────────

    private string GetBackdropClasses()
    {
        var sb = new List<string> { "sgc-modal-backdrop" };
        if (IsClosing) sb.Add("sgc-closing");
        if (Position == SgModalPosition.Top) sb.Add("sgc-modal-backdrop-top");
        if (Position == SgModalPosition.Bottom) sb.Add("sgc-modal-backdrop-bottom");
        if (!BackdropDismiss) sb.Add("sgc-modal-backdrop-static");
        return string.Join(" ", sb);
    }

    private string GetBackdropStyle()
    {
        return Styles()
            .AddStyle("z-index", BackdropZIndex.ToString(), BackdropZIndex > 0)
            .AddStyle("backdrop-filter", $"blur({BackdropBlur})", !string.IsNullOrEmpty(BackdropBlur))
            .Build();
    }

    private string GetModalClasses()
    {
        var sb = new List<string> { "sgc-modal" };
        sb.Add("sgc-modal-anim-" + AnimationClass);
        if (FullScreen || _responsiveFull) sb.Add("sgc-modal-fullscreen");
        else sb.Add("sgc-modal-size-" + SizeClass);
        if (_isMaximized && !FullScreen && !_responsiveFull) sb.Add("sgc-modal-maximized");
        if (Position == SgModalPosition.Top) sb.Add("sgc-modal-pos-top");
        if (Position == SgModalPosition.Bottom) sb.Add("sgc-modal-pos-bottom");
        if (NoPadding) sb.Add("sgc-modal-no-padding");
        if (Glass) sb.Add("sg-glass");
        if (Resizable) sb.Add("sgc-modal-resizable");
        return string.Join(" ", sb);
    }

    private string GetHeaderClasses()
    {
        var sb = new List<string> { "sgc-modal-header" };
        if (Draggable && !FullScreen && !_responsiveFull) sb.Add("sgc-modal-header-draggable");
        if (!string.IsNullOrEmpty(HeaderClass)) sb.Add(HeaderClass);
        if (ShowMaximize || ShowMinimize) sb.Add("sgc-modal-header-has-maximize");
        return string.Join(" ", sb);
    }

    private string GetBodyClasses()
    {
        var sb = new List<string> { "sgc-modal-body" };
        if (!string.IsNullOrEmpty(BodyClass)) sb.Add(BodyClass);
        return string.Join(" ", sb);
    }

    private string GetFooterClasses()
    {
        var sb = new List<string> { "sgc-modal-footer" };
        if (!string.IsNullOrEmpty(FooterClass)) sb.Add(FooterClass);
        return string.Join(" ", sb);
    }

    private string AnimationClass => Animation switch
    {
        SgModalAnimation.None => "none",
        SgModalAnimation.Fade => "fade",
        SgModalAnimation.Zoom => "zoom",
        SgModalAnimation.SlideUp => "slide-up",
        SgModalAnimation.SlideDown => "slide-down",
        SgModalAnimation.SlideLeft => "slide-left",
        SgModalAnimation.SlideRight => "slide-right",
        SgModalAnimation.Scale => "scale",
        SgModalAnimation.Slide => "slide",
        _ => "scale"
    };

    private string SizeClass => Size switch
    {
        SgModalSize.Sm => "sm",
        SgModalSize.Md => "md",
        SgModalSize.Lg => "lg",
        SgModalSize.Xl => "xl",
        _ => "md"
    };

    private string GetStyle()
    {
        return Styles()
            .AddStyle("z-index", ZIndexValue.ToString(), ZIndexValue > 0)
            .AddStyle("width", Width, !string.IsNullOrEmpty(Width))
            .AddStyle("max-width", MaxWidth, !string.IsNullOrEmpty(MaxWidth))
            .AddStyle("min-width", MinWidth, !string.IsNullOrEmpty(MinWidth))
            .AddStyle("height", Height, !string.IsNullOrEmpty(Height))
            .AddStyle("max-height", MaxHeight, !string.IsNullOrEmpty(MaxHeight))
            .AddStyle("min-height", MinHeight, !string.IsNullOrEmpty(MinHeight))
            .Build();
    }

    private Task BackdropClickAsync()
        => BackdropDismiss && CloseOnBackdrop ? CloseAsync() : Task.CompletedTask;
}
