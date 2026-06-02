using System.Globalization;
using System.Text;

using SuperUI.Enums;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Components;

/// <summary>
/// Grid column for <see cref="SgRow"/>. Supports span, offset, order, flex
/// and responsive overrides for xs/sm/md/lg/xl breakpoints.
/// </summary>
public partial class SgCol : ComponentBase
{
    /// <summary>Gutter and column-count cascaded from the parent <see cref="SgRow"/>.</summary>
    [CascadingParameter] public SgRowContext? RowContext { get; set; }

    /// <summary>Legacy cascading gutter (string). Kept for backward compatibility when used outside of <see cref="SgRow"/>.</summary>
    [CascadingParameter(Name = "Gutter")] public string? LegacyGutter { get; set; }

    /// <summary>Child content rendered inside the column.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Column span. Default = total columns of the row (full width).</summary>
    [Parameter] public int? Span { get; set; }

    /// <summary>Number of columns to offset (push content right).</summary>
    [Parameter] public int Offset { get; set; }

    /// <summary>Flex order (CSS <c>order</c>).</summary>
    [Parameter] public int? Order { get; set; }

    /// <summary>Pull (move left by N columns) — implemented via negative <c>order</c>.</summary>
    [Parameter] public int Pull { get; set; }

    /// <summary>Push (move right by N columns) — implemented via positive <c>order</c>.</summary>
    [Parameter] public int Push { get; set; }

    /// <summary>If <c>true</c>, the column auto-sizes to its content (<c>flex: 0 0 auto; width: auto</c>).</summary>
    [Parameter] public bool Auto { get; set; }

    /// <summary>If <c>true</c>, the column fills remaining space (<c>flex: 1 1 0</c>).</summary>
    [Parameter] public bool Fill { get; set; }

    /// <summary>
    /// If <c>true</c>, the column uses <see cref="SgRowContext.ItemWidth"/> (or <c>200px</c> by default)
    /// as <c>min-width</c>, so the row packs as many columns as fit. Combine with
    /// <c>SgRow.ItemWidth</c> for fine control.
    /// </summary>
    [Parameter] public bool AutoFit { get; set; }

    /// <summary>Override the auto-fit basis (default: row's <c>ItemWidth</c> or <c>200px</c>).</summary>
    [Parameter] public string? Basis { get; set; }

    /// <summary>Flex grow factor. Overrides <see cref="Fill"/> when set.</summary>
    [Parameter] public int? Grow { get; set; }

    /// <summary>Optional explicit <c>flex</c> shorthand (e.g. <c>"1 1 200px"</c>). Wins over <see cref="Auto"/>/<see cref="Fill"/>/<see cref="Grow"/>.</summary>
    [Parameter] public string? Flex { get; set; }

    /// <summary>Override cross-axis alignment of THIS column (CSS <c>align-self</c>).</summary>
    [Parameter] public SgAlignItems? AlignSelf { get; set; }

    /// <summary>If <c>true</c>, the column lifts on hover with a subtle scale effect.</summary>
    [Parameter] public bool Hoverable { get; set; }

    /// <summary>If <c>true</c>, shows pointer cursor on hover.</summary>
    [Parameter] public bool Clickable { get; set; }

    /// <summary>Column span on xs breakpoint (≤575px).</summary>
    [Parameter] public int Xs { get; set; }
    /// <summary>Column span on sm breakpoint (≥576px).</summary>
    [Parameter] public int Sm { get; set; }
    /// <summary>Column span on md breakpoint (≥768px).</summary>
    [Parameter] public int Md { get; set; }
    /// <summary>Column span on lg breakpoint (≥992px).</summary>
    [Parameter] public int Lg { get; set; }
    /// <summary>Column span on xl breakpoint (≥1200px).</summary>
    [Parameter] public int Xl { get; set; }

    /// <summary>Responsive offset on xs breakpoint.</summary>
    [Parameter] public int OffsetXs { get; set; }
    /// <summary>Responsive offset on sm breakpoint (≥576px).</summary>
    [Parameter] public int OffsetSm { get; set; }
    /// <summary>Responsive offset on md breakpoint (≥768px).</summary>
    [Parameter] public int OffsetMd { get; set; }
    /// <summary>Responsive offset on lg breakpoint (≥992px).</summary>
    [Parameter] public int OffsetLg { get; set; }
    /// <summary>Responsive offset on xl breakpoint (≥1200px).</summary>
    [Parameter] public int OffsetXl { get; set; }

    /// <summary>Responsive order on xs breakpoint.</summary>
    [Parameter] public int? OrderXs { get; set; }
    /// <summary>Responsive order on sm breakpoint (≥576px).</summary>
    [Parameter] public int? OrderSm { get; set; }
    /// <summary>Responsive order on md breakpoint (≥768px).</summary>
    [Parameter] public int? OrderMd { get; set; }
    /// <summary>Responsive order on lg breakpoint (≥992px).</summary>
    [Parameter] public int? OrderLg { get; set; }
    /// <summary>Responsive order on xl breakpoint (≥1200px).</summary>
    [Parameter] public int? OrderXl { get; set; }

    /// <summary>Responsive align-self on xs breakpoint.</summary>
    [Parameter] public SgAlignItems? AlignSelfXs { get; set; }
    /// <summary>Responsive align-self on sm breakpoint (≥576px).</summary>
    [Parameter] public SgAlignItems? AlignSelfSm { get; set; }
    /// <summary>Responsive align-self on md breakpoint (≥768px).</summary>
    [Parameter] public SgAlignItems? AlignSelfMd { get; set; }
    /// <summary>Responsive align-self on lg breakpoint (≥992px).</summary>
    [Parameter] public SgAlignItems? AlignSelfLg { get; set; }
    /// <summary>Responsive align-self on xl breakpoint (≥1200px).</summary>
    [Parameter] public SgAlignItems? AlignSelfXl { get; set; }

    /// <summary>Hides the column on xs breakpoint.</summary>
    [Parameter] public bool HideXs { get; set; }
    /// <summary>Hides the column on sm breakpoint.</summary>
    [Parameter] public bool HideSm { get; set; }
    /// <summary>Hides the column on md breakpoint.</summary>
    [Parameter] public bool HideMd { get; set; }
    /// <summary>Hides the column on lg breakpoint.</summary>
    [Parameter] public bool HideLg { get; set; }
    /// <summary>Hides the column on xl breakpoint.</summary>
    [Parameter] public bool HideXl { get; set; }

    /// <summary>If <c>true</c>, hides the column on every breakpoint (a permanent "remove" toggle).</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Additional CSS classes.</summary>
    [Parameter] public string? CssClass { get; set; }

    /// <summary>Additional inline styles.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Click event on the column root.</summary>
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>Captures any unmatched HTML attributes (id, data-*, aria-*, role, etc.).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    private int Columns => RowContext?.Columns ?? 12;

    private static string Pct(double v) =>
        v.ToString("0.######", CultureInfo.InvariantCulture) + "%";

    private string ComputedClass
    {
        get
        {
            var sb = new StringBuilder("sg-col");
            var cols = Columns;
            var span = Span ?? cols;
            if (cols == 12)
            {
                if (span is >= 1 and <= 12) sb.Append(" sg-col-").Append(span);
                if (Xs is >= 1 and <= 12) sb.Append(" sg-col-xs-").Append(Xs);
                if (Sm is >= 1 and <= 12) sb.Append(" sg-col-sm-").Append(Sm);
                if (Md is >= 1 and <= 12) sb.Append(" sg-col-md-").Append(Md);
                if (Lg is >= 1 and <= 12) sb.Append(" sg-col-lg-").Append(Lg);
                if (Xl is >= 1 and <= 12) sb.Append(" sg-col-xl-").Append(Xl);
            }

            // Responsive order (only when a non-default value is set).
            if (OrderXs is not null) sb.Append(" sg-col-order-xs-").Append(OrderXs);
            if (OrderSm is not null) sb.Append(" sg-col-order-sm-").Append(OrderSm);
            if (OrderMd is not null) sb.Append(" sg-col-order-md-").Append(OrderMd);
            if (OrderLg is not null) sb.Append(" sg-col-order-lg-").Append(OrderLg);
            if (OrderXl is not null) sb.Append(" sg-col-order-xl-").Append(OrderXl);

            // Responsive align-self.
            if (AlignSelfXs is { } ax) sb.Append(" sg-col-align-xs-").Append(ax.ToString().ToLowerInvariant());
            if (AlignSelfSm is { } asx) sb.Append(" sg-col-align-sm-").Append(asx.ToString().ToLowerInvariant());
            if (AlignSelfMd is { } amd) sb.Append(" sg-col-align-md-").Append(amd.ToString().ToLowerInvariant());
            if (AlignSelfLg is { } alg) sb.Append(" sg-col-align-lg-").Append(alg.ToString().ToLowerInvariant());
            if (AlignSelfXl is { } axl) sb.Append(" sg-col-align-xl-").Append(axl.ToString().ToLowerInvariant());

            // Responsive hidden classes
            if (HideXs) sb.Append(" sg-col-xs-hidden");
            if (HideSm) sb.Append(" sg-col-sm-hidden");
            if (HideMd) sb.Append(" sg-col-md-hidden");
            if (HideLg) sb.Append(" sg-col-lg-hidden");
            if (HideXl) sb.Append(" sg-col-xl-hidden");
            if (Hoverable) sb.Append(" sg-col-hoverable");
            if (Clickable) sb.Append(" sg-col-clickable");
            if (Hidden) sb.Append(" sg-col-hidden");
            if (!string.IsNullOrWhiteSpace(CssClass)) sb.Append(' ').Append(CssClass);
            return sb.ToString();
        }
    }

    private string ComputedStyle
    {
        get
        {
            var sb = new StringBuilder();
            var cols = Columns;
            var span = Span ?? cols;

            if (Auto)
            {
                sb.Append("flex:0 0 auto;width:auto;");
            }
            else if (Fill)
            {
                sb.Append("flex:1 1 0;min-width:0;");
            }
            else if (Grow is { } g)
            {
                sb.Append("flex:").Append(g.ToString(CultureInfo.InvariantCulture)).Append(" 1 0;min-width:0;");
            }
            else if (AutoFit)
            {
                var basis = !string.IsNullOrWhiteSpace(Basis)
                    ? Basis
                    : (RowContext?.ItemWidth ?? "200px");
                sb.Append("flex:0 1 ").Append(basis).Append(";min-width:").Append(basis).Append(";");
            }
            else if (!string.IsNullOrWhiteSpace(Flex))
            {
                sb.Append("flex:").Append(Flex).Append(';');
            }
            else if (span >= 1 && span <= cols)
            {
                var pct = (double)span / cols * 100.0;
                var gutter = RowContext?.Gutter ?? RowContext?.ColumnGutter ?? LegacyGutter;

                if (!string.IsNullOrWhiteSpace(gutter) && gutter != "0" && gutter != "0px")
                {
                    var spanRatio = (double)span / cols;
                    var complement = (1.0 - spanRatio).ToString("0.######", CultureInfo.InvariantCulture);
                    sb.Append("flex:0 0 calc(")
                      .Append(Pct(pct))
                      .Append(" - (").Append(gutter).Append(" * ").Append(complement).Append("));");
                    sb.Append("max-width:calc(")
                      .Append(Pct(pct))
                      .Append(" - (").Append(gutter).Append(" * ").Append(complement).Append("));");
                }
                else
                {
                    sb.Append("flex:0 0 ").Append(Pct(pct)).Append(';');
                    sb.Append("max-width:").Append(Pct(pct)).Append(';');
                }
            }

            if (Offset > 0 && Offset < cols)
            {
                var pct = (double)Offset / cols * 100.0;
                sb.Append("margin-inline-start:").Append(Pct(pct)).Append(';');
            }

            // order: explicit Order wins, else Push - Pull
            if (Order is { } o)
            {
                sb.Append("order:").Append(o.ToString(CultureInfo.InvariantCulture)).Append(';');
            }
            else if (Push != 0 || Pull != 0)
            {
                sb.Append("order:").Append((Push - Pull).ToString(CultureInfo.InvariantCulture)).Append(';');
            }

            if (AlignSelf is { } a)
            {
                sb.Append("align-self:").Append(AlignCss(a)).Append(';');
            }

            if (!string.IsNullOrWhiteSpace(Style)) sb.Append(Style);
            return sb.ToString();
        }
    }

    private static string AlignCss(SgAlignItems a) => a switch
    {
        SgAlignItems.Start    => "flex-start",
        SgAlignItems.Center   => "center",
        SgAlignItems.End      => "flex-end",
        SgAlignItems.Baseline => "baseline",
        _                     => "stretch"
    };

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
