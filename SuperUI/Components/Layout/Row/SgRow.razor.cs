using System.Globalization;
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
    public SgDensity Density { get; init; } = SgDensity.Default;
    public string? ItemWidth { get; init; }
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

    /// <summary>
    /// Density preset affecting gutter scale. <c>Compact</c> = 0.5×, <c>Default</c> = 1×,
    /// <c>Comfortable</c> = 1.5×. Applied to <see cref="Gutter"/>/<see cref="RowGutter"/>/<see cref="ColumnGutter"/>.
    /// </summary>
    [Parameter] public SgDensity Density { get; set; } = SgDensity.Default;

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

    /// <summary>Responsive direction for the xs breakpoint.</summary>
    [Parameter] public SgFlexDirection? DirectionXs { get; set; }
    /// <summary>Responsive direction for the sm breakpoint (≥576px).</summary>
    [Parameter] public SgFlexDirection? DirectionSm { get; set; }
    /// <summary>Responsive direction for the md breakpoint (≥768px).</summary>
    [Parameter] public SgFlexDirection? DirectionMd { get; set; }
    /// <summary>Responsive direction for the lg breakpoint (≥992px).</summary>
    [Parameter] public SgFlexDirection? DirectionLg { get; set; }
    /// <summary>Responsive direction for the xl breakpoint (≥1200px).</summary>
    [Parameter] public SgFlexDirection? DirectionXl { get; set; }

    /// <summary>Render as inline-flex instead of block flex.</summary>
    [Parameter] public bool Inline { get; set; }

    /// <summary>Stretch the row to 100% width of its parent. Default <c>true</c>.</summary>
    [Parameter] public bool FullWidth { get; set; } = true;

    /// <summary>Stretch the row to 100% height of its parent. Default <c>false</c>.</summary>
    [Parameter] public bool FullHeight { get; set; }

    /// <summary>Optional CSS min-height for the row.</summary>
    [Parameter] public string? MinHeight { get; set; }

    /// <summary>Optional background color / CSS value.</summary>
    [Parameter] public string? Background { get; set; }

    /// <summary>Optional CSS padding (e.g. "12px" or "var(--sg-spacing-2)").</summary>
    [Parameter] public string? Padding { get; set; }

    /// <summary>Optional CSS border-radius (e.g. "8px" or "var(--sg-radius-md)").</summary>
    [Parameter] public string? BorderRadius { get; set; }

    /// <summary>If <c>true</c>, the row lifts on hover with a subtle shadow transition.</summary>
    [Parameter] public bool Hoverable { get; set; }

    /// <summary>If <c>true</c>, shows pointer cursor on hover.</summary>
    [Parameter] public bool Clickable { get; set; }

    /// <summary>
    /// Fixed (or <c>minmax(...)</c>) width of each column when used in auto-fit mode.
    /// Example: <c>"240px"</c> or <c>"minmax(200px, 1fr)"</c>. When set, the row
    /// packs as many columns as fit and stretches each to <c>ItemWidth</c>;
    /// child <see cref="SgCol"/>s may use <c>AutoFit="true"</c> for matching behaviour.
    /// </summary>
    [Parameter] public string? ItemWidth { get; set; }

    /// <summary>HTML tag to render. Default <see cref="SgRowTag.Div"/>.</summary>
    [Parameter] public SgRowTag Tag { get; set; } = SgRowTag.Div;

    /// <summary>
    /// Legacy: HTML tag to render as a string.
    /// Accepted values: <c>div, section, header, footer, nav, main, ul, article, aside</c>.
    /// Kept for backward compatibility; new code should use <see cref="Tag"/>.
    /// </summary>
    [Obsolete("Use the strongly-typed Tag parameter (SgRowTag) instead.")]
    [Parameter] public string? TagName { get; set; }

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
        // Honour the obsolete string-based Tag parameter only if the user set it
        // (and the new enum Tag wasn't explicitly provided).
        if (AdditionalAttributes is null) { /* no-op */ }
        _context = new SgRowContext
        {
            Gutter = ResolvedGutter,
            RowGutter = RowGutter,
            ColumnGutter = ColumnGutter,
            Columns = Columns <= 0 ? 12 : Columns,
            Density = Density,
            ItemWidth = ItemWidth
        };
    }

    private SgRowTag ResolvedTag
    {
        get
        {
#pragma warning disable CS0618
            if (TagName is not null && Enum.TryParse<SgRowTag>(TagName, true, out var legacy) && Tag == SgRowTag.Div)
            {
                return legacy;
            }
#pragma warning restore CS0618
            return Tag;
        }
    }

    private string DensityKey => Density switch
    {
        SgDensity.Compact      => "compact",
        SgDensity.Comfortable  => "comfortable",
        _                      => "default"
    };

    private static double DensityScale(SgDensity d) => d switch
    {
        SgDensity.Compact     => 0.5,
        SgDensity.Comfortable => 1.5,
        _                     => 1.0
    };

    private string? ResolvedGutter => Space switch
    {
        SgSize.Sm     => "var(--sg-space-4)",
        SgSize.Md     => "var(--sg-space-8)",
        SgSize.Lg     => "var(--sg-space-16)",
        SgSize.Xl     => "var(--sg-space-24)",
        SgSize.FibMd  => "var(--sg-space-fib-4)",
        SgSize.FibLg  => "var(--sg-space-fib-5)",
        SgSize.FibXl  => "var(--sg-space-fib-6)",
        SgSize.FibXxl => "var(--sg-space-fib-7)",
        _ => Gutter
    };

    private string ScaleGap(string? raw, double scale)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        // If raw is a plain number → treat as px.
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var n))
        {
            return FormattableString.Invariant($"{(n * scale).ToString("0.######", CultureInfo.InvariantCulture)}px");
        }
        // For CSS values, wrap in calc() to keep the scaling dynamic.
        return FormattableString.Invariant($"calc({raw} * {scale.ToString("0.###", CultureInfo.InvariantCulture)})");
    }

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
            if (ResolvedTag == SgRowTag.Ul) sb.Append(" sg-row-list");
            if (Inline) sb.Append(" sg-row-inline");
            if (Hoverable) sb.Append(" sg-row-hoverable");
            if (Clickable) sb.Append(" sg-row-clickable");
            if (AdditionalAttributes is not null
                && AdditionalAttributes.TryGetValue("class", out var c)
                && c is not null)
            {
                sb.Append(' ').Append(c);
            }
            if (!string.IsNullOrWhiteSpace(CssClass))
                sb.Append(' ').Append(CssClass);
            return sb.ToString();
        }
    }

    private string ComputedStyle
    {
        get
        {
            var sb = new StringBuilder();
            var scale = DensityScale(Density);

            sb.Append("display:").Append(Inline ? "inline-flex" : "flex").Append(';');
            sb.Append("flex-wrap:").Append(WrapCss).Append(';');
            sb.Append("flex-direction:").Append(DirectionCss).Append(';');
            sb.Append("align-items:").Append(AlignCss).Append(';');
            sb.Append("justify-content:").Append(JustifyCss).Append(';');
            if (FullWidth && !Inline) sb.Append("width:100%;");
            if (FullHeight && !Inline) sb.Append("height:100%;");

            if (!string.IsNullOrWhiteSpace(MinHeight)) sb.Append("min-height:").Append(MinHeight).Append(';');
            if (!string.IsNullOrWhiteSpace(Background)) sb.Append("background:").Append(Background).Append(';');
            if (!string.IsNullOrWhiteSpace(Padding)) sb.Append("padding:").Append(Padding).Append(';');
            if (!string.IsNullOrWhiteSpace(BorderRadius)) sb.Append("border-radius:").Append(BorderRadius).Append(';');

            // Reset list defaults if rendered as <ul>
            if (ResolvedTag == SgRowTag.Ul)
            {
                sb.Append("list-style:none;margin:0;padding:0;");
            }

            var hasAxisGutter = !string.IsNullOrWhiteSpace(RowGutter) || !string.IsNullOrWhiteSpace(ColumnGutter);
            if (hasAxisGutter)
            {
                var colGap = ScaleGap(ColumnGutter ?? ResolvedGutter, scale);
                sb.Append("row-gap:").Append(ScaleGap(RowGutter ?? ResolvedGutter, scale)).Append(';');
                sb.Append("column-gap:").Append(colGap).Append(';');
                sb.Append("--sg-gutter:").Append(colGap).Append(';');
            }
            else if (!string.IsNullOrWhiteSpace(ResolvedGutter))
            {
                var gap = ScaleGap(ResolvedGutter, scale);
                sb.Append("gap:").Append(gap).Append(';');
                // Responsive .sg-col-{bp}-{n} classes subtract this from their
                // percentage width so N columns + (N-1) gaps still fit one row.
                sb.Append("--sg-gutter:").Append(gap).Append(';');
            }

            if (!string.IsNullOrWhiteSpace(Style)) sb.Append(Style);
            return sb.ToString();
        }
    }

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
