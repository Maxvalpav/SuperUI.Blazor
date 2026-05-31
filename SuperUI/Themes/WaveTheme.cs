namespace SuperUI.Themes;

/// <summary>
/// Wave — спектральная тема на основе физиологии человеческого глаза (CIE 1964 10° observer).
/// Primary на 490nm (циан-голубой) — область максимальной остроты зрения (L и M колбочки).
/// Semantic цвета подобраны по длинам волн с максимальной различимостью.
/// </summary>
public sealed class WaveTheme : ThemeBase
{
    public override string Id => "wave";
    public override string Name => "Wave";
    public override string? Description => "Спектральная тема по физиологии глаза (CIE 1964). Primary ~490nm — пик остроты зрения. Semantic цвета по длинам волн с макс. различимостью.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new WavePrimitives();
    protected override IThemeSemantic CreateLight() => new WaveSemanticLight();
    protected override IThemeSemantic? CreateDark() => new WaveSemanticDark();
    protected override IThemeComponents? CreateComponents() => new WaveComponents();
    protected override IThemeTypography? CreateTypography() => new WaveTypography();

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
            [data-theme-id="wave"] *,
            [data-theme-id="wave"] *::before,
            [data-theme-id="wave"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class WavePrimitives : IThemePrimitives
{
    // Neutral — pure achromatic (no hue bias)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0 0)";
    public virtual string Neutral100 => "oklch(0.97 0 0)";
    public virtual string Neutral200 => "oklch(0.93 0 0)";
    public virtual string Neutral300 => "oklch(0.87 0 0)";
    public virtual string Neutral400 => "oklch(0.76 0 0)";
    public virtual string Neutral500 => "oklch(0.64 0 0)";
    public virtual string Neutral600 => "oklch(0.52 0 0)";
    public virtual string Neutral700 => "oklch(0.40 0 0)";
    public virtual string Neutral800 => "oklch(0.28 0 0)";
    public virtual string Neutral900 => "oklch(0.16 0 0)";

    // Primary — 490nm (cyan-blue, peak acuity)
    public virtual string Primary50  => "oklch(0.95 0.035 210)";
    public virtual string Primary100 => "oklch(0.89 0.07 210)";
    public virtual string Primary200 => "oklch(0.83 0.11 210)";
    public virtual string Primary300 => "oklch(0.75 0.15 210)";
    public virtual string Primary400 => "oklch(0.66 0.17 210)";
    public virtual string Primary500 => "oklch(0.58 0.17 210)";
    public virtual string Primary600 => "oklch(0.51 0.16 210)";
    public virtual string Primary700 => "oklch(0.43 0.15 210)";
    public virtual string Primary800 => "oklch(0.35 0.13 210)";
    public virtual string Primary900 => "oklch(0.26 0.11 210)";

    // Success — 530nm (M-cone peak)
    public virtual string Success50  => "oklch(0.95 0.03 140)";
    public virtual string Success100 => "oklch(0.88 0.06 140)";
    public virtual string Success500 => "oklch(0.58 0.12 140)";
    public virtual string Success600 => "oklch(0.50 0.12 140)";
    public virtual string Success700 => "oklch(0.42 0.11 140)";

    // Danger — 650nm (L-cone extended red)
    public virtual string Danger50  => "oklch(0.95 0.04 25)";
    public virtual string Danger100 => "oklch(0.88 0.09 25)";
    public virtual string Danger500 => "oklch(0.55 0.20 25)";
    public virtual string Danger600 => "oklch(0.48 0.20 25)";
    public virtual string Danger700 => "oklch(0.40 0.19 25)";

    // Warning — 580nm (L+M balanced)
    public virtual string Warning50  => "oklch(0.97 0.04 80)";
    public virtual string Warning100 => "oklch(0.92 0.08 80)";
    public virtual string Warning500 => "oklch(0.72 0.16 80)";
    public virtual string Warning600 => "oklch(0.64 0.16 80)";

    // Info — 460nm (S-cone peak)
    public virtual string Info50  => "oklch(0.95 0.03 250)";
    public virtual string Info100 => "oklch(0.89 0.06 250)";
    public virtual string Info500 => "oklch(0.58 0.14 250)";
    public virtual string Info600 => "oklch(0.50 0.14 250)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "2px";
    public virtual string RadiusSm   => "4px";
    public virtual string RadiusMd   => "6px";
    public virtual string RadiusLg   => "10px";
    public virtual string RadiusXl   => "14px";
    public virtual string Radius2Xl  => "20px";
    public virtual string RadiusFull => "9999px";
}

internal class WaveSemanticLight : BaseLightConsistent
{
    public WaveSemanticLight() : base(210) { }

    // Pure achromatic bg (zero chroma) — optical clarity
    public override string BgDefault     => "oklch(0.99 0 0)";
    public override string BgSubtle      => "oklch(0.97 0 0)";
    public override string BgMuted       => "oklch(0.935 0 0)";
    public override string BgEmphasized  => "oklch(0.89 0 0)";
    public override string BgOverlay     => "oklch(0.16 0 0 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0 0 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0 0 / 0.3)";

    public override string FgDefault   => "oklch(0.12 0 0)";
    public override string FgSubtle    => "oklch(0.34 0 0)";
    public override string FgMuted     => "oklch(0.50 0 0)";
    public override string FgDisabled  => "oklch(0.66 0 0)";
    public override string FgInverse   => "oklch(0.99 0 0)";
    public override string FgLink      => "oklch(0.58 0.17 210)";
    public override string FgLinkHover => "oklch(0.52 0.17 210)";

    public override string BorderDefault => "oklch(0.87 0 0)";
    public override string BorderSubtle  => "oklch(0.93 0 0)";
    public override string BorderStrong  => "oklch(0.80 0 0)";
    public override string BorderFocus   => "oklch(0.58 0.17 210)";
    public override string Divider       => "oklch(0.93 0 0)";

    public override string ColorPrimary        => "oklch(0.58 0.17 210)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.09 210)";
    public override string ColorPrimaryHover   => "oklch(0.52 0.17 210)";
    public override string ColorPrimaryActive  => "oklch(0.46 0.16 210)";

    public override string ColorSuccess        => "oklch(0.58 0.12 140)";
    public override string ColorSuccessHover   => "oklch(0.52 0.12 140)";
    public override string ColorSuccessFg      => "oklch(0.99 0 0)";

    public override string ColorDanger         => "oklch(0.55 0.20 25)";
    public override string ColorDangerHover    => "oklch(0.50 0.20 25)";

    public override string ColorWarning        => "oklch(0.72 0.16 80)";
    public override string ColorWarningHover   => "oklch(0.66 0.16 80)";

    public override string ColorInfo           => "oklch(0.58 0.14 250)";
    public override string ColorInfoHover      => "oklch(0.52 0.14 250)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextBase => "0.875rem";
    public override string TextLg   => "1.125rem";

    public override string ShadowXs => "0 1px 1px 0 oklch(0.12 0 0 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.12 0 0 / 0.06), 0 1px 1px -1px oklch(0.12 0 0 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.12 0 0 / 0.08), 0 1px 2px -1px oklch(0.12 0 0 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.12 0 0 / 0.10), 0 2px 4px -2px oklch(0.12 0 0 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.12 0 0 / 0.14), 0 4px 8px -4px oklch(0.12 0 0 / 0.08)";

    public override string RadiusSm   => "4px";
    public override string RadiusMd   => "6px";
    public override string RadiusLg   => "10px";
    public override string RadiusXl   => "14px";

    public override string TransitionFast => "100ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "300ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.58 0.17 210)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.20 25)";
}

internal class WaveSemanticDark : BaseDarkConsistent
{
    public WaveSemanticDark() : base(210) { }

    public override string ColorPrimary        => "oklch(0.68 0.19 210)";
    public override string ColorPrimarySubtle  => "oklch(0.18 0.07 210)";
    public override string ColorPrimaryMuted   => "oklch(0.26 0.11 210)";
    public override string ColorPrimaryHover   => "oklch(0.76 0.19 210)";
    public override string ColorPrimaryActive  => "oklch(0.62 0.19 210)";
    public override string ColorPrimaryFg      => "oklch(0.04 0 0)";

    public override string ColorSuccess        => "oklch(0.62 0.14 140)";
    public override string ColorSuccessSubtle  => "oklch(0.16 0.04 140)";
    public override string ColorSuccessHover   => "oklch(0.68 0.14 140)";
    public override string ColorSuccessFg      => "oklch(0.04 0 0)";

    public override string ColorDanger         => "oklch(0.62 0.22 25)";
    public override string ColorDangerSubtle   => "oklch(0.18 0.07 25)";
    public override string ColorDangerHover    => "oklch(0.68 0.22 25)";
    public override string ColorDangerFg       => "oklch(0.04 0 0)";

    public override string ColorWarning        => "oklch(0.78 0.18 80)";
    public override string ColorWarningSubtle  => "oklch(0.20 0.06 80)";
    public override string ColorWarningHover   => "oklch(0.84 0.16 80)";
    public override string ColorWarningFg      => "oklch(0.04 0 0)";

    public override string ColorInfo           => "oklch(0.64 0.16 250)";
    public override string ColorInfoSubtle     => "oklch(0.18 0.05 250)";
    public override string ColorInfoHover      => "oklch(0.70 0.15 250)";
    public override string ColorInfoFg         => "oklch(0.04 0 0)";
}

internal class WaveComponents : IThemeComponents
{
    public virtual string BtnRadius     => "4px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "34px";

    public virtual string InputRadius   => "4px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "34px";

    public virtual string CardRadius      => "4px";
    public virtual string CardPadding     => "8px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "6px";

    public virtual string TableRadius          => "4px";
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

    internal sealed class WaveTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=Source+Serif+4:wght@400;600;700&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Source Serif Pro', 'Source Serif 4', serif";
        public HeadingSettings H1 => new("2rem", HeadingFont, "600", "1.15", "-0.01em");
        public HeadingSettings H2 => new("1.75rem", HeadingFont, "600", "1.2", "0");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H4 => new("1.25rem", HeadingFont, "500", "1.3", "0");
        public HeadingSettings H5 => new("1.125rem", HeadingFont, "500", "1.35", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.4", "0.01em");
    }
