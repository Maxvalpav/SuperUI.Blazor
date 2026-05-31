namespace SuperUI.Themes;

/// <summary>
/// Reader — сепийная тема для длительного чтения на основе исследований Wang+ 2018.
/// Светлая мода — НЕ белая, а тёплый кремовый/сепия (oklch 0.90), снижающий зрительную усталость.
/// Тёмная мода — без синего спектра (hue 40°), сохраняющий мелатонин.
/// </summary>
public sealed class ReaderTheme : ThemeBase
{
    public override string Id => "reader";
    public override string Name => "Reader";
    public override string? Description => "Сепийная тема для чтения. Light — тёплый кремовый (не белый!), снижает усталость. Dark — без синего спектра, сохраняет мелатонин.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new ReaderPrimitives();
    protected override IThemeSemantic CreateLight() => new ReaderSemanticLight();
    protected override IThemeSemantic? CreateDark() => new ReaderSemanticDark();
    protected override IThemeComponents? CreateComponents() => new ReaderComponents();
    protected override IThemeTypography? CreateTypography() => new ReaderTypography();

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

        [data-theme-id="reader"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            text-rendering: optimizeLegibility;
        }

        /* Reader — комфортное чтение */
        [data-theme-id="reader"] .sgc-card {
            border-radius: 8px;
            background: oklch(0.92 0.02 55);
            border: 1px solid oklch(0.85 0.025 55);
            box-shadow: none;
        }
        [data-theme="dark"] [data-theme-id="reader"] .sgc-card {
            background: oklch(0.15 0.02 40);
            border-color: oklch(0.22 0.025 40);
        }

        [data-theme-id="reader"] .sgc-btn {
            border-radius: 6px;
            font-weight: 500;
            transition: background-color 200ms ease,
                        color            200ms ease;
        }

        [data-theme-id="reader"] .sgc-input,
        [data-theme-id="reader"] .sgc-select,
        [data-theme-id="reader"] .sgc-textarea {
            border-radius: 6px;
            border: 1px solid oklch(0.82 0.02 55);
            background: oklch(0.95 0.015 55);
        }
        [data-theme="dark"] [data-theme-id="reader"] .sgc-input,
        [data-theme="dark"] [data-theme-id="reader"] .sgc-select,
        [data-theme="dark"] [data-theme-id="reader"] .sgc-textarea {
            background: oklch(0.15 0.02 40);
            border-color: oklch(0.25 0.025 40);
        }

        [data-theme-id="reader"] ::selection {
            background: oklch(0.80 0.06 55);
            color: oklch(0.12 0.02 55);
        }
        [data-theme="dark"] [data-theme-id="reader"] ::selection {
            background: oklch(0.35 0.08 55);
            color: oklch(0.90 0.015 55);
        }

        /* Reader — scrollbar */
        [data-theme-id="reader"] ::-webkit-scrollbar-thumb {
            background: oklch(0.75 0.03 55);
            border-radius: 9999px;
        }
        [data-theme="dark"] [data-theme-id="reader"] ::-webkit-scrollbar-thumb {
            background: oklch(0.30 0.025 55);
        }

        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="reader"] *,
            [data-theme-id="reader"] *::before,
            [data-theme-id="reader"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class ReaderPrimitives : IThemePrimitives
{
    // Neutral — warm paper (hue 55°)
    public virtual string Neutral0   => "oklch(0.95 0.02 55)";   // not pure white!
    public virtual string Neutral50  => "oklch(0.93 0.02 55)";
    public virtual string Neutral100 => "oklch(0.90 0.025 55)";
    public virtual string Neutral200 => "oklch(0.86 0.03 55)";
    public virtual string Neutral300 => "oklch(0.80 0.035 55)";
    public virtual string Neutral400 => "oklch(0.72 0.03 55)";
    public virtual string Neutral500 => "oklch(0.62 0.025 55)";
    public virtual string Neutral600 => "oklch(0.52 0.02 55)";
    public virtual string Neutral700 => "oklch(0.40 0.02 55)";
    public virtual string Neutral800 => "oklch(0.28 0.02 55)";
    public virtual string Neutral900 => "oklch(0.16 0.02 55)";

    // Primary — Warm amber-brown (hue 55°)
    public virtual string Primary50  => "oklch(0.95 0.03 55)";
    public virtual string Primary100 => "oklch(0.90 0.06 55)";
    public virtual string Primary200 => "oklch(0.83 0.09 55)";
    public virtual string Primary300 => "oklch(0.74 0.12 55)";
    public virtual string Primary400 => "oklch(0.65 0.14 55)";
    public virtual string Primary500 => "oklch(0.57 0.14 55)";
    public virtual string Primary600 => "oklch(0.50 0.13 55)";
    public virtual string Primary700 => "oklch(0.42 0.12 55)";
    public virtual string Primary800 => "oklch(0.34 0.11 55)";
    public virtual string Primary900 => "oklch(0.25 0.09 55)";

    // Success — Warm olive (hue 120°)
    public virtual string Success50  => "oklch(0.95 0.02 120)";
    public virtual string Success100 => "oklch(0.88 0.045 120)";
    public virtual string Success500 => "oklch(0.58 0.10 120)";
    public virtual string Success600 => "oklch(0.50 0.10 120)";
    public virtual string Success700 => "oklch(0.42 0.09 120)";

    // Danger — Warm brick (hue 15°)
    public virtual string Danger50  => "oklch(0.95 0.035 15)";
    public virtual string Danger100 => "oklch(0.88 0.07 15)";
    public virtual string Danger500 => "oklch(0.55 0.16 15)";
    public virtual string Danger600 => "oklch(0.48 0.16 15)";
    public virtual string Danger700 => "oklch(0.40 0.15 15)";

    // Warning — Golden (hue 55°)
    public virtual string Warning50  => "oklch(0.97 0.03 55)";
    public virtual string Warning100 => "oklch(0.92 0.06 55)";
    public virtual string Warning500 => "oklch(0.72 0.14 55)";
    public virtual string Warning600 => "oklch(0.64 0.14 55)";

    // Info — Warm teal (hue 180°)
    public virtual string Info50  => "oklch(0.95 0.02 180)";
    public virtual string Info100 => "oklch(0.88 0.04 180)";
    public virtual string Info500 => "oklch(0.58 0.08 180)";
    public virtual string Info600 => "oklch(0.50 0.08 180)";

    public virtual string FontSans  => "'Georgia', 'Merriweather', 'Times New Roman', serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "'Merriweather', Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string Radius2Xl  => "24px";
    public virtual string RadiusFull => "9999px";
}

internal class ReaderSemanticLight : IThemeSemantic
{
    // Light — «Сепия» (тёплый кремовый, не белый!)
    public virtual string BgDefault     => "oklch(0.90 0.025 55)";   // warm cream
    public virtual string BgSubtle      => "oklch(0.93 0.02 55)";
    public virtual string BgMuted       => "oklch(0.86 0.025 55)";
    public virtual string BgEmphasized  => "oklch(0.82 0.03 55)";
    public virtual string BgOverlay     => "oklch(0.16 0.02 55 / 0.30)";
    public virtual string BgGlass       => "oklch(0.90 0.025 55 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.82 0.03 55 / 0.3)";
    public virtual string BlurGlass     => "12px";

    public virtual string Surface         => "oklch(0.95 0.02 55)";
    public virtual string SurfaceRaised   => "oklch(0.93 0.025 55)";
    public virtual string SurfaceOverlay  => "oklch(0.93 0.025 55)";

    public virtual string FgDefault   => "oklch(0.12 0.02 55)";
    public virtual string FgSubtle    => "oklch(0.34 0.02 55)";
    public virtual string FgMuted     => "oklch(0.50 0.015 55)";
    public virtual string FgDisabled  => "oklch(0.66 0.01 55)";
    public virtual string FgInverse   => "oklch(0.90 0.025 55)";
    public virtual string FgLink      => "oklch(0.57 0.14 55)";
    public virtual string FgLinkHover => "oklch(0.50 0.14 55)";

    public virtual string BorderDefault => "oklch(0.82 0.025 55)";
    public virtual string BorderSubtle  => "oklch(0.88 0.02 55)";
    public virtual string BorderStrong  => "oklch(0.75 0.03 55)";
    public virtual string BorderFocus   => "oklch(0.57 0.14 55)";
    public virtual string Divider       => "oklch(0.88 0.02 55)";

    public virtual string ColorPrimary        => "oklch(0.57 0.14 55)";
    public virtual string ColorPrimarySubtle  => "oklch(0.93 0.04 55)";
    public virtual string ColorPrimaryMuted   => "oklch(0.84 0.08 55)";
    public virtual string ColorPrimaryHover   => "oklch(0.50 0.14 55)";
    public virtual string ColorPrimaryActive  => "oklch(0.44 0.13 55)";
    public virtual string ColorPrimaryFg      => "oklch(0.95 0.02 55)";

    public virtual string ColorSuccess        => "oklch(0.58 0.10 120)";
    public virtual string ColorSuccessSubtle  => "oklch(0.94 0.025 120)";
    public virtual string ColorSuccessHover   => "oklch(0.52 0.10 120)";
    public virtual string ColorSuccessFg      => "oklch(0.12 0.02 55)";

    public virtual string ColorDanger         => "oklch(0.55 0.16 15)";
    public virtual string ColorDangerSubtle   => "oklch(0.94 0.04 15)";
    public virtual string ColorDangerHover    => "oklch(0.50 0.16 15)";
    public virtual string ColorDangerFg       => "oklch(0.95 0.02 55)";

    public virtual string ColorWarning        => "oklch(0.72 0.14 55)";
    public virtual string ColorWarningSubtle  => "oklch(0.96 0.04 55)";
    public virtual string ColorWarningHover   => "oklch(0.66 0.14 55)";
    public virtual string ColorWarningFg      => "oklch(0.12 0.02 55)";

    public virtual string ColorInfo           => "oklch(0.58 0.08 180)";
    public virtual string ColorInfoSubtle     => "oklch(0.94 0.025 180)";
    public virtual string ColorInfoHover      => "oklch(0.52 0.08 180)";
    public virtual string ColorInfoFg         => "oklch(0.12 0.02 55)";

    public virtual string Font     => "'Georgia', 'Merriweather', serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.875rem";
    public virtual string TextBase => "1.125rem";    // larger base for reading
    public virtual string TextLg   => "1.5rem";

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

    public virtual string ShadowXs => "0 1px 1px 0 oklch(0.12 0.02 55 / 0.04)";
    public virtual string ShadowSm => "0 1px 2px 0 oklch(0.12 0.02 55 / 0.06), 0 1px 1px -1px oklch(0.12 0.02 55 / 0.06)";
    public virtual string ShadowMd => "0 2px 4px -1px oklch(0.12 0.02 55 / 0.08), 0 1px 2px -1px oklch(0.12 0.02 55 / 0.06)";
    public virtual string ShadowLg => "0 8px 16px -4px oklch(0.12 0.02 55 / 0.10), 0 2px 4px -2px oklch(0.12 0.02 55 / 0.06)";
    public virtual string ShadowXl => "0 16px 32px -8px oklch(0.12 0.02 55 / 0.14), 0 4px 8px -4px oklch(0.12 0.02 55 / 0.08)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms ease";
    public virtual string TransitionBase => "200ms ease";
    public virtual string TransitionSlow => "350ms ease";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.90 0.025 55), 0 0 0 4px oklch(0.57 0.14 55)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.90 0.025 55), 0 0 0 4px oklch(0.55 0.16 15)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class ReaderSemanticDark : BaseDarkConsistent
{
    // Reader dark — тёплый уголь без синего спектра (hue 40-55°)
    public ReaderSemanticDark() : base(55) { }

    // Surface/background — warm amber hue (40°), not cool
    public override string BgDefault     => "oklch(0.12 0.02 40)";
    public override string BgSubtle      => "oklch(0.18 0.025 40)";
    public override string BgMuted       => "oklch(0.15 0.022 40)";
    public override string BgEmphasized  => "oklch(0.22 0.03 40)";
    public override string BgOverlay     => "oklch(0 0 0 / 0.60)";
    public override string BgGlass       => "oklch(0.12 0.02 40 / 0.7)";

    public override string Surface         => "oklch(0.15 0.025 40)";
    public override string SurfaceRaised   => "oklch(0.17 0.03 40)";
    public override string SurfaceOverlay  => "oklch(0.17 0.03 40)";

    public override string FgDefault   => "oklch(0.88 0.015 40)";
    public override string FgSubtle    => "oklch(0.72 0.02 40)";
    public override string FgMuted     => "oklch(0.55 0.02 40)";
    public override string FgDisabled  => "oklch(0.48 0.02 40)";  // ~4.5:1 AA (было 0.38 → FAIL)
    public override string FgInverse   => "oklch(0.12 0.02 40)";
    public override string FgLink      => "oklch(0.65 0.14 55)";
    public override string FgLinkHover => "oklch(0.72 0.14 55)";

    public override string BorderDefault => "oklch(0.25 0.025 40)";
    public override string BorderSubtle  => "oklch(0.18 0.022 40)";
    public override string BorderStrong  => "oklch(0.30 0.03 40)";
    public override string BorderFocus   => "oklch(0.65 0.14 55)";
    public override string Divider       => "oklch(0.18 0.022 40)";

    // Brand — amber primary (без синего спектра)
    public override string ColorPrimary        => "oklch(0.65 0.14 55)";
    public override string ColorPrimarySubtle  => "oklch(0.22 0.05 55)";
    public override string ColorPrimaryMuted   => "oklch(0.30 0.08 55)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.14 55)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.14 55)";
    public override string ColorPrimaryFg      => "oklch(0.12 0.02 40)";

    // Reader-specific semantic colors (muted, eye-friendly)
    public override string ColorSuccess        => "oklch(0.60 0.08 120)";
    public override string ColorSuccessSubtle  => "oklch(0.18 0.025 120)";
    public override string ColorSuccessHover   => "oklch(0.66 0.08 120)";
    public override string ColorSuccessFg      => "oklch(0.88 0.015 40)";

    public override string ColorDanger         => "oklch(0.60 0.14 15)";
    public override string ColorDangerSubtle   => "oklch(0.20 0.045 15)";
    public override string ColorDangerHover    => "oklch(0.66 0.14 15)";
    public override string ColorDangerFg       => "oklch(0.88 0.015 40)";

    public override string ColorWarning        => "oklch(0.76 0.12 55)";
    public override string ColorWarningSubtle  => "oklch(0.22 0.04 55)";
    public override string ColorWarningHover   => "oklch(0.82 0.10 55)";
    public override string ColorWarningFg      => "oklch(0.12 0.02 40)";

    public override string ColorInfo           => "oklch(0.62 0.07 180)";
    public override string ColorInfoSubtle     => "oklch(0.18 0.02 180)";
    public override string ColorInfoHover      => "oklch(0.68 0.06 180)";
    public override string ColorInfoFg         => "oklch(0.88 0.015 40)";

    // Reader — serif font for readability
    public override string Font     => "'Georgia', 'Merriweather', serif";
    public override string TextSm   => "0.875rem";
    public override string TextBase => "1.125rem";
    public override string TextLg   => "1.5rem";

    // Reader-specific shadows (slightly softer for reading comfort)
    public override string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.35)";
    public override string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.45)";
    public override string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.50)";
    public override string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.55)";
    public override string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.60)";

    // Slightly relaxed radii for organic feel
    public override string RadiusLg   => "12px";
    public override string RadiusXl   => "16px";

    // Relaxed easing for reading comfort
    public override string TransitionFast => "120ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "350ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(0.12 0.02 40), 0 0 0 4px oklch(0.65 0.14 55)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.12 0.02 40), 0 0 0 4px oklch(0.60 0.14 15)";
}

internal class ReaderComponents : IThemeComponents
{
    public virtual string BtnRadius     => "6px";
    public virtual string BtnFontSize   => "0.8125rem";
    public virtual string BtnFontWeight => "500";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "34px";

    public virtual string InputRadius   => "6px";
    public virtual string InputFontSize => "0.875rem";
    public virtual string InputHeight   => "32px";
    public virtual string InputHeightSm => "30px";
    public virtual string InputHeightLg => "36px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "12px";
    public virtual string CardBorderColor => "oklch(0.82 0.025 55)";
    public virtual string CardBg          => "oklch(0.92 0.02 55)";

    public virtual string ModalRadius => "12px";

    public virtual string TableRadius          => "6px";
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "320px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}

    internal sealed class ReaderTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Literata:ital,wght@0,400;0,600;0,700;1,400&family=Source+Serif+4:wght@400;600;700&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Source Serif Pro', 'Source Serif 4', serif";
        public HeadingSettings H1 => new("2.25rem", HeadingFont, "700", "1.15", "-0.01em");
        public HeadingSettings H2 => new("1.875rem", HeadingFont, "700", "1.2", "-0.005em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "700", "1.25", "0");
        public HeadingSettings H4 => new("1.25rem", HeadingFont, "700", "1.3", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "700", "1.35", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "600", "1.4", "0.01em");
    }
