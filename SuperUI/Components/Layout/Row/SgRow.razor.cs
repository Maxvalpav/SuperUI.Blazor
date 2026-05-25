using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Cascading context shared from <see cref="SgRow"/> to its <see cref="SgCol"/> children.
/// Carries gutter and total column-count so columns can compute responsive widths.
/// </summary>
public sealed class SgRowContext
{
    public string? Gutter { get; init; }
    public string? RowGutter { get; init; }
    public string? ColumnGutter { get; init; }
    public int Columns { get; init; } = 12;
}

/// <summary>
/// Flexbox-based grid row. Holds <see cref="SgCol"/> children and exposes alignment,
/// wrapping, justification, direction and a configurable column-count (12 or 24).
/// </summary>
public partial class SgRow : ComponentBase
{
    private SgRowContext _context = new();

    /// <summary>Child content (typically <see cref="SgCol"/> elements).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Gutter between columns (any CSS length). Default <c>"16px"</c>. Used as both row and column gap unless overridden.</summary>
    [Parameter] public string? Gutter { get; set; } = "16px";

    /// <summary>Optional vertical gutter (between wrapped rows).</summary>
    [Parameter] public string? RowGutter { get; set; }

    /// <summary>Optional horizontal gutter (between columns).</summary>
    [Parameter] public string? ColumnGutter { get; set; }

    /// <summary>Total columns in the grid. Default 12. Use 24 for finer control.</summary>
    [Parameter] public int Columns { get; set; } = 12;

    /// <summary>Gutter size from theme. If set, overrides <see cref="Gutter"/>.</summary>
    [Parameter] public SgSize? Space { get; set; }

    /// <summary>Cross-axis alignment. Default <see cref="SgAlignItems.Stretch"/>.</summary>
    [Parameter] public SgAlignItems Align { get; set; } = SgAlignItems.Stretch;

    /// <summary>Main-axis distribution. Default <see cref="SgJustifyContent.Start"/>.</summary>
    [Parameter] public SgJustifyContent Justify { get; set; } = SgJustifyContent.Start;

    /// <summary>Wrap behaviour. Wins over <see cref="NoWrap"/> when set explicitly.</summary>
    [Parameter] public SgFlexWrap? Wrap { get; set; }

    /// <summary>Convenience flag — equivalent to <see cref="Wrap"/> = <see cref="SgFlexWrap.NoWrap"/>.</summary>
    [Parameter] public bool NoWrap { get; set; }

    /// <summary>If <c>true</c>, columns are laid out in reverse order.</summary>
    [Parameter] public bool Reverse { get; set; }

    /// <summary>Main-axis direction. Wins over <see cref="Reverse"/> when set explicitly.</summary>
    [Parameter] public SgFlexDirection? Direction { get; set; }

    /// <summary>Render as inline-flex instead of block flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>Stretch the row to 100% width of its parent. Default <c>true</c>.</summary>
    [Parameter] public bool FullWidth { get; set; } = true;

    /// <summary>Optional CSS min-height for the row.</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Optional background color / CSS value.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>HTML tag to render. Default <c>div</c>. Allowed: <c>div, section, header, footer, nav, main, ul</c>.</summary>
    [Parameter] public string Tag { get; set; } = "div";

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Additional inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Click event on the row root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Captures any unmatched HTML attributes (id, data-*, aria-*, role, etc.).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    protected override void OnParametersSet()
    {
        _context = new SgRowContext
        {
            Gutter = ResolvedGutter,
            RowGutter = RowGutter,
            ColumnGutter = ColumnGutter,
            Columns = Columns <= 0 ? 12 : Columns
        };
    }

    private string? ResolvedGutter => Space switch
    {
        SgSize.Sm => "var(--sg-spacing-2)",
        SgSize.Md => "var(--sg-spacing-4)",
        SgSize.Lg => "var(--sg-spacing-8)",
        SgSize.Xl => "var(--sg-spacing-12)",
        _ => Gutter
    };

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

    private string WrapCss
    {
        get
        {
            if (Wrap is { } w)
            {
                return w switch
                {
                    SgFlexWrap.NoWrap      => "nowrap",
                    SgFlexWrap.WrapReverse => "wrap-reverse",
                    _                      => "wrap"
                };
            }
            return NoWrap ? "nowrap" : "wrap";
        }
    }

    private string DirectionCss
    {
        get
        {
            if (Direction is { } d)
            {
                return d switch
                {
                    SgFlexDirection.RowReverse    => "row-reverse",
                    SgFlexDirection.Column        => "column",
                    SgFlexDirection.ColumnReverse => "column-reverse",
                    _                             => "row"
                };
            }
            return Reverse ? "row-reverse" : "row";
        }
    }

    private string ComputedClass
    {
        get
        {
            var sb = new StringBuilder("sg-row");
            if (string.Equals(Tag, "ul", StringComparison.OrdinalIgnoreCase)) sb.Append(" sg-row-list");
            if (Inline) sb.Append(" sg-row-inline");
            if (!string.IsNullOrWhiteSpace(CssClass))
            {
                sb.Append(' ').Append(CssClass);
            }
            return sb.ToString();
        }
    }

    private string ComputedStyle
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append("display:").Append(Inline ? "inline-flex" : "flex").Append(';');
            sb.Append("flex-wrap:").Append(WrapCss).Append(';');
            sb.Append("flex-direction:").Append(DirectionCss).Append(';');
            sb.Append("align-items:").Append(AlignCss).Append(';');
            sb.Append("justify-content:").Append(JustifyCss).Append(';');
            if (FullWidth && !Inline) sb.Append("width:100%;");

            if (!string.IsNullOrWhiteSpace(MinHeight)) sb.Append("min-height:").Append(MinHeight).Append(';');
            if (!string.IsNullOrWhiteSpace(Background)) sb.Append("background:").Append(Background).Append(';');

            // Reset list defaults if rendered as <ul>
            if (string.Equals(Tag, "ul", StringComparison.OrdinalIgnoreCase))
            {
                sb.Append("list-style:none;margin:0;padding:0;");
            }

            var hasAxisGutter = !string.IsNullOrWhiteSpace(RowGutter) || !string.IsNullOrWhiteSpace(ColumnGutter);
            if (hasAxisGutter)
            {
                sb.Append("row-gap:").Append(RowGutter ?? ResolvedGutter ?? "0").Append(';');
                sb.Append("column-gap:").Append(ColumnGutter ?? ResolvedGutter ?? "0").Append(';');
            }
            else if (!string.IsNullOrWhiteSpace(ResolvedGutter))
            {
                sb.Append("gap:").Append(ResolvedGutter).Append(';');
            }

            if (!string.IsNullOrWhiteSpace(Style)) sb.Append(Style);
            return sb.ToString();
        }
    }

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
