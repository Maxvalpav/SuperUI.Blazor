namespace SuperUI.Themes;

/// <summary>
/// Inclus — тема универсального дизайна (Color Universal Design) на основе палитры Okabe &amp; Ito (2002/2008).
/// Оптимизирована для всех типов цветовой слепоты: протанопия, дейтеранопия, тританопия.
/// Ключевые принципы: синий primary (различим при всех CVD), danger = оранжевый (не красный!),
/// success = голубой/бирюзовый (не зелёный!), shape + color encoding.
/// </summary>
public sealed class InclusTheme : ThemeBase
{
    public override string Id => "inclus";
    public override string Name => "Inclus";
    public override string? Description => "Color Universal Design на основе палитры Okabe-Ito. Для всех типов цветовой слепоты: primary — синий, danger — оранжевый (не зелёный!).";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new InclusPrimitives();
    protected override IThemeSemantic CreateLight() => new InclusSemanticLight();
    protected override IThemeSemantic? CreateDark() => new InclusSemanticDark();
    protected override IThemeComponents? CreateComponents() => new InclusComponents();
    protected override IThemeTypography? CreateTypography() => new InclusTypography();

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

        /* Inclus — high contrast focus */
        [data-theme-id="inclus"] *:focus-visible {
            outline: 2px solid var(--sg-color-primary);
            outline-offset: 2px;
            box-shadow: none;
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="inclus"] *,
            [data-theme-id="inclus"] *::before,
            [data-theme-id="inclus"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class InclusPrimitives : IThemePrimitives
{
    // Neutral — warm gray (hue 50°) for readability
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 50)";
    public virtual string Neutral100 => "oklch(0.97 0.005 50)";
    public virtual string Neutral200 => "oklch(0.93 0.008 50)";
    public virtual string Neutral300 => "oklch(0.87 0.01 50)";
    public virtual string Neutral400 => "oklch(0.76 0.012 50)";
    public virtual string Neutral500 => "oklch(0.64 0.012 50)";
    public virtual string Neutral600 => "oklch(0.52 0.014 50)";
    public virtual string Neutral700 => "oklch(0.40 0.016 50)";
    public virtual string Neutral800 => "oklch(0.28 0.018 50)";
    public virtual string Neutral900 => "oklch(0.16 0.02 50)";

    // Primary — Blue (#0077BB), distinguishable by all CVD types
    public virtual string Primary50  => "oklch(0.95 0.03 260)";
    public virtual string Primary100 => "oklch(0.89 0.06 260)";
    public virtual string Primary200 => "oklch(0.83 0.09 260)";
    public virtual string Primary300 => "oklch(0.75 0.12 260)";
    public virtual string Primary400 => "oklch(0.66 0.14 260)";
    public virtual string Primary500 => "oklch(0.58 0.14 260)";
    public virtual string Primary600 => "oklch(0.51 0.13 260)";
    public virtual string Primary700 => "oklch(0.43 0.12 260)";
    public virtual string Primary800 => "oklch(0.34 0.11 260)";
    public virtual string Primary900 => "oklch(0.25 0.09 260)";

    // Success — Sky blue / Teal (#33BBEE) — NOT green!
    public virtual string Success50  => "oklch(0.95 0.025 200)";
    public virtual string Success100 => "oklch(0.88 0.05 200)";
    public virtual string Success500 => "oklch(0.60 0.12 200)";
    public virtual string Success600 => "oklch(0.52 0.12 200)";
    public virtual string Success700 => "oklch(0.44 0.11 200)";

    // Danger — Vermillion / Orange (#EE7733) — NOT red!
    public virtual string Danger50  => "oklch(0.95 0.05 40)";
    public virtual string Danger100 => "oklch(0.88 0.10 40)";
    public virtual string Danger500 => "oklch(0.60 0.18 40)";
    public virtual string Danger600 => "oklch(0.52 0.18 40)";
    public virtual string Danger700 => "oklch(0.44 0.17 40)";

    // Warning — Yellow (#DDCC33)
    public virtual string Warning50  => "oklch(0.97 0.04 100)";
    public virtual string Warning100 => "oklch(0.92 0.08 100)";
    public virtual string Warning500 => "oklch(0.74 0.14 100)";
    public virtual string Warning600 => "oklch(0.66 0.14 100)";

    // Info — Blue (#0077BB)
    public virtual string Info50  => "oklch(0.95 0.03 260)";
    public virtual string Info100 => "oklch(0.89 0.06 260)";
    public virtual string Info500 => "oklch(0.58 0.14 260)";
    public virtual string Info600 => "oklch(0.51 0.14 260)";

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

internal class InclusSemanticLight : BaseLightConsistent
{
    public InclusSemanticLight() : base(260) { }

    // Warm balanced bg (hue 50°), accessible blue primary
    public override string BgDefault     => "oklch(0.99 0.003 50)";
    public override string BgSubtle      => "oklch(0.97 0.005 50)";
    public override string BgMuted       => "oklch(0.935 0.008 50)";
    public override string BgEmphasized  => "oklch(0.89 0.012 50)";
    public override string BgOverlay     => "oklch(0.16 0.02 50 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.003 50 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 50 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.015 50)";
    public override string FgSubtle    => "oklch(0.36 0.012 50)";
    public override string FgMuted     => "oklch(0.52 0.01 50)";
    public override string FgDisabled  => "oklch(0.68 0.008 50)";
    public override string FgInverse   => "oklch(0.99 0.003 50)";
    public override string FgLink      => "oklch(0.58 0.14 260)";
    public override string FgLinkHover => "oklch(0.52 0.14 260)";

    public override string BorderDefault => "oklch(0.87 0.012 50)";
    public override string BorderSubtle  => "oklch(0.93 0.01 50)";
    public override string BorderStrong  => "oklch(0.80 0.015 50)";
    public override string BorderFocus   => "oklch(0.58 0.14 260)";
    public override string Divider       => "oklch(0.93 0.01 50)";

    // Blue primary (hue 260°)
    public override string ColorPrimary        => "oklch(0.58 0.14 260)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.07 260)";
    public override string ColorPrimaryHover   => "oklch(0.52 0.14 260)";
    public override string ColorPrimaryActive  => "oklch(0.46 0.13 260)";

    // Teal success (not green), orange danger (not red) — accessible differentiation
    public override string ColorSuccess        => "oklch(0.60 0.12 200)";
    public override string ColorSuccessHover   => "oklch(0.54 0.12 200)";

    public override string ColorDanger         => "oklch(0.60 0.18 40)";
    public override string ColorDangerHover    => "oklch(0.54 0.18 40)";

    public override string ColorWarning        => "oklch(0.74 0.14 100)";
    public override string ColorWarningHover   => "oklch(0.68 0.14 100)";

    public override string ColorInfo           => "oklch(0.58 0.14 260)";
    public override string ColorInfoHover      => "oklch(0.52 0.14 260)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string TransitionFast => "120ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "350ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.58 0.14 260)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.60 0.18 40)";
}

internal class InclusSemanticDark : BaseDarkConsistent
{
    public InclusSemanticDark() : base(260) { }

    public override string ColorPrimary        => "oklch(0.65 0.16 260)";
    public override string ColorPrimarySubtle  => "oklch(0.20 0.05 260)";
    public override string ColorPrimaryMuted   => "oklch(0.28 0.08 260)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.16 260)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.16 260)";
    public override string ColorPrimaryFg      => "oklch(0.10 0.01 50)";

    public override string ColorSuccess        => "oklch(0.62 0.12 200)"; // teal, not green
    public override string ColorSuccessSubtle  => "oklch(0.18 0.04 200)";
    public override string ColorSuccessHover   => "oklch(0.68 0.12 200)";
    public override string ColorSuccessFg      => "oklch(0.95 0.003 50)";

    public override string ColorDanger         => "oklch(0.64 0.18 40)";  // orange, not red
    public override string ColorDangerSubtle   => "oklch(0.20 0.06 40)";
    public override string ColorDangerHover    => "oklch(0.70 0.18 40)";
    public override string ColorDangerFg       => "oklch(0.95 0.003 50)";

    public override string ColorWarning        => "oklch(0.78 0.14 100)"; // yellow
    public override string ColorWarningSubtle  => "oklch(0.22 0.05 100)";
    public override string ColorWarningHover   => "oklch(0.84 0.12 100)";
    public override string ColorWarningFg      => "oklch(0.10 0.01 50)";

    public override string ColorInfo           => "oklch(0.65 0.16 260)"; // blue
    public override string ColorInfoSubtle     => "oklch(0.20 0.05 260)";
    public override string ColorInfoHover      => "oklch(0.72 0.16 260)";
    public override string ColorInfoFg         => "oklch(0.95 0.003 50)";
}

internal class InclusComponents : IThemeComponents
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

    internal sealed class InclusTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Atkinson+Hyperlegible:wght@400;700&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => null;
        public HeadingSettings H1 => new("2rem", null, "700", "1.15", "-0.01em");
        public HeadingSettings H2 => new("1.75rem", null, "600", "1.2", "-0.005em");
        public HeadingSettings H3 => new("1.5rem", null, "600", "1.25", "0");
        public HeadingSettings H4 => new("1.25rem", null, "600", "1.3", "0");
        public HeadingSettings H5 => new("1.125rem", null, "600", "1.35", "0");
        public HeadingSettings H6 => new("0.875rem", null, "500", "1.4", "0");
    }
