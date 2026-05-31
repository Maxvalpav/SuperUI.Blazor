namespace SuperUI.Themes;

/// <summary>
/// Cosmos — тема «Гармония сфер» на основе Kepler Harmonices Mundi (1619).
/// Планетарные орбитальные пропорции как музыкальные интервалы → цвета.
/// Neutrals — цвет внепланетного пространства.
/// </summary>
public sealed class CosmosTheme : ThemeBase
{
    public override string Id => "cosmos";
    public override string Name => "Cosmos";
    public override string? Description => "Гармония сфер (Kepler). Планетарные интервалы → цвета. Земля = primary, Марс = danger, Венера = success.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Elegant";

    protected override IThemePrimitives CreatePrimitives() => new CosmosPrimitives();
    protected override IThemeSemantic CreateLight() => new CosmosSemanticLight();
    protected override IThemeSemantic? CreateDark() => new CosmosSemanticDark();
    protected override IThemeComponents? CreateComponents() => new CosmosComponents();
    protected override IThemeTypography? CreateTypography() => new CosmosTypography();

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
            [data-theme-id="cosmos"] *,
            [data-theme-id="cosmos"] *::before,
            [data-theme-id="cosmos"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class CosmosPrimitives : IThemePrimitives
{
    // Neutral — deep space (hue 270°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 270)";
    public virtual string Neutral100 => "oklch(0.97 0.005 270)";
    public virtual string Neutral200 => "oklch(0.93 0.008 270)";
    public virtual string Neutral300 => "oklch(0.87 0.01 270)";
    public virtual string Neutral400 => "oklch(0.76 0.012 270)";
    public virtual string Neutral500 => "oklch(0.64 0.012 270)";
    public virtual string Neutral600 => "oklch(0.52 0.014 270)";
    public virtual string Neutral700 => "oklch(0.40 0.016 270)";
    public virtual string Neutral800 => "oklch(0.28 0.018 270)";
    public virtual string Neutral900 => "oklch(0.16 0.02 270)";

    // Primary — Earth (1:1) → 220°
    public virtual string Primary50  => "oklch(0.95 0.03 220)";
    public virtual string Primary100 => "oklch(0.90 0.06 220)";
    public virtual string Primary200 => "oklch(0.84 0.09 220)";
    public virtual string Primary300 => "oklch(0.76 0.12 220)";
    public virtual string Primary400 => "oklch(0.67 0.14 220)";
    public virtual string Primary500 => "oklch(0.59 0.14 220)";
    public virtual string Primary600 => "oklch(0.52 0.13 220)";
    public virtual string Primary700 => "oklch(0.44 0.12 220)";
    public virtual string Primary800 => "oklch(0.35 0.11 220)";
    public virtual string Primary900 => "oklch(0.26 0.09 220)";

    // Success — Venus (5:4) → +115.9° → 335.9°
    public virtual string Success50  => "oklch(0.95 0.03 335.9)";
    public virtual string Success100 => "oklch(0.88 0.06 335.9)";
    public virtual string Success500 => "oklch(0.58 0.14 335.9)";
    public virtual string Success600 => "oklch(0.50 0.14 335.9)";
    public virtual string Success700 => "oklch(0.42 0.13 335.9)";

    // Danger — Mars (3:2) → +210.6° → 70.6°
    public virtual string Danger50  => "oklch(0.95 0.04 70.6)";
    public virtual string Danger100 => "oklch(0.88 0.09 70.6)";
    public virtual string Danger500 => "oklch(0.55 0.20 70.6)";
    public virtual string Danger600 => "oklch(0.48 0.20 70.6)";
    public virtual string Danger700 => "oklch(0.40 0.19 70.6)";

    // Warning — Jupiter (8:5) → +149.4° → 9.4°
    public virtual string Warning50  => "oklch(0.97 0.03 9.4)";
    public virtual string Warning100 => "oklch(0.92 0.06 9.4)";
    public virtual string Warning500 => "oklch(0.70 0.14 9.4)";
    public virtual string Warning600 => "oklch(0.62 0.14 9.4)";

    // Info — Mercury (6:5) → +94.7° → 314.7°
    public virtual string Info50  => "oklch(0.95 0.025 314.7)";
    public virtual string Info100 => "oklch(0.88 0.05 314.7)";
    public virtual string Info500 => "oklch(0.58 0.12 314.7)";
    public virtual string Info600 => "oklch(0.50 0.12 314.7)";

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

internal class CosmosSemanticLight : BaseLightConsistent
{
    public CosmosSemanticLight() : base(270) { }

    // Star light purple-tinted bg (hue 270°), blue primary (hue 220°)
    public override string BgDefault     => "oklch(0.99 0.003 270)";
    public override string BgSubtle      => "oklch(0.97 0.005 270)";
    public override string BgMuted       => "oklch(0.935 0.008 270)";
    public override string BgEmphasized  => "oklch(0.89 0.012 270)";
    public override string BgOverlay     => "oklch(0.16 0.02 270 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.003 270 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 270 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.015 270)";
    public override string FgSubtle    => "oklch(0.36 0.012 270)";
    public override string FgMuted     => "oklch(0.52 0.01 270)";
    public override string FgDisabled  => "oklch(0.68 0.008 270)";
    public override string FgInverse   => "oklch(0.99 0.003 270)";
    public override string FgLink      => "oklch(0.59 0.14 220)";
    public override string FgLinkHover => "oklch(0.53 0.14 220)";

    public override string BorderDefault => "oklch(0.87 0.012 270)";
    public override string BorderSubtle  => "oklch(0.93 0.01 270)";
    public override string BorderStrong  => "oklch(0.80 0.015 270)";
    public override string BorderFocus   => "oklch(0.59 0.14 220)";
    public override string Divider       => "oklch(0.93 0.01 270)";

    // Blue primary (hue 220°)
    public override string ColorPrimary        => "oklch(0.59 0.14 220)";
    public override string ColorPrimaryHover   => "oklch(0.53 0.14 220)";
    public override string ColorPrimaryActive  => "oklch(0.47 0.13 220)";

    // Inverted semantic hues — magenta success, yellow danger
    public override string ColorSuccess        => "oklch(0.58 0.14 335.9)";
    public override string ColorSuccessHover   => "oklch(0.52 0.14 335.9)";

    public override string ColorDanger         => "oklch(0.55 0.20 70.6)";
    public override string ColorDangerHover    => "oklch(0.50 0.20 70.6)";

    public override string ColorWarning        => "oklch(0.70 0.14 9.4)";
    public override string ColorWarningHover   => "oklch(0.64 0.14 9.4)";

    public override string ColorInfo           => "oklch(0.58 0.12 314.7)";
    public override string ColorInfoHover      => "oklch(0.52 0.12 314.7)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string TransitionFast => "120ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "350ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.59 0.14 220)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.20 70.6)";
}

internal class CosmosSemanticDark : IThemeSemantic
{
    // Dark — «Глубокий космос»
    public virtual string BgDefault     => "oklch(0.04 0.015 270)";
    public virtual string BgSubtle      => "oklch(0.12 0.02 270)";
    public virtual string BgMuted       => "oklch(0.09 0.018 270)";
    public virtual string BgEmphasized  => "oklch(0.16 0.025 270)";
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.80)";
    public virtual string BgGlass       => "oklch(0.04 0.015 270 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.06)";
    public virtual string BlurGlass     => "20px";

    public virtual string Surface         => "oklch(0.08 0.02 270)";
    public virtual string SurfaceRaised   => "oklch(0.10 0.025 270)";
    public virtual string SurfaceOverlay  => "oklch(0.10 0.025 270)";

    public virtual string FgDefault   => "oklch(0.96 0.005 270)";
    public virtual string FgSubtle    => "oklch(0.80 0.01 270)";
    public virtual string FgMuted     => "oklch(0.55 0.015 270)";
    public virtual string FgDisabled  => "oklch(0.35 0.015 270)";
    public virtual string FgInverse   => "oklch(0.04 0.015 270)";
    public virtual string FgLink      => "oklch(0.65 0.16 220)";
    public virtual string FgLinkHover => "oklch(0.72 0.16 220)";

    public virtual string BorderDefault => "oklch(0.22 0.02 270)";
    public virtual string BorderSubtle  => "oklch(0.15 0.018 270)";
    public virtual string BorderStrong  => "oklch(0.28 0.025 270)";
    public virtual string BorderFocus   => "oklch(0.65 0.16 220)";
    public virtual string Divider       => "oklch(0.15 0.018 270)";

    public virtual string ColorPrimary        => "oklch(0.65 0.16 220)";
    public virtual string ColorPrimarySubtle  => "oklch(0.18 0.05 220)";
    public virtual string ColorPrimaryMuted   => "oklch(0.26 0.08 220)";
    public virtual string ColorPrimaryHover   => "oklch(0.72 0.16 220)";
    public virtual string ColorPrimaryActive  => "oklch(0.59 0.16 220)";
    public virtual string ColorPrimaryFg      => "oklch(0.04 0.015 270)";

    public virtual string ColorSuccess        => "oklch(0.62 0.14 335.9)";
    public virtual string ColorSuccessSubtle  => "oklch(0.18 0.04 335.9)";
    public virtual string ColorSuccessHover   => "oklch(0.68 0.14 335.9)";
    public virtual string ColorSuccessFg      => "oklch(0.96 0.005 270)";

    public virtual string ColorDanger         => "oklch(0.60 0.22 70.6)";
    public virtual string ColorDangerSubtle   => "oklch(0.18 0.06 70.6)";
    public virtual string ColorDangerHover    => "oklch(0.66 0.22 70.6)";
    public virtual string ColorDangerFg       => "oklch(0.96 0.005 270)";

    public virtual string ColorWarning        => "oklch(0.74 0.14 9.4)";
    public virtual string ColorWarningSubtle  => "oklch(0.20 0.04 9.4)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.12 9.4)";
    public virtual string ColorWarningFg      => "oklch(0.04 0.015 270)";

    public virtual string ColorInfo           => "oklch(0.62 0.12 314.7)";
    public virtual string ColorInfoSubtle     => "oklch(0.18 0.035 314.7)";
    public virtual string ColorInfoHover      => "oklch(0.68 0.11 314.7)";
    public virtual string ColorInfoFg         => "oklch(0.96 0.005 270)";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.8125rem";
    public virtual string TextBase => "1rem";
    public virtual string TextLg   => "1.25rem";

    public virtual string TextXs   => "0.6875rem";
    public virtual string TextXl   => "1.125rem";
    public virtual string Text2Xl  => "1.375rem";
    public virtual string Text3Xl  => "1.75rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    public virtual string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.50)";
    public virtual string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.60)";
    public virtual string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.65)";
    public virtual string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.70)";
    public virtual string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.75)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms ease";
    public virtual string TransitionBase => "200ms ease";
    public virtual string TransitionSlow => "350ms ease";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.04 0.015 270), 0 0 0 4px oklch(0.65 0.16 220)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.04 0.015 270), 0 0 0 4px oklch(0.60 0.22 70.6)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class CosmosComponents : IThemeComponents
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

    internal sealed class CosmosTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Space+Grotesk:wght@400;500;600;700&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Space Grotesk', sans-serif";
        public HeadingSettings H1 => new("2.75rem", HeadingFont, "700", "1.05", "-0.02em");
        public HeadingSettings H2 => new("2.25rem", HeadingFont, "700", "1.1", "-0.015em");
        public HeadingSettings H3 => new("1.875rem", HeadingFont, "700", "1.15", "-0.01em");
        public HeadingSettings H4 => new("1.5rem", HeadingFont, "600", "1.2", "0");
        public HeadingSettings H5 => new("1.25rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H6 => new("1rem", HeadingFont, "600", "1.3", "0.01em");
    }
