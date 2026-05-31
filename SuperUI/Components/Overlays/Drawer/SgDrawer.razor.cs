using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

/// <summary>Drawer overlay that slides, fades, or scales in from any edge of the viewport.</summary>
public partial class SgDrawer : SgOverlayComponentBase
{
    private readonly string _titleId = SgIdGenerator.NewId("drawer-title");
    private string _runtimeSize = string.Empty;
    private bool _loadingVisible = true;

    protected override string ModulePath => "./_content/SuperUI/superui-drawer.js";
    protected override int ZIndexBase => SgZIndexService.DrawerBase;
    protected override int ClosingAnimationMs => Animation switch
    {
        SgDrawerAnimation.Fade => 200,
        SgDrawerAnimation.Scale => 250,
        _ => 300
    };
    protected override string IdPrefix => "sg-drawer";

    // ── Content ──────────────────────────────────────────────────────────
    /// <summary>Title text displayed in the drawer header.</summary>
    [Parameter] public string? Title { get; set; }
    /// <summary>Main body content of the drawer.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
    /// <summary>Optional footer content rendered at the bottom of the drawer.</summary>
    [Parameter] public RenderFragment? FooterContent { get; set; }
    /// <summary>Optional custom header content.</summary>
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    /// <summary>Optional template for the title region, replaces <see cref="Title"/> when set.</summary>
    [Parameter] public RenderFragment? TitleTemplate { get; set; }
    /// <summary>Optional actions rendered in the header area (e.g. close button, toolbar).</summary>
    [Parameter] public RenderFragment? HeaderActions { get; set; }

    // ── Layout & Placement ───────────────────────────────────────────────
    /// <summary>Which edge the drawer slides in from.</summary>
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.Right;
    /// <summary>Explicit width (left/right) or height (top/bottom) of the drawer.</summary>
    [Parameter] public string Size { get; set; } = "360px";
    /// <summary>Fired when the drawer size changes after a user resize.</summary>
    [Parameter] public EventCallback<string> SizeChanged { get; set; }
    /// <summary>Named size preset (Sm, Md, Lg, Xl) — overrides <see cref="Size"/> when set.</summary>
    [Parameter] public SgDrawerSize? SizePreset { get; set; }
    /// <summary>Transition animation used when opening/closing.</summary>
    [Parameter] public SgDrawerAnimation Animation { get; set; } = SgDrawerAnimation.Slide;
    /// <summary>If true, the drawer covers the full viewport.</summary>
    [Parameter] public bool FullScreen { get; set; }
    /// <summary>Removes default padding from the drawer body.</summary>
    [Parameter] public bool NoPadding { get; set; }

    // ── Behavior ─────────────────────────────────────────────────────────
    /// <summary>Allows the user to resize the drawer by dragging its edge.</summary>
    [Parameter] public bool Resizable { get; set; }
    /// <summary>Shows a close button in the drawer header.</summary>
    [Parameter] public bool ShowClose { get; set; } = true;
    /// <summary>Shows the header bar.</summary>
    [Parameter] public bool ShowHeader { get; set; } = true;
    /// <summary>Closes the drawer when the backdrop is clicked.</summary>
    [Parameter] public bool CloseOnBackdrop { get; set; } = true;
    /// <summary>Closes the drawer when the Escape key is pressed.</summary>
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    /// <summary>Automatically focuses the drawer when opened.</summary>
    [Parameter] public bool AutoFocus { get; set; } = true;
    /// <summary>Prevents the body scroll-lock behavior when the drawer is open.</summary>
    [Parameter] public bool DisableScrollLock { get; set; }
    /// <summary>Shows a loading spinner in the drawer body.</summary>
    [Parameter] public bool Loading { get; set; }

    // ── Appearance ───────────────────────────────────────────────────────
    /// <summary>Custom close icon markup (ignored if <see cref="ShowClose"/> is false).</summary>
    [Parameter] public string? CloseIcon { get; set; }
    /// <summary>Minimum size the drawer can be resized to.</summary>
    [Parameter] public string? MinSize { get; set; } = "200px";
    /// <summary>Maximum size the drawer can stretch to.</summary>
    [Parameter] public string? MaxSize { get; set; }
    /// <summary>CSS backdrop-filter blur value applied to the overlay backdrop.</summary>
    [Parameter] public string? BackdropBlur { get; set; }
    /// <summary>Additional CSS class for the body content area.</summary>
    [Parameter] public string? BodyClass { get; set; }
    /// <summary>Additional CSS class for the header area.</summary>
    [Parameter] public string? HeaderClass { get; set; }
    /// <summary>Additional CSS class for the footer area.</summary>
    [Parameter] public string? FooterClass { get; set; }

    // ── Events ───────────────────────────────────────────────────────────
    /// <summary>Fired when the drawer begins closing.</summary>
    [Parameter] public EventCallback OnClose { get; set; }
    /// <summary>Fired after the drawer has finished opening.</summary>
    [Parameter] public EventCallback OnOpened { get; set; }
    /// <summary>Fired after the drawer has finished closing.</summary>
    [Parameter] public EventCallback OnClosed { get; set; }
    /// <summary>Fired when the <see cref="Placement"/> value changes.</summary>
    [Parameter] public EventCallback<SgPlacement> PlacementChanged { get; set; }

    /// <summary>
    /// Called before close. Return false to cancel closing.
    /// </summary>
    [Parameter] public Func<Task<bool>>? CloseConfirm { get; set; }

    /// <summary>
    /// Called before closing can be cancelled by setting <c>args.Cancel = true</c>.
    /// </summary>
    [Parameter] public EventCallback<CloseEventArgs> BeforeClose { get; set; }

    // ── Computed size ────────────────────────────────────────────────────
    private string ComputedSize
    {
        get
        {
            if (!string.IsNullOrEmpty(_runtimeSize)) return _runtimeSize;
            if (SizePreset is not null) return SizePresetToPx(SizePreset.Value);
            return Size;
        }
    }

    private static string SizePresetToPx(SgDrawerSize preset, bool isHorizontal = false)
    {
        return preset switch
        {
            SgDrawerSize.Sm => isHorizontal ? "180px" : "300px",
            SgDrawerSize.Md => isHorizontal ? "240px" : "400px",
            SgDrawerSize.Lg => isHorizontal ? "320px" : "600px",
            SgDrawerSize.Xl => isHorizontal ? "480px" : "800px",
            _ => "400px"
        };
    }

    // ── CSS classes ──────────────────────────────────────────────────────
    private string PlacementClass => Placement switch
    {
        SgPlacement.Left => "sgc-drawer-left",
        SgPlacement.Top => "sgc-drawer-top",
        SgPlacement.Bottom => "sgc-drawer-bottom",
        _ => "sgc-drawer-right"
    };

    private string AnimationClass => Animation switch
    {
        SgDrawerAnimation.Fade => "sgc-drawer-fade",
        SgDrawerAnimation.Scale => "sgc-drawer-scale",
        _ => ""
    };

    private string ResizerClass => Placement switch
    {
        SgPlacement.Left => "sgc-drawer-resizer-right",
        SgPlacement.Top => "sgc-drawer-resizer-bottom",
        SgPlacement.Bottom => "sgc-drawer-resizer-top",
        _ => "sgc-drawer-resizer-left"
    };

    private string GetDrawerCssClasses()
    {
        return Css("sgc-drawer")
            .AddClass(PlacementClass)
            .AddClass(AnimationClass)
            .AddClass(IsClosing ? "sgc-closing" : "")
            .AddClass(NoPadding ? "sgc-drawer-no-padding" : "")
            .AddClass(FullScreen ? "sgc-drawer-fullscreen" : "")
            .AddClass(DisableScrollLock ? "sgc-drawer-no-scroll-lock" : "")
            .Build();
    }

    private string GetHeaderClasses()
    {
        return Css("sgc-drawer-header")
            .AddClass(HeaderClass)
            .Build();
    }

    private string GetBodyClasses()
    {
        return Css("sgc-drawer-body")
            .AddClass("sgc-drawer-body-loading", Loading)
            .AddClass(BodyClass)
            .Build();
    }

    private string GetFooterClasses()
    {
        return Css("sgc-drawer-footer")
            .AddClass(FooterClass)
            .Build();
    }

    // ── Style ────────────────────────────────────────────────────────────
    private string GetDrawerStyle()
    {
        var sz = ComputedSize;
        var isH = Placement is SgPlacement.Top or SgPlacement.Bottom;
        return Styles()
            .AddStyle("z-index", ZIndexValue.ToString(), ZIndexValue > 0)
            .AddStyle("width", sz, !isH)
            .AddStyle("max-width", MaxSize ?? "100vw", !isH)
            .AddStyle("height", sz, isH)
            .AddStyle("max-height", MaxSize ?? "100vh", isH)
            .AddStyle("--sgc-drawer-min-size", MinSize ?? "200px")
            .Build();
    }

    private string GetBackdropStyle()
    {
        return Styles()
            .AddStyle("z-index", BackdropZIndex.ToString())
            .AddStyle("backdrop-filter", BackdropBlur, !string.IsNullOrEmpty(BackdropBlur))
            .AddStyle("-webkit-backdrop-filter", BackdropBlur, !string.IsNullOrEmpty(BackdropBlur))
            .Build();
    }

    // ── Lifecycle ────────────────────────────────────────────────────────
    protected override async ValueTask OnOpeningAsync()
    {
        _loadingVisible = true;
        await SafeInvokeVoidAsync("attach", RootRef, SelfRef, CloseOnEscape, AutoFocus, DisableScrollLock);
        if (Resizable)
            await SafeInvokeVoidAsync("initResize", RootRef, SelfRef, PlacementClass, MinSize ?? "200px", MaxSize ?? "");
    }

    protected override async ValueTask OnOpenedAsync()
    {
        _loadingVisible = false;
        if (OnOpened.HasDelegate)
            await OnOpened.InvokeAsync();
    }

    protected override async ValueTask OnClosingAsync()
    {
        await SafeInvokeVoidAsync("detach", RootRef);
    }

    protected override async ValueTask OnClosedAsync()
    {
        if (OnClose.HasDelegate)
            await OnClose.InvokeAsync();
        if (OnClosed.HasDelegate)
            await OnClosed.InvokeAsync();
    }

    protected override ValueTask OnDisposingAsync()
    {
        if (Visible)
            _ = SafeInvokeVoidAsync("detach", RootRef);
        return base.OnDisposingAsync();
    }

    // ── Close logic with confirm ─────────────────────────────────────────
    public override async Task CloseAsync()
    {
        if (IsDisposed || IsClosing || !Visible) return;

        if (BeforeClose.HasDelegate)
        {
            var args = new CloseEventArgs();
            await BeforeClose.InvokeAsync(args);
            if (args.Cancel) return;
        }

        if (CloseConfirm is not null)
        {
            var confirmed = await CloseConfirm();
            if (!confirmed) return;
        }

        await base.CloseAsync();
    }

    // ── JSInvokable ──────────────────────────────────────────────────────
    [JSInvokable]
    public override Task RequestCloseAsync() => CloseAsync();

    [JSInvokable]
    public async Task UpdateSizeFromJs(string newSize)
    {
        _runtimeSize = newSize;
        if (SizeChanged.HasDelegate)
            await SizeChanged.InvokeAsync(newSize);
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task OnSwipeClose()
    {
        if (CloseOnBackdrop)
            await CloseAsync();
    }

    // ── Private ──────────────────────────────────────────────────────────
    private Task BackdropClickAsync() =>
        CloseOnBackdrop ? CloseAsync() : Task.CompletedTask;
}

/// <summary>
/// Event args for <see cref="SgDrawer.BeforeClose"/> — set <see cref="Cancel"/> to true to prevent closing.
/// </summary>
public class CloseEventArgs
{
    public bool Cancel { get; set; }
}
