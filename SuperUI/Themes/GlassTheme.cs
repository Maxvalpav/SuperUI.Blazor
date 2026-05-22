namespace SuperUI.Themes;

/// <summary>
/// Glassmorphism theme for SuperUI.
/// Vivid multi-color gradient backdrop with heavily blurred frosted surfaces.
/// Inspired by Apple "Big Sur" / dribbble-style glassmorphism: vibrant blobs,
/// high transparency, white-rim highlights, ambient color bleed through panels.
/// </summary>
public sealed class GlassTheme : ThemeBase
{
    public override string Id => "superui-glass";
    public override string Name => "Glassmorphism";
    public override string? Description => "Матовое стекло, многоцветный градиентный фон, прозрачные поверхности с белой подсветкой.";
    public override string Version => "2.1.0";

    protected override IThemePrimitives CreatePrimitives() => new GlassPrimitives();
    protected override IThemeSemantic CreateLight() => new GlassSemanticLight();
    protected override IThemeSemantic? CreateDark() => new GlassSemanticDark();
    protected override IThemeComponents? CreateComponents() => new GlassComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════════════
           GLASSMORPHISM — Vivid gradient + frosted surfaces
           ═══════════════════════════════════════════════════════════════ */

        [data-theme-id="superui-glass"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            letter-spacing: -0.005em;
            color: var(--sg-fg);
        }

        /* ── Vivid multi-color backdrop ─────────────────────────────── */
        /* Light: pink / violet / cyan blobs — classic glassmorphism palette */
        [data-theme-id="superui-glass"] .sui-shell {
            background:
                radial-gradient(at 12% 18%,  rgba(255, 99, 196, 0.45) 0px, transparent 50%),
                radial-gradient(at 82% 12%,  rgba(122, 90, 248, 0.45) 0px, transparent 55%),
                radial-gradient(at 28% 78%,  rgba(56, 189, 248, 0.42) 0px, transparent 50%),
                radial-gradient(at 88% 78%,  rgba(255, 184, 108, 0.40) 0px, transparent 55%),
                linear-gradient(135deg, #fef3ff 0%, #e0f2fe 100%) !important;
            background-attachment: fixed !important;
            min-height: 100vh;
        }
        [data-theme-id="superui-glass"] .sui-main,
        [data-theme-id="superui-glass"] .sui-content {
            background: transparent !important;
        }

        /* Dark: deep purple / fuchsia / cyan glow */
        [data-theme-id="superui-glass"][data-theme="dark"] .sui-shell {
            background:
                radial-gradient(at 12% 18%,  rgba(217, 70, 239, 0.35) 0px, transparent 50%),
                radial-gradient(at 82% 12%,  rgba(99, 102, 241, 0.40) 0px, transparent 55%),
                radial-gradient(at 28% 78%,  rgba(56, 189, 248, 0.30) 0px, transparent 50%),
                radial-gradient(at 88% 82%,  rgba(244, 114, 182, 0.28) 0px, transparent 55%),
                linear-gradient(160deg, #08060f 0%, #0b1020 100%) !important;
            background-attachment: fixed !important;
        }

        /* ── Frosted cards / panels ────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-card,
        [data-theme-id="superui-glass"] .sgc-panel {
            background: var(--sg-bg-glass) !important;
            backdrop-filter: blur(var(--sg-blur-glass)) saturate(180%);
            -webkit-backdrop-filter: blur(var(--sg-blur-glass)) saturate(180%);
            border: 1px solid var(--sg-border-glass);
            box-shadow:
                0 10px 40px -10px rgba(31, 38, 135, 0.25),
                inset 0 1px 0 rgba(255, 255, 255, 0.55),
                inset 0 0 0 1px rgba(255, 255, 255, 0.08);
            border-radius: 20px;
        }
        [data-theme-id="superui-glass"] .sgc-card-header {
            border-bottom: 1px solid rgba(255, 255, 255, 0.20);
            background: transparent;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-card,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-panel {
            box-shadow:
                0 10px 40px -10px rgba(0, 0, 0, 0.6),
                inset 0 1px 0 rgba(255, 255, 255, 0.10),
                inset 0 0 0 1px rgba(255, 255, 255, 0.04);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-card-header {
            border-bottom-color: rgba(255, 255, 255, 0.06);
        }

        /* ── Modal / Drawer ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-modal,
        [data-theme-id="superui-glass"] .sgc-modal-content,
        [data-theme-id="superui-glass"] .sgc-drawer-content {
            background: rgba(255, 255, 255, 0.55) !important;
            backdrop-filter: blur(40px) saturate(180%);
            -webkit-backdrop-filter: blur(40px) saturate(180%);
            border: 1px solid rgba(255, 255, 255, 0.6);
            box-shadow:
                0 30px 80px -20px rgba(31, 38, 135, 0.35),
                inset 0 1px 0 rgba(255, 255, 255, 0.6);
            border-radius: 24px;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-modal,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-modal-content,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-drawer-content {
            background: rgba(20, 20, 30, 0.55) !important;
            border-color: rgba(255, 255, 255, 0.10);
            box-shadow:
                0 30px 80px -20px rgba(0, 0, 0, 0.8),
                inset 0 1px 0 rgba(255, 255, 255, 0.08);
        }

        /* ── Inputs ────────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-input,
        [data-theme-id="superui-glass"] .sgc-select,
        [data-theme-id="superui-glass"] .sgc-textarea {
            background: rgba(255, 255, 255, 0.35) !important;
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.5);
            box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.6);
            transition: all var(--sg-transition-base);
            border-radius: 12px;
        }
        [data-theme-id="superui-glass"] .sgc-input:focus,
        [data-theme-id="superui-glass"] .sgc-select:focus,
        [data-theme-id="superui-glass"] .sgc-textarea:focus {
            background: rgba(255, 255, 255, 0.55) !important;
            border-color: var(--sg-color-primary);
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.7),
                0 0 0 3px rgba(14, 165, 233, 0.25);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-input,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-select,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-textarea {
            background: rgba(255, 255, 255, 0.06) !important;
            border-color: rgba(255, 255, 255, 0.12);
            box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.08);
            color: var(--sg-fg);
        }

        /* ── Buttons ───────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-btn {
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border-radius: 12px;
            transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);
        }
        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary {
            background: linear-gradient(135deg, #a855f7 0%, #ec4899 50%, #38bdf8 100%) !important;
            background-size: 200% 200%;
            background-position: 0% 50%;
            border: 1px solid rgba(255, 255, 255, 0.25);
            box-shadow:
                0 8px 24px -4px rgba(168, 85, 247, 0.45),
                inset 0 1px 0 rgba(255, 255, 255, 0.4);
            color: #fff;
            font-weight: 600;
        }
        [data-theme-id="superui-glass"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background-position: 100% 50%;
            transform: translateY(-1px);
            box-shadow:
                0 12px 32px -4px rgba(236, 72, 153, 0.55),
                inset 0 1px 0 rgba(255, 255, 255, 0.5);
        }
        [data-theme-id="superui-glass"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success) {
            background: rgba(255, 255, 255, 0.30);
            border: 1px solid rgba(255, 255, 255, 0.5);
            box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.5);
            color: var(--sg-fg);
        }
        [data-theme-id="superui-glass"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):hover:not(:disabled) {
            background: rgba(255, 255, 255, 0.45);
            transform: translateY(-1px);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success) {
            background: rgba(255, 255, 255, 0.08);
            border-color: rgba(255, 255, 255, 0.12);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):hover:not(:disabled) {
            background: rgba(255, 255, 255, 0.14);
        }

        /* ── Top bar / Side nav ────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-header {
            background: rgba(255, 255, 255, 0.30) !important;
            backdrop-filter: blur(24px) saturate(180%);
            -webkit-backdrop-filter: blur(24px) saturate(180%);
            border-bottom: 1px solid rgba(255, 255, 255, 0.30);
        }
        [data-theme-id="superui-glass"] .sgc-nav {
            background: rgba(255, 255, 255, 0.22) !important;
            backdrop-filter: blur(24px) saturate(180%);
            -webkit-backdrop-filter: blur(24px) saturate(180%);
            border-right: 1px solid rgba(255, 255, 255, 0.25);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-header,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav {
            background: rgba(15, 12, 25, 0.45) !important;
            border-color: rgba(255, 255, 255, 0.06);
        }

        [data-theme-id="superui-glass"] .sgc-nav-link {
            border-left: none !important;
            margin: 3px 8px;
            border-radius: 12px;
            color: var(--sg-fg-subtle);
            transition: all 180ms ease;
        }
        [data-theme-id="superui-glass"] .sgc-nav-link:hover {
            background: rgba(255, 255, 255, 0.30);
            color: var(--sg-fg);
        }
        [data-theme-id="superui-glass"] .sgc-nav-link.active {
            background: rgba(255, 255, 255, 0.45);
            color: var(--sg-fg);
            box-shadow:
                0 4px 12px rgba(168, 85, 247, 0.15),
                inset 0 1px 0 rgba(255, 255, 255, 0.6);
            font-weight: 600;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav-link:hover {
            background: rgba(255, 255, 255, 0.08);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-nav-link.active {
            background: rgba(255, 255, 255, 0.12);
            box-shadow:
                0 4px 12px rgba(0, 0, 0, 0.4),
                inset 0 1px 0 rgba(255, 255, 255, 0.10);
        }
        [data-theme-id="superui-glass"] .sgc-nav-section {
            padding: 14px 20px 6px;
            color: var(--sg-color-primary);
            font-size: 10px;
            opacity: 0.85;
            letter-spacing: 0.06em;
        }

        /* ── Alerts ────────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-alert {
            background: rgba(255, 255, 255, 0.28) !important;
            backdrop-filter: blur(14px);
            -webkit-backdrop-filter: blur(14px);
            border: 1px solid rgba(255, 255, 255, 0.35) !important;
            border-left-width: 4px !important;
            box-shadow: 0 6px 20px -6px rgba(31, 38, 135, 0.20);
            border-radius: 14px;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-alert {
            background: rgba(255, 255, 255, 0.06) !important;
            border-color: rgba(255, 255, 255, 0.10) !important;
        }

        /* ── Tabs ──────────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-tabs-strip {
            background: rgba(255, 255, 255, 0.18);
            backdrop-filter: blur(10px);
            -webkit-backdrop-filter: blur(10px);
            border: 1px solid rgba(255, 255, 255, 0.30);
            border-radius: 14px;
            padding: 4px;
            gap: 4px;
        }
        [data-theme-id="superui-glass"] .sgc-tab {
            border-radius: 10px;
            transition: all 180ms ease;
            border: 1px solid transparent;
        }
        [data-theme-id="superui-glass"] .sgc-tab.sgc-active {
            background: rgba(255, 255, 255, 0.55);
            border-color: rgba(255, 255, 255, 0.45);
            color: var(--sg-fg);
            box-shadow:
                0 4px 12px rgba(31, 38, 135, 0.1),
                inset 0 1px 0 rgba(255, 255, 255, 0.6);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-tabs-strip {
            background: rgba(255, 255, 255, 0.04);
            border-color: rgba(255, 255, 255, 0.06);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-tab.sgc-active {
            background: rgba(255, 255, 255, 0.10);
            border-color: rgba(255, 255, 255, 0.08);
        }

        /* ── Chips / Badges ────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-chip,
        [data-theme-id="superui-glass"] .sgc-badge {
            background: rgba(255, 255, 255, 0.30);
            backdrop-filter: blur(8px);
            border: 1px solid rgba(255, 255, 255, 0.40);
            border-radius: 999px;
        }
        [data-theme-id="superui-glass"] .sgc-chip.sgc-chip-selected {
            background: var(--sg-color-primary-subtle);
            border-color: var(--sg-color-primary);
        }
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-chip,
        [data-theme-id="superui-glass"][data-theme="dark"] .sgc-badge {
            background: rgba(255, 255, 255, 0.06);
            border-color: rgba(255, 255, 255, 0.10);
        }

        /* ── Table ─────────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] .sgc-table {
            background: transparent;
        }
        [data-theme-id="superui-glass"] .sgc-table thead th {
            background: rgba(255, 255, 255, 0.25);
            backdrop-filter: blur(8px);
            border-bottom-color: rgba(255, 255, 255, 0.30);
        }
        [data-theme-id="superui-glass"] .sgc-table tbody tr:hover td {
            background: rgba(255, 255, 255, 0.20);
        }

        /* ── Scrollbar ─────────────────────────────────────────────── */
        [data-theme-id="superui-glass"] ::-webkit-scrollbar {
            width: 10px; height: 10px;
        }
        [data-theme-id="superui-glass"] ::-webkit-scrollbar-thumb {
            background: rgba(0, 0, 0, 0.18);
            border-radius: 10px;
        }
        [data-theme-id="superui-glass"][data-theme="dark"] ::-webkit-scrollbar-thumb {
            background: rgba(255, 255, 255, 0.20);
        }
        """;
}

internal class GlassPrimitives : DefaultPrimitives { }

internal class GlassSemanticLight : DefaultSemanticLight
{
    // Backgrounds — soft frosted glass over vivid backdrop
    public override string BgDefault     => "rgba(255, 255, 255, 0.30)";
    public override string BgSubtle      => "rgba(255, 255, 255, 0.18)";
    public override string BgMuted       => "rgba(255, 255, 255, 0.12)";
    public override string BgEmphasized  => "rgba(255, 255, 255, 0.50)";
    public override string BgOverlay     => "rgba(15, 23, 42, 0.30)";
    public override string BgGlass       => "rgba(255, 255, 255, 0.22)";
    public override string BorderGlass   => "rgba(255, 255, 255, 0.45)";
    public override string BlurGlass     => "24px";

    public override string Surface        => "rgba(255, 255, 255, 0.28)";
    public override string SurfaceRaised  => "rgba(255, 255, 255, 0.40)";
    public override string SurfaceOverlay => "rgba(255, 255, 255, 0.60)";

    // Foreground — deep slate keeps contrast on bright blobs
    public override string FgDefault   => "#0f172a";
    public override string FgSubtle    => "#334155";
    public override string FgMuted     => "#475569";
    public override string FgDisabled  => "#94a3b8";
    public override string FgInverse   => "#ffffff";
    public override string FgLink      => "#7c3aed";
    public override string FgLinkHover => "#6d28d9";

    // Borders — translucent rim
    public override string BorderDefault => "rgba(15, 23, 42, 0.10)";
    public override string BorderSubtle  => "rgba(15, 23, 42, 0.05)";
    public override string BorderStrong  => "rgba(15, 23, 42, 0.22)";
    public override string BorderFocus   => "#a855f7";
    public override string Divider       => "rgba(15, 23, 42, 0.08)";

    // Vivid violet/fuchsia primary fits glass aesthetic
    public override string ColorPrimary        => "#a855f7";
    public override string ColorPrimarySubtle  => "rgba(168, 85, 247, 0.15)";
    public override string ColorPrimaryMuted   => "rgba(168, 85, 247, 0.28)";
    public override string ColorPrimaryHover   => "#9333ea";
    public override string ColorPrimaryActive  => "#7e22ce";
    public override string ColorPrimaryFg      => "#ffffff";

    public override string ColorSuccess        => "#10b981";
    public override string ColorSuccessSubtle  => "rgba(16, 185, 129, 0.18)";
    public override string ColorSuccessHover   => "#059669";
    public override string ColorSuccessFg      => "#ffffff";

    public override string ColorDanger         => "#ec4899";
    public override string ColorDangerSubtle   => "rgba(236, 72, 153, 0.18)";
    public override string ColorDangerHover    => "#db2777";
    public override string ColorDangerFg       => "#ffffff";

    public override string ColorWarning        => "#f59e0b";
    public override string ColorWarningSubtle  => "rgba(245, 158, 11, 0.20)";
    public override string ColorWarningHover   => "#d97706";
    public override string ColorWarningFg      => "#451a03";

    public override string ColorInfo           => "#0ea5e9";
    public override string ColorInfoSubtle     => "rgba(14, 165, 233, 0.18)";
    public override string ColorInfoHover      => "#0284c7";
    public override string ColorInfoFg         => "#ffffff";

    public override string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public override string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public override string TextSm   => "0.8125rem";
    public override string TextBase => "0.9375rem";
    public override string TextLg   => "1.0625rem";

    // Soft, diffused shadows — colored bleed
    public override string ShadowXs => "0 2px 8px -2px rgba(31, 38, 135, 0.08)";
    public override string ShadowSm => "0 6px 18px -6px rgba(31, 38, 135, 0.12)";
    public override string ShadowMd => "0 12px 32px -8px rgba(31, 38, 135, 0.18)";
    public override string ShadowLg => "0 20px 48px -10px rgba(31, 38, 135, 0.25)";
    public override string ShadowXl => "0 30px 80px -16px rgba(31, 38, 135, 0.35)";

    public override string RadiusSm   => "8px";
    public override string RadiusMd   => "12px";
    public override string RadiusLg   => "18px";
    public override string RadiusXl   => "24px";
    public override string RadiusFull => "9999px";

    public override string TransitionFast => "120ms cubic-bezier(0.4, 0, 0.2, 1)";
    public override string TransitionBase => "200ms cubic-bezier(0.4, 0, 0.2, 1)";
    public override string TransitionSlow => "400ms cubic-bezier(0.4, 0, 0.2, 1)";

    public override string FocusRing       => "0 0 0 3px rgba(168, 85, 247, 0.30)";
    public override string FocusRingDanger => "0 0 0 3px rgba(236, 72, 153, 0.30)";
}

internal class GlassSemanticDark : DefaultSemanticDark
{
    public override string BgDefault     => "rgba(20, 18, 38, 0.55)";
    public override string BgSubtle      => "rgba(255, 255, 255, 0.04)";
    public override string BgMuted       => "rgba(255, 255, 255, 0.07)";
    public override string BgEmphasized  => "rgba(255, 255, 255, 0.12)";
    public override string BgOverlay     => "rgba(0, 0, 0, 0.65)";
    public override string BgGlass       => "rgba(255, 255, 255, 0.06)";
    public override string BorderGlass   => "rgba(255, 255, 255, 0.12)";
    public override string BlurGlass     => "20px";

    public override string Surface        => "rgba(255, 255, 255, 0.05)";
    public override string SurfaceRaised  => "rgba(255, 255, 255, 0.10)";
    public override string SurfaceOverlay => "rgba(255, 255, 255, 0.14)";

    public override string FgDefault   => "#f8fafc";
    public override string FgSubtle    => "#cbd5e1";
    public override string FgMuted     => "#94a3b8";
    public override string FgDisabled  => "#64748b";
    public override string FgInverse   => "#0f172a";
    public override string FgLink      => "#c4b5fd";
    public override string FgLinkHover => "#ddd6fe";

    public override string BorderDefault => "rgba(255, 255, 255, 0.10)";
    public override string BorderSubtle  => "rgba(255, 255, 255, 0.05)";
    public override string BorderStrong  => "rgba(255, 255, 255, 0.22)";
    public override string BorderFocus   => "#c084fc";
    public override string Divider       => "rgba(255, 255, 255, 0.08)";

    public override string ColorPrimary        => "#c084fc";
    public override string ColorPrimarySubtle  => "rgba(192, 132, 252, 0.18)";
    public override string ColorPrimaryMuted   => "rgba(192, 132, 252, 0.32)";
    public override string ColorPrimaryHover   => "#d8b4fe";
    public override string ColorPrimaryActive  => "#e9d5ff";
    public override string ColorPrimaryFg      => "#2e1065";

    public override string ColorSuccess        => "#34d399";
    public override string ColorSuccessSubtle  => "rgba(52, 211, 153, 0.18)";
    public override string ColorSuccessHover   => "#6ee7b7";
    public override string ColorSuccessFg      => "#064e3b";

    public override string ColorDanger         => "#f472b6";
    public override string ColorDangerSubtle   => "rgba(244, 114, 182, 0.18)";
    public override string ColorDangerHover    => "#f9a8d4";
    public override string ColorDangerFg       => "#500724";

    public override string ColorWarning        => "#fbbf24";
    public override string ColorWarningSubtle  => "rgba(251, 191, 36, 0.18)";
    public override string ColorWarningHover   => "#fcd34d";
    public override string ColorWarningFg      => "#451a03";

    public override string ColorInfo           => "#60a5fa";
    public override string ColorInfoSubtle     => "rgba(96, 165, 250, 0.18)";
    public override string ColorInfoHover      => "#93c5fd";
    public override string ColorInfoFg         => "#1e3a8a";

    public override string ShadowXs => "0 2px 8px -2px rgba(0, 0, 0, 0.4)";
    public override string ShadowSm => "0 6px 18px -6px rgba(0, 0, 0, 0.5)";
    public override string ShadowMd => "0 12px 32px -8px rgba(0, 0, 0, 0.6)";
    public override string ShadowLg => "0 20px 48px -10px rgba(0, 0, 0, 0.7)";
    public override string ShadowXl => "0 30px 80px -16px rgba(0, 0, 0, 0.85)";

    public override string RadiusSm   => "8px";
    public override string RadiusMd   => "12px";
    public override string RadiusLg   => "18px";
    public override string RadiusXl   => "24px";

    public override string FocusRing       => "0 0 0 3px rgba(192, 132, 252, 0.35)";
    public override string FocusRingDanger => "0 0 0 3px rgba(244, 114, 182, 0.35)";
}

internal class GlassComponents : DefaultComponents
{
    public override string BtnRadius       => "12px";
    public override string BtnFontSize     => "0.875rem";
    public override string BtnFontWeight   => "600";
    public override string BtnHeight       => "38px";
    public override string BtnHeightSm     => "30px";
    public override string BtnHeightLg     => "46px";

    public override string InputRadius     => "12px";
    public override string InputFontSize   => "0.875rem";
    public override string InputHeight     => "38px";
    public override string InputHeightSm   => "30px";
    public override string InputHeightLg   => "46px";

    public override string CardRadius      => "20px";
    public override string CardPadding     => "20px";
    public override string CardBorderColor => "var(--sg-border-glass)";
    public override string CardBg          => "var(--sg-bg-glass)";

    public override string ModalRadius     => "24px";

    public override string TableRadius          => "16px";
    public override string TableHeaderFontWeight => "600";

    public override string TabsIndicatorHeight => "2px";
    public override string TooltipMaxWidth     => "260px";

    public override string HeaderBg      => "rgba(255, 255, 255, 0.30)";
    public override string HeaderFg      => "var(--sg-fg)";
    public override string NavBg         => "rgba(255, 255, 255, 0.22)";
    public override string NavFg         => "var(--sg-fg-subtle)";
    public override string NavActiveBg   => "rgba(255, 255, 255, 0.45)";
    public override string NavActiveFg   => "var(--sg-fg)";
}
