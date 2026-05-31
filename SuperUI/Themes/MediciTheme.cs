namespace SuperUI.Themes;

/// <summary>
/// Medici — medical EBD theme. Clinical blue, maximum readability.
/// Atkinson Hyperlegible typeface for body, larger text, relaxed line height.
/// </summary>
public sealed class MediciTheme : ThemeBase
{
    public override string Id => "medici";
    public override string Name => "Medici";
    public override string? Description => "Medical EBD clinical blue theme. Maximum readability with Atkinson Hyperlegible font. WCAG AAA optimized.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Precision";

    protected override IThemePrimitives CreatePrimitives() => new MediciPrimitives();
    protected override IThemeSemantic CreateLight() => new MediciSemanticLight();
    protected override IThemeSemantic? CreateDark() => new MediciSemanticDark();
    protected override IThemeComponents? CreateComponents() => new MediciComponents();
    protected override IThemeTypography? CreateTypography() => new MediciTypography();

    public override string? AdditionalCss => $$"""
        :root,
        [data-theme-id="medici"] {
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

        [data-theme-id="medici"] {
            font-size-adjust: 0.5;
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="medici"] *,
            [data-theme-id="medici"] *::before,
            [data-theme-id="medici"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class MediciPrimitives : IThemePrimitives
{
    private const double Hue = 210;

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

    public virtual string Success50  => "oklch(0.95 0.035 155)";
    public virtual string Success100 => "oklch(0.88 0.07 155)";
    public virtual string Success500 => "oklch(0.58 0.16 155)";
    public virtual string Success600 => "oklch(0.50 0.16 155)";
    public virtual string Success700 => "oklch(0.42 0.15 155)";

    public virtual string Danger50  => "oklch(0.95 0.04 15)";
    public virtual string Danger100 => "oklch(0.88 0.09 15)";
    public virtual string Danger500 => "oklch(0.55 0.20 15)";
    public virtual string Danger600 => "oklch(0.48 0.20 15)";
    public virtual string Danger700 => "oklch(0.40 0.19 15)";

    public virtual string Warning50  => "oklch(0.97 0.04 70)";
    public virtual string Warning100 => "oklch(0.92 0.08 70)";
    public virtual string Warning500 => "oklch(0.70 0.18 70)";
    public virtual string Warning600 => "oklch(0.62 0.18 70)";

    public virtual string Info50  => "oklch(0.95 0.03 240)";
    public virtual string Info100 => "oklch(0.88 0.06 240)";
    public virtual string Info500 => "oklch(0.58 0.15 240)";
    public virtual string Info600 => "oklch(0.50 0.15 240)";

    public virtual string FontSans  => "'Atkinson Hyperlegible', sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string Radius2Xl  => "34px";
    public virtual string RadiusFull => "9999px";
}

internal class MediciSemanticLight : BaseLightConsistent
{
    public MediciSemanticLight() : base(210) { }

    public override string BgDefault     => "oklch(0.99 0.004 210)";
    public override string BgSubtle      => "oklch(0.97 0.006 210)";
    public override string BgMuted       => "oklch(0.935 0.01 210)";
    public override string BgEmphasized  => "oklch(0.89 0.014 210)";
    public override string BgOverlay     => "oklch(0.14 0.02 210 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.004 210 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 210 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.018 210)";
    public override string FgSubtle    => "oklch(0.36 0.014 210)";
    public override string FgMuted     => "oklch(0.52 0.01 210)";
    public override string FgDisabled  => "oklch(0.68 0.008 210)";
    public override string FgInverse   => "oklch(0.99 0.004 210)";
    public override string FgLink      => "oklch(0.56 0.20 210)";
    public override string FgLinkHover => "oklch(0.50 0.20 210)";

    public override string BorderDefault => "oklch(0.87 0.012 210)";
    public override string BorderSubtle  => "oklch(0.93 0.01 210)";
    public override string BorderStrong  => "oklch(0.80 0.015 210)";
    public override string BorderFocus   => "oklch(0.56 0.20 210)";
    public override string Divider       => "oklch(0.93 0.01 210)";

    public override string Font     => "'Atkinson Hyperlegible', sans-serif";
    public override string TextBase => "1rem";
    public override string LineHeightNormal => "1.7";

    public override string ColorSuccess        => "oklch(0.55 0.18 155)";
    public override string ColorSuccessHover   => "oklch(0.50 0.18 155)";
    public override string ColorSuccessSubtle  => "oklch(0.94 0.04 155)";

    public override string ColorDanger         => "oklch(0.49 0.20 15)";
    public override string ColorDangerHover    => "oklch(0.44 0.20 15)";
    public override string ColorDangerSubtle   => "oklch(0.94 0.05 15)";

    public override string ColorWarning        => "oklch(0.68 0.14 70)";
    public override string ColorWarningHover   => "oklch(0.62 0.14 70)";
    public override string ColorWarningSubtle  => "oklch(0.96 0.04 70)";

    public override string ColorInfo           => "oklch(0.50 0.14 240)";
    public override string ColorInfoHover      => "oklch(0.45 0.14 240)";
    public override string ColorInfoSubtle     => "oklch(0.94 0.035 240)";

    public override string FocusRing       => "0 0 0 2px oklch(0.99 0.004 210), 0 0 0 4px oklch(0.56 0.20 210)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.99 0.004 210), 0 0 0 4px oklch(0.49 0.20 15)";
}

internal class MediciSemanticDark : BaseDarkConsistent
{
    public MediciSemanticDark() : base(210) { }

    public override string ColorSuccess        => "oklch(0.58 0.18 155)";
    public override string ColorSuccessSubtle  => "oklch(0.18 0.04 155)";
    public override string ColorSuccessHover   => "oklch(0.64 0.18 155)";
    public override string ColorSuccessFg      => "oklch(0.95 0.005 210)";

    public override string ColorDanger         => "oklch(0.52 0.20 15)";
    public override string ColorDangerSubtle   => "oklch(0.18 0.06 15)";
    public override string ColorDangerHover    => "oklch(0.58 0.20 15)";
    public override string ColorDangerFg       => "oklch(0.95 0.005 210)";

    public override string ColorWarning        => "oklch(0.70 0.14 70)";
    public override string ColorWarningSubtle  => "oklch(0.22 0.04 70)";
    public override string ColorWarningHover   => "oklch(0.76 0.14 70)";
    public override string ColorWarningFg      => "oklch(0.10 0.012 210)";

    public override string ColorInfo           => "oklch(0.54 0.14 240)";
    public override string ColorInfoSubtle     => "oklch(0.18 0.04 240)";
    public override string ColorInfoHover      => "oklch(0.60 0.14 240)";
    public override string ColorInfoFg         => "oklch(0.95 0.005 210)";

    public override string Font     => "'Atkinson Hyperlegible', sans-serif";
    public override string TextBase => "1rem";
    public override string LineHeightNormal => "1.7";
}

internal class MediciComponents : BaseComponents { }

internal sealed class MediciTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Atkinson+Hyperlegible:ital,wght@0,400;0,700;1,400;1,700&family=Inter:wght@400;500;600;700&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Inter', sans-serif";
    public HeadingSettings H1 => new("2.25rem", HeadingFont, "700", "1.1", "-0.02em");
    public HeadingSettings H2 => new("1.875rem", HeadingFont, "700", "1.15", "-0.015em");
    public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "-0.01em");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.25", "0");
    public HeadingSettings H5 => new("1rem", HeadingFont, "500", "1.3", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
}
