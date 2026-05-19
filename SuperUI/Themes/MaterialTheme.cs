namespace SuperUI.Themes;

/// <summary>
/// Material Design 3 theme for SuperUI.
/// Implements the full M3 color system: Primary/Secondary/Tertiary roles,
/// tonal surfaces, state layers, and correct elevation model.
/// Baseline palette: Purple primary, Teal secondary, Orange tertiary.
/// </summary>
public sealed class MaterialTheme : ThemeBase
{
    public override string Id          => "material-design-3";
    public override string Name        => "Material Design 3";
    public override string? Description => "Material You — динамические цвета, тональные поверхности, state layers.";
    public override string? Author     => "SuperUI";
    public override string Version     => "3.0.0";

    protected override IThemePrimitives  CreatePrimitives()  => new MaterialPrimitives();
    protected override IThemeSemantic    CreateLight()        => new MaterialSemanticLight();
    protected override IThemeSemantic?   CreateDark()         => new MaterialSemanticDark();
    protected override IThemeComponents? CreateComponents()   => new MaterialComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════
           MATERIAL DESIGN 3 — Component Overrides
           ═══════════════════════════════════════════════════════ */

        [data-theme-id="material-design-3"] {
            letter-spacing: 0.01em;
        }

        /* ── Tonal surface elevation ───────────────────────── */
        [data-theme-id="material-design-3"] .sgc-card {
            background: var(--sg-surface-raised);
            border: none;
            box-shadow: none;
            transition: all var(--sg-transition-base);
            border-radius: 12px;
        }

        [data-theme-id="material-design-3"] .sgc-card-filled {
            background: var(--sg-bg-emphasized);
        }

        [data-theme-id="material-design-3"] .sgc-card-outlined {
            background: transparent;
            border: 1px solid var(--sg-border-strong);
        }

        [data-theme-id="material-design-3"] .sgc-card-elevated {
            background: var(--sg-surface-overlay);
            box-shadow: var(--sg-shadow-sm);
        }

        [data-theme-id="material-design-3"] .sgc-card:hover {
            background: var(--sg-bg-emphasized);
        }

        [data-theme-id="material-design-3"] .sgc-card-elevated:hover {
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Buttons — M3 button types ─────────────────────── */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
            border: none;
            box-shadow: none;
            position: relative;
            overflow: hidden;
        }

        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            box-shadow: var(--sg-shadow-xs);
            filter: brightness(1.05);
            transform: none;
        }

        /* Tonal button */
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost) {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            border: none;
            box-shadow: none;
        }

        /* ── Inputs — M3 Filled ────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-input,
        [data-theme-id="material-design-3"] .sgc-select,
        [data-theme-id="material-design-3"] .sgc-textarea {
            background: var(--sg-bg-emphasized);
            border: none;
            border-bottom: 1px solid var(--sg-border-strong);
            border-radius: 4px 4px 0 0;
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="material-design-3"] .sgc-input:focus,
        [data-theme-id="material-design-3"] .sgc-select:focus,
        [data-theme-id="material-design-3"] .sgc-textarea:focus {
            background: var(--sg-bg-muted);
            border-bottom: 2px solid var(--sg-color-primary);
            box-shadow: none;
            outline: none;
        }

        /* ── Header ────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-header {
            background: var(--sg-surface);
            border-bottom: 1px solid var(--sg-border);
            height: 64px;
            padding: 0 16px;
        }
        [data-theme-id="material-design-3"] .sgc-nav {
            background: var(--sg-surface);
            border-right: none;
        }

        [data-theme-id="material-design-3"] .sgc-nav-body {
            padding: 12px 12px;
        }

        [data-theme-id="material-design-3"] .sgc-nav-link {
            border-left: none;
            border-radius: 28px;
            margin-bottom: 4px;
            padding: 10px 16px;
            height: 48px;
            color: var(--sg-fg-subtle);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="material-design-3"] .sgc-nav-link:hover {
            background: var(--sg-bg-emphasized);
            color: var(--sg-fg);
        }

        [data-theme-id="material-design-3"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 700;
        }

        [data-theme-id="material-design-3"] .sgc-nav-icon {
            font-size: 18px;
            margin-right: 12px;
        }

        [data-theme-id="material-design-3"] .sgc-nav-group-header {
            border-radius: 28px;
            padding: 10px 16px;
            height: 48px;
            margin-bottom: 4px;
        }

        [data-theme-id="material-design-3"] .sgc-nav-group-items {
            padding-left: 12px;
        }

        [data-theme-id="material-design-3"] .sgc-nav-section {
            padding: 18px 16px 8px;
            font-size: 11px;
            color: var(--sg-color-primary);
        }

        [data-theme-id="material-design-3"] .sgc-modal-content {
            background: var(--sg-surface-overlay);
            border: none;
            box-shadow: var(--sg-shadow-lg);
            border-radius: 28px;
        }

        /* ── Alerts ────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-alert {
            border: none;
            border-left: none;
            border-radius: 16px;
            padding: 14px 16px;
            font-size: 13px;
        }

        [data-theme-id="material-design-3"] .sgc-alert.sgc-info { background: var(--sg-color-info-subtle); color: var(--sg-color-info); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-success { background: var(--sg-color-success-subtle); color: var(--sg-color-success); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-warn { background: var(--sg-color-warning-subtle); color: var(--sg-color-warning); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-danger { background: var(--sg-color-danger-subtle); color: var(--sg-color-danger); }

        [data-theme-id="material-design-3"] .sgc-alert-icon {
            border: none;
            box-shadow: none;
            background: transparent;
            font-size: 16px;
        }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid var(--sg-border);
            border-radius: 0;
            padding: 0;
            gap: 24px;
        }

        [data-theme-id="material-design-3"] .sgc-tab {
            border-radius: 0;
            padding: 12px 0;
            font-weight: 500;
            transition: all var(--sg-transition-base);
            border-bottom: 3px solid transparent;
        }

        [data-theme-id="material-design-3"] .sgc-tab.sgc-active {
            color: var(--sg-color-primary);
            background: transparent;
            border-bottom-color: var(--sg-color-primary);
        }

        /* ── Chips ──────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-chip {
            border: 1px solid var(--sg-border-strong);
            background: transparent;
            border-radius: 8px;
            height: 32px;
            font-weight: 500;
        }

        [data-theme-id="material-design-3"] .sgc-chip.sgc-chip-selected {
            background: var(--sg-color-primary-subtle);
            border-color: transparent;
            color: var(--sg-color-primary);
        }

        /* ── Accordion ──────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-accordion-item {
            border: none;
            background: var(--sg-bg-subtle);
            margin-bottom: 4px;
            border-radius: 12px;
            overflow: hidden;
        }

        [data-theme-id="material-design-3"] .sgc-accordion-item-header {
            padding: 12px 16px;
            font-weight: 600;
        }
        """;
}

internal class MaterialPrimitives : DefaultPrimitives
{
    // M3 baseline blue palette (generated from #005AC1)
    public new string Primary50  => "#F8F9FF";
    public new string Primary100 => "#D7E3F7";
    public new string Primary200 => "#ADC6FF";
    public new string Primary300 => "#92ADF7";
    public new string Primary400 => "#7892DB";
    public new string Primary500 => "#5C78AF";
    public new string Primary600 => "#005AC1";
    public new string Primary700 => "#004494";
    public new string Primary800 => "#002F66";
    public new string Primary900 => "#001B3D";

    // M3 shape scale
    public new string RadiusNone => "0px";
    public new string RadiusXs   => "4px";
    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";
    public new string Radius2Xl  => "28px";
    public new string RadiusFull => "9999px";
}

/// <summary>
/// M3 Light scheme — generated from baseline purple seed.
/// Roles: Primary, Secondary, Tertiary, Error + their containers.
/// </summary>
internal class MaterialSemanticLight : DefaultSemanticLight
{
    // Backgrounds — M3 surface roles (Neutral Blue-Grey)
    public new string BgDefault    => "#F8F9FF";   // Surface Bright
    public new string BgSubtle     => "#F2F3F9";   // Surface Container Low
    public new string BgMuted      => "#ECEEF4";   // Surface Container
    public new string BgEmphasized => "#E7E8EE";   // Surface Container High

    // Surfaces
    public new string Surface        => "#F8F9FF";
    public new string SurfaceRaised  => "#F2F3F9";
    public new string SurfaceOverlay => "#ECEEF4";

    // Foreground
    public new string FgDefault  => "#191C20";   // On Surface
    public new string FgSubtle   => "#44474E";   // On Surface Variant
    public new string FgMuted    => "#74777F";   // Outline
    public new string FgDisabled => "#C4C6D0";   // Outline Variant
    public new string FgLink     => "#005AC1";
    public new string FgLinkHover => "#004494";

    // Borders
    public new string BorderDefault => "#C4C6D0";
    public new string BorderSubtle  => "#E1E2E8";
    public new string BorderStrong  => "#74777F";
    public new string BorderFocus   => "#005AC1";
    public new string Divider       => "#C4C6D0";

    // Primary role (Blue)
    public new string ColorPrimary        => "#005AC1";
    public new string ColorPrimarySubtle  => "#D7E3F7";  // Primary Container
    public new string ColorPrimaryMuted   => "#ADC6FF";
    public new string ColorPrimaryHover   => "#004494";
    public new string ColorPrimaryActive  => "#002F66";
    public new string ColorPrimaryFg      => "#FFFFFF";  // On Primary

    // Success → M3 Tertiary (cyan/teal)
    public new string ColorSuccess       => "#006874";
    public new string ColorSuccessSubtle => "#A1EFFF";
    public new string ColorSuccessHover  => "#004F58";
    public new string ColorSuccessFg     => "#FFFFFF";

    // Danger → M3 Error
    public new string ColorDanger        => "#BA1A1A";
    public new string ColorDangerSubtle  => "#FFDAD6";  // Error Container
    public new string ColorDangerHover   => "#93000A";
    public new string ColorDangerFg      => "#FFFFFF";  // On Error

    // Warning → M3 Secondary Blue-Grey
    public new string ColorWarning       => "#535F70";
    public new string ColorWarningSubtle => "#D7E3F7";  // Secondary Container
    public new string ColorWarningHover  => "#3B4758";
    public new string ColorWarningFg     => "#FFFFFF";

    // Info → M3 Primary Blue
    public new string ColorInfo        => "#005AC1";
    public new string ColorInfoSubtle  => "#D7E3F7";   // Primary Container
    public new string ColorInfoHover   => "#004494";
    public new string ColorInfoFg      => "#FFFFFF";

    // Typography — M3 uses Roboto
    public new string Font     => "'Roboto', 'Google Sans', system-ui, sans-serif";
    public new string FontMono => "'Roboto Mono', monospace";
    public new string TextSm   => "0.75rem";    // Body Small
    public new string TextBase => "0.875rem";   // Body Medium
    public new string TextLg   => "1rem";       // Body Large

    // M3 elevation shadows (tonal + shadow)
    public new string ShadowXs => "0px 1px 2px rgba(0,0,0,0.3)";
    public new string ShadowSm => "0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15)";
    public new string ShadowMd => "0px 1px 2px rgba(0,0,0,0.3), 0px 2px 6px 2px rgba(0,0,0,0.15)";
    public new string ShadowLg => "0px 4px 8px 3px rgba(0,0,0,0.15), 0px 1px 3px rgba(0,0,0,0.3)";
    public new string ShadowXl => "0px 6px 10px 4px rgba(0,0,0,0.15), 0px 2px 3px rgba(0,0,0,0.3)";

    // M3 shape scale
    public new string RadiusSm   => "8px";    // Small
    public new string RadiusMd   => "12px";   // Medium
    public new string RadiusLg   => "16px";   // Large
    public new string RadiusXl   => "28px";   // Extra Large

    public new string TransitionFast => "100ms";
    public new string TransitionBase => "200ms";
    public new string TransitionSlow => "300ms";

    public new string FocusRing       => "0 0 0 3px rgba(103, 80, 164, 0.30)";
    public new string FocusRingDanger => "0 0 0 3px rgba(179, 38, 30, 0.30)";
}

/// <summary>
/// M3 Dark scheme — generated from baseline purple seed.
/// </summary>
internal class MaterialSemanticDark : DefaultSemanticDark
{
    // M3 dark surface roles (Neutral Blue-Grey)
    public new string BgDefault    => "#111318";   // Surface Dim
    public new string BgSubtle     => "#191C21";   // Surface Container Low
    public new string BgMuted      => "#1D2026";   // Surface Container
    public new string BgEmphasized => "#282A30";   // Surface Container High

    public new string Surface        => "#111318";
    public new string SurfaceRaised  => "#191C21";
    public new string SurfaceOverlay => "#1D2026";

    public new string FgDefault  => "#E1E2E8";   // On Surface
    public new string FgSubtle   => "#C4C6D0";   // On Surface Variant
    public new string FgMuted    => "#8E9099";   // Outline
    public new string FgDisabled => "#44474F";   // Outline Variant
    public new string FgLink     => "#ADC6FF";
    public new string FgLinkHover => "#D7E3F7";

    public new string BorderDefault => "#44474F";
    public new string BorderSubtle  => "#2E3036";
    public new string BorderStrong  => "#8E9099";
    public new string BorderFocus   => "#ADC6FF";
    public new string Divider       => "#44474F";

    // Primary role (dark blue)
    public new string ColorPrimary        => "#ADC6FF";
    public new string ColorPrimarySubtle  => "rgba(173, 198, 255, 0.12)";
    public new string ColorPrimaryMuted   => "rgba(173, 198, 255, 0.20)";
    public new string ColorPrimaryHover   => "#D7E3F7";
    public new string ColorPrimaryActive  => "#F8F9FF";
    public new string ColorPrimaryFg      => "#002F66";  // On Primary (dark)

    public new string ColorSuccess       => "#86D2E1";
    public new string ColorSuccessSubtle => "rgba(134, 210, 225, 0.12)";
    public new string ColorSuccessHover  => "#A1EFFF";
    public new string ColorSuccessFg     => "#00363D";

    public new string ColorDanger        => "#FFB4AB";
    public new string ColorDangerSubtle  => "rgba(255, 180, 171, 0.12)";
    public new string ColorDangerHover   => "#FFDAD6";
    public new string ColorDangerFg      => "#690005";

    public new string ColorWarning       => "#BFC8D8";
    public new string ColorWarningSubtle => "rgba(191, 200, 216, 0.12)";
    public new string ColorWarningHover  => "#D7E3F7";
    public new string ColorWarningFg     => "#253140";

    public new string ColorInfo        => "#ADC6FF";
    public new string ColorInfoSubtle  => "rgba(173, 198, 255, 0.12)";
    public new string ColorInfoHover   => "#D7E3F7";
    public new string ColorInfoFg      => "#002F66";

    public new string Font     => "'Roboto', 'Google Sans', system-ui, sans-serif";
    public new string FontMono => "'Roboto Mono', monospace";
    public new string TextSm   => "0.75rem";
    public new string TextBase => "0.875rem";
    public new string TextLg   => "1rem";

    public new string ShadowXs => "0px 1px 2px rgba(0,0,0,0.5)";
    public new string ShadowSm => "0px 1px 2px rgba(0,0,0,0.5), 0px 1px 3px 1px rgba(0,0,0,0.3)";
    public new string ShadowMd => "0px 1px 2px rgba(0,0,0,0.5), 0px 2px 6px 2px rgba(0,0,0,0.3)";
    public new string ShadowLg => "0px 4px 8px 3px rgba(0,0,0,0.3), 0px 1px 3px rgba(0,0,0,0.5)";
    public new string ShadowXl => "0px 6px 10px 4px rgba(0,0,0,0.3), 0px 2px 3px rgba(0,0,0,0.5)";

    public new string RadiusSm   => "8px";
    public new string RadiusMd   => "12px";
    public new string RadiusLg   => "16px";
    public new string RadiusXl   => "28px";

    public new string FocusRing       => "0 0 0 3px rgba(208, 188, 255, 0.35)";
    public new string FocusRingDanger => "0 0 0 3px rgba(242, 184, 181, 0.35)";
}

internal class MaterialComponents : DefaultComponents
{
    // M3 button — full rounded (pill shape)
    public new string BtnRadius     => "20px";
    public new string BtnHeight     => "40px";
    public new string BtnHeightSm   => "32px";
    public new string BtnHeightLg   => "48px";
    public new string BtnFontSize   => "0.875rem";   // Label Large
    public new string BtnFontWeight => "500";

    // M3 text field — filled style
    public new string InputRadius   => "4px";
    public new string InputHeight   => "56px";
    public new string InputHeightSm => "48px";
    public new string InputHeightLg => "64px";
    public new string InputFontSize => "1rem";       // Body Large

    // M3 card — Extra Large shape
    public new string CardRadius      => "28px";
    public new string CardPadding     => "24px";
    public new string CardBorderColor => "var(--md-outline-variant)";
    public new string CardBg          => "var(--md-surface-container)";

    // M3 dialog — Extra Large shape
    public new string ModalRadius => "28px";

    // M3 table
    public new string TableRadius          => "0px";
    public new string TableHeaderFontWeight => "500";

    // M3 tabs — indicator 3px
    public new string TabsIndicatorHeight => "3px";
    public new string TooltipMaxWidth     => "200px";

    // Navigation
    public new string HeaderBg    => "var(--md-surface-container)";
    public new string HeaderFg    => "var(--sg-fg)";
    public new string NavBg       => "var(--md-surface-container)";
    public new string NavFg       => "var(--sg-fg-subtle)";
    public new string NavActiveBg => "var(--md-secondary-container)";
    public new string NavActiveFg => "var(--md-on-secondary-container)";
}
