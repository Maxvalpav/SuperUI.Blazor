namespace SuperUI.Themes;

/// <summary>
/// Clarity Clinical — медицинская тема для клинических интерфейсов.
/// Teal primary (hue 175) — trust, calm, healing. WCAG AAA контраст.
/// Крупная типографика, увеличенные touch targets, minimal animations.
/// </summary>
public sealed class ClarityClinicalTheme : ThemeBase
{
    public override string Id => "clarity-clinical";
    public override string Name => "Clarity Clinical";
    public override string? Description => "Медицинская тема для клинических интерфейсов. Teal primary, WCAG AAA контраст, крупная типографика, minimal animations.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Precision";

    protected override IThemePrimitives CreatePrimitives() => new ClarityClinicalPrimitives();
    protected override IThemeSemantic CreateLight() => new ClarityClinicalSemanticLight();
    protected override IThemeSemantic? CreateDark() => new ClarityClinicalSemanticDark();
    protected override IThemeComponents? CreateComponents() => new ClarityClinicalComponents();
    protected override IThemeTypography? CreateTypography() => new ClarityClinicalTypography();

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
           CLARITY CLINICAL — Medical theme for clinical interfaces
           Brand: Teal (hue 175) — trust, calm, healing
           WCAG AAA contrast (≥7:1), large typography, minimal animations
           Selector: [data-theme-id="clarity-clinical"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── Clinical constants ── */
        :root,
        [data-theme-id="clarity-clinical"] {
            /* Clinical spacing — generous, clinical precision */
            --clinical-space-1: 4px;
            --clinical-space-2: 8px;
            --clinical-space-3: 12px;
            --clinical-space-4: 16px;
            --clinical-space-5: 24px;
            --clinical-space-6: 32px;
            --clinical-space-7: 48px;
            --clinical-space-8: 64px;

            /* Clinical easing — minimal, purposeful */
            --clinical-ease: cubic-bezier(0.25, 0.1, 0.25, 1);
            --clinical-duration: 150ms;
        }

        /* ── Base layer — clinical precision ── */
        [data-theme-id="clarity-clinical"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Labels, titles — clean tracking ── */
        [data-theme-id="clarity-clinical"] .sgc-label,
        [data-theme-id="clarity-clinical"] .sgc-title,
        [data-theme-id="clarity-clinical"] .sgc-card-title,
        [data-theme-id="clarity-clinical"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="clarity-clinical"] .sgc-nav-section,
        [data-theme-id="clarity-clinical"] .sgc-thead,
        [data-theme-id="clarity-clinical"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 11px;
        }

        /* ── Cards — clean, clinical ── */
        [data-theme-id="clarity-clinical"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-sm);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color var(--clinical-duration) var(--clinical-ease),
                        box-shadow   var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="clarity-clinical"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="clarity-clinical"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="clarity-clinical"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="clarity-clinical"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--clinical-space-3) var(--clinical-space-4);
        }
        [data-theme-id="clarity-clinical"] .sgc-card-body {
            padding: var(--clinical-space-4);
        }
        [data-theme-id="clarity-clinical"] .sgc-card-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--clinical-space-2) var(--clinical-space-4);
        }

        /* ── Buttons — clear, high contrast ── */
        [data-theme-id="clarity-clinical"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 6px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color var(--clinical-duration) var(--clinical-ease),
                        border-color     var(--clinical-duration) var(--clinical-ease),
                        color            var(--clinical-duration) var(--clinical-ease),
                        box-shadow       var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn:hover:not(:disabled) {
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.98);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

        [data-theme-id="clarity-clinical"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="clarity-clinical"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
        }

        [data-theme-id="clarity-clinical"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary-muted);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — clean, clinical ── */
        [data-theme-id="clarity-clinical"] .sgc-input,
        [data-theme-id="clarity-clinical"] .sgc-select,
        [data-theme-id="clarity-clinical"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color var(--clinical-duration) var(--clinical-ease),
                        box-shadow   var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-input::placeholder,
        [data-theme-id="clarity-clinical"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.7;
        }
        [data-theme-id="clarity-clinical"] .sgc-input:hover:not(:focus),
        [data-theme-id="clarity-clinical"] .sgc-select:hover:not(:focus),
        [data-theme-id="clarity-clinical"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="clarity-clinical"] .sgc-input:focus,
        [data-theme-id="clarity-clinical"] .sgc-select:focus,
        [data-theme-id="clarity-clinical"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="clarity-clinical"] .sgc-input:disabled,
        [data-theme-id="clarity-clinical"] .sgc-select:disabled,
        [data-theme-id="clarity-clinical"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — clean underline ── */
        [data-theme-id="clarity-clinical"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 2px solid var(--sg-border-subtle);
            padding: 0;
            gap: 0;
        }
        [data-theme-id="clarity-clinical"] .sgc-tab {
            border-radius: 0;
            padding: var(--clinical-space-2) var(--clinical-space-4);
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            border-bottom: 2px solid transparent;
            margin-bottom: -2px;
            transition: color var(--clinical-duration) var(--clinical-ease),
                        border-color var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-tab:hover {
            color: var(--sg-fg);
            border-bottom-color: var(--sg-border-strong);
        }
        [data-theme-id="clarity-clinical"] .sgc-tab.active,
        [data-theme-id="clarity-clinical"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            border-bottom-color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* ── Alerts — clinical status bars ── */
        [data-theme-id="clarity-clinical"] .sgc-alert {
            border: 1px solid;
            border-left-width: 4px;
            border-radius: var(--sg-radius-sm);
            padding: var(--clinical-space-3) var(--clinical-space-4);
            font-size: 0.875rem;
        }
        [data-theme-id="clarity-clinical"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="clarity-clinical"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="clarity-clinical"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="clarity-clinical"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — clinical overlay ── */
        [data-theme-id="clarity-clinical"] .sgc-modal-content,
        [data-theme-id="clarity-clinical"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="clarity-clinical"] .sgc-modal-header,
        [data-theme-id="clarity-clinical"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--clinical-space-4);
        }
        [data-theme-id="clarity-clinical"] .sgc-modal-body,
        [data-theme-id="clarity-clinical"] .sgc-drawer-body {
            padding: var(--clinical-space-4);
        }
        [data-theme-id="clarity-clinical"] .sgc-modal-footer,
        [data-theme-id="clarity-clinical"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--clinical-space-3) var(--clinical-space-4);
        }

        /* ── Navigation — clean sidebar ── */
        [data-theme-id="clarity-clinical"] .sgc-nav-link {
            border-radius: var(--sg-radius-sm);
            margin: 1px 4px;
            padding: var(--clinical-space-2) var(--clinical-space-3);
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            transition: background var(--clinical-duration) var(--clinical-ease),
                        color      var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="clarity-clinical"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 3px 0 0 0 var(--sg-color-primary);
        }
        [data-theme-id="clarity-clinical"] .sgc-nav-section {
            padding: var(--clinical-space-5) var(--clinical-space-3) var(--clinical-space-2);
            color: var(--sg-fg-muted);
            font-weight: 600;
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — clinical data display ── */
        [data-theme-id="clarity-clinical"] .sg-table,
        [data-theme-id="clarity-clinical"] .sgc-table {
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-sm);
        }
        [data-theme-id="clarity-clinical"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 2px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: var(--clinical-space-2) var(--clinical-space-3);
        }
        [data-theme-id="clarity-clinical"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--clinical-space-2) var(--clinical-space-3);
        }
        [data-theme-id="clarity-clinical"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="clarity-clinical"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — clinical pills ── */
        [data-theme-id="clarity-clinical"] .sgc-badge,
        [data-theme-id="clarity-clinical"] .sgc-chip {
            border-radius: var(--sg-radius-full);
            padding: 2px var(--clinical-space-2);
            font-size: 0.75rem;
            font-weight: 500;
        }

        /* ── Tooltip — clean ── */
        [data-theme-id="clarity-clinical"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--sg-radius-sm);
            padding: var(--clinical-space-1) var(--clinical-space-2);
            font-size: 0.75rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — clinical precision ── */
        [data-theme-id="clarity-clinical"] ::-webkit-scrollbar {
            width: 8px;
            height: 8px;
        }
        [data-theme-id="clarity-clinical"] ::-webkit-scrollbar-track {
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="clarity-clinical"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 4px;
        }
        [data-theme-id="clarity-clinical"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection ── */
        [data-theme-id="clarity-clinical"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring — high visibility for clinical use ── */
        [data-theme-id="clarity-clinical"] :focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Breadcrumb ── */
        [data-theme-id="clarity-clinical"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 var(--clinical-space-1);
        }
        [data-theme-id="clarity-clinical"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color var(--clinical-duration) var(--clinical-ease);
        }
        [data-theme-id="clarity-clinical"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="clarity-clinical"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 600;
        }

        /* ── Progress bar — clinical fill ── */
        [data-theme-id="clarity-clinical"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border-radius: var(--sg-radius-full);
            overflow: hidden;
        }
        [data-theme-id="clarity-clinical"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: var(--sg-radius-full);
            transition: width 400ms var(--clinical-ease);
        }

        /* ── Skeleton — minimal shimmer ── */
        [data-theme-id="clarity-clinical"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: clinical-shimmer 2s var(--clinical-ease) infinite;
            border-radius: 2px;
        }
        @keyframes clinical-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider ── */
        [data-theme-id="clarity-clinical"] .sgc-divider {
            border: none;
            height: 1px;
            background: var(--sg-divider);
            margin: var(--clinical-space-4) 0;
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="clarity-clinical"] *,
            [data-theme-id="clarity-clinical"] *::before,
            [data-theme-id="clarity-clinical"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;

    internal sealed class ClarityClinicalTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&family=JetBrains+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
        public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.1", "-0.02em");
        public HeadingSettings H2 => new("2rem", HeadingFont, "600", "1.15", "-0.015em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.2", "-0.01em");
        public HeadingSettings H4 => new("1.125rem", HeadingFont, "600", "1.25", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "600", "1.3", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.35", "0.01em");
    }
}

internal class ClarityClinicalPrimitives : IThemePrimitives
{
    // Neutral — cool gray (hue 175°, aligned with brand teal)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 175)";
    public virtual string Neutral100 => "oklch(0.97 0.005 175)";
    public virtual string Neutral200 => "oklch(0.93 0.008 175)";
    public virtual string Neutral300 => "oklch(0.87 0.01 175)";
    public virtual string Neutral400 => "oklch(0.76 0.012 175)";
    public virtual string Neutral500 => "oklch(0.64 0.012 175)";
    public virtual string Neutral600 => "oklch(0.52 0.014 175)";
    public virtual string Neutral700 => "oklch(0.40 0.016 175)";
    public virtual string Neutral800 => "oklch(0.28 0.018 175)";
    public virtual string Neutral900 => "oklch(0.16 0.02 175)";

    // Primary — Teal (hue 175°) — trust, calm, healing
    public virtual string Primary50  => "oklch(0.95 0.03 175)";
    public virtual string Primary100 => "oklch(0.90 0.06 175)";
    public virtual string Primary200 => "oklch(0.82 0.10 175)";
    public virtual string Primary300 => "oklch(0.72 0.14 175)";
    public virtual string Primary400 => "oklch(0.63 0.17 175)";
    public virtual string Primary500 => "oklch(0.55 0.18 175)";
    public virtual string Primary600 => "oklch(0.48 0.17 175)";
    public virtual string Primary700 => "oklch(0.40 0.16 175)";
    public virtual string Primary800 => "oklch(0.30 0.14 175)";
    public virtual string Primary900 => "oklch(0.20 0.12 175)";

    // Success — Emerald
    public virtual string Success50  => "oklch(0.95 0.03 153)";
    public virtual string Success100 => "oklch(0.88 0.07 153)";
    public virtual string Success500 => "oklch(0.627 0.194 153.2)";
    public virtual string Success600 => "oklch(0.55 0.19 153)";
    public virtual string Success700 => "oklch(0.45 0.18 153)";

    // Danger — Red (strict, clinical)
    public virtual string Danger50  => "oklch(0.95 0.04 19)";
    public virtual string Danger100 => "oklch(0.88 0.09 19)";
    public virtual string Danger500 => "oklch(0.552 0.244 19.3)";
    public virtual string Danger600 => "oklch(0.48 0.24 19)";
    public virtual string Danger700 => "oklch(0.40 0.22 19)";

    // Warning — Amber
    public virtual string Warning50  => "oklch(0.97 0.03 75)";
    public virtual string Warning100 => "oklch(0.92 0.06 75)";
    public virtual string Warning500 => "oklch(0.68 0.14 75)";
    public virtual string Warning600 => "oklch(0.60 0.14 75)";

    // Info — Blue (clinical information)
    public virtual string Info50  => "oklch(0.95 0.03 254)";
    public virtual string Info100 => "oklch(0.88 0.06 254)";
    public virtual string Info500 => "oklch(0.55 0.15 254)";
    public virtual string Info600 => "oklch(0.47 0.15 254)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // Radii — clean, clinical (slightly larger than compact)
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "12px";
    public virtual string RadiusXl   => "16px";
    public virtual string Radius2Xl  => "24px";
    public virtual string RadiusFull => "9999px";
}

internal class ClarityClinicalSemanticLight : BaseLightConsistent
{
    public ClarityClinicalSemanticLight() : base(175) { }

    // Override to white surfaces for clinical cleanliness
    public override string BgDefault     => "oklch(0.99 0.003 175)";
    public override string BgSubtle      => "oklch(0.97 0.005 175)";
    public override string BgMuted       => "oklch(0.935 0.008 175)";
    public override string BgEmphasized  => "oklch(0.89 0.012 175)";
    public override string BgOverlay     => "oklch(0.14 0.015 175 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.003 175 / 0.7)";

    public override string Surface         => "oklch(1 0 0)";
    public override string SurfaceRaised   => "oklch(1 0 0)";
    public override string SurfaceOverlay  => "oklch(1 0 0)";

    // WCAG AAA foreground — high contrast for clinical readability
    public override string FgDefault   => "oklch(0.14 0.015 175)";
    public override string FgSubtle    => "oklch(0.36 0.012 175)";
    public override string FgMuted     => "oklch(0.52 0.010 175)";
    public override string FgDisabled  => "oklch(0.68 0.006 175)";

    // Clinical teal primary
    public override string ColorPrimary        => "oklch(0.55 0.18 175)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 175)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 175)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.18 175)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.17 175)";

    // Shadows — subtle, clinical
    public override string ShadowXs => "0 1px 1px 0 oklch(0.14 0.015 175 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.14 0.015 175 / 0.06), 0 1px 1px -1px oklch(0.14 0.015 175 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.14 0.015 175 / 0.08), 0 1px 2px -1px oklch(0.14 0.015 175 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.14 0.015 175 / 0.10), 0 2px 4px -2px oklch(0.14 0.015 175 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.14 0.015 175 / 0.14), 0 4px 8px -4px oklch(0.14 0.015 175 / 0.08)";

    public override string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px oklch(0.55 0.18 175)";
    public override string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px oklch(0.552 0.244 19.3)";
}

internal class ClarityClinicalSemanticDark : BaseDarkConsistent
{
    public ClarityClinicalSemanticDark() : base(175) { }

    public override string ColorPrimary        => "oklch(0.62 0.18 175)";
    public override string ColorPrimaryHover   => "oklch(0.67 0.18 175)";
    public override string ColorPrimaryActive  => "oklch(0.57 0.18 175)";

    public override string ColorDanger         => "oklch(0.552 0.244 19.3)";
    public override string ColorDangerHover    => "oklch(0.62 0.24 19)";

    public override string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public override string ColorWarningHover   => "oklch(0.82 0.16 83)";
}

internal class ClarityClinicalComponents : IThemeComponents
{
    // Larger sizing for clinical use — increased touch targets
    public virtual string BtnRadius     => "6px";
    public virtual string BtnFontSize   => "0.875rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "40px";
    public virtual string BtnHeightSm   => "36px";
    public virtual string BtnHeightLg   => "48px";

    public virtual string InputRadius   => "6px";
    public virtual string InputFontSize => "0.875rem";
    public virtual string InputHeight   => "40px";
    public virtual string InputHeightSm => "36px";
    public virtual string InputHeightLg => "48px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "16px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "12px";

    public virtual string TableRadius          => "8px";
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
