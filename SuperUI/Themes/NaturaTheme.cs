namespace SuperUI.Themes;

internal static class NaturaMath
{
    /// <summary>φ — 1.618033988749895</summary>
    internal const double Phi = 1.618033988749895;

    /// <summary>1/φ — 0.618033988749895</summary>
    internal const double InvPhi = 0.618033988749895;

    /// <summary>Золотой угол 137.5° в градусах</summary>
    internal const double GoldenAngle = 137.50776405003785;

    /// <summary>Plastic number — 1.324717957244746</summary>
    internal const double Plastic = 1.324717957244746;

    /// <summary>Числа Фибоначчи: 3, 5, 8, 13, 21, 34, 55, 89, 144</summary>
    internal static readonly int[] Fibonacci = [3, 5, 8, 13, 21, 34, 55, 89, 144];

    /// <summary>Контейнеры по Фибоначчи: micro, mobile, readable, product, wide</summary>
    internal static readonly int[] ContainerWidths = [233, 377, 610, 987, 1597];

    /// <summary>Модульная шкала на φ: base × φⁿ для n от -2 до 4</summary>
    internal static double PhiScale(double baseSize, int step) =>
        baseSize * Math.Pow(Phi, step);

    internal static string PhiScaleRem(double baseSize, int step) =>
        $"{PhiScale(baseSize, step):F3}rem";

    /// <summary>Разделение ширины на две части в пропорции φ (large = 0.618×width)</summary>
    internal static (double Large, double Small) PhiSplit(double width) =>
        (width / (Phi + 1) * Phi, width / (Phi + 1));

    /// <summary>Адаптивный коэффициент: от low (мобильный) до high (десктоп)</summary>
    internal static double AdaptiveRatio(double viewport, double minW = 360, double maxW = 1440, double low = 1.25, double high = 1.618033988749895)
    {
        var t = (viewport - minW) / (maxW - minW);
        t = Math.Max(0, Math.Min(1, t));
        return low + (high - low) * t;
    }

    /// <summary>Генерация палитры по золотому углу: n цветов, начиная с baseHue</summary>
    internal static string[] GoldenPalette(int n, double baseHue = 225, double lightness = 0.52, double chroma = 0.18)
    {
        var palette = new string[n];
        for (var i = 0; i < n; i++)
        {
            var hue = (baseHue + GoldenAngle * i) % 360;
            palette[i] = $"oklch({lightness:F3} {chroma:F3} {hue:F2})";
        }
        return palette;
    }

    /// <summary>Тональная шкала: lightness по логарифму Вебера-Фехнера</summary>
    internal static string TonalStop(double hue = 225, double chroma = 0.08, int stop = 500)
    {
        var lightness = stop switch
        {
            50 => 0.97, 100 => 0.93, 200 => 0.86, 300 => 0.76, 400 => 0.66,
            500 => 0.56, 600 => 0.47, 700 => 0.38, 800 => 0.29, 900 => 0.20,
            950 => 0.14, _ => 0.56
        };
        var c = chroma * (stop >= 300 && stop <= 600 ? 1.1 : 0.75);
        return $"oklch({lightness:F3} {c:F3} {hue:F2})";
    }
}

/// <summary>
/// Natura UI — природная дизайн-система на золотом сечении φ, числах Фибоначчи.
/// Светлая тема: Royal Cornflower — белый с оттенком василька (OKLCH).
/// Тёмная тема: «две скрепки» — холодный металлик, сталь.
/// Размеры уменьшены: medium → large, compact-форма.
/// </summary>
public sealed class NaturaTheme : ThemeBase
{
    public override string Id => "natura-ui";
    public override string Name => "Natura UI";
    public override string? Description => "Природная дизайн-система: φ-пропорции, Фибоначчи-спейсинг. Светлая — Royal Cornflower (OKLCH). Тёмная — металлик «две скрепки». Уменьшенные размеры.";
    public override string? Author => "SuperUI + Natura";
    public override string Version => "1.0.0";

    protected override IThemePrimitives CreatePrimitives() => new NaturaPrimitives();
    protected override IThemeSemantic CreateLight() => new NaturaSemanticLight();
    protected override IThemeSemantic? CreateDark() => new NaturaSemanticDark();
    protected override IThemeComponents? CreateComponents() => new NaturaComponents();

    protected override IThemeTypography? CreateTypography() => new NaturaTypography();

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
           NATURA UI — φ-proportioned design system
           Brand: Royal Cornflower / green #00A86B / red #E11D48
           Light: cool white with cornflower tint
           Dark:  deep cool steel
           Compact sizing, φ/Fibonacci proportions
           Selector: [data-theme-id="natura-ui"]
           ═══════════════════════════════════════════════════════════════ */

        /* ── φ constants & Golden Ratio helpers ── */
        :root,
        [data-theme-id="natura-ui"] {
            --natura-phi: {{NaturaMath.Phi}};
            --natura-phi-inv: {{NaturaMath.InvPhi}};
            --natura-golden-angle: {{NaturaMath.GoldenAngle}}deg;

            /* φ modular typography scale (base 16px) */
            --natura-text-micro:   0.625rem;   /* 10px */
            --natura-text-caption: 0.75rem;    /* 12px */
            --natura-text-small:   0.8125rem;  /* 13px */
            --natura-text-body:    1rem;       /* 16px */
            --natura-text-h4:      1.625rem;   /* 26px */
            --natura-text-h3:      2.625rem;   /* 42px */
            --natura-text-h2:      4.25rem;    /* 68px */
            --natura-text-h1:      6.875rem;   /* 110px */

            /* Fibonacci spacing scale */
            --natura-space-1: 3px;
            --natura-space-2: 5px;
            --natura-space-3: 8px;
            --natura-space-4: 13px;
            --natura-space-5: 21px;
            --natura-space-6: 34px;
            --natura-space-7: 55px;
            --natura-space-8: 89px;
            --natura-space-9: 144px;

            /* Organic border radius – φ proportion nesting */
            --natura-radius-organic: 34% 66% 55% 45% / 45% 41% 59% 55%;

            /* Natural easing – logistic S-curve & damped sine */
            --natura-ease-growth: cubic-bezier(0.19, 1, 0.22, 1);
            --natura-ease-breath: cubic-bezier(0.37, 0, 0.63, 1);
            --natura-ease-spring: cubic-bezier(0.34, 1.56, 0.64, 1);
            --natura-ease-settle: cubic-bezier(0.22, 1, 0.36, 1);
            --natura-ease-fall:   cubic-bezier(0.68, 0, 0.36, 1);

            /* φ container widths */
            --natura-container-micro:  {{NaturaMath.ContainerWidths[0]}}px;
            --natura-container-mobile: {{NaturaMath.ContainerWidths[1]}}px;
            --natura-container-readable: {{NaturaMath.ContainerWidths[2]}}px;
            --natura-container-product: {{NaturaMath.ContainerWidths[3]}}px;
            --natura-container-wide:    {{NaturaMath.ContainerWidths[4]}}px;
        }

        /* ── Base layer ── */
        [data-theme-id="natura-ui"] {
            -webkit-font-smoothing: antialiased;
            -moz-osx-font-smoothing: grayscale;
            font-feature-settings: "cv02", "cv03", "cv04", "cv11";
            font-optical-sizing: auto;
        }

        /* ── Labels, titles — tight tracking ── */
        [data-theme-id="natura-ui"] .sgc-label,
        [data-theme-id="natura-ui"] .sgc-title,
        [data-theme-id="natura-ui"] .sgc-card-title,
        [data-theme-id="natura-ui"] .sgc-modal-title {
            letter-spacing: -0.005em;
        }
        [data-theme-id="natura-ui"] .sgc-nav-section,
        [data-theme-id="natura-ui"] .sgc-thead,
        [data-theme-id="natura-ui"] .sgc-table thead th {
            letter-spacing: 0.04em;
            text-transform: uppercase;
            font-size: 11px;
        }

        /* ── φ-proportioned headings ── */
        [data-theme-id="natura-ui"] h1,
        [data-theme-id="natura-ui"] .sgc-heading-1 {
            font-size: var(--natura-text-h1);
            line-height: 1.1;
            letter-spacing: -0.025em;
            font-weight: 700;
            color: var(--sg-fg);
        }
        [data-theme-id="natura-ui"] h2,
        [data-theme-id="natura-ui"] .sgc-heading-2 {
            font-size: var(--natura-text-h2);
            line-height: 1.15;
            letter-spacing: -0.02em;
            font-weight: 700;
            color: var(--sg-fg);
        }
        [data-theme-id="natura-ui"] h3,
        [data-theme-id="natura-ui"] .sgc-heading-3 {
            font-size: var(--natura-text-h3);
            line-height: 1.2;
            letter-spacing: -0.015em;
            font-weight: 650;
            color: var(--sg-fg);
        }
        [data-theme-id="natura-ui"] h4,
        [data-theme-id="natura-ui"] .sgc-heading-4 {
            font-size: var(--natura-text-h4);
            line-height: 1.3;
            letter-spacing: -0.01em;
            font-weight: 600;
            color: var(--sg-fg);
        }

        /* ── Cards with organic φ proportions ── */
        [data-theme-id="natura-ui"] .sgc-card {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--natura-space-2, 5px);
            box-shadow: var(--sg-shadow-sm);
            transition: border-color 250ms var(--natura-ease-growth),
                        box-shadow   250ms var(--natura-ease-growth),
                        transform    200ms var(--natura-ease-settle);
            transform: translateZ(0);
        }
        [data-theme-id="natura-ui"] .sgc-card:hover {
            border-color: var(--sg-color-primary-muted);
            box-shadow: var(--sg-shadow-md);
        }
        [data-theme-id="natura-ui"] .sgc-card-elevated {
            box-shadow: var(--sg-shadow-lg);
        }
        [data-theme-id="natura-ui"] .sgc-card-outlined {
            background: transparent;
            box-shadow: none;
        }
        [data-theme-id="natura-ui"] .sgc-card-filled {
            background: var(--sg-bg-subtle);
            border-color: transparent;
            box-shadow: none;
        }
        [data-theme-id="natura-ui"] .sgc-card-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-space-2) var(--natura-space-3);
        }
        [data-theme-id="natura-ui"] .sgc-card-body {
            padding: var(--natura-space-3);
        }
        [data-theme-id="natura-ui"] .sgc-card-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--natura-space-2) var(--natura-space-3);
        }

        /* ── Buttons — φ-proportioned, natural press ── */
        [data-theme-id="natura-ui"] .sgc-btn {
            border-radius: var(--sgc-btn-radius, 5px);
            font-weight: 600;
            letter-spacing: 0.01em;
            transition: background-color 200ms var(--natura-ease-growth),
                        border-color     200ms var(--natura-ease-growth),
                        color            200ms var(--natura-ease-growth),
                        box-shadow       200ms var(--natura-ease-growth),
                        transform        120ms var(--natura-ease-spring);
            transform: translateZ(0);
            will-change: transform;
        }
        [data-theme-id="natura-ui"] .sgc-btn:hover:not(:disabled) {
            transform: translateY(-1px);
        }
        [data-theme-id="natura-ui"] .sgc-btn:active:not(:disabled) {
            transform: scale(0.97);
            transition-duration: 80ms;
        }
        [data-theme-id="natura-ui"] .sgc-btn:focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
        }
        [data-theme-id="natura-ui"] .sgc-btn:disabled {
            transform: none !important;
            opacity: 0.45;
            cursor: not-allowed;
        }

        [data-theme-id="natura-ui"] .sgc-btn.sgc-btn-primary {
            background: var(--sg-color-primary);
            border: 1px solid var(--sg-color-primary);
            color: var(--sg-color-primary-fg);
        }
        [data-theme-id="natura-ui"] .sgc-btn.sgc-btn-primary:hover:not(:disabled) {
            background: var(--sg-color-primary-hover);
            border-color: var(--sg-color-primary-hover);
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15),
                        0 0 0 3px var(--sg-color-primary-subtle);
        }
        [data-theme-id="natura-ui"] .sgc-btn.sgc-btn-primary:active:not(:disabled) {
            background: var(--sg-color-primary-active);
            border-color: var(--sg-color-primary-active);
            box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.1);
        }

        [data-theme-id="natura-ui"] .sgc-btn:not(.sgc-btn-primary):not(.sgc-btn-danger):not(.sgc-btn-success):not(.sgc-outlined):not(.sgc-ghost):not(.sgc-dashed):hover:not(:disabled) {
            background: var(--sg-bg-subtle);
            border-color: var(--sg-color-primary-muted);
            color: var(--sg-color-primary-hover);
        }

        /* ── Inputs — bottom-grow focus, organic ── */
        [data-theme-id="natura-ui"] .sgc-input,
        [data-theme-id="natura-ui"] .sgc-select,
        [data-theme-id="natura-ui"] .sgc-textarea {
            border: 1px solid var(--sg-border);
            border-radius: var(--natura-space-1, 3px);
            background: var(--sg-bg);
            color: var(--sg-fg);
            transition: border-color 200ms var(--natura-ease-growth),
                        box-shadow   200ms var(--natura-ease-growth),
                        background   200ms var(--natura-ease-growth);
        }
        [data-theme-id="natura-ui"] .sgc-input::placeholder,
        [data-theme-id="natura-ui"] .sgc-textarea::placeholder {
            color: var(--sg-fg-muted);
            opacity: 0.7;
        }
        [data-theme-id="natura-ui"] .sgc-input:hover:not(:focus),
        [data-theme-id="natura-ui"] .sgc-select:hover:not(:focus),
        [data-theme-id="natura-ui"] .sgc-textarea:hover:not(:focus) {
            border-color: var(--sg-border-strong);
        }
        [data-theme-id="natura-ui"] .sgc-input:focus,
        [data-theme-id="natura-ui"] .sgc-select:focus,
        [data-theme-id="natura-ui"] .sgc-textarea:focus {
            border-color: var(--sg-color-primary);
            background: var(--sg-bg);
            box-shadow: 0 2px 0 0 var(--sg-color-primary),
                        0 0 0 3px var(--sg-color-primary-subtle);
            outline: none;
        }
        [data-theme-id="natura-ui"] .sgc-input:disabled,
        [data-theme-id="natura-ui"] .sgc-select:disabled,
        [data-theme-id="natura-ui"] .sgc-textarea:disabled {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg-disabled);
            cursor: not-allowed;
            opacity: 0.6;
        }

        /* ── Tabs — organic underline ── */
        [data-theme-id="natura-ui"] .sgc-tabs-strip {
            background: transparent;
            border-bottom: 1px solid var(--sg-border-subtle);
            padding: 0;
            gap: 2px;
        }
        [data-theme-id="natura-ui"] .sgc-tab {
            border-radius: 3px 3px 0 0;
            padding: var(--natura-space-1) var(--natura-space-3);
            font-weight: 500;
            color: var(--sg-fg-subtle);
            background: transparent;
            transition: color 200ms var(--natura-ease-growth),
                        background 200ms var(--natura-ease-growth);
        }
        [data-theme-id="natura-ui"] .sgc-tab:hover {
            color: var(--sg-fg);
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="natura-ui"] .sgc-tab.active,
        [data-theme-id="natura-ui"] .sgc-tab[aria-selected="true"] {
            color: var(--sg-color-primary);
            background: var(--sg-color-primary-subtle);
        }

        /* ── Alerts — organic left accent ── */
        [data-theme-id="natura-ui"] .sgc-alert {
            border: 1px solid;
            border-left-width: 2px;
            border-radius: var(--natura-space-1, 3px);
            padding: var(--natura-space-2) var(--natura-space-3);
            font-size: 0.875rem;
        }
        [data-theme-id="natura-ui"] .sgc-alert.sgc-info {
            background: var(--sg-color-info-subtle);
            border-color: var(--sg-color-info);
            color: var(--sg-color-info-hover);
        }
        [data-theme-id="natura-ui"] .sgc-alert.sgc-success {
            background: var(--sg-color-success-subtle);
            border-color: var(--sg-color-success);
            color: var(--sg-color-success-hover);
        }
        [data-theme-id="natura-ui"] .sgc-alert.sgc-warn {
            background: var(--sg-color-warning-subtle);
            border-color: var(--sg-color-warning);
            color: var(--sg-color-warning-hover);
        }
        [data-theme-id="natura-ui"] .sgc-alert.sgc-danger {
            background: var(--sg-color-danger-subtle);
            border-color: var(--sg-color-danger);
            color: var(--sg-color-danger-hover);
        }

        /* ── Modal / Drawer — organic, lifted ── */
        [data-theme-id="natura-ui"] .sgc-modal-content,
        [data-theme-id="natura-ui"] .sgc-drawer-content {
            background: var(--sg-surface);
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--natura-space-3, 8px);
            box-shadow: var(--sg-shadow-xl);
        }
        [data-theme-id="natura-ui"] .sgc-modal-header,
        [data-theme-id="natura-ui"] .sgc-drawer-header {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-space-3);
        }
        [data-theme-id="natura-ui"] .sgc-modal-body,
        [data-theme-id="natura-ui"] .sgc-drawer-body {
            padding: var(--natura-space-3);
        }
        [data-theme-id="natura-ui"] .sgc-modal-footer,
        [data-theme-id="natura-ui"] .sgc-drawer-footer {
            border-top: 1px solid var(--sg-divider);
            padding: var(--natura-space-2) var(--natura-space-3);
        }

        /* ── Navigation — organic left glow ── */
        [data-theme-id="natura-ui"] .sgc-nav-link {
            border-radius: var(--natura-space-1, 3px);
            margin: 1px;
            padding: var(--natura-space-1) var(--natura-space-3);
            font-size: 0.8125rem;
            color: var(--sg-fg-subtle);
            transition: background 200ms var(--natura-ease-growth),
                        color      200ms var(--natura-ease-growth),
                        box-shadow 200ms var(--natura-ease-growth);
        }
        [data-theme-id="natura-ui"] .sgc-nav-link:hover {
            background: var(--sg-bg-subtle);
            color: var(--sg-fg);
        }
        [data-theme-id="natura-ui"] .sgc-nav-link.active {
            background: var(--sg-color-primary-subtle);
            color: var(--sg-color-primary);
            font-weight: 600;
            box-shadow: inset 2px 0 0 0 var(--sg-color-primary);
        }
        [data-theme-id="natura-ui"] .sgc-nav-section {
            padding: var(--natura-space-4) var(--natura-space-3) var(--natura-space-1);
            color: var(--sg-fg-muted);
            font-weight: 600;
            font-size: 0.75rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        /* ── Tables — organic, spacious ── */
        [data-theme-id="natura-ui"] .sg-table,
        [data-theme-id="natura-ui"] .sgc-table {
            border: 1px solid var(--sg-border-subtle);
            border-radius: var(--natura-space-2, 5px);
        }
        [data-theme-id="natura-ui"] .sgc-table thead th {
            background: var(--sg-bg-subtle);
            border-bottom: 1px solid var(--sg-border);
            color: var(--sg-fg-subtle);
            font-weight: 600;
            padding: var(--natura-space-1) var(--natura-space-2);
        }
        [data-theme-id="natura-ui"] .sgc-table tbody td {
            border-bottom: 1px solid var(--sg-divider);
            padding: var(--natura-space-1) var(--natura-space-2);
        }
        [data-theme-id="natura-ui"] .sgc-table tbody tr:last-child td {
            border-bottom: none;
        }
        [data-theme-id="natura-ui"] .sgc-table tbody tr:hover td {
            background: var(--sg-bg-subtle);
        }
        [data-theme-id="natura-ui"] .sgc-table tbody tr:nth-child(even) td {
            background: var(--sg-bg-subtle);
        }

        /* ── Badge / Chip — organic pills ── */
        [data-theme-id="natura-ui"] .sgc-badge,
        [data-theme-id="natura-ui"] .sgc-chip {
            border-radius: 9999px;
            padding: 1px var(--natura-space-1, 3px);
            font-size: 0.75rem;
            font-weight: 500;
        }

        /* ── Tooltip — organic bubble ── */
        [data-theme-id="natura-ui"] .sgc-tooltip {
            background: var(--sg-fg);
            color: var(--sg-bg);
            border-radius: var(--natura-space-1, 3px);
            padding: 2px var(--natura-space-2);
            font-size: 0.75rem;
            box-shadow: var(--sg-shadow-md);
        }

        /* ── Scrollbar — organic, subtle ── */
        [data-theme-id="natura-ui"] ::-webkit-scrollbar {
            width: 6px;
            height: 6px;
        }
        [data-theme-id="natura-ui"] ::-webkit-scrollbar-track {
            background: transparent;
        }
        [data-theme-id="natura-ui"] ::-webkit-scrollbar-thumb {
            background: var(--sg-border-strong);
            border-radius: 9999px;
            border: 1px solid var(--sg-bg);
        }
        [data-theme-id="natura-ui"] ::-webkit-scrollbar-thumb:hover {
            background: var(--sg-fg-muted);
        }

        /* ── Selection — sky tint ── */
        [data-theme-id="natura-ui"] ::selection {
            background: var(--sg-color-primary-muted);
            color: var(--sg-fg);
        }

        /* ── Focus ring — organic glow ── */
        [data-theme-id="natura-ui"] :focus-visible {
            outline: none;
            box-shadow: 0 0 0 2px var(--sg-bg),
                        0 0 0 4px var(--sg-color-primary);
            border-radius: 2px;
        }

        /* ── Breadcrumb — natural separator ── */
        [data-theme-id="natura-ui"] .sgc-breadcrumb-separator {
            color: var(--sg-fg-muted);
            margin: 0 var(--natura-space-1, 3px);
        }
        [data-theme-id="natura-ui"] .sgc-breadcrumb-item {
            color: var(--sg-fg-subtle);
            transition: color 150ms var(--natura-ease-growth);
        }
        [data-theme-id="natura-ui"] .sgc-breadcrumb-item:hover {
            color: var(--sg-color-primary);
        }
        [data-theme-id="natura-ui"] .sgc-breadcrumb-item.active {
            color: var(--sg-fg);
            font-weight: 600;
        }

        /* ── Progress bar — organic fill ── */
        [data-theme-id="natura-ui"] .sgc-progress-track {
            background: var(--sg-bg-muted);
            border-radius: 9999px;
            overflow: hidden;
        }
        [data-theme-id="natura-ui"] .sgc-progress-fill {
            background: var(--sg-color-primary);
            border-radius: 9999px;
            transition: width 600ms var(--natura-ease-growth);
        }

        /* ── Skeleton — organic shimmer ── */
        [data-theme-id="natura-ui"] .sgc-skeleton {
            background: linear-gradient(
                90deg,
                var(--sg-bg-muted) 25%,
                var(--sg-bg-subtle) 50%,
                var(--sg-bg-muted) 75%
            );
            background-size: 200% 100%;
            animation: natura-shimmer 1.5s var(--natura-ease-breath) infinite;
            border-radius: 2px;
        }
        @keyframes natura-shimmer {
            0% { background-position: 200% 0; }
            100% { background-position: -200% 0; }
        }

        /* ── Divider — organic line ── */
        [data-theme-id="natura-ui"] .sgc-divider {
            border: none;
            height: 1px;
            background: linear-gradient(
                90deg,
                transparent 0%,
                var(--sg-divider) 15%,
                var(--sg-divider) 85%,
                transparent 100%
            );
            margin: var(--natura-space-3) 0;
        }

        /* ── Reduced motion — respect user preference ── */
        @media (prefers-reduced-motion: reduce) {
            [data-theme-id="natura-ui"] *,
            [data-theme-id="natura-ui"] *::before,
            [data-theme-id="natura-ui"] *::after {
                animation-duration: 0.01ms !important;
                animation-iteration-count: 1 !important;
                transition-duration: 0.01ms !important;
                scroll-behavior: auto !important;
            }
            [data-theme-id="natura-ui"] .sgc-btn:hover:not(:disabled) {
                transform: none !important;
            }
            [data-theme-id="natura-ui"] .sgc-btn:active:not(:disabled) {
                transform: none !important;
            }
        }
        """;

    internal sealed class NaturaTypography : IThemeTypography
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

internal class NaturaPrimitives : IThemePrimitives
{
    // Neutral — cool scale (hue 262°, aligned with brand primary)
    public virtual string Neutral0   => "oklch(1 0 0)";
    public virtual string Neutral50  => "oklch(0.985 0.003 262)";
    public virtual string Neutral100 => "oklch(0.97 0.005 262)";
    public virtual string Neutral200 => "oklch(0.93 0.008 262)";
    public virtual string Neutral300 => "oklch(0.87 0.01 262)";
    public virtual string Neutral400 => "oklch(0.76 0.012 262)";
    public virtual string Neutral500 => "oklch(0.64 0.012 262)";
    public virtual string Neutral600 => "oklch(0.52 0.014 262)";
    public virtual string Neutral700 => "oklch(0.40 0.016 262)";
    public virtual string Neutral800 => "oklch(0.28 0.018 262)";
    public virtual string Neutral900 => "oklch(0.16 0.02 262)";

    // Primary — Royal Cornflower (hue 262°)
    public virtual string Primary50  => "oklch(0.95 0.03 262)";
    public virtual string Primary100 => "oklch(0.90 0.06 262)";
    public virtual string Primary200 => "oklch(0.82 0.10 262)";
    public virtual string Primary300 => "oklch(0.719 0.138 262)"; // Preview
    public virtual string Primary400 => "oklch(0.63 0.18 262)";
    public virtual string Primary500 => "oklch(0.55 0.22 262)";
    public virtual string Primary600 => "oklch(0.48 0.22 262)";
    public virtual string Primary700 => "oklch(0.40 0.20 262)";
    public virtual string Primary800 => "oklch(0.30 0.18 262)";
    public virtual string Primary900 => "oklch(0.20 0.15 262)";

    // Success — Brand green #00A86B → oklch(62.7% 0.194 153.2)
    public virtual string Success50  => "oklch(0.95 0.03 153)";
    public virtual string Success100 => "oklch(0.88 0.07 153)";
    public virtual string Success500 => "oklch(0.627 0.194 153.2)";
    public virtual string Success600 => "oklch(0.55 0.19 153)";
    public virtual string Success700 => "oklch(0.45 0.18 153)";

    // Danger — Brand red #E11D48 → oklch(55.2% 0.244 19.3)
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

    // Info — Azure (hue 254°, near primary)
    public virtual string Info50  => "oklch(0.95 0.03 254)";
    public virtual string Info100 => "oklch(0.88 0.06 254)";
    public virtual string Info500 => "oklch(0.55 0.15 254)";
    public virtual string Info600 => "oklch(0.47 0.15 254)";

    public virtual string FontSans  => "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono  => "'JetBrains Mono', 'Fira Code', ui-monospace, monospace";
    public virtual string FontSerif => "Georgia, 'Times New Roman', serif";

    // φ/Fibonacci radii
    public virtual string RadiusNone => "0";
    public virtual string RadiusXs   => "3px";
    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string Radius2Xl  => "34px";
    public virtual string RadiusFull => "9999px";
}

internal class NaturaSemanticLight : IThemeSemantic
{
    // Light — white with cool blue tint (hue 262°)
    public virtual string BgDefault     => "oklch(0.99 0.004 262)";
    public virtual string BgSubtle      => "oklch(0.97 0.008 262)";
    public virtual string BgMuted       => "oklch(0.935 0.012 262)";
    public virtual string BgEmphasized  => "oklch(0.89 0.016 262)";
    public virtual string BgOverlay     => "oklch(0.15 0.025 262 / 0.40)";
    public virtual string BgGlass       => "oklch(0.99 0.004 262 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.87 0.015 262 / 0.3)";
    public virtual string BlurGlass     => "12px";

    public virtual string Surface         => "oklch(1 0 0)";
    public virtual string SurfaceRaised   => "oklch(1 0 0)";
    public virtual string SurfaceOverlay  => "oklch(1 0 0)";

    public virtual string FgDefault   => "oklch(0.14 0.02 262)";
    public virtual string FgSubtle    => "oklch(0.36 0.015 262)";
    public virtual string FgMuted     => "oklch(0.52 0.012 262)";
    public virtual string FgDisabled  => "oklch(0.68 0.008 262)";
    public virtual string FgInverse   => "oklch(0.99 0.004 262)";
    public virtual string FgLink      => "oklch(0.56 0.22 262)";
    public virtual string FgLinkHover => "oklch(0.50 0.22 262)";

    public virtual string BorderDefault => "oklch(0.88 0.012 262)";
    public virtual string BorderSubtle  => "oklch(0.93 0.01 262)";
    public virtual string BorderStrong  => "oklch(0.80 0.015 262)";
    public virtual string BorderFocus   => "oklch(0.56 0.22 262)";
    public virtual string Divider       => "oklch(0.93 0.01 262)";

    // Primary — Royal Cornflower
    public virtual string ColorPrimary        => "oklch(0.56 0.22 262)";
    public virtual string ColorPrimarySubtle  => "oklch(0.94 0.04 262)";
    public virtual string ColorPrimaryMuted   => "oklch(0.85 0.08 262)";
    public virtual string ColorPrimaryHover   => "oklch(0.50 0.23 262)";
    public virtual string ColorPrimaryActive  => "oklch(0.44 0.22 262)";
    public virtual string ColorPrimaryFg      => "oklch(0.99 0 0)";

    // Success — Brand green #00A86B
    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.94 0.04 153)";
    public virtual string ColorSuccessHover   => "oklch(0.57 0.19 153)";
    public virtual string ColorSuccessFg      => "oklch(0.99 0 0)";

    // Danger — Brand red #E11D48
    public virtual string ColorDanger         => "oklch(0.552 0.244 19.3)";
    public virtual string ColorDangerSubtle   => "oklch(0.94 0.05 19)";
    public virtual string ColorDangerHover    => "oklch(0.50 0.25 19)";
    public virtual string ColorDangerFg       => "oklch(0.99 0 0)";

    // Warning — Brand amber #EAB308
    public virtual string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public virtual string ColorWarningSubtle  => "oklch(0.96 0.04 83)";
    public virtual string ColorWarningHover   => "oklch(0.70 0.18 83)";
    public virtual string ColorWarningFg      => "oklch(0.14 0.02 262)";

    // Info
    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.94 0.035 254)";
    public virtual string ColorInfoHover      => "oklch(0.50 0.15 254)";
    public virtual string ColorInfoFg         => "oklch(0.99 0 0)";

    // Typography — φ-modular with Inter
    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.8125rem";
    public virtual string TextBase => "1rem";
    public virtual string TextLg   => "1.25rem";

    public virtual string TextXs   => "0.6875rem";
    public virtual string TextXl   => "1.25rem";
    public virtual string Text2Xl  => "1.5rem";
    public virtual string Text3Xl  => "2rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    // Тени — холодные, brand-aligned
    public virtual string ShadowXs => "0 1px 1px 0 oklch(0.14 0.02 262 / 0.04)";
    public virtual string ShadowSm => "0 1px 2px 0 oklch(0.14 0.02 262 / 0.06), 0 1px 1px -1px oklch(0.14 0.02 262 / 0.06)";
    public virtual string ShadowMd => "0 2px 4px -1px oklch(0.14 0.02 262 / 0.08), 0 1px 2px -1px oklch(0.14 0.02 262 / 0.06)";
    public virtual string ShadowLg => "0 8px 16px -4px oklch(0.14 0.02 262 / 0.10), 0 2px 4px -2px oklch(0.14 0.02 262 / 0.06)";
    public virtual string ShadowXl => "0 16px 32px -8px oklch(0.14 0.02 262 / 0.14), 0 4px 8px -4px oklch(0.14 0.02 262 / 0.08)";

    public virtual string RadiusSm   => "5px";
    public virtual string RadiusMd   => "8px";
    public virtual string RadiusLg   => "13px";
    public virtual string RadiusXl   => "21px";
    public virtual string RadiusFull => "9999px";

    public virtual string TransitionFast => "120ms cubic-bezier(0.37, 0, 0.63, 1)";
    public virtual string TransitionBase => "200ms cubic-bezier(0.19, 1, 0.22, 1)";
    public virtual string TransitionSlow => "350ms cubic-bezier(0.19, 1, 0.22, 1)";

    public virtual string FocusRing       => "0 0 0 2px #fff, 0 0 0 4px oklch(0.56 0.22 262)";
    public virtual string FocusRingDanger => "0 0 0 2px #fff, 0 0 0 4px oklch(0.552 0.244 19.3)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class NaturaSemanticDark : IThemeSemantic
{
    // Dark — слои и фоны по техзаданию
    public virtual string BgDefault     => "oklch(0.123 0.007 256)";  // bg-app
    public virtual string BgSubtle      => "oklch(0.21 0.022 262)";    // element
    public virtual string BgMuted       => "oklch(0.18 0.02 260)";     // между element и surface
    public virtual string BgEmphasized  => "oklch(0.24 0.024 262)";    // чуть светлее element
    public virtual string BgOverlay     => "oklch(0 0 0 / 0.72)";
    public virtual string BgGlass       => "oklch(0.123 0.007 256 / 0.7)";
    public virtual string BorderGlass   => "oklch(0.99 0 0 / 0.08)";
    public virtual string BlurGlass     => "16px";

    public virtual string Surface         => "oklch(0.142 0.015 260)";  // bg-surface
    public virtual string SurfaceRaised   => "oklch(0.165 0.018 260)";  // bg-overlay
    public virtual string SurfaceOverlay  => "oklch(0.165 0.018 260)";  // bg-overlay

    // Foreground — типографика
    public virtual string FgDefault   => "oklch(0.98 0 0)";            // text-primary
    public virtual string FgSubtle    => "oklch(0.88 0.01 250)";       // text-secondary
    public virtual string FgMuted     => "oklch(0.65 0.015 250)";      // text-muted
    public virtual string FgDisabled  => "oklch(0.40 0.012 262)";
    public virtual string FgInverse   => "oklch(0.123 0.007 256)";
    public virtual string FgLink      => "oklch(0.56 0.22 262)";
    public virtual string FgLinkHover => "oklch(0.62 0.22 262)";

    // Borders — границы и разделители
    public virtual string BorderDefault => "oklch(0.255 0.025 262)";   // border-strong
    public virtual string BorderSubtle  => "oklch(0.18 0.015 262)";    // border-weak
    public virtual string BorderStrong  => "oklch(0.30 0.025 262)";
    public virtual string BorderFocus   => "oklch(0.56 0.22 262)";
    public virtual string Divider       => "oklch(0.18 0.015 262)";    // border-weak

    // Primary — Royal Cornflower (hue 262°)
    public virtual string ColorPrimary        => "oklch(0.56 0.22 262)";
    public virtual string ColorPrimarySubtle  => "oklch(0.20 0.05 262)";
    public virtual string ColorPrimaryMuted   => "oklch(0.28 0.08 262)";
    public virtual string ColorPrimaryHover   => "oklch(0.62 0.22 262)";
    public virtual string ColorPrimaryActive  => "oklch(0.50 0.22 262)";
    public virtual string ColorPrimaryFg      => "oklch(0.98 0 0)";

    // Success — #00A86B / oklch(62.7% 0.194 153.2)
    public virtual string ColorSuccess        => "oklch(0.627 0.194 153.2)";
    public virtual string ColorSuccessSubtle  => "oklch(0.20 0.04 153)";
    public virtual string ColorSuccessHover   => "oklch(0.70 0.18 153)";
    public virtual string ColorSuccessFg      => "oklch(0.98 0 0)";

    // Danger — #E11D48 / oklch(55.2% 0.244 19.3)
    public virtual string ColorDanger         => "oklch(0.552 0.244 19.3)";
    public virtual string ColorDangerSubtle   => "oklch(0.22 0.06 19)";
    public virtual string ColorDangerHover    => "oklch(0.62 0.24 19)";
    public virtual string ColorDangerFg       => "oklch(0.98 0 0)";

    // Warning — #EAB308 / oklch(76.7% 0.181 83.1)
    public virtual string ColorWarning        => "oklch(0.767 0.181 83.1)";
    public virtual string ColorWarningSubtle  => "oklch(0.24 0.04 75)";
    public virtual string ColorWarningHover   => "oklch(0.82 0.16 83)";
    public virtual string ColorWarningFg      => "oklch(0.123 0.007 256)";

    // Info
    public virtual string ColorInfo           => "oklch(0.55 0.15 254)";
    public virtual string ColorInfoSubtle     => "oklch(0.20 0.04 254)";
    public virtual string ColorInfoHover      => "oklch(0.60 0.14 254)";
    public virtual string ColorInfoFg         => "oklch(0.98 0 0)";

    public virtual string Font     => "'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif";
    public virtual string FontMono => "'JetBrains Mono', ui-monospace, monospace";
    public virtual string TextSm   => "0.8125rem";
    public virtual string TextBase => "1rem";
    public virtual string TextLg   => "1.25rem";

    public virtual string TextXs   => "0.6875rem";
    public virtual string TextXl   => "1.25rem";
    public virtual string Text2Xl  => "1.5rem";
    public virtual string Text3Xl  => "2rem";

    public virtual string FontWeightNormal   => "400";
    public virtual string FontWeightMedium   => "500";
    public virtual string FontWeightSemibold => "600";
    public virtual string FontWeightBold     => "700";

    public virtual string LineHeightTight   => "1.25";
    public virtual string LineHeightNormal  => "1.5";
    public virtual string LineHeightRelaxed => "1.75";

    // Тени — глубокие, металлические
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

    public virtual string FocusRing       => "0 0 0 2px oklch(0.123 0.007 256), 0 0 0 4px oklch(0.56 0.22 262)";
    public virtual string FocusRingDanger => "0 0 0 2px oklch(0.123 0.007 256), 0 0 0 4px oklch(0.552 0.244 19.3)";

    public virtual int ZDropdown => 1000;
    public virtual int ZSticky   => 1020;
    public virtual int ZModal    => 1050;
    public virtual int ZToast    => 1070;
    public virtual int ZTooltip  => 1100;
}

internal class NaturaComponents : IThemeComponents
{
    // Размеры уменьшены ещё на шаг: всё compact
    public virtual string BtnRadius     => "5px";     // fib-2
    public virtual string BtnFontSize   => "0.75rem";
    public virtual string BtnFontWeight => "600";
    public virtual string BtnHeight     => "30px";    // compact
    public virtual string BtnHeightSm   => "28px";
    public virtual string BtnHeightLg   => "34px";    // fib-7

    public virtual string InputRadius   => "3px";     // fib-1
    public virtual string InputFontSize => "0.8125rem";
    public virtual string InputHeight   => "30px";
    public virtual string InputHeightSm => "28px";
    public virtual string InputHeightLg => "34px";

    public virtual string CardRadius      => "5px";   // fib-2
    public virtual string CardPadding     => "8px";   // fib-3
    public virtual string CardBorderColor => "var(--sg-border-subtle)";
    public virtual string CardBg          => "var(--sg-surface)";

    public virtual string ModalRadius => "8px";     // fib-3

    public virtual string TableRadius          => "5px";   // fib-2
    public virtual string TableHeaderFontWeight => "650";

    public virtual string TabsIndicatorHeight => "2px";

    public virtual string TooltipMaxWidth => "260px";

    public virtual string HeaderBg    => "var(--sg-bg)";
    public virtual string HeaderFg    => "var(--sg-fg)";
    public virtual string NavBg       => "var(--sg-bg-subtle)";
    public virtual string NavFg       => "var(--sg-fg-subtle)";
    public virtual string NavActiveBg => "var(--sg-color-primary-subtle)";
    public virtual string NavActiveFg => "var(--sg-color-primary)";
}
