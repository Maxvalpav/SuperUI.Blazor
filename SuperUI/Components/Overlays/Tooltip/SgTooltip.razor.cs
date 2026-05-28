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
/// Supports rich content, arrows, color variants, delay configuration, and all <see cref="SgPlacement"/> values.
/// </summary>
public partial class SgTooltip : SgOverlayComponentBase
{
    private ElementReference _tooltipRef;
    private bool _attached;
    private readonly SgDebouncer _showDebouncer = new();
    private readonly SgDebouncer _hideDebouncer = new();

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
    /// Supports all extended placements: TopStart, TopEnd, BottomStart, BottomEnd, etc.
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
    /// Gets or sets rich content displayed inside the tooltip.
    /// When set, overrides <see cref="Text"/> with a richer layout.
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
    /// When null, the default tooltip colors are used.
    /// </summary>
    [Parameter]
    public string? Color { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the tooltip. Example: "280px", "50vw".
    /// When null, the tooltip width adapts to content with a natural max.
    /// </summary>
    [Parameter]
    public string? MaxWidth { get; set; }

    protected override string ModulePath => "./_content/SuperUI/superui-tooltip.js";
    protected override int ZIndexBase => SgZIndexService.TooltipBase;
    protected override int ClosingAnimationMs => 0;

    private string TooltipCssClass
    {
        get
        {
            var css = CssBuilder.Default("sgc-tt")
                .AddClass(PlacementCssClass)
                .AddClass("sgc-tt-arrow", ShowArrow)
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

    private string PlacementCssClass => Placement switch
    {
        SgPlacement.Bottom => "sgc-tt-bottom",
        SgPlacement.BottomStart => "sgc-tt-bottom-start",
        SgPlacement.BottomEnd => "sgc-tt-bottom-end",
        SgPlacement.Left => "sgc-tt-left",
        SgPlacement.LeftStart => "sgc-tt-left-start",
        SgPlacement.LeftEnd => "sgc-tt-left-end",
        SgPlacement.Right => "sgc-tt-right",
        SgPlacement.RightStart => "sgc-tt-right-start",
        SgPlacement.RightEnd => "sgc-tt-right-end",
        SgPlacement.TopStart => "sgc-tt-top-start",
        SgPlacement.TopEnd => "sgc-tt-top-end",
        _ => "sgc-tt-top"
    };

    private string PlacementString => Placement switch
    {
        SgPlacement.Bottom => "bottom",
        SgPlacement.BottomStart => "bottom-start",
        SgPlacement.BottomEnd => "bottom-end",
        SgPlacement.Left => "left",
        SgPlacement.LeftStart => "left-start",
        SgPlacement.LeftEnd => "left-end",
        SgPlacement.Right => "right",
        SgPlacement.RightStart => "right-start",
        SgPlacement.RightEnd => "right-end",
        SgPlacement.TopStart => "top-start",
        SgPlacement.TopEnd => "top-end",
        _ => "top"
    };

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

    private Task SetVisibleFalseAsync() =>
        CloseAsync();

    protected override async ValueTask OnOpeningAsync()
    {
        await SafeInvokeVoidAsync("attach", RootRef, _tooltipRef, PlacementString, SelfRef);
        _attached = true;
        await SafeInvokeVoidAsync("show", RootRef, _tooltipRef, PlacementString, ZIndexValue);
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
