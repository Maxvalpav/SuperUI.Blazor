using Microsoft.AspNetCore.Components;
using SuperUI.Base.ComponentBases;
using SuperUI.Services;

namespace SuperUI.Components;

/// <summary>
/// Teleports content to <c>document.body</c> (or a custom target) to escape CSS stacking contexts,
/// overflow clipping, and transform distortions. When <see cref="ZIndex"/> is not explicitly set,
/// uses <see cref="SgZIndexService"/> for automatic stacking with other overlays.
/// </summary>
public partial class SgPortal : SgJsComponentBase
{
    private bool _prevVisible;
    private bool _isTeleported;
    private int _allocatedZIndex;

    [Inject]
    private SgZIndexService ZIndexService { get; set; } = default!;

    /// <summary>Content to teleport to the target container.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>When <c>true</c>, teleports content to the render target. Supports two-way binding.</summary>
    [Parameter] public bool Visible { get; set; }

    /// <summary>Fires when <see cref="Visible"/> changes.</summary>
    [Parameter] public EventCallback<bool> VisibleChanged { get; set; }

    /// <summary>
    /// Explicit z-index for the portal. When omitted, auto-allocates from <see cref="SgZIndexService.PortalBase"/>.
    /// Set to 0 to suppress any z-index.
    /// </summary>
    [Parameter] public int? ZIndex { get; set; }

    /// <summary>When <c>true</c>, locks body scroll while the portal is open.</summary>
    [Parameter] public bool PreventScroll { get; set; }

    /// <summary>When <c>true</c>, focuses the first focusable element inside the portal on open.</summary>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>CSS selector for the target container. Defaults to <c>body</c>.</summary>
    [Parameter] public string? RenderAt { get; set; }

    /// <summary>Transition/animation duration in milliseconds.</summary>
    [Parameter] public int TransitionDuration { get; set; } = 250;

    /// <summary>Fired after the portal content has been teleported to the target container.</summary>
    [Parameter] public EventCallback OnTeleported { get; set; }

    protected override string ModulePath => "./_content/SuperUI/superui-portal.js";

    private string PortalWrapperStyle => Styles()
        .AddStyle("display", "contents")
        .AddStyle("visibility", "hidden", !(_isTeleported && Visible))
        .Build();

    private int ResolvedZIndex
    {
        get
        {
            if (ZIndex.HasValue) return ZIndex.Value;
            return _allocatedZIndex;
        }
    }

    protected override async Task OnAfterRenderSafeAsync(bool firstRender)
    {
        if (!Visible && !_prevVisible) return;

        if (Visible && !_prevVisible)
        {
            AllocateZIndex();
            await SafeInvokeVoidAsync("open", RootRef, new
            {
                zIndex = ResolvedZIndex,
                preventScroll = PreventScroll,
                autoFocus = AutoFocus,
                renderAt = RenderAt,
                transitionDuration = TransitionDuration > 0 ? TransitionDuration : (int?)null,
            });
            _isTeleported = true;
            await OnTeleported.InvokeAsync();
            StateHasChanged();
        }
        else if (!Visible && _prevVisible)
        {
            await SafeInvokeVoidAsync("close", RootRef);
            ReleaseZIndex();
            _isTeleported = false;
        }
        else if (Visible && _prevVisible)
        {
            await SafeInvokeVoidAsync("update", RootRef, new
            {
                zIndex = ResolvedZIndex,
                preventScroll = PreventScroll,
            });
        }

        _prevVisible = Visible;
    }

    protected override async ValueTask OnDisposingAsync()
    {
        if (_prevVisible)
        {
            await SafeInvokeVoidAsync("close", RootRef);
        }
        ReleaseZIndex();
        _prevVisible = false;
    }

    private void AllocateZIndex()
    {
        if (ZIndex.HasValue) return;
        if (_allocatedZIndex > 0) ZIndexService.Release(this);
        _allocatedZIndex = ZIndexService.Allocate(this, SgZIndexService.PortalBase);
    }

    private void ReleaseZIndex()
    {
        if (_allocatedZIndex > 0)
        {
            ZIndexService.Release(this);
            _allocatedZIndex = 0;
        }
    }
}
