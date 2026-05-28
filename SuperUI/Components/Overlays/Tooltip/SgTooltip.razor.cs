using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SuperUI.Base.Builders;
using SuperUI.Base.ComponentBases;
using SuperUI.Base.Utilities;
using SuperUI.Enums;
using SuperUI.Services;

namespace SuperUI.Components;

/// <summary>
/// A tooltip component that displays contextual information when hovering, focusing, or clicking a trigger element.
/// Supports rich content, arrows, color variants, 12 placements, delay configuration, interactive content, and cursor following.
/// </summary>
public partial class SgTooltip : SgOverlayComponentBase
{
    private ElementReference _tooltipRef;
    private bool _attached;
    private readonly SgDebouncer _showDebouncer = new();
    private readonly SgDebouncer _hideDebouncer = new();

    private static readonly Dictionary<SgPlacement, (string Css, string Js)> PlacementMap = new()
    {
        [SgPlacement.Top] = ("sgc-tt-top", "top"),
        [SgPlacement.TopStart] = ("sgc-tt-top-start", "top-start"),
        [SgPlacement.TopEnd] = ("sgc-tt-top-end", "top-end"),
        [SgPlacement.Bottom] = ("sgc-tt-bottom", "bottom"),
        [SgPlacement.BottomStart] = ("sgc-tt-bottom-start", "bottom-start"),
        [SgPlacement.BottomEnd] = ("sgc-tt-bottom-end", "bottom-end"),
        [SgPlacement.Left] = ("sgc-tt-left", "left"),
        [SgPlacement.LeftStart] = ("sgc-tt-left-start", "left-start"),
        [SgPlacement.LeftEnd] = ("sgc-tt-left-end", "left-end"),
        [SgPlacement.Right] = ("sgc-tt-right", "right"),
        [SgPlacement.RightStart] = ("sgc-tt-right-start", "right-start"),
        [SgPlacement.RightEnd] = ("sgc-tt-right-end", "right-end"),
    };

    /// <summary>
    /// Gets or sets the tooltip text to display. Required when <see cref="RichContent"/> is not set.
    /// </summary>
    [Parameter, EditorRequired]
    public string Text { get; set; } = default!;

    /// <summary>
    /// Gets or sets the content that triggers the tooltip.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Gets or sets the preferred placement. Default is <see cref="SgPlacement.Top"/>.
    /// </summary>
    [Parameter]
    public SgPlacement Placement { get; set; } = SgPlacement.Top;

    /// <summary>
    /// Gets or sets the trigger behavior. Default is <see cref="SgTrigger.Hover"/>.
    /// </summary>
    [Parameter]
    public SgTrigger Trigger { get; set; } = SgTrigger.Hover;

    /// <summary>
    /// Gets or sets the delay in milliseconds before showing the tooltip. Default 0.
    /// </summary>
    [Parameter]
    public int OpenDelay { get; set; }

    /// <summary>
    /// Gets or sets the delay in milliseconds before hiding the tooltip. Default 0.
    /// </summary>
    [Parameter]
    public int CloseDelay { get; set; }

    /// <summary>
    /// Gets or sets whether the tooltip is disabled.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets rich content displayed inside the tooltip. When set, overrides <see cref="Text"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? RichContent { get; set; }

    /// <summary>
    /// Gets or sets whether to show an arrow pointing to the trigger element.
    /// </summary>
    [Parameter]
    public bool ShowArrow { get; set; }

    /// <summary>
    /// Gets or sets a custom CSS color value for the tooltip background.
    /// Example: "var(--sg-color-info)", "#3b82f6", etc.
    /// </summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the tooltip. Example: "280px", "50vw".
    /// </summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Gets or sets the tooltip size. When null, uses the default size.
    /// </summary>
    [Parameter]
    public SgSize? Size { get; set; }

    /// <summary>
    /// Gets or sets the offset distance in pixels between the tooltip and the trigger element. Default is 8.
    /// </summary>
    [Parameter]
    public int Offset { get; set; } = 8;

    /// <summary>
    /// Gets or sets whether the tooltip follows the mouse cursor. Default is false.
    /// When enabled, the tooltip repositions on mouse move within the trigger element.
    /// </summary>
    [Parameter]
    public bool FollowCursor { get; set; }

    /// <summary>
    /// Gets or sets whether the tooltip content is interactive.
    /// When true, the tooltip does not hide when the mouse moves over the tooltip itself,
    /// allowing interaction with rich content.
    /// </summary>
    [Parameter]
    public bool Interactive { get; set; }

    protected override string ModulePath => "./_content/SuperUI/superui-tooltip.js";
    protected override int ZIndexBase => SgZIndexService.TooltipBase;
    protected override int ClosingAnimationMs => 0;

    private string PlacementCssClass => PlacementMap.TryGetValue(Placement, out var p) ? p.Css : "sgc-tt-top";
    private string PlacementString => PlacementMap.TryGetValue(Placement, out var p) ? p.Js : "top";

    private string TooltipCssClass
    {
        get
        {
            var css = CssBuilder.Default("sgc-tt")
                .AddClass(PlacementCssClass)
                .AddClass("sgc-tt-arrow", ShowArrow)
                .AddClass(Size switch
                {
                    SgSize.Sm => "sgc-tt-sm",
                    SgSize.Md => "sgc-tt-md",
                    SgSize.Lg => "sgc-tt-lg",
                    SgSize.Xl => "sgc-tt-xl",
                    _ => null
                })
                .AddClass("sgc-tt-interactive", Interactive)
                .Build();
            return css;
        }
    }

    private string TooltipInlineStyle
    {
        get
        {
            var parts = new List<string>
            {
                "position:fixed",
                "pointer-events:none",
                "opacity:0",
                "transition:opacity .15s ease",
                $"z-index:{ZIndexValue}"
            };
            if (!string.IsNullOrEmpty(MaxWidth))
                parts.Add($"max-width:{MaxWidth}");
            if (!string.IsNullOrEmpty(Color))
            {
                parts.Add($"--sg-tt-bg:{Color}");
                parts.Add($"background:{Color}");
                parts.Add("color:#fff");
            }
            return string.Join(";", parts) + ";";
        }
    }

    private Task OnMouseEnterAsync() => HandleTriggerAsync(SgTrigger.Hover, true);
    private Task OnMouseLeaveAsync() => HandleTriggerAsync(SgTrigger.Hover, false);
    private Task OnFocusAsync() => HandleTriggerAsync(SgTrigger.Focus, true);
    private Task OnBlurAsync() => HandleTriggerAsync(SgTrigger.Focus, false);
    private Task OnClickAsync() => HandleTriggerAsync(SgTrigger.Click, !Visible);

    private Task HandleTriggerAsync(SgTrigger triggerType, bool show)
    {
        if (Trigger != triggerType) return Task.CompletedTask;
        return show ? ShowAsync() : HideAsync();
    }

    /// <summary>
    /// Manually shows the tooltip. For use when Trigger is <see cref="SgTrigger.Manual"/>.
    /// </summary>
    public async Task ShowAsync()
    {
        if (IsDisposed || Visible || Disabled) return;
        _hideDebouncer.Cancel();

        if (OpenDelay > 0)
            await _showDebouncer.RunAsync(
                _ => InvokeAsync(SetVisibleTrueAsync),
                TimeSpan.FromMilliseconds(OpenDelay));
        else
            await SetVisibleTrueAsync();
    }

    /// <summary>
    /// Manually hides the tooltip. For use when Trigger is <see cref="SgTrigger.Manual"/>.
    /// </summary>
    public async Task HideAsync()
    {
        if (IsDisposed || !Visible) return;
        _showDebouncer.Cancel();

        if (CloseDelay > 0)
            await _hideDebouncer.RunAsync(
                _ => InvokeAsync(SetVisibleFalseAsync),
                TimeSpan.FromMilliseconds(CloseDelay));
        else
            await SetVisibleFalseAsync();
    }

    private async Task SetVisibleTrueAsync()
    {
        if (Visible) return;
        Visible = true;
        if (VisibleChanged.HasDelegate) await VisibleChanged.InvokeAsync(true);
        StateHasChanged();
    }

    private Task SetVisibleFalseAsync() => CloseAsync();

    protected override async ValueTask OnOpeningAsync()
    {
        await SafeInvokeVoidAsync("attach", RootRef, _tooltipRef, PlacementString, SelfRef,
            Offset, FollowCursor, Interactive);
        _attached = true;
        await SafeInvokeVoidAsync("show", RootRef, _tooltipRef, PlacementString, ZIndexValue, Offset);
    }

    protected override async ValueTask OnClosingAsync()
    {
        await SafeInvokeVoidAsync("hide", _tooltipRef);
    }

    [JSInvokable]
    public Task HideFromJsAsync() => HideAsync();

    protected override ValueTask OnDisposingAsync()
    {
        _showDebouncer.Dispose();
        _hideDebouncer.Dispose();

        if (_attached)
        {
            _ = SafeInvokeVoidAsync("detach", RootRef);
            _attached = false;
        }
        return base.OnDisposingAsync();
    }
}
