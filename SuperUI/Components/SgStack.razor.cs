using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Components;

/// <summary>
/// Linear flex layout container that arranges its children either horizontally or vertically
/// with full control over gap, alignment, wrapping, sizing, padding and flex-grow/shrink.
/// </summary>
public partial class SgStack : ComponentBase
{
    /// <summary>Child content to render inside the stack.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Stack orientation. Default is <see cref="SgOrientation.Horizontal"/>.</summary>
    [Parameter] public SgOrientation Orientation { get; set; } = SgOrientation.Horizontal;

    /// <summary>If <c>true</c>, items are laid out in reverse order along the main axis.</summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>Gap between items (any CSS length). Default <c>"8px"</c>.</summary>
    [Parameter] public string Gap { get; set; } = "8px";

    /// <summary>Optional row-gap override (used when wrap is enabled or with grid-like layouts).</summary>
    [Parameter] public string? RowGap { get; set; }

    /// <summary>Optional column-gap override.</summary>
    [Parameter] public string? ColumnGap { get; set; }

    /// <summary>Alias for <see cref="Gap"/>.</summary>
    [Parameter] public string Spacing { get => Gap; set => Gap = value; }

    /// <summary>Cross-axis alignment of items. Default <see cref="SgAlignItems.Stretch"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Stretch;

    /// <summary>Main-axis distribution. Default <see cref="SgJustifyContent.Start"/>.</summary>
    [Parameter] public SgJustifyContent Justify { get; set; } = SgJustifyContent.Start;

    /// <summary>If <c>true</c>, items wrap to next line/column when space is insufficient.</summary>
    [Parameter] public bool Wrap { get; set; }

    /// <summary>Render as inline-flex instead of flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>If <c>true</c>, the stack stretches to fill its parent's main axis (flex: 1).</summary>
    [Parameter] public bool Grow { get; set; }

    /// <summary>If set, controls flex-grow factor explicitly. Overrides <see cref="Grow"/>.</summary>
    [Parameter] public int? FlexGrow { get; set; }

    /// <summary>If set, controls flex-shrink factor explicitly.</summary>
    [Parameter] public int? FlexShrink { get; set; }

    /// <summary>Optional CSS padding.</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Optional CSS margin.</summary>
    [Parameter] public string? Margin { get; set; }

    /// <summary>Optional CSS width.</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Optional CSS height.</summary>
    [Parameter] public string? Height { get; set; }

    /// <summary>Optional CSS min-width.</summary>
    [Parameter] public string? MinWidth { get; set; }

    /// <summary>Optional CSS min-height.</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Optional CSS max-width.</summary>
    [Parameter] public string? MaxWidth { get; set; }

    /// <summary>Optional CSS max-height.</summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>Optional background color.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>If <c>true</c>, the stack fills the full width of its parent.</summary>
    [Parameter] public bool FullWidth { get; set; }

    /// <summary>If <c>true</c>, the stack fills the full height of its parent.</summary>
    [Parameter] public bool FullHeight { get; set; }

    /// <summary>Additional CSS classes appended to the root element.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Additional inline styles appended to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Click event on the stack root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Captures any unmatched HTML attributes (id, data-*, aria-*, role, etc.).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private string AlignCss => Align switch
    {
        SgAlignItems.Start    => "flex-start",
        SgAlignItems.Center   => "center",
        SgAlignItems.End      => "flex-end",
        SgAlignItems.Baseline => "baseline",
        _                     => "stretch"
    };

    private string JustifyCss => Justify switch
    {
        SgJustifyContent.Center       => "center",
        SgJustifyContent.End          => "flex-end",
        SgJustifyContent.SpaceBetween => "space-between",
        SgJustifyContent.SpaceAround  => "space-around",
        SgJustifyContent.SpaceEvenly  => "space-evenly",
        _                             => "flex-start"
    };

    private string FlexDirectionCss
    {
        get
        {
            var dir = Orientation == SgOrientation.Vertical ? "column" : "row";
            return Reverse ? dir + "-reverse" : dir;
        }
    }

    private string ComputedClass
    {
        get
        {
            var sb = new StringBuilder("sg-stack");
            if (Inline) sb.Append(" sg-stack-inline");
            if (Wrap) sb.Append(" sg-stack-wrap");
            if (Orientation == SgOrientation.Vertical) sb.Append(" sg-stack-vertical");
            else sb.Append(" sg-stack-horizontal");
            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                sb.Append(' ').Append(CssClass);
            }
            return sb.ToString();
        }
    }

    private string FixUnit(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "0px";
        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
        {
            return value + "px";
        }
        return value;
    }

    private string ComputedStyle
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("display:").Append(Inline ? "inline-flex" : "flex").Append(';');
            sb.Append("flex-direction:").Append(FlexDirectionCss).Append(';');

            var gapVal = FixUnit(Gap);
            var rowGapVal = !string.IsNullOrWhiteSpace(RowGap) ? FixUnit(RowGap) : gapVal;
            var colGapVal = !string.IsNullOrWhiteSpace(ColumnGap) ? FixUnit(ColumnGap) : gapVal;

            if (rowGapVal != "0px" || colGapVal != "0px")
            {
                sb.Append("row-gap:").Append(rowGapVal).Append(';');
                sb.Append("column-gap:").Append(colGapVal).Append(';');
            }

            sb.Append("align-items:").Append(AlignCss).Append(';');
            sb.Append("justify-content:").Append(JustifyCss).Append(';');
            sb.Append("flex-wrap:").Append(Wrap ? "wrap" : "nowrap").Append(';');

            if (FlexGrow is { } g) sb.Append("flex-grow:").Append(g.ToString(CultureInfo.InvariantCulture)).Append(';');
            else if (Grow) sb.Append("flex:1 1 auto;");
            if (FlexShrink is { } s) sb.Append("flex-shrink:").Append(s.ToString(CultureInfo.InvariantCulture)).Append(';');

            if (FullWidth) sb.Append("width:100%;");
            else if (!string.IsNullOrWhiteSpace(Width)) sb.Append("width:").Append(Width).Append(';');

            if (FullHeight) sb.Append("height:100%;");
            else if (!string.IsNullOrWhiteSpace(Height)) sb.Append("height:").Append(Height).Append(';');

            if (!string.IsNullOrWhiteSpace(MinWidth))  sb.Append("min-width:").Append(MinWidth).Append(';');
            if (!string.IsNullOrWhiteSpace(MinHeight)) sb.Append("min-height:").Append(MinHeight).Append(';');
            if (!string.IsNullOrWhiteSpace(MaxWidth))  sb.Append("max-width:").Append(MaxWidth).Append(';');
            if (!string.IsNullOrWhiteSpace(MaxHeight)) sb.Append("max-height:").Append(MaxHeight).Append(';');

            if (!string.IsNullOrWhiteSpace(Padding))    sb.Append("padding:").Append(Padding).Append(';');
            if (!string.IsNullOrWhiteSpace(Margin))     sb.Append("margin:").Append(Margin).Append(';');
            if (!string.IsNullOrWhiteSpace(Background)) sb.Append("background:").Append(Background).Append(';');

            if (!string.IsNullOrWhiteSpace(Style)) sb.Append(Style);
            return sb.ToString();
        }
    }

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
