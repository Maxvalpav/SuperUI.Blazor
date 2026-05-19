namespace SuperUI.Themes;

/// <summary>
/// Glassmorphism theme for SuperUI.
/// Frosted glass surfaces over a vivid gradient background.
/// </summary>
public sealed class GlassTheme : ThemeBase
{
    public override string Id => "superui-glass";
    public override string Name => "Glassmorphism";
    public override string? Description => "Матовое стекло, яркий градиентный фон, полупрозрачные поверхности.";
    public override string Version => "2.0.0";

    protected override IThemePrimitives CreatePrimitives() => new GlassPrimitives();
    protected override IThemeSemantic CreateLight() => new GlassSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GlassSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GlassComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════
           GLASSMORPHISM — Design Language
           ═══════════════════════════════════════════════════ */
        [data-theme-id="superui-glass"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            letter-spacing: -0.01em;
            color: var(--sg-fg);
        }

        [data-theme-id="superui-glass"] .sgc-label,
        [data-theme-id="superui-glass"] .sgc-title {
            font-weight: 700;
            color: var(--sg-fg);
        }

        /* ── Vivid gradient background ─────────────────────── */
        [data-theme-id="superui-glass"] .sui-shell,
        [data-theme-id="superui-glass"] .sui-main,
        [data-theme-id="superui-glass"] .sui-content {
            background: transparent;
        }

        /* Light mode — soft sky blue + subtle blobs */
        [data-theme-id="superui-glass"] .sui-shell {
            background:
                radial-gradient(circle at 15% 15%, rgba(21, 101, 192, 0.15) 0%, transparent 40%),
                radial-gradient(circle at 85% 25%, rgba(0, 200, 83, 0.12) 0%, transparent 35%),
                radial-gradient(circle at 50% 80%, rgba(30, 136, 229, 0.18) 0%, transparent 45%),
                linear-gradient(160deg, #E3F2FD 0%, #BBDEFB 100%) !important;
            background-attachment: fixed !important;
        }

        /* Dark mode — deep cosmic blue */
        [data-theme-id="superui-glass"][data-theme="dark"] .sui-shell {
            background:
                radial-gradient(circle at 10% 10%, rgba(13, 71, 161, 0.3) 0%, transparent 40%),
                radial-gradient(circle at 90% 20%, rgba(0, 77, 64, 0.25) 0%, transparent 35%),
                linear-gradient(160deg, #010409 0%, #0d1117 100%) !important;
            background-attachment: fixed !important;
        }

        /* ── Glass components overrides ────────────────────── */
        [data-theme-id="superui-glass"] .sgc-card,
        [data-theme-id="superui-glass"] .sgc-panel {
            backdrop-filter: var(--sg-blur-glass);
            -webkit-backdrop-filter: var(--sg-blur-glass);
            box-shadow: 
                0 8px 32px rgba(31, 38, 135, 0.15),
                inset 0 0 0 1px rgba(255, 255, 255, 0.4);
            border: 1px solid rgba(255, 255, 255, 0.18);
        }

        [data-theme-id="superui-glass"] .sgc-card-header {
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-card,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-panel {
            box-shadow: 
                0 8px 32px rgba(0, 0, 0, 0.3),
                inset 0 0 0 1px rgba(255, 255, 255, 0.1);
            border: 1px solid rgba(255, 255, 255, 0.05);
        }
        [data-theme-id="superui-glass"] .sgc-modal-content,
        [data-theme-id="superui-glass"] .sgc-drawer-content {
            backdrop-filter: blur(40px);
            -webkit-backdrop-filter: blur(40px);
            box-shadow: var(--sg-shadow-xl), inset 0 1px 0 rgba(255,255,255,0.3);
        }

        /* ── Inputs ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-input,
        [data-theme-id="superui-glass"] .sgc-select,
        [data-theme-id="superui-glass"] .sgc-textarea {
            background: var(--sg-bg-glass);
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border: 1px solid var(--sg-border-glass);
            box-shadow: inset 0 1px 1px rgba(0,0,0,0.05);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="superui-glass"] .sgc-input:focus,
        [data-theme-id="superui-glass"] .sgc-select:focus,
        [data-theme-id="superui-glass"] .sgc-textarea:focus {
            background: rgba(255, 255, 255, 0.4);
            border-color: var(--sg-color-primary);
            box-shadow: var(--sg-focus-ring);
        }

        /* ── Buttons ───────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary {
            background: linear-gradient(135deg, #1E88E5 0%, #00C853 100%);
            border: none;
            box-shadow: 0 4px 14px rgba(0, 150, 100, 0.3);
            color: #fff;
        }

        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            filter: brightness(1.1);
            transform: translateY(-1px);
            box-shadow: 0 6px 20px rgba(0, 150, 100, 0.4);
        }

        /* ── Header ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-header {
            background: rgba(255, 255, 255, 0.15) !important;
            backdrop-filter: blur(20px) saturate(180%);
            -webkit-backdrop-filter: blur(20px) saturate(180%);
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-header {
            background: rgba(0, 0, 0, 0.3) !important;
            border-bottom-color: rgba(255, 255, 255, 0.05);
        }
        [data-theme-id="superui-glass"] .sgc-nav {
            background: rgba(255, 255, 255, 0.15) !important;
            backdrop-filter: blur(20px) saturate(180%);
            -webkit-backdrop-filter: blur(20px) saturate(180%);
            border-right: 1px solid rgba(255, 255, 255, 0.1);
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav {
            background: rgba(0, 0, 0, 0.3) !important;
            border-right-color: rgba(255, 255, 255, 0.05);
        }

        [data-theme-id="superui-glass"] .sgc-nav-link {
            border-left: none;
            margin: 4px 10px;
            border-radius: 12px;
            color: var(--sg-fg-subtle);
            transition: all var(--sg-transition-base);
        }

        [data-theme-id="superui-glass"] .sgc-nav-link:hover {
            background: rgba(255, 255, 255, 0.15);
            color: var(--sg-fg);
        }

        [data-theme-id="superui-glass"] .sgc-nav-link.active {
            background: rgba(255, 255, 255, 0.25);
            color: var(--sg-fg);
            box-shadow: 
                0 4px 12px rgba(0, 0, 0, 0.05),
                inset 0 0 0 1px rgba(255, 255, 255, 0.2);
            font-weight: 700;
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav-link.active {
            background: rgba(255, 255, 255, 0.1);
            box-shadow: 
                0 4px 12px rgba(0, 0, 0, 0.2),
                inset 0 0 0 1px rgba(255, 255, 255, 0.05);
        }

        [data-theme-id="superui-glass"] .sgc-nav-group-header {
            margin: 4px 10px;
            border-radius: 12px;
        }

        [data-theme-id="superui-glass"] .sgc-nav-section {
            padding: 14px 20px 6px;
            color: var(--sg-color-primary);
            font-size: 10px;
            opacity: 0.8;
        }

        /* ── Table & Navigation ────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-table thead th {
            background: rgba(255, 255, 255, 0.2);
            backdrop-filter: blur(5px);
        }

        /* ── Scrollbar ──────────────────────────────────────── */
        [data-theme-id="superui-glass"] ::-webkit-scrollbar-thumb {
            background: rgba(0, 0, 0, 0.15);
            border-radius: 10px;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] ::-webkit-scrollbar-thumb {
            background: rgba(255, 255, 255, 0.2);
        }

        /* ── Alerts ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-alert {
            background: rgba(255, 255, 255, 0.15) !important;
            backdrop-filter: blur(12px);
            -webkit-backdrop-filter: blur(12px);
            border: 1px solid rgba(255, 255, 255, 0.2) !important;
            border-left-width: 4px !important;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.05);
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-alert {
            background: rgba(0, 0, 0, 0.2) !important;
            border-color: rgba(255, 255, 255, 0.05) !important;
        }

        /* ── Tabs ───────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-tabs-strip {
            background: rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(8px);
            border-radius: 12px;
            padding: 4px;
            gap: 4px;
        }

        [data-theme-id="superui-glass"] .sgc-tab {
            border-radius: 8px;
            transition: all var(--sg-transition-base);
            border: 1px solid transparent;
        }

        [data-theme-id="superui-glass"] .sgc-tab.sgc-active {
            background: rgba(255, 255, 255, 0.2);
            border-color: rgba(255, 255, 255, 0.1);
            color: var(--sg-fg);
            box-shadow: 0 2px 8px rgba(0,0,0,0.05);
        }

        /* ── Chips ──────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-chip {
            background: rgba(255, 255, 255, 0.12);
            backdrop-filter: blur(4px);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 99px;
        }

        [data-theme-id="superui-glass"] .sgc-chip.sgc-chip-selected {
            background: var(--sg-color-primary-subtle);
            border-color: var(--sg-color-primary);
        }
        """;
}

internal class GlassPrimitives : DefaultPrimitives { }

internal class GlassSemanticLight : IThemeSemantic
{
    // Backgrounds — soft frosted glass
    public string BgDefault     => "rgba(255, 255, 255, 0.25)";
    public string BgSubtle      => "rgba(255, 255, 255, 0.15)";
    public string BgMuted       => "rgba(255, 255, 255, 0.10)";
    public string BgEmphasized  => "rgba(255, 255, 255, 0.40)";
    public string BgOverlay     => "rgba(15, 23, 42, 0.3)";
    public string BgGlass       => "rgba(255, 255, 255, 0.18)";
    public string BorderGlass   => "rgba(255, 255, 255, 0.40)";
    public string BlurGlass     => "24px";

    // Surfaces
    public string Surface        => "rgba(255, 255, 255, 0.22)";
    public string SurfaceRaised  => "rgba(255, 255, 255, 0.35)";
    public string SurfaceOverlay => "rgba(255, 255, 255, 0.55)";

    // Foreground — high contrast navy
    public string FgDefault   => "#0f172a"; 
    public string FgSubtle    => "#475569";
    public string FgMuted     => "#64748b";
    public string FgDisabled  => "#94a3b8";
    public string FgInverse   => "#ffffff";
    public string FgLink      => "#0284c7";
    public string FgLinkHover => "#0369a1";

    // Borders
    public string BorderDefault => "rgba(15, 23, 42, 0.12)";
    public string BorderSubtle  => "rgba(15, 23, 42, 0.06)";
    public string BorderStrong  => "rgba(15, 23, 42, 0.25)";
    public string BorderFocus   => "#0ea5e9";
    public string Divider       => "rgba(15, 23, 42, 0.08)";

    // Primary
    public string ColorPrimary        => "#0ea5e9";
    public string ColorPrimarySubtle  => "rgba(14, 165, 233, 0.15)";
    public string ColorPrimaryMuted   => "rgba(14, 165, 233, 0.25)";
    public string ColorPrimaryHover   => "#0284c7";
    public string ColorPrimaryActive  => "#0369a1";
    public string ColorPrimaryFg      => "#ffffff";

    public string ColorSuccess        => "#10b981";
    public string ColorSuccessSubtle  => "rgba(16, 185, 129, 0.15)";
    public string ColorSuccessHover   => "#059669";
    public string ColorSuccessFg      => "#ffffff";

    public string ColorDanger         => "#f43f5e";
    public string ColorDangerSubtle   => "rgba(244, 63, 94, 0.15)";
    public string ColorDangerHover    => "#e11d48";
    public string ColorDangerFg       => "#ffffff";

    public string ColorWarning        => "#f59e0b";
    public string ColorWarningSubtle  => "rgba(245, 158, 11, 0.15)";
    public string ColorWarningHover   => "#d97706";
    public string ColorWarningFg      => "#ffffff";

    public string ColorInfo           => "#3b82f6";
    public string ColorInfoSubtle     => "rgba(59, 130, 246, 0.15)";
    public string ColorInfoHover      => "#2563eb";
    public string ColorInfoFg         => "#ffffff";

    public string Font     => "system-ui, -apple-system, sans-serif";
    public string FontMono => "ui-monospace, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px rgba(0, 0, 0, 0.05)";
    public string ShadowSm => "0 4px 6px rgba(0, 0, 0, 0.05)";
    public string ShadowMd => "0 10px 15px rgba(0, 0, 0, 0.07)";
    public string ShadowLg => "0 20px 25px rgba(0, 0, 0, 0.1)";
    public string ShadowXl => "0 25px 50px rgba(0, 0, 0, 0.15)";

    public string RadiusSm   => "8px";
    public string RadiusMd   => "12px";
    public string RadiusLg   => "16px";
    public string RadiusXl   => "24px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms";
    public string TransitionBase => "200ms";
    public string TransitionSlow => "400ms";

    public string FocusRing        => "0 0 0 3px rgba(14, 165, 233, 0.3)";
    public string FocusRingDanger  => "0 0 0 3px rgba(244, 63, 94, 0.3)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class GlassSemanticDark : IThemeSemantic
{
    // Dark mode — deep indigo glass
    public string BgDefault     => "rgba(15, 23, 42, 0.8)";
    public string BgSubtle      => "rgba(30, 41, 59, 0.6)";
    public string BgMuted       => "rgba(51, 65, 85, 0.5)";
    public string BgEmphasized  => "rgba(71, 85, 105, 0.4)";
    public string BgOverlay     => "rgba(0, 0, 0, 0.8)";
    public string BgGlass       => "rgba(255, 255, 255, 0.05)";
    public string BorderGlass   => "rgba(255, 255, 255, 0.12)";
    public string BlurGlass     => "16px";

    public string Surface        => "rgba(255, 255, 255, 0.05)";
    public string SurfaceRaised  => "rgba(255, 255, 255, 0.10)";
    public string SurfaceOverlay => "rgba(255, 255, 255, 0.15)";

    public string FgDefault   => "#f8fafc";
    public string FgSubtle    => "#cbd5e1";
    public string FgMuted     => "#94a3b8";
    public string FgDisabled  => "#64748b";
    public string FgInverse   => "#0f172a";
    public string FgLink      => "#38bdf8";
    public string FgLinkHover => "#7dd3fc";

    public string BorderDefault => "rgba(255, 255, 255, 0.1)";
    public string BorderSubtle  => "rgba(255, 255, 255, 0.05)";
    public string BorderStrong  => "rgba(255, 255, 255, 0.2)";
    public string BorderFocus   => "#38bdf8";
    public string Divider       => "rgba(255, 255, 255, 0.08)";

    public string ColorPrimary        => "#38bdf8";
    public string ColorPrimarySubtle  => "rgba(56, 189, 248, 0.15)";
    public string ColorPrimaryMuted   => "rgba(56, 189, 248, 0.25)";
    public string ColorPrimaryHover   => "#7dd3fc";
    public string ColorPrimaryActive  => "#bae6fd";
    public string ColorPrimaryFg      => "#082f49";

    public string ColorSuccess        => "#34d399";
    public string ColorSuccessSubtle  => "rgba(52, 211, 153, 0.15)";
    public string ColorSuccessHover   => "#6ee7b7";
    public string ColorSuccessFg      => "#064e3b";

    public string ColorDanger         => "#fb7185";
    public string ColorDangerSubtle   => "rgba(251, 113, 133, 0.15)";
    public string ColorDangerHover    => "#fda4af";
    public string ColorDangerFg       => "#ffffff";

    public string ColorWarning        => "#fbbf24";
    public string ColorWarningSubtle  => "rgba(251, 191, 36, 0.15)";
    public string ColorWarningHover   => "#fcd34d";
    public string ColorWarningFg      => "#451a03";

    public string ColorInfo           => "#60a5fa";
    public string ColorInfoSubtle     => "rgba(96, 165, 250, 0.15)";
    public string ColorInfoHover      => "#93c5fd";
    public string ColorInfoFg         => "#1e3a8a";

    public string Font     => "system-ui, -apple-system, sans-serif";
    public string FontMono => "ui-monospace, monospace";
    public string TextSm   => "0.875rem";
    public string TextBase => "1rem";
    public string TextLg   => "1.125rem";

    public string ShadowXs => "0 1px 2px rgba(0, 0, 0, 0.5)";
    public string ShadowSm => "0 4px 6px rgba(0, 0, 0, 0.5)";
    public string ShadowMd => "0 10px 15px rgba(0, 0, 0, 0.6)";
    public string ShadowLg => "0 20px 25px rgba(0, 0, 0, 0.7)";
    public string ShadowXl => "0 25px 50px rgba(0, 0, 0, 0.8)";

    public string RadiusSm   => "8px";
    public string RadiusMd   => "12px";
    public string RadiusLg   => "16px";
    public string RadiusXl   => "24px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "100ms";
    public string TransitionBase => "200ms";
    public string TransitionSlow => "400ms";

    public string FocusRing       => "0 0 0 3px rgba(56, 189, 248, 0.3)";
    public string FocusRingDanger => "0 0 0 3px rgba(251, 113, 133, 0.3)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class GlassComponents : IThemeComponents
{
    public string BtnRadius       => "12px";
    public string BtnFontSize     => "0.875rem";
    public string BtnFontWeight   => "600";
    public string BtnHeight       => "40px";
    public string BtnHeightSm     => "32px";
    public string BtnHeightLg     => "48px";

    public string InputRadius     => "12px";
    public string InputFontSize   => "0.875rem";
    public string InputHeight     => "40px";
    public string InputHeightSm   => "32px";
    public string InputHeightLg   => "48px";

    public string CardRadius      => "20px";
    public string CardPadding     => "24px";
    public string CardBorderColor => "var(--sg-border-glass)";
    public string CardBg          => "var(--sg-bg-glass)";

    public string ModalRadius     => "24px";

    public string TableRadius          => "16px";
    public string TableHeaderFontWeight => "600";

    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth     => "260px";

    public string HeaderBg      => "rgba(255, 255, 255, 0.2)";
    public string HeaderFg      => "var(--sg-fg)";
    public string NavBg         => "transparent";
    public string NavFg         => "var(--sg-fg-subtle)";
    public string NavActiveBg   => "rgba(255, 255, 255, 0.25)";
    public string NavActiveFg   => "var(--sg-fg)";
}
