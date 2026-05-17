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
           GLASSMORPHISM — Vivid gradient background
           ═══════════════════════════════════════════════════ */
        :root {
            --sg-glass-blur:        blur(20px);
            --sg-glass-blur-heavy:  blur(32px);
            /* Light: near-transparent white glass on vivid blue bg */
            --sg-glass-bg-light:    rgba(255, 255, 255, 0.13);
            --sg-glass-bg-medium:   rgba(255, 255, 255, 0.20);
            --sg-glass-bg-strong:   rgba(255, 255, 255, 0.32);
            --sg-glass-border:      rgba(255, 255, 255, 0.45);
            --sg-glass-border-soft: rgba(255, 255, 255, 0.25);
            --sg-glass-shadow:      0 8px 32px rgba(0, 100, 200, 0.18), 0 2px 8px rgba(0,0,0,0.10);
            --sg-glass-shadow-lg:   0 20px 60px rgba(0, 100, 200, 0.25), 0 4px 16px rgba(0,0,0,0.12);
        }

        [data-theme="dark"] {
            --sg-glass-bg-light:    rgba(0, 30, 80, 0.40);
            --sg-glass-bg-medium:   rgba(0, 30, 80, 0.55);
            --sg-glass-bg-strong:   rgba(0, 30, 80, 0.70);
            --sg-glass-border:      rgba(255, 255, 255, 0.18);
            --sg-glass-border-soft: rgba(255, 255, 255, 0.10);
            --sg-glass-shadow:      0 8px 32px rgba(0, 0, 0, 0.50);
            --sg-glass-shadow-lg:   0 20px 60px rgba(0, 0, 0, 0.65);
        }

        /* ── Vivid gradient background ─────────────────────── */
        [data-theme-id="superui-glass"] .sui-shell,
        [data-theme-id="superui-glass"] .sui-main,
        [data-theme-id="superui-glass"] .sui-content {
            background: transparent !important;
        }

        /* Light mode — насыщенный голубой фон + плавающие шары */
        [data-theme-id="superui-glass"] .sui-shell {
            background:
                /* Floating balls — синие */
                radial-gradient(circle at 8%  18%, #1565C0 0%, #1565C0 5%, transparent 5.5%),
                radial-gradient(circle at 22% 72%, #1976D2 0%, #1976D2 3.5%, transparent 4%),
                radial-gradient(circle at 88% 12%, #0D47A1 0%, #0D47A1 6%, transparent 6.5%),
                radial-gradient(circle at 75% 55%, #1E88E5 0%, #1E88E5 4%, transparent 4.5%),
                radial-gradient(circle at 45% 85%, #1565C0 0%, #1565C0 3%, transparent 3.5%),
                /* Floating balls — зелёные/бирюзовые */
                radial-gradient(circle at 35% 10%, #00C853 0%, #00C853 4%, transparent 4.5%),
                radial-gradient(circle at 92% 68%, #00BFA5 0%, #00BFA5 5%, transparent 5.5%),
                radial-gradient(circle at 60% 30%, #00E676 0%, #00E676 2.5%, transparent 3%),
                radial-gradient(circle at 15% 90%, #1DE9B6 0%, #1DE9B6 3.5%, transparent 4%),
                /* Большие мягкие шары на фоне */
                radial-gradient(circle at 30% 60%, rgba(21, 101, 192, 0.70) 0%, transparent 28%),
                radial-gradient(circle at 70% 40%, rgba(0, 200, 83, 0.50) 0%, transparent 22%),
                radial-gradient(circle at 85% 80%, rgba(30, 136, 229, 0.60) 0%, transparent 25%),
                radial-gradient(circle at 10% 40%, rgba(0, 191, 165, 0.40) 0%, transparent 20%),
                /* Основной голубой фон */
                linear-gradient(160deg, #29B6F6 0%, #039BE5 40%, #0288D1 70%, #0277BD 100%) !important;
            background-attachment: fixed !important;
        }

        /* Dark mode */
        [data-theme-id="superui-glass"][data-theme="dark"] .sui-shell {
            background:
                radial-gradient(circle at 8%  18%, #0D47A1 0%, #0D47A1 5%, transparent 5.5%),
                radial-gradient(circle at 88% 12%, #01579B 0%, #01579B 6%, transparent 6.5%),
                radial-gradient(circle at 75% 55%, #0277BD 0%, #0277BD 4%, transparent 4.5%),
                radial-gradient(circle at 35% 10%, #00695C 0%, #00695C 4%, transparent 4.5%),
                radial-gradient(circle at 92% 68%, #004D40 0%, #004D40 5%, transparent 5.5%),
                radial-gradient(circle at 30% 60%, rgba(13, 71, 161, 0.70) 0%, transparent 28%),
                radial-gradient(circle at 70% 40%, rgba(0, 105, 92, 0.50) 0%, transparent 22%),
                linear-gradient(160deg, #01579B 0%, #0D47A1 40%, #002171 70%, #000a1f 100%) !important;
            background-attachment: fixed !important;
        }

        /* ── Glass surfaces — ultra-thin frosted ──────────── */
        [data-theme-id="superui-glass"] .sgc-card,
        [data-theme-id="superui-glass"] .sgc-panel {
            background: rgba(255, 255, 255, 0.15) !important;
            backdrop-filter: var(--sg-glass-blur) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur) !important;
            border: 1px solid rgba(255, 255, 255, 0.40) !important;
            box-shadow: 0 8px 32px rgba(0, 80, 180, 0.20), inset 0 1px 0 rgba(255,255,255,0.30) !important;
        }

        [data-theme-id="superui-glass"] .sgc-modal-content,
        [data-theme-id="superui-glass"] .sgc-drawer-content {
            background: rgba(255, 255, 255, 0.22) !important;
            backdrop-filter: var(--sg-glass-blur-heavy) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur-heavy) !important;
            border: 1px solid rgba(255, 255, 255, 0.50) !important;
            box-shadow: 0 20px 60px rgba(0, 80, 180, 0.30), inset 0 1px 0 rgba(255,255,255,0.35) !important;
        }

        [data-theme-id="superui-glass"] .sgc-dropdown-menu,
        [data-theme-id="superui-glass"] .sgc-popover-content,
        [data-theme-id="superui-glass"] .sgc-tooltip {
            background: rgba(255, 255, 255, 0.25) !important;
            backdrop-filter: var(--sg-glass-blur) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur) !important;
            border: 1px solid rgba(255, 255, 255, 0.45) !important;
            box-shadow: 0 8px 32px rgba(0, 80, 180, 0.22), inset 0 1px 0 rgba(255,255,255,0.30) !important;
        }

        /* ── Header & Nav ──────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-header,
        [data-theme-id="superui-glass"] .sgc-nav,
        [data-theme-id="superui-glass"] .sgc-sidebar {
            background: rgba(255, 255, 255, 0.12) !important;
            backdrop-filter: var(--sg-glass-blur-heavy) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur-heavy) !important;
            border-color: rgba(255, 255, 255, 0.30) !important;
        }

        /* ── Inputs ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-input,
        [data-theme-id="superui-glass"] .sgc-select,
        [data-theme-id="superui-glass"] .sgc-textarea,
        [data-theme-id="superui-glass"] .sgc-combo-control {
            background: rgba(255, 255, 255, 0.18) !important;
            backdrop-filter: blur(12px) !important;
            -webkit-backdrop-filter: blur(12px) !important;
            border: 1px solid rgba(255, 255, 255, 0.45) !important;
            color: #fff !important;
            box-shadow: inset 0 1px 0 rgba(255,255,255,0.20) !important;
        }

        [data-theme-id="superui-glass"] .sgc-input::placeholder,
        [data-theme-id="superui-glass"] .sgc-textarea::placeholder {
            color: rgba(255, 255, 255, 0.50) !important;
        }

        [data-theme-id="superui-glass"] .sgc-input:focus,
        [data-theme-id="superui-glass"] .sgc-select:focus,
        [data-theme-id="superui-glass"] .sgc-textarea:focus {
            background: rgba(255, 255, 255, 0.28) !important;
            border-color: rgba(255, 255, 255, 0.75) !important;
            box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.20), inset 0 1px 0 rgba(255,255,255,0.25) !important;
        }

        /* ── Buttons ───────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-btn {
            backdrop-filter: blur(12px) !important;
            -webkit-backdrop-filter: blur(12px) !important;
        }

        /* Primary — яркий синий→зелёный градиент */
        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary {
            background: linear-gradient(135deg, #1E88E5 0%, #00C853 100%) !important;
            border: 1px solid rgba(255, 255, 255, 0.40) !important;
            box-shadow: 0 4px 20px rgba(0, 150, 100, 0.40), inset 0 1px 0 rgba(255,255,255,0.25) !important;
            color: #fff !important;
            font-weight: 600 !important;
        }

        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: linear-gradient(135deg, #1565C0 0%, #00A040 100%) !important;
            box-shadow: 0 6px 28px rgba(0, 150, 100, 0.55), inset 0 1px 0 rgba(255,255,255,0.25) !important;
            transform: translateY(-2px) !important;
        }

        /* Default — стеклянная кнопка */
        [data-theme-id="superui-glass"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success) {
            background: rgba(255, 255, 255, 0.18) !important;
            border: 1px solid rgba(255, 255, 255, 0.45) !important;
            color: #fff !important;
            box-shadow: inset 0 1px 0 rgba(255,255,255,0.20) !important;
        }

        [data-theme-id="superui-glass"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):hover:not(:disabled) {
            background: rgba(255, 255, 255, 0.28) !important;
            border-color: rgba(255, 255, 255, 0.65) !important;
            transform: translateY(-1px) !important;
        }

        /* ── Table ─────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-table-wrap,
        [data-theme-id="superui-glass"] .sgc-grid-wrap {
            background: rgba(255, 255, 255, 0.12) !important;
            backdrop-filter: var(--sg-glass-blur) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur) !important;
            border: 1px solid rgba(255, 255, 255, 0.35) !important;
            border-radius: var(--sg-radius-xl) !important;
        }

        [data-theme-id="superui-glass"] .sgc-table thead th,
        [data-theme-id="superui-glass"] .sgc-grid-header-cell {
            background: rgba(255, 255, 255, 0.10) !important;
            border-bottom: 1px solid rgba(255, 255, 255, 0.30) !important;
            color: rgba(255, 255, 255, 0.85) !important;
        }

        [data-theme-id="superui-glass"] .sgc-table tbody tr:hover td {
            background: rgba(255, 255, 255, 0.08) !important;
        }

        /* ── Tabs ──────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-tabs-strip {
            background: rgba(255, 255, 255, 0.12) !important;
            backdrop-filter: var(--sg-glass-blur) !important;
            -webkit-backdrop-filter: var(--sg-glass-blur) !important;
            border: 1px solid rgba(255, 255, 255, 0.30) !important;
            border-radius: var(--sg-radius-full) !important;
            padding: 4px !important;
        }

        [data-theme-id="superui-glass"] .sgc-tab-item.is-active {
            background: rgba(255, 255, 255, 0.28) !important;
            border: 1px solid rgba(255, 255, 255, 0.50) !important;
            box-shadow: 0 4px 16px rgba(0, 80, 180, 0.20), inset 0 1px 0 rgba(255,255,255,0.30) !important;
            color: #fff !important;
        }

        /* ── Dropdown items ────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-dropdown-item {
            color: #fff !important;
        }
        [data-theme-id="superui-glass"] .sgc-dropdown-item:hover {
            background: rgba(255, 255, 255, 0.15) !important;
        }

        /* ── Scrollbar ──────────────────────────────────────── */
        [data-theme-id="superui-glass"] ::-webkit-scrollbar-thumb {
            background: rgba(255, 255, 255, 0.30) !important;
            border-radius: 9999px !important;
        }
        [data-theme-id="superui-glass"] ::-webkit-scrollbar-track {
            background: rgba(255, 255, 255, 0.08) !important;
        }

        /* ── Dark mode overrides ────────────────────────────── */
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-card,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-panel {
            background: rgba(0, 30, 80, 0.45) !important;
            border-color: rgba(255, 255, 255, 0.15) !important;
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-header,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-sidebar {
            background: rgba(0, 20, 60, 0.60) !important;
            border-color: rgba(255, 255, 255, 0.12) !important;
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-dropdown-menu,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-popover-content {
            background: rgba(0, 20, 60, 0.75) !important;
            border-color: rgba(255, 255, 255, 0.18) !important;
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-input,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-select,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-textarea {
            background: rgba(0, 30, 80, 0.40) !important;
            border-color: rgba(255, 255, 255, 0.20) !important;
        }

        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success) {
            background: rgba(0, 30, 80, 0.40) !important;
            border-color: rgba(255, 255, 255, 0.20) !important;
        }
        """;
}

internal class GlassPrimitives : DefaultPrimitives { }

internal class GlassSemanticLight : IThemeSemantic
{
    // Backgrounds — ultra-thin glass on vivid blue
    public string BgDefault     => "rgba(255, 255, 255, 0.15)";
    public string BgSubtle      => "rgba(255, 255, 255, 0.10)";
    public string BgMuted       => "rgba(255, 255, 255, 0.08)";
    public string BgEmphasized  => "rgba(255, 255, 255, 0.22)";
    public string BgOverlay     => "rgba(0, 40, 100, 0.60)";
    public string BgGlass       => "rgba(255, 255, 255, 0.20)";
    public string BorderGlass   => "rgba(255, 255, 255, 0.40)";
    public string BlurGlass     => "12px";

    // Surfaces
    public string Surface        => "rgba(255, 255, 255, 0.18)";
    public string SurfaceRaised  => "rgba(255, 255, 255, 0.28)";
    public string SurfaceOverlay => "rgba(255, 255, 255, 0.38)";

    // Foreground — белый текст на голубом фоне
    public string FgDefault   => "#ffffff";
    public string FgSubtle    => "rgba(255, 255, 255, 0.85)";
    public string FgMuted     => "rgba(255, 255, 255, 0.60)";
    public string FgDisabled  => "rgba(255, 255, 255, 0.35)";
    public string FgInverse   => "#0277BD";
    public string FgLink      => "#B3E5FC";
    public string FgLinkHover => "#E1F5FE";

    // Borders — тонкие белые
    public string BorderDefault => "rgba(255, 255, 255, 0.40)";
    public string BorderSubtle  => "rgba(255, 255, 255, 0.22)";
    public string BorderStrong  => "rgba(255, 255, 255, 0.65)";
    public string BorderFocus   => "rgba(255, 255, 255, 0.85)";
    public string Divider       => "rgba(255, 255, 255, 0.20)";

    // Primary — синий→зелёный
    public string ColorPrimary        => "#00E676";
    public string ColorPrimarySubtle  => "rgba(0, 230, 118, 0.20)";
    public string ColorPrimaryMuted   => "rgba(0, 230, 118, 0.35)";
    public string ColorPrimaryHover   => "#69F0AE";
    public string ColorPrimaryActive  => "#B9F6CA";
    public string ColorPrimaryFg      => "#003300";

    public string ColorSuccess        => "#00E676";
    public string ColorSuccessSubtle  => "rgba(0, 230, 118, 0.20)";
    public string ColorSuccessHover   => "#69F0AE";
    public string ColorSuccessFg      => "#003300";

    public string ColorDanger         => "#FF5252";
    public string ColorDangerSubtle   => "rgba(255, 82, 82, 0.20)";
    public string ColorDangerHover    => "#FF8A80";
    public string ColorDangerFg       => "#fff";

    public string ColorWarning        => "#FFD740";
    public string ColorWarningSubtle  => "rgba(255, 215, 64, 0.20)";
    public string ColorWarningHover   => "#FFE57F";
    public string ColorWarningFg      => "#1a1000";

    public string ColorInfo           => "#40C4FF";
    public string ColorInfoSubtle     => "rgba(64, 196, 255, 0.20)";
    public string ColorInfoHover      => "#80D8FF";
    public string ColorInfoFg         => "#003050";

    public string Font     => "'Inter', system-ui, sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm   => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg   => "1rem";

    public string ShadowXs => "0 1px 4px rgba(0, 80, 180, 0.20)";
    public string ShadowSm => "0 4px 16px rgba(0, 80, 180, 0.25)";
    public string ShadowMd => "0 8px 32px rgba(0, 80, 180, 0.30)";
    public string ShadowLg => "0 16px 48px rgba(0, 80, 180, 0.35)";
    public string ShadowXl => "0 24px 64px rgba(0, 80, 180, 0.40)";

    public string RadiusSm   => "10px";
    public string RadiusMd   => "14px";
    public string RadiusLg   => "20px";
    public string RadiusXl   => "28px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "120ms";
    public string TransitionBase => "220ms";
    public string TransitionSlow => "350ms";

    public string FocusRing        => "0 0 0 3px rgba(255, 255, 255, 0.40)";
    public string FocusRingDanger  => "0 0 0 3px rgba(255, 82, 82, 0.50)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class GlassSemanticDark : IThemeSemantic
{
    // Dark mode — deep space with glass
    public string BgDefault     => "rgba(10, 6, 30, 0.85)";
    public string BgSubtle      => "rgba(20, 12, 50, 0.60)";
    public string BgMuted       => "rgba(30, 20, 65, 0.50)";
    public string BgEmphasized  => "rgba(45, 30, 90, 0.45)";
    public string BgOverlay     => "rgba(0, 0, 0, 0.75)";
    public string BgGlass       => "rgba(255, 255, 255, 0.07)";
    public string BorderGlass   => "rgba(255, 255, 255, 0.15)";
    public string BlurGlass     => "12px";

    public string Surface        => "rgba(255, 255, 255, 0.07)";
    public string SurfaceRaised  => "rgba(255, 255, 255, 0.12)";
    public string SurfaceOverlay => "rgba(255, 255, 255, 0.18)";

    public string FgDefault   => "#f1f5f9";
    public string FgSubtle    => "rgba(241, 245, 249, 0.75)";
    public string FgMuted     => "rgba(241, 245, 249, 0.45)";
    public string FgDisabled  => "rgba(241, 245, 249, 0.25)";
    public string FgInverse   => "#0f172a";
    public string FgLink      => "#a78bfa";
    public string FgLinkHover => "#c4b5fd";

    public string BorderDefault => "rgba(255, 255, 255, 0.12)";
    public string BorderSubtle  => "rgba(255, 255, 255, 0.07)";
    public string BorderStrong  => "rgba(255, 255, 255, 0.25)";
    public string BorderFocus   => "rgba(167, 139, 250, 0.70)";
    public string Divider       => "rgba(255, 255, 255, 0.08)";

    public string ColorPrimary        => "#a78bfa";
    public string ColorPrimarySubtle  => "rgba(167, 139, 250, 0.18)";
    public string ColorPrimaryMuted   => "rgba(167, 139, 250, 0.30)";
    public string ColorPrimaryHover   => "#c4b5fd";
    public string ColorPrimaryActive  => "#ddd6fe";
    public string ColorPrimaryFg      => "#1e1b4b";

    public string ColorSuccess        => "#34d399";
    public string ColorSuccessSubtle  => "rgba(52, 211, 153, 0.18)";
    public string ColorSuccessHover   => "#6ee7b7";
    public string ColorSuccessFg      => "#064e3b";

    public string ColorDanger         => "#fb7185";
    public string ColorDangerSubtle   => "rgba(251, 113, 133, 0.18)";
    public string ColorDangerHover    => "#fda4af";
    public string ColorDangerFg       => "#fff";

    public string ColorWarning        => "#fbbf24";
    public string ColorWarningSubtle  => "rgba(251, 191, 36, 0.18)";
    public string ColorWarningHover   => "#fcd34d";
    public string ColorWarningFg      => "#1c1917";

    public string ColorInfo           => "#38bdf8";
    public string ColorInfoSubtle     => "rgba(56, 189, 248, 0.18)";
    public string ColorInfoHover      => "#7dd3fc";
    public string ColorInfoFg         => "#0c4a6e";

    public string Font     => "'Inter', system-ui, sans-serif";
    public string FontMono => "'JetBrains Mono', monospace";
    public string TextSm   => "0.8125rem";
    public string TextBase => "0.875rem";
    public string TextLg   => "1rem";

    public string ShadowXs => "0 1px 4px rgba(0, 0, 0, 0.40)";
    public string ShadowSm => "0 4px 16px rgba(0, 0, 0, 0.50)";
    public string ShadowMd => "0 8px 32px rgba(0, 0, 0, 0.55)";
    public string ShadowLg => "0 16px 48px rgba(0, 0, 0, 0.60)";
    public string ShadowXl => "0 24px 64px rgba(0, 0, 0, 0.70)";

    public string RadiusSm   => "10px";
    public string RadiusMd   => "14px";
    public string RadiusLg   => "20px";
    public string RadiusXl   => "28px";
    public string RadiusFull => "9999px";

    public string TransitionFast => "120ms";
    public string TransitionBase => "220ms";
    public string TransitionSlow => "350ms";

    public string FocusRing       => "0 0 0 3px rgba(167, 139, 250, 0.40)";
    public string FocusRingDanger => "0 0 0 3px rgba(251, 113, 133, 0.40)";

    public int ZDropdown => 1000;
    public int ZSticky   => 1020;
    public int ZModal    => 1050;
    public int ZToast    => 1070;
    public int ZTooltip  => 1100;
}

internal class GlassComponents : IThemeComponents
{
    public string BtnRadius       => "14px";
    public string BtnFontSize     => "0.875rem";
    public string BtnFontWeight   => "600";
    public string BtnHeight       => "40px";
    public string BtnHeightSm     => "32px";
    public string BtnHeightLg     => "48px";

    public string InputRadius     => "14px";
    public string InputFontSize   => "0.875rem";
    public string InputHeight     => "40px";
    public string InputHeightSm   => "32px";
    public string InputHeightLg   => "48px";

    public string CardRadius      => "24px";
    public string CardPadding     => "24px";
    public string CardBorderColor => "var(--sg-glass-border)";
    public string CardBg          => "var(--sg-glass-bg-medium)";

    public string ModalRadius     => "28px";

    public string TableRadius          => "20px";
    public string TableHeaderFontWeight => "700";

    public string TabsIndicatorHeight => "2px";
    public string TooltipMaxWidth     => "260px";

    public string HeaderBg      => "var(--sg-glass-bg-medium)";
    public string HeaderFg      => "#ffffff";
    public string NavBg         => "var(--sg-glass-bg-medium)";
    public string NavFg         => "rgba(255, 255, 255, 0.80)";
    public string NavActiveBg   => "rgba(255, 255, 255, 0.25)";
    public string NavActiveFg   => "#ffffff";
}
