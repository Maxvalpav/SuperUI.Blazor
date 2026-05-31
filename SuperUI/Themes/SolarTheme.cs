namespace SuperUI.Themes;

/// <summary>
/// Solaris — солнечная дизайн-система на природных пропорциях φ.
/// Светлая: «Золотой день» — песчано-кремовая гамма с янтарным акцентом.
/// Тёмная: «Ночное небо» — уголь с янтарным свечением.
/// </summary>
public sealed class SolarisTheme : ThemeBase
{
    public override string Id => "solaris";
    public override string Name => "Solaris";
    public override string? Description => "Солнечная дизайн-система: φ-пропорции, янтарная гамма. Светлая — «Золотой день» (песчано-кремовая). Тёмная — «Ночное небо» (уголь с янтарным свечением).";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new SolarisPrimitives();
    protected override IThemeSemantic CreateLight() => new SolarisSemanticLight();
    protected override IThemeSemantic? CreateDark() => new SolarisSemanticDark();
    protected override IThemeComponents? CreateComponents() => new SolarisComponents();
    protected override IThemeTypography? CreateTypography() => new SolarisTypography();

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
            --sg-text-xs: 0.75rem;
            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);
        }

        /* ── Solaris — Golden ratio helpers ── */
        :root,
        [data-theme-id="solaris"] {
            --natura-phi: 1.618033988749895;
            --natura-phi-inv: 0.618033988749895;
            --natura-golden-angle: 137.50776405003785deg;

            /* Warm φ modular typography scale (base 16px) */
            --natura-text-micro:   0.625rem;
            --natura-text-caption: 0.75rem;
            --natura-text-small:   0.8125rem;
            --natura-text-body:    1rem;
            --natura-text-h4:      1.625rem;
            --natura-text-h3:      2.625rem;
            --natura-text-h2:      4.25rem;
            --natura-text-h1:      6.875rem;

            /* Fibonacci spacing (compact) */
            --natura-space-1: 3px;
            --natura-space-2: 5px;
            --natura-space-3: 8px;
            --natura-space-4: 13px;
            --natura-space-5: 21px;
            --natura-space-6: 34px;
            --natura-space-7: 55px;
            --natura-space-8: 89px;
            --natura-space-9: 144px;

            --natura-ease-growth: cubic-bezier(0.19, 1, 0.22, 1);
            --natura-ease-breath: cubic-bezier(0.37, 0, 0.63, 1);
            --natura-ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
            --natura-ease-settle: cubic-bezier(0.22, 1, 0.36, 1);
        }

        /* ── Solaris — warm tone layer ── */
        [data-theme-id="solaris"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Cards — warm, sunlit ── */
        [data-theme-id="solaris"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--natura-space-2, 5px);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color 250ms var(--natura-ease-growth),
                        box-shadow   250ms var(--natura-ease-growth);
            transform: translateZ(0);
        }
        [data-theme-id="solaris"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: 0 4px 16px oklch(0.62 0.16 65 / 0.12);
        }

        /* ── Buttons — warm press ── */
        [data-theme-id="solaris"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 5px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color 200ms var(--natura-ease-growth),
                        border-color     200ms var(--natura-ease-growth),
                        color            200ms var(--natura-ease-growth),
                        box-shadow       200ms var(--natura-ease-growth),
                        transform        120ms var(--natura-ease-spring);
        }
        [data-theme-id="solaris"] .sgc-btn:hover:not(:disabled) {
            transform: translateY(-1px);
        }
        [data-theme-id="solaris"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.97);
            transition-duration: 80ms;
        }
        [data-theme-id="solaris"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="solaris"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px oklch(0.62 0.16 65 / 0.30),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="solaris"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
            border-radius: 2px;
        }

        /* ── Inputs — warm focus glow ── */
        [data-theme-id="solaris"] .sgc-input,
        [data-theme-id="solaris"] .sgc-select,
        [data-theme-id="solaris"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--natura-space-1, 3px);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color 200ms var(--natura-ease-growth),
                        box-shadow   200ms var(--natura-ease-growth);
        }
        [data-theme-id="solaris"] .sgc-input:focus,
        [data-theme-id="solaris"] .sgc-select:focus,
        [data-theme-id="solaris"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 2px 0 0 var(--sg-color-primary),
                        0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }

        /* ── Navigation — warm amber glow ── */
        [data-theme-id="solaris"] .sgc-nav-link {
            border-radius: var(--natura-space-1, 3px);
            padding: var(--natura-space-1) var(--natura-space-3);
            font-size: 0.8125rem;
            color: var(--sg-fg-subtle);
            transition: background 200ms var(--natura-ease-growth),
                        color      200ms var(--natura-ease-growth);
        }
        [data-theme-id="solaris"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="solaris"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 2px 0 0 0 var(--sg-color-primary);
        }

        /* ── Tabs — warm underline ── */
        [data-theme-id="solaris"] .sgc-tabs-strip {
            border-bottom: 1px solid var(--sg-border-subtle);
        }
        [data-theme-id="solaris"] .sgc-tab.active,
        [data-theme-id="solaris"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            background: var(--sg-color-primary-subtle);
        }

        /* ── Progress — amber fill ── */
        [data-theme-id="solaris"] .sgc-progress-fill {
            background: linear-gradient(90deg, var(--sg-color-primary), var(--sg-color-warning));
        }

        /* ── Selection — warm tint ── */
        [data-theme-id="solaris"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Scrollbar — warm ── */
        [data-theme-id="solaris"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 9999px;
        }

        /* ── Skeleton — warm shimmer ── */
        [data-theme-id="solaris"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: solaris-shimmer 1.5s var(--natura-ease-breath) infinite;
            border-radius: 2px;
        }
        @keyframes solaris-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Reduced motion ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="solaris"] *,
            [data-theme-id="solaris"] *::before,
            [data-theme-id="solaris"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
            [data-theme-id="solaris"] .sgc-btn:hover:not(:disabled) {
                transform: none !important;
            }
            [data-theme-id="solaris"] .sgc-btn:active:not(:disabled) {
                transform: none !important;
            }
        }
        """;
}

internal class SolarisPrimitives : IThemePrimitives
{
    // Neutral — warm sand scale (hue 50°)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 50)";
    public virtual string Neutral100 => "oklch(0.97 0.006 50)";
    public virtual string Neutral200 => "oklch(0.93 0.009 50)";
    public virtual string Neutral300 => "oklch(0.87 0.012 50)";
    public virtual string Neutral400 => "oklch(0.76 0.014 50)";
    public virtual string Neutral500 => "oklch(0.64 0.014 50)";
    public virtual string Neutral600 => "oklch(0.52 0.016 50)";
    public virtual string Neutral700 => "oklch(0.40 0.018 50)";
    public virtual string Neutral800 => "oklch(0.28 0.02 50)";
    public virtual string Neutral900 => "oklch(0.16 0.022 50)";

    // Primary — Golden Amber (hue 65°)
    public virtual string Primary50  => "oklch(0.95 0.03 65)";
    public virtual string Primary100 => "oklch(0.90 0.06 65)";
    public virtual string Primary200 => "oklch(0.84 0.10 65)";
    public virtual string Primary300 => "oklch(0.76 0.14 65)";
    public virtual string Primary400 => "oklch(0.68 0.16 65)";
    public virtual string Primary500 => "oklch(0.62 0.16 65)";
    public virtual string Primary600 => "oklch(0.55 0.15 65)";
    public virtual string Primary700 => "oklch(0.47 0.14 65)";
    public virtual string Primary800 => "oklch(0.38 0.13 65)";
    public virtual string Primary900 => "oklch(0.28 0.11 65)";

    // Success — Sage green (hue 145°, lower chroma)
    public virtual string Success50  => "oklch(0.95 0.02 145)";
    public virtual string Success100 => "oklch(0.88 0.05 145)";
    public virtual string Success500 => "oklch(0.60 0.12 145)";
    public virtual string Success600 => "oklch(0.52 0.12 145)";
    public virtual string Success700 => "oklch(0.44 0.11 145)";

    // Danger — Terracotta (hue 18°)
    public virtual string Danger50  => "oklch(0.95 0.04 18)";
    public virtual string Danger100 => "oklch(0.88 0.09 18)";
    public virtual string Danger500 => "oklch(0.55 0.20 18)";
    public virtual string Danger600 => "oklch(0.48 0.20 18)";
    public virtual string Danger700 => "oklch(0.40 0.19 18)";

    // Warning — Saffron (hue 50°, warmer gold)
    public virtual string Warning50  => "oklch(0.97 0.03 50)";
    public virtual string Warning100 => "oklch(0.92 0.06 50)";
    public virtual string Warning500 => "oklch(0.70 0.16 50)";
    public virtual string Warning600 => "oklch(0.62 0.16 50)";

    // Info — Honey orange (hue 35°)
    public virtual string Info50  => "oklch(0.95 0.03 35)";
    public virtual string Info100 => "oklch(0.88 0.06 35)";
    public virtual string Info500 => "oklch(0.58 0.14 35)";
    public virtual string Info600 => "oklch(0.50 0.14 35)";

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

internal class SolarisSemanticLight : BaseLightConsistent
{
    public SolarisSemanticLight() : base(50) { }

    // Warm cream bg (hue 50°) — close to base warm (60°) but slightly more golden
    public override string BgDefault     => "oklch(0.99 0.005 50)";
    public override string BgSubtle      => "oklch(0.97 0.008 50)";
    public override string BgMuted       => "oklch(0.935 0.012 50)";
    public override string BgEmphasized  => "oklch(0.89 0.016 50)";
    public override string BgOverlay     => "oklch(0.15 0.02 50 / 0.40)";
    public override string BgGlass       => "oklch(0.99 0.005 50 / 0.7)";
    public override string BorderGlass   => "oklch(0.87 0.015 50 / 0.3)";

    public override string FgDefault   => "oklch(0.14 0.02 50)";
    public override string FgSubtle    => "oklch(0.36 0.015 50)";
    public override string FgMuted     => "oklch(0.52 0.012 50)";
    public override string FgDisabled  => "oklch(0.68 0.008 50)";
    public override string FgInverse   => "oklch(0.99 0.005 50)";
    public override string FgLink      => "oklch(0.62 0.16 65)";
    public override string FgLinkHover => "oklch(0.56 0.16 65)";

    public override string BorderDefault => "oklch(0.87 0.012 50)";
    public override string BorderSubtle  => "oklch(0.93 0.01 50)";
    public override string BorderStrong  => "oklch(0.80 0.015 50)";
    public override string BorderFocus   => "oklch(0.62 0.16 65)";
    public override string Divider       => "oklch(0.93 0.01 50)";

    // Golden primary — warm amber at hue 65°
    public override string ColorPrimary        => "oklch(0.62 0.16 65)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 65)";
    public override string ColorPrimaryHover   => "oklch(0.56 0.16 65)";
    public override string ColorPrimaryActive  => "oklch(0.50 0.15 65)";

    public override string ColorSuccess        => "oklch(0.60 0.12 145)";
    public override string ColorSuccessHover   => "oklch(0.54 0.12 145)";

    public override string ColorDanger         => "oklch(0.55 0.20 18)";
    public override string ColorDangerHover    => "oklch(0.50 0.20 18)";

    public override string ColorWarning        => "oklch(0.70 0.16 50)";
    public override string ColorWarningHover   => "oklch(0.64 0.16 50)";
    public override string ColorWarningFg      => "oklch(0.14 0.02 50)";

    public override string ColorInfo           => "oklch(0.58 0.14 35)";
    public override string ColorInfoHover      => "oklch(0.52 0.14 35)";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "1rem";
    public override string TextLg   => "1.25rem";

    public override string FocusRing       => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.62 0.16 65)";
    public override string FocusRingDanger => "0 0 0 2px oklch(1 0 0), 0 0 0 4px oklch(0.55 0.20 18)";
}

internal class SolarisSemanticDark : IThemeSemantic
{
    // Dark — «Ночное небо»: глубокий уголь с янтарным свечением (hue 50°)
    public virtual string BgDefault     => "oklch(0.10 0.010 50)";  // уголь
    public virtual string BgSubtle      => "oklch(0.18 0.015 50)";  // тлеющие угли
    public virtual string BgMuted       => "oklch(0.15 0.012 50)";  // пепел
    public virtual string BgEmphasized  => "oklch(0.22 0.018 50)";  // искра
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => "oklch(0.10 0.010 50 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.08)";
    public virtual string BlurGlass     => "16px";

    public virtual string Surface         => "oklch(0.13 0.015 50)";   // обсидиан
    public virtual string SurfaceRaised   => "oklch(0.15 0.018 50)";   // тёплый камень
    public virtual string SurfaceOverlay  => "oklch(0.15 0.018 50)";

    public virtual string FgDefault   => "oklch(0.95 0.005 50)";    // звёздный свет
    public virtual string FgSubtle    => "oklch(0.82 0.01 50)";     // сумерки
    public virtual string FgMuted     => "oklch(0.62 0.015 50)";    // туман
    public virtual string FgDisabled  => "oklch(0.40 0.015 50)";
    public virtual string FgInverse   => "oklch(0.10 0.010 50)";
    public virtual string FgLink      => "oklch(0.68 0.18 65)";     // янтарь
    public virtual string FgLinkHover => "oklch(0.74 0.18 65)";     // яркий янтарь

    public virtual string BorderDefault => "oklch(0.25 0.02 50)";    // угольная грань
    public virtual string BorderSubtle  => "oklch(0.18 0.015 50)";   // тень
    public virtual string BorderStrong  => "oklch(0.30 0.025 50)";
    public virtual string BorderFocus   => "oklch(0.68 0.18 65)";    // янтарное свечение
    public virtual string Divider       => "oklch(0.18 0.015 50)";

    public virtual string ColorPrimary        => "oklch(0.68 0.18 65)";   // янтарь в темноте
    public virtual string ColorPrimarySubtle  => "oklch(0.20 0.05 65)";   // разогретый уголь
    public virtual string ColorPrimaryMuted   => "oklch(0.28 0.08 65)";   // тлеющий
    public virtual string ColorPrimaryHover   => "oklch(0.74 0.18 65)";   // вспышка
    public virtual string ColorPrimaryActive  => "oklch(0.62 0.18 65)";   // пульс
    public virtual string ColorPrimaryFg      => "oklch(0.10 0.010 50)";

    // Success — sage (приглушённый, вечерний)
    public virtual string ColorSuccess        => "oklch(0.60 0.10 145)";
    public virtual string ColorSuccessSubtle  => "oklch(0.18 0.03 145)";
    public virtual string ColorSuccessHover   => "oklch(0.66 0.10 145)";
    public virtual string ColorSuccessFg      => "oklch(0.95 0.005 50)";

    // Danger — terracotta (отблеск костра)
    public virtual string ColorDanger         => "oklch(0.60 0.18 18)";
    public virtual string ColorDangerSubtle   => "oklch(0.20 0.05 18)";
    public virtual string ColorDangerHover    => "oklch(0.66 0.18 18)";
    public virtual string ColorDangerFg       => "oklch(0.95 0.005 50)";

    // Warning — amber
    public virtual string ColorWarning        => "oklch(0.74 0.16 50)";
    public virtual string ColorWarningSubtle  => "oklch(0.22 0.04 50)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.14 50)";
    public virtual string ColorWarningFg      => "oklch(0.10 0.010 50)";

    // Info — honey orange
    public virtual string ColorInfo           => "oklch(0.62 0.14 35)";
    public virtual string ColorInfoSubtle     => "oklch(0.20 0.04 35)";
    public virtual string ColorInfoHover      => "oklch(0.68 0.13 35)";
    public virtual string ColorInfoFg         => "oklch(0.95 0.005 50)";

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

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.10 0.010 50), 0 0 0 4px oklch(0.68 0.18 65)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.10 0.010 50), 0 0 0 4px oklch(0.60 0.18 18)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class SolarisComponents : IThemeComponents
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

    internal sealed class SolarisTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=DM+Sans:wght@400;500;600;700&family=DM+Serif+Display:ital@0;1&family=DM+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'DM Serif Display', serif";
        public HeadingSettings H1 => new("2.618rem", HeadingFont, "700", "1.1", "-0.02em");
        public HeadingSettings H2 => new("2.118rem", HeadingFont, "600", "1.15", "-0.015em");
        public HeadingSettings H3 => new("1.618rem", HeadingFont, "600", "1.2", "-0.01em");
        public HeadingSettings H4 => new("1.25rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "600", "1.3", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
    }
