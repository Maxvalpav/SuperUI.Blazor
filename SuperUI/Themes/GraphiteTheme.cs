namespace SuperUI.Themes;

/// <summary>
/// Graphite — строгая сине-серая тема для B2B / industrial enterprise.
/// Светлая: «Холодное серебро», тёмная: «Антрацит».
/// </summary>
public sealed class GraphiteTheme : ThemeBase
{
    public override string Id => "graphite";
    public override string Name => "Graphite";
    public override string? Description => "Строгая сине-серая тема для B2B enterprise. Светлая — «Холодное серебро». Тёмная — «Антрацит».";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Elegant";

    protected override IThemePrimitives CreatePrimitives() => new GraphitePrimitives();
    protected override IThemeSemantic CreateLight() => new GraphiteSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GraphiteSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GraphiteComponents();
    protected override IThemeTypography? CreateTypography() => new GraphiteTypography();

    public override string? AdditionalCss => $$"""
        :root,
        [data-theme="light"],
        [data-theme="dark"] {
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
            [data-theme-id="graphite"] *,
            [data-theme-id="graphite"] *::before,
            [data-theme-id="graphite"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class GraphitePrimitives : IThemePrimitives
{
    // Neutral — cool steel gray (hue 240°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 240)";
    public virtual string Neutral100 => "oklch(0.97 0.005 240)";
    public virtual string Neutral200 => "oklch(0.93 0.008 240)";
    public virtual string Neutral300 => "oklch(0.87 0.01 240)";
    public virtual string Neutral400 => "oklch(0.76 0.012 240)";
    public virtual string Neutral500 => "oklch(0.64 0.012 240)";
    public virtual string Neutral600 => "oklch(0.52 0.014 240)";
    public virtual string Neutral700 => "oklch(0.40 0.016 240)";
    public virtual string Neutral800 => "oklch(0.28 0.018 240)";
    public virtual string Neutral900 => "oklch(0.16 0.02 240)";

    // Primary — Industrial blue-gray, low chroma
    public virtual string Primary50  => "oklch(0.95 0.015 240)";
    public virtual string Primary100 => "oklch(0.90 0.025 240)";
    public virtual string Primary200 => "oklch(0.84 0.035 240)";
    public virtual string Primary300 => "oklch(0.76 0.045 240)";
    public virtual string Primary400 => "oklch(0.67 0.055 240)";
    public virtual string Primary500 => "oklch(0.59 0.06 240)";
    public virtual string Primary600 => "oklch(0.52 0.055 240)";
    public virtual string Primary700 => "oklch(0.44 0.05 240)";
    public virtual string Primary800 => "oklch(0.35 0.04 240)";
    public virtual string Primary900 => "oklch(0.26 0.03 240)";

    // Success — Slate green
    public virtual string Success50  => "oklch(0.95 0.015 155)";
    public virtual string Success100 => "oklch(0.88 0.035 155)";
    public virtual string Success500 => "oklch(0.58 0.08 155)";
    public virtual string Success600 => "oklch(0.50 0.08 155)";
    public virtual string Success700 => "oklch(0.42 0.075 155)";

    // Danger — Muted brick (hue 10°)
    public virtual string Danger50  => "oklch(0.95 0.025 10)";
    public virtual string Danger100 => "oklch(0.88 0.06 10)";
    public virtual string Danger500 => "oklch(0.55 0.14 10)";
    public virtual string Danger600 => "oklch(0.48 0.14 10)";
    public virtual string Danger700 => "oklch(0.40 0.13 10)";

    // Warning — Muted amber (hue 50°)
    public virtual string Warning50  => "oklch(0.97 0.02 50)";
    public virtual string Warning100 => "oklch(0.92 0.04 50)";
    public virtual string Warning500 => "oklch(0.70 0.10 50)";
    public virtual string Warning600 => "oklch(0.62 0.10 50)";

    // Info — Steel blue (hue 220°)
    public virtual string Info50  => "oklch(0.95 0.02 220)";
    public virtual string Info100 => "oklch(0.88 0.04 220)";
    public virtual string Info500 => "oklch(0.58 0.09 220)";
    public virtual string Info600 => "oklch(0.50 0.09 220)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
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

internal class GraphiteSemanticLight : BaseLightConsistent
{
    public GraphiteSemanticLight() : base(240) { }

    // Cold silver — very low chroma (hue 240°)
    public override string BgDefault     => "oklch(0.99 0.003 240)";
    public override string BgSubtle      => "oklch(0.97 0.005 240)";
    public override string BgMuted       => "oklch(0.935 0.008 240)";
    public override string BgEmphasized  => "oklch(0.89 0.012 240)";
    public override string BgOverlay     => "oklch(0.16 0.02 240 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.003 240 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 240 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.018 240)";
    public override string FgSubtle    => "oklch(0.36 0.015 240)";
    public override string FgMuted     => "oklch(0.52 0.012 240)";
    public override string FgDisabled  => "oklch(0.68 0.008 240)";
    public override string FgInverse   => "oklch(0.99 0.003 240)";
    public override string FgLink      => "oklch(0.59 0.06 240)";
    public override string FgLinkHover => "oklch(0.53 0.06 240)";

    public override string BorderDefault => "oklch(0.87 0.012 240)";
    public override string BorderSubtle  => "oklch(0.93 0.01 240)";
    public override string BorderStrong  => "oklch(0.80 0.015 240)";
    public override string BorderFocus   => "oklch(0.59 0.06 240)";
    public override string Divider       => "oklch(0.93 0.01 240)";

    // Extremely low chroma primary (0.06) — silver aesthetic
    public override string ColorPrimary        => "oklch(0.59 0.06 240)";
    public override string ColorPrimarySubtle  => "oklch(0.95 0.02 240)";
    public override string ColorPrimaryMuted   => "oklch(0.86 0.035 240)";
    public override string ColorPrimaryHover   => "oklch(0.53 0.06 240)";
    public override string ColorPrimaryActive  => "oklch(0.47 0.055 240)";

    public override string ColorSuccess        => "oklch(0.58 0.08 155)";
    public override string ColorSuccessHover   => "oklch(0.52 0.08 155)";

    public override string ColorDanger         => "oklch(0.55 0.14 10)";
    public override string ColorDangerHover    => "oklch(0.50 0.14 10)";

    public override string ColorWarning        => "oklch(0.70 0.10 50)";
    public override string ColorWarningHover   => "oklch(0.64 0.10 50)";

    public override string ColorInfo           => "oklch(0.58 0.09 220)";
    public override string ColorInfoHover      => "oklch(0.52 0.09 220)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.59 0.06 240)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.14 10)";
}

internal class GraphiteSemanticDark : BaseDarkConsistent
{
    public GraphiteSemanticDark() : base(240) { }

    public override string ColorPrimary        => "oklch(0.65 0.07 240)";
    public override string ColorPrimarySubtle  => "oklch(0.20 0.025 240)";
    public override string ColorPrimaryMuted   => "oklch(0.28 0.035 240)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.07 240)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.07 240)";
    public override string ColorPrimaryFg      => "oklch(0.10 0.008 240)";

    public override string ColorSuccess        => "oklch(0.58 0.07 155)";
    public override string ColorSuccessSubtle  => "oklch(0.18 0.02 155)";
    public override string ColorSuccessHover   => "oklch(0.64 0.07 155)";
    public override string ColorSuccessFg      => "oklch(0.95 0.003 240)";

    public override string ColorDanger         => "oklch(0.58 0.12 10)";
    public override string ColorDangerSubtle   => "oklch(0.20 0.035 10)";
    public override string ColorDangerHover    => "oklch(0.64 0.12 10)";
    public override string ColorDangerFg       => "oklch(0.95 0.003 240)";

    public override string ColorWarning        => "oklch(0.72 0.10 50)";
    public override string ColorWarningSubtle  => "oklch(0.22 0.025 50)";
    public override string ColorWarningHover   => "oklch(0.78 0.08 50)";
    public override string ColorWarningFg      => "oklch(0.10 0.008 240)";

    public override string ColorInfo           => "oklch(0.60 0.08 220)";
    public override string ColorInfoSubtle     => "oklch(0.20 0.025 220)";
    public override string ColorInfoHover      => "oklch(0.66 0.07 220)";
    public override string ColorInfoFg         => "oklch(0.95 0.003 240)";
}

internal class GraphiteComponents : IThemeComponents
{
    public virtual string BtnRadius     => "5px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "34px";

    public virtual string InputRadius   => "3px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "34px";

    public virtual string CardRadius      => "5px";
    public virtual string CardPadding     => "8px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "8px";

    public virtual string TableRadius          => "5px";
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "260px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}

    internal sealed class GraphiteTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Serif:wght@400;600;700&family=IBM+Plex+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'IBM Plex Serif', serif";
        public HeadingSettings H1 => new("2.25rem", HeadingFont, "600", "1.1", "-0.02em");
        public HeadingSettings H2 => new("1.875rem", HeadingFont, "600", "1.15", "-0.01em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "0");
        public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H5 => new("1.125rem", HeadingFont, "500", "1.3", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
    }
