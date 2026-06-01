namespace SuperUI.Themes;

/// <summary>
/// Window — тема в стиле Windows 11. Mica и Acrylic материалы, Segoe UI Variable, Reveal Highlight, слоистая глубина.
/// </summary>
public sealed class WindowTheme : ThemeBase
{
    public override string Id => "window";
    public override string Name => "Window";
    public override string? Description => "Тема в стиле Windows 11. Mica и Acrylic материалы, Segoe UI Variable, Reveal Highlight, слоистая глубина.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new WindowPrimitives();
    protected override IThemeSemantic CreateLight() => new WindowSemanticLight();
    protected override IThemeSemantic? CreateDark() => new WindowSemanticDark();
    protected override IThemeComponents? CreateComponents() => new WindowComponents();
    protected override IThemeTypography? CreateTypography() => new WindowTypography();

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
            --sui-hover-bg:    rgba(0, 0, 0, 0.04);
            --sui-active-bg:   rgba(0, 0, 0, 0.06);
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

        /* ── Window — Card (Mica material) ── */
        [data-theme-id="window"] .sgc-card {
            background: oklch(0.97 0.004 220 / 0.8);
            backdrop-filter: blur(30px) saturate(150%);
            -webkit-backdrop-filter: blur(30px) saturate(150%);
            border: 1px solid oklch(0 0 0 / 0.06);
            border-radius: 8px;
            box-shadow: 0 2px 4px oklch(0 0 0 / 0.04);
            transition: border-color 200ms ease,
                        box-shadow   200ms ease;
        }
        [data-theme-id="window"] .sgc-card:hover {
            border-color: oklch(0.55 0.15 220 / 0.15);
            box-shadow: 0 2px 8px oklch(0 0 0 / 0.06);
        }

        /* ── Window — Surface ── */
        [data-theme-id="window"] .sgc-surface {
            background: oklch(0.97 0.003 220 / 0.75);
            backdrop-filter: blur(24px) saturate(140%);
            -webkit-backdrop-filter: blur(24px) saturate(140%);
            border: 1px solid oklch(0 0 0 / 0.05);
            border-radius: 6px;
        }

        /* ── Window — Buttons ── */
        [data-theme-id="window"] .sgc-btn {
            border-radius: 4px;
            font-weight: 500;
            border: 1px solid oklch(0 0 0 / 0.06);
            background: oklch(0.96 0.003 220 / 0.9);
            box-shadow: 0 1px 2px oklch(0 0 0 / 0.04);
            transition: background 100ms ease,
                        border-color 100ms ease,
                        box-shadow 100ms ease;
        }
        [data-theme-id="window"] .sgc-btn:hover {
            background: oklch(0.93 0.005 220 / 0.9);
            border-color: oklch(0 0 0 / 0.1);
        }
        [data-theme-id="window"] .sgc-btn:active {
            background: oklch(0.90 0.006 220 / 0.9);
            box-shadow: inset 0 1px 2px oklch(0 0 0 / 0.06);
        }
        [data-theme-id="window"] .sgc-btn.sgc-btn-primary {
            background: oklch(0.55 0.17 250);
            border-color: oklch(0.50 0.17 250);
            color: oklch(1 0 0);
            box-shadow: 0 1px 2px oklch(0 0 0 / 0.1);
        }
        [data-theme-id="window"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: oklch(0.50 0.17 250);
            border-color: oklch(0.45 0.17 250);
        }
        [data-theme-id="window"] .sgc-btn.sgc-btn-primary:active {
            background: oklch(0.45 0.17 250);
            box-shadow: inset 0 1px 2px oklch(0.40 0.15 250 / 0.3);
        }
        [data-theme-id="window"] .sgc-btn.sgc-btn-ghost {
            background: transparent;
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="window"] .sgc-btn.sgc-btn-ghost:hover {
            background: oklch(0 0 0 / 0.04);
            border-color: transparent;
        }
        [data-theme-id="window"] .sgc-btn:focus-visible {
            outline: 2px solid oklch(0.55 0.17 250);
            outline-offset: 1px;
        }

        /* ── Window — Inputs ── */
        [data-theme-id="window"] .sgc-input,
        [data-theme-id="window"] .sgc-select,
        [data-theme-id="window"] .sgc-textarea {
            background: oklch(1 0 0 / 0.85);
            border: 1px solid oklch(0 0 0 / 0.12);
            border-radius: 4px;
            color: oklch(0.14 0.018 220);
            box-shadow: inset 0 1px 2px oklch(0 0 0 / 0.04);
            transition: border-color 150ms ease,
                        box-shadow   150ms ease;
        }
        [data-theme-id="window"] .sgc-input:focus,
        [data-theme-id="window"] .sgc-select:focus,
        [data-theme-id="window"] .sgc-textarea:focus {
            border-color: oklch(0.55 0.17 250);
            box-shadow: inset 0 1px 2px oklch(0 0 0 / 0.04),
                        0 0 0 1px oklch(0.55 0.17 250 / 0.3);
            outline: none;
        }

        /* ── Window — Nav (Mica sidebar) ── */
        [data-theme-id="window"] .sgc-nav {
            background: oklch(0.96 0.004 220 / 0.85);
            backdrop-filter: blur(30px) saturate(150%);
            -webkit-backdrop-filter: blur(30px) saturate(150%);
            border-right: 1px solid oklch(0 0 0 / 0.06);
        }
        [data-theme-id="window"] .sgc-nav-link {
            border-radius: 4px;
            padding: 5px 10px;
            color: oklch(0.35 0.015 220);
            transition: background 150ms ease,
                        color      150ms ease;
        }
        [data-theme-id="window"] .sgc-nav-link:hover {
            background: oklch(0 0 0 / 0.04);
            color: oklch(0.14 0.018 220);
        }
        [data-theme-id="window"] .sgc-nav-link.active {
            background: oklch(0.55 0.17 250 / 0.08);
            color: oklch(0.50 0.17 250);
        }

        /* ── Window — Modal (Acrylic) ── */
        [data-theme-id="window"] .sgc-modal-content {
            background: oklch(0.96 0.003 220 / 0.75);
            backdrop-filter: blur(20px) saturate(120%);
            -webkit-backdrop-filter: blur(20px) saturate(120%);
            border: 1px solid oklch(0 0 0 / 0.08);
            border-radius: 8px;
            box-shadow:
                0 8px 32px oklch(0 0 0 / 0.12),
                0 2px 4px oklch(0 0 0 / 0.04);
        }

        /* ── Window — Dropdown (Acrylic) ── */
        [data-theme-id="window"] .sgc-dropdown-menu {
            background: oklch(0.96 0.003 220 / 0.75);
            backdrop-filter: blur(20px) saturate(120%);
            -webkit-backdrop-filter: blur(20px) saturate(120%);
            border: 1px solid oklch(0 0 0 / 0.08);
            border-radius: 8px;
            box-shadow: 0 8px 32px oklch(0 0 0 / 0.12);
        }

        /* ── Window — Tabs ── */
        [data-theme-id="window"] .sgc-tabs-list {
            border-bottom: 1px solid oklch(0 0 0 / 0.06);
        }
        [data-theme-id="window"] .sgc-tab {
            border-radius: 4px 4px 0 0;
            border: 1px solid transparent;
            border-bottom: none;
            padding: 6px 12px;
            transition: background 150ms ease,
                        border-color 150ms ease;
        }
        [data-theme-id="window"] .sgc-tab:hover {
            background: oklch(0 0 0 / 0.03);
        }
        [data-theme-id="window"] .sgc-tab[aria-selected="true"],
        [data-theme-id="window"] .sgc-tab.active {
            border-color: oklch(0 0 0 / 0.06);
            border-bottom-color: transparent;
            background: oklch(1 0 0 / 0.5);
        }

        /* ── Window — Selection ── */
        [data-theme-id="window"] ::selection {
            background: oklch(0.55 0.17 250 / 0.2);
            color: oklch(0.14 0.018 220);
        }

        /* ── Window — Progress ── */
        [data-theme-id="window"] .sgc-progress-fill {
            background: oklch(0.55 0.17 250);
            border-radius: 9999px;
        }

        /* ── Window — Scrollbar ── */
        [data-theme-id="window"] ::-webkit-scrollbar-thumb {
            background: oklch(0 0 0 / 0.2);
            border-radius: 9999px;
            border: 3px solid transparent;
            background-clip: content-box;
        }
        [data-theme-id="window"] ::-webkit-scrollbar-thumb:hover {
            background: oklch(0 0 0 / 0.35);
            background-clip: content-box;
        }

        /* ── Window — Toggle ── */
        [data-theme-id="window"] .sgc-toggle-track {
            background: oklch(0.85 0.02 220);
            border-radius: 50px;
            border: 1px solid oklch(0 0 0 / 0.06);
        }
        [data-theme-id="window"] .sgc-toggle-thumb {
            background: oklch(1 0 0);
            border-radius: 50%;
            box-shadow: 0 1px 3px oklch(0 0 0 / 0.15);
        }

        /* ── Window — Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="window"] *,
            [data-theme-id="window"] *::before,
            [data-theme-id="window"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class WindowPrimitives : IThemePrimitives
{
    // Neutral — Windows gray (hue 220°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 220)";
    public virtual string Neutral100 => "oklch(0.97 0.004 220)";
    public virtual string Neutral200 => "oklch(0.93 0.005 220)";
    public virtual string Neutral300 => "oklch(0.87 0.007 220)";
    public virtual string Neutral400 => "oklch(0.76 0.008 220)";
    public virtual string Neutral500 => "oklch(0.64 0.008 220)";
    public virtual string Neutral600 => "oklch(0.52 0.01 220)";
    public virtual string Neutral700 => "oklch(0.40 0.012 220)";
    public virtual string Neutral800 => "oklch(0.28 0.014 220)";
    public virtual string Neutral900 => "oklch(0.16 0.016 220)";

    // Primary — Windows blue (hue 250°, #0078D4)
    public virtual string Primary50  => "oklch(0.95 0.03 250)";
    public virtual string Primary100 => "oklch(0.90 0.06 250)";
    public virtual string Primary200 => "oklch(0.84 0.09 250)";
    public virtual string Primary300 => "oklch(0.76 0.12 250)";
    public virtual string Primary400 => "oklch(0.67 0.15 250)";
    public virtual string Primary500 => "oklch(0.55 0.17 250)";
    public virtual string Primary600 => "oklch(0.48 0.17 250)";
    public virtual string Primary700 => "oklch(0.40 0.15 250)";
    public virtual string Primary800 => "oklch(0.32 0.12 250)";
    public virtual string Primary900 => "oklch(0.24 0.10 250)";

    // Success — Windows green
    public virtual string Success50  => "oklch(0.95 0.025 155)";
    public virtual string Success100 => "oklch(0.88 0.05 155)";
    public virtual string Success500 => "oklch(0.60 0.14 155)";
    public virtual string Success600 => "oklch(0.52 0.14 155)";
    public virtual string Success700 => "oklch(0.44 0.12 155)";

    // Danger — Windows red
    public virtual string Danger50  => "oklch(0.95 0.03 10)";
    public virtual string Danger100 => "oklch(0.88 0.07 10)";
    public virtual string Danger500 => "oklch(0.60 0.18 10)";
    public virtual string Danger600 => "oklch(0.52 0.18 10)";
    public virtual string Danger700 => "oklch(0.44 0.16 10)";

    // Warning — Windows yellow
    public virtual string Warning50  => "oklch(0.97 0.025 80)";
    public virtual string Warning100 => "oklch(0.92 0.05 80)";
    public virtual string Warning500 => "oklch(0.75 0.12 80)";
    public virtual string Warning600 => "oklch(0.67 0.12 80)";

    // Info — Windows teal
    public virtual string Info50  => "oklch(0.95 0.025 180)";
    public virtual string Info100 => "oklch(0.88 0.05 180)";
    public virtual string Info500 => "oklch(0.60 0.12 180)";
    public virtual string Info600 => "oklch(0.52 0.12 180)";

    public virtual string FontSans  => "'Segoe UI Variable', 'Segoe UI', -apple-system, BlinkMacSystemFont, Roboto, 'Helvetica Neue', Arial, sans-serif";
    public virtual string FontMono  => "'Cascadia Code', 'Cascadia Mono', 'Consolas', 'Courier New', monospace";
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

internal class WindowSemanticLight : BaseLightConsistent
{
    public WindowSemanticLight() : base(220) { }

    public override string BgDefault     => "oklch(0.97 0.004 220)";
    public override string BgSubtle      => "oklch(0.95 0.005 220)";
    public override string BgMuted       => "oklch(0.93 0.006 220)";
    public override string BgEmphasized  => "oklch(0.88 0.01 220)";
    public override string BgOverlay     => "oklch(0.16 0.02 220 / 0.35)";
    public override string BgGlass       => "oklch(0.97 0.004 220 / 0.8)";
    public override string BorderGlass   => "oklch(0 0 0 / 0.06)";
    public override string BlurGlass     => "30px";

    public override string Surface         => "oklch(1 0 0 / 0.75)";
    public override string SurfaceRaised   => "oklch(1 0 0 / 0.85)";
    public override string SurfaceOverlay  => "oklch(1 0 0 / 0.9)";

    public override string FgDefault   => "oklch(0.14 0.018 220)";
    public override string FgSubtle    => "oklch(0.35 0.015 220)";
    public override string FgMuted     => "oklch(0.50 0.012 220)";
    public override string FgDisabled  => "oklch(0.68 0.008 220)";
    public override string FgInverse   => "oklch(0.99 0.003 220)";
    public override string FgLink      => "oklch(0.50 0.17 250)";
    public override string FgLinkHover => "oklch(0.44 0.17 250)";

    public override string BorderDefault => "oklch(0 0 0 / 0.06)";
    public override string BorderSubtle  => "oklch(0 0 0 / 0.04)";
    public override string BorderStrong  => "oklch(0 0 0 / 0.12)";
    public override string BorderFocus   => "oklch(0.55 0.17 250)";
    public override string Divider       => "oklch(0 0 0 / 0.06)";

    public override string ColorPrimary        => "oklch(0.55 0.17 250)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.17 250)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.16 250)";

    public override string ColorSuccess        => "oklch(0.60 0.14 155)";
    public override string ColorSuccessHover   => "oklch(0.54 0.14 155)";

    public override string ColorDanger         => "oklch(0.60 0.18 10)";
    public override string ColorDangerHover    => "oklch(0.54 0.18 10)";

    public override string ColorWarning        => "oklch(0.75 0.12 80)";
    public override string ColorWarningHover   => "oklch(0.68 0.12 80)";
    public override string ColorWarningFg      => "oklch(0.14 0.018 220)";

    public override string ColorInfo           => "oklch(0.60 0.12 180)";
    public override string ColorInfoHover      => "oklch(0.54 0.12 180)";

    public override string Font     => "'Segoe UI Variable', 'Segoe UI', system-ui, -apple-system, Roboto, 'Helvetica Neue', Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string FocusRing       => "2px solid oklch(0.55 0.17 250)";
    public override string FocusRingDanger => "2px solid oklch(0.60 0.18 10)";
}

internal class WindowSemanticDark : BaseDarkConsistent
{
    public WindowSemanticDark() : base(220) { }

    public override string BgGlass       => "oklch(0.16 0.012 220 / 0.8)";
    public override string BorderGlass   => "oklch(1 0 0 / 0.06)";
    public override string BlurGlass     => "30px";

    public override string Surface         => "oklch(0.18 0.01 220 / 0.7)";
    public override string SurfaceRaised   => "oklch(0.22 0.012 220 / 0.75)";
    public override string SurfaceOverlay  => "oklch(0.26 0.015 220 / 0.8)";

    public override string ColorPrimary        => "oklch(0.62 0.17 250)";
    public override string ColorPrimarySubtle  => "oklch(0.28 0.06 250 / 0.40)";
    public override string ColorPrimaryMuted   => "oklch(0.38 0.10 250 / 0.50)";
    public override string ColorPrimaryHover   => "oklch(0.70 0.17 250)";
    public override string ColorPrimaryActive  => "oklch(0.55 0.17 250)";
    public override string ColorPrimaryFg      => "oklch(0.06 0.01 260)";

    public override string ColorSuccess        => "oklch(0.65 0.14 155)";
    public override string ColorSuccessSubtle  => "oklch(0.25 0.05 155 / 0.40)";
    public override string ColorSuccessHover   => "oklch(0.72 0.14 155)";
    public override string ColorSuccessFg      => "oklch(0.06 0.01 260)";

    public override string ColorDanger         => "oklch(0.65 0.18 10)";
    public override string ColorDangerSubtle   => "oklch(0.25 0.06 10 / 0.40)";
    public override string ColorDangerHover    => "oklch(0.72 0.18 10)";
    public override string ColorDangerFg       => "oklch(0.06 0.01 260)";

    public override string ColorWarning        => "oklch(0.80 0.12 80)";
    public override string ColorWarningSubtle  => "oklch(0.28 0.04 80 / 0.40)";
    public override string ColorWarningHover   => "oklch(0.86 0.10 80)";
    public override string ColorWarningFg      => "oklch(0.06 0.01 260)";

    public override string ColorInfo           => "oklch(0.65 0.12 180)";
    public override string ColorInfoSubtle     => "oklch(0.25 0.05 180 / 0.40)";
    public override string ColorInfoHover      => "oklch(0.72 0.10 180)";
    public override string ColorInfoFg         => "oklch(0.06 0.01 260)";
}

internal class WindowComponents : IThemeComponents
{
    public virtual string BtnRadius     => "4px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "500";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "26px";
    public virtual string BtnHeightLg   => "34px";

    public virtual string InputRadius   => "4px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "26px";
    public virtual string InputHeightLg => "34px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "12px";
    public virtual string CardBorderColor => "oklch(0 0 0 / 0.06)";
    public virtual string CardBg          => "oklch(0.97 0.004 220 / 0.8)";

    public virtual string ModalRadius => "8px";

    public virtual string TableRadius          => "6px";
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "240px";

    public virtual string HeaderBg    => "oklch(0.96 0.004 220 / 0.85)";
    public virtual string HeaderFg    => "oklch(0.14 0.018 220)";
    public virtual string NavBg       => "oklch(0.96 0.004 220 / 0.85)";
    public virtual string NavFg       => "oklch(0.35 0.015 220)";
    public virtual string NavActiveBg => "oklch(0.55 0.17 250 / 0.08)";
    public virtual string NavActiveFg => "oklch(0.50 0.17 250)";
}

internal sealed class WindowTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "";
    public bool EmbedGoogleFontsImport => false;
    public string? HeadingFont => "'Segoe UI Variable', 'Segoe UI', sans-serif";
    public HeadingSettings H1 => new("2.25rem", HeadingFont, "600", "1.1", "-0.015em");
    public HeadingSettings H2 => new("1.875rem", HeadingFont, "600", "1.15", "-0.01em");
    public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "0");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "500", "1.25", "0");
    public HeadingSettings H5 => new("1.125rem", HeadingFont, "500", "1.3", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
}
