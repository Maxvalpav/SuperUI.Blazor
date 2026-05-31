namespace SuperUI.Themes;

/// <summary>
/// Aether — AI-era purple cosmic theme. Glassmorphism, deep space dark, ethereal.
/// Deep purple primary with frosted glass surfaces and blur effects.
/// </summary>
public sealed class AetherTheme : ThemeBase
{
    public override string Id => "aether";
    public override string Name => "Aether";
    public override string? Description => "AI-era purple cosmic theme. Glassmorphism with deep space dark mode. Ethereal and futuristic.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Art";

    protected override IThemePrimitives CreatePrimitives() => new AetherPrimitives();
    protected override IThemeSemantic CreateLight() => new AetherSemanticLight();
    protected override IThemeSemantic? CreateDark() => new AetherSemanticDark();
    protected override IThemeComponents? CreateComponents() => new AetherComponents();
    protected override IThemeTypography? CreateTypography() => new AetherTypography();

    public override string? AdditionalCss => $$"""
        :root,
        [data-theme-id="aether"] {
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

        [data-theme-id="aether"] .sgc-card {
            backdrop-filter: blur(20px);
            background: var(--sg-bg-glass);
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="aether"] *,
            [data-theme-id="aether"] *::before,
            [data-theme-id="aether"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class AetherPrimitives : IThemePrimitives
{
    private const double Hue = 270;

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

    public virtual string FontSans  => "'Plus Jakarta Sans', sans-serif";
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

internal class AetherSemanticLight : BaseLightConsistent
{
    public AetherSemanticLight() : base(270) { }

    public override string BgDefault     => "oklch(0.99 0.003 270)";
    public override string BgSubtle      => "oklch(0.97 0.005 270)";
    public override string BgMuted       => "oklch(0.935 0.008 270)";
    public override string BgEmphasized  => "oklch(0.89 0.012 270)";
    public override string BgOverlay     => "oklch(0.14 0.02 270 / 0.30)";
    public override string BgGlass       => "oklch(0.99 0.003 270 / 0.65)";
    public override string BorderGlass   => "oklch(0.87 0.015 270 / 0.25)";
    public override string BlurGlass     => "20px";

    public override string FgDefault   => "oklch(0.14 0.02 270)";
    public override string FgSubtle    => "oklch(0.36 0.015 270)";
    public override string FgMuted     => "oklch(0.52 0.012 270)";
    public override string FgDisabled  => "oklch(0.68 0.008 270)";
    public override string FgInverse   => "oklch(0.99 0.003 270)";
    public override string FgLink      => "oklch(0.56 0.20 270)";
    public override string FgLinkHover => "oklch(0.50 0.20 270)";

    public override string BorderDefault => "oklch(0.87 0.012 270)";
    public override string BorderSubtle  => "oklch(0.93 0.01 270)";
    public override string BorderStrong  => "oklch(0.80 0.015 270)";
    public override string BorderFocus   => "oklch(0.56 0.20 270)";
    public override string Divider       => "oklch(0.93 0.01 270)";

    public override string ColorPrimary        => "oklch(0.56 0.20 270)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.20 270)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.19 270)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 270)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 270)";

    public override string Font     => "'Plus Jakarta Sans', sans-serif";

    public override string FocusRing       => "0 0 0 2px oklch(0.99 0.003 270), 0 0 0 4px oklch(0.56 0.20 270)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.99 0.003 270), 0 0 0 4px oklch(0.55 0.20 19)";
}

internal class AetherSemanticDark : BaseDarkConsistent
{
    public AetherSemanticDark() : base(270) { }

    public override string BgDefault     => "oklch(0.06 0.025 270)";
    public override string BgSubtle      => "oklch(0.10 0.03 270)";
    public override string BgMuted       => "oklch(0.14 0.035 270)";
    public override string BgEmphasized  => "oklch(0.18 0.04 270)";
    public override string BgGlass       => "oklch(0.06 0.025 270 / 0.6)";
    public override string BlurGlass     => "24px";

    public override string Surface         => "oklch(0.11 0.035 270)";
    public override string SurfaceRaised   => "oklch(0.15 0.04 270)";
    public override string SurfaceOverlay  => "oklch(0.15 0.04 270)";

    public override string FgDefault   => "oklch(0.96 0.008 270)";
    public override string FgSubtle    => "oklch(0.80 0.012 270)";
    public override string FgMuted     => "oklch(0.60 0.015 270)";

    public override string ColorPrimary        => "oklch(0.66 0.20 270)";
    public override string ColorPrimarySubtle  => "oklch(0.20 0.06 270)";
    public override string ColorPrimaryMuted   => "oklch(0.28 0.10 270)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.20 270)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.20 270)";
    public override string ColorPrimaryFg      => "oklch(0.06 0.025 270)";

    public override string Font     => "'Plus Jakarta Sans', sans-serif";
}

internal class AetherComponents : BaseComponents { }

internal sealed class AetherTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:ital,wght@0,400;0,500;0,600;0,700;1,400&family=JetBrains+Mono:wght@400;500;600&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Plus Jakarta Sans', sans-serif";
    public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.05", "-0.02em");
    public HeadingSettings H2 => new("2rem", HeadingFont, "700", "1.1", "-0.015em");
    public HeadingSettings H3 => new("1.625rem", HeadingFont, "600", "1.15", "-0.01em");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.2", "0");
    public HeadingSettings H5 => new("1rem", HeadingFont, "500", "1.25", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.3", "0.01em");
}
