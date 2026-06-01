namespace SuperUI.Themes;

/// <summary>
/// Biofilia — биофильный дизайн, связь с природой.
/// Forest green primary (hue 145), earth brown accents (hue 30).
/// Fibonacci spacing, organic radii, natural easing curves.
/// </summary>
public sealed class BiofiliaTheme : ThemeBase
{
    public override string Id => "biofilia";
    public override string Name => "Biofilia";
    public override string? Description => "Биофильный дизайн — связь с природой снижает стресс. Золотое сечение, естественные гармонии, organic radii.";
    public override string? Author => "SuperUI";
    public override string Version => "1.0.0";
    public override string Category => "Nature";

    protected override IThemePrimitives CreatePrimitives() => new BiofiliaPrimitives();
    protected override IThemeSemantic CreateLight() => new BiofiliaSemanticLight();
    protected override IThemeSemantic? CreateDark() => new BiofiliaSemanticDark();
    protected override IThemeComponents? CreateComponents() => new BiofiliaComponents();
    protected override IThemeTypography? CreateTypography() => new BiofiliaTypography();

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
           BIOFILIA — Biophilic design, connection with nature
           Brand: Forest green (hue 145) + Earth brown accents (hue 30)
           Fibonacci spacing, organic radii, natural easing curves
           Reduces stress through nature-inspired design
           Selector: [data-theme-id="biofilia"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── Biofilia constants ── */
        :root,
        [data-theme-id="biofilia"] {
            /* Fibonacci spacing scale */
            --bio-space-1: 3px;
            --bio-space-2: 5px;
            --bio-space-3: 8px;
            --bio-space-4: 13px;
            --bio-space-5: 21px;
            --bio-space-6: 34px;
            --bio-space-7: 55px;
            --bio-space-8: 89px;

            /* Organic border radius */
            --bio-radius-organic: 34% 66% 55% 45% / 45% 41% 59% 55%;

            /* Natural easing — breath, growth, spring */
            --bio-ease-breath: cubic-bezier(0.37, 0, 0.63, 1);
            --bio-ease-growth: cubic-bezier(0.19, 1, 0.22, 1);
            --bio-ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
            --bio-ease-settle: cubic-bezier(0.22, 1, 0.36, 1);
            --bio-duration: 250ms;
        }

        /* ── Base layer — natural feel ── */
        [data-theme-id="biofilia"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Labels, titles — natural tracking ── */
        [data-theme-id="biofilia"] .sgc-label,
        [data-theme-id="biofilia"] .sgc-title,
        [data-theme-id="biofilia"] .sgc-card-title,
        [data-theme-id="biofilia"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="biofilia"] .sgc-nav-section,
        [data-theme-id="biofilia"] .sgc-thead,
        [data-theme-id="biofilia"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 11px;
        }

        /* ── Cards — organic, natural ── */
        [data-theme-id="biofilia"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color var(--bio-duration) var(--bio-ease-growth),
                        box-shadow   var(--bio-duration) var(--bio-ease-growth),
                        transform    200ms var(--bio-ease-settle);
            transform: translateZ(0);
        }
        [data-theme-id="biofilia"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="biofilia"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="biofilia"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="biofilia"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="biofilia"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--bio-space-3) var(--bio-space-4);
        }
        [data-theme-id="biofilia"] .sgc-card-body {
            padding: var(--bio-space-4);
        }
        [data-theme-id="biofilia"] .sgc-card-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--bio-space-3) var(--bio-space-4);
        }

        /* ── Buttons — natural press, spring effect ── */
        [data-theme-id="biofilia"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 8px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color var(--bio-duration) var(--bio-ease-growth),
                        border-color     var(--bio-duration) var(--bio-ease-growth),
                        color            var(--bio-duration) var(--bio-ease-growth),
                        box-shadow       var(--bio-duration) var(--bio-ease-growth),
                        transform        120ms var(--bio-ease-spring);
            transform: translateZ(0);
            will-change: transform;
        }
        [data-theme-id="biofilia"] .sgc-btn:hover:not(:disabled) {
            transform: translateY(-1px);
        }
        [data-theme-id="biofilia"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.97);
            transition-duration: 80ms;
        }
        [data-theme-id="biofilia"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="biofilia"] .sgc-btn:disabled {
            transform: none !important;
            opacity: 0.45;
            cursor: not-allowed;
        }

        [data-theme-id="biofilia"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="biofilia"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="biofilia"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
            box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        [data-theme-id="biofilia"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary-muted);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — organic focus ── */
        [data-theme-id="biofilia"] .sgc-input,
        [data-theme-id="biofilia"] .sgc-select,
        [data-theme-id="biofilia"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--sg-radius-sm);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color var(--bio-duration) var(--bio-ease-growth),
                        box-shadow   var(--bio-duration) var(--bio-ease-growth),
                        background   var(--bio-duration) var(--bio-ease-growth);
        }
        [data-theme-id="biofilia"] .sgc-input::placeholder,
        [data-theme-id="biofilia"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.7;
        }
        [data-theme-id="biofilia"] .sgc-input:hover:not(:focus),
        [data-theme-id="biofilia"] .sgc-select:hover:not(:focus),
        [data-theme-id="biofilia"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="biofilia"] .sgc-input:focus,
        [data-theme-id="biofilia"] .sgc-select:focus,
        [data-theme-id="biofilia"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 2px 0 0 var(--sg-color-primary),
                        0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="biofilia"] .sgc-input:disabled,
        [data-theme-id="biofilia"] .sgc-select:disabled,
        [data-theme-id="biofilia"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — natural underline ── */
        [data-theme-id="biofilia"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid var(--sg-border-subtle);
            padding: 0;
            gap: 2px;
        }
        [data-theme-id="biofilia"] .sgc-tab {
            border-radius: 3px 3px 0 0;
            padding: var(--bio-space-2) var(--bio-space-4);
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            transition: color var(--bio-duration) var(--bio-ease-growth),
                        background var(--bio-duration) var(--bio-ease-growth);
        }
        [data-theme-id="biofilia"] .sgc-tab:hover {
            color: var(--sg-fg);
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="biofilia"] .sgc-tab.active,
        [data-theme-id="biofilia"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            background: var(--sg-color-primary-subtle);
        }

        /* ── Alerts — natural left accent ── */
        [data-theme-id="biofilia"] .sgc-alert {
            border: 1px solid;
            border-left-width: 3px;
            border-radius: var(--sg-radius-sm);
            padding: var(--bio-space-3) var(--bio-space-4);
            font-size: 0.875rem;
        }
        [data-theme-id="biofilia"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="biofilia"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="biofilia"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="biofilia"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — organic, lifted ── */
        [data-theme-id="biofilia"] .sgc-modal-content,
        [data-theme-id="biofilia"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-lg);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="biofilia"] .sgc-modal-header,
        [data-theme-id="biofilia"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--bio-space-4);
        }
        [data-theme-id="biofilia"] .sgc-modal-body,
        [data-theme-id="biofilia"] .sgc-drawer-body {
            padding: var(--bio-space-4);
        }
        [data-theme-id="biofilia"] .sgc-modal-footer,
        [data-theme-id="biofilia"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--bio-space-3) var(--bio-space-4);
        }

        /* ── Navigation — natural left glow ── */
        [data-theme-id="biofilia"] .sgc-nav-link {
            border-radius: var(--sg-radius-sm);
            margin: 1px 4px;
            padding: var(--bio-space-2) var(--bio-space-3);
            font-size: 0.875rem;
            color: var(--sg-fg-subtle);
            transition: background var(--bio-duration) var(--bio-ease-growth),
                        color      var(--bio-duration) var(--bio-ease-growth),
                        box-shadow var(--bio-duration) var(--bio-ease-growth);
        }
        [data-theme-id="biofilia"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="biofilia"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 2px 0 0 0 var(--sg-color-primary);
        }
        [data-theme-id="biofilia"] .sgc-nav-section {
            padding: var(--bio-space-5) var(--bio-space-3) var(--bio-space-2);
            color: var(--sg-fg-muted);
            font-weight: 600;
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — natural, spacious ── */
        [data-theme-id="biofilia"] .sg-table,
        [data-theme-id="biofilia"] .sgc-table {
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--sg-radius-md);
        }
        [data-theme-id="biofilia"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 1px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: var(--bio-space-2) var(--bio-space-3);
        }
        [data-theme-id="biofilia"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--bio-space-2) var(--bio-space-3);
        }
        [data-theme-id="biofilia"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="biofilia"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="biofilia"] .sgc-table tbody tr:nth-child(even) td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — organic pills ── */
        [data-theme-id="biofilia"] .sgc-badge,
        [data-theme-id="biofilia"] .sgc-chip {
            border-radius: 9999px;
            padding: 2px var(--bio-space-2);
            font-size: 0.75rem;
            font-weight: 500;
        }

        /* ── Tooltip — organic bubble ── */
        [data-theme-id="biofilia"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--bio-space-2);
            padding: 3px var(--bio-space-2);
            font-size: 0.75rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — natural, subtle ── */
        [data-theme-id="biofilia"] ::-webkit-scrollbar {
            width: 6px;
            height: 6px;
        }
        [data-theme-id="biofilia"] ::-webkit-scrollbar-track {
            background: transparent;
        }
        [data-theme-id="biofilia"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 9999px;
            border: 1px solid var(--sg-bg);
        }
        [data-theme-id="biofilia"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection — green tint ── */
        [data-theme-id="biofilia"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring — natural glow ── */
        [data-theme-id="biofilia"] :focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
            border-radius: 2px;
        }

        /* ── Breadcrumb — natural separator ── */
        [data-theme-id="biofilia"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 var(--bio-space-2);
        }
        [data-theme-id="biofilia"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color 150ms var(--bio-ease-growth);
        }
        [data-theme-id="biofilia"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="biofilia"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 600;
        }

        /* ── Progress bar — natural fill ── */
        [data-theme-id="biofilia"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border-radius: 9999px;
            overflow: hidden;
        }
        [data-theme-id="biofilia"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: 9999px;
            transition: width 600ms var(--bio-ease-growth);
        }

        /* ── Skeleton — natural shimmer ── */
        [data-theme-id="biofilia"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: bio-shimmer 1.5s var(--bio-ease-breath) infinite;
            border-radius: 2px;
        }
        @keyframes bio-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider — natural line ── */
        [data-theme-id="biofilia"] .sgc-divider {
            border: none;
            height: 1px;
            background: linear-gradient(
                90deg,
                transparent 0%,
                var(--sg-divider) 15%,
                var(--sg-divider) 85%,
                transparent 100%
            );
            margin: var(--bio-space-4) 0;
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="biofilia"] *,
            [data-theme-id="biofilia"] *::before,
            [data-theme-id="biofilia"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
            [data-theme-id="biofilia"] .sgc-btn:hover:not(:disabled) {
                transform: none !important;
            }
            [data-theme-id="biofilia"] .sgc-btn:active:not(:disabled) {
                transform: none !important;
            }
        }
        """;

    internal sealed class BiofiliaTypography : IThemeTypography
    {
        public string GoogleFontsImportUrl => "https://fonts.googleapis.com/css2?family=Nunito:wght@400;500;600;700&family=JetBrains+Mono&display=swap";
        public bool EmbedGoogleFontsImport => true;
        public string? HeadingFont => "'Nunito', system-ui, -apple-system, 'Segoe UI', sans-serif";
        public HeadingSettings H1 => new("2.5rem", HeadingFont, "700", "1.15", "-0.02em");
        public HeadingSettings H2 => new("2rem", HeadingFont, "600", "1.2", "-0.015em");
        public HeadingSettings H3 => new("1.5rem", HeadingFont, "600", "1.25", "-0.01em");
        public HeadingSettings H4 => new("1.125rem", HeadingFont, "600", "1.3", "0");
        public HeadingSettings H5 => new("1rem", HeadingFont, "600", "1.35", "0");
        public HeadingSettings H6 => new("0.875rem", HeadingFont, "500", "1.4", "0.01em");
    }
}

internal class BiofiliaPrimitives : IThemePrimitives
{
    // Neutral — warm earth (hue 30°, aligned with earth brown accents)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.005 30)";
    public virtual string Neutral100 => "oklch(0.97 0.007 30)";
    public virtual string Neutral200 => "oklch(0.93 0.010 30)";
    public virtual string Neutral300 => "oklch(0.87 0.013 30)";
    public virtual string Neutral400 => "oklch(0.76 0.015 30)";
    public virtual string Neutral500 => "oklch(0.64 0.015 30)";
    public virtual string Neutral600 => "oklch(0.52 0.017 30)";
    public virtual string Neutral700 => "oklch(0.40 0.019 30)";
    public virtual string Neutral800 => "oklch(0.28 0.021 30)";
    public virtual string Neutral900 => "oklch(0.16 0.023 30)";

    // Primary — Forest green (hue 145°) — nature, growth, life
    public virtual string Primary50  => "oklch(0.95 0.03 145)";
    public virtual string Primary100 => "oklch(0.90 0.06 145)";
    public virtual string Primary200 => "oklch(0.82 0.10 145)";
    public virtual string Primary300 => "oklch(0.72 0.14 145)";
    public virtual string Primary400 => "oklch(0.63 0.16 145)";
    public virtual string Primary500 => "oklch(0.55 0.16 145)";
    public virtual string Primary600 => "oklch(0.48 0.15 145)";
    public virtual string Primary700 => "oklch(0.40 0.14 145)";
    public virtual string Primary800 => "oklch(0.30 0.12 145)";
    public virtual string Primary900 => "oklch(0.20 0.10 145)";

    // Success — Bright green (hue 150°)
    public virtual string Success50  => "oklch(0.95 0.035 150)";
    public virtual string Success100 => "oklch(0.88 0.07 150)";
    public virtual string Success500 => "oklch(0.62 0.18 150)";
    public virtual string Success600 => "oklch(0.54 0.18 150)";
    public virtual string Success700 => "oklch(0.45 0.17 150)";

    // Danger — Soft red (hue 10°) — earthy, not harsh
    public virtual string Danger50  => "oklch(0.95 0.04 10)";
    public virtual string Danger100 => "oklch(0.88 0.09 10)";
    public virtual string Danger500 => "oklch(0.58 0.20 10)";
    public virtual string Danger600 => "oklch(0.50 0.20 10)";
    public virtual string Danger700 => "oklch(0.42 0.19 10)";

    // Warning — Warm amber (hue 60°)
    public virtual string Warning50  => "oklch(0.97 0.04 60)";
    public virtual string Warning100 => "oklch(0.92 0.08 60)";
    public virtual string Warning500 => "oklch(0.68 0.16 60)";
    public virtual string Warning600 => "oklch(0.60 0.16 60)";

    // Info — Sky blue (hue 210°)
    public virtual string Info50  => "oklch(0.95 0.03 210)";
    public virtual string Info100 => "oklch(0.88 0.06 210)";
    public virtual string Info500 => "oklch(0.58 0.14 210)";
    public virtual string Info600 => "oklch(0.50 0.14 210)";

    public virtual string FontSans  => "'Nunito', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // Radii — organic, Fibonacci-based
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string Radius2Xl  => "34px";
    public virtual string RadiusFull => "9999px";
}

internal class BiofiliaSemanticLight : BaseLightConsistent
{
    public BiofiliaSemanticLight() : base(145) { }

    // Warm, earthy surfaces
    public override string BgDefault     => "oklch(0.99 0.005 30)";
    public override string BgSubtle      => "oklch(0.97 0.007 30)";
    public override string BgMuted       => "oklch(0.935 0.011 30)";
    public override string BgEmphasized  => "oklch(0.89 0.015 30)";
    public override string BgOverlay     => "oklch(0.14 0.02 30 / 0.35)";
    public override string BgGlass       => "oklch(0.99 0.005 30 / 0.7)";

    public override string Surface         => "oklch(1 0 0)";
    public override string SurfaceRaised   => "oklch(1 0 0)";
    public override string SurfaceOverlay  => "oklch(1 0 0)";

    // Warm foreground — earthy tones
    public override string FgDefault   => "oklch(0.14 0.02 30)";
    public override string FgSubtle    => "oklch(0.36 0.015 30)";
    public override string FgMuted     => "oklch(0.52 0.012 30)";
    public override string FgDisabled  => "oklch(0.68 0.008 30)";

    // Forest green primary
    public override string ColorPrimary        => "oklch(0.55 0.16 145)";
    public override string ColorPrimarySubtle  => "oklch(0.94 0.04 145)";
    public override string ColorPrimaryMuted   => "oklch(0.85 0.08 145)";
    public override string ColorPrimaryHover   => "oklch(0.50 0.16 145)";
    public override string ColorPrimaryActive  => "oklch(0.44 0.15 145)";

    // Earthy shadows
    public override string ShadowXs => "0 1px 1px 0 oklch(0.14 0.02 30 / 0.04)";
    public override string ShadowSm => "0 1px 2px 0 oklch(0.14 0.02 30 / 0.06), 0 1px 1px -1px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowMd => "0 2px 4px -1px oklch(0.14 0.02 30 / 0.08), 0 1px 2px -1px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowLg => "0 8px 16px -4px oklch(0.14 0.02 30 / 0.10), 0 2px 4px -2px oklch(0.14 0.02 30 / 0.06)";
    public override string ShadowXl => "0 16px 32px -8px oklch(0.14 0.02 30 / 0.14), 0 4px 8px -4px oklch(0.14 0.02 30 / 0.08)";

    public override string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px oklch(0.55 0.16 145)";
    public override string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px oklch(0.58 0.20 10)";
}

internal class BiofiliaSemanticDark : BaseDarkConsistent
{
    public BiofiliaSemanticDark() : base(145) { }

    public override string ColorPrimary        => "oklch(0.62 0.16 145)";
    public override string ColorPrimaryHover   => "oklch(0.67 0.16 145)";
    public override string ColorPrimaryActive  => "oklch(0.57 0.16 145)";

    public override string ColorDanger         => "oklch(0.58 0.20 10)";
    public override string ColorDangerHover    => "oklch(0.65 0.20 10)";

    public override string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public override string ColorWarningHover   => "oklch(0.82 0.16 83)";
}

internal class BiofiliaComponents : IThemeComponents
{
    // Natural sizing — balanced
    public virtual string BtnRadius     => "8px";
    public virtual string BtnFontSize   => "0.875rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "38px";
    public virtual string BtnHeightSm   => "32px";
    public virtual string BtnHeightLg   => "44px";

    public virtual string InputRadius   => "5px";
    public virtual string InputFontSize => "0.875rem";
    public virtual string InputHeight   => "38px";
    public virtual string InputHeightSm => "32px";
    public virtual string InputHeightLg => "44px";

    public virtual string CardRadius      => "8px";
    public virtual string CardPadding     => "13px";
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "13px";

    public virtual string TableRadius          => "8px";
    public virtual string TableHeaderFontWeight => "650";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "280px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
