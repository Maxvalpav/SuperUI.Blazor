namespace SuperUI.Themes;

/// <summary>
/// Prism — joy & discovery. Purple with gradient effects. Color scripting with max chroma.
/// Vivid semantic colors, pill-style buttons, colored shadows, glow focus rings.
/// </summary>
public sealed class PrismTheme : ThemeBase
{
    public override string Id => "prism";
    public override string Name => "Prism";
    public override string? Description => "Joy & discovery theme. Purple with gradient effects. Max chroma semantic colors, pill buttons, glow focus.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Art";

    protected override IThemePrimitives CreatePrimitives() => new PrismPrimitives();
    protected override IThemeSemantic CreateLight() => new PrismSemanticLight();
    protected override IThemeSemantic? CreateDark() => new PrismSemanticDark();
    protected override IThemeComponents? CreateComponents() => new PrismComponents();
    protected override IThemeTypography? CreateTypography() => new PrismTypography();

    public override string? AdditionalCss => $$"""
        :root,
        [data-theme-id="prism"] {
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

        @keyframes prism-glow {
            0%, 100% { box-shadow: 0 0 4px oklch(0.54 0.22 280 / 0.5), 0 0 8px oklch(0.54 0.22 280 / 0.3); }
            50% { box-shadow: 0 0 8px oklch(0.54 0.22 280 / 0.7), 0 0 16px oklch(0.54 0.22 280 / 0.4); }
        }

        [data-theme-id="prism"] :focus-visible {
            animation: prism-glow 2s ease-in-out infinite;
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="prism"] *,
            [data-theme-id="prism"] *::before,
            [data-theme-id="prism"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class PrismPrimitives : IThemePrimitives
{
    private const double NeutralHue = 280;
    private const double PrimaryHue = 280;

    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => $"oklch(0.985 0.004 {NeutralHue})";
    public virtual string Neutral100 => $"oklch(0.97 0.006 {NeutralHue})";
    public virtual string Neutral200 => $"oklch(0.93 0.009 {NeutralHue})";
    public virtual string Neutral300 => $"oklch(0.87 0.012 {NeutralHue})";
    public virtual string Neutral400 => $"oklch(0.76 0.014 {NeutralHue})";
    public virtual string Neutral500 => $"oklch(0.64 0.014 {NeutralHue})";
    public virtual string Neutral600 => $"oklch(0.52 0.016 {NeutralHue})";
    public virtual string Neutral700 => $"oklch(0.40 0.018 {NeutralHue})";
    public virtual string Neutral800 => $"oklch(0.28 0.02 {NeutralHue})";
    public virtual string Neutral900 => $"oklch(0.16 0.022 {NeutralHue})";

    public virtual string Primary50  => $"oklch(0.95 0.04 {PrimaryHue})";
    public virtual string Primary100 => $"oklch(0.89 0.08 {PrimaryHue})";
    public virtual string Primary200 => $"oklch(0.83 0.12 {PrimaryHue})";
    public virtual string Primary300 => $"oklch(0.75 0.16 {PrimaryHue})";
    public virtual string Primary400 => $"oklch(0.66 0.20 {PrimaryHue})";
    public virtual string Primary500 => $"oklch(0.58 0.20 {PrimaryHue})";
    public virtual string Primary600 => $"oklch(0.51 0.18 {PrimaryHue})";
    public virtual string Primary700 => $"oklch(0.43 0.17 {PrimaryHue})";
    public virtual string Primary800 => $"oklch(0.34 0.16 {PrimaryHue})";
    public virtual string Primary900 => $"oklch(0.25 0.14 {PrimaryHue})";

    public virtual string Success50  => "oklch(0.95 0.04 120)";
    public virtual string Success100 => "oklch(0.88 0.08 120)";
    public virtual string Success500 => "oklch(0.58 0.20 120)";
    public virtual string Success600 => "oklch(0.50 0.20 120)";
    public virtual string Success700 => "oklch(0.42 0.19 120)";

    public virtual string Danger50  => "oklch(0.95 0.05 10)";
    public virtual string Danger100 => "oklch(0.88 0.10 10)";
    public virtual string Danger500 => "oklch(0.55 0.24 10)";
    public virtual string Danger600 => "oklch(0.48 0.24 10)";
    public virtual string Danger700 => "oklch(0.40 0.23 10)";

    public virtual string Warning50  => "oklch(0.97 0.05 55)";
    public virtual string Warning100 => "oklch(0.92 0.10 55)";
    public virtual string Warning500 => "oklch(0.70 0.22 55)";
    public virtual string Warning600 => "oklch(0.62 0.22 55)";

    public virtual string Info50  => "oklch(0.95 0.04 190)";
    public virtual string Info100 => "oklch(0.88 0.08 190)";
    public virtual string Info500 => "oklch(0.58 0.18 190)";
    public virtual string Info600 => "oklch(0.50 0.18 190)";

    public virtual string FontSans  => "'Outfit', sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "4px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "10px";
    public virtual string RadiusLg   => "16px";
    public virtual string RadiusXl   => "24px";
    public virtual string Radius2Xl  => "36px";
    public virtual string RadiusFull => "9999px";
}

internal class PrismSemanticLight : BaseLightConsistent
{
    public PrismSemanticLight() : base(280) { }

    public override string BgDefault     => "oklch(0.99 0.004 280)";
    public override string BgSubtle      => "oklch(0.97 0.006 280)";
    public override string BgMuted       => "oklch(0.935 0.01 280)";
    public override string BgEmphasized  => "oklch(0.89 0.014 280)";
    public override string BgOverlay     => "oklch(0.14 0.022 280 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.004 280 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 280 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.022 280)";
    public override string FgSubtle    => "oklch(0.36 0.016 280)";
    public override string FgMuted     => "oklch(0.52 0.014 280)";
    public override string FgDisabled  => "oklch(0.68 0.01 280)";
    public override string FgInverse   => "oklch(0.99 0.004 280)";
    public override string FgLink      => "oklch(0.54 0.22 280)";
    public override string FgLinkHover => "oklch(0.48 0.22 280)";

    public override string BorderDefault => "oklch(0.87 0.014 280)";
    public override string BorderSubtle  => "oklch(0.93 0.012 280)";
    public override string BorderStrong  => "oklch(0.80 0.018 280)";
    public override string BorderFocus   => "oklch(0.54 0.22 280)";
    public override string Divider       => "oklch(0.93 0.012 280)";

    public override string ColorPrimary        => "oklch(0.54 0.22 280)";
    public override string ColorPrimaryHover   => "oklch(0.48 0.22 280)";
    public override string ColorPrimaryActive  => "oklch(0.42 0.21 280)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.045 280)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.09 280)";

    public override string ColorSuccess        => "oklch(0.58 0.20 120)";
    public override string ColorSuccessHover   => "oklch(0.52 0.20 120)";
    public override string ColorSuccessSubtle  => "oklch(0.94 0.045 120)";

    public override string ColorDanger         => "oklch(0.55 0.24 10)";
    public override string ColorDangerHover    => "oklch(0.49 0.24 10)";
    public override string ColorDangerSubtle   => "oklch(0.94 0.055 10)";

    public override string ColorWarning        => "oklch(0.70 0.22 55)";
    public override string ColorWarningHover   => "oklch(0.64 0.22 55)";
    public override string ColorWarningSubtle  => "oklch(0.96 0.05 55)";
    public override string ColorWarningFg      => "oklch(0.14 0.022 280)";

    public override string ColorInfo           => "oklch(0.58 0.18 190)";
    public override string ColorInfoHover      => "oklch(0.52 0.18 190)";
    public override string ColorInfoSubtle     => "oklch(0.94 0.04 190)";

    public override string Font     => "'Outfit', sans-serif";

    public override string ShadowXs => "0 1px 1px 0 oklch(0.54 0.22 280 / 0.08)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.54 0.22 280 / 0.12), 0 1px 1px -1px oklch(0.54 0.22 280 / 0.08)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.54 0.22 280 / 0.15), 0 1px 2px -1px oklch(0.54 0.22 280 / 0.10)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.54 0.22 280 / 0.18), 0 2px 4px -2px oklch(0.54 0.22 280 / 0.10)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.54 0.22 280 / 0.22), 0 4px 8px -4px oklch(0.54 0.22 280 / 0.12)";

    public override string FocusRing       => "0 0 0 3px oklch(0.99 0.004 280), 0 0 0 6px oklch(0.54 0.22 280)";
    public override string FocusRingDanger => "0 0 0 3px oklch(0.99 0.004 280), 0 0 0 6px oklch(0.55 0.24 10)";
}

internal class PrismSemanticDark : BaseDarkConsistent
{
    public PrismSemanticDark() : base(280) { }

    public override string ColorPrimary        => "oklch(0.66 0.22 280)";
    public override string ColorPrimarySubtle  => "oklch(0.20 0.07 280)";
    public override string ColorPrimaryMuted   => "oklch(0.28 0.12 280)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.20 280)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.22 280)";
    public override string ColorPrimaryFg      => "oklch(0.10 0.015 280)";

    public override string ColorSuccess        => "oklch(0.60 0.20 120)";
    public override string ColorSuccessSubtle  => "oklch(0.18 0.05 120)";
    public override string ColorSuccessHover   => "oklch(0.66 0.20 120)";
    public override string ColorSuccessFg      => "oklch(0.95 0.005 280)";

    public override string ColorDanger         => "oklch(0.58 0.24 10)";
    public override string ColorDangerSubtle   => "oklch(0.18 0.07 10)";
    public override string ColorDangerHover    => "oklch(0.64 0.24 10)";
    public override string ColorDangerFg       => "oklch(0.95 0.005 280)";

    public override string ColorWarning        => "oklch(0.72 0.22 55)";
    public override string ColorWarningSubtle  => "oklch(0.22 0.06 55)";
    public override string ColorWarningHover   => "oklch(0.78 0.20 55)";
    public override string ColorWarningFg      => "oklch(0.10 0.015 280)";

    public override string ColorInfo           => "oklch(0.60 0.18 190)";
    public override string ColorInfoSubtle     => "oklch(0.20 0.05 190)";
    public override string ColorInfoHover      => "oklch(0.66 0.16 190)";
    public override string ColorInfoFg         => "oklch(0.95 0.005 280)";

    public override string Font     => "'Outfit', sans-serif";

    public override string FocusRing       => "0 0 0 3px oklch(0.11 0.008 280), 0 0 0 6px oklch(0.66 0.22 280)";
    public override string FocusRingDanger => "0 0 0 3px oklch(0.11 0.008 280), 0 0 0 6px oklch(0.58 0.24 10)";
}

internal class PrismComponents : BaseComponents
{
    public override string BtnRadius     => "12px";
    public override string CardRadius    => "16px";
    public override string ModalRadius   => "16px";
}

internal sealed class PrismTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Outfit:wght@400;500;600;700&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Outfit', sans-serif";
    public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.05", "-0.025em");
    public HeadingSettings H2 => new("2rem", HeadingFont, "700", "1.1", "-0.02em");
    public HeadingSettings H3 => new("1.625rem", HeadingFont, "600", "1.15", "-0.015em");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.2", "-0.01em");
    public HeadingSettings H5 => new("1rem", HeadingFont, "500", "1.25", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.3", "0.01em");
}
