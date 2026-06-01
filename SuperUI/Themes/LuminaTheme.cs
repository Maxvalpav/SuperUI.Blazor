namespace SuperUI.Themes;

/// <summary>
/// Lumina — тема доступности и инклюзивности.
/// Blue primary (hue 230) — наиболее различимый для всех типов color blindness.
/// WCAG 2.1 AAA, enhanced focus rings, CVD-friendly colors, reduced motion support.
/// </summary>
public sealed class LuminaTheme : ThemeBase
{
    public override string Id => "lumina";
    public override string Name => "Lumina";
    public override string? Description => "Тема доступности и инклюзивности. WCAG 2.1 AAA, CVD-friendly, enhanced focus rings, reduced motion support.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new LuminaPrimitives();
    protected override IThemeSemantic CreateLight() => new LuminaSemanticLight();
    protected override IThemeSemantic? CreateDark() => new LuminaSemanticDark();
    protected override IThemeComponents? CreateComponents() => new LuminaComponents();
    protected override IThemeTypography? CreateTypography() => new LuminaTypography();

    public override string? AdditionalCss => $$"""
        /* ── Global fallback aliases (backward compat with --sui-*) ── */
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
            --sui-success-border: var(--sg-border-subtle);

            --sui-danger:        var(--sg-color-danger);
            --sui-danger-bg:     var(--sg-color-danger-subtle);
            --sui-danger-border: var(--sg-border-subtle);

            --sui-warn:        var(--sg-color-warning);
            --sui-warn-bg:     var(--sg-color-warning-subtle);
            --sui-warn-border: var(--sg-border-subtle);

            --sui-info:        var(--sg-color-info);
            --sui-info-bg:     var(--sg-color-info-subtle);
            --sui-info-border: var(--sg-border-subtle);

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

        /* ═══════════════════════════════════════════════════════════════
           LUMINA — Accessibility & Inclusivity theme
           Brand: Blue (hue 230) — most distinguishable for CVD
           WCAG 2.1 AAA contrast, enhanced focus rings
           CVD-friendly color palette, reduced motion support
           Selector: [data-theme-id="lumina"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── Lumina constants ── */
        :root,
        [data-theme-id="lumina"] {
            /* Accessibility spacing — generous, predictable */
            --lum-space-1: 4px;
            --lum-space-2: 8px;
            --lum-space-3: 12px;
            --lum-space-4: 16px;
            --lum-space-5: 24px;
            --lum-space-6: 32px;
            --lum-space-7: 48px;
            --lum-space-8: 64px;

            /* Accessibility easing — minimal, purposeful */
            --lum-ease: cubic-bezier(0.25, 0.1, 0.25, 1);
            --lum-duration: 150ms;

            /* Enhanced focus ring — high visibility */
            --lum-focus-ring: 0 0 0 3px var(--sg-bg), 0 0 0 5px var(--sg-color-primary);
            --lum-focus-ring-danger: 0 0 0 3px var(--sg-bg), 0 0 0 5px var(--sg-color-danger);
        }

        /* ── Base layer — accessibility first ── */
        [data-theme-id="lumina"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Skip link — keyboard accessibility ── */
        [data-theme-id="lumina"] .sgc-skip-link {
            position: absolute;
            top: -40px;
            left: 0;
            background: var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
            padding: 8px 16px;
            z-index: 9999;
            transition: top var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-skip-link:focus {
            top: 0;
        }

        /* ── Labels, titles — clear hierarchy ── */
        [data-theme-id="lumina"] .sgc-label,
        [data-theme-id="lumina"] .sgc-title,
        [data-theme-id="lumina"] .sgc-card-title,
        [data-theme-id="lumina"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="lumina"] .sgc-nav-section,
        [data-theme-id="lumina"] .sgc-thead,
        [data-theme-id="lumina"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 11px;
        }

        /* ── Cards — clear borders, high contrast ── */
        [data-theme-id="lumina"] .sgc-card {
            background: var(--sg-surface);
            border: 2px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color var(--lum-duration) var(--lum-ease),
                        box-shadow   var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-card:hover {
            border-color: var(--sg-color-primary);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="lumina"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="lumina"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="lumina"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-border);
            box-shadow: none;
        }
        [data-theme-id="lumina"] .sgc-card-header {
            border-bottom: 2px solid var(--sg-divider);
            padding: var(--lum-space-3) var(--lum-space-4);
        }
        [data-theme-id="lumina"] .sgc-card-body {
            padding: var(--lum-space-4);
        }
        [data-theme-id="lumina"] .sgc-card-footer {
            border-top: 2px solid var(--sg-divider);
            padding: var(--lum-space-3) var(--lum-space-4);
        }

        /* ── Buttons — high contrast, large targets ── */
        [data-theme-id="lumina"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 6px);
            font-weight: 600;
            letter-spacing: 0.01em;
            border: 2px solid transparent;
            transition: background-color var(--lum-duration) var(--lum-ease),
                        border-color     var(--lum-duration) var(--lum-ease),
                        color            var(--lum-duration) var(--lum-ease),
                        box-shadow       var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-btn:hover:not(:disabled) {
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }
        [data-theme-id="lumina"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.98);
        }
        [data-theme-id="lumina"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: var(--lum-focus-ring);
        }
        [data-theme-id="lumina"] .sgc-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

        [data-theme-id="lumina"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 2px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="lumina"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="lumina"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
        }

        [data-theme-id="lumina"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — high contrast borders ── */
        [data-theme-id="lumina"] .sgc-input,
        [data-theme-id="lumina"] .sgc-select,
        [data-theme-id="lumina"] .sgc-textarea {
            border: 2px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color var(--lum-duration) var(--lum-ease),
                        box-shadow   var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-input::placeholder,
        [data-theme-id="lumina"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.8;
        }
        [data-theme-id="lumina"] .sgc-input:hover:not(:focus),
        [data-theme-id="lumina"] .sgc-select:hover:not(:focus),
        [data-theme-id="lumina"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="lumina"] .sgc-input:focus,
        [data-theme-id="lumina"] .sgc-select:focus,
        [data-theme-id="lumina"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: var(--lum-focus-ring);
            outline: none;
        }
        [data-theme-id="lumina"] .sgc-input:disabled,
        [data-theme-id="lumina"] .sgc-select:disabled,
        [data-theme-id="lumina"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — clear active state ── */
        [data-theme-id="lumina"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 2px solid var(--sg-border);
            padding: 0;
            gap: 0;
        }
        [data-theme-id="lumina"] .sgc-tab {
            border-radius: 0;
            padding: var(--lum-space-2) var(--lum-space-4);
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            border-bottom: 3px solid transparent;
            margin-bottom: -2px;
            transition: color var(--lum-duration) var(--lum-ease),
                        border-color var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-tab:hover {
            color: var(--sg-fg);
            border-bottom-color: var(--sg-border-strong);
        }
        [data-theme-id="lumina"] .sgc-tab.active,
        [data-theme-id="lumina"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            border-bottom-color: var(--sg-color-primary);
            font-weight: 700;
        }

        /* ── Alerts — high contrast borders ── */
        [data-theme-id="lumina"] .sgc-alert {
            border: 2px solid;
            border-left-width: 6px;
            border-radius: var(--sg-radius-sm);
            padding: var(--lum-space-3) var(--lum-space-4);
            font-size: 0.9375rem;
        }
        [data-theme-id="lumina"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="lumina"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="lumina"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="lumina"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — accessible overlay ── */
        [data-theme-id="lumina"] .sgc-modal-content,
        [data-theme-id="lumina"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 2px solid var(--sg-border);
            border-radius: var(--sg-radius-md);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="lumina"] .sgc-modal-header,
        [data-theme-id="lumina"] .sgc-drawer-header {
            border-bottom: 2px solid var(--sg-divider);
            padding: var(--lum-space-4);
        }
        [data-theme-id="lumina"] .sgc-modal-body,
        [data-theme-id="lumina"] .sgc-drawer-body {
            padding: var(--lum-space-4);
        }
        [data-theme-id="lumina"] .sgc-modal-footer,
        [data-theme-id="lumina"] .sgc-drawer-footer {
            border-top: 2px solid var(--sg-divider);
            padding: var(--lum-space-3) var(--lum-space-4);
        }

        /* ── Navigation — clear active indicator ── */
        [data-theme-id="lumina"] .sgc-nav-link {
            border-radius: var(--sg-radius-sm);
            margin: 1px 4px;
            padding: var(--lum-space-2) var(--lum-space-3);
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            border-left: 3px solid transparent;
            transition: background var(--lum-duration) var(--lum-ease),
                        color      var(--lum-duration) var(--lum-ease),
                        border-color var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="lumina"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 700;
            border-left-color: var(--sg-color-primary);
        }
        [data-theme-id="lumina"] .sgc-nav-section {
            padding: var(--lum-space-5) var(--lum-space-3) var(--lum-space-2);
            color: var(--sg-fg-muted);
            font-weight: 700;
            font-size: 0.8125rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — high contrast borders ── */
        [data-theme-id="lumina"] .sg-table,
        [data-theme-id="lumina"] .sgc-table {
            border: 2px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
        }
        [data-theme-id="lumina"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 2px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 700;
            padding: var(--lum-space-2) var(--lum-space-3);
        }
        [data-theme-id="lumina"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--lum-space-2) var(--lum-space-3);
        }
        [data-theme-id="lumina"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="lumina"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — high contrast pills ── */
        [data-theme-id="lumina"] .sgc-badge,
        [data-theme-id="lumina"] .sgc-chip {
            border-radius: var(--sg-radius-full);
            padding: 2px 10px;
            font-size: 0.8125rem;
            font-weight: 600;
            border: 1px solid;
        }

        /* ── Tooltip — accessible ── */
        [data-theme-id="lumina"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--sg-radius-sm);
            padding: 6px 12px;
            font-size: 0.875rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — easy to see ── */
        [data-theme-id="lumina"] ::-webkit-scrollbar {
            width: 12px;
            height: 12px;
        }
        [data-theme-id="lumina"] ::-webkit-scrollbar-track {
            background: var(--sg-bg-subtle);
            border: 1px solid var(--sg-border);
        }
        [data-theme-id="lumina"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 6px;
            border: 2px solid var(--sg-bg-subtle);
        }
        [data-theme-id="lumina"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection ── */
        [data-theme-id="lumina"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring — enhanced visibility ── */
        [data-theme-id="lumina"] :focus-visible {
            outline: none;
            box-shadow: var(--lum-focus-ring);
        }

        /* ── Breadcrumb ── */
        [data-theme-id="lumina"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 var(--lum-space-1);
        }
        [data-theme-id="lumina"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color var(--lum-duration) var(--lum-ease);
        }
        [data-theme-id="lumina"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="lumina"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 700;
        }

        /* ── Progress bar — high contrast ── */
        [data-theme-id="lumina"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border: 1px solid var(--sg-border);
            border-radius: var(--sg-radius-full);
            overflow: hidden;
        }
        [data-theme-id="lumina"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: var(--sg-radius-full);
            transition: width 400ms var(--lum-ease);
        }

        /* ── Skeleton — minimal shimmer ── */
        [data-theme-id="lumina"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: lum-shimmer 2s var(--lum-ease) infinite;
            border-radius: 2px;
        }
        @keyframes lum-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider ── */
        [data-theme-id="lumina"] .sgc-divider {
            border: none;
            height: 2px;
            background: var(--sg-divider);
            margin: var(--lum-space-4) 0;
        }

        /* ── High contrast mode support ── */
        @media (prefers-contrast: more) {
            [data-theme-id="lumina"] .sgc-card {
                border-width: 3px;
            }
            [data-theme-id="lumina"] .sgc-btn {
                border-width: 3px;
            }
            [data-theme-id="lumina"] .sgc-input,
            [data-theme-id="lumina"] .sgc-select,
            [data-theme-id="lumina"] .sgc-textarea {
                border-width: 3px;
            }
            [data-theme-id="lumina"] .sgc-alert {
                border-width: 3px;
                border-left-width: 8px;
            }
            [data-theme-id="lumina"] :focus-visible {
                box-shadow: 0 0 0 4px var(--sg-bg), 0 0 0 6px var(--sg-color-primary);
            }
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="lumina"] *,
            [data-theme-id="lumina"] *::before,
            [data-theme-id="lumina"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;

    internal sealed class LuminaTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Atkinson+Hyperlegible:wght@400;700&family=Inter:wght@400;500;600;700&family=JetBrains+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Atkinson Hyperlegible', 'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
        public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.2", "-0.02em");
        public HeadingSettings H2 => new("2rem", HeadingFont, "700", "1.25", "-0.015em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "700", "1.3", "-0.01em");
        public HeadingSettings H4 => new("1.125rem", HeadingFont, "700", "1.35", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "700", "1.4", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "700", "1.45", "0.01em");
    }
}

internal class LuminaPrimitives : IThemePrimitives
{
    // Neutral — cool gray (hue 230°, aligned with brand blue)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 230)";
    public virtual string Neutral100 => "oklch(0.97 0.005 230)";
    public virtual string Neutral200 => "oklch(0.93 0.008 230)";
    public virtual string Neutral300 => "oklch(0.87 0.01 230)";
    public virtual string Neutral400 => "oklch(0.76 0.012 230)";
    public virtual string Neutral500 => "oklch(0.64 0.012 230)";
    public virtual string Neutral600 => "oklch(0.52 0.014 230)";
    public virtual string Neutral700 => "oklch(0.40 0.016 230)";
    public virtual string Neutral800 => "oklch(0.28 0.018 230)";
    public virtual string Neutral900 => "oklch(0.16 0.02 230)";

    // Primary — Blue (hue 230°) — most distinguishable for CVD
    public virtual string Primary50  => "oklch(0.95 0.03 230)";
    public virtual string Primary100 => "oklch(0.90 0.06 230)";
    public virtual string Primary200 => "oklch(0.82 0.10 230)";
    public virtual string Primary300 => "oklch(0.72 0.14 230)";
    public virtual string Primary400 => "oklch(0.63 0.17 230)";
    public virtual string Primary500 => "oklch(0.55 0.18 230)";
    public virtual string Primary600 => "oklch(0.48 0.17 230)";
    public virtual string Primary700 => "oklch(0.40 0.16 230)";
    public virtual string Primary800 => "oklch(0.30 0.14 230)";
    public virtual string Primary900 => "oklch(0.20 0.12 230)";

    // Success — Teal (hue 180°) — distinguishable from primary blue
    public virtual string Success50  => "oklch(0.95 0.03 180)";
    public virtual string Success100 => "oklch(0.88 0.06 180)";
    public virtual string Success500 => "oklch(0.60 0.15 180)";
    public virtual string Success600 => "oklch(0.52 0.15 180)";
    public virtual string Success700 => "oklch(0.44 0.14 180)";

    // Danger — Red (hue 25°) — clearly distinct from blue/green
    public virtual string Danger50  => "oklch(0.95 0.04 25)";
    public virtual string Danger100 => "oklch(0.88 0.09 25)";
    public virtual string Danger500 => "oklch(0.55 0.22 25)";
    public virtual string Danger600 => "oklch(0.48 0.22 25)";
    public virtual string Danger700 => "oklch(0.40 0.20 25)";

    // Warning — Orange (hue 55°) — distinguishable from all other states
    public virtual string Warning50  => "oklch(0.97 0.04 55)";
    public virtual string Warning100 => "oklch(0.92 0.08 55)";
    public virtual string Warning500 => "oklch(0.70 0.16 55)";
    public virtual string Warning600 => "oklch(0.62 0.16 55)";

    // Info — Light blue (hue 240°) — similar to primary but lighter
    public virtual string Info50  => "oklch(0.95 0.03 240)";
    public virtual string Info100 => "oklch(0.88 0.06 240)";
    public virtual string Info500 => "oklch(0.55 0.14 240)";
    public virtual string Info600 => "oklch(0.47 0.14 240)";

    public virtual string FontSans  => "'Atkinson Hyperlegible', 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // Radii — clean, accessible
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string Radius2Xl  => "24px";
    public virtual string RadiusFull => "9999px";
}

internal class LuminaSemanticLight : BaseLightConsistent
{
    public LuminaSemanticLight() : base(230) { }

    // Clean, high-contrast surfaces
    public override string BgDefault     => "oklch(0.99 0.003 230)";
    public override string BgSubtle      => "oklch(0.97 0.005 230)";
    public override string BgMuted       => "oklch(0.935 0.008 230)";
    public override string BgEmphasized  => "oklch(0.89 0.012 230)";
    public override string BgOverlay     => "oklch(0.14 0.015 230 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.003 230 / 0.7)";

    public override string Surface         => "oklch(1 0 0)";
    public override string SurfaceRaised   => "oklch(1 0 0)";
    public override string SurfaceOverlay  => "oklch(1 0 0)";

    // WCAG AAA foreground — maximum contrast
    public override string FgDefault   => "oklch(0.14 0.015 230)";
    public override string FgSubtle    => "oklch(0.36 0.012 230)";
    public override string FgMuted     => "oklch(0.52 0.010 230)";
    public override string FgDisabled  => "oklch(0.68 0.006 230)";

    // Blue primary
    public override string ColorPrimary        => "oklch(0.55 0.18 230)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 230)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 230)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.18 230)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.17 230)";

    // High-contrast shadows
    public override string ShadowXs => "0 1px 1px 0 oklch(0.14 0.015 230 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.14 0.015 230 / 0.06), 0 1px 1px -1px oklch(0.14 0.015 230 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.14 0.015 230 / 0.08), 0 1px 2px -1px oklch(0.14 0.015 230 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.14 0.015 230 / 0.10), 0 2px 4px -2px oklch(0.14 0.015 230 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.14 0.015 230 / 0.14), 0 4px 8px -4px oklch(0.14 0.015 230 / 0.08)";

    public override string FocusRing       => "0 0 0 3px oklch(0.99 0.003 230), 0 0 0 5px oklch(0.55 0.18 230)";
    public override string FocusRingDanger => "0 0 0 3px oklch(0.99 0.003 230), 0 0 0 5px oklch(0.55 0.22 25)";
}

internal class LuminaSemanticDark : BaseDarkConsistent
{
    public LuminaSemanticDark() : base(230) { }

    public override string ColorPrimary        => "oklch(0.62 0.18 230)";
    public override string ColorPrimaryHover   => "oklch(0.67 0.18 230)";
    public override string ColorPrimaryActive  => "oklch(0.57 0.18 230)";

    public override string ColorDanger         => "oklch(0.55 0.22 25)";
    public override string ColorDangerHover    => "oklch(0.62 0.22 25)";

    public override string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public override string ColorWarningHover   => "oklch(0.82 0.16 83)";
}

internal class LuminaComponents : IThemeComponents
{
    // Large, accessible sizing
    public virtual string BtnRadius     => "6px";
    public virtual string BtnFontSize   => "0.9375rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "44px";
    public virtual string BtnHeightSm   => "38px";
    public virtual string BtnHeightLg   => "52px";

    public virtual string InputRadius   => "6px";
    public virtual string InputFontSize => "0.9375rem";
    public virtual string InputHeight   => "44px";
    public virtual string InputHeightSm => "38px";
    public virtual string InputHeightLg => "52px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "16px";
    public virtual string CardBorderColor => "var(--sg-border)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "12px";

    public virtual string TableRadius          => "8px";
    public virtual string TableHeaderFontWeight => "700";

    public virtual string TabsIndicatorHeight => "3px";

    public virtual string TooltipMaxWidth => "320px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
