namespace SuperUI.Themes;

/// <summary>
/// Signature — фирменная SuperUI тема. Deep navy primary, тёплые комфортные neutrals.
/// Светлая: «Кремовый navy», тёмная: «Угольный премиум».
/// </summary>
public sealed class SignatureTheme : ThemeBase
{
    public override string Id => "signature";
    public override string Name => "Signature";
    public override string? Description => "Фирменная тема SuperUI. Deep navy с тёплыми комфортными neutrals. Светлая — «Кремовый navy». Тёмная — «Угольный премиум».";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new SignaturePrimitives();
    protected override IThemeSemantic CreateLight() => new SignatureSemanticLight();
    protected override IThemeSemantic? CreateDark() => new SignatureSemanticDark();
    protected override IThemeComponents? CreateComponents() => new SignatureComponents();
    protected override IThemeTypography? CreateTypography() => new SignatureTypography();

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

        [data-theme-id="signature"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
        }

        /* ── Signature — Cards ── */
        [data-theme-id="signature"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: 8px;
            box-shadow: var(--sg-shadow-sm);
            transition: border-color 250ms ease,
                        box-shadow   250ms ease;
        }
        [data-theme-id="signature"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Signature — Buttons ── */
        [data-theme-id="signature"] .sgc-btn {
            border-radius: 6px;
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color 200ms ease,
                        border-color     200ms ease,
                        box-shadow       200ms ease,
                        transform        100ms ease;
        }
        [data-theme-id="signature"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="signature"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="signature"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Signature — Inputs ── */
        [data-theme-id="signature"] .sgc-input,
        [data-theme-id="signature"] .sgc-select,
        [data-theme-id="signature"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: 6px;
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color 200ms ease,
                        box-shadow   200ms ease;
        }
        [data-theme-id="signature"] .sgc-input:focus,
        [data-theme-id="signature"] .sgc-select:focus,
        [data-theme-id="signature"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            box-shadow: 0 0 0 1px var(--sg-color-primary-muted);
            outline: none;
        }

        /* ── Signature — Nav ── */
        [data-theme-id="signature"] .sgc-nav-link {
            border-radius: 6px;
            padding: 4px 10px;
            font-size: 0.8125rem;
            color: var(--sg-fg-subtle);
            transition: background 200ms ease,
                        color      200ms ease;
        }
        [data-theme-id="signature"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="signature"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* ── Signature — Tabs ── */
        [data-theme-id="signature"] .sgc-tabs-strip {
            border-bottom: 1px solid var(--sg-border-subtle);
        }
        [data-theme-id="signature"] .sgc-tab.active,
        [data-theme-id="signature"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            background: var(--sg-color-primary-subtle);
        }

        /* ── Signature — Progress ── */
        [data-theme-id="signature"] .sgc-progress-fill {
            background: linear-gradient(90deg, var(--sg-color-primary), var(--sg-color-info));
            border-radius: 9999px;
        }

        /* ── Signature — Selection ── */
        [data-theme-id="signature"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Signature — Scrollbar ── */
        [data-theme-id="signature"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 9999px;
        }

        /* ── Signature — Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="signature"] *,
            [data-theme-id="signature"] *::before,
            [data-theme-id="signature"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;
}

internal class SignaturePrimitives : IThemePrimitives
{
    // Neutral — warm cream (hue 45°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 45)";
    public virtual string Neutral100 => "oklch(0.97 0.006 45)";
    public virtual string Neutral200 => "oklch(0.93 0.009 45)";
    public virtual string Neutral300 => "oklch(0.87 0.012 45)";
    public virtual string Neutral400 => "oklch(0.76 0.014 45)";
    public virtual string Neutral500 => "oklch(0.64 0.014 45)";
    public virtual string Neutral600 => "oklch(0.52 0.016 45)";
    public virtual string Neutral700 => "oklch(0.40 0.018 45)";
    public virtual string Neutral800 => "oklch(0.28 0.02 45)";
    public virtual string Neutral900 => "oklch(0.16 0.022 45)";

    // Primary — Deep Navy (hue 230°)
    public virtual string Primary50  => "oklch(0.95 0.025 230)";
    public virtual string Primary100 => "oklch(0.90 0.05 230)";
    public virtual string Primary200 => "oklch(0.84 0.075 230)";
    public virtual string Primary300 => "oklch(0.76 0.10 230)";
    public virtual string Primary400 => "oklch(0.67 0.12 230)";
    public virtual string Primary500 => "oklch(0.58 0.12 230)";
    public virtual string Primary600 => "oklch(0.51 0.11 230)";
    public virtual string Primary700 => "oklch(0.43 0.10 230)";
    public virtual string Primary800 => "oklch(0.34 0.09 230)";
    public virtual string Primary900 => "oklch(0.25 0.07 230)";

    // Success — Emerald
    public virtual string Success50  => "oklch(0.95 0.025 150)";
    public virtual string Success100 => "oklch(0.88 0.055 150)";
    public virtual string Success500 => "oklch(0.58 0.12 150)";
    public virtual string Success600 => "oklch(0.50 0.12 150)";
    public virtual string Success700 => "oklch(0.42 0.11 150)";

    // Danger — Crimson (hue 10°)
    public virtual string Danger50  => "oklch(0.95 0.035 10)";
    public virtual string Danger100 => "oklch(0.88 0.08 10)";
    public virtual string Danger500 => "oklch(0.55 0.18 10)";
    public virtual string Danger600 => "oklch(0.48 0.18 10)";
    public virtual string Danger700 => "oklch(0.40 0.17 10)";

    // Warning — Warm amber (hue 50°)
    public virtual string Warning50  => "oklch(0.97 0.03 50)";
    public virtual string Warning100 => "oklch(0.92 0.06 50)";
    public virtual string Warning500 => "oklch(0.70 0.14 50)";
    public virtual string Warning600 => "oklch(0.62 0.14 50)";

    // Info — Sky blue (hue 200°)
    public virtual string Info50  => "oklch(0.95 0.03 200)";
    public virtual string Info100 => "oklch(0.88 0.06 200)";
    public virtual string Info500 => "oklch(0.58 0.14 200)";
    public virtual string Info600 => "oklch(0.50 0.14 200)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string Radius2Xl  => "24px";
    public virtual string RadiusFull => "9999px";
}

internal class SignatureSemanticLight : IThemeSemantic
{
    // Light — «Кремовый navy»
    public virtual string BgDefault     => "oklch(0.99 0.005 45)";
    public virtual string BgSubtle      => "oklch(0.97 0.008 45)";
    public virtual string BgMuted       => "oklch(0.935 0.012 45)";
    public virtual string BgEmphasized  => "oklch(0.89 0.016 45)";
    public virtual string BgOverlay     => "oklch(0.16 0.02 45 / 0.40)";
    public virtual string BgGlass       => "oklch(0.99 0.005 45 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.87 0.015 45 / 0.3)";
    public virtual string BlurGlass     => "12px";

    public virtual string Surface         => "oklch(1 0 0)";
    public virtual string SurfaceRaised   => "oklch(1 0 0)";
    public virtual string SurfaceOverlay  => "oklch(1 0 0)";

    public virtual string FgDefault   => "oklch(0.14 0.02 45)";
    public virtual string FgSubtle    => "oklch(0.36 0.015 45)";
    public virtual string FgMuted     => "oklch(0.52 0.012 45)";
    public virtual string FgDisabled  => "oklch(0.68 0.008 45)";
    public virtual string FgInverse   => "oklch(0.99 0.005 45)";
    public virtual string FgLink      => "oklch(0.58 0.12 230)";
    public virtual string FgLinkHover => "oklch(0.52 0.12 230)";

    public virtual string BorderDefault => "oklch(0.87 0.012 45)";
    public virtual string BorderSubtle  => "oklch(0.93 0.01 45)";
    public virtual string BorderStrong  => "oklch(0.80 0.015 45)";
    public virtual string BorderFocus   => "oklch(0.58 0.12 230)";
    public virtual string Divider       => "oklch(0.93 0.01 45)";

    public virtual string ColorPrimary        => "oklch(0.58 0.12 230)";
    public virtual string ColorPrimarySubtle  => "oklch(0.94 0.035 230)";
    public virtual string ColorPrimaryMuted   => "oklch(0.85 0.07 230)";
    public virtual string ColorPrimaryHover   => "oklch(0.52 0.12 230)";
    public virtual string ColorPrimaryActive  => "oklch(0.46 0.11 230)";
    public virtual string ColorPrimaryFg      => "oklch(0.99 0 0)";

    public virtual string ColorSuccess        => "oklch(0.58 0.12 150)";
    public virtual string ColorSuccessSubtle  => "oklch(0.94 0.03 150)";
    public virtual string ColorSuccessHover   => "oklch(0.52 0.12 150)";
    public virtual string ColorSuccessFg      => "oklch(0.99 0 0)";

    public virtual string ColorDanger         => "oklch(0.55 0.18 10)";
    public virtual string ColorDangerSubtle   => "oklch(0.94 0.045 10)";
    public virtual string ColorDangerHover    => "oklch(0.50 0.18 10)";
    public virtual string ColorDangerFg       => "oklch(0.99 0 0)";

    public virtual string ColorWarning        => "oklch(0.70 0.14 50)";
    public virtual string ColorWarningSubtle  => "oklch(0.96 0.04 50)";
    public virtual string ColorWarningHover   => "oklch(0.64 0.14 50)";
    public virtual string ColorWarningFg      => "oklch(0.14 0.02 45)";

    public virtual string ColorInfo           => "oklch(0.58 0.14 200)";
    public virtual string ColorInfoSubtle     => "oklch(0.94 0.035 200)";
    public virtual string ColorInfoHover      => "oklch(0.52 0.14 200)";
    public virtual string ColorInfoFg         => "oklch(0.99 0 0)";

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

    public virtual string ShadowXs => "0 1px 1px 0 oklch(0.14 0.02 45 / 0.04)";
    public virtual string ShadowSm => "0 1px 2px 0 oklch(0.14 0.02 45 / 0.06), 0 1px 1px -1px oklch(0.14 0.02 45 / 0.06)";
    public virtual string ShadowMd => "0 2px 4px -1px oklch(0.14 0.02 45 / 0.08), 0 1px 2px -1px oklch(0.14 0.02 45 / 0.06)";
    public virtual string ShadowLg => "0 8px 16px -4px oklch(0.14 0.02 45 / 0.10), 0 2px 4px -2px oklch(0.14 0.02 45 / 0.06)";
    public virtual string ShadowXl => "0 16px 32px -8px oklch(0.14 0.02 45 / 0.14), 0 4px 8px -4px oklch(0.14 0.02 45 / 0.08)";

    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "100ms ease";
    public virtual string TransitionBase => "200ms ease";
    public virtual string TransitionSlow => "300ms ease";

    public virtual string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.58 0.12 230)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.18 10)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class SignatureSemanticDark : IThemeSemantic
{
    // Dark — «Угольный премиум»
    public virtual string BgDefault     => "oklch(0.10 0.01 45)";
    public virtual string BgSubtle      => "oklch(0.18 0.015 45)";
    public virtual string BgMuted       => "oklch(0.15 0.012 45)";
    public virtual string BgEmphasized  => "oklch(0.22 0.018 45)";
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => "oklch(0.10 0.01 45 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.08)";
    public virtual string BlurGlass     => "16px";

    public virtual string Surface         => "oklch(0.13 0.015 45)";
    public virtual string SurfaceRaised   => "oklch(0.15 0.018 45)";
    public virtual string SurfaceOverlay  => "oklch(0.15 0.018 45)";

    public virtual string FgDefault   => "oklch(0.95 0.005 45)";
    public virtual string FgSubtle    => "oklch(0.82 0.01 45)";
    public virtual string FgMuted     => "oklch(0.62 0.015 45)";
    public virtual string FgDisabled  => "oklch(0.40 0.015 45)";
    public virtual string FgInverse   => "oklch(0.10 0.01 45)";
    public virtual string FgLink      => "oklch(0.65 0.14 230)";
    public virtual string FgLinkHover => "oklch(0.72 0.14 230)";

    public virtual string BorderDefault => "oklch(0.25 0.02 45)";
    public virtual string BorderSubtle  => "oklch(0.18 0.015 45)";
    public virtual string BorderStrong  => "oklch(0.30 0.025 45)";
    public virtual string BorderFocus   => "oklch(0.65 0.14 230)";
    public virtual string Divider       => "oklch(0.18 0.015 45)";

    public virtual string ColorPrimary        => "oklch(0.65 0.14 230)";
    public virtual string ColorPrimarySubtle  => "oklch(0.20 0.05 230)";
    public virtual string ColorPrimaryMuted   => "oklch(0.28 0.08 230)";
    public virtual string ColorPrimaryHover   => "oklch(0.72 0.14 230)";
    public virtual string ColorPrimaryActive  => "oklch(0.59 0.14 230)";
    public virtual string ColorPrimaryFg      => "oklch(0.10 0.01 45)";

    public virtual string ColorSuccess        => "oklch(0.60 0.10 150)";
    public virtual string ColorSuccessSubtle  => "oklch(0.18 0.03 150)";
    public virtual string ColorSuccessHover   => "oklch(0.66 0.10 150)";
    public virtual string ColorSuccessFg      => "oklch(0.95 0.005 45)";

    public virtual string ColorDanger         => "oklch(0.60 0.16 10)";
    public virtual string ColorDangerSubtle   => "oklch(0.20 0.045 10)";
    public virtual string ColorDangerHover    => "oklch(0.66 0.16 10)";
    public virtual string ColorDangerFg       => "oklch(0.95 0.005 45)";

    public virtual string ColorWarning        => "oklch(0.74 0.14 50)";
    public virtual string ColorWarningSubtle  => "oklch(0.22 0.04 50)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.12 50)";
    public virtual string ColorWarningFg      => "oklch(0.10 0.01 45)";

    public virtual string ColorInfo           => "oklch(0.62 0.12 200)";
    public virtual string ColorInfoSubtle     => "oklch(0.20 0.04 200)";
    public virtual string ColorInfoHover      => "oklch(0.68 0.11 200)";
    public virtual string ColorInfoFg         => "oklch(0.95 0.005 45)";

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

    public virtual string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.40)";
    public virtual string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.50)";
    public virtual string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.55)";
    public virtual string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.60)";
    public virtual string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.65)";

    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "100ms ease";
    public virtual string TransitionBase => "200ms ease";
    public virtual string TransitionSlow => "300ms ease";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.10 0.01 45), 0 0 0 4px oklch(0.65 0.14 230)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.10 0.01 45), 0 0 0 4px oklch(0.60 0.16 10)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class SignatureComponents : IThemeComponents
{
    public virtual string BtnRadius     => "6px";
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "34px";

    public virtual string InputRadius   => "6px";
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "34px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "10px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "12px";

    public virtual string TableRadius          => "6px";
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

    internal sealed class SignatureTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Playfair+Display:ital,wght@0,400;0,500;0,600;0,700;1,400&family=Inter:wght@400;500;600;700&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Playfair Display', serif";
        public HeadingSettings H1 => new("3rem", HeadingFont, "700", "1.05", "-0.025em");
        public HeadingSettings H2 => new("2.375rem", HeadingFont, "700", "1.1", "-0.015em");
        public HeadingSettings H3 => new("1.875rem", HeadingFont, "600", "1.15", "-0.01em");
        public HeadingSettings H4 => new("1.5rem", HeadingFont, "600", "1.2", "0");
        public HeadingSettings H5 => new("1.25rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H6 => new("0.9375rem", HeadingFont, "500", "1.3", "0.01em");
    }
