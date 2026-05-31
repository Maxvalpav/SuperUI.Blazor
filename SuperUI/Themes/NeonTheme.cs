namespace SuperUI.Themes;

/// <summary>
/// Neon — футуристическая кибер-тема с неоновым свечением.
/// Dark-first: «Кибер-ночь» (основная), светлая: «Ледяной терминал».
/// </summary>
public sealed class NeonTheme : ThemeBase
{
    public override string Id => "neon";
    public override string Name => "Neon";
    public override string? Description => "Футуристическая кибер-тема с неоновым свечением. Dark-first: «Кибер-ночь». Светлая: «Ледяной терминал».";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Elegant";

    protected override IThemePrimitives CreatePrimitives() => new NeonPrimitives();
    protected override IThemeSemantic CreateLight() => new NeonSemanticLight();
    protected override IThemeSemantic? CreateDark() => new NeonSemanticDark();
    protected override IThemeComponents? CreateComponents() => new NeonComponents();
    protected override IThemeTypography? CreateTypography() => new NeonTypography();

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

        [data-theme-id="neon"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
        }

        /* ── Neon Glow — Buttons ── */
        [data-theme-id="neon"] .sgc-btn {
            border-radius: 4px;
            font-weight: 600;
            letter-spacing: 0.02em;
            text-transform: uppercase;
            transition: background-color 200ms ease,
                        border-color     200ms ease,
                        box-shadow       200ms ease,
                        transform        100ms ease;
        }
        [data-theme-id="neon"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="neon"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 0 16px var(--sg-color-primary),
                        0 0 4px var(--sg-color-primary-muted);
        }
        [data-theme-id="neon"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Neon Glow — Inputs ── */
        [data-theme-id="neon"] .sgc-input,
        [data-theme-id="neon"] .sgc-select,
        [data-theme-id="neon"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: 4px;
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color 200ms ease,
                        box-shadow   200ms ease;
        }
        [data-theme-id="neon"] .sgc-input:focus,
        [data-theme-id="neon"] .sgc-select:focus,
        [data-theme-id="neon"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            box-shadow: 0 0 0 1px var(--sg-color-primary),
                        0 0 8px var(--sg-color-primary-muted);
            outline: none;
        }

        /* ── Neon Glow — Focus ring ── */
        [data-theme-id="neon"] *:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Neon — Cards ── */
        [data-theme-id="neon"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: 4px;
            transition: border-color 200ms ease,
                        box-shadow   200ms ease;
        }
        [data-theme-id="neon"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: 0 0 12px var(--sg-color-primary-muted);
        }

        /* ── Neon — Progress ── */
        [data-theme-id="neon"] .sgc-progress-fill {
            background: linear-gradient(90deg, var(--sg-color-primary), var(--sg-color-info));
        }

        /* ── Neon — Nav ── */
        [data-theme-id="neon"] .sgc-nav-link {
            border-radius: 4px;
            padding: 4px 8px;
            font-size: 0.8125rem;
            color: var(--sg-fg-subtle);
            transition: background 200ms ease,
                        color      200ms ease;
        }
        [data-theme-id="neon"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="neon"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            box-shadow: 0 0 6px var(--sg-color-primary-subtle);
        }

        /* ── Neon — Selection ── */
        [data-theme-id="neon"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Neon — Scrollbar ── */
        [data-theme-id="neon"] ::-webkit-scrollbar-thumb {
            background: var(--sg-color-primary-muted);
            border-radius: 9999px;
        }

        /* ── Neon — Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="neon"] *,
            [data-theme-id="neon"] *::before,
            [data-theme-id="neon"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class NeonPrimitives : IThemePrimitives
{
    // Neutral — deep cool blue-gray (hue 220°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 220)";
    public virtual string Neutral100 => "oklch(0.97 0.005 220)";
    public virtual string Neutral200 => "oklch(0.93 0.008 220)";
    public virtual string Neutral300 => "oklch(0.87 0.01 220)";
    public virtual string Neutral400 => "oklch(0.76 0.012 220)";
    public virtual string Neutral500 => "oklch(0.64 0.012 220)";
    public virtual string Neutral600 => "oklch(0.52 0.014 220)";
    public virtual string Neutral700 => "oklch(0.40 0.016 220)";
    public virtual string Neutral800 => "oklch(0.28 0.018 220)";
    public virtual string Neutral900 => "oklch(0.16 0.02 220)";

    // Primary — Neon Cyan (hue 200°, high chroma)
    public virtual string Primary50  => "oklch(0.95 0.04 200)";
    public virtual string Primary100 => "oklch(0.89 0.08 200)";
    public virtual string Primary200 => "oklch(0.82 0.14 200)";
    public virtual string Primary300 => "oklch(0.74 0.20 200)";
    public virtual string Primary400 => "oklch(0.65 0.24 200)";
    public virtual string Primary500 => "oklch(0.57 0.24 200)";
    public virtual string Primary600 => "oklch(0.50 0.22 200)";
    public virtual string Primary700 => "oklch(0.42 0.20 200)";
    public virtual string Primary800 => "oklch(0.34 0.18 200)";
    public virtual string Primary900 => "oklch(0.25 0.14 200)";

    // Success — Neon green (hue 160°)
    public virtual string Success50  => "oklch(0.95 0.03 160)";
    public virtual string Success100 => "oklch(0.88 0.07 160)";
    public virtual string Success500 => "oklch(0.58 0.16 160)";
    public virtual string Success600 => "oklch(0.50 0.16 160)";
    public virtual string Success700 => "oklch(0.42 0.15 160)";

    // Danger — Hot pink (hue 350°)
    public virtual string Danger50  => "oklch(0.95 0.05 350)";
    public virtual string Danger100 => "oklch(0.88 0.10 350)";
    public virtual string Danger500 => "oklch(0.55 0.24 350)";
    public virtual string Danger600 => "oklch(0.48 0.24 350)";
    public virtual string Danger700 => "oklch(0.40 0.22 350)";

    // Warning — Neon amber (hue 50°)
    public virtual string Warning50  => "oklch(0.97 0.04 50)";
    public virtual string Warning100 => "oklch(0.92 0.08 50)";
    public virtual string Warning500 => "oklch(0.70 0.20 50)";
    public virtual string Warning600 => "oklch(0.62 0.20 50)";

    // Info — Electric purple (hue 280°)
    public virtual string Info50  => "oklch(0.95 0.04 280)";
    public virtual string Info100 => "oklch(0.88 0.08 280)";
    public virtual string Info500 => "oklch(0.58 0.18 280)";
    public virtual string Info600 => "oklch(0.50 0.18 280)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "2px";
    public virtual string RadiusSm   => "4px";
    public virtual string RadiusMd   => "6px";
    public virtual string RadiusLg   => "8px";
    public virtual string RadiusXl   => "12px";
    public virtual string Radius2Xl  => "16px";
    public virtual string RadiusFull => "9999px";
}

internal class NeonSemanticLight : BaseLightConsistent
{
    public NeonSemanticLight() : base(220) { }

    // Cold terminal aesthetic (hue 220°)
    public override string BgDefault     => "oklch(0.99 0.003 220)";
    public override string BgSubtle      => "oklch(0.97 0.005 220)";
    public override string BgMuted       => "oklch(0.935 0.008 220)";
    public override string BgEmphasized  => "oklch(0.89 0.012 220)";
    public override string BgOverlay     => "oklch(0.16 0.02 220 / 0.45)";
    public override string BgGlass       => "oklch(0.99 0.003 220 / 0.6)";
    public override string BorderGlass   => "oklch(0.87 0.015 220 / 0.3)";
    public override string BlurGlass     => "16px";

    public override string FgDefault   => "oklch(0.12 0.02 220)";
    public override string FgSubtle    => "oklch(0.34 0.015 220)";
    public override string FgMuted     => "oklch(0.50 0.012 220)";
    public override string FgDisabled  => "oklch(0.66 0.008 220)";
    public override string FgInverse   => "oklch(0.99 0.003 220)";
    public override string FgLink      => "oklch(0.57 0.24 200)";
    public override string FgLinkHover => "oklch(0.50 0.24 200)";

    public override string BorderDefault => "oklch(0.87 0.012 220)";
    public override string BorderSubtle  => "oklch(0.93 0.01 220)";
    public override string BorderStrong  => "oklch(0.80 0.015 220)";
    public override string BorderFocus   => "oklch(0.57 0.24 200)";
    public override string Divider       => "oklch(0.93 0.01 220)";

    // High-chroma cyan primary (hue 200°)
    public override string ColorPrimary        => "oklch(0.57 0.24 200)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.12 200)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.24 200)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.22 200)";

    public override string ColorSuccess        => "oklch(0.58 0.16 160)";
    public override string ColorSuccessHover   => "oklch(0.52 0.16 160)";

    public override string ColorDanger         => "oklch(0.55 0.24 350)";
    public override string ColorDangerHover    => "oklch(0.50 0.24 350)";

    public override string ColorWarning        => "oklch(0.70 0.20 50)";
    public override string ColorWarningHover   => "oklch(0.64 0.20 50)";
    public override string ColorWarningFg      => "oklch(0.12 0.02 220)";

    public override string ColorInfo           => "oklch(0.58 0.18 280)";
    public override string ColorInfoHover      => "oklch(0.52 0.18 280)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextBase => "0.875rem";
    public override string TextLg   => "1.125rem";

    public override string ShadowXs => "0 1px 1px 0 oklch(0.12 0.02 220 / 0.05)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.12 0.02 220 / 0.08), 0 1px 1px -1px oklch(0.12 0.02 220 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.12 0.02 220 / 0.10), 0 1px 2px -1px oklch(0.12 0.02 220 / 0.08)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.12 0.02 220 / 0.12), 0 2px 4px -2px oklch(0.12 0.02 220 / 0.08)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.12 0.02 220 / 0.16), 0 4px 8px -4px oklch(0.12 0.02 220 / 0.10)";

    public override string RadiusSm   => "4px";
    public override string RadiusMd   => "6px";
    public override string RadiusLg   => "8px";
    public override string RadiusXl   => "12px";

    public override string TransitionFast => "100ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "300ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.57 0.24 200)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.24 350)";
}

internal class NeonSemanticDark : BaseDarkConsistent
{
    public NeonSemanticDark() : base(200) { }

    public override string ColorPrimary        => "oklch(0.70 0.26 200)";
    public override string ColorPrimarySubtle  => "oklch(0.18 0.08 200)";
    public override string ColorPrimaryMuted   => "oklch(0.25 0.12 200)";
    public override string ColorPrimaryHover   => "oklch(0.78 0.26 200)";
    public override string ColorPrimaryActive  => "oklch(0.64 0.26 200)";
    public override string ColorPrimaryFg      => "oklch(0.04 0.015 240)";

    public override string ColorSuccess        => "oklch(0.62 0.18 160)";
    public override string ColorSuccessSubtle  => "oklch(0.16 0.05 160)";
    public override string ColorSuccessHover   => "oklch(0.68 0.18 160)";
    public override string ColorSuccessFg      => "oklch(0.04 0.015 240)";

    public override string ColorDanger         => "oklch(0.62 0.26 350)";
    public override string ColorDangerSubtle   => "oklch(0.18 0.08 350)";
    public override string ColorDangerHover    => "oklch(0.68 0.26 350)";
    public override string ColorDangerFg       => "oklch(0.04 0.015 240)";

    public override string ColorWarning        => "oklch(0.76 0.22 50)";
    public override string ColorWarningSubtle  => "oklch(0.20 0.06 50)";
    public override string ColorWarningHover   => "oklch(0.82 0.20 50)";
    public override string ColorWarningFg      => "oklch(0.04 0.015 240)";

    public override string ColorInfo           => "oklch(0.64 0.20 280)";
    public override string ColorInfoSubtle     => "oklch(0.18 0.06 280)";
    public override string ColorInfoHover      => "oklch(0.70 0.18 280)";
    public override string ColorInfoFg         => "oklch(0.04 0.015 240)";
}

internal class NeonComponents : IThemeComponents
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

    internal sealed class NeonTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Orbitron:wght@400;500;600;700;800&family=Rajdhani:wght@400;500;600;700&family=Share+Tech+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Orbitron', sans-serif";
        public HeadingSettings H1 => new("2.75rem", HeadingFont, "700", "1.0", "-0.03em");
        public HeadingSettings H2 => new("2.25rem", HeadingFont, "700", "1.05", "-0.02em");
        public HeadingSettings H3 => new("1.75rem", HeadingFont, "700", "1.1", "-0.01em");
        public HeadingSettings H4 => new("1.375rem", HeadingFont, "700", "1.15", "0");
        public HeadingSettings H5 => new("1.125rem", HeadingFont, "600", "1.2", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "600", "1.25", "0.02em");
    }
