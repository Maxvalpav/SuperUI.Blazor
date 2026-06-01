namespace SuperUI.Themes;

/// <summary>
/// Ergo — эргономичная тема для длительного использования.
/// Warm neutral primary (hue 30) с sage green accents (hue 150).
/// Оптимальные контрасты, увеличенные элементы, comfortable spacing.
/// </summary>
public sealed class ErgoTheme : ThemeBase
{
    public override string Id => "ergo";
    public override string Name => "Ergo";
    public override string? Description => "Эргономичная тема для длительного использования. Оптимальные контрасты, увеличенные touch targets, comfortable spacing.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Precision";

    protected override IThemePrimitives CreatePrimitives() => new ErgoPrimitives();
    protected override IThemeSemantic CreateLight() => new ErgoSemanticLight();
    protected override IThemeSemantic? CreateDark() => new ErgoSemanticDark();
    protected override IThemeComponents? CreateComponents() => new ErgoComponents();
    protected override IThemeTypography? CreateTypography() => new ErgoTypography();

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
           ERGO — Ergonomic design for prolonged use
           Brand: Warm neutral (hue 30) + Sage green accents (hue 150)
           Increased touch targets (44px+), generous spacing, soft shadows
           Optimized for eye comfort during long sessions
           Selector: [data-theme-id="ergo"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── Ergo constants ── */
        :root,
        [data-theme-id="ergo"] {
            /* Ergonomic spacing — generous, comfortable */
            --ergo-space-1: 6px;
            --ergo-space-2: 10px;
            --ergo-space-3: 16px;
            --ergo-space-4: 24px;
            --ergo-space-5: 32px;
            --ergo-space-6: 48px;
            --ergo-space-7: 64px;
            --ergo-space-8: 96px;

            /* Ergonomic easing — gentle, comfortable */
            --ergo-ease: cubic-bezier(0.25, 0.1, 0.25, 1);
            --ergo-duration: 200ms;

            /* Eye comfort — reduced blue light */
            --ergo-color-temperature: hue-rotate(0deg);
        }

        /* ── Base layer — eye comfort ── */
        [data-theme-id="ergo"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
            line-height: 1.6; /* Increased for readability */
        }

        /* ── Labels, titles — comfortable tracking ── */
        [data-theme-id="ergo"] .sgc-label,
        [data-theme-id="ergo"] .sgc-title,
        [data-theme-id="ergo"] .sgc-card-title,
        [data-theme-id="ergo"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="ergo"] .sgc-nav-section,
        [data-theme-id="ergo"] .sgc-thead,
        [data-theme-id="ergo"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 12px;
        }

        /* ── Cards — soft, comfortable ── */
        [data-theme-id="ergo"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color var(--ergo-duration) var(--ergo-ease),
                        box-shadow   var(--ergo-duration) var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="ergo"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="ergo"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="ergo"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="ergo"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--ergo-space-3) var(--ergo-space-4);
        }
        [data-theme-id="ergo"] .sgc-card-body {
            padding: var(--ergo-space-4);
        }
        [data-theme-id="ergo"] .sgc-card-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--ergo-space-3) var(--ergo-space-4);
        }

        /* ── Buttons — large touch targets, gentle press ── */
        [data-theme-id="ergo"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 8px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color var(--ergo-duration) var(--ergo-ease),
                        border-color     var(--ergo-duration) var(--ergo-ease),
                        color            var(--ergo-duration) var(--ergo-ease),
                        box-shadow       var(--ergo-duration) var(--ergo-ease),
                        transform        120ms var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-btn:hover:not(:disabled) {
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
        }
        [data-theme-id="ergo"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.98);
        }
        [data-theme-id="ergo"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="ergo"] .sgc-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

        [data-theme-id="ergo"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="ergo"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.12),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="ergo"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
        }

        [data-theme-id="ergo"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary-muted);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — comfortable, easy to click ── */
        [data-theme-id="ergo"] .sgc-input,
        [data-theme-id="ergo"] .sgc-select,
        [data-theme-id="ergo"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color var(--ergo-duration) var(--ergo-ease),
                        box-shadow   var(--ergo-duration) var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-input::placeholder,
        [data-theme-id="ergo"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.7;
        }
        [data-theme-id="ergo"] .sgc-input:hover:not(:focus),
        [data-theme-id="ergo"] .sgc-select:hover:not(:focus),
        [data-theme-id="ergo"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="ergo"] .sgc-input:focus,
        [data-theme-id="ergo"] .sgc-select:focus,
        [data-theme-id="ergo"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="ergo"] .sgc-input:disabled,
        [data-theme-id="ergo"] .sgc-select:disabled,
        [data-theme-id="ergo"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — comfortable click targets ── */
        [data-theme-id="ergo"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 2px solid var(--sg-border-subtle);
            padding: 0;
            gap: 0;
        }
        [data-theme-id="ergo"] .sgc-tab {
            border-radius: 0;
            padding: 12px 20px;
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            border-bottom: 2px solid transparent;
            margin-bottom: -2px;
            transition: color var(--ergo-duration) var(--ergo-ease),
                        border-color var(--ergo-duration) var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-tab:hover {
            color: var(--sg-fg);
            border-bottom-color: var(--sg-border-strong);
        }
        [data-theme-id="ergo"] .sgc-tab.active,
        [data-theme-id="ergo"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            border-bottom-color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* ── Alerts — comfortable padding ── */
        [data-theme-id="ergo"] .sgc-alert {
            border: 1px solid;
            border-left-width: 4px;
            border-radius: var(--sg-radius-sm);
            padding: var(--ergo-space-3) var(--ergo-space-4);
            font-size: 0.9375rem;
        }
        [data-theme-id="ergo"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="ergo"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="ergo"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="ergo"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — soft overlay ── */
        [data-theme-id="ergo"] .sgc-modal-content,
        [data-theme-id="ergo"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-lg);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="ergo"] .sgc-modal-header,
        [data-theme-id="ergo"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--ergo-space-4);
        }
        [data-theme-id="ergo"] .sgc-modal-body,
        [data-theme-id="ergo"] .sgc-drawer-body {
            padding: var(--ergo-space-4);
        }
        [data-theme-id="ergo"] .sgc-modal-footer,
        [data-theme-id="ergo"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--ergo-space-3) var(--ergo-space-4);
        }

        /* ── Navigation — comfortable sidebar ── */
        [data-theme-id="ergo"] .sgc-nav-link {
            border-radius: var(--sg-radius-sm);
            margin: 2px 6px;
            padding: var(--ergo-space-1) var(--ergo-space-3);
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            transition: background var(--ergo-duration) var(--ergo-ease),
                        color      var(--ergo-duration) var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="ergo"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 3px 0 0 0 var(--sg-color-primary);
        }
        [data-theme-id="ergo"] .sgc-nav-section {
            padding: var(--ergo-space-5) var(--ergo-space-3) var(--ergo-space-2);
            color: var(--sg-fg-muted);
            font-weight: 600;
            font-size: 0.8125rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — comfortable reading ── */
        [data-theme-id="ergo"] .sg-table,
        [data-theme-id="ergo"] .sgc-table {
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
        }
        [data-theme-id="ergo"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 2px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: var(--ergo-space-2) var(--ergo-space-3);
        }
        [data-theme-id="ergo"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--ergo-space-2) var(--ergo-space-3);
        }
        [data-theme-id="ergo"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="ergo"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — comfortable pills ── */
        [data-theme-id="ergo"] .sgc-badge,
        [data-theme-id="ergo"] .sgc-chip {
            border-radius: var(--sg-radius-full);
            padding: 3px 10px;
            font-size: 0.8125rem;
            font-weight: 500;
        }

        /* ── Tooltip — comfortable ── */
        [data-theme-id="ergo"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--sg-radius-sm);
            padding: 6px 12px;
            font-size: 0.8125rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — easy to grab ── */
        [data-theme-id="ergo"] ::-webkit-scrollbar {
            width: 10px;
            height: 10px;
        }
        [data-theme-id="ergo"] ::-webkit-scrollbar-track {
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="ergo"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 5px;
        }
        [data-theme-id="ergo"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection ── */
        [data-theme-id="ergo"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring — high visibility ── */
        [data-theme-id="ergo"] :focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Breadcrumb ── */
        [data-theme-id="ergo"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 6px;
        }
        [data-theme-id="ergo"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color var(--ergo-duration) var(--ergo-ease);
        }
        [data-theme-id="ergo"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="ergo"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 600;
        }

        /* ── Progress bar — comfortable fill ── */
        [data-theme-id="ergo"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border-radius: var(--sg-radius-full);
            overflow: hidden;
        }
        [data-theme-id="ergo"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: var(--sg-radius-full);
            transition: width 500ms var(--ergo-ease);
        }

        /* ── Skeleton — gentle shimmer ── */
        [data-theme-id="ergo"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: ergo-shimmer 2s var(--ergo-ease) infinite;
            border-radius: 4px;
        }
        @keyframes ergo-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider ── */
        [data-theme-id="ergo"] .sgc-divider {
            border: none;
            height: 1px;
            background: var(--sg-divider);
            margin: var(--ergo-space-4) 0;
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="ergo"] *,
            [data-theme-id="ergo"] *::before,
            [data-theme-id="ergo"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;

    internal sealed class ErgoTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=IBM+Plex+Sans:wght@400;500;600;700&family=IBM+Plex+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'IBM Plex Sans', system-ui, -apple-system, 'Segoe UI', sans-serif";
        public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.15", "-0.02em");
        public HeadingSettings H2 => new("2rem", HeadingFont, "600", "1.2", "-0.015em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.25", "-0.01em");
        public HeadingSettings H4 => new("1.125rem", HeadingFont, "600", "1.3", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "600", "1.35", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.4", "0.01em");
    }
}

internal class ErgoPrimitives : IThemePrimitives
{
    // Neutral — warm gray (hue 30°, aligned with brand warm neutral)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 30)";
    public virtual string Neutral100 => "oklch(0.97 0.006 30)";
    public virtual string Neutral200 => "oklch(0.93 0.009 30)";
    public virtual string Neutral300 => "oklch(0.87 0.012 30)";
    public virtual string Neutral400 => "oklch(0.76 0.014 30)";
    public virtual string Neutral500 => "oklch(0.64 0.014 30)";
    public virtual string Neutral600 => "oklch(0.52 0.016 30)";
    public virtual string Neutral700 => "oklch(0.40 0.018 30)";
    public virtual string Neutral800 => "oklch(0.28 0.02 30)";
    public virtual string Neutral900 => "oklch(0.16 0.022 30)";

    // Primary — Warm neutral (hue 30°) — comfortable, non-fatiguing
    public virtual string Primary50  => "oklch(0.95 0.03 30)";
    public virtual string Primary100 => "oklch(0.90 0.06 30)";
    public virtual string Primary200 => "oklch(0.82 0.10 30)";
    public virtual string Primary300 => "oklch(0.72 0.12 30)";
    public virtual string Primary400 => "oklch(0.63 0.12 30)";
    public virtual string Primary500 => "oklch(0.56 0.12 30)";
    public virtual string Primary600 => "oklch(0.49 0.11 30)";
    public virtual string Primary700 => "oklch(0.41 0.10 30)";
    public virtual string Primary800 => "oklch(0.32 0.09 30)";
    public virtual string Primary900 => "oklch(0.22 0.08 30)";

    // Success — Sage green (hue 150°) — natural, calming
    public virtual string Success50  => "oklch(0.95 0.03 150)";
    public virtual string Success100 => "oklch(0.88 0.06 150)";
    public virtual string Success500 => "oklch(0.58 0.14 150)";
    public virtual string Success600 => "oklch(0.50 0.14 150)";
    public virtual string Success700 => "oklch(0.42 0.13 150)";

    // Danger — Soft red (not harsh)
    public virtual string Danger50  => "oklch(0.95 0.04 22)";
    public virtual string Danger100 => "oklch(0.88 0.09 22)";
    public virtual string Danger500 => "oklch(0.58 0.20 22)";
    public virtual string Danger600 => "oklch(0.50 0.20 22)";
    public virtual string Danger700 => "oklch(0.42 0.19 22)";

    // Warning — Warm amber
    public virtual string Warning50  => "oklch(0.97 0.04 65)";
    public virtual string Warning100 => "oklch(0.92 0.08 65)";
    public virtual string Warning500 => "oklch(0.68 0.16 65)";
    public virtual string Warning600 => "oklch(0.60 0.16 65)";

    // Info — Cool blue
    public virtual string Info50  => "oklch(0.95 0.03 230)";
    public virtual string Info100 => "oklch(0.88 0.06 230)";
    public virtual string Info500 => "oklch(0.55 0.14 230)";
    public virtual string Info600 => "oklch(0.47 0.14 230)";

    public virtual string FontSans  => "'IBM Plex Sans', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'IBM Plex Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // Radii — comfortable, rounded
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "4px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "10px";
    public virtual string RadiusLg   => "14px";
    public virtual string RadiusXl   => "20px";
    public virtual string Radius2Xl  => "28px";
    public virtual string RadiusFull => "9999px";
}

internal class ErgoSemanticLight : BaseLightConsistent
{
    public ErgoSemanticLight() : base(30) { }

    // Warm, comfortable surfaces
    public override string BgDefault     => "oklch(0.99 0.006 30)";
    public override string BgSubtle      => "oklch(0.97 0.009 30)";
    public override string BgMuted       => "oklch(0.935 0.013 30)";
    public override string BgEmphasized  => "oklch(0.89 0.017 30)";
    public override string BgOverlay     => "oklch(0.14 0.02 30 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.006 30 / 0.7)";

    public override string Surface         => "oklch(1 0 0)";
    public override string SurfaceRaised   => "oklch(1 0 0)";
    public override string SurfaceOverlay  => "oklch(1 0 0)";

    // Warm foreground — comfortable for long reading
    public override string FgDefault   => "oklch(0.14 0.02 30)";
    public override string FgSubtle    => "oklch(0.36 0.015 30)";
    public override string FgMuted     => "oklch(0.52 0.012 30)";
    public override string FgDisabled  => "oklch(0.68 0.008 30)";

    // Warm primary
    public override string ColorPrimary        => "oklch(0.56 0.12 30)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 30)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 30)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.12 30)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.11 30)";

    // Warm shadows
    public override string ShadowXs => "0 1px 1px 0 oklch(0.14 0.02 30 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.14 0.02 30 / 0.06), 0 1px 1px -1px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.14 0.02 30 / 0.08), 0 1px 2px -1px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.14 0.02 30 / 0.10), 0 2px 4px -2px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.14 0.02 30 / 0.14), 0 4px 8px -4px oklch(0.14 0.02 30 / 0.08)";

    public override string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px oklch(0.56 0.12 30)";
    public override string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px oklch(0.58 0.20 22)";
}

internal class ErgoSemanticDark : BaseDarkConsistent
{
    public ErgoSemanticDark() : base(30) { }

    public override string ColorPrimary        => "oklch(0.62 0.12 30)";
    public override string ColorPrimaryHover   => "oklch(0.67 0.12 30)";
    public override string ColorPrimaryActive  => "oklch(0.57 0.12 30)";

    public override string ColorDanger         => "oklch(0.58 0.20 22)";
    public override string ColorDangerHover    => "oklch(0.65 0.20 22)";

    public override string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public override string ColorWarningHover   => "oklch(0.82 0.16 83)";
}

internal class ErgoComponents : IThemeComponents
{
    // Extra large touch targets for ergonomic use
    public virtual string BtnRadius     => "8px";
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

    public virtual string CardRadius      => "10px";
    public virtual string CardPadding     => "20px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "14px";

    public virtual string TableRadius          => "10px";
    public virtual string TableHeaderFontWeight => "650";

    public virtual string TabsIndicatorHeight => "3px";

    public virtual string TooltipMaxWidth => "300px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
