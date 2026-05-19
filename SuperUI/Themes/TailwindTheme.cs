namespace SuperUI.Themes;

/// <summary>
/// Tailwind CSS v4 / Tailwind UI design system theme for SuperUI.
/// Faithful to Tailwind's actual design language:
///   - Slate neutrals (not gray/zinc)
///   - Indigo-600 primary (Tailwind UI default)
///   - ring / ring-offset focus system
///   - Precise shadow scale matching Tailwind's shadow-* utilities
///   - Cubic-bezier easing matching Tailwind's transition-* utilities
///   - Compact sizing (text-sm everywhere, tight line-heights)
/// </summary>
public sealed class TailwindTheme : ThemeBase
{
    public override string Id          => "tailwind-v3";
    public override string Name        => "Tailwind CSS v3";
    public override string? Description => "Tailwind UI design system — Slate neutrals, Indigo primary, ring focus.";
    public override string? Author     => "SuperUI";
    public override string Version     => "4.0.0";

    protected override IThemePrimitives  CreatePrimitives()  => new TailwindPrimitives();
    protected override IThemeSemantic    CreateLight()        => new TailwindSemanticLight();
    protected override IThemeSemantic?   CreateDark()         => new TailwindSemanticDark();
    protected override IThemeComponents? CreateComponents()   => new TailwindComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════
           TAILWIND UTILITY STYLE — Component Overrides
           ═══════════════════════════════════════════════════════ */

        [data-theme-id="tailwind-v3"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
        }

        /* ── Modern Card ───────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border);
            box-shadow: var(--sg-shadow-sm);
            transition: all var(--sg-transition-base);
            border-radius: 8px;
        }

        [data-theme-id="tailwind-v3"] .sgc-card:hover {
            box-shadow: var(--sg-shadow-md);
        }

        [data-theme-id="tailwind-v3"] .sgc-card-outlined {
            box-shadow: none;
            border: 1px solid var(--sg-border-strong);
        }

        [data-theme-id="tailwind-v3"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border: none;
            box-shadow: none;
        }

        /* ── Buttons ───────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
            border: 1px solid transparent;
            box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            filter: brightness(1.1);
            transform: translateY(-1px);
            box-shadow: var(--sg-shadow-sm);
        }

        [data-theme-id="tailwind-v3"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            transform: translateY(0);
        }

        /* ── Inputs ────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-input,
        [data-theme-id="tailwind-v3"] .sgc-select,
        [data-theme-id="tailwind-v3"] .sgc-textarea {
            border: 1px solid var(--sg-border-strong);
            box-shadow: 0 1px 2px 0 rgba(0, 0, 0, 0.05);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="tailwind-v3"] .sgc-input:focus,
        [data-theme-id="tailwind-v3"] .sgc-select:focus,
        [data-theme-id="tailwind-v3"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            box-shadow: 0 0 0 1px var(--sg-color-primary), 0 1px 2px 0 rgba(0, 0, 0, 0.05);
            outline: none;
        }

        /* ── Header ────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-header {
            background: #ffffff;
            border-bottom: 1px solid #e2e8f0; /* Slate-200 */
        }

        [data-theme-id="tailwind-v3"][data-theme="dark"] .sgc-header {
            background: #1e293b; /* Slate-800 */
            border-bottom-color: #334155; /* Slate-700 */
        }
        [data-theme-id="tailwind-v3"] .sgc-nav {
            background: #1e293b; /* Slate-800 */
            color: #f1f5f9; /* Slate-100 */
            border-right: none;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-link {
            border-left: 2px solid transparent;
            margin: 2px 8px;
            padding: 8px 12px;
            border-radius: 6px;
            color: #94a3b8; /* Slate-400 */
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-link:hover {
            background: #334155; /* Slate-700 */
            color: #f1f5f9;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-link.active {
            background: #0ea5e9; /* Sky-500 */
            color: #fff;
            font-weight: 600;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-icon {
            color: inherit;
            opacity: 1;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-section {
            color: #64748b; /* Slate-500 */
            padding: 20px 16px 8px;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-group-header {
            margin: 2px 8px;
            border-radius: 6px;
            color: #f1f5f9;
        }

        [data-theme-id="tailwind-v3"] .sgc-nav-group-header:hover {
            background: #334155;
        }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-tabs-strip {
            background: var(--sg-bg-muted);
            border-radius: var(--sg-radius-lg);
            padding: 4px;
        }

        [data-theme-id="tailwind-v3"] .sgc-tab-item {
            border-radius: var(--sg-radius-md);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="tailwind-v3"] .sgc-tab-item.is-active {
            background: var(--sg-surface);
            box-shadow: var(--sg-shadow-sm);
            color: var(--sg-fg);
        }

        /* ── Alerts ────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-alert {
            border: 1px solid transparent;
            border-radius: 6px;
            box-shadow: none;
        }

        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-info    { background: #f0f9ff; border-color: #bae6fd; color: #0369a1; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-success { background: #f0fdf4; border-color: #bbf7d0; color: #15803d; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-warn    { background: #fffbeb; border-color: #fef3c7; color: #a16207; }
        [data-theme-id="tailwind-v3"] .sgc-alert.sgc-danger  { background: #fef2f2; border-color: #fecaca; color: #b91c1c; }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid #e2e8f0;
            border-radius: 0;
            padding: 0;
            gap: 32px;
        }

        [data-theme-id="tailwind-v3"] .sgc-tab {
            border-radius: 0;
            padding: 12px 0;
            border-bottom: 2px solid transparent;
            font-weight: 500;
            color: #64748b;
        }

        [data-theme-id="tailwind-v3"] .sgc-tab.sgc-active {
            color: #0ea5e9;
            border-bottom-color: #0ea5e9;
            background: transparent;
            box-shadow: none;
        }

        /* ── Chips ──────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sgc-chip {
            background: #f1f5f9;
            color: #334155;
            border: none;
            border-radius: 9999px;
            padding: 2px 10px;
            font-size: 12px;
        }

        [data-theme-id="tailwind-v3"] .sgc-chip.sgc-chip-selected {
            background: #0ea5e9;
            color: #fff;
        }

        /* ── Badges ─────────────────────────────────────────── */
        [data-theme-id="tailwind-v3"] .sg-badge {
            font-weight: 500;
            letter-spacing: 0.025em;
        }
        """;
}

internal class TailwindPrimitives : IThemePrimitives
{
    // Tailwind Slate — the neutral palette used by Tailwind UI
    public string Neutral0   => "#FFFFFF";
    public string Neutral50  => "#F8FAFC";
    public string Neutral100 => "#F1F5F9";
    public string Neutral200 => "#E2E8F0";
    public string Neutral300 => "#CBD5E1";
    public string Neutral400 => "#94A3B8";
    public string Neutral500 => "#64748B";
    public string Neutral600 => "#475569";
    public string Neutral700 => "#334155";
    public string Neutral800 => "#1E293B";
    public string Neutral900 => "#0F172A";

    // Tailwind Indigo — Tailwind UI primary
    public string Primary50  => "#EEF2FF";
    public string Primary100 => "#E0E7FF";
    public string Primary200 => "#C7D2FE";
    public string Primary300 => "#A5B4FC";
    public string Primary400 => "#818CF8";
    public string Primary500 => "#6366F1";
    public string Primary600 => "#4F46E5";
    public string Primary700 => "#4338CA";
    public string Primary800 => "#3730A3";
    public string Primary900 => "#312E81";

    // Tailwind Emerald
    public string Success50  => "#ECFDF5";
    public string Success100 => "#D1FAE5";
    public string Success500 => "#10B981";
    public string Success600 => "#059669";
    public string Success700 => "#047857";

    // Tailwind Red
    public string Danger50  => "#FEF2F2";
    public string Danger100 => "#FEE2E2";
    public string Danger500 => "#EF4444";
    public string Danger600 => "#DC2626";
    public string Danger700 => "#B91C1C";

    // Tailwind Amber
    public string Warning50  => "#FFFBEB";
    public string Warning100 => "#FEF3C7";
    public string Warning500 => "#F59E0B";
    public string Warning600 => "#D97706";

    // Tailwind Sky
    public string Info50  => "#F0F9FF";
    public string Info100 => "#E0F2FE";
    public string Info500 => "#0EA5E9";
    public string Info600 => "#0284C7";

    // Tailwind system font stack
    public string FontSans  => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono  => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, 'Liberation Mono', 'Courier New', monospace";
    public string FontSerif => "ui-serif, Georgia, Cambria, 'Times New Roman', Times, serif";

    // Tailwind border-radius scale
    public string RadiusNone => "0px";
    public string RadiusXs   => "2px";    // rounded-sm
    public string RadiusSm   => "4px";    // rounded
    public string RadiusMd   => "6px";    // rounded-md
    public string RadiusLg   => "8px";    // rounded-lg
    public string RadiusXl   => "12px";   // rounded-xl
    public string Radius2Xl  => "16px";   // rounded-2xl
    public string RadiusFull => "9999px"; // rounded-full
}

/// <summary>Light mode — white backgrounds, slate text, indigo accents.</summary>
internal class TailwindSemanticLight : IThemeSemantic
{
    // Backgrounds
    public string BgDefault    => "#FFFFFF";
    public string BgSubtle     => "#F8FAFC";   // slate-50
    public string BgMuted      => "#F1F5F9";   // slate-100
    public string BgEmphasized => "#E2E8F0";   // slate-200
    public string BgOverlay    => "rgba(15, 23, 42, 0.5)";
    public string BgGlass      => "rgba(255, 255, 255, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.50)";
    public string BlurGlass    => "12px";

    // Surfaces
    public string Surface        => "#FFFFFF";
    public string SurfaceRaised  => "#FFFFFF";
    public string SurfaceOverlay => "#FFFFFF";

    // Foreground — slate scale
    public string FgDefault  => "#0F172A";   // slate-900
    public string FgSubtle   => "#475569";   // slate-600
    public string FgMuted    => "#94A3B8";   // slate-400
    public string FgDisabled => "#CBD5E1";   // slate-300
    public string FgInverse  => "#FFFFFF";
    public string FgLink     => "#4F46E5";   // indigo-600
    public string FgLinkHover => "#4338CA";  // indigo-700

    // Borders
    public string BorderDefault => "#E2E8F0";  // slate-200
    public string BorderSubtle  => "#F1F5F9";  // slate-100
    public string BorderStrong  => "#94A3B8";  // slate-400
    public string BorderFocus   => "#6366F1";  // indigo-500
    public string Divider       => "#E5E7EB";  // gray-200

    // Primary — Indigo (Tailwind UI default)
    public string ColorPrimary        => "#4F46E5";   // indigo-600
    public string ColorPrimarySubtle  => "#EEF2FF";   // indigo-50
    public string ColorPrimaryMuted   => "#E0E7FF";   // indigo-100
    public string ColorPrimaryHover   => "#4338CA";   // indigo-700
    public string ColorPrimaryActive  => "#3730A3";   // indigo-800
    public string ColorPrimaryFg      => "#FFFFFF";

    // Success — Emerald
    public string ColorSuccess       => "#059669";   // emerald-600
    public string ColorSuccessSubtle => "#ECFDF5";   // emerald-50
    public string ColorSuccessHover  => "#047857";   // emerald-700
    public string ColorSuccessFg     => "#FFFFFF";

    // Danger — Red
    public string ColorDanger        => "#DC2626";   // red-600
    public string ColorDangerSubtle  => "#FEF2F2";   // red-50
    public string ColorDangerHover   => "#B91C1C";   // red-700
    public string ColorDangerFg      => "#FFFFFF";

    // Warning — Amber
    public string ColorWarning       => "#D97706";   // amber-600
    public string ColorWarningSubtle => "#FFFBEB";   // amber-50
    public string ColorWarningHover  => "#B45309";   // amber-700
    public string ColorWarningFg     => "#FFFFFF";

    // Info — Sky
    public string ColorInfo        => "#0284C7";   // sky-600
    public string ColorInfoSubtle  => "#F0F9FF";   // sky-50
    public string ColorInfoHover   => "#0369A1";   // sky-700
    public string ColorInfoFg      => "#FFFFFF";

    // Typography — Tailwind system stack
    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";   // text-sm
    public string TextBase => "0.9375rem";  // 15px baseline for UI density
    public string TextLg   => "1.0625rem";  // 17px

    // Tailwind shadow scale
    public string ShadowXs => "0 1px 2px 0 rgb(0 0 0 / 0.05)";
    public string ShadowSm => "0 1px 3px 0 rgb(0 0 0 / 0.1), 0 1px 2px -1px rgb(0 0 0 / 0.1)";
    public string ShadowMd => "0 4px 6px -1px rgb(0 0 0 / 0.1), 0 2px 4px -2px rgb(0 0 0 / 0.1)";
    public string ShadowLg => "0 10px 15px -3px rgb(0 0 0 / 0.1), 0 4px 6px -4px rgb(0 0 0 / 0.1)";
    public string ShadowXl => "0 20px 25px -5px rgb(0 0 0 / 0.1), 0 8px 10px -6px rgb(0 0 0 / 0.1)";

    // Tailwind border-radius
    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    // Tailwind easing
    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    // Ring focus system
    public string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px rgba(99, 102, 241, 0.5)";
    public string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

/// <summary>Dark mode — slate-900 bg, slate-100 text, indigo-400 accents.</summary>
internal class TailwindSemanticDark : IThemeSemantic
{
    public string BgDefault    => "#0F172A";   // slate-900
    public string BgSubtle     => "#1E293B";   // slate-800
    public string BgMuted      => "#334155";   // slate-700
    public string BgEmphasized => "#475569";   // slate-600
    public string BgOverlay    => "rgba(0, 0, 0, 0.8)";
    public string BgGlass      => "rgba(30, 41, 59, 0.60)";
    public string BorderGlass  => "rgba(255, 255, 255, 0.10)";
    public string BlurGlass    => "12px";

    public string Surface        => "#1E293B";  // slate-800
    public string SurfaceRaised  => "#334155";  // slate-700
    public string SurfaceOverlay => "#1E293B";

    public string FgDefault  => "#F1F5F9";   // slate-100
    public string FgSubtle   => "#94A3B8";   // slate-400
    public string FgMuted    => "#64748B";   // slate-500
    public string FgDisabled => "#475569";   // slate-600
    public string FgInverse  => "#0F172A";
    public string FgLink     => "#818CF8";   // indigo-400
    public string FgLinkHover => "#A5B4FC";  // indigo-300

    public string BorderDefault => "#334155";  // slate-700
    public string BorderSubtle  => "#1E293B";  // slate-800
    public string BorderStrong  => "#64748B";  // slate-500
    public string BorderFocus   => "#818CF8";  // indigo-400
    public string Divider       => "#1E293B";  // slate-800

    // Primary — lighter indigo for dark bg
    public string ColorPrimary        => "#6366F1";   // indigo-500
    public string ColorPrimarySubtle  => "rgba(99, 102, 241, 0.15)";
    public string ColorPrimaryMuted   => "rgba(99, 102, 241, 0.25)";
    public string ColorPrimaryHover   => "#818CF8";   // indigo-400
    public string ColorPrimaryActive  => "#A5B4FC";   // indigo-300
    public string ColorPrimaryFg      => "#FFFFFF";

    public string ColorSuccess       => "#10B981";
    public string ColorSuccessSubtle => "rgba(16, 185, 129, 0.15)";
    public string ColorSuccessHover  => "#34D399";
    public string ColorSuccessFg     => "#FFFFFF";

    public string ColorDanger        => "#EF4444";
    public string ColorDangerSubtle  => "rgba(239, 68, 68, 0.15)";
    public string ColorDangerHover   => "#F87171";
    public string ColorDangerFg      => "#FFFFFF";

    public string ColorWarning       => "#F59E0B";
    public string ColorWarningSubtle => "rgba(245, 158, 11, 0.15)";
    public string ColorWarningHover  => "#FBBF24";
    public string ColorWarningFg     => "#0F172A";

    public string ColorInfo        => "#0EA5E9";
    public string ColorInfoSubtle  => "rgba(14, 165, 233, 0.15)";
    public string ColorInfoHover   => "#38BDF8";
    public string ColorInfoFg      => "#FFFFFF";

    public string Font     => "ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif";
    public string FontMono => "ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "0.9375rem";
    public string TextLg   => "1.0625rem";

    public string ShadowXs => "0 1px 2px 0 rgb(0 0 0 / 0.4)";
    public string ShadowSm => "0 1px 3px 0 rgb(0 0 0 / 0.5), 0 1px 2px -1px rgb(0 0 0 / 0.5)";
    public string ShadowMd => "0 4px 6px -1px rgb(0 0 0 / 0.5), 0 2px 4px -2px rgb(0 0 0 / 0.5)";
    public string ShadowLg => "0 10px 15px -3px rgb(0 0 0 / 0.5), 0 4px 6px -4px rgb(0 0 0 / 0.5)";
    public string ShadowXl => "0 20px 25px -5px rgb(0 0 0 / 0.6), 0 8px 10px -6px rgb(0 0 0 / 0.6)";

    public string RadiusSm   => "4px";
    public string RadiusMd   => "6px";
    public string RadiusLg   => "8px";
    public string RadiusXl   => "12px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "150ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public string TransitionSlow => "300ms cubic-bezier(0.4, 0, 0.2, 1)";

    public string FocusRing       => "0 0 0 2px #0f172a, 0 0 0 4px rgba(99, 102, 241, 0.5)";
    public string FocusRingDanger => "0 0 0 2px #0f172a, 0 0 0 4px rgba(239, 68, 68, 0.5)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class TailwindComponents : IThemeComponents
{
    // Tailwind UI button sizing
    public string BtnRadius     => "6px";      // rounded-md
    public string BtnFontSize   => "0.875rem"; // text-sm
    public string BtnFontWeight => "600";      // font-semibold
    public string BtnHeight     => "36px";     // h-9
    public string BtnHeightSm   => "28px";     // h-7
    public string BtnHeightLg   => "40px";     // h-10

    // Tailwind form inputs
    public string InputRadius   => "6px";
    public string InputFontSize => "0.875rem";
    public string InputHeight   => "36px";
    public string InputHeightSm => "28px";
    public string InputHeightLg => "40px";

    // Cards
    public string CardRadius      => "12px";   // rounded-xl
    public string CardPadding     => "24px";   // p-6
    public string CardBorderColor => "#E2E8F0";
    public string CardBg          => "#FFFFFF";

    // Modal
    public string ModalRadius => "12px";

    // Table
    public string TableRadius          => "8px";
    public string TableHeaderFontWeight => "600";

    // Tabs
    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth     => "256px";

    // Navigation
    public string HeaderBg    => "#FFFFFF";
    public string HeaderFg    => "#0F172A";
    public string NavBg       => "#FFFFFF";
    public string NavFg       => "#475569";
    public string NavActiveBg => "#EEF2FF";   // indigo-50
    public string NavActiveFg => "#4F46E5";   // indigo-600
}
