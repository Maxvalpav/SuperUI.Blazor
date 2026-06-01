namespace SuperUI.Themes;

/// <summary>
/// Glass Light — плотное frosted glass на светлом фоне. Мягкие тени, холодный голубой оттенок, полупрозрачные панели.
/// </summary>
public sealed class GlassLightTheme : ThemeBase
{
    public override string Id => "glass-light";
    public override string Name => "Glass Light";
    public override string? Description => "Плотное frosted glass на светлом фоне. Мягкие тени, холодный голубой оттенок, полупрозрачные панели.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new GlassLightPrimitives();
    protected override IThemeSemantic CreateLight() => new GlassLightSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GlassLightSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GlassLightComponents();
    protected override IThemeTypography? CreateTypography() => new GlassLightTypography();

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

        /* ── Glass Light — Card ── */
        [data-theme-id="glass-light"] .sgc-card {
            background: oklch(0.97 0.005 220 / 0.72);
            backdrop-filter: blur(24px) saturate(180%);
            -webkit-backdrop-filter: blur(24px) saturate(180%);
            border: 1px solid oklch(1 0 0 / 0.45);
            border-radius: 16px;
            box-shadow:
                0 4px 24px oklch(0.5 0.02 220 / 0.08),
                inset 0 1px 0 oklch(1 0 0 / 0.6);
            transition: border-color 250ms ease,
                        box-shadow   250ms ease;
        }
        [data-theme-id="glass-light"] .sgc-card:hover {
            border-color: oklch(0.85 0.02 220 / 0.5);
            box-shadow:
                0 8px 32px oklch(0.5 0.02 220 / 0.12),
                inset 0 1px 0 oklch(1 0 0 / 0.7);
        }

        /* ── Glass Light — Surface ── */
        [data-theme-id="glass-light"] .sgc-surface {
            background: oklch(0.98 0.003 220 / 0.65);
            backdrop-filter: blur(20px) saturate(160%);
            -webkit-backdrop-filter: blur(20px) saturate(160%);
            border: 1px solid oklch(1 0 0 / 0.4);
            border-radius: 14px;
        }

        /* ── Glass Light — Buttons ── */
        [data-theme-id="glass-light"] .sgc-btn {
            border-radius: 10px;
            font-weight: 500;
            backdrop-filter: blur(16px);
            -webkit-backdrop-filter: blur(16px);
            border: 1px solid oklch(1 0 0 / 0.3);
            box-shadow: 0 2px 8px oklch(0.5 0.02 220 / 0.06);
            transition: background-color 200ms ease,
                        border-color     200ms ease,
                        box-shadow       200ms ease,
                        transform        100ms ease;
        }
        [data-theme-id="glass-light"] .sgc-btn:hover {
            box-shadow: 0 6px 20px oklch(0.5 0.02 220 / 0.12);
            transform: translateY(-1px);
        }
        [data-theme-id="glass-light"] .sgc-btn.sgc-btn-primary {
            background: oklch(0.55 0.16 220);
            border: 1px solid oklch(0.50 0.16 220);
            color: oklch(1 0 0);
            box-shadow: 0 2px 12px oklch(0.55 0.16 220 / 0.3);
        }
        [data-theme-id="glass-light"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: oklch(0.50 0.16 220);
            border-color: oklch(0.45 0.16 220);
            box-shadow: 0 4px 20px oklch(0.55 0.16 220 / 0.4);
        }
        [data-theme-id="glass-light"] .sgc-btn.sgc-btn-ghost {
            background: oklch(0.98 0.003 220 / 0.5);
            border: 1px solid oklch(1 0 0 / 0.3);
        }
        [data-theme-id="glass-light"] .sgc-btn.sgc-btn-ghost:hover {
            background: oklch(0.95 0.005 220 / 0.6);
        }
        [data-theme-id="glass-light"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px oklch(0.97 0.005 220),
                        0 0 0 4px oklch(0.55 0.16 220);
        }

        /* ── Glass Light — Inputs ── */
        [data-theme-id="glass-light"] .sgc-input,
        [data-theme-id="glass-light"] .sgc-select,
        [data-theme-id="glass-light"] .sgc-textarea {
            background: oklch(0.98 0.003 220 / 0.6);
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            border: 1px solid oklch(1 0 0 / 0.4);
            border-radius: 10px;
            color: oklch(0.14 0.018 220);
            transition: border-color 200ms ease,
                        box-shadow   200ms ease;
        }
        [data-theme-id="glass-light"] .sgc-input:focus,
        [data-theme-id="glass-light"] .sgc-select:focus,
        [data-theme-id="glass-light"] .sgc-textarea:focus {
            border-color: oklch(0.55 0.16 220);
            box-shadow: 0 0 0 2px oklch(0.55 0.16 220 / 0.15);
            background: oklch(1 0 0 / 0.8);
            outline: none;
        }

        /* ── Glass Light — Nav ── */
        [data-theme-id="glass-light"] .sgc-nav {
            background: oklch(0.97 0.004 220 / 0.6);
            backdrop-filter: blur(24px) saturate(160%);
            -webkit-backdrop-filter: blur(24px) saturate(160%);
            border: 1px solid oklch(1 0 0 / 0.35);
        }
        [data-theme-id="glass-light"] .sgc-nav-link {
            border-radius: 8px;
            padding: 6px 12px;
            color: oklch(0.40 0.015 220);
            transition: background 200ms ease,
                        color      200ms ease;
        }
        [data-theme-id="glass-light"] .sgc-nav-link:hover {
            background: oklch(0.55 0.16 220 / 0.08);
            color: oklch(0.20 0.018 220);
        }
        [data-theme-id="glass-light"] .sgc-nav-link.active {
            background: oklch(0.55 0.16 220 / 0.12);
            color: oklch(0.50 0.16 220);
        }

        /* ── Glass Light — Modal ── */
        [data-theme-id="glass-light"] .sgc-modal-content {
            background: oklch(0.98 0.003 220 / 0.75);
            backdrop-filter: blur(32px) saturate(200%);
            -webkit-backdrop-filter: blur(32px) saturate(200%);
            border: 1px solid oklch(1 0 0 / 0.5);
            border-radius: 16px;
            box-shadow:
                0 16px 48px oklch(0.5 0.02 220 / 0.15),
                inset 0 1px 0 oklch(1 0 0 / 0.6);
        }

        /* ── Glass Light — Dropdown ── */
        [data-theme-id="glass-light"] .sgc-dropdown-menu {
            background: oklch(0.98 0.003 220 / 0.7);
            backdrop-filter: blur(24px) saturate(180%);
            -webkit-backdrop-filter: blur(24px) saturate(180%);
            border: 1px solid oklch(1 0 0 / 0.4);
            border-radius: 12px;
            box-shadow: 0 8px 24px oklch(0.5 0.02 220 / 0.1);
        }

        /* ── Glass Light — Selection ── */
        [data-theme-id="glass-light"] ::selection {
            background: oklch(0.55 0.16 220 / 0.2);
            color: oklch(0.14 0.018 220);
        }

        /* ── Glass Light — Progress ── */
        [data-theme-id="glass-light"] .sgc-progress-fill {
            background: linear-gradient(90deg, oklch(0.55 0.16 220), oklch(0.50 0.18 200));
            border-radius: 9999px;
        }

        /* ── Glass Light — Scrollbar ── */
        [data-theme-id="glass-light"] ::-webkit-scrollbar-thumb {
            background: oklch(0.85 0.01 220 / 0.5);
            border-radius: 9999px;
        }
        [data-theme-id="glass-light"] ::-webkit-scrollbar-thumb:hover {
            background: oklch(0.75 0.015 220 / 0.6);
        }

        /* ── Glass Light — Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="glass-light"] *,
            [data-theme-id="glass-light"] *::before,
            [data-theme-id="glass-light"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class GlassLightPrimitives : IThemePrimitives
{
    // Neutral — cool blue (hue 220°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 220)";
    public virtual string Neutral100 => "oklch(0.97 0.006 220)";
    public virtual string Neutral200 => "oklch(0.93 0.008 220)";
    public virtual string Neutral300 => "oklch(0.87 0.01 220)";
    public virtual string Neutral400 => "oklch(0.76 0.012 220)";
    public virtual string Neutral500 => "oklch(0.64 0.012 220)";
    public virtual string Neutral600 => "oklch(0.52 0.014 220)";
    public virtual string Neutral700 => "oklch(0.40 0.016 220)";
    public virtual string Neutral800 => "oklch(0.28 0.018 220)";
    public virtual string Neutral900 => "oklch(0.16 0.02 220)";

    // Primary — Cold blue (hue 220°)
    public virtual string Primary50  => "oklch(0.95 0.025 220)";
    public virtual string Primary100 => "oklch(0.90 0.05 220)";
    public virtual string Primary200 => "oklch(0.84 0.075 220)";
    public virtual string Primary300 => "oklch(0.76 0.10 220)";
    public virtual string Primary400 => "oklch(0.67 0.13 220)";
    public virtual string Primary500 => "oklch(0.59 0.16 220)";
    public virtual string Primary600 => "oklch(0.52 0.16 220)";
    public virtual string Primary700 => "oklch(0.44 0.14 220)";
    public virtual string Primary800 => "oklch(0.35 0.12 220)";
    public virtual string Primary900 => "oklch(0.26 0.10 220)";

    // Success — Soft teal
    public virtual string Success50  => "oklch(0.95 0.02 155)";
    public virtual string Success100 => "oklch(0.88 0.045 155)";
    public virtual string Success500 => "oklch(0.58 0.10 155)";
    public virtual string Success600 => "oklch(0.50 0.10 155)";
    public virtual string Success700 => "oklch(0.42 0.09 155)";

    // Danger — Soft rose
    public virtual string Danger50  => "oklch(0.95 0.025 5)";
    public virtual string Danger100 => "oklch(0.88 0.06 5)";
    public virtual string Danger500 => "oklch(0.55 0.14 5)";
    public virtual string Danger600 => "oklch(0.48 0.14 5)";
    public virtual string Danger700 => "oklch(0.40 0.13 5)";

    // Warning — Soft gold
    public virtual string Warning50  => "oklch(0.97 0.02 45)";
    public virtual string Warning100 => "oklch(0.92 0.04 45)";
    public virtual string Warning500 => "oklch(0.70 0.10 45)";
    public virtual string Warning600 => "oklch(0.62 0.10 45)";

    // Info — Periwinkle
    public virtual string Info50  => "oklch(0.95 0.025 260)";
    public virtual string Info100 => "oklch(0.88 0.05 260)";
    public virtual string Info500 => "oklch(0.58 0.10 260)";
    public virtual string Info600 => "oklch(0.50 0.10 260)";

    public virtual string FontSans  => "'Plus Jakarta Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "4px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "10px";
    public virtual string RadiusLg   => "14px";
    public virtual string RadiusXl   => "20px";
    public virtual string Radius2Xl  => "28px";
    public virtual string RadiusFull => "9999px";
}

internal class GlassLightSemanticLight : BaseLightConsistent
{
    public GlassLightSemanticLight() : base(220) { }

    public override string BgDefault     => "oklch(0.98 0.004 220)";
    public override string BgSubtle      => "oklch(0.96 0.006 220)";
    public override string BgMuted       => "oklch(0.93 0.008 220)";
    public override string BgEmphasized  => "oklch(0.88 0.012 220)";
    public override string BgOverlay     => "oklch(0.16 0.02 220 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.003 220 / 0.72)";
    public override string BorderGlass   => "oklch(1 0 0 / 0.45)";
    public override string BlurGlass     => "24px";

    public override string Surface         => "oklch(1 0 0 / 0.50)";
    public override string SurfaceRaised   => "oklch(1 0 0 / 0.60)";
    public override string SurfaceOverlay  => "oklch(1 0 0 / 0.70)";

    public override string FgDefault   => "oklch(0.14 0.018 220)";
    public override string FgSubtle    => "oklch(0.36 0.015 220)";
    public override string FgMuted     => "oklch(0.52 0.012 220)";
    public override string FgDisabled  => "oklch(0.68 0.008 220)";
    public override string FgInverse   => "oklch(0.99 0.003 220)";
    public override string FgLink      => "oklch(0.50 0.16 220)";
    public override string FgLinkHover => "oklch(0.44 0.16 220)";

    public override string BorderDefault => "oklch(0.87 0.012 220)";
    public override string BorderSubtle  => "oklch(1 0 0 / 0.40)";
    public override string BorderStrong  => "oklch(0.80 0.015 220)";
    public override string BorderFocus   => "oklch(0.55 0.16 220)";
    public override string Divider       => "oklch(1 0 0 / 0.35)";

    public override string ColorPrimary        => "oklch(0.55 0.16 220)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.16 220)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.15 220)";

    public override string ColorSuccess        => "oklch(0.58 0.10 155)";
    public override string ColorSuccessHover   => "oklch(0.52 0.10 155)";

    public override string ColorDanger         => "oklch(0.55 0.14 5)";
    public override string ColorDangerHover    => "oklch(0.50 0.14 5)";

    public override string ColorWarning        => "oklch(0.70 0.10 45)";
    public override string ColorWarningHover   => "oklch(0.64 0.10 45)";
    public override string ColorWarningFg      => "oklch(0.14 0.018 220)";

    public override string ColorInfo           => "oklch(0.58 0.10 260)";
    public override string ColorInfoHover      => "oklch(0.52 0.10 260)";

    public override string Font     => "'Plus Jakarta Sans', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string ShadowXs => "0 1px 1px 0 oklch(0.5 0.02 220 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.5 0.02 220 / 0.06), 0 1px 1px -1px oklch(0.5 0.02 220 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.5 0.02 220 / 0.08), 0 1px 2px -1px oklch(0.5 0.02 220 / 0.06)";
    public override string ShadowLg => "0 4px 24px oklch(0.5 0.02 220 / 0.10), 0 2px 4px -2px oklch(0.5 0.02 220 / 0.06)";
    public override string ShadowXl => "0 8px 32px oklch(0.5 0.02 220 / 0.14), 0 4px 8px -4px oklch(0.5 0.02 220 / 0.08)";

    public override string RadiusSm   => "6px";
    public override string RadiusMd   => "10px";
    public override string RadiusLg   => "14px";
    public override string RadiusXl   => "20px";

    public override string TransitionFast => "120ms ease";
    public override string TransitionBase => "200ms ease";
    public override string TransitionSlow => "350ms ease";

    public override string FocusRing       => "0 0 0 2px oklch(0.98 0.004 220), 0 0 0 4px oklch(0.55 0.16 220)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.98 0.004 220), 0 0 0 4px oklch(0.55 0.14 5)";
}

internal class GlassLightSemanticDark : BaseDarkConsistent
{
    public GlassLightSemanticDark() : base(220) { }

    public override string BgGlass       => "oklch(0.99 0 0 / 0.08)";
    public override string BorderGlass   => "oklch(0.99 0 0 / 0.10)";
    public override string BlurGlass     => "20px";

    public override string Surface         => "oklch(0.99 0 0 / 0.05)";
    public override string SurfaceRaised   => "oklch(0.99 0 0 / 0.07)";
    public override string SurfaceOverlay  => "oklch(0.99 0 0 / 0.09)";

    public override string ColorPrimary        => "oklch(0.65 0.14 220)";
    public override string ColorPrimarySubtle  => "oklch(0.30 0.06 220 / 0.40)";
    public override string ColorPrimaryMuted   => "oklch(0.40 0.08 220 / 0.50)";
    public override string ColorPrimaryHover   => "oklch(0.72 0.14 220)";
    public override string ColorPrimaryActive  => "oklch(0.59 0.14 220)";
    public override string ColorPrimaryFg      => "oklch(0.06 0.01 230)";

    public override string ColorSuccess        => "oklch(0.60 0.10 155)";
    public override string ColorSuccessSubtle  => "oklch(0.25 0.04 155 / 0.40)";
    public override string ColorSuccessHover   => "oklch(0.66 0.10 155)";
    public override string ColorSuccessFg      => "oklch(0.06 0.01 230)";

    public override string ColorDanger         => "oklch(0.60 0.14 5)";
    public override string ColorDangerSubtle   => "oklch(0.25 0.05 5 / 0.40)";
    public override string ColorDangerHover    => "oklch(0.66 0.14 5)";
    public override string ColorDangerFg       => "oklch(0.06 0.01 230)";

    public override string ColorWarning        => "oklch(0.74 0.10 45)";
    public override string ColorWarningSubtle  => "oklch(0.28 0.04 45 / 0.40)";
    public override string ColorWarningHover   => "oklch(0.80 0.08 45)";
    public override string ColorWarningFg      => "oklch(0.06 0.01 230)";

    public override string ColorInfo           => "oklch(0.62 0.10 260)";
    public override string ColorInfoSubtle     => "oklch(0.25 0.04 260 / 0.40)";
    public override string ColorInfoHover      => "oklch(0.68 0.09 260)";
    public override string ColorInfoFg         => "oklch(0.06 0.01 230)";
}

internal class GlassLightComponents : IThemeComponents
{
    public virtual string BtnRadius     => "10px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "500";
    public virtual string BtnHeight     => "32px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "36px";

    public virtual string InputRadius   => "10px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "32px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "36px";

    public virtual string CardRadius      => "16px";
    public virtual string CardPadding     => "16px";
    public virtual string CardBorderColor => "oklch(1 0 0 / 0.45)";
    public virtual string CardBg          => "oklch(0.99 0.003 220 / 0.72)";

    public virtual string ModalRadius => "16px";

    public virtual string TableRadius          => "10px";
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "260px";

    public virtual string HeaderBg    => "oklch(0.97 0.004 220 / 0.65)";
    public virtual string HeaderFg    => "oklch(0.14 0.018 220)";
    public virtual string NavBg       => "oklch(0.97 0.004 220 / 0.6)";
    public virtual string NavFg       => "oklch(0.40 0.015 220)";
    public virtual string NavActiveBg => "oklch(0.55 0.16 220 / 0.12)";
    public virtual string NavActiveFg => "oklch(0.50 0.16 220)";
}

internal sealed class GlassLightTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@300;400;500;600;700&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Plus Jakarta Sans', sans-serif";
    public HeadingSettings H1 => new("2.25rem", HeadingFont, "600", "1.1", "-0.015em");
    public HeadingSettings H2 => new("1.875rem", HeadingFont, "600", "1.15", "-0.01em");
    public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "0");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "500", "1.25", "0");
    public HeadingSettings H5 => new("1.125rem", HeadingFont, "500", "1.3", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
}
