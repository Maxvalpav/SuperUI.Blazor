namespace SuperUI.Themes;

/// <summary>
/// Material Design 3 theme for SuperUI.
/// Implements the canonical M3 baseline color scheme (Purple seed #6750A4),
/// Roboto typography, tonal surface containers, state-layer hovers, pill buttons,
/// and the proper M3 elevation shadow set.
/// Reference: https://m3.material.io/styles/color/the-color-system/key-colors-tones
/// </summary>
public sealed class MaterialTheme : ThemeBase
{
    public override string Id          => "material-design-3";
    public override string Name        => "Material Design 3";
    public override string? Description => "Material You — пурпурный seed #6750A4, тональные поверхности, state layers, Roboto.";
    public override string? Author     => "SuperUI";
    public override string Version     => "3.1.0";

    protected override IThemePrimitives  CreatePrimitives()  => new MaterialPrimitives();
    protected override IThemeSemantic    CreateLight()        => new MaterialSemanticLight();
    protected override IThemeSemantic?   CreateDark()         => new MaterialSemanticDark();
    protected override IThemeComponents? CreateComponents()   => new MaterialComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════════════
           MATERIAL DESIGN 3 — Canonical baseline (#6750A4 purple seed)
           ═══════════════════════════════════════════════════════════════ */

        [data-theme-id="material-design-3"] {
            font-family: 'Roboto', 'Google Sans', system-ui, -apple-system, 'Segoe UI', sans-serif;
            letter-spacing: 0.005em;
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }

        /* ── Body / shell ──────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sui-shell,
        [data-theme-id="material-design-3"] .sui-main,
        [data-theme-id="material-design-3"] .sui-content {
            background: var(--sg-bg) !important;
        }

        /* ── M3 surface tonal containers ───────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-card {
            background: var(--sg-surface-raised);
            border: none;
            border-radius: 12px;
            box-shadow: none;
            transition: box-shadow 200ms cubic-bezier(0.4, 0, 0.2, 1),
                        background 150ms ease;
        }
        [data-theme-id="material-design-3"] .sgc-card-elevated {
            background: var(--sg-surface);
            box-shadow:
                0 1px 2px 0 rgba(0, 0, 0, 0.30),
                0 1px 3px 1px rgba(0, 0, 0, 0.15);
        }
        [data-theme-id="material-design-3"] .sgc-card-filled {
            background: var(--sg-bg-emphasized);
        }
        [data-theme-id="material-design-3"] .sgc-card-outlined {
            background: transparent;
            border: 1px solid var(--sg-border);
        }
        [data-theme-id="material-design-3"] .sgc-card-elevated:hover {
            box-shadow:
                0 1px 2px 0 rgba(0, 0, 0, 0.30),
                0 2px 6px 2px rgba(0, 0, 0, 0.15);
        }

        /* ── M3 Buttons ────────────────────────────────────────────── */
        /* Filled (default primary) — fully rounded pill */
        [data-theme-id="material-design-3"] .sgc-btn {
            border-radius: 20px;
            font-family: inherit;
            font-weight: 500;
            letter-spacing: 0.01em;
            text-transform: none;
            padding: 0 24px;
            height: 40px;
            transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);
            position: relative;
            overflow: hidden;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary) !important;
            color: var(--sg-color-primary-fg) !important;
            border: none;
            box-shadow: none;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            box-shadow:
                0 1px 2px 0 rgba(0, 0, 0, 0.30),
                0 1px 3px 1px rgba(0, 0, 0, 0.15);
            background: color-mix(in srgb, var(--sg-color-primary) 92%, white 8%) !important;
            transform: none;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: color-mix(in srgb, var(--sg-color-primary) 88%, black 12%) !important;
            box-shadow: none;
        }

        /* Tonal (secondary container) */
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost) {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            border: none;
        }
        [data-theme-id="material-design-3"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):hover:not(:disabled) {
            background: var(--sg-color-primary-muted);
        }

        /* Outlined */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-outlined {
            background: transparent;
            border: 1px solid var(--sg-border-strong);
            color: var(--sg-color-primary);
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-outlined:hover:not(:disabled) {
            background: var(--sg-color-primary-subtle);
            border-color: var(--sg-color-primary);
        }

        /* Text/Ghost */
        [data-theme-id="material-design-3"] .sgc-btn.sgc-ghost {
            background: transparent;
            color: var(--sg-color-primary);
            padding: 0 12px;
        }
        [data-theme-id="material-design-3"] .sgc-btn.sgc-ghost:hover:not(:disabled) {
            background: var(--sg-color-primary-subtle);
        }

        /* ── M3 Filled Text Field ──────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-input,
        [data-theme-id="material-design-3"] .sgc-select,
        [data-theme-id="material-design-3"] .sgc-textarea {
            background: var(--sg-bg-emphasized);
            border: none;
            border-bottom: 1px solid var(--sg-fg-subtle);
            border-radius: 4px 4px 0 0;
            padding: 8px 16px;
            transition: border-color 200ms cubic-bezier(0.4, 0, 0.2, 1),
                        background 150ms ease;
        }
        [data-theme-id="material-design-3"] .sgc-input:hover:not(:focus),
        [data-theme-id="material-design-3"] .sgc-select:hover:not(:focus),
        [data-theme-id="material-design-3"] .sgc-textarea:hover:not(:focus) {
            border-bottom-color: var(--sg-fg);
            background: var(--sg-bg-muted);
        }
        [data-theme-id="material-design-3"] .sgc-input:focus,
        [data-theme-id="material-design-3"] .sgc-select:focus,
        [data-theme-id="material-design-3"] .sgc-textarea:focus {
            background: var(--sg-bg-emphasized);
            border-bottom: 2px solid var(--sg-color-primary);
            padding-bottom: 7px;
            box-shadow: none;
            outline: none;
        }

        /* ── Top App Bar ───────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-header {
            background: var(--sg-surface) !important;
            border-bottom: 1px solid var(--sg-border-subtle);
            height: 64px;
            padding: 0 16px;
            box-shadow: none;
        }
        [data-theme-id="material-design-3"] .sgc-header-title {
            font-size: 1.375rem;
            font-weight: 400;
            letter-spacing: 0;
        }

        /* ── Navigation Drawer ─────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-nav {
            background: var(--sg-surface) !important;
            border-right: none;
        }
        [data-theme-id="material-design-3"] .sgc-nav-body { padding: 12px; }

        [data-theme-id="material-design-3"] .sgc-nav-link {
            border-left: none !important;
            border-radius: 28px;
            margin-bottom: 2px;
            padding: 0 16px;
            height: 56px;
            color: var(--sg-fg-subtle);
            font-size: 0.875rem;
            font-weight: 500;
            letter-spacing: 0.01em;
            transition: background 150ms ease, color 150ms ease;
            display: flex;
            align-items: center;
        }
        [data-theme-id="material-design-3"] .sgc-nav-link:hover {
            background: color-mix(in srgb, var(--sg-fg) 8%, transparent);
            color: var(--sg-fg);
        }
        [data-theme-id="material-design-3"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
        }
        [data-theme-id="material-design-3"] .sgc-nav-icon {
            font-size: 20px;
            margin-right: 12px;
        }
        [data-theme-id="material-design-3"] .sgc-nav-group-header {
            border-radius: 28px;
            padding: 0 16px;
            height: 56px;
            display: flex;
            align-items: center;
        }
        [data-theme-id="material-design-3"] .sgc-nav-section {
            padding: 18px 16px 8px;
            font-size: 0.6875rem;
            font-weight: 500;
            color: var(--sg-color-primary);
            text-transform: uppercase;
            letter-spacing: 0.08em;
        }

        /* ── Modal / Dialog ────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-modal-content {
            background: var(--sg-surface-overlay);
            border: none;
            border-radius: 28px;
            box-shadow:
                0 6px 10px 4px rgba(0, 0, 0, 0.15),
                0 2px 3px 0 rgba(0, 0, 0, 0.30);
        }
        [data-theme-id="material-design-3"] .sgc-modal-title {
            font-size: 1.5rem;
            font-weight: 400;
            letter-spacing: 0;
        }

        /* ── Alerts ────────────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-alert {
            border: none;
            border-left: none;
            border-radius: 12px;
            padding: 14px 16px;
            font-size: 0.875rem;
            line-height: 1.5;
        }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-info    { background: var(--sg-color-info-subtle);    color: var(--sg-color-info); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-success { background: var(--sg-color-success-subtle); color: var(--sg-color-success); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-warn    { background: var(--sg-color-warning-subtle); color: var(--sg-color-warning); }
        [data-theme-id="material-design-3"] .sgc-alert.sgc-danger  { background: var(--sg-color-danger-subtle);  color: var(--sg-color-danger); }

        /* ── Tabs ──────────────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid var(--sg-border);
            border-radius: 0;
            padding: 0;
            gap: 0;
        }
        [data-theme-id="material-design-3"] .sgc-tab {
            border-radius: 0;
            padding: 14px 16px;
            min-width: 90px;
            font-weight: 500;
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            border-bottom: 3px solid transparent;
            transition: color 150ms ease, border-color 150ms ease, background 150ms ease;
        }
        [data-theme-id="material-design-3"] .sgc-tab:hover {
            background: color-mix(in srgb, var(--sg-color-primary) 8%, transparent);
        }
        [data-theme-id="material-design-3"] .sgc-tab.sgc-active {
            color: var(--sg-color-primary);
            background: transparent;
            border-bottom-color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* ── Chips ─────────────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-chip {
            border: 1px solid var(--sg-border);
            background: transparent;
            border-radius: 8px;
            height: 32px;
            padding: 0 14px;
            font-weight: 500;
            font-size: 0.875rem;
            color: var(--sg-fg);
        }
        [data-theme-id="material-design-3"] .sgc-chip:hover {
            background: color-mix(in srgb, var(--sg-fg) 8%, transparent);
        }
        [data-theme-id="material-design-3"] .sgc-chip.sgc-chip-selected {
            background: var(--sg-color-primary-subtle);
            border-color: transparent;
            color: var(--sg-color-primary);
        }

        /* ── Accordion ─────────────────────────────────────────────── */
        [data-theme-id="material-design-3"] .sgc-accordion-item {
            border: none;
            background: var(--sg-bg-subtle);
            margin-bottom: 4px;
            border-radius: 12px;
            overflow: hidden;
        }
        [data-theme-id="material-design-3"] .sgc-accordion-item-header {
            padding: 14px 16px;
            font-weight: 500;
            font-size: 0.9375rem;
        }
        """;
}

internal class MaterialPrimitives : DefaultPrimitives
{
    // M3 baseline Primary palette (Purple seed #6750A4, tones P95..P10)
    public override string Primary50  => "#F6EDFF";  // P95
    public override string Primary100 => "#EADDFF";  // P90  Primary Container (light)
    public override string Primary200 => "#D0BCFF";  // P80  Primary (dark)
    public override string Primary300 => "#B69DF8";  // P70
    public override string Primary400 => "#9A82DB";  // P60
    public override string Primary500 => "#7F67BE";  // P50
    public override string Primary600 => "#6750A4";  // P40  Primary (light)
    public override string Primary700 => "#4F378B";  // P30
    public override string Primary800 => "#381E72";  // P20
    public override string Primary900 => "#21005D";  // P10  On Primary Container (light)

    // M3 shape scale
    public override string RadiusNone => "0px";
    public override string RadiusXs   => "4px";
    public override string RadiusSm   => "8px";
    public override string RadiusMd   => "12px";
    public override string RadiusLg   => "16px";
    public override string RadiusXl   => "28px";
    public override string Radius2Xl  => "28px";
    public override string RadiusFull => "9999px";
}

/// <summary>M3 Light scheme — baseline (#6750A4 seed).</summary>
internal class MaterialSemanticLight : DefaultSemanticLight
{
    // M3 Surface tonal containers (Neutral, N=4 hue)
    public override string BgDefault    => "#FFFBFE";   // Surface
    public override string BgSubtle     => "#F7F2FA";   // Surface Container Low
    public override string BgMuted      => "#F3EDF7";   // Surface Container
    public override string BgEmphasized => "#ECE6F0";   // Surface Container High
    public override string BgOverlay    => "rgba(0, 0, 0, 0.32)";

    public override string Surface        => "#FFFBFE";
    public override string SurfaceRaised  => "#F7F2FA";
    public override string SurfaceOverlay => "#F3EDF7";

    public override string FgDefault  => "#1C1B1F";    // On Surface
    public override string FgSubtle   => "#49454F";    // On Surface Variant
    public override string FgMuted    => "#79747E";    // Outline
    public override string FgDisabled => "#CAC4D0";    // Outline Variant
    public override string FgInverse  => "#FFFBFE";
    public override string FgLink     => "#6750A4";
    public override string FgLinkHover => "#4F378B";

    public override string BorderDefault => "#CAC4D0";  // Outline Variant
    public override string BorderSubtle  => "#E7E0EC";
    public override string BorderStrong  => "#79747E";  // Outline
    public override string BorderFocus   => "#6750A4";
    public override string Divider       => "#CAC4D0";

    // Primary (M3 baseline purple)
    public override string ColorPrimary        => "#6750A4";
    public override string ColorPrimarySubtle  => "#EADDFF";    // Primary Container
    public override string ColorPrimaryMuted   => "#D0BCFF";
    public override string ColorPrimaryHover   => "#4F378B";
    public override string ColorPrimaryActive  => "#381E72";
    public override string ColorPrimaryFg      => "#FFFFFF";    // On Primary

    // Success — pulled outside M3 baseline since baseline has no green role.
    public override string ColorSuccess       => "#386A1F";
    public override string ColorSuccessSubtle => "#C5F3A0";
    public override string ColorSuccessHover  => "#1E5106";
    public override string ColorSuccessFg     => "#FFFFFF";

    // Danger → M3 Error
    public override string ColorDanger        => "#B3261E";
    public override string ColorDangerSubtle  => "#F9DEDC";    // Error Container
    public override string ColorDangerHover   => "#8C1D18";
    public override string ColorDangerFg      => "#FFFFFF";

    // Warning → M3 Tertiary (warm)
    public override string ColorWarning       => "#7D5260";    // Tertiary
    public override string ColorWarningSubtle => "#FFD8E4";    // Tertiary Container
    public override string ColorWarningHover  => "#633B48";
    public override string ColorWarningFg     => "#FFFFFF";

    // Info → M3 Secondary
    public override string ColorInfo        => "#625B71";      // Secondary
    public override string ColorInfoSubtle  => "#E8DEF8";      // Secondary Container
    public override string ColorInfoHover   => "#4A4458";
    public override string ColorInfoFg      => "#FFFFFF";

    // Typography — Roboto
    public override string Font     => "'Roboto', 'Google Sans', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public override string FontMono => "'Roboto Mono', 'JetBrains Mono', ui-monospace, monospace";
    public override string TextSm   => "0.75rem";     // Body Small  (12px)
    public override string TextBase => "0.875rem";    // Body Medium (14px)
    public override string TextLg   => "1rem";        // Body Large  (16px)

    // M3 elevation shadows (key + ambient)
    public override string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.30)";
    public override string ShadowSm => "0 1px 2px 0 rgba(0, 0, 0, 0.30), 0 1px 3px 1px rgba(0, 0, 0, 0.15)";
    public override string ShadowMd => "0 1px 2px 0 rgba(0, 0, 0, 0.30), 0 2px 6px 2px rgba(0, 0, 0, 0.15)";
    public override string ShadowLg => "0 4px 8px 3px rgba(0, 0, 0, 0.15), 0 1px 3px 0 rgba(0, 0, 0, 0.30)";
    public override string ShadowXl => "0 6px 10px 4px rgba(0, 0, 0, 0.15), 0 2px 3px 0 rgba(0, 0, 0, 0.30)";

    public override string RadiusSm   => "8px";
    public override string RadiusMd   => "12px";
    public override string RadiusLg   => "16px";
    public override string RadiusXl   => "28px";
    public override string RadiusFull => "9999px";

    public override string TransitionFast => "100ms cubic-bezier(0.4, 0, 0.2, 1)";
    public override string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public override string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public override string FocusRing       => "0 0 0 3px rgba(103, 80, 164, 0.30)";
    public override string FocusRingDanger => "0 0 0 3px rgba(179, 38, 30, 0.30)";
}

/// <summary>M3 Dark scheme — baseline (#6750A4 seed).</summary>
internal class MaterialSemanticDark : DefaultSemanticDark
{
    public override string BgDefault    => "#1C1B1F";   // Surface
    public override string BgSubtle     => "#211F26";   // Surface Container Low
    public override string BgMuted      => "#2B2930";   // Surface Container
    public override string BgEmphasized => "#36343B";   // Surface Container High
    public override string BgOverlay    => "rgba(0, 0, 0, 0.55)";

    public override string Surface        => "#1C1B1F";
    public override string SurfaceRaised  => "#211F26";
    public override string SurfaceOverlay => "#2B2930";

    public override string FgDefault  => "#E6E1E5";    // On Surface
    public override string FgSubtle   => "#CAC4D0";    // On Surface Variant
    public override string FgMuted    => "#938F99";    // Outline
    public override string FgDisabled => "#49454F";    // Outline Variant
    public override string FgInverse  => "#1C1B1F";
    public override string FgLink     => "#D0BCFF";
    public override string FgLinkHover => "#EADDFF";

    public override string BorderDefault => "#49454F";   // Outline Variant
    public override string BorderSubtle  => "#36343B";
    public override string BorderStrong  => "#938F99";   // Outline
    public override string BorderFocus   => "#D0BCFF";
    public override string Divider       => "#49454F";

    // Primary (dark)
    public override string ColorPrimary        => "#D0BCFF";   // Primary (dark)
    public override string ColorPrimarySubtle  => "#4F378B";   // Primary Container (dark)
    public override string ColorPrimaryMuted   => "#5E45A0";
    public override string ColorPrimaryHover   => "#EADDFF";
    public override string ColorPrimaryActive  => "#F6EDFF";
    public override string ColorPrimaryFg      => "#381E72";   // On Primary (dark)

    public override string ColorSuccess       => "#9CDB7E";
    public override string ColorSuccessSubtle => "#1E5106";
    public override string ColorSuccessHover  => "#B6F4A0";
    public override string ColorSuccessFg     => "#003910";

    public override string ColorDanger        => "#F2B8B5";    // Error (dark)
    public override string ColorDangerSubtle  => "#8C1D18";    // Error Container (dark)
    public override string ColorDangerHover   => "#F9DEDC";
    public override string ColorDangerFg      => "#601410";    // On Error (dark)

    public override string ColorWarning       => "#EFB8C8";    // Tertiary (dark)
    public override string ColorWarningSubtle => "#633B48";    // Tertiary Container (dark)
    public override string ColorWarningHover  => "#FFD8E4";
    public override string ColorWarningFg     => "#492532";

    public override string ColorInfo        => "#CCC2DC";      // Secondary (dark)
    public override string ColorInfoSubtle  => "#4A4458";      // Secondary Container (dark)
    public override string ColorInfoHover   => "#E8DEF8";
    public override string ColorInfoFg      => "#332D41";

    public override string Font     => "'Roboto', 'Google Sans', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public override string FontMono => "'Roboto Mono', 'JetBrains Mono', ui-monospace, monospace";
    public override string TextSm   => "0.75rem";
    public override string TextBase => "0.875rem";
    public override string TextLg   => "1rem";

    public override string ShadowXs => "0 1px 2px 0 rgba(0, 0, 0, 0.45)";
    public override string ShadowSm => "0 1px 2px 0 rgba(0, 0, 0, 0.45), 0 1px 3px 1px rgba(0, 0, 0, 0.25)";
    public override string ShadowMd => "0 1px 2px 0 rgba(0, 0, 0, 0.45), 0 2px 6px 2px rgba(0, 0, 0, 0.25)";
    public override string ShadowLg => "0 4px 8px 3px rgba(0, 0, 0, 0.30), 0 1px 3px 0 rgba(0, 0, 0, 0.50)";
    public override string ShadowXl => "0 6px 10px 4px rgba(0, 0, 0, 0.30), 0 2px 3px 0 rgba(0, 0, 0, 0.50)";

    public override string RadiusSm   => "8px";
    public override string RadiusMd   => "12px";
    public override string RadiusLg   => "16px";
    public override string RadiusXl   => "28px";

    public override string FocusRing       => "0 0 0 3px rgba(208, 188, 255, 0.35)";
    public override string FocusRingDanger => "0 0 0 3px rgba(242, 184, 181, 0.35)";
}

internal class MaterialComponents : DefaultComponents
{
    // M3 button — pill (fully rounded ends), 40dp height
    public override string BtnRadius     => "20px";
    public override string BtnHeight     => "40px";
    public override string BtnHeightSm   => "32px";
    public override string BtnHeightLg   => "48px";
    public override string BtnFontSize   => "0.875rem";   // Label Large
    public override string BtnFontWeight => "500";

    // M3 text field — filled, 56dp tall
    public override string InputRadius   => "4px";
    public override string InputHeight   => "56px";
    public override string InputHeightSm => "48px";
    public override string InputHeightLg => "64px";
    public override string InputFontSize => "1rem";       // Body Large

    // M3 card — Medium (12). Modals use Extra Large (28) via ModalRadius.
    public override string CardRadius      => "12px";
    public override string CardPadding     => "16px";
    public override string CardBorderColor => "var(--sg-border)";
    public override string CardBg          => "var(--sg-surface-raised)";

    public override string ModalRadius => "28px";

    public override string TableRadius          => "0px";
    public override string TableHeaderFontWeight => "500";

    public override string TabsIndicatorHeight => "3px";
    public override string TooltipMaxWidth     => "200px";

    public override string HeaderBg    => "var(--sg-surface)";
    public override string HeaderFg    => "var(--sg-fg)";
    public override string NavBg       => "var(--sg-surface)";
    public override string NavFg       => "var(--sg-fg-subtle)";
    public override string NavActiveBg => "var(--sg-color-primary-subtle)";
    public override string NavActiveFg => "var(--sg-color-primary)";
}
