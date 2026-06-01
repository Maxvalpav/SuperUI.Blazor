namespace SuperUI.Themes;

/// <summary>
/// Circadian — тема на основе хронобиологии.
/// Тёплые тона (amber, hue 75) в light, холодные (desaturated blue, hue 250) в dark.
/// Поддержка циркадных ритмов через цветовую температуру.
/// </summary>
public sealed class CircadianTheme : ThemeBase
{
    public override string Id => "circadian";
    public override string Name => "Circadian";
    public override string? Description => "Тема на основе хронобиологии. Тёплые тона (день) → холодные (ночь). Цветовая температура для циркадных ритмов.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Special";

    protected override IThemePrimitives CreatePrimitives() => new CircadianPrimitives();
    protected override IThemeSemantic CreateLight() => new CircadianSemanticLight();
    protected override IThemeSemantic? CreateDark() => new CircadianSemanticDark();
    protected override IThemeComponents? CreateComponents() => new CircadianComponents();
    protected override IThemeTypography? CreateTypography() => new CircadianTypography();

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
           CIRCADIAN — Chronobiology-based theme
           Light: Warm amber (hue 75) — energizing daylight
           Dark:  Desaturated blue (hue 250) — calming night
           Smooth transitions between color temperatures
           Selector: [data-theme-id="circadian"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── Circadian constants ── */
        :root,
        [data-theme-id="circadian"] {
            /* Warm easing — natural day/night rhythm */
            --circadian-ease: cubic-bezier(0.4, 0, 0.2, 1);
            --circadian-duration: 250ms;

            /* Temperature transition — smooth color shift */
            --circadian-transition-temperature: 600ms cubic-bezier(0.4, 0, 0.2, 1);
        }

        /* ── Base layer — warm daylight ── */
        [data-theme-id="circadian"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Cards — warm surface ── */
        [data-theme-id="circadian"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color var(--circadian-duration) var(--circadian-ease),
                        box-shadow   var(--circadian-duration) var(--circadian-ease),
                        background   var(--circadian-transition-temperature);
        }
        [data-theme-id="circadian"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="circadian"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="circadian"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="circadian"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="circadian"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--sg-radius-md) var(--sg-radius-lg);
        }
        [data-theme-id="circadian"] .sgc-card-body {
            padding: var(--sg-radius-lg);
        }
        [data-theme-id="circadian"] .sgc-card-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--sg-radius-md) var(--sg-radius-lg);
        }

        /* ── Buttons — warm press ── */
        [data-theme-id="circadian"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 8px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color var(--circadian-duration) var(--circadian-ease),
                        border-color     var(--circadian-duration) var(--circadian-ease),
                        color            var(--circadian-duration) var(--circadian-ease),
                        box-shadow       var(--circadian-duration) var(--circadian-ease),
                        transform        120ms var(--circadian-ease);
        }
        [data-theme-id="circadian"] .sgc-btn:hover:not(:disabled) {
            transform: translateY(-1px);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
        }
        [data-theme-id="circadian"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.98);
        }
        [data-theme-id="circadian"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="circadian"] .sgc-btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
        }

        [data-theme-id="circadian"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="circadian"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="circadian"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
        }

        [data-theme-id="circadian"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary-muted);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — warm focus ── */
        [data-theme-id="circadian"] .sgc-input,
        [data-theme-id="circadian"] .sgc-select,
        [data-theme-id="circadian"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color var(--circadian-duration) var(--circadian-ease),
                        box-shadow   var(--circadian-duration) var(--circadian-ease),
                        background   var(--circadian-transition-temperature);
        }
        [data-theme-id="circadian"] .sgc-input::placeholder,
        [data-theme-id="circadian"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.7;
        }
        [data-theme-id="circadian"] .sgc-input:hover:not(:focus),
        [data-theme-id="circadian"] .sgc-select:hover:not(:focus),
        [data-theme-id="circadian"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="circadian"] .sgc-input:focus,
        [data-theme-id="circadian"] .sgc-select:focus,
        [data-theme-id="circadian"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="circadian"] .sgc-input:disabled,
        [data-theme-id="circadian"] .sgc-select:disabled,
        [data-theme-id="circadian"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — warm underline ── */
        [data-theme-id="circadian"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 2px solid var(--sg-border-subtle);
            padding: 0;
            gap: 0;
        }
        [data-theme-id="circadian"] .sgc-tab {
            border-radius: 0;
            padding: 10px 16px;
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            border-bottom: 2px solid transparent;
            margin-bottom: -2px;
            transition: color var(--circadian-duration) var(--circadian-ease),
                        border-color var(--circadian-duration) var(--circadian-ease);
        }
        [data-theme-id="circadian"] .sgc-tab:hover {
            color: var(--sg-fg);
            border-bottom-color: var(--sg-border-strong);
        }
        [data-theme-id="circadian"] .sgc-tab.active,
        [data-theme-id="circadian"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            border-bottom-color: var(--sg-color-primary);
            font-weight: 600;
        }

        /* ── Alerts — warm status ── */
        [data-theme-id="circadian"] .sgc-alert {
            border: 1px solid;
            border-left-width: 4px;
            border-radius: var(--sg-radius-sm);
            padding: 12px 16px;
            font-size: 0.875rem;
        }
        [data-theme-id="circadian"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="circadian"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="circadian"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="circadian"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — warm overlay ── */
        [data-theme-id="circadian"] .sgc-modal-content,
        [data-theme-id="circadian"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-lg);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="circadian"] .sgc-modal-header,
        [data-theme-id="circadian"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--sg-radius-lg);
        }
        [data-theme-id="circadian"] .sgc-modal-body,
        [data-theme-id="circadian"] .sgc-drawer-body {
            padding: var(--sg-radius-lg);
        }
        [data-theme-id="circadian"] .sgc-modal-footer,
        [data-theme-id="circadian"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--sg-radius-md) var(--sg-radius-lg);
        }

        /* ── Navigation — warm sidebar ── */
        [data-theme-id="circadian"] .sgc-nav-link {
            border-radius: var(--sg-radius-sm);
            margin: 1px 4px;
            padding: 8px 12px;
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            transition: background var(--circadian-duration) var(--circadian-ease),
                        color      var(--circadian-duration) var(--circadian-ease);
        }
        [data-theme-id="circadian"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="circadian"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 3px 0 0 0 var(--sg-color-primary);
        }
        [data-theme-id="circadian"] .sgc-nav-section {
            padding: 20px 12px 8px;
            color: var(--sg-fg-muted);
            font-weight: 600;
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — warm display ── */
        [data-theme-id="circadian"] .sg-table,
        [data-theme-id="circadian"] .sgc-table {
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
        }
        [data-theme-id="circadian"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 2px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: 10px 12px;
        }
        [data-theme-id="circadian"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: 10px 12px;
        }
        [data-theme-id="circadian"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="circadian"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — warm pills ── */
        [data-theme-id="circadian"] .sgc-badge,
        [data-theme-id="circadian"] .sgc-chip {
            border-radius: var(--sg-radius-full);
            padding: 2px 8px;
            font-size: 0.75rem;
            font-weight: 500;
        }

        /* ── Tooltip — warm ── */
        [data-theme-id="circadian"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--sg-radius-sm);
            padding: 4px 8px;
            font-size: 0.75rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — warm ── */
        [data-theme-id="circadian"] ::-webkit-scrollbar {
            width: 8px;
            height: 8px;
        }
        [data-theme-id="circadian"] ::-webkit-scrollbar-track {
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="circadian"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 4px;
        }
        [data-theme-id="circadian"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection ── */
        [data-theme-id="circadian"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring ── */
        [data-theme-id="circadian"] :focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }

        /* ── Breadcrumb ── */
        [data-theme-id="circadian"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 4px;
        }
        [data-theme-id="circadian"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color var(--circadian-duration) var(--circadian-ease);
        }
        [data-theme-id="circadian"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="circadian"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 600;
        }

        /* ── Progress bar — warm fill ── */
        [data-theme-id="circadian"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border-radius: var(--sg-radius-full);
            overflow: hidden;
        }
        [data-theme-id="circadian"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: var(--sg-radius-full);
            transition: width 600ms var(--circadian-ease);
        }

        /* ── Skeleton — warm shimmer ── */
        [data-theme-id="circadian"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: circadian-shimmer 1.8s var(--circadian-ease) infinite;
            border-radius: 4px;
        }
        @keyframes circadian-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider ── */
        [data-theme-id="circadian"] .sgc-divider {
            border: none;
            height: 1px;
            background: var(--sg-divider);
            margin: 16px 0;
        }

        /* ── Dark mode overrides — calming night ── */
        [data-theme="dark"][data-theme-id="circadian"] {
            --sg-bg: oklch(0.13 0.012 250);
            --sg-bg-subtle: oklch(0.17 0.015 250);
            --sg-bg-muted: oklch(0.21 0.018 250);
            --sg-fg: oklch(0.95 0.005 250);
            --sg-fg-subtle: oklch(0.82 0.008 250);
            --sg-border: oklch(0.28 0.016 250);
            --sg-color-primary: oklch(0.62 0.15 250);
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="circadian"] *,
            [data-theme-id="circadian"] *::before,
            [data-theme-id="circadian"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
        }
        """;

    internal sealed class CircadianTypography : IThemeTypography
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

internal class CircadianPrimitives : IThemePrimitives
{
    // Neutral — warm gray (hue 75° for light, 250° for dark)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.004 75)";
    public virtual string Neutral100 => "oklch(0.97 0.006 75)";
    public virtual string Neutral200 => "oklch(0.93 0.009 75)";
    public virtual string Neutral300 => "oklch(0.87 0.012 75)";
    public virtual string Neutral400 => "oklch(0.76 0.014 75)";
    public virtual string Neutral500 => "oklch(0.64 0.014 75)";
    public virtual string Neutral600 => "oklch(0.52 0.016 75)";
    public virtual string Neutral700 => "oklch(0.40 0.018 75)";
    public virtual string Neutral800 => "oklch(0.28 0.02 75)";
    public virtual string Neutral900 => "oklch(0.16 0.022 75)";

    // Primary — Amber (hue 75°) — warm daylight
    public virtual string Primary50  => "oklch(0.95 0.03 75)";
    public virtual string Primary100 => "oklch(0.90 0.06 75)";
    public virtual string Primary200 => "oklch(0.82 0.10 75)";
    public virtual string Primary300 => "oklch(0.72 0.14 75)";
    public virtual string Primary400 => "oklch(0.63 0.16 75)";
    public virtual string Primary500 => "oklch(0.56 0.16 75)";
    public virtual string Primary600 => "oklch(0.49 0.15 75)";
    public virtual string Primary700 => "oklch(0.41 0.14 75)";
    public virtual string Primary800 => "oklch(0.32 0.12 75)";
    public virtual string Primary900 => "oklch(0.22 0.10 75)";

    // Success — Emerald
    public virtual string Success50  => "oklch(0.95 0.03 153)";
    public virtual string Success100 => "oklch(0.88 0.07 153)";
    public virtual string Success500 => "oklch(0.627 0.194 153.2)";
    public virtual string Success600 => "oklch(0.55 0.19 153)";
    public virtual string Success700 => "oklch(0.45 0.18 153)";

    // Danger — Red
    public virtual string Danger50  => "oklch(0.95 0.04 19)";
    public virtual string Danger100 => "oklch(0.88 0.09 19)";
    public virtual string Danger500 => "oklch(0.552 0.244 19.3)";
    public virtual string Danger600 => "oklch(0.48 0.24 19)";
    public virtual string Danger700 => "oklch(0.40 0.22 19)";

    // Warning — Deep amber
    public virtual string Warning50  => "oklch(0.97 0.04 60)";
    public virtual string Warning100 => "oklch(0.92 0.08 60)";
    public virtual string Warning500 => "oklch(0.68 0.16 60)";
    public virtual string Warning600 => "oklch(0.60 0.16 60)";

    // Info — Blue (cool counterpart)
    public virtual string Info50  => "oklch(0.95 0.03 250)";
    public virtual string Info100 => "oklch(0.88 0.06 250)";
    public virtual string Info500 => "oklch(0.55 0.14 250)";
    public virtual string Info600 => "oklch(0.47 0.14 250)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // Radii — organic, warm
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "6px";
    public virtual string RadiusMd   => "10px";
    public virtual string RadiusLg   => "14px";
    public virtual string RadiusXl   => "20px";
    public virtual string Radius2Xl  => "28px";
    public virtual string RadiusFull => "9999px";
}

internal class CircadianSemanticLight : BaseLightConsistent
{
    public CircadianSemanticLight() : base(75) { }

    // Warm daylight surfaces
    public override string BgDefault     => "oklch(0.99 0.006 75)";
    public override string BgSubtle      => "oklch(0.97 0.009 75)";
    public override string BgMuted       => "oklch(0.935 0.013 75)";
    public override string BgEmphasized  => "oklch(0.89 0.017 75)";
    public override string BgOverlay     => "oklch(0.14 0.02 75 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.006 75 / 0.7)";

    public override string Surface         => "oklch(1 0 0)";
    public override string SurfaceRaised   => "oklch(1 0 0)";
    public override string SurfaceOverlay  => "oklch(1 0 0)";

    // Warm foreground
    public override string FgDefault   => "oklch(0.14 0.02 75)";
    public override string FgSubtle    => "oklch(0.36 0.015 75)";
    public override string FgMuted     => "oklch(0.52 0.012 75)";
    public override string FgDisabled  => "oklch(0.68 0.008 75)";

    // Amber primary
    public override string ColorPrimary        => "oklch(0.56 0.16 75)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 75)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 75)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.16 75)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.15 75)";

    // Warm shadows
    public override string ShadowXs => "0 1px 1px 0 oklch(0.14 0.02 75 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.14 0.02 75 / 0.06), 0 1px 1px -1px oklch(0.14 0.02 75 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.14 0.02 75 / 0.08), 0 1px 2px -1px oklch(0.14 0.02 75 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.14 0.02 75 / 0.10), 0 2px 4px -2px oklch(0.14 0.02 75 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.14 0.02 75 / 0.14), 0 4px 8px -4px oklch(0.14 0.02 75 / 0.08)";

    public override string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px oklch(0.56 0.16 75)";
    public override string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px oklch(0.552 0.244 19.3)";
}

internal class CircadianSemanticDark : BaseDarkConsistent
{
    public CircadianSemanticDark() : base(250) { }

    // Cool night surfaces
    public override string BgDefault     => "oklch(0.13 0.012 250)";
    public override string BgSubtle      => "oklch(0.17 0.015 250)";
    public override string BgMuted       => "oklch(0.21 0.018 250)";
    public override string BgEmphasized  => "oklch(0.25 0.020 250)";

    public override string Surface         => "oklch(0.18 0.015 250)";
    public override string SurfaceRaised   => "oklch(0.22 0.018 250)";
    public override string SurfaceOverlay  => "oklch(0.22 0.018 250)";

    // Cool blue primary for night mode
    public override string ColorPrimary        => "oklch(0.62 0.15 250)";
    public override string ColorPrimaryHover   => "oklch(0.67 0.15 250)";
    public override string ColorPrimaryActive  => "oklch(0.57 0.15 250)";

    public override string ColorDanger         => "oklch(0.552 0.244 19.3)";
    public override string ColorDangerHover    => "oklch(0.62 0.24 19)";

    public override string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public override string ColorWarningHover   => "oklch(0.82 0.16 83)";
}

internal class CircadianComponents : IThemeComponents
{
    // Comfortable sizing — balanced between compact and spacious
    public virtual string BtnRadius     => "8px";
    public virtual string BtnFontSize   => "0.875rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "38px";
    public virtual string BtnHeightSm   => "32px";
    public virtual string BtnHeightLg   => "44px";

    public virtual string InputRadius   => "6px";
    public virtual string InputFontSize => "0.875rem";
    public virtual string InputHeight   => "38px";
    public virtual string InputHeightSm => "32px";
    public virtual string InputHeightLg => "44px";

    public virtual string CardRadius      => "10px";
    public virtual string CardPadding     => "16px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "14px";

    public virtual string TableRadius          => "10px";
    public virtual string TableHeaderFontWeight => "650";

    public virtual string TabsIndicatorHeight => "3px";

    public virtual string TooltipMaxWidth => "280px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
