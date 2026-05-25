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

    /// <summary>Optional explicit <c>flex</c> shorthand (e.g. <c>"1 1 200px"</c>).</summary>
    [Parameter] public string? Flex { get; set; }

    /// <summary>Override cross-axis alignment of THIS column (CSS <c>align-self</c>).</summary>
    [Parameter] public SgAlignItems? AlignSelf { get; set; }

    // Responsive spans
    [Parameter] public int Xs { get; set; }
    [Parameter] public int Sm { get; set; }
    [Parameter] public int Md { get; set; }
    [Parameter] public int Lg { get; set; }
    [Parameter] public int Xl { get; set; }

    // Responsive offsets
    [Parameter] public int OffsetXs { get; set; }
    [Parameter] public int OffsetSm { get; set; }
    [Parameter] public int OffsetMd { get; set; }
    [Parameter] public int OffsetLg { get; set; }
    [Parameter] public int OffsetXl { get; set; }

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
            // Keep CSS-class hooks for the standard 1-12 grid (matches existing stylesheet).
            if (Columns == 12)
            {
                var span = Span ?? 12;
                if (span >= 1 && span <= 12) sb.Append(" sg-col-").Append(span);
                if (Xs is >= 1 and <= 12) sb.Append(" sg-col-xs-").Append(Xs);
                if (Sm is >= 1 and <= 12) sb.Append(" sg-col-sm-").Append(Sm);
                if (Md is >= 1 and <= 12) sb.Append(" sg-col-md-").Append(Md);
                if (Lg is >= 1 and <= 12) sb.Append(" sg-col-lg-").Append(Lg);
                if (Xl is >= 1 and <= 12) sb.Append(" sg-col-xl-").Append(Xl);
            }
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
            else if (!string.IsNullOrWhiteSpace(Flex))
            {
                sb.Append("flex:").Append(Flex).Append(';');
            }
            else
            {
                // Width by span. For Columns != 12 (e.g. 24) we emit calc-based width inline,
                // since the bundled stylesheet only has 1-12 helper classes.
                if (cols != 12 || (span >= 1 && span <= cols && Span is null is false))
                {
                    if (span >= 1 && span <= cols)
                    {
                        var pct = (double)span / cols * 100.0;
                        var gutter = RowContext?.Gutter ?? RowContext?.ColumnGutter ?? LegacyGutter;

                        if (!string.IsNullOrWhiteSpace(gutter) && gutter != "0" && gutter != "0px")
                        {
                            // Correct flex calculation: flex: 0 0 calc(PERCENT - GUTTER + (GUTTER * SPAN / TOTAL))
                            // But for simple "gap" usage in Row, we just need box-sizing: border-box and proper width.
                            // If Row uses 'gap', then width should be calculated considering gaps.
                            // However, the current SgRow implementation uses 'gap' on the container.
                            // To prevent overflow, we should use flex: 0 0 calc((100% - (GAPS_COUNT * GAP)) * (SPAN / TOTAL))
                            // A simpler way with 'gap' is flex: 0 0 calc((100% - (TOTAL - 1) * GAP) / TOTAL * SPAN + (SPAN - 1) * GAP)
                            // Even simpler: width: calc((100% - (TOTAL/SPAN - 1) * GAP) / (TOTAL/SPAN))
                            
                            // Let's use the most robust CSS Grid-like math for Flexbox with gaps:
                            // width = (100% - (TOTAL - 1) * GAP) / TOTAL * SPAN + (SPAN - 1) * GAP
                            // Simplified: width = PERCENT - (GAP * (1 - SPAN/TOTAL))
                            
                            var spanRatio = (double)span / cols;
                            sb.Append("flex:0 0 calc(").Append(Pct(pct)).Append(" - (").Append(gutter).Append(" * ").Append((1.0 - spanRatio).ToString("0.######", CultureInfo.InvariantCulture)).Append("));");
                            sb.Append("max-width:calc(").Append(Pct(pct)).Append(" - (").Append(gutter).Append(" * ").Append((1.0 - spanRatio).ToString("0.######", CultureInfo.InvariantCulture)).Append("));");
                        }
                        else
                        {
                            sb.Append("flex:0 0 ").Append(Pct(pct)).Append(';');
                            sb.Append("max-width:").Append(Pct(pct)).Append(';');
                        }
                    }
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
                sb.Append("align-self:").Append(a switch
                {
                    SgAlignItems.Start    => "flex-start",
                    SgAlignItems.Center   => "center",
                    SgAlignItems.End      => "flex-end",
                    SgAlignItems.Baseline => "baseline",
                    _                     => "stretch"
                }).Append(';');
            }

            if (!string.IsNullOrWhiteSpace(Style)) sb.Append(Style);
            return sb.ToString();
        }
    }

    private Task HandleClick(MouseEventArgs args) =>
        OnClick.HasDelegate ? OnClick.InvokeAsync(args) : Task.CompletedTask;
}
