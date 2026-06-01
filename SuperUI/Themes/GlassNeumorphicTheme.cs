namespace SuperUI.Themes;

/// <summary>
/// Glass Neumorphic — гибрид glassmorphism + neumorphism. Soft shadows вместо borders, выпуклые и вдавленные элементы.
/// </summary>
public sealed class GlassNeumorphicTheme : ThemeBase
{
    public override string Id => "glass-neumorphic";
    public override string Name => "Glass Neumorphic";
    public override string? Description => "Гибрид glassmorphism + neumorphism. Soft shadows вместо borders, выпуклые и вдавленные элементы.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new GlassNeumorphicPrimitives();
    protected override IThemeSemantic CreateLight() => new GlassNeumorphicSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GlassNeumorphicSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GlassNeumorphicComponents();
    protected override IThemeTypography? CreateTypography() => new GlassNeumorphicTypography();

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

        /* ── Glass Neumorphic — Card (convex) ── */
        [data-theme-id="glass-neumorphic"] .sgc-card {
            background: oklch(0.95 0.005 240 / 0.6);
            backdrop-filter: blur(16px) saturate(150%);
            -webkit-backdrop-filter: blur(16px) saturate(150%);
            border-radius: 20px;
            border: none;
            box-shadow:
                8px 8px 16px oklch(0.8 0.02 240 / 0.5),
                -8px -8px 16px oklch(1 0 0 / 0.8),
                inset 0 1px 0 oklch(1 0 0 / 0.4);
            transition: box-shadow 250ms ease;
        }
        [data-theme-id="glass-neumorphic"] .sgc-card:hover {
            box-shadow:
                10px 10px 20px oklch(0.78 0.02 240 / 0.55),
                -10px -10px 20px oklch(1 0 0 / 0.85),
                inset 0 1px 0 oklch(1 0 0 / 0.5);
        }

        /* ── Glass Neumorphic — Surface ── */
        [data-theme-id="glass-neumorphic"] .sgc-surface {
            background: oklch(0.94 0.005 240 / 0.55);
            backdrop-filter: blur(14px) saturate(140%);
            -webkit-backdrop-filter: blur(14px) saturate(140%);
            border-radius: 16px;
            border: none;
            box-shadow:
                6px 6px 12px oklch(0.8 0.02 240 / 0.45),
                -6px -6px 12px oklch(1 0 0 / 0.75);
        }

        /* ── Glass Neumorphic — Buttons (convex) ── */
        [data-theme-id="glass-neumorphic"] .sgc-btn {
            border-radius: 14px;
            border: none;
            font-weight: 500;
            box-shadow:
                4px 4px 10px oklch(0.8 0.02 240 / 0.4),
                -4px -4px 10px oklch(1 0 0 / 0.8);
            transition: box-shadow 150ms ease,
                        transform  100ms ease;
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn:hover {
            box-shadow:
                6px 6px 14px oklch(0.78 0.02 240 / 0.45),
                -6px -6px 14px oklch(1 0 0 / 0.85);
            transform: translateY(-1px);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn:active {
            box-shadow:
                inset 3px 3px 6px oklch(0.8 0.02 240 / 0.4),
                inset -3px -3px 6px oklch(1 0 0 / 0.7);
            transform: translateY(0);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn.sgc-btn-primary {
            background: oklch(0.55 0.14 240);
            color: oklch(1 0 0);
            box-shadow:
                4px 4px 10px oklch(0.55 0.14 240 / 0.3),
                -4px -4px 10px oklch(0.7 0.08 240 / 0.4);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: oklch(0.50 0.14 240);
            box-shadow:
                6px 6px 14px oklch(0.55 0.14 240 / 0.4),
                -6px -6px 14px oklch(0.7 0.08 240 / 0.5);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn.sgc-btn-primary:active {
            box-shadow:
                inset 3px 3px 6px oklch(0.45 0.12 240 / 0.5),
                inset -3px -3px 6px oklch(0.65 0.08 240 / 0.3);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn.sgc-btn-ghost {
            background: oklch(0.95 0.005 240 / 0.5);
            box-shadow:
                3px 3px 8px oklch(0.8 0.02 240 / 0.3),
                -3px -3px 8px oklch(1 0 0 / 0.7);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn.sgc-btn-ghost:hover {
            background: oklch(0.93 0.008 240 / 0.6);
        }
        [data-theme-id="glass-neumorphic"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow:
                0 0 0 2px oklch(0.95 0.005 240),
                0 0 0 4px oklch(0.55 0.14 240);
        }

        /* ── Glass Neumorphic — Inputs (concave/inset) ── */
        [data-theme-id="glass-neumorphic"] .sgc-input,
        [data-theme-id="glass-neumorphic"] .sgc-select,
        [data-theme-id="glass-neumorphic"] .sgc-textarea {
            background: oklch(0.93 0.005 240 / 0.5);
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            border-radius: 12px;
            border: none;
            color: oklch(0.14 0.018 240);
            box-shadow:
                inset 4px 4px 8px oklch(0.8 0.02 240 / 0.4),
                inset -4px -4px 8px oklch(1 0 0 / 0.7);
            transition: box-shadow 200ms ease;
        }
        [data-theme-id="glass-neumorphic"] .sgc-input:focus,
        [data-theme-id="glass-neumorphic"] .sgc-select:focus,
        [data-theme-id="glass-neumorphic"] .sgc-textarea:focus {
            box-shadow:
                inset 4px 4px 8px oklch(0.8 0.02 240 / 0.35),
                inset -4px -4px 8px oklch(1 0 0 / 0.65),
                0 0 0 2px oklch(0.55 0.14 240 / 0.3);
            outline: none;
        }

        /* ── Glass Neumorphic — Nav ── */
        [data-theme-id="glass-neumorphic"] .sgc-nav {
            background: oklch(0.94 0.005 240 / 0.55);
            backdrop-filter: blur(20px) saturate(150%);
            -webkit-backdrop-filter: blur(20px) saturate(150%);
            border-radius: 16px;
            border: none;
            box-shadow:
                6px 6px 14px oklch(0.8 0.02 240 / 0.4),
                -6px -6px 14px oklch(1 0 0 / 0.75);
        }
        [data-theme-id="glass-neumorphic"] .sgc-nav-link {
            border-radius: 10px;
            padding: 6px 12px;
            color: oklch(0.40 0.015 240);
            transition: background 200ms ease,
                        color      200ms ease,
                        box-shadow 200ms ease;
        }
        [data-theme-id="glass-neumorphic"] .sgc-nav-link:hover {
            background: oklch(0.55 0.14 240 / 0.08);
            color: oklch(0.20 0.018 240);
            box-shadow:
                inset 2px 2px 4px oklch(0.8 0.02 240 / 0.2),
                inset -2px -2px 4px oklch(1 0 0 / 0.5);
        }
        [data-theme-id="glass-neumorphic"] .sgc-nav-link.active {
            background: oklch(0.55 0.14 240 / 0.12);
            color: oklch(0.50 0.14 240);
            box-shadow:
                inset 2px 2px 5px oklch(0.55 0.14 240 / 0.15),
                inset -2px -2px 5px oklch(1 0 0 / 0.4);
        }

        /* ── Glass Neumorphic — Modal ── */
        [data-theme-id="glass-neumorphic"] .sgc-modal-content {
            background: oklch(0.95 0.005 240 / 0.65);
            backdrop-filter: blur(24px) saturate(160%);
            -webkit-backdrop-filter: blur(24px) saturate(160%);
            border-radius: 24px;
            border: none;
            box-shadow:
                12px 12px 24px oklch(0.75 0.02 240 / 0.4),
                -12px -12px 24px oklch(1 0 0 / 0.8),
                inset 0 1px 0 oklch(1 0 0 / 0.4);
        }

        /* ── Glass Neumorphic — Dropdown ── */
        [data-theme-id="glass-neumorphic"] .sgc-dropdown-menu {
            background: oklch(0.95 0.005 240 / 0.6);
            backdrop-filter: blur(20px) saturate(150%);
            -webkit-backdrop-filter: blur(20px) saturate(150%);
            border-radius: 14px;
            border: none;
            box-shadow:
                8px 8px 16px oklch(0.8 0.02 240 / 0.4),
                -8px -8px 16px oklch(1 0 0 / 0.75);
        }

        /* ── Glass Neumorphic — Toggle (convex) ── */
        [data-theme-id="glass-neumorphic"] .sgc-toggle-track {
            border-radius: 50px;
            border: none;
            box-shadow:
                inset 3px 3px 6px oklch(0.8 0.02 240 / 0.3),
                inset -3px -3px 6px oklch(1 0 0 / 0.6);
        }
        [data-theme-id="glass-neumorphic"] .sgc-toggle-thumb {
            box-shadow:
                2px 2px 4px oklch(0.6 0.04 240 / 0.3),
                -1px -1px 3px oklch(1 0 0 / 0.5);
        }

        /* ── Glass Neumorphic — Selection ── */
        [data-theme-id="glass-neumorphic"] ::selection {
            background: oklch(0.55 0.14 240 / 0.2);
            color: oklch(0.14 0.018 240);
        }

        /* ── Glass Neumorphic — Progress ── */
        [data-theme-id="glass-neumorphic"] .sgc-progress-fill {
            background: linear-gradient(90deg, oklch(0.55 0.14 240), oklch(0.60 0.12 260));
            border-radius: 9999px;
            box-shadow:
                inset 1px 1px 3px oklch(0.55 0.14 240 / 0.3),
                inset -1px -1px 3px oklch(0.7 0.08 240 / 0.2);
        }

        /* ── Glass Neumorphic — Scrollbar ── */
        [data-theme-id="glass-neumorphic"] ::-webkit-scrollbar-thumb {
            background: oklch(0.85 0.01 240 / 0.5);
            border-radius: 9999px;
            box-shadow:
                1px 1px 2px oklch(0.8 0.02 240 / 0.3),
                -1px -1px 2px oklch(1 0 0 / 0.5);
        }

        /* ── Glass Neumorphic — Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="glass-neumorphic"] *,
            [data-theme-id="glass-neumorphic"] *::before,
            [data-theme-id="glass-neumorphic"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class GlassNeumorphicPrimitives : IThemePrimitives
{
    // Neutral — soft violet (hue 240°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 240)";
    public virtual string Neutral100 => "oklch(0.97 0.006 240)";
    public virtual string Neutral200 => "oklch(0.93 0.008 240)";
    public virtual string Neutral300 => "oklch(0.87 0.01 240)";
    public virtual string Neutral400 => "oklch(0.76 0.012 240)";
    public virtual string Neutral500 => "oklch(0.64 0.012 240)";
    public virtual string Neutral600 => "oklch(0.52 0.014 240)";
    public virtual string Neutral700 => "oklch(0.40 0.016 240)";
    public virtual string Neutral800 => "oklch(0.28 0.018 240)";
    public virtual string Neutral900 => "oklch(0.16 0.02 240)";

    // Primary — Soft violet (hue 240°, muted)
    public virtual string Primary50  => "oklch(0.95 0.02 240)";
    public virtual string Primary100 => "oklch(0.90 0.04 240)";
    public virtual string Primary200 => "oklch(0.84 0.06 240)";
    public virtual string Primary300 => "oklch(0.76 0.08 240)";
    public virtual string Primary400 => "oklch(0.67 0.10 240)";
    public virtual string Primary500 => "oklch(0.59 0.14 240)";
    public virtual string Primary600 => "oklch(0.52 0.14 240)";
    public virtual string Primary700 => "oklch(0.44 0.12 240)";
    public virtual string Primary800 => "oklch(0.35 0.10 240)";
    public virtual string Primary900 => "oklch(0.26 0.08 240)";

    // Success — Soft teal
    public virtual string Success50  => "oklch(0.95 0.02 155)";
    public virtual string Success100 => "oklch(0.88 0.04 155)";
    public virtual string Success500 => "oklch(0.58 0.08 155)";
    public virtual string Success600 => "oklch(0.50 0.08 155)";
    public virtual string Success700 => "oklch(0.42 0.07 155)";

    // Danger — Soft rose
    public virtual string Danger50  => "oklch(0.95 0.02 5)";
    public virtual string Danger100 => "oklch(0.88 0.05 5)";
    public virtual string Danger500 => "oklch(0.55 0.12 5)";
    public virtual string Danger600 => "oklch(0.48 0.12 5)";
    public virtual string Danger700 => "oklch(0.40 0.11 5)";

    // Warning — Soft gold
    public virtual string Warning50  => "oklch(0.97 0.02 45)";
    public virtual string Warning100 => "oklch(0.92 0.04 45)";
    public virtual string Warning500 => "oklch(0.70 0.08 45)";
    public virtual string Warning600 => "oklch(0.62 0.08 45)";

    // Info — Soft periwinkle
    public virtual string Info50  => "oklch(0.95 0.02 260)";
    public virtual string Info100 => "oklch(0.88 0.04 260)";
    public virtual string Info500 => "oklch(0.58 0.08 260)";
    public virtual string Info600 => "oklch(0.50 0.08 260)";

    public virtual string FontSans  => "'Nunito', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "6px";
    public virtual string RadiusSm   => "8px";
    public virtual string RadiusMd   => "12px";
    public virtual string RadiusLg   => "16px";
    public virtual string RadiusXl   => "22px";
    public virtual string Radius2Xl  => "30px";
    public virtual string RadiusFull => "9999px";
}

internal class GlassNeumorphicSemanticLight : BaseLightConsistent
{
    public GlassNeumorphicSemanticLight() : base(240) { }

    public override string BgDefault     => "oklch(0.96 0.005 240)";
    public override string BgSubtle      => "oklch(0.94 0.006 240)";
    public override string BgMuted       => "oklch(0.92 0.008 240)";
    public override string BgEmphasized  => "oklch(0.88 0.012 240)";
    public override string BgOverlay     => "oklch(0.16 0.02 240 / 0.35)";
    public override string BgGlass       => "oklch(0.95 0.005 240 / 0.6)";
    public override string BorderGlass   => "oklch(0.95 0.005 240 / 0.6)";
    public override string BlurGlass     => "16px";

    public override string Surface         => "oklch(0.94 0.005 240 / 0.55)";
    public override string SurfaceRaised   => "oklch(0.95 0.005 240 / 0.65)";
    public override string SurfaceOverlay  => "oklch(0.96 0.005 240 / 0.75)";

    public override string FgDefault   => "oklch(0.14 0.018 240)";
    public override string FgSubtle    => "oklch(0.36 0.015 240)";
    public override string FgMuted     => "oklch(0.52 0.012 240)";
    public override string FgDisabled  => "oklch(0.68 0.008 240)";
    public override string FgInverse   => "oklch(0.99 0.003 240)";
    public override string FgLink      => "oklch(0.52 0.14 240)";
    public override string FgLinkHover => "oklch(0.44 0.14 240)";

    public override string BorderDefault => "oklch(0.95 0.005 240 / 0.55)";
    public override string BorderSubtle  => "oklch(0.95 0.005 240 / 0.45)";
    public override string BorderStrong  => "oklch(0.90 0.01 240 / 0.5)";
    public override string BorderFocus   => "oklch(0.55 0.14 240)";
    public override string Divider       => "oklch(0.95 0.005 240 / 0.4)";

    public override string ColorPrimary        => "oklch(0.55 0.14 240)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.14 240)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.12 240)";

    public override string ColorSuccess        => "oklch(0.58 0.08 155)";
    public override string ColorSuccessHover   => "oklch(0.52 0.08 155)";

    public override string ColorDanger         => "oklch(0.55 0.12 5)";
    public override string ColorDangerHover    => "oklch(0.50 0.12 5)";

    public override string ColorWarning        => "oklch(0.70 0.08 45)";
    public override string ColorWarningHover   => "oklch(0.64 0.08 45)";
    public override string ColorWarningFg      => "oklch(0.14 0.018 240)";

    public override string ColorInfo           => "oklch(0.58 0.08 260)";
    public override string ColorInfoHover      => "oklch(0.52 0.08 260)";

    public override string Font     => "'Nunito', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string FocusRing       => "0 0 0 2px oklch(0.96 0.005 240), 0 0 0 4px oklch(0.55 0.14 240)";
    public override string FocusRingDanger => "0 0 0 2px oklch(0.96 0.005 240), 0 0 0 4px oklch(0.55 0.12 5)";
}

internal class GlassNeumorphicSemanticDark : BaseDarkConsistent
{
    public GlassNeumorphicSemanticDark() : base(240) { }

    public override string BgGlass       => "oklch(0.18 0.02 240 / 0.55)";
    public override string BorderGlass   => "oklch(0.18 0.02 240 / 0.55)";
    public override string BlurGlass     => "16px";

    public override string Surface         => "oklch(0.16 0.02 240 / 0.5)";
    public override string SurfaceRaised   => "oklch(0.20 0.025 240 / 0.55)";
    public override string SurfaceOverlay  => "oklch(0.24 0.03 240 / 0.6)";

    public override string ColorPrimary        => "oklch(0.62 0.14 240)";
    public override string ColorPrimarySubtle  => "oklch(0.28 0.05 240 / 0.40)";
    public override string ColorPrimaryMuted   => "oklch(0.38 0.08 240 / 0.50)";
    public override string ColorPrimaryHover   => "oklch(0.70 0.14 240)";
    public override string ColorPrimaryActive  => "oklch(0.55 0.14 240)";
    public override string ColorPrimaryFg      => "oklch(0.06 0.01 250)";

    public override string ColorSuccess        => "oklch(0.60 0.08 155)";
    public override string ColorSuccessSubtle  => "oklch(0.25 0.03 155 / 0.40)";
    public override string ColorSuccessHover   => "oklch(0.66 0.08 155)";
    public override string ColorSuccessFg      => "oklch(0.06 0.01 250)";

    public override string ColorDanger         => "oklch(0.60 0.12 5)";
    public override string ColorDangerSubtle   => "oklch(0.25 0.04 5 / 0.40)";
    public override string ColorDangerHover    => "oklch(0.66 0.12 5)";
    public override string ColorDangerFg       => "oklch(0.06 0.01 250)";

    public override string ColorWarning        => "oklch(0.74 0.08 45)";
    public override string ColorWarningSubtle  => "oklch(0.28 0.03 45 / 0.40)";
    public override string ColorWarningHover   => "oklch(0.80 0.06 45)";
    public override string ColorWarningFg      => "oklch(0.06 0.01 250)";

    public override string ColorInfo           => "oklch(0.62 0.08 260)";
    public override string ColorInfoSubtle     => "oklch(0.25 0.03 260 / 0.40)";
    public override string ColorInfoHover      => "oklch(0.68 0.07 260)";
    public override string ColorInfoFg         => "oklch(0.06 0.01 250)";
}

internal class GlassNeumorphicComponents : IThemeComponents
{
    public virtual string BtnRadius     => "14px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "500";
    public virtual string BtnHeight     => "32px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "36px";

    public virtual string InputRadius   => "12px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "32px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "36px";

    public virtual string CardRadius      => "20px";
    public virtual string CardPadding     => "16px";
    public virtual string CardBorderColor => "oklch(0.95 0.005 240 / 0.55)";
    public virtual string CardBg          => "oklch(0.95 0.005 240 / 0.6)";

    public virtual string ModalRadius => "24px";

    public virtual string TableRadius          => "14px";
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "260px";

    public virtual string HeaderBg    => "oklch(0.94 0.005 240 / 0.55)";
    public virtual string HeaderFg    => "oklch(0.14 0.018 240)";
    public virtual string NavBg       => "oklch(0.94 0.005 240 / 0.55)";
    public virtual string NavFg       => "oklch(0.40 0.015 240)";
    public virtual string NavActiveBg => "oklch(0.55 0.14 240 / 0.12)";
    public virtual string NavActiveFg => "oklch(0.50 0.14 240)";
}

internal sealed class GlassNeumorphicTypography : IThemeTypography
{
    public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Nunito:wght@300;400;500;600;700&display=swap";
    public bool EmbedGoogleFontsImport => true;
    public string? HeadingFont => "'Nunito', sans-serif";
    public HeadingSettings H1 => new("2.25rem", HeadingFont, "600", "1.1", "-0.015em");
    public HeadingSettings H2 => new("1.875rem", HeadingFont, "600", "1.15", "-0.01em");
    public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "0");
    public HeadingSettings H4 => new("1.25rem", HeadingFont, "500", "1.25", "0");
    public HeadingSettings H5 => new("1.125rem", HeadingFont, "500", "1.3", "0");
    public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
}
