namespace SuperUI.Themes;

/// <summary>
/// Apex — steel blue flagship theme. Laws of UX applied for maximum efficiency.
/// Cool off-white surfaces, higher chroma primary, Fitts-friendly touch targets.
/// </summary>
public sealed class ApexTheme : ThemeBase
{
    public override string Id => "apex";
    public override string Name => "Apex";
    public override string? Description => "Steel blue flagship theme. Laws of UX applied for maximum efficiency. Larger touch targets per Fitts.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Flagship";

    protected override IThemePrimitives CreatePrimitives() => new ApexPrimitives();
    protected override IThemeSemantic CreateLight() => new ApexSemanticLight();
    protected override IThemeSemantic? CreateDark() => new ApexSemanticDark();
    protected override IThemeComponents? CreateComponents() => new ApexComponents();
    protected override IThemeTypography? CreateTypography() => new ApexTypography();

    public override string? AdditionalCss => $$"""
        :root,
        [data-theme-id="apex"] {
            --sui-bg-primary:   var(--sg-bg);
            --sui-bg-secondary: var(--sg-bg-subtle);
            --sui-bg-tertiary:  var(--sg-bg-muted);
            --sui-text-primary:   var(--sg-fg);
            --sui-text-secondary: var(--sg-fg-subtle);
            --sui-text-muted:     var(--sg-fg-muted);
            --sui-text-disabled:  var(--sg-fg-disabled);
            --sui-border:       var(--sg-border);
            --sui-divider:      var(--sg-divider);
            --sui-border-hover: var(--sg-border-strong);
            --sui-border-focus: var(--sg-border-focus);
            --sui-accent:        var(--sg-color-primary);
            --sui-primary:       var(--sg-color-primary);
            --sui-accent-hover:  var(--sg-color-primary-hover);
            --sui-accent-active: var(--sg-color-primary-active);
            --sui-success:        var(--sg-color-success);
            --sui-success-bg:     var(--sg-color-success-subtle);
            --sui-danger:        var(--sg-color-danger);
            --sui-danger-bg:     var(--sg-color-danger-subtle);
            --sui-warn:        var(--sg-color-warning);
            --sui-warn-bg:     var(--sg-color-warning-subtle);
            --sui-info:        var(--sg-color-info);
            --sui-info-bg:     var(--sg-color-info-subtle);
            --sui-shadow-sm: var(--sg-shadow-sm);
            --sui-shadow-md: var(--sg-shadow-md);
            --sui-shadow-lg: var(--sg-shadow-lg);
            --sui-overlay-bg: var(--sg-bg-overlay);
            --sui-glass-bg:     var(--sg-bg-glass);
            --sui-glass-border: var(--sg-border-glass);
            --sui-glass-blur:   var(--sg-blur-glass);
            --sui-hover-bg:    rgba(15, 23, 42, 0.04);
            --sui-active-bg:   rgba(15, 23, 42, 0.08);
            --sui-selected-bg: var(--sg-color-primary-muted);
            --sui-font-family:    var(--sg-font);
            --sui-font-size-xs:   var(--sg-text-xs);
            --sui-font-size-sm:   var(--sg-text-sm);
            --sui-font-size-base: var(--sg-text-base);
            --sui-font-size-lg:   var(--sg-text-lg);
            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="apex"] *,
            [data-theme-id="apex"] *::before,
            [data-theme-id="apex"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class ApexPrimitives : IThemePrimitives
{
    private const double Hue = 240;

    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => $"oklch(0.985 0.003 {Hue})";
    public virtual string Neutral100 => $"oklch(0.97 0.005 {Hue})";
    public virtual string Neutral200 => $"oklch(0.93 0.008 {Hue})";
    public virtual string Neutral300 => $"oklch(0.87 0.01 {Hue})";
    public virtual string Neutral400 => $"oklch(0.76 0.012 {Hue})";
    public virtual string Neutral500 => $"oklch(0.64 0.012 {Hue})";
    public virtual string Neutral600 => $"oklch(0.52 0.014 {Hue})";
    public virtual string Neutral700 => $"oklch(0.40 0.016 {Hue})";
    public virtual string Neutral800 => $"oklch(0.28 0.018 {Hue})";
    public virtual string Neutral900 => $"oklch(0.16 0.02 {Hue})";

    public virtual string Primary50  => $"oklch(0.95 0.035 {Hue})";
    public virtual string Primary100 => $"oklch(0.89 0.07 {Hue})";
    public virtual string Primary200 => $"oklch(0.83 0.10 {Hue})";
    public virtual string Primary300 => $"oklch(0.75 0.13 {Hue})";
    public virtual string Primary400 => $"oklch(0.66 0.16 {Hue})";
    public virtual string Primary500 => $"oklch(0.58 0.16 {Hue})";
    public virtual string Primary600 => $"oklch(0.51 0.15 {Hue})";
    public virtual string Primary700 => $"oklch(0.43 0.14 {Hue})";
    public virtual string Primary800 => $"oklch(0.34 0.13 {Hue})";
    public virtual string Primary900 => $"oklch(0.25 0.11 {Hue})";

    public virtual string Success50  => "oklch(0.95 0.035 153)";
    public virtual string Success100 => "oklch(0.88 0.07 153)";
    public virtual string Success500 => "oklch(0.58 0.16 153)";
    public virtual string Success600 => "oklch(0.50 0.16 153)";
    public virtual string Success700 => "oklch(0.42 0.15 153)";

    public virtual string Danger50  => "oklch(0.95 0.04 19)";
    public virtual string Danger100 => "oklch(0.88 0.09 19)";
    public virtual string Danger500 => "oklch(0.55 0.20 19)";
    public virtual string Danger600 => "oklch(0.48 0.20 19)";
    public virtual string Danger700 => "oklch(0.40 0.19 19)";

    public virtual string Warning50  => "oklch(0.97 0.04 83)";
    public virtual string Warning100 => "oklch(0.92 0.08 83)";
    public virtual string Warning500 => "oklch(0.70 0.18 83)";
    public virtual string Warning600 => "oklch(0.62 0.18 83)";

    public virtual string Info50  => "oklch(0.95 0.03 254)";
    public virtual string Info100 => "oklch(0.88 0.06 254)";
    public virtual string Info500 => "oklch(0.58 0.15 254)";
    public virtual string Info600 => "oklch(0.50 0.15 254)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "4px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "10px";
    public virtual string RadiusLg   => "16px";
    public virtual string RadiusXl   => "24px";
    public virtual string Radius2Xl  => "38px";
    public virtual string RadiusFull => "9999px";
}

internal class ApexSemanticLight : BaseLightConsistent
{
    public ApexSemanticLight() : base(240) { }

    public override string BgDefault     => "oklch(0.985 0.005 240)";
    public override string BgSubtle      => "oklch(0.96 0.008 240)";
    public override string BgMuted       => "oklch(0.93 0.012 240)";
    public override string BgEmphasized  => "oklch(0.88 0.016 240)";
    public override string BgOverlay     => "oklch(0.16 0.02 240 / 0.40)";
    public override string BgGlass       => "oklch(0.985 0.005 240 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 240 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.02 240)";
    public override string FgSubtle    => "oklch(0.36 0.015 240)";
    public override string FgMuted     => "oklch(0.52 0.012 240)";
    public override string FgDisabled  => "oklch(0.68 0.008 240)";
    public override string FgInverse   => "oklch(0.985 0.005 240)";
    public override string FgLink      => "oklch(0.55 0.24 240)";
    public override string FgLinkHover => "oklch(0.49 0.24 240)";

    public override string BorderDefault => "oklch(0.87 0.012 240)";
    public override string BorderSubtle  => "oklch(0.93 0.01 240)";
    public override string BorderStrong  => "oklch(0.80 0.015 240)";
    public override string BorderFocus   => "oklch(0.55 0.24 240)";
    public override string Divider       => "oklch(0.93 0.01 240)";

    public override string ColorPrimary        => "oklch(0.55 0.24 240)";
    public override string ColorPrimaryHover   => "oklch(0.49 0.24 240)";
    public override string ColorPrimaryActive  => "oklch(0.43 0.23 240)";
    public override string ColorPrimarySubtle  => "oklch(0.93 0.05 240)";
    public override string ColorPrimaryMuted   => "oklch(0.84 0.10 240)";

    public override string RadiusSm   => "6px";
    public override string RadiusMd   => "10px";
    public override string RadiusLg   => "16px";
    public override string RadiusXl   => "24px";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public override string FontMono => "'JetBrains Mono', ui-monospace, monospace";

    public override string FocusRing       => "0 0 0 2px oklch(0.985 0.005 240), 0 0 0 4px oklch(0.55 0.24 240)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.985 0.005 240), 0 0 0 4px oklch(0.55 0.20 19)";
}

internal class ApexSemanticDark : BaseDarkConsistent
{
    public ApexSemanticDark() : base(240) { }

    public override string ColorPrimary        => "oklch(0.65 0.24 240)";
    public override string ColorPrimarySubtle  => "oklch(0.20 0.06 240)";
    public override string ColorPrimaryMuted   => "oklch(0.28 0.10 240)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.22 240)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.24 240)";
    public override string ColorPrimaryFg      => "oklch(0.10 0.012 240)";

    public override string ColorSuccess        => "oklch(0.60 0.16 153)";
    public override string ColorSuccessSubtle  => "oklch(0.18 0.04 153)";
    public override string ColorSuccessHover   => "oklch(0.66 0.16 153)";
    public override string ColorSuccessFg      => "oklch(0.95 0.005 240)";

    public override string ColorDanger         => "oklch(0.58 0.20 19)";
    public override string ColorDangerSubtle   => "oklch(0.20 0.06 19)";
    public override string ColorDangerHover    => "oklch(0.64 0.20 19)";
    public override string ColorDangerFg       => "oklch(0.95 0.005 240)";

    public override string ColorWarning        => "oklch(0.72 0.18 83)";
    public override string ColorWarningSubtle  => "oklch(0.22 0.05 83)";
    public override string ColorWarningHover   => "oklch(0.78 0.16 83)";
    public override string ColorWarningFg      => "oklch(0.10 0.012 240)";

    public override string ColorInfo           => "oklch(0.60 0.15 254)";
    public override string ColorInfoSubtle     => "oklch(0.20 0.04 254)";
    public override string ColorInfoHover      => "oklch(0.66 0.14 254)";
    public override string ColorInfoFg         => "oklch(0.95 0.005 240)";

    public override string RadiusSm   => "6px";
    public override string RadiusMd   => "10px";
    public override string RadiusLg   => "16px";
    public override string RadiusXl   => "24px";
}

internal class ApexComponents : BaseComponents
{
    public override string BtnHeight   => "36px";
    public override string BtnHeightLg => "42px";
    public override string InputHeight   => "36px";
    public override string InputHeightLg => "42px";
}

internal sealed class ApexTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono:wght@400;500;600&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Inter', sans-serif";
    public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.1", "-0.02em");
    public HeadingSettings H2 => new("2rem", HeadingFont, "700", "1.15", "-0.015em");
    public HeadingSettings H3 => new("1.625rem", HeadingFont, "600", "1.2", "-0.01em");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.25", "0");
    public HeadingSettings H5 => new("1rem", HeadingFont, "500", "1.3", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
}
