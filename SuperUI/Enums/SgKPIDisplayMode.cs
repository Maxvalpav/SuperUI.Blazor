namespace SuperUI.Enums;

/// <summary>Visual layout mode for <c>SgKPICard</c>.</summary>
public enum SgKPIDisplayMode
{
    /// <summary>Full card with gradient background and wave chart (default).</summary>
    Card,

    /// <summary>Circular ring with value inside — compact radial indicator.</summary>
    Ring,

    /// <summary>Horizontal row: label · value · dot-sparkline. Ideal for dense tables.</summary>
    Inline,

    /// <summary>
    /// BAN (Big Ass Number) — title top-left, huge value center, inline trend badge,
    /// optional secondary delta metrics row at the bottom. Inspired by Power BI BAN cards.
    /// </summary>
    Ban,

    /// <summary>
    /// Analytic card — icon + title header, sparkline fills the upper area,
    /// value + multiple comparison deltas at the bottom. Inspired by Tableau BAN templates.
    /// </summary>
    Analytic
}
