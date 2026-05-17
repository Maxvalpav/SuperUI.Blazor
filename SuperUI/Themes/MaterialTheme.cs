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
           MATERIAL DESIGN 3 — Extended color roles & state layers
           ═══════════════════════════════════════════════════════ */

        /* M3 extended color tokens */
        :root {
            /* Secondary role */
            --md-secondary:            #625B71;
            --md-on-secondary:         #FFFFFF;
            --md-secondary-container:  #E8DEF8;
            --md-on-secondary-container: #1D192B;

            /* Tertiary role */
            --md-tertiary:             #7D5260;
            --md-on-tertiary:          #FFFFFF;
            --md-tertiary-container:   #FFD8E4;
            --md-on-tertiary-container: #31111D;

            /* Error role */
            --md-error:                #B3261E;
            --md-on-error:             #FFFFFF;
            --md-error-container:      #F9DEDC;
            --md-on-error-container:   #410E0B;

            /* Surface roles */
            --md-surface-dim:          #DED8E1;
            --md-surface-bright:       #FEF7FF;
            --md-surface-container-lowest:  #FFFFFF;
            --md-surface-container-low:     #F7F2FA;
            --md-surface-container:         #F3EDF7;
            --md-surface-container-high:    #ECE6F0;
            --md-surface-container-highest: #E6E0E9;

            /* Outline roles */
            --md-outline:              #79747E;
            --md-outline-variant:      #CAC4D0;

            /* Inverse */
            --md-inverse-surface:      #322F35;
            --md-inverse-on-surface:   #F5EFF7;
            --md-inverse-primary:      #D0BCFF;

            /* Scrim */
            --md-scrim:                #000000;

            /* State layer opacities */
            --md-state-hover:    0.08;
            --md-state-focus:    0.12;
            --md-state-pressed:  0.12;
            --md-state-dragged:  0.16;

            /* Elevation tonal overlays (surface + primary tint) */
            --md-elevation-1: rgba(103, 80, 164, 0.05);
            --md-elevation-2: rgba(103, 80, 164, 0.08);
            --md-elevation-3: rgba(103, 80, 164, 0.11);
            --md-elevation-4: rgba(103, 80, 164, 0.12);
            --md-elevation-5: rgba(103, 80, 164, 0.14);

            /* M3 type scale */
            --md-display-large:   57px;
            --md-display-medium:  45px;
            --md-display-small:   36px;
            --md-headline-large:  32px;
            --md-headline-medium: 28px;
            --md-headline-small:  24px;
            --md-title-large:     22px;
            --md-title-medium:    16px;
            --md-title-small:     14px;
            --md-label-large:     14px;
            --md-label-medium:    12px;
            --md-label-small:     11px;
            --md-body-large:      16px;
            --md-body-medium:     14px;
            --md-body-small:      12px;
        }

        [data-theme="dark"] {
            --md-secondary:            #CCC2DC;
            --md-on-secondary:         #332D41;
            --md-secondary-container:  #4A4458;
            --md-on-secondary-container: #E8DEF8;

            --md-tertiary:             #EFB8C8;
            --md-on-tertiary:          #492532;
            --md-tertiary-container:   #633B48;
            --md-on-tertiary-container: #FFD8E4;

            --md-error:                #F2B8B5;
            --md-on-error:             #601410;
            --md-error-container:      #8C1D18;
            --md-on-error-container:   #F9DEDC;

            --md-surface-dim:          #141218;
            --md-surface-bright:       #3B383E;
            --md-surface-container-lowest:  #0F0D13;
            --md-surface-container-low:     #1D1B20;
            --md-surface-container:         #211F26;
            --md-surface-container-high:    #2B2930;
            --md-surface-container-highest: #36343B;

            --md-outline:              #938F99;
            --md-outline-variant:      #49454F;

            --md-inverse-surface:      #E6E1E5;
            --md-inverse-on-surface:   #313033;
            --md-inverse-primary:      #6750A4;

            --md-elevation-1: rgba(208, 188, 255, 0.05);
            --md-elevation-2: rgba(208, 188, 255, 0.08);
            --md-elevation-3: rgba(208, 188, 255, 0.11);
            --md-elevation-4: rgba(208, 188, 255, 0.12);
            --md-elevation-5: rgba(208, 188, 255, 0.14);
        }

        /* ── Tonal surface elevation ───────────────────────── */
        [data-theme-id="material-design-3"] .sgc-card {
            background: var(--md-surface-container) !important;
            border: none !important;
            box-shadow: none !important;
        }

        [data-theme-id="material-design-3"] .sgc-card:hover {
            background: var(--md-surface-container-high) !important;
        }

        /* Filled card */
        [data-theme-id="material-design-3"] .sgc-card.sgc-card-filled {
            background: var(--md-surface-container-highest) !important;
        }

        /* Outlined card */
        [data-theme-id="material-design-3"] .sgc-card.sgc-card-outlined {
            background: var(--md-surface-container-lowest) !important;
            border: 1px solid var(--md-outline-variant) !important;
        }

        /* Elevated card — tonal overlay */
        [data-theme-id="material-design-3"] .sgc-card.sgc-card-elevated {
            background: var(--md-surface-container-low) !important;
            box-shadow: 0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15) !important;
        }

        /* ── Buttons — M3 button types ─────────────────────── */

        /* Filled button */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary) !important;
            color: var(--sg-color-primary-fg) !important;
            border: none !important;
            box-shadow: none !important;
            letter-spacing: 0.00625em;
            position: relative;
            overflow: hidden;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary::before {
            content: '';
            position: absolute;
            inset: 0;
            background: var(--sg-color-primary-fg);
            opacity: 0;
            transition: opacity 200ms;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled)::before {
            opacity: var(--md-state-hover);
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:active:not(:disabled)::before {
            opacity: var(--md-state-pressed);
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            transform: none !important;
            box-shadow: 0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15) !important;
        }

        /* Tonal button (default/ghost) */
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost) {
            background: var(--md-secondary-container) !important;
            color: var(--md-on-secondary-container) !important;
            border: none !important;
            box-shadow: none !important;
        }
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):hover:not(:disabled) {
            transform: none !important;
            box-shadow: 0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15) !important;
        }

        /* Outlined button */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-outlined {
            background: transparent !important;
            color: var(--sg-color-primary) !important;
            border: 1px solid var(--md-outline) !important;
            box-shadow: none !important;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-outlined:hover:not(:disabled) {
            background: var(--sg-color-primary-subtle) !important;
            transform: none !important;
            box-shadow: none !important;
        }

        /* Text button (ghost) */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-ghost {
            background: transparent !important;
            color: var(--sg-color-primary) !important;
            border: none !important;
            box-shadow: none !important;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-ghost:hover:not(:disabled) {
            background: var(--sg-color-primary-subtle) !important;
            transform: none !important;
        }

        /* ── FAB — Floating Action Button ──────────────────── */
        [data-theme-id="material-design-3"] .sgc-fab {
            background: var(--md-tertiary-container);
            color: var(--md-on-tertiary-container);
            border-radius: 16px;
            box-shadow: 0px 1px 2px rgba(0,0,0,0.3), 0px 1px 3px 1px rgba(0,0,0,0.15);
        }

        /* ── Inputs — M3 Filled & Outlined ─────────────────── */
        [data-theme-id="material-design-3"] .sgc-input,
        [data-theme-id="material-design-3"] .sgc-select,
        [data-theme-id="material-design-3"] .sgc-textarea {
            background: var(--md-surface-container-highest) !important;
            border: none !important;
            border-bottom: 1px solid var(--md-outline) !important;
            border-radius: 4px 4px 0 0 !important;
            padding: 20px 16px 6px !important;
            height: auto !important;
            min-height: 56px !important;
            transition: border-color 200ms, box-shadow 200ms !important;
        }
        [data-theme-id="material-design-3"] .sgc-input:hover,
        [data-theme-id="material-design-3"] .sgc-select:hover,
        [data-theme-id="material-design-3"] .sgc-textarea:hover {
            border-bottom-color: var(--sg-fg) !important;
        }
        [data-theme-id="material-design-3"] .sgc-input:focus,
        [data-theme-id="material-design-3"] .sgc-select:focus,
        [data-theme-id="material-design-3"] .sgc-textarea:focus {
            border-bottom: 2px solid var(--sg-color-primary) !important;
            box-shadow: none !important;
            outline: none !important;
        }

        /* ── Navigation Rail / Drawer ───────────────────────── */
        [data-theme-id="material-design-3"] .sgc-nav,
        [data-theme-id="material-design-3"] .sgc-sidebar {
            background: var(--md-surface-container) !important;
            border-right: none !important;
        }

        [data-theme-id="material-design-3"] .sgc-nav-item.is-active,
        [data-theme-id="material-design-3"] .sgc-nav-link.is-active {
            background: var(--md-secondary-container) !important;
            color: var(--md-on-secondary-container) !important;
            border-radius: 28px !important;
        }

        /* ── Top App Bar ────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-header {
            background: var(--md-surface-container) !important;
            border-bottom: none !important;
            box-shadow: none !important;
        }

        /* ── Chips ──────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sg-badge,
        [data-theme-id="material-design-3"] .sgc-chip {
            border-radius: 8px !important;
            font-size: var(--md-label-large) !important;
            font-weight: 500 !important;
            letter-spacing: 0.00625em !important;
            height: 32px !important;
            padding: 0 12px !important;
        }

        /* ── Divider ────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-divider,
        [data-theme-id="material-design-3"] hr {
            border-color: var(--md-outline-variant) !important;
        }

        /* ── Dialog / Modal ─────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-modal-content {
            background: var(--md-surface-container-high) !important;
            border: none !important;
            box-shadow: 0px 4px 8px 3px rgba(0,0,0,0.15), 0px 1px 3px rgba(0,0,0,0.3) !important;
        }

        /* ── Snackbar / Toast ───────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-toast {
            background: var(--md-inverse-surface) !important;
            color: var(--md-inverse-on-surface) !important;
            border-radius: 4px !important;
            box-shadow: 0px 4px 8px 3px rgba(0,0,0,0.15), 0px 1px 3px rgba(0,0,0,0.3) !important;
        }

        /* ── Dropdown / Menu ────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-dropdown-menu {
            background: var(--md-surface-container) !important;
            border: none !important;
            border-radius: 4px !important;
            box-shadow: 0px 2px 6px 2px rgba(0,0,0,0.15), 0px 1px 2px rgba(0,0,0,0.3) !important;
        }

        [data-theme-id="material-design-3"] .sgc-dropdown-item:hover {
            background: color-mix(in srgb, var(--sg-color-primary) 8%, transparent) !important;
        }

        /* ── Table ──────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-table thead th {
            background: var(--md-surface-container-low) !important;
            font-size: var(--md-label-large) !important;
            font-weight: 500 !important;
            letter-spacing: 0.00625em !important;
            color: var(--md-outline) !important;
        }

        [data-theme-id="material-design-3"] .sgc-table tbody tr:hover td {
            background: color-mix(in srgb, var(--sg-color-primary) 8%, transparent) !important;
        }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-tabs-strip {
            background: var(--md-surface-container) !important;
            border-radius: 0 !important;
            border-bottom: 1px solid var(--md-outline-variant) !important;
            padding: 0 !important;
        }

        [data-theme-id="material-design-3"] .sgc-tab-item {
            border-radius: 0 !important;
            font-size: var(--md-title-small) !important;
            font-weight: 500 !important;
            letter-spacing: 0.00625em !important;
        }

        [data-theme-id="material-design-3"] .sgc-tab-item.is-active {
            color: var(--sg-color-primary) !important;
            background: transparent !important;
            border-bottom: 3px solid var(--sg-color-primary) !important;
        }

        /* ── Progress ───────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-progress {
            background: var(--sg-color-primary-subtle) !important;
            border-radius: 2px !important;
            height: 4px !important;
        }

        [data-theme-id="material-design-3"] .sgc-progress-fill {
            background: var(--sg-color-primary) !important;
            border-radius: 2px !important;
        }

        /* ── Switch ─────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-switch-slider {
            background: var(--md-outline) !important;
            border: 2px solid var(--md-outline) !important;
            border-radius: 100px !important;
        }

        [data-theme-id="material-design-3"] .sgc-switch input:checked + .sgc-switch-slider {
            background: var(--sg-color-primary) !important;
            border-color: var(--sg-color-primary) !important;
        }

        /* ── Tooltip ────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-tooltip,
        [data-theme-id="material-design-3"] .sg-tooltip-dark {
            background: var(--md-inverse-surface) !important;
            color: var(--md-inverse-on-surface) !important;
            border-radius: 4px !important;
            font-size: var(--md-body-small) !important;
        }

        /* ── Scrollbar ──────────────────────────────────────── */
        [data-theme-id="material-design-3"] ::-webkit-scrollbar-thumb {
            background: var(--md-outline-variant) !important;
            border-radius: 2px !important;
        }

        /* ── Ripple animation ───────────────────────────────── */
        @keyframes md-ripple {
            from { transform: scale(0); opacity: 0.3; }
            to   { transform: scale(4); opacity: 0; }
        }
        """;
}

internal class MaterialPrimitives : DefaultPrimitives
{
    // M3 baseline purple palette (generated from #6750A4)
    public new string Primary50  => "#F3EDF7";
    public new string Primary100 => "#E8DEF8";
    public new string Primary200 => "#CCC2DC";
    public new string Primary300 => "#B69DF8";
    public new string Primary400 => "#9A82DB";
    public new string Primary500 => "#7965AF";
    public new string Primary600 => "#6750A4";
    public new string Primary700 => "#4F378B";
    public new string Primary800 => "#381E72";
    public new string Primary900 => "#21005D";

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
    // Backgrounds — M3 surface roles
    public new string BgDefault    => "#FEF7FF";   // Surface
    public new string BgSubtle     => "#F7F2FA";   // Surface Container Low
    public new string BgMuted      => "#F3EDF7";   // Surface Container
    public new string BgEmphasized => "#ECE6F0";   // Surface Container High

    // Surfaces
    public new string Surface        => "#FEF7FF";  // Surface Bright
    public new string SurfaceRaised  => "#F7F2FA";  // Surface Container Low
    public new string SurfaceOverlay => "#F3EDF7";  // Surface Container

    // Foreground
    public new string FgDefault  => "#1C1B1F";   // On Surface
    public new string FgSubtle   => "#49454F";   // On Surface Variant
    public new string FgMuted    => "#79747E";   // Outline
    public new string FgDisabled => "#CAC4D0";   // Outline Variant

    // Borders
    public new string BorderDefault => "#CAC4D0";  // Outline Variant
    public new string BorderSubtle  => "#E6E0E9";
    public new string BorderStrong  => "#79747E";  // Outline
    public new string BorderFocus   => "#6750A4";  // Primary
    public new string Divider       => "#CAC4D0";  // Outline Variant

    // Primary role
    public new string ColorPrimary        => "#6750A4";
    public new string ColorPrimarySubtle  => "#E8DEF8";  // Primary Container
    public new string ColorPrimaryMuted   => "#CCC2DC";
    public new string ColorPrimaryHover   => "#4F378B";
    public new string ColorPrimaryActive  => "#381E72";
    public new string ColorPrimaryFg      => "#FFFFFF";  // On Primary

    // Success → M3 Tertiary (teal-green)
    public new string ColorSuccess       => "#006A60";
    public new string ColorSuccessSubtle => "#9CF6E8";
    public new string ColorSuccessHover  => "#004D46";
    public new string ColorSuccessFg     => "#FFFFFF";

    // Danger → M3 Error
    public new string ColorDanger        => "#B3261E";
    public new string ColorDangerSubtle  => "#F9DEDC";  // Error Container
    public new string ColorDangerHover   => "#8C1D18";
    public new string ColorDangerFg      => "#FFFFFF";  // On Error

    // Warning → M3 Tertiary orange
    public new string ColorWarning       => "#7D5260";
    public new string ColorWarningSubtle => "#FFD8E4";  // Tertiary Container
    public new string ColorWarningHover  => "#633B48";
    public new string ColorWarningFg     => "#FFFFFF";

    // Info → M3 Secondary
    public new string ColorInfo        => "#625B71";
    public new string ColorInfoSubtle  => "#E8DEF8";   // Secondary Container
    public new string ColorInfoHover   => "#4A4458";
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
    // M3 dark surface roles
    public new string BgDefault    => "#141218";   // Surface Dim
    public new string BgSubtle     => "#1D1B20";   // Surface Container Low
    public new string BgMuted      => "#211F26";   // Surface Container
    public new string BgEmphasized => "#2B2930";   // Surface Container High

    public new string Surface        => "#141218";
    public new string SurfaceRaised  => "#1D1B20";
    public new string SurfaceOverlay => "#211F26";

    public new string FgDefault  => "#E6E1E5";   // On Surface
    public new string FgSubtle   => "#CAC4D0";   // On Surface Variant
    public new string FgMuted    => "#938F99";   // Outline
    public new string FgDisabled => "#49454F";   // Outline Variant

    public new string BorderDefault => "#49454F";
    public new string BorderSubtle  => "#36343B";
    public new string BorderStrong  => "#938F99";
    public new string BorderFocus   => "#D0BCFF";
    public new string Divider       => "#49454F";

    // Primary role (dark)
    public new string ColorPrimary        => "#D0BCFF";
    public new string ColorPrimarySubtle  => "rgba(208, 188, 255, 0.12)";
    public new string ColorPrimaryMuted   => "rgba(208, 188, 255, 0.20)";
    public new string ColorPrimaryHover   => "#E8DEF8";
    public new string ColorPrimaryActive  => "#F3EDF7";
    public new string ColorPrimaryFg      => "#381E72";  // On Primary (dark)

    public new string ColorSuccess       => "#4EE8D4";
    public new string ColorSuccessSubtle => "rgba(78, 232, 212, 0.12)";
    public new string ColorSuccessHover  => "#9CF6E8";
    public new string ColorSuccessFg     => "#003731";

    public new string ColorDanger        => "#F2B8B5";
    public new string ColorDangerSubtle  => "rgba(242, 184, 181, 0.12)";
    public new string ColorDangerHover   => "#F9DEDC";
    public new string ColorDangerFg      => "#601410";

    public new string ColorWarning       => "#EFB8C8";
    public new string ColorWarningSubtle => "rgba(239, 184, 200, 0.12)";
    public new string ColorWarningHover  => "#FFD8E4";
    public new string ColorWarningFg     => "#492532";

    public new string ColorInfo        => "#CCC2DC";
    public new string ColorInfoSubtle  => "rgba(204, 194, 220, 0.12)";
    public new string ColorInfoHover   => "#E8DEF8";
    public new string ColorInfoFg      => "#332D41";

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
