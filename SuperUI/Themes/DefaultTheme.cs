namespace SuperUI.Themes;

/// <summary>
/// Default SuperUI theme — strict, compact, angular.
/// Palette: azure primary on slate neutrals (Inter / JetBrains Mono).
/// Geometry: φ-radii, 30px controls, 1px borders.
/// Motion: no vertical hops — color shifts + scale(0.98) press, 150ms ease-out.
/// "Изюминка": 2px accent stripe on focused inputs, top-accent that grows on cards,
/// a primary dot on active nav-link, micro letter-spacing on labels.
/// </summary>
public sealed class DefaultTheme : ThemeBase
{
    public override string Id => "superui-default";
    public override string Name => "SuperUI Default";
    public override string? Description => "Строгая компактная тема SuperUI: blue-600 + slate, маленькие радиусы, спокойные анимации.";
    public override string Version => "2.1.0";

    protected override IThemePrimitives CreatePrimitives() => new DefaultPrimitives();
    protected override IThemeSemantic CreateLight() => new DefaultSemanticLight();
    protected override IThemeSemantic? CreateDark() => new DefaultSemanticDark();
    protected override IThemeComponents? CreateComponents() => new DefaultComponents();

    public override string? AdditionalCss => """
        /* ═══════════════════════════════════════════════════════════════
           SUPERUI DEFAULT — backward-compat aliases & polish
           ═══════════════════════════════════════════════════════════════ */

        /* Backward-compat aliases: --sui-* → --sg-* */
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

            --sg-text-xs: 0.75rem;

            --sui-radius-sm:   var(--sg-radius-sm);
            --sui-radius-md:   var(--sg-radius-md);
            --sui-radius-lg:   var(--sg-radius-lg);
            --sui-radius-full: var(--sg-radius-full);

            /* φ constants & Fibonacci spacing */
            --natura-phi: 1.618033988749895;
            --natura-phi-inv: 0.618033988749895;

            /* Fibonacci spacing scale (compact) */
            --natura-fib-1: 3px;
            --natura-fib-2: 5px;
            --natura-fib-3: 8px;
            --natura-fib-4: 13px;
            --natura-fib-5: 21px;
            --natura-fib-6: 34px;
            --natura-fib-7: 55px;
            --natura-fib-8: 89px;
            --natura-fib-9: 144px;

            /* φ natural easing curves */
            --natura-ease-growth: cubic-bezier(0.19, 1, 0.22, 1);
            --natura-ease-breath: cubic-bezier(0.37, 0, 0.63, 1);
            --natura-ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
            --natura-ease-settle: cubic-bezier(0.22, 1, 0.36, 1);
            --natura-ease-fall:   cubic-bezier(0.68, 0, 0.36, 1);
        }

        /* ═══════════════════════════════════════════════════════════════
           DEFAULT THEME — strict, angular, calm, φ-proportioned
           Selector: [data-theme-id="superui-default"]
           ═══════════════════════════════════════════════════════════════ */

        [data-theme-id="superui-default"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";  /* Inter stylistic tweaks */
        }

        /* Labels & headings — tight tracking for the strict look */
        [data-theme-id="superui-default"] .sgc-label,
        [data-theme-id="superui-default"] .sgc-title,
        [data-theme-id="superui-default"] .sgc-card-title,
        [data-theme-id="superui-default"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="superui-default"] .sgc-nav-section,
        [data-theme-id="superui-default"] .sgc-thead,
        [data-theme-id="superui-default"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 11px;
        }

        /* ═══════════════════════════════════════════════════════════════
           CARDS — angular, crisp, with a top-accent that hints at primary
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border);
            border-top: 2px solid var(--sg-border);
            border-radius: var(--natura-fib-2, 5px);
            box-shadow: var(--sg-shadow-xs);
            transition: border-color 200ms var(--natura-ease-growth),
                        box-shadow   200ms var(--natura-ease-growth);
        }
        [data-theme-id="superui-default"] .sgc-card:hover {
            border-top-color: var(--sg-color-primary);
            box-shadow: var(--sg-shadow-sm);
        }
        [data-theme-id="superui-default"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="superui-default"] .sgc-card-outlined {
            box-shadow: none;
            border-top-width: 1px;
        }
        [data-theme-id="superui-default"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border: 1px solid transparent;
            border-top: 2px solid transparent;
            box-shadow: none;
        }
        [data-theme-id="superui-default"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-fib-2) var(--natura-fib-4);
        }
        [data-theme-id="superui-default"] .sgc-card-body {
            padding: var(--natura-fib-3) var(--natura-fib-4);
        }

        /* ═══════════════════════════════════════════════════════════════
           TABLES — minimal grid, accent on hover
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sg-table,
        [data-theme-id="superui-default"] .sgc-table {
            --sg-table-padding: 6px 10px;
            border: 1px solid var(--sg-border);
            border-radius: var(--natura-fib-2, 5px);
            /* overflow: hidden убран — ломает position:sticky на thead th в SgDataGrid */
        }
        [data-theme-id="superui-default"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 1px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: var(--natura-fib-2) var(--natura-fib-3);
        }
        [data-theme-id="superui-default"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-fib-1) var(--natura-fib-3);
        }
        [data-theme-id="superui-default"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="superui-default"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }

        /* ═══════════════════════════════════════════════════════════════
           SIDEBAR — strict left rail, primary dot for the active item
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-nav-link {
            border-left: 3px solid transparent;
            margin: 1px 0;
            border-radius: 0;
            padding: var(--natura-fib-1) var(--natura-fib-5) var(--natura-fib-1) var(--natura-fib-4);
            font-size: 12.5px;
            color: var(--sg-fg-subtle);
            transition: background 200ms var(--natura-ease-growth),
                        color      200ms var(--natura-ease-growth),
                        border-color 200ms var(--natura-ease-growth);
            position: relative;
        }
        [data-theme-id="superui-default"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="superui-default"] .sgc-nav-link.active {
            background: var(--sg-bg-subtle);
            border-left-color: var(--sg-color-primary);
            color: var(--sg-color-primary);
            font-weight: 600;
        }
        /* Изюминка: tiny primary dot on the right of the active item */
        [data-theme-id="superui-default"] .sgc-nav-link.active::after {
            content: "";
            position: absolute;
            right: 10px;
            top: 50%;
            width: 4px;
            height: 4px;
            background: var(--sg-color-primary);
            border-radius: 50%;
            transform: translateY(-50%);
        }
        [data-theme-id="superui-default"] .sgc-nav-section {
            padding: var(--natura-fib-5) var(--natura-fib-5) var(--natura-fib-1);
            color: var(--sg-fg-muted);
            font-weight: 700;
        }
        [data-theme-id="superui-default"] .sgc-nav-group-header {
            border-radius: 0;
            padding: var(--natura-fib-1) var(--natura-fib-4);
        }

        /* Top app bar — strict 1px border, no shadow */
        [data-theme-id="superui-default"] .sgc-header {
            background: var(--sg-bg);
            border-bottom: 1px solid var(--sg-border);
            box-shadow: none;
        }

        /* ═══════════════════════════════════════════════════════════════
           BUTTONS — strict, modern color, calm motion
           - No translateY (kills jitter against neighbours)
           - Press = scale(0.98) for tactile feedback without layout shift
           - Decoupled transitions so each property animates on its own
           - Primary keeps inset 1px highlight + soft colored halo on hover
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-btn {
            border-radius: var(--natura-fib-2, 5px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color 200ms var(--natura-ease-growth),
                        border-color     200ms var(--natura-ease-growth),
                        color            200ms var(--natura-ease-growth),
                        box-shadow       200ms var(--natura-ease-growth),
                        transform        120ms var(--natura-ease-spring);
            transform: translateZ(0);
        }
        [data-theme-id="superui-default"] .sgc-btn:hover:not(:disabled) {
            transform: none;
        }
        [data-theme-id="superui-default"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.98);
            box-shadow: none;
        }
        [data-theme-id="superui-default"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="superui-default"] .sgc-btn:disabled {
            transform: none !important;
            box-shadow: none !important;
            cursor: not-allowed;
            opacity: 0.5;
        }

        /* Primary — solid blue-600 with inset highlight + colored hover halo */
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.18),
                0 1px 2px 0 rgba(15, 23, 42, 0.08);
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.22),
                0 1px 3px 0 rgba(15, 23, 42, 0.12),
                0 0 0 3px rgba(59, 130, 246, 0.14);
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
            box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.12);
        }
        [data-theme-id="superui-default"][data-theme="dark"] .sgc-btn.sgc-btn-primary {
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.08),
                0 1px 2px 0 rgba(0, 0, 0, 0.35);
        }
        [data-theme-id="superui-default"][data-theme="dark"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.10),
                0 1px 3px 0 rgba(0, 0, 0, 0.45),
                0 0 0 3px rgba(96, 165, 250, 0.20);
        }

        /* Secondary (default) — white-on-slate, hover gives primary border + halo */
        [data-theme-id="superui-default"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary);
            color: var(--sg-color-primary-hover);
            box-shadow: 0 0 0 3px var(--sg-color-primary-subtle);
        }

        /* Danger / Success — same restraint as Primary */
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-danger,
        [data-theme-id="superui-default"] .sgc-btn.sgc-danger,
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-success,
        [data-theme-id="superui-default"] .sgc-btn.sgc-success {
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.18),
                0 1px 2px 0 rgba(15, 23, 42, 0.08);
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-danger:hover:not(:disabled),
        [data-theme-id="superui-default"] .sgc-btn.sgc-danger:hover:not(:disabled) {
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.22),
                0 1px 3px 0 rgba(15, 23, 42, 0.12),
                0 0 0 3px rgba(220, 38, 38, 0.16);
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-success:hover:not(:disabled),
        [data-theme-id="superui-default"] .sgc-btn.sgc-success:hover:not(:disabled) {
            box-shadow:
                inset 0 1px 0 rgba(255, 255, 255, 0.22),
                0 1px 3px 0 rgba(15, 23, 42, 0.12),
                0 0 0 3px rgba(16, 185, 129, 0.16);
        }

        /* Outlined / Dashed — fill with primary-subtle, no jump */
        [data-theme-id="superui-default"] .sgc-btn.sgc-outlined,
        [data-theme-id="superui-default"] .sgc-btn.sgc-dashed {
            box-shadow: none;
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-outlined:hover:not(:disabled),
        [data-theme-id="superui-default"] .sgc-btn.sgc-dashed:hover:not(:disabled) {
            background: var(--sg-color-primary-subtle);
            border-color: var(--sg-color-primary);
            color: var(--sg-color-primary-hover);
            box-shadow: none;
        }

        /* Ghost — pure background change */
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-ghost,
        [data-theme-id="superui-default"] .sgc-btn.sgc-ghost {
            box-shadow: none;
        }
        [data-theme-id="superui-default"] .sgc-btn.sgc-btn-ghost:hover:not(:disabled),
        [data-theme-id="superui-default"] .sgc-btn.sgc-ghost:hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
            box-shadow: none;
        }

        /* ═══════════════════════════════════════════════════════════════
           INPUTS — strict, with a colored left-accent on focus
           Изюминка: 2px left border tints in primary when focused
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-input,
        [data-theme-id="superui-default"] .sgc-select,
        [data-theme-id="superui-default"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--natura-fib-1, 3px);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color    200ms var(--natura-ease-growth),
                        background      200ms var(--natura-ease-growth),
                        box-shadow      200ms var(--natura-ease-growth);
        }
        [data-theme-id="superui-default"] .sgc-input::placeholder,
        [data-theme-id="superui-default"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
        }
        [data-theme-id="superui-default"] .sgc-input:hover:not(:focus),
        [data-theme-id="superui-default"] .sgc-select:hover:not(:focus),
        [data-theme-id="superui-default"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="superui-default"] .sgc-input:focus,
        [data-theme-id="superui-default"] .sgc-select:focus,
        [data-theme-id="superui-default"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow:
                inset 2px 0 0 0 var(--sg-color-primary),
                0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="superui-default"] .sgc-input:disabled,
        [data-theme-id="superui-default"] .sgc-select:disabled,
        [data-theme-id="superui-default"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
        }

        /* ═══════════════════════════════════════════════════════════════
           TABS — underlined, 2px accent, no background fills
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid var(--sg-border);
            border-radius: 0;
            padding: 0;
            gap: var(--natura-fib-1, 3px);
        }
        [data-theme-id="superui-default"] .sgc-tab {
            border-radius: 0;
            padding: var(--natura-fib-2) var(--natura-fib-3);
            border-bottom: 2px solid transparent;
            margin-bottom: -1px;
            font-weight: 500;
            font-size: 12.5px;
            color: var(--sg-fg-subtle);
            background: transparent;
            transition: color        200ms var(--natura-ease-growth),
                        border-color 200ms var(--natura-ease-growth);
        }
        [data-theme-id="superui-default"] .sgc-tab:hover {
            color: var(--sg-fg);
            border-bottom-color: var(--sg-border-strong);
        }
        [data-theme-id="superui-default"] .sgc-tab.sgc-active {
            color: var(--sg-color-primary);
            border-bottom-color: var(--sg-color-primary);
            background: transparent;
            box-shadow: none;
            font-weight: 600;
        }

        /* ═══════════════════════════════════════════════════════════════
           CHIPS / BADGES — small, angular, single-line
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-chip,
        [data-theme-id="superui-default"] .sgc-badge {
            border-radius: var(--natura-fib-1, 3px);
            padding: 1px 7px;
            font-size: 11px;
            font-weight: 500;
            line-height: 1.3;
        }
        [data-theme-id="superui-default"] .sgc-chip.sgc-chip-selected {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary);
        }

        /* ═══════════════════════════════════════════════════════════════
           ALERTS — left accent bar, strict color tints
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-alert {
            border: 1px solid;
            border-left-width: 3px;
            border-radius: var(--natura-fib-1, 3px);
            padding: var(--natura-fib-2) var(--natura-fib-3);
            font-size: 12.5px;
            box-shadow: none;
        }
        [data-theme-id="superui-default"] .sgc-alert.sgc-info    { background: var(--sg-color-info-subtle);    border-color: var(--sg-color-info-border, var(--sg-color-info));       color: var(--sg-color-info-hover); }
        [data-theme-id="superui-default"] .sgc-alert.sgc-success { background: var(--sg-color-success-subtle); border-color: var(--sg-color-success-border, var(--sg-color-success)); color: var(--sg-color-success-hover); }
        [data-theme-id="superui-default"] .sgc-alert.sgc-warn    { background: var(--sg-color-warning-subtle); border-color: var(--sg-color-warning-border, var(--sg-color-warning)); color: var(--sg-color-warning-hover); }
        [data-theme-id="superui-default"] .sgc-alert.sgc-danger  { background: var(--sg-color-danger-subtle);  border-color: var(--sg-color-danger-border, var(--sg-color-danger));   color: var(--sg-color-danger-hover); }

        /* ═══════════════════════════════════════════════════════════════
           MODAL / DRAWER — angular, lifted, no animation jitter
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] .sgc-modal-content,
        [data-theme-id="superui-default"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border);
            border-radius: var(--natura-fib-3, 8px);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="superui-default"] .sgc-modal-header,
        [data-theme-id="superui-default"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-fib-3) var(--natura-fib-5);
        }
        [data-theme-id="superui-default"] .sgc-modal-footer,
        [data-theme-id="superui-default"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--natura-fib-3) var(--natura-fib-5);
        }

        /* ═══════════════════════════════════════════════════════════════
           SCROLLBAR — thin, on-brand
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] ::-webkit-scrollbar {
            width: var(--natura-fib-2, 5px);
            height: var(--natura-fib-2, 5px);
        }
        [data-theme-id="superui-default"] ::-webkit-scrollbar-track {
            background: transparent;
        }
        [data-theme-id="superui-default"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border: 1px solid var(--sg-bg);
            border-radius: var(--natura-fib-1, 3px);
        }
        [data-theme-id="superui-default"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ═══════════════════════════════════════════════════════════════
           SELECTION — primary tint, not OS default
           ═══════════════════════════════════════════════════════════════ */
        [data-theme-id="superui-default"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ═══════════════════════════════════════════════════════════════
           Respect reduced-motion: kill all transforms & transitions
           ═══════════════════════════════════════════════════════════════ */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="superui-default"] *,
            [data-theme-id="superui-default"] *::before,
            [data-theme-id="superui-default"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
                transform: none !important;
            }
        }
        """;
}

internal class DefaultPrimitives : IThemePrimitives
{
    // Neutral — Tailwind Slate (calmer than Gray, the classic strict palette)
    public virtual string Neutral0   => "#ffffff";
    public virtual string Neutral50  => "#f8fafc";
    public virtual string Neutral100 => "#f1f5f9";
    public virtual string Neutral200 => "#e2e8f0";
    public virtual string Neutral300 => "#cbd5e1";
    public virtual string Neutral400 => "#94a3b8";
    public virtual string Neutral500 => "#64748b";
    public virtual string Neutral600 => "#475569";
    public virtual string Neutral700 => "#334155";
    public virtual string Neutral800 => "#1e293b";
    public virtual string Neutral900 => "#0f172a";

    // Primary — Azure Blue (чистый синий, без фиолетового отлива)
    public virtual string Primary50  => "#eef6ff";
    public virtual string Primary100 => "#d8e4ff";
    public virtual string Primary200 => "#b1c9ff";
    public virtual string Primary300 => "#89aeff";
    public virtual string Primary400 => "#6293ff";
    public virtual string Primary500 => "#4f86ff";
    public virtual string Primary600 => "#3B78FF";
    public virtual string Primary700 => "#2f60cc";
    public virtual string Primary800 => "#234899";
    public virtual string Primary900 => "#183066";

    // Success — Emerald
    public virtual string Success50  => "#ecfdf5";
    public virtual string Success100 => "#d1fae5";
    public virtual string Success500 => "#10b981";
    public virtual string Success600 => "#059669";
    public virtual string Success700 => "#047857";

    // Danger — Red 600 (strict, not rose)
    public virtual string Danger50  => "#fef2f2";
    public virtual string Danger100 => "#fee2e2";
    public virtual string Danger500 => "#ef4444";
    public virtual string Danger600 => "#dc2626";
    public virtual string Danger700 => "#b91c1c";

    // Warning — Amber
    public virtual string Warning50  => "#fffbeb";
    public virtual string Warning100 => "#fef3c7";
    public virtual string Warning500 => "#f59e0b";
    public virtual string Warning600 => "#d97706";

    // Info — Sky (cooler than Primary, distinguishable on info banners)
    public virtual string Info50  => "#f0f9ff";
    public virtual string Info100 => "#e0f2fe";
    public virtual string Info500 => "#0ea5e9";
    public virtual string Info600 => "#0284c7";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // φ/Fibonacci radii — organic proportions, compact scale
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";    // fib-1
    public virtual string RadiusSm   => "5px";    // fib-2
    public virtual string RadiusMd   => "8px";    // fib-3
    public virtual string RadiusLg   => "13px";   // fib-4
    public virtual string RadiusXl   => "21px";   // fib-5
    public virtual string Radius2Xl  => "34px";   // fib-6
    public virtual string RadiusFull => "9999px";
}

internal class DefaultSemanticLight : IThemeSemantic
{
    // Surfaces — clean slate-based
    public virtual string BgDefault     => "#ffffff";
    public virtual string BgSubtle      => "#f8fafc";   // slate-50
    public virtual string BgMuted       => "#f1f5f9";   // slate-100
    public virtual string BgEmphasized  => "#e2e8f0";   // slate-200
    public virtual string BgOverlay     => "rgba(15, 23, 42, 0.45)";
    public virtual string BgGlass       => "rgba(255, 255, 255, 0.7)";
    public virtual string BorderGlass   => "rgba(255, 255, 255, 0.3)";
    public virtual string BlurGlass     => "8px";

    public virtual string Surface         => "#ffffff";
    public virtual string SurfaceRaised   => "#ffffff";
    public virtual string SurfaceOverlay  => "#ffffff";

    // Text — slate scale
    public virtual string FgDefault   => "#0f172a";    // slate-900
    public virtual string FgSubtle    => "#475569";    // slate-600
    public virtual string FgMuted     => "#64748b";    // slate-500
    public virtual string FgDisabled  => "#94a3b8";    // slate-400
    public virtual string FgInverse   => "#ffffff";
    public virtual string FgLink      => "#3B78FF";    // azure blue
    public virtual string FgLinkHover => "#2f60cc";    // azure blue hover

    // Borders
    public virtual string BorderDefault => "#e2e8f0";  // slate-200
    public virtual string BorderSubtle  => "#f1f5f9";  // slate-100
    public virtual string BorderStrong  => "#cbd5e1";  // slate-300
    public virtual string BorderFocus   => "#3B78FF";  // azure blue
    public virtual string Divider       => "#f1f5f9";  // slate-100

    // Primary — Azure Blue
    public virtual string ColorPrimary        => "#3B78FF";
    public virtual string ColorPrimarySubtle  => "#eef6ff";
    public virtual string ColorPrimaryMuted   => "#d8e4ff";
    public virtual string ColorPrimaryHover   => "#2f60cc";
    public virtual string ColorPrimaryActive  => "#234899";
    public virtual string ColorPrimaryFg      => "#ffffff";

    // Success — Emerald 600
    public virtual string ColorSuccess        => "#059669";
    public virtual string ColorSuccessSubtle  => "#ecfdf5";
    public virtual string ColorSuccessHover   => "#047857";
    public virtual string ColorSuccessFg      => "#ffffff";

    // Danger — Red 600 (strict)
    public virtual string ColorDanger         => "#dc2626";
    public virtual string ColorDangerSubtle   => "#fef2f2";
    public virtual string ColorDangerHover    => "#b91c1c";
    public virtual string ColorDangerFg       => "#ffffff";

    // Warning — Amber 600
    public virtual string ColorWarning        => "#d97706";
    public virtual string ColorWarningSubtle  => "#fffbeb";
    public virtual string ColorWarningHover   => "#b45309";
    public virtual string ColorWarningFg      => "#ffffff";

    // Info — Sky 600
    public virtual string ColorInfo           => "#0284c7";
    public virtual string ColorInfoSubtle     => "#f0f9ff";
    public virtual string ColorInfoHover      => "#0369a1";
    public virtual string ColorInfoFg         => "#ffffff";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.75rem";    // 12px
    public virtual string TextBase => "0.8125rem";  // 13px (compact)
    public virtual string TextLg   => "0.9375rem";  // 15px

    public virtual string ShadowXs => "0 1px 1px 0 rgba(15, 23, 42, 0.04)";
    public virtual string ShadowSm => "0 1px 2px 0 rgba(15, 23, 42, 0.06), 0 1px 1px -1px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowMd => "0 2px 4px -1px rgba(15, 23, 42, 0.08), 0 1px 2px -1px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowLg => "0 8px 16px -4px rgba(15, 23, 42, 0.10), 0 2px 4px -2px rgba(15, 23, 42, 0.06)";
    public virtual string ShadowXl => "0 16px 32px -8px rgba(15, 23, 42, 0.14), 0 4px 8px -4px rgba(15, 23, 42, 0.08)";

    // φ/Fibonacci radii — organic proportions
    public virtual string RadiusSm   => "5px";    // fib-2
    public virtual string RadiusMd   => "8px";    // fib-3
    public virtual string RadiusLg   => "13px";   // fib-4
    public virtual string RadiusXl   => "21px";   // fib-5
    public virtual string RadiusFull => "9999px";

    // φ natural easing curves
    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px #ffffff, 0 0 0 4px #3B78FF";
    public virtual string FocusRingDanger => "0 0 0 2px #ffffff, 0 0 0 4px #dc2626";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class DefaultSemanticDark : IThemeSemantic
{
    // Dark — слои и фоны (slate, строгие)
    public virtual string BgDefault     => "oklch(0.12 0.008 240)";  // bg-app / slate-900
    public virtual string BgSubtle      => "oklch(0.20 0.012 240)";  // element / slate-800
    public virtual string BgMuted       => "oklch(0.17 0.010 240)";  // между element и surface
    public virtual string BgEmphasized  => "oklch(0.24 0.014 240)";  // чуть светлее element
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => "oklch(0.12 0.008 240 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.10)";
    public virtual string BlurGlass     => "12px";

    public virtual string Surface         => "oklch(0.14 0.010 240)";  // bg-surface
    public virtual string SurfaceRaised   => "oklch(0.16 0.012 240)";  // bg-overlay
    public virtual string SurfaceOverlay  => "oklch(0.16 0.012 240)";  // bg-overlay

    // Foreground — типографика (slate)
    public virtual string FgDefault   => "oklch(0.95 0.004 240)";   // text-primary / slate-100
    public virtual string FgSubtle    => "oklch(0.80 0.006 240)";   // text-secondary / slate-300
    public virtual string FgMuted     => "oklch(0.58 0.008 240)";   // text-muted / slate-400
    public virtual string FgDisabled  => "oklch(0.42 0.010 240)";   // slate-500
    public virtual string FgInverse   => "oklch(0.12 0.008 240)";
    public virtual string FgLink      => "oklch(0.50 0.20 240)";    // azure blue
    public virtual string FgLinkHover => "oklch(0.56 0.18 240)";    // azure blue hover

    // Borders — границы и разделители (slate)
    public virtual string BorderDefault => "oklch(0.25 0.016 240)";  // border-strong / slate-700
    public virtual string BorderSubtle  => "oklch(0.18 0.012 240)";  // border-weak
    public virtual string BorderStrong  => "oklch(0.30 0.018 240)";
    public virtual string BorderFocus   => "oklch(0.50 0.20 240)";   // azure blue
    public virtual string Divider       => "oklch(0.18 0.012 240)";  // border-weak

    // Primary — Azure Blue (чистый синий, hue 240°)
    public virtual string ColorPrimary        => "oklch(0.50 0.20 240)";
    public virtual string ColorPrimarySubtle  => "oklch(0.20 0.04 240)";
    public virtual string ColorPrimaryMuted   => "oklch(0.28 0.07 240)";
    public virtual string ColorPrimaryHover   => "oklch(0.56 0.18 240)";
    public virtual string ColorPrimaryActive  => "oklch(0.45 0.20 240)";
    public virtual string ColorPrimaryFg      => "oklch(0.98 0 0)";

    // Success — #10b981 (emerald)
    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.20 0.04 153)";
    public virtual string ColorSuccessHover   => "oklch(0.70 0.18 153)";
    public virtual string ColorSuccessFg      => "oklch(0.98 0 0)";

    // Danger — #ef4444 (red-500)
    public virtual string ColorDanger         => "oklch(0.58 0.22 22)";
    public virtual string ColorDangerSubtle   => "oklch(0.22 0.06 22)";
    public virtual string ColorDangerHover    => "oklch(0.65 0.20 22)";
    public virtual string ColorDangerFg       => "oklch(0.98 0 0)";

    // Warning — #f59e0b (amber-500)
    public virtual string ColorWarning        => "oklch(0.74 0.16 75)";
    public virtual string ColorWarningSubtle  => "oklch(0.24 0.04 75)";
    public virtual string ColorWarningHover   => "oklch(0.80 0.14 75)";
    public virtual string ColorWarningFg      => "oklch(0.12 0.008 240)";

    // Info — #0ea5e9 (sky-500)
    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.20 0.04 254)";
    public virtual string ColorInfoHover      => "oklch(0.60 0.14 254)";
    public virtual string ColorInfoFg         => "oklch(0.98 0 0)";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.75rem";
    public virtual string TextBase => "0.8125rem";
    public virtual string TextLg   => "0.9375rem";

    public virtual string ShadowXs => "0 1px 2px 0 oklch(0 0 0 / 0.40)";
    public virtual string ShadowSm => "0 2px 4px -1px oklch(0 0 0 / 0.50)";
    public virtual string ShadowMd => "0 4px 12px -2px oklch(0 0 0 / 0.55)";
    public virtual string ShadowLg => "0 8px 24px -4px oklch(0 0 0 / 0.60)";
    public virtual string ShadowXl => "0 16px 48px -8px oklch(0 0 0 / 0.65)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px oklch(0.12 0.008 240), 0 0 0 4px oklch(0.50 0.20 240)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.12 0.008 240), 0 0 0 4px oklch(0.58 0.22 22)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class DefaultComponents : IThemeComponents
{
    // φ/Fibonacci proportions — compact scale
    public virtual string BtnRadius     => "5px";    // fib-2
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";
    public virtual string BtnHeightSm   => "24px";
    public virtual string BtnHeightLg   => "36px";

    public virtual string InputRadius   => "3px";    // fib-1
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "24px";
    public virtual string InputHeightLg => "36px";

    public virtual string CardRadius      => "5px";  // fib-2
    public virtual string CardPadding     => "8px";  // fib-3
    public virtual string CardBorderColor => "var(--sg-border)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "8px";     // fib-3

    public virtual string TableRadius          => "5px";   // fib-2
    public virtual string TableHeaderFontWeight => "600";

    public virtual string TabsIndicatorHeight => "2px";
    public virtual string TooltipMaxWidth     => "240px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-bg-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
