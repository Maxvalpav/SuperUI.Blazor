using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

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
    [Parameter] public string? Title { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? FooterContent { get; set; }
    [Parameter] public RenderFragment? HeaderContent { get; set; }
    [Parameter] public RenderFragment? TitleTemplate { get; set; }
    [Parameter] public RenderFragment? HeaderActions { get; set; }

    // ── Layout & Placement ───────────────────────────────────────────────
    [Parameter] public SgPlacement Placement { get; set; } = SgPlacement.Right;
    [Parameter] public string Size { get; set; } = "360px";
    [Parameter] public EventCallback<string> SizeChanged { get; set; }
    [Parameter] public SgDrawerSize? SizePreset { get; set; }
    [Parameter] public SgDrawerAnimation Animation { get; set; } = SgDrawerAnimation.Slide;
    [Parameter] public bool FullScreen { get; set; }
    [Parameter] public bool NoPadding { get; set; }

    // ── Behavior ─────────────────────────────────────────────────────────
    [Parameter] public bool Resizable { get; set; }
    [Parameter] public bool ShowClose { get; set; } = true;
    [Parameter] public bool ShowHeader { get; set; } = true;
    [Parameter] public bool CloseOnBackdrop { get; set; } = true;
    [Parameter] public bool CloseOnEscape { get; set; } = true;
    [Parameter] public bool AutoFocus { get; set; } = true;
    [Parameter] public bool DisableScrollLock { get; set; }
    [Parameter] public bool Loading { get; set; }

    // ── Appearance ───────────────────────────────────────────────────────
    [Parameter] public string? CloseIcon { get; set; }
    [Parameter] public string? MinSize { get; set; } = "200px";
    [Parameter] public string? MaxSize { get; set; }
    [Parameter] public string? BackdropBlur { get; set; }
    [Parameter] public string? BodyClass { get; set; }
    [Parameter] public string? HeaderClass { get; set; }
    [Parameter] public string? FooterClass { get; set; }

    // ── Events ───────────────────────────────────────────────────────────
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public EventCallback OnOpened { get; set; }
    [Parameter] public EventCallback OnClosed { get; set; }
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

    // ── Close logic with confirm ─────────────────────────────────────────
    public new async Task CloseAsync()
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
