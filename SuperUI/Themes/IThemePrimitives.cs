namespace SuperUI.Themes;

/// <summary>
/// Primitive values of the theme — color scales, spacing, typography.
/// Not used directly in components.
/// </summary>
public interface IThemePrimitives
{
    string Neutral0 { get; }
    string Neutral50 { get; }
    string Neutral100 { get; }
    string Neutral200 { get; }
    string Neutral300 { get; }
    string Neutral400 { get; }
    string Neutral500 { get; }
    string Neutral600 { get; }
    string Neutral700 { get; }
    string Neutral800 { get; }
    string Neutral900 { get; }

    string Primary50 { get; }
    string Primary100 { get; }
    string Primary200 { get; }
    string Primary300 { get; }
    string Primary400 { get; }
    string Primary500 { get; }
    string Primary600 { get; }
    string Primary700 { get; }
    string Primary800 { get; }
    string Primary900 { get; }

    string Success50 { get; }
    string Success100 { get; }
    string Success500 { get; }
    string Success600 { get; }
    string Success700 { get; }

    string Danger50 { get; }
    string Danger100 { get; }
    string Danger500 { get; }
    string Danger600 { get; }
    string Danger700 { get; }

    string Warning50 { get; }
    string Warning100 { get; }
    string Warning500 { get; }
    string Warning600 { get; }

    string Info50 { get; }
    string Info100 { get; }
    string Info500 { get; }
    string Info600 { get; }

    string FontSans { get; }
    string FontMono { get; }
    string FontSerif { get; }
    string FontDisplay { get; }
    string FontMedical { get; }

    string RadiusNone { get; }
    string RadiusXs { get; }
    string RadiusSm { get; }
    string RadiusMd { get; }
    string RadiusLg { get; }
    string RadiusXl { get; }
    string Radius2Xl { get; }
    string RadiusFull { get; }

    // ── v2.0 Organic proportional additions ────────────────────────

    /// <summary>Fibonacci spacing scale (px). Defaults: 0/2/3/5/8/13/21/34/55/89.</summary>
    string Spacing0 { get; }
    string Spacing1 { get; }
    string Spacing2 { get; }
    string Spacing3 { get; }
    string Spacing4 { get; }
    string Spacing5 { get; }
    string Spacing6 { get; }
    string Spacing7 { get; }
    string Spacing8 { get; }

    /// <summary>Icon size scale (px). Defaults: sm=12, md=16, lg=20, xl=24, 2xl=32.</summary>
    string IconSizeSm { get; }
    string IconSizeMd { get; }
    string IconSizeLg { get; }
    string IconSizeXl { get; }
    string IconSize2Xl { get; }

    /// <summary>Border-width scale. Defaults: default=1, strong=2, accent=3.</summary>
    string BorderWidthDefault { get; }
    string BorderWidthStrong { get; }
    string BorderWidthAccent { get; }
}
